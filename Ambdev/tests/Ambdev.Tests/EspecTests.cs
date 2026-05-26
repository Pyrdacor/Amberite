using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Runtime;

public class EspecTests
{
    // Base etype used by most tests
    private const string Base =
        "etype[12, Condition]: - 0: byte x - 1: byte y - 2: word something ";

    // ── Basic parsing ─────────────────────────────────────────────────────────

    [Fact]
    public void EspecEtypeIndexAndName()
    {
        var i  = Fixture.Run(Base + "espec[12, SpecialCondition]:");
        var es = i.Espec("SpecialCondition");
        Assert.Equal(12, es.EtypeIndex);
        Assert.Equal("SpecialCondition", es.Name);
    }

    [Fact]
    public void EspecNoItems()
    {
        var i = Fixture.Run(Base + "espec[12, Empty]:");
        Assert.Empty(i.Espec("Empty").Conditions);
        Assert.Empty(i.Espec("Empty").Fields);
    }

    // ── When conditions ───────────────────────────────────────────────────────

    [Fact]
    public void EspecWhenEq()
    {
        var i  = Fixture.Run(Base + "espec[12, S]: - when x == 5");
        var c  = Assert.IsType<CompareConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.Equal("x",       c.FieldName);
        Assert.Equal(CompareOp.Eq, c.Op);
        Assert.Equal(5L,        c.Value);
    }

    [Fact]
    public void EspecWhenNeq()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - when x != 0");
        var c = Assert.IsType<CompareConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.Equal(CompareOp.Neq, c.Op);
    }

    [Fact]
    public void EspecWhenLt()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - when y < 10");
        var c = Assert.IsType<CompareConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.Equal(CompareOp.Lt, c.Op);
        Assert.Equal(10L, c.Value);
    }

    [Fact]
    public void EspecWhenGt()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - when y > 3");
        var c = Assert.IsType<CompareConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.Equal(CompareOp.Gt, c.Op);
    }

    [Fact]
    public void EspecWhenLte()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - when y <= 10");
        var c = Assert.IsType<CompareConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.Equal(CompareOp.Lte, c.Op);
    }

    [Fact]
    public void EspecWhenGte()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - when y >= 3");
        var c = Assert.IsType<CompareConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.Equal(CompareOp.Gte, c.Op);
    }

    [Fact]
    public void EspecWhenHexLiteral()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - when x == 0xFF");
        var c = Assert.IsType<CompareConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.Equal(255L, c.Value);
    }

    [Fact]
    public void EspecWhenAndCondition()
    {
        var i    = Fixture.Run(Base + "espec[12, S]: - when x == 5 and y < 10");
        var cond = Assert.IsType<AndConditionInfo>(i.Espec("S").Conditions[0]);
        var left  = Assert.IsType<CompareConditionInfo>(cond.Left);
        var right = Assert.IsType<CompareConditionInfo>(cond.Right);
        Assert.Equal("x", left.FieldName);
        Assert.Equal("y", right.FieldName);
    }

    [Fact]
    public void EspecWhenOrCondition()
    {
        var i    = Fixture.Run(Base + "espec[12, S]: - when x == 5 or x == 7");
        var cond = Assert.IsType<OrConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.IsType<CompareConditionInfo>(cond.Left);
        Assert.IsType<CompareConditionInfo>(cond.Right);
    }

    [Fact]
    public void EspecAndBindsTighterThanOr()
    {
        // "a or b and c"  →  "a or (b and c)"  because 'and' has higher precedence
        var i    = Fixture.Run(Base + "espec[12, S]: - when x == 1 or y == 2 and x == 3");
        var cond = Assert.IsType<OrConditionInfo>(i.Espec("S").Conditions[0]);
        Assert.IsType<CompareConditionInfo>(cond.Left);
        Assert.IsType<AndConditionInfo>(cond.Right);
    }

    [Fact]
    public void EspecMultipleWhenClauses()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - when x == 5 - when y < 10");
        Assert.Equal(2, i.Espec("S").Conditions.Count);
    }

    // ── Fields ────────────────────────────────────────────────────────────────

    [Fact]
    public void EspecFieldOffset()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - 2: byte foo");
        Assert.Equal(2, i.Espec("S").Fields[0].Offset);
    }

    [Fact]
    public void EspecFieldType()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - 2: byte foo");
        Assert.Equal(new PrimitiveTypeRef(PrimType.Byte), i.Espec("S").Fields[0].Type);
    }

    [Fact]
    public void EspecFieldName()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - 2: byte foo");
        Assert.Equal("foo", i.Espec("S").Fields[0].Name);
    }

    [Fact]
    public void EspecFieldOptional()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - 3: byte? bar = 0");
        var f = i.Espec("S").Fields[0];
        Assert.True(f.IsOptional);
        Assert.Equal(0L, f.DefaultValue);
    }

    [Fact]
    public void EspecFieldRange()
    {
        var i = Fixture.Run(Base + "espec[12, S]: - 2: byte foo [0..7]");
        var range = Assert.IsType<ContinuousRange>(i.Espec("S").Fields[0].Range);
        Assert.Equal(0L, range.Min);
        Assert.Equal(7L, range.Max);
    }

    [Fact]
    public void EspecMixedWhenAndFields()
    {
        var i = Fixture.Run(Base +
            "espec[12, SpecialCondition]: " +
            "- when x == 5 and y < 10 " +
            "- 2: byte foo [0..7] " +
            "- 3: byte? bar = 0");
        var es = i.Espec("SpecialCondition");
        Assert.Single(es.Conditions);
        Assert.Equal(2, es.Fields.Count);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public void EspecUnknownEtypeThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("espec[99, S]:"));

    [Fact]
    public void EspecWhenUnknownFieldThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run(Base + "espec[12, S]: - when z == 1"));

    [Fact]
    public void EspecWhenUnknownFieldInAndThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run(Base + "espec[12, S]: - when x == 1 and z == 2"));

    [Fact]
    public void DuplicateEspecNameThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run(Base + "espec[12, S]: espec[12, S]:"));
}
