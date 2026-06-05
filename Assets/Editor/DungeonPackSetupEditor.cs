#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DungeonPackSetupEditor
{
    private const string PackRoot = "Assets/2D Pixel Dungeon Asset Pack";
    private const string ResourcePath = "Assets/Resources/DungeonPackSpriteSet.asset";

    [MenuItem("DungeonSoul/Assets/Link 2D Pixel Dungeon Pack")]
    public static void BuildAndLink()
    {
        BuildInternal(showDialog: true);
    }

    public static void BuildSilent()
    {
        BuildInternal(showDialog: false);
    }

    private static void BuildInternal(bool showDialog)
    {
        if (!AssetDatabase.IsValidFolder(PackRoot))
        {
            Debug.LogError("[DungeonPack] Missing folder: " + PackRoot);
            return;
        }

        FixPixelImport(PackRoot);
        EnsureResourcesFolder();

        DungeonPackSpriteSet set = AssetDatabase.LoadAssetAtPath<DungeonPackSpriteSet>(ResourcePath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<DungeonPackSpriteSet>();
            AssetDatabase.CreateAsset(set, ResourcePath);
        }

        set.hpBarFrame = LoadSprite($"{PackRoot}/interface/square_left_1.png");
        set.hpBarFill = LoadSprite($"{PackRoot}/interface/square_left_2.png");
        set.expBarFrame = LoadSprite($"{PackRoot}/interface/square_up_down_1.png");
        set.expBarFill = LoadSprite($"{PackRoot}/interface/square_up_down_2.png");
        set.chestClosed = LoadSprite($"{PackRoot}/items and trap_animation/chest/chest_1.png");
        set.coinCommon = LoadSprite($"{PackRoot}/items and trap_animation/coin/coin_1.png");
        set.coinRare = LoadSprite($"{PackRoot}/items and trap_animation/coin/coin_4.png");
        set.coinSpin = LoadSpriteArray(
            $"{PackRoot}/items and trap_animation/coin/coin_1.png",
            $"{PackRoot}/items and trap_animation/coin/coin_2.png",
            $"{PackRoot}/items and trap_animation/coin/coin_3.png",
            $"{PackRoot}/items and trap_animation/coin/coin_4.png");
        set.heroWarrior = LoadSprite($"{PackRoot}/Character_animation/priests_idle/priest1/v1/priest1_v1_1.png");
        set.heroRanger = LoadSprite($"{PackRoot}/Character_animation/priests_idle/priest2/v1/priest2_v1_1.png");
        set.heroMage = LoadSprite($"{PackRoot}/Character_animation/priests_idle/priest3/v1/priest3_v1_1.png");

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        DungeonPackLibrary.InvalidateCache();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Soul",
                "Đã gắn 2D Pixel Dungeon Pack:\n• Thanh HP/EXP\n• Rương, xu\n• Hero (priest)\n\nVào Play Mode để xem.",
                "OK");
        }
        else
        {
            Debug.Log("[DungeonPack] Linked 2D Pixel Dungeon Asset Pack.");
        }
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
    }

    private static Sprite[] LoadSpriteArray(params string[] paths)
    {
        System.Collections.Generic.List<Sprite> list = new System.Collections.Generic.List<Sprite>(paths.Length);
        for (int i = 0; i < paths.Length; i++)
        {
            Sprite s = LoadSprite(paths[i]);
            if (s != null)
                list.Add(s);
        }

        return list.ToArray();
    }

    private static Sprite LoadSprite(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            Debug.LogWarning("[DungeonPack] Missing: " + assetPath);
            return null;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        Debug.LogWarning("[DungeonPack] No sprite in: " + assetPath);
        return null;
    }

    private static void FixPixelImport(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        int count = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = false;
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                count++;
            }
        }

        if (count > 0)
            Debug.Log($"[DungeonPack] Point filter applied to {count} textures.");
    }
}
#endif
