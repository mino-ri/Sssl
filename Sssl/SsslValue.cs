using System.Diagnostics;

namespace Sssl
{
    [DebuggerDisplay("{DebuggerPrint(),nq}")]
    public class SsslValue : SsslObject
    {
        public object? Value { get; }

        public SsslValueType Type { get; }

        public static SsslValue Null { get; } = new SsslValue();

        private SsslValue()
        {
            Value = null;
            Type = SsslValueType.Null;
        }

        internal SsslValue(double value)
        {
            Value = value;
            Type = SsslValueType.Number;
        }

        internal SsslValue(bool value)
        {
            Value = value;
            Type = SsslValueType.Boolean;
        }

        internal SsslValue(string value)
        {
            Value = value;
            Type = SsslValueType.String;
        }

        public override void WriteTo(ISsslWriter writer) => writer.Write(this);

        public override void WriteTo<TContext>(ISsslWriter<TContext> writer, TContext context) => writer.Write(context, this);
    }

    public enum SsslValueType
    {
        Null,
        String,
        Boolean,
        Number,
    }
}
