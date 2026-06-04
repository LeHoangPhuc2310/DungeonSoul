using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("References (auto-built if null)")]
    [SerializeField] private TMP_Text scoreResultText;
    [SerializeField] private TMP_Text floorResultText;
    [SerializeField] private TMP_Text coinsResultText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button menuButton;

    private Canvas canvas;

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
        EnsureUI();
    }

    public void Show(int score, int floor, int coins)
    {
        EnsureUI();
        if (canvas != null)
            canvas.gameObject.SetActive(true);
        Setup(score, floor, coins);
    }

    public void Setup(int score, int floor, int coins)
    {
        EnsureUI();
        if (scoreResultText != null) scoreResultText.text = "Score: " + score;
        if (floorResultText != null) floorResultText.text = "Floor: " + floor;
        if (coinsResultText != null) coinsResultText.text = "Coins: " + coins;
    }

    private void EnsureUI()
    {
        if (canvas != null) return;

        // Use an existing canvas on this or parent first
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
            return;
        }

        // Build at runtime
        GameObject canvasGO = new GameObject("GameOverCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        canvas.gameObject.SetActive(false);

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Dark overlay
        GameObject overlay = MakeRect("Overlay", canvasGO.transform, Vector2.zero, Vector2.zero);
        RectTransform overlayRT = overlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        // Panel
        GameObject panel = MakeRect("Panel", canvasGO.transform, new Vector2(520f, 420f), Vector2.zero);
        panel.GetComponent<RectTransform>().anchorMin = panel.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f, 0.98f);

        Transform p = panel.transform;

        // Title
        MakeText("GAME OVER", p, new Vector2(0f, 160f), 46f, new Color(0.95f, 0.25f, 0.25f), FontStyles.Bold);

        // Result texts
        scoreResultText = MakeText("Score: 0", p, new Vector2(0f, 80f), 26f, Color.white);
        floorResultText = MakeText("Floor: 0", p, new Vector2(0f, 36f), 26f, Color.white);
        coinsResultText = MakeText("Coins: 0", p, new Vector2(0f, -8f), 26f, Color.white);

        // Buttons
        playAgainButton = MakeButton("PLAY AGAIN", p, new Vector2(-95f, -140f), PlayAgain);
        menuButton      = MakeButton("MENU",       p, new Vector2(95f,  -140f), GoToMenu);
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

    private static TMP_Text MakeText(string text, Transform parent, Vector2 pos, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject go = MakeRect("Lbl_" + text, parent, new Vector2(480f, 56f), pos);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeRect(label, parent, new Vector2(170f, 58f), pos);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.32f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
        cb.pressedColor     = new Color(0.4f, 0.4f, 0.6f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        GameObject textGO = MakeRect("Text", go.transform, Vector2.zero, Vector2.zero);
        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 21f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.raycastTarget = false;
        return btn;
    }

    // ── Actions ──────────────────────────────────────────────────────────────

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
