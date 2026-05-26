namespace Ambdev.Interpreter.AST;

// ── Base ──────────────────────────────────────────────────────────────────────

public abstract record AstNode;
public record ProgramNode(IReadOnlyList<AstNode> Items) : AstNode;

// ── Field type references ─────────────────────────────────────────────────────
// Inherits AstNode so ANTLR visitor methods can return them.

public abstract record FieldTypeNode : AstNode;
public record PrimitiveFieldTypeNode(PrimType Type) : FieldTypeNode;
public record NamedFieldTypeNode(string TypeName) : FieldTypeNode;

// ── Constants ─────────────────────────────────────────────────────────────────

public record ConstDeclarationNode(string Name, ConstExprNode Expr) : AstNode;

public abstract record ConstExprNode : AstNode;
public record LiteralConstExprNode(long Value) : ConstExprNode;
public record BoolConstExprNode(bool Value) : ConstExprNode;
public record ConstRefNode(string Name) : ConstExprNode;
public record NegConstExprNode(ConstExprNode Operand) : ConstExprNode;
public record BinaryConstExprNode(ConstExprNode Left, BinaryOp Op, ConstExprNode Right) : ConstExprNode;

// ── Enum ──────────────────────────────────────────────────────────────────────

public record EnumDeclarationNode(
    string Name,
    PrimType BaseType,
    IReadOnlyList<EnumMemberNode> Members) : AstNode;

// ValueExpr null → auto-increment from previous (starts at 0)
public record EnumMemberNode(string Name, ConstExprNode? ValueExpr);

// ── Bitfield ──────────────────────────────────────────────────────────────────

public record BitfieldDeclarationNode(
    string Name,
    PrimType BaseType,
    IReadOnlyList<BitfieldMemberNode> Members) : AstNode;

// ValueExpr null → next power of 2 after previous member
public record BitfieldMemberNode(string Name, BitfieldValueExprNode? ValueExpr);

public abstract record BitfieldValueExprNode : AstNode;
public record LiteralBitfieldValueNode(long Value) : BitfieldValueExprNode;
// Covers both same-bitfield member references and constant references; interpreter disambiguates
public record IdentRefBitfieldValueNode(string Name) : BitfieldValueExprNode;
public record OrBitfieldValueNode(BitfieldValueExprNode Left, BitfieldValueExprNode Right) : BitfieldValueExprNode;

// ── Event Type ────────────────────────────────────────────────────────────────

public record EtypeDeclarationNode(
    int Index,
    string Name,
    IReadOnlyList<EtypeFieldNode> Fields) : AstNode;

// EtypeFieldNode also serves as a field item inside espec declarations
// (espec field items are structurally identical — just a different context).
public record EtypeFieldNode(
    int Offset,
    FieldTypeNode Type,
    bool IsOptional,
    string Name,
    ConstExprNode? DefaultValue,
    RangeConstraintNode? Range) : EspecItemNode;

// ── Event Specialization ──────────────────────────────────────────────────────

public record EspecDeclarationNode(
    int EtypeIndex,
    string Name,
    IReadOnlyList<EspecItemNode> Items) : AstNode;

// Base for the two kinds of espec items
public abstract record EspecItemNode : AstNode;

public record WhenEspecItemNode(ConditionNode Condition) : EspecItemNode;

// (EtypeFieldNode : EspecItemNode — declared above)

// ── Conditions ────────────────────────────────────────────────────────────────

public abstract record ConditionNode : AstNode;
public record AndConditionNode(ConditionNode Left, ConditionNode Right) : ConditionNode;
public record OrConditionNode(ConditionNode Left, ConditionNode Right) : ConditionNode;
public record CompareConditionNode(string FieldName, CompareOp Op, long Value) : ConditionNode;

// ── Range constraints ─────────────────────────────────────────────────────────

public abstract record RangeConstraintNode : AstNode;
public record ContinuousRangeNode(long Min, long Max) : RangeConstraintNode;
public record ValueListRangeNode(IReadOnlyList<long> Values) : RangeConstraintNode;

// ── Event ─────────────────────────────────────────────────────────────────────

public record EventDeclarationNode(
    int Index,
    string Name,
    int EtypeIndex,
    IReadOnlyList<EventFieldNode> Fields) : AstNode;

public record EventFieldNode(string FieldName, EventValueExprNode ValueExpr) : AstNode;

public abstract record EventValueExprNode : AstNode;
public record LiteralEventValueNode(long Value) : EventValueExprNode;
public record QualifiedEventValueNode(string TypeName, string MemberName) : EventValueExprNode;
public record ConstRefEventValueNode(string ConstName) : EventValueExprNode;
public record OrEventValueNode(EventValueExprNode Left, EventValueExprNode Right) : EventValueExprNode;

// ── Chain ─────────────────────────────────────────────────────────────────────

public record ChainDeclarationNode(
    int Index,
    string Name,
    IReadOnlyList<ChainStepNode> Steps) : AstNode;

// Exactly one of TargetIndex / TargetName is set (null = name-based, not null = index-based)
public record ChainStepNode(StepPrefix Prefix, StepTarget Target, int? TargetIndex, string? TargetName) : AstNode;
