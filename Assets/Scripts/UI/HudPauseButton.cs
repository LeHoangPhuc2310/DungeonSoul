using UnityEngine;
using UnityEngine.UI;

/// <summary>Pause button — top-left, away from Score/Xu (top-right).</summary>
public class HudPauseButton : MonoBehaviour
{
    private void Start()
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        Transform existing = transform.Find("PauseButton");
        if (existing == null)
            BuildButton();
        else
            LayoutPauseButton(existing as RectTransform);
    }

    private void BuildButton()
    {
        if (transform.Find("PauseButton") != null)
        {
            RefreshLayout();
            return;
        }

        GameObject go = new GameObject("PauseButton", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        LayoutPauseButton(rt);

        Image bg = go.AddComponent<Image>();
        if (!GuiArtLibrary.ApplyIcon(bg, GuiArtLibrary.IconPause))
            bg.color = new Color(0.12f, 0.14f, 0.22f, 0.9f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(OnPause);

        if (GuiArtLibrary.IconPause == null)
        {
            GameObject textGO = new GameObject("Icon", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            RectTransform trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "II";
            tmp.fontSize = 28f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }
    }

    private static void LayoutPauseButton(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(16f, -96f);
        rt.sizeDelta = new Vector2(52f, 52f);
    }

    private static void OnPause()
    {
        PauseMenuUI pause = PauseMenuUI.Instance;
        if (pause == null)
        {
            GameObject go = new GameObject("PauseMenuUI");
            pause = go.AddComponent<PauseMenuUI>();
        }
        pause.Toggle();
    }
}
