// DungeonSoul — FontSwitcher.cs — Swap legacy UI.Text to TMP + apply readable GameUIFont.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FontSwitcher : MonoBehaviour
{
    [SerializeField] private bool runOnAwake = true;
    [SerializeField] private bool includeInactive = true;

    private void Awake()
    {
        if (runOnAwake)
            ApplyAllInScene();
    }

    [ContextMenu("Apply All TMP Fonts")]
    public void ApplyAllInScene()
    {
        Text[] legacy = includeInactive
            ? Resources.FindObjectsOfTypeAll<Text>()
            : Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude);

        for (int i = 0; i < legacy.Length; i++)
        {
            if (legacy[i] == null || legacy[i].gameObject.scene.name == null)
                continue;
            ConvertLegacyText(legacy[i]);
        }

        TMP_Text[] tmps = includeInactive
            ? Resources.FindObjectsOfTypeAll<TMP_Text>()
            : Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);

        for (int i = 0; i < tmps.Length; i++)
        {
            if (tmps[i] == null)
                continue;
            GameUIFont.ApplyUiFont(tmps[i]);
            tmps[i].outlineWidth = 0f;
        }
    }

    private static void ConvertLegacyText(Text legacy)
    {
        GameObject go = legacy.gameObject;
        string content = legacy.text;
        Color color = legacy.color;
        int size = legacy.fontSize;
        FontStyle style = legacy.fontStyle;
        TextAnchor anchor = legacy.alignment;

        Object.DestroyImmediate(legacy);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();

        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        GameUIFont.ApplyUiFont(tmp);
        tmp.outlineWidth = 0f;
        tmp.alignment = AnchorToTmp(anchor);
        tmp.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.raycastTarget = false;
    }

    private static TextAlignmentOptions AnchorToTmp(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            default: return TextAlignmentOptions.BottomRight;
        }
    }
}
