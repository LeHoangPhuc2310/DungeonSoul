using UnityEngine;

public class LootDrop : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] private float dropChance = 0.25f;
    [SerializeField] private int bonusCoinMin = 1;
    [SerializeField] private int bonusCoinMax = 3;

    public void TryDrop(Vector3 position)
    {
        if (Random.value > dropChance)
            return;

        GameObject coin = new GameObject("CoinPickup");
        coin.transform.position = position;
        CircleCollider2D col = coin.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;
        SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = WeaponVisualLibrary.GetCircleSprite();
        sr.color = new Color(1f, 0.85f, 0.2f, 1f);
        sr.sortingOrder = 11;
        CoinPickup pickup = coin.AddComponent<CoinPickup>();
        pickup.Initialize(Random.Range(bonusCoinMin, bonusCoinMax + 1));
    }
}

public class CoinPickup : MonoBehaviour
{
    private int amount;
    private float life = 30f;

    public void Initialize(int coinAmount)
    {
        amount = Mathf.Max(1, coinAmount);
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        HUDManager.Instance?.AddCoins(amount);
        Destroy(gameObject);
    }
}
