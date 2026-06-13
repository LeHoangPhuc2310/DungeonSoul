// DungeonSoul — Pixel icons generated for skills, passives, weapons (Resources/GeneratedIcons).

using System.Collections.Generic;
using UnityEngine;

public static class GeneratedPixelIconLibrary
{
    private const string Root = "GeneratedIcons";
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static bool HasPack { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Warmup()
    {
        HasPack = Skill(SkillType.DoubleShot) != null;
    }

    public static Sprite Skill(SkillType type) => Load("Skills", type.ToString());

    public static Sprite Passive(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        return Load("Passives", id);
    }

    public static Sprite Weapon(WeaponType type) => Load("Weapons", type.ToString());

    private static Sprite Load(string folder, string name)
    {
        string key = folder + "/" + name;
        if (Cache.TryGetValue(key, out Sprite cached))
            return cached;

        string path = $"{Root}/{folder}/{name}";
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex != null)
                sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        Cache[key] = sprite;
        if (sprite != null)
            HasPack = true;
        return sprite;
    }
}
