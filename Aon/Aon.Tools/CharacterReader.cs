using System.Text;

namespace Ambermoon.Aon.Tools;

internal static class CharacterReader
{
    public static CharacterData Read(byte[] data)
    {
        var c = new CharacterData();
        ReadInto(c, data);
        return c;
    }

    public static MonsterData ReadMonster(byte[] data)
    {
        var m = new MonsterData();
        ReadInto(m, data);

        // 0x01E8: 8 animations × 32 frame indices
        for (int i = 0; i < 8; i++)
            Array.Copy(data, 0x01E8 + i * 32, m.Animations[i].FrameIndices, 0, 32);

        // 0x02E8: frame counts, 0x02F0: Atari palette, 0x0300: Amiga palette
        Array.Copy(data, 0x02E8, m.FrameCounts,  0, 8);
        Array.Copy(data, 0x02F0, m.AtariPalette, 0, 16);
        Array.Copy(data, 0x0300, m.AmigaPalette, 0, 32);

        m.AnimationDirectionFlags = data[0x0320];
        // 0x0321 padding byte ignored

        m.FrameWidth        = ReadU16(data, 0x0322);
        m.FrameHeight       = ReadU16(data, 0x0324);
        m.MappedFrameWidth  = ReadU16(data, 0x0326);
        m.MappedFrameHeight = ReadU16(data, 0x0328);

        return m;
    }

    private static void ReadInto(CharacterData c, byte[] data)
    {
        // 0x0000–0x0013: single-byte fields
        c.CharacterType         = data[0x00];
        c.Gender                = data[0x01];
        c.Race                  = data[0x02];
        c.Class                 = data[0x03];
        c.UsableSpellTypes      = data[0x04];
        c.Level                 = data[0x05];
        c.NumOccupiedHands      = data[0x06];
        c.NumOccupiedFingers    = data[0x07];
        c.SpokenLanguages       = data[0x08];
        c.InventoryInaccessible = data[0x09];
        c.PortraitIndex         = data[0x0A];
        c.JoinPercentage        = data[0x0B];
        c.CombatGraphicIndex    = data[0x0C];
        c.SpellChancePercentage = data[0x0D];
        c.MagicBonusToHit       = data[0x0E];
        c.MonsterMorale         = data[0x0F];
        c.SpellImmunity         = data[0x10];
        c.AttacksPerRound       = data[0x11];
        c.BattleFlags           = data[0x12];
        c.ElementsAndImmunities = data[0x13];

        // 0x0014–0x0029: big-endian 16-bit words
        c.SpellLearningPoints        = ReadU16(data, 0x0014);
        c.TrainingPoints             = ReadU16(data, 0x0016);
        c.Gold                       = ReadU16(data, 0x0018);
        c.Food                       = ReadU16(data, 0x001A);
        c.CharacterBitIndex          = ReadU16(data, 0x001C);
        c.Conditions                 = ReadU16(data, 0x001E);
        c.MonsterExperience          = ReadU16(data, 0x0020);
        c.BattleRoundSpellUsage      = ReadU16(data, 0x0022);
        c.MarkOfReturnX              = ReadU16(data, 0x0024);
        c.MarkOfReturnY              = ReadU16(data, 0x0026);
        c.MarkOfReturnMapIndex       = ReadU16(data, 0x0028);

        // 0x002A: 8 attributes (CharacterValue × 8)
        for (int i = 0; i < 8; i++)
            c.Attributes[i] = ReadCharVal(data, 0x002A + i * 8);

        // 0x006A: Age, 0x0072: SpellDamageBonus
        c.Age              = ReadCharVal(data, 0x006A);
        c.SpellDamageBonus = ReadCharVal(data, 0x0072);

        // 0x007A: 10 abilities (CharacterValue × 10)
        for (int i = 0; i < 10; i++)
            c.Abilities[i] = ReadCharVal(data, 0x007A + i * 8);

        // 0x00CA–0x00ED
        c.CurrentHitPoints            = ReadU16(data, 0x00CA);
        c.MaxHitPoints                = ReadU16(data, 0x00CC);
        c.BonusHitPoints              = ReadU16(data, 0x00CE);
        c.CurrentSpellPoints          = ReadU16(data, 0x00D0);
        c.MaxSpellPoints              = ReadU16(data, 0x00D2);
        c.BonusSpellPoints            = ReadU16(data, 0x00D4);
        c.BaseDefense                 = ReadS16(data, 0x00D6);
        c.BonusDefense                = ReadS16(data, 0x00D8);
        c.BaseAttackDamage            = ReadS16(data, 0x00DA);
        c.BonusAttackDamage           = ReadS16(data, 0x00DC);
        c.MagicAttackLevel            = ReadU16(data, 0x00DE);
        c.MagicDefenseLevel           = ReadU16(data, 0x00E0);
        c.APRIncreaseLevels           = ReadU16(data, 0x00E2);
        c.HitPointsPerLevel           = ReadU16(data, 0x00E4);
        c.SpellPointsPerLevel         = ReadU16(data, 0x00E6);
        c.SpellLearningPointsPerLevel = ReadU16(data, 0x00E8);
        c.TrainingPointsPerLevel      = ReadU16(data, 0x00EA);
        c.LookTextIndex               = ReadU16(data, 0x00EC);

        // 0x00EE–0x0111
        c.Experience               = ReadU32(data, 0x00EE);
        c.LearnedHealingSpells     = ReadU32(data, 0x00F2);
        c.LearnedAlchemisticSpells = ReadU32(data, 0x00F6);
        c.LearnedMysticSpells      = ReadU32(data, 0x00FA);
        c.LearnedDestructionSpells = ReadU32(data, 0x00FE);
        for (int i = 0; i < 3; i++)
            c.LearnedFunctionalSpells[i] = ReadU32(data, 0x0102 + i * 4);
        c.Weight = ReadU32(data, 0x010E);

        // 0x0112: name, 16 bytes, null-terminated, CP850
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc = Encoding.GetEncoding(850);
        int nameEnd = 0x0112;
        while (nameEnd < 0x0122 && data[nameEnd] != 0) nameEnd++;
        c.Name = enc.GetString(data, 0x0112, nameEnd - 0x0112);
    }

    internal static ushort ReadU16(byte[] d, int o) =>
        (ushort)((d[o] << 8) | d[o + 1]);

    private static short ReadS16(byte[] d, int o) =>
        (short)((d[o] << 8) | d[o + 1]);

    private static uint ReadU32(byte[] d, int o) =>
        ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];

    private static CharacterValue ReadCharVal(byte[] d, int o) => new()
    {
        Current = ReadU16(d, o),
        Max     = ReadU16(d, o + 2),
        Bonus   = ReadS16(d, o + 4),
        Backup  = ReadU16(d, o + 6),
    };
}
