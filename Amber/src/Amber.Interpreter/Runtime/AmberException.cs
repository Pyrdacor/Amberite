namespace Amber.Interpreter.Runtime;

public class AmberRuntimeException(string message) : Exception(message);
public class AmberParseException(string message) : Exception(message);
