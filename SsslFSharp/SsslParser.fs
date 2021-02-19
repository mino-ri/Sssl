namespace SsslFSharp
open System
open System.Globalization
open System.Text

exception SsslParseException of message: string * sourceText: string * index: int with
    override this.Message =
        let beginIndex = max 0 (this.index - 10)
        let endIndex = min (this.sourceText.Length - 1) (this.index + 10)
        this.message + $"\r\nindex: %i{this.index}\r\n" +
        this.sourceText.[beginIndex..endIndex].Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ') +
        "\r\n" + String(' ', this.index - beginIndex) + "^"


type internal SsslParser(source: string) =
    let builder = StringBuilder()
    let mutable index = 0

    static let isDigit c = '0' <= c && c <= '9'

    static let isHex c = '0' <= c && c <= '9' || 'a' <= c && c <= 'f' || 'A' <= c && c <= 'F'

    static let isIdHead c =
        c = '_' || c = '$' ||
        match Char.GetUnicodeCategory(c) with
        | UnicodeCategory.UppercaseLetter
        | UnicodeCategory.LowercaseLetter
        | UnicodeCategory.TitlecaseLetter
        | UnicodeCategory.ModifierLetter
        | UnicodeCategory.OtherLetter
        | UnicodeCategory.LetterNumber -> true
        | _ -> false

    static let isIdChar c =
        c = '_' || c = '$' || c = '.' ||
        match Char.GetUnicodeCategory(c) with
        | UnicodeCategory.UppercaseLetter
        | UnicodeCategory.LowercaseLetter
        | UnicodeCategory.TitlecaseLetter
        | UnicodeCategory.ModifierLetter
        | UnicodeCategory.OtherLetter
        | UnicodeCategory.LetterNumber
        | UnicodeCategory.NonSpacingMark
        | UnicodeCategory.SpacingCombiningMark
        | UnicodeCategory.DecimalDigitNumber
        | UnicodeCategory.ConnectorPunctuation -> true
        | _ -> false

    let error message = raise (SsslParseException(message, source, index))

    let consume (c: char) =
        index <- index + 1
        ignore <| builder.Append(c)

    let getCurrent() = if index >= source.Length then ' ' else source.[index]

    let consumeWhile predicate =
        let mutable c = getCurrent()
        while predicate c do
            consume c
            c <- getCurrent()

    let consumeWhileOne predicate errorMessage =
        let c = getCurrent()
        if not (predicate c) then error errorMessage
        consume c
        consumeWhile predicate

    let skipSpace() =
        while (index < source.Length && " \t\r\n".Contains(getCurrent())) do
            index <- index + 1

    member private _.GetNumber() =
        let mutable c = getCurrent()
        ignore <| builder.Clear()
        if c = '-' || c = '+' then
            consume c
            c <- getCurrent()
        if c = '0' then consume c
        elif '1' <= c && c <= '9' then consumeWhile isDigit
        else error "Digits are required."
        // read fraction
        c <- getCurrent()
        if c = '.' then
            consume c
            consumeWhileOne isDigit "Digits are required."
        // read exponent
        c <- getCurrent()
        if c = 'e' || c = 'E' then
            consume c
            c <- getCurrent()
            if c = '-' || c = '+' then consume c
            consumeWhileOne isDigit "Digits are required."
        skipSpace()
        Sssl.Number(Double.Parse(builder.ToString()))

    member private _.GetId() =
        let c = getCurrent()
        ignore <| builder.Clear()
        if not (isIdHead c) then error "A identifier is required."
        consume c
        consumeWhile isIdChar
        skipSpace()
        builder.ToString()

    member private _.GetString() =
        let mutable c = getCurrent()
        ignore <| builder.Clear()
        if c <> '"' then error "A string is required."
        index <- index + 1
        c <- getCurrent()
        while c <> '"' do
            if c = '\\' then
                index <- index + 1
                c <- getCurrent()
                match c with
                | '\\' | '"' | '/' -> c
                | 'b' -> '\b'
                | 'f' -> '\f'
                | 'n' -> '\n'
                | 'r' -> '\r'
                | 't' -> '\t'
                | 'u' ->
                    let codeChars = Array.zeroCreate 4
                    for i in 0..3 do
                        index <- index + 1
                        codeChars.[i] <- getCurrent()
                        if not (isHex codeChars.[i]) then error "Invalid escape sequence."
                    char (Int32.Parse(String(codeChars), NumberStyles.HexNumber))
                | _ -> error "Invalid escape sequence."
            else c
            |> builder.Append |> ignore
            index <- index + 1
            if index >= source.Length then error "End of string end not found."
            c <- getCurrent()
        index <- index + 1
        skipSpace()
        builder.ToString()

    member private this.GetPair(key) =
        if getCurrent() <> ':' then error "Unexpected token"
        index <- index + 1
        skipSpace()
        Sssl.Pair(key, this.GetValue())

    member private this.GetRecord(name) =
        let endBracket, recordType =
            match getCurrent() with
            | '(' -> ')', SsslRecordType.Parentheses
            | '{' -> '}', SsslRecordType.Braces
            | '[' -> ']', SsslRecordType.Brackets
            | _ -> error "予期しないトークンです。" // this exception is never thrown
        index <- index + 1
        skipSpace()
        let contents = ResizeArray()
        let rec readContent() =
            if getCurrent() = endBracket then ()
            else
                contents.Add(this.GetValue())
                let c = getCurrent()
                if c = endBracket then ()
                elif c <> ',' then error "オブジェクトの終端が必要です。"
                else
                    index <- index + 1
                    skipSpace()
                    readContent()
        readContent()
        index <- index + 1
        skipSpace()
        Sssl.Record(name, recordType, contents.ToArray())

    member private this.GetValue() =
        match getCurrent() with
        | '(' | '{' | '[' -> this.GetRecord("")
        | '"' ->
            let str = this.GetString()
            match getCurrent() with
            | ':' -> this.GetPair(str)
            | '(' | '{' | '[' -> this.GetRecord(str)
            | _ -> Sssl.String(str)
        | '-' | '+' -> this.GetNumber()
        | c when isDigit c -> this.GetNumber()
        | c when isIdHead c ->
            match this.GetId() with
            | "true" -> Sssl.Bool(true)
            | "false" -> Sssl.Bool(false)
            | "null" -> Sssl.Null
            | "nan" -> Sssl.Number(nan)
            | "inf" -> Sssl.Number(infinity)
            | "ninf" -> Sssl.Number(-infinity)
            | name ->
                match getCurrent() with
                | ':' -> this.GetPair(name)
                | '(' | '{' | '[' -> this.GetRecord(name)
                | _ -> error "Unknown token in literal."
        | _ -> error "Unknown token in value."

    member this.Parse() =
        skipSpace()
        let result = this.GetValue()
        if index < source.Length then error "Unknown token."
        result
