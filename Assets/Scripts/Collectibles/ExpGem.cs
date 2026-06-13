using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CoinPickupAnimator))]
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
    [SerializeField] private float coinWorldSize = GameScale.ExpGemSize;

    private float expValue = 10f;
    private Transform player;
    private CircleCollider2D collectCollider;
    private SpriteRenderer spriteRenderer;
    private CoinPickupAnimator coinAnimator;

    private void Awake()
    {
        collectCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        coinAnimator = GetComponent<CoinPickupAnimator>();
        collectCollider.isTrigger = true;
        spriteRenderer.sortingOrder = 12;
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

        if (ExpSystem.Instance != null)
        {
            ExpSystem.Instance.AddExp(expValue);
            AudioManager.PlayCoinCollect();
        }

        Destroy(gameObject);
    }

    public void Initialize(GemRarity gemRarity, float expAmount = -1f)
    {
        rarity = gemRarity;
        expValue = expAmount > 0f
            ? expAmount
            : (rarity == GemRarity.Rare ? 22f : 10f);
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (spriteRenderer == null)
            return;

        bool rare = rarity == GemRarity.Rare;

        // Viên kinh nghiệm = pha lê (xanh lá / xanh dương), KHÔNG dùng sprite xu vàng để tránh nhầm.
        Sprite gem = ExpGemVisual.GetGemSprite(rare);
        spriteRenderer.sprite = gem;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 12;

        // CoinPickupAnimator (required component) sẽ tự nạp frame xu trong OnEnable —
        // ép nó giữ đúng 1 frame pha lê để không quay thành đồng xu.
        coinAnimator?.SetFrames(new[] { gem });

        float baseSize = rare ? GameScale.ExpGemSize * 1.18f : GameScale.ExpGemSize;
        transform.localScale = Vector3.one * baseSize;

        // Hào quang nhẹ phía sau cho viên nổi bật trên nền sàn tối.
        EnsureGlow(rare);
    }

    private SpriteRenderer glow;

    private void EnsureGlow(bool rare)
    {
        if (glow == null)
        {
            GameObject go = new GameObject("Glow");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 1.7f;
            glow = go.AddComponent<SpriteRenderer>();
            glow.sprite = WeaponVisualLibrary.GetCircleSprite();
            glow.sortingOrder = spriteRenderer.sortingOrder - 1;
        }

        glow.color = rare
            ? new Color(0.4f, 0.7f, 1f, 0.28f)
            : new Color(0.4f, 1f, 0.55f, 0.24f);
    }

    private float bobPhase;

    private void LateUpdate()
    {
        // Nhịp hào quang để viên "sống". Không di chuyển root (sẽ phá logic hút nam châm + collider).
        bobPhase += Time.deltaTime * 4f;
        if (glow != null)
        {
            float pulse = 0.85f + Mathf.Sin(bobPhase * 1.6f) * 0.15f;
            glow.transform.localScale = Vector3.one * (1.7f * pulse);
        }
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
        const float pixelsPerUnit = 10f;
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        return circleSprite;
    }
}
