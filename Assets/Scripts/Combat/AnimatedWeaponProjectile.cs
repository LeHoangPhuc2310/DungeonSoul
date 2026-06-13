using System.Collections.Generic;
using UnityEngine;

/// <summary>Đạn vũ khí VS — sprite animation + va chạm + VFX trúng đích.</summary>
public class AnimatedWeaponProjectile : MonoBehaviour
{
    private static readonly List<AnimatedWeaponProjectile> Pool = new List<AnimatedWeaponProjectile>(48);
    private static Transform poolRoot;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D col;
    private Rigidbody2D rb;
    private Sprite[] frames;
    private int frameIndex;
    private float frameTimer;
    private float frameDuration = 1f / 12f;
    private float damage;
    private WeaponType weaponType;
    private bool active;
    private Transform homingTarget;
    private float homingStrength = 4f;
    private float speed;
    private float lifetime;
    private readonly HashSet<int> hitIds = new HashSet<int>();
    private System.Action<Vector3> onHitWorld;
    private bool hitCallbackUsed;

    public static AnimatedWeaponProjectile Spawn(
        WeaponType weapon,
        Vector3 origin,
        Transform target,
        float hitDamage,
        float moveSpeed,
        float maxLifetime = 2.5f,
        System.Action<Vector3> onHit = null)
    {
        EnsurePoolRoot();
        AnimatedWeaponProjectile proj = GetFromPool();
        proj.Launch(weapon, origin, target, hitDamage, moveSpeed, maxLifetime, onHit);
        return proj;
    }

    private static void EnsurePoolRoot()
    {
        if (poolRoot != null)
            return;

        GameObject root = RuntimeSpawnGuard.Mark(new GameObject("WeaponProjectilePool"));
        poolRoot = root.transform;
    }

    private static AnimatedWeaponProjectile GetFromPool()
    {
        for (int i = 0; i < Pool.Count; i++)
        {
            AnimatedWeaponProjectile p = Pool[i];
            if (p != null && !p.active)
            {
                p.gameObject.SetActive(true);
                return p;
            }
        }

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("WeaponProjectile"));
        go.transform.SetParent(poolRoot, false);
        AnimatedWeaponProjectile created = go.AddComponent<AnimatedWeaponProjectile>();
        created.BuildComponents();
        Pool.Add(created);
        return created;
    }

    private void BuildComponents()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 22;
    }

    private void Launch(WeaponType weapon, Vector3 origin, Transform target, float hitDamage, float moveSpeed, float maxLifetime,
        System.Action<Vector3> onHit)
    {
        weaponType = weapon;
        damage = hitDamage;
        homingTarget = target;
        speed = Mathf.Max(2f, moveSpeed);
        lifetime = maxLifetime;
        onHitWorld = onHit;
        hitCallbackUsed = false;
        active = true;
        hitIds.Clear();
        frameIndex = 0;
        frameTimer = 0f;

        frames = WeaponVfxLibrary.GetProjectileFrames(weapon);
        frameDuration = 1f / Mathf.Max(8f, WeaponVfxLibrary.GetProjectileFps(weapon));

        transform.position = origin;
        if (frames != null && frames.Length > 0)
        {
            spriteRenderer.sprite = frames[0];
            spriteRenderer.color = WeaponVfxLibrary.GetTint(weapon);
            float scale = WeaponVfxLibrary.GetProjectileScale(weapon);
            float h = Mathf.Max(0.02f, frames[0].bounds.size.y);
            transform.localScale = Vector3.one * (scale / h);
        }

        Vector2 dir = target != null
            ? (Vector2)(target.position - origin)
            : Vector2.right;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;
        dir.Normalize();

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        rb.linearVelocity = dir * speed;
        col.enabled = true;

        WeaponVfxLibrary.PlayMuzzle(weapon, origin, angle);
    }

    private void Update()
    {
        if (!active)
            return;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Release();
            return;
        }

        TickAnimation();

        if (homingTarget != null && homingTarget.gameObject.activeInHierarchy)
        {
            Vector2 to = (Vector2)(homingTarget.position - transform.position);
            if (to.sqrMagnitude > 0.01f)
            {
                Vector2 vel = rb.linearVelocity;
                Vector2 desired = to.normalized * speed;
                rb.linearVelocity = Vector2.Lerp(vel, desired, homingStrength * Time.deltaTime);
                float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    private void TickAnimation()
    {
        if (frames == null || frames.Length <= 1 || spriteRenderer == null)
            return;

        frameTimer += Time.deltaTime;
        if (frameTimer < frameDuration)
            return;

        frameTimer -= frameDuration;
        frameIndex = (frameIndex + 1) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!active || !other.CompareTag("Enemy"))
            return;

        int id = other.gameObject.GetInstanceID();
        if (hitIds.Contains(id))
            return;
        hitIds.Add(id);

        HealthSystem hp = other.GetComponent<HealthSystem>();
        if (hp != null)
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

            hp.TakeDamage(finalDamage, isCrit);
            WeaponVfxLibrary.PlayHit(weaponType, other.transform.position, isCrit);
            SkillBehaviors behaviors = FindBehaviors();
            behaviors?.OnPlayerDealtDamage(finalDamage, hp);
            if (HUDManager.Instance != null)
                HUDManager.Instance.RegisterDamageDealt(finalDamage);
            if (!hitCallbackUsed && onHitWorld != null)
            {
                hitCallbackUsed = true;
                onHitWorld.Invoke(other.transform.position);
                onHitWorld = null;
            }
        }

        if (!WeaponVfxLibrary.Pierces(weaponType))
            Release();
    }

    private static SkillBehaviors FindBehaviors()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<SkillBehaviors>() : null;
    }

    private void Release()
    {
        if (!active)
            return;

        active = false;
        homingTarget = null;
        onHitWorld = null;
        rb.linearVelocity = Vector2.zero;
        col.enabled = false;
        gameObject.SetActive(false);
    }
}
