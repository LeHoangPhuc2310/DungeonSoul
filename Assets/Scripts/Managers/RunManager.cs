using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    private int runCoins;
    private int runScore;
    private bool runActive = true;

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
    }

    public void AddRunScore(int amount)
    {
        if (!runActive || amount <= 0)
            return;
        runScore += amount;
    }

    public void OnBossDefeated()
    {
        if (FloorManager.Instance != null && FloorManager.Instance.CurrentFloor >= 10)
            EndRun(true);
    }

    public void EndRun(bool victory)
    {
        if (!runActive)
            return;

        runActive = false;
        MetaProgression.Instance?.AddMetaCoins(runCoins);

        string result = victory ? "VICTORY" : "GAME OVER";
        Debug.Log($"[Run] {result} | Score={runScore} | Coins saved to meta={runCoins}");

        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowRunResult(victory, runScore, runCoins);
    }
}