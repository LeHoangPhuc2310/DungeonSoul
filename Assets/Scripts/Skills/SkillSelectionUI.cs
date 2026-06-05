using System.Collections;
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
    private readonly HashSet<SkillType> rolledSkillTypes = new HashSet<SkillType>();
    private readonly SkillCardRefs[] cardRefs = new SkillCardRefs[3];

    private bool isOpen;
    public bool IsPanelOpen => isOpen;
    private bool rerollUsed;
    private int rerollsRemaining = 1;
    private bool openedFromChestReward;
    private HealthSystem pausedPlayerHealth;
    private Coroutine postCloseInvulnRoutine;
    private Image overlayBackdrop;
    private TMP_Text headerTitle;
    private TMP_Text headerHint;
    private Button rerollButton;

    private const float CardWidth = 200f;
    private const float CardHeight = 280f;
    private const float CardGap = 20f;

    private class SkillCardRefs
    {
        public Image background;
        public Image border;
        public Image icon;
        public TMP_Text title;
        public TMP_Text description;
        public TMP_Text rarity;
        public TMP_Text stack;
    }

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

        BeginPostCloseInvulnerability();
        HUDManager.Resolve()?.RefreshPlayerHealthReference();
    }

    public void Show()
    {
        openedFromChestReward = false;
        OpenPanel(useFullChoices: true, RoomType.Normal);
    }

    public void ShowChest(RoomType chestRoom = RoomType.Normal)
    {
        openedFromChestReward = true;
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
            canvasGO.AddComponent<CanvasScaler>();
        }

        ConfigureSkillCanvas();

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
        PauseCombatForSkillPick();
        Time.timeScale = 0f;
        ConfigureSkillCanvas();
        skillCanvas.gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        InitRerolls();
        EnsureHeaderUI();
        BindChoiceButtons();
        ApplySkillButtonLayout();
        RefreshRerollButton();
        ApplyTypographyToAllCards();

        if (isActiveAndEnabled)
            StartCoroutine(AnimateCardsIn());
    }

    private void ApplyTypographyToAllCards()
    {
        GameUIFont.Apply(headerTitle, GameUIFont.Role.HeaderTitle);
        GameUIFont.Apply(headerHint, GameUIFont.Role.HeaderHint);

        if (rerollButton != null)
        {
            TMP_Text rerollLabel = rerollButton.GetComponentInChildren<TMP_Text>(true);
            if (rerollLabel != null)
                GameUIFont.Apply(rerollLabel, GameUIFont.Role.Button);
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] == null)
                continue;

            SkillCardRefs refs = EnsureCardRefs(skillButtons[i], i);
            StyleCardTypography(refs);
        }
    }

    private static void StyleCardTypography(SkillCardRefs refs)
    {
        if (refs == null)
            return;

        GameUIFont.Apply(refs.rarity, GameUIFont.Role.CardRarity);
        GameUIFont.Apply(refs.title, GameUIFont.Role.CardTitle);
        GameUIFont.Apply(refs.description, GameUIFont.Role.CardBody);
        GameUIFont.Apply(refs.stack, GameUIFont.Role.CardStack);
    }

    private IEnumerator AnimateCardsIn()
    {
        const float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(0.88f, 1f, t);

            for (int i = 0; i < skillButtons.Length; i++)
            {
                Button button = skillButtons[i];
                if (button == null || !button.gameObject.activeSelf)
                    continue;

                button.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] != null)
                skillButtons[i].transform.localScale = Vector3.one;
        }
    }

    private void BindChoiceButtons()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            SkillCardRefs refs = EnsureCardRefs(button, i);

            LevelUpChoice choice = i < currentChoices.Count ? currentChoices[i] : null;
            button.interactable = choice != null;
            button.gameObject.SetActive(choice != null);
            ApplyChoiceToCard(refs, choice);

            if (choice != null)
            {
                LevelUpChoice capturedChoice = choice;
                button.onClick.AddListener(() => OnChoiceClicked(capturedChoice));
            }
        }
    }

    private void OnChoiceClicked(LevelUpChoice choice)
    {
        if (choice == null)
            return;

        AudioManager.PlayUiTap();

        switch (choice.kind)
        {
            case ChoiceKind.SkillUpgrade:
                if (choice.skill != null && PlayerSkillHandler.Instance != null)
                {
                    PlayerSkillHandler.Instance.ApplySkill(choice.skill);
                    Debug.Log("[Skill] Đã chọn: " + choice.skill.skillName);
                }
                break;
            case ChoiceKind.WeaponPickup:
                if (WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.AddOrUpgradeWeapon(choice.weaponType);
                    Debug.Log("[Weapon] Đã nhận: " + choice.weaponType);
                }
                break;
            case ChoiceKind.PassiveItem:
                if (PassiveItemManager.Instance != null && choice.passiveItem != null)
                {
                    PassiveItemManager.Instance.ApplyPassive(choice.passiveItem);
                    Debug.Log("[Passive] Đã nhận: " + choice.passiveItem.itemName);
                }
                break;
        }

        bool fromChest = openedFromChestReward;
        Hide();
        if (fromChest)
            CompleteChestReward();
    }

    private void CompleteChestReward()
    {
        openedFromChestReward = false;

        if (!IsPlayerAlive())
            return;

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null && spawner.CurrentWave >= 10)
        {
            RunManager run = RunManager.Instance;
            if (run != null)
                run.EndRun(true);
            else
                HUDManager.Resolve()?.ShowRunResult(true, 0, 0);

            HUDManager.Resolve()?.ShowWaveAnnouncement("CHIẾN THẮNG — Hoàn thành 10 tầng!");
            return;
        }

        if (spawner != null)
            spawner.BeginNextWave();

        HUDManager hud = HUDManager.Resolve();
        if (hud != null)
        {
            int wave = spawner != null ? spawner.CurrentWave : 0;
            hud.ShowWaveAnnouncement("Đợt " + wave + " — tiêu diệt quái!");
        }
    }

    private static bool IsPlayerAlive()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return false;

        HealthSystem hp = player.GetComponent<HealthSystem>();
        return hp != null && hp.CurrentHP > 0f;
    }

    private void PauseCombatForSkillPick()
    {
        if (postCloseInvulnRoutine != null)
        {
            StopCoroutine(postCloseInvulnRoutine);
            postCloseInvulnRoutine = null;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            pausedPlayerHealth = null;
            return;
        }

        pausedPlayerHealth = player.GetComponent<HealthSystem>();
        pausedPlayerHealth?.SetInvulnerable(true);
    }

    private void BeginPostCloseInvulnerability()
    {
        if (!isActiveAndEnabled)
            return;

        if (postCloseInvulnRoutine != null)
            StopCoroutine(postCloseInvulnRoutine);

        postCloseInvulnRoutine = StartCoroutine(PostCloseInvulnerabilityRoutine());
    }

    private IEnumerator PostCloseInvulnerabilityRoutine()
    {
        if (pausedPlayerHealth != null)
            pausedPlayerHealth.SetInvulnerable(true);

        yield return new WaitForSecondsRealtime(1.25f);

        if (pausedPlayerHealth != null)
            pausedPlayerHealth.SetInvulnerable(false);

        pausedPlayerHealth = null;
        postCloseInvulnRoutine = null;
    }

    private void InitRerolls()
    {
        rerollUsed = false;
        rerollsRemaining = 1;
        if (MetaRunModifiers.Instance != null)
            rerollsRemaining += MetaRunModifiers.Instance.ExtraForgeRerolls;
    }

    private void OnRerollClicked()
    {
        if (rerollUsed || rerollsRemaining <= 0)
            return;

        rerollsRemaining--;
        if (rerollsRemaining <= 0)
            rerollUsed = true;
        AudioManager.PlayUiTap();
        BuildSkillOnlyChoices(RoomType.Normal);
        BindChoiceButtons();
        ApplySkillButtonLayout();
        RefreshRerollButton();
        if (isActiveAndEnabled)
            StartCoroutine(AnimateCardsIn());
    }

    private void ConfigureSkillCanvas()
    {
        if (skillCanvas == null)
            return;

        skillCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        skillCanvas.sortingOrder = 250;
        skillCanvas.enabled = true;
        skillCanvas.worldCamera = null;

        if (skillCanvas.GetComponent<GraphicRaycaster>() == null)
            skillCanvas.gameObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = skillCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = skillCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.4f;

        RectTransform root = skillCanvas.transform as RectTransform;
        if (root != null)
        {
            root.localScale = Vector3.one;
            root.localPosition = Vector3.zero;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        EnsureOverlayBackdrop();
        EnsureHeaderUI();
        EnsureRerollButton();
    }

    private void EnsureHeaderUI()
    {
        if (skillCanvas == null)
            return;

        if (headerTitle == null)
        {
            Transform existing = skillCanvas.transform.Find("HeaderTitle");
            if (existing != null)
                headerTitle = existing.GetComponent<TMP_Text>();
        }

        if (headerTitle == null)
        {
            GameObject titleGO = new GameObject("HeaderTitle", typeof(RectTransform));
            titleGO.transform.SetParent(skillCanvas.transform, false);
            RectTransform rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.72f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720f, 56f);
            rt.anchoredPosition = Vector2.zero;
            headerTitle = titleGO.AddComponent<TextMeshProUGUI>();
            headerTitle.raycastTarget = false;
        }

        headerTitle.text = "CHỌN KỸ NĂNG";
        GameUIFont.Apply(headerTitle, GameUIFont.Role.HeaderTitle);

        if (headerHint == null)
        {
            Transform existing = skillCanvas.transform.Find("HeaderHint");
            if (existing != null)
                headerHint = existing.GetComponent<TMP_Text>();
        }

        if (headerHint == null)
        {
            GameObject hintGO = new GameObject("HeaderHint", typeof(RectTransform));
            hintGO.transform.SetParent(skillCanvas.transform, false);
            RectTransform rt = hintGO.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.66f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720f, 32f);
            rt.anchoredPosition = Vector2.zero;
            headerHint = hintGO.AddComponent<TextMeshProUGUI>();
            headerHint.raycastTarget = false;
        }

        headerHint.text = "Chạm thẻ để nhận skill — mỗi rương đổi lại 1 lần";
        GameUIFont.Apply(headerHint, GameUIFont.Role.HeaderHint);
    }

    private void EnsureRerollButton()
    {
        if (skillCanvas == null || rerollButton != null)
            return;

        Transform existing = skillCanvas.transform.Find("RerollButton");
        if (existing != null)
        {
            rerollButton = existing.GetComponent<Button>();
            if (rerollButton != null)
            {
                rerollButton.onClick.RemoveAllListeners();
                rerollButton.onClick.AddListener(OnRerollClicked);
            }
            return;
        }

        GameObject go = new GameObject("RerollButton", typeof(RectTransform));
        go.transform.SetParent(skillCanvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.28f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(260f, 48f);
        rt.anchoredPosition = Vector2.zero;

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.22f, 0.35f, 0.95f);
        rerollButton = go.AddComponent<Button>();
        rerollButton.targetGraphic = bg;
        rerollButton.onClick.AddListener(OnRerollClicked);

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TMP_Text tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "ĐỔI LẠI (1 lần)";
        GameUIFont.Apply(tmp, GameUIFont.Role.Button);
    }

    private void RefreshRerollButton()
    {
        if (rerollButton == null)
            return;

        rerollButton.interactable = !rerollUsed && rerollsRemaining > 0;
        TMP_Text label = rerollButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = rerollUsed
                ? "ĐÃ ĐỔI LẠI"
                : "ĐỔI LẠI (" + Mathf.Max(0, rerollsRemaining) + ")";
    }

    private void EnsureOverlayBackdrop()
    {
        if (overlayBackdrop != null)
            return;

        Transform existing = skillCanvas.transform.Find("Overlay");
        if (existing != null)
        {
            overlayBackdrop = existing.GetComponent<Image>();
            return;
        }

        GameObject overlayGO = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(skillCanvas.transform, false);
        overlayGO.transform.SetAsFirstSibling();
        RectTransform rt = overlayGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        overlayBackdrop = overlayGO.GetComponent<Image>();
        overlayBackdrop.color = new Color(0f, 0f, 0f, 0.65f);
        overlayBackdrop.raycastTarget = true;
    }

    private void ApplySkillButtonLayout()
    {
        float totalWidth = CardWidth * 3f + CardGap * 2f;
        float startX = -totalWidth * 0.5f + CardWidth * 0.5f;

        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;

            RectTransform rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.42f);
            rt.anchorMax = new Vector2(0.5f, 0.42f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CardWidth, CardHeight);
            rt.anchoredPosition = new Vector2(startX + i * (CardWidth + CardGap), 0f);
            rt.localScale = Vector3.one;

            SkillCardRefs refs = EnsureCardRefs(button, i);
            StyleCardTypography(refs);
        }
    }

    private SkillCardRefs EnsureCardRefs(Button button, int index)
    {
        if (index >= 0 && index < cardRefs.Length && cardRefs[index] != null
            && cardRefs[index].title != null)
            return cardRefs[index];

        SkillCardRefs refs = new SkillCardRefs();
        Transform root = button.transform;

        refs.background = button.GetComponent<Image>();
        if (refs.background != null)
            refs.background.color = new Color(0.1f, 0.11f, 0.16f, 0.98f);

        refs.border = GetOrCreateChildImage(root, "Border");
        RectTransform borderRt = refs.border.rectTransform;
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-3f, -3f);
        borderRt.offsetMax = new Vector2(3f, 3f);
        refs.border.raycastTarget = false;

        refs.rarity = GetOrCreateChildText(root, "Rarity");
        RectTransform rarityRt = refs.rarity.rectTransform;
        rarityRt.anchorMin = new Vector2(0f, 1f);
        rarityRt.anchorMax = new Vector2(1f, 1f);
        rarityRt.pivot = new Vector2(0.5f, 1f);
        rarityRt.anchoredPosition = new Vector2(0f, -8f);
        rarityRt.sizeDelta = new Vector2(-12f, 28f);
        refs.title = GetOrCreateChildText(root, "Title");
        RectTransform titleRt = refs.title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(-16f, 52f);

        // Icon ở giữa thẻ, dưới tiêu đề.
        refs.icon = GetOrCreateChildImage(root, "Icon");
        RectTransform iconRt = refs.icon.rectTransform;
        iconRt.anchorMin = new Vector2(0.5f, 1f);
        iconRt.anchorMax = new Vector2(0.5f, 1f);
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.anchoredPosition = new Vector2(0f, -96f);
        iconRt.sizeDelta = new Vector2(72f, 72f);
        refs.icon.preserveAspect = true;
        refs.icon.raycastTarget = false;
        refs.icon.transform.SetAsLastSibling();

        refs.description = GetOrCreateChildText(root, "Description");
        RectTransform descRt = refs.description.rectTransform;
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.offsetMin = new Vector2(12f, 36f);
        descRt.offsetMax = new Vector2(-12f, -176f);
        refs.stack = GetOrCreateChildText(root, "Stack");
        RectTransform stackRt = refs.stack.rectTransform;
        stackRt.anchorMin = new Vector2(0f, 0f);
        stackRt.anchorMax = new Vector2(1f, 0f);
        stackRt.pivot = new Vector2(0.5f, 0f);
        stackRt.anchoredPosition = new Vector2(0f, 10f);
        stackRt.sizeDelta = new Vector2(-12f, 24f);
        Transform legacyLabel = root.Find("Label");
        if (legacyLabel != null)
            legacyLabel.gameObject.SetActive(false);

        if (index >= 0 && index < cardRefs.Length)
            cardRefs[index] = refs;

        StyleCardTypography(refs);
        return refs;
    }

    private static Image GetOrCreateChildImage(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            child = go.transform;
            go.transform.SetAsFirstSibling();
        }

        Image img = child.GetComponent<Image>();
        if (img == null)
            img = child.gameObject.AddComponent<Image>();
        return img;
    }

    private static TMP_Text GetOrCreateChildText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            child = go.transform;
        }

        TMP_Text tmp = child.GetComponent<TMP_Text>();
        if (tmp == null)
            tmp = child.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.color = Color.white;
        return tmp;
    }

    private void ApplyChoiceToCard(SkillCardRefs refs, LevelUpChoice choice)
    {
        if (refs == null)
            return;

        if (choice == null)
        {
            if (refs.title != null) refs.title.text = string.Empty;
            if (refs.description != null) refs.description.text = string.Empty;
            if (refs.rarity != null) refs.rarity.text = string.Empty;
            if (refs.stack != null) refs.stack.text = string.Empty;
            if (refs.icon != null) refs.icon.enabled = false;
            return;
        }

        Color accent = GetChoiceAccentColor(choice);
        if (refs.border != null)
            refs.border.color = accent;
        if (refs.background != null)
            refs.background.color = Color.Lerp(accent, new Color(0.08f, 0.09f, 0.14f, 1f), 0.82f);

        switch (choice.kind)
        {
            case ChoiceKind.WeaponPickup:
                if (refs.title != null) refs.title.text = WeaponDisplayName(choice.weaponType);
                if (refs.description != null)
                    refs.description.text = "Vũ khí — tự động trang bị và nâng cấp khi nhặt trùng.";
                if (refs.rarity != null) refs.rarity.text = "VŨ KHÍ";
                if (refs.stack != null) refs.stack.text = string.Empty;
                SetIcon(refs, GameIconLibrary.WeaponSprite(choice.weaponType), GameIconLibrary.WeaponTint(choice.weaponType));
                break;
            case ChoiceKind.PassiveItem:
                if (refs.title != null)
                    refs.title.text = choice.passiveItem != null ? choice.passiveItem.itemName : "Trang bị";
                if (refs.description != null)
                    refs.description.text = choice.passiveItem != null ? choice.passiveItem.description : string.Empty;
                if (refs.rarity != null) refs.rarity.text = "TRANG BỊ";
                if (refs.stack != null) refs.stack.text = string.Empty;
                if (choice.passiveItem != null)
                    SetIcon(refs, GameIconLibrary.PassiveSprite(choice.passiveItem), GameIconLibrary.PassiveTint(choice.passiveItem.itemType));
                break;
            default:
                SkillData skill = choice.skill;
                if (skill == null)
                    break;
                if (refs.title != null) refs.title.text = skill.skillName;
                if (refs.description != null) refs.description.text = skill.description;
                if (refs.rarity != null) refs.rarity.text = GetRarityLabel(skill.rarity);
                if (refs.stack != null)
                {
                    int stack = PlayerSkillHandler.Instance != null
                        ? PlayerSkillHandler.Instance.GetStack(skill.skillType)
                        : 0;
                    refs.stack.text = stack > 0 ? "Stack hiện tại: " + stack + " → " + (stack + 1) : "Stack mới: 1";
                }
                SetIcon(refs, GameIconLibrary.SkillSprite(skill.skillType), GameIconLibrary.SkillTint(skill.skillType));
                break;
        }

        StyleCardTypography(refs);
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
        float totalWidth = CardWidth * 3f + CardGap * 2f;
        float startX = -totalWidth * 0.5f + CardWidth * 0.5f;
        rt.anchorMin = new Vector2(0.5f, 0.42f);
        rt.anchorMax = new Vector2(0.5f, 0.42f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CardWidth, CardHeight);
        rt.anchoredPosition = new Vector2(startX + index * (CardWidth + CardGap), 0f);

        Image bg = buttonGO.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.11f, 0.16f, 0.98f);

        Button btn = buttonGO.AddComponent<Button>();
        btn.targetGraphic = bg;
        EnsureCardRefs(btn, index);

        return btn;
    }

    private void BuildSkillOnlyChoices(RoomType chestRoom)
    {
        currentChoices.Clear();
        rolledSkillTypes.Clear();
        for (int i = 0; i < 3; i++)
        {
            SkillData pick = PickWeightedSkillUnique(chestRoom);
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

    private SkillData PickWeightedSkillUnique(RoomType chestRoom)
    {
        for (int attempt = 0; attempt < 16; attempt++)
        {
            SkillData pick = PickWeightedSkill(chestRoom);
            if (pick == null)
                continue;
            if (!rolledSkillTypes.Contains(pick.skillType))
            {
                rolledSkillTypes.Add(pick.skillType);
                return pick;
            }
        }

        return PickWeightedSkill(chestRoom);
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
        if (MetaRunModifiers.Instance != null)
            roll -= MetaRunModifiers.Instance.SkillRarityBonus * 0.08f;
        roll = Mathf.Clamp01(roll);
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

    private static string GetRarityLabel(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Rare: return "HIẾM";
            case SkillRarity.Epic: return "SỬ THI";
            case SkillRarity.Legendary: return "HUYỀN THOẠI";
            default: return "THƯỜNG";
        }
    }

    private static Color GetRarityColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Rare: return new Color(0.2f, 0.6f, 0.95f, 1f);
            case SkillRarity.Epic: return new Color(0.55f, 0.28f, 0.85f, 1f);
            case SkillRarity.Legendary: return new Color(0.83f, 0.67f, 0.12f, 1f);
            default: return new Color(0.55f, 0.55f, 0.58f, 1f);
        }
    }

    private static Color GetChoiceAccentColor(LevelUpChoice choice)
    {
        if (choice == null)
            return Color.white;

        switch (choice.kind)
        {
            case ChoiceKind.WeaponPickup:
                return new Color(1f, 0.78f, 0.22f, 1f);
            case ChoiceKind.PassiveItem:
                return new Color(0.28f, 0.9f, 0.45f, 1f);
            default:
                return choice.skill != null
                    ? GetRarityColor(choice.skill.rarity)
                    : Color.white;
        }
    }

    private static void SetIcon(SkillCardRefs refs, Sprite sprite, Color tint)
    {
        if (refs == null || refs.icon == null)
            return;

        if (sprite != null)
        {
            refs.icon.sprite = sprite;
            refs.icon.color = tint;
            refs.icon.enabled = true;
        }
        else
        {
            refs.icon.enabled = false;
        }
    }

    private static string WeaponDisplayName(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.IronBow: return "Cung Sắt";
            case WeaponType.StormBow: return "Cung Bão";
            case WeaponType.FireStaff: return "Gậy Lửa";
            case WeaponType.DragonStaff: return "Gậy Rồng";
            case WeaponType.FrostWand: return "Đũa Băng";
            case WeaponType.BlizzardWand: return "Đũa Bão Tuyết";
            case WeaponType.PoisonDagger: return "Dao Độc";
            case WeaponType.DeathDagger: return "Dao Tử Thần";
            case WeaponType.HolyCross: return "Thánh Giá";
            case WeaponType.HolyNova: return "Thánh Quang";
            case WeaponType.ThunderRod: return "Trượng Sấm";
            case WeaponType.ZeusRod: return "Trượng Zeus";
            default: return type.ToString();
        }
    }
}
