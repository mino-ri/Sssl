module SsslFSharp.Test.SsslTypeTest
open SsslFSharp
open Xunit

[<Fact>]
let ``getName.正常 同じ型は空白`` () =
    test SsslType.getName (typeof<int>, typeof<int>) ==> equal ""

[<Fact>]
let ``getName.正常 同じアセンブリの型`` () =
    test SsslType.getName (typeof<obj>, typeof<int>) ==> equal typeof<int>.FullName

[<Fact>]
let ``getName.正常 違うアセンブリの型`` () =
    test SsslType.getName (typeof<obj>, typeof<Sssl>) ==>
        notEqual typeof<Sssl>.FullName ^^ startsWith typeof<Sssl>.FullName

[<Fact>]
let ``getName.正常 ネスト型`` () =
    test SsslType.getName (typeof<Sssl>, Sssl.Bool(true).GetType()) ==> equal (nameof Sssl.Bool)

let private restoreType expected ty = SsslType.getName expected ty |> SsslType.getType expected

[<Fact>]
let ``getType.正常 同じ型`` () =
    test restoreType (typeof<int>, typeof<int>) ==> equal typeof<int>

[<Fact>]
let ``getType.正常 同じアセンブリ型`` () =
    test restoreType (typeof<obj>, typeof<int>) ==> equal typeof<int>

[<Fact>]
let ``getType.正常 違うアセンブリ型`` () =
    test restoreType (typeof<obj>, typeof<Sssl>) ==> equal typeof<Sssl>

[<Fact>]
let ``getType.正常 ネスト型`` () =
    let nestedType = Sssl.Bool(true).GetType()
    test restoreType (typeof<Sssl>, nestedType) ==> equal nestedType
