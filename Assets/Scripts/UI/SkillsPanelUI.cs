using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillsPanelUI : MonoBehaviour
{
    public static SkillsPanelUI Instance { get; private set; }

    [SerializeField] private float slotSize = 44f;
    [SerializeField] private float spacing = 6f;
    [SerializeField] private int maxSlots = 8;

    private readonly Dictionary<SkillType, SkillSlotView> slots = new Dictionary<SkillType, SkillSlotView>();

    private class SkillSlotView
    {
        public GameObject root;
        public TMP_Text abbrevText;
        public TMP_Text stackText;
        public Image borderImage;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureLayout();
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        RefreshFrom(PlayerSkillHandler.Instance);
    }

    private void EnsureLayout()
    {
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 2, 0);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    public void AddSkill(SkillData skill) => AddOrUpdateSkill(skill);

    public void AddOrUpdateSkill(SkillData skill)
    {
        if (skill == null)
            return;

        int stack = PlayerSkillHandler.Instance != null
            ? PlayerSkillHandler.Instance.GetStack(skill.skillType)
            : 1;

        if (slots.TryGetValue(skill.skillType, out SkillSlotView existing))
        {
            UpdateStackLabel(existing, stack);
            return;
        }

        if (slots.Count >= maxSlots)
            return;

        slots[skill.skillType] = CreateSlot(skill, stack);
    }

    public void RefreshFrom(PlayerSkillHandler handler)
    {
        ClearAll();
        if (handler == null)
            return;

        var seen = new HashSet<SkillType>();
        for (int i = 0; i < handler.activeSkills.Count; i++)
        {
            SkillData data = handler.activeSkills[i];
            if (data == null || seen.Contains(data.skillType))
                continue;

            seen.Add(data.skillType);
            AddOrUpdateSkill(data);
        }
    }

    private SkillSlotView CreateSlot(SkillData skill, int stack)
    {
        GameObject root = new GameObject("Skill_" + skill.skillType, typeof(RectTransform));
        root.transform.SetParent(transform, false);

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(slotSize, slotSize);

        LayoutElement le = root.AddComponent<LayoutElement>();
        le.preferredWidth = slotSize;
        le.preferredHeight = slotSize;
        le.minWidth = slotSize;
        le.minHeight = slotSize;

        GameObject borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(root.transform, false);
        RectTransform borderRt = borderGo.GetComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = Vector2.zero;
        borderRt.offsetMax = Vector2.zero;
        Image borderImg = borderGo.GetComponent<Image>();
        borderImg.color = GetRarityColor(skill.rarity);
        borderImg.raycastTarget = false;

        GameObject bgGo = new GameObject("IconBg", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(root.transform, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(3f, 3f);
        bgRt.offsetMax = new Vector2(-3f, -3f);
        Image bgImg = bgGo.GetComponent<Image>();
        bgImg.color = new Color(0.07f, 0.08f, 0.13f, 1f);
        bgImg.raycastTarget = false;

        // Icon sprite thật từ thư viện art.
        Sprite iconSprite = GameIconLibrary.SkillSprite(skill.skillType);

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(bgGo.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(2f, 2f);
        iconRt.offsetMax = new Vector2(-2f, -2f);
        Image iconImg = iconGo.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        iconImg.sprite = iconSprite;
        iconImg.color = iconSprite != null ? GameIconLibrary.SkillTint(skill.skillType) : new Color(1f, 1f, 1f, 0f);
        iconImg.enabled = iconSprite != null;

        // Chữ viết tắt làm dự phòng khi không tải được sprite.
        GameObject abbrevGo = new GameObject("Abbrev", typeof(RectTransform), typeof(TextMeshProUGUI));
        abbrevGo.transform.SetParent(bgGo.transform, false);
        RectTransform abbrevRt = abbrevGo.GetComponent<RectTransform>();
        abbrevRt.anchorMin = Vector2.zero;
        abbrevRt.anchorMax = Vector2.one;
        abbrevRt.offsetMin = Vector2.zero;
        abbrevRt.offsetMax = Vector2.zero;
        TextMeshProUGUI abbrev = abbrevGo.GetComponent<TextMeshProUGUI>();
        abbrev.text = GetAbbreviation(skill);
        abbrev.fontSize = 14f;
        abbrev.fontStyle = FontStyles.Bold;
        abbrev.alignment = TextAlignmentOptions.Center;
        abbrev.color = Color.white;
        abbrev.raycastTarget = false;
        abbrev.textWrappingMode = TextWrappingModes.NoWrap;
        abbrev.gameObject.SetActive(iconSprite == null);

        GameObject stackGo = new GameObject("Stack", typeof(RectTransform), typeof(TextMeshProUGUI));
        stackGo.transform.SetParent(root.transform, false);
        RectTransform stackRt = stackGo.GetComponent<RectTransform>();
        stackRt.anchorMin = new Vector2(1f, 0f);
        stackRt.anchorMax = new Vector2(1f, 0f);
        stackRt.pivot = new Vector2(1f, 0f);
        stackRt.anchoredPosition = new Vector2(2f, -2f);
        stackRt.sizeDelta = new Vector2(22f, 16f);
        TextMeshProUGUI stackTmp = stackGo.GetComponent<TextMeshProUGUI>();
        stackTmp.fontSize = 11f;
        stackTmp.fontStyle = FontStyles.Bold;
        stackTmp.alignment = TextAlignmentOptions.BottomRight;
        stackTmp.color = new Color(1f, 0.92f, 0.35f, 1f);
        stackTmp.raycastTarget = false;
        var view = new SkillSlotView
        {
            root = root,
            abbrevText = abbrev,
            stackText = stackTmp,
            borderImage = borderImg
        };
        UpdateStackLabel(view, stack);
        return view;
    }

    private static void UpdateStackLabel(SkillSlotView view, int stack)
    {
        if (view.stackText == null)
            return;

        view.stackText.text = stack > 1 ? "x" + stack : string.Empty;
        view.stackText.gameObject.SetActive(stack > 1);
    }

    private void ClearAll()
    {
        foreach (var pair in slots)
        {
            if (pair.Value.root != null)
                Destroy(pair.Value.root);
        }
        slots.Clear();
    }

    private static string GetAbbreviation(SkillData skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.skillName))
            return "?";

        string name = skill.skillName.Trim();
        if (name.Length <= 2)
            return name.ToUpperInvariant();

        if (name.Contains(' '))
        {
            string[] parts = name.Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return (char.ToUpperInvariant(parts[0][0]).ToString()
                    + char.ToUpperInvariant(parts[1][0]));
        }

        return name.Substring(0, 2).ToUpperInvariant();
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

}
