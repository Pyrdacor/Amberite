namespace MapEventReader;

// Stub DataReader — big-endian (Motorola 68000) binary reader.
public class DataReader
{
    private readonly byte[] _data;

    public DataReader(byte[] data) => _data = data;

    public int Position { get; set; }

    public byte ReadByte() => _data[Position++];

    public ushort ReadWord()
    {
        int hi = _data[Position++];
        int lo = _data[Position++];
        return (ushort)((hi << 8) | lo);
    }

    public uint ReadLong()
    {
        uint hi = ReadWord();
        uint lo = ReadWord();
        return (hi << 16) | lo;
    }

    public byte[] ReadBytes(int count)
    {
        var result = new byte[count];
        Array.Copy(_data, Position, result, 0, count);
        Position += count;
        return result;
    }
}
