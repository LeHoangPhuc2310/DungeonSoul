// DungeonSoul — TinyRpgProjectileLibrary.cs — Mũi tên / đạn từ Tiny RPG Character Asset Pack.

using System.Collections.Generic;
using UnityEngine;

public static class TinyRpgProjectileLibrary
{
    private const string PackRoot =
        "Assets/Art/EnemyAssets/Tiny RPG Character Asset Pack v1.03b -Full 20 Characters/Tiny RPG Character Asset Pack v1.03 -Full 20 Characters/";

    private const string SharedArrowDir = PackRoot + "Arrow(Projectile)/";

    private static readonly Dictionary<string, string> CharacterArrowPaths = new Dictionary<string, string>
    {
        { "archer", PackRoot + "Characters(100x100)/Archer/Arrow(projectile)/Arrow02(100x100).png" },
        { "soldier", PackRoot + "Characters(100x100)/Soldier/Arrow(projectile)/Arrow01(100x100).png" },
        { "skeleton_archer", PackRoot + "Characters(100x100)/Skeleton Archer/Arrow(projectile)/Arrow03(100x100).png" }
    };

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static bool CharacterUsesBow(string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
            return false;

        return CharacterArrowPaths.ContainsKey(characterId)
            || characterId.Contains("archer")
            || characterId == "lancer"
            || characterId == "orc_rider";
    }

    public static Sprite GetArrowSprite(PlayableCharacterEntry entry)
    {
        if (entry != null && !string.IsNullOrEmpty(entry.id))
        {
            if (CharacterArrowPaths.TryGetValue(entry.id, out string path))
            {
                Sprite s = Load(path);
                if (s != null)
                    return s;
            }
        }

        return Load(SharedArrowDir + "Arrow02(100x100).png")
            ?? Load(SharedArrowDir + "Arrow01(100x100).png");
    }

    public static float ArrowWorldLength => GameScale.ProjectileSize * 2.75f;

    public static float ScaleForArrow(Sprite sprite)
    {
        if (sprite == null)
            return GameScale.ProjectileSize;

        float len = Mathf.Max(0.02f, sprite.bounds.size.x, sprite.bounds.size.y);
        return ArrowWorldLength / len;
    }

    private static Sprite Load(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (cache.TryGetValue(path, out Sprite cached) && cached != null)
            return cached;

#if UNITY_EDITOR
        UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        Sprite best = null;
        float bestArea = 0f;
        if (assets != null)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite slice = assets[i] as Sprite;
                if (slice == null)
                    continue;

                float area = slice.rect.width * slice.rect.height;
                if (best == null || area > bestArea)
                {
                    best = slice;
                    bestArea = area;
                }
            }
        }

        if (best != null)
        {
            cache[path] = best;
            return best;
        }
#endif
        return null;
    }
}
