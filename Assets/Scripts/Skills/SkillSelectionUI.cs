using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSelectionUI : MonoBehaviour
{
    public static SkillSelectionUI Instance { get; private set; }

    public Canvas skillCanvas;
    public Button[] skillButtons = new Button[4];
    public List<SkillData> allSkills = new List<SkillData>();

    private readonly List<SkillSelectionChoice> currentChoices = new List<SkillSelectionChoice>(3);
    private readonly SkillCardRefs[] cardRefs = new SkillCardRefs[4];

    private bool isOpen;
    public bool IsPanelOpen => isOpen;
    private int rerollsUsed;
    private bool openedFromChestReward;
    private SkillSelectionContext currentContext = SkillSelectionContext.LevelUp;
    private RoomType currentChestRoom = RoomType.Normal;
    private int highlightedCardIndex = -1;
    private int pendingConfirmIndex = -1;
    private float panelOpenUnscaledTime;
    private bool acceptingChoices;
    private bool choiceConfirmed;
    private Coroutine unlockInputRoutine;
    private Coroutine closeAfterSelectionRoutine;
    private Coroutine hideAnimatedRoutine;
    private int panelOpenGeneration;
    private CanvasGroup panelInputGroup;

    /// <summary>Chặn click “rơi” vào thẻ ngay khi panel vừa mở (EXP/rương cùng frame).</summary>
    private const float InputUnlockDelay = 0.55f;

    private HealthSystem pausedPlayerHealth;
    private Coroutine postCloseInvulnRoutine;
    private Coroutine animRoutine;
    private Image overlayBackdrop;
    private Image cardsPanelBg;
    private TMP_Text headerTitle;
    private TMP_Text headerHint;
    private Button rerollButton;
    private Button skipButton;
    private Button banishButton;
    private Button confirmButton;
    private Button buyButton;
    private Button lockButton;
    private readonly bool[] pinnedForReroll = new bool[4];
    private int activeChoiceCount = 3;
    private GameObject skipConfirmRoot;
    private bool skipConfirmArmed;

    private SkillSelectionConfig config;

    private const float CardWidth = 240f;
    private const float CardHeight = 320f;
    private const float CardGap = 18f;
    private const float MinTapSize = 80f;

    // Màu badge loại thẻ
    private static readonly Color SkillBadgeColor = new Color(0.204f, 0.596f, 0.859f, 1f);
    private static readonly Color WeaponBadgeColor = new Color(0.906f, 0.298f, 0.235f, 1f);
    private static readonly Color PassiveBadgeColor = new Color(0.180f, 0.800f, 0.443f, 1f);

    private class SkillCardRefs
    {
        public Image background;
        public Image accentBar;
        public Image iconBg;
        public Image border;
        public Image synergyGlow;
        public Image progressFill;
        public Image icon;
        public TMP_Text typeBadge;
        public TMP_Text levelBadge;
        public TMP_Text title;
        public TMP_Text description;
        public TMP_Text statLine;
        public TMP_Text synergyLabel;
        public TMP_Text progressText;
        public TMP_Text rarity;
        public TMP_Text stack;
        public Image lockIcon;
        public Image priceBg;
        public TMP_Text priceLabel;
        public SkillCardInteraction interaction;
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
        // Object có thể bị inactive trong scene → Awake chạy MUỘN, ngay sau khi
        // OpenPanel() đã SetActive(true) và mở panel. Lúc đó tuyệt đối không được
        // gọi Hide() vì sẽ đóng panel vừa mở (log "hide-animated").
        if (!isOpen)
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
        if (!isOpen)
        {
            HideImmediate("hide-request");
            return;
        }

        if (isActiveAndEnabled)
        {
            StopCloseCoroutines();
            hideAnimatedRoutine = StartCoroutine(HideAnimated());
        }
        else
            HideImmediate("hide-request");
    }

    private IEnumerator HideAnimated()
    {
        int generation = panelOpenGeneration;
        yield return AnimatePanelClose();
        if (generation == panelOpenGeneration)
            HideImmediate("hide-animated");
        hideAnimatedRoutine = null;
    }

    private void HideImmediate(string reason = null)
    {
        if (isOpen && !string.IsNullOrEmpty(reason))
            Debug.Log("[SkillSelectionUI] Đóng panel — " + reason);

        isOpen = false;
        acceptingChoices = false;
        choiceConfirmed = false;
        highlightedCardIndex = -1;
        pendingConfirmIndex = -1;
        if (unlockInputRoutine != null)
        {
            StopCoroutine(unlockInputRoutine);
            unlockInputRoutine = null;
        }

        StopCloseCoroutines();
        SetPanelInputBlocked(true);
        DismissSkipConfirm();
        SkillTooltipUI.Instance?.Hide();

        if (skillCanvas != null)
            skillCanvas.gameObject.SetActive(false);

        if (Time.timeScale == 0f)
            Time.timeScale = 1f;

        BeginPostCloseInvulnerability();
        HUDManager.Resolve()?.RefreshPlayerHealthReference();
    }

    /// <summary>Ẩn panel nhưng giữ timeScale=0 (popup swap passive).</summary>
    private void HidePanelVisualOnly(string reason)
    {
        if (isOpen)
            Debug.Log("[SkillSelectionUI] Ẩn panel (giữ pause) — " + reason);

        isOpen = false;
        acceptingChoices = false;
        choiceConfirmed = false;
        highlightedCardIndex = -1;
        pendingConfirmIndex = -1;
        if (unlockInputRoutine != null)
        {
            StopCoroutine(unlockInputRoutine);
            unlockInputRoutine = null;
        }

        StopCloseCoroutines();
        SetPanelInputBlocked(true);
        DismissSkipConfirm();
        SkillTooltipUI.Instance?.Hide();

        if (skillCanvas != null)
            skillCanvas.gameObject.SetActive(false);
    }

    private void DismissSkipConfirm()
    {
        skipConfirmArmed = false;
        if (skipConfirmRoot != null)
            skipConfirmRoot.SetActive(false);
    }

    private void StopCloseCoroutines()
    {
        if (closeAfterSelectionRoutine != null)
        {
            StopCoroutine(closeAfterSelectionRoutine);
            closeAfterSelectionRoutine = null;
        }

        if (hideAnimatedRoutine != null)
        {
            StopCoroutine(hideAnimatedRoutine);
            hideAnimatedRoutine = null;
        }
    }

    private static bool IsAnyPointerHeld()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            TouchPhase phase = Input.GetTouch(i).phase;
            if (phase != TouchPhase.Ended && phase != TouchPhase.Canceled)
                return true;
        }

        return false;
    }

    private void EnsurePanelInputGroup()
    {
        if (panelInputGroup != null || skillCanvas == null)
            return;

        panelInputGroup = skillCanvas.GetComponent<CanvasGroup>();
        if (panelInputGroup == null)
            panelInputGroup = skillCanvas.gameObject.AddComponent<CanvasGroup>();
    }

    private void SetPanelInputBlocked(bool blocked)
    {
        EnsurePanelInputGroup();
        if (panelInputGroup == null)
            return;

        panelInputGroup.interactable = !blocked;
        panelInputGroup.blocksRaycasts = !blocked;
    }

    public void Show()
    {
        openedFromChestReward = false;
        currentContext = SkillSelectionContext.LevelUp;
        currentChestRoom = RoomType.Normal;
        OpenPanel();
    }

    public void ShowChest(RoomType chestRoom = RoomType.Normal)
    {
        openedFromChestReward = true;
        currentChestRoom = chestRoom;
        currentContext = MapChestContext(chestRoom);
        OpenPanel();
    }

    private static SkillSelectionContext MapChestContext(RoomType room)
    {
        switch (room)
        {
            case RoomType.Treasure:
            case RoomType.Boss:
                return SkillSelectionContext.BossChest;
            case RoomType.Elite:
                return SkillSelectionContext.EliteChest;
            default:
                return SkillSelectionContext.NormalChest;
        }
    }

    private void EnsureCanvasReference()
    {
        // Kiến trúc: Controller (script này) phải sống trên GameObject luôn active;
        // Canvas là CHILD được show/hide. Nếu gộp chung một GameObject thì SetActive(false)
        // sẽ tắt cả controller → Awake chạy muộn và phá trạng thái panel.
        Canvas selfCanvas = GetComponent<Canvas>();
        if (selfCanvas != null)
        {
            // Canvas đang nằm chung GameObject với controller (legacy scene wiring).
            // Tách view xuống một child riêng để controller không bao giờ bị tắt theo.
            skillCanvas = SeparateCanvasToChild(selfCanvas);
        }

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

        // Controller GameObject phải luôn bật — chỉ child canvas mới được tắt.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        ConfigureSkillCanvas();

        // Buttons require an EventSystem to receive clicks
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    /// <summary>
    /// Tách Canvas (đang gộp chung GameObject với controller) xuống một child riêng,
    /// rồi neutralize các UI-component còn sót trên self. Unity không cho move component
    /// giữa GameObject lúc runtime, nên ta dựng child canvas mới và reparent toàn bộ con
    /// (Overlay/CardsPanel/SkillButton…) xuống đó. Kết quả: controller ở GameObject cha
    /// luôn active, view nằm trên child có thể show/hide thoải mái.
    /// </summary>
    private Canvas SeparateCanvasToChild(Canvas selfCanvas)
    {
        // Đã tách rồi (child canvas tồn tại) → chỉ cần dọn canvas trên self.
        Canvas childCanvas = GetComponentInChildren<Canvas>(true);
        if (childCanvas != null && childCanvas != selfCanvas)
        {
            NeutralizeSelfCanvas(selfCanvas);
            return childCanvas;
        }

        GameObject viewGO = new GameObject("SkillCanvas");
        viewGO.transform.SetParent(transform, false);
        Canvas view = viewGO.AddComponent<Canvas>();

        RectTransform viewRt = viewGO.GetComponent<RectTransform>();
        if (viewRt != null)
        {
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.offsetMin = viewRt.offsetMax = Vector2.zero;
        }

        // Reparent mọi UI con đang treo trực tiếp dưới controller (legacy layout)
        // xuống child canvas mới. Lặp ngược vì SetParent làm đổi danh sách con.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == viewGO.transform)
                continue;
            child.SetParent(viewGO.transform, false);
        }

        NeutralizeSelfCanvas(selfCanvas);
        return view;
    }

    /// <summary>
    /// Gỡ hẳn Canvas + Raycaster + Scaler khỏi GameObject controller. Phải DESTROY chứ
    /// không chỉ disable: nếu controller còn giữ Canvas component, child canvas sẽ bị coi
    /// là NESTED canvas và kế thừa trạng thái parent → parent disabled thì child không vẽ
    /// (panel mở nhưng không hiện gì).
    /// </summary>
    private void NeutralizeSelfCanvas(Canvas selfCanvas)
    {
        // Thứ tự: Raycaster & Scaler phụ thuộc Canvas → huỷ chúng trước.
        GraphicRaycaster selfRaycaster = GetComponent<GraphicRaycaster>();
        if (selfRaycaster != null)
            Destroy(selfRaycaster);

        CanvasScaler selfScaler = GetComponent<CanvasScaler>();
        if (selfScaler != null)
            Destroy(selfScaler);

        if (selfCanvas != null)
            Destroy(selfCanvas);
    }

    private void OpenPanel()
    {
        EnsureCanvasReference();
        if (skillCanvas == null)
        {
            Debug.LogWarning("[SkillSelectionUI] skillCanvas missing — cannot open panel.");
            return;
        }

        config = SkillSelectionConfig.Get();

        if (allSkills == null || allSkills.Count == 0)
        {
            SkillData[] loaded = Resources.LoadAll<SkillData>("SkillData");
            allSkills = new List<SkillData>(loaded);
        }

        EnsureButtonArray();
        BuildWeightedChoices();

        if (currentChoices.Count == 0)
        {
            Debug.LogWarning("[SkillSelectionUI] Pool rỗng — không mở panel.");
            return;
        }

        Debug.Log("[SkillSelectionUI] Mở panel " + currentContext + " với " + currentChoices.Count + " lựa chọn.");

        panelOpenGeneration++;
        StopCloseCoroutines();

        isOpen = true;
        highlightedCardIndex = -1;
        pendingConfirmIndex = -1;
        rerollsUsed = 0;
        for (int i = 0; i < pinnedForReroll.Length; i++)
            pinnedForReroll[i] = false;
        choiceConfirmed = false;
        acceptingChoices = false;
        panelOpenUnscaledTime = Time.unscaledTime;
        SetPanelInputBlocked(true);
        DismissSkipConfirm();

        PauseCombatForSkillPick();
        Time.timeScale = 0f;
        ConfigureSkillCanvas();
        skillCanvas.gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        ClearStalePointerState();

        ApplyContextVisuals();
        EnsureHeaderUI();
        EnsureCardsPanel();
        EnsureActionButtons();
        BindChoiceButtons();
        ApplySkillButtonLayout();
        SetAllChoiceButtonsInteractable(false);
        RefreshActionButtons();
        ApplyTypographyToAllCards();

        if (unlockInputRoutine != null)
            StopCoroutine(unlockInputRoutine);
        unlockInputRoutine = StartCoroutine(UnlockChoiceInputAfterDelay());

        if (isActiveAndEnabled)
        {
            if (animRoutine != null)
                StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(AnimateCardsSlideIn());
        }
    }

    private static void ClearStalePointerState()
    {
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null)
            return;

        // Bỏ focus UI cũ — tránh Submit/Navigation kích hoạt nhầm thẻ mới
        es.SetSelectedGameObject(null);
    }

    private IEnumerator UnlockChoiceInputAfterDelay()
    {
        int generation = panelOpenGeneration;

        yield return null;
        yield return null;
        float wait = Mathf.Max(0.1f, InputUnlockDelay - (Time.unscaledTime - panelOpenUnscaledTime));
        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        // Chờ thả chuột/chạm cũ — tránh Button.onClick kích hoạt khi mouse up
        while (generation == panelOpenGeneration && isOpen && IsAnyPointerHeld())
            yield return null;

        yield return null;
        yield return null;

        if (generation != panelOpenGeneration || !isOpen)
        {
            unlockInputRoutine = null;
            yield break;
        }

        acceptingChoices = true;
        SetPanelInputBlocked(false);
        SetAllChoiceButtonsInteractable(true);
        RefreshActionButtons();
        if (headerHint != null)
        {
            if (SurvivalRunManager.IsSurvivalMode())
            {
                string luckHint = activeChoiceCount >= 4 ? " · 4 lựa chọn (Luck!)" : string.Empty;
                headerHint.text = "Chọn 1 thẻ  |  Reroll / Skip / Banish" + luckHint;
            }
            else
            {
                headerHint.text = "Chạm thẻ để chọn  |  Giữ lâu: xem chi tiết";
            }
        }
        unlockInputRoutine = null;
    }

    private void SetAllChoiceButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;
            bool hasChoice = i < currentChoices.Count && currentChoices[i] != null;
            button.interactable = interactable && hasChoice;
        }
    }

    private void BuildWeightedChoices(bool rerolling = false)
    {
        int level = ExpSystem.Instance != null ? ExpSystem.Instance.CurrentLevel : 1;
        List<SkillSelectionChoice> previous = rerolling ? new List<SkillSelectionChoice>(currentChoices) : null;

        currentChoices.Clear();
        var usedKeys = new HashSet<string>();

        SkillSelectionChoice savedPurchase = RunManager.Instance?.PeekLockedSkillPurchase();
        if (savedPurchase != null && !rerolling)
        {
            SkillSelectionChoice locked = savedPurchase.Clone();
            locked.note = "locked_purchase";
            currentChoices.Add(locked);
            usedKeys.Add(locked.GetUniqueKey());
        }

        if (rerolling && previous != null)
        {
            for (int i = 0; i < pinnedForReroll.Length && i < previous.Count; i++)
            {
                if (!pinnedForReroll[i] || previous[i] == null)
                    continue;

                string key = previous[i].GetUniqueKey();
                if (usedKeys.Contains(key))
                    continue;

                currentChoices.Add(previous[i].Clone());
                usedKeys.Add(key);
            }
        }

        List<SkillSelectionChoice> pool = SkillSelectionPoolBuilder.Build(currentContext, allSkills, level);
        for (int i = 0; i < pool.Count && currentChoices.Count < skillButtons.Length; i++)
        {
            SkillSelectionChoice choice = pool[i];
            if (choice == null)
                continue;

            string key = choice.GetUniqueKey();
            if (usedKeys.Contains(key))
                continue;

            currentChoices.Add(choice);
            usedKeys.Add(key);
        }

        activeChoiceCount = Mathf.Clamp(currentChoices.Count, 3, skillButtons.Length);
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
        if (refs.rarity != null)
        {
            refs.rarity.fontSize = 12f;
            refs.rarity.characterSpacing = 4f;
        }

        GameUIFont.Apply(refs.title, GameUIFont.Role.CardTitle);
        if (refs.title != null)
        {
            refs.title.fontSize = 18f;
            refs.title.color = new Color(0.98f, 0.96f, 0.9f, 1f);
            refs.title.alignment = TextAlignmentOptions.Center;
        }

        GameUIFont.Apply(refs.description, GameUIFont.Role.CardBody);
        if (refs.description != null)
        {
            refs.description.fontSize = 14f;
            refs.description.color = new Color(0.82f, 0.86f, 0.92f, 1f);
            refs.description.alignment = TextAlignmentOptions.Top;
            refs.description.lineSpacing = 0f;
            refs.description.overflowMode = TextOverflowModes.Ellipsis;
        }

        GameUIFont.Apply(refs.stack, GameUIFont.Role.CardStack);
        if (refs.stack != null)
        {
            refs.stack.fontSize = 12f;
            refs.stack.color = new Color(0.7f, 0.76f, 0.86f, 1f);
            refs.stack.alignment = TextAlignmentOptions.Center;
        }
    }

    private IEnumerator AnimateCardsSlideIn()
    {
        const float duration = 0.28f;
        const float stagger = 0.08f;
        int count = skillButtons.Length;
        Vector2[] targets = new Vector2[count];
        Vector2[] starts = new Vector2[count];
        float[] delays = new float[count];
        float elapsed = 0f;
        float totalDuration = duration + stagger * (count - 1);

        for (int i = 0; i < count; i++)
        {
            Button button = skillButtons[i];
            if (button == null || !button.gameObject.activeSelf)
                continue;

            RectTransform rt = button.GetComponent<RectTransform>();
            targets[i] = rt.anchoredPosition;
            starts[i] = targets[i] + new Vector2(80f, -50f);
            rt.anchoredPosition = starts[i];
            delays[i] = i * stagger;

            CanvasGroup cg = button.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = button.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;
        }

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < count; i++)
            {
                Button button = skillButtons[i];
                if (button == null || !button.gameObject.activeSelf)
                    continue;

                float t = Mathf.Clamp01((elapsed - delays[i]) / duration);
                if (t <= 0f)
                    continue;

                float eased = 1f - Mathf.Pow(1f - t, 3f);
                RectTransform rt = button.GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.Lerp(starts[i], targets[i], eased);
                rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (skillButtons[i] == null)
                continue;
            RectTransform rt = skillButtons[i].GetComponent<RectTransform>();
            rt.anchoredPosition = targets[i];
            rt.localScale = Vector3.one;
            CanvasGroup cg = skillButtons[i].GetComponent<CanvasGroup>();
            if (cg != null)
                cg.alpha = 1f;
        }

        animRoutine = null;
    }

    private IEnumerator AnimateShuffleCards()
    {
        const float duration = 0.18f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float wobble = Mathf.Sin(elapsed * 40f) * (1f - elapsed / duration) * 8f;
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null)
                    continue;
                RectTransform rt = skillButtons[i].GetComponent<RectTransform>();
                rt.localRotation = Quaternion.Euler(0f, 0f, wobble * (i % 2 == 0 ? 1f : -1f));
            }

            yield return null;
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] != null)
                skillButtons[i].transform.localRotation = Quaternion.identity;
        }
    }

    private IEnumerator AnimateSelectCard(int selectedIndex)
    {
        const float duration = 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < skillButtons.Length; i++)
            {
                Button button = skillButtons[i];
                if (button == null || !button.gameObject.activeSelf)
                    continue;

                CanvasGroup cg = button.GetComponent<CanvasGroup>();
                if (cg == null)
                    cg = button.gameObject.AddComponent<CanvasGroup>();

                if (i == selectedIndex)
                    button.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, t);
                else
                    cg.alpha = Mathf.Lerp(1f, 0.2f, t);
            }

            yield return null;
        }
    }

    private IEnumerator AnimatePanelClose()
    {
        const float duration = 0.24f;
        float elapsed = 0f;
        RectTransform panelRt = cardsPanelBg != null ? cardsPanelBg.rectTransform : null;
        Vector2 start = panelRt != null ? panelRt.anchoredPosition : Vector2.zero;
        Vector2 end = start + new Vector2(0f, -120f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (panelRt != null)
                panelRt.anchoredPosition = Vector2.Lerp(start, end, t);
            if (overlayBackdrop != null)
            {
                Color c = overlayBackdrop.color;
                c.a = Mathf.Lerp(0.78f, 0f, t);
                overlayBackdrop.color = c;
            }

            yield return null;
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

            SkillSelectionChoice choice = i < currentChoices.Count ? currentChoices[i] : null;
            button.interactable = choice != null;
            button.gameObject.SetActive(choice != null);
            ApplyChoiceToCard(refs, choice, i);

            // Đảm bảo thẻ luôn hiện — tránh kẹt alpha=0 khi animation bị ngắt
            CanvasGroup cg = button.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = button.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = choice != null ? 1f : 0.35f;
            button.transform.localScale = Vector3.one;

            if (choice != null)
            {
                int capturedIndex = i;
                SkillSelectionChoice capturedChoice = choice;
                button.onClick.AddListener(() => OnCardTapped(capturedIndex, capturedChoice));
            }
        }
    }

    /// <summary>Chạm thẻ = chọn (highlight) — xác nhận / mua / khóa ở nút dưới.</summary>
    private void OnCardTapped(int index, SkillSelectionChoice choice)
    {
        if (choice == null || !acceptingChoices || choiceConfirmed)
            return;

        AudioManager.PlayUiTap();
        pendingConfirmIndex = index;
        HighlightCard(index);
        RefreshActionButtons();
        RefreshLockButtonIcon();
    }

    private void HighlightCard(int index)
    {
        highlightedCardIndex = index;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] == null)
                continue;

            float scale = i == index ? 1.05f : 1f;
            skillButtons[i].transform.localScale = Vector3.one * scale;
            SkillCardRefs refs = i < cardRefs.Length ? cardRefs[i] : null;
            if (refs?.synergyGlow != null)
                refs.synergyGlow.color = new Color(1f, 0.85f, 0.2f, i == index ? 0.55f : 0.25f);

            ApplyCardLockState(i, refs);
        }
    }

    private void ApplyCardLockState(int index, SkillCardRefs refs)
    {
        if (refs?.lockIcon == null)
            return;

        bool isLockedPurchase = index < currentChoices.Count
            && currentChoices[index] != null
            && IsLockedPurchaseChoice(currentChoices[index]);
        bool isPinned = index >= 0 && index < pinnedForReroll.Length && pinnedForReroll[index];
        bool show = isLockedPurchase || isPinned;

        refs.lockIcon.enabled = show;
        if (!show)
            return;

        SkillSelectionChoice choice = index < currentChoices.Count ? currentChoices[index] : null;
        SkillRarity rarity = SkillPurchasePricing.ResolveRarity(choice);
        refs.lockIcon.sprite = isLockedPurchase
            ? UnlockIconLibrary.LockedBadge
            : UnlockIconLibrary.ForRarity(rarity);
        refs.lockIcon.color = Color.white;
    }

    private void RefreshLockButtonIcon()
    {
        if (lockButton == null || pendingConfirmIndex < 0 || pendingConfirmIndex >= currentChoices.Count)
            return;

        SkillSelectionChoice choice = currentChoices[pendingConfirmIndex];
        Transform iconTf = lockButton.transform.Find("Icon");
        Image iconImg = iconTf != null ? iconTf.GetComponent<Image>() : null;
        if (iconImg == null)
            return;

        SkillRarity rarity = SkillPurchasePricing.ResolveRarity(choice);
        iconImg.enabled = true;
        iconImg.sprite = UnlockIconLibrary.ForRarity(rarity);
        iconImg.color = Color.white;
    }

    private void OnConfirmClicked()
    {
        if (!acceptingChoices || choiceConfirmed)
            return;
        if (pendingConfirmIndex < 0 || pendingConfirmIndex >= currentChoices.Count)
            return;

        SkillSelectionChoice choice = currentChoices[pendingConfirmIndex];
        if (IsLockedPurchaseChoice(choice))
            return;

        ConfirmChoice(choice, pendingConfirmIndex);
    }

    private static bool IsLockedPurchaseChoice(SkillSelectionChoice choice)
    {
        return choice != null && choice.note == "locked_purchase";
    }

    private void ConfirmChoice(SkillSelectionChoice choice, int index)
    {
        if (choice == null || choiceConfirmed || !acceptingChoices)
            return;

        choiceConfirmed = true;
        acceptingChoices = false;
        DismissSkipConfirm();
        SetAllChoiceButtonsInteractable(false);
        Debug.Log("[SkillSelectionUI] Đã chọn: " + choice.kind + " (index " + index + ")");

        RunManager.Instance?.ClearLockedSkillPurchase();

        if (isActiveAndEnabled)
            StartCoroutine(AnimateSelectCard(index));

        bool deferClose = ApplyChoiceReward(choice);

        bool fromChest = openedFromChestReward;
        if (!deferClose)
        {
            StopCloseCoroutines();
            closeAfterSelectionRoutine = StartCoroutine(CloseAfterSelection(fromChest));
        }
    }

    /// <returns>true nếu đóng panel được hoãn (vd. popup swap passive).</returns>
    private bool ApplyChoiceReward(SkillSelectionChoice choice)
    {
        switch (choice.kind)
        {
            case SkillSelectionChoiceKind.SkillUpgrade:
                if (choice.skill != null && PlayerSkillHandler.Instance != null)
                {
                    PlayerSkillHandler.Instance.ApplySkill(choice.skill);
                    Debug.Log("[Skill] Đã chọn: " + choice.skill.skillName);
                }
                break;
            case SkillSelectionChoiceKind.WeaponPickup:
                if (WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.AddOrUpgradeWeapon(choice.weaponType);
                    Debug.Log("[Weapon] Đã nhận: " + choice.weaponType);
                }
                break;
            case SkillSelectionChoiceKind.PassiveItem:
                if (PassiveItemManager.Instance != null && choice.passiveItem != null)
                {
                    PassiveSwapUI.PendingChestReward = openedFromChestReward;
                    bool applied = PassiveItemManager.Instance.TryApplyPassive(choice.passiveItem);
                    Debug.Log("[Passive] Đã chọn: " + choice.passiveItem.displayName);
                    if (!applied)
                    {
                        HidePanelVisualOnly("passive-swap");
                        return true;
                    }
                }
                break;
            case SkillSelectionChoiceKind.BonusHp:
            case SkillSelectionChoiceKind.HealFallback:
                HealPlayer(choice.bonusHp > 0f ? choice.bonusHp : config.allMaxedHealHp);
                break;
            case SkillSelectionChoiceKind.BonusCoin:
                RunManager.Instance?.AddRunCoins(Mathf.Max(1, choice.bonusCoins));
                break;
        }

        return false;
    }

    private IEnumerator CloseAfterSelection(bool fromChest)
    {
        int generation = panelOpenGeneration;
        yield return new WaitForSecondsRealtime(0.2f);
        if (generation != panelOpenGeneration || !isOpen)
        {
            closeAfterSelectionRoutine = null;
            yield break;
        }

        yield return AnimatePanelClose();
        if (generation != panelOpenGeneration || !isOpen)
        {
            closeAfterSelectionRoutine = null;
            yield break;
        }

        HideImmediate("selection");
        closeAfterSelectionRoutine = null;
        if (fromChest)
            CompleteChestReward();
    }

    private static void HealPlayer(float amount)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        HealthSystem hp = player != null ? player.GetComponent<HealthSystem>() : null;
        if (hp != null)
            hp.Heal(amount);
    }

    /// <summary>Gọi sau khi swap passive xong (rương wave đang chờ).</summary>
    public void CompleteChestAfterSwap()
    {
        if (!openedFromChestReward)
            openedFromChestReward = true;
        CompleteChestReward();
    }

    private void CompleteChestReward()
    {
        openedFromChestReward = false;

        if (!IsPlayerAlive())
            return;

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (SurvivalRunManager.IsSurvivalMode())
        {
            PassiveItemManager.Instance?.CheckPassiveEvolutionsAtWaveEnd();
            return;
        }

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

        PassiveItemManager.Instance?.CheckPassiveEvolutionsAtWaveEnd();

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

    private void OnRerollClicked()
    {
        if (!acceptingChoices || choiceConfirmed)
            return;

        if (config == null)
            config = SkillSelectionConfig.Get();

        int maxRerolls = config.maxRerollsPerPanel;

        if (rerollsUsed >= maxRerolls)
            return;

        RunManager run = RunManager.Instance;
        if (run == null || !run.TrySpendRunCoins(config.rerollCoinCost))
            return;

        rerollsUsed++;
        AudioManager.PlayUiTap();
        BuildWeightedChoices(rerolling: true);
        pendingConfirmIndex = -1;
        acceptingChoices = false;
        SetPanelInputBlocked(true);
        BindChoiceButtons();
        ApplySkillButtonLayout();
        SetAllChoiceButtonsInteractable(false);
        RefreshActionButtons();
        if (unlockInputRoutine != null)
            StopCoroutine(unlockInputRoutine);
        panelOpenUnscaledTime = Time.unscaledTime;
        unlockInputRoutine = StartCoroutine(UnlockChoiceInputAfterDelay());
        if (isActiveAndEnabled)
            StartCoroutine(AnimateShuffleCards());
    }

    private void OnBanishClicked()
    {
        if (!acceptingChoices || choiceConfirmed)
            return;

        if (config == null)
            config = SkillSelectionConfig.Get();

        if (pendingConfirmIndex < 0 || pendingConfirmIndex >= currentChoices.Count)
            return;

        SkillSelectionChoice choice = currentChoices[pendingConfirmIndex];
        if (choice == null || !CanBanishChoice(choice))
            return;

        string key = choice.GetUniqueKey();
        if (!BanishRegistry.TryBanish(key, config.maxBanishesPerRun))
            return;

        Debug.Log("[SkillSelectionUI] Banish: " + key);
        AudioManager.PlayUiTap();
        choiceConfirmed = true;
        acceptingChoices = false;
        DismissSkipConfirm();
        SetAllChoiceButtonsInteractable(false);

        bool fromChest = openedFromChestReward;
        StopCloseCoroutines();
        closeAfterSelectionRoutine = StartCoroutine(CloseAfterSelection(fromChest));
    }

    private static bool CanBanishChoice(SkillSelectionChoice choice)
    {
        if (choice == null)
            return false;

        switch (choice.kind)
        {
            case SkillSelectionChoiceKind.BonusHp:
            case SkillSelectionChoiceKind.BonusCoin:
            case SkillSelectionChoiceKind.HealFallback:
                return false;
            default:
                return true;
        }
    }

    private void OnSkipClicked()
    {
        if (!acceptingChoices || choiceConfirmed)
            return;

        skipConfirmArmed = true;
        ShowSkipConfirm();
    }

    private void ShowSkipConfirm()
    {
        EnsureSkipConfirmPopup();
        if (skipConfirmRoot != null)
            skipConfirmRoot.SetActive(true);
    }

    private void ConfirmSkip()
    {
        if (!isOpen || !skipConfirmArmed || choiceConfirmed)
            return;

        Debug.Log("[SkillSelectionUI] Bỏ qua phần thưởng (skip confirm).");
        DismissSkipConfirm();

        if (config == null)
            config = SkillSelectionConfig.Get();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        HealthSystem hp = player != null ? player.GetComponent<HealthSystem>() : null;
        if (hp != null)
            hp.Heal(hp.MaxHP * config.skipHealPercent);

        choiceConfirmed = true;
        acceptingChoices = false;
        SetAllChoiceButtonsInteractable(false);

        bool fromChest = openedFromChestReward;
        StopCloseCoroutines();
        closeAfterSelectionRoutine = StartCoroutine(CloseAfterSelection(fromChest));
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
        EnsureCardsPanel();
        EnsureHeaderUI();
        EnsureActionButtons();
    }

    private void ApplyContextVisuals()
    {
        if (overlayBackdrop == null || cardsPanelBg == null)
            return;

        Color backdrop;
        Color panel;
        switch (currentContext)
        {
            case SkillSelectionContext.LevelUp:
                backdrop = new Color(0.05f, 0.12f, 0.28f, 0.82f);
                panel = new Color(0.08f, 0.14f, 0.26f, 0.94f);
                break;
            case SkillSelectionContext.EliteChest:
                backdrop = new Color(0.14f, 0.06f, 0.22f, 0.82f);
                panel = new Color(0.18f, 0.1f, 0.28f, 0.94f);
                break;
            case SkillSelectionContext.BossChest:
                backdrop = new Color(0.22f, 0.16f, 0.04f, 0.85f);
                panel = new Color(0.28f, 0.2f, 0.06f, 0.95f);
                break;
            default:
                backdrop = new Color(0.12f, 0.08f, 0.05f, 0.8f);
                panel = new Color(0.16f, 0.11f, 0.07f, 0.94f);
                break;
        }

        overlayBackdrop.color = backdrop;
        cardsPanelBg.color = panel;
    }

    private void EnsureCardsPanel()
    {
        if (skillCanvas == null)
            return;

        if (cardsPanelBg == null)
        {
            Transform existing = skillCanvas.transform.Find("CardsPanel");
            if (existing != null)
                cardsPanelBg = existing.GetComponent<Image>();
        }

        if (cardsPanelBg == null)
        {
            GameObject panelGO = new GameObject("CardsPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(skillCanvas.transform, false);
            panelGO.transform.SetSiblingIndex(1);
            cardsPanelBg = panelGO.GetComponent<Image>();
        }

        float totalWidth = CardWidth * 3f + CardGap * 2f;
        RectTransform rt = cardsPanelBg.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.42f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(totalWidth + 56f, CardHeight + 52f);
        rt.anchoredPosition = Vector2.zero;

        if (!GuiArtLibrary.ApplyPanel(cardsPanelBg, GuiArtLibrary.DialogPanel))
            cardsPanelBg.color = new Color(0.06f, 0.07f, 0.1f, 0.94f);

        cardsPanelBg.raycastTarget = false;
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

        int level = ExpSystem.Instance != null ? ExpSystem.Instance.CurrentLevel : 1;
        headerTitle.text = GetContextTitle(level);
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

        headerHint.text = "Đang chuẩn bị lựa chọn...";
        GameUIFont.Apply(headerHint, GameUIFont.Role.HeaderHint);
    }

    private string GetContextTitle(int level)
    {
        switch (currentContext)
        {
            case SkillSelectionContext.LevelUp:
                return SurvivalRunManager.IsSurvivalMode() ? "LEVEL UP!" : "Lên cấp " + level + "!";
            case SkillSelectionContext.EliteChest:
                return "Rương Elite";
            case SkillSelectionContext.BossChest:
                return "Rương Boss — " + GetLastBossName();
            case SkillSelectionContext.NormalChest:
            default:
                return "Rương báu";
        }
    }

    private static string GetLastBossName()
    {
        BossController[] bosses = Object.FindObjectsByType<BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (bosses != null && bosses.Length > 0 && bosses[0].Data != null)
            return bosses[0].Data.bossName;
        return "Boss";
    }

    private void EnsureActionButtons()
    {
        if (skillCanvas == null)
            return;

        rerollButton = EnsureFooterButton("RerollButton", new Vector2(0.08f, 0.22f), OnRerollClicked, ref rerollButton);
        banishButton = EnsureFooterButton("BanishButton", new Vector2(0.5f, 0.22f), OnBanishClicked, ref banishButton);
        skipButton = EnsureFooterButton("SkipButton", new Vector2(0.92f, 0.22f), OnSkipClicked, ref skipButton);
        buyButton = EnsureFooterButton("BuyButton", new Vector2(0.28f, 0.14f), OnBuyClicked, ref buyButton);
        lockButton = EnsureFooterButton("LockButton", new Vector2(0.72f, 0.14f), OnLockClicked, ref lockButton);
        confirmButton = EnsureFooterButton("ConfirmButton", new Vector2(0.5f, 0.08f), OnConfirmClicked, ref confirmButton);
        StyleFooterButtonWithIcon(lockButton, UnlockIconLibrary.LockedBadge);
        StyleFooterButtonWithIcon(buyButton, null);

        EnsureSkipConfirmPopup();
    }

    private static void StyleFooterButtonWithIcon(Button button, Sprite icon)
    {
        if (button == null)
            return;

        Image bg = button.GetComponent<Image>();
        if (bg != null && GuiArtLibrary.ButtonSecondary != null)
        {
            bg.sprite = GuiArtLibrary.ButtonSecondary;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }

        Transform iconTf = button.transform.Find("Icon");
        Image iconImg;
        if (iconTf == null)
        {
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(button.transform, false);
            RectTransform irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(10f, 0f);
            irt.sizeDelta = new Vector2(34f, 34f);
            iconImg = iconGo.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            iconImg = iconTf.GetComponent<Image>();
        }

        if (iconImg != null)
        {
            iconImg.enabled = icon != null;
            if (icon != null)
            {
                iconImg.sprite = icon;
                iconImg.color = Color.white;
            }
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            RectTransform lrt = label.rectTransform;
            lrt.offsetMin = new Vector2(icon != null ? 48f : 8f, 4f);
            lrt.offsetMax = new Vector2(-8f, -4f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private Button EnsureFooterButton(string name, Vector2 anchor, UnityEngine.Events.UnityAction onClick, ref Button cached)
    {
        if (cached != null)
            return cached;

        Transform existing = skillCanvas.transform.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(skillCanvas.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x < 0.5f ? 0f : anchor.x > 0.5f ? 1f : 0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220f, 52f);
            rt.anchoredPosition = Vector2.zero;
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.22f, 0.96f);
            cached = go.AddComponent<Button>();
            cached.targetGraphic = bg;

            GameObject textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            RectTransform trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            TMP_Text tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            GameUIFont.Apply(tmp, GameUIFont.Role.Button);
        }

        cached.onClick.RemoveAllListeners();
        cached.onClick.AddListener(onClick);
        return cached;
    }

    private void EnsureSkipConfirmPopup()
    {
        if (skipConfirmRoot != null || skillCanvas == null)
            return;

        skipConfirmRoot = new GameObject("SkipConfirm", typeof(RectTransform), typeof(Image));
        skipConfirmRoot.transform.SetParent(skillCanvas.transform, false);
        RectTransform rt = skipConfirmRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        skipConfirmRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(skipConfirmRoot.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(420f, 180f);
        panel.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.98f);

        TMP_Text msg = CreatePopupLabel(panel.transform, "Bỏ qua phần thưởng?", 0f);
        Button yes = CreatePopupButton(panel.transform, "Có", -40f, ConfirmSkip);
        Button no = CreatePopupButton(panel.transform, "Không", -90f, () =>
        {
            DismissSkipConfirm();
        });

        skipConfirmRoot.SetActive(false);
    }

    private static TMP_Text CreatePopupLabel(Transform parent, string text, float y)
    {
        GameObject go = new GameObject("Msg", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(380f, 48f);
        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        GameUIFont.Apply(tmp, GameUIFont.Role.HeaderTitle);
        return tmp;
    }

    private static Button CreatePopupButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(180f, 40f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.34f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TMP_Text tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        GameUIFont.Apply(tmp, GameUIFont.Role.Button);
        return btn;
    }

    private void RefreshActionButtons()
    {
        if (config == null)
            config = SkillSelectionConfig.Get();

        int maxRerolls = config.maxRerollsPerPanel;

        int coins = RunManager.Instance != null ? RunManager.Instance.RunCoins : 0;
        bool canReroll = acceptingChoices && !choiceConfirmed
            && rerollsUsed < maxRerolls && coins >= config.rerollCoinCost;

        if (rerollButton != null)
        {
            rerollButton.interactable = canReroll;
            TMP_Text label = rerollButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = canReroll
                    ? "Đổi lại (" + config.rerollCoinCost + " xu)"
                    : "Đổi lại (thiếu xu)";
                label.color = canReroll ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
            }
        }

        if (skipButton != null)
        {
            skipButton.interactable = acceptingChoices && !choiceConfirmed;
            TMP_Text label = skipButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = SurvivalRunManager.IsSurvivalMode() ? "Skip" : "Bỏ qua (+10% HP)";
        }

        if (banishButton != null)
        {
            bool hasBanishable = pendingConfirmIndex >= 0 && pendingConfirmIndex < currentChoices.Count
                && CanBanishChoice(currentChoices[pendingConfirmIndex]);
            int maxBanish = config.maxBanishesPerRun;
            bool banishLeft = maxBanish <= 0 || BanishRegistry.BanishesUsed < maxBanish;
            banishButton.interactable = acceptingChoices && !choiceConfirmed && hasBanishable && banishLeft;
            banishButton.gameObject.SetActive(SurvivalRunManager.IsSurvivalMode());

            TMP_Text label = banishButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (!banishLeft)
                    label.text = "Banish (hết)";
                else if (!hasBanishable)
                    label.text = "Banish";
                else
                    label.text = maxBanish > 0
                        ? "Banish (" + (maxBanish - BanishRegistry.BanishesUsed) + ")"
                        : "Banish";
            }
        }

        RefreshConfirmButton();
        RefreshBuyAndLockButtons();
    }

    private void RefreshBuyAndLockButtons()
    {
        if (config == null)
            config = SkillSelectionConfig.Get();

        int coins = RunManager.Instance != null ? RunManager.Instance.RunCoins : 0;
        SkillSelectionChoice selected = hasSelectionChoice()
            ? currentChoices[pendingConfirmIndex]
            : null;
        int buyCost = selected != null ? SkillPurchasePricing.GetCost(selected) : config.skillPurchaseCoinCost;
        bool purchasable = selected != null && SkillPurchasePricing.IsPurchasable(selected);
        bool hasSelection = hasSelectionChoice();
        bool canBuy = hasSelection && purchasable && buyCost > 0 && coins >= buyCost;
        bool canLockPurchase = hasSelection && purchasable && buyCost > 0 && !canBuy;
        bool canPinReroll = hasSelection && config.maxPinnedCardsOnReroll > 0;

        if (buyButton != null)
        {
            buyButton.interactable = canBuy;
            TMP_Text label = buyButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (!purchasable || buyCost <= 0)
                    label.text = "Miễn phí";
                else if (canBuy)
                    label.text = "Mua · " + buyCost + " xu";
                else
                    label.text = "Mua · " + buyCost + " xu (thiếu)";
                label.color = canBuy ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
            }
        }

        if (lockButton != null)
        {
            bool pinned = hasSelection && pinnedForReroll[pendingConfirmIndex];
            lockButton.interactable = canLockPurchase || canPinReroll;
            TMP_Text label = lockButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                if (canLockPurchase)
                    label.text = "Khóa · " + buyCost + " xu";
                else if (pinned)
                    label.text = "Đã ghim thẻ";
                else
                    label.text = "Ghim thẻ";
                label.color = lockButton.interactable ? Color.white : new Color(0.55f, 0.55f, 0.58f, 1f);
            }
        }
    }

    private bool hasSelectionChoice()
    {
        return acceptingChoices && !choiceConfirmed && pendingConfirmIndex >= 0
            && pendingConfirmIndex < currentChoices.Count && currentChoices[pendingConfirmIndex] != null;
    }

    private void OnBuyClicked()
    {
        if (!acceptingChoices || choiceConfirmed || pendingConfirmIndex < 0
            || pendingConfirmIndex >= currentChoices.Count)
            return;

        if (config == null)
            config = SkillSelectionConfig.Get();

        SkillSelectionChoice choice = currentChoices[pendingConfirmIndex];
        int buyCost = SkillPurchasePricing.GetCost(choice);
        if (buyCost <= 0)
            return;

        RunManager run = RunManager.Instance;
        if (run == null || !run.TrySpendRunCoins(buyCost))
            return;
        run.ClearLockedSkillPurchase();
        AudioManager.PlayUiTap();
        ConfirmChoice(choice, pendingConfirmIndex);
    }

    private void OnLockClicked()
    {
        if (!acceptingChoices || choiceConfirmed || pendingConfirmIndex < 0
            || pendingConfirmIndex >= currentChoices.Count)
            return;

        if (config == null)
            config = SkillSelectionConfig.Get();

        SkillSelectionChoice choice = currentChoices[pendingConfirmIndex];
        if (choice == null)
            return;

        int buyCost = SkillPurchasePricing.GetCost(choice);
        int coins = RunManager.Instance != null ? RunManager.Instance.RunCoins : 0;
        bool canBuy = buyCost > 0 && coins >= buyCost;

        if (!canBuy)
        {
            choice.purchaseCoinCost = buyCost;
            RunManager.Instance?.SetLockedSkillPurchase(choice);
            AudioManager.PlayUiTap();
            Debug.Log("[SkillSelectionUI] Đã khóa thẻ để mua sau: " + choice.GetUniqueKey());
            choiceConfirmed = true;
            acceptingChoices = false;
            SetAllChoiceButtonsInteractable(false);
            StopCloseCoroutines();
            closeAfterSelectionRoutine = StartCoroutine(CloseAfterSelection(openedFromChestReward));
            return;
        }

        int pinnedCount = 0;
        for (int i = 0; i < pinnedForReroll.Length; i++)
            if (pinnedForReroll[i])
                pinnedCount++;

        bool alreadyPinned = pinnedForReroll[pendingConfirmIndex];
        if (!alreadyPinned && pinnedCount >= config.maxPinnedCardsOnReroll)
            return;

        pinnedForReroll[pendingConfirmIndex] = !alreadyPinned;
        AudioManager.PlayUiTap();
        for (int i = 0; i < skillButtons.Length; i++)
            ApplyCardLockState(i, i < cardRefs.Length ? cardRefs[i] : null);
        RefreshActionButtons();
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton == null)
            return;

        bool hasSelection = pendingConfirmIndex >= 0 && pendingConfirmIndex < currentChoices.Count
            && currentChoices[pendingConfirmIndex] != null;
        bool lockedPurchase = hasSelection && IsLockedPurchaseChoice(currentChoices[pendingConfirmIndex]);

        confirmButton.interactable = acceptingChoices && !choiceConfirmed && hasSelection && !lockedPurchase;
        TMP_Text label = confirmButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            if (!hasSelection)
                label.text = "Chọn thẻ trước";
            else if (lockedPurchase)
                label.text = "Dùng nút Mua";
            else
                label.text = "XÁC NHẬN";
        }
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
        overlayBackdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.78f);
        overlayBackdrop.raycastTarget = true;
    }

    private void ApplySkillButtonLayout()
    {
        int visible = Mathf.Clamp(activeChoiceCount, 3, skillButtons.Length);
        float cardW = visible >= 4 ? 200f : CardWidth;
        float gap = visible >= 4 ? 14f : CardGap;
        float totalWidth = cardW * visible + gap * (visible - 1);
        float startX = -totalWidth * 0.5f + cardW * 0.5f;

        if (cardsPanelBg != null)
        {
            RectTransform panelRt = cardsPanelBg.rectTransform;
            panelRt.sizeDelta = new Vector2(totalWidth + 56f, CardHeight + 52f);
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            Button button = skillButtons[i];
            if (button == null)
                continue;

            bool show = i < visible;
            button.gameObject.SetActive(show);
            if (!show)
                continue;

            RectTransform rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.42f);
            rt.anchorMax = new Vector2(0.5f, 0.42f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(cardW, MinTapSize), Mathf.Max(CardHeight, MinTapSize));
            rt.anchoredPosition = new Vector2(startX + i * (cardW + gap), 0f);
            rt.localScale = Vector3.one;

            SkillCardRefs refs = EnsureCardRefs(button, i);
            StyleCardTypography(refs);
        }
    }

    private SkillCardRefs EnsureCardRefs(Button button, int index)
    {
        // Luôn rebuild nếu thiếu UI mới (scene cũ chỉ có Rarity/Title)
        if (index >= 0 && index < cardRefs.Length && cardRefs[index] != null
            && cardRefs[index].title != null && cardRefs[index].typeBadge != null
            && cardRefs[index].statLine != null && cardRefs[index].levelBadge != null)
            return cardRefs[index];

        SkillCardRefs refs = new SkillCardRefs();
        Transform root = button.transform;

        refs.background = button.GetComponent<Image>();
        if (refs.background != null)
        {
            refs.background.color = new Color(0.94f, 0.94f, 0.97f, 1f);
            refs.background.preserveAspect = false;
        }

        refs.accentBar = GetOrCreateChildImage(root, "AccentBar");
        RectTransform accentRt = refs.accentBar.rectTransform;
        accentRt.anchorMin = new Vector2(0f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.pivot = new Vector2(0.5f, 1f);
        accentRt.anchoredPosition = Vector2.zero;
        accentRt.sizeDelta = new Vector2(0f, 5f);
        refs.accentBar.raycastTarget = false;

        refs.border = GetOrCreateChildImage(root, "Border");
        refs.border.raycastTarget = false;
        // Khung phủ toàn thẻ (hơi loe ra để viền ôm sát mép) — sprite gán ở ApplyChoiceToCard theo độ hiếm.
        RectTransform borderRt = refs.border.rectTransform;
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.offsetMin = new Vector2(-2f, -2f);
        borderRt.offsetMax = new Vector2(2f, 2f);
        refs.border.transform.SetAsLastSibling(); // viền nằm trên cùng để không bị icon/nền che

        refs.synergyGlow = GetOrCreateChildImage(root, "SynergyGlow");
        refs.synergyGlow.raycastTarget = false;
        RectTransform glowRt = refs.synergyGlow.rectTransform;
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.offsetMin = new Vector2(-4f, -4f);
        glowRt.offsetMax = new Vector2(4f, 4f);
        refs.synergyGlow.color = new Color(1f, 0.85f, 0.2f, 0f);

        refs.typeBadge = GetOrCreateChildText(root, "TypeBadge");
        RectTransform typeRt = refs.typeBadge.rectTransform;
        typeRt.anchorMin = new Vector2(0f, 1f);
        typeRt.anchorMax = new Vector2(0f, 1f);
        typeRt.pivot = new Vector2(0f, 1f);
        typeRt.anchoredPosition = new Vector2(10f, -8f);
        typeRt.sizeDelta = new Vector2(88f, 22f);
        refs.typeBadge.fontSize = 11f;
        refs.typeBadge.fontStyle = FontStyles.Bold;

        refs.levelBadge = GetOrCreateChildText(root, "LevelBadge");
        RectTransform lvlRt = refs.levelBadge.rectTransform;
        lvlRt.anchorMin = new Vector2(1f, 1f);
        lvlRt.anchorMax = new Vector2(1f, 1f);
        lvlRt.pivot = new Vector2(1f, 1f);
        lvlRt.anchoredPosition = new Vector2(-10f, -8f);
        lvlRt.sizeDelta = new Vector2(92f, 22f);
        refs.levelBadge.fontSize = 11f;
        refs.levelBadge.alignment = TextAlignmentOptions.TopRight;
        refs.levelBadge.fontStyle = FontStyles.Bold;

        refs.rarity = GetOrCreateChildText(root, "Rarity");
        RectTransform rarityRt = refs.rarity.rectTransform;
        rarityRt.anchorMin = new Vector2(0.5f, 1f);
        rarityRt.anchorMax = new Vector2(0.5f, 1f);
        rarityRt.pivot = new Vector2(0.5f, 1f);
        rarityRt.anchoredPosition = new Vector2(0f, -34f);
        rarityRt.sizeDelta = new Vector2(200f, 18f);
        refs.rarity.fontSize = 11f;
        refs.rarity.fontStyle = FontStyles.Bold;
        refs.rarity.alignment = TextAlignmentOptions.Center;

        refs.lockIcon = GetOrCreateChildImage(root, "LockIcon");
        RectTransform lockRt = refs.lockIcon.rectTransform;
        lockRt.anchorMin = new Vector2(1f, 1f);
        lockRt.anchorMax = new Vector2(1f, 1f);
        lockRt.pivot = new Vector2(1f, 1f);
        lockRt.anchoredPosition = new Vector2(-8f, -34f);
        lockRt.sizeDelta = new Vector2(36f, 36f);
        refs.lockIcon.preserveAspect = true;
        refs.lockIcon.raycastTarget = false;
        refs.lockIcon.enabled = false;

        refs.priceBg = GetOrCreateChildImage(root, "PriceBg");
        RectTransform priceBgRt = refs.priceBg.rectTransform;
        priceBgRt.anchorMin = new Vector2(0.5f, 0f);
        priceBgRt.anchorMax = new Vector2(0.5f, 0f);
        priceBgRt.pivot = new Vector2(0.5f, 0f);
        priceBgRt.anchoredPosition = new Vector2(0f, 8f);
        priceBgRt.sizeDelta = new Vector2(168f, 30f);
        refs.priceBg.color = new Color(0.05f, 0.06f, 0.1f, 0.88f);
        refs.priceBg.raycastTarget = false;
        if (GuiArtLibrary.ButtonSecondary != null)
        {
            refs.priceBg.sprite = GuiArtLibrary.ButtonSecondary;
            refs.priceBg.type = Image.Type.Sliced;
            refs.priceBg.color = new Color(0.12f, 0.1f, 0.08f, 0.94f);
        }

        refs.priceLabel = GetOrCreateChildText(root, "PriceLabel");
        RectTransform priceRt = refs.priceLabel.rectTransform;
        priceRt.anchorMin = new Vector2(0.5f, 0f);
        priceRt.anchorMax = new Vector2(0.5f, 0f);
        priceRt.pivot = new Vector2(0.5f, 0f);
        priceRt.anchoredPosition = new Vector2(0f, 8f);
        priceRt.sizeDelta = new Vector2(160f, 30f);
        refs.priceLabel.fontSize = 15f;
        refs.priceLabel.fontStyle = FontStyles.Bold;
        refs.priceLabel.alignment = TextAlignmentOptions.Center;

        refs.iconBg = GetOrCreateChildImage(root, "IconBg");
        RectTransform iconBgRt = refs.iconBg.rectTransform;
        iconBgRt.anchorMin = iconBgRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconBgRt.pivot = new Vector2(0.5f, 0.5f);
        iconBgRt.sizeDelta = new Vector2(104f, 104f);
        refs.iconBg.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
        refs.iconBg.raycastTarget = false;

        refs.icon = GetOrCreateChildImage(root, "Icon");
        RectTransform iconRt = refs.icon.rectTransform;
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(96f, 96f);
        refs.icon.preserveAspect = true;
        refs.icon.raycastTarget = false;

        refs.title = GetOrCreateChildText(root, "Title");
        RectTransform titleRt = refs.title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -118f);
        titleRt.sizeDelta = new Vector2(-16f, 30f);

        refs.statLine = GetOrCreateChildText(root, "StatLine");
        RectTransform statRt = refs.statLine.rectTransform;
        statRt.anchorMin = new Vector2(0f, 1f);
        statRt.anchorMax = new Vector2(1f, 1f);
        statRt.pivot = new Vector2(0.5f, 1f);
        statRt.anchoredPosition = new Vector2(0f, -148f);
        statRt.sizeDelta = new Vector2(-16f, 24f);
        refs.statLine.fontSize = 16f;
        refs.statLine.fontStyle = FontStyles.Bold;

        refs.description = GetOrCreateChildText(root, "Description");
        RectTransform descRt = refs.description.rectTransform;
        descRt.anchorMin = new Vector2(0f, 0f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.offsetMin = new Vector2(14f, 52f);
        descRt.offsetMax = new Vector2(-14f, -176f);
        refs.description.fontStyle = FontStyles.Italic;

        refs.synergyLabel = GetOrCreateChildText(root, "Synergy");
        RectTransform synRt = refs.synergyLabel.rectTransform;
        synRt.anchorMin = new Vector2(0f, 0f);
        synRt.anchorMax = new Vector2(1f, 0f);
        synRt.pivot = new Vector2(0.5f, 0f);
        synRt.anchoredPosition = new Vector2(0f, 36f);
        synRt.sizeDelta = new Vector2(-12f, 18f);
        refs.synergyLabel.fontSize = 12f;
        refs.synergyLabel.color = new Color(1f, 0.88f, 0.35f, 1f);

        refs.progressFill = GetOrCreateChildImage(root, "ProgressFill");
        RectTransform fillRt = refs.progressFill.rectTransform;
        fillRt.anchorMin = new Vector2(0.08f, 0f);
        fillRt.anchorMax = new Vector2(0.08f, 0f);
        fillRt.pivot = new Vector2(0f, 0f);
        fillRt.anchoredPosition = new Vector2(0f, 14f);
        fillRt.sizeDelta = new Vector2(0f, 8f);
        refs.progressFill.color = new Color(0.3f, 0.75f, 0.95f, 1f);
        refs.progressFill.raycastTarget = false;

        refs.progressText = GetOrCreateChildText(root, "ProgressText");
        RectTransform progRt = refs.progressText.rectTransform;
        progRt.anchorMin = new Vector2(0f, 0f);
        progRt.anchorMax = new Vector2(1f, 0f);
        progRt.pivot = new Vector2(0.5f, 0f);
        progRt.anchoredPosition = new Vector2(0f, 24f);
        progRt.sizeDelta = new Vector2(-12f, 18f);
        refs.progressText.fontSize = 12f;

        refs.stack = GetOrCreateChildText(root, "Stack");
        refs.stack.gameObject.SetActive(false);
        Transform legacyLabel = root.Find("Label");
        if (legacyLabel != null)
            legacyLabel.gameObject.SetActive(false);

        refs.interaction = button.GetComponent<SkillCardInteraction>();
        if (refs.interaction == null)
            refs.interaction = button.gameObject.AddComponent<SkillCardInteraction>();

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

    private void ApplyChoiceToCard(SkillCardRefs refs, SkillSelectionChoice choice, int cardIndex)
    {
        if (refs == null)
            return;

        if (choice == null)
        {
            ClearCard(refs);
            return;
        }

        refs.interaction?.Bind(choice);

        SkillRarity rarity = SkillPurchasePricing.ResolveRarity(choice);
        Color borderColor = GetRarityBorderColor(rarity);
        Color typeColor = GetTypeBadgeColor(choice.kind);
        bool synergy = SkillSelectionSynergy.HasSynergy(choice);

        if (refs.background != null)
            refs.background.color = new Color(0.12f, 0.13f, 0.18f, 0.98f);

        if (refs.border != null)
        {
            refs.border.enabled = true;
            // Dùng sprite khung 9-slice theo độ hiếm (xanh lá/lam/tím/vàng). Fallback: viền màu trơn.
            Sprite frameSprite = GuiArtLibrary.CardFrame(rarity);
            if (frameSprite != null)
            {
                refs.border.sprite = frameSprite;
                refs.border.type = Image.Type.Sliced;
                refs.border.color = Color.white;
            }
            else
            {
                refs.border.sprite = null;
                refs.border.color = borderColor;
            }
        }

        if (refs.accentBar != null)
        {
            refs.accentBar.enabled = true;
            refs.accentBar.color = typeColor;
        }

        if (refs.typeBadge != null)
        {
            refs.typeBadge.text = GetTypeBadgeLabel(choice.kind);
            refs.typeBadge.color = typeColor;
        }

        int curLevel;
        int maxLevel;
        GetLevelProgress(choice, out curLevel, out maxLevel);
        ApplyLevelBadge(refs, curLevel, maxLevel);
        ApplyProgressBar(refs, curLevel, maxLevel);

        if (refs.synergyGlow != null)
            refs.synergyGlow.color = new Color(1f, 0.85f, 0.2f, synergy ? 0.45f : 0f);

        int coinCost = SkillPurchasePricing.GetCost(choice);
        bool showPrice = SkillPurchasePricing.IsPurchasable(choice) && coinCost > 0;

        if (refs.rarity != null)
        {
            refs.rarity.gameObject.SetActive(showPrice);
            refs.rarity.text = GetRarityLabel(rarity);
            refs.rarity.color = SkillPurchasePricing.GetPriceColor(rarity);
        }

        if (refs.priceBg != null)
            refs.priceBg.gameObject.SetActive(showPrice);

        if (refs.priceLabel != null)
        {
            refs.priceLabel.gameObject.SetActive(showPrice);
            if (showPrice)
            {
                refs.priceLabel.text = choice.note == "locked_purchase"
                    ? "ĐÃ KHÓA · " + coinCost + " XU"
                    : coinCost + " XU";
                refs.priceLabel.color = SkillPurchasePricing.GetPriceColor(rarity);
            }
        }

        if (refs.synergyLabel != null)
        {
            if (choice.note == "locked_purchase")
                refs.synergyLabel.text = "Mua để nhận thẻ này";
            else
            {
                string evolve = SkillSelectionSynergy.GetEvolutionLabel(choice);
                refs.synergyLabel.text = !string.IsNullOrEmpty(evolve)
                    ? evolve
                    : (synergy ? "Có thể evolve!" : string.Empty);
            }
        }

        switch (choice.kind)
        {
            case SkillSelectionChoiceKind.WeaponPickup:
                if (refs.title != null) refs.title.text = WeaponDisplayName(choice.weaponType);
                if (refs.description != null)
                    refs.description.text = TruncateLines(RunLoadout.Description(choice.weaponType), 2);
                if (refs.statLine != null)
                {
                    int copies = WeaponManager.Instance != null ? WeaponManager.Instance.GetWeaponCopies(choice.weaponType) : 0;
                    refs.statLine.text = copies > 0 ? "Nâng cấp x" + (copies + 1) : "Vũ khí mới";
                }
                SetIcon(refs, GameIconLibrary.WeaponSprite(choice.weaponType), GameIconLibrary.WeaponTint(choice.weaponType));
                break;

            case SkillSelectionChoiceKind.PassiveItem:
                if (choice.passiveItem != null)
                {
                    if (refs.title != null) refs.title.text = choice.passiveItem.displayName;
                    if (refs.description != null)
                        refs.description.text = TruncateLines(choice.passiveItem.description, 2);
                    if (refs.statLine != null)
                        refs.statLine.text = FormatPassiveStat(choice.passiveItem, curLevel + 1);
                    SetIcon(refs, GameIconLibrary.PassiveSprite(choice.passiveItem),
                        GameIconLibrary.PassiveTint(choice.passiveItem));
                }
                break;

            case SkillSelectionChoiceKind.BonusHp:
            case SkillSelectionChoiceKind.HealFallback:
                if (refs.title != null) refs.title.text = choice.kind == SkillSelectionChoiceKind.HealFallback ? "Hồi phục" : "+HP";
                if (refs.description != null)
                    refs.description.text = choice.note ?? "Phần thưởng dự phòng khi pool cạn.";
                if (refs.statLine != null)
                {
                    float hp = choice.bonusHp > 0 ? choice.bonusHp : SkillSelectionConfig.Get().allMaxedHealHp;
                    refs.statLine.text = "+" + Mathf.RoundToInt(hp) + " HP";
                }
                SetIcon(refs, null, SkillBadgeColor);
                break;

            case SkillSelectionChoiceKind.BonusCoin:
                if (refs.title != null) refs.title.text = "+Xu";
                if (refs.description != null) refs.description.text = "Nhận xu ngay trong run.";
                if (refs.statLine != null) refs.statLine.text = "+" + choice.bonusCoins + " xu";
                SetIcon(refs, null, new Color(1f, 0.85f, 0.35f));
                break;

            default:
                SkillData skill = choice.skill;
                if (skill == null)
                    break;
                if (refs.title != null) refs.title.text = skill.skillName;
                if (refs.description != null) refs.description.text = TruncateLines(skill.description, 2);
                if (refs.statLine != null) refs.statLine.text = FormatSkillStat(skill);
                SetIcon(refs, GameIconLibrary.SkillSprite(skill.skillType), GameIconLibrary.SkillTint(skill.skillType));
                break;
        }

        StyleCardTypography(refs);
        ApplyCardLockState(cardIndex, refs);
    }

    private static void ClearCard(SkillCardRefs refs)
    {
        if (refs.title != null) refs.title.text = string.Empty;
        if (refs.description != null) refs.description.text = string.Empty;
        if (refs.statLine != null) refs.statLine.text = string.Empty;
        if (refs.typeBadge != null) refs.typeBadge.text = string.Empty;
        if (refs.levelBadge != null) refs.levelBadge.text = string.Empty;
        if (refs.icon != null) refs.icon.enabled = false;
        if (refs.lockIcon != null) refs.lockIcon.enabled = false;
        if (refs.priceBg != null) refs.priceBg.gameObject.SetActive(false);
        if (refs.priceLabel != null) refs.priceLabel.gameObject.SetActive(false);
        if (refs.rarity != null) refs.rarity.gameObject.SetActive(false);
    }

    private static SkillRarity GetChoiceRarity(SkillSelectionChoice choice)
    {
        if (choice == null)
            return SkillRarity.Common;
        if (choice.kind == SkillSelectionChoiceKind.SkillUpgrade && choice.skill != null)
            return choice.skill.rarity;
        if (choice.kind == SkillSelectionChoiceKind.PassiveItem && choice.passiveItem != null)
            return choice.passiveItem.rarity;
        return SkillRarity.Common;
    }

    private static Color GetTypeBadgeColor(SkillSelectionChoiceKind kind)
    {
        switch (kind)
        {
            case SkillSelectionChoiceKind.WeaponPickup:
                return WeaponBadgeColor;
            case SkillSelectionChoiceKind.PassiveItem:
                return PassiveBadgeColor;
            default:
                return SkillBadgeColor;
        }
    }

    private static string GetTypeBadgeLabel(SkillSelectionChoiceKind kind)
    {
        switch (kind)
        {
            case SkillSelectionChoiceKind.WeaponPickup: return "WEAPON";
            case SkillSelectionChoiceKind.PassiveItem: return "PASSIVE";
            default: return "SKILL";
        }
    }

    private static Color GetRarityBorderColor(SkillRarity rarity)
    {
        switch (rarity)
        {
            case SkillRarity.Rare: return new Color(0.2f, 0.75f, 0.35f, 1f);
            case SkillRarity.Epic: return new Color(0.55f, 0.28f, 0.85f, 1f);
            case SkillRarity.Legendary: return new Color(0.95f, 0.72f, 0.15f, 1f);
            default: return new Color(0.55f, 0.55f, 0.58f, 1f);
        }
    }

    private void GetLevelProgress(SkillSelectionChoice choice, out int current, out int max)
    {
        current = 0;
        max = config != null ? config.maxSkillStack : 3;

        if (choice == null)
            return;

        switch (choice.kind)
        {
            case SkillSelectionChoiceKind.SkillUpgrade:
                if (choice.skill != null && PlayerSkillHandler.Instance != null)
                    current = PlayerSkillHandler.Instance.GetStack(choice.skill.skillType);
                max = choice.skill != null && choice.skill.rarity == SkillRarity.Legendary ? 1 : max;
                break;
            case SkillSelectionChoiceKind.PassiveItem:
                if (choice.passiveItem != null)
                {
                    max = choice.passiveItem.maxLevel;
                    if (PassiveItemManager.Instance != null)
                        current = PassiveItemManager.Instance.GetLevel(choice.passiveItem);
                }
                break;
            case SkillSelectionChoiceKind.WeaponPickup:
                if (WeaponManager.Instance != null)
                    current = WeaponManager.Instance.GetWeaponCopies(choice.weaponType);
                max = 8;
                break;
        }
    }

    private static void ApplyLevelBadge(SkillCardRefs refs, int current, int max)
    {
        if (refs.levelBadge == null)
            return;

        bool vsStyle = SurvivalRunManager.IsSurvivalMode();

        if (current >= max && max > 0)
            refs.levelBadge.text = vsStyle ? "level: MAX" : "MAX";
        else if (current <= 0)
            refs.levelBadge.text = vsStyle ? "level: 1" : "MỚI!";
        else
            refs.levelBadge.text = vsStyle
                ? "level: " + (current + 1)
                : "Lv " + current + " → Lv " + (current + 1);
    }

    private void ApplyProgressBar(SkillCardRefs refs, int current, int max)
    {
        if (refs.progressText != null)
            refs.progressText.text = Mathf.Clamp(current, 0, max) + "/" + Mathf.Max(1, max);

        if (refs.progressFill == null || refs.background == null)
            return;

        float width = (refs.background.rectTransform.rect.width - 28f) * Mathf.Clamp01((float)current / Mathf.Max(1, max));
        RectTransform rt = refs.progressFill.rectTransform;
        rt.sizeDelta = new Vector2(width, 8f);
    }

    private static string TruncateLines(string text, int maxLines)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string[] lines = text.Split('\n');
        if (lines.Length <= maxLines)
            return text;

        return lines[0] + "\n" + lines[1];
    }

    private static string FormatSkillStat(SkillData skill)
    {
        if (skill == null)
            return string.Empty;
        if (skill.value > 0f)
            return "+" + skill.value.ToString("0.#") + " power";
        return GetRarityLabel(skill.rarity);
    }

    private static string FormatPassiveStat(PassiveItemData passive, int nextLevel)
    {
        if (passive == null)
            return string.Empty;

        float v = passive.GetValueAtLevel(nextLevel);
        string sign = v >= 0f ? "+" : "";
        if (passive.isPercent)
            return sign + (v * 100f).ToString("0.#") + "% " + passive.statModifierType;
        return sign + v.ToString("0.#") + " " + passive.statModifierType;
    }

    private void EnsureButtonArray()
    {
        if (skillButtons == null || skillButtons.Length != 4)
            skillButtons = new Button[4];

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
