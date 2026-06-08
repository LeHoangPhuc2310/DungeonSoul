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

        projectileCollider.enabled = true;
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        if (!isActiveProjectile)
            return;

        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
            Release();
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void FixedUpdate()
    {
        if (isActiveProjectile && rb != null)
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

            enemyHealth.TakeDamage(finalDamage, isCrit);
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

        Release();
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

    public void Apply(float damagePerSecond, float seconds)
    {
        health = GetComponent<HealthSystem>();
        dps = damagePerSecond;
        duration = seconds;
    }

    private void Update()
    {
        if (health == null || duration <= 0f)
        {
            Destroy(this);
            return;
        }

        duration -= Time.deltaTime;
        health.TakeDamage(dps * Time.deltaTime);
    }
}
