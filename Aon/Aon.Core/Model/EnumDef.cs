namespace Ambermoon.Aon;

public sealed record EnumMember(string Name, int? ExplicitValue);

public sealed record EnumDef(
    string Name,
    PrimitiveType UnderlyingType,
    IReadOnlyList<EnumMember> Members
) : AodDefinition(Name);
