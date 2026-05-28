namespace Ambermoon.Aon;

public enum PrimitiveType
{
    UByte,
    SByte,
    UWord,
    SWord,
    UDWord,
    SDWord,
}

public static class PrimitiveTypeExtensions
{
    public static int SizeInBytes(this PrimitiveType type) => type switch
    {
        PrimitiveType.UByte  => 1,
        PrimitiveType.SByte  => 1,
        PrimitiveType.UWord  => 2,
        PrimitiveType.SWord  => 2,
        PrimitiveType.UDWord => 4,
        PrimitiveType.SDWord => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
