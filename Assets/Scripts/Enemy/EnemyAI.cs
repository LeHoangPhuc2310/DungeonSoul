using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyAI : MonoBehaviour
{
    [Header("Chase")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float damageInterval = 1.5f;
    [SerializeField] private string playerTag = "Player";

    private Rigidbody2D rb;
    private Transform player;
    private float nextDamageTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        EnsureEnemyCollider();
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude > detectionRadius * detectionRadius)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = toPlayer.normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(playerTag))
            return;

        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        TryDamagePlayer(other.gameObject);
    }

    private void TryDamagePlayer(GameObject playerObject)
    {
        if (Time.time < nextDamageTime)
            return;

        HealthSystem playerHealth = playerObject.GetComponent<HealthSystem>();
        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(contactDamage);
        nextDamageTime = Time.time + damageInterval;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private void EnsureEnemyCollider()
    {
        Collider2D enemyCollider = GetComponent<CircleCollider2D>();
        if (enemyCollider == null)
            enemyCollider = GetComponent<BoxCollider2D>();
        if (enemyCollider == null)
            enemyCollider = gameObject.AddComponent<CircleCollider2D>();

        enemyCollider.isTrigger = false;
    }
}
