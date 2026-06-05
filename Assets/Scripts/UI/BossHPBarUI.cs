// DungeonSoul — BossHPBarUI.cs — Thanh máu boss (giữa-dưới), cùng phong cách thanh HP nhân vật.

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHPBarUI : MonoBehaviour
{
    public static BossHPBarUI Instance { get; private set; }

    private static readonly Color BarBg = new Color(0.1f, 0.1f, 0.14f, 0.92f);
    private static readonly Color BarFill = new Color(0.92f, 0.22f, 0.24f, 1f);
    private const float FillPadding = 5f;

    private static Sprite whiteSprite;

    private Canvas canvas;
    private RectTransform panelRt;
    private RectTransform fillRt;
    private Image fillImage;
    private TMP_Text nameText;
    private HealthSystem tracked;
    private Coroutine slideRoutine;
    private readonly float shownY = 132f;
    private readonly float hiddenY = -120f;
    private float displayedRatio = 1f;

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
        BuildUI();
        Hide();
    }

    private void Update()
    {
        if (tracked == null)
            return;

        float max = Mathf.Max(1f, tracked.MaxHP);
        float target = Mathf.Clamp01(tracked.CurrentHP / max);
        displayedRatio = Mathf.MoveTowards(displayedRatio, target, Time.unscaledDeltaTime * 1.6f);
        ApplyRatio(displayedRatio);
    }

    public static void Track(HealthSystem bossHealth, string displayName)
    {
        if (bossHealth == null)
            return;

        BossHPBarUI ui = Instance;
        if (ui == null)
        {
            GameObject go = new GameObject("BossHPBarUI");
            ui = go.AddComponent<BossHPBarUI>();
        }

        ui.tracked = bossHealth;
        ui.displayedRatio = Mathf.Clamp01(bossHealth.CurrentHP / Mathf.Max(1f, bossHealth.MaxHP));
        ui.ApplyRatio(ui.displayedRatio);
        ui.ShowBar(displayName);
    }

    public static void HideBar()
    {
        if (Instance != null)
            Instance.Hide();
    }

    private void ApplyRatio(float ratio01)
    {
        if (fillRt == null)
            return;

        ratio01 = Mathf.Clamp01(ratio01);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(ratio01, 1f);
        fillRt.offsetMin = new Vector2(FillPadding, FillPadding);
        fillRt.offsetMax = new Vector2(-FillPadding * 0.5f, -FillPadding);

        if (fillImage != null)
            fillImage.enabled = ratio01 > 0.001f;
    }

    private void ShowBar(string displayName)
    {
        if (canvas == null)
            BuildUI();

        canvas.gameObject.SetActive(true);
        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(displayName) ? "BOSS" : displayName;

        if (slideRoutine != null)
            StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideTo(shownY));
    }

    private void Hide()
    {
        tracked = null;
        if (panelRt == null)
            return;

        if (slideRoutine != null)
            StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideTo(hiddenY, deactivateCanvas: true));
    }

    private IEnumerator SlideTo(float targetY, bool deactivateCanvas = false)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector2 start = panelRt.anchoredPosition;
        Vector2 end = new Vector2(0f, targetY);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            panelRt.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        panelRt.anchoredPosition = end;
        if (deactivateCanvas && canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        if (canvas != null)
            return;

        GameObject canvasGO = new GameObject("BossHPBarCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Outer frame (thin dark border) -> background -> fill, like the player HP bar.
        GameObject panel = new GameObject("BossBar", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, hiddenY);
        panelRt.sizeDelta = new Vector2(760f, 38f);

        Image frame = panel.AddComponent<Image>();
        frame.sprite = GetWhiteSprite();
        frame.type = Image.Type.Simple;
        frame.color = new Color(0f, 0f, 0f, 0.7f);
        frame.raycastTarget = false;

        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(panel.transform, false);
        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(3f, 3f);
        bgRt.offsetMax = new Vector2(-3f, -3f);
        Image bg = bgGO.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.type = Image.Type.Simple;
        bg.color = BarBg;
        bg.raycastTarget = false;

        GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
        fillGO.transform.SetParent(panel.transform, false);
        fillRt = fillGO.GetComponent<RectTransform>();
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillImage = fillGO.AddComponent<Image>();
        fillImage.sprite = GetWhiteSprite();
        fillImage.type = Image.Type.Simple;
        fillImage.color = BarFill;
        fillImage.raycastTarget = false;
        ApplyRatio(1f);

        GameObject labelGO = new GameObject("Name", typeof(RectTransform));
        labelGO.transform.SetParent(panel.transform, false);
        RectTransform labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 6f);
        labelRt.sizeDelta = new Vector2(0f, 34f);
        nameText = labelGO.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 26f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = new Color(1f, 0.86f, 0.55f, 1f);
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Overflow;
        nameText.outlineWidth = 0.18f;
        nameText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        nameText.raycastTarget = false;
        GameUIFont.Apply(nameText, GameUIFont.Role.CardTitle);
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        const int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}
