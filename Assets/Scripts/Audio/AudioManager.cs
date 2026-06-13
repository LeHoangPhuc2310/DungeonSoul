using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>BGM + SFX từ pack TheHeroKnight (Resources/Audio/HeroKnight).</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>Đảm bảo có AudioManager + AudioListener (mọi scene, kể cả menu không có camera listener).</summary>
    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        AudioManager existing = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            existing.EnsureAudioListener();
            return;
        }

        GameObject go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
    }

    private const string AudioRoot = "Audio/HeroKnight/";
    private const string SupplementalRoot = "Audio/Supplemental/";

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private AudioClip bgMusic;
    private AudioClip combatMusic;
    private AudioClip levelUpClip;
    private AudioClip coinClip;
    private AudioClip uiClickClip;
    private AudioClip roomClearClip;
    private AudioClip healClip;
    private AudioClip swordClip;
    private AudioClip arrowClip;
    private AudioClip pickupClip;
    private AudioClip combatHitClip;
    private AudioClip bossAttackClip;
    private AudioClip gameOverClip;
    private AudioClip victoryClip;
    private AudioClip enemyAttackClip;
    private AudioClip playerHurtClip;
    private AudioClip dashClip;
    private AudioClip bossSpawnClip;
    private AudioClip lightningClip;
    private AudioClip lightningZapClip;
    private AudioClip trapSpikeClip;

    private float lastEnemyAttackTime = -1f;
    private float lastPlayerHurtTime = -1f;
    private float lastLightningTime = -1f;
    private float lastTrapSpikeTime = -1f;

    private bool combatMusicActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureAudioListener();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0.42f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 0.85f;

        LoadClips();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListener();
    }

    private void Start()
    {
        PlayBackgroundMusic();
    }

    private void EnsureAudioListener()
    {
        AudioListener listener = GetComponent<AudioListener>();
        if (listener == null)
            listener = gameObject.AddComponent<AudioListener>();
        listener.enabled = true;

        // Unity chỉ cần một listener — tắt listener trên camera scene để tránh trùng.
        AudioListener[] all = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i] != listener)
                all[i].enabled = false;
        }
    }

    private void LoadClips()
    {
        bgMusic = LoadClip("BgSound");
        combatMusic = LoadClip("ComBat");
        levelUpClip = LoadSupplemental("in-game-level-uptype-2-230567");
        coinClip = LoadClipOrSupplemental("Pickup", "retro-coin-4-236671");
        uiClickClip = LoadClipOrSupplemental("Pickup", "mixkit-player-jumping-in-a-video-game-2043");
        roomClearClip = LoadSupplemental("rocky-victory1");
        healClip = LoadClip("Heal");
        swordClip = LoadClipOrSupplemental("Sword", "slash-21834");
        arrowClip = LoadClip("Arrow");
        pickupClip = LoadClip("Pickup");
        combatHitClip = LoadClip("SwordBlood");
        bossAttackClip = LoadClip("Attack2");
        gameOverClip = LoadClip("GameOver");
        // "Victory.mp3" trong pack thực ra là nhạc dài ~3.6 phút → dùng jingle ngắn cho khoảnh khắc thắng.
        victoryClip = LoadSupplemental("rocky-victory1");
        enemyAttackClip = LoadClip("EnemyAttack");
        playerHurtClip = LoadClipOrSupplemental("SwordBlood", "slash-21834");
        dashClip = LoadClip("Dash");
        bossSpawnClip = LoadClip("MonsterBreath");
        lightningClip = LoadSupplemental("lightning-crack")
            ?? LoadSupplemental("lightning")
            ?? ProceduralLightningSfx.GetCrack();
        lightningZapClip = LoadSupplemental("lightning-zap") ?? ProceduralLightningSfx.GetZap();
        trapSpikeClip = LoadSupplemental("trap-spike") ?? ProceduralTrapSfx.GetSpikePop();
        if (bgMusic == null)
            bgMusic = LoadSupplemental("Simulacra-chosic.com_");
    }

    private static AudioClip LoadClip(string fileName)
    {
        return Resources.Load<AudioClip>(AudioRoot + fileName);
    }

    private static AudioClip LoadSupplemental(string fileName)
    {
        return Resources.Load<AudioClip>(SupplementalRoot + fileName);
    }

    private static AudioClip LoadClipOrSupplemental(string heroKnightName, string supplementalName)
    {
        AudioClip clip = LoadClip(heroKnightName);
        return clip != null ? clip : LoadSupplemental(supplementalName);
    }

    public static void PlayBackgroundMusic()
    {
        if (Instance == null)
            return;
        Instance.combatMusicActive = false;
        Instance.PlayMusicLoop(Instance.bgMusic, 0.42f);
    }

    public static void PlayCombatMusic()
    {
        if (Instance == null || Instance.combatMusic == null)
            return;
        if (Instance.combatMusicActive && Instance.musicSource.clip == Instance.combatMusic)
            return;

        Instance.combatMusicActive = true;
        Instance.PlayMusicLoop(Instance.combatMusic, 0.5f);
    }

    public static void PlayLevelUp()
    {
        Instance?.PlayOneShot(Instance.levelUpClip ?? Instance.victoryClip, 0.95f, 1f);
    }

    public static void PlayCoinCollect()
    {
        Instance?.PlayOneShot(Instance.coinClip ?? Instance.pickupClip, 0.9f, 1.05f);
    }

    public static void PlayUiTap()
    {
        Instance?.PlayOneShot(Instance.uiClickClip, 0.55f, 1f);
    }

    public static void PlayRoomClear()
    {
        Instance?.PlayOneShot(Instance.roomClearClip ?? Instance.victoryClip, 0.85f, 1f);
    }

    public static void PlayHeal()
    {
        Instance?.PlayOneShot(Instance.healClip, 0.8f, 1f);
    }

    public static void PlaySwordSwing()
    {
        Instance?.PlayOneShot(Instance.swordClip, 0.75f, Random.Range(0.95f, 1.05f));
    }

    public static void PlayArrowShot()
    {
        Instance?.PlayOneShot(Instance.arrowClip, 0.7f, Random.Range(0.95f, 1.08f));
    }

    public static void PlayCombatHit()
    {
        Instance?.PlayOneShot(Instance.combatHitClip, 0.65f, 1f);
    }

    /// <summary>Tiếng sét — LightningChain, Cung Bão, Thunder Rod…</summary>
    public static void PlayLightning(bool chainZap = false)
    {
        if (Instance == null)
            return;

        float throttle = chainZap ? 0.04f : 0.07f;
        if (Time.unscaledTime - Instance.lastLightningTime < throttle)
            return;
        Instance.lastLightningTime = Time.unscaledTime;

        AudioClip clip = chainZap ? Instance.lightningZapClip : Instance.lightningClip;
        float vol = chainZap ? 0.62f : 0.78f;
        float pitch = Random.Range(0.92f, 1.08f);
        Instance.PlayOneShot(clip, vol, pitch);
    }

    /// <summary>Tiếng chông nhô từ bẫy sàn.</summary>
    public static void PlayTrapSpike()
    {
        if (Instance == null)
            return;

        if (Time.unscaledTime - Instance.lastTrapSpikeTime < 0.06f)
            return;
        Instance.lastTrapSpikeTime = Time.unscaledTime;
        Instance.PlayOneShot(Instance.trapSpikeClip, 0.42f, Random.Range(0.94f, 1.06f));
    }

    public static void PlayBossAttack()
    {
        Instance?.PlayOneShot(Instance.bossAttackClip, 0.8f, 1f);
    }

    /// <summary>Quái thường đánh player — throttle để không "ò ó o" khi nhiều quái đánh cùng lúc.</summary>
    public static void PlayEnemyAttack()
    {
        if (Instance == null)
            return;
        if (Time.unscaledTime - Instance.lastEnemyAttackTime < 0.08f)
            return;
        Instance.lastEnemyAttackTime = Time.unscaledTime;
        Instance.PlayOneShot(Instance.enemyAttackClip, 0.55f, Random.Range(0.95f, 1.06f));
    }

    /// <summary>Player ăn đòn — throttle tránh chồng tiếng khi bị vây.</summary>
    public static void PlayPlayerHurt()
    {
        if (Instance == null)
            return;
        if (Time.unscaledTime - Instance.lastPlayerHurtTime < 0.12f)
            return;
        Instance.lastPlayerHurtTime = Time.unscaledTime;
        Instance.PlayOneShot(Instance.playerHurtClip, 0.7f, Random.Range(0.96f, 1.04f));
    }

    public static void PlayDash()
    {
        Instance?.PlayOneShot(Instance.dashClip, 0.7f, 1f);
    }

    public static void PlayBossSpawn()
    {
        Instance?.PlayOneShot(Instance.bossSpawnClip, 0.9f, 1f);
    }

    public static void PlayVictory()
    {
        if (Instance == null)
            return;
        Instance.combatMusicActive = false;
        Instance.musicSource.Stop();
        Instance.PlayOneShot(Instance.victoryClip, 1f, 1f);
    }

    public static void PlayGameOver()
    {
        if (Instance == null)
            return;
        Instance.combatMusicActive = false;
        Instance.musicSource.Stop();
        Instance.PlayOneShot(Instance.gameOverClip, 1f, 1f);
    }

    private void PlayMusicLoop(AudioClip clip, float volume)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    private void PlayOneShot(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volume);
    }
}
