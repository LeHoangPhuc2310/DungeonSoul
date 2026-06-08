// DungeonSoul — GameIconLibrary.cs — Map skill/weapon/passive sang icon tile + màu tint.
// Dùng chung cho SkillSelectionUI, MetaShopUI, SkillsPanelUI...

using UnityEngine;

public static class GameIconLibrary
{
    // ---------------- SKILL ----------------

    /// <summary>Tile index trong Assets/Art/Tiles cho từng loại kỹ năng.</summary>
    public static int SkillTile(SkillType type)
    {
        switch (type)
        {
            // Bắn / mũi tên
            case SkillType.DoubleShot: return 131;       // giáo/mũi
            case SkillType.QuadShot: return 131;
            case SkillType.TwinArrows: return 107;       // cung bão
            case SkillType.FireArrow: return 129;        // gậy lửa
            case SkillType.PiercingArrow: return 131;
            case SkillType.MultiTarget: return 107;
            case SkillType.ExplosiveRounds: return 129;
            case SkillType.Explosion: return 29;         // ô lửa đỏ

            // Phòng thủ / máu
            case SkillType.IronBody: return 29;
            case SkillType.ToughSkin: return 105;        // kiếm cong (khiên thay thế)
            case SkillType.Vampire: return 103;          // dao găm (hút máu)
            case SkillType.LifeSteal: return 103;

            // Di chuyển / tiện ích
            case SkillType.SpeedBoost: return 131;
            case SkillType.QuickReload: return 117;      // búa (nạp nhanh)
            case SkillType.CoinMagnet: return 89;        // rương

            // Sát thương / chí mạng
            case SkillType.SteadyAim: return 106;        // kiếm rộng
            case SkillType.CriticalHit: return 118;      // rìu chiến
            case SkillType.BladeStorm: return 104;       // kiếm ngắn

            // Phép thuật vùng
            case SkillType.IceAura: return 130;          // gậy băng
            case SkillType.PoisonCloud: return 103;
            case SkillType.LightningChain: return 117;
            case SkillType.Boomerang: return 119;        // rìu gỗ (boomerang)

            // Huyền thoại
            case SkillType.DeathMark: return 104;
            case SkillType.TimeFreeze: return 130;
            case SkillType.DragonStrike: return 129;
            case SkillType.SoulHarvest: return 118;
            case SkillType.GhostForm: return 105;
            case SkillType.MirrorImage: return 106;

            default: return 106;
        }
    }

    public static Color SkillTint(SkillType type)
    {
        switch (type)
        {
            case SkillType.FireArrow:
            case SkillType.Explosion:
            case SkillType.ExplosiveRounds:
            case SkillType.DragonStrike:
                return new Color(1f, 0.55f, 0.3f);            // lửa
            case SkillType.IceAura:
            case SkillType.TimeFreeze:
                return new Color(0.6f, 0.9f, 1f);             // băng
            case SkillType.PoisonCloud:
            case SkillType.Vampire:
            case SkillType.LifeSteal:
                return new Color(0.6f, 1f, 0.55f);            // độc/hồi
            case SkillType.LightningChain:
                return new Color(0.85f, 0.75f, 1f);           // sét
            case SkillType.IronBody:
            case SkillType.ToughSkin:
                return new Color(0.8f, 0.85f, 0.95f);         // thép
            case SkillType.CoinMagnet:
                return new Color(1f, 0.85f, 0.35f);           // vàng
            case SkillType.DeathMark:
            case SkillType.SoulHarvest:
            case SkillType.MirrorImage:
            case SkillType.GhostForm:
                return new Color(0.85f, 0.6f, 1f);            // huyền thoại tím
            case SkillType.SpeedBoost:
            case SkillType.DoubleShot:
            case SkillType.QuadShot:
                return new Color(0.7f, 0.95f, 1f);
            default:
                return new Color(0.95f, 0.92f, 0.85f);
        }
    }

    public static Sprite SkillSprite(SkillType type)
    {
        return ArtSpriteLibrary.LoadTile(SkillTile(type));
    }

    // ---------------- WEAPON ----------------

    public static Sprite WeaponSprite(WeaponType type)
    {
        // Ưu tiên icon vũ khí thật từ Assets/Art/Sprite/Weapon.
        Sprite s = WeaponIconLibrary.GetWeapon(type);
        return s != null ? s : ArtSpriteLibrary.GetWeaponSprite(type);
    }

