using UnityEngine;

[System.Serializable]
public class PlayableCharacterEntry
{
    public string id;
    public string displayName;
    public HeroType combatClass = HeroType.Warrior;
    public Sprite[] idle;
    public Sprite[] walk;
    public Sprite[] attack;
    public Sprite[] hurt;
    public Sprite[] death;
    [Tooltip("FPS cho animation Attack01/02 từ Tiny RPG pack.")]
    public float attackFps = 12f;

    [Header("4 hướng (ASEPRITE)")]
    public bool useFourDirections;
    public Sprite[] idleBack;
    public Sprite[] walkBack;
    [Tooltip("Nhìn sang phải.")]
    public Sprite[] idleSideRight;
    public Sprite[] walkSideRight;
    [Tooltip("Nhìn sang trái (nếu có — không cần flipX).")]
    public Sprite[] idleSideLeft;
    public Sprite[] walkSideLeft;
    [Tooltip("Attack nhìn lên (back).")]
    public Sprite[] attackBack;
    [Tooltip("Attack nhìn phải.")]
    public Sprite[] attackSideRight;
    [Tooltip("Attack nhìn trái.")]
    public Sprite[] attackSideLeft;

    public bool HasAttackAnimation => attack != null && attack.Length > 0;

    public bool HasFourWayAttack =>
        HasFourDirections
        && HasAttackAnimation
        && attackBack != null && attackBack.Length > 0
        && attackSideRight != null && attackSideRight.Length > 0
        && (attackSideLeft != null && attackSideLeft.Length > 0
            || attackSideRight != null && attackSideRight.Length > 0);

    public bool HasFourDirections =>
        useFourDirections
        && idleBack != null && idleBack.Length > 0
        && walkBack != null && walkBack.Length > 0
        && idleSideRight != null && idleSideRight.Length > 0
        && walkSideRight != null && walkSideRight.Length > 0;

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

    public string CombatStyleLabel => WeaponStyleUtil.GetCombatStyleLabel(combatClass);

    public string CombatStyleDescription => WeaponStyleUtil.GetCombatStyleDescription(combatClass);
}
