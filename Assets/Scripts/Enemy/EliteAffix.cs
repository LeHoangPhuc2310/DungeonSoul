using UnityEngine;

/// <summary>Biến dị (affix) gắn cho quái Elite — cho mỗi Elite một hành vi nguy hiểm riêng,
/// thay vì chỉ khác máu/màu. Tạo "khoảnh khắc" và lý do ưu tiên xử lý Elite.</summary>
public enum EliteAffix
{
    None,
    Explosive,   // nổ vùng khi chết
    Summoner,    // định kỳ triệu hồi grunt nhỏ
    Regenerator, // tự hồi máu liên tục
    Charger,     // định kỳ lao nhanh về phía player
    Empowerer    // hào quang tăng tốc quái thường quanh nó
}

/// <summary>
/// Điều khiển hành vi affix cho một Elite. Tự xử lý vòng đời (Update cho regen/charge/aura,
/// nghe EventBus.OnEnemyKilled cho explosive/summon). Gắn trong EnemyArchetypeUtility khi elite.
/// </summary>
public class EliteAffixController : MonoBehaviour
{
    private EliteAffix affix;
    private EnemyAI ai;
    private HealthSystem health;
    private Transform player;

    // Charger
    private float chargeTimer;
    private float baseSpeed;
    private float chargeUntil;

    // Summoner
    private float summonTimer;

    // Aura (Empowerer)
    private readonly Collider2D[] auraHits = new Collider2D[16];
    private float auraTimer;

    public EliteAffix Affix => affix;

    public static EliteAffix RollAffix()
    {
        // None hiếm khi xảy ra để vẫn có Elite "trơn"; còn lại chia đều các affix.
        int r = Random.Range(0, 5);
        switch (r)
        {
            case 0: return EliteAffix.Explosive;
            case 1: return EliteAffix.Summoner;
            case 2: return EliteAffix.Regenerator;
            case 3: return EliteAffix.Charger;
            default: return EliteAffix.Empowerer;
        }
    }

    public void Setup(EliteAffix value)
    {
        affix = value;
        ai = GetComponent<EnemyAI>();
        health = GetComponent<HealthSystem>();
        baseSpeed = ai != null ? ai.MoveSpeed : 2.2f;

        ApplyAffixVisual();
    }

    private void OnEnable() => EventBus.OnEnemyKilled += HandleEnemyKilled;
    private void OnDisable() => EventBus.OnEnemyKilled -= HandleEnemyKilled;

    private void Update()
    {
        if (health == null || health.CurrentHP <= 0f)
            return;

        switch (affix)
        {
            case EliteAffix.Regenerator:
                health.Heal(health.MaxHP * 0.04f * Time.deltaTime); // ~4%/giây
                break;
            case EliteAffix.Charger:
                TickCharger();
                break;
            case EliteAffix.Summoner:
                TickSummoner();
                break;
            case EliteAffix.Empowerer:
                TickAura();
                break;
        }
    }

    private void TickCharger()
    {
        if (ai == null)
            return;

        if (Time.time < chargeUntil)
            return; // đang trong cú lao (tốc đã tăng)

        chargeTimer -= Time.deltaTime;
        if (chargeTimer > 0f)
        {
            ai.MoveSpeed = baseSpeed;
            return;
        }

        // Bắt đầu lao: tăng tốc mạnh trong 0.6s, cooldown 3.5s.
        chargeTimer = 3.5f;
        chargeUntil = Time.time + 0.6f;
        ai.MoveSpeed = baseSpeed * 3.2f;
        EffectLibrary.Play(EffectKind.SpawnPoint, transform.position, 1.0f, new Color(1f, 0.5f, 0.2f, 0.9f));
    }

