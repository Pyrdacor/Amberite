using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Runtime;

public class EtypeTests
{
    [Fact]
    public void EtypeIndexAndName()
    {
        var i = Fixture.Run("etype[12, Condition]:");
        var e = i.Etype(12);
        Assert.Equal(12, e.Index);
        Assert.Equal("Condition", e.Name);
    }

    [Fact]
    public void EtypeNoFields()
    {
        var i = Fixture.Run("etype[0, Empty]:");
        Assert.Empty(i.Etype(0).Fields);
    }

    [Fact]
    public void EtypeFieldCount()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte x - 1: word y");
        Assert.Equal(2, i.Etype(1).Fields.Count);
    }

    [Fact]
    public void EtypeFieldTypes()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte a - 1: word b - 2: long c");
        var f = i.Etype(1).Fields;
        Assert.Equal(new PrimitiveTypeRef(PrimType.Byte), f[0].Type);
        Assert.Equal(new PrimitiveTypeRef(PrimType.Word), f[1].Type);
        Assert.Equal(new PrimitiveTypeRef(PrimType.Long), f[2].Type);
    }

    [Fact]
    public void EtypeFieldOffset()
    {
        var i = Fixture.Run("etype[1, E]: - 4: byte x");
        Assert.Equal(4, i.Etype(1).Fields[0].Offset);
    }

    [Fact]
    public void EtypeFieldName()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte myField");
        Assert.Equal("myField", i.Etype(1).Fields[0].Name);
    }

    [Fact]
    public void EtypeOptionalField()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte? x");
        Assert.True(i.Etype(1).Fields[0].IsOptional);
    }

    [Fact]
    public void EtypeNonOptionalField()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte x");
        Assert.False(i.Etype(1).Fields[0].IsOptional);
    }

    [Fact]
    public void EtypeDefaultValue()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte? x = 5");
        Assert.Equal(5L, i.Etype(1).Fields[0].DefaultValue);
    }

    [Fact]
    public void EtypeHexDefaultValue()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte? x = 0xFF");
        Assert.Equal(255L, i.Etype(1).Fields[0].DefaultValue);
    }

    [Fact]
    public void EtypeNoDefaultValue()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte x");
        Assert.Null(i.Etype(1).Fields[0].DefaultValue);
    }

    [Fact]
    public void EtypeContinuousRange()
    {
        var i = Fixture.Run("etype[1, E]: - 0: word x [0..1023]");
        var range = Assert.IsType<ContinuousRange>(i.Etype(1).Fields[0].Range);
        Assert.Equal(0L, range.Min);
        Assert.Equal(1023L, range.Max);
    }

    [Fact]
    public void EtypeValueListRange()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte x [0, 1, 2, 7]");
        var range = Assert.IsType<ValueListRange>(i.Etype(1).Fields[0].Range);
        Assert.Equal([0L, 1L, 2L, 7L], range.Values);
    }

    [Fact]
    public void EtypeSingleValueRange()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte x [5]");
        var range = Assert.IsType<ValueListRange>(i.Etype(1).Fields[0].Range);
        Assert.Equal([5L], range.Values);
    }

    [Fact]
    public void EtypeNoRange()
    {
        var i = Fixture.Run("etype[1, E]: - 0: byte x");
        Assert.Null(i.Etype(1).Fields[0].Range);
    }

    [Fact]
    public void EtypeNamedFieldType()
    {
        var i = Fixture.Run(
            "enum Color : byte { Red, Green } " +
            "etype[1, E]: - 0: Color myColor");
        Assert.Equal(new NamedTypeRef("Color"), i.Etype(1).Fields[0].Type);
    }

    [Fact]
    public void EtypeBlockCommentBeforeDecl()
    {
        var i = Fixture.Run("/* Some description */ etype[1, E]: - 0: byte x");
        Assert.Equal("E", i.Etype(1).Name);
    }

    [Fact]
    public void DuplicateEtypeThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("etype[1, A]: etype[1, B]:"));

    [Fact]
    public void ParseError()
        => Assert.Throws<AmbdevParseException>(() =>
            Fixture.Run("etype[, Name]:"));
}
