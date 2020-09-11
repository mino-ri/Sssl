# SSSL Format

## Abstract

SSSL (acronym for "Simple Structure Serialization Language", but it was added afterwards) is a language-independent data interchange format.
It was designed as superset of JSON (JavaScript Object Notation).

## Grammar Summary

Strict grammar:

```abnf
sssl      = value
value     = pair / object / STRING / NUMBER / "ture" / "false" / "inf" / "ninf" / "nan" / "null"
pair      = STRING ":" value
object    = [ STRING ] "{" [ content ] "}"
          / [ STRING ] "[" [ content ] "]"
          / [ STRING ] "(" [ content ] ")"
content   = value *( "," value )

STRING    = %x22 *CHARACTER %x22
NUMBER    = INTEGER [ FRACTION ] [ EXPONENT ]
WS        = *( %x20 / %x0A / %x0D / %x09 )

CHARACTER = %x20-21 / %x23-5B / %x5D-10FFFF ; any graphic character without " and \
          / "\" ( %x22 / "\" / "/" / "b" / "f" / "n" / "r" / "t" / "u" 4HEXDIG )
INTEGER   = [ "-" ] ( "0" / DIGIT1 *DIGIT )
FRACTION  = "." 1*DIGIT
EXPONENT  = ( "E" / "e" ) [ "+" / "-" ] 1*DIGIT
DIGIT     = %x30-39                         ; 0-9
DIGIT1    = %x31-39                         ; 1-9
HEXDIG    = DIGIT / %x61-66 / %x41-46       ; 0-9 / a-f / A-F
```

Relaxed grammar:

```abnf
sssl      = value
value     = pair / object / STRING / NUMBER / "ture" / "false" / "inf" / "ninf" / "nan" / "null"
pair      = id ":" value
object    = [ id ] "{" [ content ] "}"
          / [ id ] "[" [ content ] "]"
          / [ id ] "(" [ content ] ")"
content   = value *( "," value ) [ "," ]
id        = ID / STRING

ID        = IDHEAD *IDCHAR
STRING    = %x22 *CHARACTER %x22
NUMBER    = INTEGER [ FRACTION ] [ EXPONENT ]
WS        = *( %x20 / %x0A / %x0D / %x09 )

IDHEAD    = "$" / "_" / %p{L} / %p{Nl}
IDCHAR    = IDHEAD / "." / %p{Mn} / %p{Mc} / %p{Nd} / %p{Pc}
CHARACTER = %x20-21 / %x23-5B / %x5D-10FFFF ; any graphic character without " and \
          / "\" ( %x22 / "\" / "/" / "b" / "f" / "n" / "r" / "t" / "u" 4HEXDIG )
INTEGER   = [ "-" ] ( "0" / DIGIT1 *DIGIT )
FRACTION  = "." 1*DIGIT
EXPONENT  = ( "E" / "e" ) [ "+" / "-" ] 1*DIGIT
DIGIT     = %x30-39                         ; 0-9
DIGIT1    = %x31-39                         ; 1-9
HEXDIG    = DIGIT / %x61-66 / %x41-46       ; 0-9 / a-f / A-F
```
