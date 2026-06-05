// DungeonSoul — EffectLibrary.cs — Load các sprite-sheet hiệu ứng trong Assets/Art/Sprite/Effect.
// Phát VFX 1 lần (hit, nổ, spawn) bằng HeroKnightVfxRunner. Có fallback an toàn.

using System.Collections.Generic;
using UnityEngine;

public enum EffectKind
{
    HitImpact,      // đòn trúng quái thường
    CritImpact,     // đòn chí mạng
    FireExplosion,  // nổ lửa (quái chết / skill lửa)
    IceExplosion,   // nổ băng
    BlueExplosion,  // nổ xanh (boss/đặc biệt)
    PoisonBoom,     // độc
    FireBreath,     // hơi thở lửa (boss)
    SpawnPoint      // vòng triệu hồi spawn
}

public static class EffectLibrary
{
    private const string Root = "Assets/Art/Sprite/Effect/";

    private static readonly Dictionary<EffectKind, Sprite[]> cache = new Dictionary<EffectKind, Sprite[]>();

    private static string SheetPath(EffectKind kind)
    {
        switch (kind)
        {
            case EffectKind.FireExplosion: return Root + "FireExplosion.png";
            case EffectKind.IceExplosion: return Root + "IceExplosion.png";
            case EffectKind.BlueExplosion: return Root + "BlueExplosion.png";
            case EffectKind.PoisonBoom: return Root + "PoisonBoom.png";
            case EffectKind.FireBreath: return Root + "FireBreath.png";
            case EffectKind.SpawnPoint: return Root + "SpawnPoint_anim.png";
            // Hit dùng các pack Retro Impact (nhiều biến thể).
            case EffectKind.HitImpact: return Root + "Retro Impact Effect Pack ALL/Retro Impact Effect Pack 1 A.png";
            case EffectKind.CritImpact: return Root + "Retro Impact Effect Pack ALL/Retro Impact Effect Pack 3 A.png";
            default: return Root + "FireExplosion.png";
        }
    }

    public static Sprite[] GetFrames(EffectKind kind)
    {
        if (cache.TryGetValue(kind, out Sprite[] cached) && cached != null && cached.Length > 0)
            return cached;

        Sprite[] frames = LoadSheet(SheetPath(kind));
        cache[kind] = frames;
        return frames;
    }

    /// <summary>Phát hiệu ứng 1 lần tại vị trí thế giới. worldSize = chiều cao mong muốn (unit).</summary>
    public static void Play(EffectKind kind, Vector3 worldPos, float worldSize = 1.2f, Color? tint = null,
        float fps = 20f, int sortingOrder = 24)
    {
        Sprite[] frames = GetFrames(kind);
        if (frames == null || frames.Length == 0)
            return;

        GameObject go = new GameObject("VFX_" + kind);
        go.transform.position = worldPos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = tint ?? Color.white;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<HeroKnightVfxRunner>().Begin(frames, fps, worldSize);
    }

    private static Sprite[] LoadSheet(string path)
    {
#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite s)
                sprites.Add(s);
        }

        if (sprites.Count > 0)
        {
            sprites.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            return sprites.ToArray();
        }
#endif
        return System.Array.Empty<Sprite>();
    }

    private static int FrameIndex(string name)
    {
        if (string.IsNullOrEmpty(name))
            return 0;
        // Lấy chuỗi số cuối tên (vd "...A_3" → 3, "FireExplosion_12" → 12).
        int i = name.Length - 1;
        while (i >= 0 && !char.IsDigit(name[i])) i--;
        int end = i + 1;
        while (i >= 0 && char.IsDigit(name[i])) i--;
        if (end > i + 1 && int.TryParse(name.Substring(i + 1, end - i - 1), out int n))
            return n;
        return 0;
    }
}
