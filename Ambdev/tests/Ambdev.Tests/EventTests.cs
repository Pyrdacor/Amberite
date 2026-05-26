using Ambdev.Interpreter.Runtime;

public class EventTests
{
    [Fact]
    public void EventIndexNameAndEtype()
    {
        var i = Fixture.Run(
            "etype[12, ET]: - 0: byte x " +
            "event[1, MyEvent] = etype[12] - x: 10");
        var ev = i.Event(1);
        Assert.Equal(1, ev.Index);
        Assert.Equal("MyEvent", ev.Name);
        Assert.Equal(12, ev.EtypeIndex);
    }

    [Fact]
    public void EventFieldValue()
    {
        var i = Fixture.Run(
            "etype[1, ET]: - 0: byte val " +
            "event[1, Ev] = etype[1] - val: 42");
        Assert.Equal(42L, i.Event(1).Fields[0].Value);
    }

    [Fact]
    public void EventFieldName()
    {
        var i = Fixture.Run(
            "etype[1, ET]: - 0: byte myField " +
            "event[1, Ev] = etype[1] - myField: 7");
        Assert.Equal("myField", i.Event(1).Fields[0].Name);
    }

    [Fact]
    public void EventHexFieldValue()
    {
        var i = Fixture.Run(
            "etype[1, ET]: - 0: word val " +
            "event[1, Ev] = etype[1] - val: 0xFF");
        Assert.Equal(255L, i.Event(1).Fields[0].Value);
    }

    [Fact]
    public void EventMultipleFields()
    {
        var i = Fixture.Run(
            "etype[1, ET]: - 0: byte x - 1: byte y " +
            "event[1, Ev] = etype[1] - x: 3 - y: 7");
        var f = i.Event(1).Fields;
        Assert.Equal(2, f.Count);
        Assert.Equal(3L, f[0].Value);
        Assert.Equal(7L, f[1].Value);
    }

    [Fact]
    public void EventMissingRequiredFieldThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run(
                "etype[1, ET]: - 0: byte x - 1: byte y " +
                "event[1, Ev] = etype[1] - x: 1"));

    [Fact]
    public void EventOptionalFieldCanBeOmitted()
    {
        var i = Fixture.Run(
            "etype[1, ET]: - 0: byte x - 1: byte? y = 0 " +
            "event[1, Ev] = etype[1] - x: 5");
        Assert.Single(i.Event(1).Fields);
    }

    [Fact]
    public void EventUnknownEtypeThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("event[1, Ev] = etype[99]"));

    [Fact]
    public void EventFieldQualifiedBitfieldValue()
    {
        var i = Fixture.Run(
            "bitfield Flags : byte { Read = 0x01, Write } " +
            "etype[1, ET]: - 0: Flags flags " +
            "event[1, Ev] = etype[1] - flags: Flags.Read");
        Assert.Equal(0x01L, i.Event(1).Fields[0].Value);
    }

    [Fact]
    public void EventFieldOrQualifiedBitfieldValues()
    {
        var i = Fixture.Run(
            "bitfield Flags : byte { Read = 0x01, Write } " +
            "etype[1, ET]: - 0: Flags flags " +
            "event[1, Ev] = etype[1] - flags: Flags.Read | Flags.Write");
        Assert.Equal(0x03L, i.Event(1).Fields[0].Value);
    }

    [Fact]
    public void EventFieldQualifiedEnumValue()
    {
        var i = Fixture.Run(
            "enum Dir : byte { North, South, East, West } " +
            "etype[1, ET]: - 0: Dir dir " +
            "event[1, Ev] = etype[1] - dir: Dir.South");
        Assert.Equal(1L, i.Event(1).Fields[0].Value);
    }

    [Fact]
    public void DuplicateEventThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run(
                "etype[1, ET]: " +
                "event[1, A] = etype[1] " +
                "event[1, B] = etype[1]"));
}
