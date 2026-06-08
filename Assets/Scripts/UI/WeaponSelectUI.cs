// DungeonSoul — WeaponSelectUI.cs — Màn chọn vũ khí khởi đầu (5 vũ khí) trước khi vào game.
// Flow: CharacterSelect → WeaponSelect → SampleScene. Tự dựng UI runtime.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WeaponSelectUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    private static readonly Color BgColor = new Color(0.03f, 0.04f, 0.07f, 0.96f);
    private static readonly Color Gold = new Color(0.96f, 0.82f, 0.28f, 1f);
    private static readonly Color SlotNormal = new Color(0.11f, 0.1f, 0.09f, 0.98f);
    private static readonly Color SlotSelected = new Color(0.28f, 0.22f, 0.08f, 1f);

    private WeaponType selected;
    private readonly Dictionary<WeaponType, Image> slotFrames = new Dictionary<WeaponType, Image>();
    private TMP_Text detailName;
    private TMP_Text detailDesc;
    private Image detailIcon;

    private void Start()
    {
        Time.timeScale = 1f;
        if (VirtualJoystick.Instance != null)
            Destroy(VirtualJoystick.Instance.gameObject);

        BuildUI();
        selected = RunLoadout.StartingWeapon;
        SelectWeapon(selected, playSound: false);
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("WeaponSelectCanvas");
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

        TMP_Text title = MakeLabel("CHỌN VŨ KHÍ KHỞI ĐẦU", canvasGO.transform,
            new Vector2(0f, 430f), new Vector2(1400f, 70f), TextAlignmentOptions.Center);
        GameUIFont.Apply(title, GameUIFont.Role.HeaderTitle);

        TMP_Text hint = MakeLabel("Chọn một vũ khí rồi bấm Bắt đầu", canvasGO.transform,
            new Vector2(0f, 372f), new Vector2(1200f, 40f), TextAlignmentOptions.Center);
        GameUIFont.Apply(hint, GameUIFont.Role.HeaderHint);

        // Hàng 5 vũ khí.
        WeaponType[] choices = RunLoadout.StartingChoices;
        const float cell = 200f;
        const float gap = 30f;
        float totalW = choices.Length * cell + (choices.Length - 1) * gap;
        float startX = -totalW * 0.5f + cell * 0.5f;
        for (int i = 0; i < choices.Length; i++)
        {
            float x = startX + i * (cell + gap);
            BuildWeaponSlot(canvasGO.transform, choices[i], new Vector2(x, 90f), new Vector2(cell, cell + 40f));
        }

        // Panel mô tả dưới.
        GameObject detail = MakeRect("Detail", canvasGO.transform, new Vector2(1100f, 180f), new Vector2(0f, -180f));
        Image detailBg = detail.AddComponent<Image>();
        if (!GuiArtLibrary.ApplyPanel(detailBg, GuiArtLibrary.DialogPanel))
        {
            detailBg.color = new Color(0.07f, 0.06f, 0.05f, 0.98f);
            Outline outline = detail.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        GameObject iconFrameGo = MakeRect("IconFrame", detail.transform, new Vector2(150f, 150f), new Vector2(-460f, 0f));
        Image iconFrame = iconFrameGo.AddComponent<Image>();
        iconFrame.raycastTarget = false;
        GuiArtLibrary.ApplyCardFrame(iconFrame, GuiArtLibrary.CardFrameForWeapon(selected), SlotSelected);

        detailIcon = MakeRect("Icon", iconFrameGo.transform, new Vector2(110f, 110f), Vector2.zero).AddComponent<Image>();
        detailIcon.preserveAspect = true;
        detailIcon.raycastTarget = false;

        detailName = MakeLabel("", detail.transform, new Vector2(40f, 45f), new Vector2(820f, 50f), TextAlignmentOptions.Left);
        GameUIFont.Apply(detailName, GameUIFont.Role.CardTitle);
        detailName.alignment = TextAlignmentOptions.Left;

        detailDesc = MakeLabel("", detail.transform, new Vector2(40f, -30f), new Vector2(820f, 110f), TextAlignmentOptions.TopLeft);
        GameUIFont.Apply(detailDesc, GameUIFont.Role.CardBody);

        // Nút.
        MakeButton("Quay lại", canvasGO.transform, new Vector2(-200f, -400f), new Vector2(240f, 70f),
            OnBack, new Color(0.3f, 0.22f, 0.26f, 1f));
        MakeButton("Bắt đầu", canvasGO.transform, new Vector2(200f, -400f), new Vector2(280f, 72f),
            OnStart, new Color(0.2f, 0.6f, 0.32f, 1f));
    }

    private void BuildWeaponSlot(Transform parent, WeaponType type, Vector2 pos, Vector2 size)
    {
        GameObject slot = MakeRect("Slot_" + type, parent, size, pos);
        Image frame = slot.AddComponent<Image>();
        ApplyWeaponSlotFrame(frame, type, false);
        Button btn = slot.AddComponent<Button>();
        btn.targetGraphic = frame;
        WeaponType captured = type;
        btn.onClick.AddListener(() => SelectWeapon(captured));
        slotFrames[type] = frame;

        GameObject iconGo = MakeRect("Icon", slot.transform, new Vector2(120f, 120f), new Vector2(0f, 22f));
        Image icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Sprite s = WeaponIconLibrary.GetWeapon(type);
        if (s != null) { icon.sprite = s; icon.color = WeaponIconLibrary.Tint(type); }

        TMP_Text name = MakeLabel(RunLoadout.DisplayName(type), slot.transform, new Vector2(0f, -78f),
            new Vector2(190f, 40f), TextAlignmentOptions.Center);
        GameUIFont.Apply(name, GameUIFont.Role.CardStack);
        name.fontSize = 16f;
    }

    private void SelectWeapon(WeaponType type, bool playSound = true)
    {
        if (playSound)
            AudioManager.PlayUiTap();

        selected = type;
        RunLoadout.StartingWeapon = type;

        foreach (KeyValuePair<WeaponType, Image> kv in slotFrames)
            ApplyWeaponSlotFrame(kv.Value, kv.Key, kv.Key == selected);

        if (detailName != null) detailName.text = RunLoadout.DisplayName(type);
        if (detailDesc != null) detailDesc.text = RunLoadout.Description(type);
        if (detailIcon != null)
        {
            Sprite s = WeaponIconLibrary.GetWeapon(type);
            if (s != null) { detailIcon.sprite = s; detailIcon.color = WeaponIconLibrary.Tint(type); detailIcon.enabled = true; }
        }

        if (detailIcon != null && detailIcon.transform.parent != null)
        {
            Image frame = detailIcon.transform.parent.GetComponent<Image>();
            if (frame != null)
                GuiArtLibrary.ApplyCardFrame(frame, GuiArtLibrary.CardFrameForWeapon(type), SlotSelected);
        }
    }

    private static void ApplyWeaponSlotFrame(Image frame, WeaponType type, bool selected)
    {
        Sprite sprite = selected ? GuiArtLibrary.CardSelected : GuiArtLibrary.CardFrameForWeapon(type);
        Color fallback = selected ? SlotSelected : SlotNormal;
        GuiArtLibrary.ApplyCardFrame(frame, sprite, fallback, selected);
    }

    private void OnStart()
    {
        AudioManager.PlayUiTap();
        RunLoadout.StartingWeapon = selected;
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            Debug.LogError("[WeaponSelectUI] Không tìm thấy SampleScene trong Build Settings.");
    }

    private void OnBack()
    {
        AudioManager.PlayUiTap();
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("CharacterSelectScene"))
            SceneManager.LoadScene("CharacterSelectScene");
    }

    // ── Helpers (giữ pattern CharacterSelectUI) ───────────────────────────────

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
        Sprite guiBtn = label.Contains("Bắt đầu", System.StringComparison.OrdinalIgnoreCase)
            ? GuiArtLibrary.ButtonPrimary
            : GuiArtLibrary.ButtonSecondary;
        if (!GuiArtLibrary.ApplyButton(bg, guiBtn, bgColor))
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
