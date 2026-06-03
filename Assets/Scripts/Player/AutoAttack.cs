using UnityEngine;
using System.Collections.Generic;

public class AutoAttack : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float attackRadius = 8f;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Projectile")]
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private float critChance = 0f;
    [SerializeField] private float projectileScale = 0.2f;
    [SerializeField] private Color projectileColor = new Color(1f, 0.95f, 0.4f, 1f);
    [SerializeField] private int initialProjectilePoolSize = 10;
    [SerializeField] private int maxProjectilePoolSize = 20;

    private Transform projectileRoot;

    public float FireInterval
    {
        get => fireInterval;
        set => fireInterval = Mathf.Max(0.05f, value);
    }

    public int ProjectileCount
    {
        get => projectileCount;
        set => projectileCount = Mathf.Max(1, value);
    }

    public float CritChance
    {
        get => critChance;
        set => critChance = Mathf.Clamp01(value);
    }

    public float ProjectileDamage
    {
        get => projectileDamage;
        set => projectileDamage = Mathf.Max(0f, value);
    }

    public int MultiTargetCount
    {
        get => multiTargetCount;
        set => multiTargetCount = Mathf.Max(1, value);
    }

    public float BaseFireInterval => baseFireInterval;
    public float BaseProjectileDamage => baseProjectileDamage;

    private static Sprite cachedCircleSprite;
    private float shotCooldown;
    private float baseFireInterval = 1f;
    private float baseProjectileDamage = 10f;
    private int multiTargetCount = 1;
    private Collider2D[] playerColliders;
    private Transform cachedTransform;
    private readonly List<Projectile> projectilePool = new List<Projectile>(20);
    private readonly Collider2D[] enemyResults = new Collider2D[64];

    private void Awake()
    {
        cachedTransform = transform;
        playerColliders = GetComponentsInChildren<Collider2D>();
        baseFireInterval = fireInterval;
        baseProjectileDamage = projectileDamage;

        GameObject poolRoot = new GameObject("ProjectilePool");
        projectileRoot = poolRoot.transform;

        PrewarmProjectilePool();
    }

    private void Update()
    {
        shotCooldown -= Time.deltaTime;
        if (shotCooldown > 0f)
            return;

        Transform nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null)
            return;

        int targets = Mathf.Max(1, multiTargetCount);
        if (targets <= 1)
        {
            FireProjectile(nearestEnemy.position);
        }
        else
        {
            FireAtMultipleTargets(targets);
        }

        shotCooldown = fireInterval;
    }

    private void FireAtMultipleTargets(int count)
    {
        int found = Physics2D.OverlapCircleNonAlloc(cachedTransform.position, attackRadius, enemyResults);
        int fired = 0;
        for (int i = 0; i < found && fired < count; i++)
        {
            Collider2D col = enemyResults[i];
            if (col == null || !col.CompareTag(enemyTag))
                continue;
            FireProjectile(col.transform.position);
            fired++;
        }
    }

    private Transform FindNearestEnemy()
    {
        int count = Physics2D.OverlapCircleNonAlloc(cachedTransform.position, attackRadius, enemyResults);
        if (count <= 0)
            return null;

        float closestDistanceSqr = attackRadius * attackRadius;
        Transform closestTarget = null;
        Vector3 origin = cachedTransform.position;

        for (int i = 0; i < count; i++)
        {
            Collider2D enemyCollider = enemyResults[i];
            if (enemyCollider == null)
                continue;

            Transform enemy = enemyCollider.transform;
            if (!enemy.gameObject.activeInHierarchy || !enemy.CompareTag(enemyTag))
                continue;

            float distanceSqr = (enemy.position - origin).sqrMagnitude;
            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = enemy;
            }
        }

        return closestTarget;
    }

    private void FireProjectile(Vector3 targetPosition)
    {
        Vector2 direction = (targetPosition - cachedTransform.position);
        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();
        int shotCount = Mathf.Max(1, projectileCount);
        float spacing = projectileScale * 1.5f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        for (int i = 0; i < shotCount; i++)
        {
            float centerOffset = i - (shotCount - 1) * 0.5f;
            Vector2 spawnOffset = perpendicular * (centerOffset * spacing);
            SpawnSingleProjectile(direction, spawnOffset);
        }
    }

    private void SpawnSingleProjectile(Vector2 direction, Vector2 spawnOffset)
    {
        Projectile projectile = GetPooledProjectile();
        if (projectile == null)
            return;

        Transform projectileTransform = projectile.transform;
        projectileTransform.position = cachedTransform.position + (Vector3)spawnOffset;
        projectileTransform.localScale = Vector3.one * projectileScale;

        projectile.ConfigureVisual(GetCircleSprite(), projectileColor, 10);
        projectile.Initialize(direction * projectileSpeed, projectileLifetime, projectileDamage, this);
    }

    public void ReturnProjectile(Projectile projectile)
    {
        if (projectile == null)
            return;

        projectile.gameObject.SetActive(false);
    }

    private void PrewarmProjectilePool()
    {
        int count = Mathf.Min(maxProjectilePoolSize, Mathf.Max(0, initialProjectilePoolSize));
        for (int i = 0; i < count; i++)
            CreateProjectileObject();
    }

    private Projectile GetPooledProjectile()
    {
        for (int i = 0; i < projectilePool.Count; i++)
        {
            Projectile pooled = projectilePool[i];
            if (pooled != null && !pooled.gameObject.activeSelf)
            {
                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }

        if (projectilePool.Count < maxProjectilePoolSize)
        {
            Projectile created = CreateProjectileObject();
            if (created != null)
                created.gameObject.SetActive(true);
            return created;
        }

        return null;
    }

    private Projectile CreateProjectileObject()
    {
        GameObject projectileObject = new GameObject("AutoAttackProjectile");
        projectileObject.transform.SetParent(projectileRoot);
        projectileObject.SetActive(false);

        SpriteRenderer spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetCircleSprite();
        spriteRenderer.color = projectileColor;
        spriteRenderer.sortingOrder = 10;

        CircleCollider2D circleCollider = projectileObject.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        Rigidbody2D rb = projectileObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        Projectile projectile = projectileObject.AddComponent<Projectile>();
        projectile.CacheComponents();
        IgnorePlayerCollisions(circleCollider);
        projectilePool.Add(projectile);
        return projectile;
    }

    private void IgnorePlayerCollisions(Collider2D projectileCollider)
    {
        if (projectileCollider == null || playerColliders == null)
            return;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D playerCollider = playerColliders[i];
            if (playerCollider != null)
                Physics2D.IgnoreCollision(projectileCollider, playerCollider, true);
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null)
            return cachedCircleSprite;

        const int textureSize = 32;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = textureSize * 0.5f;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color solid = Color.white;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? solid : clear);
            }
        }

        texture.Apply();
        cachedCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize);

        return cachedCircleSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.3f, 0.3f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
