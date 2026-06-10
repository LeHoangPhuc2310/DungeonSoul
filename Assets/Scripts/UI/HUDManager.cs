using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Top Left")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private GameObject skillsPanel;

    [Header("Top Center")]
    [SerializeField] private TMP_Text floorText;

    [Header("Top Right")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;

    [Header("Bottom Bar")]
    [SerializeField] private Image expFillImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;

    [Header("Damage Numbers")]
    [SerializeField] private GameObject damageNumberPrefab;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameOverUI gameOverUI;

    private HealthSystem playerHealth;
    private int score;
    private int coins;
    private float displayScore;
    private float displayCoins;
    private float scoreAnimVelocity;
    private float coinsAnimVelocity;
    private int currentFloor = 1;
    private bool runEnded;
    private bool hudBarStyleApplied;

    [Header("Score / Coins Animation")]
    [SerializeField] private float statAnimSmoothTime = 0.35f;

    [Header("EXP Animation")]
    [Tooltip("Seconds to fill the EXP bar from empty to full.")]
    [SerializeField] private float expBarFillDuration = 1.15f;

    private readonly ExpBarAnimator expAnimator = new ExpBarAnimator();
    private bool expSubscribed;
    private bool levelSubscribed;
    private float displayedExpFill;
    private float displayedExpValue;
    private float expFillVelocity;
    private float expValueVelocity;
    private static Sprite uiWhiteSprite;
    private GameObject weaponBarRoot;
    private readonly List<Image> weaponSlotImages = new List<Image>();
    private readonly List<HudHoverTooltip> weaponSlotTooltips = new List<HudHoverTooltip>();
    private GameObject passiveBarRoot;
    private readonly List<Image> passiveSlotImages = new List<Image>();
    private readonly List<TMP_Text> passiveLevelLabels = new List<TMP_Text>();
    private readonly List<HudHoverTooltip> passiveSlotTooltips = new List<HudHoverTooltip>();

    private const float ScoreFontSize = 28f;
    private const float CoinsFontSize = 28f;

    private static readonly Color ExpBarBg = new Color(0.08f, 0.09f, 0.14f, 0.92f);
    private static readonly Color ExpFillColor = new Color(0.28f, 0.58f, 0.98f, 1f);
    private static readonly Color LevelTextColor = new Color(1f, 0.88f, 0.35f, 1f);
    private static readonly Color ExpTextColor = new Color(0.82f, 0.86f, 0.92f, 0.95f);
    private static readonly Color HpBarBg = new Color(0.1f, 0.1f, 0.14f, 0.9f);
    private static readonly Color HpFillColor = new Color(0.92f, 0.22f, 0.24f, 1f);

    private const float ExpBarHeight = 32f;
    private const float HpBarHeight = 36f;
    private const float BottomPanelHeight = 72f;
    private static readonly Vector2 HpFillPadding = new Vector2(6f, 5f);
    private static readonly Vector2 ExpFillPadding = new Vector2(4f, 4f);

    public static HUDManager Resolve()
    {
        if (Instance != null)
            return Instance;

        Instance = Object.FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);
        return Instance;
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        EnsureCanvasScale();
        EnsureCanvasScaler();
        ResolveReferences();
        DisableStrayStatLabels();
        if (!hudBarStyleApplied)
        {
            ApplyHudStyle();
            hudBarStyleApplied = true;
        }

        EnsureGameplayUiSystems();
        EnsureKeepAlive();

        if (gameOverUI == null)
            gameOverUI = Object.FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
    }

    private void EnsureKeepAlive()
    {
        if (!Application.isPlaying)
            return;

        HudKeepAlive keepAlive = GetComponent<HudKeepAlive>();
        if (keepAlive == null)
            keepAlive = gameObject.AddComponent<HudKeepAlive>();
        keepAlive.target = this;
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        ResetForNewRun();
        UpdateHp();
        displayScore = score;
        displayCoins = coins;
        expAnimator.Configure(expBarFillDuration);
        SubscribeExpEvents();
        StartCoroutine(BindSystemsWhenReady());
        AchievementManager.Instance?.OnRunStarted();
    }

    private IEnumerator BindSystemsWhenReady()
    {
        float timeout = 3f;
        while (timeout > 0f && (ExpSystem.Instance == null || RunManager.Instance == null))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        SubscribeExpEvents();
        ForceRefreshFromSystems(true);
    }

    /// <summary>Push live ExpSystem / RunManager values onto HUD widgets.</summary>
    public void ForceRefreshFromSystems(bool snapExpBar = false)
    {
        if (!Application.isPlaying)
            return;

        ExpSystem exp = FindExpSystem();
        if (exp != null)
        {
            float cur = exp.CurrentExp;
            float max = Mathf.Max(1f, exp.ExpToNextLevel);
            int lv = exp.CurrentLevel;
            float fill = Mathf.Clamp01(cur / max);

            if (snapExpBar)
            {
                expAnimator.SnapTo(cur, max, lv);
                displayedExpFill = fill;
                displayedExpValue = cur;
                expFillVelocity = 0f;
                expValueVelocity = 0f;
                ApplyExpFillVisual(fill);
                if (expText != null)
                    expText.text = Mathf.FloorToInt(cur) + " / " + Mathf.FloorToInt(max);
            }
            else
            {
                expAnimator.SetTarget(cur, max, lv);
            }

            if (levelText != null)
                levelText.text = "LV." + lv;

            if (expText != null)
            {
                float showMax = Mathf.Max(1f, max);
                expText.text = Mathf.FloorToInt(cur) + " / " + Mathf.FloorToInt(showMax);
            }
        }

        RunManager run = FindRunManager();
        if (run != null)
        {
            score = run.RunScore;
            coins = run.RunCoins;
            displayScore = score;
            displayCoins = coins;
        }

        SyncFloorFromWave();

        RefreshScoreAndCoinLabels();
    }

    private void SyncFloorFromWave()
    {
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
            currentFloor = Mathf.Clamp(spawner.CurrentWave, 1, 10);

        if (floorText != null)
            floorText.text = "TẦNG " + currentFloor + " / 10";
    }

    private void OnDestroy()
    {
        UnsubscribeExpEvents();
    }

    private void SubscribeExpEvents()
    {
        ExpSystem exp = FindExpSystem();
        if (exp == null)
            return;

        if (!expSubscribed)
        {
            exp.OnExpChanged += OnExpChanged;
            expSubscribed = true;
        }

        if (!levelSubscribed)
        {
            exp.OnLevelUpEvent += OnPlayerLevelUp;
            levelSubscribed = true;
        }
    }

    private void UnsubscribeExpEvents()
    {
        if (ExpSystem.Instance == null)
        {
            expSubscribed = false;
            levelSubscribed = false;
            return;
        }

        if (expSubscribed)
        {
            ExpSystem.Instance.OnExpChanged -= OnExpChanged;
            expSubscribed = false;
        }

        if (levelSubscribed)
        {
            ExpSystem.Instance.OnLevelUpEvent -= OnPlayerLevelUp;
            levelSubscribed = false;
        }
    }

    private void OnExpChanged(float current, float max)
    {
        int level = ExpSystem.Instance != null ? ExpSystem.Instance.CurrentLevel : 1;
        SetExpTarget(current, max, level, false);
    }

    private void OnPlayerLevelUp(int level)
    {
        ForceRefreshFromSystems(true);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        EnsureCanvasScale();
        EnsureCanvasScaler();
        ResolveReferences();
        if (!hudBarStyleApplied)
        {
            ApplyHudStyle();
            hudBarStyleApplied = true;
        }

        ForceRefreshFromSystems(true);
    }

    private static ExpSystem FindExpSystem()
    {
        if (ExpSystem.Instance != null)
            return ExpSystem.Instance;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<ExpSystem>() : null;
    }

    private static RunManager FindRunManager()
    {
        if (RunManager.Instance != null)
            return RunManager.Instance;

        return Object.FindAnyObjectByType<RunManager>();
    }

    private void EnsureCanvasScale()
    {
        RectTransform root = transform as RectTransform;
        if (root != null && root.localScale.sqrMagnitude < 0.01f)
            root.localScale = Vector3.one;
    }

    private void EnsureCanvasScaler()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.4f;
    }

    private static void EnsureGameplayUiSystems()
    {
        if (Object.FindAnyObjectByType<PauseMenuUI>() == null)
            new GameObject("PauseMenuUI").AddComponent<PauseMenuUI>();

        AudioManager.EnsureExists();

        if (Object.FindAnyObjectByType<HeroRunStats>() == null)
            new GameObject("HeroRunStats").AddComponent<HeroRunStats>();

        if (Object.FindAnyObjectByType<AchievementManager>() == null)
            new GameObject("AchievementManager").AddComponent<AchievementManager>();

        if (Object.FindAnyObjectByType<BossSpawnManager>() == null)
            new GameObject("BossSpawnManager").AddComponent<BossSpawnManager>();

        // Passive item: phải tồn tại để 12 passive trong Resources/PassiveItems xuất hiện khi level-up.
        if (Object.FindAnyObjectByType<PassiveItemManager>() == null)
            new GameObject("PassiveItemManager").AddComponent<PassiveItemManager>();

        if (Object.FindAnyObjectByType<ObjectPooler>() == null)
            new GameObject("ObjectPooler").AddComponent<ObjectPooler>();

        // GameJuice (screen shake / hit-stop) — bám camera, cần sẵn từ đầu trận.
        GameJuice.Ensure();

        if (Object.FindAnyObjectByType<BossHPBarUI>() == null)
            new GameObject("BossHPBarUI").AddComponent<BossHPBarUI>();

        if (Camera.main != null && Camera.main.GetComponent<GameplayPresentation>() == null)
            Camera.main.gameObject.AddComponent<GameplayPresentation>();

        EnsureEventSystemForHover();

        if (Object.FindAnyObjectByType<VirtualJoystick>() == null)
        {
            GameObject joy = new GameObject("VirtualJoystick");
            VirtualJoystick vj = joy.AddComponent<VirtualJoystick>();
            vj.EnsureBuilt(showOnDesktop: true);
        }

        HUDManager hud = Object.FindAnyObjectByType<HUDManager>();
        if (hud != null && hud.GetComponent<HudPauseButton>() == null)
            hud.gameObject.AddComponent<HudPauseButton>();
    }

    private static void EnsureEventSystemForHover()
    {
        if (EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private void ResolveReferences()
    {
        Transform bottom = transform.Find("Bottom");
        if (levelText == null && bottom != null)
            levelText = bottom.Find("LevelText")?.GetComponent<TMP_Text>();
        if (expText == null && bottom != null)
            expText = bottom.Find("ExpText")?.GetComponent<TMP_Text>();
        if (expFillImage == null && bottom != null)
            expFillImage = bottom.Find("ExpBar/ExpFill")?.GetComponent<Image>();

        Transform topCenter = transform.Find("TopCenter");
        if (floorText == null && topCenter != null)
            floorText = topCenter.Find("FloorText")?.GetComponent<TMP_Text>();

        Transform topRight = transform.Find("TopRight");
        if (topRight != null)
        {
            if (scoreText == null || !scoreText.transform.IsChildOf(topRight))
                scoreText = topRight.Find("ScoreText")?.GetComponent<TMP_Text>();
            if (coinsText == null || !coinsText.transform.IsChildOf(topRight))
                coinsText = topRight.Find("CoinsText")?.GetComponent<TMP_Text>();
        }

        Transform topLeft = transform.Find("TopLeft");
        if (topLeft != null)
        {
            if (hpText == null)
                hpText = topLeft.Find("HPText")?.GetComponent<TMP_Text>();
            Transform hpBar = topLeft.Find("HPBar");
            if (hpFillImage == null && hpBar != null)
                hpFillImage = hpBar.Find("HPFill")?.GetComponent<Image>();
        }

        if (skillsPanel == null)
        {
            Transform panel = transform.Find("TopLeft/SkillsPanel");
            if (panel == null)
                panel = transform.Find("SkillsPanel");
            if (panel != null)
            {
                skillsPanel = panel.gameObject;
                if (skillsPanel.GetComponent<SkillsPanelUI>() == null)
                    skillsPanel.AddComponent<SkillsPanelUI>();
            }
        }

        Transform strayHp = transform.Find("HPBar");
        if (strayHp != null && strayHp.parent == transform)
            strayHp.gameObject.SetActive(false);
    }

    private void ApplyHudStyle()
    {
        StyleBottomBar();
        StyleTopTexts();
        StyleHpBar();
    }

    private void StyleBottomBar()
    {
        Transform bottom = transform.Find("Bottom");
        if (bottom == null)
            return;

        RectTransform bottomRt = bottom as RectTransform;
        if (bottomRt != null)
        {
            bottomRt.anchorMin = new Vector2(0f, 0f);
            bottomRt.anchorMax = new Vector2(1f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.anchoredPosition = new Vector2(0f, 14f);
            bottomRt.sizeDelta = new Vector2(0f, BottomPanelHeight);
        }

        Transform expBar = bottom.Find("ExpBar");
        if (expBar != null)
        {
            RectTransform barRt = expBar as RectTransform;
            if (barRt != null)
            {
                const float horizontalInset = 240f;
                barRt.anchorMin = new Vector2(0f, 0.5f);
                barRt.anchorMax = new Vector2(1f, 0.5f);
                barRt.pivot = new Vector2(0.5f, 0.5f);
                barRt.anchoredPosition = Vector2.zero;
                barRt.sizeDelta = new Vector2(-horizontalInset, ExpBarHeight);
            }

            Image barBg = expBar.GetComponent<Image>();
            if (barBg != null)
            {
                ConfigureBarBackground(barBg, ExpBarBg);
                barBg.raycastTarget = false;
            }

            Transform fill = expBar.Find("ExpFill");
            if (fill is RectTransform fillRt)
            {
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = new Vector2(2f, 2f);
                fillRt.offsetMax = new Vector2(-2f, -2f);
            }
        }

        if (expFillImage != null)
        {
            expFillImage.raycastTarget = false;
            expFillImage.transform.SetAsLastSibling();
            ApplyBarFillRatio(expFillImage, ExpFillColor, displayedExpFill);
        }

        if (levelText != null)
        {
            RectTransform lvRt = levelText.rectTransform;
            lvRt.anchorMin = new Vector2(0f, 0.5f);
            lvRt.anchorMax = new Vector2(0f, 0.5f);
            lvRt.pivot = new Vector2(0f, 0.5f);
            lvRt.anchoredPosition = new Vector2(12f, 0f);
            lvRt.sizeDelta = new Vector2(72f, 36f);
            StyleTmpLabel(levelText, 26f, FontStyles.Bold, LevelTextColor, TextAlignmentOptions.MidlineLeft);
            levelText.text = levelText.text.StartsWith("LV", System.StringComparison.OrdinalIgnoreCase)
                ? levelText.text
                : "LV.1";
        }

        if (expText != null)
        {
            RectTransform expRt = expText.rectTransform;
            expRt.anchorMin = new Vector2(1f, 0.5f);
            expRt.anchorMax = new Vector2(1f, 0.5f);
            expRt.pivot = new Vector2(1f, 0.5f);
            expRt.anchoredPosition = new Vector2(-8f, 0f);
            expRt.sizeDelta = new Vector2(200f, 36f);
            StyleTmpLabel(expText, 20f, FontStyles.Normal, ExpTextColor, TextAlignmentOptions.MidlineRight);
            expText.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void StyleTopTexts()
    {
        if (floorText != null)
        {
            RectTransform floorRt = floorText.rectTransform;
            floorRt.anchorMin = new Vector2(0.5f, 1f);
            floorRt.anchorMax = new Vector2(0.5f, 1f);
            floorRt.pivot = new Vector2(0.5f, 1f);
            floorRt.anchoredPosition = new Vector2(0f, -12f);
            floorRt.sizeDelta = new Vector2(420f, 44f);
            StyleTmpLabel(floorText, 30f, FontStyles.Bold, new Color(0.95f, 0.82f, 0.28f, 1f), TextAlignmentOptions.Center);
        }

        Transform topRight = transform.Find("TopRight");
        if (topRight is RectTransform rightRt)
        {
            rightRt.anchorMin = new Vector2(1f, 1f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 1f);
            rightRt.anchoredPosition = new Vector2(-14f, -14f);
            rightRt.sizeDelta = new Vector2(340f, 108f);
        }

        if (scoreText != null)
        {
            RectTransform rt = scoreText.rectTransform;
            if (topRight != null && !rt.IsChildOf(topRight))
                rt.SetParent(topRight, false);

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f, -6f);
            rt.sizeDelta = new Vector2(330f, 44f);
            StyleTmpLabel(scoreText, ScoreFontSize, FontStyles.Bold, new Color(0.9f, 0.92f, 1f, 1f), TextAlignmentOptions.MidlineRight);
        }

        if (coinsText != null)
        {
            RectTransform rt = coinsText.rectTransform;
            if (topRight != null && !rt.IsChildOf(topRight))
                rt.SetParent(topRight, false);

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-6f, -52f);
            rt.sizeDelta = new Vector2(330f, 44f);
            StyleTmpLabel(coinsText, CoinsFontSize, FontStyles.Bold, new Color(1f, 0.78f, 0.22f, 1f), TextAlignmentOptions.MidlineRight);
        }

        HudPauseButton pause = GetComponent<HudPauseButton>();
        if (pause != null)
            pause.RefreshLayout();

        Transform topCenter = transform.Find("TopCenter");
        if (topCenter is RectTransform centerRt)
        {
            centerRt.anchorMin = new Vector2(0.5f, 1f);
            centerRt.anchorMax = new Vector2(0.5f, 1f);
            centerRt.pivot = new Vector2(0.5f, 1f);
            centerRt.anchoredPosition = new Vector2(0f, -18f);
            centerRt.sizeDelta = new Vector2(560f, 64f);
        }
    }

    private void StyleHpBar()
    {
        Transform topLeft = transform.Find("TopLeft");
        if (topLeft is RectTransform leftRt)
        {
            leftRt.anchorMin = new Vector2(0f, 1f);
            leftRt.anchorMax = new Vector2(0f, 1f);
            leftRt.pivot = new Vector2(0f, 1f);
            leftRt.anchoredPosition = new Vector2(16f, -16f);
            leftRt.sizeDelta = new Vector2(320f, 168f);
        }

        EnsurePlayerStatsPanel(topLeft);

        Transform bottom = transform.Find("Bottom");
        Transform expBar = bottom != null ? bottom.Find("ExpBar") : null;
        Transform hpBar = topLeft != null ? topLeft.Find("HPBar") : null;
        if (hpBar is RectTransform hpRt)
        {
            hpRt.anchorMin = new Vector2(0f, 1f);
            hpRt.anchorMax = new Vector2(0f, 1f);
            hpRt.pivot = new Vector2(0f, 1f);
            hpRt.anchoredPosition = Vector2.zero;
            hpRt.sizeDelta = new Vector2(320f, HpBarHeight);

            Image hpBg = hpBar.GetComponent<Image>();
            if (hpBg != null)
                ConfigureBarBackground(hpBg, HpBarBg);
        }

        ApplyGuiHudArt(hpBar, expBar);

        if (hpFillImage != null)
        {
            hpFillImage.gameObject.SetActive(true);
            hpFillImage.transform.SetAsLastSibling();
            float hpRatio = playerHealth != null
                ? Mathf.Clamp01(playerHealth.CurrentHP / Mathf.Max(1f, playerHealth.MaxHP))
                : 1f;
            ApplyBarFillRatio(hpFillImage, HpFillColor, hpRatio);
        }

        if (hpText != null)
        {
            RectTransform textRt = hpText.rectTransform;
            if (hpBar != null && hpText.transform.parent != hpBar)
            {
                textRt.SetParent(hpBar, false);
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
            }
            StyleTmpLabel(hpText, 28f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            hpText.outlineWidth = 0.2f;
            hpText.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        }

        StylePlayerStatsText(topLeft);

        Transform skills = topLeft != null ? topLeft.Find("SkillsPanel") : null;
        if (skills is RectTransform skillsRt)
        {
            skillsRt.anchorMin = new Vector2(0f, 0f);
            skillsRt.anchorMax = new Vector2(1f, 0f);
            skillsRt.pivot = new Vector2(0f, 1f);
            skillsRt.anchoredPosition = Vector2.zero;
            skillsRt.sizeDelta = new Vector2(0f, 44f);
            skillsRt.anchoredPosition = new Vector2(0f, -34f);

            HorizontalLayoutGroup layout = skills.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.spacing = 6f;
            }
        }
    }

    private void ApplyGuiHudArt(Transform hpBar, Transform expBar)
    {
        if (!GuiArtLibrary.HasPack)
            return;

        if (hpBar != null)
        {
            Image hpBg = hpBar.GetComponent<Image>();
            if (GuiArtLibrary.ApplyBarFrame(hpBg, GuiArtLibrary.HpBarFrame) && hpFillImage != null)
            {
                RectTransform fillRt = hpFillImage.rectTransform;
                fillRt.offsetMin = new Vector2(10f, 6f);
                fillRt.offsetMax = new Vector2(-10f, -6f);
            }
        }

        if (expBar != null)
        {
            Image expBg = expBar.GetComponent<Image>();
            if (GuiArtLibrary.ApplyBarFrame(expBg, GuiArtLibrary.ExpBarFrame) && expFillImage != null)
            {
                RectTransform fillRt = expFillImage.rectTransform;
                fillRt.offsetMin = new Vector2(8f, 5f);
                fillRt.offsetMax = new Vector2(-8f, -5f);
            }
        }
    }

    private void EnsurePlayerStatsPanel(Transform topLeft)
    {
        if (topLeft == null)
            return;

        Transform existing = topLeft.Find("PlayerStats");
        GameObject statsGo;
        if (existing != null)
            statsGo = existing.gameObject;
        else
        {
            statsGo = new GameObject("PlayerStats", typeof(RectTransform));
            statsGo.transform.SetParent(topLeft, false);
        }

        if (statsGo.GetComponent<PlayerStatsUI>() == null)
            statsGo.AddComponent<PlayerStatsUI>();
    }

    private static void StylePlayerStatsText(Transform topLeft)
    {
        if (topLeft == null)
            return;

        PlayerStatsUI stats = topLeft.GetComponentInChildren<PlayerStatsUI>(true);
        stats?.ApplyHudStyle();
    }

    private static void StyleTmpLabel(TMP_Text text, float size, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameUIFont.ApplyHud(text, size);
        text.fontSize = size;
        text.fontSizeMin = size;
        text.fontSizeMax = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = align;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.outlineWidth = 0f;
        text.raycastTarget = false;
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!expSubscribed)
            SubscribeExpEvents();

        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<HealthSystem>();
        }

        UpdateHp();
        AnimateExpAndStats();
        RefreshScoreAndCoinLabels();
        ForceRefreshFromSystems(false);

        if (SkillSelectionUI.Instance != null && SkillSelectionUI.Instance.IsPanelOpen)
            return;

        RefreshPlayerHealthReference();
        if (playerHealth != null && playerHealth.CurrentHP <= 0f && !runEnded)
            ShowGameOver();
    }

    public void RefreshPlayerHealthReference()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        playerHealth = player.GetComponent<HealthSystem>();
        UpdateHp();
    }

    private void AnimateExpAndStats()
    {
        ExpSystem exp = FindExpSystem();
        if (exp != null)
        {
            float max = Mathf.Max(1f, exp.ExpToNextLevel);
            float targetFill = Mathf.Clamp01(exp.CurrentExp / max);
            float smooth = Mathf.Max(0.35f, expBarFillDuration);

            displayedExpFill = Mathf.SmoothDamp(displayedExpFill, targetFill, ref expFillVelocity, smooth, 1f, Time.unscaledDeltaTime);
            displayedExpValue = Mathf.SmoothDamp(displayedExpValue, exp.CurrentExp, ref expValueVelocity, smooth * 0.65f, max * 2f, Time.unscaledDeltaTime);

            ApplyExpFillVisual(displayedExpFill);
            if (levelText != null)
                levelText.text = "LV." + exp.CurrentLevel;
            if (expText != null)
                expText.text = Mathf.Max(0, Mathf.FloorToInt(displayedExpValue)) + " / " + Mathf.FloorToInt(max);
        }

        RunManager run = FindRunManager();
        if (run != null)
        {
            score = run.RunScore;
            coins = run.RunCoins;
        }

        float statSmooth = Mathf.Max(0.08f, statAnimSmoothTime);
        float sdt = Time.unscaledDeltaTime;
        displayScore = Mathf.SmoothDamp(displayScore, score, ref scoreAnimVelocity, statSmooth, Mathf.Infinity, sdt);
        displayCoins = Mathf.SmoothDamp(displayCoins, coins, ref coinsAnimVelocity, statSmooth, Mathf.Infinity, sdt);
    }

    private void RefreshScoreAndCoinLabels()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + Mathf.RoundToInt(displayScore);
        if (coinsText != null)
            coinsText.text = "Xu: " + Mathf.RoundToInt(displayCoins);
    }

    public void UpdateHp()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<HealthSystem>();
        }

        if (playerHealth == null)
            return;

        if (hpFillImage == null)
            ResolveReferences();

        float maxHp = Mathf.Max(1f, playerHealth.MaxHP);
        float ratio = Mathf.Clamp01(playerHealth.CurrentHP / maxHp);

        if (hpFillImage != null)
            ApplyBarFillRatio(hpFillImage, HpFillColor, ratio);

        if (hpText != null)
            hpText.text = Mathf.CeilToInt(playerHealth.CurrentHP) + " / " + Mathf.CeilToInt(maxHp);
    }

    public void UpdateExp(float current, float max, int level)
    {
        SetExpTarget(current, max, level, false);
    }

    private void SetExpTarget(float current, float max, int level, bool snap)
    {
        if (snap)
            expAnimator.SnapTo(current, max, level);
        else
            expAnimator.SetTarget(current, max, level);

        ForceRefreshFromSystems(snap);
    }

    private void ApplyExpFillVisual(float fill01)
    {
        if (expFillImage == null)
            return;

        ApplyBarFillRatio(expFillImage, ExpFillColor, fill01);
    }

    private static void ConfigureBarBackground(Image image, Color bgColor)
    {
        if (image == null)
            return;

        Sprite solid = GetUiWhiteSprite();
        if (image.sprite != solid)
            image.sprite = solid;
        image.overrideSprite = null;
        image.type = Image.Type.Simple;
        image.fillCenter = true;
        image.color = bgColor;
        image.enabled = true;
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private static void ApplyBarFillRatio(Image image, Color fillColor, float ratio01)
    {
        if (image == null)
            return;

        ratio01 = Mathf.Clamp01(ratio01);

        // Always force a clean solid sprite so any stale "Filled" type or themed
        // dungeon-pack sprite from the scene can never bleed through and break the bar.
        Sprite solid = GetUiWhiteSprite();
        if (image.sprite != solid)
            image.sprite = solid;
        image.overrideSprite = null;
        image.type = Image.Type.Simple;
        image.fillCenter = true;
        image.color = fillColor;
        image.enabled = ratio01 > 0.001f;
        image.preserveAspect = false;
        image.raycastTarget = false;

        RectTransform rt = image.rectTransform;
        Vector2 pad = image.name.Contains("Exp", System.StringComparison.OrdinalIgnoreCase)
            ? ExpFillPadding
            : HpFillPadding;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(ratio01, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = pad;
        rt.offsetMax = new Vector2(-pad.x * 0.5f, -pad.y);
    }

    private static Sprite GetUiWhiteSprite()
    {
        if (uiWhiteSprite == null)
            uiWhiteSprite = CreateUiWhiteSprite();
        return uiWhiteSprite;
    }

    private static Sprite CreateUiWhiteSprite()
    {
        const int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color white = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, white);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public void UpdateFloor(int floor)
    {
        currentFloor = floor;
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        score += amount;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        coins += amount;
    }

    private void RefreshTexts()
    {
        RefreshScoreAndCoinLabels();
        if (floorText != null)
            floorText.text = "TẦNG " + currentFloor + " / 10";
    }

    private void DisableStrayStatLabels()
    {
        TMP_Text[] labels = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null)
                continue;

            if (label.name == "ExpText" || label.name == "LevelText")
            {
                if (!label.transform.IsChildOf(transform))
                    label.gameObject.SetActive(false);
                continue;
            }

            if (label.name != "ScoreText" && label.name != "CoinsText")
                continue;

            if (label.transform.IsChildOf(transform))
                continue;

            label.gameObject.SetActive(false);
        }
    }

    public void ResetForNewRun()
    {
        runEnded = false;
        playerHealth = null;
        Time.timeScale = 1f;
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
        GameOverUI.Instance?.HidePanel();
    }

    public void ShowGameOver()
    {
        runEnded = true;
        AudioManager.PlayGameOver();

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            gameOverUI?.Setup(score, currentFloor, coins, false);
        }
        else if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.Show(score, currentFloor, coins, false);
        }
        else
        {
            new GameObject("GameOverUI").AddComponent<GameOverUI>().Show(score, currentFloor, coins, false);
        }

        Time.timeScale = 0f;
    }

    public static void SpawnDamageNumber(Vector3 worldPosition, float amount, bool isCrit = false)
    {
        if (amount <= 0f)
            return;

        HUDManager hud = Instance;
        if (hud != null && hud.damageNumberPrefab != null)
        {
            hud.SpawnFromPrefab(worldPosition, amount, isCrit);
            return;
        }

        SpawnRuntimeDamageText(worldPosition, amount, isCrit);
    }

    private void SpawnFromPrefab(Vector3 worldPosition, float amount, bool isCrit)
    {
        if (damageNumberPrefab == null)
        {
            SpawnRuntimeDamageText(worldPosition, amount, isCrit);
            return;
        }

        Vector3 spawnPos = worldPosition + Vector3.up * 0.5f;
        GameObject go = Instantiate(damageNumberPrefab, spawnPos, Quaternion.identity);

        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null)
            tmp = go.GetComponentInChildren<TextMeshPro>();

        if (tmp != null)
        {
            tmp.text = isCrit ? Mathf.RoundToInt(amount) + "!" : Mathf.RoundToInt(amount).ToString();
            // Chí mạng = ĐỎ và to hơn; thường = trắng.
            tmp.color = isCrit ? new Color(1f, 0.2f, 0.18f, 1f) : Color.white;
            tmp.fontStyle = isCrit ? FontStyles.Bold : FontStyles.Normal;
            if (isCrit)
                tmp.fontSize *= 1.6f;
        }

        DamageNumberFloat floater = go.GetComponent<DamageNumberFloat>();
        if (floater == null)
            floater = go.AddComponent<DamageNumberFloat>();
        floater.Initialize(0.8f);
    }

    private static void SpawnRuntimeDamageText(Vector3 worldPosition, float amount, bool isCrit)
    {
        GameObject go = new GameObject("DmgNum");
        go.transform.position = worldPosition + Vector3.up * 0.5f;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = isCrit ? Mathf.RoundToInt(amount) + "!" : Mathf.RoundToInt(amount).ToString();
        // Chí mạng = ĐỎ và to hơn; thường = trắng.
        tmp.fontSize = isCrit ? 7.5f : 4f;
        tmp.fontStyle = isCrit ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = isCrit ? new Color(1f, 0.2f, 0.18f, 1f) : Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 30;

        DamageNumberFloat floater = go.AddComponent<DamageNumberFloat>();
        floater.Initialize(0.8f);
    }

    /// <summary>Hiển thị số EXP nhận được (màu xanh lá) tại vị trí quái chết.</summary>
    public static void SpawnExpNumber(Vector3 worldPosition, float expAmount)
    {
        if (expAmount <= 0f)
            return;

        GameObject go = new GameObject("ExpNum");
        go.transform.position = worldPosition + Vector3.up * 0.8f;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "+" + Mathf.RoundToInt(expAmount) + " EXP";
        tmp.fontSize = 3.4f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.4f, 1f, 0.45f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 29;

        DamageNumberFloat floater = go.AddComponent<DamageNumberFloat>();
        floater.Initialize(1.0f);
    }

    public void RegisterDamageDealt(float amount) {}
    public void ShowWaveAnnouncement(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || floorText == null)
            return;

        floorText.text = message;
        CancelInvoke(nameof(RestoreFloorLabel));
        Invoke(nameof(RestoreFloorLabel), 2.5f);
    }

    private void RestoreFloorLabel()
    {
        if (floorText != null && FloorManager.Instance != null)
            floorText.text = "TẦNG " + FloorManager.Instance.CurrentFloor + " / 10";
    }
    public void SetWeaponBarVisible(bool visible)
    {
        if (!visible && weaponBarRoot != null)
            weaponBarRoot.SetActive(false);
        else if (visible && weaponBarRoot != null)
            weaponBarRoot.SetActive(true);
    }

    public void UpdateWeaponSlots(IReadOnlyList<WeaponManager.WeaponSlot> activeSlots, int maxSlots)
    {
        if (!Application.isPlaying)
            return;

        if (!WeaponStyleUtil.UsesWeaponPickupRewards())
        {
            SetWeaponBarVisible(false);
            return;
        }

        maxSlots = Mathf.Clamp(maxSlots, 1, 6);
        EnsureWeaponBar(maxSlots);
        SetWeaponBarVisible(true);

        for (int i = 0; i < weaponSlotImages.Count; i++)
        {
            Image slot = weaponSlotImages[i];
            if (slot == null)
                continue;

            HudHoverTooltip tooltip = i < weaponSlotTooltips.Count ? weaponSlotTooltips[i] : null;

            if (activeSlots != null && i < activeSlots.Count)
            {
                WeaponManager.WeaponSlot weaponSlot = activeSlots[i];
                Sprite icon = GameIconLibrary.WeaponSprite(weaponSlot.weaponType);
                if (icon != null)
                {
                    slot.sprite = icon;
                    slot.color = GameIconLibrary.WeaponTint(weaponSlot.weaponType);
                    slot.preserveAspect = true;
                }
                else
                    slot.color = GetWeaponSlotColor(weaponSlot.weaponType);
                slot.enabled = true;
                tooltip?.ConfigureWeapon(weaponSlot.weaponType, weaponSlot.copies, weaponSlot.evolved);
            }
            else
            {
                slot.sprite = GetUiWhiteSprite();
                slot.color = new Color(0.15f, 0.16f, 0.22f, 0.75f);
                slot.enabled = true;
                tooltip?.ConfigureEmpty();
            }
        }
    }

    private void EnsureWeaponBar(int maxSlots)
    {
        if (weaponBarRoot != null && weaponSlotImages.Count == maxSlots)
            return;

        if (weaponBarRoot != null)
            Destroy(weaponBarRoot);

        weaponSlotImages.Clear();
        weaponSlotTooltips.Clear();
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;

        SkillTooltipUI.GetOrCreate(canvas);

        weaponBarRoot = new GameObject("WeaponBar");
        weaponBarRoot.transform.SetParent(canvas.transform, false);
        RectTransform barRt = weaponBarRoot.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0f);
        barRt.anchorMax = new Vector2(0f, 0f);
        barRt.pivot = new Vector2(0f, 0f);
        barRt.anchoredPosition = new Vector2(12f, 78f);
        barRt.sizeDelta = new Vector2(maxSlots * 44f + 8f, 40f);

        HorizontalLayoutGroup layout = weaponBarRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotGo = new GameObject("WeaponSlot" + i);
            slotGo.transform.SetParent(weaponBarRoot.transform, false);
            RectTransform rt = slotGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(36f, 36f);
            Image img = slotGo.AddComponent<Image>();
            img.sprite = GetUiWhiteSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.15f, 0.16f, 0.22f, 0.75f);
            img.raycastTarget = true;
            weaponSlotImages.Add(img);

            HudHoverTooltip hover = slotGo.AddComponent<HudHoverTooltip>();
            hover.ConfigureEmpty();
            weaponSlotTooltips.Add(hover);
        }
    }

    private static Color GetWeaponSlotColor(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:
                return new Color(1f, 0.45f, 0.2f);
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:
                return new Color(0.45f, 0.85f, 1f);
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return new Color(0.55f, 0.95f, 0.35f);
            case WeaponType.HolyCross:
            case WeaponType.HolyNova:
                return new Color(1f, 0.95f, 0.55f);
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
                return new Color(0.75f, 0.55f, 1f);
            case WeaponType.StormBow:
                return new Color(0.5f, 0.9f, 1f);
            default:
                return new Color(0.75f, 0.7f, 0.65f);
        }
    }
    public void UpdatePassiveSlots(IReadOnlyList<PassivePick> passives, int maxSlots)
    {
        if (!Application.isPlaying)
            return;

        maxSlots = Mathf.Clamp(maxSlots, 1, 6);
        EnsurePassiveBar(maxSlots);

        for (int i = 0; i < passiveSlotImages.Count; i++)
        {
            Image slot = passiveSlotImages[i];
            TMP_Text levelLabel = i < passiveLevelLabels.Count ? passiveLevelLabels[i] : null;
            HudHoverTooltip tooltip = i < passiveSlotTooltips.Count ? passiveSlotTooltips[i] : null;

            if (passives != null && i < passives.Count && passives[i]?.data != null)
            {
                PassivePick pick = passives[i];
                Sprite icon = GameIconLibrary.PassiveSprite(pick.data);
                if (icon != null)
                {
                    slot.sprite = icon;
                    slot.color = GameIconLibrary.PassiveTint(pick.data);
                    slot.preserveAspect = true;
                }
                else
                {
                    slot.sprite = GetUiWhiteSprite();
                    slot.color = GameIconLibrary.PassiveTint(pick.data);
                }

                slot.enabled = true;
                if (levelLabel != null)
                {
                    levelLabel.text = pick.level.ToString();
                    levelLabel.enabled = true;
                }

                tooltip?.ConfigurePassive(pick.data, pick.level);
            }
            else
            {
                slot.sprite = GetUiWhiteSprite();
                slot.color = new Color(0.12f, 0.13f, 0.18f, 0.55f);
                slot.enabled = true;
                if (levelLabel != null)
                {
                    levelLabel.text = string.Empty;
                    levelLabel.enabled = false;
                }

                tooltip?.ConfigureEmpty();
            }
        }
    }

    private void EnsurePassiveBar(int maxSlots)
    {
        if (passiveBarRoot != null && passiveSlotImages.Count == maxSlots)
            return;

        if (passiveBarRoot != null)
            Destroy(passiveBarRoot);

        passiveSlotImages.Clear();
        passiveLevelLabels.Clear();
        passiveSlotTooltips.Clear();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;

        SkillTooltipUI.GetOrCreate(canvas);

        passiveBarRoot = new GameObject("PassiveBar");
        passiveBarRoot.transform.SetParent(canvas.transform, false);
        RectTransform barRt = passiveBarRoot.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0f, 0f);
        barRt.anchorMax = new Vector2(0f, 0f);
        barRt.pivot = new Vector2(0f, 0f);
        barRt.anchoredPosition = new Vector2(12f, 38f);
        barRt.sizeDelta = new Vector2(maxSlots * 36f + 8f, 34f);

        HorizontalLayoutGroup layout = passiveBarRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotGo = new GameObject("PassiveSlot" + i);
            slotGo.transform.SetParent(passiveBarRoot.transform, false);
            RectTransform rt = slotGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(32f, 32f);
            Image img = slotGo.AddComponent<Image>();
            img.sprite = GetUiWhiteSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.13f, 0.18f, 0.55f);
            img.raycastTarget = true;
            passiveSlotImages.Add(img);

            GameObject lvlGo = new GameObject("Level", typeof(RectTransform), typeof(TextMeshProUGUI));
            lvlGo.transform.SetParent(slotGo.transform, false);
            RectTransform lrt = lvlGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(1f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(1f, 0f);
            lrt.anchoredPosition = new Vector2(-2f, 2f);
            lrt.sizeDelta = new Vector2(18f, 14f);
            TMP_Text lvl = lvlGo.GetComponent<TextMeshProUGUI>();
            GameUIFont.Apply(lvl, GameUIFont.Role.CardStack);
            lvl.fontSize = 11f;
            lvl.color = Color.white;
            lvl.alignment = TextAlignmentOptions.BottomRight;
            lvl.raycastTarget = false;
            passiveLevelLabels.Add(lvl);

            HudHoverTooltip hover = slotGo.AddComponent<HudHoverTooltip>();
            hover.ConfigureEmpty();
            passiveSlotTooltips.Add(hover);
        }
    }
    public void RegisterEnemyKilled(int s, int c)
    {
        RunManager run = FindRunManager();
        if (run != null)
        {
            run.AddRunScore(s);
            run.AddRunCoins(c);
            score = run.RunScore;
            coins = run.RunCoins;
        }
        else
        {
            AddScore(s);
            AddCoins(c);
        }

        displayScore = score;
        displayCoins = coins;
        ForceRefreshFromSystems(false);
        AchievementManager.Instance?.OnEnemyKilled();
        if (c > 0)
            AudioManager.PlayCoinCollect();
    }
    public void ShowRunResult(bool victory, int finalScore, int coinsEarned)
    {
        score = finalScore > 0 ? finalScore : score;
        coins = coinsEarned > 0 ? coinsEarned : coins;

        if (victory)
            currentFloor = Mathf.Max(currentFloor, 10);
        else
            SyncFloorFromWave();

        if (victory)
            ShowVictoryScreen();
        else
            ShowGameOver();
    }

    public void ShowVictoryScreen()
    {
        runEnded = true;
        Time.timeScale = 0f;

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            gameOverUI?.Setup(score, currentFloor, coins, true);
        }
        else if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show(score, currentFloor, coins, true);
        else
            new GameObject("GameOverUI").AddComponent<GameOverUI>().Show(score, currentFloor, coins, true);

        AchievementManager.Instance?.OnRunEnded(true, currentFloor);
    }
    public void AddScore(int amount, bool animateDelta) => AddScore(amount);
}
