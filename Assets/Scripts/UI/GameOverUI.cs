using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("References (auto-built if null)")]
    [SerializeField] private TMP_Text scoreResultText;
    [SerializeField] private TMP_Text floorResultText;
    [SerializeField] private TMP_Text coinsResultText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private TMP_Text titleText;

    private Canvas canvas;
    private Transform statsColumn;     // cột Player Stats bên trái
    private TMP_Text heroLevelText;    // "Anh hùng / LEVEL N" giữa
    private TMP_Text bossInfoText;     // tên boss + cấp bên phải
    private Image heroIconImage;       // icon nhân vật
    private Image bossIconImage;       // icon boss/vũ khí
    private readonly System.Collections.Generic.List<TMP_Text> statValueLabels =
        new System.Collections.Generic.List<TMP_Text>();

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
        EnsureUI();
        WireButtons();
    }

    public void Show(int score, int floor, int coins, bool victory = false)
    {
        EnsureUI();
        WireButtons();
        if (canvas != null)
            canvas.gameObject.SetActive(true);
        Setup(score, floor, coins, victory);
    }

    public void HidePanel()
    {
        Canvas c = canvas != null ? canvas : GetComponent<Canvas>();
        if (c == null)
            c = GetComponentInChildren<Canvas>(true);
        if (c != null)
            c.gameObject.SetActive(false);
    }

    public void Setup(int score, int floor, int coins, bool victory = false)
    {
        EnsureUI();
        ResolveSceneTextReferences();

        if (titleText != null)
        {
            titleText.text = victory ? "CHIẾN THẮNG" : "THẤT BẠI";
            titleText.color = victory ? new Color(0.95f, 0.82f, 0.2f, 1f) : new Color(0.95f, 0.2f, 0.2f, 1f);
        }

        if (scoreResultText != null) scoreResultText.text = "Score: " + score;
        if (floorResultText != null) floorResultText.text = victory ? "Hoàn thành 10 tầng!" : "Floor: " + floor;
        if (coinsResultText != null)
            coinsResultText.text = "Xu: " + coins;

        FillPlayerStats();
        FillHeroAndBoss(floor);

        AchievementManager.Instance?.OnRunEnded(victory, floor);
    }

    /// <summary>Điền danh sách chỉ số người chơi (kiểu Player Stats của KnightFall).</summary>
    private void FillPlayerStats()
    {
        if (statsColumn == null)
            return;

        // Xoá dòng cũ.
        for (int i = statsColumn.childCount - 1; i >= 0; i--)
        {
            Transform c = statsColumn.GetChild(i);
            if (c.name.StartsWith("Stat_"))
                Destroy(c.gameObject);
        }
        statValueLabels.Clear();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        AutoAttack atk = player != null ? player.GetComponent<AutoAttack>() : null;
        HealthSystem hs = player != null ? player.GetComponent<HealthSystem>() : null;
        PlayerSkillStats st = player != null ? player.GetComponent<PlayerSkillStats>() : null;
        PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;

        float dmg = atk != null ? atk.ProjectileDamage : 0f;
        float aspd = atk != null && atk.FireInterval > 0.01f ? 1f / atk.FireInterval : 0f;
        float crit = st != null ? st.CritChance * 100f : 0f;
        float critDmg = st != null ? st.CritMultiplier * 100f : 200f;
        float maxHp = hs != null ? hs.MaxHP : 0f;
        float lifeSteal = st != null ? st.LifeStealPercent * 100f : 0f;
        float move = pc != null ? pc.MoveSpeed : 0f;
        float coinBonus = st != null ? st.CoinDropBonus * 100f : 100f;

        AddStatRow("Sát thương", Mathf.RoundToInt(dmg).ToString());
        AddStatRow("Tốc độ tấn công", aspd.ToString("0.0") + "/s");
        AddStatRow("Tỷ lệ chí mạng", Mathf.RoundToInt(crit) + "%");
        AddStatRow("Sát thương chí mạng", Mathf.RoundToInt(critDmg) + "%");
        AddStatRow("Máu tối đa", Mathf.RoundToInt(maxHp).ToString());
        AddStatRow("Hút máu", Mathf.RoundToInt(lifeSteal) + "%");
        AddStatRow("Tốc độ di chuyển", move.ToString("0.0"));
        AddStatRow("Thu nhập vàng", Mathf.RoundToInt(coinBonus) + "%");
    }

    private int statRowIndex;

    private void AddStatRow(string label, string value)
    {
        if (statValueLabels.Count == 0)
            statRowIndex = 0;

        float y = 220f - statRowIndex * 42f;
        statRowIndex++;

        TMP_Text l = MakeText(label, statsColumn, new Vector2(-30f, y), 20f, new Color(0.82f, 0.85f, 0.92f));
        l.alignment = TextAlignmentOptions.Left;
        l.rectTransform.sizeDelta = new Vector2(280f, 32f);
        l.gameObject.name = "Stat_L";

        TMP_Text v = MakeText(value, statsColumn, new Vector2(160f, y), 20f, new Color(0.55f, 1f, 0.55f), FontStyles.Bold);
        v.alignment = TextAlignmentOptions.Right;
        v.rectTransform.sizeDelta = new Vector2(120f, 32f);
        v.gameObject.name = "Stat_V";
        statValueLabels.Add(v);
    }

    private void FillHeroAndBoss(int floor)
    {
        HeroType hero = HeroRunStats.Instance != null ? HeroRunStats.Instance.SelectedHero : HeroType.Warrior;
        int level = ExpSystem.Instance != null ? ExpSystem.Instance.CurrentLevel : 1;

        if (heroLevelText != null)
            heroLevelText.text = HeroRunStats.GetDisplayName(hero) + "\nLEVEL " + level;

        if (heroIconImage != null)
        {
            Sprite[] frames = HeroKnightLibrary.GetHeroIdleFrames(hero);
            Sprite s = frames != null && frames.Length > 0 ? frames[0] : CharacterArtLibrary.GetHeroSprite(hero);
            if (s != null) { heroIconImage.sprite = s; heroIconImage.enabled = true; }
        }

        // Boss gần nhất theo tầng.
        string bossName = floor >= 10 ? "Dragon Lord" : floor >= 9 ? "Shadow Witch" : floor >= 6 ? "Stone Golem" : "Goblin King";
        if (bossInfoText != null)
            bossInfoText.text = bossName + "\n" + floor;

        if (bossIconImage != null)
        {
            Sprite[] bframes = HeroKnightLibrary.GetBossIdleFrames(bossName);
            if (bframes != null && bframes.Length > 0)
            {
                bossIconImage.sprite = bframes[0];
                bossIconImage.enabled = true;
            }
        }
    }

    private void WireButtons()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(PlayAgain);
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(GoToMenu);
            menuButton.onClick.AddListener(GoToMenu);
        }
    }

    private void ResolveSceneTextReferences()
    {
        if (titleText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].gameObject.name.IndexOf("GameOverTitle", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || texts[i].gameObject.name.IndexOf("Title", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    titleText = texts[i];
                    break;
                }
            }
        }
    }

    private void EnsureUI()
    {
        if (canvas != null)
        {
            ResolveSceneTextReferences();
            return;
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            ResolveSceneTextReferences();
            return;
        }

        // Build at runtime
        GameObject canvasGO = new GameObject("GameOverCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        canvas.gameObject.SetActive(false);

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Nền tối phủ kín.
        GameObject overlay = MakeRect("Overlay", canvasGO.transform, Vector2.zero, Vector2.zero);
        RectTransform overlayRT = overlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.04f, 0.92f);

        // Panel chính rộng (kiểu KnightFall).
        GameObject panel = MakeRect("Panel", canvasGO.transform, new Vector2(1280f, 760f), Vector2.zero);
        panel.GetComponent<RectTransform>().anchorMin = panel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        Image panelImg = panel.AddComponent<Image>();
        if (!GuiArtLibrary.ApplyPanel(panelImg, GuiArtLibrary.MenuPanel))
            panelImg.color = new Color(0.06f, 0.06f, 0.1f, 0.97f);
        Transform p = panel.transform;

        // Tiêu đề lớn trên cùng.
        titleText = MakeText("THẤT BẠI", p, new Vector2(0f, 320f), 64f, new Color(0.95f, 0.2f, 0.2f), FontStyles.Bold);
        RectTransform titleRt = titleText.rectTransform;
        titleRt.sizeDelta = new Vector2(800f, 90f);

        // --- Cột Player Stats bên trái ---
        GameObject statsPanel = MakeRect("StatsPanel", p, new Vector2(420f, 600f), new Vector2(-410f, -40f));
        statsPanel.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.06f, 0.95f);
        MakeText("Player Stats", statsPanel.transform, new Vector2(0f, 270f), 30f, Color.white, FontStyles.Bold);
        statsColumn = statsPanel.transform;

        // --- Giữa: Anh hùng + LEVEL + icon ---
        heroIconImage = MakeIcon("HeroIcon", p, new Vector2(60f, -90f), new Vector2(150f, 150f));
        heroLevelText = MakeText("Anh hùng\nLEVEL 1", p, new Vector2(60f, 120f), 30f, new Color(0.92f, 0.92f, 1f), FontStyles.Bold);
        heroLevelText.rectTransform.sizeDelta = new Vector2(360f, 120f);

        // --- Phải: Boss + cấp + icon ---
        bossIconImage = MakeIcon("BossIcon", p, new Vector2(440f, 200f), new Vector2(110f, 110f));
        bossInfoText = MakeText("Inferno\n5", p, new Vector2(440f, 120f), 28f, new Color(0.95f, 0.92f, 0.85f), FontStyles.Bold);
        bossInfoText.rectTransform.sizeDelta = new Vector2(320f, 110f);

        // Result text (gộp score/floor/xu) đặt giữa-dưới.
        scoreResultText = MakeText("Score: 0", p, new Vector2(60f, -250f), 24f, new Color(0.85f, 0.88f, 0.95f));
        floorResultText = MakeText("Floor: 0", p, new Vector2(60f, -285f), 24f, new Color(0.85f, 0.88f, 0.95f));
        coinsResultText = MakeText("Xu: 0", p, new Vector2(60f, -320f), 24f, new Color(1f, 0.85f, 0.35f));

        // Nút dưới.
        playAgainButton = MakeButton("Chơi lại!", p, new Vector2(-150f, -340f), PlayAgain);
        menuButton      = MakeButton("Thoát",     p, new Vector2(150f,  -340f), GoToMenu);
        TintButton(playAgainButton, new Color(0.55f, 0.16f, 0.18f, 1f));
        TintButton(menuButton, new Color(0.55f, 0.16f, 0.18f, 1f));
    }

    private static Image MakeIcon(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = MakeRect(name, parent, size, pos);
        GameObject frame = MakeRect("Frame", go.transform, Vector2.zero, Vector2.zero);
        RectTransform frt = frame.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
        frame.AddComponent<Image>().color = new Color(0.12f, 0.13f, 0.2f, 1f);

        GameObject inner = MakeRect("Img", go.transform, Vector2.zero, Vector2.zero);
        RectTransform irt = inner.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(8f, 8f); irt.offsetMax = new Vector2(-8f, -8f);
        Image img = inner.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static void TintButton(Button btn, Color color)
    {
        if (btn == null) return;
        Image bg = btn.targetGraphic as Image;
        if (bg != null) bg.color = color;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static GameObject MakeRect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    private static TMP_Text MakeText(string text, Transform parent, Vector2 pos, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject go = MakeRect("Lbl_" + text, parent, new Vector2(480f, 56f), pos);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeRect(label, parent, new Vector2(170f, 58f), pos);
        Image bg = go.AddComponent<Image>();
        Sprite guiBtn = label.Contains("lại", System.StringComparison.OrdinalIgnoreCase)
            ? GuiArtLibrary.ButtonPrimary
            : GuiArtLibrary.ButtonDanger;
        if (!GuiArtLibrary.ApplyButton(bg, guiBtn, new Color(0.18f, 0.18f, 0.32f, 1f)))
            bg.color = new Color(0.18f, 0.18f, 0.32f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
        cb.pressedColor     = new Color(0.4f, 0.4f, 0.6f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        GameObject textGO = MakeRect("Text", go.transform, Vector2.zero, Vector2.zero);
        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 21f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.raycastTarget = false;
        return btn;
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        RunManager.Instance?.ResetForNewRun();
        HUDManager.Resolve()?.ResetForNewRun();
        HidePanel();
        Instance = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        RunManager.Instance?.ResetForNewRun();
        HUDManager.Resolve()?.ResetForNewRun();
        HidePanel();
        Instance = null;

        // Về MainMenu nếu có, không thì về màn chọn nhân vật.
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
        else if (Application.CanStreamedLevelBeLoaded("CharacterSelectScene"))
            SceneManager.LoadScene("CharacterSelectScene");
        else
            SceneManager.LoadScene(0);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
