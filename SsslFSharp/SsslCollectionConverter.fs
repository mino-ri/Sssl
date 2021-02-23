module internal SsslFSharp.SsslCollectionConverter
open System
open System.Collections
open System.Collections.Generic
open System.Globalization
open System.Runtime.CompilerServices

let getEnumerableArg (expected: Type) =
    if expected.IsArray then
        expected.GetElementType()
    elif expected.IsGenericOf(typedefof<IEnumerable<_>>) then
        expected.TypeArgs.[0]
    else
        expected.GetInterfaces()
        |> Seq.find (fun t -> t.IsGenericOf(typedefof<IEnumerable<_>>))
        |> (fun t -> t.TypeArgs.[0])

let getDictionaryArgs (expected: Type) =
    expected.GetInterfaces()
    |> Seq.find (fun t -> t.IsGenericOfAny(typedefof<IDictionary<_, _>>, typedefof<IReadOnlyDictionary<_, _>>))
    |> (fun t -> t.TypeArgs)

let getKeyValuePairType (expected: Type) =
    typedefof<KeyValuePair<_, _>>.MakeGenericType(getDictionaryArgs expected)

let getTupleType (expected: Type) =
    typedefof<Tuple<_, _>>.MakeGenericType(getDictionaryArgs expected)

let makeDictionaryType (itemType: Type) = typedefof<Dictionary<_, _>>.MakeGenericType(itemType.TypeArgs)

let makeMapType (itemType: Type) = typedefof<Dictionary<_, _>>.MakeGenericType(itemType.TypeArgs)


let tryConvertFrom
    (getItemType: Type -> Type)
    (getConcreteType: Type -> Type)
    (converter: ISsslConverter)
    (value: obj)
    (expected: Type)
    =
    let itemType = getItemType (value.GetType())
    option {
        let! enm = tryCast<IEnumerable> value
        let! lst = chooseAllE (fun item -> converter.TryConvertFrom(item, itemType)) enm
        let name =
            if itemType = getItemType expected then ""
            else SsslType.getName expected (getConcreteType itemType)
        return Sssl.Record(name, SsslRecordType.Brackets, lst.ToArray())
    }

let tryConvertTo
    (itemType: Type)
    (itemConvert: Sssl -> obj option)
    (createResult: Array -> obj)
    (sssl: Sssl)
    =
    match sssl with
    | Sssl.Record(_, _, contents) ->
        chooseAll itemConvert contents
        |> Option.map (fun lst ->
            let array = Array.CreateInstance(itemType, contents.Length)
            for i = 0 to array.Length - 1 do
                array.SetValue(lst.[i], i)
            createResult array)
    | _ -> None
