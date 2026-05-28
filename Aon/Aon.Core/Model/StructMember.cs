namespace Ambermoon.Aon;

public abstract record StructMember;

public sealed record OffsetDirective(int Offset) : StructMember;

public sealed record FieldDecl(TypeRef Type, string Name, bool IsOverride) : StructMember;

public sealed record FieldFixup(string FieldName, string Value) : StructMember;
