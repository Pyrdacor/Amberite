using Ambdev.Interpreter.Runtime;

public class ConstTests
{
    // ── Basic declaration ─────────────────────────────────────────────────────

    [Fact]
    public void ConstIntegerValue()
    {
        var i = Fixture.Run("const MY_CONST = 42");
        Assert.Equal(42L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstHexValue()
    {
        var i = Fixture.Run("const MY_CONST = 0xFF");
        Assert.Equal(255L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstBoolTrueValue()
    {
        var i = Fixture.Run("const MY_FLAG = true");
        Assert.Equal(1L, i.Registry.GetConstant("MY_FLAG").Value);
    }

    [Fact]
    public void ConstBoolFalseValue()
    {
        var i = Fixture.Run("const MY_FLAG = false");
        Assert.Equal(0L, i.Registry.GetConstant("MY_FLAG").Value);
    }

    [Fact]
    public void ConstWithSemicolon()
    {
        var i = Fixture.Run("const MY_CONST = 7;");
        Assert.Equal(7L, i.Registry.GetConstant("MY_CONST").Value);
    }

    // ── Arithmetic expressions ────────────────────────────────────────────────

    [Fact]
    public void ConstAddition()
    {
        var i = Fixture.Run("const MY_CONST = 3 + 4");
        Assert.Equal(7L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstSubtraction()
    {
        var i = Fixture.Run("const MY_CONST = 10 - 3");
        Assert.Equal(7L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstMultiplication()
    {
        var i = Fixture.Run("const MY_CONST = 3 * 4");
        Assert.Equal(12L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstDivision()
    {
        var i = Fixture.Run("const MY_CONST = 10 / 2");
        Assert.Equal(5L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstMulBindsTighterThanAdd()
    {
        // 2 + 3 * 4 = 2 + 12 = 14, not (2+3)*4 = 20
        var i = Fixture.Run("const MY_CONST = 2 + 3 * 4");
        Assert.Equal(14L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstParenOverridesPrecedence()
    {
        var i = Fixture.Run("const MY_CONST = (2 + 3) * 4");
        Assert.Equal(20L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstUnaryNegation()
    {
        var i = Fixture.Run("const MY_CONST = 0 - 5");
        Assert.Equal(-5L, i.Registry.GetConstant("MY_CONST").Value);
    }

    [Fact]
    public void ConstComplexExpression()
    {
        var i = Fixture.Run("const MY_CONST = (10 + 5) * 2 / 3");
        Assert.Equal(10L, i.Registry.GetConstant("MY_CONST").Value);
    }

    // ── Constant references ───────────────────────────────────────────────────

    [Fact]
    public void ConstReferenceOtherConst()
    {
        var i = Fixture.Run("const BASE = 10 const DERIVED = BASE + 5");
        Assert.Equal(15L, i.Registry.GetConstant("DERIVED").Value);
    }

    [Fact]
    public void ConstChainedReferences()
    {
        var i = Fixture.Run("const A = 2 const B = A * 3 const C = B + A");
        Assert.Equal(8L, i.Registry.GetConstant("C").Value);
    }

    // ── Usage in enum members ─────────────────────────────────────────────────

    [Fact]
    public void ConstUsedInEnumMemberValue()
    {
        var i = Fixture.Run(
            "const BASE = 10 " +
            "enum E : byte { A = BASE, B }");
        var m = i.Registry.GetEnum("E").Members;
        Assert.Equal(10L, m.First(x => x.Name == "A").Value);
        Assert.Equal(11L, m.First(x => x.Name == "B").Value);
    }

    // ── Usage in bitfield members ─────────────────────────────────────────────

    [Fact]
    public void ConstUsedInBitfieldMemberValue()
    {
        var i = Fixture.Run(
            "const MY_FLAG = 0x04 " +
            "bitfield F : byte { Read = 0x01, Write, Execute = MY_FLAG }");
        Assert.Equal(0x04L, i.Registry.GetBitfield("F").Members.First(m => m.Name == "Execute").Value);
    }

    // ── Usage as etype field default ──────────────────────────────────────────

    [Fact]
    public void ConstUsedAsEtypeFieldDefault()
    {
        var i = Fixture.Run(
            "const DEFAULT_STEPS = 3 " +
            "etype[1, E]: - 0: byte? steps = DEFAULT_STEPS");
        Assert.Equal(3L, i.Etype(1).Fields[0].DefaultValue);
    }

    // ── Usage in event field values ───────────────────────────────────────────

    [Fact]
    public void ConstUsedInEventFieldValue()
    {
        var i = Fixture.Run(
            "const HERO_ID = 1 " +
            "etype[1, Et]: - 0: byte actorId " +
            "event[1, Ev] = etype[1] - actorId: HERO_ID");
        Assert.Equal(1L, i.Event(1).Fields[0].Value);
    }

    // ── Validation errors ─────────────────────────────────────────────────────

    [Fact]
    public void ConstNameTrailingUnderscoreThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("const MY_CONST_ = 5"));

    [Fact]
    public void ConstUnknownReferenceThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("const MY_CONST = UNKNOWN_REF"));

    [Fact]
    public void ConstDivisionByZeroThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("const MY_CONST = 10 / 0"));

    [Fact]
    public void ConstDuplicateNameThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("const MY_CONST = 1 const MY_CONST = 2"));

    [Fact]
    public void ConstEnumValueDoesNotFitTypeThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("const BIG = 300 enum E : byte { A = BIG }"));

    [Fact]
    public void ConstEtypeDefaultDoesNotFitTypeThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("const BIG = 300 etype[1, E]: - 0: byte? x = BIG"));
}
