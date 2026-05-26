namespace Ambdev.Interpreter.Runtime;

public class AmbdevRuntimeException(string message) : Exception(message);
public class AmbdevParseException(string message) : Exception(message);
