// DungeonSoul — Component runtime cho các skill mới: PoisonCloud, DeathMark, MirrorImage,
// LightningChain và visual cho SoulOrb. VFX lấy từ GeneratedSkillVfx (PNG do baker tạo).

using System.Collections.Generic;
using UnityEngine;

/// <summary>Vùng mây độc tại xác quái: tick DoT lên enemy trong bán kính, visual loop frame PoisonCloud.</summary>
public class PoisonCloudZone : MonoBehaviour
{
    private static readonly List<PoisonCloudZone> Active = new List<PoisonCloudZone>(8);
    private const int MaxActive = 5;

    private readonly Collider2D[] buffer = new Collider2D[24];
    private float radius;
    private float dps;
    private float remaining;
    private float tickTimer;
    private SpriteRenderer sr;
    private Sprite[] frames;
    private float frameTimer;
    private int frameIndex;

    public static void TrySpawn(Vector3 position, float radius, float dps, float duration)
    {
        if (Active.Count >= MaxActive)
            return;
        for (int i = 0; i < Active.Count; i++)
        {
            if (Active[i] != null && (Active[i].transform.position - position).sqrMagnitude < 2.25f)
                return; // tránh chồng mây sát nhau
        }

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("PoisonCloudZone"));
        go.transform.position = position;
        PoisonCloudZone zone = go.AddComponent<PoisonCloudZone>();
        zone.radius = radius;
        zone.dps = dps;
        zone.remaining = duration;
    }

    private void Awake()
    {
        Active.Add(this);
    }

    // Visual dựng trong Start: chạy sau khi TrySpawn đã gán radius/dps (Awake chạy ngay lúc AddComponent).
    private void Start()
    {
        frames = GeneratedSkillVfxLibrary.GetFramesForSkill(SkillType.PoisonCloud);
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 19;
        sr.color = new Color(1f, 1f, 1f, 0.8f);
        if (frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
            float h = frames[0].bounds.size.y;
            float s = h > 0.001f ? radius * 2f / h : 1f;
            transform.localScale = Vector3.one * Mathf.Clamp(s, 0.2f, 3f);
        }
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // loop frame chậm, mờ dần ở giây cuối
        if (frames != null && frames.Length > 1)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 0.12f)
            {
                frameTimer = 0f;
                frameIndex = (frameIndex + 1) % frames.Length;
                sr.sprite = frames[frameIndex];
            }
        }
        if (sr != null && remaining < 1f)
        {
            Color c = sr.color;
            c.a = 0.8f * remaining;
            sr.color = c;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer < 0.5f)
            return;
        tickTimer = 0f;

        int count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, buffer);
        for (int i = 0; i < count; i++)
        {
            if (buffer[i] == null || !buffer[i].CompareTag("Enemy"))
                continue;
            HealthSystem hs = buffer[i].GetComponent<HealthSystem>();
            if (hs != null)
                hs.TakeDamage(dps * 0.5f);
        }
    }
}

/// <summary>Dấu ấn tử thần: đánh dấu enemy, sau delay kích nổ gây damage cộng thêm theo MaxHP.</summary>
public class DeathMarkDebuff : MonoBehaviour
{
    private HealthSystem health;
    private float fuse = 1.5f;
    private GameObject markVisual;

    public static void TryApply(GameObject enemy)
    {
        if (enemy == null || enemy.GetComponent<DeathMarkDebuff>() != null)
            return;
        enemy.AddComponent<DeathMarkDebuff>();
    }

    private void Awake()
    {
        health = GetComponent<HealthSystem>();

        Sprite[] frames = GeneratedSkillVfxLibrary.GetFramesForSkill(SkillType.DeathMark);
        if (frames != null && frames.Length > 2)
        {
            markVisual = RuntimeSpawnGuard.Mark(new GameObject("DeathMark"));
            markVisual.transform.SetParent(transform, false);
            markVisual.transform.localPosition = Vector3.zero;
            SpriteRenderer sr = markVisual.AddComponent<SpriteRenderer>();
            sr.sprite = frames[2]; // frame rune hiện rõ
            sr.sortingOrder = 24;
            float h = frames[2].bounds.size.y;
            markVisual.transform.localScale = Vector3.one * (h > 0.001f ? 0.9f / h : 1f);
        }
    }

    private void Update()
    {
        fuse -= Time.deltaTime;
        if (fuse > 0f)
            return;

        Vector3 pos = transform.position;
        if (markVisual != null)
            Destroy(markVisual);
        if (health != null && health.CurrentHP > 0f)
        {
            float bonus = 20f + health.MaxHP * 0.1f;
            health.TakeDamage(bonus, true);
            SkillVfxLibrary.PlayForSkill(SkillType.DeathMark, pos, 0.95f);
            if (HUDManager.Instance != null)
                HUDManager.Instance.RegisterDamageDealt(bonus);
        }
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (markVisual != null)
            Destroy(markVisual);
    }
}

