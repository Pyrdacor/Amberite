namespace Ambermoon.Aon;

public abstract record AonValue;

public sealed record AonIntValue(long Value) : AonValue;

public sealed record AonHexValue(long Value) : AonValue;

public sealed record AonRefValue(string QualifiedName) : AonValue;

public sealed record AonStringValue(string Value) : AonValue;

public sealed record AonFlagsValue(AonValue Left, AonValue Right) : AonValue;

public sealed record AonObjectValue(IReadOnlyList<AonField> Fields) : AonValue;

public sealed record AonArrayValue(IReadOnlyList<AonValue> Items) : AonValue;
