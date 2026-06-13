// DungeonSoul — RuntimeSpawnGuardEditorCleanup.cs — Phần editor của RuntimeSpawnGuard:
// 1. Trước khi scene được lưu: tạm gắn HideFlags.DontSaveInEditor cho mọi object mang RuntimeSpawnedTag
//    để chúng KHÔNG vào file scene (kể cả khi Save giữa Play mode), lưu xong gỡ cờ ngay —
//    object vẫn bị destroy bình thường khi đổi scene/thoát Play nên không leak.
// 2. Khi chuyển play↔edit: dọn object runtime còn sót (kể cả orphan cũ mang cờ DontSaveInEditor
//    vĩnh viễn từ phiên trước — chúng có go.scene rỗng nên phải lọc bằng EditorUtility.IsPersistent).

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class RuntimeSpawnGuardEditorCleanup
{
    private static readonly List<GameObject> hiddenDuringSave = new List<GameObject>(256);

    static RuntimeSpawnGuardEditorCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorSceneManager.sceneSaved += OnSceneSaved;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        hiddenDuringSave.Clear();
        RuntimeSpawnedTag[] tags = Object.FindObjectsByType<RuntimeSpawnedTag>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < tags.Length; i++)
        {
            GameObject go = tags[i] != null ? tags[i].gameObject : null;
            if (go == null || (go.hideFlags & HideFlags.DontSaveInEditor) != 0)
                continue;

            go.hideFlags |= HideFlags.DontSaveInEditor;
            hiddenDuringSave.Add(go);
        }

        if (hiddenDuringSave.Count > 0)
            Debug.Log($"[RuntimeSpawnGuard] Loại {hiddenDuringSave.Count} object runtime khỏi lần lưu scene '{scene.name}'.");
    }

    private static void OnSceneSaved(Scene scene)
    {
        for (int i = 0; i < hiddenDuringSave.Count; i++)
        {
            GameObject go = hiddenDuringSave[i];
            if (go != null)
                go.hideFlags &= ~HideFlags.DontSaveInEditor;
        }

        hiddenDuringSave.Clear();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Dọn cả 2 chiều: trước khi vào Play (orphan không được lọt vào gameplay)
        // và sau khi về Edit (không tích tụ rác trong editor).
        if (state != PlayModeStateChange.EnteredEditMode && state != PlayModeStateChange.ExitingEditMode)
            return;

        int purged = PurgeLeakedRuntimeObjects();
        if (purged > 0)
            Debug.Log($"[RuntimeSpawnGuard] Đã dọn {purged} object runtime leak trong editor ({state}).");
    }

    private static int PurgeLeakedRuntimeObjects()
    {
        int purged = 0;
        // PHẢI dùng Resources.FindObjectsOfTypeAll: FindObjectsByType bỏ qua object
        // mang cờ DontSave — chính là các orphan cần dọn.
        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || EditorUtility.IsPersistent(go))
                continue;

            // Object hệ thống của Unity (vd InternalIdentityTransform) cũng mang cờ DontSave
            // nhưng kèm HideInHierarchy — tuyệt đối không đụng vào.
            if ((go.hideFlags & HideFlags.HideInHierarchy) != 0)
                continue;

            bool isLeaked =
                go.GetComponent<RuntimeSpawnedTag>() != null
                // Orphan từ cơ chế cũ: cờ vĩnh viễn + không thuộc scene nào đang mở.
                || ((go.hideFlags & HideFlags.DontSaveInEditor) != 0 && !go.scene.IsValid());

            if (!isLeaked)
                continue;

            // Chỉ destroy từ root bị leak — con cháu bị hủy kèm theo parent.
            Transform parent = go.transform.parent;
            if (parent != null && (parent.GetComponentInParent<RuntimeSpawnedTag>(true) != null
                || (parent.gameObject.hideFlags & HideFlags.DontSaveInEditor) != 0))
                continue;

            Object.DestroyImmediate(go);
            purged++;
        }

        return purged;
    }
}
