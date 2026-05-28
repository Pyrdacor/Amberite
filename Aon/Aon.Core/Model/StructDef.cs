namespace Ambermoon.Aon;

public sealed record StructDef(
    string Name,
    string? BaseType,
    IReadOnlyList<StructMember> Members
) : AodDefinition(Name);
