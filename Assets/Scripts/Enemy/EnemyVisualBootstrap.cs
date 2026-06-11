using UnityEngine;

/// <summary>Safety net: sửa sprite placeholder nếu spawn/apply bị lỗi thứ tự script.</summary>
[DefaultExecutionOrder(250)]
public class EnemyVisualBootstrap : MonoBehaviour
{
    private void Start()
    {
        if (!CompareTag("Enemy"))
            return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (!EnemyVisualUtil.NeedsVisualFix(sr))
            return;

        EnemyArchetypeMarker marker = GetComponent<EnemyArchetypeMarker>();
        EnemyArchetype archetype = marker != null ? marker.Archetype : EnemyArchetype.Grunt;
        EnemyVisualUtil.EnsureApplied(gameObject, archetype, marker != null ? marker.AnimationSet : null);
    }
}
