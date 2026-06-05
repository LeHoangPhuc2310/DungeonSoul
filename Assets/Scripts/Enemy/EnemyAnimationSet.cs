// DungeonSoul — EnemyAnimationSet.cs — Bộ frame + metadata cho một loại quái.

using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAnimationSet", menuName = "DungeonSoul/Enemy Animation Set")]
public class EnemyAnimationSet : ScriptableObject
{
    public string id = "enemy";
    public string displayName = "Enemy";
    [TextArea(2, 4)] public string description = "";
    public EnemyArchetype defaultArchetype = EnemyArchetype.Grunt;

    [Header("UI — Bonus (KnightFall style)")]
    public string bonusPositive = "";
    public string bonusNegative = "";
    public string abilityName = "";
    [TextArea(2, 5)] public string abilityDescription = "";

    [Header("Sprites")]
    public Sprite[] idle;
    public Sprite[] move;
    public Sprite[] hurt;
    public Sprite[] death;
    public Sprite[] attack;

    [Header("Timing")]
    [Range(4f, 24f)] public float idleFps = 8f;
    [Range(4f, 24f)] public float moveFps = 12f;
    [Range(4f, 24f)] public float hurtFps = 10f;
    [Range(4f, 24f)] public float deathFps = 10f;
    [Range(0.5f, 8f)] public float visualScale = 2f;

    public Sprite PreviewSprite =>
        idle != null && idle.Length > 0 ? idle[0]
        : move != null && move.Length > 0 ? move[0]
        : null;

    public float DeathDuration =>
        death != null && death.Length > 0 ? death.Length / Mathf.Max(1f, deathFps) : 0.35f;
}
