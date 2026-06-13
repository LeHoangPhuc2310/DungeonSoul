using System.Collections.Generic;
using UnityEngine;

public static class GeneratedHeldWeaponLibrary
{
    private const string Root = "GeneratedHeldWeapons";
    private static readonly Dictionary<WeaponType, Sprite> Cache = new Dictionary<WeaponType, Sprite>();

    public static bool HasPack { get; private set; }

    public static Sprite GetHeld(WeaponType type)
    {
        if (Cache.TryGetValue(type, out Sprite cached))
            return cached;

        Sprite s = Resources.Load<Sprite>($"{Root}/{type}");
        Cache[type] = s;
        if (s != null)
            HasPack = true;
        return s;
    }
}
