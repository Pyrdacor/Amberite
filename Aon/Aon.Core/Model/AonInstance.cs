namespace Ambermoon.Aon;

public sealed record AonInstance(
    string TypeName,
    string Name,
    IReadOnlyList<AonField> Fields
);
