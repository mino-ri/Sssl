using System.Diagnostics;

namespace Sssl
{
    [DebuggerDisplay("{DebuggerPrint(),nq}")]
    public class SsslPair : SsslObject
    {
        private string _key = "";
        public string Key { get => _key; set => _key = value ?? ""; }

        private SsslObject _value = SsslValue.Null;
        public SsslObject Value { get => _value; set => _value = value ?? SsslValue.Null; }

        public SsslPair() { }

        public SsslPair(string key, SsslObject value)
        {
            Key = key;
            Value = value;
        }

        public void Deconstruct(out string key, out SsslObject value)
        {
            key = Key;
            value = Value;
        }

        public override void WriteTo(ISsslWriter writer) => writer.Write(this);

        public override void WriteTo<TContext>(ISsslWriter<TContext> writer, TContext context) => writer.Write(context, this);
    }
}
