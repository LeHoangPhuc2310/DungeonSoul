using System.IO;
using UnityEditor;
using UnityEngine;

public static class ImportGeneratedSkillVfx
{
    private static readonly string[] Roots =
    {
        "Assets/Resources/GeneratedSkillVfx",
        "Assets/Resources/GeneratedHeldWeapons"
    };

    [MenuItem("Tools/DungeonSoul/Import Generated Skill VFX")]
    public static void ImportAll()
    {
        int count = 0;
        for (int r = 0; r < Roots.Length; r++)
        {
            if (!Directory.Exists(Roots[r]))
                continue;

        foreach (string path in Directory.GetFiles(Roots[r], "*.png", SearchOption.AllDirectories))
        {
            string asset = path.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(asset) as TextureImporter;
            if (importer == null) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            count++;
        }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Generated character VFX: {count} sprites imported.");
    }
}
