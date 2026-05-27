namespace Ambdev.Interpreter.Compiler;

using Ambdev.Interpreter.Runtime;

/// <summary>
/// Prepared compilation input: a resolved registry plus sorted event and chain sequences.
/// Created by <see cref="AmbdevCompiler"/> and passed to the chosen <see cref="ICompilerBackend"/>.
/// </summary>
public sealed class CompilationInput
{
    /// <summary>The full registry with all type, event and chain definitions.</summary>
    public AmbdevRegistry Registry { get; }

    /// <summary>Events sorted ascending by their 1-based index.</summary>
    public IReadOnlyList<EventInfo> Events { get; }

    /// <summary>Chains sorted ascending by their index.</summary>
    public IReadOnlyList<ChainInfo> Chains { get; }

    public CompilationInput(AmbdevRegistry registry)
    {
        Registry = registry;
        Events   = registry.Events.Values.OrderBy(e => e.Index).ToList();
        Chains   = registry.Chains.Values.OrderBy(c => c.Index).ToList();
    }
}
