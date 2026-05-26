public class BitOpTests
{
    [Fact] public void And()            { var i = Fixture.Run("byte x = 0xFF & 0x0F;");       Assert.Equal(0x0F, i.Byte("x")); }
    [Fact] public void Or()             { var i = Fixture.Run("byte x = 0xF0 | 0x0F;");       Assert.Equal(0xFF, i.Byte("x")); }
    [Fact] public void Xor()            { var i = Fixture.Run("byte x = 0xFF ^ 0x0F;");       Assert.Equal(0xF0, i.Byte("x")); }
    [Fact] public void BitwiseNot()     { var i = Fixture.Run("byte x = ~0x0F;");             Assert.Equal(0xF0, i.Byte("x")); }
    [Fact] public void ShiftLeft()      { var i = Fixture.Run("byte x = 1 << 3;");            Assert.Equal(8,    i.Byte("x")); }
    [Fact] public void ShiftRight()     { var i = Fixture.Run("byte x = 0x80 >> 4;");         Assert.Equal(8,    i.Byte("x")); }
    [Fact] public void WordAnd()        { var i = Fixture.Run("word x = 0x1234 & 0x00FF;");   Assert.Equal(0x0034, i.Word("x")); }
    [Fact] public void LogicalNotTrue() { var i = Fixture.Run("bool x = !false;");            Assert.True(i.Bool("x")); }
    [Fact] public void LogicalNotFalse(){ var i = Fixture.Run("bool x = !true;");             Assert.False(i.Bool("x")); }
    [Fact] public void ShiftWordLeft()  { var i = Fixture.Run("word x = 0x0001 << 8;");       Assert.Equal(0x0100, i.Word("x")); }
}
