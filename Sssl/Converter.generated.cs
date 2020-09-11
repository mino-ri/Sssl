#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Sssl
{
    public partial class DefaultSsslConverter
    {
        private bool TryConvertFromGenerated(object value, [NotNullWhen(true)] out SsslObject result)
        {
            switch (value)
            {
                case double v:
                    result = ConvertFrom(v);
                    return true;
                case bool v:
                    result = ConvertFrom(v);
                    return true;
                case string v:
                    result = ConvertFrom(v);
                    return true;
                case char v:
                    result = ConvertFrom(v);
                    return true;
                case DateTime v:
                    result = ConvertFrom(v);
                    return true;
                case DateTimeOffset v:
                    result = ConvertFrom(v);
                    return true;
                case byte v:
                    result = ConvertFrom((double)v);
                    return true;
                case sbyte v:
                    result = ConvertFrom((double)v);
                    return true;
                case short v:
                    result = ConvertFrom((double)v);
                    return true;
                case ushort v:
                    result = ConvertFrom((double)v);
                    return true;
                case int v:
                    result = ConvertFrom((double)v);
                    return true;
                case uint v:
                    result = ConvertFrom((double)v);
                    return true;
                case long v:
                    result = ConvertFrom((double)v);
                    return true;
                case ulong v:
                    result = ConvertFrom((double)v);
                    return true;
                case float v:
                    result = ConvertFrom((double)v);
                    return true;
                case decimal v:
                    result = ConvertFrom((double)v);
                    return true;
                default:
                    result = null!;
                    return false;
            }
        }

        private bool TryConvertToGenerated(SsslObject ssslObject, Type type, out object? result)
        {
            if (type == typeof(double))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)val : null!;
                return canConvert;
            }

            if (type == typeof(bool))
            {
                var canConvert = TryConvertTo(ssslObject, out bool val);
                result = canConvert ? (object)val : null!;
                return canConvert;
            }

            if (type == typeof(string))
            {
                var canConvert = TryConvertTo(ssslObject, out string val);
                result = canConvert ? (object)val : null!;
                return canConvert;
            }

            if (type == typeof(char))
            {
                var canConvert = TryConvertTo(ssslObject, out char val);
                result = canConvert ? (object)val : null!;
                return canConvert;
            }

            if (type == typeof(DateTime))
            {
                var canConvert = TryConvertTo(ssslObject, out DateTime val);
                result = canConvert ? (object)val : null!;
                return canConvert;
            }

            if (type == typeof(DateTimeOffset))
            {
                var canConvert = TryConvertTo(ssslObject, out DateTimeOffset val);
                result = canConvert ? (object)val : null!;
                return canConvert;
            }

            if (type == typeof(byte))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(byte)val : null!;
                return canConvert;
            }

            if (type == typeof(sbyte))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(sbyte)val : null!;
                return canConvert;
            }

            if (type == typeof(short))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(short)val : null!;
                return canConvert;
            }

            if (type == typeof(ushort))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(ushort)val : null!;
                return canConvert;
            }

            if (type == typeof(int))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(int)val : null!;
                return canConvert;
            }

            if (type == typeof(uint))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(uint)val : null!;
                return canConvert;
            }

            if (type == typeof(long))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(long)val : null!;
                return canConvert;
            }

            if (type == typeof(ulong))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(ulong)val : null!;
                return canConvert;
            }

            if (type == typeof(float))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(float)val : null!;
                return canConvert;
            }

            if (type == typeof(decimal))
            {
                var canConvert = TryConvertTo(ssslObject, out double val);
                result = canConvert ? (object)(decimal)val : null!;
                return canConvert;
            }

            result = null;
            return false;
        }
    }

    public partial class SsslObject
    {
        public static explicit operator double(SsslObject value) => SsslConverter.Default.ConvertTo<double>(value);

        public static explicit operator double?(SsslObject value) => SsslConverter.Default.ConvertTo<double?>(value);

        public static explicit operator double[](SsslObject value) => SsslConverter.Default.ConvertTo<double[]>(value);

        public static explicit operator bool(SsslObject value) => SsslConverter.Default.ConvertTo<bool>(value);

        public static explicit operator bool?(SsslObject value) => SsslConverter.Default.ConvertTo<bool?>(value);

        public static explicit operator bool[](SsslObject value) => SsslConverter.Default.ConvertTo<bool[]>(value);

        public static explicit operator char(SsslObject value) => SsslConverter.Default.ConvertTo<char>(value);

        public static explicit operator char?(SsslObject value) => SsslConverter.Default.ConvertTo<char?>(value);

        public static explicit operator char[](SsslObject value) => SsslConverter.Default.ConvertTo<char[]>(value);

        public static explicit operator DateTime(SsslObject value) => SsslConverter.Default.ConvertTo<DateTime>(value);

        public static explicit operator DateTime?(SsslObject value) => SsslConverter.Default.ConvertTo<DateTime?>(value);

        public static explicit operator DateTime[](SsslObject value) => SsslConverter.Default.ConvertTo<DateTime[]>(value);

        public static explicit operator DateTimeOffset(SsslObject value) => SsslConverter.Default.ConvertTo<DateTimeOffset>(value);

        public static explicit operator DateTimeOffset?(SsslObject value) => SsslConverter.Default.ConvertTo<DateTimeOffset?>(value);

        public static explicit operator DateTimeOffset[](SsslObject value) => SsslConverter.Default.ConvertTo<DateTimeOffset[]>(value);

        public static explicit operator byte(SsslObject value) => SsslConverter.Default.ConvertTo<byte>(value);

        public static explicit operator byte?(SsslObject value) => SsslConverter.Default.ConvertTo<byte?>(value);

        public static explicit operator byte[](SsslObject value) => SsslConverter.Default.ConvertTo<byte[]>(value);

        public static explicit operator sbyte(SsslObject value) => SsslConverter.Default.ConvertTo<sbyte>(value);

        public static explicit operator sbyte?(SsslObject value) => SsslConverter.Default.ConvertTo<sbyte?>(value);

        public static explicit operator sbyte[](SsslObject value) => SsslConverter.Default.ConvertTo<sbyte[]>(value);

        public static explicit operator short(SsslObject value) => SsslConverter.Default.ConvertTo<short>(value);

        public static explicit operator short?(SsslObject value) => SsslConverter.Default.ConvertTo<short?>(value);

        public static explicit operator short[](SsslObject value) => SsslConverter.Default.ConvertTo<short[]>(value);

        public static explicit operator ushort(SsslObject value) => SsslConverter.Default.ConvertTo<ushort>(value);

        public static explicit operator ushort?(SsslObject value) => SsslConverter.Default.ConvertTo<ushort?>(value);

        public static explicit operator ushort[](SsslObject value) => SsslConverter.Default.ConvertTo<ushort[]>(value);

        public static explicit operator int(SsslObject value) => SsslConverter.Default.ConvertTo<int>(value);

        public static explicit operator int?(SsslObject value) => SsslConverter.Default.ConvertTo<int?>(value);

        public static explicit operator int[](SsslObject value) => SsslConverter.Default.ConvertTo<int[]>(value);

        public static explicit operator uint(SsslObject value) => SsslConverter.Default.ConvertTo<uint>(value);

        public static explicit operator uint?(SsslObject value) => SsslConverter.Default.ConvertTo<uint?>(value);

        public static explicit operator uint[](SsslObject value) => SsslConverter.Default.ConvertTo<uint[]>(value);

        public static explicit operator long(SsslObject value) => SsslConverter.Default.ConvertTo<long>(value);

        public static explicit operator long?(SsslObject value) => SsslConverter.Default.ConvertTo<long?>(value);

        public static explicit operator long[](SsslObject value) => SsslConverter.Default.ConvertTo<long[]>(value);

        public static explicit operator ulong(SsslObject value) => SsslConverter.Default.ConvertTo<ulong>(value);

        public static explicit operator ulong?(SsslObject value) => SsslConverter.Default.ConvertTo<ulong?>(value);

        public static explicit operator ulong[](SsslObject value) => SsslConverter.Default.ConvertTo<ulong[]>(value);

        public static explicit operator float(SsslObject value) => SsslConverter.Default.ConvertTo<float>(value);

        public static explicit operator float?(SsslObject value) => SsslConverter.Default.ConvertTo<float?>(value);

        public static explicit operator float[](SsslObject value) => SsslConverter.Default.ConvertTo<float[]>(value);

        public static explicit operator decimal(SsslObject value) => SsslConverter.Default.ConvertTo<decimal>(value);

        public static explicit operator decimal?(SsslObject value) => SsslConverter.Default.ConvertTo<decimal?>(value);

        public static explicit operator decimal[](SsslObject value) => SsslConverter.Default.ConvertTo<decimal[]>(value);

        public static explicit operator string(SsslObject value) => SsslConverter.Default.ConvertTo<string>(value);

        public static explicit operator string[](SsslObject value) => SsslConverter.Default.ConvertTo<string[]>(value);

        public static implicit operator SsslObject(double value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(double? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(double[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(bool value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(bool? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(bool[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(char value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(char? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(char[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(DateTime value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(DateTime? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(DateTime[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(DateTimeOffset value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(DateTimeOffset? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(DateTimeOffset[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(byte value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(byte? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(byte[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(sbyte value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(sbyte? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(sbyte[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(short value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(short? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(short[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(ushort value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(ushort? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(ushort[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(int value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(int? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(int[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(uint value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(uint? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(uint[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(long value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(long? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(long[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(ulong value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(ulong? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(ulong[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(float value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(float? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(float[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(decimal value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(decimal? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(decimal[] value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(string? value) => SsslConverter.Default.ConvertFrom(value);

        public static implicit operator SsslObject(string[] value) => SsslConverter.Default.ConvertFrom(value);

    }
}