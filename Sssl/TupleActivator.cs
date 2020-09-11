using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace Sssl
{
    internal static class TupleActivator
    {
        private delegate object Constructor(object?[] args);

        private static readonly ConcurrentDictionary<Type, Constructor> ConstructorCache = new ConcurrentDictionary<Type, Constructor>();

        public static object Create(Type type, params object?[] args)
        {
            return ConstructorCache.GetOrAdd(type, CreateConstructor)(args);
        }

        private static Constructor CreateConstructor(Type type)
        {
            var arguments = type.GetGenericArguments();
            var ctor = type.GetConstructor(arguments) ?? throw new ArgumentException("Type does not have valid constructor.", nameof(type));

            var param = Expression.Parameter(typeof(object[]), "args");
            var args = arguments
                .Select((argType, i) => (Expression)Expression.Convert(Expression.ArrayIndex(param, Expression.Constant(i)), argType))
                .ToArray();

            return Expression.Lambda<Constructor>(
                    Expression.Convert(Expression.New(ctor, args), typeof(object)),
                    param)
                .Compile();
        }
    }
}
