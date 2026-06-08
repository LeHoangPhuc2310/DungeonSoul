// DungeonSoul — PauseMenuUI.cs — Menu tạm dừng dạng card gọn, chuyên nghiệp.

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    private static readonly Color OverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.82f);
    private static readonly Color PanelBg = new Color(0.12f, 0.13f, 0.19f, 1f);
    private static readonly Color PanelBorder = new Color(0.32f, 0.36f, 0.5f, 1f);
    private static readonly Color Divider = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color BtnPrimary = new Color(0.22f, 0.62f, 0.4f, 1f);
    private static readonly Color BtnSecondary = new Color(0.2f, 0.23f, 0.33f, 1f);
    private static readonly Color BtnDanger = new Color(0.5f, 0.22f, 0.26f, 1f);
    private static readonly Color HeroNormal = new Color(0.17f, 0.19f, 0.28f, 1f);
    private static readonly Color HeroSelected = new Color(0.26f, 0.42f, 0.7f, 1f);
    private static readonly Color Gold = new Color(1f, 0.86f, 0.36f, 1f);

    private static Sprite whiteSprite;

    private Canvas canvas;
    private bool paused;
    private Image warriorFrame;
    private Image rangerFrame;
    private Image mageFrame;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        DestroyOldCanvas();
        BuildUI();
        Hide();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    public void Toggle()
    {
        if (paused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (canvas == null)
            BuildUI();
        paused = true;
        canvas.gameObject.SetActive(true);
        canvas.transform.SetAsLastSibling();
        RefreshHeroSelection();
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        AudioManager.PlayUiTap();
        paused = false;
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Hide()
    {
        paused = false;
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void DestroyOldCanvas()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
            canvas = null;
        }

        GameObject old = GameObject.Find("PauseCanvas");
        if (old != null)
            Destroy(old);
    }

    private void BuildUI()
    {
        if (canvas != null)
            return;

        GameObject canvasGo = new GameObject("PauseCanvas");
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        Image overlayImg = CreateStretch("Overlay", canvasGo.transform).AddComponent<Image>();
        overlayImg.color = OverlayColor;
        overlayImg.raycastTarget = true;

        // Centered card with a thin border frame.
        GameObject frame = new GameObject("CardFrame", typeof(RectTransform));
        frame.transform.SetParent(canvasGo.transform, false);
        RectTransform frameRt = frame.GetComponent<RectTransform>();
        frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
        frameRt.pivot = new Vector2(0.5f, 0.5f);
        frameRt.anchoredPosition = Vector2.zero;
        frameRt.sizeDelta = new Vector2(760f, 880f);
        Image frameImg = frame.AddComponent<Image>();
        if (!GuiArtLibrary.ApplyPanel(frameImg, GuiArtLibrary.MenuPanel))
        {
            frameImg.sprite = GetWhiteSprite();
            frameImg.type = Image.Type.Simple;
            frameImg.color = PanelBorder;
        }

        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(frame.transform, false);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(3f, 3f);
        panelRt.offsetMax = new Vector2(-3f, -3f);
        Image panelImg = panel.AddComponent<Image>();
        if (!GuiArtLibrary.ApplyPanel(panelImg, GuiArtLibrary.DialogPanel))
        {
            panelImg.sprite = GetWhiteSprite();
            panelImg.type = Image.Type.Simple;
            panelImg.color = PanelBg;
        }

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 36, 36);
        vlg.spacing = 16f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        BuildTitle(panel.transform);
        AddDivider(panel.transform);
        BuildHeroList(panel.transform);
        AddDivider(panel.transform);
        BuildActions(panel.transform);

        canvas.gameObject.SetActive(false);
    }

    private void BuildTitle(Transform parent)
    {
        GameObject block = Block(parent, 74f);
        TextMeshProUGUI title = block.AddComponent<TextMeshProUGUI>();
        StyleLabel(title, "TẠM DỪNG", 60f, Gold, FontStyles.Bold);

        GameObject sub = Block(parent, 36f);
        TextMeshProUGUI subtitle = sub.AddComponent<TextMeshProUGUI>();
        StyleLabel(subtitle, "Chọn nhân vật", 28f, new Color(0.78f, 0.82f, 0.9f, 1f), FontStyles.Normal);
    }

    private void BuildHeroList(Transform parent)
    {
        GameObject section = Block(parent, 0f);
        LayoutElement sectionLe = section.GetComponent<LayoutElement>();
        sectionLe.minHeight = 120f;
        sectionLe.preferredHeight = 120f;

        HorizontalLayoutGroup h = section.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 14f;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        warriorFrame = HeroChip(section.transform, "Chiến binh", HeroType.Warrior);
        rangerFrame = HeroChip(section.transform, "Hiệp sĩ", HeroType.Ranger);
        mageFrame = HeroChip(section.transform, "Pháp sư", HeroType.Mage);
    }

    private Image HeroChip(Transform parent, string label, HeroType hero)
    {
        GameObject chip = new GameObject("HeroChip", typeof(RectTransform));
        chip.transform.SetParent(parent, false);
        Image bg = chip.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.type = Image.Type.Simple;
        bg.color = HeroNormal;

        Button btn = chip.AddComponent<Button>();
        btn.targetGraphic = bg;
        HeroType captured = hero;
        btn.onClick.AddListener(() =>
        {
            AudioManager.PlayUiTap();
            HeroRunStats.SetHero(captured);
            RefreshHeroSelection();
        });

        GameObject textGo = CreateStretch("Label", chip.transform);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        StyleLabel(tmp, label, 26f, Color.white, FontStyles.Bold);
        return bg;
    }

    private void BuildActions(Transform parent)
    {
        GameObject section = Block(parent, 0f);
        LayoutElement le = section.GetComponent<LayoutElement>();
        le.minHeight = 250f;
        le.flexibleHeight = 1f;

        VerticalLayoutGroup v = section.AddComponent<VerticalLayoutGroup>();
        v.spacing = 14f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        ActionBtn(section.transform, "TIẾP TỤC", Resume, GuiArtLibrary.ButtonPrimary, BtnPrimary, 78f, 32f);
        ActionBtn(section.transform, "CỬA HÀNG", OpenMetaShop, GuiArtLibrary.ButtonSecondary, BtnSecondary, 70f, 28f);
        ActionBtn(section.transform, "VỀ MENU", QuitToScene, GuiArtLibrary.ButtonDanger, BtnDanger, 70f, 28f);
    }

    private void ActionBtn(Transform parent, string label, UnityEngine.Events.UnityAction action, Sprite guiSprite,
        Color bgColor, float height, float fontSize)
    {
        GameObject go = Block(parent, height);
        Image bg = go.AddComponent<Image>();
        if (!GuiArtLibrary.ApplyButton(bg, guiSprite, bgColor))
        {
            bg.sprite = GetWhiteSprite();
            bg.type = Image.Type.Simple;
            bg.color = bgColor;
        }

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(action);
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        GameObject textGo = CreateStretch("Text", go.transform);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        StyleLabel(tmp, label, fontSize, Color.white, FontStyles.Bold);
    }

    private void AddDivider(Transform parent)
    {
        GameObject go = Block(parent, 2f);
        Image img = go.AddComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = Divider;
        img.raycastTarget = false;
    }

    private void RefreshHeroSelection()
    {
        HeroType selected = HeroRunStats.Instance != null
            ? HeroRunStats.Instance.SelectedHero
            : (HeroType)Mathf.Clamp(PlayerPrefs.GetInt("ds_selected_hero", 0), 0, 2);

        SetHeroHighlight(warriorFrame, selected == HeroType.Warrior);
        SetHeroHighlight(rangerFrame, selected == HeroType.Ranger);
        SetHeroHighlight(mageFrame, selected == HeroType.Mage);
    }

    private static void SetHeroHighlight(Image bg, bool on)
    {
        if (bg == null)
            return;
        bg.color = on ? HeroSelected : HeroNormal;
    }

    private static void StyleLabel(TextMeshProUGUI tmp, string text, float size, Color color, FontStyles style)
    {
        GameUIFont.Apply(tmp, GameUIFont.Role.Button);
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontSizeMin = size;
        tmp.fontSizeMax = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.enableWordWrapping = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        tmp.margin = Vector4.zero;
        tmp.ForceMeshUpdate();
    }

    private void OpenMetaShop()
    {
        AudioManager.PlayUiTap();
        MetaShopUI shop = MetaShopUI.Instance;
        if (shop == null)
        {
            GameObject go = new GameObject("MetaShopUI");
            shop = go.AddComponent<MetaShopUI>();
        }

        // Ẩn card pause (không bật lại timeScale) rồi mở shop; shop sẽ gọi ReopenFromShop khi đóng.
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        shop.ShowFromPause(this);
    }

    /// <summary>MetaShopUI gọi lại khi người chơi đóng shop để quay về card pause.</summary>
    public void ReopenFromShop()
    {
        if (!paused)
            return;
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        canvas.transform.SetAsLastSibling();
        RefreshHeroSelection();
        Time.timeScale = 0f;
    }

    private static void QuitToScene()
    {
        AudioManager.PlayUiTap();
        Time.timeScale = 1f;

        // Về MainMenu nếu có, không thì về màn chọn nhân vật; cuối cùng mới reload scene hiện tại.
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
        else if (Application.CanStreamedLevelBeLoaded("CharacterSelectScene"))
            SceneManager.LoadScene("CharacterSelectScene");
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private static GameObject CreateStretch(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    private static GameObject Block(Transform parent, float height)
    {
        GameObject go = new GameObject("Block", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = height > 0f ? height : 10f;
        if (height > 0f)
            le.preferredHeight = height;
        le.flexibleWidth = 1f;
        return go;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        const int size = 8;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return whiteSprite;
    }
}
