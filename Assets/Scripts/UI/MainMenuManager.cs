using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

// Drop this script on any GameObject in a "MainMenu" scene (build index 0).
// It builds the entire menu UI at runtime — no prefabs or Inspector wiring needed.
public class MainMenuManager : MonoBehaviour
{
    private void Start()
    {
        Time.timeScale = 1f;
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

        // Buttons
        MakeButton("PLAY",  canvasGO.transform, new Vector2(0f, -30f),  OnPlay);
        MakeButton("QUIT",  canvasGO.transform, new Vector2(0f, -120f), OnQuit);
    }

    private void OnPlay()
    {
        // Game scene is expected at build index 1
        if (SceneManager.sceneCountInBuildSettings > 1)
            SceneManager.LoadScene(1);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // fallback: reload self
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

    private static void MakeButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeRect(label, parent, new Vector2(320f, 80f), pos);
        go.GetComponent<RectTransform>().anchorMin =
        go.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        Image bg = go.AddComponent<Image>();
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
    }
}
