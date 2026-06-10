using UnityEngine;

public enum EnemyArchetype
{
    Grunt,
    Runner,
    Brute,
    Elite
}

public static class EnemyArchetypeUtility
{
    public static EnemyArchetype RollForWave(int waveIndex)
    {
        float roll = Random.value;
        if (waveIndex >= 5 && roll < 0.14f)
            return EnemyArchetype.Elite;
        if (waveIndex >= 3 && roll < 0.38f)
            return EnemyArchetype.Brute;
        if (roll < 0.42f)
            return EnemyArchetype.Runner;
        return EnemyArchetype.Grunt;
    }

    public static void Apply(GameObject enemy, EnemyArchetype archetype, int waveIndex)
    {
        if (enemy == null)
            return;

        GetConfig(archetype, out EnemyArtKind art, out Color tint, out float hpMult, out float speedMult,
            out float scaleMult, out int score, out int coinMin, out int coinMax, out bool elite);

        float waveHp = 1f + Mathf.Max(0, waveIndex - 1) * 0.18f;

        HealthSystem health = enemy.GetComponent<HealthSystem>();
        if (health != null)
        {
            float baseHp = health.MaxHP > 0f ? health.MaxHP : 52f;
            health.MaxHP = Mathf.Max(12f, baseHp * hpMult * waveHp);
            health.CurrentHP = health.MaxHP;
        }

        if (enemy.GetComponent<EnemyPhysicsSetup>() == null)
            enemy.AddComponent<EnemyPhysicsSetup>();

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.MoveSpeed *= speedMult;
            ai.StopDistance = archetype == EnemyArchetype.Runner ? 0.42f : archetype == EnemyArchetype.Brute ? 0.54f : 0.48f;
            ai.MeleeRange = archetype == EnemyArchetype.Runner ? 0.44f : archetype == EnemyArchetype.Brute ? 0.56f : 0.5f;
            // +8%/wave (cũ 5%): để wave 7-10 có sức ép thật, không bị player scale qua mặt.
            float waveDmg = 1f + Mathf.Max(0, waveIndex - 1) * 0.08f;
            ai.ContactDamage = (archetype == EnemyArchetype.Brute ? 9f : archetype == EnemyArchetype.Elite ? 12f : 6f) * waveDmg;
        }

        EnemyReward reward = enemy.GetComponent<EnemyReward>();
        if (reward != null)
            reward.Configure(score, coinMin, coinMax, elite);

        // Animation từ EnemyAnimationDatabase (Medusa, Golem, Orc, Tiny RPG, …).
        EnemyAnimationSet animSet = EnemyVisualLibrary.PickRandomSet(archetype);
        if (animSet != null)
        {
            EnemyVisualLibrary.ApplySet(enemy, animSet, archetype);

            ai?.RefreshBaseScale();

            EnemyPhysicsSetup physicsSetup = enemy.GetComponent<EnemyPhysicsSetup>();
            physicsSetup?.FitColliderToSprite();

            EnemyOverheadHPBar.Ensure(enemy);

            EnemyArchetypeMarker marker = enemy.GetComponent<EnemyArchetypeMarker>();
            if (marker == null)
                marker = enemy.AddComponent<EnemyArchetypeMarker>();
            marker.Set(archetype, animSet);
            return;
        }

        // Fallback sprite tĩnh.
        {
            SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = ArtSpriteLibrary.GetEnemySprite(art);
                sr.color = tint;
                sr.sortingOrder = 5;
                GameScale.FitEnemy(enemy.transform, sr.sprite, archetype);
            }
        }

        ai?.RefreshBaseScale();

        EnemyPhysicsSetup physics = enemy.GetComponent<EnemyPhysicsSetup>();
        physics?.FitColliderToSprite();

        EnemyOverheadHPBar.Ensure(enemy);

        EnemyArchetypeMarker markerFallback = enemy.GetComponent<EnemyArchetypeMarker>();
        if (markerFallback == null)
            markerFallback = enemy.AddComponent<EnemyArchetypeMarker>();
        markerFallback.Set(archetype, null);
    }

    private static void GetConfig(EnemyArchetype archetype, out EnemyArtKind art, out Color tint,
        out float hpMult, out float speedMult, out float scaleMult,
        out int score, out int coinMin, out int coinMax, out bool elite)
    {
        switch (archetype)
        {
            case EnemyArchetype.Runner:
                art = EnemyArtKind.Runner;
                tint = new Color(1f, 0.7f, 0.35f);
                hpMult = 0.9f;
                speedMult = 1.55f;
                scaleMult = 1f;
                score = 12;
                coinMin = 2;
                coinMax = 5;
                elite = false;
                break;
            case EnemyArchetype.Brute:
                art = EnemyArtKind.Brute;
                tint = new Color(0.65f, 0.45f, 1f);
                hpMult = 2.35f;
                speedMult = 0.75f;
                scaleMult = 1.25f;
                score = 18;
                coinMin = 5;
                coinMax = 9;
                elite = false;
                break;
            case EnemyArchetype.Elite:
                art = EnemyArtKind.Elite;
                tint = new Color(1f, 0.92f, 0.4f);
                hpMult = 3.1f;
                speedMult = 1.1f;
                scaleMult = 1.35f;
                score = 35;
                coinMin = 10;
                coinMax = 16;
                elite = true;
                break;
            default:
                art = EnemyArtKind.Grunt;
                tint = Color.white;
                hpMult = 1.25f;
                speedMult = 1f;
                scaleMult = 1f;
                score = 10;
                coinMin = 3;
                coinMax = 6;
                elite = false;
                break;
        }
    }
}

public class EnemyArchetypeMarker : MonoBehaviour
{
    public EnemyArchetype Archetype { get; private set; }
    public EnemyAnimationSet AnimationSet { get; private set; }

    public void Set(EnemyArchetype value, EnemyAnimationSet set)
    {
        Archetype = value;
        AnimationSet = set;
    }
}
