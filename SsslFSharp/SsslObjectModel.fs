namespace SsslFSharp
open System
open System.Diagnostics
open System.Linq.Expressions
open System.Reflection
open ExprHelper

[<Struct>]
type internal Accessor<'T> =
    { Type: Type
      Getter: Func<'T, obj>
      Setter: Action<'T, obj> }


type internal ObjectModelInfo =
    { CanCreateInstance: bool
      ConvertFrom: obj -> seq<string * Type * obj>
      ConvertTo: seq<string * obj> -> obj
      RequiredValues: Map<string, Type> }


[<Sealed>]
type internal SsslObjectModel<'T> private () =
//    static let modelInfo =

    static member ModelInfo =
        let getCustomName (m: #MemberInfo) isPublic =
            match m.GetCustomAttribute<SsslNameAttribute>() |> Option.ofObj with
            | Some(ssslName) -> Some(ssslName.Name)
            | None ->
                match m.GetCustomAttribute<DebuggerBrowsableAttribute>() |> Option.ofObj with
                | Some(db) when db.State = DebuggerBrowsableState.Never -> None
                | _ ->
                    if isPublic m then Some(m.Name) else None

        let t = typeof<'T>
        let constructor =
            if t.IsValueType then Func<'T>(fun () -> Unchecked.defaultof<'T>)
            else Expression.New(t) |> compile (array.Empty())
        let bindingFlags = BindingFlags.Public ||| BindingFlags.NonPublic ||| BindingFlags.Instance
        let fields = seq {
            for field in t.GetFields(bindingFlags) do
            if isNull (field.GetCustomAttribute<NotSsslFieldAttribute>()) then
                match getCustomName field (fun f -> f.IsPublic) with
                | Some(name) -> yield field, name
                | None -> ()
        }
        let props = seq {
            for prop in t.GetProperties(bindingFlags) do
            if isNull (prop.GetCustomAttribute<NotSsslFieldAttribute>()) then
                match getCustomName prop (fun f -> 
                    not (isNull f.GetMethod) && f.GetMethod.IsPublic &&
                    not (isNull f.SetMethod) && f.SetMethod.IsPublic) with
                | Some(name) -> yield prop, name
                | None -> ()
        }
        let value = parameter<obj> "value"
        let ob = parameter<'T> "obj"
        let createAccessor memberExpr ty =
            {
                Type = ty
                Getter =
                    memberExpr
                    |> convert<obj>
                    |> compile<Func<'T, obj>> [| ob |]
                Setter =
                    value
                    |> convertOf ty
                    |> assignTo memberExpr
                    |> compile [| ob; value |]
            }
        let accessors =
            Seq.append
                (fields |> Seq.map (fun (field, name) ->
                    name, createAccessor (Expression.Field(ob, field)) field.FieldType))
                (props |> Seq.map (fun (prop, name) ->
                    name, createAccessor (Expression.Property(ob, prop)) prop.PropertyType))
            |> Map
        {
            CanCreateInstance = not (isNull constructor)
            ConvertFrom = fun ob ->
                let typed = ob :?> 'T
                seq {
                    for KeyValue(name, accessor) in accessors ->
                        name, accessor.Type, accessor.Getter.Invoke(typed)
                }
            ConvertTo = fun members ->
                let ob = constructor.Invoke()
                for name, value in members do
                    Map.tryFind name accessors
                    |> Option.iter (fun accessor -> accessor.Setter.Invoke(ob, value))
                box ob
            RequiredValues = accessors |> Map.map (fun _ v -> v.Type)
        }
