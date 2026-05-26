namespace Amber.Interpreter.AST;

// ── Type declarations (top-level) ─────────────────────────────────────────────

public abstract record TypeDeclarationNode : AstNode;

public record EnumDeclarationNode(
    string Name,
    AmberType BaseType,
    IReadOnlyList<EnumMemberNode> Members) : TypeDeclarationNode;

// ExplicitValue is null → auto-increment from previous member (starts at 0)
public record EnumMemberNode(string Name, long? ExplicitValue);

// ExplicitValue is null → next power of 2 after previous member
public record BitfieldMemberNode(string Name, long? ExplicitValue);

public record BitfieldDeclarationNode(
    string Name,
    AmberType BaseType,
    IReadOnlyList<BitfieldMemberNode> Members) : TypeDeclarationNode;

public record StructDeclarationNode(
    string Name,
    IReadOnlyList<StructBodyItem> Items) : TypeDeclarationNode;

// ── Struct body items ─────────────────────────────────────────────────────────
// Inherits AstNode so items can be returned from ANTLR visitor methods.

public abstract record StructBodyItem : AstNode;

// ArraySize is null → scalar field; non-null → fixed-size array
public record StructFieldNode(FieldTypeNode Type, string Name, long? ArraySize) : StructBodyItem;

// Splices the fields of the named struct at this position
public record StructImportNode(string TypeName) : StructBodyItem;

// ── Function declaration ──────────────────────────────────────────────────────

public record FunctionDeclarationNode(
    string Name,
    IReadOnlyList<ParamNode> Parameters,
    IReadOnlyList<StatementNode> Body) : TypeDeclarationNode;

// Register is the m68k register name (d0–d7 for values, a0–a6 for pointers/structs)
public record ParamNode(
    ParamDirection Direction,
    string Register,
    FieldTypeNode Type,
    string Name) : AstNode;

// ── Type references (struct fields, function parameters) ──────────────────────
// Inherits AstNode so it can be returned from ANTLR visitor methods.

public abstract record FieldTypeNode : AstNode;

public record PrimitiveFieldType(AmberType Type) : FieldTypeNode;
public record NamedFieldType(string TypeName) : FieldTypeNode;
public record PointerFieldType(FieldTypeNode ElementType) : FieldTypeNode;
