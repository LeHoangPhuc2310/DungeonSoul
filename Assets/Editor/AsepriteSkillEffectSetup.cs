using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Import ASEPRITE_skill_effect → Resources/AsepriteSkillVfx + cấu hình sprite pixel-perfect.
/// </summary>
public static class AsepriteSkillEffectSetup
{
    private const string SourceRoot = AsepriteSkillEffectPaths.SourceRoot;
    private const string ResourcesRoot = "Assets/Resources/AsepriteSkillVfx";
    public const int PixelsPerUnit = 100;

    [MenuItem("DungeonSoul/VFX/Setup ASEPRITE Skill Effects")]
    public static void SetupFromMenu() => Run(showDialog: true);

    [InitializeOnLoadMethod]
    private static void AutoSetupOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string marker = $"{ResourcesRoot}/Fire/Fire1.png";
            if (!File.Exists(marker))
                Run(showDialog: false);
        };
    }

    public static void Run(bool showDialog)
    {
        if (!Directory.Exists(SourceRoot))
        {
            Debug.LogError("[AsepriteVfx] Không tìm thấy " + SourceRoot);
            return;
        }

        EnsureFolder(ResourcesRoot);
        int pngCount = 0;

        for (int i = 0; i < AsepriteSkillEffectPaths.AllCategories.Length; i++)
        {
            string category = AsepriteSkillEffectPaths.AllCategories[i];
            string srcDir = $"{SourceRoot}/{category}";
            string dstDir = $"{ResourcesRoot}/{category}";

            if (!Directory.Exists(srcDir))
                continue;

            EnsureFolder(dstDir);
            string[] files = Directory.GetFiles(srcDir, "*.png", SearchOption.TopDirectoryOnly);
            for (int f = 0; f < files.Length; f++)
            {
                string fileName = Path.GetFileName(files[f]);
                string dst = Path.Combine(dstDir, fileName);
                File.Copy(files[f], dst, overwrite: true);

                string srcMeta = files[f] + ".meta";
                string dstMeta = dst + ".meta";
                if (File.Exists(srcMeta))
                    File.Copy(srcMeta, dstMeta, overwrite: true);

                ConfigureImporter(dst);
                pngCount++;
            }
        }

        AssetDatabase.Refresh();
        AsepriteSkillVfxLoader.ClearCache();

        Debug.Log($"[AsepriteVfx] Đã sync {pngCount} PNG → {ResourcesRoot}. PPU={PixelsPerUnit}");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("ASEPRITE Skill VFX",
                $"Đã import {pngCount} sprite sheet.\n\n" +
                "• SkillVfxLibrary → Fire/Ice/Lightning/Poison/Arcane/Slash\n" +
                "• EffectLibrary → nổ, crit, spawn, boss\n" +
                "• GameIconLibrary → icon skill UI\n\n" +
                "Nhấn Play và thử skill / đánh quái để xem hiệu ứng.",
                "OK");
        }
    }

    private static void ConfigureImporter(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
