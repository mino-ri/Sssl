module SsslFSharp.SsslObjectConverter
open System
open System.Reflection
open System.Collections.Concurrent
open System.Collections.Generic

let private cache = ConcurrentDictionary<Type, ObjectModelInfo>()

let private getModelInfo (t: Type) =
    cache.GetOrAdd(t, fun t ->
        typedefof<SsslObjectModel<_>>.MakeGenericType(t)
            .GetProperty("ModelInfo", BindingFlags.Static ||| BindingFlags.NonPublic)
            .GetValue(null)
        :?> ObjectModelInfo)

let tryConvertFrom (converter: ISsslConverter) (value: obj) (expected: Type) =
    let t = value.GetType()
    let values = getModelInfo(t).ConvertFrom(value)
    let contents = ResizeArray()
    let rec getValue = function
        | SEmpty ->
            Some(Sssl.Record(SsslType.getName expected.Assembly t, SsslRecordType.Braces, contents.ToArray()))
        | SCons((name, exType, value), tail) ->
            match converter.TryConvertFrom(value, exType) with
            | None -> None
            | Some(ssslValue) ->
                contents.Add(Sssl.Pair(name, ssslValue))
                getValue tail
    use enumerator = values.GetEnumerator()
    getValue enumerator

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
            let valueMap = ResizeArray()
            let rec getValues = function
                | SEmpty -> true
                | SCons(KeyValue(name, valueType), tail) ->
                    let converted =
                        match record.TryGetByName(name) with
                        | None -> if allowMissingMember then Ok(None) else Error()
                        | Some(value) ->
                            match converter.TryConvertTo(value, valueType) with
                            | None -> Error()
                            | Some(c) -> Ok(Some(c))
                    match converted with
                    | Error() -> false
                    | Ok(valueOpt) ->
                        valueOpt |> Option.iter (fun c -> valueMap.Add(name, c))
                        getValues tail
            let success =
                using
                    ((modelInfo.RequiredValues :> IEnumerable<KeyValuePair<string, Type>>).GetEnumerator())
                    getValues
            if success
            then Some(modelInfo.ConvertTo(upcast valueMap))
            else None
        else
            None
    | _ -> None
