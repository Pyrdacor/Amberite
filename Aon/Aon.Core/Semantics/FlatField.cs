namespace Ambermoon.Aon;

internal sealed record FlatField(
    int Offset,
    TypeRef Type,
    string Name,
    bool IsOptional,
    AodDefaultValue? DefaultValue,
    AonValue? FixedValue   // non-null when a FieldFixup applies to this field
);
