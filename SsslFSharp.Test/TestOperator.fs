[<AutoOpen>]
module TestOperator

type Testing<'T> = unit -> 'T

type Validator<'T> = Testing<'T> -> unit

type ExceptionValidator = { ObserveException: (unit -> unit) -> unit }


[<AbstractClass; Sealed>]
type Testing =
    static member CreateTest(f, arg1: 'T1) : Testing<'R> =
        fun () -> f arg1

    static member CreateTest(f, (arg1: 'T1, arg2: 'T2)) : Testing<'R> =
        fun () -> f arg1 arg2

    static member CreateTest(f, (arg1: 'T1, arg2: 'T2, arg3: 'T3)) : Testing<'R> =
        fun () -> f arg1 arg2 arg3

    static member CreateTest(f, (arg1: 'T1, arg2: 'T2, arg3: 'T3, arg4: 'T4)) : Testing<'R> =
        fun () -> f arg1 arg2 arg3 arg4

    static member CreateTest(f, (arg1: 'T1, arg2: 'T2, arg3: 'T3, arg4: 'T4, arg5: 'T5)) : Testing<'R> =
        fun () -> f arg1 arg2 arg3 arg4 arg5

    static member CreateTest(f, (arg1: 'T1, arg2: 'T2, arg3: 'T3, arg4: 'T4, arg5: 'T5, arg6: 'T6))
        : Testing<'R> =
        fun () -> f arg1 arg2 arg3 arg4 arg5 arg6

    static member CreateTest(f, (arg1: 'T1, arg2: 'T2, arg3: 'T3, arg4: 'T4, arg5: 'T5, arg6: 'T6, arg7: 'T7))
        : Testing<'R> =
        fun () -> f arg1 arg2 arg3 arg4 arg5 arg6 arg7

    static member CreateTest
        (f, (arg1: 'T1, arg2: 'T2, arg3: 'T3, arg4: 'T4, arg5: 'T5, arg6: 'T6, arg7: 'T7, arg8: 'T8))
        : Testing<'R> =
        fun () -> f arg1 arg2 arg3 arg4 arg5 arg6 arg7 arg8


[<AbstractClass; Sealed>]
type Validator =
    static member CreateValidator(ex: ExceptionValidator) : Validator<'Any> =
        fun r -> ex.ObserveException (r >> ignore)

    static member CreateValidator(v1) : Validator<'T> =
        fun r -> v1 (r())

    static member CreateValidator((v1, v2)) : Validator<'T1 * 'T2> =
        fun r ->
            let r1, r2 = r()
            v1 r1; v2 r2

    static member CreateValidator((v1, v2, v3)) : Validator<'T1 * 'T2 * 'T3> =
        fun r ->
            let r1, r2, r3 = r()
            v1 r1; v2 r2; v3 r3

    static member CreateValidator((v1, v2, v3, v4)) : Validator<'T1 * 'T2 * 'T3 * 'T4> =
        fun r ->
            let r1, r2, r3, r4 = r()
            v1 r1; v2 r2; v3 r3; v4 r4

    static member CreateValidator((v1, v2, v3, v4, v5)) : Validator<'T1 * 'T2 * 'T3 * 'T4 * 'T5> =
        fun r ->
            let r1, r2, r3, r4, r5 = r()
            v1 r1; v2 r2; v3 r3; v4 r4
            v5 r5

    static member CreateValidator((v1, v2, v3, v4, v5, v6)) : Validator<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6> =
        fun r ->
            let r1, r2, r3, r4, r5, r6 = r()
            v1 r1; v2 r2; v3 r3; v4 r4; v5 r5; v6 r6

    static member CreateValidator((v1, v2, v3, v4, v5, v6, v7))
        : Validator<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7> =
        fun r ->
            let r1, r2, r3, r4, r5, r6, r7 = r()
            v1 r1; v2 r2; v3 r3; v4 r4; v5 r5; v6 r6; v7 r7

    static member CreateValidator((v1, v2, v3, v4, v5, v6, v7, v8))
        : Validator<'T1 * 'T2 * 'T3 * 'T4 * 'T5 * 'T6 * 'T7 * 'T8> =
        fun r ->
            let r1, r2, r3, r4, r5, r6, r7, r8 = r()
            v1 r1; v2 r2; v3 r3; v4 r4; v5 r5; v6 r6; v7 r7; v8 r8


let inline private createValidator (_: ^C) (source: ^Source) =
    ((^C or ^Source) : (static member CreateValidator : ^Source -> Validator< ^Result >) source)

let inline createTest (_: ^C) (f: ^Func) (args: ^Args) =
    ((^C or ^Func) : (static member CreateTest : ^Func * ^Args -> Testing< ^R >) f, args)

let inline test (testFunc: ^``('Args -> 'Result)``) (args: ^Args) : Testing< ^Result > =
    createTest (Unchecked.defaultof<Testing>) testFunc args

let inline ( ==> ) (testing: Testing< ^T >) (assertion: ^``('T -> unit)``) =
    createValidator (Unchecked.defaultof<Validator>) assertion testing
