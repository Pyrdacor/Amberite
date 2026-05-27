grammar Ambdev;

// ===== Parser Rules =====

program
    : topLevel* EOF
    ;

topLevel
    : enumDecl
    | bitfieldDecl
    | constDecl
    | etypeDecl
    | especDecl
    | eventDecl
    | chainDecl
    ;

// Accepts both mixed-case identifiers and ALL_CAPS constant-style names in all name positions
ident : IDENTIFIER | CONST_IDENTIFIER ;

// ── Enum ──────────────────────────────────────────────────────────────────────

enumDecl
    : ENUM ident COLON baseType LBRACE enumMembers RBRACE
    ;

enumMembers
    : enumMember (COMMA enumMember)* COMMA?
    ;

enumMember
    : ident (ASSIGN constExpr)?
    ;

// ── Bitfield ──────────────────────────────────────────────────────────────────

bitfieldDecl
    : BITFIELD ident COLON baseType LBRACE bitfieldMembers RBRACE
    ;

bitfieldMembers
    : bitfieldMember (COMMA bitfieldMember)* COMMA?
    ;

bitfieldMember
    : ident (ASSIGN bitfieldValueExpr)?
    ;

// 'ident' alternative covers both same-bitfield member references and constant references;
// the interpreter disambiguates by looking up the name in members first, then constants.
bitfieldValueExpr
    : bitfieldValueExpr PIPE bitfieldValueExpr  # orBitfieldValue
    | literal                                    # literalBitfieldValue
    | ident                                      # identRefBitfieldValue
    ;

// Enum / bitfield base type — bool excluded
baseType
    : BYTE
    | WORD
    | LONG
    ;

// ── Constants ─────────────────────────────────────────────────────────────────

constDecl
    : CONST ident ASSIGN constExpr SEMI?
    ;

// * and / bind tighter than + and - (earlier alternatives = higher precedence)
constExpr
    : constExpr STAR  constExpr   # mulConstExpr
    | constExpr SLASH constExpr   # divConstExpr
    | constExpr PLUS  constExpr   # addConstExpr
    | constExpr MINUS constExpr   # subConstExpr
    | MINUS constExpr              # negConstExpr
    | LPAREN constExpr RPAREN     # parenConstExpr
    | TRUE                         # trueConstExpr
    | FALSE                        # falseConstExpr
    | literal                      # numLiteralConstExpr
    | ident                        # constRefConstExpr
    ;

// ── Event Type Definition ─────────────────────────────────────────────────────

etypeDecl
    : ETYPE LBRACKET INTEGER_LITERAL COMMA ident RBRACKET COLON
      etypeField*
    ;

etypeField
    : MINUS INTEGER_LITERAL COLON fieldType QUESTION? ident (ASSIGN constExpr)? rangeConstraint?
    ;

// ── Event Specialization ──────────────────────────────────────────────────────

especDecl
    : ESPEC LBRACKET INTEGER_LITERAL COMMA ident RBRACKET COLON
      especItem*
    ;

especItem
    : MINUS WHEN condition         # whenEspecItem
    | MINUS INTEGER_LITERAL COLON fieldType QUESTION? ident (ASSIGN constExpr)? rangeConstraint?  # fieldEspecItem
    ;

// Conditions: 'and' binds tighter than 'or' (first alternative = higher precedence)
condition
    : left=condition AND right=condition  # andCondition
    | left=condition OR  right=condition  # orCondition
    | ident compareOp literal             # compareCondition
    ;

compareOp
    : EQ | NEQ | LTE | GTE | LT | GT
    ;

// ── Shared field type and range ───────────────────────────────────────────────

// Primitive or named (enum/bitfield) type for etype/espec fields
fieldType
    : BYTE  # byteFieldType
    | WORD  # wordFieldType
    | LONG  # longFieldType
    | ident # namedFieldType
    ;

