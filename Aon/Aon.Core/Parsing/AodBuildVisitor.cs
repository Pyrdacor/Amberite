using Antlr4.Runtime.Misc;

namespace Ambermoon.Aon;

internal sealed class AodBuildVisitor : AodBaseVisitor<object?>
{
    private readonly List<AodDefinition> _definitions = [];
    private readonly List<ParseDiagnostic> _diagnostics;

    public IReadOnlyList<AodDefinition> Definitions => _definitions;

    public AodBuildVisitor(List<ParseDiagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public override object? VisitAodFile([NotNull] AodParser.AodFileContext context)
    {
        foreach (var def in context.definition())
            Visit(def);
        return null;
    }

    public override object? VisitEnumDef([NotNull] AodParser.EnumDefContext context)
    {
        var name = context.name.Text;
        var underlying = ParsePrimitiveType(context.underlyingType);
        var members = context.enumBody().enumMember()
            .Select(m => new EnumMember(
                m.name.Text,
                m.value != null ? ParseInt(m.value) : null))
            .ToList();

        _definitions.Add(new EnumDef(name, underlying, members));
        return null;
    }

    public override object? VisitBitfieldDef([NotNull] AodParser.BitfieldDefContext context)
    {
        var name = context.name.Text;
        var underlying = ParsePrimitiveType(context.underlyingType);
        var flags = context.bitfieldBody().bitfieldMember()
            .Select(m => m.name.Text)
            .ToList();

        _definitions.Add(new BitfieldDef(name, underlying, flags));
        return null;
    }

    public override object? VisitStructDef([NotNull] AodParser.StructDefContext context)
    {
        var name = context.name.Text;
        var baseType = context.@base?.Text;
        int? declaredSize = context.sizeDirective() is { } sd ? ParseInt(sd.size) : null;

        var members = context.structMember()
            .Select(m => (StructMember?)Visit(m))
            .Where(m => m != null)
            .Select(m => m!)
            .ToList();

        _definitions.Add(new StructDef(name, baseType, declaredSize, members));
        return null;
    }

    public override object? VisitOffsetDirective([NotNull] AodParser.OffsetDirectiveContext context)
    {
        return new OffsetDirective(ParseInt(context.offset));
    }

    public override object? VisitFieldFixup([NotNull] AodParser.FieldFixupContext context)
    {
        var fieldName = context.fieldName.Text;
        var value = string.Join(".", context.value.IDENT().Select(t => t.GetText()));
        return new FieldFixup(fieldName, value);
    }

    public override object? VisitFieldDecl([NotNull] AodParser.FieldDeclContext context)
    {
        var isOverride = context.isOverride != null;
        var typeRef    = (TypeRef)Visit(context.type_)!;
        var fieldName  = context.fieldName.Text;

        // '?' lives inside the typeRef alternative (before the optional array size)
        bool isOptional = context.type_ switch
        {
            AodParser.PrimitiveTypeRefContext p => p.QUESTION() != null,
            AodParser.NamedTypeRefContext n      => n.QUESTION() != null,
            _                                   => false,
        };

        AodDefaultValue? defaultValue = null;
        if (context.defaultValue != null)
            defaultValue = (AodDefaultValue)Visit(context.defaultValue)!;

        return new FieldDecl(typeRef, fieldName, isOverride, isOptional, defaultValue);
    }

    public override object? VisitAodIntDefault([NotNull] AodParser.AodIntDefaultContext context)
    {
        return new AodIntDefault(ParseInt(context.intLiteral()));
    }

    public override object? VisitAodRefDefault([NotNull] AodParser.AodRefDefaultContext context)
    {
        var name = string.Join(".", context.qualifiedIdent().IDENT().Select(t => t.GetText()));
        return new AodRefDefault(name);
    }

    public override object? VisitPrimitiveTypeRef([NotNull] AodParser.PrimitiveTypeRefContext context)
    {
        var type = ParsePrimitiveType(context.primitiveType());
        var size = context.size != null ? ParseInt(context.size) : (int?)null;
        return new PrimitiveTypeRef(type, size);
    }

    public override object? VisitNamedTypeRef([NotNull] AodParser.NamedTypeRefContext context)
    {
        var typeName = context.typeName.Text;
        var size = context.size != null ? ParseInt(context.size) : (int?)null;
        return new NamedTypeRef(typeName, size);
    }

    private static PrimitiveType ParsePrimitiveType(AodParser.PrimitiveTypeContext ctx) =>
        ctx.GetText() switch
        {
            "ubyte"  => PrimitiveType.UByte,
            "sbyte"  => PrimitiveType.SByte,
            "uword"  => PrimitiveType.UWord,
            "sword"  => PrimitiveType.SWord,
            "udword" => PrimitiveType.UDWord,
            "sdword" => PrimitiveType.SDWord,
            var t    => throw new InvalidOperationException($"Unknown primitive type: {t}"),
        };

    private static int ParseInt(AodParser.IntLiteralContext ctx)
    {
        var text = ctx.GetText();
        return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToInt32(text, 16)
            : int.Parse(text);
    }
}
