#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PixelArtTextureMenu
{
    private static readonly string[] Folders =
    {
        "Assets/Enemy_Animations_Set",
        "Assets/Art",
        "Assets/2D Pixel Dungeon Asset Pack"
    };

    [MenuItem("DungeonSoul/Art/Fix Pixel Art Blur (Point Filter)")]
    public static void ApplyPointFilterToPixelArt()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", Folders);
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

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
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

        EditorUtility.DisplayDialog(
            "Dungeon Soul",
            $"Đã bật Point Filter (nét pixel) cho {count} texture.\n\nGame view: để Scale = 1x để xem không bị mờ thêm.",
            "OK");
    }
}
#endif
