using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    private int runCoins;
    private int runScore;
    private bool runActive = true;

    public int RunScore => runScore;
    public int RunCoins => runCoins;

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

    public void OnBossDefeated()
    {
        if (FloorManager.Instance != null && FloorManager.Instance.CurrentFloor >= 10)
            EndRun(true);
    }

    public void EndRun(bool victory)
    {
        runActive = false;

        string result = victory ? "VICTORY" : "GAME OVER";
        Debug.Log($"[Run] {result} | Score={runScore} | Coins={runCoins}");

        if (victory)
            AudioManager.PlayVictory();
        else
            AudioManager.PlayGameOver();

        HUDManager.Resolve()?.ShowRunResult(victory, runScore, runCoins);
    }

    public void ResetForNewRun()
    {
        runActive = true;
        runCoins = 0;
        runScore = 0;
    }
}