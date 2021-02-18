[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SsslFSharp.SsslConverter
open System
open System.Collections
open System.Collections.Generic
open System.Globalization
open System.Runtime.CompilerServices
open BoolOption

let inline stringInvariant (value: #IFormattable) =
    Some(value.ToString(null, CultureInfo.InvariantCulture))

let inline numberCnv (convertTo: float -> ^T) =
    NumberConverterPart(float >> Some, convertTo >> Some) :> ISsslConverterPart

let boolCnv = BoolConverterPart(Some, Some) :> ISsslConverterPart

let stringCnv = StringConverterPart(Some, Some) :> ISsslConverterPart

let charCnv =
    StringConverterPart(
        string >> Some,
        (fun str -> if str.Length = 1 then Some(str.[0]) else None))
    :> ISsslConverterPart

let tryParseCnv tryConvertFrom (tryConvertTo: string -> bool * 'T) =
    StringConverterPart(
        tryConvertFrom,
        fun str ->
            let ok, result = tryConvertTo str
            if ok then Some(result) else None)
    :> ISsslConverterPart

let dateTimeCnv = tryParseCnv stringInvariant DateTime.TryParse

let dateTimeOffsetCnv = tryParseCnv stringInvariant DateTimeOffset.TryParse

let enumCnv =
    { new ISsslConverterPart with
        member _.TryConvertFrom(_, value, expected) =
            Sssl.String(value.ToString())
            |> SsslType.wrapTyped expected (value.GetType())
            |> Some
                
        member _.TryConvertTo(_, sssl, expected) =
            let (|TargetType|_|) (t: Type) = if t.IsEnum then Some(t) else None
            match sssl, expected with
            | Sssl.String(value), TargetType(t)
            | Sssl.Pair(SsslType.Load expected (TargetType(t)), Sssl.String(value)), _ ->
                Enum.TryParse(t, value) |> BoolOption.toOption
            | _ -> None
    }

let objCnv options =
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            SsslObjectConverter.tryConvertFrom converter value expected
        
        member _.TryConvertTo(converter, sssl, expected) =
            SsslObjectConverter.tryConvertTo converter options sssl expected
    }

let pairCnv =
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            option {
                let pairType = value.GetType()
                let! name, v = SsslActivator.(|DynamicKeyValue|_|) value
                let! valueSssl = converter.TryConvertFrom(v, pairType.TypeArgs.[1])
                return Sssl.Pair(name, valueSssl) |> SsslType.wrapTyped expected pairType
            }
    
        member _.TryConvertTo(converter, sssl, expected) =
            let (|TargetType|_|) (t: Type) =
                if t.IsGenericOf(typedefof<KeyValuePair<_, _>>) && t.TypeArgs.[0] = typeof<string>
                then Some(t.TypeArgs.[1])
                else None
            match sssl, expected with
            | Sssl.Pair(SsslType.Load expected (TargetType(t)), Sssl.Pair(name, value)), _
            | Sssl.Pair(name, value), TargetType(t) ->
                converter.TryConvertTo(value, t.TypeArgs.[1])
                |> Option.map (SsslActivator.createKeyValuePair t name)
            | _ -> None
    }

let tupleCnv =
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            option {
                let! tuple = tryCast<ITuple> value
                let tupleType = value.GetType()
                if tupleType.IsGenericType && tupleType.TypeArgs.Length = tuple.Length then
                    let! contents =
                        tupleType.TypeArgs
                        |> Seq.mapi (fun i t -> tuple.[i], t)
                        |> chooseAll converter.TryConvertFrom
                    let typeName =
                        if tupleType = expected then ""
                        else SsslType.getName expected.Assembly tupleType
                    return Sssl.Record(typeName, SsslRecordType.Parentheses, contents.ToArray())
            }
    
        member _.TryConvertTo(converter, sssl, expected) =
            let typeArgs = expected.TypeArgs
            match sssl with
            | Sssl.Record(_, _, contents) when contents.Length = typeArgs.Length ->
                Seq.zip contents typeArgs
                |> chooseAll converter.TryConvertTo
                |> Option.map (fun lst -> SsslActivator.createTuple expected (lst.ToArray()))
            | _ -> None
    }

let dictionaryCnv =
    let getItemType (expected: Type) = typedefof<KeyValuePair<_, _>>.MakeGenericType(expected.TypeArgs)
    let getConcreteType (expected: Type) = typedefof<Dictionary<_, _>>.MakeGenericType(expected.TypeArgs)
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            option {
                let itemType = getItemType (value.GetType())
                let! enm = tryCast<IEnumerable> value
                let! lst = chooseAllE (fun item -> converter.TryConvertFrom(item, itemType)) enm
                let name =
                    if itemType = getItemType expected then ""
                    else SsslType.getName expected.Assembly (itemType.MakeArrayType())
                return Sssl.Record(name, SsslRecordType.Brackets, lst.ToArray())
            }
    
        member _.TryConvertTo(converter, sssl, expected) =
            match sssl with
            | Sssl.Record(_, _, contents) ->
                let itemType = getItemType expected
                chooseAll (fun item -> converter.TryConvertTo(item, itemType)) contents
                |> Option.map (fun lst ->
                    let array = Array.CreateInstance(itemType, contents.Length)
                    for i = 0 to array.Length - 1 do
                        array.SetValue(lst.[i], i)
                    SsslActivator.createCollection (getConcreteType expected) itemType array)
            | _ -> None
    }

