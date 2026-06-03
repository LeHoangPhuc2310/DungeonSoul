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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Update()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        // Fallback for projects where Vertical axis mapping is broken/missing.
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

        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();
    }

    private void FixedUpdate()
    {
        if (moveInput.x != 0f && spriteRenderer != null)
            spriteRenderer.flipX = moveInput.x < 0f;

        Vector2 nextPosition = rb.position + moveInput * (moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);
    }
}
