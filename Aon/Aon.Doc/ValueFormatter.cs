using Ambermoon.Aon;

namespace Ambermoon.Aon.Doc;

internal static class ValueFormatter
{
    // Renders an AonValue as a compact single-line string suitable for a table cell.
    public static string Format(AonValue value, int depth = 0) => value switch
    {
        AonIntValue i    => i.Value.ToString(),
        AonHexValue h    => $"0x{h.Value:X}",
        AonStringValue s => s.Value,
        AonRefValue r    => r.QualifiedName,
        AonFlagsValue f  => $"{Format(f.Left, depth)} | {Format(f.Right, depth)}",
        AonObjectValue o => FormatObject(o, depth),
        AonArrayValue a  => FormatArray(a, depth),
        _                => value.ToString() ?? "",
    };

    private static string FormatObject(AonObjectValue o, int depth)
    {
        if (o.Fields.Count == 0) return "{}";
        var inner = string.Join(", ", o.Fields.Select(f => $"{f.Name}={Format(f.Value, depth + 1)}"));
        return depth == 0 ? inner : $"{{{inner}}}";
    }

    private static string FormatArray(AonArrayValue a, int depth)
    {
        if (a.Items.Count == 0) return "[]";
        var items = a.Items.Select(v => Format(v, depth + 1));
        return $"[{string.Join(", ", items)}]";
    }

    // Returns a short display string for a TypeRef, e.g. "ubyte", "uword[8]", "SpellTypes?".
    public static string FormatType(TypeRef typeRef, bool isOptional)
    {
        var baseName = typeRef switch
        {
            PrimitiveTypeRef p => p.Type switch
            {
                PrimitiveType.UByte  => "ubyte",
                PrimitiveType.SByte  => "sbyte",
                PrimitiveType.UWord  => "uword",
                PrimitiveType.SWord  => "sword",
                PrimitiveType.UDWord => "udword",
                PrimitiveType.SDWord => "sdword",
                _                    => p.Type.ToString().ToLower(),
            },
            NamedTypeRef n => n.TypeName,
            _              => typeRef.ToString() ?? "",
        };

        var arrayPart    = typeRef.ArraySize.HasValue ? $"[{typeRef.ArraySize}]" : "";
        var optionalPart = isOptional ? "?" : "";
        return $"{baseName}{optionalPart}{arrayPart}";
    }
}