/// <summary>Phân thân gương: bám theo player, copy sprite body lúc runtime (tint tím),
/// tự bắn đạn 50% damage vào quái gần nhất. KHÔNG dùng PNG nhân vật — visual do code dựng.</summary>
public class MirrorImageClone : MonoBehaviour
{
    private Transform player;
    private AutoAttack ownerAttack;
    private SpriteRenderer sr;
    private SpriteRenderer playerBody;
    private float fireTimer;
    private readonly Vector3 offset = new Vector3(-0.9f, 0.4f, 0f);
    private readonly Collider2D[] buffer = new Collider2D[32];

    public void Initialize(Transform followTarget, AutoAttack attack)
    {
        player = followTarget;
        ownerAttack = attack;

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.8f, 0.5f, 1f, 0.55f);
        sr.sortingOrder = 14; // dưới player một chút

        Transform body = followTarget != null ? followTarget.Find("HeroBody") : null;
        if (body != null)
            playerBody = body.GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.Lerp(transform.position, player.position + offset, Time.deltaTime * 6f);

        // soi gương: copy sprite + hướng lật của body player từng frame
        if (playerBody != null && sr != null)
        {
            sr.sprite = playerBody.sprite;
            sr.flipX = playerBody.flipX;
            transform.localScale = playerBody.transform.lossyScale;
        }

        if (ownerAttack == null)
            return;

        fireTimer += Time.deltaTime;
        if (fireTimer < ownerAttack.FireInterval * 2f)
            return;

        Transform target = FindNearestEnemy(6f);
        if (target == null)
            return;

        fireTimer = 0f;
        ownerAttack.FireCloneProjectile(transform.position, target.position, 0.5f);
    }

    private Transform FindNearestEnemy(float range)
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, range, buffer);
        Transform best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (buffer[i] == null || !buffer[i].CompareTag("Enemy"))
                continue;
            float d = (buffer[i].transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = buffer[i].transform;
            }
        }
        return best;
    }
}

/// <summary>Visual cho SoulOrb pickup: loop frame aura SoulHarvest tím nhỏ, nhấp nháy nhẹ.</summary>
public class SoulOrbVisual : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite[] frames;
    private float timer;
    private int index;
    private float pulse;

    public static void Attach(GameObject orb)
    {
        if (orb != null)
            orb.AddComponent<SoulOrbVisual>();
    }

    private void Awake()
    {
        frames = GeneratedAuraLibrary.GetSkillAura(SkillType.SoulHarvest);
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 20;
        sr.color = new Color(0.8f, 0.6f, 1f, 0.95f);
        if (frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
            float h = frames[0].bounds.size.y;
            transform.localScale = Vector3.one * (h > 0.001f ? 0.5f / h : 0.3f);
        }
    }

    private void Update()
    {
        pulse += Time.deltaTime * 5f;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.75f + Mathf.Sin(pulse) * 0.2f;
            sr.color = c;
        }

        if (frames == null || frames.Length <= 1)
            return;
        timer += Time.deltaTime;
        if (timer < 0.12f)
            return;
        timer = 0f;
        index = (index + 1) % frames.Length;
        sr.sprite = frames[index];
    }
}

/// <summary>LightningChain: nhảy sét từ enemy vừa trúng crit sang các enemy gần đó.</summary>
public static class LightningChainEffect
{
    private static readonly Collider2D[] Buffer = new Collider2D[32];

    public static void Trigger(Vector3 origin, GameObject firstEnemy, float baseDamage, int jumps, float damageRatio)
    {
        // Luôn nháy sét tại quái bị trúng (kể cả khi không có quái thứ 2 để nhảy sang).
        SkillVfxLibrary.PlayForSkill(SkillType.LightningChain, origin, 0.9f);
        AudioManager.PlayLightning();

        Vector3 from = origin;
        GameObject current = firstEnemy;
        float damage = baseDamage * damageRatio;

        for (int j = 0; j < jumps; j++)
        {
            GameObject next = FindNext(from, current);
            if (next == null)
                return;

            GeneratedSkillVfxLibrary.PlayForSkillDirected(SkillType.LightningChain, from, next.transform.position, 24);
            SkillVfxLibrary.PlayForSkill(SkillType.LightningChain, next.transform.position, 0.8f);
            AudioManager.PlayLightning(chainZap: true);
            HealthSystem hs = next.GetComponent<HealthSystem>();
            if (hs != null)
                hs.TakeDamage(damage);

            from = next.transform.position;
            current = next;
            damage *= damageRatio;
        }
    }

    private static GameObject FindNext(Vector3 from, GameObject exclude)
    {
        int count = Physics2D.OverlapCircleNonAlloc(from, 3.5f, Buffer);
        GameObject best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (Buffer[i] == null || !Buffer[i].CompareTag("Enemy") || Buffer[i].gameObject == exclude)
                continue;
            HealthSystem hs = Buffer[i].GetComponent<HealthSystem>();
            if (hs == null || hs.CurrentHP <= 0f)
                continue;
            float d = (Buffer[i].transform.position - from).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = Buffer[i].gameObject;
            }
        }
        return best;
    }
}
