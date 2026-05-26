using Amber.Interpreter.Runtime;

public class ArithmeticTests
{
    [Fact] public void ByteAdd()        { var i = Fixture.Run("byte x = 10 + 5;");           Assert.Equal(15,   i.Byte("x")); }
    [Fact] public void ByteOverflow()   { var i = Fixture.Run("byte x = 200 + 100;");         Assert.Equal(44,   i.Byte("x")); }  // wraps mod 256
    [Fact] public void WordSub()        { var i = Fixture.Run("word x = 1000 - 250;");        Assert.Equal(750,  i.Word("x")); }
    [Fact] public void LongMul()        { var i = Fixture.Run("long x = 1000 * 1000;");       Assert.Equal(1_000_000u, i.ULong("x")); }
    [Fact] public void IntDiv()         { var i = Fixture.Run("long x = 17 / 5;");            Assert.Equal(3u,   i.ULong("x")); }
    [Fact] public void Modulo()         { var i = Fixture.Run("long x = 17 % 5;");            Assert.Equal(2u,   i.ULong("x")); }
    [Fact] public void UnaryNegate()    { var i = Fixture.Run("byte x = 5; byte y = -x;");   Assert.Equal(251,  i.Byte("y")); }  // -5 mod 256 = 251
    [Fact] public void Precedence()     { var i = Fixture.Run("long x = 2 + 3 * 4;");        Assert.Equal(14u,  i.ULong("x")); }
    [Fact] public void Parens()         { var i = Fixture.Run("long x = (2 + 3) * 4;");      Assert.Equal(20u,  i.ULong("x")); }
    [Fact] public void Assignment()     { var i = Fixture.Run("long x = 1; x = x + 9;");     Assert.Equal(10u,  i.ULong("x")); }
    [Fact] public void HexLiteral()     { var i = Fixture.Run("long x = 0xFF;");              Assert.Equal(255u, i.ULong("x")); }
    [Fact] public void DivByZero()      => Assert.Throws<AmberRuntimeException>(() => Fixture.Run("long x = 1 / 0;"));
    [Fact] public void ModByZero()      => Assert.Throws<AmberRuntimeException>(() => Fixture.Run("long x = 1 % 0;"));
    [Fact] public void RedeclareVar()   => Assert.Throws<AmberRuntimeException>(() => Fixture.Run("byte x = 1; byte x = 2;"));
    [Fact] public void UndefinedVar()   => Assert.Throws<AmberRuntimeException>(() => Fixture.Run("long x = y;"));
}
