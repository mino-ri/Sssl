﻿[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SsslFSharp.SsslConverter
open System
open System.Collections.Generic
open System.Globalization
open System.Runtime.CompilerServices

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
                if pairType.TypeArgs.[0] = typeof<string> then
                    return Sssl.Pair(name :?> string, valueSssl) |> SsslType.wrapTyped expected pairType
                else
                    let! nameSssl = converter.TryConvertFrom(name, pairType.TypeArgs.[0])
                    let typeName = SsslType.getName expected pairType
                    return Sssl.Record(typeName, SsslRecordType.Parentheses, [| nameSssl; valueSssl |])
            }
    
        member _.TryConvertTo(converter, sssl, expected) =
            let (|TargetType|_|) (t: Type) =
                if t.IsGenericOf(typedefof<KeyValuePair<_, _>>)
                then Some(t.TypeArgs.[0], t.TypeArgs.[1])
                else None
            let makePairType k v = typedefof<KeyValuePair<_, _>>.MakeGenericType(k, v)
            match sssl, expected with
            | Sssl.Pair(SsslType.Load expected (TargetType(kType, vType)), Sssl.Pair(name, value)), _
            | Sssl.Pair(name, value), TargetType(kType, vType) when kType = typeof<string> ->
                converter.TryConvertTo(value, vType)
                |> Option.map (SsslActivator.createKeyValuePair (makePairType kType vType) name)
            | Sssl.Record(SsslType.Load expected (TargetType(kType, vType)), _, contents), _
            | Sssl.Record("", _, contents), TargetType(kType, vType) when contents.Length = 2 ->
                option {
                    let! key = converter.TryConvertTo(contents.[0], kType)
                    let! value = converter.TryConvertTo(contents.[1], vType)
                    return SsslActivator.createKeyValuePair (makePairType kType vType) key value
                }
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
                    let typeName = SsslType.getName expected tupleType
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
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            SsslCollectionConverter.tryConvertFrom
                SsslCollectionConverter.getKeyValuePairType
                SsslCollectionConverter.makeDictionaryType
                converter value expected
    
        member _.TryConvertTo(converter, sssl, expected) =
            let itemType = SsslCollectionConverter.getKeyValuePairType expected
            let concreteType = SsslCollectionConverter.makeDictionaryType itemType
            SsslCollectionConverter.tryConvertTo
                itemType
                (fun item -> converter.TryConvertTo(item, itemType))
                (fun array -> SsslActivator.createCollection concreteType itemType array)
                sssl
    }

let mapCnv =
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            SsslCollectionConverter.tryConvertFrom
                SsslCollectionConverter.getKeyValuePairType
                SsslCollectionConverter.makeMapType
                converter value expected
    
        member _.TryConvertTo(converter, sssl, expected) =
            let itemType = SsslCollectionConverter.getKeyValuePairType expected
            let tupleType = SsslCollectionConverter.getTupleType expected
            let concreteType = SsslCollectionConverter.makeDictionaryType itemType
            SsslCollectionConverter.tryConvertTo
                itemType
                (fun item ->
                    match converter.TryConvertTo(item, itemType) with
                    | Some(SsslActivator.DynamicKeyValue(key, value)) ->
                        Some(SsslActivator.createTuple tupleType [| key; value |])
                    | _ -> None)
                (fun array -> SsslActivator.createCollection concreteType tupleType array)
                sssl
    }

let arrayCnv =
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            SsslCollectionConverter.tryConvertFrom
                SsslCollectionConverter.getEnumerableArg
                (fun t -> t.MakeArrayType())
                converter value expected
        
        member _.TryConvertTo(converter, sssl, expected) =
            let itemType = SsslCollectionConverter.getEnumerableArg expected
            SsslCollectionConverter.tryConvertTo
                itemType
                (fun item -> converter.TryConvertTo(item, itemType))
                box
                sssl
    }

let listCnv =
    { new ISsslConverterPart with
        member _.TryConvertFrom(converter, value, expected) =
            SsslCollectionConverter.tryConvertFrom
                SsslCollectionConverter.getEnumerableArg
                (fun t -> typedefof<_ list>.MakeGenericType(t))
                converter value expected
        
        member _.TryConvertTo(converter, sssl, expected) =
            let itemType = SsslCollectionConverter.getEnumerableArg expected
            SsslCollectionConverter.tryConvertTo
                itemType
                (fun item -> converter.TryConvertTo(item, itemType))
                (SsslActivator.createList itemType)
                sssl
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
    |> addWhen (fun t -> t.IsGenericOf(typedefof<_ list>)) listCnv
    |> addWhen (fun t -> t.IsGenericOfAny(dictionaryTypes)) dictionaryCnv
    |> addWhen (fun t -> t.IsGenericOf(typedefof<Map<_, _>>)) mapCnv

let addPrimitiveValues builder =
    builder
    |> addWhen typeof<ITuple>.IsAssignableFrom tupleCnv
    |> addWhen (fun t -> t.IsGenericOf(typedefof<KeyValuePair<_, _>>)) pairCnv
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
