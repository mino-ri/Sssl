namespace SsslFSharp
open System

[<AllowNullLiteral>]
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field)>]
type NotSsslFieldAttribute() = inherit Attribute()


[<AllowNullLiteral>]
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field)>]
type SsslFieldAttribute() = inherit Attribute()


[<AllowNullLiteral>]
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field)>]
type SsslNameAttribute(name: string) =
    inherit Attribute()
    member _.Name = name
