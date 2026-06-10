// DungeonSoul — GameRunBootstrap.cs — Khởi tạo run, mobile, chọn mode Wave/Dungeon.

using UnityEngine;

public enum GameRunMode
{
    WaveArena,
    ProceduralDungeon
}

public class GameRunBootstrap : MonoBehaviour
{
    public static GameRunBootstrap Instance { get; private set; }

    [SerializeField] private GameRunMode runMode = GameRunMode.WaveArena;
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
        else
        {
            RoomManager room = RoomManager.Instance;
            if (room != null)
                room.SetWaveMode(true);
        }

        RefreshPlayerVisual();
        Invoke(nameof(RefreshPlayerVisual), 0.2f);
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

    private static T Ensure<T>(string name) where T : Component
    {
        T existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name);
        return go.AddComponent<T>();
    }
}
