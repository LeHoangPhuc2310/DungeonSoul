using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportGeneratedWeaponVfx
{
    private const string Root = "Assets/Resources/GeneratedWeaponVfx";

    [MenuItem("Tools/DungeonSoul/Import Generated Weapon VFX")]
    public static void ImportAll()
    {
        if (!Directory.Exists(Root))
        {
            Debug.LogError($"Missing {Root}. Run SliceGeneratedWeaponVfx.ps1 first.");
            return;
        }

        int count = 0;
        string[] pngs = Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories);
        for (int i = 0; i < pngs.Length; i++)
        {
            string path = pngs[i].Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Weapon VFX: configured {count} sprites under {Root}.");
    }
}
