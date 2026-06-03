using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpBarUI : MonoBehaviour
{
    [Header("References (Optional)")]
    [SerializeField] private Image expFillImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expText;

    [Header("Visual")]
    [SerializeField] private Color lowExpColor = new Color(0.95f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color highExpColor = new Color(0.25f, 0.95f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.25f;
    [SerializeField] private Color flashColor = Color.white;

    private int lastKnownLevel = -1;
    private float flashTimer;
    private Image expBackgroundImage;

    private void Awake()
    {
        EnsureUI();
    }

    private void Update()
    {
        ExpSystem expSystem = ExpSystem.Instance;
        if (expSystem == null)
            return;

        float maxExp = Mathf.Max(1f, expSystem.ExpToNextLevel);
        float ratio = Mathf.Clamp01(expSystem.CurrentExp / maxExp);
        int level = Mathf.Max(1, expSystem.CurrentLevel);

        if (expFillImage != null)
        {
            expFillImage.fillAmount = ratio;
            Color baseColor = Color.Lerp(lowExpColor, highExpColor, ratio);
            expFillImage.color = flashTimer > 0f ? Color.Lerp(baseColor, flashColor, flashTimer / flashDuration) : baseColor;
        }

        if (levelText != null)
            levelText.text = $"LV.{level}";

        if (expText != null)
            expText.text = $"{Mathf.FloorToInt(expSystem.CurrentExp)} / {Mathf.CeilToInt(maxExp)} EXP";

        if (lastKnownLevel > 0 && level > lastKnownLevel)
            flashTimer = flashDuration;

        lastKnownLevel = level;
        flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime);
    }

    private void EnsureUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("ExpBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        RectTransform root = FindOrCreateRect("ExpBar", canvasRect);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 0f);
        root.sizeDelta = new Vector2(0f, 25f);

        RectTransform bg = FindOrCreateRect("ExpBarBackground", root);
        bg.anchorMin = new Vector2(0f, 0f);
        bg.anchorMax = new Vector2(1f, 1f);
        bg.offsetMin = new Vector2(16f, 0f);
        bg.offsetMax = new Vector2(-16f, 0f);

        if (expBackgroundImage == null)
            expBackgroundImage = GetOrAdd<UnityEngine.UI.Image>(bg.gameObject);
        expBackgroundImage.color = new Color(0.08f, 0.1f, 0.12f, 0.9f);

        RectTransform fillRect = FindOrCreateRect("ExpBarFill", bg);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        if (expFillImage == null)
            expFillImage = GetOrAdd<UnityEngine.UI.Image>(fillRect.gameObject);
        expFillImage.type = UnityEngine.UI.Image.Type.Filled;
        expFillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        expFillImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
        expFillImage.fillAmount = 0f;

        if (levelText == null)
        {
            RectTransform levelRect = FindOrCreateRect("LevelText", root);
            levelRect.anchorMin = new Vector2(0f, 0.5f);
            levelRect.anchorMax = new Vector2(0f, 0.5f);
            levelRect.pivot = new Vector2(0f, 0.5f);
            levelRect.anchoredPosition = new Vector2(24f, 0f);
            levelRect.sizeDelta = new Vector2(180f, 25f);
            levelText = ConfigureText(levelRect.gameObject, TextAlignmentOptions.Left, 14f, FontStyles.Bold);
        }

        if (expText == null)
        {
            RectTransform expRect = FindOrCreateRect("ExpText", root);
            expRect.anchorMin = new Vector2(1f, 0.5f);
            expRect.anchorMax = new Vector2(1f, 0.5f);
            expRect.pivot = new Vector2(1f, 0.5f);
            expRect.anchoredPosition = new Vector2(-24f, 0f);
            expRect.sizeDelta = new Vector2(380f, 25f);
            expText = ConfigureText(expRect.gameObject, TextAlignmentOptions.Right, 14f, FontStyles.Bold);
        }
    }

    private static RectTransform FindOrCreateRect(string name, RectTransform parent)
    {
        Transform existing = parent.Find(name);
        RectTransform rect;
        if (existing != null)
        {
            rect = existing as RectTransform;
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }

        return rect;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = target.AddComponent<T>();
        return component;
    }

    private static TMP_Text ConfigureText(GameObject target, TextAlignmentOptions alignment, float size, FontStyles style)
    {
        TMP_Text text = GetOrAdd<TextMeshProUGUI>(target);
        text.alignment = alignment;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
