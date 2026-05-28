namespace Ambermoon.Aon;

public sealed record CompileDiagnostic(
    DiagnosticSeverity Severity,
    string Context,
    string Message
)
{
    public override string ToString() =>
        $"{Severity} [{Context}]: {Message}";
}
