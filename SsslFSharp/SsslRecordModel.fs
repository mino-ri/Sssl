namespace SsslFSharp
open System
open System.Linq.Expressions
open System.Reflection
open ExprHelper

[<Struct>]
type internal GetAccessor<'T> =
    { Type: Type
      Getter: Func<'T, obj>
      Default: obj }


[<AbstractClass; Sealed>]
type internal SsslRecordModel<'T>() =
    static member val ModelInfo =
        let t = typeof<'T>
        let bindingFlags = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance
        let ob = parameter<'T> "obj"
        let accessors =
            query {
                for prop in t.GetProperties(bindingFlags) do
                let att = prop.GetCustomAttribute<CompilationMappingAttribute>()
                where (isNotNullf att)
                sortBy att.SequenceNumber
                select (prop.Name, {
                    Type = prop.PropertyType
                    Getter =
                        Expression.Property(ob, prop)
                        |> convert<obj>
                        |> compile<Func<'T, obj>> [| ob |]
                    Default = SsslActivator.getDefault prop.PropertyType
                })
            }
            |> Seq.toArray
        let constructor = Option.toObj <| option {
            let! ctor =
                t.GetConstructors(BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance)
                |> Array.tryFind (fun ctor -> ctor.GetParameters().Length = accessors.Length)
            let args = parameter<obj[]> "args"
            let ctorArgs = ctor.GetParameters() |> Array.mapi (fun i p ->
                    args
                    |> arrayIndex (constExpr i)
                    |> convertOf p.ParameterType)
            return
                Expression.New(ctor, ctorArgs)
                |> convert<obj>
                |> compile<Func<obj[], obj>> [| args |]
        }
        {
            CanCreateInstance = isNotNull constructor
            ConvertFrom = fun ob ->
                let typed = ob :?> 'T
                seq {
                    for name, accessor in accessors ->
                        name, accessor.Type, accessor.Getter.Invoke(typed)
                }
            ConvertTo = fun members ->
                let membersMap = Map(members)
                constructor.Invoke([|
                    for name, accessor in accessors ->
                        membersMap.TryFind name
                        |> Option.defaultValue accessor.Default
                |])
            RequiredValues = accessors |> Seq.map (fun (k, v) -> k, v.Type) |> Map
        }
