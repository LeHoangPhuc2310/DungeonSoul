using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class ExpGem : MonoBehaviour
{
    public enum GemRarity
    {
        Common,
        Rare
    }

    [SerializeField] private GemRarity rarity = GemRarity.Common;
    [SerializeField] private float collectRadius = 3f;
    [SerializeField] private float moveSpeed = 7f;

    private Transform player;
    private CircleCollider2D collectCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        collectCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        collectCollider.isTrigger = true;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        ApplyVisual();
    }

    private void Update()
    {
        if (player == null)
            return;

        float magnetBonus = MagnetRadiusUpgrade.Instance != null ? MagnetRadiusUpgrade.Instance.BonusRadius : 0f;
        float activeRadius = collectRadius + magnetBonus;
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= activeRadius)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        float exp = rarity == GemRarity.Rare ? 30f : 10f;
        if (ExpSystem.Instance != null)
            ExpSystem.Instance.AddExp(exp);

        Destroy(gameObject);
    }

    public void Initialize(GemRarity gemRarity)
    {
        rarity = gemRarity;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (spriteRenderer == null)
            return;

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = WeaponVisualLibrary.GetCircleSprite();
        spriteRenderer.color = rarity == GemRarity.Rare ? new Color(0.2f, 0.55f, 1f, 1f) : new Color(0.2f, 1f, 0.3f, 1f);
        transform.localScale = Vector3.one * (rarity == GemRarity.Rare ? 0.2f : 0.16f);
    }
}

public class MagnetRadiusUpgrade : MonoBehaviour
{
    public static MagnetRadiusUpgrade Instance { get; private set; }
    public float BonusRadius { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SetBonus(float amount)
    {
        BonusRadius = Mathf.Max(0f, amount);
    }

    public void AddBonus(float amount)
    {
        BonusRadius += Mathf.Max(0f, amount);
    }
}

public static class WeaponVisualLibrary
{
    private static Sprite circleSprite;

    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, d <= radius ? Color.white : clear);
            }
        }
        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
