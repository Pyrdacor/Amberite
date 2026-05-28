namespace Ambermoon.Aon;

internal static class SizeCalculator
{
    public static int TypeSize(TypeRef typeRef, AodFile file)
    {
        int baseSize = typeRef switch
        {
            PrimitiveTypeRef p => p.Type.SizeInBytes(),
            NamedTypeRef n     => NamedTypeSize(n.TypeName, file),
            _                  => throw new InvalidOperationException($"Unknown TypeRef: {typeRef}"),
        };
        return baseSize * (typeRef.ArraySize ?? 1);
    }

    private static int NamedTypeSize(string typeName, AodFile file)
    {
        var def = file.Get<AodDefinition>(typeName);
        if (def == null) return 0;   // forward-referenced — caller handles error separately
        return def switch
        {
            EnumDef e    => e.UnderlyingType.SizeInBytes(),
            BitfieldDef b => b.UnderlyingType.SizeInBytes(),
            StructDef s  => StructSize(s, file),
            _            => 0,
        };
    }

    public static int StructSize(StructDef structDef, AodFile file)
    {
        if (structDef.DeclaredSizeInBytes.HasValue)
            return structDef.DeclaredSizeInBytes.Value;

        var flat = StructFlattener.Flatten(structDef, file);
        if (flat.Count == 0) return 0;
        return flat.Max(f => f.Offset + TypeSize(f.Type, file));
    }
}
