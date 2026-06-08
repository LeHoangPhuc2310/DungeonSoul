/// <summary>Loại thẻ thưởng trên panel chọn.</summary>
public enum SkillSelectionChoiceKind
{
    SkillUpgrade,
    WeaponPickup,
    PassiveItem,
    BonusHp,
    BonusCoin,
    HealFallback
}

/// <summary>Ngữ cảnh mở panel — ảnh hưởng pool và giao diện.</summary>
public enum SkillSelectionContext
{
    LevelUp,
    NormalChest,
    EliteChest,
    BossChest
}

/// <summary>Một lựa chọn trên panel (skill / passive / vũ khí / fallback).</summary>
public class SkillSelectionChoice
{
    public SkillSelectionChoiceKind kind;
    public SkillData skill;
    public WeaponType weaponType;
    public PassiveItemData passiveItem;
    public float bonusHp;
    public int bonusCoins;
    public string note;

    public string GetUniqueKey()
    {
        switch (kind)
        {
            case SkillSelectionChoiceKind.SkillUpgrade:
                return skill != null ? "skill:" + skill.skillType : "skill:none";
            case SkillSelectionChoiceKind.WeaponPickup:
                return "weapon:" + weaponType;
            case SkillSelectionChoiceKind.PassiveItem:
                return passiveItem != null ? "passive:" + passiveItem.id : "passive:none";
            case SkillSelectionChoiceKind.BonusHp:
                return "bonus_hp";
            case SkillSelectionChoiceKind.BonusCoin:
                return "bonus_coin";
            case SkillSelectionChoiceKind.HealFallback:
                return "heal_fallback";
            default:
                return kind.ToString();
        }
    }
}
