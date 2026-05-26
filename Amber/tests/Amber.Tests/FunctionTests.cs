using Amber.Interpreter.AST;
using Amber.Interpreter.Runtime;

public class FunctionTests
{
    // ── Declaration & parameter parsing ──────────────────────────────────────

    [Fact]
    public void SimpleFunction()
    {
        var i = Fixture.Run("function add(in d0: byte x, in d1: byte y, out d0: word result) { }");
        var fn = i.Types.Get<FunctionInfo>("add");
        Assert.Equal("add", fn.Name);
        Assert.Equal(3, fn.Parameters.Count);
    }

    [Fact]
    public void ParamDirections()
    {
        var i = Fixture.Run(
            "function f(in d0: byte a, out d1: word b, inout d2: long c) { }");
        var parms = i.Types.Get<FunctionInfo>("f").Parameters;
        Assert.Equal(ParamDirection.In,    parms[0].Direction);
        Assert.Equal(ParamDirection.Out,   parms[1].Direction);
        Assert.Equal(ParamDirection.InOut, parms[2].Direction);
    }

    [Fact]
    public void ParamRegisters()
    {
        var i = Fixture.Run(
            "function f(in d0: byte x, in a0: Ptr<byte> p) { }");
        var parms = i.Types.Get<FunctionInfo>("f").Parameters;
        Assert.Equal("d0", parms[0].Register);
        Assert.Equal("a0", parms[1].Register);
    }

    [Fact]
    public void ParamNames()
    {
        var i = Fixture.Run(
            "function f(in d0: byte alpha, out d1: word beta) { }");
        var parms = i.Types.Get<FunctionInfo>("f").Parameters;
        Assert.Equal("alpha", parms[0].Name);
        Assert.Equal("beta",  parms[1].Name);
    }

    [Fact]
    public void ParamTypes()
    {
        var i = Fixture.Run(
            "function f(in d0: byte a, in d1: word b, in d2: long c, in d3: bool d) { }");
        var parms = i.Types.Get<FunctionInfo>("f").Parameters;
        Assert.Equal(new PrimitiveTypeRef(AmberType.Byte), parms[0].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Word), parms[1].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Long), parms[2].Type);
        Assert.Equal(new PrimitiveTypeRef(AmberType.Bool), parms[3].Type);
    }

    [Fact]
    public void PtrParam()
    {
        var i = Fixture.Run(
            "function f(in a0: Ptr<long> buf) { }");
        var param = i.Types.Get<FunctionInfo>("f").Parameters[0];
        Assert.Equal(new PointerTypeRef(new PrimitiveTypeRef(AmberType.Long)), param.Type);
    }

    [Fact]
    public void NamedTypeParam()
    {
        var i = Fixture.Run(
            "struct Vec2 { word x; word y; } " +
            "function f(in a0: Ptr<Vec2> v) { }");
        var param = i.Types.Get<FunctionInfo>("f").Parameters[0];
        Assert.Equal(new PointerTypeRef(new UserTypeRef("Vec2")), param.Type);
    }

    [Fact]
    public void FunctionWithBody()
    {
        var i = Fixture.Run(
            "function inc(in d0: byte x, out d0: byte result) { byte r = x; }");
        var fn = i.Types.Get<FunctionInfo>("inc");
        Assert.Single(fn.Body);
    }

    [Fact]
    public void AllDataRegisters()
    {
        // d0–d7 are all valid for value types
        for (int r = 0; r <= 7; r++)
            Fixture.Run($"function f(in d{r}: byte x) {{ }}");
    }

    [Fact]
    public void AllAddressRegisters()
    {
        // a0–a6 are valid (a7 is the stack pointer, excluded)
        for (int r = 0; r <= 6; r++)
            Fixture.Run($"function f(in a{r}: Ptr<byte> p) {{ }}");
    }

    [Fact]
    public void InvalidRegister_a7()
        => Assert.Throws<AmberRuntimeException>(() =>
            Fixture.Run("function f(in a7: Ptr<byte> p) { }"));

    [Fact]
    public void InvalidRegister_d8()
        => Assert.Throws<AmberRuntimeException>(() =>
            Fixture.Run("function f(in d8: byte x) { }"));

    [Fact]
    public void NoParams()
    {
        var i = Fixture.Run("function noop() { }");
        Assert.Empty(i.Types.Get<FunctionInfo>("noop").Parameters);
    }

    [Fact]
    public void DuplicateFunctionName()
        => Assert.Throws<AmberRuntimeException>(() =>
            Fixture.Run("function f() { } function f() { }"));
}
