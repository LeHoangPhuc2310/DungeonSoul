using System.Collections.Generic;

/// <summary>Lọc skill theo lớp — chiến binh không nhận skill chỉ dành cho đạn.</summary>
public static class SkillCombatRules
{
    private static readonly HashSet<SkillType> RangedOnlySkills = new HashSet<SkillType>
    {
        SkillType.DoubleShot,
        SkillType.QuadShot,
        SkillType.TwinArrows,
        SkillType.MultiTarget
    };

    private static readonly HashSet<SkillType> WarriorPreferred = new HashSet<SkillType>
    {
        SkillType.BladeStorm,
        SkillType.IronBody,
        SkillType.ToughSkin,
        SkillType.Vampire,
        SkillType.LifeSteal,
        SkillType.CriticalHit,
        SkillType.QuickReload,
        SkillType.SpeedBoost,
        SkillType.GhostForm,
        SkillType.DeathMark,
        SkillType.SoulHarvest
    };

    private static readonly HashSet<SkillType> MagePreferred = new HashSet<SkillType>
    {
        SkillType.FireArrow,
        SkillType.IceAura,
        SkillType.PoisonCloud,
        SkillType.LightningChain,
        SkillType.Explosion,
        SkillType.PiercingArrow,
        SkillType.ExplosiveRounds,
        SkillType.DragonStrike,
        SkillType.TimeFreeze,
        SkillType.MirrorImage
    };

    public static bool IsOfferedToHero(SkillType type, HeroType hero)
    {
        if (WeaponStyleUtil.IsMeleeHero(hero))
            return !RangedOnlySkills.Contains(type);

        return true;
    }

    public static float WeightMultiplier(SkillType type, HeroType hero)
    {
        if (WeaponStyleUtil.IsMeleeHero(hero) && WarriorPreferred.Contains(type))
            return 1.4f;

        if (hero == HeroType.Mage && MagePreferred.Contains(type))
            return 1.35f;

        return 1f;
    }
}
