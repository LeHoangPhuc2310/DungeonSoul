using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tooltip khi rê chuột vào ô skill / vũ khí trên HUD.</summary>
public class SkillTooltipUI : MonoBehaviour
{
    public static SkillTooltipUI Instance { get; private set; }

    [SerializeField] private Vector2 screenOffset = new Vector2(14f, -14f);
    [SerializeField] private float maxWidth = 280f;

    private RectTransform panelRt;
    private TMP_Text titleText;
    private TMP_Text metaText;
    private TMP_Text bodyText;
    private Image borderImage;
    private Canvas rootCanvas;
    private bool visible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();
        BuildPanel();
        Hide();
    }

    private void Update()
    {
        if (!visible || panelRt == null)
            return;

        Vector2 pos = (Vector2)Input.mousePosition + screenOffset;
        ClampToScreen(ref pos);
        panelRt.position = pos;
    }

    public void ShowSkill(SkillData skill, int stack, RectTransform anchor)
    {
        if (skill == null)
        {
            Hide();
            return;
        }

        string meta = GetRarityLabel(skill.rarity);
        if (stack > 1)
            meta += "  •  Cấp " + stack;

        Show(skill.skillName, meta, skill.description, GetRarityColor(skill.rarity), anchor);
    }

    public void ShowWeapon(WeaponType type, int copies, bool evolved, RectTransform anchor)
    {
        string meta = evolved ? "Vũ khí  •  Tiến hóa" : "Vũ khí";
        if (copies > 1)
            meta += "  •  x" + copies;

        Show(RunLoadout.DisplayName(type), meta, RunLoadout.Description(type),
            new Color(0.35f, 0.75f, 0.95f, 1f), anchor);
    }

    public void ShowPassive(PassiveItemData passive, int level, RectTransform anchor)
    {
        if (passive == null)
        {
            Hide();
            return;
        }

        string meta = GetRarityLabel(passive.rarity) + "  •  Trang bị";
        if (level > 0)
            meta += "  •  Cấp " + level;

        Show(passive.displayName, meta, passive.GetLevelDescription(level),
            GetRarityColor(passive.rarity), anchor);
    }

    public void Show(string title, string meta, string description, Color accent, RectTransform anchor)
    {
        EnsurePanel();

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(title) ? "—" : title;
        if (metaText != null)
            metaText.text = meta ?? string.Empty;
        if (bodyText != null)
            bodyText.text = string.IsNullOrWhiteSpace(description) ? "Không có mô tả." : description;
        if (borderImage != null)
            borderImage.color = accent;

        panelRt.gameObject.SetActive(true);
        visible = true;

        if (anchor != null)
        {
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector2 pos = (Vector2)corners[2] + screenOffset;
            ClampToScreen(ref pos);
            panelRt.position = pos;
        }
        else
        {
            Vector2 pos = (Vector2)Input.mousePosition + screenOffset;
            ClampToScreen(ref pos);
            panelRt.position = pos;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
    }

    public void Hide()
    {
        visible = false;
        if (panelRt != null)
            panelRt.gameObject.SetActive(false);
    }

    private void EnsurePanel()
    {
        if (panelRt != null)
            return;

        BuildPanel();
    }

    private void BuildPanel()
    {
        GameObject panelGo = new GameObject("SkillTooltip", typeof(RectTransform));
        panelGo.transform.SetParent(transform, false);
        panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.sizeDelta = new Vector2(maxWidth, 10f);

        GameObject borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(panelRt, false);
        RectTransform borderRt = borderGo.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = Vector2.zero;
        borderRt.offsetMax = Vector2.zero;
        borderImage = borderGo.GetComponent<Image>();
        borderImage.color = new Color(0.4f, 0.7f, 1f, 1f);
        borderImage.raycastTarget = false;

        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        bgGo.transform.SetParent(panelRt, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(2f, 2f);
        bgRt.offsetMax = new Vector2(-2f, -2f);
        Image bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.96f);
        bgImg.raycastTarget = false;

        VerticalLayoutGroup vlg = bgGo.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = bgGo.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        titleText = CreateTooltipLine(bgGo.transform, "Title", GameUIFont.Role.TooltipTitle, 16f);
        metaText = CreateTooltipLine(bgGo.transform, "Meta", GameUIFont.Role.CardRarity, 12f);
        bodyText = CreateTooltipLine(bgGo.transform, "Body", GameUIFont.Role.TooltipBody, 13f);

        panelGo.transform.SetAsLastSibling();
    }

    private static TMP_Text CreateTooltipLine(Transform parent, string name, GameUIFont.Role role, float minHeight)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = minHeight;
        le.preferredWidth = 240f;

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        GameUIFont.Apply(tmp, role);
        tmp.raycastTarget = false;
        tmp.richText = true;
        return tmp;
    }

    private void ClampToScreen(ref Vector2 screenPos)
    {
        if (panelRt == null)
            return;

        float w = panelRt.rect.width > 1f ? panelRt.rect.width : maxWidth;
        float h = panelRt.rect.height > 1f ? panelRt.rect.height : 80f;

        screenPos.x = Mathf.Clamp(screenPos.x, 4f, Screen.width - w - 4f);
        screenPos.y = Mathf.Clamp(screenPos.y, h + 4f, Screen.height - 4f);
    }

    private static string GetRarityLabel(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Rare: return "Hiếm";
            case SkillRarity.Epic: return "Sử thi";
            case SkillRarity.Legendary: return "Huyền thoại";
            default: return "Thường";
        }
    }

    private static Color GetRarityColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Rare: return new Color(0.2f, 0.6f, 0.95f, 1f);
            case SkillRarity.Epic: return new Color(0.55f, 0.28f, 0.85f, 1f);
            case SkillRarity.Legendary: return new Color(0.95f, 0.72f, 0.15f, 1f);
            default: return new Color(0.45f, 0.48f, 0.55f, 1f);
        }
    }

    public static SkillTooltipUI GetOrCreate(Canvas canvas)
    {
        if (Instance != null)
            return Instance;

        if (canvas == null)
            canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
            return null;

        GameObject go = new GameObject("SkillTooltipUI");
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go.AddComponent<SkillTooltipUI>();
    }
}
