// DungeonSoul — Đường dẫn + map folder ASEPRITE_skill_effect → VFX trong game.

using UnityEngine;

public static class AsepriteSkillEffectPaths
{
    public const string SourceRoot = "Assets/ASEPRITE/ASEPRITE_skill_effect/PNG";
    public const string ResourcesRoot = "AsepriteSkillVfx";

    public static readonly string[] AllCategories =
    {
        "Circle_explosion",
        "Explosion",
        "Explosion_blue_circle",
        "Explosion_blue_oval",
        "Explosion_gas",
        "Explosion_gas_circle",
        "Explosion_two_colors",
        "Fire",
        "Icons",
        "Lightning",
        "Nuclear_explosion",
        "Smoke"
    };

    public static string[] FoldersFor(SkillVfxStyle style)
    {
        switch (style)
        {
            case SkillVfxStyle.Fire:
                return new[] { "Fire", "Explosion" };
            case SkillVfxStyle.Ice:
                return new[] { "Explosion_blue_oval", "Explosion_blue_circle" };
            case SkillVfxStyle.Lightning:
                return new[] { "Lightning" };
            case SkillVfxStyle.Poison:
                return new[] { "Explosion_gas", "Explosion_gas_circle" };
            case SkillVfxStyle.Arcane:
                return new[] { "Explosion_two_colors", "Nuclear_explosion" };
            case SkillVfxStyle.Slash:
                return new[] { "Circle_explosion" };
            default:
                return new[] { "Explosion" };
        }
    }

    public static string[] FoldersFor(EffectKind kind)
    {
        switch (kind)
        {
            case EffectKind.FireExplosion:
                return new[] { "Explosion", "Fire" };
            case EffectKind.IceExplosion:
                return new[] { "Explosion_blue_oval" };
            case EffectKind.BlueExplosion:
                return new[] { "Explosion_blue_circle", "Nuclear_explosion" };
            case EffectKind.PoisonBoom:
                return new[] { "Explosion_gas_circle", "Explosion_gas" };
            case EffectKind.FireBreath:
                return new[] { "Fire" };
            case EffectKind.SpawnPoint:
                return new[] { "Smoke", "Circle_explosion" };
            case EffectKind.HitImpact:
                return new[] { "Circle_explosion" };
            case EffectKind.CritImpact:
                return new[] { "Nuclear_explosion", "Explosion_two_colors" };
            default:
                return new[] { "Explosion" };
        }
    }

    public static string IconsFolder => "Icons";

    /// <summary>Icon skill theo loại — 11 icon trong pack.</summary>
    public static int SkillIconIndex(SkillType type)
    {
        switch (type)
        {
            case SkillType.FireArrow:
            case SkillType.ExplosiveRounds:
            case SkillType.Explosion:
            case SkillType.DragonStrike:
                return 0;
            case SkillType.LightningChain:
                return 1;
            case SkillType.IceAura:
            case SkillType.TimeFreeze:
                return 2;
            case SkillType.PoisonCloud:
                return 3;
            case SkillType.GhostForm:
            case SkillType.SoulHarvest:
            case SkillType.MirrorImage:
            case SkillType.Vampire:
                return 4;
            case SkillType.BladeStorm:
            case SkillType.CriticalHit:
            case SkillType.DeathMark:
                return 5;
            case SkillType.Boomerang:
                return 6;
            case SkillType.IronBody:
            case SkillType.ToughSkin:
                return 7;
            case SkillType.CoinMagnet:
                return 8;
            case SkillType.SpeedBoost:
            case SkillType.DoubleShot:
            case SkillType.QuadShot:
            case SkillType.TwinArrows:
                return 9;
            default:
                return (int)type % 11;
        }
    }
}
