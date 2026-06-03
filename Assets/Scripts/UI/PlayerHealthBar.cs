using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("UI References (auto-created if missing)")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform barRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text hpText;

    private const float BarWidth = 300f;
    private const float BarHeight = 25f;
    private readonly Color backgroundColor = new Color(0.35f, 0.08f, 0.08f, 0.95f); // Dark red
    private readonly Color highHpColor = new Color(0.2f, 1f, 0.2f, 1f); // Bright green
    private readonly Color midHpColor = new Color(1f, 0.85f, 0.2f, 1f); // Yellow
    private readonly Color lowHpColor = new Color(0.95f, 0.2f, 0.2f, 1f); // Red

    private void Awake()
    {
        EnsureUI();
    }

    private void Update()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            SetEmptyState();
            return;
        }

        HealthSystem health = player.GetComponent<HealthSystem>();
        if (health == null)
        {
            SetEmptyState();
            return;
        }

        float maxHp = Mathf.Max(1f, health.MaxHP);
        float currentHp = Mathf.Clamp(health.CurrentHP, 0f, maxHp);
        float hpPercent = currentHp / maxHp;

        if (fillImage != null)
            fillImage.fillAmount = hpPercent;

        if (hpText != null)
            hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";

        if (fillImage != null)
            fillImage.color = GetHpColor(hpPercent);
    }

    private Color GetHpColor(float hpPercent)
    {
        if (hpPercent > 0.6f)
            return highHpColor;

        if (hpPercent >= 0.3f)
            return midHpColor;

        // Low HP flash animation.
        float pulse = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
        return Color.Lerp(lowHpColor, Color.white, pulse * 0.35f);
    }

    private void SetEmptyState()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = lowHpColor;
        }

        if (hpText != null)
            hpText.text = "0 / 0";
    }

    private void EnsureUI()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>(true);

        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("PlayerHealthBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            targetCanvas = canvasObject.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (barRoot == null)
        {
            GameObject rootObject = new GameObject("PlayerHealthBarRoot", typeof(RectTransform));
            barRoot = rootObject.GetComponent<RectTransform>();
            barRoot.SetParent(targetCanvas.transform, false);
        }

        barRoot.anchorMin = new Vector2(0f, 1f);
        barRoot.anchorMax = new Vector2(0f, 1f);
        barRoot.pivot = new Vector2(0f, 1f);
        barRoot.anchoredPosition = new Vector2(20f, -20f);
        barRoot.sizeDelta = new Vector2(BarWidth, BarHeight);

        if (backgroundImage == null)
        {
            backgroundImage = barRoot.GetComponent<Image>();
            if (backgroundImage == null)
                backgroundImage = barRoot.gameObject.AddComponent<Image>();
        }
        backgroundImage.color = backgroundColor;

        if (fillImage == null)
        {
            Transform existing = barRoot.Find("Fill");
            if (existing != null)
                fillImage = existing.GetComponent<Image>();

            if (fillImage == null)
            {
                GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillObject.transform.SetParent(barRoot, false);
                fillImage = fillObject.GetComponent<Image>();
            }
        }

        RectTransform fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.color = highHpColor;

        if (hpText == null)
        {
            Transform existingText = barRoot.Find("HPText");
            if (existingText != null)
                hpText = existingText.GetComponent<TMP_Text>();

            if (hpText == null)
            {
                GameObject textObject = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(barRoot, false);
                hpText = textObject.GetComponent<TMP_Text>();
            }
        }

        RectTransform textRect = hpText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.fontSize = 18f;
        hpText.fontStyle = FontStyles.Bold;
        hpText.color = Color.white;
        hpText.text = "0 / 0";
    }
}
