using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sssl
{
    internal static class SsslObjectConverter
    {
        private static readonly ConcurrentDictionary<Type, ObjectModelInfo> ObjectModelInfos =
            new ConcurrentDictionary<Type, ObjectModelInfo>();

        private static ObjectModelInfo GetModelInfo(Type type)
        {
            return ObjectModelInfos.GetOrAdd(type, t =>
                (ObjectModelInfo)typeof(SsslObjectModel<>).MakeGenericType(type).GetField(nameof(SsslObjectModel<object>.ModelInfo)).GetValue(null));
        }

        public static bool TryConvertFrom(ISsslConverter converter, object value, [NotNullWhen(true)] out SsslObject result)
        {
            var type = value.GetType();
            var obj = new SsslRecord(SsslRecordType.Braces, type.GetFullName());
            var values = GetModelInfo(type).ConvertFrom(value);

            foreach (var (name, val) in values)
            {
                if (!converter.TryConvertFrom(val, out var ssslVal))
                {
                    result = null!;
                    return false;
                }

                obj[name] = ssslVal;
            }

            result = obj;
            return true;
        }

        public static bool TryConvertTo(ISsslConverter converter, SsslObject sssl, Type type, ObjectConversionOptions options, [NotNullWhen(true)] out object result)
        {
            var allowUnknownMember = options.HasFlag(ObjectConversionOptions.AllowUnknownMember);
            var allowMissingMember = options.HasFlag(ObjectConversionOptions.AllowMissingMember);

            result = null!;

            if (!(sssl is SsslRecord record))
                return false;

            if (record.Name != "" && !type.IsSealed && record.Name != type.GetFullName())
            {
                if (type.Assembly.GetType(record.Name) is { } assemblyInnerType &&
                    type.IsAssignableFrom(assemblyInnerType))
                {
                    type = assemblyInnerType;
                }
                if (Type.GetType(record.Name) is { } coreType &&
                    type.IsAssignableFrom(coreType))
                {
                    type = coreType;
                }
                else if (AppDomain.CurrentDomain
                        .GetAssemblies()
                        .Select(x => x.GetType(record.Name))
                        .FirstOrDefault(x => x is { } && type.IsAssignableFrom(x)) is { } otherType &&
                        type.IsAssignableFrom(otherType))
                {
                    type = otherType;
                }
            }

            var modelInfo = GetModelInfo(type);
            if (!modelInfo.CanCreateInstance)
                return false;

            if (!allowUnknownMember && record.GetKeys().Any(memberName => !modelInfo.RequiredValues.ContainsKey(memberName)))
                return false;

            var values = new Dictionary<string, object?>();
            foreach (var (name, memberType) in modelInfo.RequiredValues)
            {
                if (record.TryGetValue(name, out var valueSssl))
                {
                    if (converter.TryConvertTo(valueSssl, memberType, out var value))
                        values[name] = value;
                    else
                        return false;
                }
                else if (!allowMissingMember)
                    return false;
            }

            result = modelInfo.ConvertTo(values);
            return true;
        }
    }

    internal class SsslObjectModel<T>
    {
        private readonly struct Accessor
        {
            public readonly Type Type;
            public readonly Func<T, object?> Getter;
            public readonly Action<T, object?>? Setter;

            public Accessor(Type type, Func<T, object?> getter, Action<T, object?>? setter)
            {
                Type = type;
                Getter = getter;
                Setter = setter;
            }
        }

        private static readonly Dictionary<string, Accessor> Accessors;
        private static readonly Func<T>? Constructor;

        // ReSharper disable once StaticMemberInGenericType
        public static ObjectModelInfo ModelInfo;

        static SsslObjectModel()
        {
            var type = typeof(T);

            if (type.IsValueType)
            {
                Constructor = () => default!;
            }
            else if (type.GetConstructor(Type.EmptyTypes) is { })
            {
                Constructor = Expression.Lambda<Func<T>>(Expression.New(type)).Compile();
            }

            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.GetCustomAttribute<NotSsslFieldAttribute>() is null)
                .Select(x => (field: x, name: GetCustomName(x)))
                .Where(x => x.name is { })
                .ToArray();

            var props = type
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.GetMethod != null && x.GetCustomAttribute<NotSsslFieldAttribute>() is null)
                .Select(x => (prop: x, name: GetCustomName(x)))
                .Where(x => x.name is { })
                .ToArray();

            var value = Expression.Parameter(typeof(object), "value");
            var obj = Expression.Parameter(typeof(T), "obj");

            Accessors = new Dictionary<string, Accessor>();

            foreach (var (field, name) in fields)
            {
                Accessors[name] = new Accessor(
                    field.FieldType,
                    Expression.Lambda<Func<T, object?>>(
                            Expression.Convert(Expression.Field(obj, field), typeof(object)),
                            obj)
                        .Compile(),
                    Expression.Lambda<Action<T, object?>>(
                            Expression.Assign(Expression.Field(obj, field), Expression.Convert(value, field.FieldType)),
                            obj, value)
                        .Compile());
            }

            foreach (var (prop, name) in props)
            {
                Accessors[name] = new Accessor(
                    prop.PropertyType,
                    Expression.Lambda<Func<T, object?>>(
                            Expression.Convert(Expression.Property(obj, prop), typeof(object)),
                            obj)
                        .Compile(),
                    prop.SetMethod is null ? null :
                    Expression.Lambda<Action<T, object?>>(
                            Expression.Assign(Expression.Property(obj, prop), Expression.Convert(value, prop.PropertyType)),
                            obj, value)
                        .Compile());
            }

            ModelInfo = new ObjectModelInfo(
                CanCreateInstance(),
                GetMembers,
                CreateInstance,
                Accessors.ToDictionary(x => x.Key, x => x.Value.Type));
        }

        private static string? GetCustomName(PropertyInfo prop)
        {
            if (prop.GetCustomAttribute<SsslNameAttribute>() is { } ssslName)
                return ssslName.Name;

            if (prop.GetCustomAttribute<DebuggerBrowsableAttribute>() is { } db &&
                db.State == DebuggerBrowsableState.Never)
                return null;

            if (prop.GetCustomAttribute<SsslFieldAttribute>() is { } || prop.GetMethod.IsPublic)
                return prop.Name;

            return null;
        }

        private static string? GetCustomName(FieldInfo field)
        {
            if (field.GetCustomAttribute<SsslNameAttribute>() is { } ssslName)
                return ssslName.Name;

            if (field.GetCustomAttribute<DebuggerBrowsableAttribute>() is { } db &&
                db.State == DebuggerBrowsableState.Never)
                return null;

            if (field.GetCustomAttribute<SsslFieldAttribute>() is { } || field.IsPublic)
                return field.Name;

            return null;
        }

        private static bool CanCreateInstance() => Constructor is { };

        private static object CreateInstance(IEnumerable<KeyValuePair<string, object?>> members)
        {
            var instance = Constructor!();
            SetMembers(instance, members);
            return instance!;
        }

        private static IEnumerable<KeyValuePair<string, object?>> GetMembers(object obj)
        {
            var typedObj = (T)obj;
            foreach (var (name, accessor) in Accessors)
            {
                yield return new KeyValuePair<string, object?>(name, accessor.Getter(typedObj));
            }
        }

        private static void SetMembers(T obj, IEnumerable<KeyValuePair<string, object?>> members)
        {
            foreach (var (name, value) in members)
            {
                if (Accessors.TryGetValue(name, out var accessor))
                    accessor.Setter?.Invoke(obj, value);
            }
        }
    }

    internal class ObjectModelInfo
    {
        public readonly bool CanCreateInstance;
        public readonly Func<object, IEnumerable<KeyValuePair<string, object?>>> ConvertFrom;
        public readonly Func<IEnumerable<KeyValuePair<string, object?>>, object> ConvertTo;
        public readonly Dictionary<string, Type> RequiredValues;

        public ObjectModelInfo(bool canCreateInstance,
            Func<object, IEnumerable<KeyValuePair<string, object?>>> convertFrom,
            Func<IEnumerable<KeyValuePair<string, object?>>, object> convertTo,
            Dictionary<string, Type> requiredValues)
        {
            CanCreateInstance = canCreateInstance;
            ConvertFrom = convertFrom;
            ConvertTo = convertTo;
            RequiredValues = requiredValues;
        }
    }
}
