using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D projectileCollider;
    private SpriteRenderer spriteRenderer;
    private Vector2 velocity;
    private float damage;
    private float lifetimeRemaining;
    private bool isActiveProjectile;
    private AutoAttack owner;
    private int pierceRemaining;
    private bool boomerang;
    private bool returning;
    private float launchSpeed;
    private Transform ownerTransform;
    private readonly HashSet<int> hitEnemyIds = new HashSet<int>();

    public void CacheComponents()
    {
        EnsurePhysicsComponents();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void ConfigureVisual(Sprite sprite, Color color, int sortingOrder)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    public void Initialize(Vector2 launchVelocity, float lifetime, float hitDamage, AutoAttack projectileOwner)
    {
        EnsurePhysicsComponents();
        owner = projectileOwner;
        hitEnemyIds.Clear();

        velocity = launchVelocity;
        damage = hitDamage;
        lifetimeRemaining = Mathf.Max(0.01f, lifetime);
        isActiveProjectile = true;

        PlayerSkillStats stats = PlayerSkillHandler.Instance != null
            ? PlayerSkillHandler.Instance.GetComponent<PlayerSkillStats>()
            : null;
        pierceRemaining = stats != null ? stats.PierceCount : 0;
        boomerang = stats != null && stats.BoomerangEnabled;
        returning = false;
        launchSpeed = launchVelocity.magnitude;
        ownerTransform = projectileOwner != null ? projectileOwner.transform : null;

        projectileCollider.enabled = true;
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        if (!isActiveProjectile)
            return;

        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
        {
            if (boomerang && !returning)
                BeginReturn();
            else
                Release();
            return;
        }

        // Boomerang đang bay về: chạm player thì thu hồi.
        if (returning && ownerTransform != null &&
            ((Vector2)(ownerTransform.position - transform.position)).sqrMagnitude < 0.25f)
            Release();
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void FixedUpdate()
    {
        if (!isActiveProjectile || rb == null)
            return;

        if (returning && ownerTransform != null)
        {
            Vector2 toOwner = (Vector2)(ownerTransform.position - transform.position);
            if (toOwner.sqrMagnitude > 0.0001f)
                velocity = toOwner.normalized * launchSpeed;
        }

        rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        int id = other.gameObject.GetInstanceID();
        if (hitEnemyIds.Contains(id))
            return;
        hitEnemyIds.Add(id);

        HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
        if (enemyHealth != null)
        {
            float finalDamage = damage;
            bool isCrit = false;
            PlayerSkillStats stats = PlayerSkillHandler.Instance != null
                ? PlayerSkillHandler.Instance.GetComponent<PlayerSkillStats>()
                : null;

            if (stats != null && stats.CritChance > 0f && Random.value < stats.CritChance)
            {
                finalDamage *= stats.CritMultiplier;
                isCrit = true;
            }

            if (stats != null && hitEnemyIds.Count > 1)
                finalDamage *= 1f - stats.PierceDamageFalloff;

            // Đẩy lùi theo hướng bay của đạn (lấy vị trí ngay phía sau điểm chạm).
            Vector2 knockSource = (Vector2)other.transform.position - velocity.normalized;
            enemyHealth.TakeDamage(finalDamage, isCrit, knockSource);
            PlayHitVfx(other.transform.position, stats, isCrit);

            if (stats != null && stats.FireDotDuration > 0f)
            {
                BurnDebuff burn = other.GetComponent<BurnDebuff>();
                if (burn == null)
                    burn = other.gameObject.AddComponent<BurnDebuff>();
                burn.Apply(stats.FireDotDamage, stats.FireDotDuration);
            }
            else if (stats != null && stats.PassiveBurnChance > 0f && Random.value < stats.PassiveBurnChance)
            {
                BurnDebuff burn = other.GetComponent<BurnDebuff>();
                if (burn == null)
                    burn = other.gameObject.AddComponent<BurnDebuff>();
                burn.Apply(4f, 2.5f);
            }

            if (stats != null && stats.ExplosionOnHitRadius > 0f)
            {
                ExplodeOnHit(other.transform.position, stats.ExplosionOnHitRadius,
                    finalDamage * stats.ExplosionOnHitDamageRatio, other.gameObject);
                SkillVfxLibrary.PlayForSkill(SkillType.ExplosiveRounds, other.transform.position,
                    stats.ExplosionOnHitRadius * 0.8f);
            }

            // LightningChain: crit luôn nhảy; đòn thường có 35% cơ hội nhảy (không cần crit nữa).
            if (stats != null && stats.ChainJumpCount > 0 && (isCrit || Random.value < 0.35f))
                LightningChainEffect.Trigger(other.transform.position, other.gameObject,
                    finalDamage, stats.ChainJumpCount, stats.ChainDamageRatio);

            if (stats != null && stats.DeathMarkEnabled)
                DeathMarkDebuff.TryApply(other.gameObject);

            SkillBehaviors behaviors = FindPlayerBehaviors();
            behaviors?.OnPlayerDealtDamage(finalDamage, enemyHealth);

            if (HUDManager.Instance != null)
                HUDManager.Instance.RegisterDamageDealt(finalDamage);
        }

        if (pierceRemaining > 0)
        {
            pierceRemaining--;
            return;
        }

        if (boomerang && !returning)
        {
            BeginReturn();
            return;
        }

        Release();
    }

    // Boomerang: quay đầu bay về player, có thể trúng lại các quái trên đường về.
    private void BeginReturn()
    {
        returning = true;
        hitEnemyIds.Clear();
        lifetimeRemaining = Mathf.Max(lifetimeRemaining, 2.5f);
        SkillVfxLibrary.PlayForSkill(SkillType.Boomerang, transform.position, 0.6f);
    }

    private static void ExplodeOnHit(Vector3 center, float radius, float damage, GameObject directHit)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || !hits[i].CompareTag("Enemy") || hits[i].gameObject == directHit)
                continue;
            HealthSystem hs = hits[i].GetComponent<HealthSystem>();
            if (hs != null)
                hs.TakeDamage(damage);
        }
    }

    private static void PlayHitVfx(Vector3 worldPos, PlayerSkillStats stats, bool isCrit)
    {
        // Chỉ hiện VFX cho đòn ĐẶC BIỆT (chí mạng) để tránh rối khi bắn liên tục.
        // Đòn thường không VFX — chỉ có số sát thương là đủ phản hồi.
        if (isCrit)
            EffectLibrary.Play(EffectKind.CritImpact, worldPos, 0.9f, new Color(1f, 0.85f, 0.4f, 1f), 26f, 24);
    }

    private static SkillBehaviors FindPlayerBehaviors()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<SkillBehaviors>() : null;
    }

    private void Release()
    {
        if (!isActiveProjectile)
            return;

        transform.rotation = Quaternion.identity;

        isActiveProjectile = false;
        velocity = Vector2.zero;
        lifetimeRemaining = 0f;
        hitEnemyIds.Clear();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (projectileCollider != null)
            projectileCollider.enabled = false;

        if (owner != null)
            owner.ReturnProjectile(this);
        else
            gameObject.SetActive(false);
    }

    private void EnsurePhysicsComponents()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        if (projectileCollider == null)
            projectileCollider = GetComponent<Collider2D>();
        if (projectileCollider == null)
            projectileCollider = gameObject.AddComponent<CircleCollider2D>();

        projectileCollider.isTrigger = true;
    }
}

