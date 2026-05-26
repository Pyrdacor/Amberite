using Amber.Interpreter.AST;
using Amber.Interpreter.Runtime;

public class TypeSystemTests
{
    // ── enum ─────────────────────────────────────────────────────────────────

    [Fact]
    public void EnumAutoIncrement()
    {
        var i = Fixture.Run("enum Color : byte { Red, Green, Blue }");
        var e = i.Enum("Color");
        Assert.Equal([("Red", 0L), ("Green", 1L), ("Blue", 2L)], e.Members);
    }

    [Fact]
    public void EnumExplicitValue()
    {
        var i = Fixture.Run("enum Color : byte { Red, Green, Blue, White = 15 }");
        var e = i.Enum("Color");
        Assert.Equal(15L, e.Members.First(m => m.Name == "White").Value);
    }

    [Fact]
    public void EnumResumesAfterExplicit()
    {
        var i = Fixture.Run("enum E : byte { A = 10, B, C }");
        var e = i.Enum("E");
        Assert.Equal([("A", 10L), ("B", 11L), ("C", 12L)], e.Members);
    }

    [Fact]
    public void EnumHexValue()
    {
        var i = Fixture.Run("enum E : word { A = 0x100, B }");
        var e = i.Enum("E");
        Assert.Equal(0x100L, e.Members[0].Value);
        Assert.Equal(0x101L, e.Members[1].Value);
    }

    [Fact]
    public void EnumBaseType()
    {
        var i = Fixture.Run("enum E : word { X }");
        Assert.Equal(AmberType.Word, i.Enum("E").BaseType);
    }

    // ── bitfield ─────────────────────────────────────────────────────────────

    [Fact]
    public void BitfieldPowersOfTwo()
    {
        var i = Fixture.Run("bitfield F : byte { A, B, C, D }");
        var b = i.Bitfield("F");
        Assert.Equal([("A", 1L), ("B", 2L), ("C", 4L), ("D", 8L)], b.Members);
    }

    [Fact]
    public void BitfieldBaseType()
    {
        var i = Fixture.Run("bitfield F : long { X }");
        Assert.Equal(AmberType.Long, i.Bitfield("F").BaseType);
    }

    [Fact]
    public void BitfieldExplicitValue()
    {
        var i = Fixture.Run("bitfield F : byte { A = 0x10 }");
        Assert.Equal(0x10L, i.Bitfield("F").Members[0].Value);
    }

    [Fact]
    public void BitfieldResumesAfterExplicit()
    {
        // After 0x04 the next auto bit should be 0x08
        var i = Fixture.Run("bitfield F : byte { A, B = 0x04, C }");
        var m = i.Bitfield("F").Members;
        Assert.Equal([("A", 0x01L), ("B", 0x04L), ("C", 0x08L)], m);
    }

    [Fact]
    public void BitfieldExplicitNonPowerOfTwo()
    {
        // Explicit value need not be a power of 2; auto resumes from next power of 2 above it
        var i = Fixture.Run("bitfield F : byte { A = 0x03, B }");
        var m = i.Bitfield("F").Members;
        Assert.Equal(0x03L, m[0].Value);
        Assert.Equal(0x04L, m[1].Value);  // next power of 2 above 3
    }

    [Fact]
    public void BitfieldAllExplicit()
    {
        var i = Fixture.Run("bitfield F : word { Read = 0x001, Write = 0x002, Exec = 0x100 }");
        var m = i.Bitfield("F").Members;
        Assert.Equal([("Read", 0x001L), ("Write", 0x002L), ("Exec", 0x100L)], m);
    }

    [Fact]
    public void BitfieldHexAndDecimalMixed()
    {
        var i = Fixture.Run("bitfield F : byte { A = 1, B, C = 0x10, D }");
        var m = i.Bitfield("F").Members;
        Assert.Equal([("A", 1L), ("B", 2L), ("C", 0x10L), ("D", 0x20L)], m);
    }

