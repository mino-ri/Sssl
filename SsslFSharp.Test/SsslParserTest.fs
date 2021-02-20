module SsslFSharp.Test.SsslParserTest
open Xunit
open SsslFSharp

[<Fact>]
let ``parse.異常 ゼロ文字列`` () =
    test Sssl.parse "" ==> throws<SsslParseException>

[<Fact>]
let ``parse.異常 空白文字列`` () =
    test Sssl.parse "  \r\n" ==> throws<SsslParseException>

[<Fact>]
let ``parse.正常 null`` () =
    test Sssl.parse "null" ==> equal Sssl.Null

[<Fact>]
let ``parse.異常 Null(大文字)`` () =
    test Sssl.parse "Null" ==> throws<SsslParseException>

[<Fact>]
let ``parse.正常 true`` () =
    test Sssl.parse "true" ==> equal Sssl.True

[<Fact>]
let ``parse.正常 false`` () =
    test Sssl.parse "false" ==> equal Sssl.False

[<Fact>]
let ``parse.正常 nan`` () =
    test Sssl.parse "nan" ==> equal Sssl.NaN

[<Fact>]
let ``parse.正常 inf`` () =
    test Sssl.parse "inf" ==> equal Sssl.Inf

[<Fact>]
let ``parse.正常 ninf`` () =
    test Sssl.parse "ninf" ==> equal Sssl.NInf

[<Fact>]
let ``parse.正常 整数`` () =
    test Sssl.parse "20" ==> equal (Sssl.Number(20.0))

[<Fact>]
let ``parse.正常 小数`` () =
    test Sssl.parse "20.75" ==> equal (Sssl.Number(20.75))

[<Fact>]
let ``parse.異常 0始まり`` () =
    test Sssl.parse "02" ==> throws<SsslParseException>

[<Fact>]
let ``parse.正常 指数表記`` () =
    test Sssl.parse "1.75e1" ==> equal (Sssl.Number(17.5))

[<Fact>]
let ``parse.正常 文字列`` () =
    test Sssl.parse "\"AbcDef\"" ==> equal (Sssl.String("AbcDef"))

[<Fact>]
let ``parse.正常 エスケープあり文字列`` () =
    test Sssl.parse """ "Abc\\\"\b\f\n\r\tDef" """ ==> equal (Sssl.String("Abc\\\"\b\f\n\r\tDef"))

[<Fact>]
let ``parse.正常 ペア`` () =
    test Sssl.parse """ "Abc": null """ ==> equal (Sssl.Pair("Abc", Sssl.Null))

[<Fact>]
let ``parse.正常 入れ子のペア`` () =
    test Sssl.parse """ "Abc": "Def": true """ ==>
        equal (Sssl.Pair("Abc", Sssl.Pair("Def", Sssl.True)))

[<Fact>]
let ``parse.正常 タプル型オブジェクト`` () =
    test Sssl.parse """ "name" (null, true) """ ==>
        equal (Sssl.Tuple("name", Sssl.Null, Sssl.True))

[<Fact>]
let ``parse.正常 タプル型名前なしオブジェクト`` () =
    test Sssl.parse """ (null, true) """ ==>
        equal (Sssl.Tuple("", Sssl.Null, Sssl.True))

[<Fact>]
let ``parse.正常 リスト型オブジェクト`` () =
    test Sssl.parse """ "name" [null, true] """ ==>
        equal (Sssl.List("name", Sssl.Null, Sssl.True))

[<Fact>]
let ``parse.正常 オブジェクト型オブジェクト`` () =
    test Sssl.parse """ "name" {null, true} """ ==>
        equal (Sssl.Object("name", Sssl.Null, Sssl.True))
