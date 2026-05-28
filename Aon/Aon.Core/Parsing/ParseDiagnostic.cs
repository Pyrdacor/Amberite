namespace Ambermoon.Aon;

public enum DiagnosticSeverity { Warning, Error }

public sealed record ParseDiagnostic(
    DiagnosticSeverity Severity,
    string Message,
    int Line,
    int Column
)
{
    public override string ToString() =>
        $"{Severity} ({Line},{Column}): {Message}";
}
