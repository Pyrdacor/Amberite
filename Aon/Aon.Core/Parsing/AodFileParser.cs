using Antlr4.Runtime;

namespace Ambermoon.Aon;

public sealed class AodParseResult
{
    public AodFile? File { get; }
    public IReadOnlyList<ParseDiagnostic> Diagnostics { get; }
    public bool Success => File != null && !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    internal AodParseResult(AodFile? file, IReadOnlyList<ParseDiagnostic> diagnostics)
    {
        File = file;
        Diagnostics = diagnostics;
    }
}

public static class AodFileParser
{
    public static AodParseResult Parse(string text)
    {
        var errorListener = new AodErrorListener();
        var inputStream = new AntlrInputStream(text);

        var lexer = new AodLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorListener);

        var tokenStream = new CommonTokenStream(lexer);

        var parser = new AodParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);

        var tree = parser.aodFile();

        var diagnostics = new List<ParseDiagnostic>(errorListener.Diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new AodParseResult(null, diagnostics);

        var visitor = new AodBuildVisitor(diagnostics);
        visitor.Visit(tree);

        var file = new AodFile(visitor.Definitions);
        return new AodParseResult(file, diagnostics);
    }

    public static AodParseResult ParseFile(string path) =>
        Parse(System.IO.File.ReadAllText(path));
}
