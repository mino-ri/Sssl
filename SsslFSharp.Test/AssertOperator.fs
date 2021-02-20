[<AutoOpen>]
module AssertOperator
open System
open System.Collections.Generic
open Xunit

let ( ^^ ) assertion1 (assertion2: 'T -> unit) =
    fun arg ->
        assertion1 arg
        assertion2 arg

let any (_: 'T) = ()

let equal (expected: 'T) (actual: 'T) = Assert.Equal<'T>(expected, actual)

let equalStr (expected: string) (actual: string) = Assert.Equal(expected, actual)

let equalSeq (expected: seq<'T>) (actual: seq<'T>) = Assert.Equal<'T>(expected, actual)

let notEqual (expected: 'T) (actual: 'T) = Assert.NotEqual<'T>(expected, actual)

let notEqualSeq (expected: seq<'T>) (actual: seq<'T>) = Assert.NotEqual<'T>(expected, actual)

let startsWith (expectedStart: string) (actual: string) = Assert.StartsWith(expectedStart, actual)

let endsWith (expectedEnd: string) (actual: string) = Assert.EndsWith(expectedEnd, actual)

let same (expected: 'T) (actual: 'T) = Assert.Same(box expected, box actual)

let notSame (expected: 'T) (actual: 'T) = Assert.NotNull(box expected, box actual)

let null' (value: 'T) = Assert.Null(box value)

let notNull (value: 'T) = Assert.NotNull(box value)

let empty (value: 'T) = Assert.Empty(value)

let notEmpty (value: 'T) = Assert.NotEmpty(value)

let inRange (low: 'T) high actual = Assert.InRange(actual, low, high)

let notInRange (low: 'T) high actual = Assert.NotInRange(actual, low, high)

let contains (expected: 'T) (collection: seq<'T>) = Assert.Contains(expected, collection)

let containsStr (expected: string) (collection: string) = Assert.Contains(expected, collection)

let doesNotContain (expected: 'T) (collection: seq<'T>) = Assert.DoesNotContain(expected, collection)

let doesNotContainStr (expected: string) (collection: string) = Assert.DoesNotContain(expected, collection)

let containsKey (expected: 'T) (collection: IDictionary<'T, 'V>) = Assert.Contains(expected, collection)

let doesNotContainKey (expected: 'T) (collection: IDictionary<'T, 'V>) = Assert.DoesNotContain(expected, collection)

let isType<'T> (value: 'T) = Assert.IsType<'T>(value)

let isTypeOf (expectedType: Type) (value: 'T) = Assert.IsType(expectedType, value)

let isNotType<'T> (value: 'T) = Assert.IsNotType<'T>(value)

let isNotType' (expectedType: Type) (value: 'T) = Assert.IsNotType(expectedType, value)

let true' (condition: bool) = Assert.True(condition)

let false' (condition: bool) = Assert.False(condition)

let satisfies condition (actual: 'T) = Assert.True(condition actual)

let doesNotSatisfy condition (actual: 'T) = Assert.False(condition actual)

let isAssignableFrom<'T> (value) = Assert.IsAssignableFrom<'T>(value)

let isAssignableFrom' expectedType (value) = Assert.IsAssignableFrom(expectedType, value)

let some (actual: 'T option) =
    Assert.True(actual.IsSome, $"Shoud be Some. \r\nActual: {actual}")

let none (actual: 'T option) = equal None actual

let ok (actual: Result<'Ok, 'Error>) =
    let isOk = match actual with | Ok _ -> true | _ -> false
    Assert.True(isOk, $"shoud be Ok. actual: {actual}")

let error (actual: Result<'Ok, 'Error>) =
    let isError = match actual with | Error _ -> true | _ -> false
    Assert.True(isError, $"shoud be Error. actual: {actual}")

[<RequiresExplicitTypeArguments>]
let throws<'Exception when 'Exception :> exn> =
    { ObserveException = fun f -> ignore <| Assert.Throws<'Exception>(Action(f)) }

let throws' (exnType: Type) =
    { ObserveException = fun f -> ignore <| Assert.Throws(exnType, Action(f)) }

let throwsAny<'Exception when 'Exception :> exn> =
    { ObserveException = fun f -> ignore <| Assert.ThrowsAny<'Exception>(Action(f)) }
