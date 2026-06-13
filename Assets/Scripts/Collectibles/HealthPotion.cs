using UnityEngine;

/// <summary>
/// Bình thuốc hồi máu rơi trên map. Nhặt được khi player chạm vào → hồi một lượng HP
/// (theo % máu tối đa, có sàn tối thiểu). Visual procedural (không cần asset), tự hủy
/// sau một thời gian để map không ngập item. Dùng chung pattern với CoinPickup/ExpGem.
/// </summary>
public class HealthPotion : MonoBehaviour
{
    private static Sprite potionSprite;

    /// <summary>Tạo một bình thuốc tại vị trí world. healFraction = % máu tối đa hồi lại.</summary>
    public static HealthPotion Spawn(Vector3 position, float healFraction = 0.25f, float minHeal = 20f)
    {
        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("HealthPotion"));
        go.transform.position = position;
        go.transform.localScale = Vector3.one * GameScale.PotionSize;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetPotionSprite();
        sr.color = Color.white;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 12;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        HealthPotion potion = go.AddComponent<HealthPotion>();
        potion.healFraction = Mathf.Clamp01(healFraction);
        potion.minHeal = Mathf.Max(1f, minHeal);
        return potion;
    }

    private float healFraction = 0.25f;
    private float minHeal = 20f;
    private float life = 25f;
    private float bobPhase;
    private Vector3 basePos;

    private void Start()
    {
        basePos = transform.position;
        bobPhase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        // Nhấp nhô nhẹ cho dễ thấy.
        bobPhase += Time.deltaTime * 3f;
        transform.position = basePos + Vector3.up * (Mathf.Sin(bobPhase) * 0.08f);

        life -= Time.deltaTime;
        if (life <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        HealthSystem hp = other.GetComponent<HealthSystem>();
        if (hp == null)
            hp = other.GetComponentInParent<HealthSystem>();
        if (hp == null)
            return;

        float healAmount = Mathf.Max(minHeal, hp.MaxHP * healFraction);
        hp.Heal(healAmount);
        HUDManager.Resolve()?.UpdateHp();
        HUDManager.SpawnHealNumber(transform.position, healAmount);
        AudioManager.PlayCoinCollect();
        Destroy(gameObject);
    }

    /// <summary>Sprite bình thuốc procedural: thân tròn đỏ + cổ + nắp sáng.</summary>
    private static Sprite GetPotionSprite()
    {
        if (potionSprite != null)
            return potionSprite;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Color clear = new Color(0, 0, 0, 0);
        Color glass = new Color(0.95f, 0.22f, 0.28f, 1f);   // dung dịch đỏ
        Color glassHi = new Color(1f, 0.55f, 0.55f, 1f);    // highlight
        Color cork = new Color(0.72f, 0.55f, 0.32f, 1f);    // nút bần
        Color outline = new Color(0.1f, 0.04f, 0.06f, 1f);

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        Vector2 c = new Vector2(size * 0.5f - 0.5f, size * 0.42f);
        float bodyR = size * 0.30f;

        // Thân tròn.
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                if (d <= bodyR)
                {
                    bool hi = (x - c.x) < -bodyR * 0.25f && (y - c.y) > bodyR * 0.1f;
                    tex.SetPixel(x, y, hi ? glassHi : glass);
                }
                else if (d <= bodyR + 1.4f)
                {
                    tex.SetPixel(x, y, outline);
                }
            }
        }

        // Cổ bình + nắp ở trên.
        int neckX0 = (int)(c.x - bodyR * 0.32f);
        int neckX1 = (int)(c.x + bodyR * 0.32f);
        int neckY0 = (int)(c.y + bodyR * 0.7f);
        int neckY1 = (int)(c.y + bodyR * 1.25f);
        for (int y = neckY0; y <= neckY1 && y < size; y++)
            for (int x = neckX0; x <= neckX1 && x < size; x++)
                tex.SetPixel(x, y, y >= neckY1 - 3 ? cork : glass);

        tex.Apply();
        potionSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        potionSprite.name = "HealthPotion_Procedural";
        return potionSprite;
    }
}
