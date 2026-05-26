namespace Amber.Interpreter.Parsing;

using Antlr4.Runtime;

/// Collects lexer and parser errors into a list instead of printing to stderr.
internal sealed class CollectingErrorListener(List<string> errors)
    : BaseErrorListener, IAntlrErrorListener<int>
{
    // Parser errors (IToken offending symbol)
    public override void SyntaxError(
        System.IO.TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line, int charPositionInLine,
        string msg, RecognitionException e)
        => errors.Add($"line {line}:{charPositionInLine} {msg}");

    // Lexer errors (int offending symbol = char code)
    void IAntlrErrorListener<int>.SyntaxError(
        System.IO.TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line, int charPositionInLine,
        string msg, RecognitionException e)
        => errors.Add($"line {line}:{charPositionInLine} {msg}");
}
