// DungeonSoul — EnemyVisualLibrary.cs — Load EnemyAnimationDatabase và gắn sprite quái.

using UnityEngine;

public static class EnemyVisualLibrary
{
    public const float KenneyFramePixels = 17f;
    public const float KenneySpritePpu = 100f;
    public const float TargetWorldHeight = 2.35f;

    public static float ResolveDisplayScale(float animVisualScale, float archetypeScaleMult)
    {
        float spriteWorldHeight = KenneyFramePixels / KenneySpritePpu;
        float fitScale = TargetWorldHeight / Mathf.Max(0.01f, spriteWorldHeight);
        float normalized = Mathf.Max(0.5f, animVisualScale) / 2f;
        return fitScale * normalized * Mathf.Max(0.5f, archetypeScaleMult);
    }

    private static EnemyAnimationDatabase cached;

    public static EnemyAnimationDatabase Database
    {
        get
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<EnemyAnimationDatabase>("EnemyAnimations/Database");
#if UNITY_EDITOR
            if (cached == null)
                cached = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyAnimationDatabase>(
                    "Assets/Resources/EnemyAnimations/Database.asset");
#endif
            return cached;
        }
    }

    public static void InvalidateCache() => cached = null;

    public static EnemyAnimationSet PickRandomSet(EnemyArchetype archetype)
    {
        EnemyAnimationDatabase db = Database;
        return db != null ? db.GetRandom(archetype) : null;
    }

    public static void ApplyAnimatedVisual(GameObject enemy, EnemyArchetype archetype)
    {
        ApplySet(enemy, PickRandomSet(archetype), archetype);
    }

    public static void ApplySet(GameObject enemy, EnemyAnimationSet set, EnemyArchetype archetype)
    {
        if (enemy == null || set == null)
            return;

        SimpleSpriteAnimator knightAnim = enemy.GetComponent<SimpleSpriteAnimator>();
        if (knightAnim != null)
            knightAnim.enabled = false;

        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = enemy.AddComponent<SpriteRenderer>();

        EnemySpriteAnimator anim = enemy.GetComponent<EnemySpriteAnimator>();
        if (anim == null)
            anim = enemy.AddComponent<EnemySpriteAnimator>();
        anim.enabled = true;

        if (sr != null)
        {
            sr.color = Color.white;
            sr.enabled = true;
        }

        anim.ApplySet(set);

        Sprite preview = set.PreviewSprite;
        if (sr != null && preview != null)
        {
            sr.sprite = preview;
            sr.color = Color.white;
            sr.sortingOrder = 8;
            sr.enabled = true;
        }

        if (preview != null)
            GameScale.FitEnemy(enemy.transform, preview, archetype);
        else
            EnemyVisualUtil.ApplyStaticFallback(enemy, archetype);
    }
}
