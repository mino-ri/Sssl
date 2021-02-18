namespace SsslFSharp
open System

type ISsslConverter =
    abstract member TryConvertFrom : value: obj * expectedType: Type -> Sssl option
    abstract member TryConvertTo : sssl: Sssl * expectedType : Type -> obj option


type ISsslConverterPart =
    abstract member TryConvertFrom : converter: ISsslConverter * value: obj * expectedType: Type -> Sssl option
    abstract member TryConvertTo : converter: ISsslConverter * sssl: Sssl * expectedType : Type -> obj option


[<AbstractClass>]
type SsslConverterPart<'T>() =
    abstract member TryConvertFrom : converter: ISsslConverter * value: 'T * expectedType: Type -> Sssl option
    abstract member TryConvertTo : converter: ISsslConverter * sssl: Sssl * expectedType: Type -> 'T option
    interface ISsslConverterPart with
        member this.TryConvertFrom(converter, value, expectedType) =
            match value with
            | :? 'T as typed -> this.TryConvertFrom(converter, typed, expectedType)
            | _ -> None
        member this.TryConvertTo(converter, sssl, expectedType) =
            this.TryConvertTo(converter, sssl, expectedType) |> Option.map box


[<AutoOpen>]
module SsslConverterOperator =
    type ISsslConverter with
        member this.TryConvertTo(sssl) : 'T option =
            this.TryConvertTo(sssl, typeof<'T>) |> Option.map (fun x -> x :?> 'T)

        member this.ConvertTo(sssl, expectedType) = this.TryConvertTo(sssl, expectedType).Value

        member this.ConvertTo(sssl) : 'T = this.TryConvertTo(sssl).Value

        member this.TryConvertFrom(value: 'T) =
            this.TryConvertFrom(box value, typeof<'T>)

        member this.ConvertFrom(value: obj, expectedType) = this.TryConvertFrom(value, expectedType).Value


type SsslConverterBuilder =
    {
        ConcreteConverters: (Type * ISsslConverterPart) list
        ConditionalConverters: ((Type -> bool) * ISsslConverterPart) list
    }


type NumberConverterPart<'T>(tryConvertFrom: 'T -> float option, tryConvertTo: float -> 'T option) =
    inherit SsslConverterPart<'T>()
    override _.TryConvertFrom(_, value, expected) =
        tryConvertFrom value
        |> Option.map (Sssl.Number >> SsslType.wrapTyped expected typeof<'T>)

    override _.TryConvertTo(_, sssl, expected) =
        match SsslType.unwrapTyped expected typeof<'T> sssl with
        | Sssl.Number(value) -> tryConvertTo value
        | _ -> None


type StringConverterPart<'T>(tryConvertFrom: 'T -> string option, tryConvertTo: string -> 'T option) =
    inherit SsslConverterPart<'T>()
    override _.TryConvertFrom(_, value, expected) =
        tryConvertFrom value
        |> Option.map (Sssl.String >> SsslType.wrapTyped expected typeof<'T>)

    override _.TryConvertTo(_, sssl, expected) =
        match SsslType.unwrapTyped expected typeof<'T> sssl with
        | Sssl.String(value) -> tryConvertTo value
        | _ -> None


type BoolConverterPart<'T>(tryConvertFrom: 'T -> bool option, tryConvertTo: bool -> 'T option) =
    inherit SsslConverterPart<'T>()
    override _.TryConvertFrom(_, value, expected) =
        tryConvertFrom value
        |> Option.map (Sssl.Bool >> SsslType.wrapTyped expected typeof<'T>)

    override _.TryConvertTo(_, sssl, expected) =
        match SsslType.unwrapTyped expected typeof<'T> sssl with
        | Sssl.Bool(value) -> tryConvertTo value
        | _ -> None
