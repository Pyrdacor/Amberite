using Ambdev.Interpreter.Compiler;
using Ambdev.Interpreter.Compiler.Backends;

/// <summary>
/// Tests for AmbdevCompiler + AmbermoonBinaryBackend.
///
/// Binary layout under test:
///   [0..1]   word  chainCount
///   [2..N]   word  per top-level chain: 0-based start-event index
///   [N..N+1] word  eventCount
///   then eventCount × 12-byte records: [type(1)] [data(9)] [next(2)]
/// </summary>
public class CompilerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static byte[] Compile(string aedSource, string aevSource)
        => AmbdevCompiler.Compile(aedSource, aevSource, new AmbermoonBinaryBackend());

    /// Returns the byte offset where event records begin.
    private static int EventsOffset(int chainCount) => 2 + chainCount * 2 + 2;

    /// Returns the byte offset of record <paramref name="binaryIdx"/> (0-based).
    private static int RecordOffset(int chainCount, int binaryIdx)
        => EventsOffset(chainCount) + binaryIdx * 12;

    private static ushort ReadWord(byte[] b, int off) => (ushort)((b[off] << 8) | b[off + 1]);

    // ── Empty input ───────────────────────────────────────────────────────────

    [Fact]
    public void EmptyInput_ProducesZeroChainCountAndZeroEventCount()
    {
        var bytes = Compile("", "");
        // 2 bytes chainCount + 2 bytes eventCount = 4 bytes total
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0, ReadWord(bytes, 0)); // chainCount
        Assert.Equal(0, ReadWord(bytes, 2)); // eventCount
    }

    // ── Type byte ─────────────────────────────────────────────────────────────

    [Fact]
    public void SingleEvent_TypeByteEqualsEtypeIndex()
    {
        var bytes = Compile(
            "etype[7, Foo]: - 0: byte x",
            "event[1, Ev] = etype[Foo] - x: 0");

        int rOff = RecordOffset(0, 0); // no chains
        Assert.Equal(7, bytes[rOff]); // type byte = etype index
    }

    // ── Field values ──────────────────────────────────────────────────────────

    [Fact]
    public void ByteFieldAtOffset0IsWritten()
    {
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x",
            "event[1, E] = etype[Ev] - x: 42");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(42, bytes[rOff + 1]); // data[0]
    }

    [Fact]
    public void WordFieldIsBigEndian()
    {
        var bytes = Compile(
            "etype[1, Ev]: - 0: word val",
            "event[1, E] = etype[Ev] - val: 0x1234");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(0x12, bytes[rOff + 1]); // data[0] = hi
        Assert.Equal(0x34, bytes[rOff + 2]); // data[1] = lo
    }

    [Fact]
    public void FieldAtNonZeroOffset()
    {
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte a - 3: byte b",
            "event[1, E] = etype[Ev] - a: 10 - b: 20");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(10, bytes[rOff + 1]); // data[0]
        Assert.Equal(20, bytes[rOff + 4]); // data[3]
    }

    [Fact]
    public void OptionalFieldDefaultIsWritten_WhenFieldOmitted()
    {
        // flags is optional with default 7; omitting it must still write 7
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x - 1: byte? flags = 7",
            "event[1, E] = etype[Ev] - x: 5");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(5, bytes[rOff + 1]); // data[0] = x
        Assert.Equal(7, bytes[rOff + 2]); // data[1] = flags (default)
    }

    [Fact]
    public void ExplicitFieldOverridesDefault()
    {
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x - 1: byte? flags = 7",
            "event[1, E] = etype[Ev] - x: 5 - flags: 3");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(3, bytes[rOff + 2]); // explicit 3 beats default 7
    }

    // ── No-continuation sentinel ──────────────────────────────────────────────

    [Fact]
    public void UnlinkedEvent_NextIs0xFFFF()
    {
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x",
            "event[1, E] = etype[Ev] - x: 0");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(0xFF, bytes[rOff + 10]);
        Assert.Equal(0xFF, bytes[rOff + 11]);
    }

    // ── Chain table ───────────────────────────────────────────────────────────

    [Fact]
    public void ChainTableSize_EqualsHighestTopLevelChainIndex()
    {
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x",
            "event[1, E1] = etype[Ev] - x: 1\n" +
            "event[2, E2] = etype[Ev] - x: 2\n" +
            "chain[1, A]\n    - event 1\n" +
            "chain[2, B]\n    - event 2");

        Assert.Equal(2, ReadWord(bytes, 0)); // chainCount = 2
    }

    [Fact]
    public void ChainStartPointsToFirstEventBinaryIndex()
    {
        // chain[1] starts at event[1] → binary index 0
        // chain[2] starts at event[3] → binary index 2
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x",
            "event[1, E1] = etype[Ev] - x: 0\n" +
            "event[2, E2] = etype[Ev] - x: 0\n" +
            "event[3, E3] = etype[Ev] - x: 0\n" +
            "chain[1, C1]\n    - event 1\n" +
            "chain[2, C2]\n    - event 3");

        Assert.Equal(0, ReadWord(bytes, 2)); // chain 1 → binary index 0
        Assert.Equal(2, ReadWord(bytes, 4)); // chain 2 → binary index 2
    }

    // ── Linear chain linking ──────────────────────────────────────────────────

    [Fact]
    public void LinearChain_LinksNextEventBytes()
    {
        // Chain: event1 → event2 → event3 (end)
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x",
            "event[1, E1] = etype[Ev] - x: 0\n" +
            "event[2, E2] = etype[Ev] - x: 0\n" +
            "event[3, E3] = etype[Ev] - x: 0\n" +
            "chain[1, C]\n    - event 1\n    - event 2\n    - event 3");

        int r0 = RecordOffset(1, 0);
        int r1 = RecordOffset(1, 1);
        int r2 = RecordOffset(1, 2);

        Assert.Equal(1, ReadWord(bytes, r0 + 10)); // event1.next = 1
        Assert.Equal(2, ReadWord(bytes, r1 + 10)); // event2.next = 2
        Assert.Equal(0xFFFF, ReadWord(bytes, r2 + 10)); // event3.next = none
    }

    // ── Branching chains ──────────────────────────────────────────────────────

    [Fact]
    public void BranchingEvent_SuccessPathSetsNext()
    {
        // chain[1]: - event1, ? chain[65]
        // chain[65]: - event2
        var bytes = Compile(
            "etype[2, Branch]: - 0: byte x\n" +
            "etype[1, Leaf]:   - 0: byte y",
            "event[1, Br] = etype[Branch] - x: 0\n" +
            "event[2, Lf] = etype[Leaf]   - y: 0\n" +
            "chain[1, Main]\n    - event 1\n    ? chain 65\n" +
            "chain[65, Succ]\n    - event 2");

        int r0 = RecordOffset(1, 0); // event 1 record; 1 top-level chain → offset = 2+2+2 = 6
        Assert.Equal(1, ReadWord(bytes, r0 + 10)); // success → binary index 1 (event 2)
    }

    [Fact]
    public void BranchingEvent_FailurePathSetsDataOffset7()
    {
        // chain[1]: - event1, ! chain[65]
        // chain[65]: - event2
        var bytes = Compile(
            "etype[2, Branch]: - 0: byte x\n" +
            "etype[1, Leaf]:   - 0: byte y",
            "event[1, Br] = etype[Branch] - x: 0\n" +
            "event[2, Lf] = etype[Leaf]   - y: 0\n" +
            "chain[1, Main]\n    - event 1\n    ! chain 65\n" +
            "chain[65, Fail]\n    - event 2");

        // record[8..9] = data[7..8] = failure target
        int r0 = RecordOffset(1, 0);
        Assert.Equal(1, ReadWord(bytes, r0 + 8)); // fail → binary index 1
    }

    [Fact]
    public void BranchingEvent_BothPaths()
    {
        var bytes = Compile(
            "etype[2, Branch]: - 0: byte x\n" +
            "etype[1, Leaf]:   - 0: byte y",
            "event[1, Br]   = etype[Branch] - x: 0\n" +
            "event[2, Succ] = etype[Leaf]   - y: 10\n" +
            "event[3, Fail] = etype[Leaf]   - y: 20\n" +
            "chain[1, Main]\n    - event 1\n    ? chain 65\n    ! chain 66\n" +
            "chain[65, S]\n    - event 2\n" +
            "chain[66, F]\n    - event 3");

        int r0 = RecordOffset(1, 0);
        Assert.Equal(1, ReadWord(bytes, r0 + 10)); // success → event 2 (binary 1)
        Assert.Equal(2, ReadWord(bytes, r0 + 8));  // failure → event 3 (binary 2)
    }

    // ── Multiple top-level chains ─────────────────────────────────────────────

    [Fact]
    public void MultipleTopLevelChains_AllLinkedIndependently()
    {
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x",
            "event[1, A1] = etype[Ev] - x: 1\n" +
            "event[2, A2] = etype[Ev] - x: 2\n" +
            "event[3, B1] = etype[Ev] - x: 3\n" +
            "event[4, B2] = etype[Ev] - x: 4\n" +
            "chain[1, ChainA]\n    - event 1\n    - event 2\n" +
            "chain[2, ChainB]\n    - event 3\n    - event 4");

        // Chain A: event1 → event2
        int rA0 = RecordOffset(2, 0);
        int rA1 = RecordOffset(2, 1);
        Assert.Equal(1,      ReadWord(bytes, rA0 + 10)); // next = 1
        Assert.Equal(0xFFFF, ReadWord(bytes, rA1 + 10)); // end

        // Chain B: event3 → event4
        int rB0 = RecordOffset(2, 2);
        int rB1 = RecordOffset(2, 3);
        Assert.Equal(3,      ReadWord(bytes, rB0 + 10)); // next = 3
        Assert.Equal(0xFFFF, ReadWord(bytes, rB1 + 10)); // end
    }

    // ── Sub-chains (index > 64) not in chain table ───────────────────────────

    [Fact]
    public void SubChainsAbove64_NotInChainTable()
    {
        var bytes = Compile(
            "etype[2, Branch]: - 0: byte x\n" +
            "etype[1, Leaf]:   - 0: byte y",
            "event[1, Br] = etype[Branch] - x: 0\n" +
            "event[2, Lf] = etype[Leaf]   - y: 0\n" +
            "chain[1, Main]\n    - event 1\n    ? chain 65\n" +
            "chain[65, Sub]\n    - event 2");

        // Only chain 1 is top-level; chainCount must be 1
        Assert.Equal(1, ReadWord(bytes, 0));
    }

    // ── Event count / record count ────────────────────────────────────────────

    [Fact]
    public void EventCount_EqualsHighestEventIndex()
    {
        // Events 1 and 3 declared; slot 2 is a gap → eventCount = 3
        var bytes = Compile(
            "etype[1, Ev]: - 0: byte x",
            "event[1, E1] = etype[Ev] - x: 0\n" +
            "event[3, E3] = etype[Ev] - x: 0");

        int evCountOff = 2; // no chains
        Assert.Equal(3, ReadWord(bytes, evCountOff));
        Assert.Equal(4 + 3 * 12, bytes.Length); // header + 3 records
    }

    // ── Enum / bitfield field values ──────────────────────────────────────────

    [Fact]
    public void EnumFieldValue_WrittenCorrectly()
    {
        var bytes = Compile(
            "enum Dir : byte { North, South, East, West }\n" +
            "etype[1, Ev]: - 0: Dir dir",
            "event[1, E] = etype[Ev] - dir: Dir.South");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(1, bytes[rOff + 1]); // South = 1
    }

    [Fact]
    public void BitfieldFieldValue_WrittenCorrectly()
    {
        var bytes = Compile(
            "bitfield Flags : byte { Read = 0x01, Write }\n" +
            "etype[1, Ev]: - 0: Flags flags",
            "event[1, E] = etype[Ev] - flags: Flags.Read | Flags.Write");

        int rOff = RecordOffset(0, 0);
        Assert.Equal(0x03, bytes[rOff + 1]); // Read|Write = 0x03
    }
}
