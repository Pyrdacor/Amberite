grammar Aod;

// ============================================================
// Parser Rules
// ============================================================

aodFile
    : definition* EOF
    ;

definition
    : enumDef
    | bitfieldDef
    | structDef
    ;

// enum CharacterType : ubyte { PartyMember, NPC, Monster }
enumDef
    : ENUM name=IDENT ':' underlyingType=primitiveType '{' enumBody '}'
    ;

enumBody
    : enumMember (',' enumMember)* ','?
    ;

enumMember
    : name=IDENT ('=' value=intLiteral)?
    ;

// bitfield Language : ubyte { Human, Dwarf, Elf }
bitfieldDef
    : BITFIELD name=IDENT ':' underlyingType=primitiveType '{' bitfieldBody '}'
    ;

bitfieldBody
    : bitfieldMember (',' bitfieldMember)* ','?
    ;

bitfieldMember
    : name=IDENT
    ;

// struct Character [Size = 0x0122] { ... }
// struct Monster : Character { ... }
structDef
    : STRUCT name=IDENT (':' base=IDENT)? sizeDirective? '{' structMember* '}'
    ;

// [Size = 0x0122]
sizeDirective
    : '[' SIZE '=' size=intLiteral ']'
    ;

structMember
    : offsetDirective           // [0x000B]
    | fieldFixup                // Type = CharacterType.Monster
    | fieldDecl                 // [new] TypeName[N] FieldName[?] [= default]
    ;

// [0x000B]
offsetDirective
    : '[' offset=intLiteral ']'
    ;

// Type = CharacterType.Monster  (fix a field to a constant value)
fieldFixup
    : fieldName=IDENT '=' value=qualifiedIdent
    ;

// ubyte Level
// ubyte? LookTextIndex = 0xFFFF
// CharacterValue[8] Attributes
// new AdvancedMonsterFlags MonsterFlags
fieldDecl
    : isOverride=NEW? type_=typeRef fieldName=IDENT ('=' defaultValue=aodDefaultValue)?
    ;

// The optional marker '?' sits between the base type and the array size so that
// both "uword?" and "uword?[3]" are valid (mirrors C# nullable annotation style).
typeRef
    : primitiveType QUESTION? ('[' size=intLiteral ']')?   # primitiveTypeRef
    | typeName=IDENT QUESTION? ('[' size=intLiteral ']')?  # namedTypeRef
    ;

// Default value for an optional field
aodDefaultValue
    : intLiteral      # aodIntDefault
    | qualifiedIdent  # aodRefDefault
    ;

// A.B or just A
qualifiedIdent
    : IDENT ('.' IDENT)*
    ;

primitiveType
    : UBYTE | SBYTE
    | UWORD | SWORD
    | UDWORD | SDWORD
    ;

intLiteral
    : INT
    | HEX_INT
    ;

// ============================================================
// Lexer Rules — keywords must precede IDENT
// ============================================================

ENUM     : 'enum' ;
BITFIELD : 'bitfield' ;
STRUCT   : 'struct' ;
NEW      : 'new' ;
SIZE     : 'Size' ;
UBYTE    : 'ubyte' ;
SBYTE    : 'sbyte' ;
UWORD    : 'uword' ;
SWORD    : 'sword' ;
UDWORD   : 'udword' ;
SDWORD   : 'sdword' ;

QUESTION : '?' ;

IDENT   : [a-zA-Z_][a-zA-Z0-9_]* ;
INT     : [0-9]+ ;
HEX_INT : '0' [xX] [0-9a-fA-F]+ ;

LINE_COMMENT  : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT : '/*' .*? '*/' -> skip ;
WS            : [ \t\r\n]+    -> skip ;
