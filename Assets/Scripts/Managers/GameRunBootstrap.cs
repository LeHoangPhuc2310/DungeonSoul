// DungeonSoul — GameRunBootstrap.cs — Khởi tạo run, mobile, chọn mode Wave/Dungeon.

using UnityEngine;

public enum GameRunMode
{
    Survival,
    WaveArena,
    ProceduralDungeon
}

public class GameRunBootstrap : MonoBehaviour
{
    public static GameRunBootstrap Instance { get; private set; }

    [SerializeField] private GameRunMode runMode = GameRunMode.Survival;
    [SerializeField] private bool applyMobileSettings = true;
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool preventScreenSleep = true;

    public GameRunMode Mode => runMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!RunEntryGate.ConfirmedThisPlaySession)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelectScene");
            return;
        }

        if (applyMobileSettings)
            ApplyMobileSettingsInstance();

        EnsureManagers();
    }

    private void Start()
    {
        if (runMode == GameRunMode.ProceduralDungeon)
        {
            DungeonRunController dungeon = GetComponent<DungeonRunController>();
            if (dungeon == null)
                dungeon = gameObject.AddComponent<DungeonRunController>();
            dungeon.BeginDungeonRun();
        }
        else if (runMode == GameRunMode.WaveArena)
        {
            DisableSurvivalRun();
            RoomManager room = RoomManager.Instance;
            if (room != null)
                room.SetWaveMode(true);
        }
        else if (runMode == GameRunMode.Survival)
        {
            EnsureSurvivalRun();
        }

        RefreshPlayerVisual();
        Invoke(nameof(RefreshPlayerVisual), 0.2f);

        EnsureTrapSpawner();
        EnsureHealthPotionSpawner();
    }

    /// <summary>Gắn TrapSpawner (rải bẫy chông lên sàn) — dùng chung GameObject với EnemySpawner
    /// để tận dụng FloorLayer/WallLayer, fallback DungeonGrid.</summary>
    private void EnsureTrapSpawner()
    {
        if (Object.FindAnyObjectByType<TrapSpawner>() != null)
            return;

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        GameObject host = spawner != null ? spawner.gameObject : GameObject.Find("DungeonGrid");
        if (host == null)
            return;

        if (host.GetComponent<TrapSpawner>() == null)
            host.AddComponent<TrapSpawner>();
    }

    /// <summary>Gắn HealthPotionSpawner (thả ngẫu nhiên bình thuốc hồi máu lên sàn).</summary>
    private void EnsureHealthPotionSpawner()
    {
        if (Object.FindAnyObjectByType<HealthPotionSpawner>() != null)
            return;

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        GameObject host = spawner != null ? spawner.gameObject : GameObject.Find("DungeonGrid");
        if (host == null)
            return;

        if (host.GetComponent<HealthPotionSpawner>() == null)
            host.AddComponent<HealthPotionSpawner>();
    }

    private void RefreshPlayerVisual()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayerController ctrl = player.GetComponent<PlayerController>();
        if (ctrl == null)
            return;

        HeroType hero = HeroRunStats.Instance != null
            ? HeroRunStats.Instance.SelectedHero
            : (HeroType)Mathf.Clamp(PlayerPrefs.GetInt("ds_selected_hero", 0), 0, 2);
        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        if (entry != null)
            ctrl.ApplyPlayableCharacter(entry);
        else
            ctrl.ApplyHeroVisual(hero);
        GameplayPresentation.Instance?.ApplyPlayerScale();
    }

    private void ApplyMobileSettingsInstance()
    {
        Application.targetFrameRate = targetFrameRate > 0 ? targetFrameRate : 60;
        Input.multiTouchEnabled = true;
        QualitySettings.vSyncCount = 0;
        if (preventScreenSleep)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private static void EnsureManagers()
    {
        Ensure<RunManager>("RunManager");
        Ensure<SurvivalRunManager>("SurvivalRunManager");
        Ensure<BossSpawnManager>("BossSpawnManager");
        Ensure<AchievementManager>("AchievementManager");
        Ensure<HeroRunStats>("HeroRunStats");
        Ensure<RoomClearBridge>("RoomClearBridge");

        HUDManager hud = Object.FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);
        if (hud != null && hud.GetComponent<HudPauseButton>() == null)
            hud.gameObject.AddComponent<HudPauseButton>();

        if (Object.FindAnyObjectByType<MobileSafeArea>() == null)
        {
            GameObject safe = new GameObject("MobileSafeArea");
            safe.AddComponent<MobileSafeArea>();
        }

        if (Object.FindAnyObjectByType<FontSwitcher>() == null)
        {
            GameObject font = new GameObject("FontSwitcher");
            font.AddComponent<FontSwitcher>();
        }
    }

    private void EnsureSurvivalRun()
    {
        SurvivalRunManager mgr = Ensure<SurvivalRunManager>("SurvivalRunManager");
        mgr.SetSurvivalMode(true);
        mgr.ResetForNewRun();

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
            spawner.EnableSurvivalMode(true);
    }

    private void DisableSurvivalRun()
    {
        SurvivalRunManager mgr = Object.FindAnyObjectByType<SurvivalRunManager>();
        mgr?.SetSurvivalMode(false);

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
            spawner.EnableSurvivalMode(false);
    }

    private static T Ensure<T>(string name) where T : Component
    {
        T existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name);
        return go.AddComponent<T>();
    }
}
