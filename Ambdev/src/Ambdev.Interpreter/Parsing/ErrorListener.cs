namespace Ambdev.Interpreter.Parsing;

using Antlr4.Runtime;

internal sealed class CollectingErrorListener(List<string> errors)
    : BaseErrorListener, IAntlrErrorListener<int>
{
    public override void SyntaxError(
        System.IO.TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line, int charPositionInLine,
        string msg, RecognitionException e)
        => errors.Add($"line {line}:{charPositionInLine} {msg}");

    void IAntlrErrorListener<int>.SyntaxError(
        System.IO.TextWriter output,
        IRecognizer recognizer,
        int offendingSymbol,
        int line, int charPositionInLine,
        string msg, RecognitionException e)
        => errors.Add($"line {line}:{charPositionInLine} {msg}");
}
