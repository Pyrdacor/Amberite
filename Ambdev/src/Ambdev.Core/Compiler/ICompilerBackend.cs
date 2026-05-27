namespace Ambdev.Interpreter.Compiler;

/// <summary>
/// Contract for all Ambdev compilation backends.
/// Each backend transforms a resolved <see cref="CompilationInput"/> into a
/// target-specific binary payload.
/// </summary>
public interface ICompilerBackend
{
    /// <summary>Human-readable identifier for this backend (e.g. "Ambermoon").</summary>
    string Name { get; }

    /// <summary>Compile the given input into a binary payload.</summary>
    byte[] Compile(CompilationInput input);
}