// [0..1023] continuous range — tried first; [0, 1, 2] or [5] falls to valueListRange
rangeConstraint
    : LBRACKET INTEGER_LITERAL DOTDOT INTEGER_LITERAL RBRACKET    # continuousRange
    | LBRACKET INTEGER_LITERAL (COMMA INTEGER_LITERAL)* RBRACKET  # valueListRange
    ;

literal
    : INTEGER_LITERAL
    | HEX_LITERAL
    ;

// ── Event Definition ──────────────────────────────────────────────────────────

eventDecl
    : EVENT LBRACKET INTEGER_LITERAL COMMA ident RBRACKET ASSIGN ETYPE LBRACKET etypeRef RBRACKET
      eventField*
    ;

// etype reference inside an event declaration: by numeric index or by name
etypeRef
    : INTEGER_LITERAL  # indexEtypeRef
    | ident            # nameEtypeRef
    ;

eventField
    : MINUS ident COLON eventValueExpr
    ;

// 'ident' alone covers constant references; 'ident DOT ident' covers qualified names.
eventValueExpr
    : eventValueExpr PIPE eventValueExpr  # orEventValue
    | ident DOT ident                      # qualifiedEventValue
    | ident                                # constRefEventValue
    | literal                              # literalEventValue
    ;

// ── Chain Definition ──────────────────────────────────────────────────────────

chainDecl
    : CHAIN LBRACKET INTEGER_LITERAL COMMA ident RBRACKET
      chainStep*
    ;

chainStep
    : stepPrefix stepTarget chainStepTarget
    ;

chainStepTarget
    : INTEGER_LITERAL  # indexChainTarget
    | ident            # nameChainTarget
    ;

stepPrefix
    : MINUS
    | QUESTION
    | BANG
    ;

stepTarget
    : EVENT
    | CHAIN
    ;

// ===== Lexer Rules =====

// Keywords
ENUM     : 'enum'     ;
BITFIELD : 'bitfield' ;
CONST    : 'const'    ;
ETYPE    : 'etype'    ;
ESPEC    : 'espec'    ;
EVENT    : 'event'    ;
CHAIN    : 'chain'    ;
BYTE     : 'byte'     ;
WORD     : 'word'     ;
LONG     : 'long'     ;
WHEN     : 'when'     ;
AND      : 'and'      ;
OR       : 'or'       ;
TRUE     : 'true'     ;
FALSE    : 'false'    ;

// Comparison operators — longer tokens must be defined before shorter prefixes
EQ    : '==' ;
NEQ   : '!=' ;
LTE   : '<=' ;
GTE   : '>=' ;
LT    : '<'  ;
GT    : '>'  ;

// Other operators / punctuation
ASSIGN   : '='  ;
MINUS    : '-'  ;
PLUS     : '+'  ;
STAR     : '*'  ;
SLASH    : '/'  ;
QUESTION : '?'  ;
BANG     : '!'  ;
PIPE     : '|'  ;
COMMA    : ','  ;
COLON    : ':'  ;
SEMI     : ';'  ;
LBRACE   : '{'  ;
RBRACE   : '}'  ;
LBRACKET : '['  ;
RBRACKET : ']'  ;
LPAREN   : '('  ;
RPAREN   : ')'  ;

DOTDOT   : '..' ;
DOT      : '.'  ;

// Literals — HEX before INTEGER so "0xFF" doesn't tokenize as "0" + "xFF"
HEX_LITERAL      : '0' [xX] [0-9a-fA-F]+ ;
INTEGER_LITERAL  : [0-9]+                 ;

// Must be before IDENTIFIER — all-caps names are constants, not regular identifiers
CONST_IDENTIFIER : [A-Z][A-Z0-9_]*       ;
IDENTIFIER       : [a-zA-Z_] [a-zA-Z_0-9]* ;

WS            : [ \t\r\n]+  -> skip ;
LINE_COMMENT  : '//' ~[\r\n]* -> skip ;
BLOCK_COMMENT : '/*' .*? '*/' -> skip ;
