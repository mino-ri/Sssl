namespace SsslFSharp
open System
open System.Collections.Generic
open BoolOption

type SsslStandardConverter(builder: SsslConverterBuilder) =
    let concreteParts = Dictionary(builder.ConcreteConverters |> Seq.map KeyValuePair)

    member _.GetPart(t: Type) =
        toOption (concreteParts.TryGetValue(t))
        |> Option.orElseWith (fun () ->
            let aType, aPart =
                ((typeof<obj>, Unchecked.defaultof<_>), concreteParts)
                ||> Seq.fold (fun (acmType, acmPart) (KeyValue(ty, part)) ->
                    if ty.IsAssignableFrom(t) && acmType.IsAssignableFrom(ty)
                    then ty, part
                    else acmType, acmPart)
            if aType = typeof<obj> then None else Some(aPart))
        |> Option.orElseWith (fun () ->
            builder.ConditionalConverters
            |> List.tryPick (fun (cond, part) -> if cond t then Some(part) else None))

    member this.TryConvertFrom(value: obj, expected: Type) =
        match value with
        | null -> Some(Sssl.Null)
        | :? Sssl as sssl -> Some(sssl)
        | _ ->
            let expected = expected.Unnullable()
            this.GetPart(value.GetType())
            |> Option.bind (fun p -> p.TryConvertFrom(this, value, expected))

    member this.TryConvertTo(sssl: Sssl, expected: Type) =
        match sssl with
        | Sssl.Null -> if expected.IsNullable then Some(null) else None
        | _ ->
            let expected = expected.Unnullable()
            let expected =
                match sssl with
                | Sssl.Pair(name, _) | Sssl.Record(name, _, _) -> SsslType.getType expected name
                | _ -> expected
            this.GetPart(expected)
            |> Option.bind (fun p -> p.TryConvertTo(this, sssl, expected))

    interface ISsslConverter with
        member this.TryConvertFrom(value, expected) = this.TryConvertFrom(value, expected)
        member this.TryConvertTo(sssl, expected) = this.TryConvertTo(sssl, expected)
