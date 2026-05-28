namespace Ambermoon.Aon;

public sealed record BitfieldDef(
    string Name,
    PrimitiveType UnderlyingType,
    IReadOnlyList<string> Flags
) : AodDefinition(Name);
