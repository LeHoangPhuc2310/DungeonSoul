using System.Collections.Generic;
using UnityEngine;

public enum EnemyArtKind
{
    Grunt,
    Runner,
    Brute,
    Elite,
    Chest
}

/// <summary>Sprites from Assets/Art/Tiles (editor) or ArtSpriteSet in Resources, with procedural fallback.</summary>
public static class ArtSpriteLibrary
{
    private static ArtSpriteSet cachedSet;
    private static readonly Sprite[] procedural = new Sprite[5];
    private static readonly Dictionary<int, Sprite> tileCache = new Dictionary<int, Sprite>();

    public static Sprite GetEnemySprite(EnemyArtKind kind) => Get(kind);

    public static Sprite GetChestSprite()
    {
        Sprite pack = DungeonPackLibrary.GetChest();
        if (IsValidSprite(pack))
            return pack;
        return Get(EnemyArtKind.Chest);
    }

    /// <summary>Tile index for each weapon (Assets/Art/Tiles).</summary>
    public static int GetWeaponTileIndex(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return type == WeaponType.DeathDagger ? 104 : 103;
            case WeaponType.HolyCross:
                return 105;
            case WeaponType.HolyNova:
                return 106;
            case WeaponType.IronBow:
            case WeaponType.StormBow:
                return type == WeaponType.StormBow ? 107 : 131;
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:
                return 129;
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:
                return 130;
            case WeaponType.ThunderRod:
                return 117;
            case WeaponType.ZeusRod:
                return 118;
            default:
                return 106;
        }
    }

    public static int GetHeroDefaultWeaponTile(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Ranger:
                return 131;
            case HeroType.Mage:
                return 129;
            default:
                return 106;
        }
    }

    public static Sprite GetWeaponSprite(WeaponType type)
    {
        Sprite sprite = LoadTile(GetWeaponTileIndex(type));
        return sprite != null ? sprite : GetProceduralWeapon(HeroType.Warrior);
    }

    public static Sprite GetWeaponSprite(HeroType hero)
    {
        Sprite sprite = LoadTile(GetHeroDefaultWeaponTile(hero));
        return sprite != null ? sprite : GetProceduralWeapon(hero);
    }

    public static Color GetWeaponTint(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:
                return new Color(1f, 0.55f, 0.35f);
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:
                return new Color(0.65f, 0.9f, 1f);
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return new Color(0.6f, 1f, 0.55f);
            case WeaponType.HolyCross:
            case WeaponType.HolyNova:
                return new Color(1f, 0.95f, 0.6f);
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
                return new Color(0.85f, 0.75f, 1f);
            case WeaponType.StormBow:
                return new Color(0.75f, 0.9f, 1f);
            default:
                return Color.white;
        }
    }

    public static Sprite LoadTile(int tileIndex)
    {
        if (tileCache.TryGetValue(tileIndex, out Sprite cached) && cached != null)
            return cached;

        Sprite fromSet = FromResourcesTile(tileIndex);
        if (IsValidSprite(fromSet))
        {
            tileCache[tileIndex] = fromSet;
            return fromSet;
        }

#if UNITY_EDITOR
        Sprite fromEditor = LoadEditorTileByIndex(tileIndex);
        if (IsValidSprite(fromEditor))
        {
            tileCache[tileIndex] = fromEditor;
            return fromEditor;
        }
#endif

        Sprite fallback = GetProceduralWeapon(HeroType.Warrior);
        tileCache[tileIndex] = fallback;
        return fallback;
    }

    private static Sprite FromResourcesTile(int tileIndex)
    {
        if (cachedSet == null)
            cachedSet = Resources.Load<ArtSpriteSet>("ArtSpriteSet");
        if (cachedSet == null)
            return null;

        switch (tileIndex)
        {
            case 103: return cachedSet.weaponDagger;
            case 104: return cachedSet.weaponShortSword;
            case 105: return cachedSet.weaponCurvedBlade;
            case 106: return cachedSet.weaponBroadsword;
            case 107: return cachedSet.weaponGreatsword;
            case 117: return cachedSet.weaponHammer;
            case 118: return cachedSet.weaponBattleAxe;
            case 119: return cachedSet.weaponWoodAxe;
            case 129: return cachedSet.weaponStaffPurple;
            case 130: return cachedSet.weaponStaffBlue;
            case 131: return cachedSet.weaponSpear;
            default: return null;
        }
    }

