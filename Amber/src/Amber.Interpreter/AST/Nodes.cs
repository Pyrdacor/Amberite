namespace Amber.Interpreter.AST;

// Root
public abstract record AstNode;
public abstract record StatementNode : AstNode;
public abstract record ExpressionNode : AstNode;

// Items can be TypeDeclarationNode or StatementNode, preserving source order
public record ProgramNode(IReadOnlyList<AstNode> Items) : AstNode;

// Statements
public record VarDeclarationNode(
    AmberType Type,
    string Name,
    ExpressionNode? Initializer) : StatementNode;

public record AssignmentNode(
    string Name,
    ExpressionNode Value) : StatementNode;

// Expressions
public record BinaryExprNode(
    BinaryOp Op,
    ExpressionNode Left,
    ExpressionNode Right) : ExpressionNode;

public record UnaryExprNode(
    UnaryOp Op,
    ExpressionNode Operand) : ExpressionNode;

public record IntLiteralNode(long Value) : ExpressionNode;
public record BoolLiteralNode(bool Value) : ExpressionNode;
public record IdentifierNode(string Name) : ExpressionNode;
// Sub-part access: foo.w0, foo.b2 etc.
public record PartAccessNode(ExpressionNode Source, string Part) : ExpressionNode;
