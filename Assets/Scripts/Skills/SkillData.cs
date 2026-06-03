using UnityEngine;

public enum SkillRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum SkillType
{
    DoubleShot,
    SpeedBoost,
    IronBody,
    QuickReload,
    CoinMagnet,
    ToughSkin,
    FireArrow,
    SteadyAim,
    PiercingArrow,
    MultiTarget,
    CriticalHit,
    LifeSteal,
    Boomerang,
    LightningChain,
    PoisonCloud,
    ExplosiveRounds,
    Explosion,
    IceAura,
    GhostForm,
    QuadShot,
    BladeStorm,
    Vampire,
    TwinArrows,
    DeathMark,
    TimeFreeze,
    DragonStrike,
    SoulHarvest,
    MirrorImage
}

[CreateAssetMenu(fileName = "SkillData", menuName = "DungeonSoul/Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName;
    [TextArea(2, 4)] public string description;
    public SkillRarity rarity;
    public SkillType skillType;
    public float value;
}
