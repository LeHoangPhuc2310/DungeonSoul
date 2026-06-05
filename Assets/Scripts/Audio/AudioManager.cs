using UnityEngine;

/// <summary>BGM + SFX từ pack TheHeroKnight (Resources/Audio/HeroKnight).</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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

    private void Start()
    {
        PlayBackgroundMusic();
    }

    private void LoadClips()
    {
        bgMusic = LoadClip("BgSound");
        combatMusic = LoadClip("ComBat");
        levelUpClip = LoadClipOrSupplemental("Victory", "in-game-level-uptype-2-230567");
        coinClip = LoadClipOrSupplemental("Pickup", "retro-coin-4-236671");
        uiClickClip = LoadClipOrSupplemental("Pickup", "mixkit-player-jumping-in-a-video-game-2043");
        roomClearClip = LoadClipOrSupplemental("Victory", "rocky-victory1");
        healClip = LoadClip("Heal");
        swordClip = LoadClipOrSupplemental("Sword", "slash-21834");
        arrowClip = LoadClip("Arrow");
        pickupClip = LoadClip("Pickup");
        combatHitClip = LoadClip("SwordBlood");
        bossAttackClip = LoadClip("Attack2");
        gameOverClip = LoadClip("GameOver");
        victoryClip = LoadClipOrSupplemental("Victory", "rocky-victory1");
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

    public static void PlayBossAttack()
    {
        Instance?.PlayOneShot(Instance.bossAttackClip, 0.8f, 1f);
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
