using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSelectionUI : MonoBehaviour
{
    private enum ChoiceKind
    {
        SkillUpgrade,
        WeaponPickup,
        PassiveItem
    }

    private class LevelUpChoice
    {
        public ChoiceKind kind;
        public SkillData skill;
        public WeaponType weaponType;
        public PassiveItem passiveItem;
    }

    public static SkillSelectionUI Instance { get; private set; }

    public Canvas skillCanvas;
    public Button[] skillButtons = new Button[3];
    public List<SkillData> allSkills = new List<SkillData>();

    private readonly List<LevelUpChoice> currentChoices = new List<LevelUpChoice>(3);
    private bool isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureCanvasReference();
        Hide();
        EnsureButtonArray();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static SkillSelectionUI GetOrFind()
    {
        if (Instance != null)
            return Instance;

        Instance = Object.FindAnyObjectByType<SkillSelectionUI>(FindObjectsInactive.Include);
        if (Instance != null)
            return Instance;

        // Auto-create if not present in scene
        GameObject go = new GameObject("SkillSelectionUI");
        go.AddComponent<SkillSelectionUI>();
        return Instance;
    }

    public void Hide()
    {
        isOpen = false;
        if (skillCanvas != null)
            skillCanvas.gameObject.SetActive(false);

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    public void Show()
    {
        OpenPanel(useFullChoices: true, RoomType.Normal);
    }

    public void ShowChest(RoomType chestRoom = RoomType.Normal)
    {
        OpenPanel(useFullChoices: false, chestRoom);
    }

    private void EnsureCanvasReference()
    {
        if (skillCanvas == null)
            skillCanvas = GetComponent<Canvas>();

        if (skillCanvas == null)
            skillCanvas = GetComponentInChildren<Canvas>(true);

        // No canvas anywhere — build one at runtime so the panel always works.
        if (skillCanvas == null)
        {
            GameObject canvasGO = new GameObject("SkillSelectionCanvas");
            canvasGO.transform.SetParent(transform, false);
            skillCanvas = canvasGO.AddComponent<Canvas>();
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        skillCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        skillCanvas.sortingOrder = 200;
        skillCanvas.enabled = true;

        if (skillCanvas.GetComponent<GraphicRaycaster>() == null)
            skillCanvas.gameObject.AddComponent<GraphicRaycaster>();

        // Buttons require an EventSystem to receive clicks
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private void OpenPanel(bool useFullChoices, RoomType chestRoom)
    {
        EnsureCanvasReference();
        if (skillCanvas == null)
        {
            Debug.LogWarning("[SkillSelectionUI] skillCanvas missing — cannot open panel.");
            return;
        }

        if (allSkills == null || allSkills.Count == 0)
        {
            SkillData[] loaded = Resources.LoadAll<SkillData>("SkillData");
            allSkills = new List<SkillData>(loaded);
        }

        EnsureButtonArray();
        if (useFullChoices)
            BuildChoices();
        else
            BuildSkillOnlyChoices(chestRoom);

        if (currentChoices.Count == 0)
        {
            Debug.LogWarning("[SkillSelectionUI] No skill choices — skipping pause.");
            return;
        }

        isOpen = true;
        Time.timeScale = 0f;
        skillCanvas.gameObject.SetActive(true);

        BindChoiceButtons();
    }

    private void BindChoiceButtons()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();

            LevelUpChoice choice = i < currentChoices.Count ? currentChoices[i] : null;
            button.interactable = choice != null;
            button.gameObject.SetActive(choice != null);

            if (button.targetGraphic != null)
                button.targetGraphic.color = GetChoiceColor(choice);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.color = Color.white;
                label.text = choice == null ? string.Empty : GetChoiceLabel(choice);
            }

            if (choice != null)
            {
                LevelUpChoice capturedChoice = choice;
                button.onClick.AddListener(() => OnChoiceClicked(capturedChoice));
            }
        }
    }

    private void OnChoiceClicked(LevelUpChoice choice)
    {
        if (choice != null)
        {
            switch (choice.kind)
            {
                case ChoiceKind.SkillUpgrade:
                    if (choice.skill != null && PlayerSkillHandler.Instance != null)
                        PlayerSkillHandler.Instance.ApplySkill(choice.skill);
                    break;
                case ChoiceKind.WeaponPickup:
                    if (WeaponManager.Instance != null)
                        WeaponManager.Instance.AddOrUpgradeWeapon(choice.weaponType);
                    break;
                case ChoiceKind.PassiveItem:
                    if (PassiveItemManager.Instance != null && choice.passiveItem != null)
                        PassiveItemManager.Instance.ApplyPassive(choice.passiveItem);
                    break;
            }
        }

        Hide();
    }

    private void EnsureButtonArray()
    {
        if (skillButtons == null || skillButtons.Length != 3)
            skillButtons = new Button[3];

        // Build any missing buttons at runtime so the panel works without Inspector wiring.
        if (skillCanvas == null)
            return;

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] != null)
                continue;

            skillButtons[i] = BuildRuntimeButton(i);
        }
    }

    private Button BuildRuntimeButton(int index)
    {
        GameObject buttonGO = new GameObject("SkillButton_" + index, typeof(RectTransform));
        buttonGO.transform.SetParent(skillCanvas.transform, false);

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        float panelW = 300f;
        float panelH = 120f;
        float startX = -(panelW + 20f);
        rt.anchoredPosition = new Vector2(startX + index * (panelW + 20f), 0f);
        rt.sizeDelta = new Vector2(panelW, panelH);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        UnityEngine.UI.Image bg = buttonGO.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.97f);

        Button btn = buttonGO.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(8f, 8f);
        textRT.offsetMax = new Vector2(-8f, -8f);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 14f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    private void BuildSkillOnlyChoices(RoomType chestRoom)
    {
        currentChoices.Clear();
        for (int i = 0; i < 3; i++)
        {
            SkillData pick = PickWeightedSkill(chestRoom);
            if (pick == null)
                continue;
            currentChoices.Add(new LevelUpChoice
            {
                kind = ChoiceKind.SkillUpgrade,
                skill = pick
            });
        }

        // No SkillData assets found (Resources/SkillData/ folder empty or missing).
        // Fall back to weapon + passive choices so the chest panel always opens.
        if (currentChoices.Count == 0)
            BuildChoices();
    }

    private SkillData PickWeightedSkill(RoomType chestRoom)
    {
        if (allSkills == null || allSkills.Count == 0)
            return null;

        SkillRarity target = RollRarity(chestRoom);
        List<SkillData> pool = allSkills.FindAll(s => s != null && s.rarity == target);
        if (pool.Count == 0)
            pool = allSkills.FindAll(s => s != null);
        if (pool.Count == 0)
            return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private static SkillRarity RollRarity(RoomType chestRoom)
    {
        float roll = Random.value;
        switch (chestRoom)
        {
            case RoomType.Treasure:
                if (roll < 0.3f) return SkillRarity.Rare;
                if (roll < 0.85f) return SkillRarity.Epic;
                return SkillRarity.Legendary;
            case RoomType.Elite:
                if (roll < 0.1f) return SkillRarity.Common;
                if (roll < 0.6f) return SkillRarity.Rare;
                if (roll < 0.9f) return SkillRarity.Epic;
                return SkillRarity.Legendary;
            default:
                if (roll < 0.5f) return SkillRarity.Common;
                if (roll < 0.8f) return SkillRarity.Rare;
                if (roll < 0.95f) return SkillRarity.Epic;
                return SkillRarity.Legendary;
        }
    }

    private void BuildChoices()
    {
        currentChoices.Clear();

        // 1) Existing skill upgrades
        List<SkillData> skillPool = new List<SkillData>();
        if (allSkills != null)
        {
            for (int i = 0; i < allSkills.Count; i++)
            {
                if (allSkills[i] != null)
                    skillPool.Add(allSkills[i]);
            }
        }

        if (skillPool.Count > 0)
        {
            int randomIndex = Random.Range(0, skillPool.Count);
            currentChoices.Add(new LevelUpChoice
            {
                kind = ChoiceKind.SkillUpgrade,
                skill = skillPool[randomIndex]
            });
        }

        // 2) Weapon options
        if (WeaponManager.Instance != null)
        {
            List<WeaponType> weaponChoices = WeaponManager.Instance.GetRandomWeaponChoices(2);
            for (int i = 0; i < weaponChoices.Count && currentChoices.Count < 3; i++)
            {
                currentChoices.Add(new LevelUpChoice
                {
                    kind = ChoiceKind.WeaponPickup,
                    weaponType = weaponChoices[i]
                });
            }
        }

        // 3) Passive item option
        if (PassiveItemManager.Instance != null && currentChoices.Count < 3)
        {
            List<PassiveItem> passives = PassiveItemManager.Instance.GetRandomCandidates(1);
            if (passives.Count > 0)
            {
                currentChoices.Add(new LevelUpChoice
                {
                    kind = ChoiceKind.PassiveItem,
                    passiveItem = passives[0]
                });
            }
        }

        // Fill remaining slots with skill upgrades if needed
        while (currentChoices.Count < 3 && skillPool.Count > 0)
        {
            int randomIndex = Random.Range(0, skillPool.Count);
            currentChoices.Add(new LevelUpChoice
            {
                kind = ChoiceKind.SkillUpgrade,
                skill = skillPool[randomIndex]
            });
            skillPool.RemoveAt(randomIndex);
        }
    }

    private static string GetChoiceLabel(LevelUpChoice choice)
    {
        if (choice == null)
            return "Empty";

        switch (choice.kind)
        {
            case ChoiceKind.WeaponPickup:
                return $"WEAPON\n{choice.weaponType}\nAuto-fire upgrade";
            case ChoiceKind.PassiveItem:
                if (choice.passiveItem == null)
                    return "PASSIVE\nUnknown";
                return $"PASSIVE\n{choice.passiveItem.itemName}\n{choice.passiveItem.description}";
            default:
                if (choice.skill == null)
                    return "SKILL\nUnknown";
                return $"SKILL\n{choice.skill.skillName}\n{choice.skill.description}\n{choice.skill.rarity}";
        }
    }

    private static Color GetChoiceColor(LevelUpChoice choice)
    {
        if (choice == null)
            return Color.white;

        switch (choice.kind)
        {
            case ChoiceKind.WeaponPickup:
                return new Color(1f, 0.95f, 0.35f, 1f);
            case ChoiceKind.PassiveItem:
                return new Color(0.35f, 1f, 0.55f, 1f);
            default:
                return Color.white;
        }
    }
}
