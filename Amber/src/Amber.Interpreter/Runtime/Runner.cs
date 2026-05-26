namespace Amber.Interpreter.Runtime;

using Amber.Interpreter.Grammar;
using Amber.Interpreter.Parsing;
using Antlr4.Runtime;

/// Parses and executes an Amber source string; returns the populated interpreter.
/// Throws <see cref="AmberParseException"/> on syntax errors,
/// <see cref="AmberRuntimeException"/> on runtime errors.
public static class AmberRunner
{
    public static AmberInterpreter Execute(string source, string sourceName = "<input>")
    {
        var errors       = new List<string>();
        var listener     = new CollectingErrorListener(errors);

        var stream = new AntlrInputStream(source);

        var lexer = new AmberLexer(stream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var tokens = new CommonTokenStream(lexer);

        var parser = new AmberParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var tree = parser.program();

        if (errors.Count > 0)
            throw new AmberParseException(
                string.Join("\n", errors.Select(e => $"{sourceName}:{e}")));

        var program     = new AstBuilder().BuildProgram(tree);
        var interpreter = new AmberInterpreter();
        interpreter.Execute(program);
        return interpreter;
    }
}
