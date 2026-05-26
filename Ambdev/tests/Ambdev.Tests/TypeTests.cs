using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Runtime;

public class TypeTests
{
    // ── enum ─────────────────────────────────────────────────────────────────

    [Fact]
    public void EnumAutoIncrement()
    {
        var i = Fixture.Run("enum Color : byte { Red, Green, Blue }");
        Assert.Equal([("Red", 0L), ("Green", 1L), ("Blue", 2L)], i.Registry.GetEnum("Color").Members);
    }

    [Fact]
    public void EnumExplicitValue()
    {
        var i = Fixture.Run("enum Color : byte { Red, Green, White = 15 }");
        Assert.Equal(15L, i.Registry.GetEnum("Color").Members.First(m => m.Name == "White").Value);
    }

    [Fact]
    public void EnumResumesAfterExplicit()
    {
        var i = Fixture.Run("enum E : byte { A = 10, B, C }");
        Assert.Equal([("A", 10L), ("B", 11L), ("C", 12L)], i.Registry.GetEnum("E").Members);
    }

    [Fact]
    public void EnumHexValue()
    {
        var i = Fixture.Run("enum E : word { A = 0x100, B }");
        var m = i.Registry.GetEnum("E").Members;
        Assert.Equal(0x100L, m[0].Value);
        Assert.Equal(0x101L, m[1].Value);
    }

    [Fact]
    public void EnumBaseType()
    {
        var i = Fixture.Run("enum E : word { X }");
        Assert.Equal(PrimType.Word, i.Registry.GetEnum("E").BaseType);
    }

    [Fact]
    public void DuplicateEnumThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("enum E : byte { A } enum E : byte { B }"));

    // ── bitfield ─────────────────────────────────────────────────────────────

    [Fact]
    public void BitfieldPowersOfTwo()
    {
        var i = Fixture.Run("bitfield F : byte { A, B, C, D }");
        Assert.Equal([("None", 0L), ("A", 1L), ("B", 2L), ("C", 4L), ("D", 8L)], i.Registry.GetBitfield("F").Members);
    }

    [Fact]
    public void BitfieldBaseType()
    {
        var i = Fixture.Run("bitfield F : long { X }");
        Assert.Equal(PrimType.Long, i.Registry.GetBitfield("F").BaseType);
    }

    [Fact]
    public void BitfieldExplicitValue()
    {
        var i = Fixture.Run("bitfield F : byte { A = 0x10 }");
        Assert.Equal(0x10L, i.Registry.GetBitfield("F").Members[1].Value);
    }

    [Fact]
    public void BitfieldResumesAfterExplicit()
    {
        var i = Fixture.Run("bitfield F : byte { A, B = 0x04, C }");
        var m = i.Registry.GetBitfield("F").Members;
        Assert.Equal([("None", 0L), ("A", 0x01L), ("B", 0x04L), ("C", 0x08L)], m);
    }

    [Fact]
    public void BitfieldExplicitNonPowerOfTwo()
    {
        var i = Fixture.Run("bitfield F : byte { A = 0x03, B }");
        var m = i.Registry.GetBitfield("F").Members;
        Assert.Equal(0x03L, m[1].Value);
        Assert.Equal(0x04L, m[2].Value);
    }

    [Fact]
    public void BitfieldAllExplicit()
    {
        var i = Fixture.Run("bitfield F : word { Read = 0x001, Write = 0x002, Exec = 0x100 }");
        var m = i.Registry.GetBitfield("F").Members;
        Assert.Equal([("None", 0L), ("Read", 0x001L), ("Write", 0x002L), ("Exec", 0x100L)], m);
    }

    [Fact]
    public void BitfieldOrExpression()
    {
        var i = Fixture.Run("bitfield F : byte { Read = 0x01, Write, ReadWrite = Read | Write }");
        var m = i.Registry.GetBitfield("F").Members;
        Assert.Equal(0x03L, m.First(x => x.Name == "ReadWrite").Value);
    }

    [Fact]
    public void BitfieldExplicitZeroSuppressesNone()
    {
        var i = Fixture.Run("bitfield F : byte { Nothing = 0, A }");
        Assert.DoesNotContain(i.Registry.GetBitfield("F").Members, m => m.Name == "None");
    }

    [Fact]
    public void BitfieldExplicitNoneNameSuppressesAutoNone()
    {
        var i = Fixture.Run("bitfield F : byte { None = 0, A }");
        Assert.Single(i.Registry.GetBitfield("F").Members, m => m.Name == "None");
    }

    [Fact]
    public void DuplicateBitfieldThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("bitfield F : byte { A } bitfield F : byte { B }"));

    // ── used in etype ─────────────────────────────────────────────────────────

    [Fact]
    public void BitfieldUsedAsEtypeFieldType()
    {
        var i = Fixture.Run(
            "bitfield Flags : byte { A, B } " +
            "etype[1, E]: - 0: Flags flags");
        Assert.Equal(new NamedTypeRef("Flags"), i.Etype(1).Fields[0].Type);
    }
}
