module SsslFSharp.Test.SsslConverterTest
open System.Collections.Generic
open Xunit
open SsslFSharp
open RightArrow

type TestData = { Id: int; Name: string }

[<Fact>]
let ``convertFrom.正常 null`` () =
    test Sssl.convertFrom null ==> it ^= Sssl.Null

[<Fact>]
let ``convertFrom.正常 float`` () =
    test Sssl.convertFrom 12.0 ==> it ^= Sssl.Number(12.0)

[<Fact>]
let ``convertFrom.正常 int`` () =
    test Sssl.convertFrom 12 ==> it ^= Sssl.Number(12.0)

[<Fact>]
let ``convertFrom.正常 bool`` () =
    test Sssl.convertFrom true ==> it ^= Sssl.True

[<Fact>]
let ``convertFrom.正常 string`` () =
    test Sssl.convertFrom "Abc" ==> it ^= Sssl.String("Abc")

[<Fact>]
let ``convertFrom.正常 タプル`` () =
    test Sssl.convertFrom (12, true) ==> it ^= Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)

[<Fact>]
let ``convertFrom.正常 配列`` () =
    test Sssl.convertFrom [| true; false |] ==> it ^= Sssl.List("", Sssl.True, Sssl.False)

[<Fact>]
let ``convertFrom.正常 リスト`` () =
    test Sssl.convertFrom [ true; false ] ==> it ^= Sssl.List("", Sssl.True, Sssl.False)

[<Fact>]
let ``convertFrom.正常 ディクショナリ1`` () =
    let dict = Dictionary()
    dict.[12] <- true
    dict.[15] <- false
    let sssl = Sssl.List("", [|
        Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)
        Sssl.Tuple("", Sssl.Number(15.0), Sssl.False)
    |])
    test Sssl.convertFrom dict ==> it ^= sssl

[<Fact>]
let ``convertFrom.正常 ディクショナリ2`` () =
    let dict = Dictionary()
    dict.["a"] <- true
    dict.["b"] <- false
    let sssl = Sssl.List("", [|
        Sssl.Pair("a", Sssl.True)
        Sssl.Pair("b", Sssl.False)
    |])
    test Sssl.convertFrom dict ==> it ^= sssl

[<Fact>]
let ``convertFrom.正常 マップ`` () =
    let map = Map([ (12, true); (15, false) ])
    let sssl = Sssl.List("", [|
        Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)
        Sssl.Tuple("", Sssl.Number(15.0), Sssl.False)
    |])
    test Sssl.convertFrom map ==> it ^= sssl

[<Fact>]
let ``convertFrom.正常 オブジェクト`` () =
    let sssl = Sssl.Object("", [|
        Sssl.Pair("Id", Sssl.Number(0.0))
        Sssl.Pair("Name", Sssl.String("Abc"))
    |])
    test Sssl.convertFrom { Id = 0; Name = "Abc" } ==> it ^= sssl

[<Fact>]
let ``convertTo.正常 null`` () =
    test Sssl.convertTo Sssl.Null ==> it ^= null

[<Fact>]
let ``convertTo.正常 float`` () =
    test Sssl.convertTo (Sssl.Number(12.0)) ==> it ^= 12.0

[<Fact>]
let ``convertTo.正常 int`` () =
    test Sssl.convertTo (Sssl.Number(12.0)) ==> it ^= 12

[<Fact>]
let ``convertTo.正常 bool`` () =
    test Sssl.convertTo Sssl.True ==> it ^= true

[<Fact>]
let ``convertTo.正常 string`` () =
    test Sssl.convertTo (Sssl.String("Abc")) ==> it ^= "Abc"

[<Fact>]
let ``convertTo.正常 タプル`` () =
    test Sssl.convertTo (Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)) ==> it ^= (12, true)

[<Fact>]
let ``convertTo.正常 配列`` () =
    test Sssl.convertTo (Sssl.List("", Sssl.True, Sssl.False)) ==> it ^=@ [| true; false |]

[<Fact>]
let ``convertTo.正常 リスト`` () =
    test Sssl.convertTo (Sssl.List("", Sssl.True, Sssl.False)) ==> it ^= [ true; false ]

[<Fact>]
let ``convertTo.正常 ディクショナリ1`` () =
    let dict = Dictionary()
    dict.[12] <- true
    dict.[15] <- false
    let sssl = Sssl.List("", [|
        Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)
        Sssl.Tuple("", Sssl.Number(15.0), Sssl.False)
    |])
    test Sssl.convertTo sssl ==> it ^=@ dict

[<Fact>]
let ``convertTo.正常 ディクショナリ2`` () =
    let dict = Dictionary()
    dict.["a"] <- true
    dict.["b"] <- false
    let sssl = Sssl.List("", [|
        Sssl.Pair("a", Sssl.True)
        Sssl.Pair("b", Sssl.False)
    |])
    test Sssl.convertTo sssl ==> it ^=@ dict

[<Fact>]
let ``convertTo.正常 マップ`` () =
    let dict = Map([ (12, true); (15, false) ])
    let sssl = Sssl.List("", [|
        Sssl.Tuple("", Sssl.Number(12.0), Sssl.True)
        Sssl.Tuple("", Sssl.Number(15.0), Sssl.False)
    |])
    test Sssl.convertTo sssl ==> it ^=@ dict

[<Fact>]
let ``convertTo.正常 オブジェクト`` () =
    let sssl = Sssl.Object("", [|
        Sssl.Pair("Id", Sssl.Number(0.0))
        Sssl.Pair("Name", Sssl.String("Abc"))
    |])
    test Sssl.convertTo sssl ==> it ^= { Id = 0; Name = "Abc" }