    private void TickSummoner()
    {
        summonTimer -= Time.deltaTime;
        if (summonTimer > 0f)
            return;
        summonTimer = 5f;

        int count = Random.Range(2, 4);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.2f);
            SpawnMinion((Vector2)transform.position + offset);
        }
        EffectLibrary.Play(EffectKind.PoisonBoom, transform.position, 1.2f, new Color(0.6f, 0.3f, 1f, 0.9f));
    }

    private void SpawnMinion(Vector2 pos)
    {
        GameObject minion = RuntimeSpawnGuard.Mark(new GameObject("EliteMinion"));
        minion.tag = "Enemy";
        minion.transform.position = pos;

        SpriteRenderer sr = minion.AddComponent<SpriteRenderer>();
        sr.sprite = WeaponVisualLibrary.GetCircleSprite();
        sr.color = new Color(0.6f, 0.35f, 0.95f, 1f);
        sr.sortingOrder = 5;
        minion.transform.localScale = Vector3.one * 0.7f;

        Rigidbody2D rb = minion.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        HealthSystem hp = minion.AddComponent<HealthSystem>();
        hp.MaxHP = 18f;
        hp.CurrentHP = 18f;

        minion.AddComponent<EnemyAI>();
        minion.AddComponent<EnemyReward>();
        minion.AddComponent<EnemyPhysicsSetup>().FitColliderToSprite();
    }

    private void TickAura()
    {
        auraTimer -= Time.deltaTime;
        if (auraTimer > 0f)
            return;
        auraTimer = 0.5f;

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, 2.5f, auraHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D c = auraHits[i];
            if (c == null || c.gameObject == gameObject)
                continue;
            if (!c.CompareTag("Enemy"))
                continue;

            EnemyAI buddy = c.GetComponent<EnemyAI>();
            if (buddy == null || buddy.GetComponent<EliteAffixController>() != null)
                continue;

            // Đánh dấu để chỉ buff một lần (tránh cộng dồn mỗi tick).
            EliteAuraBuffMarker mark = c.GetComponent<EliteAuraBuffMarker>();
            if (mark == null)
            {
                mark = c.gameObject.AddComponent<EliteAuraBuffMarker>();
                float original = buddy.MoveSpeed;
                buddy.MoveSpeed = original * 1.35f;
                mark.Init(buddy, original);
            }
            mark.Refresh();
        }
    }

    private void HandleEnemyKilled(EnemyKilledInfo info)
    {
        if (info.Enemy != gameObject)
            return;

        if (affix == EliteAffix.Explosive)
        {
            EffectLibrary.Play(EffectKind.FireExplosion, info.Position, 2.4f, new Color(1f, 0.5f, 0.2f, 1f));
            DealExplosionDamage(info.Position, 2.0f, 28f);
        }
    }

    private static void DealExplosionDamage(Vector3 center, float radius, float damage)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var c in hits)
        {
            if (c == null || !c.CompareTag("Player"))
                continue;
            HealthSystem hp = c.GetComponent<HealthSystem>();
            if (hp == null)
                hp = c.GetComponentInParent<HealthSystem>();
            if (hp != null)
                hp.TakeDamage(damage, false, (Vector2)center);
        }
    }

    private void ApplyAffixVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
            return;

        // Tô nhẹ màu theo affix để người chơi đọc được mối nguy (giữ sprite gốc).
        Color tint = affix switch
        {
            EliteAffix.Explosive => new Color(1f, 0.6f, 0.45f, 1f),
            EliteAffix.Summoner => new Color(0.8f, 0.6f, 1f, 1f),
            EliteAffix.Regenerator => new Color(0.6f, 1f, 0.7f, 1f),
            EliteAffix.Charger => new Color(1f, 0.85f, 0.5f, 1f),
            EliteAffix.Empowerer => new Color(1f, 0.5f, 0.85f, 1f),
            _ => Color.white
        };
        sr.color = tint;
    }
}

/// <summary>Đánh dấu quái đã được hào quang Elite buff — tự gỡ buff khi hết hạn (rời vùng).</summary>
public class EliteAuraBuffMarker : MonoBehaviour
{
    private float expire;
    private EnemyAI ai;
    private float restoreSpeed;

    public void Init(EnemyAI target, float originalSpeed)
    {
        ai = target;
        restoreSpeed = originalSpeed;
    }

    public void Refresh() => expire = Time.time + 1.2f;

    private void Update()
    {
        if (Time.time < expire)
            return;

        if (ai != null)
            ai.MoveSpeed = restoreSpeed;
        Destroy(this);
    }
}
