# Amber

A statically-typed scripting language for the Amberite game framework, targeting Motorola 68000 conventions.

## Language overview

### Primitive types

| Type   | Size    | Description              |
|--------|---------|--------------------------|
| `byte` | 8 bit   | Unsigned integer         |
| `word` | 16 bit  | Unsigned integer         |
| `long` | 32 bit  | Unsigned integer         |
| `bool` | —       | Boolean (`true`/`false`) |

Numeric types promote automatically in mixed expressions (byte < word < long).

### Enums

```amber
enum Color : byte {
    Red,
    Green,
    Blue,
    White = 15
}
```

Base type is `byte`, `word`, or `long`. Members auto-increment from the previous value; explicit values may use decimal or hex literals.

### Bitfields

```amber
bitfield ItemSlotFlags : byte {
    Identified,   // 0x01
    Broken,       // 0x02
    Cursed        // 0x04
}
```

Members auto-assign successive powers of two. An explicit value resumes auto-increment from the next power of two above it.

### Structs

```amber
struct ItemSlot {
    byte          amount;
    byte          charges;
    ItemSlotFlags flags;
}

struct Palette {
    byte         count;
    Ptr<Color>   entries[16];   // array field
    Ptr<Palette> next;          // recursive pointer
}
```

A struct may `import` another struct to flatten its fields inline:

```amber
struct ExtendedSlot {
    import ItemSlot;
    word bonus;
}
```

Pointer types use `Ptr<T>` syntax. Array size is written after the field name: `Type name[size]`.

### Functions

Function parameters map to Motorola 68000 registers. Data registers `d0`–`d7` hold scalar values; address registers `a0`–`a6` hold pointers and structs.

```amber
function add(in d0: byte x, in d1: byte y, out d0: word result) {
    word r = x + y;
}

function move(in a0: Ptr<Vec2> src, inout a1: Ptr<Vec2> dst) {
}
```

Parameter directions:

| Direction | Meaning |
|-----------|---------|
| `in`      | Read-only input  |
| `out`     | Write-only output |
| `inout`   | Read and write |

### Variables and assignments

```amber
byte  lo   = 0xFF;
word  hi   = 0x1234;
long  base = lo + hi * 2;
bool  flag = true;

lo = lo & 0x0F;
hi = hi | 0x8000;
```

### Expressions

| Category   | Operators                   |
|------------|-----------------------------|
| Arithmetic | `+`, `-`, `*`, `/`, `%`     |
| Bitwise    | `&`, `\|`, `^`, `~`         |
| Shift      | `<<`, `>>`                  |
| Logical    | `!`                         |
| Part access | `.b0`, `.b1`, `.b2`, `.b3`, `.w0`, `.w1` |

**Part access** extracts sub-words and sub-bytes from wider types:

```amber
long  val  = 0x12345678;
word  low  = val.w0;    // 0x5678
word  high = val.w1;    // 0x1234
byte  b0   = val.b0;    // 0x78
byte  b3   = val.b3;    // 0x12
```

## Project layout

```
Amber/
  src/Amber.Interpreter/
    Grammar/      Amber.g4            ANTLR4 grammar
    AST/          Nodes.cs, Enums.cs, TypeNodes.cs
    Parsing/      AstBuilder.cs       ANTLR visitor → AST
    Runtime/      Interpreter.cs      expression evaluator & type registry
                  AmberValue.cs       runtime value type
                  AmberEnvironment.cs variable scope
                  TypeRegistry.cs     type/function registry
  tests/Amber.Tests/
    ArithmeticTests.cs, BitOpTests.cs, TypeSystemTests.cs,
    FunctionTests.cs, PartAccessTests.cs
  test.amb           basic types, arithmetic, and bit ops
  test_types.amb     structs, enums, bitfields, pointer fields
  test_functions.amb function declarations with register parameters
  test_parts.amb     sub-word and sub-byte part access
```

## Requirements

- .NET 10 SDK
- ANTLR4 (pulled automatically via NuGet)
