using System.Collections;
using UnityEngine;

/// <summary>Passive tick behaviors for legendary / epic skills.</summary>
public class SkillBehaviors : MonoBehaviour
{
    private PlayerSkillHandler handler;
    private HealthSystem health;
    private PlayerSkillStats stats;
    private Transform cachedTransform;
    private float ghostTimer;
    private float timeFreezeTimer;
    private float dragonStrikeTimer;
    private float iceAuraVfxTimer;
    private float bladeStormTimer;
    private bool ghostActive;
    private MirrorImageClone mirrorClone;
    private readonly Collider2D[] iceAuraBuffer = new Collider2D[32];
    private readonly Collider2D[] bladeStormBuffer = new Collider2D[24];

    private void Awake()
    {
        cachedTransform = transform;
        handler = GetComponent<PlayerSkillHandler>();
        health = GetComponent<HealthSystem>();
        stats = GetComponent<PlayerSkillStats>();
    }

    private void OnEnable()
    {
        EventBus.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EventBus.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled(EnemyKilledInfo info)
    {
        OnEnemyKilled(info.Position, info.MaxHp);
    }

    private void Update()
    {
        if (handler == null)
            return;

        TickGhostForm();
        TickTimeFreeze();
        TickDragonStrike();
        TickIceAura();
        TickBladeStorm();
        TickMirrorImage();
    }

    public void OnPlayerDealtDamage(float damage, HealthSystem target)
    {
        if (health != null && stats != null && stats.LifeStealPercent > 0f)
            health.Heal(damage * stats.LifeStealPercent);
    }

    public void OnEnemyKilled(Vector3 position, float maxHp)
    {
        if (stats == null)
            return;

        if (stats.ExplosionOnKillRadius > 0f)
        {
            ExplodeAt(position, stats.ExplosionOnKillRadius, maxHp * stats.ExplosionOnKillDamageRatio);
            SkillVfxLibrary.PlayForSkill(SkillType.Explosion, position, Mathf.Min(stats.ExplosionOnKillRadius, 2.4f));
        }

        if (handler.HasSkill(SkillType.Vampire) && health != null)
        {
            int stack = handler.GetStack(SkillType.Vampire);
            float healPct = stack >= 2 ? 0.08f : 0.05f;
            health.Heal(health.MaxHP * healPct);
            SkillVfxLibrary.PlayForSkill(SkillType.Vampire, health.transform.position, 1f);
        }

        if (stats.PoisonCloudRadius > 0f)
            PoisonCloudZone.TrySpawn(position, stats.PoisonCloudRadius, stats.PoisonCloudDps, 5f);

        if (handler.HasSkill(SkillType.SoulHarvest) && Random.value < 0.3f)
        {
            // Bỏ VFX nổ Arcane (gây vòng tròn to kẹt giữa map) — chỉ giữ orb linh hồn để nhặt.
            SpawnSoulOrb(position);
        }
    }

    private void TickGhostForm()
    {
        if (handler == null || !handler.HasSkill(SkillType.GhostForm))
            return;

        ghostTimer += Time.deltaTime;
        int stack = handler.GetStack(SkillType.GhostForm);
        float interval = 15f;
        float duration = stack >= 2 ? 3.5f : 2.5f;

        if (!ghostActive && ghostTimer >= interval)
        {
            ghostActive = true;
            ghostTimer = 0f;
            SkillVfxLibrary.PlayForSkill(SkillType.GhostForm, cachedTransform.position, 1.35f);
            StartCoroutine(GhostRoutine(duration));
        }
    }

    private IEnumerator GhostRoutine(float duration)
    {
        if (health != null)
            health.SetInvulnerable(true);
        yield return new WaitForSeconds(duration);
        if (health != null)
            health.SetInvulnerable(false);
        ghostActive = false;
    }

    private void TickTimeFreeze()
    {
        if (!handler.HasSkill(SkillType.TimeFreeze))
            return;

        timeFreezeTimer += Time.deltaTime;
        if (timeFreezeTimer < 30f)
            return;

        timeFreezeTimer = 0f;
        SkillVfxLibrary.PlayForSkill(SkillType.TimeFreeze, cachedTransform.position, 1.4f);
        StartCoroutine(FreezeAllEnemies(2.5f));
    }

    private IEnumerator FreezeAllEnemies(float duration)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        EnemyAI[] ais = new EnemyAI[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            ais[i] = enemies[i].GetComponent<EnemyAI>();
            if (ais[i] != null)
                ais[i].enabled = false;
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < ais.Length; i++)
        {
            if (ais[i] != null)
                ais[i].enabled = true;
        }
    }

    private void TickDragonStrike()
    {
        if (!handler.HasSkill(SkillType.DragonStrike))
            return;

        dragonStrikeTimer += Time.deltaTime;
        if (dragonStrikeTimer < 8f)
            return;

        dragonStrikeTimer = 0f;
        Transform target = FindNearestEnemy(12f);
        if (target == null)
            return;

        HealthSystem hs = target.GetComponent<HealthSystem>();
        if (hs != null)
        {
            hs.TakeDamage(150f);
            SkillVfxLibrary.PlayForSkill(SkillType.DragonStrike, target.position, 1.3f);
            AudioManager.PlayBossAttack();
        }
    }

