// DungeonSoul — Hiệu ứng kỹ năng (ưu tiên ASEPRITE_skill_effect, fallback EffectsPack14 / EffectLibrary).

using UnityEngine;

public enum SkillVfxStyle
{
    Fire = 1,
    Ice = 2,
    Lightning = 3,
    Poison = 4,
    Arcane = 5,
    Slash = 6
}

public static class SkillVfxLibrary
{
    public static Sprite[] GetFrames(SkillVfxStyle style)
    {
        Sprite[] generated = GeneratedSkillVfxLibrary.GetFrames(style);
        if (generated != null && generated.Length > 0)
            return generated;

        Sprite[] aseprite = AsepriteSkillVfxLoader.LoadFolders(AsepriteSkillEffectPaths.FoldersFor(style));
        if (aseprite != null && aseprite.Length > 0)
            return aseprite;

        return LoadLegacyPack((int)style);
    }

    public static void Play(SkillVfxStyle style, Vector3 worldPos, float scale = 1.2f, Color? tint = null, int sortingOrder = 23,
        float rotationZ = 0f)
    {
        Sprite[] frames = GetFrames(style);

        if (frames == null || frames.Length == 0)
        {
            EffectLibrary.Play(MapToEffectKind(style), worldPos, scale, tint, 20f, sortingOrder);
            return;
        }

        Color color = tint ?? Color.white;
        GameObject go = RuntimeSpawnGuard.Mark(new GameObject("SkillVfx_" + style));
        go.transform.position = worldPos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        go.AddComponent<HeroKnightVfxRunner>().Begin(frames, 18f, scale);
    }

    private static EffectKind MapToEffectKind(SkillVfxStyle style)
    {
        switch (style)
        {
            case SkillVfxStyle.Fire: return EffectKind.FireExplosion;
            case SkillVfxStyle.Ice: return EffectKind.IceExplosion;
            case SkillVfxStyle.Poison: return EffectKind.PoisonBoom;
            case SkillVfxStyle.Lightning: return EffectKind.BlueExplosion;
            case SkillVfxStyle.Arcane: return EffectKind.BlueExplosion;
            default: return EffectKind.HitImpact;
        }
    }

    public static void PlayForSkill(SkillType type, Vector3 worldPos, float scale = 1.15f)
    {
        if (GeneratedSkillVfxLibrary.HasPack)
        {
            GeneratedSkillVfxLibrary.PlayForSkill(type, worldPos, scale);
            return;
        }

        SkillVfxStyle style = MapSkill(type);
        Color tint = TintFor(style);
        Play(style, worldPos, scale, tint);
    }

    public static SkillVfxStyle MapSkill(SkillType type)
    {
        switch (type)
        {
            case SkillType.FireArrow:
            case SkillType.ExplosiveRounds:
            case SkillType.Explosion:
            case SkillType.DragonStrike:
                return SkillVfxStyle.Fire;
            case SkillType.IceAura:
            case SkillType.TimeFreeze:
                return SkillVfxStyle.Ice;
            case SkillType.LightningChain:
                return SkillVfxStyle.Lightning;
            case SkillType.PoisonCloud:
                return SkillVfxStyle.Poison;
            case SkillType.GhostForm:
            case SkillType.SoulHarvest:
            case SkillType.MirrorImage:
            case SkillType.Vampire:
                return SkillVfxStyle.Arcane;
            case SkillType.BladeStorm:
            case SkillType.CriticalHit:
            case SkillType.DeathMark:
                return SkillVfxStyle.Slash;
            default:
                return SkillVfxStyle.Slash;
        }
    }

    private static Color TintFor(SkillVfxStyle style)
    {
        return style switch
        {
            SkillVfxStyle.Fire => new Color(1f, 0.65f, 0.35f, 1f),
            SkillVfxStyle.Ice => new Color(0.65f, 0.9f, 1f, 1f),
            SkillVfxStyle.Lightning => new Color(0.85f, 0.9f, 1f, 1f),
            SkillVfxStyle.Poison => new Color(0.55f, 1f, 0.45f, 1f),
            SkillVfxStyle.Arcane => new Color(0.75f, 0.55f, 1f, 1f),
            _ => Color.white
        };
    }

    private static Sprite[] LoadLegacyPack(int folderIndex)
    {
        string resourcesPath = $"EffectsPack14/{folderIndex}";
        Sprite[] fromResources = Resources.LoadAll<Sprite>(resourcesPath);
        if (fromResources != null && fromResources.Length > 0)
            return fromResources;

#if UNITY_EDITOR
        string assetPath = $"Assets/Resources/EffectsPack14/{folderIndex}";
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { assetPath });
        if (guids != null && guids.Length > 0)
        {
            Sprite[] list = new Sprite[guids.Length];
            int count = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null)
                    list[count++] = s;
            }

            if (count > 0)
            {
                Sprite[] trimmed = new Sprite[count];
                System.Array.Copy(list, trimmed, count);
                return trimmed;
            }
        }
#endif

        return System.Array.Empty<Sprite>();
    }
}
