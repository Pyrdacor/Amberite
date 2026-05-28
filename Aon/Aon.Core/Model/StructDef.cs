namespace Ambermoon.Aon;

public sealed record StructDef(
    string Name,
    string? BaseType,
    int? DeclaredSizeInBytes,
    IReadOnlyList<StructMember> Members
) : AodDefinition(Name);
