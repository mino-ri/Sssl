namespace SsslFSharp
open System


type SsslFormat =
    {
        IndentChar: char
        NewLine: string
        IndentInterval: int
        InitialIndent: int
        Spacing: bool
    }


[<Flags>]
type ObjectConversionOptions =
    | None = 0
    | AllowMissingMember = 1
    | AllowUnknownMember = 2


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SsslFormat =
    let Minified = {
        IndentChar = ' '
        NewLine = ""
        IndentInterval = 0
        InitialIndent = 0
        Spacing = false
    }

    let Inline = {
        IndentChar = ' '
        NewLine = " "
        IndentInterval = 0
        InitialIndent = 0
        Spacing = true
    }

    let Default = {
        IndentChar = ' '
        NewLine = Environment.NewLine
        IndentInterval = 2
        InitialIndent = 0
        Spacing = true
    }
    