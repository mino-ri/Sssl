using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Sssl
{
    [DebuggerDisplay("{DebuggerPrint()}")]
    public class SsslRecord : SsslObject, IList<SsslObject>
    {
        private readonly List<SsslObject> _contents;

        private SsslRecordType _recordType;
        public SsslRecordType RecordType
        {
            get => _recordType;
            set
            {
                if (value < SsslRecordType.Parentheses || SsslRecordType.Brackets < value)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _recordType = value;
            }
        }

        private string _name = "";
        public string Name { get => _name; set => _name = value ?? ""; }

        public int Count => _contents.Count;

        public SsslObject this[int index] { get => _contents[index]; set => _contents[index] = value ?? SsslValue.Null; }

        public SsslObject this[string key]
        {
            get => (FindPair(key) ?? throw new KeyNotFoundException()).Value;
            set
            {
                var pair = FindPair(key);
                if (pair is null)
                    Add(new SsslPair(key, value ?? SsslValue.Null));
                else
                    pair.Value = value ?? SsslValue.Null;
            }
        }

        public IEnumerable<string> GetKeys() => _contents.OfType<SsslPair>().Select(x => x.Key);

        public SsslRecord(SsslRecordType recordType)
        {
            RecordType = recordType;
            _contents = new List<SsslObject>();
        }

        public SsslRecord(SsslRecordType recordType, string name)
        {
            RecordType = recordType;
            Name = name;
            _contents = new List<SsslObject>();
        }

        public SsslRecord(SsslRecordType recordType, IEnumerable<SsslObject> contents)
        {
            RecordType = recordType;
            _contents = contents.Select(x => x ?? SsslValue.Null).ToList();
        }

        public SsslRecord(SsslRecordType recordType, string name, IEnumerable<SsslObject> contents)
        {
            RecordType = recordType;
            Name = name;
            _contents = contents.Select(x => x ?? SsslValue.Null).ToList();
        }

        private SsslPair? FindPair(string key) => (SsslPair?)_contents.FirstOrDefault(x => x is SsslPair pair && pair.Key == key);

        public bool HasKey(string key) => FindPair(key) is { };

        public bool TryGetValue(string key, [NotNullWhen(true)] out SsslObject result)
        {
            var pair = FindPair(key);
            result = pair?.Value!;
            return pair is { };
        }

        public int IndexOf(SsslObject item) => _contents.IndexOf(item);

        public void Insert(int index, SsslObject item) => _contents.Insert(index, item ?? SsslValue.Null);

        public void RemoveAt(int index) => _contents.RemoveAt(index);

        public void Add(SsslObject item) => _contents.Add(item ?? SsslValue.Null);

        public void Clear() => _contents.Clear();

        public bool Contains(SsslObject item) => _contents.Contains(item);

        public void CopyTo(SsslObject[] array, int arrayIndex) => _contents.CopyTo(array, arrayIndex);

        public bool Remove(SsslObject item) => _contents.Remove(item);

        public bool RemoveKey(string key) => FindPair(key) is { } pair && Remove(pair);

        public override void WriteTo(ISsslWriter writer) => writer.Write(this);

        public override void WriteTo<TContext>(ISsslWriter<TContext> writer, TContext context) => writer.Write(context, this);

        public IEnumerator<SsslObject> GetEnumerator() => ((IList<SsslObject>)_contents).GetEnumerator();

        bool ICollection<SsslObject>.IsReadOnly => ((IList<SsslObject>)_contents).IsReadOnly;

        IEnumerator IEnumerable.GetEnumerator() => ((IList<SsslObject>)_contents).GetEnumerator();
    }

    public enum SsslRecordType
    {
        Parentheses,
        Braces,
        Brackets,
    }
}
