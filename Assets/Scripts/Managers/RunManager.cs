using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    private int runCoins;
    private int runScore;
    private int killCount;
    private bool runActive = true;
    private SkillSelectionChoice lockedSkillPurchase;

    public int RunScore => runScore;
    public int RunCoins => runCoins;
    public int KillCount => killCount;
    public bool HasLockedSkillPurchase => lockedSkillPurchase != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddRunCoins(int amount)
    {
        if (!runActive || amount <= 0)
            return;
        runCoins += amount;
        HUDManager.Resolve()?.ForceRefreshFromSystems(false);
    }

    /// <summary>Trừ coin trong run (reroll, v.v.). Trả về false nếu không đủ.</summary>
    public bool TrySpendRunCoins(int amount)
    {
        if (!runActive || amount <= 0 || runCoins < amount)
            return false;

        runCoins -= amount;
        HUDManager.Resolve()?.ForceRefreshFromSystems(false);
        return true;
    }

    public void AddRunScore(int amount)
    {
        if (!runActive || amount <= 0)
            return;
        runScore += amount;
        HUDManager.Resolve()?.ForceRefreshFromSystems(false);
    }

    public void RegisterKill()
    {
        if (!runActive)
            return;
        killCount++;
    }

    public void OnBossDefeated()
    {
        if (SurvivalRunManager.IsSurvivalMode())
            return;

        if (FloorManager.Instance != null && FloorManager.Instance.CurrentFloor >= 10)
            EndRun(true);
    }

    public void EndRun(bool victory)
    {
        // Chống gọi đôi (vd boss wave 10: OnBossDefeated + CompleteChestReward đều gọi EndRun).
        if (!runActive)
            return;

        runActive = false;

        string result = victory ? "VICTORY" : "GAME OVER";
        Debug.Log($"[Run] {result} | Score={runScore} | Coins={runCoins}");

        if (victory)
            AudioManager.PlayVictory();
        else
            AudioManager.PlayGameOver();

        float survivalSec = SurvivalRunManager.Instance != null
            ? SurvivalRunManager.Instance.ElapsedSeconds
            : 0f;
        int soulsEarned = MetaRunProgress.RecordRun(victory, runScore, killCount, survivalSec);

        EventBus.InvokeRunEnded(victory);
        HUDManager.Resolve()?.ShowRunResult(victory, runScore, runCoins, soulsEarned, survivalSec);
    }

    public void ResetForNewRun()
    {
        runActive = true;
        runCoins = 0;
        runScore = 0;
        killCount = 0;
        lockedSkillPurchase = null;
        SurvivalRunManager.Instance?.ResetForNewRun();
    }

    public SkillSelectionChoice PeekLockedSkillPurchase()
    {
        return lockedSkillPurchase;
    }

    public void SetLockedSkillPurchase(SkillSelectionChoice choice)
    {
        lockedSkillPurchase = choice != null ? choice.Clone() : null;
    }

    public void ClearLockedSkillPurchase()
    {
        lockedSkillPurchase = null;
    }
}