let arrayCnv =
    let getItemType (expected: Type) =
        if expected.IsArray then expected.GetElementType()
        elif expected.TypeArgs.Length = 1 then expected.TypeArgs.[0]
        else typeof<obj>
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            option {
                let itemType = getItemType (value.GetType())
                let! enm = tryCast<IEnumerable> value
                let! lst = chooseAllE (fun item -> converter.TryConvertFrom(item, itemType)) enm
                let name =
                    if itemType = getItemType expected then ""
                    else SsslType.getName expected.Assembly (itemType.MakeArrayType())
                return Sssl.Record(name, SsslRecordType.Brackets, lst.ToArray())
            }
        
        member _.TryConvertTo(converter, sssl, expected) =
            match sssl with
            | Sssl.Record(_, _, contents) ->
                let itemType = getItemType expected
                chooseAll (fun item -> converter.TryConvertTo(item, itemType)) contents
                |> Option.map (fun lst ->
                    let array = Array.CreateInstance(itemType, contents.Length)
                    for i = 0 to array.Length - 1 do
                        array.SetValue(lst.[i], i)
                    box array)
            | _ -> None
    }

let rawCnv =
    { new ISsslConverterPart with
        member _.TryConvertFrom(_, value, expected) =
            tryCast<Sssl> value
            |> Option.map (SsslType.wrapTyped expected typeof<Sssl>)
    
        member _.TryConvertTo(_, sssl, expected) =
            let (|TargetType|_|) (t: Type) = if typeof<Sssl>.IsAssignableFrom(t) then Some() else None
            match sssl, expected with
            | Sssl.Pair(SsslType.Load expected TargetType, sssl), _ 
            | sssl, TargetType -> Some(box sssl)
            | _ -> None
    }

let add targetType cnv builder =
    { builder with ConcreteConverters = (targetType, cnv) :: builder.ConcreteConverters }

let addRange cnvs builder =
    { builder with ConcreteConverters = cnvs @ builder.ConcreteConverters }

let addWhen predicate cnv builder =
    { builder with ConditionalConverters = (predicate, cnv) :: builder.ConditionalConverters }

let addWhenRange cnvs builder =
    { builder with ConditionalConverters = cnvs @ builder.ConditionalConverters }

let addNumbers builder =
    let inline numberCnvWithType (convertTo: float -> ^T) =
        typeof< ^T >, numberCnv convertTo
    { builder with
        ConcreteConverters =
            numberCnvWithType int8 ::
            numberCnvWithType int16 ::
            numberCnvWithType int32 ::
            numberCnvWithType int64 ::
            numberCnvWithType uint8 ::
            numberCnvWithType uint16 ::
            numberCnvWithType uint32 ::
            numberCnvWithType uint64 ::
            numberCnvWithType float32 ::
            (typeof<float>, upcast NumberConverterPart(Some, Some)) ::
            numberCnvWithType decimal ::
            builder.ConcreteConverters
    }

let addBaseValues options builder =
    let arrayTypes = [|
        typedefof<IEnumerable<_>>
        typedefof<ICollection<_>>
        typedefof<IList<_>>
        typedefof<IReadOnlyCollection<_>>
        typedefof<IReadOnlyList<_>>
    |]
    let dictionaryTypes = [|
        typedefof<Dictionary<_, _>>
        typedefof<IDictionary<_, _>>
        typedefof<IReadOnlyDictionary<_, _>>
    |]
    builder
    |> addWhen (fun _ -> true) (objCnv options)
    |> addWhen (fun t -> t.IsArray || t.IsGenericOfAny(arrayTypes)) arrayCnv
    |> addWhen (fun t -> t.IsGenericOfAny(dictionaryTypes)) dictionaryCnv

let addPrimitiveValues builder =
    builder
    |> addWhen typeof<ITuple>.IsAssignableFrom tupleCnv
    |> addWhen (fun t -> t.IsGenericOf(typedefof<KeyValuePair<_, _>>) && t.TypeArgs.[0] = typeof<string>) pairCnv
    |> addWhen (fun t -> t.IsEnum) enumCnv
    |> addRange [
        typeof<bool>, boolCnv
        typeof<char>, charCnv
        typeof<string>, stringCnv
        typeof<DateTime>, dateTimeCnv
        typeof<DateTimeOffset>, dateTimeOffsetCnv
    ]
    |> addNumbers
    |> add typeof<Sssl> rawCnv

let emptyBuilder = { ConcreteConverters = []; ConditionalConverters = [] }

let build (builder: SsslConverterBuilder) = SsslStandardConverter(builder)

let defaultConverter =
    emptyBuilder
    |> addBaseValues (ObjectConversionOptions.AllowMissingMember ||| ObjectConversionOptions.AllowUnknownMember)
    |> addPrimitiveValues
    |> build
