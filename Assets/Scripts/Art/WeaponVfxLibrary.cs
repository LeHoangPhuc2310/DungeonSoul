// DungeonSoul — Animation đạn / muzzle / hit / orbit cho 12 WeaponType.
// Fallback hoàn toàn procedural (ProceduralWeaponVfxPainter) nếu không có file PNG.

using System.Collections.Generic;
using UnityEngine;

public static class WeaponVfxLibrary
{
    private const string Root = "GeneratedWeaponVfx";

    private static readonly Dictionary<WeaponType, Sprite[]> ProjectileCache =
        new Dictionary<WeaponType, Sprite[]>();
    private static readonly Dictionary<string, Sprite[]> MuzzleCache =
        new Dictionary<string, Sprite[]>();
    private static readonly Dictionary<string, Sprite[]> HitCache =
        new Dictionary<string, Sprite[]>();

    private static Sprite[] orbitFrames;

    public static bool HasPack { get; private set; }

    // ── Inject từ ProceduralWeaponVfxInjector ─────────────────────
    public static void InjectProjectile(WeaponType type, Sprite[] frames)
    {
        if (frames != null && frames.Length > 0)
        {
            ProjectileCache[type] = frames;
            HasPack = true;
        }
    }

    public static void InjectMuzzle(string key, Sprite[] frames)
    {
        if (frames != null && frames.Length > 0)
            MuzzleCache[key] = frames;
    }

    public static void InjectHit(string key, Sprite[] frames)
    {
        if (frames != null && frames.Length > 0)
            HitCache[key] = frames;
    }

    // ── Warmup ────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Warmup()
    {
        // Nạp procedural trước; PNG từ Resources sẽ override nếu có.
        ProceduralWeaponVfxInjector.InjectAll();
        HasPack = GetProjectileFrames(WeaponType.FireStaff) is { Length: > 0 };
    }

    // ── Projectile ────────────────────────────────────────────────
    public static Sprite[] GetProjectileFrames(WeaponType type)
    {
        if (ProjectileCache.TryGetValue(type, out Sprite[] cached))
            return cached;

        string path = $"{Root}/Projectiles/{type}";
        Sprite[] loaded = LoadSortedSprites(path);
        if (loaded != null && loaded.Length > 0)
        {
            ProjectileCache[type] = loaded;
            HasPack = true;
            return loaded;
        }
        // Không có PNG → giữ procedural đã inject (nếu có)
        return ProjectileCache.TryGetValue(type, out Sprite[] proc) ? proc
             : System.Array.Empty<Sprite>();
    }

    public static Sprite[] GetOrbitFrames()
    {
        if (orbitFrames != null)
            return orbitFrames;

        orbitFrames = LoadSortedSprites($"{Root}/Orbit");
        return orbitFrames;
    }

