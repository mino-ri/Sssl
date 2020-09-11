using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sssl
{
    public partial class DefaultSsslConverter : ISsslConverter
    {
        private readonly ObjectConversionOptions _objectConversionOptions;

        public DefaultSsslConverter(ObjectConversionOptions objectConversionOptions)
        {
            _objectConversionOptions = objectConversionOptions;
        }

        public bool TryConvertFrom(object? value, [NotNullWhen(true)] out SsslObject result)
        {
            return value switch
            {
                null => Success(SsslValue.Null, out result),
                SsslObject ssslObject => Success(ssslObject, out result),
                _ => TryConvertFromCore(value, out result),
            };
        }

        public bool TryConvertTo(SsslObject ssslObject, Type type, out object? result)
        {
            var isNullable = type.IsGenericOf(typeof(Nullable<>));
            if (ssslObject == SsslValue.Null)
            {
                result = null;
                return isNullable || !type.IsValueType;
            }

            if (isNullable)
                type = type.GetGenericArguments()[0];

            return TryConvertToCore(ssslObject, type, out result);
        }

        protected virtual bool TryConvertFromCore(object value, [NotNullWhen(true)] out SsslObject result)
        {
            if (TryConvertFromGenerated(value, out result))
                return true;

            var type = value.GetType();
            if (type.IsGenericOf(typeof(KeyValuePair<,>)) && type.GetGenericArguments()[0] == typeof(string))
                return TryConvertFromKeyValuePair(value, out result);

            return value switch
            {
                SsslObject ssslObject => Success(ssslObject, out result),
                SsslDynamicProvider ssslDynamic => Success(ssslDynamic.GetSource(), out result),
                ITuple t => TryConvertFromTuple(t, out result),
                IDictionary d => TryConvertFromDictionary(d, out result),
                IEnumerable e => TryConvertFromEnumerable(e, out result),
                _ => TryConvertFromObject(value, out result),
            };
        }

        protected virtual bool TryConvertToCore(SsslObject ssslObject, Type type, out object? result)
        {
            if (TryConvertToRaw(ssslObject, type, out result) ||
                TryConvertToGenerated(ssslObject, type, out result))
                return true;

            if (type.IsGenericType && (TryConvertToTuple(ssslObject, type, out result) ||
                                       TryConvertToKeyValuePair(ssslObject, type, out result) ||
                                       TryConvertToDictionary(ssslObject, type, out result)))
                return true;

            return TryConvertToArray(ssslObject, type, out result) ||
                   TryConvertToObject(ssslObject, type, out result);
        }

        public SsslObject ConvertFrom(double value) => new SsslValue(value);

        public SsslObject ConvertFrom(bool value) => new SsslValue(value);

        public SsslObject ConvertFrom(string? value) => value is null ? SsslValue.Null : new SsslValue(value);

        public SsslObject ConvertFrom(char value) => new SsslValue(new string(value, 1));

        public SsslObject ConvertFrom(DateTime value) => new SsslValue(value.ToString(CultureInfo.InvariantCulture));

        public SsslObject ConvertFrom(DateTimeOffset value) => new SsslValue(value.ToString(CultureInfo.InvariantCulture));

        public bool TryConvertFromKeyValuePair(object pair, [NotNullWhen(true)] out SsslObject result)
        {
            var (key, value) = KeyValuePairActivator.Deconstruct(pair);
            return TryConvertFrom(value, out var ssslValue)
                ? Success(new SsslPair(key, ssslValue), out result)
                : Failure(out result);
        }

        public bool TryConvertFromTuple(ITuple tuple, [NotNullWhen(true)] out SsslObject result)
        {
            var ssslRecord = new SsslRecord(SsslRecordType.Parentheses);
            for (var i = 0; i < tuple.Length; i++)
            {
                if (TryConvertFrom(tuple[i], out var sssl))
                {
                    ssslRecord.Add(sssl);
                }
                else
                {
                    return Failure(out result);
                }
            }

            return Success(ssslRecord, out result);
        }

        public bool TryConvertFromEnumerable(IEnumerable enumerable, [NotNullWhen(true)] out SsslObject result)
        {
            var ssslRecord = new SsslRecord(SsslRecordType.Brackets);
            foreach (var obj in enumerable)
            {
                if (TryConvertFrom(obj, out var sssl))
                {
                    ssslRecord.Add(sssl);
                }
                else
                {
                    return Failure(out result);
                }
            }

            return Success(ssslRecord, out result);
        }

        public bool TryConvertFromDictionary(IDictionary dictionary, [NotNullWhen(true)] out SsslObject result)
        {
            if (dictionary.GetType().GetInterfaces().Any(type => type.IsGenericOfAny(typeof(IDictionary<,>), typeof(IReadOnlyDictionary<,>)) &&
                                                                 type.GetGenericArguments()[0] == typeof(string)))
            {
                var ssslRecord = new SsslRecord(SsslRecordType.Braces);
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (TryConvertFrom(entry.Value, out var sssl))
                    {
                        ssslRecord[entry.Key.ToString()] = sssl;
                    }
                    else
                    {
                        return Failure(out result);
                    }
                }

                return Success(ssslRecord, out result);
            }

            return TryConvertFromEnumerable(dictionary, out result);
        }

        public SsslObject ConvertFromObject(object obj)
        {
            return TryConvertFromObject(obj, out var result)
                ? result
                : throw new InvalidCastException();
        }

        public bool TryConvertFromObject(object obj, [NotNullWhen(true)] out SsslObject result)
        {
            return SsslObjectConverter.TryConvertFrom(this, obj, out result);
        }

        public bool TryConvertTo(SsslObject ssslObject, out double result)
        {
            return ssslObject is SsslValue val && val.Value is double d
                ? Success(d, out result)
                : Failure(out result);
        }

        public bool TryConvertTo(SsslObject ssslObject, out bool result)
        {
            return ssslObject is SsslValue val && val.Value is bool d
                ? Success(d, out result)
                : Failure(out result);
        }

        public bool TryConvertTo(SsslObject ssslObject, [NotNullWhen(true)] out string result)
        {
            return ssslObject is SsslValue val && val.Value is string d
                ? Success(d, out result)
                : Failure(out result);
        }

        public bool TryConvertTo(SsslObject ssslObject, out char result)
        {
            return TryConvertTo(ssslObject, out string str) && str.Length == 1
                ? Success(str[0], out result)
                : Failure(out result);
        }

        public bool TryConvertTo(SsslObject ssslObject, out DateTime result)
        {
            result = default;
            return TryConvertTo(ssslObject, out string str) &&
                   DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }

        public bool TryConvertTo(SsslObject ssslObject, out DateTimeOffset result)
        {
            result = default;
            return TryConvertTo(ssslObject, out string str) && 
                   DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }

        public bool TryConvertToKeyValuePair(SsslObject ssslObject, Type type, [NotNullWhen(true)] out object result)
        {
            if (!type.IsGenericOf(typeof(KeyValuePair<,>))) return Failure(out result);

            var typeArgs = type.GetGenericArguments();
            if (typeArgs[0] != typeof(string)) return Failure(out result);
            
            var valueType = typeArgs[1];
            var nullable = valueType.IsNullable();
            if (!(ssslObject is SsslPair(var key, var valueSssl))) return Failure(out result);

            if (valueSssl != SsslValue.Null || IsRawValueType(valueType))
            {
                if (TryConvertTo(valueSssl, valueType, out var value))
                {
                    return Success(KeyValuePairActivator.Create(valueType, key, value), out result);
                }
            }
            else if (nullable)
            {
                return Success(KeyValuePairActivator.Create(valueType, key, null), out result);
            }

            return Failure(out result);
        }

        public bool TryConvertToTuple(SsslObject ssslObject, Type type, [NotNullWhen(true)] out object result)
        {
            if (!typeof(ITuple).IsAssignableFrom(type)) return Failure(out result);

            var typeArgs = type.GetGenericArguments();

            if (ssslObject is SsslPair pair && typeArgs.Length == 2 && typeArgs[0] == typeof(string))
            {
                if (!TryConvertTo(pair.Value, typeArgs[1], out var value)) return Failure(out result);

                return Success(TupleActivator.Create(type, pair.Key, value), out result);
            }

            if (!(ssslObject is SsslRecord record) || record.Count != typeArgs.Length) return Failure(out result);

            var values = new object?[typeArgs.Length];
            for (var i = 0; i < values.Length; i++)
            {
                if (!TryConvertTo(record[i], typeArgs[i], out values[i]))
                    return Failure(out result);
            }

            return Success(TupleActivator.Create(type, values), out result);
        }

        public bool TryConvertToArray(SsslObject ssslObject, Type type, [NotNullWhen(true)] out object result)
        {
            if (!(ssslObject is SsslRecord record)) return Failure(out result);

            Type elementType;
            if (type.IsArray)
                elementType = type.GetElementType();
            else if (type.IsGenericOfAny(typeof(IEnumerable<>), typeof(IList<>), typeof(ICollection<>), typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>)))
                elementType = type.GetGenericArguments()[0];
            else
                return Failure(out result);

            var nullable = elementType.IsNullable();
            var array = Array.CreateInstance(elementType, record.Count);
            for (var i = 0; i < array.Length; i++)
            {
                if (record[i] != SsslValue.Null || IsRawValueType(elementType))
                {
                    if (!TryConvertTo(record[i], elementType, out var element)) return Failure(out result);
                    array.SetValue(element, i);
                }
                else if (!nullable)
                {
                    return Failure(out result);
                }
            }

            return Success(array, out result);
        }

        public bool TryConvertToDictionary(SsslObject ssslObject, Type type, [NotNullWhen(true)] out object result)
        {
            if (!(ssslObject is SsslRecord record) ||
                !type.IsGenericOfAny(typeof(IDictionary<,>), typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>)))
                return Failure(out result);

            var argumentTypes = type.GetGenericArguments();
            if (argumentTypes[0] != typeof(string))
                return Failure(out result);

            var valueType = argumentTypes[1];
            var nullable = valueType.IsNullable();
            var dictionary = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType));

            foreach (var (key, valueSssl) in record.OfType<SsslPair>())
            {
                if (valueSssl != SsslValue.Null || IsRawValueType(valueType))
                {
                    if (!TryConvertTo(valueSssl, valueType, out var value)) return Failure(out result);
                    dictionary.Add(key, value);
                }
                else if (nullable)
                {
                    dictionary.Add(key, null);
                }
                else
                {
                    return Failure(out result);
                }
            }

            return Success(dictionary, out result);
        }

        public bool TryConvertToObject(SsslObject ssslObject, Type type, [NotNullWhen(true)] out object result)
        {
            return SsslObjectConverter.TryConvertTo(this, ssslObject, type, _objectConversionOptions, out result);
        }

        public bool TryConvertToObject<T>(SsslObject ssslObject, [NotNullWhen(true)] out T result)
        {
            var success = SsslObjectConverter.TryConvertTo(this, ssslObject, typeof(T), _objectConversionOptions, out var value);
            result = success ? default! : (T)value;
            return success;
        }

        private bool TryConvertToRaw(SsslObject ssslObject, Type type, [NotNullWhen(true)] out object result)
        {
            if (type == typeof(object))
            {
                return Success(new SsslDynamicProvider(ssslObject, this), out result);
            }

            if (typeof(SsslObject).IsAssignableFrom(type))
            {
                var canConvert = type.IsInstanceOfType(ssslObject);
                result = canConvert ? ssslObject : null!;
                return canConvert;
            }

            if (type == typeof(SsslObject[]))
            {
                result = ssslObject is SsslRecord array ? array.ToArray() : null!;
                return result is { };
            }

            if (type.IsAssignableFrom(typeof(SsslPair)))
            {
                result = (ssslObject as SsslPair)!;
                return result is { };
            }

            if (type.IsAssignableFrom(typeof(SsslRecord)))
            {
                result = (ssslObject as SsslRecord)!;
                return result is { };
            }

            if (type.IsAssignableFrom(typeof(SsslValue)))
            {
                result = (ssslObject as SsslValue)!;
                return result is { };
            }

            return Failure(out result);
        }

        private static bool IsRawValueType(Type type) => type.IsAssignableFrom(typeof(SsslValue));

        private bool Success<T>(T value, out T result)
        {
            result = value;
            return true;
        }

        private bool Failure<T>(out T result)
        {
            result = default!;
            return false;
        }
    }

    [Flags]
    public enum ObjectConversionOptions
    {
        None = 0,
        AllowMissingMember = 1,
        AllowUnknownMember = 2,
    }
}