    // ── struct ────────────────────────────────────────────────────────────────

    [Fact]
    public void StructFieldCount()
    {
        var i = Fixture.Run("struct S { byte a; word b; long c; }");
        Assert.Equal(3, i.Struct("S").Fields.Count);
    }

    [Fact]
    public void StructFieldTypes()
    {
        var i = Fixture.Run("struct S { byte a; word b; long c; bool d; }");
        var f = i.Struct("S").Fields;
        Assert.Equal(new PrimitiveTypeRef(AmberType.Byte), f[0].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Word), f[1].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Long), f[2].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Bool), f[3].Type);
    }

    [Fact]
    public void StructNamedField()
    {
        var i = Fixture.Run("enum Color : byte { R } struct S { Color c; }");
        var field = i.Struct("S").Fields[0];
        Assert.Equal(new UserTypeRef("Color"), field.Type);
    }

    [Fact]
    public void StructPointerFieldPtrSyntax()
    {
        var i = Fixture.Run("struct Node { Ptr<Node> next; }");
        Assert.Equal(new PointerTypeRef(new UserTypeRef("Node")), i.Struct("Node").Fields[0].Type);
    }

    [Fact]
    public void StructPtrPrimitive()
    {
        var i = Fixture.Run("struct S { Ptr<long> addr; }");
        Assert.Equal(new PointerTypeRef(new PrimitiveTypeRef(AmberType.Long)),
                     i.Struct("S").Fields[0].Type);
    }

    [Fact]
    public void StructArrayField()
    {
        var i = Fixture.Run("struct S { byte data[32]; }");
        var field = i.Struct("S").Fields[0];
        Assert.Equal(32L, field.ArraySize);
    }

    [Fact]
    public void DuplicateTypeName()
        => Assert.Throws<AmberRuntimeException>(() =>
            Fixture.Run("enum E : byte { A } enum E : byte { B }"));

    [Fact]
    public void ParseError()
        => Assert.Throws<AmberParseException>(() => Fixture.Run("enum : byte { }"));

    // ── struct import ─────────────────────────────────────────────────────────

    [Fact]
    public void StructImportFlattensFields()
    {
        var i = Fixture.Run(
            "struct Inner { byte x; word y; } " +
            "struct Outer { long a; import Inner; bool b; }");
        var f = i.Struct("Outer").Fields;
        Assert.Equal(4, f.Count);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Long), f[0].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Byte), f[1].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Word), f[2].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Bool), f[3].Type);
    }

    [Fact]
    public void StructImportPreservesFieldNames()
    {
        var i = Fixture.Run(
            "struct Inner { byte foo; word bar; } " +
            "struct Outer { import Inner; }");
        var f = i.Struct("Outer").Fields;
        Assert.Equal("foo", f[0].Name);
        Assert.Equal("bar", f[1].Name);
    }

    [Fact]
    public void StructMultipleImports()
    {
        var i = Fixture.Run(
            "struct A { byte a1; } " +
            "struct B { word b1; } " +
            "struct C { import A; import B; }");
        var f = i.Struct("C").Fields;
        Assert.Equal(2, f.Count);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Byte), f[0].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Word), f[1].Type);
    }

    [Fact]
    public void StructTransitiveImport()
    {
        var i = Fixture.Run(
            "struct A { byte a; } " +
            "struct B { import A; word b; } " +
            "struct C { import B; long c; }");
        var f = i.Struct("C").Fields;
        Assert.Equal(3, f.Count);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Byte), f[0].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Word), f[1].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Long), f[2].Type);
    }

    [Fact]
    public void StructImportUnknownTypeThrows()
        => Assert.Throws<AmberRuntimeException>(() =>
            Fixture.Run("struct S { import DoesNotExist; }"));

    [Fact]
    public void StructImportNonStructTypeThrows()
        => Assert.Throws<AmberRuntimeException>(() =>
            Fixture.Run("enum E : byte { A } struct S { import E; }"));
}
