module internal SsslFSharp.BoolOption

let toOption (ok, value) = if ok then Some(value) else None

let (|BoolSome|BoolNone|) (ok, value) = if ok then BoolSome(value) else BoolNone
