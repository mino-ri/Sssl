module SsslFSharp.SsslType
open System
open System.Reflection

let rec private getNameCore (baseAssembly: Assembly) (ty: Type) =
    if ty.Name.StartsWith("<>f__AnonymousType") then ""
    else
        let addSuffix typeName =
            if baseAssembly = ty.Assembly || ty.Assembly = typeof<byte>.Assembly
            then typeName
            else Assembly.CreateQualifiedName(ty.Assembly.GetName().Name, typeName)
        if ty.IsArray then
            let arraySuffix = "[" + String(',', ty.GetArrayRank() - 1) + "]"
            addSuffix (getNameCore baseAssembly (ty.GetElementType()) + arraySuffix)
        elif ty.IsGenericType && not ty.IsGenericTypeDefinition then
            let typeArgs =
                ty.GetGenericArguments()
                |> Array.map (getNameCore baseAssembly)
                |> String.concat "], ["
            addSuffix (ty.GetGenericTypeDefinition().FullName + "[[" + typeArgs + "]]")
        else addSuffix ty.FullName

let getName (expected: Type) (ty: Type) =
    let bindingFlags = BindingFlags.Public ||| BindingFlags.NonPublic
    if expected = ty then ""
    elif (if expected.IsGenericType
          then expected.GetNestedTypes(bindingFlags) |> Array.exists ty.IsGenericOf
          else expected.GetNestedTypes(bindingFlags) |> Array.contains ty) then ty.Name
    else getNameCore expected.Assembly ty

let getType (expected: Type) (typeName: string) =
    if typeName = "" || expected.IsSealed || getNameCore expected.Assembly expected = typeName then
        expected
    else
        seq {
            let nested = expected.GetNestedType(typeName, BindingFlags.Public ||| BindingFlags.NonPublic)
            if isNotNull nested then
                if not expected.IsGenericType then
                    nested
                elif expected.TypeArgs.Length = nested.TypeArgs.Length then
                    nested.MakeGenericType(expected.TypeArgs)
            Type.GetType(typeName)
            expected.Assembly.GetType(typeName)
            expected
        }
        |> Seq.find (fun t -> isNotNull t && expected.IsAssignableFrom(t))

let (|Load|) expected typeName = getType expected typeName

let wrapTyped (expectedType: Type) valueType sssl =
    if expectedType = valueType then
        sssl
    else
        Sssl.Pair(getName expectedType valueType, sssl)

let unwrapTyped (expectedType: Type) valueType sssl =
    match sssl with
    | Sssl.Pair(name, value) when name = getName expectedType valueType -> value
    | _ -> sssl

let (|Unwrap|) expectedType valueType sssl = unwrapTyped expectedType valueType sssl
