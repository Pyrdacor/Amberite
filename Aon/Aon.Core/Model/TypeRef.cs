namespace Ambermoon.Aon;

public abstract record TypeRef(int? ArraySize);

public sealed record PrimitiveTypeRef(PrimitiveType Type, int? ArraySize) : TypeRef(ArraySize);

public sealed record NamedTypeRef(string TypeName, int? ArraySize) : TypeRef(ArraySize);
