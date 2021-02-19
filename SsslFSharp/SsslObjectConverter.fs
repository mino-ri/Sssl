module SsslFSharp.SsslObjectConverter
open System
open System.Reflection
open System.Collections.Concurrent

let private cache = ConcurrentDictionary<Type, ObjectModelInfo>()

let private getModelInfo (t: Type) =
    cache.GetOrAdd(t, fun t ->
        let isRecord =
            let att = t.GetCustomAttribute<CompilationMappingAttribute>()
            isNotNullf att &&
            (att.SourceConstructFlags = SourceConstructFlags.SumType ||
             att.SourceConstructFlags = SourceConstructFlags.RecordType)
        let baseType =
            if isRecord
            then typedefof<SsslRecordModel<_>>
            else typedefof<SsslObjectModel<_>>
        baseType.MakeGenericType(t)
            .GetProperty("ModelInfo", BindingFlags.Static ||| BindingFlags.NonPublic)
            .GetValue(null)
        :?> ObjectModelInfo)

let tryConvertFrom (converter: ISsslConverter) (value: obj) expected =
    let t = value.GetType()
    getModelInfo(t).ConvertFrom(value)
    |> chooseAll (fun (name, exType, value) ->
        converter.TryConvertFrom(value, exType)
        |> Option.map (fun ssslValue -> Sssl.Pair(name, ssslValue)))
    |> Option.map (fun contents ->
        Sssl.Record(SsslType.getNameFrom expected t, SsslRecordType.Braces, contents.ToArray()))

let tryConvertTo (converter: ISsslConverter) (options: ObjectConversionOptions) (sssl: Sssl) (expected: Type) =
    let allowUnknownMember = options.HasFlag(ObjectConversionOptions.AllowUnknownMember)
    let allowMissingMember = options.HasFlag(ObjectConversionOptions.AllowMissingMember)
    match sssl with
    | Sssl.Record(SsslType.Load expected (actualType), _, contents) as record ->
        let modelInfo = getModelInfo(actualType)
        let isMembersValid() =
            let existsKey = function
                | Sssl.Pair(name, _) -> modelInfo.RequiredValues.ContainsKey(name)
                | _ -> true
            let existsMember (KeyValue(name, _)) = record.HasName(name)
            (allowUnknownMember || Array.forall existsKey contents) &&
            (allowMissingMember || Seq.forall existsMember modelInfo.RequiredValues)
        if modelInfo.CanCreateInstance && isMembersValid() then
            modelInfo.RequiredValues
            |> chooseAll (fun (KeyValue(name, valueType)) ->
                match record.TryGetByName(name) with
                | None -> if allowMissingMember then Some(None) else None
                | Some(value) ->
                    converter.TryConvertTo(value, valueType)
                    |> Option.map (fun v -> Some(name, v)))
            |> Option.map (fun valueMap -> modelInfo.ConvertTo(Seq.choose id valueMap))
        else
            None
    | _ -> None
