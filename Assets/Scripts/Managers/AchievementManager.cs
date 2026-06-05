using UnityEngine;

/// <summary>Tracks a subset of GDD F.1 achievements via PlayerPrefs.</summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private const string KeyKillTotal = "ds_ach_kills";
    private const string KeyFirstRun = "ds_ach_first_run";
    private const string KeyLevel5 = "ds_ach_lv5";

    public int TotalKills { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        TotalKills = PlayerPrefs.GetInt(KeyKillTotal, 0);
    }

    public void OnRunStarted()
    {
        if (PlayerPrefs.GetInt(KeyFirstRun, 0) == 0)
        {
            Unlock("First Steps", 50);
            PlayerPrefs.SetInt(KeyFirstRun, 1);
            PlayerPrefs.Save();
        }
    }

    public void OnEnemyKilled()
    {
        TotalKills++;
        PlayerPrefs.SetInt(KeyKillTotal, TotalKills);
        PlayerPrefs.Save();

        if (TotalKills >= 100)
            Unlock("Goblin Slayer", 100);
    }

    public void OnPlayerLevel(int level)
    {
        if (level >= 5 && PlayerPrefs.GetInt(KeyLevel5, 0) == 0)
        {
            Unlock("Rising Star", 75);
            PlayerPrefs.SetInt(KeyLevel5, 1);
            PlayerPrefs.Save();
        }
    }

    public void OnRunEnded(bool victory, int floor)
    {
        if (victory && floor >= 10)
            Unlock("Dungeon Master", 200);
    }

    private static void Unlock(string title, int metaCoinReward)
    {
        Debug.Log("[Achievement] " + title + " +" + metaCoinReward + " meta xu");
        MetaProgression.Instance?.AddMetaCoins(metaCoinReward);
    }
}
