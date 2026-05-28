namespace Ambermoon.Aon.Tools;

internal sealed class AnimationInfo
{
    public byte[] FrameIndices { get; } = new byte[32];
}

internal sealed class MonsterData : CharacterData
{
    // 0x01E8: 8 animation sets, each 32 frame indices
    public AnimationInfo[] Animations { get; } =
        Enumerable.Range(0, 8).Select(_ => new AnimationInfo()).ToArray();

    // 0x02E8: used frame count per animation
    public byte[] FrameCounts { get; } = new byte[8];

    // 0x02F0: Atari palette (16 color indices), 0x0300: Amiga palette (32 color indices)
    public byte[] AtariPalette { get; } = new byte[16];
    public byte[] AmigaPalette { get; } = new byte[32];

    // 0x0320
    public byte AnimationDirectionFlags { get; set; }
    // 0x0321: padding byte (always 0)

    // 0x0322
    public ushort FrameWidth         { get; set; }
    public ushort FrameHeight        { get; set; }
    public ushort MappedFrameWidth   { get; set; }
    public ushort MappedFrameHeight  { get; set; }
}
