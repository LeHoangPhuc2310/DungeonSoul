using UnityEngine;

/// <summary>Cấu hình panel chọn skill/passive/vũ khí — chỉnh trong Inspector hoặc Resources.</summary>
[CreateAssetMenu(fileName = "SkillSelectionConfig", menuName = "DungeonSoul/UI/Skill Selection Config")]
public class SkillSelectionConfig : ScriptableObject
{
    [Header("Reroll")]
    public int rerollCoinCost = 50;
    public int maxRerollsPerPanel = 3;

    [Header("Skip")]
    [Range(0f, 1f)] public float skipHealPercent = 0.1f;

    [Header("Fallback thưởng")]
    public float fallbackBonusHp = 10f;
    public int fallbackBonusCoins = 5;
    public float allMaxedHealHp = 30f;

    [Header("Skill stack tối đa (non-legendary)")]
    public int maxSkillStack = 3;

    [Header("Debug")]
    public bool logPoolWeights;

    private static SkillSelectionConfig cached;

    public static SkillSelectionConfig Get()
    {
        if (cached != null)
            return cached;

        cached = Resources.Load<SkillSelectionConfig>("SkillSelectionConfig");
        if (cached == null)
        {
            cached = CreateInstance<SkillSelectionConfig>();
            cached.hideFlags = HideFlags.HideAndDontSave;
        }

        return cached;
    }
}
