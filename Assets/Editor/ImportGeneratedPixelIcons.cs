using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Import settings + gán icon pixel vào PassiveItemData.</summary>
public static class ImportGeneratedPixelIcons
{
    private const string IconsRoot = "Assets/Resources/GeneratedIcons";

    [MenuItem("Tools/DungeonSoul/Import Generated Pixel Icons")]
    public static void ImportAll()
    {
        if (!Directory.Exists(IconsRoot))
        {
            Debug.LogError($"Missing folder: {IconsRoot}. Run SliceGeneratedPixelIcons.ps1 first.");
            return;
        }

        int imported = 0;
        string[] pngs = Directory.GetFiles(IconsRoot, "*.png", SearchOption.AllDirectories);
        for (int i = 0; i < pngs.Length; i++)
        {
            string path = pngs[i].Replace('\\', '/');
            if (ConfigureSpriteImport(path))
                imported++;
        }

        AssetDatabase.Refresh();

        int assigned = AssignPassiveIcons();
        Debug.Log($"Generated pixel icons: configured {imported} sprites, assigned {assigned} passives.");
    }

    private static bool ConfigureSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return false;

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }

        if (importer.spritePixelsPerUnit != 100f)
        {
            importer.spritePixelsPerUnit = 100f;
            dirty = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (importer.alphaIsTransparency != true)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        if (dirty)
            importer.SaveAndReimport();

        return true;
    }

    private static int AssignPassiveIcons()
    {
        string[] guids = AssetDatabase.FindAssets("t:PassiveItemData", new[] { "Assets/Resources/PassiveItems" });
        int count = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            PassiveItemData passive = AssetDatabase.LoadAssetAtPath<PassiveItemData>(assetPath);
            if (passive == null || string.IsNullOrEmpty(passive.id))
                continue;

            string iconPath = $"{IconsRoot}/Passives/{passive.id}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite == null)
                continue;

            if (passive.icon == sprite)
                continue;

            passive.icon = sprite;
            EditorUtility.SetDirty(passive);
            count++;
        }

        AssetDatabase.SaveAssets();
        return count;
    }
}
