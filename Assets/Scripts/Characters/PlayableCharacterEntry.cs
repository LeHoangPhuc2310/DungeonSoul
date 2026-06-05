using UnityEngine;

[System.Serializable]
public class PlayableCharacterEntry
{
    public string id;
    public string displayName;
    public HeroType combatClass = HeroType.Warrior;
    public Sprite[] idle;
    public Sprite[] walk;

    [Header("Stats")]
    public float hp = 120f;
    public float damage = 15f;
    public float moveSpeed = 4.5f;
    public float fireRate = 1.2f;
    public float crit = 0.05f;

    [Header("UI")]
    public string bonusPositive = "+15% sát thương";
    public string bonusNegative = "-10% tốc chạy";
    public string abilityName = "Cuồng Chiến";
    [TextArea(2, 4)] public string abilityDescription = "Chiến đấu cân bằng.";

    public Sprite PreviewSprite =>
        idle != null && idle.Length > 0 ? idle[0]
        : walk != null && walk.Length > 0 ? walk[0]
        : null;

    public string ClassLabel => PlayableCharacterCatalog.GetClassLabel(combatClass);
}
