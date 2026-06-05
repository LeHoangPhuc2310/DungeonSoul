using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [Header("Di chuyển mượt")]
    [SerializeField] private float inputSmoothTime = 0.04f;
    [SerializeField] private float velocitySmoothTime = 0f;
    [Tooltip("Bán kính va chạm = tỉ lệ × chiều cao nhân vật (world units).")]
    [SerializeField] private float colliderRadiusRatio = 0.32f;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;
    private Vector2 smoothedInput;
    private Vector2 inputSmoothVelocity;
    private Vector2 velocitySmoothRef;

    private static PhysicsMaterial2D slideMaterial;

    public Vector2 VirtualJoystickInput { get; set; }

    /// <summary>Tâm hình hiển thị thực của nhân vật — điểm xuất phát đạn (bất kể pivot/scale).</summary>
    public Vector3 MuzzlePosition
    {
        get
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                return spriteRenderer.bounds.center;
            return transform.position;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        EnsureBodySpriteRenderer();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearDamping = 0f;

        bool hasSolid = false;
        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (!c.isTrigger)
            {
                hasSolid = true;
                break;
            }
        }

        if (!hasSolid)
        {
            CapsuleCollider2D col = gameObject.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.7f, 0.9f);
            col.isTrigger = false;
        }

        FitBodyCollider();
    }

    private void Start()
    {
        if (GetComponent<PlayerWeaponVisual>() == null)
            gameObject.AddComponent<PlayerWeaponVisual>();

        HeroType hero = HeroRunStats.Instance != null
            ? HeroRunStats.Instance.SelectedHero
            : (HeroType)Mathf.Clamp(PlayerPrefs.GetInt("ds_selected_hero", 0), 0, 2);
        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        if (entry != null)
            ApplyPlayableCharacter(entry);
        else
            ApplyHeroVisual(hero);

        FitBodyCollider();
    }

    private SpriteRenderer bodyRenderer;

    private void EnsureBodySpriteRenderer()
    {
        // Sprite hero được render trên child "HeroBody" để có thể căn giữa (bù pivot góc của
        // asset pack). Transform gốc của player giữ nguyên tâm — đạn vẫn bắn đúng từ tâm.
        if (bodyRenderer == null)
        {
            Transform existing = transform.Find("HeroBody");
            if (existing != null)
                bodyRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (bodyRenderer == null)
        {
            GameObject bodyGo = new GameObject("HeroBody");
            bodyGo.transform.SetParent(transform, false);
            bodyGo.transform.localPosition = Vector3.zero;
            bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
        }

        // Tắt SpriteRenderer trực tiếp trên player (nếu có) để tránh vẽ trùng/lệch.
        SpriteRenderer rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null && rootSr != bodyRenderer)
            rootSr.enabled = false;

        spriteRenderer = bodyRenderer;
        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = 25;

        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer >= 0)
            gameObject.layer = defaultLayer;
    }

    /// <summary>Đặt localPosition của HeroBody để sprite (pivot bất kỳ) hiển thị căn giữa transform.</summary>
    private void CenterBodyOnTransform(Sprite sprite)
    {
        if (bodyRenderer == null || sprite == null)
            return;

        // Vector từ pivot tới tâm sprite (đơn vị world của sprite). bounds.center đã tính theo pivot.
        Vector3 pivotToCenter = sprite.bounds.center;
        bodyRenderer.transform.localPosition = new Vector3(-pivotToCenter.x, -pivotToCenter.y, 0f);
    }

    public void ApplyPlayableCharacter(PlayableCharacterEntry entry)
    {
        if (entry == null || entry.PreviewSprite == null)
            return;

        EnsureBodySpriteRenderer();

        Sprite[] idle = entry.idle != null && entry.idle.Length > 0 ? entry.idle : entry.walk;
        Sprite[] walk = entry.walk != null && entry.walk.Length > 0 ? entry.walk : idle;
        Sprite sprite = idle != null && idle.Length > 0 ? idle[0] : entry.PreviewSprite;

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 25;

        SimpleSpriteAnimator anim = bodyRenderer.GetComponent<SimpleSpriteAnimator>();
        if (idle != null && idle.Length > 1)
        {
            if (anim == null)
                anim = bodyRenderer.gameObject.AddComponent<SimpleSpriteAnimator>();
            anim.enabled = true;
            anim.SetMoveTarget(transform);
            anim.SetAutoFlip(false);
            anim.PlayWithWalk(idle, walk, 10f);
        }
        else if (anim != null)
        {
            anim.enabled = false;
        }

        CenterBodyOnTransform(sprite);
        float scale = GameScale.ScaleFor(sprite, GameScale.PlayerHeight);
        transform.localScale = Vector3.one * Mathf.Min(scale, 4f);

        FitBodyCollider();

        PlayerWeaponVisual weaponVisual = GetComponent<PlayerWeaponVisual>();
        if (weaponVisual != null)
            weaponVisual.RefreshFromLoadout();
    }

    public void ApplyHeroVisual(HeroType hero)
    {
        EnsureBodySpriteRenderer();

        // Ưu tiên animation HeroKnight; fallback về sprite tĩnh CharacterArtLibrary.
        Sprite[] idleFrames = HeroKnightLibrary.GetHeroIdleFrames(hero);
        bool usingKnight = idleFrames != null && idleFrames.Length > 0;

        Sprite sprite = usingKnight ? idleFrames[0] : CharacterArtLibrary.GetHeroSprite(hero);
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = usingKnight ? HeroKnightLibrary.GetHeroTint(hero) : Color.white;
        spriteRenderer.sortingOrder = 25;
        spriteRenderer.maskInteraction = SpriteMaskInteraction.None;

        // Chạy animation idle/walk nếu có nhiều frame.
        SimpleSpriteAnimator anim = bodyRenderer.GetComponent<SimpleSpriteAnimator>();
        if (usingKnight && idleFrames.Length > 1)
        {
            if (anim == null)
                anim = bodyRenderer.gameObject.AddComponent<SimpleSpriteAnimator>();
            anim.SetMoveTarget(transform);          // đo di chuyển theo player (cha)
            anim.SetAutoFlip(false);                // PlayerController tự lo flipX
            anim.PlayWithWalk(idleFrames, idleFrames, 8f);
        }
        else if (anim != null)
        {
            anim.enabled = false;
        }

        // Căn hình về giữa transform để bù pivot của sprite.
        CenterBodyOnTransform(sprite);

        // Kích thước player theo bảng chuẩn GameScale — nhất quán với quái/boss.
        float scale = usingKnight
            ? GameScale.ScaleFor(sprite, GameScale.PlayerHeight)
            : HeroVisualLibrary.ResolveDisplayScale(sprite, GameScale.PlayerHeight, 2f);
        transform.localScale = Vector3.one * scale;

        FitBodyCollider();

        PlayerWeaponVisual weaponVisual = GetComponent<PlayerWeaponVisual>();
        if (weaponVisual != null)
            weaponVisual.RefreshFromLoadout();
    }

    private void FitBodyCollider()
    {
        float scale = Mathf.Max(0.01f, transform.lossyScale.x);
        float worldRadius = GameScale.PlayerHeight * colliderRadiusRatio;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            circle.radius = worldRadius / scale;
            circle.offset = Vector2.zero;
            ApplySlideMaterial(circle);
            return;
        }

        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (capsule == null)
            return;

        float worldHeight = GameScale.PlayerHeight * 0.85f;
        float worldWidth = worldRadius * 2f;
        capsule.size = new Vector2(worldWidth / scale, worldHeight / scale);
        capsule.offset = Vector2.zero;
        ApplySlideMaterial(capsule);
    }

    private static void ApplySlideMaterial(Collider2D col)
    {
        if (col == null)
            return;

        if (slideMaterial == null)
        {
            slideMaterial = new PhysicsMaterial2D("PlayerSlide")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        col.sharedMaterial = slideMaterial;
    }

    private void Update()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        if (Mathf.Approximately(moveInput.y, 0f))
        {
            if (Input.GetKey(KeyCode.W)) moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S)) moveInput.y -= 1f;
        }

        if (Mathf.Approximately(moveInput.x, 0f))
        {
            if (Input.GetKey(KeyCode.D)) moveInput.x += 1f;
            if (Input.GetKey(KeyCode.A)) moveInput.x -= 1f;
        }

        if (VirtualJoystickInput.sqrMagnitude > 0.01f)
            moveInput = VirtualJoystickInput;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        float smooth = Mathf.Max(0.02f, inputSmoothTime);
        smoothedInput = Vector2.SmoothDamp(smoothedInput, moveInput, ref inputSmoothVelocity, smooth, 20f, Time.unscaledDeltaTime);
    }

    private void FixedUpdate()
    {
        if (smoothedInput.x != 0f && spriteRenderer != null)
            spriteRenderer.flipX = smoothedInput.x < 0f;

        if (smoothedInput.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector2.zero;
            velocitySmoothRef = Vector2.zero;
            return;
        }

        Vector2 targetVelocity = smoothedInput * moveSpeed;
        if (velocitySmoothTime <= 0.001f)
            rb.linearVelocity = targetVelocity;
        else
        {
            float velSmooth = Mathf.Max(0.02f, velocitySmoothTime);
            rb.linearVelocity = Vector2.SmoothDamp(
                rb.linearVelocity, targetVelocity, ref velocitySmoothRef,
                velSmooth, moveSpeed * 3f, Time.fixedDeltaTime);
        }
    }
}
