using System.Text;

namespace Ambermoon.Aon.Tools;

internal static class CharacterAonWriter
{
    // ── enum tables ──────────────────────────────────────────────────────────

    private static readonly string[] CharacterTypes = ["CharacterType.PartyMember", "CharacterType.NPC", "CharacterType.Monster"];
    private static readonly string[] Genders        = ["Gender.Male", "Gender.Female", "Gender.None"];
    private static readonly string[] Races          = ["Race.Human", "Race.Elf", "Race.HalfElf", "Race.Dwarf", "Race.Gnome", "Race.Halfling", "Race.Animal", "Race.Monster"];
    private static readonly string[] Classes        = ["Class.Adventurer", "Class.Warrior", "Class.Paladin", "Class.Thief", "Class.Ranger", "Class.Healer", "Class.Alchemist", "Class.Mystic", "Class.Mage", "Class.Animal"];

    // ── bitfield tables ──────────────────────────────────────────────────────

    private static readonly (int Bit, string Name)[] SpellTypeFlags =
    [
        (0x01, "SpellTypes.Healing"), (0x02, "SpellTypes.Alchemistic"),
        (0x04, "SpellTypes.Mystic"),  (0x08, "SpellTypes.Destruction"),
        (0x10, "SpellTypes.Unused1"), (0x20, "SpellTypes.Unused2"),
        (0x40, "SpellTypes.Functional"),
    ];

    private static readonly (int Bit, string Name)[] LanguageFlags =
    [
        (0x01, "Languages.Human"), (0x02, "Languages.Dwarf"),
        (0x04, "Languages.Gnome"), (0x08, "Languages.Elf"),
        (0x10, "Languages.Monster"),
    ];

    private static readonly (int Bit, string Name)[] BattleFlagFlags =
    [
        (0x01, "BattleFlags.Monster"),    (0x02, "BattleFlags.Boss"),
        (0x04, "BattleFlags.DontMove"),   (0x08, "BattleFlags.Undead"),
        (0x10, "BattleFlags.Demon"),      (0x20, "BattleFlags.DropNoPseudoItem"),
    ];

    private static readonly (int Bit, string Name)[] ElementFlags =
    [
        (0x01, "Elements.Earth"), (0x02, "Elements.Wind"),
        (0x04, "Elements.Fire"),  (0x08, "Elements.Water"),
        (0x10, "Elements.Undead"),(0x20, "Elements.Ghost"),
    ];

    private static readonly (int Bit, string Name)[] ConditionFlags =
    [
        (0x0001, "Conditions.Irritated"), (0x0002, "Conditions.Crazy"),
        (0x0004, "Conditions.Sleep"),     (0x0008, "Conditions.Panic"),
        (0x0010, "Conditions.Blind"),     (0x0020, "Conditions.Drugged"),
        (0x0040, "Conditions.Exhausted"), (0x0080, "Conditions.Unused"),
        (0x0100, "Conditions.Petrified"), (0x0200, "Conditions.Diseased"),
        (0x0400, "Conditions.Aging"),     (0x0800, "Conditions.DeadCorpse"),
        (0x1000, "Conditions.DeadAshes"), (0x2000, "Conditions.DeadDust"),
    ];

    private static readonly string[] AttributeNames = ["STR", "INT", "DEX", "SPD", "STA", "CHA", "LUK", "A-M"];
    private static readonly string[] AbilityNames   = ["ATT", "PAR", "SWI", "CRI", "F-T", "D-T", "L-P", "SRC", "R-M", "U-M"];

    // ────────────────────────────────────────────────────────────────────────

    public static string Write(CharacterData c, string sourceFile)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// Source: {sourceFile}");
        sb.AppendLine();

        var instanceName = string.IsNullOrEmpty(c.Name) ? "Unknown" : c.Name;
        sb.AppendLine($"[Character] {instanceName} = {{");

        Field(sb, "Type",                   Enum(c.CharacterType, CharacterTypes));
        Field(sb, "Gender",                 Enum(c.Gender, Genders));
        Field(sb, "Race",                   Enum(c.Race, Races));
        Field(sb, "Class",                  Enum(c.Class, Classes));
        Field(sb, "UsableSpellTypes",       Bitfield(c.UsableSpellTypes, SpellTypeFlags));
        Field(sb, "Level",                  c.Level.ToString());
        Field(sb, "NumOccupiedHands",       c.NumOccupiedHands.ToString());
        Field(sb, "NumOccupiedFingers",     c.NumOccupiedFingers.ToString());
        Field(sb, "SpokenLanguages",        Bitfield(c.SpokenLanguages, LanguageFlags));
        Field(sb, "InventoryInaccessible",  c.InventoryInaccessible.ToString());
        Field(sb, "PortraitIndex",          c.PortraitIndex.ToString());
        Field(sb, "JoinPercentage",         c.JoinPercentage.ToString());
        Field(sb, "CombatGraphicIndex",     c.CombatGraphicIndex.ToString());
        Field(sb, "SpellChancePercentage",  c.SpellChancePercentage.ToString());
        Field(sb, "MagicBonusToHit",        c.MagicBonusToHit.ToString());
        Field(sb, "MonsterMorale",          c.MonsterMorale.ToString());
        Field(sb, "SpellImmunity",          Bitfield(c.SpellImmunity, SpellTypeFlags));
        Field(sb, "AttacksPerRound",        c.AttacksPerRound.ToString());
        Field(sb, "BattleFlags",            Bitfield(c.BattleFlags, BattleFlagFlags));
        Field(sb, "ElementsAndImmunities",  Bitfield(c.ElementsAndImmunities, ElementFlags));
        Field(sb, "SpellLearningPoints",    c.SpellLearningPoints.ToString());
        Field(sb, "TrainingPoints",         c.TrainingPoints.ToString());
        Field(sb, "Gold",                   c.Gold.ToString());
        Field(sb, "Food",                   c.Food.ToString());
        Field(sb, "CharacterBitIndex",      Hex16(c.CharacterBitIndex));
        Field(sb, "Conditions",             Bitfield(c.Conditions, ConditionFlags));
        Field(sb, "MonsterExperience",      c.MonsterExperience.ToString());
        Field(sb, "BattleRoundSpellUsage",  c.BattleRoundSpellUsage.ToString());
        Field(sb, "MarkOfReturnX",          c.MarkOfReturnX.ToString());
        Field(sb, "MarkOfReturnY",          c.MarkOfReturnY.ToString());
        Field(sb, "MarkOfReturnMapIndex",   c.MarkOfReturnMapIndex.ToString());

