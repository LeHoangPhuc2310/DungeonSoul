using UnityEngine;

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

    private static Sprite cachedCircleSprite;
    private float shotCooldown;
    private Collider2D[] playerColliders;

    private void Awake()
    {
        playerColliders = GetComponentsInChildren<Collider2D>();
    }

    private void Update()
    {
        shotCooldown -= Time.deltaTime;
        if (shotCooldown > 0f)
            return;

        Transform nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null)
            return;

        FireProjectile(nearestEnemy.position);
        shotCooldown = fireInterval;
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies == null || enemies.Length == 0)
            return null;

        float closestDistanceSqr = attackRadius * attackRadius;
        Transform closestTarget = null;
        Vector3 origin = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
                continue;

            float distanceSqr = (enemy.transform.position - origin).sqrMagnitude;
            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = enemy.transform;
            }
        }

        return closestTarget;
    }

    private void FireProjectile(Vector3 targetPosition)
    {
        Vector2 direction = (targetPosition - transform.position);
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
        GameObject projectileObject = new GameObject("AutoAttackProjectile");
        projectileObject.transform.position = transform.position + (Vector3)spawnOffset;
        projectileObject.transform.localScale = Vector3.one * projectileScale;

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
        projectile.Initialize(direction * projectileSpeed, projectileLifetime, projectileDamage);

        IgnorePlayerCollisions(circleCollider);
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
