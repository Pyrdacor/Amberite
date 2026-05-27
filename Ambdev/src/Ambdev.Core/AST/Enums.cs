namespace Ambdev.Interpreter.AST;

public enum PrimType { Byte, Word, Long }
public enum StepPrefix { Always, IfSuccess, IfFailure }
public enum StepTarget { Event, Chain }
public enum CompareOp  { Eq, Neq, Lt, Gt, Lte, Gte }
public enum BinaryOp   { Add, Sub, Mul, Div }
