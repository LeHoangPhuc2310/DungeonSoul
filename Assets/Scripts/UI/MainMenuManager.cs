using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

// Drop this script on any GameObject in a "MainMenu" scene (build index 0).
// It builds the entire menu UI at runtime — no prefabs or Inspector wiring needed.
public class MainMenuManager : MonoBehaviour
{
    private GameObject settingsPanel;
    private TMP_Text playLabel;
    private TMP_Text settingsBtnLabel;
    private TMP_Text quitLabel;
    private TMP_Text soulsLabel;
    private AchievementsMenuUI achievementsMenu;

    private void Start()
    {
        Time.timeScale = 1f;
        AudioManager.EnsureExists();
        GameSettings.ApplyAudio();
        BuildUI();
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Background
        GameObject bg = MakeRect("BG", canvasGO.transform, Vector2.zero, Vector2.zero);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.10f, 1f);

        // Title
        GameObject titleGO = MakeRect("Title", canvasGO.transform, new Vector2(900f, 160f), new Vector2(0f, 200f));
        titleGO.GetComponent<RectTransform>().anchorMin =
        titleGO.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "DUNGEON SOUL";
        title.fontSize = 80f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.95f, 0.80f, 0.20f, 1f);
        title.raycastTarget = false;

        // Subtitle
        GameObject subGO = MakeRect("Sub", canvasGO.transform, new Vector2(600f, 60f), new Vector2(0f, 130f));
        subGO.GetComponent<RectTransform>().anchorMin =
        subGO.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        TextMeshProUGUI sub = subGO.AddComponent<TextMeshProUGUI>();
        sub.text = "Survive. Level Up. Conquer.";
        sub.fontSize = 28f;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = new Color(0.7f, 0.7f, 0.8f, 1f);
        sub.raycastTarget = false;

        soulsLabel = MakeText("Souls: " + MetaRunProgress.SoulPoints, canvasGO.transform,
            new Vector2(0f, 280f), 24f, new Color(0.75f, 0.85f, 1f, 1f), FontStyles.Normal);

        // Nút chính.
        playLabel = MakeButton(GameSettings.T("CHƠI", "PLAY"), canvasGO.transform, new Vector2(0f, -20f), OnPlay);
        MakeButton(GameSettings.T("THÀNH TỰU", "ACHIEVEMENTS"), canvasGO.transform, new Vector2(0f, -110f), OnAchievements);
        settingsBtnLabel = MakeButton(GameSettings.T("CÀI ĐẶT", "SETTINGS"), canvasGO.transform, new Vector2(0f, -200f), OnSettings);
        quitLabel = MakeButton(GameSettings.T("THOÁT", "QUIT"), canvasGO.transform, new Vector2(0f, -290f), OnQuit);

        achievementsMenu = gameObject.AddComponent<AchievementsMenuUI>();
        BuildSettingsPanel(canvasGO.transform);
    }

    private void OnAchievements()
    {
        if (achievementsMenu != null)
            achievementsMenu.Toggle(transform);
    }

    private void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = MakeRect("SettingsPanel", parent, new Vector2(640f, 460f), Vector2.zero);
        Image bg = settingsPanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.1f, 0.99f);
        Outline outline = settingsPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.36f, 0.25f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        Transform p = settingsPanel.transform;

        MakeText(GameSettings.T("CÀI ĐẶT", "SETTINGS"), p, new Vector2(0f, 185f), 40f,
            new Color(0.96f, 0.82f, 0.28f, 1f), FontStyles.Bold);

        // Toggle nhạc.
        MakeText(GameSettings.T("Nhạc nền", "Music"), p, new Vector2(-150f, 95f), 26f, Color.white, FontStyles.Normal, TextAlignmentOptions.Left);
        MakeToggleButton(p, new Vector2(180f, 95f), () => GameSettings.MusicOn, v => GameSettings.MusicOn = v);

        // Toggle SFX.
        MakeText(GameSettings.T("Hiệu ứng âm thanh", "Sound FX"), p, new Vector2(-150f, 30f), 26f, Color.white, FontStyles.Normal, TextAlignmentOptions.Left);
        MakeToggleButton(p, new Vector2(180f, 30f), () => GameSettings.SfxOn, v => GameSettings.SfxOn = v);

        // Ngôn ngữ.
        MakeText(GameSettings.T("Ngôn ngữ", "Language"), p, new Vector2(-150f, -40f), 26f, Color.white, FontStyles.Normal, TextAlignmentOptions.Left);
        MakeLanguageButton(p, new Vector2(180f, -40f));

        // Đóng.
        MakeButton(GameSettings.T("ĐÓNG", "CLOSE"), p, new Vector2(0f, -150f), () => settingsPanel.SetActive(false));

        settingsPanel.SetActive(false);
    }

    private void MakeToggleButton(Transform parent, Vector2 pos, System.Func<bool> getter, System.Action<bool> setter)
    {
        GameObject go = MakeRect("Toggle", parent, new Vector2(140f, 56f), pos);
        Image bg = go.AddComponent<Image>();
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject textGo = MakeRect("Text", go.transform, Vector2.zero, Vector2.zero);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24f; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        void Refresh()
        {
            bool on = getter();
            tmp.text = on ? GameSettings.T("BẬT", "ON") : GameSettings.T("TẮT", "OFF");
            tmp.color = Color.white;
            bg.color = on ? new Color(0.2f, 0.55f, 0.3f, 1f) : new Color(0.45f, 0.2f, 0.22f, 1f);
        }

        btn.onClick.AddListener(() => { setter(!getter()); Refresh(); });
        Refresh();
    }

    private void MakeLanguageButton(Transform parent, Vector2 pos)
    {
        GameObject go = MakeRect("LangBtn", parent, new Vector2(200f, 56f), pos);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.32f, 0.5f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;

        GameObject textGo = MakeRect("Text", go.transform, Vector2.zero, Vector2.zero);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24f; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        void Refresh() => tmp.text = GameSettings.Language == GameLanguage.English ? "English" : "Tiếng Việt";

        btn.onClick.AddListener(() =>
        {
            GameSettings.Language = GameSettings.Language == GameLanguage.English
                ? GameLanguage.Vietnamese : GameLanguage.English;
            Refresh();
            RefreshMenuLabels();
        });
        Refresh();
    }

    private void RefreshMenuLabels()
    {
        if (playLabel != null) playLabel.text = GameSettings.T("CHƠI", "PLAY");
        if (settingsBtnLabel != null) settingsBtnLabel.text = GameSettings.T("CÀI ĐẶT", "SETTINGS");
        if (quitLabel != null) quitLabel.text = GameSettings.T("THOÁT", "QUIT");
    }

    private void OnSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void Update()
    {
        if (soulsLabel != null)
            soulsLabel.text = "Souls: " + MetaRunProgress.SoulPoints;
    }

    private void OnPlay()
    {
        // Vào màn chọn nhân vật trước khi vào game.
        const string selectScene = "CharacterSelectScene";
        if (Application.CanStreamedLevelBeLoaded(selectScene))
            SceneManager.LoadScene(selectScene);
        else if (SceneManager.sceneCountInBuildSettings > 1)
            SceneManager.LoadScene(1); // dự phòng: scene kế tiếp
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    private static TMP_Text MakeButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeRect(label, parent, new Vector2(320f, 80f), pos);
        go.GetComponent<RectTransform>().anchorMin =
        go.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        Image bg = go.AddComponent<Image>();
        Sprite guiBtn = label.Contains("THOÁT", System.StringComparison.OrdinalIgnoreCase)
            || label.Contains("QUIT", System.StringComparison.OrdinalIgnoreCase)
            ? GuiArtLibrary.ButtonDanger
            : GuiArtLibrary.ButtonPrimary;
        if (!GuiArtLibrary.ApplyButton(bg, guiBtn, new Color(0.14f, 0.14f, 0.24f, 1f)))
            bg.color = new Color(0.14f, 0.14f, 0.24f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.24f, 0.24f, 0.44f, 1f);
        cb.pressedColor     = new Color(0.34f, 0.34f, 0.54f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        GameObject textGO = MakeRect("Text", go.transform, Vector2.zero, Vector2.zero);
        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 32f; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.raycastTarget = false;
        return tmp;
    }

    private static TMP_Text MakeText(string text, Transform parent, Vector2 pos, float size, Color color,
        FontStyles style, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = MakeRect("Txt", parent, new Vector2(360f, 50f), pos);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
        tmp.color = color; tmp.alignment = align; tmp.raycastTarget = false;
        return tmp;
    }
}
