# Ambdev

A domain-specific language and interpreter for declaring Ambermoon map event data.

Ambermoon stores map events as packed binary records with typed fields. Ambdev lets you describe those events at a high level — naming fields, constraining values, composing events into conditional chains — and validates the declarations before they are used.

## Language overview

### Constants

```
const MAX_ACTORS  = 16
const HERO_ID     = MAX_ACTORS / 2   // arithmetic over other constants
const KEY_CAT     = 0x40             // hex literals supported
```

Names must be `ALL_CAPS` (uppercase letters, digits, underscores; no trailing underscore).  
Values may use `+`, `-`, `*`, `/`, parentheses, `true`/`false`, and references to earlier constants.

### Enums

```
enum Direction : byte {
    North,          // auto-increment from 0
    South,
    East,
    West
}

enum ItemCategory : byte {
    None   = 0,
    Weapon = 10,
    Potion = 30,
    Key    = 0x40
}
```

Base type is `byte`, `word`, or `long`. Members auto-increment from the previous value; explicit values may use decimal or hex literals, or constant references.

### Bitfields

```
bitfield ActorFlags : byte {
    Visible,    // 0x01 — auto powers-of-two
    Active,     // 0x02
    Hostile     // 0x04
}

bitfield AccessFlags : byte {
    Read      = 0x01,
    Write,              // 0x02
    ReadWrite = Read | Write,   // member OR expression
    Admin     = 0x40
}
```

A `None = 0` entry is auto-generated unless a member already has value `0` or is explicitly named `None`.

### Event types

An event type (`etype`) is a named template for binary event records. Each field has a byte offset, a type, an optional flag (`?`), an optional default, and an optional range constraint.

```
etype[1, Move]:
    - 0: byte       actorId
    - 1: Direction  dir
    - 2: byte?      steps = 1 [1..8]   // optional; default 1; range 1–8
```

Field types: `byte`, `word`, `long`, or any named enum/bitfield.

Range constraints:
- Continuous: `[0..4095]`
- Value list: `[0, 1, 2, 3]`

### Event specializations

An event specialization (`espec`) refines an etype with `when` conditions and optional field overrides.

```
espec[1, MoveNorth]:
    - when dir == 0
    - 2: byte? steps = 1 [1..4]    // tighter range for this specialization

espec[1, Dash]:
    - when steps >= 5 and dir != 0

espec[3, StoryDialog]:
    - when textId >= 100 or flags != 0
```

Condition operators: `==`, `!=`, `<`, `>`, `<=`, `>=`, `and`, `or`.

### Events

A concrete event record binds an etype to field values.

```
event[8, HeroSlaysOrc] = etype[4]
    - attacker:  1
    - target:    12
    - damage:    150
    - inflicted: StatusEffects.Dead | StatusEffects.Poisoned
```

Field values may be integer/hex literals, named enum or bitfield members (`Type.Member`), constant references, or bitwise OR combinations of the above.

### Chains

A chain is a sequence of event or chain steps, each prefixed with a flow-control symbol.

```
chain[1, QuestIntro]
    - event KingGivesQuest   // always execute
    ? chain QuestAccepted    // if previous succeeded
    ! chain QuestRefused     // if previous failed
```

| Prefix | Meaning |
|--------|---------|
| `-` | Always execute |
| `?` | Execute only if previous step succeeded |
| `!` | Execute only if previous step failed |

Step targets may be referenced by numeric index or by name. Chain name references support forward references — a chain may name a chain declared later in the file.

## Validation

The interpreter checks:

- Constant values fit the target field type (`byte` 0–255, `word` 0–65535, `long` 0–4294967295)
- Enum and bitfield member values fit the declared base type
- Required etype fields are present in every event
- Espec conditions reference fields that exist in the parent etype
- Unknown constant, event, or chain names in any expression
- Duplicate declarations (constants, enums, etypes, especs, events, chains)
- Division by zero in constant expressions

## Project layout

```
Ambdev/
  src/Ambdev.Interpreter/
    Grammar/       Ambdev.g4          ANTLR4 grammar
    AST/           Nodes.cs, Enums.cs AST record types
    Parsing/       AstBuilder.cs      ANTLR visitor → AST
    Runtime/       Interpreter.cs     semantic analysis & registry
                   Registry.cs        runtime info types
  tests/Ambdev.Tests/
    TypeTests.cs, ConstTests.cs, EtypeTests.cs,
    EspecTests.cs, EventTests.cs, ChainTests.cs
  sample.ambdev    full-featured example covering all constructs
  EventData.md     Ambermoon binary event format reference
```

## Requirements

- .NET 10 SDK
- ANTLR4 (pulled automatically via NuGet)
