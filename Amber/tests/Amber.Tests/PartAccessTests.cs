using Amber.Interpreter.Runtime;

public class PartAccessTests
{
    const string Source = "long foo = 0x12345678;";

    [Fact] public void LongW0()  { var i = Fixture.Run($"{Source} word x = foo.w0;"); Assert.Equal(0x5678, i.Word("x")); }
    [Fact] public void LongW1()  { var i = Fixture.Run($"{Source} word x = foo.w1;"); Assert.Equal(0x1234, i.Word("x")); }
    [Fact] public void LongB0()  { var i = Fixture.Run($"{Source} byte x = foo.b0;"); Assert.Equal(0x78,   i.Byte("x")); }
    [Fact] public void LongB1()  { var i = Fixture.Run($"{Source} byte x = foo.b1;"); Assert.Equal(0x56,   i.Byte("x")); }
    [Fact] public void LongB2()  { var i = Fixture.Run($"{Source} byte x = foo.b2;"); Assert.Equal(0x34,   i.Byte("x")); }
    [Fact] public void LongB3()  { var i = Fixture.Run($"{Source} byte x = foo.b3;"); Assert.Equal(0x12,   i.Byte("x")); }

    [Fact] public void WordB0()  { var i = Fixture.Run("word w = 0xABCD; byte x = w.b0;"); Assert.Equal(0xCD, i.Byte("x")); }
    [Fact] public void WordB1()  { var i = Fixture.Run("word w = 0xABCD; byte x = w.b1;"); Assert.Equal(0xAB, i.Byte("x")); }

    [Fact] public void Chained() { var i = Fixture.Run($"{Source} byte x = foo.w0.b1;");   Assert.Equal(0x56, i.Byte("x")); }

    [Fact] public void InvalidPartOnByte()
        => Assert.Throws<AmberRuntimeException>(() => Fixture.Run("byte b = 1; byte x = b.b0;"));

    [Fact] public void InvalidPartOnLong()
        => Assert.Throws<AmberRuntimeException>(() => Fixture.Run("long l = 1; word x = l.w2;"));
}
