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

let converter = SsslConverter.defaultConverter

let tryConvertTo<'T> sssl = converter.TryConvertTo<'T>(sssl)

let tryConvertFrom<'T> value = converter.TryConvertFrom<'T>(value)

let convertTo<'T> sssl = converter.ConvertTo<'T>(sssl)

let convertFrom<'T> (value: 'T) = converter.ConvertFrom(box value, typeof<'T>)

let tryConvertToObj resultType sssl = converter.TryConvertTo(sssl, resultType)

let tryConvertFromObj valueType (value: obj) = converter.TryConvertFrom(value, valueType)

let convertToObj resultType sssl = converter.ConvertTo(sssl, resultType)

let convertFromObj valueType (value: obj) = converter.ConvertFrom(box value, valueType)

let loadFromReader (textReader: TextReader) = parse <| textReader.ReadToEnd()
    
let loadFromStream (stream: Stream) = loadFromReader <| new StreamReader(stream, Encoding.UTF8)

let loadFromFile path =
    use stream = File.OpenRead(path)
    loadFromStream stream

let saveToWriter format textWriter (sssl: Sssl) = sssl.WriteTo(textWriter, format)

let saveToStream format (stream: Stream) sssl =
    use writer = new StreamWriter(stream, Encoding.UTF8)
    saveToWriter format writer sssl
    writer.Flush()

let saveToFile format path sssl =
    use stream = File.Create(path)
    saveToStream format stream sssl
