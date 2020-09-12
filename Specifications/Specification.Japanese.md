# SSSL

## 1. 導入

SSSL (Simple Structure Serialization Language, この頭字語は後付けである) は、言語に依存しないデータ記述言語である。

### 1.1. デザインコンセプト

* JSON (JavaScript Object Notation) のスーパーセットとなること (すべての JSON を SSSL として解釈できること)
* XAML (Extensible Application Markup Language) と同等の表現力を持つこと (すべての XAML を同等の意味を持つ SSSL に変換できること)

### 1.2. 値と型

SSSL は4種類のプリミティブ型 (文字列、数値、論理値、null) および2種類の構築型 (ペア、オブジェクト) を表現できる。JSON における配列は、SSSL においてはオブジェクトに統合されている。

文字列 (string) は、0個以上の Unicode 文字である。JSON における定義と一致する。
JSON と同じく、これは Unicode の最新バージョンを参照する。そのため、Unicode の仕様変更は、SSSL の構文に影響する。

ペアは、名前と値のペアである。ここで名前は文字列であり、値は単一の SSSL 値(文字列、数値、論理値、null、ペアまたはオブジェクト)である。
JSON におけるオブジェクト内の名前/値のペアを包含し、XAML におけるプロパティ属性、プロパティ要素を表現する。

オブジェクトは、省略可能な名前を持つ順序付けられた0個以上の SSSL 値のコレクションである。それを囲う括弧によって3種類 (`()`, `{}`, `[]`) が存在する。
JSON におけるオブジェクトと配列の両方を包含し、XAML におけるオブジェクト要素、マークアップ拡張を表現する。

## 2. 構文

SSSL テキストは連続するトークンで構成される。トークンは8種類の構造化文字 (`(`, `)`, `{`, `}`, `[`, `]`, `:`, `,`) 、数値、文字列または識別子である。あらゆるトークンの間には4種類の空白文字(`%x20`, `%x09`, `%x0A`, `%x0D`)を入れることができる。

SSSL テキストはシリアライズされた単一の値であり、必ずしもオブジェクトである必要はない。例えば単一の `null` は有効な SSSL テキストである。

### 2.1. 記法

本書面において、構文を表すのには ABNF (Augmented Backus–Naur form) を使用する。
ただし、明記しない限り大文字と小文字を区別する。
また、 `%p{##}` という記法で Unicode カテゴリー `##` に属する全ての文字を表す。

### 2.2. 概要

#### 2.2.1. 厳格構文

シリアライザーは、SSSLを次の規則に一致するように生成しなければならない。

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

#### 2.2.2. 緩和構文

他言語との相互運用や人間が SSSL を記述することを想定する場合、パーサーは SSSL を次の規則のように解釈してもよい。

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

## 3. SSSL の値

### 3.1. リテラル

SSSL の値はオブジェクト、ペア、文字列、数値または**識別子リテラル** ( `true`, `false`, `inf`, `ninf`, `nan`, `null` のいずれか) でなければならない。
識別子リテラルは小文字でなければならず、またこれら6つ以外の識別子リテラルは許可されない。

```abnf
value     = pair / object / STRING / NUMBER / ture / false / inf / ninf / nan / null
true      = %x74.72.75.65      ; true
false     = %x66.61.6c.73.65   ; false
inf       = %x69.6e.66         ; inf
ninf      = %x6e.69.6e.76      ; ninf
nan       = %x6e.61.6e         ; nan
null      = %x6e.75.6c.6c      ; null
```

### 3.2. ペア

**ペア**は、名前と値のペアである。名前と値をコロンで区切って表現する。
ここで名前は文字列であり、値は単一の SSSL 値(文字列、数値、論理値、null、ペアまたはオブジェクト)である。
厳格構文においては名前は常に二重引用符 `"` で囲う必要があるが、緩和構文においては特定の名前において二重引用符を省略することができる。

厳格構文:

```abnf
pair      = STRING ":" value
```

緩和構文:

```abnf
pair      = id ":" value
id        = ID / STRING

ID        = IDHEAD *IDCHAR
IDHEAD    = "$" / "_" / %p{L} / %p{Nl}
IDCHAR    = IDHEAD / "." / %p{Mn} / %p{Mc} / %p{Nd} / %p{Pc}
```

