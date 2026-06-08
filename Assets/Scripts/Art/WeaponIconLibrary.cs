// DungeonSoul — WeaponIconLibrary.cs — Sprite vũ khí thật từ Assets/Art/Sprite/Weapon.
// Map 12 WeaponType + 3 hero → icon vũ khí (kiếm/gậy/cung/rìu...). Dùng cho vũ khí cầm tay,
// thẻ chọn vũ khí, và weapon slot HUD.

using System.Collections.Generic;
using UnityEngine;

public static class WeaponIconLibrary
{
    private const string SwordDir = "Assets/Art/Sprite/Weapon/ALL/Icons_background/swords/";
    private const string StaffDir = "Assets/Art/Sprite/Weapon/ALL/Icons_background/staffs/";
    private const string AxeDir = "Assets/Art/Sprite/Weapon/ALL/Icons_background/axes/";
    private const string TridentDir = "Assets/Art/Sprite/Weapon/ALL/Icons_background/tridents/";
    private const string SlingDir = "Assets/Art/Sprite/Weapon/ALL/Icons_background/slingshots/";

    // Sprite không nền — dùng cho vũ khí cầm trên tay (tránh hình thoi nâu của Icons_background).
    private const string SwordHeldDir = "Assets/Art/Sprite/Weapon/ALL/Icons_no_background/swords/";
    private const string StaffHeldDir = "Assets/Art/Sprite/Weapon/ALL/Icons_no_background/staffs/";
    private const string SlingHeldDir = "Assets/Art/Sprite/Weapon/ALL/Icons_no_background/slingshots/";

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    /// <summary>Icon cho từng loại vũ khí.</summary>
    public static Sprite GetWeapon(WeaponType type)
    {
        switch (type)
        {
            // Cung / ná → slingshots
            case WeaponType.IronBow: return Load(SlingDir + "rpg_icons_141.png");
            case WeaponType.StormBow: return Load(SlingDir + "rpg_icons_144.png");
            // Gậy phép → staffs
            case WeaponType.FireStaff: return Load(StaffDir + "rpg_icons_69.png");
            case WeaponType.DragonStaff: return Load(StaffDir + "rpg_icons_73.png");
            case WeaponType.FrostWand: return Load(StaffDir + "rpg_icons_71.png");
            case WeaponType.BlizzardWand: return Load(StaffDir + "rpg_icons_75.png");
            case WeaponType.ThunderRod: return Load(StaffDir + "rpg_icons_77.png");
            case WeaponType.ZeusRod: return Load(StaffDir + "rpg_icons_79.png");
            // Dao găm / kiếm → swords
            case WeaponType.PoisonDagger: return Load(SwordDir + "rpg_icons_25.png");
            case WeaponType.DeathDagger: return Load(SwordDir + "rpg_icons_30.png");
            case WeaponType.HolyCross: return Load(SwordDir + "rpg_icons_35.png");
            case WeaponType.HolyNova: return Load(SwordDir + "rpg_icons_40.png");
            default: return Load(SwordDir + "rpg_icons_27.png");
        }
    }

    /// <summary>Sprite vũ khí cầm tay — không có nền icon.</summary>
    public static Sprite GetWeaponHeld(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.IronBow: return Load(SlingHeldDir + "rpg_icons141.png");
            case WeaponType.StormBow: return Load(SlingHeldDir + "rpg_icons144.png");
            case WeaponType.FireStaff: return Load(StaffHeldDir + "rpg_icons69.png");
            case WeaponType.DragonStaff: return Load(StaffHeldDir + "rpg_icons73.png");
            case WeaponType.FrostWand: return Load(StaffHeldDir + "rpg_icons71.png");
            case WeaponType.BlizzardWand: return Load(StaffHeldDir + "rpg_icons75.png");
            case WeaponType.ThunderRod: return Load(StaffHeldDir + "rpg_icons77.png");
            case WeaponType.ZeusRod: return Load(StaffHeldDir + "rpg_icons79.png");
            case WeaponType.PoisonDagger: return Load(SwordHeldDir + "rpg_icons25.png");
            case WeaponType.DeathDagger: return Load(SwordHeldDir + "rpg_icons30.png");
            case WeaponType.HolyCross: return Load(SwordHeldDir + "rpg_icons35.png");
            case WeaponType.HolyNova: return Load(SwordHeldDir + "rpg_icons40.png");
            default: return Load(SwordHeldDir + "rpg_icons27.png");
        }
    }

    /// <summary>Vũ khí cầm tay mặc định theo hero.</summary>
    public static Sprite GetHeroWeaponHeld(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Warrior: return Load(SwordHeldDir + "rpg_icons27.png");
            case HeroType.Ranger: return Load(SlingHeldDir + "rpg_icons141.png");
            case HeroType.Mage: return Load(StaffHeldDir + "rpg_icons69.png");
            default: return Load(SwordHeldDir + "rpg_icons27.png");
        }
    }

    /// <summary>Vũ khí mặc định theo hero (khi chưa nhặt vũ khí nào).</summary>
    public static Sprite GetHeroWeapon(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Warrior: return Load(SwordDir + "rpg_icons_27.png"); // kiếm
            case HeroType.Ranger: return Load(SlingDir + "rpg_icons_141.png"); // cung
            case HeroType.Mage: return Load(StaffDir + "rpg_icons_69.png");    // gậy
            default: return Load(SwordDir + "rpg_icons_27.png");
        }
    }

    public static Color Tint(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff: return new Color(1f, 0.6f, 0.4f);
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand: return new Color(0.6f, 0.9f, 1f);
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger: return new Color(0.6f, 1f, 0.55f);
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod: return new Color(0.85f, 0.75f, 1f);
            default: return Color.white;
        }
    }

    public static bool HasPack => GetWeapon(WeaponType.IronBow) != null;

    private static Sprite Load(string path)
    {
        if (cache.TryGetValue(path, out Sprite cached) && cached != null)
            return cached;

#if UNITY_EDITOR
        Sprite s = LoadBestSpriteAtPath(path);
        if (s != null)
        {
            cache[path] = s;
            return s;
        }
#endif
        return null;
    }

#if UNITY_EDITOR
    /// <summary>PNG Multiple sprites: lấy slice lớn nhất (tránh mảnh 9×9 bị co thành vô hình).</summary>
    private static Sprite LoadBestSpriteAtPath(string path)
    {
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
            return best;

        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
#endif
}
