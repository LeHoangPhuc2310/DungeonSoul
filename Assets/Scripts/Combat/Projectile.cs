using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 velocity;
    private float damage;

    public void Initialize(Vector2 launchVelocity, float lifetime, float hitDamage)
    {
        EnsurePhysicsComponents();

        velocity = launchVelocity;
        damage = hitDamage;

        rb.linearVelocity = velocity;
        Destroy(gameObject, lifetime);
    }

    private void Awake()
    {
        EnsurePhysicsComponents();
    }

    private void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            HUDManager.SpawnDamageNumber(other.transform.position, damage, false);
        }

        Destroy(gameObject);
    }

    private void EnsurePhysicsComponents()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        Collider2D projectileCollider = GetComponent<Collider2D>();
        if (projectileCollider == null)
            projectileCollider = gameObject.AddComponent<CircleCollider2D>();

        projectileCollider.isTrigger = true;
    }
}
