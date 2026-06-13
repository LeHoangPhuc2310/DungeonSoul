using System.Collections.Generic;
using UnityEngine;

public static class GeneratedAuraLibrary
{
    private const string Root = "GeneratedSkillVfx/Auras";
    private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

    public static bool HasPack { get; private set; }

    // Domain reload có thể bị tắt → clear cache static mỗi lần vào Play để không dùng sprite cũ.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetCache()
    {
        Cache.Clear();
        HasPack = false;
    }

    public static Sprite[] GetSkillAura(SkillType type) => Load($"Skills/{type}");

    public static Sprite[] GetPassiveAura(string passiveId)
    {
        if (string.IsNullOrEmpty(passiveId))
            return System.Array.Empty<Sprite>();
        return Load($"Passives/{passiveId}");
    }

    public static Color SkillAuraTint(SkillType type)
    {
        switch (SkillVfxLibrary.MapSkill(type))
        {
            case SkillVfxStyle.Fire: return new Color(1f, 0.6f, 0.35f, 0.85f);
            case SkillVfxStyle.Ice: return new Color(0.6f, 0.9f, 1f, 0.85f);
            case SkillVfxStyle.Lightning: return new Color(0.8f, 0.75f, 1f, 0.85f);
            case SkillVfxStyle.Poison: return new Color(0.55f, 1f, 0.5f, 0.85f);
            case SkillVfxStyle.Arcane: return new Color(0.75f, 0.55f, 1f, 0.85f);
            default: return new Color(0.95f, 0.92f, 0.85f, 0.8f);
        }
    }

    public static Color PassiveAuraTint(PassiveItemData item)
    {
        if (item == null)
            return Color.white;
        return GameIconLibrary.PassiveTint(item);
    }

    private static Sprite[] Load(string subPath)
    {
        if (Cache.TryGetValue(subPath, out Sprite[] cached))
            return cached;

        Sprite[] raw = Resources.LoadAll<Sprite>($"{Root}/{subPath}");
        if (raw == null || raw.Length == 0)
        {
            Cache[subPath] = System.Array.Empty<Sprite>();
            return Cache[subPath];
        }

        System.Array.Sort(raw, (a, b) => string.CompareOrdinal(a.name, b.name));
        Cache[subPath] = raw;
        HasPack = true;
        return raw;
    }
}
