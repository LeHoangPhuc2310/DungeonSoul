using UnityEngine;

/// <summary>Cấu hình run kiểu Vampire Survivors — timer, boss theo phút, spawn curve.</summary>
[CreateAssetMenu(fileName = "SurvivalRunConfig", menuName = "DungeonSoul/Run/Survival Run Config")]
public class SurvivalRunConfig : ScriptableObject
{
    [Header("Thời gian")]
    [Tooltip("Sống sót đủ thời gian này = thắng (giây). VS mặc định 30 phút.")]
    public float survivalDurationSeconds = 1800f;

    [Tooltip("Boss xuất hiện tại các mốc thời gian (giây).")]
    public float[] bossSpawnTimesSeconds = { 300f, 600f, 1200f, 1500f };

    [Tooltip("Spawn Death/Reaper trước khi hết giờ (giây). 0 = tắt.")]
    public float reaperSpawnBeforeEndSeconds = 90f;

    [Header("Spawn liên tục")]
    public int initialBurstCount = 24;
    public float initialSpawnInterval = 1.9f;
    public float minSpawnInterval = 0.35f;
    public int maxEnemiesOnScreen = 90;
    [Tooltip("Sau mỗi phút, giảm interval spawn thêm bao nhiêu giây.")]
    public float spawnIntervalReductionPerMinute = 0.14f;

    [Header("Độ khó theo thời gian")]
    [Tooltip("Mỗi N phút tăng 1 tier (dùng cho HP/damage quái).")]
    public float minutesPerDifficultyTier = 2f;

    private static SurvivalRunConfig cached;

    public static SurvivalRunConfig Get()
    {
        if (cached != null)
            return cached;

        cached = Resources.Load<SurvivalRunConfig>("SurvivalRunConfig");
        if (cached == null)
        {
            cached = CreateInstance<SurvivalRunConfig>();
            cached.hideFlags = HideFlags.HideAndDontSave;
        }

        return cached;
    }
}
