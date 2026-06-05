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

    private void Start()
    {
        MetaProgression.Instance?.ApplyToPlayer();
    }

    public void AddRunCoins(int amount)
    {
        if (!runActive || amount <= 0)
            return;
        runCoins += amount;
        HUDManager.Resolve()?.ForceRefreshFromSystems(false);
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
        bool firstEnd = runActive;
        runActive = false;

        if (firstEnd)
            MetaProgression.Instance?.AddMetaCoins(runCoins);

        string result = victory ? "VICTORY" : "GAME OVER";
        Debug.Log($"[Run] {result} | Score={runScore} | Coins saved to meta={runCoins}");

        HUDManager.Resolve()?.ShowRunResult(victory, runScore, runCoins);
    }

    public void ResetForNewRun()
    {
        runActive = true;
        runCoins = 0;
        runScore = 0;
    }
}