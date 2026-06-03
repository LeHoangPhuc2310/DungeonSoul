using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    private class DamageNumberEntry
    {
        public RectTransform rect;
        public TMP_Text text;
        public CanvasGroup canvasGroup;
        public float elapsed;
        public float duration;
        public Vector2 velocity;
        public bool active;
    }

    public static HUDManager Instance { get; private set; }

    [Header("HP")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpText;

    [Header("Skills")]
    [SerializeField] private TMP_Text activeSkillsText;

    [Header("Score / Coins / Floor")]
    public TMP_Text scoreText;
    public TMP_Text coinsText;
    public TMP_Text floorText;
    public TMP_Text scoreDeltaText;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverCanvas;

    private int currentFloor = 1;

    [Header("Damage Numbers")]
    [SerializeField] private RectTransform floatingDamageRoot;
    [SerializeField] private int prewarmPoolCount = 15;

    private Canvas rootCanvas;
    private HealthSystem playerHealth;
    private PlayerSkillHandler playerSkillHandler;
    private Camera worldCamera;

    private readonly List<DamageNumberEntry> activeDamageNumbers = new List<DamageNumberEntry>();
    private readonly Stack<DamageNumberEntry> pooledDamageNumbers = new Stack<DamageNumberEntry>();

    private int score;
    private int coins;
    private float scoreDeltaTimer;
    private string lastSkillsSignature = string.Empty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUI();
        PrewarmDamagePool();
        RefreshPlayerReferences();
        RefreshScoreAndCoinText();
    }

    private void Update()
    {
        RefreshPlayerReferences();
        UpdateHPDisplay();
        UpdateActiveSkillsDisplay();
        UpdateDamageNumbers();
        UpdateScoreDeltaText();

        if (playerHealth != null && playerHealth.CurrentHP <= 0f)
        {
            ShowGameOver();
        }
    }

    private void ShowGameOver()
    {
        if (gameOverCanvas != null && !gameOverCanvas.activeSelf)
        {
            gameOverCanvas.SetActive(true);
            
            // Update GameOver stats
            Transform scoreT = gameOverCanvas.transform.Find("ScoreText");
            if (scoreT != null) scoreT.GetComponent<TMP_Text>().text = $"Score: {score}";
            
            Transform coinsT = gameOverCanvas.transform.Find("CoinsText");
            if (coinsT != null) coinsT.GetComponent<TMP_Text>().text = $"Coins: {coins}";
            
            Transform floorT = gameOverCanvas.transform.Find("FloorText");
            if (floorT != null) floorT.GetComponent<TMP_Text>().text = $"Floor: {currentFloor}";
        }
    }

    public static void SpawnDamageNumber(Vector3 worldPosition, float amount, bool isCrit)
    {
        if (Instance != null)
            Instance.ShowDamageNumber(worldPosition, amount, isCrit);
    }

    public void ShowDamageNumber(Vector3 worldPosition, float amount, bool isCrit)
    {
        if (floatingDamageRoot == null)
            return;

        DamageNumberEntry entry = GetDamageNumberEntry();
        if (entry == null)
            return;

        entry.active = true;
        entry.elapsed = 0f;
        entry.duration = 0.8f;
        entry.velocity = new Vector2(0f, 80f);
        entry.text.text = isCrit ? $"CRIT! {Mathf.RoundToInt(amount)}" : Mathf.RoundToInt(amount).ToString();
        entry.text.color = isCrit ? new Color(1f, 0.9f, 0.2f, 1f) : Color.white;
        entry.text.fontSize = isCrit ? 34f : 30f;
        entry.canvasGroup.alpha = 1f;
        entry.rect.localScale = Vector3.one;
        entry.rect.gameObject.SetActive(true);

        Vector2 localPoint;
        Camera cam = rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : GetWorldCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetWorldCamera(), worldPosition + Vector3.up * 0.7f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(floatingDamageRoot, screenPoint, cam, out localPoint);
        entry.rect.anchoredPosition = localPoint;

        activeDamageNumbers.Add(entry);
    }

    public void AddScore(int amount, bool animateDelta = true)
    {
        if (amount <= 0)
            return;

        score += amount;
        RefreshScoreAndCoinText();

        if (animateDelta && scoreDeltaText != null)
        {
            scoreDeltaText.text = $"+{amount}";
            scoreDeltaText.alpha = 1f;
            scoreDeltaTimer = 0.8f;
        }
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        coins += amount;
        RefreshScoreAndCoinText();
    }

    public void RegisterEnemyKilled(int scoreReward, int coinReward)
    {
        AddScore(scoreReward, true);
        AddCoins(coinReward);
    }

    public void EnsureUI()
{
        rootCanvas = GetComponentInChildren<Canvas>(true);
        if (rootCanvas == null)
        {
            GameObject canvasGO = new GameObject("HUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            rootCanvas = canvasGO.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (floatingDamageRoot == null)
            floatingDamageRoot = CreateRoot("FloatingDamageRoot", rootCanvas.transform as RectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        EnsureTopLeftGroup();
        EnsureTopRightGroup();
        EnsureTopCenterGroup();
    }

    private void EnsureTopLeftGroup()
    {
        RectTransform topLeftRoot = CreateRoot(
            "TopLeftHUD",
            rootCanvas.transform as RectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(20f, -20f),
            new Vector2(320f, 180f));

        RectTransform hpBarBgRect = CreateRoot(
            "HPBarBackground",
            topLeftRoot,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(110f, -15f),
            new Vector2(220f, 22f));

        Image hpBg = hpBarBgRect.GetComponent<Image>();
        if (hpBg == null)
            hpBg = hpBarBgRect.gameObject.AddComponent<Image>();
        hpBg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        if (hpFillImage == null)
        {
            RectTransform fillRect = CreateRoot(
                "HPBarFill",
                hpBarBgRect,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            hpFillImage = fillRect.GetComponent<Image>();
            if (hpFillImage == null)
                hpFillImage = fillRect.gameObject.AddComponent<Image>();

            hpFillImage.type = Image.Type.Filled;
            hpFillImage.fillMethod = Image.FillMethod.Horizontal;
            hpFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        if (hpText == null)
            hpText = CreateTmpText("HPText", topLeftRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -36f), new Vector2(260f, 28f), 24f, FontStyles.Bold);

        if (activeSkillsText == null)
            activeSkillsText = CreateTmpText("ActiveSkillsText", topLeftRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -72f), new Vector2(560f, 56f), 20f, FontStyles.Normal);
    }

    private void EnsureTopRightGroup()
    {
        RectTransform topRightRoot = CreateRoot(
            "TopRightHUD",
            rootCanvas.transform as RectTransform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-20f, -20f),
            new Vector2(360f, 140f));

        if (scoreText == null)
            scoreText = CreateTmpText("ScoreText", topRightRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(330f, 30f), 24f, FontStyles.Bold, TextAlignmentOptions.Right);

        if (coinsText == null)
            coinsText = CreateTmpText("CoinsText", topRightRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(330f, 30f), 24f, FontStyles.Bold, TextAlignmentOptions.Right);

        if (scoreDeltaText == null)
        {
            scoreDeltaText = CreateTmpText("ScoreDeltaText", topRightRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -112f), new Vector2(160f, 26f), 20f, FontStyles.Bold, TextAlignmentOptions.Right);
            scoreDeltaText.color = new Color(1f, 0.85f, 0.25f, 1f);
            scoreDeltaText.alpha = 0f;
        }
}

    private void PrewarmDamagePool()
    {
        for (int i = 0; i < Mathf.Max(0, prewarmPoolCount); i++)
            pooledDamageNumbers.Push(CreateDamageNumberEntry());
    }

    private DamageNumberEntry CreateDamageNumberEntry()
    {
        RectTransform rect = CreateRoot(
            "DamageNumber",
            floatingDamageRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(240f, 60f));

        TMP_Text txt = rect.GetComponent<TMP_Text>();
        if (txt == null)
            txt = rect.gameObject.AddComponent<TextMeshProUGUI>();
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 30f;
        txt.fontStyle = FontStyles.Bold;
        txt.raycastTarget = false;

        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        if (group == null)
            group = rect.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        rect.gameObject.SetActive(false);

        return new DamageNumberEntry
        {
            rect = rect,
            text = txt,
            canvasGroup = group,
            active = false
        };
    }

    private DamageNumberEntry GetDamageNumberEntry()
    {
        if (pooledDamageNumbers.Count > 0)
            return pooledDamageNumbers.Pop();

        return CreateDamageNumberEntry();
    }

    private void ReturnDamageNumberEntry(DamageNumberEntry entry)
    {
        entry.active = false;
        entry.rect.gameObject.SetActive(false);
        pooledDamageNumbers.Push(entry);
    }

    private void UpdateDamageNumbers()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = activeDamageNumbers.Count - 1; i >= 0; i--)
        {
            DamageNumberEntry entry = activeDamageNumbers[i];
            if (entry == null || !entry.active)
            {
                activeDamageNumbers.RemoveAt(i);
                continue;
            }

            entry.elapsed += dt;
            float t = Mathf.Clamp01(entry.elapsed / entry.duration);
            entry.rect.anchoredPosition += entry.velocity * dt;
            entry.canvasGroup.alpha = 1f - t;

            if (entry.elapsed >= entry.duration)
            {
                activeDamageNumbers.RemoveAt(i);
                ReturnDamageNumberEntry(entry);
            }
        }
    }

    private void UpdateHPDisplay()
    {
        if (playerHealth == null || hpFillImage == null || hpText == null)
            return;

        float maxHp = Mathf.Max(1f, playerHealth.MaxHP);
        float hpPercent = Mathf.Clamp01(playerHealth.CurrentHP / maxHp);

        hpFillImage.fillAmount = hpPercent;
        hpFillImage.color = GetHPColor(hpPercent);
        hpText.text = $"HP: {Mathf.CeilToInt(playerHealth.CurrentHP)}/{Mathf.CeilToInt(maxHp)}";
    }

    private void UpdateActiveSkillsDisplay()
    {
        if (activeSkillsText == null || playerSkillHandler == null)
            return;

        List<SkillData> skills = playerSkillHandler.activeSkills;
        if (skills == null || skills.Count == 0)
        {
            if (activeSkillsText.text != "Skills: None")
                activeSkillsText.text = "Skills: None";
            lastSkillsSignature = string.Empty;
            return;
        }

        int showCount = Mathf.Min(4, skills.Count);
        string signature = string.Empty;
        for (int i = 0; i < showCount; i++)
        {
            SkillData skill = skills[i];
            string name = skill != null ? skill.skillName : "Unknown";
            signature += name;
            if (i < showCount - 1)
                signature += "|";
        }

        if (signature == lastSkillsSignature)
            return;

        lastSkillsSignature = signature;
        activeSkillsText.text = $"Skills: {signature.Replace("|", " | ")}";
    }

    private void UpdateScoreDeltaText()
    {
        if (scoreDeltaText == null)
            return;

        if (scoreDeltaTimer <= 0f)
        {
            scoreDeltaText.alpha = 0f;
            return;
        }

        scoreDeltaTimer -= Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(scoreDeltaTimer / 0.8f);
        scoreDeltaText.alpha = t;
    }

    public void UpdateFloor(int floor)
    {
        currentFloor = floor;
        RefreshScoreAndCoinText();
    }

    private void RefreshScoreAndCoinText()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
        if (coinsText != null)
            coinsText.text = $"Coins: {coins}";
        if (floorText != null)
            floorText.text = $"Floor: {currentFloor}";
    }

    private void RefreshPlayerReferences()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<HealthSystem>();
                playerSkillHandler = player.GetComponent<PlayerSkillHandler>();
            }
        }

        if (playerSkillHandler == null)
            playerSkillHandler = PlayerSkillHandler.Instance;
    }

    private Camera GetWorldCamera()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        return worldCamera;
    }

    private static Color GetHPColor(float percent)
    {
        if (percent > 0.5f)
        {
            float t = (percent - 0.5f) / 0.5f;
            return Color.Lerp(new Color(1f, 0.9f, 0.2f, 1f), new Color(0.2f, 0.95f, 0.2f, 1f), t);
        }

        float lowT = percent / 0.5f;
        return Color.Lerp(new Color(0.95f, 0.2f, 0.2f, 1f), new Color(1f, 0.9f, 0.2f, 1f), lowT);
    }

    private static RectTransform CreateRoot(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        RectTransform rect;
        if (existing != null)
        {
            rect = existing as RectTransform;
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            rect = go.GetComponent<RectTransform>();
            if (parent != null)
                rect.SetParent(parent, false);
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return rect;
    }

    private static TMP_Text CreateTmpText(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPos,
        Vector2 size,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        RectTransform rect = CreateRoot(name, parent, anchorMin, anchorMax, anchoredPos, size);
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text == null)
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private void EnsureTopCenterGroup()
    {
        RectTransform topCenterRoot = CreateRoot(
            "TopCenterHUD",
            rootCanvas.transform as RectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -20f),
            new Vector2(400f, 100f));

        if (floorText == null)
            floorText = CreateTmpText("FloorText", topCenterRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(300f, 40f), 28f, FontStyles.Bold, TextAlignmentOptions.Center);
    }
}
