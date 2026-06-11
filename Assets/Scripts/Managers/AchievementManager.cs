using System.Text;
using UnityEngine;

/// <summary>Achievement vĩnh viễn (PlayerPrefs) + toast UI.</summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private const string KeyKillTotal = "ds_ach_kills";
    private const string KeyFirstRun = "ds_ach_first_run";
    private const string KeyLevel5 = "ds_ach_lv5";
    private const string KeyGoblinSlayer = "ds_ach_goblin_slayer";
    private const string KeyDungeonMaster = "ds_ach_dungeon_master";
    private const string KeySurvivalMaster = "ds_ach_survival_master";

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

    private void OnEnable()
    {
        EventBus.OnEnemyKilled += HandleEnemyKilled;
        EventBus.OnPlayerLevelUp += HandlePlayerLevelUp;
        EventBus.OnRunEnded += HandleRunEnded;
    }

    private void OnDisable()
    {
        EventBus.OnEnemyKilled -= HandleEnemyKilled;
        EventBus.OnPlayerLevelUp -= HandlePlayerLevelUp;
        EventBus.OnRunEnded -= HandleRunEnded;
    }

    public void OnRunStarted()
    {
        if (PlayerPrefs.GetInt(KeyFirstRun, 0) != 0)
            return;

        Unlock("First Steps", "Bắt đầu run đầu tiên");
        PlayerPrefs.SetInt(KeyFirstRun, 1);
        PlayerPrefs.Save();
    }

    private void HandleEnemyKilled(EnemyKilledInfo info) => OnEnemyKilled();

    public void OnEnemyKilled()
    {
        TotalKills++;
        PlayerPrefs.SetInt(KeyKillTotal, TotalKills);

        if (TotalKills % 10 == 0)
            PlayerPrefs.Save();

        if (TotalKills >= 100 && PlayerPrefs.GetInt(KeyGoblinSlayer, 0) == 0)
        {
            Unlock("Goblin Slayer", "Tiêu diệt 100 quái (tổng)");
            PlayerPrefs.SetInt(KeyGoblinSlayer, 1);
            PlayerPrefs.Save();
        }
    }

    private void HandlePlayerLevelUp(int level) => OnPlayerLevel(level);

    public void OnPlayerLevel(int level)
    {
        if (level < 5 || PlayerPrefs.GetInt(KeyLevel5, 0) != 0)
            return;

        Unlock("Rising Star", "Đạt cấp 5 trong một run");
        PlayerPrefs.SetInt(KeyLevel5, 1);
        PlayerPrefs.Save();
    }

    private void HandleRunEnded(bool victory)
    {
        if (SurvivalRunManager.IsSurvivalMode())
        {
            if (victory)
                NotifySurvivalVictory();
            return;
        }

        int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
        OnRunEnded(victory, floor);
    }

    public void OnRunEnded(bool victory, int floor)
    {
        if (!victory || floor < 10 || PlayerPrefs.GetInt(KeyDungeonMaster, 0) != 0)
            return;

        Unlock("Dungeon Master", "Hoàn thành 10 tầng wave arena");
        PlayerPrefs.SetInt(KeyDungeonMaster, 1);
        PlayerPrefs.Save();
    }

    public static void NotifySurvivalVictory()
    {
        if (PlayerPrefs.GetInt(KeySurvivalMaster, 0) != 0)
            return;

        UnlockStatic("Survival Master", "Sống sót đủ 30 phút");
        PlayerPrefs.SetInt(KeySurvivalMaster, 1);
        PlayerPrefs.Save();
    }

    public static string GetAchievementReport()
    {
        var sb = new StringBuilder(512);
        AppendAch(sb, "First Steps", KeyFirstRun, "Bắt đầu run đầu tiên");
        AppendAch(sb, "Goblin Slayer", KeyGoblinSlayer, "100 kill tổng");
        AppendAch(sb, "Rising Star", KeyLevel5, "Cấp 5 trong run");
        AppendAch(sb, "Dungeon Master", KeyDungeonMaster, "Thắng wave 10 tầng");
        AppendAch(sb, "Survival Master", KeySurvivalMaster, "Sống sót 30 phút");
        sb.Append("\nTổng kill: ").Append(PlayerPrefs.GetInt(KeyKillTotal, 0));
        return sb.ToString();
    }

    private static void AppendAch(StringBuilder sb, string name, string key, string desc)
    {
        bool done = PlayerPrefs.GetInt(key, 0) == 1;
        sb.Append(done ? "[✓] " : "[ ] ");
        sb.Append(name).Append(" — ").Append(desc).Append('\n');
    }

    private void Unlock(string title, string description)
    {
        UnlockStatic(title, description);
    }

    private static void UnlockStatic(string title, string description)
    {
        Debug.Log("[Achievement] Mở khóa: " + title);
        AchievementToastUI.Show(title, description);
    }
}
