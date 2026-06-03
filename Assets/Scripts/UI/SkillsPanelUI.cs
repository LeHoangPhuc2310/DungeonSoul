using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SkillsPanelUI : MonoBehaviour
{
    public static SkillsPanelUI Instance { get; private set; }

    [SerializeField] private GameObject badgePrefab;
    [SerializeField] private float spacing = 5f;

    private List<GameObject> activeBadges = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        }
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    public void AddSkill(SkillData skill)
    {
        if (skill == null) return;
        if (activeBadges.Count >= 6) return;

        GameObject badge = CreateBadge(skill);
        activeBadges.Add(badge);
    }

    private GameObject CreateBadge(SkillData skill)
    {
        GameObject badge = new GameObject("SkillBadge_" + skill.skillName, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        badge.transform.SetParent(transform, false);

        UnityEngine.UI.Image bg = badge.GetComponent<UnityEngine.UI.Image>();
        bg.color = GetRarityColor(skill.rarity);
        // Assuming a simple rounded look via a sprite if available, otherwise just color
        // For this task, we will use a solid color.

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(badge.transform, false);
        
        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = skill.skillName;
        text.fontSize = 12;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-10, -4); // Padding

        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(text.preferredWidth + 15, 20);

        return badge;
    }

    private Color GetRarityColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Common: return new Color(0.5f, 0.5f, 0.5f);
            case SkillRarity.Rare: return new Color(0.2f, 0.4f, 0.8f);
            case SkillRarity.Epic: return new Color(0.6f, 0.2f, 0.8f);
            case SkillRarity.Legendary: return new Color(0.9f, 0.7f, 0.1f);
            default: return Color.gray;
        }
    }
}
