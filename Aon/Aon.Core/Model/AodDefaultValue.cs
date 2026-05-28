namespace Ambermoon.Aon;

public abstract record AodDefaultValue;

public sealed record AodIntDefault(long Value) : AodDefaultValue;

public sealed record AodRefDefault(string QualifiedName) : AodDefaultValue;
