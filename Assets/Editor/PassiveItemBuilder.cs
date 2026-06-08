using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Tạo 12 PassiveItemData Phase 1 trong Resources/PassiveItems/.</summary>
public static class PassiveItemBuilder
{
    private const string Folder = "Assets/Resources/PassiveItems";

    [MenuItem("Tools/DungeonSoul/Create All Passive Items")]
    public static void CreateAllPassiveItems()
    {
        EnsureFolder();

        CreateDefense("ao_giap_da", "Áo Giáp Da", "Giảm sát thương nhận 4% mỗi cấp.",
            Repeat(0.04f, 5), SkillRarity.Common);
        CreateFlat("tim_rong", "Tim Rỗng", "Tăng HP tối đa +20 mỗi cấp.",
            PassiveStatModifierType.HP, Repeat(20f, 5), false, SkillRarity.Common);
        CreatePercent("canh_quat", "Cánh Quạ", "Tăng tốc độ di chuyển 5% mỗi cấp.",
            PassiveStatModifierType.MoveSpeed, Repeat(0.05f, 5), SkillRarity.Common);
        CreatePercent("mong_vuot", "Móng Vuốt", "Tăng sát thương 10% mỗi cấp.",
            PassiveStatModifierType.Damage, Repeat(0.1f, 5), SkillRarity.Common,
            WeaponType.PoisonDagger, WeaponType.DeathDagger, 3f);
        CreatePercent("dong_ho_cat", "Đồng Hồ Cát", "Giảm hồi chiêu 8% mỗi cấp.",
            PassiveStatModifierType.CooldownReduction, Repeat(0.08f, 5), SkillRarity.Rare,
            WeaponType.IronBow, WeaponType.StormBow, 1f);
        CreatePercent("vuong_mien_exp", "Vương Miện", "Tăng EXP nhận được 8% mỗi cấp.",
            PassiveStatModifierType.ExpGain, Repeat(0.08f, 5), SkillRarity.Rare);
        CreatePercent("luoi_sac", "Lưỡi Sắc", "Tăng tỉ lệ chí mạng 5% mỗi cấp.",
            PassiveStatModifierType.CritChance, Repeat(0.05f, 5), SkillRarity.Rare);
        CreateFlat("tui_tham_lam", "Túi Tham Lam", "Tăng bán kính hút EXP +0.5 mỗi cấp.",
            PassiveStatModifierType.Magnet, Repeat(0.5f, 5), false, SkillRarity.Rare);
        CreatePercent("luoi_lua", "Lưỡi Lửa", "Tăng tỉ lệ gây bỏng 10% mỗi cấp.",
            PassiveStatModifierType.BurnChance, Repeat(0.1f, 3), SkillRarity.Epic,
            WeaponType.FireStaff, WeaponType.DragonStaff, 1.6f);
        CreatePercent("hon_quy", "Hồn Quỷ", "Hút máu 3% sát thương gây ra mỗi cấp.",
            PassiveStatModifierType.LifeSteal, Repeat(0.03f, 3), SkillRarity.Epic);
        CreateFlat("manh_hon_rong", "Mảnh Hồn Rồng", "Thêm 1 đạn mỗi cấp.",
            PassiveStatModifierType.ProjectileCount, Repeat(1f, 3), false, SkillRarity.Epic);
        CreateRevive("vuong_mien_vinh_cuu", "Vương Miện Vĩnh Cửu",
            "Hồi sinh 1 lần với 50% HP khi chết.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PassiveItemBuilder] Đã tạo 12 passive trong " + Folder);
    }

    [MenuItem("Tools/DungeonSoul/Debug/Spawn Passive - Common")]
    private static void DebugSpawnCommon() => DebugSpawn(SkillRarity.Common);

    [MenuItem("Tools/DungeonSoul/Debug/Spawn Passive - Rare")]
    private static void DebugSpawnRare() => DebugSpawn(SkillRarity.Rare);

    [MenuItem("Tools/DungeonSoul/Debug/Spawn Passive - Epic")]
    private static void DebugSpawnEpic() => DebugSpawn(SkillRarity.Epic);

    [MenuItem("Tools/DungeonSoul/Debug/Spawn Passive - Legendary")]
    private static void DebugSpawnLegendary() => DebugSpawn(SkillRarity.Legendary);

    private static void DebugSpawn(SkillRarity rarity)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Debug] Chỉ dùng khi đang Play mode.");
            return;
        }

        if (PassiveItemManager.Instance == null)
        {
            Debug.LogWarning("[Debug] PassiveItemManager chưa có trong scene.");
            return;
        }

        PassiveItemManager.Instance.DebugGrantRandom(rarity);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources", "PassiveItems");
    }

    private static float[] Repeat(float value, int count)
    {
        float[] arr = new float[count];
        for (int i = 0; i < count; i++)
            arr[i] = value;
        return arr;
    }

    private static void CreateDefense(string id, string name, string desc, float[] values, SkillRarity rarity)
    {
        CreateAsset(id, name, desc, PassiveStatModifierType.Defense, values, true, rarity);
    }

    private static void CreatePercent(string id, string name, string desc, PassiveStatModifierType stat,
        float[] values, SkillRarity rarity,
        WeaponType evolveFrom = WeaponType.IronBow, WeaponType evolveTo = WeaponType.IronBow, float evolveDmg = 1f)
    {
        PassiveItemData asset = CreateAsset(id, name, desc, stat, values, true, rarity);
        if (evolveTo != evolveFrom)
        {
            asset.enablesWeaponEvolve = true;
            asset.evolveTargetWeapon = evolveFrom;
            asset.evolveResultWeapon = evolveTo;
            asset.evolveDamageMultiplier = evolveDmg;
            EditorUtility.SetDirty(asset);
        }
    }

    private static void CreateFlat(string id, string name, string desc, PassiveStatModifierType stat,
        float[] values, bool isPercent, SkillRarity rarity)
    {
        CreateAsset(id, name, desc, stat, values, isPercent, rarity);
    }

    private static void CreateRevive(string id, string name, string desc)
    {
        PassiveItemData asset = CreateAsset(id, name, desc, PassiveStatModifierType.Revive,
            new[] { 1f }, false, SkillRarity.Legendary);
        asset.maxLevel = 1;
        EditorUtility.SetDirty(asset);
    }

    private static PassiveItemData CreateAsset(string id, string name, string desc,
        PassiveStatModifierType stat, float[] values, bool isPercent, SkillRarity rarity)
    {
        string path = Path.Combine(Folder, "Passive_" + id + ".asset").Replace("\\", "/");
        PassiveItemData existing = AssetDatabase.LoadAssetAtPath<PassiveItemData>(path);
        PassiveItemData asset = existing != null ? existing : ScriptableObject.CreateInstance<PassiveItemData>();

        asset.id = id;
        asset.displayName = name;
        asset.description = desc;
        asset.statModifierType = stat;
        asset.valuePerLevel = values;
        asset.maxLevel = values != null ? values.Length : 5;
        asset.isPercent = isPercent;
        asset.rarity = rarity;
        asset.enablesWeaponEvolve = false;

        if (existing == null)
            AssetDatabase.CreateAsset(asset, path);
        else
            EditorUtility.SetDirty(asset);

        return asset;
    }
}
