// DungeonSoul — MobileSafeArea.cs — Padding HUD theo notch / safe area mobile.

using UnityEngine;

public class MobileSafeArea : MonoBehaviour
{
    [SerializeField] private bool applyOnStart = true;

    private void Start()
    {
        if (!applyOnStart)
            return;

        ApplyToHud();
    }

    public static void ApplyToHud()
    {
        Rect safe = Screen.safeArea;
        HUDManager mgr = HUDManager.Resolve();
        Canvas hud = mgr != null ? mgr.GetComponentInParent<Canvas>() : null;
        if (hud == null && mgr != null)
            hud = mgr.GetComponent<Canvas>();
        if (hud == null)
            return;

        RectTransform root = hud.GetComponent<RectTransform>();
        if (root == null)
            return;

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        root.anchorMin = anchorMin;
        root.anchorMax = anchorMax;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
    }
}
