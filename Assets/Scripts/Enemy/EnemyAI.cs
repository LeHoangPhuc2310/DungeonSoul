// DungeonSoul — EnemyAI.cs — Đuổi player, tấn công cận chiến có animation (không gây sát thương từ xa).

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(EnemyPhysicsSetup))]
public class EnemyAI : MonoBehaviour
{
    [Header("Chase")]
    [Tooltip("0 = tự động theo camera. Giới hạn để quái không đuổi từ ngoài map.")]
    [SerializeField] private float detectionRadius = 0f;

    private float _resolvedDetectionRadius = -1f;

    private float EffectiveDetectionRadius
    {
        get
        {
            if (_resolvedDetectionRadius < 0f)
            {
                float camRange = GameScale.GetCombatRangeFromCamera(1.12f);
                _resolvedDetectionRadius = detectionRadius <= 0f
                    ? camRange
                    : Mathf.Min(detectionRadius, camRange * 1.25f);
            }

            return _resolvedDetectionRadius;
        }
    }
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float stopDistance = 0.48f;
    [SerializeField] private float meleeRange = 0.52f;

    [Header("Attack")]
    [SerializeField] private float contactDamage = 7f;
    [SerializeField] private float attackCooldown = 1.15f;
    [SerializeField] private string playerTag = "Player";

