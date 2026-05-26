using Amber.Interpreter.Runtime;

/// Thin wrapper around AmberRunner for use in tests.
internal static class Fixture
{
    internal static AmberInterpreter Run(string source)
        => AmberRunner.Execute(source, "<test>");

    // ── Variable accessors ────────────────────────────────────────────────────

    internal static long   Long(this AmberInterpreter i, string name) => i.Environment.Get(name).RawValue;
    internal static uint   ULong(this AmberInterpreter i, string name) => i.Environment.Get(name).AsLong;
    internal static ushort Word(this AmberInterpreter i, string name) => i.Environment.Get(name).AsWord;
    internal static byte   Byte(this AmberInterpreter i, string name) => i.Environment.Get(name).AsByte;
    internal static bool   Bool(this AmberInterpreter i, string name) => i.Environment.Get(name).AsBool;

    // ── Type registry accessors ───────────────────────────────────────────────

    internal static EnumTypeInfo     Enum(this AmberInterpreter i, string name)
        => i.Types.Get<EnumTypeInfo>(name);

    internal static BitfieldTypeInfo Bitfield(this AmberInterpreter i, string name)
        => i.Types.Get<BitfieldTypeInfo>(name);

    internal static StructTypeInfo   Struct(this AmberInterpreter i, string name)
        => i.Types.Get<StructTypeInfo>(name);
}
