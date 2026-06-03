using UnityEngine;

public class EnemyPrefabSetup : MonoBehaviour
{
    // This script ensures the Enemy has all required components
    public void Setup(GameObject enemy)
    {
        if (enemy.GetComponent<EnemyAI>() == null) enemy.AddComponent<EnemyAI>();
        if (enemy.GetComponent<HealthSystem>() == null) enemy.AddComponent<HealthSystem>();
        
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb == null) rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;

        if (enemy.GetComponent<Collider2D>() == null)
        {
            CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
        }

        // Add sprite if missing
        SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(enemy.transform);
            sr = visual.AddComponent<SpriteRenderer>();
        }
    }
}
