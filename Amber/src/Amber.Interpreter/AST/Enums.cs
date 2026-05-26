namespace Amber.Interpreter.AST;

public enum AmberType { Byte, Word, Long, Bool }

public enum BinaryOp
{
    Add, Sub,
    Mul, Div, Mod,
    And, Or, Xor,
    Shl, Shr,
}

public enum UnaryOp { Negate, BitwiseNot, LogicalNot }

public enum ParamDirection { In, Out, InOut }
