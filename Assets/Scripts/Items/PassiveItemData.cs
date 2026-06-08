using UnityEngine;

/// <summary>Enum legacy — giữ cho GameIconLibrary fallback.</summary>
public enum PassiveItemType
{
    Spinach,
    Armor,
    Wings,
    EmptyTome,
    Candelabrador,
    Bracer,
    HollowHeart,
    Pummarola
}

/// <summary>Loại chỉ số mà trang bị thụ động cộng theo cấp (kiểu Vampire Survivors).</summary>
public enum PassiveStatModifierType
{
    Defense,
    HP,
    MoveSpeed,
    Damage,
    CooldownReduction,
    ExpGain,
    CritChance,
    Magnet,
    BurnChance,
    LifeSteal,
    ProjectileCount,
    Revive,
    Luck,
    AreaSize
}

/// <summary>ScriptableObject mô tả một passive item — dùng cho level-up / rương wave.</summary>
[CreateAssetMenu(fileName = "PassiveItem", menuName = "DungeonSoul/Items/Passive Item Data")]
public class PassiveItemData : ScriptableObject
{
    [Tooltip("ID duy nhất — dùng cho evolve / debug.")]
    public string id;

    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    public PassiveStatModifierType statModifierType = PassiveStatModifierType.Damage;

    [Tooltip("Giá trị mỗi cấp (length = maxLevel). % thì dùng 0.04 = 4%.")]
    public float[] valuePerLevel = { 0.1f };

    public int maxLevel = 5;

    public SkillRarity rarity = SkillRarity.Common;

    [Tooltip("true nếu valuePerLevel là phần trăm (0.05 = +5%).")]
    public bool isPercent = true;

    [Header("Tiến hóa vũ khí (tùy chọn)")]
    public bool enablesWeaponEvolve;

    public WeaponType evolveTargetWeapon = WeaponType.IronBow;
    public WeaponType evolveResultWeapon = WeaponType.StormBow;

    [Tooltip("Nhân sát thương thêm khi tiến hóa bằng passive (1 = không đổi).")]
    public float evolveDamageMultiplier = 1f;

    public bool HasEvolveCombo =>
        enablesWeaponEvolve && evolveResultWeapon != evolveTargetWeapon;

    /// <summary>Giá trị tại cấp hiện tại (1-based).</summary>
    public float GetValueAtLevel(int level)
    {
        if (valuePerLevel == null || valuePerLevel.Length == 0)
            return 0f;

        int idx = Mathf.Clamp(level - 1, 0, valuePerLevel.Length - 1);
        return valuePerLevel[idx];
    }

    /// <summary>Tổng giá trị cộng dồn từ cấp 1 đến level (cho stat cộng dồn).</summary>
    public float GetTotalValueAtLevel(int level)
    {
        if (valuePerLevel == null || valuePerLevel.Length == 0)
            return 0f;

        int count = Mathf.Clamp(level, 0, valuePerLevel.Length);
        float sum = 0f;
        for (int i = 0; i < count; i++)
            sum += valuePerLevel[i];
        return sum;
    }

    public string GetLevelDescription(int level)
    {
        if (level <= 0)
            return description;

        float v = GetValueAtLevel(level);
        string sign = v >= 0f ? "+" : "";
        string unit = isPercent ? "%" : "";
        float display = isPercent ? v * 100f : v;
        return description + "\n\nCấp " + level + "/" + maxLevel + ": " + sign + display.ToString("0.#") + unit;
    }
}

/// <summary>Trạng thái passive đã nhặt trong một run.</summary>
[System.Serializable]
public class PassivePick
{
    public PassiveItemData data;
    public int level = 1;

    public bool IsMaxed => data != null && level >= data.maxLevel;
}
