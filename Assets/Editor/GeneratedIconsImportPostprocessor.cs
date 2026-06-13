#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Tự cấu hình sprite import cho GeneratedIcons khi PNG thay đổi.</summary>
public class GeneratedIconsImportPostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("Assets/Resources/GeneratedIcons/"))
            return;
        if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }
}
#endif
