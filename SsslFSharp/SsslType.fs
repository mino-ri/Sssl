module SsslFSharp.SsslType
open System
open System.Reflection

let rec getName (baseAssembly: Assembly) (ty: Type) =
    if ty.Name.StartsWith("<>f__AnonymousType") then ""
    else
        let addSuffix typeName =
            if baseAssembly = ty.Assembly || baseAssembly = typeof<byte>.Assembly
            then typeName
            else Assembly.CreateQualifiedName(ty.Assembly.GetName().Name, typeName)
        if ty.IsArray then
            let arraySuffix = "[" + String(',', ty.GetArrayRank() - 1) + "]"
            addSuffix (getName baseAssembly (ty.GetElementType()) + arraySuffix)
        elif ty.IsGenericType && not ty.IsGenericTypeDefinition then
            let typeArgs =
                ty.GetGenericArguments()
                |> Array.map (getName baseAssembly)
                |> String.concat "], ["
            addSuffix (ty.GetGenericTypeDefinition().FullName + "[[" + typeArgs + "]]")
        else addSuffix ty.FullName

let getType (expected: Type) (typeName: string) =
    if typeName = "" || expected.IsSealed || getName expected.Assembly expected = typeName then
        expected
    else
        let (|TargetType|_|) t =
            if not (isNull t) && expected.IsAssignableFrom(t) then Some(t) else None
        match Type.GetType(typeName) with
        | TargetType(coreType) -> coreType
        | _ ->
            match expected.Assembly.GetType(typeName) with
            | TargetType(innerType) -> innerType
            | _ -> expected

let (|Load|) expected typeName = getType expected typeName

let wrapTyped (expectedType: Type) valueType sssl =
    if expectedType = valueType then
        sssl
    else
        Sssl.Pair(getName expectedType.Assembly valueType, sssl)

let unwrapTyped (expectedType: Type) valueType sssl =
    match sssl with
    | Sssl.Pair(name, value) when name = getName expectedType.Assembly valueType -> value
    | _ -> sssl

let (|Unwrap|) expectedType valueType sssl = unwrapTyped expectedType valueType sssl
