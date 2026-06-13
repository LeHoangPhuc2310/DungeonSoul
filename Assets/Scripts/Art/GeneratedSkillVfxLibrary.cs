// DungeonSoul — VFX skill do AI tạo (Resources/GeneratedSkillVfx), không dùng Aseprite pack.

using System.Collections.Generic;
using UnityEngine;

public static class GeneratedSkillVfxLibrary
{
    private const string Root = "GeneratedSkillVfx";
    private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();
    // Key được inject procedural — chỉ là fallback, PNG thật trong Resources luôn override.
    private static readonly HashSet<string> InjectedKeys = new HashSet<string>();

    public static bool HasPack { get; private set; }

    // Cho phép ProceduralVfxInjector nạp frame sinh bằng code khi không có file PNG.
    public static void InjectFrames(string key, Sprite[] frames)
    {
        if (frames != null && frames.Length > 0)
        {
            Cache[key] = frames;
            InjectedKeys.Add(key);
            HasPack = true;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Warmup()
    {
        // Domain reload có thể bị tắt (Enter Play Mode Options) → cache static giữ sprite cũ
        // từ lần bake trước. Clear để Resources.LoadAll đọc asset mới nhất.
        Cache.Clear();
        InjectedKeys.Clear();
        HasPack = false;

        ProceduralVfxInjector.InjectAll();
        // HasPack dựa trên sprite PerSkill thật (FireArrow là folder luôn có) — không còn phụ thuộc procedural.
        HasPack = LoadFolder("PerSkill/FireArrow") is { Length: > 0 };
    }

    public static Sprite[] GetFrames(SkillVfxStyle style) => GetFrames(style.ToString());

    public static Sprite[] GetFramesForSkill(SkillType type)
    {
        Sprite[] perSkill = LoadFolder($"PerSkill/{type}");
        if (perSkill.Length > 0)
            return perSkill;

        if (type == SkillType.DragonStrike)
        {
            Sprite[] nuclear = LoadFolder("Nuclear");
            if (nuclear.Length > 0)
                return nuclear;
        }

        return GetFrames(SkillVfxLibrary.MapSkill(type));
    }

    public static Sprite[] GetFrames(string folderName)
    {
        if (string.IsNullOrEmpty(folderName))
            return System.Array.Empty<Sprite>();

        if (Cache.TryGetValue(folderName, out Sprite[] cached))
            return cached;

        return LoadFolder(folderName);
    }

    private static Sprite[] LoadFolder(string subPath)
    {
        // Cache-hit chỉ dùng khi entry KHÔNG phải đồ inject — PNG thật phải được thử trước.
        if (!InjectedKeys.Contains(subPath) && Cache.TryGetValue(subPath, out Sprite[] cached))
            return cached;

        string path = $"{Root}/{subPath}";
        Sprite[] raw = Resources.LoadAll<Sprite>(path);
        if (raw == null || raw.Length == 0)
        {
            // Giữ lại procedural nếu đã inject — không ghi đè bằng empty.
            if (!Cache.ContainsKey(subPath))
                Cache[subPath] = System.Array.Empty<Sprite>();
            return Cache.TryGetValue(subPath, out Sprite[] fallback) ? fallback : System.Array.Empty<Sprite>();
        }

        System.Array.Sort(raw, (a, b) => string.CompareOrdinal(a.name, b.name));
        Cache[subPath] = raw;
        InjectedKeys.Remove(subPath); // PNG thật đã override procedural — cache-hit từ giờ hợp lệ
        HasPack = true;
        return raw;
    }

    public static void PlayForSkill(SkillType type, Vector3 worldPos, float scale = 1.15f)
    {
        // CHỈ dùng sprite PNG per-skill (Resources/GeneratedSkillVfx/PerSkill/<type>).
        // Không fallback style/procedural — frame đã vẽ đủ màu nên tint trắng.
        Sprite[] frames = LoadFolder($"PerSkill/{type}");
        if (frames == null || frames.Length == 0)
            return;

        float s = type == SkillType.DragonStrike ? scale * 1.4f : scale;
        PlayFrames(frames, worldPos, s, Color.white, type == SkillType.DragonStrike ? 24 : 23);
    }

    /// <summary>VFX nối 2 điểm (vd LightningChain A→B): đặt giữa, xoay theo hướng, scale theo khoảng cách.</summary>
    public static void PlayForSkillDirected(SkillType type, Vector3 from, Vector3 to, int sortingOrder = 23)
    {
        Sprite[] frames = GetFramesForSkill(type);
        if (frames == null || frames.Length == 0 || frames[0] == null)
            return;

        Vector3 mid = (from + to) * 0.5f;
        Vector3 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.05f)
            return;

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("GenSkillVfxDir"));
        go.transform.position = mid;
        // Tia sét vẽ THEO TRỤC DỌC của sprite (chạy từ chân lên đỉnh) → trừ 90° để trục dọc
        // khớp với hướng A→B nằm ngang.
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = Color.white;
        sr.sortingOrder = sortingOrder;
        // Tia sét: KÉO DÀI đúng bằng khoảng cách 2 quái, ĐỘ DÀY cố định (không phình to khi xa).
        const float boltThickness = 0.9f;
        go.AddComponent<HeroKnightVfxRunner>().BeginStretched(frames, 14f, dist, boltThickness);
    }

    public static void Play(string folder, Vector3 worldPos, float scale, Color tint, int sortingOrder)
    {
        Sprite[] frames = GetFrames(folder);
        PlayFrames(frames, worldPos, scale, tint, sortingOrder);
    }

    private static void PlayFrames(Sprite[] frames, Vector3 worldPos, float scale, Color tint, int sortingOrder)
    {
        if (frames == null || frames.Length == 0 || frames[0] == null)
            return;
        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("GenSkillVfx"));
        go.transform.position = worldPos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = tint;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<HeroKnightVfxRunner>().Begin(frames, 14f, scale);
    }

    private static Color TintFor(SkillVfxStyle style)
    {
        return style switch
        {
            SkillVfxStyle.Fire => new Color(1f, 0.65f, 0.35f),
            SkillVfxStyle.Ice => new Color(0.65f, 0.9f, 1f),
            SkillVfxStyle.Lightning => new Color(0.85f, 0.9f, 1f),
            SkillVfxStyle.Poison => new Color(0.55f, 1f, 0.45f),
            SkillVfxStyle.Arcane => new Color(0.75f, 0.55f, 1f),
            _ => Color.white
        };
    }
}
