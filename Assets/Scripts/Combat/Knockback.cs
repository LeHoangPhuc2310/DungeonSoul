using UnityEngine;

/// <summary>
/// Đẩy lùi quái khi trúng đòn. Gắn tự động khi HealthSystem nhận sát thương có vị trí nguồn.
/// Dùng Rigidbody2D.MovePosition để không phá vỡ va chạm tường, tự tắt dần theo thời gian.
/// </summary>
[DisallowMultipleComponent]
public class Knockback : MonoBehaviour
{
    private Rigidbody2D body;
    private EnemyAI enemyAI;
    private Vector2 velocity;
    private float timeRemaining;

    private const float Damping = 14f;     // tốc độ tắt dần (cao = dừng nhanh)
    private const float MaxDuration = 0.22f; // không đẩy quá lâu để quái không trôi xa

    public bool IsActive => timeRemaining > 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
    }

    /// <summary>Đẩy theo hướng từ nguồn sát thương tới quái, cường độ theo strength (world units/s).</summary>
    public static void Apply(GameObject target, Vector2 sourcePosition, float strength)
    {
        if (target == null || strength <= 0f)
            return;

        Knockback kb = target.GetComponent<Knockback>();
        if (kb == null)
            kb = target.AddComponent<Knockback>();
        kb.Push(sourcePosition, strength);
    }

    private void Push(Vector2 sourcePosition, float strength)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
            if (body == null)
                return;
        }

        Vector2 dir = body.position - sourcePosition;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Random.insideUnitCircle.normalized;
        else
            dir.Normalize();

        velocity = dir * strength;
        timeRemaining = MaxDuration;
    }

    private void FixedUpdate()
    {
        if (timeRemaining <= 0f || body == null)
            return;

        // Quái đang bị đẩy thì khựng AI lại một nhịp để cú đẩy "ăn" rõ.
        timeRemaining -= Time.fixedDeltaTime;

        Vector2 next = body.position + velocity * Time.fixedDeltaTime;
        body.MovePosition(next);

        // Tắt dần theo cấp số nhân để cú đẩy mượt.
        velocity = Vector2.Lerp(velocity, Vector2.zero, Damping * Time.fixedDeltaTime);
        if (velocity.sqrMagnitude < 0.01f)
            timeRemaining = 0f;
    }
}
