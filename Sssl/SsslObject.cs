using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace Sssl
{
    [DebuggerDisplay("{DebuggerPrint(),nq}")]
    public abstract partial class SsslObject
    {
        public static readonly object Undefined = new object();

        public dynamic ToDynamic() => new SsslDynamicProvider(this);

        public dynamic ToDynamic(ISsslConverter converter) => new SsslDynamicProvider(this, converter);

        public string ToString(SsslSerializationFormat format)
        {
            var builder = new StringBuilder();
            using var textWriter = new StringWriter(builder);
            WriteTo(new TextSsslWriter(textWriter, format));
            return builder.ToString();
        }

        public override string ToString() => ToString(SsslSerializationFormat.Default);

        public string ToStringMinified() => ToString(SsslSerializationFormat.Minified);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string DebuggerPrint()
        {
            var str = ToStringMinified();
            return str.Length > 512 ? str.Substring(0, 509).Replace('"', '\'') + "..." : str.Replace('"', '\'');
        }

        public abstract void WriteTo(ISsslWriter writer);

        public abstract void WriteTo<TContext>(ISsslWriter<TContext> writer, TContext context);

        public void WriteTo<TContext>(ISsslWriter<TContext> writer) => WriteTo(writer, writer.InitialContext);

        public static string EscapeString(string str) => string.Concat(str.Select(EscapeChar));

        public static string EscapeChar(char c)
        {
            return c switch
            {
                '"' => @"\""",
                '\\' => @"\\",
                '\b' => @"\b",
                '\f' => @"\f",
                '\n' => @"\n",
                '\r' => @"\r",
                '\t' => @"\t",
                _ when c < 0x20 => $@"\u{(int)c:x4}",
                _ => c.ToString(),
            };
        }

        #region save & load
        public void Save(TextWriter textWriter) { WriteTo(new TextSsslWriter(textWriter, SsslSerializationFormat.Default)); }


        public void Save(TextWriter textWriter, SsslSerializationFormat format) => WriteTo(new TextSsslWriter(textWriter, format));

        public void Save(Stream stream)
        {
            var writer = new StreamWriter(stream, Encoding.UTF8);
            Save(writer);
            writer.Flush();
        }

        public void Save(Stream stream, SsslSerializationFormat format)
        {
            var writer = new StreamWriter(stream, Encoding.UTF8);
            Save(writer, format);
            writer.Flush();
        }

        public void Save(string path)
        {
            using var stream = File.Create(path);
            Save(stream);
        }

        public void Save(string path, SsslSerializationFormat format)
        {
            using var stream = File.Create(path);
            Save(stream, format);
        }

        public static SsslObject Load(TextReader textReader) => Parse(textReader.ReadToEnd());

        public static SsslObject Load(Stream stream) => Load(new StreamReader(stream, Encoding.UTF8));

        public static SsslObject Load(string path)
        {
            using var stream = File.OpenRead(path);
            return Load(stream);
        }
        #endregion

        #region conversion
        public T ConvertTo<T>() => SsslConverter.Default.ConvertTo<T>(this);

        public object? ConvertTo(Type type) => SsslConverter.Default.ConvertTo(this, type);

        public bool TryConvertTo<T>(out T result) => SsslConverter.Default.TryConvertTo(this, out result!);

        public bool TryConvertTo(Type type, out object? result) => SsslConverter.Default.TryConvertTo(this, type, out result);

        public static SsslObject ConvertFrom(object? value) => SsslConverter.Default.ConvertFrom(value);
        #endregion

        #region serialize & deserialize
        public static SsslObject Parse(string source) => new SsslParser(source).Parse();

        public static string Serialize(object? value, ISsslConverter converter, SsslSerializationFormat format) => converter.ConvertFrom(value).ToString(format);

        public static string Serialize(object? value, ISsslConverter converter) => converter.ConvertFrom(value).ToString();

        public static string Serialize(object? value, SsslSerializationFormat format) => ConvertFrom(value).ToString(format);

        public static string Serialize(object? value) => ConvertFrom(value).ToString();

        [return: MaybeNull]
        public static T Deserialize<T>(string json) => Parse(json).ConvertTo<T>();

        public static object? Deserialize(string json, Type type) => Parse(json).ConvertTo(type);
        #endregion

        #region type converters
        public static explicit operator SsslObject[](SsslObject obj) => ((SsslRecord)obj).ToArray();

        public static explicit operator Dictionary<string, SsslObject>(SsslObject obj) => ((SsslRecord)obj).OfType<SsslPair>().ToDictionary(x => x.Key, x => x.Value);

        public static explicit operator KeyValuePair<string, SsslObject>(SsslObject obj) => obj is SsslPair pair
                ? KeyValuePair.Create(pair.Key, pair.Value)
                : throw new InvalidCastException();
        #endregion
    }
}