    private void TickBladeStorm()
    {
        if (handler == null || !handler.HasSkill(SkillType.BladeStorm))
            return;

        int stack = handler.GetStack(SkillType.BladeStorm);
        float interval = stack >= 2 ? 1.4f : stack >= 1 ? 1.8f : 2.2f;
        bladeStormTimer += Time.deltaTime;
        if (bladeStormTimer < interval)
            return;

        bladeStormTimer = 0f;
        float radius = 1.6f + stack * 0.25f;
        float damage = 10f + stack * 6f;
        int hits = DamageEnemiesInRadius(cachedTransform.position, radius, damage, bladeStormBuffer);
        if (hits > 0)
        {
            AudioManager.PlaySwordSwing();
            SkillVfxLibrary.PlayForSkill(SkillType.BladeStorm, cachedTransform.position, 1.1f + stack * 0.08f);
        }
    }

    private int DamageEnemiesInRadius(Vector3 center, float radius, float damage, Collider2D[] buffer)
    {
        int count = Physics2D.OverlapCircleNonAlloc(center, radius, buffer);
        int hits = 0;
        for (int i = 0; i < count; i++)
        {
            if (buffer[i] == null || !buffer[i].CompareTag("Enemy"))
                continue;

            HealthSystem hs = buffer[i].GetComponent<HealthSystem>();
            if (hs == null)
                continue;

            hs.TakeDamage(damage, false, (Vector2)center);
            hits++;
            OnPlayerDealtDamage(damage, hs);
        }

        return hits;
    }

    private void TickIceAura()
    {
        if (stats == null || stats.SlowAuraRadius <= 0f)
            return;

        iceAuraVfxTimer += Time.deltaTime;
        if (iceAuraVfxTimer >= 4f)
        {
            iceAuraVfxTimer = 0f;
            // Aura nhỏ gọn — chỉ gợi ý phạm vi, không choáng màn hình.
            SkillVfxLibrary.PlayForSkill(SkillType.IceAura, cachedTransform.position,
                Mathf.Min(stats.SlowAuraRadius * 0.5f, 1.3f));
        }

        int count = Physics2D.OverlapCircleNonAlloc(cachedTransform.position, stats.SlowAuraRadius, iceAuraBuffer);
        for (int i = 0; i < count; i++)
        {
            if (!iceAuraBuffer[i].CompareTag("Enemy"))
                continue;
            EnemyAI ai = iceAuraBuffer[i].GetComponent<EnemyAI>();
            if (ai != null)
                ai.ApplySlow(stats.SlowAuraStrength, 0.25f);
        }
    }

    // MirrorImage: duy trì 1 phân thân auto-attack do code spawn (visual copy sprite player lúc runtime).
    private void TickMirrorImage()
    {
        bool wants = handler.HasSkill(SkillType.MirrorImage);
        if (!wants)
        {
            if (mirrorClone != null)
            {
                Destroy(mirrorClone.gameObject);
                mirrorClone = null;
            }
            return;
        }

        if (mirrorClone != null)
            return;

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("MirrorImageClone"));
        go.transform.position = cachedTransform.position + new Vector3(-0.9f, 0.4f, 0f);
        mirrorClone = go.AddComponent<MirrorImageClone>();
        mirrorClone.Initialize(cachedTransform, GetComponent<AutoAttack>());
        SkillVfxLibrary.PlayForSkill(SkillType.MirrorImage, go.transform.position, 1.25f);
    }

    private static void ExplodeAt(Vector3 position, float radius, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy"))
                continue;
            HealthSystem hs = hits[i].GetComponent<HealthSystem>();
            if (hs != null)
                hs.TakeDamage(damage);
        }
    }

    private void SpawnSoulOrb(Vector3 position)
    {
        GameObject orb = RuntimeSpawnGuard.Mark(new GameObject("SoulOrb"));
        orb.transform.position = position;
        CircleCollider2D col = orb.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;
        SoulOrbPickup pickup = orb.AddComponent<SoulOrbPickup>();
        pickup.Initialize(health, 10f, 0.05f, 5f);
        SoulOrbVisual.Attach(orb);
    }

    private Transform FindNearestEnemy(float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(cachedTransform.position, range);
        Transform best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy"))
                continue;
            float d = (hits[i].transform.position - cachedTransform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = hits[i].transform;
            }
        }
        return best;
    }
}

public class SoulOrbPickup : MonoBehaviour
{
    private HealthSystem playerHealth;
    private float heal;
    private float damageBonus;
    private float duration;
    private float life = 8f;

    public void Initialize(HealthSystem player, float healAmount, float dmgBonus, float buffDuration)
    {
        playerHealth = player;
        heal = healAmount;
        damageBonus = dmgBonus;
        duration = buffDuration;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        if (playerHealth != null)
            playerHealth.Heal(heal);
        AutoAttack atk = other.GetComponent<AutoAttack>();
        if (atk != null)
            StartCoroutine(TempDamageBoost(atk));
        Destroy(gameObject);
    }

    private IEnumerator TempDamageBoost(AutoAttack atk)
    {
        float original = atk.ProjectileDamage;
        atk.ProjectileDamage += original * damageBonus;
        yield return new WaitForSeconds(duration);
        atk.ProjectileDamage = original;
    }
}
