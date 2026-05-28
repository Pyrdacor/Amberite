using Antlr4.Runtime;

namespace Ambermoon.Aon;

public sealed class AonParseResult
{
    public AonDocument? Document { get; }
    public IReadOnlyList<ParseDiagnostic> Diagnostics { get; }
    public bool Success => Document != null && !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    internal AonParseResult(AonDocument? document, IReadOnlyList<ParseDiagnostic> diagnostics)
    {
        Document = document;
        Diagnostics = diagnostics;
    }
}

public static class AonDocumentParser
{
    public static AonParseResult Parse(string text)
    {
        var errorListener = new AodErrorListener();
        var inputStream = new AntlrInputStream(text);

        var lexer = new AonLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorListener);

        var tokenStream = new CommonTokenStream(lexer);

        var parser = new AonParser(tokenStream);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);

        var tree = parser.aonFile();
        var diagnostics = new List<ParseDiagnostic>(errorListener.Diagnostics);

        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new AonParseResult(null, diagnostics);

        var visitor = new AonBuildVisitor(diagnostics);
        visitor.Visit(tree);

        var doc = new AonDocument(visitor.Instances);
        return new AonParseResult(doc, diagnostics);
    }

    public static AonParseResult ParseFile(string path) =>
        Parse(File.ReadAllText(path));
}
