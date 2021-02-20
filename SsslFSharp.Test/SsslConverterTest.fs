module SsslFSharp.Test.SsslConverterTest
open System.Collections.Generic
open Xunit
open SsslFSharp

type TestData = { Id: int; Name: string }

[<Fact>]
let ``convertFrom.正常 null`` () =
    test Sssl.convertFrom null ==> equal (Sssl.Null)

[<Fact>]
let ``convertFrom.正常 float`` () =
    test Sssl.convertFrom 12.0 ==> equal (Sssl.Number(12.0))

[<Fact>]
let ``convertFrom.正常 int`` () =
    test Sssl.convertFrom 12 ==> equal (Sssl.Number(12.0))

[<Fact>]
let ``convertFrom.正常 bool`` () =
    test Sssl.convertFrom true ==> equal Sssl.True

[<Fact>]
let ``convertFrom.正常 string`` () =
    test Sssl.convertFrom "Abc" ==> equal (Sssl.String("Abc"))

[<Fact>]
let ``convertFrom.正常 タプル`` () =
    test Sssl.convertFrom (12, true) ==> equal (Sssl.Tuple("", Sssl.Number(12.0), Sssl.True))

[<Fact>]
let ``convertFrom.正常 配列`` () =
    test Sssl.convertFrom [| true; false |] ==> equal (Sssl.List("", Sssl.True, Sssl.False))

[<Fact>]
let ``convertFrom.正常 ディクショナリ`` () =
    let dict = Dictionary()
    dict.[12] <- true
    dict.[15] <- false
    let sssl = Sssl.List("", [|
        Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)
        Sssl.Tuple("", Sssl.Number(15.0), Sssl.False)
    |])
    test Sssl.convertFrom dict ==> equal sssl

[<Fact>]
let ``convertFrom.正常 オブジェクト`` () =
    let sssl = Sssl.Object("", [|
        Sssl.Pair("Id", Sssl.Number(0.0))
        Sssl.Pair("Name", Sssl.String("Abc"))
    |])
    test Sssl.convertFrom { Id = 0; Name = "Abc" } ==> equal sssl

[<Fact>]
let ``convertTo.正常 null`` () =
    test Sssl.convertTo Sssl.Null ==> equal null

[<Fact>]
let ``convertTo.正常 float`` () =
    test Sssl.convertTo (Sssl.Number(12.0)) ==> equal 12.0

[<Fact>]
let ``convertTo.正常 int`` () =
    test Sssl.convertTo (Sssl.Number(12.0)) ==> equal 12

[<Fact>]
let ``convertTo.正常 bool`` () =
    test Sssl.convertTo Sssl.True ==> equal true

[<Fact>]
let ``convertTo.正常 string`` () =
    test Sssl.convertTo (Sssl.String("Abc")) ==> equal "Abc"

[<Fact>]
let ``convertTo.正常 タプル`` () =
    test Sssl.convertTo (Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)) ==> equal (12, true)

[<Fact>]
let ``convertTo.正常 配列`` () =
    test Sssl.convertTo (Sssl.List("", Sssl.True, Sssl.False)) ==> equalSeq [| true; false |]

[<Fact>]
let ``convertTo.正常 オブジェクト`` () =
    let sssl = Sssl.Object("", [|
        Sssl.Pair("Id", Sssl.Number(0.0))
        Sssl.Pair("Name", Sssl.String("Abc"))
    |])
    test Sssl.convertTo sssl ==> equal { Id = 0; Name = "Abc" }
