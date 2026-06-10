// DungeonSoul — WeaponStyle.cs — Phân loại vũ khí: tầm xa (bắn đạn) hay cận chiến (đánh gần).

using UnityEngine;

public enum AttackStyle
{
    Ranged,  // cung/súng/gậy phép → bắn đạn ra từ vũ khí
    Melee    // kiếm/đao/rìu → vung đánh tầm gần
}

public static class WeaponStyleUtil
{
    /// <summary>Kiểu tấn công theo lớp nhân vật (quyết định ranged/melee chính).</summary>
    public static AttackStyle ForHero(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Warrior: return AttackStyle.Melee;   // chiến binh cầm kiếm
            case HeroType.Ranger: return AttackStyle.Ranged;   // cung
            case HeroType.Mage: return AttackStyle.Ranged;     // gậy phép bắn
            default: return AttackStyle.Ranged;
        }
    }

    public static bool IsMeleeHero(HeroType hero) => ForHero(hero) == AttackStyle.Melee;

    /// <summary>Kiểu tấn công theo loại vũ khí (nếu game mở rộng vũ khí cận chiến sau này).</summary>
    public static AttackStyle ForWeapon(WeaponType type)
    {
        switch (type)
        {
            // Dao găm = cận chiến.
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return AttackStyle.Melee;
            // Còn lại (cung, gậy, trượng, thánh giá) = tầm xa.
            default:
                return AttackStyle.Ranged;
        }
    }

    public static HeroType GetSelectedHeroClass()
    {
        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        if (entry != null)
            return entry.combatClass;

        if (HeroRunStats.Instance != null)
            return HeroRunStats.Instance.SelectedHero;

        return (HeroType)Mathf.Clamp(PlayerPrefs.GetInt("ds_selected_hero", 0), 0, 2);
    }

    /// <summary>Lớp cận chiến không được dùng vũ khí/đạn tầm xa.</summary>
    public static bool HeroCanUseWeapon(HeroType hero, WeaponType weapon) =>
        !IsMeleeHero(hero) || ForWeapon(weapon) == AttackStyle.Melee;

    /// <summary>Chiến binh nhặt skill/passive; pháp sư/cung có thể nhặt thêm slot phép.</summary>
    public static bool UsesWeaponPickupRewards(HeroType hero) => !IsMeleeHero(hero);

    public static bool UsesWeaponPickupRewards() => UsesWeaponPickupRewards(GetSelectedHeroClass());

    public static string GetCombatStyleLabel(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Mage: return "Pháp thuật";
            case HeroType.Ranger: return "Cung thủ";
            default: return "Cận chiến";
        }
    }

    public static string GetCombatStyleDescription(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Mage:
                return "Bắn đạn tự động. Có thể nhặt thêm phép trong dungeon — tay không đổi.";
            case HeroType.Ranger:
                return "Tấn công tầm xa. Nhặt skill, passive và vũ khí trong dungeon.";
            default:
                return "Đánh gần bằng vũ khí gắn sẵn. Nhặt skill & passive — không đổi kiếm/rìu trên tay.";
        }
    }

    public static WeaponType DefaultMeleeStarterWeapon => WeaponType.PoisonDagger;
}
