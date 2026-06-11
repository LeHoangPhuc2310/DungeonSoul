using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel trái kiểu Vampire Survivors: 6 vũ khí/skill + 6 passive + chỉ số Might/Area/...</summary>
public class VsStatsPanelUI : MonoBehaviour
{
    public static VsStatsPanelUI Instance { get; private set; }

    private const int SlotCount = 6;
    private const float SlotSize = 34f;

    private RectTransform panelRoot;
    private readonly Image[] weaponSlots = new Image[SlotCount];
    private readonly TMP_Text[] weaponLevels = new TMP_Text[SlotCount];
    private readonly Image[] passiveSlots = new Image[SlotCount];
    private readonly TMP_Text[] passiveLevels = new TMP_Text[SlotCount];
    private TMP_Text statsText;
    private readonly StringBuilder sb = new StringBuilder(256);
    private float refreshTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildPanel();
        SetVisible(SurvivalRunManager.IsSurvivalMode());
    }

    private void OnEnable()
    {
        GlobalStats.OnChanged += OnStatsChanged;
        if (PassiveItemManager.Instance != null)
            PassiveItemManager.Instance.OnPassivesChanged += OnStatsChanged;
    }

    private void OnDisable()
    {
        GlobalStats.OnChanged -= OnStatsChanged;
        if (PassiveItemManager.Instance != null)
            PassiveItemManager.Instance.OnPassivesChanged -= OnStatsChanged;
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf)
            return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.2f;
            GlobalStats.Refresh();
            RefreshStatsText();
        }
    }

    public void SetVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(visible);
    }

    public void RefreshWeaponSlots(IReadOnlyList<WeaponManager.WeaponSlot> weapons, int maxSlots)
    {
        if (!gameObject.activeSelf)
            return;

        bool useWeapons = WeaponStyleUtil.UsesWeaponPickupRewards();
        for (int i = 0; i < SlotCount; i++)
        {
            Image slot = weaponSlots[i];
            TMP_Text lvl = weaponLevels[i];
            if (slot == null)
                continue;

            if (useWeapons && weapons != null && i < weapons.Count)
            {
                WeaponManager.WeaponSlot w = weapons[i];
                Sprite icon = GameIconLibrary.WeaponSprite(w.weaponType);
                slot.sprite = icon != null ? icon : HUDManager.GetUiWhiteSpriteStatic();
                slot.color = icon != null ? GameIconLibrary.WeaponTint(w.weaponType) : new Color(0.2f, 0.22f, 0.3f, 0.8f);
                slot.enabled = true;
                if (lvl != null)
                {
                    lvl.text = w.evolved ? "★" + w.copies : w.copies.ToString();
                    lvl.enabled = true;
                }
            }
            else if (!useWeapons)
            {
                ApplySkillSlot(i, slot, lvl);
            }
            else
            {
                slot.sprite = HUDManager.GetUiWhiteSpriteStatic();
                slot.color = new Color(0.12f, 0.13f, 0.18f, 0.55f);
                if (lvl != null)
                {
                    lvl.text = string.Empty;
                    lvl.enabled = false;
                }
            }
        }
    }

    private static void ApplySkillSlot(int index, Image slot, TMP_Text lvl)
    {
        PlayerSkillHandler handler = PlayerSkillHandler.Instance;
        if (handler == null || handler.activeSkills == null || index >= handler.activeSkills.Count)
        {
            slot.sprite = HUDManager.GetUiWhiteSpriteStatic();
            slot.color = new Color(0.12f, 0.13f, 0.18f, 0.55f);
            if (lvl != null)
            {
                lvl.text = string.Empty;
                lvl.enabled = false;
            }
            return;
        }

        SkillData skill = handler.activeSkills[index];
        if (skill == null)
            return;

        Sprite icon = GameIconLibrary.SkillSprite(skill.skillType);
        slot.sprite = icon != null ? icon : HUDManager.GetUiWhiteSpriteStatic();
        slot.color = icon != null ? GameIconLibrary.SkillTint(skill.skillType) : Color.white;
        if (lvl != null)
        {
            int stack = handler.GetStack(skill.skillType);
            lvl.text = stack > 0 ? stack.ToString() : string.Empty;
            lvl.enabled = stack > 0;
        }
    }

    public void RefreshPassiveSlots(IReadOnlyList<PassivePick> passives, int maxSlots)
    {
        if (!gameObject.activeSelf)
            return;

        for (int i = 0; i < SlotCount; i++)
        {
            Image slot = passiveSlots[i];
            TMP_Text lvl = passiveLevels[i];
            if (slot == null)
                continue;

            if (passives != null && i < passives.Count && passives[i]?.data != null)
            {
                PassivePick pick = passives[i];
                Sprite icon = GameIconLibrary.PassiveSprite(pick.data);
                slot.sprite = icon != null ? icon : HUDManager.GetUiWhiteSpriteStatic();
                slot.color = icon != null ? GameIconLibrary.PassiveTint(pick.data) : Color.white;
                if (lvl != null)
                {
                    lvl.text = pick.level.ToString();
                    lvl.enabled = true;
                }
            }
            else
            {
                slot.sprite = HUDManager.GetUiWhiteSpriteStatic();
                slot.color = new Color(0.12f, 0.13f, 0.18f, 0.55f);
                if (lvl != null)
                {
                    lvl.text = string.Empty;
                    lvl.enabled = false;
                }
            }
        }
    }

    private void OnStatsChanged()
    {
        RefreshStatsText();
    }

    private void RefreshStatsText()
    {
        if (statsText == null)
            return;

        VsStatsSnapshot s = GlobalStats.Current;
        sb.Clear();

        AppendLine("Max Health", s.MaxHealth.ToString("0"));
        AppendLine("Recovery", s.Recovery.ToString("0.##"));
        AppendLine("Armor", FormatPercent(s.Armor));
        AppendLine("Move Speed", FormatPercent(s.MoveSpeedPercent));
        sb.Append('\n');
        AppendLine("Might", FormatPercent(s.MightPercent));
        AppendLine("Area", FormatPercent(s.AreaPercent));
        AppendLine("Speed", FormatPercent(s.SpeedPercent));
        AppendLine("Duration", FormatPercent(s.DurationPercent));
        AppendLine("Amount", s.Amount > 0 ? "+" + s.Amount : "0");
        AppendLine("Cooldown", FormatSignedPercent(-s.CooldownPercent));
        sb.Append('\n');
        AppendLine("Luck", FormatPercent(s.Luck * 100f));
        AppendLine("Growth", FormatPercent(s.GrowthPercent));
        AppendLine("Greed", FormatPercent(s.GreedPercent));
        AppendLine("Magnet", s.Magnet.ToString("0"));

        statsText.text = sb.ToString();
    }

    private void AppendLine(string label, string value)
    {
        sb.Append(label).Append("  ").Append(value).Append('\n');
    }

    private static string FormatPercent(float v)
    {
        if (Mathf.Abs(v) < 0.05f)
            return "0%";
        return (v >= 0f ? "+" : "") + v.ToString("0") + "%";
    }

    private static string FormatSignedPercent(float v)
    {
        if (Mathf.Abs(v) < 0.05f)
            return "0%";
        return v.ToString("0") + "%";
    }

    private void BuildPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = HUDManager.Resolve() != null ? HUDManager.Resolve().GetComponentInParent<Canvas>() : null;
        if (canvas == null)
            return;

        GameObject root = new GameObject("VsStatsPanel", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvas.transform, false);
        panelRoot = root.GetComponent<RectTransform>();

        Image bg = root.GetComponent<Image>();
        bg.sprite = HUDManager.GetUiWhiteSpriteStatic();
        bg.color = new Color(0.04f, 0.06f, 0.1f, 0.72f);
        bg.raycastTarget = false;

        panelRoot.anchorMin = new Vector2(0f, 0.12f);
        panelRoot.anchorMax = new Vector2(0f, 0.88f);
        panelRoot.pivot = new Vector2(0f, 1f);
        panelRoot.anchoredPosition = new Vector2(8f, -8f);
        panelRoot.sizeDelta = new Vector2(200f, 0f);

        float y = -10f;
        y = BuildSlotRow(root.transform, "Weapons", weaponSlots, weaponLevels, y, "Vũ khí / Skill");
        y -= 8f;
        y = BuildSlotRow(root.transform, "Passives", passiveSlots, passiveLevels, y, "Passive");
        y -= 12f;

        GameObject statsGo = new GameObject("StatsText", typeof(RectTransform));
        statsGo.transform.SetParent(root.transform, false);
        RectTransform statsRt = statsGo.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0f, 1f);
        statsRt.anchorMax = new Vector2(1f, 1f);
        statsRt.pivot = new Vector2(0f, 1f);
        statsRt.anchoredPosition = new Vector2(8f, y);
        statsRt.sizeDelta = new Vector2(-16f, 420f);

        statsText = statsGo.AddComponent<TextMeshProUGUI>();
        GameUIFont.Apply(statsText, GameUIFont.Role.CardBody);
        statsText.fontSize = 15f;
        statsText.color = new Color(0.88f, 0.9f, 0.95f, 0.95f);
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.raycastTarget = false;
    }

    private float BuildSlotRow(Transform parent, string rowName, Image[] slots, TMP_Text[] levels, float topY, string label)
    {
        GameObject labelGo = new GameObject(rowName + "Label", typeof(RectTransform));
        labelGo.transform.SetParent(parent, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0f, 1f);
        labelRt.anchoredPosition = new Vector2(10f, topY);
        labelRt.sizeDelta = new Vector2(-12f, 18f);
        TMP_Text labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        GameUIFont.Apply(labelTmp, GameUIFont.Role.CardStack);
        labelTmp.fontSize = 12f;
        labelTmp.text = label;
        labelTmp.color = new Color(0.75f, 0.78f, 0.85f, 0.9f);

        float rowY = topY - 22f;
        for (int i = 0; i < SlotCount; i++)
        {
            GameObject slotGo = new GameObject(rowName + i, typeof(RectTransform), typeof(Image));
            slotGo.transform.SetParent(parent, false);
            RectTransform rt = slotGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f + i * (SlotSize + 4f), rowY);
            rt.sizeDelta = new Vector2(SlotSize, SlotSize);

            Image img = slotGo.GetComponent<Image>();
            img.sprite = HUDManager.GetUiWhiteSpriteStatic();
            img.color = new Color(0.12f, 0.13f, 0.18f, 0.55f);
            img.raycastTarget = false;
            slots[i] = img;

            GameObject lvlGo = new GameObject("Lv", typeof(RectTransform));
            lvlGo.transform.SetParent(slotGo.transform, false);
            RectTransform lrt = lvlGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(1f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(1f, 0f);
            lrt.anchoredPosition = new Vector2(-1f, 1f);
            lrt.sizeDelta = new Vector2(16f, 12f);
            TMP_Text lvl = lvlGo.AddComponent<TextMeshProUGUI>();
            GameUIFont.Apply(lvl, GameUIFont.Role.CardStack);
            lvl.fontSize = 10f;
            lvl.alignment = TextAlignmentOptions.BottomRight;
            lvl.color = Color.white;
            lvl.raycastTarget = false;
            levels[i] = lvl;
        }

        return rowY - SlotSize;
    }
}
