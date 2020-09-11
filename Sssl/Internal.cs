using System;
using System.Linq;

namespace Sssl
{
    internal static class Internal
    {
        public static bool IsGenericOf(this Type type, Type genericType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == genericType;
        }

        public static bool IsGenericOfAny(this Type type, params Type[] genericTypes)
        {
            return type.IsGenericType && genericTypes.Contains(type.GetGenericTypeDefinition());
        }

        public static bool IsNullable(this Type type)
        {
            return !type.IsValueType || type.IsGenericOf(typeof(Nullable<>));
        }

        public static string GetFullName(this Type type)
        {
            if (type.Name.StartsWith("<>f__AnonymousType"))
            {
                return "";
            }

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var def = type.GetGenericTypeDefinition();
                return def.FullName + "[" +
                    string.Join(", ", type.GetGenericArguments().Select(GetFullName)) +
                    "]";
            }
            else
            {
                return type.FullName;
            }
        }
    }
}