public class BurnDebuff : MonoBehaviour
{
    private HealthSystem health;
    private float dps;
    private float duration;
    private SpriteRenderer flame;
    private float flicker;

    public void Apply(float damagePerSecond, float seconds)
    {
        health = GetComponent<HealthSystem>();
        dps = damagePerSecond;
        duration = Mathf.Max(duration, seconds); // refresh, không cộng dồn
        EnsureFlameVisual();
    }

    /// <summary>Ngọn lửa nhỏ bám theo quái — đốt là thấy "đang cháy", không phải cột lửa giữa map.</summary>
    private void EnsureFlameVisual()
    {
        if (flame != null)
            return;

        GameObject go = new GameObject("BurnFlame");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        flame = go.AddComponent<SpriteRenderer>();
        flame.sprite = WeaponVisualLibrary.GetCircleSprite();
        flame.color = new Color(1f, 0.5f, 0.12f, 0.85f);
        SpriteRenderer host = GetComponent<SpriteRenderer>();
        flame.sortingOrder = (host != null ? host.sortingOrder : 5) + 1;
        go.transform.localScale = Vector3.one * 0.32f;
    }

    private void Update()
    {
        if (health == null || duration <= 0f)
        {
            if (flame != null)
                Destroy(flame.gameObject);
            Destroy(this);
            return;
        }

        duration -= Time.deltaTime;
        health.TakeDamage(dps * Time.deltaTime);

        // Nhấp nháy lửa cho sống động.
        if (flame != null)
        {
            flicker += Time.deltaTime * 18f;
            float s = 0.30f + Mathf.Sin(flicker) * 0.06f;
            flame.transform.localScale = Vector3.one * s;
            float a = 0.7f + Mathf.Sin(flicker * 1.3f) * 0.2f;
            flame.color = new Color(1f, 0.5f, 0.12f, a);
        }
    }
}
