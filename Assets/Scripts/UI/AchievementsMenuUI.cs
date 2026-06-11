using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Panel danh sách achievement — mở từ Main Menu.</summary>
public class AchievementsMenuUI : MonoBehaviour
{
    private GameObject panelRoot;
    private TMP_Text listText;

    public void Toggle(Transform parent)
    {
        if (panelRoot == null)
            Build(parent);

        bool show = !panelRoot.activeSelf;
        panelRoot.SetActive(show);
        if (show)
            Refresh();
    }

    private void Build(Transform parent)
    {
        panelRoot = new GameObject("AchievementsPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(parent, false);
        RectTransform rt = panelRoot.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(720f, 520f);

        Image bg = panelRoot.GetComponent<Image>();
        bg.sprite = HUDManager.GetUiWhiteSpriteStatic();
        bg.color = new Color(0.06f, 0.07f, 0.11f, 0.98f);

        TMP_Text title = MakeLabel("THÀNH TỰU", panelRoot.transform, new Vector2(0f, 210f), 36f, FontStyles.Bold);
        title.color = new Color(0.96f, 0.82f, 0.28f, 1f);

        listText = MakeLabel("", panelRoot.transform, new Vector2(0f, -20f), 22f, FontStyles.Normal);
        RectTransform lrt = listText.rectTransform;
        lrt.sizeDelta = new Vector2(640f, 360f);
        listText.alignment = TextAlignmentOptions.TopLeft;

        Button close = MakeButton("Đóng", panelRoot.transform, new Vector2(0f, -220f), () => panelRoot.SetActive(false));
        panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        if (listText == null)
            return;

        listText.text =
            AchievementManager.GetAchievementReport()
            + "\n\n— Kỷ lục —\n"
            + "Thời gian sống sót: " + SurvivalRunManager.FormatTime(MetaRunProgress.BestSurvivalSeconds) + "\n"
            + "Kill cao nhất: " + MetaRunProgress.BestKills + "\n"
            + "Score cao nhất: " + MetaRunProgress.BestScore + "\n"
            + "Soul Points: " + MetaRunProgress.SoulPoints;
    }

    private static TMP_Text MakeLabel(string text, Transform parent, Vector2 pos, float size, FontStyles style)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(640f, 400f);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200f, 48f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.32f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        GameObject tgo = new GameObject("T", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        TMP_Text tmp = tgo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        Stretch(tgo.GetComponent<RectTransform>());
        return btn;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
