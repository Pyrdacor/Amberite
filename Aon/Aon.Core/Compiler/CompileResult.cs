namespace Ambermoon.Aon;

public sealed class CompileResult
{
    public bool Success { get; }
    public IReadOnlyList<CompileDiagnostic> Diagnostics { get; }
    public IReadOnlyDictionary<string, byte[]>? Outputs { get; }

    internal CompileResult(
        IReadOnlyDictionary<string, byte[]>? outputs,
        IReadOnlyList<CompileDiagnostic> diagnostics)
    {
        Outputs     = outputs;
        Diagnostics = diagnostics;
        Success     = outputs != null && !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
    }
}
