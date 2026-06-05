using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime sprites from Assets/2D Pixel Dungeon Asset Pack (via Resources/DungeonPackSpriteSet).</summary>
public static class DungeonPackLibrary
{
    private const string ResourcePath = "DungeonPackSpriteSet";
    private static DungeonPackSpriteSet cached;
    private static Sprite[] cachedCoinSpin;
    private static Sprite[] proceduralCoinSpin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCacheOnDomainReload()
    {
        InvalidateCache();
    }

    public static DungeonPackSpriteSet Set
    {
        get
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<DungeonPackSpriteSet>(ResourcePath);
#if UNITY_EDITOR
            if (cached == null)
            {
                cached = UnityEditor.AssetDatabase.LoadAssetAtPath<DungeonPackSpriteSet>(
                    "Assets/Resources/DungeonPackSpriteSet.asset");
            }
#endif
            return cached;
        }
    }

    public static void InvalidateCache()
    {
        cached = null;
        cachedCoinSpin = null;
    }

    public static bool HasPack => Set != null;

    public static bool HasValidCoinSpinInSet(DungeonPackSpriteSet pack)
    {
        return SanitizeSpriteArray(TryGetCoinSpinArray(pack)).Length > 0;
    }

    public static Sprite GetHero(HeroType hero)
    {
        DungeonPackSpriteSet pack = Set;
        if (pack == null)
            return null;

        return hero switch
        {
            HeroType.Ranger => SafeSprite(pack.heroRanger),
            HeroType.Mage => SafeSprite(pack.heroMage),
            _ => SafeSprite(pack.heroWarrior)
        };
    }

    public static Sprite GetChest()
    {
        DungeonPackSpriteSet pack = Set;
        return pack != null ? SafeSprite(pack.chestClosed) : null;
    }

    public static Sprite GetCoin(bool rare)
    {
        DungeonPackSpriteSet pack = Set;
        if (pack == null)
            return null;

        return SafeSprite(rare ? pack.coinRare : pack.coinCommon);
    }

    public static Sprite[] GetCoinSpinFrames()
    {
        if (cachedCoinSpin != null && cachedCoinSpin.Length > 0)
            return cachedCoinSpin;

        DungeonPackSpriteSet pack = Set;
        Sprite[] fromPack = SanitizeSpriteArray(TryGetCoinSpinArray(pack));
        if (fromPack.Length > 0)
        {
            cachedCoinSpin = fromPack;
            return cachedCoinSpin;
        }

        if (pack != null)
        {
            cachedCoinSpin = BuildCoinSpinFallback(pack);
            if (cachedCoinSpin != null && cachedCoinSpin.Length > 0)
                return cachedCoinSpin;
        }

#if UNITY_EDITOR
        cachedCoinSpin = SanitizeSpriteArray(LoadCoinSpinFramesEditor());
        if (cachedCoinSpin != null && cachedCoinSpin.Length > 0)
            return cachedCoinSpin;
#endif

        cachedCoinSpin = BuildProceduralCoinSpin();
        return cachedCoinSpin;
    }

    private static Sprite[] TryGetCoinSpinArray(DungeonPackSpriteSet pack)
    {
        if (pack == null)
            return null;

        try
        {
            return pack.coinSpin;
        }
        catch (MissingReferenceException)
        {
            return null;
        }
    }

    private static Sprite[] SanitizeSpriteArray(Sprite[] source)
    {
        if (source == null || source.Length == 0)
            return System.Array.Empty<Sprite>();

        List<Sprite> valid = new List<Sprite>(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            Sprite sprite = SafeSprite(source[i]);
            if (sprite != null)
                valid.Add(sprite);
        }

        return valid.Count > 0 ? valid.ToArray() : System.Array.Empty<Sprite>();
    }

    private static Sprite SafeSprite(Sprite sprite)
    {
        if (sprite == null)
            return null;

        try
        {
            return sprite.bounds.size.x > 0.001f && sprite.bounds.size.y > 0.001f ? sprite : null;
        }
        catch (MissingReferenceException)
        {
            return null;
        }
    }

    private static Sprite[] BuildCoinSpinFallback(DungeonPackSpriteSet pack)
    {
        if (pack == null)
            return null;

        List<Sprite> list = new List<Sprite>(4);
        Sprite common = SafeSprite(pack.coinCommon);
        Sprite rare = SafeSprite(pack.coinRare);
        if (common != null)
            list.Add(common);
        if (rare != null && rare != common)
            list.Add(rare);

        return list.Count > 0 ? list.ToArray() : null;
    }

    private static Sprite[] BuildProceduralCoinSpin()
    {
        if (proceduralCoinSpin != null && proceduralCoinSpin.Length > 0)
            return proceduralCoinSpin;

        Color gold = new Color(1f, 0.82f, 0.2f);
        Color rim = new Color(0.85f, 0.6f, 0.1f);
        float[] widths = { 12f, 8f, 3f, 8f };
        proceduralCoinSpin = new Sprite[widths.Length];
        for (int i = 0; i < widths.Length; i++)
            proceduralCoinSpin[i] = CreateCoinFrame(widths[i], gold, rim);

        return proceduralCoinSpin;
    }

    private static Sprite CreateCoinFrame(float width, Color body, Color rim)
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color clear = new Color(0f, 0f, 0f, 0f);
        float centerX = (size - 1) * 0.5f;
        float centerY = (size - 1) * 0.5f;
        float radiusY = 5.5f;
        float radiusX = Mathf.Max(1f, width * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - centerX) / radiusX;
                float dy = (y - centerY) / radiusY;
                float dist = dx * dx + dy * dy;
                if (dist <= 1f)
                    tex.SetPixel(x, y, dist > 0.72f ? rim : body);
                else
                    tex.SetPixel(x, y, clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

#if UNITY_EDITOR
    private static Sprite[] LoadCoinSpinFramesEditor()
    {
        const string root = "Assets/2D Pixel Dungeon Asset Pack/items and trap_animation/coin";
        string[] paths =
        {
            $"{root}/coin_1.png",
            $"{root}/coin_2.png",
            $"{root}/coin_3.png",
            $"{root}/coin_4.png"
        };

        List<Sprite> list = new List<Sprite>(4);
        for (int i = 0; i < paths.Length; i++)
        {
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(paths[i]);
            for (int j = 0; j < assets.Length; j++)
            {
                if (assets[j] is Sprite sprite && SafeSprite(sprite) != null)
                {
                    list.Add(sprite);
                    break;
                }
            }
        }

        return list.Count > 0 ? list.ToArray() : null;
    }
#endif
}
