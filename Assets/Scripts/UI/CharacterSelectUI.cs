// DungeonSoul — CharacterSelectUI.cs — Lưới 20 nhân vật Tiny RPG + panel chi tiết.

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
    private static readonly Color ClassWarrior = new Color(0.85f, 0.55f, 0.35f, 1f);
    private static readonly Color ClassRanger = new Color(0.45f, 0.85f, 0.55f, 1f);
    private static readonly Color ClassMage = new Color(0.65f, 0.55f, 0.95f, 1f);

    private PlayableCharacterEntry selected;
    private readonly Dictionary<string, Image> slotFrames = new Dictionary<string, Image>();
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
        HideStrayGameplayUi();
        BuildUI();

        selected = PlayableCharacterCatalog.GetSelected();
        if (selected == null && PlayableCharacterCatalog.All.Count > 0)
            selected = PlayableCharacterCatalog.All[0];

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

        TMP_Text hint = MakeLabel("20 nhân vật — chọn một anh hùng rồi bấm Chọn", canvasGO.transform,
            new Vector2(0f, 405f), new Vector2(1200f, 40f), TextAlignmentOptions.Center);
        GameUIFont.Apply(hint, GameUIFont.Role.HeaderHint);

        GameObject body = MakeRect("Body", canvasGO.transform, new Vector2(1780f, 820f), new Vector2(0f, -20f));
        BuildGridPanel(body.transform);
        BuildDetailPanel(body.transform);

        confirmButton = MakeButton("Chọn", canvasGO.transform, new Vector2(640f, -430f),
            new Vector2(280f, 72f), OnConfirm, new Color(0.72f, 0.12f, 0.12f, 1f));
    }

    private void BuildGridPanel(Transform parent)
    {
        GameObject panel = MakeRect("GridPanel", parent, new Vector2(1080f, 760f), new Vector2(-350f, 0f));
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.28f);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.3f, 0.2f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject scrollGo = MakeRect("Scroll", panel.transform, new Vector2(1040f, 720f), Vector2.zero);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        GameObject viewport = MakeRect("Viewport", scrollGo.transform, Vector2.zero, Vector2.zero);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject content = MakeRect("Content", viewport.transform, new Vector2(980f, 900f), Vector2.zero);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        scroll.content = contentRt;

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(128f, 138f);
        grid.spacing = new Vector2(10f, 10f);
        grid.padding = new RectOffset(14, 14, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        IReadOnlyList<PlayableCharacterEntry> all = PlayableCharacterCatalog.All;
        if (all.Count == 0)
        {
            MakeSectionLabel(content.transform, "Chạy menu: DungeonSoul → Characters → Build Playable Character Database");
            return;
        }

        AddClassSection(content.transform, "CHIẾN BINH", HeroType.Warrior, ClassWarrior);
        AddClassSection(content.transform, "CUNG THỦ", HeroType.Ranger, ClassRanger);
        AddClassSection(content.transform, "PHÁP SƯ", HeroType.Mage, ClassMage);
    }

    private void AddClassSection(Transform content, string label, HeroType heroClass, Color accent)
    {
        MakeSectionLabel(content, label, accent);
        foreach (PlayableCharacterEntry entry in PlayableCharacterCatalog.ByClass(heroClass))
        {
            if (entry != null && entry.PreviewSprite != null)
                BuildCharacterSlot(content, entry);
        }
    }

    private void BuildDetailPanel(Transform parent)
    {
        GameObject panel = MakeRect("DetailPanel", parent, new Vector2(620f, 760f), new Vector2(530f, 0f));
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = PanelBg;
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(3f, -3f);

        detailName = MakeLabel("Tên nhân vật", panel.transform, new Vector2(0f, 320f),
            new Vector2(560f, 56f), TextAlignmentOptions.Center);
        GameUIFont.Apply(detailName, GameUIFont.Role.CardTitle);

        detailClass = MakeLabel("Lớp", panel.transform, new Vector2(0f, 268f),
            new Vector2(560f, 32f), TextAlignmentOptions.Center);
        GameUIFont.Apply(detailClass, GameUIFont.Role.CardRarity);

        GameObject previewGo = MakeRect("Preview", panel.transform, new Vector2(240f, 240f), new Vector2(0f, 130f));
        detailPreview = previewGo.AddComponent<Image>();
        detailPreview.preserveAspect = true;
        detailPreview.raycastTarget = false;

        detailBonus = MakeLabel("", panel.transform, new Vector2(0f, -30f),
            new Vector2(540f, 130f), TextAlignmentOptions.TopLeft);
        GameUIFont.Apply(detailBonus, GameUIFont.Role.CardBody);

        detailAbilityTitle = MakeLabel("", panel.transform, new Vector2(0f, -130f),
            new Vector2(540f, 36f), TextAlignmentOptions.TopLeft);
        GameUIFont.Apply(detailAbilityTitle, GameUIFont.Role.CardTitle);
        detailAbilityTitle.fontSize = 22f;
        detailAbilityTitle.alignment = TextAlignmentOptions.TopLeft;

        detailAbilityBody = MakeLabel("", panel.transform, new Vector2(0f, -250f),
            new Vector2(540f, 120f), TextAlignmentOptions.TopLeft);
        GameUIFont.Apply(detailAbilityBody, GameUIFont.Role.CardBody);
    }

    private void BuildCharacterSlot(Transform parent, PlayableCharacterEntry entry)
    {
        string key = entry.id;
        PlayableCharacterEntry captured = entry;
        Image frame = BuildSlotFrame(parent, key, () => SelectCharacter(captured));

        GameObject iconGo = MakeRect("Icon", frame.transform, new Vector2(84f, 84f), new Vector2(0f, 14f));
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.sprite = entry.PreviewSprite;

        TMP_Text name = MakeLabel(entry.displayName, frame.transform, new Vector2(0f, -48f),
            new Vector2(118f, 36f), TextAlignmentOptions.Center);
        GameUIFont.Apply(name, GameUIFont.Role.CardStack);
        name.fontSize = 12f;
        name.enableWordWrapping = true;
    }

    private Image BuildSlotFrame(Transform parent, string key, UnityEngine.Events.UnityAction onClick)
    {
        GameObject slot = MakeRect("Slot_" + key, parent, new Vector2(128f, 138f), Vector2.zero);
        Image frame = slot.AddComponent<Image>();
        frame.color = SlotNormal;
        Button btn = slot.AddComponent<Button>();
        btn.targetGraphic = frame;
        btn.onClick.AddListener(onClick);
        slotFrames[key] = frame;
        return frame;
    }

    private void MakeSectionLabel(Transform parent, string text, Color? accent = null)
    {
        GameObject go = MakeRect("Section", parent, new Vector2(960f, 34f), Vector2.zero);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 34f;
        le.preferredHeight = 34f;
        le.minWidth = 960f;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        GameUIFont.Apply(tmp, GameUIFont.Role.CardRarity);
        tmp.text = text;
        tmp.fontSize = 15f;
        tmp.color = accent ?? new Color(0.75f, 0.7f, 0.6f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
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
            kv.Value.color = selected != null && kv.Key == selected.id ? SlotSelected : SlotNormal;
    }

    private void RefreshDetail(PlayableCharacterEntry entry)
    {
        detailName.text = entry.displayName;
        detailClass.text = entry.ClassLabel.ToUpperInvariant();
        detailClass.color = entry.combatClass switch
        {
            HeroType.Ranger => ClassRanger,
            HeroType.Mage => ClassMage,
            _ => ClassWarrior
        };

        detailBonus.text = "<color=#73FF73>Bonus</color>\n"
            + "<color=#73FF73>" + entry.bonusPositive + "</color>\n"
            + "<color=#FF6666>" + entry.bonusNegative + "</color>\n\n"
            + "Máu: " + entry.hp + "  |  Sát thương: " + entry.damage + "\n"
            + "Tốc đánh: " + entry.fireRate + "/s  |  Chạy: " + entry.moveSpeed + "\n"
            + "Chí mạng: " + Mathf.RoundToInt(entry.crit * 100f) + "%";

        detailAbilityTitle.text = entry.abilityName;
        detailAbilityBody.text = entry.abilityDescription;

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

        AudioManager.PlayUiTap();
        HeroRunStats.SetCharacter(selected);
        RunEntryGate.ConfirmCharacterSelect();
        Time.timeScale = 1f;

        if (gameSceneBuildIndex > 0 && gameSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(gameSceneBuildIndex);
        else if (!string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError("[CharacterSelectUI] Không tìm thấy SampleScene trong Build Settings.");
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

    private static Button MakeButton(string label, Transform parent, Vector2 pos, Vector2 size,
        UnityEngine.Events.UnityAction action, Color bgColor)
    {
        GameObject go = MakeRect(label, parent, size, pos);
        Image bg = go.AddComponent<Image>();
        bg.color = bgColor;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(action);

        GameObject textGo = MakeRect("Text", go.transform, Vector2.zero, Vector2.zero);
        Stretch(textGo.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
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
