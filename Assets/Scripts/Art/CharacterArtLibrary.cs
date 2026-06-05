// DungeonSoul — CharacterArtLibrary.cs — Sprite riêng cho từng hero & boss từ asset pack.
// Load sprite con đã slice trong "Dungeon_Character.png" (hero) và frame idle quái (boss).

using System.Collections.Generic;
using UnityEngine;

public static class CharacterArtLibrary
{
    private const string CharSheet = "Assets/2D Pixel Dungeon Asset Pack/character and tileset/Dungeon_Character.png";

    private static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    // --- HERO: index sprite con trong Dungeon_Character.png cho mỗi lớp ---
    // CHỈ chọn các sprite ĐƠN (1 nhân vật, w≈14-19). Tránh _0/_2/_6/_13 (w>30 = 2 người dính nhau).
    private static int HeroSpriteIndex(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Warrior: return 11;  // w19 h19 — giáp
            case HeroType.Ranger: return 9;    // w19 h20
            case HeroType.Mage: return 21;     // w17 h17
            default: return 11;
        }
    }

    public static Sprite GetHeroSprite(HeroType hero)
    {
        Sprite s = LoadSubSprite(CharSheet, "Dungeon_Character_" + HeroSpriteIndex(hero));
        if (s != null)
            return s;

        // Dự phòng về thư viện cũ nếu không load được.
        return ArtSpriteLibrary.GetHeroSprite(hero);
    }

    // --- BOSS: mỗi tầng/boss một loại quái idle riêng ---
    // Tên loại quái khớp thư mục Character_animation/monsters_idle hoặc priests_idle.
    public static Sprite GetBossSprite(string bossName)
    {
        string folder = BossFolderFor(bossName);
        Sprite s = LoadMonsterIdle(folder);
        if (s != null)
            return s;

        return ArtSpriteLibrary.GetEnemySprite(EnemyArtKind.Elite);
    }

    private static string BossFolderFor(string bossName)
    {
        if (string.IsNullOrEmpty(bossName))
            return "monsters_idle/skeleton1/v1/skeleton_v1";

        string n = bossName.ToLowerInvariant();
        if (n.Contains("goblin")) return "monsters_idle/skeleton1/v1/skeleton_v1";
        if (n.Contains("golem") || n.Contains("stone")) return "monsters_idle/skeleton2/v1/skeleton2_v1";
        if (n.Contains("witch") || n.Contains("shadow")) return "monsters_idle/vampire/v1/vampire_v1";
        if (n.Contains("dragon") || n.Contains("lord")) return "monsters_idle/skull/v1/skull_v1";
        return "monsters_idle/skeleton1/v1/skeleton_v1";
    }

    private static Sprite LoadMonsterIdle(string relativeNoExt)
    {
        string path = "Assets/2D Pixel Dungeon Asset Pack/Character_animation/" + relativeNoExt + "_1.png";
        return LoadSingleSprite(path);
    }

    // ── Load helpers ─────────────────────────────────────────────────────────

    private static Sprite LoadSingleSprite(string path)
    {
        if (spriteCache.TryGetValue(path, out Sprite cached) && cached != null)
            return cached;

#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                spriteCache[path] = sprite;
                return sprite;
            }
        }
#endif
        return null;
    }

    private static Sprite LoadSubSprite(string sheetPath, string subName)
    {
        string key = sheetPath + "#" + subName;
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == subName)
            {
                spriteCache[key] = sprite;
                return sprite;
            }
        }
#endif
        return null;
    }
}