    // ── Per-weapon metadata ───────────────────────────────────────
    public static float GetProjectileFps(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return 14f;
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
            case WeaponType.StormBow:
                return 16f;
            default:
                return 12f;
        }
    }

    public static float GetProjectileScale(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.DragonStaff:
                return 0.55f;
            case WeaponType.HolyNova:
                return 0.5f;
            case WeaponType.IronBow:
            case WeaponType.StormBow:
                return 0.35f;
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return 0.28f;
            default:
                return 0.4f;
        }
    }

    public static Color GetTint(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:
                return new Color(1f, 0.7f, 0.45f);
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:
                return new Color(0.65f, 0.92f, 1f);
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return new Color(0.55f, 1f, 0.5f);
            case WeaponType.HolyCross:
            case WeaponType.HolyNova:
                return new Color(1f, 0.95f, 0.55f);
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
            case WeaponType.StormBow:
                return new Color(0.8f, 0.75f, 1f);
            default:
                return Color.white;
        }
    }

    public static bool Pierces(WeaponType type)
    {
        return type == WeaponType.StormBow
            || type == WeaponType.DragonStaff
            || type == WeaponType.DeathDagger;
    }

    // ── Muzzle ────────────────────────────────────────────────────
    public static void PlayMuzzle(WeaponType type, Vector3 worldPos, float angleZ)
    {
        string key = MuzzleKey(type);
        Sprite[] frames = GetMuzzleFrames(key);
        if (frames == null || frames.Length == 0)
            return;

        SpawnBurst(worldPos, frames, 18f, 0.3f, GetTint(type), 25, angleZ);
    }

    // ── Hit ───────────────────────────────────────────────────────
    public static void PlayHit(WeaponType type, Vector3 worldPos, bool crit)
    {
        string key = HitKey(type);
        Sprite[] frames = GetHitFrames(key);
        if (frames == null || frames.Length == 0)
        {
            if (crit)
                EffectLibrary.Play(EffectKind.CritImpact, worldPos, 0.85f,
                    new Color(1f, 0.85f, 0.4f), 26f, 24);
            return;
        }

        float scale = crit ? 0.55f : 0.42f;
        SpawnBurst(worldPos, frames, 16f, scale, GetTint(type), 24);
        if (key == "lightning")
            AudioManager.PlayLightning(chainZap: !crit);
        if (crit)
            EffectLibrary.Play(EffectKind.CritImpact, worldPos, 0.7f,
                new Color(1f, 0.85f, 0.4f), 26f, 25);
    }

    // ── Area ─────────────────────────────────────────────────────
    public static void PlayArea(WeaponType type, Vector3 worldPos, float radius)
    {
        SkillVfxStyle style = MapAreaStyle(type);
        SkillVfxLibrary.Play(style, worldPos,
            Mathf.Clamp(radius * 0.45f, 0.8f, 2.8f), GetTint(type), 23);
    }

    // ── Internals ─────────────────────────────────────────────────
    private static Sprite[] GetMuzzleFrames(string key)
    {
        if (MuzzleCache.TryGetValue(key, out Sprite[] cached))
            return cached;

        // Thử load từ Resources
        Sprite[] loaded = LoadSortedSprites($"{Root}/Muzzle/{key}");
        if (loaded != null && loaded.Length > 0)
        {
            MuzzleCache[key] = loaded;
            return loaded;
        }
        // Fallback: muzzle chung (legacy single-folder)
        Sprite[] legacy = LoadSortedSprites($"{Root}/Muzzle");
        if (legacy != null && legacy.Length > 0)
        {
            MuzzleCache[key] = legacy;
            return legacy;
        }
        return System.Array.Empty<Sprite>();
    }

    private static Sprite[] GetHitFrames(string key)
    {
        if (HitCache.TryGetValue(key, out Sprite[] cached))
            return cached;

        Sprite[] loaded = LoadSortedSprites($"{Root}/Hit/{key}");
        if (loaded != null && loaded.Length > 0)
        {
            HitCache[key] = loaded;
            return loaded;
        }
        Sprite[] legacy = LoadSortedSprites($"{Root}/Hit");
        if (legacy != null && legacy.Length > 0)
        {
            HitCache[key] = legacy;
            return legacy;
        }
        return System.Array.Empty<Sprite>();
    }

    private static string MuzzleKey(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:   return "fire";
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:  return "ice";
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:   return "poison";
            case WeaponType.HolyCross:
            case WeaponType.HolyNova:      return "holy";
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
            case WeaponType.StormBow:      return "lightning";
            default:                       return "arrow";
        }
    }

    private static string HitKey(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:   return "fire";
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:  return "ice";
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:   return "poison";
            case WeaponType.HolyCross:
            case WeaponType.HolyNova:      return "holy";
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
            case WeaponType.StormBow:      return "lightning";
            default:                       return "arrow";
        }
    }

    private static SkillVfxStyle MapAreaStyle(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:   return SkillVfxStyle.Fire;
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:  return SkillVfxStyle.Ice;
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:   return SkillVfxStyle.Poison;
            case WeaponType.HolyCross:
            case WeaponType.HolyNova:      return SkillVfxStyle.Arcane;
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
            case WeaponType.StormBow:      return SkillVfxStyle.Lightning;
            default:                       return SkillVfxStyle.Slash;
        }
    }

    private static Sprite[] LoadSortedSprites(string resourcesPath)
    {
        Sprite[] raw = Resources.LoadAll<Sprite>(resourcesPath);
        if (raw == null || raw.Length == 0)
            return System.Array.Empty<Sprite>();

        System.Array.Sort(raw, (a, b) => string.CompareOrdinal(a.name, b.name));
        return raw;
    }

    private static void SpawnBurst(Vector3 worldPos, Sprite[] frames, float fps,
        float scale, Color tint, int sortingOrder, float rotationZ = 0f)
    {
        if (frames == null || frames.Length == 0 || frames[0] == null)
            return;

        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("WeaponVfx"));
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = tint;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<HeroKnightVfxRunner>().Begin(frames, fps, scale);
    }
}
