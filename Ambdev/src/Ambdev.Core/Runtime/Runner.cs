namespace Ambdev.Interpreter.Runtime;

using Ambdev.Interpreter.Grammar;
using Ambdev.Interpreter.Parsing;
using Antlr4.Runtime;

public static class AmbdevRunner
{
    public static AmbdevInterpreter Execute(string source, string sourceName = "<input>")
    {
        var errors   = new List<string>();
        var listener = new CollectingErrorListener(errors);

        var stream = new AntlrInputStream(source);

        var lexer = new AmbdevLexer(stream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var tokens = new CommonTokenStream(lexer);

        var parser = new AmbdevParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var tree = parser.program();

        if (errors.Count > 0)
            throw new AmbdevParseException(
                string.Join("\n", errors.Select(e => $"{sourceName}:{e}")));

        var program     = new AstBuilder().BuildProgram(tree);
        var interpreter = new AmbdevInterpreter();
        interpreter.Execute(program);
        return interpreter;
    }
}
