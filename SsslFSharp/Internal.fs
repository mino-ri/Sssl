[<AutoOpen>]
module internal SsslFSharp.Internal
open System
open System.Collections
open System.Collections.Generic

let (|SCons|SEmpty|) (e: IEnumerator<'T>) =
    if e.MoveNext() then SCons(e.Current, e) else SEmpty

let chooseAll (chooser: 'T -> 'U option) (source: seq<'T>) =
    let result = ResizeArray()
    let rec recSelf = function
        | SEmpty -> Some(result)
        | SCons(head, tail) ->
            match chooser head with
            | None -> None
            | Some(value) ->
                result.Add(value)
                recSelf tail
    use enumerator = source.GetEnumerator()
    recSelf enumerator

let (|ECons|EEmpty|) (e: IEnumerator) =
    if e.MoveNext() then ECons(e.Current, e) else EEmpty

let chooseAllE (chooser: obj -> 'T option) (source: IEnumerable) =
    let result = ResizeArray()
    let rec recSelf = function
        | EEmpty -> Some(result)
        | ECons(head, tail) ->
            match chooser head with
            | None -> None
            | Some(value) ->
                result.Add(value)
                recSelf tail
    recSelf (source.GetEnumerator())

type Type with
    member this.IsGenericOf(genericType) =
        this.IsGenericType && 
        this.GetGenericTypeDefinition() = genericType

    member this.IsGenericOfAny([<ParamArray>] genericTypes) =
        this.IsGenericType &&
        genericTypes |> Array.contains (this.GetGenericTypeDefinition())

    member this.IsNullable = not this.IsValueType || this.IsGenericOf(typedefof<Nullable<_>>)

    member inline this.TypeArgs = this.GetGenericArguments()

    member this.Unnullable() =
        if this.IsGenericOf(typedefof<Nullable<_>>) then this.TypeArgs.[0] else this

let tryCast<'T> (v: obj) = match v with :? 'T as typed -> Some(typed) | _ -> None

type OptionBuilder() =
    member inline _.Bind(m, f) = Option.bind f m
    member inline _.Return(x) = Some(x)
    member inline _.ReturnFrom(m: 'T option) = m
    member inline _.Zero() = None

let option = OptionBuilder()
