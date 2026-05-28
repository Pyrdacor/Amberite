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

// struct Character { ... }
// struct Monster : Character { ... }
structDef
    : STRUCT name=IDENT (':' base=IDENT)? '{' structMember* '}'
    ;

structMember
    : offsetDirective           // [0x000B]
    | fieldFixup                // Type = CharacterType.Monster
    | fieldDecl                 // [new] TypeName[N] FieldName
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
// CharacterValue[8] Attributes
// new AdvancedMonsterFlags MonsterFlags
fieldDecl
    : isOverride=NEW? type_=typeRef fieldName=IDENT
    ;

typeRef
    : primitiveType ('[' size=intLiteral ']')?   # primitiveTypeRef
    | typeName=IDENT ('[' size=intLiteral ']')?  # namedTypeRef
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
UBYTE    : 'ubyte' ;
SBYTE    : 'sbyte' ;
UWORD    : 'uword' ;
SWORD    : 'sword' ;
UDWORD   : 'udword' ;
SDWORD   : 'sdword' ;

IDENT   : [a-zA-Z_][a-zA-Z0-9_]* ;
INT     : [0-9]+ ;
HEX_INT : '0' [xX] [0-9a-fA-F]+ ;

LINE_COMMENT  : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT : '/*' .*? '*/' -> skip ;
WS            : [ \t\r\n]+    -> skip ;
