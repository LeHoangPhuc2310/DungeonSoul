using System.Collections;
using UnityEngine;

/// <summary>Trả quái thường về ObjectPooler sau khi chết — boss không dùng component này.</summary>
[DisallowMultipleComponent]
public class EnemyPoolable : MonoBehaviour
{
    public const string PoolKey = "enemy_regular";

    private HealthSystem health;
    private EnemyAI ai;
    private Coroutine returnRoutine;

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
        ai = GetComponent<EnemyAI>();
    }

    public void PrepareForSpawn(int difficultyTier, EnemyArchetype archetype)
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }

        gameObject.tag = "Enemy";
        EnemyArchetypeUtility.Apply(gameObject, archetype, difficultyTier);
        EnemyArchetypeMarker marker = GetComponent<EnemyArchetypeMarker>();
        EnemyVisualUtil.EnsureApplied(gameObject, archetype, marker != null ? marker.AnimationSet : null);

        if (health != null)
            health.ResetForPool();

        if (ai != null)
            ai.enabled = true;
    }

    public void ScheduleReturn(float delaySeconds)
    {
        if (GetComponent<BossController>() != null)
            return;

        if (returnRoutine != null)
            StopCoroutine(returnRoutine);

        returnRoutine = StartCoroutine(ReturnAfter(delaySeconds));
    }

    private IEnumerator ReturnAfter(float delay)
    {
        if (ai != null)
            ai.enabled = false;

        yield return new WaitForSeconds(delay);

        returnRoutine = null;
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.Return(PoolKey, gameObject);
        else
            Destroy(gameObject);
    }
}
