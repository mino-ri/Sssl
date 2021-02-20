module SsslFSharp.Test.SsslTest
open System
open System.Collections.Generic
open SsslFSharp
open Xunit

let private testRecord =
    Sssl.Record("", SsslRecordType.Brackets, [|
        Sssl.Null
        Sssl.Pair("Abc", Sssl.Number(12.0))
    |])

let private getByIndex (sssl: Sssl) (index: int) = sssl.[index]

[<Fact>]
let ``[int].正常`` () =
    test getByIndex (testRecord, 0) ==> equal Sssl.Null

[<Fact>]
let ``[int].例外 インデックスが範囲外`` () =
    test getByIndex (testRecord, 3) ==> throws<IndexOutOfRangeException>

let private getByName (sssl: Sssl) (name: string) = sssl.[name]

[<Fact>]
let ``[string].正常`` () =
    test getByName (testRecord, "Abc") ==> equal (Sssl.Number(12.0))

[<Fact>]
let ``[string].例外 キーが存在しない`` () =
    test getByName (testRecord, "Def") ==> throws<KeyNotFoundException>

let private tryGetByName (sssl: Sssl) (name: string) =
    sssl.HasName(name), sssl.TryGetByName(name)

[<Fact>]
let ``TryGetByName.正常`` () =
    test tryGetByName (testRecord, "Abc") ==> (true', equal (Some(Sssl.Number(12.0))))

[<Fact>]
let ``TryGetByName.正常 キーが存在しない`` () =
    test tryGetByName (testRecord, "Def") ==> (false', equal None)