    [Header("Animation")]
    [SerializeField] private float bobAmplitude = 0.07f;
    [SerializeField] private float bobSpeed = 9f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SimpleSpriteAnimator knightAnimator;
    private EnemySpriteAnimator kenneyAnimator;
    private Transform player;
    private HealthSystem playerHealth;
    private float nextAttackTime;
    private float slowTimer;
    private float slowFactor = 1f;
    private float playerRefreshTimer;
    private Vector3 baseScale;
    private float animPhase;
    private bool isMoving;
    private bool isAttacking;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0.1f, value);
    }

    public float ContactDamage
    {
        get => contactDamage;
        set => contactDamage = Mathf.Max(0f, value);
    }

    public float StopDistance
    {
        get => stopDistance;
        set => stopDistance = Mathf.Max(0.28f, value);
    }

    public float MeleeRange
    {
        get => meleeRange;
        set => meleeRange = Mathf.Max(0.28f, value);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 0.01f)
            baseScale = Vector3.one;
    }

    private void Start()
    {
        CachePlayer();
        knightAnimator = GetComponent<SimpleSpriteAnimator>();
        kenneyAnimator = GetComponent<EnemySpriteAnimator>();
        meleeRange = Mathf.Min(meleeRange, stopDistance + 0.06f);
    }

    private void Update()
    {
        if (kenneyAnimator == null && knightAnimator == null)
            ApplyWalkAnimation();
    }

    private void FixedUpdate()
    {
        isMoving = false;

        if (isAttacking || (knightAnimator != null && knightAnimator.IsAttacking))
            return;

        if (slowTimer > 0f)
        {
            slowTimer -= Time.fixedDeltaTime;
            if (slowTimer <= 0f)
                slowFactor = 1f;
        }

        playerRefreshTimer -= Time.fixedDeltaTime;
        if (playerRefreshTimer <= 0f)
        {
            playerRefreshTimer = 0.75f;
            if (player == null)
                CachePlayer();
        }

        if (player == null)
            return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;

        if (dist > EffectiveDetectionRadius)
            return;

        if (dist <= meleeRange && Time.time >= nextAttackTime)
        {
            TryStartAttack(dist);
            return;
        }

        if (dist > stopDistance)
        {
            Vector2 direction = toPlayer / Mathf.Max(dist, 0.001f);
            float step = moveSpeed * slowFactor * Time.fixedDeltaTime;

            // Chặn tường: nếu hướng tới player bị tường cản, thử trượt theo X hoặc Y.
            Vector2 moveDir = ResolveWallSlide(direction, step);
            Vector2 next = rb.position + moveDir * step;

            if (Vector2.Distance(next, (Vector2)player.position) < stopDistance)
                next = (Vector2)player.position - direction * stopDistance;

            rb.MovePosition(next);
            isMoving = moveDir.sqrMagnitude > 0.0001f;
            UpdateFacing(direction);
        }
        else if (dist > 0.01f)
        {
            UpdateFacing(toPlayer / dist);
        }

        kenneyAnimator?.SetMoving(isMoving);
    }

    private const float WallProbeRadius = 0.22f;
    private static readonly RaycastHit2D[] wallHits = new RaycastHit2D[4];

    /// <summary>Trả hướng di chuyển sau khi tránh tường: nếu thẳng bị chặn, trượt theo X hoặc Y.</summary>
    private Vector2 ResolveWallSlide(Vector2 direction, float step)
    {
        if (!IsBlocked(direction, step))
            return direction;

        // Thử trượt ngang (chỉ X) rồi dọc (chỉ Y).
        Vector2 xOnly = new Vector2(direction.x, 0f).normalized;
        if (xOnly.sqrMagnitude > 0.001f && !IsBlocked(xOnly, step))
            return xOnly;

        Vector2 yOnly = new Vector2(0f, direction.y).normalized;
        if (yOnly.sqrMagnitude > 0.001f && !IsBlocked(yOnly, step))
            return yOnly;

        return Vector2.zero; // kẹt hẳn — đứng yên frame này.
    }

    private bool IsBlocked(Vector2 dir, float step)
    {
        float dist = step + WallProbeRadius;
        int count = Physics2D.CircleCastNonAlloc(rb.position, WallProbeRadius, dir, wallHits, dist);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = wallHits[i].collider;
            // Chỉ chặn bởi tường: collider rắn (không trigger), không phải player/enemy.
            if (col == null || col.isTrigger)
                continue;
            if (col.CompareTag("Player") || col.CompareTag("Enemy"))
                continue;
            return true;
        }
        return false;
    }

    private void TryStartAttack(float distToPlayer)
    {
        isAttacking = true;
        isMoving = false;

        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.position - rb.position;
            if (toPlayer.sqrMagnitude > 0.0001f)
                UpdateFacing(toPlayer.normalized);
        }

        if (knightAnimator != null && knightAnimator.PlayAttack(
                () => DealDamageIfInRange(),
                () => FinishAttack()))
            return;

        if (kenneyAnimator != null)
        {
            kenneyAnimator.SetMoving(false);
            DealDamageIfInRange();
            Invoke(nameof(FinishAttack), 0.35f);
            return;
        }

        DealDamageIfInRange();
        FinishAttack();
    }

    private void DealDamageIfInRange()
    {
        if (player == null || playerHealth == null)
            return;

        float dist = Vector2.Distance(rb.position, player.position);
        float reach = meleeRange + 0.12f;
        if (dist <= reach)
        {
            playerHealth.TakeDamage(contactDamage);
            AudioManager.PlayEnemyAttack();
        }
    }

    private void FinishAttack()
    {
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
        kenneyAnimator?.SetMoving(false);
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction.x) < 0.05f)
            return;

        spriteRenderer.flipX = direction.x < 0f;
    }

    private void ApplyWalkAnimation()
    {
        if (baseScale.sqrMagnitude < 0.01f)
            baseScale = transform.localScale;

        if (!isMoving)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 12f);
            return;
        }

        animPhase += Time.deltaTime * bobSpeed;
        float bob = 1f + Mathf.Sin(animPhase) * bobAmplitude;
        transform.localScale = baseScale * bob;
    }

    private void CachePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerHealth = playerObject.GetComponent<HealthSystem>();
    }

    public void ApplySlow(float factor, float duration)
    {
        slowFactor = Mathf.Clamp(factor, 0.1f, 1f);
        slowTimer = Mathf.Max(slowTimer, duration);
    }

    public void RefreshBaseScale()
    {
        baseScale = transform.localScale;
        EnemyPhysicsSetup physics = GetComponent<EnemyPhysicsSetup>();
        physics?.FitColliderToSprite();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, EffectiveDetectionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
