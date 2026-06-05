// DungeonSoul — EnemyPhysicsSetup.cs — Kinematic + trigger khớp kích thước sprite thật.

using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPhysicsSetup : MonoBehaviour
{
    [SerializeField] private float colliderBodyFraction = 0.28f;

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        EnsureTriggerCollider();
    }

    /// <summary>Gọi sau khi đã Fit scale — collider theo thân sprite, không phình theo scale.</summary>
    public void FitColliderToSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        if (circle == null)
            circle = gameObject.AddComponent<CircleCollider2D>();

        circle.isTrigger = true;

        if (sr == null || sr.sprite == null)
        {
            float s = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.001f);
            circle.radius = 0.18f / s;
            return;
        }

        // bounds = world space; radius collider = local → chia cho scale.
        float worldRadius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y) * colliderBodyFraction;
        float uniformScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.001f);
        circle.radius = Mathf.Clamp(worldRadius / uniformScale, 0.1f, 0.22f);
    }

    private void EnsureTriggerCollider()
    {
        Collider2D[] cols = GetComponents<Collider2D>();
        if (cols.Length == 0)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.18f;
            cols = new[] { circle };
        }

        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null)
                cols[i].isTrigger = true;
        }
    }
}