#if UNITY_EDITOR
    private static Sprite LoadEditorTileByIndex(int tileIndex)
    {
        string path = $"Assets/Art/Tiles/tile_{tileIndex:0000}.png";
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return null;
    }
#endif

    private static Sprite GetProceduralWeapon(HeroType hero)
    {
        const int s = 10;
        Texture2D tex = new Texture2D(s, s);
        Color body = hero switch
        {
            HeroType.Ranger => new Color(0.6f, 0.75f, 0.95f),
            HeroType.Mage => new Color(0.7f, 0.4f, 0.95f),
            _ => new Color(0.85f, 0.65f, 0.35f)
        };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, x > 2 && x < 8 && y > 2 && y < 7 ? body : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.2f, 0.5f), 16f);
    }

    private static readonly Sprite[] proceduralHeroes = new Sprite[3];

    /// <summary>Warrior=tile_0087, Ranger (Knight)=tile_0098, Mage (Wizard)=tile_0084.</summary>
    public static Sprite GetHeroSprite(HeroType hero)
    {
        Sprite fromPack = DungeonPackLibrary.GetHero(hero);
        if (IsValidSprite(fromPack))
            return fromPack;

        Sprite fromSet = FromResourcesHero(hero);
        if (IsValidSprite(fromSet))
            return fromSet;

#if UNITY_EDITOR
        Sprite fromTiles = LoadEditorHeroTile(hero);
        if (IsValidSprite(fromTiles))
            return fromTiles;
#endif

        return GetProceduralHero(hero);
    }

    private static bool IsValidSprite(Sprite sprite)
    {
        if (sprite == null)
            return false;
        Vector2 size = sprite.bounds.size;
        return size.x > 0.02f && size.y > 0.02f;
    }

    public static Sprite GetProceduralHero(HeroType hero)
    {
        int index = Mathf.Clamp((int)hero, 0, 2);
        if (proceduralHeroes[index] != null)
            return proceduralHeroes[index];

        Color body = hero switch
        {
            HeroType.Ranger => new Color(0.35f, 0.55f, 0.95f),
            HeroType.Mage => new Color(0.55f, 0.3f, 0.9f),
            _ => new Color(0.85f, 0.55f, 0.25f)
        };
        Color accent = Color.Lerp(body, Color.white, 0.45f);
        proceduralHeroes[index] = CreateProceduralCharacter(body, accent, true);
        return proceduralHeroes[index];
    }

    private static Sprite CreateProceduralCharacter(Color body, Color accent, bool humanoid)
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        if (humanoid)
        {
            FillRect(tex, 5, 9, 6, 4, body);
            FillRect(tex, 6, 12, 4, 3, accent);
            FillRect(tex, 4, 5, 8, 5, body);
            FillRect(tex, 5, 6, 2, 2, accent);
            FillRect(tex, 9, 6, 2, 2, accent);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.35f), 16f);
    }

    private static Sprite FromResourcesHero(HeroType hero)
    {
        if (cachedSet == null)
            cachedSet = Resources.Load<ArtSpriteSet>("ArtSpriteSet");
        if (cachedSet == null)
            return null;

        return hero switch
        {
            HeroType.Warrior => cachedSet.heroWarrior,
            HeroType.Ranger => cachedSet.heroRanger,
            HeroType.Mage => cachedSet.heroMage,
            _ => cachedSet.heroWarrior
        };
    }

#if UNITY_EDITOR
    private static Sprite LoadEditorHeroTile(HeroType hero)
    {
        int tileIndex = hero switch
        {
            HeroType.Warrior => 87,
            HeroType.Ranger => 98,
            HeroType.Mage => 84,
            _ => 87
        };

        string path = $"Assets/Art/Tiles/tile_{tileIndex:0000}.png";
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return null;
    }
