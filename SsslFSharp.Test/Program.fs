module Program

open System
open System.Collections.Generic
open SsslFSharp

type OptionBuilder() =
    member inline _.Bind(m, f) = Option.bind f m
    member inline _.Return(x) = Some(x)
    member inline _.ReturnFrom(m: 'T option) = m
    member inline _.Zero() = None

type DoOptionBuilder() =
    member inline _.Bind(m, f) = Option.iter f m
    member inline _.Zero() = ()

let option = OptionBuilder()

let doOption = DoOptionBuilder()

type TestType(id: int, name: string) =
    member val Id = id with get, set
    member val Name = name with get, set
    new() = TestType(0, "")

[<Struct>]
type TestRecord = { Id: int; Name: string }

type TestSum =
    | TestSome of id: int * text: string
    | TestNone

type TestSingle = TestSingle of string * int

let test (value: 'T) =
    try
        doOption {
            let! sssl = Sssl.tryConvertFrom<'T> value
            let text = sssl.ToString()
            printfn "%s" text
            let backSssl = Sssl.parse text
            let! back = Sssl.tryConvertTo<'T> backSssl
            printfn "%b" <| EqualityComparer<'T>.Default.Equals(value, back)
        }
    with SsslParseException(message, source, index) ->
        Console.ForegroundColor <- ConsoleColor.Yellow
        printfn "%s" message
        if index > 1 then
            Console.ResetColor()
            printf "%s" source.[..index - 1]
        Console.ForegroundColor <- ConsoleColor.Red
        if index >= source.Length || " \r\n\t".Contains(source.[index])
        then printf "_"
        else printf "%c" source.[index]
        Console.ResetColor()
        printfn "%s" (if index >= source.Length then "" else source.[index + 1..])
    printfn ""

[<EntryPoint>]
let main argv =
    test <| 12.0
    test <| box 12.0
    test <| (1, "ABC", Nullable(true))
    test <| [| 1; 3; 5 |]
    test <| [ 1; 3; 5 ]
    test <| TestType(12, "Me")
    test <| { Id = 0; Name = "You" }
    test <| {| Index = 5; Name = "Them" |}
    test <| TestSome(12, "abc")
    test <| TestNone
    test <| TestSingle("KKK", 58)
    test <| None
    test <| box DayOfWeek.Monday
    test <| Dictionary([ KeyValuePair(0, "Zero"); KeyValuePair(1, "One") ])
    test <| Dictionary([ KeyValuePair("Zero", 0); KeyValuePair("One", 1) ])
    test <|
        {|
            Text = "ab\n\rc"
            Int = 12
            Double = 12.6
            Bool = false
            DayOfWeek = DayOfWeek.Monday
            Tuple = 12, "abc"
            Obj = {| A = true; B = false |}
            Array = [| 1; 2; 3; 4 |]
            Null = null
            Dict = Dictionary([
                KeyValuePair(1, 5)
                KeyValuePair(2, 6)
                KeyValuePair(12, 9)
            ])
        |}
    0
