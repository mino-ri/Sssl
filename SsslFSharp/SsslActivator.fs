module internal SsslFSharp.SsslActivator
open System
open System.Collections
open System.Collections.Generic
open System.Collections.Concurrent
open System.Linq.Expressions
open ExprHelper

// Tuple
let private tupleConstructorCache = ConcurrentDictionary<Type, Func<obj[], obj>>()

let private createTupleConstructor (t: Type) =
    let arguments = t.GetGenericArguments()
    let ctor = t.GetConstructor(arguments)
    let param = parameter<obj[]> "args"
    let args =
        arguments
        |> Array.mapi (fun i argType ->
            Expression.ArrayIndex(param, Expression.Constant(i))
            |> convertOf argType)
    Expression.New(ctor, args)
    |> convert<obj>
    |> compile<Func<obj[], obj>> [| param |]

let createTuple (t: Type) (args: obj[]) =
    tupleConstructorCache.GetOrAdd(t, createTupleConstructor).Invoke(args)

// KeyValuePair
let private kvPairExtractorCache = ConcurrentDictionary<Type, Func<obj, string * obj>>()

let createKeyValuePair valueType (key: string) (value: obj) =
    createTuple
        (typedefof<KeyValuePair<_, _>>.MakeGenericType(typeof<string>, valueType))
        [| box key; value |]

let private createKvPairExtractor (pairType: Type) =
    let ctor = typeof<string * obj>.GetConstructor([| typeof<string>; typeof<obj> |])
    let pair = parameter<obj> "pair"
    let converted = pair |> convertOf pairType
    Expression.New(ctor,
        Expression.Property(converted, "Key"),
        Expression.Property(converted, "Value") |> convert<obj>)
    |> compile<Func<obj, string * obj>> [| pair |]

let (|DynamicKeyValue|_|) (pair: obj) =
    let pairType = pair.GetType()
    if pairType.IsGenericOf(typedefof<KeyValuePair<_, _>>) && pairType.TypeArgs.[0] = typeof<string>
    then Some(kvPairExtractorCache.GetOrAdd(pairType.TypeArgs.[1], createKvPairExtractor).Invoke(pair))
    else None

// Collection
let private collectionConstructorCache = ConcurrentDictionary<Type, Func<Array, obj>>()

let private createCollectionConstructor (t: Type) (itemType: Type) =
    let ctor = t.GetConstructor([| typeof<IEnumerable<_>>.MakeGenericType(itemType) |])
    let source = parameter<Array> "source"
    Expression.New(ctor, source)
    |> compile<Func<Array, obj>> [| source |]

let createCollection (t: Type) (itemType: Type) (source: Array) =
    collectionConstructorCache.GetOrAdd(t, (fun t -> createCollectionConstructor t itemType)).Invoke(source)