    public static Color WeaponTint(WeaponType type)
    {
        return WeaponIconLibrary.GetWeapon(type) != null
            ? WeaponIconLibrary.Tint(type)
            : ArtSpriteLibrary.GetWeaponTint(type);
    }

    // ---------------- PASSIVE ----------------

    public static int PassiveTile(PassiveItemType type)
    {
        switch (type)
        {
            case PassiveItemType.Spinach: return 106;        // kiếm (sát thương)
            case PassiveItemType.Armor: return 105;          // giáp
            case PassiveItemType.Wings: return 131;          // tốc độ
            case PassiveItemType.EmptyTome: return 129;      // tome/phép
            case PassiveItemType.Candelabrador: return 129;
            case PassiveItemType.Bracer: return 107;
            case PassiveItemType.HollowHeart: return 29;     // máu
            case PassiveItemType.Pummarola: return 103;      // hồi máu
            default: return 89;
        }
    }

    public static Color PassiveTint(PassiveItemType type)
    {
        switch (type)
        {
            case PassiveItemType.Spinach: return new Color(0.7f, 1f, 0.5f);
            case PassiveItemType.Armor: return new Color(0.8f, 0.85f, 0.95f);
            case PassiveItemType.Wings: return new Color(0.85f, 0.95f, 1f);
            case PassiveItemType.HollowHeart: return new Color(1f, 0.45f, 0.5f);
            case PassiveItemType.Pummarola: return new Color(1f, 0.5f, 0.55f);
            case PassiveItemType.EmptyTome:
            case PassiveItemType.Candelabrador: return new Color(0.85f, 0.7f, 1f);
            default: return Color.white;
        }
    }

    public static Sprite PassiveSprite(PassiveItemData item)
    {
        if (item != null && item.icon != null)
            return item.icon;
        return ArtSpriteLibrary.LoadTile(PassiveTileByStat(item != null ? item.statModifierType : PassiveStatModifierType.Damage));
    }

    public static Color PassiveTint(PassiveItemData item)
    {
        if (item == null)
            return Color.white;
        return PassiveTintByStat(item.statModifierType);
    }

    public static int PassiveTileByStat(PassiveStatModifierType type)
    {
        switch (type)
        {
            case PassiveStatModifierType.Defense: return 105;
            case PassiveStatModifierType.HP: return 29;
            case PassiveStatModifierType.MoveSpeed: return 131;
            case PassiveStatModifierType.Damage: return 106;
            case PassiveStatModifierType.CooldownReduction: return 129;
            case PassiveStatModifierType.ExpGain: return 89;
            case PassiveStatModifierType.CritChance: return 118;
            case PassiveStatModifierType.Magnet: return 89;
            case PassiveStatModifierType.BurnChance: return 129;
            case PassiveStatModifierType.LifeSteal: return 103;
            case PassiveStatModifierType.ProjectileCount: return 107;
            case PassiveStatModifierType.Revive: return 89;
            default: return 106;
        }
    }

    public static Color PassiveTintByStat(PassiveStatModifierType type)
    {
        switch (type)
        {
            case PassiveStatModifierType.Defense: return new Color(0.8f, 0.85f, 0.95f);
            case PassiveStatModifierType.HP: return new Color(1f, 0.45f, 0.5f);
            case PassiveStatModifierType.MoveSpeed: return new Color(0.85f, 0.95f, 1f);
            case PassiveStatModifierType.Damage: return new Color(0.7f, 1f, 0.5f);
            case PassiveStatModifierType.CooldownReduction: return new Color(0.85f, 0.7f, 1f);
            case PassiveStatModifierType.ExpGain: return new Color(1f, 0.85f, 0.35f);
            case PassiveStatModifierType.CritChance: return new Color(1f, 0.55f, 0.35f);
            case PassiveStatModifierType.Magnet: return new Color(1f, 0.85f, 0.35f);
            case PassiveStatModifierType.BurnChance: return new Color(1f, 0.55f, 0.3f);
            case PassiveStatModifierType.LifeSteal: return new Color(1f, 0.5f, 0.55f);
            case PassiveStatModifierType.ProjectileCount: return new Color(0.5f, 0.9f, 1f);
            case PassiveStatModifierType.Revive: return new Color(0.95f, 0.72f, 0.15f);
            default: return Color.white;
        }
    }
}
