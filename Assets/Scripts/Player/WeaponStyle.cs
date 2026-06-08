// DungeonSoul — WeaponStyle.cs — Phân loại vũ khí: tầm xa (bắn đạn) hay cận chiến (đánh gần).

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
}
