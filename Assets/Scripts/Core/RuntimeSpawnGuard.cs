// DungeonSoul — RuntimeSpawnGuard.cs — Chặn object runtime bị serialize vào file scene
// (nguyên nhân quái ma / thanh máu lơ lửng / vệt chém giữa chỗ trống khi scene bị Save trong Play mode),
// kèm bộ quét dọn rác runtime sót lại mỗi khi load scene.
//
// Cơ chế: Mark() gắn marker RuntimeSpawnedTag (chỉ trong Editor). Hook editor
// (RuntimeSpawnGuardEditorCleanup) tạm gắn HideFlags.DontSaveInEditor cho các object này
// NGAY TRƯỚC khi scene được lưu rồi gỡ ra sau khi lưu xong — object không bao giờ vào file scene
// nhưng vẫn bị Unity destroy bình thường khi đổi scene / thoát Play (không leak như gắn cờ vĩnh viễn).

using UnityEngine;

/// <summary>Marker: object này được sinh lúc runtime, không bao giờ được lưu vào file scene.</summary>
[DisallowMultipleComponent]
public sealed class RuntimeSpawnedTag : MonoBehaviour
{
}

public static class RuntimeSpawnGuard
{
    /// <summary>Đánh dấu object sinh lúc runtime. Không ảnh hưởng gì tới build.</summary>
    public static GameObject Mark(GameObject go)
    {
#if UNITY_EDITOR
        if (go != null && go.GetComponent<RuntimeSpawnedTag>() == null)
            go.AddComponent<RuntimeSpawnedTag>();
#endif
        return go;
    }

    // Các object runtime luôn nằm ở root hierarchy — nếu xuất hiện NGAY lúc scene vừa load
    // (trước khi gameplay spawn) thì chắc chắn là rác bị lưu nhầm từ trước.
    private static readonly string[] StaleRootNames =
    {
        "Enemy(Clone)", "DmgNum", "ExpNum", "HeroKnightVfx",
        "LevelUpFloatingText", "LevelUpParticleRing", "WeaponEvolutionText"
    };

    private static readonly string[] StaleRootPrefixes = { "SkillVfx_", "VFX_" };

    /// <summary>
    /// Dọn object runtime bị lưu nhầm trong scene ngay sau khi load, trước khi gameplay bắt đầu.
    /// Quái spawn từ Start/coroutine nên mọi enemy ACTIVE tồn tại ở thời điểm này đều là rác.
    /// </summary>
    public static void PurgeStaleRuntimeObjects()
    {
        int purged = 0;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];
            if (enemy == null)
                continue;

            // Không đụng tới quái nằm trong pool (inactive, con của ObjectPooler).
            if (enemy.GetComponentInParent<ObjectPooler>(true) != null)
                continue;

            Object.Destroy(enemy);
            purged++;
        }

        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t.parent != null || !IsStaleRootName(t.name))
                continue;

            // Object vừa được spawn hợp lệ trong phiên này đã mang marker — bỏ qua.
            if (t.GetComponent<RuntimeSpawnedTag>() != null)
                continue;

            Object.Destroy(t.gameObject);
            purged++;
        }

        if (purged > 0)
            Debug.LogWarning($"[RuntimeSpawnGuard] Đã dọn {purged} object runtime bị lưu nhầm trong file scene.");
    }

    private static bool IsStaleRootName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        for (int i = 0; i < StaleRootNames.Length; i++)
        {
            if (name == StaleRootNames[i])
                return true;
        }

        for (int i = 0; i < StaleRootPrefixes.Length; i++)
        {
            if (name.StartsWith(StaleRootPrefixes[i], System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
