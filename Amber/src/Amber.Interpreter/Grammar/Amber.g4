grammar Amber;

// ===== Parser Rules =====

program
    : topLevel* EOF
    ;

topLevel
    : typeDeclaration
    | statement
    ;

// ── Type / function declarations ──────────────────────────────────────────────

typeDeclaration
    : enumDecl
    | bitfieldDecl
    | structDecl
    | functionDecl
    ;

// ── Enum ──────────────────────────────────────────────────────────────────────

enumDecl
    : ENUM IDENTIFIER COLON baseType LBRACE enumMembers RBRACE
    ;

enumMembers
    : enumMember (COMMA enumMember)* COMMA?
    ;

enumMember
    : IDENTIFIER (ASSIGN (INTEGER_LITERAL | HEX_LITERAL))?
    ;

// ── Bitfield ──────────────────────────────────────────────────────────────────

bitfieldDecl
    : BITFIELD IDENTIFIER COLON baseType LBRACE bitfieldMembers RBRACE
    ;

bitfieldMembers
    : bitfieldMember (COMMA bitfieldMember)* COMMA?
    ;

bitfieldMember
    : IDENTIFIER (ASSIGN (INTEGER_LITERAL | HEX_LITERAL))?
    ;

// ── Struct ────────────────────────────────────────────────────────────────────

structDecl
    : STRUCT IDENTIFIER LBRACE structItem* RBRACE
    ;

structItem
    : structField
    | structImport
    ;

// Array size goes after the name: byte items[16]
structField
    : typeRef IDENTIFIER (LBRACKET INTEGER_LITERAL RBRACKET)? SEMI
    ;

structImport
    : IMPORT IDENTIFIER SEMI
    ;

// ── Function ──────────────────────────────────────────────────────────────────

functionDecl
    : FUNCTION IDENTIFIER LPAREN paramList? RPAREN LBRACE statement* RBRACE
    ;

paramList
    : param (COMMA param)*
    ;

// "in d0: byte x"  →  direction register COLON type name
param
    : paramDir IDENTIFIER COLON typeRef IDENTIFIER
    ;

paramDir
    : IN
    | OUT
    | INOUT
    ;

// ── Unified type reference ────────────────────────────────────────────────────
// Used for struct fields, function parameters, and (future) variable declarations.
// Right-recursive so STAR chains: **byte = ptr-to-ptr-to-byte.

typeRef
    : PTR LT typeRef GT    # ptrTypeRef       // Ptr<T>  — only legal pointer syntax
    | BYTE                  # byteTypeRef
    | WORD                  # wordTypeRef
    | LONG                  # longTypeRef
    | BOOL                  # boolTypeRef
    | IDENTIFIER            # namedTypeRef
    ;

// Enum / bitfield base type — bool intentionally excluded
baseType
    : BYTE
    | WORD
    | LONG
    ;

// ── Statements ────────────────────────────────────────────────────────────────

statement
    : varDeclaration
    | assignment
    ;

varDeclaration
    : typeSpec IDENTIFIER (ASSIGN expression)? SEMI
    ;

assignment
    : IDENTIFIER ASSIGN expression SEMI
    ;

typeSpec
    : BYTE
    | WORD
    | LONG
    | BOOL
    ;

// ── Expressions (highest precedence first) ────────────────────────────────────

expression
    : LPAREN expression RPAREN                                          # parenExpr
    | expression DOT IDENTIFIER                                         # partExpr
    | op=(MINUS | TILDE | BANG) expression                             # unaryExpr
    | left=expression op=(STAR | SLASH | PERCENT) right=expression     # mulExpr
    | left=expression op=(PLUS | MINUS) right=expression               # addExpr
    | left=expression op=(LSHIFT | RSHIFT) right=expression            # shiftExpr
    | left=expression AMPERSAND right=expression                       # bandExpr
    | left=expression CARET right=expression                           # bxorExpr
    | left=expression PIPE right=expression                            # borExpr
    | IDENTIFIER                                                        # identExpr
    | INTEGER_LITERAL                                                   # intLitExpr
    | HEX_LITERAL                                                       # hexLitExpr
    | BOOL_LITERAL                                                      # boolLitExpr
    ;

// ===== Lexer Rules =====

// Keywords — longer matches must come first (INOUT before IN/OUT)
BYTE     : 'byte'     ;
WORD     : 'word'     ;
LONG     : 'long'     ;
BOOL     : 'bool'     ;
ENUM     : 'enum'     ;
BITFIELD : 'bitfield' ;
STRUCT   : 'struct'   ;
FUNCTION : 'function' ;
IMPORT   : 'import'   ;
INOUT    : 'inout'    ;
IN       : 'in'       ;
OUT      : 'out'      ;
PTR      : 'Ptr'      ;

BOOL_LITERAL : 'true' | 'false' ;

// Operators
ASSIGN    : '='  ;
PLUS      : '+'  ;
MINUS     : '-'  ;
STAR      : '*'  ;
SLASH     : '/'  ;
PERCENT   : '%'  ;
AMPERSAND : '&'  ;
PIPE      : '|'  ;
CARET     : '^'  ;
TILDE     : '~'  ;
BANG      : '!'  ;
LSHIFT    : '<<' ;
RSHIFT    : '>>' ;

// Comparison (longer first so << / >> beat < / >)
LT : '<' ;
GT : '>' ;

// Punctuation
LPAREN   : '(' ;
RPAREN   : ')' ;
LBRACE   : '{' ;
RBRACE   : '}' ;
LBRACKET : '[' ;
RBRACKET : ']' ;
SEMI     : ';' ;
COLON    : ':' ;
COMMA    : ',' ;
DOT      : '.' ;

// Literals — HEX before INTEGER so "0x1F" doesn't tokenize as "0" + "x1F"
HEX_LITERAL     : '0' [xX] [0-9a-fA-F]+ ;
INTEGER_LITERAL : [0-9]+                 ;

// Identifiers — after all keywords
IDENTIFIER : [a-zA-Z_] [a-zA-Z_0-9]* ;

// Skipped
WS           : [ \t\r\n]+  -> skip ;
LINE_COMMENT  : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT : '/*' .*? '*/' -> skip ;
