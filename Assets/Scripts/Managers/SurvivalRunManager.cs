using System.Collections.Generic;
using UnityEngine;

/// <summary>Run survival kiểu Vampire Survivors — đồng hồ, kill count, boss theo phút, thắng khi hết giờ.</summary>
public class SurvivalRunManager : MonoBehaviour
{
    public static SurvivalRunManager Instance { get; private set; }

    [SerializeField] private bool survivalModeEnabled = true;

    private SurvivalRunConfig config;
    private float elapsedSeconds;
    private bool runActive = true;
    private readonly HashSet<int> triggeredBossIndices = new HashSet<int>();
    private bool victoryTriggered;
    private bool reaperSpawned;

    public bool IsActive => survivalModeEnabled;

    public void SetSurvivalMode(bool enabled)
    {
        survivalModeEnabled = enabled;
    }
    public float ElapsedSeconds => elapsedSeconds;
    public int KillCount => RunManager.Instance != null ? RunManager.Instance.KillCount : 0;
    public float SurvivalDuration => config != null ? config.survivalDurationSeconds : 1800f;

    public static bool IsSurvivalMode()
    {
        if (GameRunBootstrap.Instance != null)
        {
            if (GameRunBootstrap.Instance.Mode == GameRunMode.WaveArena)
                return false;
            if (GameRunBootstrap.Instance.Mode == GameRunMode.Survival)
                return true;
        }

        return Instance != null && Instance.IsActive;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        config = SurvivalRunConfig.Get();
    }

    private void OnEnable()
    {
        EventBus.OnEnemyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        EventBus.OnEnemyKilled -= OnEnemyKilled;
    }

    private void Update()
    {
        if (!survivalModeEnabled || !runActive || victoryTriggered)
            return;

        if (Time.timeScale <= 0f)
            return;

        elapsedSeconds += Time.deltaTime;
        CheckBossMilestones();
        CheckReaperSpawn();
        CheckVictory();
    }

    public void ResetForNewRun()
    {
        config = SurvivalRunConfig.Get();
        elapsedSeconds = 0f;
        runActive = true;
        victoryTriggered = false;
        triggeredBossIndices.Clear();
        reaperSpawned = false;
        BanishRegistry.ResetForNewRun();
    }

    public int GetDifficultyTier()
    {
        float minutesPerTier = config != null ? config.minutesPerDifficultyTier : 2f;
        if (minutesPerTier <= 0.01f)
            minutesPerTier = 2f;

        float minutes = elapsedSeconds / 60f;
        return 1 + Mathf.FloorToInt(minutes / minutesPerTier);
    }

    public float GetSpawnInterval()
    {
        if (config == null)
            return 2.5f;

        float minutes = elapsedSeconds / 60f;
        float interval = config.initialSpawnInterval - minutes * config.spawnIntervalReductionPerMinute;
        return Mathf.Max(config.minSpawnInterval, interval);
    }

    public int GetMaxEnemiesOnScreen()
    {
        return config != null ? config.maxEnemiesOnScreen : 50;
    }

    public int GetInitialBurstCount()
    {
        return config != null ? config.initialBurstCount : 12;
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int m = total / 60;
        int s = total % 60;
        return m.ToString("00") + ":" + s.ToString("00");
    }

    private void OnEnemyKilled(EnemyKilledInfo info)
    {
        if (!survivalModeEnabled || !runActive || info.IsBoss)
            return;

        RunManager.Instance?.RegisterKill();
        HUDManager.Resolve()?.RefreshSurvivalHud();
    }

    private void CheckBossMilestones()
    {
        if (config == null || config.bossSpawnTimesSeconds == null)
            return;

        for (int i = 0; i < config.bossSpawnTimesSeconds.Length; i++)
        {
            if (triggeredBossIndices.Contains(i))
                continue;

            if (elapsedSeconds < config.bossSpawnTimesSeconds[i])
                continue;

            triggeredBossIndices.Add(i);
            SpawnSurvivalBoss(i);
        }
    }

    private void SpawnSurvivalBoss(int bossIndex)
    {
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        Vector3 pos = spawner != null ? spawner.GetRandomSpawnPosition() : Vector3.zero;
        GameObject prefab = spawner != null ? spawner.enemyPrefab : null;

        int waveForScale = 3 + bossIndex * 2;
        BossSpawnManager.SpawnForSurvivalTier(bossIndex, waveForScale, pos, prefab);

        string bossName = BossSpawnManager.GetBossNameForTier(bossIndex);
        HUDManager.Resolve()?.ShowWaveAnnouncement("BOSS — " + bossName);
        Debug.Log("[Survival] Boss milestone " + (bossIndex + 1) + " at " + FormatTime(elapsedSeconds));
    }

    private void CheckReaperSpawn()
    {
        if (config == null || reaperSpawned || config.reaperSpawnBeforeEndSeconds <= 0f)
            return;

        float triggerAt = config.survivalDurationSeconds - config.reaperSpawnBeforeEndSeconds;
        if (elapsedSeconds < triggerAt)
            return;

        reaperSpawned = true;
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        Vector3 pos = spawner != null ? spawner.GetRandomSpawnPosition() : Vector3.zero;
        BossSpawnManager.SpawnForSurvivalTier(3, 10, pos, spawner != null ? spawner.enemyPrefab : null);
        HUDManager.Resolve()?.ShowWaveAnnouncement("☠ DEATH ĐẾN — SỐNG SÓT!");
    }

    private void CheckVictory()
    {
        if (config == null || victoryTriggered)
            return;

        if (elapsedSeconds < config.survivalDurationSeconds)
            return;

        victoryTriggered = true;
        runActive = false;
        Debug.Log("[Survival] Victory — survived " + FormatTime(elapsedSeconds));
        HUDManager.Resolve()?.ShowWaveAnnouncement("SỐNG SÓT — CHIẾN THẮNG!");
        RunManager.Instance?.EndRun(true);
    }

    public bool IsFinalBossTier(int tierIndex)
    {
        if (config?.bossSpawnTimesSeconds == null)
            return false;

        return tierIndex >= config.bossSpawnTimesSeconds.Length - 1;
    }
}
