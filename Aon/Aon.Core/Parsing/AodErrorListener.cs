using Antlr4.Runtime;

namespace Ambermoon.Aon;

internal sealed class AodErrorListener :
    IAntlrErrorListener<IToken>,
    IAntlrErrorListener<int>
{
    private readonly List<ParseDiagnostic> _diagnostics = [];

    public IReadOnlyList<ParseDiagnostic> Diagnostics => _diagnostics;

    // Parser errors (IToken)
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _diagnostics.Add(new ParseDiagnostic(DiagnosticSeverity.Error, msg, line, charPositionInLine));
    }

    // Lexer errors (int)
    public void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        _diagnostics.Add(new ParseDiagnostic(DiagnosticSeverity.Error, msg, line, charPositionInLine));
    }
}
