#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GuiSetupEditor
{
    private const string GuiRoot = "Assets/Art/GUI";
    private const string CardHi = GuiRoot + "/Card Frame Images/High Resolution";
    private const string ResourcePath = "Assets/Resources/GuiSpriteSet.asset";

    [MenuItem("DungeonSoul/Assets/Link GUI Art Pack")]
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
        if (!AssetDatabase.IsValidFolder(GuiRoot))
        {
            Debug.LogError("[GUI] Missing folder: " + GuiRoot);
            return;
        }

        FixGuiImport(GuiRoot);
        EnsureResourcesFolder();

        GuiSpriteSet set = AssetDatabase.LoadAssetAtPath<GuiSpriteSet>(ResourcePath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<GuiSpriteSet>();
            AssetDatabase.CreateAsset(set, ResourcePath);
        }

        set.hpBarFrame = LoadNamedSprite(GuiRoot + "/HUD Menus.png", "HUD Menus_3");
        set.expBarFrame = LoadNamedSprite(GuiRoot + "/HUD Menus.png", "HUD Menus_1");
        set.menuPanel = LoadNamedSprite(GuiRoot + "/HUD Menus Blue Green.png", "HUD Menus Blue Green_0");
        set.dialogPanel = LoadSprite($"{CardHi}/card_frame_back_blue.png");

        set.buttonPrimary = LoadSprite($"{CardHi}/card_frame_option_blue.png");
        set.buttonSecondary = LoadSprite($"{CardHi}/card_frame_option_green.png");
        set.buttonDanger = LoadSprite($"{CardHi}/card_frame_option_red.png");
        set.iconPause = LoadSprite($"{GuiRoot}/UI_Buttons/Icon/Pause/C_Buttons15.png");
        set.iconPlay = LoadSprite($"{GuiRoot}/UI_Buttons/Icon/Play/B_Button5.png");
        set.iconClose = LoadSprite($"{GuiRoot}/UI_Buttons/Icon/Close/A_Close3.png");
        set.iconSettings = LoadSprite($"{GuiRoot}/UI_Buttons/Icon/Settings/A_Settings1.png");
        set.iconBack = LoadSprite($"{GuiRoot}/UI_Buttons/Icon/Back/A_Back2.png");

        set.cardCommon = LoadSprite($"{CardHi}/card_frame_blank_green.png");
        set.cardRare = LoadSprite($"{CardHi}/card_frame_blank_blue.png");
        set.cardEpic = LoadSprite($"{CardHi}/card_frame_blank_purple.png");
        set.cardLegendary = LoadSprite($"{CardHi}/card_frame_blank_yellow.png");

        // Portrait blank cards (viền neon + nền tối) — màn chọn nhân vật.
        set.cardWarrior = LoadSprite($"{CardHi}/card_frame_blank_red.png");
        set.cardRanger = LoadSprite($"{CardHi}/card_frame_blank_blue.png");
        set.cardMage = LoadSprite($"{CardHi}/card_frame_blank_purple.png");
        set.cardSelected = LoadSprite($"{CardHi}/card_frame_blank_yellow.png");

        set.cardWeaponBow = LoadSprite($"{CardHi}/card_frame_blue.png");
        set.cardWeaponStaff = LoadSprite($"{CardHi}/card_frame_red.png");
        set.cardWeaponBlade = LoadSprite($"{CardHi}/card_frame_green.png");
        set.cardWeaponHoly = LoadSprite($"{CardHi}/card_frame_yellow.png");
        set.cardWeaponThunder = LoadSprite($"{CardHi}/card_frame_purple.png");

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        GuiArtLibrary.InvalidateCache();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Soul",
                "Đã gắn Assets/Art/GUI:\n• Thanh HP / EXP\n• Khung menu & thẻ skill\n• Nút pause / play / đóng\n\nVào Play Mode để xem.",
                "OK");
        }
        else
        {
            Debug.Log("[GUI] Linked Art/GUI pack.");
        }
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
    }

    private static Sprite LoadSprite(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            Debug.LogWarning("[GUI] Missing: " + assetPath);
            return null;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        Debug.LogWarning("[GUI] No sprite in: " + assetPath);
        return null;
    }

    private static Sprite LoadNamedSprite(string texturePath, string spriteName)
    {
        if (!File.Exists(texturePath))
        {
            Debug.LogWarning("[GUI] Missing texture: " + texturePath);
            return null;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        Debug.LogWarning("[GUI] Sprite not found: " + spriteName + " in " + texturePath);
        return null;
    }

    private static void FixGuiImport(string folder)
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
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                count++;
            }
        }

        if (count > 0)
            Debug.Log($"[GUI] Reimported {count} textures.");
    }
}
#endif
