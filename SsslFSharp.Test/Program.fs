open System
open System.Collections.Generic
open SsslFSharp

let test (value: 'T) =
    let sssl = Sssl.convertFrom value
    printfn "%O" sssl
    let back = Sssl.convertTo<'T> sssl
    printfn "%b" <| EqualityComparer<'T>.Default.Equals(value, back)

[<EntryPoint>]
let main argv =
    test (box 12.0)
    test (Some(true))
    test (box (1, "ABC", Nullable(true)))
    0