        sb.AppendLine();
        sb.AppendLine("    Attributes = [");
        for (int i = 0; i < 8; i++)
            sb.AppendLine($"        {CharVal(c.Attributes[i])}    // {AttributeNames[i]}");
        sb.AppendLine("    ]");

        sb.AppendLine($"    Age              = {CharVal(c.Age)}");
        sb.AppendLine($"    SpellDamageBonus = {CharVal(c.SpellDamageBonus)}");

        sb.AppendLine();
        sb.AppendLine("    Abilities = [");
        for (int i = 0; i < 10; i++)
            sb.AppendLine($"        {CharVal(c.Abilities[i])}    // {AbilityNames[i]}");
        sb.AppendLine("    ]");

        sb.AppendLine();
        Field(sb, "CurrentHitPoints",            c.CurrentHitPoints.ToString());
        Field(sb, "MaxHitPoints",                c.MaxHitPoints.ToString());
        Field(sb, "BonusHitPoints",              c.BonusHitPoints.ToString());
        Field(sb, "CurrentSpellPoints",          c.CurrentSpellPoints.ToString());
        Field(sb, "MaxSpellPoints",              c.MaxSpellPoints.ToString());
        Field(sb, "BonusSpellPoints",            c.BonusSpellPoints.ToString());
        Field(sb, "BaseDefense",                 c.BaseDefense.ToString());
        Field(sb, "BonusDefense",                c.BonusDefense.ToString());
        Field(sb, "BaseAttackDamage",            c.BaseAttackDamage.ToString());
        Field(sb, "BonusAttackDamage",           c.BonusAttackDamage.ToString());
        Field(sb, "MagicAttackLevel",            c.MagicAttackLevel.ToString());
        Field(sb, "MagicDefenseLevel",           c.MagicDefenseLevel.ToString());
        Field(sb, "APRIncreaseLevels",           c.APRIncreaseLevels.ToString());
        Field(sb, "HitPointsPerLevel",           c.HitPointsPerLevel.ToString());
        Field(sb, "SpellPointsPerLevel",         c.SpellPointsPerLevel.ToString());
        Field(sb, "SpellLearningPointsPerLevel", c.SpellLearningPointsPerLevel.ToString());
        Field(sb, "TrainingPointsPerLevel",      c.TrainingPointsPerLevel.ToString());
        Field(sb, "LookTextIndex",               Hex16(c.LookTextIndex));

        sb.AppendLine();
        Field(sb, "Experience",               c.Experience.ToString());
        Field(sb, "LearnedHealingSpells",     Hex32(c.LearnedHealingSpells));
        Field(sb, "LearnedAlchemisticSpells", Hex32(c.LearnedAlchemisticSpells));
        Field(sb, "LearnedMysticSpells",      Hex32(c.LearnedMysticSpells));
        Field(sb, "LearnedDestructionSpells", Hex32(c.LearnedDestructionSpells));
        sb.AppendLine($"    LearnedFunctionalSpells = [ {Hex32(c.LearnedFunctionalSpells[0])}, {Hex32(c.LearnedFunctionalSpells[1])}, {Hex32(c.LearnedFunctionalSpells[2])} ]");
        Field(sb, "Weight",                   c.Weight.ToString());

        sb.AppendLine();
        Field(sb, "Name",                     $"\"{EscapeString(c.Name)}\"");

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void Field(StringBuilder sb, string name, string value) =>
        sb.AppendLine($"    {name,-28} = {value}");

    private static string Enum(int value, string[] table) =>
        value < table.Length ? table[value] : value.ToString();

    private static string Bitfield(int value, (int Bit, string Name)[] flags)
    {
        if (value == 0) return "0";

        var parts = new List<string>();
        int remaining = value;
        foreach (var (bit, name) in flags)
        {
            if ((remaining & bit) != 0)
            {
                parts.Add(name);
                remaining &= ~bit;
            }
        }
        if (remaining != 0)
            parts.Add($"0x{remaining:X2}");

        return string.Join(" | ", parts);
    }

    private static string CharVal(CharacterValue v) =>
        $"{{ Current = {v.Current,5}, Max = {v.Max,5}, Bonus = {v.Bonus,5}, Backup = {v.Backup,5} }}";

    private static string Hex16(ushort v) => v == 0xFFFF ? "0xFFFF" : $"0x{v:X4}";
    private static string Hex32(uint v)   => v == 0 ? "0" : $"0x{v:X8}";

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
