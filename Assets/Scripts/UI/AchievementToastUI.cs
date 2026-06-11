using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Toast góc trên khi mở khóa achievement.</summary>
public class AchievementToastUI : MonoBehaviour
{
    public static AchievementToastUI Instance { get; private set; }

    private Canvas canvas;
    private TMP_Text label;
    private Coroutine hideRoutine;

    public static void Show(string title, string description = null)
    {
        EnsureInstance();
        if (Instance != null)
            Instance.Display(title, description);
    }

    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("AchievementToastUI");
        DontDestroyOnLoad(go);
        go.AddComponent<AchievementToastUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
    }

    private void BuildUi()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject panel = new GameObject("Toast", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -24f);
        rt.sizeDelta = new Vector2(520f, 72f);

        Image bg = panel.GetComponent<Image>();
        bg.sprite = HUDManager.GetUiWhiteSpriteStatic();
        bg.color = new Color(0.08f, 0.1f, 0.16f, 0.94f);

        label = panel.AddComponent<TextMeshProUGUI>();
        GameUIFont.Apply(label, GameUIFont.Role.CardBody);
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.9f, 0.45f, 1f);
        label.raycastTarget = false;

        canvas.gameObject.SetActive(false);
    }

    private void Display(string title, string description)
    {
        if (label == null)
            return;

        label.text = string.IsNullOrEmpty(description)
            ? "🏆 " + title
            : "🏆 " + title + "\n" + description;

        canvas.gameObject.SetActive(true);
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfter(3.2f));
        AudioManager.PlayUiTap();
    }

    private IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        hideRoutine = null;
    }
}
