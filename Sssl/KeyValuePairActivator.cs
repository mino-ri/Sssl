using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sssl
{
    internal static class KeyValuePairActivator
    {
        private delegate (string, object?) PairDeconstructor(object pair);

        private static readonly ConcurrentDictionary<Type, PairDeconstructor> Cache =
            new ConcurrentDictionary<Type, PairDeconstructor>();

        public static object Create(Type valueType, string key, object? value) =>
            TupleActivator.Create(typeof(KeyValuePair<,>).MakeGenericType(typeof(string), valueType), key, value);

        public static (string key, object? value) Deconstruct(object pair)
        {
            var type = pair.GetType().GetGenericArguments()[1];
            return Cache.GetOrAdd(type, CreateDeconstructor)(pair);
        }

        private static PairDeconstructor CreateDeconstructor(Type pairType)
        {
            var ctor = typeof((string, object)).GetConstructor(new[] { typeof(string), typeof(object) });
            var pair = Expression.Parameter(typeof(object), "pair");
            var converted = Expression.Convert(pair, pairType);

            return Expression.Lambda<PairDeconstructor>(
                    Expression.New(ctor,
                        Expression.Property(converted, "Key"),
                        Expression.Convert(Expression.Property(converted, "Value"), typeof(object))),
                    pair)
                .Compile();
        }
    }
}
