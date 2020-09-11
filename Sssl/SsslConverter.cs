using System;
using System.Diagnostics.CodeAnalysis;

namespace Sssl
{
    public interface ISsslConverter
    {
        bool TryConvertFrom(object? value, [NotNullWhen(true)] out SsslObject result);

        bool TryConvertTo(SsslObject ssslObject, Type type, out object? result);
    }

    public static class SsslConverter
    {
        public static DefaultSsslConverter Default { get; } = new DefaultSsslConverter(ObjectConversionOptions.AllowMissingMember | ObjectConversionOptions.AllowUnknownMember);

        public static DefaultSsslConverter Strict { get; } = new DefaultSsslConverter(ObjectConversionOptions.None);

        public static SsslObject ConvertFrom(this ISsslConverter converter, object? value)
        {
            if (converter is null) throw new ArgumentNullException(nameof(converter));

            return converter.TryConvertFrom(value, out var result)
                ? result
                : throw new InvalidCastException();
        }

        public static object? ConvertTo(this ISsslConverter converter, SsslObject ssslObject, Type type)
        {
            if (converter is null) throw new ArgumentNullException(nameof(converter));

            return converter.TryConvertTo(ssslObject, type, out var result)
                ? result
                : throw new InvalidCastException();
        }

        public static T ConvertTo<T>(this ISsslConverter converter, SsslObject ssslObject)
        {
            if (converter is null) throw new ArgumentNullException(nameof(converter));

            return (T)converter.ConvertTo(ssslObject, typeof(T))!;
        }

        public static bool TryConvertTo<T>(this ISsslConverter converter, SsslObject ssslObject, [MaybeNull] out T result)
        {
            if (converter is null) throw new ArgumentNullException(nameof(converter));

            if (converter.TryConvertTo(ssslObject, typeof(T), out var obj))
            {
                result = (T)obj!;
                return true;
            }

            result = default!;
            return false;
        }
    }
}
