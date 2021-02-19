[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SsslFSharp.Sssl
open System.IO
open System.Text

let (|Value|Container|) sssl =
    match sssl with
    | Sssl.Null -> Value(null)
    | Sssl.String(value) -> Value(box value)
    | Sssl.Bool(value) -> Value(box value)
    | Sssl.Number(value) -> Value(box value)
    | Sssl.Pair(name, value) -> Container(name, [| value |])
    | Sssl.Record(name, _, contents) -> Container(name, contents)

let escapeChar c = Sssl.EscapeChar(c)

let escapeString str = Sssl.EscapeString(str)

let writeTo textWriter format (sssl: Sssl) = sssl.WriteTo(textWriter, format)

let parse text = (SsslParser(text)).Parse()

let tryConvertTo<'T> sssl = SsslConverter.defaultConverter.TryConvertTo<'T>(sssl)

let tryConvertFrom<'T> value = SsslConverter.defaultConverter.TryConvertFrom<'T>(value)

let convertTo<'T> sssl = SsslConverter.defaultConverter.ConvertTo<'T>(sssl)

let convertFrom<'T> (value: 'T) = SsslConverter.defaultConverter.ConvertFrom(box value, typeof<'T>)

let loadFromReader (textReader: TextReader) = parse <| textReader.ReadToEnd()
    
let loadFromStream (stream: Stream) = loadFromReader <| new StreamReader(stream, Encoding.UTF8)

let loadFromFile path =
    use stream = File.OpenRead(path)
    loadFromStream stream

let saveToWriter format textWriter (sssl: Sssl) = sssl.WriteTo(textWriter, format)

let saveToStream format (stream: Stream) sssl =
    saveToWriter format (new StreamWriter(stream, Encoding.UTF8)) sssl

let saveToFile format path sssl =
    use stream = File.Create(path)
    saveToStream format stream sssl