#endif

    private static Sprite Get(EnemyArtKind kind)
    {
        Sprite fromSet = FromResourcesSet(kind);
        if (fromSet != null)
            return fromSet;

#if UNITY_EDITOR
        Sprite fromTiles = LoadEditorTile(kind);
        if (fromTiles != null)
            return fromTiles;
#endif

        int index = (int)kind;
        if (procedural[index] != null)
            return procedural[index];

        procedural[index] = CreateProcedural(kind);
        return procedural[index];
    }

    private static Sprite FromResourcesSet(EnemyArtKind kind)
    {
        if (cachedSet == null)
            cachedSet = Resources.Load<ArtSpriteSet>("ArtSpriteSet");

        if (cachedSet == null)
            return null;

        return kind switch
        {
            EnemyArtKind.Grunt => cachedSet.enemyGrunt,
            EnemyArtKind.Runner => cachedSet.enemyRunner,
            EnemyArtKind.Brute => cachedSet.enemyBrute,
            EnemyArtKind.Elite => cachedSet.enemyElite,
            EnemyArtKind.Chest => cachedSet.chest,
            _ => null
        };
    }

#if UNITY_EDITOR
    private static Sprite LoadEditorTile(EnemyArtKind kind)
    {
        int tileIndex = kind switch
        {
            EnemyArtKind.Grunt => 62,
            EnemyArtKind.Runner => 63,
            EnemyArtKind.Brute => 64,
            EnemyArtKind.Elite => 120,
            EnemyArtKind.Chest => 89,
            _ => 40
        };

        string path = $"Assets/Art/Tiles/tile_{tileIndex:0000}.png";
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return null;
    }
#endif

    private static Sprite CreateProcedural(EnemyArtKind kind)
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color body = kind switch
        {
            EnemyArtKind.Grunt => new Color(0.75f, 0.2f, 0.2f),
            EnemyArtKind.Runner => new Color(1f, 0.55f, 0.1f),
            EnemyArtKind.Brute => new Color(0.45f, 0.2f, 0.75f),
            EnemyArtKind.Elite => new Color(0.95f, 0.82f, 0.15f),
            EnemyArtKind.Chest => new Color(0.55f, 0.35f, 0.12f),
            _ => Color.red
        };
        Color accent = kind == EnemyArtKind.Chest
            ? new Color(0.95f, 0.78f, 0.25f)
            : Color.Lerp(body, Color.white, 0.35f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);
        }

        if (kind == EnemyArtKind.Chest)
            PaintChest(tex, body, accent);
        else
            PaintMonster(tex, body, accent, kind);

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.35f), 16f);
    }

    private static void PaintMonster(Texture2D tex, Color body, Color accent, EnemyArtKind kind)
    {
        FillRect(tex, 4, 3, 8, 7, body);
        FillRect(tex, 5, 9, 6, 3, body);
        if (kind == EnemyArtKind.Brute)
            FillRect(tex, 3, 2, 10, 9, body);
        if (kind == EnemyArtKind.Runner)
            FillRect(tex, 6, 1, 4, 2, accent);

        FillRect(tex, 5, 6, 2, 2, accent);
        FillRect(tex, 9, 6, 2, 2, accent);
        if (kind == EnemyArtKind.Elite)
            FillRect(tex, 6, 10, 4, 2, accent);
    }

    private static void PaintChest(Texture2D tex, Color body, Color accent)
    {
        // Lấp gần hết chiều cao texture (16px) để GameScale.Fit cho đúng kích thước,
        // tránh tình trạng rương chỉ hiện ~1/2 do nhiều pixel trong suốt.
        FillRect(tex, 2, 1, 12, 14, body);   // thân rương: y1..14 (~88% chiều cao)
        FillRect(tex, 2, 8, 12, 2, accent);  // viền nắp
        FillRect(tex, 2, 13, 12, 2, accent); // nẹp trên cùng
        FillRect(tex, 7, 5, 2, 4, accent);   // ổ khóa
    }

    private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color color)
    {
        for (int py = y; py < y + h; py++)
        {
            for (int px = x; px < x + w; px++)
                tex.SetPixel(px, py, color);
        }
    }
}
