using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;

namespace Sssl
{
    [DebuggerDisplay("{DebuggerPrint(),nq}")]
    internal class SsslDynamicProvider : DynamicObject
    {
        private readonly SsslObject _ssslObject;
        private readonly ISsslConverter _converter;

        internal SsslDynamicProvider(SsslObject ssslObject)
        {
            _ssslObject = ssslObject;
            _converter = SsslConverter.Default;
        }

        internal SsslDynamicProvider(SsslObject ssslObject, ISsslConverter converter)
        {
            _ssslObject = ssslObject;
            _converter = converter;
        }

        internal SsslObject GetSource() => _ssslObject;

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _ssslObject switch
            {
                SsslRecord record => record.GetKeys().Concat(Enumerable.Range(0, record.Count).Select(x => x.ToString())).Concat(new[] { "Count" }),
                SsslPair _ => new[] { "Key", "Value" },
                _ => Enumerable.Empty<string>(),
            };
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            switch (_ssslObject)
            {
                case SsslRecord record when binder.Name == "Count":
                    result = record.Count;
                    return true;

                case SsslRecord record when int.TryParse(binder.Name, out var index) && 0 <= index && index < record.Count:
                    result = new SsslDynamicProvider(record[index], _converter);
                    return true;

                case SsslRecord record:
                    var hasMember = record.TryGetValue(binder.Name, out var value);
                    result = new SsslDynamicProvider(value, _converter);
                    return hasMember;

                case SsslPair pair when binder.Name == "Key":
                    result = pair.Key;
                    return true;

                case SsslPair pair when binder.Name == "Value":
                    result = new SsslDynamicProvider(pair.Value, _converter);
                    return true;

                default:
                    result = null;
                    return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            if (_ssslObject is SsslPair pair)
            {
                switch (binder.Name)
                {
                    case "Key":
                        pair.Key = value?.ToString() ?? "";
                        return true;
                    case "Value":
                        pair.Value = _converter.ConvertFrom(value);
                        return true;
                    default:
                        return false;
                }
            }

            if (!(_ssslObject is SsslRecord record))
                return false;

            if (int.TryParse(binder.Name, out var index))
            {
                if (value == SsslObject.Undefined)
                    record.RemoveAt(index);
                else
                    record[index] = _converter.ConvertFrom(value);

                return true;
            }

            if (value == SsslObject.Undefined)
                record.Remove(binder.Name);
            else
                record[binder.Name] = _converter.ConvertFrom(value);

            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            if (indexes.Length != 1)
            {
                result = null;
                return false;
            }

            if (_ssslObject is SsslRecord record)
            {
                if (indexes[0] is string memberName)
                {
                    var hasMember = record.TryGetValue(memberName, out var value);
                    result = new SsslDynamicProvider(value, _converter);
                    return hasMember;
                }

                if (indexes[0] is int index && 0 <= index && index < record.Count)
                {
                    result = new SsslDynamicProvider(record[index], _converter);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
        {
            if (indexes.Length != 1) return false;

            if (_ssslObject is SsslRecord record)
            {
                if (indexes[0] is string memberName)
                {
                    if (value == SsslObject.Undefined)
                        record.RemoveKey(memberName);
                    else
                        record[memberName] = _converter.ConvertFrom(value);
                    return true;
                }

                if (indexes[0] is int index && 0 <= index && index < record.Count)
                {
                    if (value == SsslObject.Undefined)
                        record.RemoveAt(index);
                    else
                        record[index] = _converter.ConvertFrom(value);
                    return true;
                }
            }

            return false;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[] args, out object? result)
        {
            if (args.Length == 0)
            {
                result = binder.Name == "Null"
                    ? _ssslObject == SsslValue.Null
                    : _ssslObject is SsslRecord record && record.HasKey(binder.Name);
                return true;
            }

            if (args.Length == 1)
            {
                switch (binder.Name, args[0], _ssslObject)
                {
                    case ("Has", string memberName, SsslRecord record):
                        result = record.HasKey(memberName);
                        return true;

                    case ("Remove", string memberName, SsslRecord record):
                        record.RemoveKey(memberName);
                        result = null;
                        return true;

                    case ("Remove", int index, SsslRecord record):
                        record.RemoveAt(index);
                        result = null;
                        return true;

                    case ("Add", var item, SsslRecord record):
                        record.Add(_converter.ConvertFrom(item));
                        result = null;
                        return true;
                }
            }

            result = null;
            return false;
        }

        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            if (binder.Type != typeof(IEnumerable))
                return _converter.TryConvertTo(_ssslObject, binder.Type, out result);

            if (_ssslObject is SsslRecord record)
            {
                result = record.Select(element => new SsslDynamicProvider(element, _converter));
                return true;
            }

            result = null;
            return false;
        }

        public override string ToString() => _ssslObject.ToString();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string DebuggerPrint()
        {
            var str = _ssslObject.DebuggerPrint();
            return str.Length > 512 ? str.Substring(0, 509).Replace('"', '\'') + "..." : str.Replace('"', '\'');
        }

    }
}
