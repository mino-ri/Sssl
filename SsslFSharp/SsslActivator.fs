module internal SsslFSharp.SsslActivator
open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Linq.Expressions
open System.Reflection
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
let private kvPairExtractorCache = ConcurrentDictionary<Type, Func<obj, obj * obj>>()

let createKeyValuePair pairType (key: obj) (value: obj) =
    createTuple pairType [| key; value |]

let private createKvPairExtractor (pairType: Type) =
    let ctor = typeof<obj * obj>.GetConstructor([| typeof<obj>; typeof<obj> |])
    let pair = parameter<obj> "pair"
    let converted = pair |> convertOf pairType
    Expression.New(ctor,
        Expression.Property(converted, "Key") |> convert<obj>,
        Expression.Property(converted, "Value") |> convert<obj>)
    |> compile<Func<obj, obj * obj>> [| pair |]

let (|DynamicKeyValue|_|) (pair: obj) =
    let pairType = pair.GetType()
    if pairType.IsGenericOf(typedefof<KeyValuePair<_, _>>)
    then Some(kvPairExtractorCache.GetOrAdd(pairType, createKvPairExtractor).Invoke(pair))
    else None

// Collection
let private collectionConstructorCache = ConcurrentDictionary<Type, Func<Array, obj>>()

let private createCollectionConstructor (t: Type) (itemType: Type) =
    let argType = typedefof<IEnumerable<_>>.MakeGenericType(itemType)
    let ctor = t.GetConstructor([| argType |])
    let source = parameter<Array> "source"
    Expression.New(ctor, source |> convertOf argType)
    |> convert<obj>
    |> compile<Func<Array, obj>> [| source |]

let createCollection (t: Type) (itemType: Type) (source: Array) =
    collectionConstructorCache.GetOrAdd(t, (fun t -> createCollectionConstructor t itemType)).Invoke(source)

type private IListCreator =
    abstract member Create : array: Array -> obj

type private ListCreator<'T>() =
    interface IListCreator with
        member _. Create(array) = array :?> 'T[] |> List.ofArray |> box

let private listCreatorCache = ConcurrentDictionary<Type, IListCreator>()

let createList (itemType: Type) array =
    listCreatorCache.GetOrAdd(itemType,
        fun t -> Activator.CreateInstance(typedefof<ListCreator<_>>.MakeGenericType(t)) :?> IListCreator)
        .Create(array)

let private getDefaultCache = ConcurrentDictionary<Type, obj>()

let private createDefault t =
    let getter =
        Expression.Default(t)
        |> convert<obj>
        |> compile<Func<obj>> (array.Empty())
    getter.Invoke()

let getDefault (t: Type) =
    if t.IsNullable
    then null
    else getDefaultCache.GetOrAdd(t, createDefault)
