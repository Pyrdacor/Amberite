namespace Ambdev.Interpreter.Compiler;

using Ambdev.Interpreter.Runtime;

/// <summary>
/// Orchestrates the two-file compilation workflow:
/// parses an .aed (type definitions) and an .aev (event/chain declarations) as a
/// single Ambdev program, then delegates binary generation to the chosen backend.
/// </summary>
public static class AmbdevCompiler
{
    /// <summary>
    /// Compile the given .aed and .aev sources using the specified backend.
    /// </summary>
    /// <param name="aedSource">Contents of the type-definition file (.aed).</param>
    /// <param name="aevSource">Contents of the event/chain-declaration file (.aev).</param>
    /// <param name="backend">The target backend that produces the final binary.</param>
    /// <param name="aedName">Display name used in parse-error messages.</param>
    /// <param name="aevName">Display name used in parse-error messages.</param>
    public static byte[] Compile(
        string           aedSource,
        string           aevSource,
        ICompilerBackend backend,
        string           aedName = "<aed>",
        string           aevName = "<aev>")
    {
        // Concatenate: .aed declares all types; .aev adds events and chains.
        var combined    = aedSource + "\n" + aevSource;
        var interpreter = AmbdevRunner.Execute(combined, $"{aedName}+{aevName}");
        var input       = new CompilationInput(interpreter.Registry);
        return backend.Compile(input);
    }
}
