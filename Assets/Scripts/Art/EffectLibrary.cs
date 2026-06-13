// DungeonSoul — VFX combat (ưu tiên ASEPRITE_skill_effect, fallback Assets/Art/Sprite/Effect).

using UnityEngine;

public enum EffectKind
{
    HitImpact,
    CritImpact,
    FireExplosion,
    IceExplosion,
    BlueExplosion,
    PoisonBoom,
    FireBreath,
    SpawnPoint
}

public static class EffectLibrary
{
    private const string LegacyRoot = "Assets/Art/Sprite/Effect/";

    private static readonly System.Collections.Generic.Dictionary<EffectKind, Sprite[]> Cache =
        new System.Collections.Generic.Dictionary<EffectKind, Sprite[]>();

    private static string LegacySheetPath(EffectKind kind)
    {
        switch (kind)
        {
            case EffectKind.FireExplosion: return LegacyRoot + "FireExplosion.png";
            case EffectKind.IceExplosion: return LegacyRoot + "IceExplosion.png";
            case EffectKind.BlueExplosion: return LegacyRoot + "BlueExplosion.png";
            case EffectKind.PoisonBoom: return LegacyRoot + "PoisonBoom.png";
            case EffectKind.FireBreath: return LegacyRoot + "FireBreath.png";
            case EffectKind.SpawnPoint: return LegacyRoot + "SpawnPoint_anim.png";
            case EffectKind.HitImpact:
                return LegacyRoot + "Retro Impact Effect Pack ALL/Retro Impact Effect Pack 1 A.png";
            case EffectKind.CritImpact:
                return LegacyRoot + "Retro Impact Effect Pack ALL/Retro Impact Effect Pack 3 A.png";
            default: return LegacyRoot + "FireExplosion.png";
        }
    }

    public static Sprite[] GetFrames(EffectKind kind)
    {
        if (Cache.TryGetValue(kind, out Sprite[] cached) && cached != null && cached.Length > 0)
            return cached;

        Sprite[] frames = AsepriteSkillVfxLoader.LoadFolders(AsepriteSkillEffectPaths.FoldersFor(kind));
        if (frames == null || frames.Length == 0)
            frames = LoadLegacySheet(LegacySheetPath(kind));

        Cache[kind] = frames ?? System.Array.Empty<Sprite>();
        return Cache[kind];
    }

    public static void Play(EffectKind kind, Vector3 worldPos, float worldSize = 1.2f, Color? tint = null,
        float fps = 20f, int sortingOrder = 24)
    {
        Sprite[] frames = GetFrames(kind);
        if (frames == null || frames.Length == 0)
            return;

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("VFX_" + kind));
        go.transform.position = worldPos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = tint ?? Color.white;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<HeroKnightVfxRunner>().Begin(frames, fps, worldSize);
    }

    private static Sprite[] LoadLegacySheet(string path)
    {
#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null || assets.Length == 0)
            return System.Array.Empty<Sprite>();

        System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite s)
                sprites.Add(s);
        }

        if (sprites.Count > 0)
            return sprites.ToArray();
#endif
        return System.Array.Empty<Sprite>();
    }
}
