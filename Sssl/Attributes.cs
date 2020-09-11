using System;

namespace Sssl
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class NotSsslFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SsslFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SsslNameAttribute : Attribute
    {
        public string Name { get; set; }

        public SsslNameAttribute(string name)
        {
            Name = name;
        }
    }
}
