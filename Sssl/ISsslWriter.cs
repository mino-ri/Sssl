using System;
using System.Globalization;
using System.IO;

namespace Sssl
{
    public interface ISsslWriter
    {
        void Write(SsslValue value);

        void Write(SsslRecord record);

        void Write(SsslPair pair);
    }

    public interface ISsslWriter<TContext>
    {
        TContext InitialContext { get; }

        void Write(TContext context, SsslValue value);

        void Write(TContext context, SsslRecord record);

        void Write(TContext context, SsslPair pair);
    }

    public class SsslSerializationFormat
    {
        public char IndentChar { get; set; } = ' ';

        public string NewLine { get; set; } = "";

        public int IndentInterval { get; set; }

        public int InitialIndent { get; set; }

        public bool Spacing { get; set; }

        public SsslSerializationFormat() { }

        public SsslSerializationFormat(char indentChar, string newLine, int indentInterval, int initialIndent, bool spacing)
        {
            IndentChar = indentChar;
            NewLine = newLine;
            IndentInterval = indentInterval;
            InitialIndent = initialIndent;
            Spacing = spacing;
        }

        public static SsslSerializationFormat Minified { get; } = new SsslSerializationFormat(' ', "", 0, 0, false);

        public static SsslSerializationFormat Default { get; } = new SsslSerializationFormat(' ', Environment.NewLine, 2, 0, true);
    }

    public class TextSsslWriter : ISsslWriter<int>
    {
        private readonly TextWriter _textWriter;
        private readonly char _indentChar;
        private readonly string _newLine;
        private readonly int _indentInterval;
        private readonly bool _spacing;

        public int InitialContext { get; }

        private string GetIndent(int indent) => new string(_indentChar, indent * _indentInterval);

        public TextSsslWriter(TextWriter textWriter, SsslSerializationFormat format)
        {
            _textWriter = textWriter;
            _indentChar = format.IndentChar;
            _newLine = format.NewLine;
            _indentInterval = format.IndentInterval;
            InitialContext = format.InitialIndent;
            _spacing = format.Spacing;
        }

        public void Write(int indent, SsslValue value)
        {
            switch (value.Value)
            {
                case double @double:
                    if (double.IsNaN(@double))
                        _textWriter.Write("nan");
                    else if (double.IsPositiveInfinity(@double))
                        _textWriter.Write("inf");
                    else if (double.IsNegativeInfinity(@double))
                        _textWriter.Write("ninf");
                    else
                        _textWriter.Write(@double.ToString(CultureInfo.InvariantCulture));
                    break;
                case bool @bool:
                    _textWriter.Write(@bool ? "true" : "false");
                    break;
                case string @string:
                    WriteEscape(@string);
                    break;
                default:
                    _textWriter.Write("null");
                    break;
            }
        }

        public void Write(int indent, SsslRecord record)
        {
            var (begin, end) = record.RecordType switch
            {
                SsslRecordType.Parentheses => ('(', ')'),
                SsslRecordType.Braces => ('{', '}'),
                SsslRecordType.Brackets => ('[', ']'),
                _ => throw new InvalidOperationException(),
            };

            if (!string.IsNullOrEmpty(record.Name))
            {
                WriteEscape(record.Name);
                if (_spacing)
                    _textWriter.Write(' ');
            }

            if (record.Count == 0)
            {
                _textWriter.Write(begin);
                _textWriter.Write(end);
                return;
            }

            _textWriter.Write(begin);

            var first = true;
            foreach (var value in record)
            {
                if (first) first = false;
                else _textWriter.Write(",");

                _textWriter.Write(_newLine);
                _textWriter.Write(GetIndent(indent + 1));

                value.WriteTo(this, indent + 1);
            }

            _textWriter.Write(_newLine);
            _textWriter.Write(GetIndent(indent));
            _textWriter.Write(end);
        }

        public void Write(int indent, SsslPair pair)
        {
            WriteEscape(pair.Key);
            _textWriter.Write(_spacing ? ": " : ":");
            pair.Value.WriteTo(this, indent);
        }

        private void WriteEscape(string str)
        {
            _textWriter.Write('"');
            var beginIndex = 0;
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (c != '"' && c != '\\' && c >= 0x20)
                    continue;

                var length = i - beginIndex;
                if (length > 0)
                    _textWriter.Write(str.AsSpan(beginIndex, length));

                _textWriter.Write(SsslObject.EscapeChar(c));
                beginIndex = i + 1;
            }

            {
                var length = str.Length - beginIndex;
                if (length > 0)
                    _textWriter.Write(str.AsSpan(beginIndex, length));
            }
            _textWriter.Write('"');
        }
    }
}
