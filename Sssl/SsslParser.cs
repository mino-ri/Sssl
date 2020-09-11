using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace Sssl
{
    internal class SsslParser
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly string _source;
        private int _index;

        public SsslParser(string source)
        {
            _source = source;
        }

        public SsslObject Parse()
        {
            SkipSpace();
            var result = GetValue();
            if (_index < _source.Length) throw Error("Unknown token.");
            return result;
        }

        private void Consume(char c)
        {
            _index++;
            _builder.Append(c);
        }

        private static bool IsDigit(char c) => '0' <= c && c <= '9';

        private static bool IsHex(char c) => '0' <= c && c <= '9' || 'a' <= c && c <= 'f' || 'A' <= c && c <= 'F';

        private static bool IsIdHead(char c) =>
            c == '_' || c == '$' || char.GetUnicodeCategory(c) switch
            {
                UnicodeCategory.UppercaseLetter => true,
                UnicodeCategory.LowercaseLetter => true,
                UnicodeCategory.TitlecaseLetter => true,
                UnicodeCategory.ModifierLetter => true,
                UnicodeCategory.OtherLetter => true,
                UnicodeCategory.LetterNumber => true,
                _ => false,
            };

        private static bool IsIdChar(char c) =>
            c == '_' || c == '$' || c == '.' || char.GetUnicodeCategory(c) switch
            {
                UnicodeCategory.UppercaseLetter => true,
                UnicodeCategory.LowercaseLetter => true,
                UnicodeCategory.TitlecaseLetter => true,
                UnicodeCategory.ModifierLetter => true,
                UnicodeCategory.OtherLetter => true,
                UnicodeCategory.LetterNumber => true,
                UnicodeCategory.NonSpacingMark => true,
                UnicodeCategory.SpacingCombiningMark => true,
                UnicodeCategory.DecimalDigitNumber => true,
                UnicodeCategory.ConnectorPunctuation => true,
                _ => false,
            };

        private char GetCurrent() => _index >= _source.Length ? ' ' : _source[_index];

        private void ConsumeWhile(Func<char, bool> predicate)
        {
            var c = GetCurrent();
            while (predicate(c))
            {
                Consume(c);
                c = GetCurrent();
            }
        }

        private void ConsumeWhileOne(Func<char, bool> predicate, string errorMessage)
        {
            var c = GetCurrent();
            if (!predicate(c)) throw Error(errorMessage);

            Consume(c);
            ConsumeWhile(predicate);
        }

        private void SkipSpace()
        {
            while (_index < _source.Length && " \t\r\n".Contains(GetCurrent()))
                _index++;
        }

        private SsslObject GetValue()
        {
            var c = GetCurrent();
            if (c == '(' || c == '{' || c == '[') return GetRecord("");
            
            if (IsDigit(c) || c == '-' || c == '+') return GetNumber();

            if (c == '"')
            {
                var str = GetString();
                return GetCurrent() switch
                {
                    ':' => (SsslObject)GetPair(str),
                    '(' => GetRecord(str),
                    '{' => GetRecord(str),
                    '[' => GetRecord(str),
                    _ => new SsslValue(str),
                };
            }
            
            if (IsIdHead(c))
            {
                var id = GetId();
                switch (id)
                {
                    case "true": return new SsslValue(true);
                    case "false": return new SsslValue(false);
                    case "null": return SsslValue.Null;
                    case "nan": return new SsslValue(double.NaN);
                    case "inf": return new SsslValue(double.PositiveInfinity);
                    case "ninf": return new SsslValue(double.NegativeInfinity);
                    default:
                        return GetCurrent() switch
                        {
                            ':' => (SsslObject)GetPair(id),
                            '(' => GetRecord(id),
                            '{' => GetRecord(id),
                            '[' => GetRecord(id),
                            _ => throw Error("Unknown token."),
                        };
                }
            }

            throw Error("Unknown token.");
        }

        private Exception Error(string message) =>
            new SsslParseException($"Invalid SSSL starting at character {_index},\r\n'{GetCurrent()}': {message}\r\n-----\r\n{_source[Math.Max(0, _index - 10)..Math.Min(_source.Length, _index + 10)]}", _source, _index);

        private SsslValue GetNumber()
        {
            var c = GetCurrent();
            _builder.Clear();

            if (c == '-' || c == '+')
            {
                Consume(c);
                c = GetCurrent();
            }

            if (c == '0') Consume(c);
            if ('1' <= c && c <= '9') ConsumeWhile(IsDigit);
            else throw Error("Digits are required.");

            // read fraction
            c = GetCurrent();
            if (c == '.')
            {
                Consume(c);
                ConsumeWhileOne(IsDigit, "Digits are required.");
            }

            // read exponent
            c = GetCurrent();
            if (c == 'e' || c == 'E')
            {
                Consume(c);
                c = GetCurrent();
                if (c == '-' || c == '+') Consume(c);
                ConsumeWhileOne(IsDigit, "Digits are required.");
            }

            SkipSpace();
            return new SsslValue(double.Parse(_builder.ToString()));
        }

        private string GetId()
        {
            var c = GetCurrent();
            _builder.Clear();

            if (!IsIdHead(c)) throw Error("A identifier is required.");

            Consume(c);
            ConsumeWhile(IsIdChar);
            SkipSpace();

            return _builder.ToString();
        }

        private string GetString()
        {
            var c = GetCurrent();
            _builder.Clear();

            if (c != '"') throw Error("A string is required.");

            _index++;
            c = GetCurrent();

            while (c != '"')
            {
                if (c == '\\')
                {
                    _index++;
                    c = GetCurrent();
                    switch (c)
                    {
                        case '\\': case '"': case '/': _builder.Append(c); break;
                        case 'b': _builder.Append('\b'); break;
                        case 'f': _builder.Append('\f'); break;
                        case 'n': _builder.Append('\n'); break;
                        case 'r': _builder.Append('\r'); break;
                        case 't': _builder.Append('\t'); break;
                        case 'u':
                            var codeChars = new char[4];
                            for (var i = 0; i < 4; i++)
                            {
                                _index++;
                                codeChars[i] = GetCurrent();
                                if (!IsHex(codeChars[i])) throw Error("Invalid escape sequence.");
                            }

                            _builder.Append((char)int.Parse(new string(codeChars), NumberStyles.HexNumber));
                            break;
                        default: throw Error("Invalid escape sequence.");
                    }
                }
                else
                {
                    _builder.Append(c);
                }

                _index++;
                c = GetCurrent();
            }

            _index++;
            SkipSpace();
            return _builder.ToString();
        }

        private SsslRecord GetRecord(string name)
        {
            var (endBracket, type) = GetCurrent() switch
            {
                '(' => (')', SsslRecordType.Parentheses),
                '{' => ('}', SsslRecordType.Braces),
                '[' => (']', SsslRecordType.Brackets),
                _ => throw Error("予期しないトークンです。"), // this exception is never thrown
            };

            _index++;
            SkipSpace();

            var result = new SsslRecord(type, name);
            while (true)
            {
                if (GetCurrent() == endBracket) break;
                result.Add(GetValue());
                var c = GetCurrent();
                if (c == endBracket) break;
                if (c != ',') throw Error("オブジェクトの終端が必要です。");
                _index++;
                SkipSpace();
            }

            _index++;
            SkipSpace();
            return result;
        }

        private SsslPair GetPair(string key)
        {
            if (GetCurrent() != ':') throw Error("予期しないトークンです。");
            _index++;
            SkipSpace();
            return new SsslPair(key, GetValue());
        }
    }

    [Serializable]
    public class SsslParseException : Exception
    {
        public string SourceText { get; }

        public int Index { get; }

        public override string Message => base.Message;

        public SsslParseException() => SourceText = "";

        public SsslParseException(string message, string sourceText, int index) : base(message)
        {
            SourceText = sourceText;
            Index = index;
        }

        public SsslParseException(string message) : base(message) => SourceText = "";

        public SsslParseException(string message, Exception inner) : base(message, inner) => SourceText = "";

        protected SsslParseException(SerializationInfo info, StreamingContext context) : base(info, context) => SourceText = "";
    }
}
