using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveInput;

    // Touch/virtual joystick support (assigned at runtime by mobile UI)
    public Vector2 VirtualJoystickInput { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Guarantee a solid (non-trigger) collider so wall physics work at runtime
        bool hasSolid = false;
        foreach (Collider2D c in GetComponents<Collider2D>())
        {
            if (!c.isTrigger) { hasSolid = true; break; }
        }
        if (!hasSolid)
        {
            CapsuleCollider2D col = gameObject.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.7f, 0.9f);
            col.isTrigger = false;
        }
    }

    private void Update()
    {
        // Keyboard input
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

        // Virtual joystick overrides keyboard when active
        if (VirtualJoystickInput.sqrMagnitude > 0.01f)
            moveInput = VirtualJoystickInput;

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();
    }

    private void FixedUpdate()
    {
        if (moveInput.x != 0f && spriteRenderer != null)
            spriteRenderer.flipX = moveInput.x < 0f;

        // Use velocity instead of MovePosition so the physics solver resolves
        // wall penetration — MovePosition on a Dynamic body teleports and ignores collisions.
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
