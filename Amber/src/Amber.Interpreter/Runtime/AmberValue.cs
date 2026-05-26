namespace Amber.Interpreter.Runtime;

using Amber.Interpreter.AST;

/// <summary>
/// An Amber runtime value. Numeric types are stored as a raw Int64 and masked to their
/// declared width on every operation so overflow behavior is well-defined.
/// </summary>
public readonly record struct AmberValue(AmberType Type, long RawValue)
{
    public static AmberValue FromByte(long v) => new(AmberType.Byte, v & 0xFF);
    public static AmberValue FromWord(long v) => new(AmberType.Word, v & 0xFFFF);
    public static AmberValue FromLong(long v) => new(AmberType.Long, (long)((ulong)v & 0xFFFF_FFFF));
    public static AmberValue FromBool(bool v)  => new(AmberType.Bool, v ? 1 : 0);

    public static AmberValue Default(AmberType type) => type switch
    {
        AmberType.Byte => FromByte(0),
        AmberType.Word => FromWord(0),
        AmberType.Long => FromLong(0),
        AmberType.Bool => FromBool(false),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public AmberValue Coerce(AmberType target) => target switch
    {
        AmberType.Byte => FromByte(RawValue),
        AmberType.Word => FromWord(RawValue),
        AmberType.Long => FromLong(RawValue),
        AmberType.Bool => FromBool(RawValue != 0),
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    public byte   AsByte => (byte)(RawValue & 0xFF);
    public ushort AsWord => (ushort)(RawValue & 0xFFFF);
    public uint   AsLong => (uint)((ulong)RawValue & 0xFFFF_FFFF);
    public bool   AsBool => RawValue != 0;

    public override string ToString() => Type switch
    {
        AmberType.Bool => AsBool ? "true" : "false",
        AmberType.Byte => $"0x{AsByte:X2}  ({AsByte})",
        AmberType.Word => $"0x{AsWord:X4}  ({AsWord})",
        AmberType.Long => $"0x{AsLong:X8}  ({AsLong})",
        _ => RawValue.ToString()
    };
}
