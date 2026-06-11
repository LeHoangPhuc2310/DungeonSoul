// DungeonSoul — CharacterSelectUI.cs — Lưới nhân vật chọn + panel chi tiết.

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private int gameSceneBuildIndex = 1;
    [SerializeField] private string gameSceneName = "SampleScene";

    private static readonly Color BgColor = new Color(0.03f, 0.04f, 0.07f, 0.96f);
    private static readonly Color PanelBg = new Color(0.07f, 0.06f, 0.05f, 0.98f);
    private static readonly Color Gold = new Color(0.96f, 0.82f, 0.28f, 1f);
    private static readonly Color SlotNormal = new Color(0.11f, 0.1f, 0.09f, 0.98f);
    private static readonly Color SlotSelected = new Color(0.28f, 0.22f, 0.08f, 1f);
    private const float PortraitCardWidth = 118f;
    private const float PortraitCardHeight = 188f;
    private const int CardsPerRow = 5;
    private const float GridContentWidth = 680f;
    private static readonly Vector2 PortraitCell = new Vector2(PortraitCardWidth, PortraitCardHeight);
    private static readonly Color ClassWarrior = new Color(0.85f, 0.55f, 0.35f, 1f);
    private static readonly Color ClassRanger = new Color(0.45f, 0.85f, 0.55f, 1f);
    private static readonly Color ClassMage = new Color(0.65f, 0.55f, 0.95f, 1f);

    private PlayableCharacterEntry selected;
    private readonly Dictionary<string, Image> slotFrames = new Dictionary<string, Image>();
    private readonly Dictionary<string, HeroType> slotHeroClasses = new Dictionary<string, HeroType>();
    private Image detailPanelBg;
    private Image detailPreview;
    private TMP_Text detailName;
    private TMP_Text detailClass;
    private TMP_Text detailBonus;
    private TMP_Text detailAbilityTitle;
    private TMP_Text detailAbilityBody;
    private Button confirmButton;
    private Coroutine previewRoutine;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioManager.EnsureExists();
        HideStrayGameplayUi();
        BuildUI();

        selected = PlayableCharacterCatalog.GetSelected();
        if (selected == null && PlayableCharacterCatalog.Visible.Count > 0)
            selected = PlayableCharacterCatalog.Visible[0];

        if (selected != null)
            SelectCharacter(selected, playSound: false);
    }

    private static void HideStrayGameplayUi()
    {
        if (VirtualJoystick.Instance != null)
            Destroy(VirtualJoystick.Instance.gameObject);
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("CharacterSelectCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        Image bg = MakeRect("BG", canvasGO.transform, Vector2.zero, Vector2.zero).AddComponent<Image>();
        Stretch(bg.rectTransform);
        bg.color = BgColor;

        TMP_Text title = MakeLabel("CHỌN MỘT ANH HÙNG ĐỂ BẮT ĐẦU", canvasGO.transform,
            new Vector2(0f, 470f), new Vector2(1500f, 70f), TextAlignmentOptions.Center);
        GameUIFont.Apply(title, GameUIFont.Role.HeaderTitle);

        TMP_Text hint = MakeLabel("Chọn nhân vật rồi bấm Tiếp theo — vào dungeon ngay", canvasGO.transform,
            new Vector2(0f, 405f), new Vector2(1200f, 40f), TextAlignmentOptions.Center);
        GameUIFont.Apply(hint, GameUIFont.Role.HeaderHint);

        GameObject body = MakeRect("Body", canvasGO.transform, Vector2.zero, Vector2.zero);
        RectTransform bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(24f, 24f);
        bodyRt.offsetMax = new Vector2(-24f, -130f);
        BuildGridPanel(body.transform);
        BuildDetailPanel(body.transform);
    }

    private void BuildGridPanel(Transform parent)
    {
        GameObject panel = MakeRect("GridPanel", parent, Vector2.zero, Vector2.zero);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = new Vector2(0.58f, 1f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = new Vector2(-8f, 0f);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.32f);

        GameObject scrollGo = MakeRect("Scroll", panel.transform, Vector2.zero, Vector2.zero);
        Stretch(scrollGo.GetComponent<RectTransform>());
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;

        GameObject viewport = MakeRect("Viewport", scrollGo.transform, Vector2.zero, Vector2.zero);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = MakeRect("Content", viewport.transform, new Vector2(0f, 100f), Vector2.zero);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 100f);
        scroll.content = contentRt;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 16f;
        contentLayout.padding = new RectOffset(8, 8, 8, 16);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        IReadOnlyList<PlayableCharacterEntry> visible = PlayableCharacterCatalog.Visible;
        if (visible.Count == 0)
        {
            MakeSectionLabel(content.transform, "Chạy menu: DungeonSoul → Characters → Import ASEPRITE Characters");
            return;
        }

        if (PlayableCharacterCatalog.HasVisibleInClass(HeroType.Warrior))
            BuildClassSection(content.transform, "CHIẾN BINH", HeroType.Warrior, ClassWarrior);
        if (PlayableCharacterCatalog.HasVisibleInClass(HeroType.Ranger))
            BuildClassSection(content.transform, "CUNG THỦ", HeroType.Ranger, ClassRanger);
        if (PlayableCharacterCatalog.HasVisibleInClass(HeroType.Mage))
            BuildClassSection(content.transform, "PHÁP SƯ", HeroType.Mage, ClassMage);
    }

    private void BuildClassSection(Transform content, string label, HeroType heroClass, Color accent)
    {
        GameObject section = MakeRect("Section_" + label, content, new Vector2(GridContentWidth, 10f), Vector2.zero);
        LayoutElement sectionLe = section.AddComponent<LayoutElement>();
        sectionLe.minWidth = GridContentWidth;
        sectionLe.preferredWidth = GridContentWidth;

        VerticalLayoutGroup sectionLayout = section.AddComponent<VerticalLayoutGroup>();
        sectionLayout.spacing = 8f;
        sectionLayout.padding = new RectOffset(0, 0, 0, 0);
        sectionLayout.childAlignment = TextAnchor.UpperLeft;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;

        ContentSizeFitter sectionFitter = section.AddComponent<ContentSizeFitter>();
        sectionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        MakeSectionLabel(section.transform, label, accent);

        GameObject cardsRoot = MakeRect("Cards", section.transform, new Vector2(GridContentWidth, 10f), Vector2.zero);
        LayoutElement cardsLe = cardsRoot.AddComponent<LayoutElement>();
        cardsLe.minWidth = GridContentWidth;
        cardsLe.preferredWidth = GridContentWidth;

        GridLayoutGroup grid = cardsRoot.AddComponent<GridLayoutGroup>();
        grid.cellSize = PortraitCell;
        grid.spacing = new Vector2(10f, 10f);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = CardsPerRow;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter cardsFitter = cardsRoot.AddComponent<ContentSizeFitter>();
        cardsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (PlayableCharacterEntry entry in PlayableCharacterCatalog.ByClass(heroClass))
        {
            if (entry != null && entry.PreviewSprite != null)
                BuildCharacterSlot(cardsRoot.transform, entry);
        }
    }

    private void BuildDetailPanel(Transform parent)
    {
        GameObject panel = MakeRect("DetailPanel", parent, Vector2.zero, Vector2.zero);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.58f, 0f);
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(8f, 0f);
        panelRt.offsetMax = Vector2.zero;

        detailPanelBg = panel.AddComponent<Image>();
        detailPanelBg.color = new Color(0.05f, 0.06f, 0.1f, 0.88f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        detailName = MakeLayoutLabel(panel.transform, "Tên nhân vật", 40f, TextAlignmentOptions.Center);
        GameUIFont.Apply(detailName, GameUIFont.Role.CardTitle);

        detailClass = MakeLayoutLabel(panel.transform, "Lớp", 26f, TextAlignmentOptions.Center);
        GameUIFont.Apply(detailClass, GameUIFont.Role.CardRarity);

        GameObject previewFrameGo = MakeRect("PreviewFrame", panel.transform, new Vector2(200f, 300f), Vector2.zero);
        LayoutElement previewLe = previewFrameGo.AddComponent<LayoutElement>();
        previewLe.preferredWidth = 200f;
        previewLe.preferredHeight = 300f;
        previewLe.minHeight = 300f;
        Image previewFrame = previewFrameGo.AddComponent<Image>();
        previewFrame.raycastTarget = false;
        previewFrame.color = Color.white;

        GameObject previewGo = MakeRect("Preview", previewFrameGo.transform, new Vector2(130f, 130f), new Vector2(0f, 16f));
        detailPreview = previewGo.AddComponent<Image>();
        detailPreview.preserveAspect = true;
        detailPreview.raycastTarget = false;

        detailBonus = MakeLayoutLabel(panel.transform, "", 150f, TextAlignmentOptions.TopLeft);
        GameUIFont.Apply(detailBonus, GameUIFont.Role.CardBody);
        detailBonus.fontSize = 15f;

        detailAbilityTitle = MakeLayoutLabel(panel.transform, "", 30f, TextAlignmentOptions.TopLeft);
        GameUIFont.Apply(detailAbilityTitle, GameUIFont.Role.CardTitle);
        detailAbilityTitle.fontSize = 20f;
        detailAbilityTitle.alignment = TextAlignmentOptions.TopLeft;

        detailAbilityBody = MakeLayoutLabel(panel.transform, "", 72f, TextAlignmentOptions.TopLeft);
        GameUIFont.Apply(detailAbilityBody, GameUIFont.Role.CardBody);
        detailAbilityBody.fontSize = 14f;

        confirmButton = MakeLayoutButton("Tiếp theo →", panel.transform, 64f, OnConfirm);
    }

    private void BuildCharacterSlot(Transform parent, PlayableCharacterEntry entry)
    {
        string key = entry.id;
        PlayableCharacterEntry captured = entry;
        Image frame = BuildSlotFrame(parent, key, entry.combatClass, () => SelectCharacter(captured));

        GameObject iconGo = MakeRect("Icon", frame.transform, new Vector2(72f, 72f), new Vector2(0f, 18f));
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.sprite = entry.PreviewSprite;

        TMP_Text name = MakeLabel(entry.displayName, frame.transform, new Vector2(0f, -72f),
            new Vector2(100f, 30f), TextAlignmentOptions.Center);
        GameUIFont.Apply(name, GameUIFont.Role.CardStack);
        name.fontSize = 11f;
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
    }

    private Image BuildSlotFrame(Transform parent, string key, HeroType heroClass, UnityEngine.Events.UnityAction onClick)
    {
        GameObject slot = MakeRect("Slot_" + key, parent, PortraitCell, Vector2.zero);
        LayoutElement slotLe = slot.AddComponent<LayoutElement>();
        slotLe.preferredWidth = PortraitCardWidth;
        slotLe.preferredHeight = PortraitCardHeight;
        slotLe.minWidth = PortraitCardWidth;
        slotLe.minHeight = PortraitCardHeight;

        Image frame = slot.AddComponent<Image>();
        slotHeroClasses[key] = heroClass;
        ApplyCharacterSlotFrame(frame, heroClass, false);
        Button btn = slot.AddComponent<Button>();
        btn.targetGraphic = frame;
        btn.onClick.AddListener(onClick);
        slotFrames[key] = frame;
        return frame;
    }

    private static void ApplyCharacterSlotFrame(Image frame, HeroType heroClass, bool selected)
    {
        Sprite sprite = selected ? GuiArtLibrary.CardSelected : GuiArtLibrary.CardFrameForHero(heroClass);
        Color fallback = selected ? SlotSelected : SlotNormal;
        GuiArtLibrary.ApplyCardFrame(frame, sprite, fallback, selected);
        if (!selected)
            frame.color = new Color(0.78f, 0.78f, 0.82f, 1f);
    }

    private void MakeSectionLabel(Transform parent, string text, Color? accent = null)
    {
        GameObject go = MakeRect("SectionLabel", parent, new Vector2(GridContentWidth, 26f), Vector2.zero);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 26f;
        le.preferredHeight = 26f;
        le.minWidth = GridContentWidth;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        GameUIFont.Apply(tmp, GameUIFont.Role.CardRarity);
        tmp.text = text;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = accent ?? new Color(0.75f, 0.7f, 0.6f, 1f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static TMP_Text MakeLayoutLabel(Transform parent, string text, float height, TextAlignmentOptions align)
    {
        GameObject go = MakeRect("Lbl", parent, new Vector2(500f, height), Vector2.zero);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.minWidth = 480f;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.richText = true;
        return tmp;
    }

    private void SelectCharacter(PlayableCharacterEntry entry, bool playSound = true)
    {
        if (entry == null)
            return;

        if (playSound)
            AudioManager.PlayUiTap();

        selected = entry;
        HeroRunStats.SetCharacter(entry);
        RefreshSlotHighlights();
        RefreshDetail(entry);
    }

    private void RefreshSlotHighlights()
    {
        foreach (KeyValuePair<string, Image> kv in slotFrames)
        {
            bool isSelected = selected != null && kv.Key == selected.id;
            HeroType heroClass = slotHeroClasses.TryGetValue(kv.Key, out HeroType cls) ? cls : HeroType.Warrior;
            ApplyCharacterSlotFrame(kv.Value, heroClass, isSelected);
        }


    }

    private void RefreshDetail(PlayableCharacterEntry entry)
    {
        detailName.text = entry.displayName;
        detailClass.text = entry.ClassLabel.ToUpperInvariant() + " · " + entry.CombatStyleLabel.ToUpperInvariant();
        detailClass.color = entry.combatClass switch
        {
            HeroType.Ranger => ClassRanger,
            HeroType.Mage => ClassMage,
            _ => ClassWarrior
        };

        Transform previewFrame = detailPreview != null ? detailPreview.transform.parent : null;
        if (previewFrame != null)
        {
            Image frameImg = previewFrame.GetComponent<Image>();
            if (frameImg != null)
                GuiArtLibrary.ApplyCardFrame(frameImg, GuiArtLibrary.CardSelected, SlotSelected, true);
        }

        detailBonus.text = "<color=#73FF73>Bonus</color>\n"
            + "<color=#73FF73>" + entry.bonusPositive + "</color>\n"
            + "<color=#FF6666>" + entry.bonusNegative + "</color>\n\n"
            + "Máu: " + entry.hp + "  |  Sát thương: " + entry.damage + "\n"
            + "Tốc đánh: " + entry.fireRate + "/s  |  Chạy: " + entry.moveSpeed + "\n"
            + "Chí mạng: " + Mathf.RoundToInt(entry.crit * 100f) + "%";

        detailAbilityTitle.text = entry.abilityName;
        detailAbilityBody.text = "<color=#A8C8FF><i>" + entry.CombatStyleDescription + "</i></color>\n\n"
            + entry.abilityDescription;

        Sprite[] frames = entry.idle != null && entry.idle.Length > 0 ? entry.idle : entry.walk;
        StartPreview(frames);
    }

    private void StartPreview(Sprite[] frames)
    {
        if (previewRoutine != null)
            StopCoroutine(previewRoutine);

        if (frames == null || frames.Length == 0)
        {
            detailPreview.sprite = selected != null ? selected.PreviewSprite : null;
            return;
        }

        detailPreview.sprite = frames[0];
        if (frames.Length > 1)
            previewRoutine = StartCoroutine(AnimatePreview(frames, 10f));
    }

    private IEnumerator AnimatePreview(Sprite[] frames, float fps)
    {
        int idx = 0;
        float timer = 0f;
        while (true)
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= 1f / fps)
            {
                timer = 0f;
                idx = (idx + 1) % frames.Length;
                if (detailPreview != null)
                    detailPreview.sprite = frames[idx];
            }

            yield return null;
        }
    }

    private void OnConfirm()
    {
        if (selected == null)
            return;

        if (!PlayableCharacterCatalog.IsUnlocked(selected))
        {
            int cost = MetaRunProgress.GetUnlockCost(selected.id);
            if (!MetaRunProgress.TryUnlockCharacter(selected.id))
            {
                Debug.Log("[CharacterSelect] Cần " + cost + " Souls để mở khóa " + selected.displayName);
                return;
            }
        }

        AudioManager.PlayUiTap();
        HeroRunStats.SetCharacter(selected);
        RunEntryGate.ConfirmCharacterSelect();
        Time.timeScale = 1f;

        string nextScene = ResolveNextScene();
        if (!string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            Debug.LogError("[CharacterSelectUI] Không tìm thấy scene: " + nextScene);
    }

    private string ResolveNextScene()
    {
        if (selected != null && WeaponStyleUtil.UsesWeaponPickupRewards(selected.combatClass))
        {
            if (Application.CanStreamedLevelBeLoaded("WeaponSelectScene"))
                return "WeaponSelectScene";
        }

        return gameSceneName;
    }

    private static GameObject MakeRect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text MakeLabel(string text, Transform parent, Vector2 pos, Vector2 size,
        TextAlignmentOptions align)
    {
        GameObject go = MakeRect("Lbl", parent, size, pos);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.richText = true;
        return tmp;
    }

    private static Button MakeLayoutButton(string label, Transform parent, float height,
        UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.preferredWidth = 280f;

        Image bg = go.AddComponent<Image>();
        if (!GuiArtLibrary.ApplyButton(bg, GuiArtLibrary.ButtonPrimary, new Color(0.2f, 0.6f, 0.32f, 1f)))
            bg.color = new Color(0.2f, 0.6f, 0.32f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(action);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        Stretch(textGo.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        GameUIFont.Apply(tmp, GameUIFont.Role.Button);
        return btn;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}
