using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoAttack : MonoBehaviour
{
    public event Action<Vector2> OnProjectileFired;
    public event Action<Vector2> OnMeleeSwing;
    public Vector2 LastAimDirection { get; private set; } = Vector2.right;

    [Header("Attack style")]
    [SerializeField] private AttackStyle attackStyle = AttackStyle.Ranged;
    [Tooltip("Tầm với đòn cận chiến (world units).")]
    [SerializeField] private float meleeRange = 1.6f;
    [Tooltip("Chỉ phát animation Attack trên body khi quái trong tầm này (tầm xa vẫn bắn nhưng giữ idle/walk).")]
    [SerializeField] private float bodyAttackAnimRange = 2.4f;

    public AttackStyle Style
    {
        get => attackStyle;
        set => attackStyle = value;
    }

    [Header("Targeting")]
    [Tooltip("Tầm tìm mục tiêu. Tự giới hạn theo camera để không đánh quái ngoài màn hình.")]
    [SerializeField] private float attackRadius = 5f;
    [SerializeField] private bool clampRangeToCamera = true;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Projectile")]
    [SerializeField] private float fireInterval = 1f;
    [SerializeField] private float projectileSpeed = 12f;
    [Tooltip("Tốc độ mũi tên cung — chậm hơn đạn phép để dễ nhìn quỹ đạo.")]
    [SerializeField] private float bowProjectileSpeed = 5.5f;
    [Tooltip("Nhân tố làm chậm tốc bắn cung (1.5 = chậm hơn 50%).")]
    [SerializeField] private float bowFireIntervalMultiplier = 1.5f;
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

    [SerializeField] private float critMultiplier = 2f;
    public float CritMultiplier
    {
        get => critMultiplier;
        set => critMultiplier = Mathf.Max(1f, value);
    }

    public float ProjectileDamage
    {
        get => projectileDamage;
        set => projectileDamage = Mathf.Max(0f, value);
    }

    public void SetProjectileVisualScale(float scale)
    {
        projectileScale = Mathf.Max(0.05f, scale);
    }

    public void AddPermanentDamage(float amount)
    {
        float bonus = Mathf.Max(0f, amount);
        baseProjectileDamage += bonus;
        projectileDamage += bonus;
    }

    public void ApplyHeroBaseStats(float damage, float shotsPerSecond, float crit)
    {
        baseProjectileDamage = Mathf.Max(0f, damage);
        projectileDamage = baseProjectileDamage;
        baseFireInterval = 1f / Mathf.Max(0.1f, shotsPerSecond);
        fireInterval = baseFireInterval;
        critChance = Mathf.Clamp01(crit);
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
    private PlayerController cachedPlayer;
    private PlayerWeaponVisual cachedWeaponVisual;
    private SimpleSpriteAnimator bodyAnimator;
    private SpriteRenderer bodySpriteRenderer;
    private bool useBodyAttackAnimation;
    private float bodyAttackFps = 12f;
    private Transform pendingAttackTarget;

    /// <summary>Bật animation Attack từ Tiny RPG pack trên HeroBody (tắt overlay vũ khí riêng).</summary>
    public void ConfigureBodyAnimator(SimpleSpriteAnimator animator, bool enabled, float attackFps = 12f)
    {
        bodyAnimator = animator;
        useBodyAttackAnimation = enabled && animator != null;
        bodyAttackFps = Mathf.Max(6f, attackFps);
        CacheBodySpriteRenderer();
    }

    private void CacheBodySpriteRenderer()
    {
        if (bodySpriteRenderer != null)
            return;

        Transform heroBody = cachedTransform != null ? cachedTransform.Find("HeroBody") : null;
        if (heroBody != null)
            bodySpriteRenderer = heroBody.GetComponent<SpriteRenderer>();
    }

    /// <summary>Điểm xuất phát đạn: tâm hình nhân vật (vũ khí đã vẽ trong frame attack).</summary>
    private Vector3 FireOrigin
    {
        get
        {
            if (!useBodyAttackAnimation)
            {
                if (cachedWeaponVisual == null)
                    cachedWeaponVisual = GetComponent<PlayerWeaponVisual>();
                if (cachedWeaponVisual != null && cachedWeaponVisual.IsOverlayVisible)
                    return cachedWeaponVisual.MuzzleWorldPosition;
            }

            if (cachedPlayer == null)
                cachedPlayer = GetComponent<PlayerController>();
            return cachedPlayer != null ? cachedPlayer.MuzzlePosition : cachedTransform.position;
        }
    }

    /// <summary>Tâm để TÌM địch (luôn dùng tâm hình player, không phải mũi súng).</summary>
    private Vector3 SearchCenter
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = GetComponent<PlayerController>();
            return cachedPlayer != null ? cachedPlayer.MuzzlePosition : cachedTransform.position;
        }
    }
    private readonly List<Projectile> projectilePool = new List<Projectile>(20);
    private readonly Collider2D[] enemyResults = new Collider2D[64];

    private float EffectiveAttackRadius =>
        clampRangeToCamera
            ? Mathf.Min(attackRadius, GameScale.GetCombatRangeFromCamera(1.05f))
            : attackRadius;

    private void Awake()
    {
        cachedTransform = transform;
        playerColliders = GetComponentsInChildren<Collider2D>();
        baseFireInterval = fireInterval;
        baseProjectileDamage = projectileDamage;

        GameObject poolRoot = RuntimeSpawnGuard.Mark(new GameObject("ProjectilePool"));
        projectileRoot = poolRoot.transform;

        PrewarmProjectilePool();
    }

    private void OnDestroy()
    {
        // Pool đạn là object root độc lập — phải hủy cùng player, tránh rò rỉ qua các lần đổi scene.
        if (projectileRoot != null)
            Destroy(projectileRoot.gameObject);
    }

    private void Start()
    {
        shotCooldown = GetShotCooldownInterval();
    }

    private void Update()
    {
        shotCooldown -= Time.deltaTime;
        if (shotCooldown > 0f)
            return;

        if (useBodyAttackAnimation && bodyAnimator != null && bodyAnimator.IsAttacking)
        {
            shotCooldown = 0f;   // reset để không bắn burst sau khi animation kết thúc
            return;
        }

        if (attackStyle == AttackStyle.Melee)
        {
            Transform meleeTarget = FindNearestEnemyInFacingArc();
            if (!IsValidAttackTarget(meleeTarget))
                return;

            PerformAttack(meleeTarget);
            return;
        }

        Transform nearestEnemy = FindNearestEnemy();
        if (!IsValidAttackTarget(nearestEnemy))
            return;

        PerformAttack(nearestEnemy);
    }

    private void PerformAttack(Transform target)
    {
        if (!IsValidAttackTarget(target))
            return;

        if (ShouldPlayBodyAttackAnim(target) && TryPlayBodyAttack(target))
            return;

        if (attackStyle == AttackStyle.Melee)
        {
            MeleeAttack(target);
        }
        else
        {
            int targets = Mathf.Max(1, multiTargetCount);
            if (targets <= 1)
                FireProjectile(target.position);
            else
                FireAtMultipleTargets(targets);
        }

        shotCooldown = GetShotCooldownInterval();
    }

    private bool IsValidAttackTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy || !target.CompareTag(enemyTag))
            return false;

        HealthSystem hp = target.GetComponent<HealthSystem>();
        return hp == null || hp.CurrentHP > 0f;
    }

    private bool ShouldPlayBodyAttackAnim(Transform target)
    {
        if (!useBodyAttackAnimation || bodyAnimator == null || !IsValidAttackTarget(target))
            return false;

        float animRange = attackStyle == AttackStyle.Melee
            ? meleeRange
            : Mathf.Max(0.8f, bodyAttackAnimRange);

        return Vector2.Distance(SearchCenter, target.position) <= animRange;
    }

    private float GetShotCooldownInterval()
    {
        if (attackStyle == AttackStyle.Ranged && UsesBowArrow())
            return fireInterval * Mathf.Max(1f, bowFireIntervalMultiplier);
        return fireInterval;
    }

    private bool TryPlayBodyAttack(Transform target)
    {
        if (!ShouldPlayBodyAttackAnim(target))
            return false;

        pendingAttackTarget = target;
        Vector2 dir = attackStyle == AttackStyle.Melee
            ? GetFacingDirection()
            : AimDirectionToward(target.position);
        LastAimDirection = dir;

        if (attackStyle != AttackStyle.Melee)
            ApplyAttackFacing(dir);

        if (attackStyle == AttackStyle.Melee)
            AudioManager.PlaySwordSwing();

        bool started = bodyAnimator.PlayAttack(
            ExecuteBodyAttackHit,
            () => shotCooldown = GetShotCooldownInterval(),
            bodyAttackFps);

        if (!started)
            pendingAttackTarget = null;

        return started;
    }

    private void ExecuteBodyAttackHit()
    {
        Transform target = pendingAttackTarget;
        pendingAttackTarget = null;
        if (!IsValidAttackTarget(target))
            return;

        if (attackStyle == AttackStyle.Melee)
            MeleeAttack(target, playWeaponFx: false);
        else
            FireProjectile(target.position, playWeaponFx: false);
    }

    private Vector2 GetFacingDirection()
    {
        if (bodyAnimator != null && bodyAnimator.UsesFourDirections)
            return bodyAnimator.GetFacingDirection();

        CacheBodySpriteRenderer();
        if (bodySpriteRenderer != null)
            return bodySpriteRenderer.flipX ? Vector2.left : Vector2.right;

        if (LastAimDirection.sqrMagnitude > 0.01f)
            return LastAimDirection.x < 0f ? Vector2.left : Vector2.right;

        return Vector2.right;
    }

    private void ApplyAttackFacing(Vector2 dir)
    {
        FaceAttackDirection(dir);
    }

    private Vector2 AimDirectionToward(Vector3 worldPosition)
    {
        Vector3 origin = SearchCenter;
        Vector2 dir = (Vector2)(worldPosition - origin);
        if (dir.sqrMagnitude < 0.0001f)
            dir = LastAimDirection.sqrMagnitude > 0.01f ? LastAimDirection : Vector2.right;
        dir.Normalize();
        return dir;
    }

    private bool IsEnemyInFacingArc(Vector3 enemyPosition, float maxRange)
    {
        Vector3 origin = SearchCenter;
        Vector2 toEnemy = (Vector2)(enemyPosition - origin);
        float distSqr = toEnemy.sqrMagnitude;
        if (distSqr < 0.0001f || distSqr > maxRange * maxRange)
            return false;

        Vector2 facing = GetFacingDirection();
        Vector2 toNorm = toEnemy / Mathf.Sqrt(distSqr);
        float arcThreshold = bodyAnimator != null && bodyAnimator.UsesFourDirections ? 0.45f : 0.2f;
        return Vector2.Dot(toNorm, facing) >= arcThreshold;
    }

    private Transform FindNearestEnemyInFacingArc()
    {
        Vector3 origin = SearchCenter;
        float maxRange = meleeRange;
        float maxRangeSqr = maxRange * maxRange;
        int count = Physics2D.OverlapCircleNonAlloc(origin, maxRange, enemyResults);
        if (count <= 0)
            return null;

        Transform closestTarget = null;
        float closestDistanceSqr = maxRangeSqr;

        for (int i = 0; i < count; i++)
        {
            Collider2D enemyCollider = enemyResults[i];
            if (enemyCollider == null)
                continue;

            Transform enemy = enemyCollider.transform;
            if (!enemy.gameObject.activeInHierarchy || !enemy.CompareTag(enemyTag))
                continue;

            if (!IsEnemyInFacingArc(enemy.position, maxRange))
                continue;

            float distanceSqr = ((Vector2)enemy.position - (Vector2)origin).sqrMagnitude;
            if (distanceSqr <= closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestTarget = enemy;
            }
        }

        return closestTarget;
    }

    private void FaceAttackDirection(Vector2 dir)
    {
        CacheBodySpriteRenderer();
        if (bodySpriteRenderer == null || Mathf.Abs(dir.x) < 0.001f)
            return;

        bodySpriteRenderer.flipX = dir.x < 0f;
    }

    /// <summary>Đòn cận chiến: chỉ trúng 1 quái phía trước theo hướng nhìn (flipX).</summary>
    private void MeleeAttack(Transform target, bool playWeaponFx = true)
    {
        if (target == null || !target.gameObject.activeInHierarchy || !target.CompareTag(enemyTag))
            return;

        if (!IsEnemyInFacingArc(target.position, meleeRange))
            return;

        Vector2 facing = GetFacingDirection();
        LastAimDirection = facing;

        if (playWeaponFx)
        {
            AudioManager.PlaySwordSwing();
            OnMeleeSwing?.Invoke(facing);
        }

        HealthSystem hs = target.GetComponent<HealthSystem>();
        if (hs == null)
            return;

        bool isCrit = critChance > 0f && UnityEngine.Random.value < critChance;
        float dmg = projectileDamage * (isCrit ? critMultiplier : 1f);
        hs.TakeDamage(dmg, isCrit, (Vector2)SearchCenter);

        if (isCrit)
            EffectLibrary.Play(EffectKind.CritImpact, target.position, 0.9f,
                new Color(1f, 0.85f, 0.4f, 1f), 26f, 24);

        AudioManager.PlayCombatHit();
    }

    private void FireAtMultipleTargets(int count)
    {
        int found = Physics2D.OverlapCircleNonAlloc(SearchCenter, EffectiveAttackRadius, enemyResults);
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
        Vector3 origin = SearchCenter;
        float radius = EffectiveAttackRadius;
        int count = Physics2D.OverlapCircleNonAlloc(origin, radius, enemyResults);
        if (count <= 0)
            return null;

        float closestDistanceSqr = radius * radius;
        Transform closestTarget = null;

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

    private void FireProjectile(Vector3 targetPosition, bool playWeaponFx = true)
    {
        if (attackStyle == AttackStyle.Melee)
            return;

        Vector3 origin = FireOrigin;
        Vector2 direction = (targetPosition - origin);
        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();
        LastAimDirection = direction;
        if (playWeaponFx)
            OnProjectileFired?.Invoke(direction);

        int shotCount = Mathf.Max(1, projectileCount);
        float spacing = projectileScale * 1.5f;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);

        bool useBowArrow = UsesBowArrow();
        if (useBowArrow)
            AudioManager.PlayArrowShot();
        else
            AudioManager.PlaySwordSwing();

        for (int i = 0; i < shotCount; i++)
        {
            float centerOffset = i - (shotCount - 1) * 0.5f;
            Vector2 spawnOffset = perpendicular * (centerOffset * spacing);
            SpawnSingleProjectile(origin, direction, spawnOffset, useBowArrow);
        }

        // TwinArrows: mũi tên follow-up delay 0.1s từ cùng điểm bắn.
        if (PlayerSkillHandler.Instance != null && PlayerSkillHandler.Instance.HasSkill(SkillType.TwinArrows))
            StartCoroutine(FireEchoShot(direction, useBowArrow));
    }

    private IEnumerator FireEchoShot(Vector2 direction, bool useBowArrow)
    {
        yield return new WaitForSeconds(0.1f);
        if (gameObject.activeInHierarchy)
            SpawnSingleProjectile(FireOrigin, direction, Vector2.zero, useBowArrow);
    }

    /// <summary>Bắn đạn từ vị trí tùy ý (MirrorImage clone) với hệ số damage riêng.</summary>
    public void FireCloneProjectile(Vector3 origin, Vector3 targetPosition, float damageMultiplier)
    {
        Vector2 direction = targetPosition - origin;
        if (direction.sqrMagnitude < 0.0001f)
            return;
        direction.Normalize();

        Projectile projectile = GetPooledProjectile();
        if (projectile == null)
            return;

        projectile.transform.position = origin;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.localScale = Vector3.one * projectileScale;
        projectile.ConfigureVisual(GetCircleSprite(), new Color(0.8f, 0.5f, 1f, 0.9f), 10);
        projectile.Initialize(direction * projectileSpeed, projectileLifetime,
            projectileDamage * Mathf.Max(0.05f, damageMultiplier), this);
    }

    private bool UsesBowArrow()
    {
        if (attackStyle != AttackStyle.Ranged)
            return false;

        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        if (entry != null && TinyRpgProjectileLibrary.CharacterUsesBow(entry.id))
            return true;

        if (HeroRunStats.Instance != null && HeroRunStats.Instance.SelectedHero == HeroType.Ranger)
            return true;

        if (WeaponManager.Instance != null)
        {
            var weapons = WeaponManager.Instance.ActiveWeapons;
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponType type = weapons[i].weaponType;
                if (type == WeaponType.IronBow || type == WeaponType.StormBow)
                    return true;
            }
        }

        return false;
    }

    private void SpawnSingleProjectile(Vector3 origin, Vector2 direction, Vector2 spawnOffset, bool useArrowSprite)
    {
        Projectile projectile = GetPooledProjectile();
        if (projectile == null)
            return;

        Transform projectileTransform = projectile.transform;
        projectileTransform.position = origin + (Vector3)spawnOffset;

        Sprite visual = useArrowSprite ? GetArrowSprite() : GetCircleSprite();
        Color tint = useArrowSprite ? Color.white : projectileColor;
        float scale = useArrowSprite
            ? TinyRpgProjectileLibrary.ScaleForArrow(visual)
            : projectileScale;
        projectileTransform.localScale = Vector3.one * scale;

        if (useArrowSprite)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectileTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            projectileTransform.rotation = Quaternion.identity;
        }

        projectile.ConfigureVisual(visual, tint, useArrowSprite ? 22 : 10);
        float speed = useArrowSprite ? bowProjectileSpeed : projectileSpeed;
        projectile.Initialize(direction * speed, projectileLifetime, projectileDamage, this);
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

    private static Sprite cachedArrowSprite;

    private static Sprite GetArrowSprite()
    {
        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        Sprite tiny = TinyRpgProjectileLibrary.GetArrowSprite(entry);
        if (tiny != null)
            return tiny;

        if (cachedArrowSprite != null)
            return cachedArrowSprite;

        cachedArrowSprite = HeroKnightLibrary.GetArrowProjectileSprite();
        return cachedArrowSprite != null ? cachedArrowSprite : GetCircleSprite();
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
        Gizmos.DrawWireSphere(SearchCenter, EffectiveAttackRadius);
    }
}
