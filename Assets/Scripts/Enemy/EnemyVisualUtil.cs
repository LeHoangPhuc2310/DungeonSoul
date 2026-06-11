using UnityEngine;

/// <summary>Đảm bảo quái không hiển thị sprite placeholder Unity (Square đỏ).</summary>
public static class EnemyVisualUtil
{
    public static bool NeedsVisualFix(SpriteRenderer sr)
    {
        if (sr == null)
            return true;

        if (sr.sprite == null)
            return true;

        if (IsPlaceholderSprite(sr.sprite))
            return true;

        return sr.color.r > 0.95f && sr.color.g < 0.12f && sr.color.b < 0.12f;
    }

    public static bool IsPlaceholderSprite(Sprite sprite)
    {
        if (sprite == null)
            return true;

        string name = sprite.name;
        return name == "Square" || name == "Knob" || name == "Background";
    }

    public static void EnsureApplied(GameObject enemy, EnemyArchetype archetype, EnemyAnimationSet animSet)
    {
        if (enemy == null)
            return;

        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null && !NeedsVisualFix(sr))
            return;

        if (animSet != null)
        {
            EnemyVisualLibrary.ApplySet(enemy, animSet, archetype);
            sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null && !NeedsVisualFix(sr))
                return;
        }

        ApplyStaticFallback(enemy, archetype);
    }

    public static void ApplyStaticFallback(GameObject enemy, EnemyArchetype archetype)
    {
        if (enemy == null)
            return;

        EnemyArtKind art = archetype switch
        {
            EnemyArchetype.Runner => EnemyArtKind.Runner,
            EnemyArchetype.Brute => EnemyArtKind.Brute,
            EnemyArchetype.Elite => EnemyArtKind.Elite,
            _ => EnemyArtKind.Grunt
        };

        Color tint = archetype switch
        {
            EnemyArchetype.Runner => new Color(1f, 0.7f, 0.35f),
            EnemyArchetype.Brute => new Color(0.65f, 0.45f, 1f),
            EnemyArchetype.Elite => new Color(1f, 0.92f, 0.4f),
            _ => Color.white
        };

        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = enemy.AddComponent<SpriteRenderer>();

        Sprite sprite = ArtSpriteLibrary.GetEnemySprite(art);
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = 8;
        sr.enabled = sprite != null;

        EnemySpriteAnimator kenney = enemy.GetComponent<EnemySpriteAnimator>();
        if (kenney != null)
            kenney.enabled = false;

        if (sprite != null)
            GameScale.FitEnemy(enemy.transform, sprite, archetype);
    }
}
