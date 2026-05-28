using Antlr4.Runtime.Misc;

namespace Ambermoon.Aon;

internal sealed class AonBuildVisitor : AonBaseVisitor<object?>
{
    private readonly List<AonInstance> _instances = [];
    private readonly List<ParseDiagnostic> _diagnostics;

    public IReadOnlyList<AonInstance> Instances => _instances;

    public AonBuildVisitor(List<ParseDiagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public override object? VisitAonFile([NotNull] AonParser.AonFileContext context)
    {
        foreach (var inst in context.instance())
            Visit(inst);
        return null;
    }

    public override object? VisitInstance([NotNull] AonParser.InstanceContext context)
    {
        var typeName = context.typeName.Text;
        var name = context.name.Text;
        var fields = context.field().Select(f => (AonField)Visit(f)!).ToList();
        _instances.Add(new AonInstance(typeName, name, fields));
        return null;
    }

    public override object? VisitField([NotNull] AonParser.FieldContext context)
    {
        var name = context.fieldName.Text;
        var value = (AonValue)Visit(context.value())!;
        return new AonField(name, value);
    }

    public override object? VisitFlagsValue([NotNull] AonParser.FlagsValueContext context)
    {
        var left = (AonValue)Visit(context.left)!;
        var right = (AonValue)Visit(context.right)!;
        return new AonFlagsValue(left, right);
    }

    public override object? VisitIntValue([NotNull] AonParser.IntValueContext context)
    {
        return ParseIntLiteral(context.intLiteral());
    }

    public override object? VisitRefValue([NotNull] AonParser.RefValueContext context)
    {
        var name = string.Join(".", context.qualifiedIdent().IDENT().Select(t => t.GetText()));
        return new AonRefValue(name);
    }

    public override object? VisitStringValue([NotNull] AonParser.StringValueContext context)
    {
        var raw = context.STRING().GetText();
        // strip surrounding quotes and unescape
        var inner = raw[1..^1]
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t");
        return new AonStringValue(inner);
    }

    public override object? VisitObjectValue([NotNull] AonParser.ObjectValueContext context)
    {
        var fields = context.field().Select(f => (AonField)Visit(f)!).ToList();
        return new AonObjectValue(fields);
    }

    public override object? VisitArrayValue([NotNull] AonParser.ArrayValueContext context)
    {
        var items = context.arrayItems() is { } ai
            ? ai.value().Select(v => (AonValue)Visit(v)!).ToList()
            : (List<AonValue>)[];
        return new AonArrayValue(items);
    }

    private static AonValue ParseIntLiteral(AonParser.IntLiteralContext ctx)
    {
        var text = ctx.GetText();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return new AonHexValue(Convert.ToInt64(text, 16));
        if (text.StartsWith('-'))
            return new AonIntValue(long.Parse(text));
        return new AonIntValue(long.Parse(text));
    }
}
