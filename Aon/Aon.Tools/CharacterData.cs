namespace Ambermoon.Aon.Tools;

internal sealed class CharacterValue
{
    public ushort Current { get; set; }
    public ushort Max     { get; set; }
    public short  Bonus   { get; set; }
    public ushort Backup  { get; set; }
}

internal class CharacterData
{
    // 0x0000–0x0013
    public byte CharacterType         { get; set; }
    public byte Gender                { get; set; }
    public byte Race                  { get; set; }
    public byte Class                 { get; set; }
    public byte UsableSpellTypes      { get; set; }
    public byte Level                 { get; set; }
    public byte NumOccupiedHands      { get; set; }
    public byte NumOccupiedFingers    { get; set; }
    public byte SpokenLanguages       { get; set; }
    public byte InventoryInaccessible { get; set; }
    public byte PortraitIndex         { get; set; }
    public byte JoinPercentage        { get; set; }   // 0x000B
    public byte CombatGraphicIndex    { get; set; }   // 0x000C
    public byte SpellChancePercentage { get; set; }   // 0x000D (Adv: extra languages)
    public byte MagicBonusToHit       { get; set; }   // 0x000E
    public byte MonsterMorale         { get; set; }   // 0x000F
    public byte SpellImmunity         { get; set; }   // 0x0010
    public byte AttacksPerRound       { get; set; }   // 0x0011
    public byte BattleFlags           { get; set; }   // 0x0012
    public byte ElementsAndImmunities { get; set; }   // 0x0013

    // 0x0014–0x0029
    public ushort SpellLearningPoints      { get; set; }
    public ushort TrainingPoints           { get; set; }
    public ushort Gold                     { get; set; }
    public ushort Food                     { get; set; }
    public ushort CharacterBitIndex        { get; set; }
    public ushort Conditions               { get; set; }
    public ushort MonsterExperience        { get; set; }
    public ushort BattleRoundSpellUsage    { get; set; }
    public ushort MarkOfReturnX            { get; set; }
    public ushort MarkOfReturnY            { get; set; }
    public ushort MarkOfReturnMapIndex     { get; set; }

    // 0x002A–0x00C9  (8 attributes + Age + SpellDamageBonus + 10 abilities)
    public CharacterValue[] Attributes      { get; set; } = new CharacterValue[8];   // STR INT DEX SPD STA CHA LUK A-M
    public CharacterValue   Age             { get; set; } = new();
    public CharacterValue   SpellDamageBonus{ get; set; } = new();
    public CharacterValue[] Abilities       { get; set; } = new CharacterValue[10];  // ATT PAR SWI CRI FT DT LP SRC RM UM

    // 0x00CA–0x00ED
    public ushort CurrentHitPoints             { get; set; }
    public ushort MaxHitPoints                 { get; set; }
    public ushort BonusHitPoints               { get; set; }
    public ushort CurrentSpellPoints           { get; set; }
    public ushort MaxSpellPoints               { get; set; }
    public ushort BonusSpellPoints             { get; set; }
    public short  BaseDefense                  { get; set; }
    public short  BonusDefense                 { get; set; }
    public short  BaseAttackDamage             { get; set; }
    public short  BonusAttackDamage            { get; set; }
    public ushort MagicAttackLevel             { get; set; }
    public ushort MagicDefenseLevel            { get; set; }
    public ushort APRIncreaseLevels            { get; set; }
    public ushort HitPointsPerLevel            { get; set; }
    public ushort SpellPointsPerLevel          { get; set; }
    public ushort SpellLearningPointsPerLevel  { get; set; }
    public ushort TrainingPointsPerLevel       { get; set; }
    public ushort LookTextIndex                { get; set; }

    // 0x00EE–0x0111
    public uint   Experience                   { get; set; }
    public uint   LearnedHealingSpells         { get; set; }
    public uint   LearnedAlchemisticSpells     { get; set; }
    public uint   LearnedMysticSpells          { get; set; }
    public uint   LearnedDestructionSpells     { get; set; }
    public uint[] LearnedFunctionalSpells      { get; set; } = new uint[3];
    public uint   Weight                       { get; set; }

    // 0x0112
    public string Name { get; set; } = "";
}
