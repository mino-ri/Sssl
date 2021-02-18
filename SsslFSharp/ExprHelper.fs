module internal SsslFSharp.ExprHelper
open System
open System.Linq.Expressions

let convertOf (t: Type) expr = Expression.Convert(expr, t) :> Expression

let convert<'T> expr = Expression.Convert(expr, typeof<'T>) :> Expression

let compile<'T> args body = Expression.Lambda<'T>(body, args).Compile()

let parameterOf (t: Type) name = Expression.Parameter(t, name)

let parameter<'T> name = Expression.Parameter(typeof<'T>, name)

let assignFor right left = Expression.Assign(left, right) :> Expression

let assignTo left right = Expression.Assign(left, right) :> Expression