ペアは、主にオブジェクトのメンバーや辞書構造のキー/値ペアを表現するのに使用される。
JSON におけるオブジェクト内の名前/値のペアを包含し、XAML におけるプロパティ属性、プロパティ要素を表現する。

SSSL においてペアはそれ単独で成立する値であり、必ずしもオブジェクトの内部に現れる必要はない。例えば、次は有効な SSSL である:

```
"key": 12.0
```

```
"key1": "key2": true
```

### 3.3. オブジェクト

**オブジェクト**は、省略可能な名前を持つ順序付けられた0個以上の SSSL 値のコレクションである。それを囲う括弧によって3種類 (`()`, `{}`, `[]`) が存在する。

厳格構文:

```abnf
object    = [ STRING ] "{" [ content ] "}"
          / [ STRING ] "[" [ content ] "]"
          / [ STRING ] "(" [ content ] ")"
```

緩和構文:

```abnf
object    = [ id ] "{" [ content ] "}"
          / [ id ] "[" [ content ] "]"
          / [ id ] "(" [ content ] ")"
```

オブジェクトが持つ括弧の種類は、アプリケーションに報告されなければならない。
SSSL の構文としては括弧の種類による違いはないが、アプリケーションは括弧の種類によってオブジェクトを区別してもよい。

以下に、括弧の種類によるオブジェクトの区別のガイドラインを示す。混乱を避けるためにもアプリケーションは可能な限り従うことが好ましい。

* `()` : タプルの表現が期待される。名前を持たず、内容として順序付けられた異なる型の並びを含む。
* `{}` : オブジェクトの表現が期待される。名前を持ち、内容として事前に定義された順序付けられていないペアの並び ("メンバー" や "プロパティ" などと呼ばれることが多い) を含む。
* `[]` : コレクションや辞書の表現が期待される。コレクションの種類 (リストや配列など) を指定する必要がある場合に名前を持ち、内容として順序付けられた同じ型の並びか、事前に定義されていない順序付けられていない同じ型のペアを含む。

### 3.4. 数値

**数値**の表現は、プログラミング言語で使用されているものに似ている。 JSON における定義と一致する。
数値は10進数として表現される。省略可能な負の符号と整数部を持ち、小数部および/または指数部が後続することがある。先行する`0`は許可されない。
識別子リテラルの `inf` , `ninf` , `nan` は文法上は数値ではないが、アプリケーションはこれらをそれぞれ正の無限大、負の無限大、非数として扱うことが好ましい。

```abnf
NUMBER    = INTEGER [ FRACTION ] [ EXPONENT ]

INTEGER   = [ "-" ] ( "0" / DIGIT1 *DIGIT )
FRACTION  = "." 1*DIGIT
EXPONENT  = ( "E" / "e" ) [ "+" / "-" ] 1*DIGIT
DIGIT     = %x30-39                         ; 0-9
DIGIT1    = %x31-39                         ; 1-9
```

数値の精度は SSSL においては定めない。

### 3.5. 文字列

**文字列**の表現は、C言語系のプログラミング言語で使用されているものに似ている。
文字列は二重引用符 `"` で始まり、二重引用符で終わる。
二重引用符の間には、エスケープしなければならない文字 (二重引用符 `"`, バックスラッシュ `\`, 制御文字 `U+0000 - U+001F`) 以外の全ての文字を置くことができる。

`\u` に続けて4桁の16進数で文字コードポイントを記述することで、任意の文字をエスケープできる。
また、よく使用される文字は2文字のエスケープシーケンスがある。例えば、バックスラッシュ `\` 自身は、エスケープシーケンス `\\` として表現することができる。

```abnf
STRING    = %x22 *CHARACTER %x22

CHARACTER = %x20-21 / %x23-5B / %x5D-10FFFF ; any graphic character without " and \
          / "\" ( %x22 / "\" / "/" / "b" / "f" / "n" / "r" / "t" / "u" 4HEXDIG )
DIGIT     = %x30-39                         ; 0-9
HEXDIG    = DIGIT / %x61-66 / %x41-46       ; 0-9 / a-f / A-F
```

この定義は JSON における定義と一致する。
JSON と同じく、これは Unicode の最新バージョンを参照する。そのため、Unicode の仕様変更は、SSSL の構文に影響する。
