grammar Aon;

// ============================================================
// Parser Rules
// ============================================================

// A .aon file contains zero or more typed object instances.
aonFile
    : instance* EOF
    ;

// [Character] Thalion = { ... }
instance
    : '[' typeName=IDENT ']' name=IDENT '=' '{' field* '}'
    ;

// FieldName = value
field
    : fieldName=IDENT '=' value
    ;

// Values — | is the lowest-precedence operator (bitfield flag combination)
value
    : left=value '|' right=value   # flagsValue
    | intLiteral                   # intValue
    | qualifiedIdent               # refValue
    | STRING                       # stringValue
    | '{' field* '}'               # objectValue
    | '[' arrayItems? ']'          # arrayValue
    ;

arrayItems
    : value (',' value)* ','?
    ;

// A.B or just A
qualifiedIdent
    : IDENT ('.' IDENT)*
    ;

intLiteral
    : INT
    | HEX_INT
    | NEG_INT
    ;

// ============================================================
// Lexer Rules
// ============================================================

IDENT   : [a-zA-Z_][a-zA-Z0-9_]* ;
INT     : [0-9]+ ;
HEX_INT : '0' [xX] [0-9a-fA-F]+ ;
NEG_INT : '-' [0-9]+ ;
STRING  : '"' (~["\r\n\\] | '\\' .)* '"' ;

LINE_COMMENT  : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT : '/*' .*? '*/' -> skip ;
WS            : [ \t\r\n]+    -> skip ;
