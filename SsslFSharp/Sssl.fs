namespace SsslFSharp
open System
open System.Globalization
open System.IO
open System.Text

type SsslRecordType =
    | Parentheses = 0
    | Braces = 1
    | Brackets = 2


[<RequireQualifiedAccess>]
type Sssl = 
    | Null
    | String of string
    | Bool of bool
    | Number of float
    | Pair of name: string * value: Sssl
    | Record of name: string * recordType: SsslRecordType * contents: Sssl[]
    with
    static member internal EscapeChar(c) =
        match c with
        | '"' -> @"\"""
        | '\\' -> @"\\"
        | '\b' -> @"\b"
        | '\f' -> @"\f"
        | '\n' -> @"\n"
        | '\r' -> @"\r"
        | '\t' -> @"\t"
        | _ when c < char 0x20 -> $@"\u{int c:x4}"
        | _ -> c.ToString()

    static member private WriteEscaped(textWriter: TextWriter, str: string) =
        let mutable beginIndex = 0
        for i in 0..str.Length - 1 do
            let c = str.[i]
            if c = '"' || c = '\\' || c < char 0x20 then
                let length = i - beginIndex
                if length > 0 then textWriter.Write(str.AsSpan(beginIndex, length))
                textWriter.Write(Sssl.EscapeChar(c))
                beginIndex <- i + 1
        let length = str.Length - beginIndex
        if length > 0 then
            textWriter.Write(str.AsSpan(beginIndex, length))
        
    static member internal EscapeString(str) =
        let builder = StringBuilder()
        use textWriter = new StringWriter(builder)
        Sssl.WriteEscaped(textWriter, str)
        builder.ToString()
        
    static member True = Bool(true)

    static member False = Bool(false)

    static member NaN = Number(nan)

    static member Inf = Number(infinity)

    static member NInf = Number(-infinity)

    static member Tuple(name: string, [<ParamArray>] contents) =
        Record(name, SsslRecordType.Parentheses, contents)

    static member List(name: string, [<ParamArray>] contents) =
        Record(name, SsslRecordType.Brackets, contents)

    static member Object(name: string, [<ParamArray>] contents) =
        Record(name, SsslRecordType.Braces, contents)

    member sssl.WriteTo(textWriter: TextWriter, format) =
        let getIndent format indent = System.String(format.IndentChar, indent * format.IndentInterval)
        let rec recSelf sssl indent =
            match sssl with
            | Sssl.Null -> textWriter.Write("null")
            | Sssl.Bool(value) -> textWriter.Write(if value then "true" else "false")
            | Sssl.String(value) ->
                textWriter.Write('"')
                Sssl.WriteEscaped(textWriter, value)
                textWriter.Write('"')
            | Sssl.Number(value) ->
                if Double.IsNaN(value) then "nan"
                elif Double.IsPositiveInfinity(value) then "inf"
                elif Double.IsNegativeInfinity(value) then "ninf"
                else value.ToString(CultureInfo.InvariantCulture)
                |> textWriter.Write
            | Sssl.Pair(name, value) ->
                textWriter.Write('"')
                Sssl.WriteEscaped(textWriter, name)
                textWriter.Write(if format.Spacing then "\": " else "\":")
                recSelf value indent
            | Sssl.Record(name, recordType, contents) ->
                let beginP, endP =
                    match recordType with
                    | SsslRecordType.Parentheses -> '(', ')'
                    | SsslRecordType.Braces -> '{', '}'
                    | SsslRecordType.Brackets -> '[', ']'
                    | _ -> invalidArg (nameof sssl) "sssl has invalid SsslRecordType"
                if not (String.IsNullOrEmpty(name)) then
                    textWriter.Write('"')
                    Sssl.WriteEscaped(textWriter, name)
                    textWriter.Write('"')
                    if format.Spacing then textWriter.Write(' ')
                textWriter.Write(beginP)
                if contents.Length <> 0 then
                    let localFormat =
                        if Array.forall (function Sssl.Record _ | Sssl.Pair _ -> false | _ -> true) contents &&
                           (recordType = SsslRecordType.Parentheses &&
                            contents.Length <= 8 ||
                            contents.Length <= 4) then
                            if format.Spacing then SsslFormat.Inline else SsslFormat.Minified
                        else format
                    let mutable first = true
                    for value in contents do
                        if first then first <- false
                        else textWriter.Write(",")
                        textWriter.Write(localFormat.NewLine)
                        textWriter.Write(getIndent localFormat (indent + 1))
                        recSelf value (indent + 1)
                    textWriter.Write(localFormat.NewLine)
                    textWriter.Write(getIndent localFormat indent)
                textWriter.Write(endP)
        recSelf sssl 0

    member this.Item
        with get (index: int) =
            match this with
            | Record(_, _, contents) -> contents.[index]
            | _ -> invalidOp "Sssl is not record."

    member this.Item
        with get name =
            match this with
            | Record(_, _, contents) ->
                Array.pick (function Pair(n, value) when n = name -> Some(value) | _ -> None) contents
            | _ -> invalidOp "Sssl is not record."

    member this.Name =
        match this with
        | Pair(name, _)
        | Record(name, _, _) -> name
        | _ -> invalidOp "Sssl value do not have name."

    member this.TryGetByName(name) =
        match this with
        | Record(_, _, contents) ->
            Array.tryPick (function Pair(n, value) when n = name -> Some(value) | _ -> None) contents
        | _ -> None

    member this.HasName(name) =
        match this with
        | Record(_, _, contents) ->
            Array.exists (function Pair(n, _) -> n = name | _ -> false) contents
        | _ -> false
        
    member this.ToString(format) =
        let builder = StringBuilder()
        use textWriter = new StringWriter(builder)
        this.WriteTo(textWriter, format)
        builder.ToString()

    override this.ToString() = this.ToString(SsslFormat.Default)
