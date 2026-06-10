using UnityEngine;

/// <summary>Tracks a subset of GDD F.1 achievements via PlayerPrefs.</summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private const string KeyKillTotal = "ds_ach_kills";
    private const string KeyFirstRun = "ds_ach_first_run";
    private const string KeyLevel5 = "ds_ach_lv5";
    private const string KeyGoblinSlayer = "ds_ach_goblin_slayer";
    private const string KeyDungeonMaster = "ds_ach_dungeon_master";

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
        if (PlayerPrefs.GetInt(KeyFirstRun, 0) != 0)
            return;

        Unlock("First Steps");
        PlayerPrefs.SetInt(KeyFirstRun, 1);
        PlayerPrefs.Save();
    }

    public void OnEnemyKilled()
    {
        TotalKills++;
        PlayerPrefs.SetInt(KeyKillTotal, TotalKills);

        // Chỉ save khi đạt mốc — tránh PlayerPrefs.Save() mỗi kill (gây lag/đứng trên WebGL).
        if (TotalKills % 10 == 0)
            PlayerPrefs.Save();

        if (TotalKills >= 100 && PlayerPrefs.GetInt(KeyGoblinSlayer, 0) == 0)
        {
            Unlock("Goblin Slayer");
            PlayerPrefs.SetInt(KeyGoblinSlayer, 1);
            PlayerPrefs.Save();
        }
    }

    public void OnPlayerLevel(int level)
    {
        if (level < 5 || PlayerPrefs.GetInt(KeyLevel5, 0) != 0)
            return;

        Unlock("Rising Star");
        PlayerPrefs.SetInt(KeyLevel5, 1);
        PlayerPrefs.Save();
    }

    public void OnRunEnded(bool victory, int floor)
    {
        if (!victory || floor < 10 || PlayerPrefs.GetInt(KeyDungeonMaster, 0) != 0)
            return;

        Unlock("Dungeon Master");
        PlayerPrefs.SetInt(KeyDungeonMaster, 1);
        PlayerPrefs.Save();
    }

    private static void Unlock(string title)
    {
        Debug.Log("[Achievement] Mở khóa: " + title);
    }
}
