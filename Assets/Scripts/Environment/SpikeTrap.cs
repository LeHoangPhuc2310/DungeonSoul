using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bẫy chông trên sàn dungeon. Chu kỳ: ẩn (an toàn) → nhô chông (peaks_1→4) → gây sát thương
/// cho bất kỳ ai (player + quái) đứng trên khi chông nhô tối đa → hạ xuống. Visual + collider
/// dựng runtime, không cần prefab. Sprite lấy từ Resources/Traps/Peaks.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class SpikeTrap : MonoBehaviour
{
    private static Sprite[] sharedFrames;

    [Tooltip("Chu kỳ một lần nhô chông (giây).")]
    private float cycle = 2.4f;
    [Tooltip("Phần thời gian chông ở trạng thái NHÔ (gây hại) trong mỗi chu kỳ.")]
    private float dangerWindow = 0.55f;
    private float damage = 12f;
    private float worldHitRadius = 0.5f;

    private SpriteRenderer sr;
    private CircleCollider2D zone;
    private Sprite[] frames;
    private float phase;
    private bool armed;            // chông đang nhô (gây hại)
    private float damageCooldown;  // không gây hại liên tục mỗi frame
    private readonly Collider2D[] hits = new Collider2D[16];
    private readonly HashSet<int> hitThisStrike = new HashSet<int>();

    public static Sprite[] LoadFrames()
    {
        if (sharedFrames != null && sharedFrames.Length > 0)
            return sharedFrames;

        var list = new List<Sprite>(4);
        for (int i = 1; i <= 4; i++)
        {
            Sprite s = Resources.Load<Sprite>($"Traps/Peaks/peaks_{i}");
            if (s != null)
                list.Add(s);
        }

        if (list.Count < 3)
        {
            list.Clear();
            string[] sheetFrames = { "trap_animation_14", "trap_animation_92", "trap_animation_170" };
            for (int i = 0; i < sheetFrames.Length; i++)
            {
                Sprite s = Resources.Load<Sprite>($"Traps/{sheetFrames[i]}");
                if (s != null)
                    list.Add(s);
            }
            if (list.Count >= 2)
                list.Add(list[list.Count - 1]);
        }

        if (list.Count < 3)
            list.AddRange(ProceduralSpikeTrapSprites.Build());

        sharedFrames = list.ToArray();
        return sharedFrames;
    }

    /// <summary>Tạo một bẫy tại vị trí world (đặt trên tâm ô sàn).</summary>
    public static SpikeTrap Spawn(Vector3 position, float worldSize = 0.85f, float dmg = 12f, float cyclePhaseOffset = 0f)
    {
        Sprite[] f = LoadFrames();
        if (f == null || f.Length == 0)
            return null;

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("SpikeTrap"));
        go.transform.position = position;
        SpikeTrap trap = go.AddComponent<SpikeTrap>();
        trap.frames = f;
        trap.damage = dmg;
        trap.phase = cyclePhaseOffset;
        trap.worldHitRadius = worldSize * 0.52f;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = f[0];
        // Màu trắng tường minh + material sprite mặc định: mọi bẫy hiển thị đồng đều, không bị
        // tint khác nhau theo lighting hay vùng alpha lộ nền sàn bên dưới.
        renderer.color = Color.white;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 2; // trên nền sàn, dưới quái/player
        float h = f[0].bounds.size.y;
        go.transform.localScale = Vector3.one * (h > 0.001f ? worldSize / h : worldSize);
        trap.sr = renderer;

        // Nền tối đồng nhất phía dưới chông: che tile sàn (sắc độ thay đổi theo vị trí) để
        // mọi bẫy trông giống nhau, không còn cảm giác "màu không đồng đều".
        AddUniformBacking(go.transform, renderer.sprite);

        return trap;
    }

    private static void AddUniformBacking(Transform parent, Sprite shapeSprite)
    {
        GameObject bg = new GameObject("TrapBacking");
        bg.transform.SetParent(parent, false);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = Vector3.one;

        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = shapeSprite;
        bgSr.color = new Color(0.05f, 0.05f, 0.08f, 0.85f); // nền tối mờ đồng nhất
        bgSr.sortingLayerName = "Default";
        bgSr.sortingOrder = 1; // dưới chông (2), trên nền sàn
    }

    private void Awake()
    {
        zone = GetComponent<CircleCollider2D>();
        zone.isTrigger = true;
        FitColliderRadius();
    }

    private void FitColliderRadius()
    {
        if (zone == null)
            return;

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.001f);
        zone.radius = worldHitRadius / scale;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0 || sr == null)
            return;

        phase += Time.deltaTime;
        if (phase >= cycle)
            phase -= cycle;

        // Tiến trình trong chu kỳ: 0..1
        float t = phase / cycle;

        // Cửa sổ nguy hiểm nằm ở giữa chu kỳ (chông nhô cao nhất).
        float dangerStart = 0.5f - dangerWindow * 0.5f;
        float dangerEnd = 0.5f + dangerWindow * 0.5f;
        bool nowArmed = t >= dangerStart && t <= dangerEnd;

        // Chọn frame: ngoài cửa sổ = peaks_1 (ẩn/thấp); trong cửa sổ = peaks_2→4 theo độ sâu.
        int frameIndex;
        if (!nowArmed)
        {
            frameIndex = 0;
        }
        else
        {
            float inner = Mathf.InverseLerp(dangerStart, dangerEnd, t); // 0..1 trong cửa sổ
            // nhô lên (1→3) rồi giữ ở đỉnh
            float rise = Mathf.Sin(inner * Mathf.PI);                   // 0→1→0
            frameIndex = 1 + Mathf.Clamp(Mathf.RoundToInt(rise * (frames.Length - 1)), 0, frames.Length - 1);
            frameIndex = Mathf.Min(frameIndex, frames.Length - 1);
        }
        sr.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];

        // Vừa nhô lên (bắt đầu cửa sổ) → âm thanh + reset danh sách đã trúng + đập NGAY
        // (damageCooldown=0) thay vì chờ tàn dư cooldown chu kỳ trước, nếu không cú đập
        // đầu tiên có thể bị bỏ lỡ — đây là lý do quái lướt qua không dính sát thương.
        if (nowArmed && !armed)
        {
            armed = true;
            hitThisStrike.Clear();
            damageCooldown = 0f;
            AudioManager.PlayTrapSpike();
        }
        else if (!nowArmed && armed)
        {
            armed = false;
        }

        if (!armed)
            return;

        damageCooldown -= Time.deltaTime;
        if (damageCooldown > 0f)
            return;
        damageCooldown = 0.12f;  // quét thường xuyên hơn để bắt quái đang di chuyển

        // Bán kính quét rộng hơn collider hình ảnh một chút: quái di chuyển nhanh, nếu chỉ
        // quét đúng tâm thì dễ trượt khung trúng. Vẫn hợp lý so với kích thước ô bẫy.
        float scanRadius = worldHitRadius * 1.25f;
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, scanRadius, hits);
        for (int i = 0; i < count; i++)
        {
            if (!TryGetDamageTarget(hits[i], out HealthSystem hp, out int id))
                continue;
            if (hitThisStrike.Contains(id))
                continue;

            hitThisStrike.Add(id);
            hp.TakeDamage(damage, false, (Vector2)transform.position);
        }
    }

    private static bool TryGetDamageTarget(Collider2D col, out HealthSystem health, out int trackId)
    {
        health = null;
        trackId = 0;
        if (col == null || col.GetComponent<SpikeTrap>() != null)
            return false;

        health = col.GetComponent<HealthSystem>();
        if (health == null)
            health = col.GetComponentInParent<HealthSystem>();
        if (health == null)
            return false;

        GameObject root = health.gameObject;
        if (!root.CompareTag("Player") && !root.CompareTag("Enemy"))
            return false;

        trackId = root.GetInstanceID();
        return true;
    }
}
