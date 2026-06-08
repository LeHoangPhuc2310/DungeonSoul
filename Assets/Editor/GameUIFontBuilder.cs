using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class GameUIFontBuilder
{
    private const string PixelTtfPath = "Assets/Resources/Fonts/PressStart2P.ttf";
    private const string PixelSdfPath = "Assets/Resources/Fonts/PixelGame SDF.asset";
    private const string LegacySerifPath = "Assets/Resources/TimesNewRoman SDF.asset";
    private const string LiberationPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("DungeonSoul/Fonts/Create Pixel Game SDF (Resources)")]
    public static void CreatePixelGameAssetMenu()
    {
        if (TryCreatePixelGameAsset(out string message))
            Debug.Log("[GameUIFont] " + message);
        else
            Debug.LogError("[GameUIFont] " + message);
    }

    [MenuItem("DungeonSoul/Fonts/Create Times New Roman SDF (Legacy)")]
    public static void CreateTimesNewRomanAsset()
    {
        if (TryCreateTimesNewRomanAsset(out string message))
            Debug.Log("[GameUIFont] " + message);
        else
            Debug.LogError("[GameUIFont] " + message);
    }

    /// <summary>Tạo TMP SDF pixel font trong Resources — kèm atlas + material con.</summary>
    public static bool TryCreatePixelGameAsset(out string message)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Fonts"))
            AssetDatabase.CreateFolder("Assets/Resources", "Fonts");

        Font source = AssetDatabase.LoadAssetAtPath<Font>(PixelTtfPath);
        if (source == null)
        {
            message = "Thiếu " + PixelTtfPath + " — đặt PressStart2P.ttf vào thư mục Fonts.";
            return false;
        }

        TMP_FontAsset liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelSdfPath) != null)
            AssetDatabase.DeleteAsset(PixelSdfPath);

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
            source,
            16,
            5,
            GlyphRenderMode.SDFAA,
            512,
            512,
            AtlasPopulationMode.Dynamic);
        if (asset == null)
        {
            message = "Không tạo được PixelGame SDF từ PressStart2P.";
            return false;
        }

        asset.name = "PixelGame SDF";
        asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        EnsureFontMaterial(asset);

        if (!HasValidAtlas(asset))
        {
            message = "TMP không tạo được atlas cho PressStart2P.";
            return false;
        }

        AssetDatabase.CreateAsset(asset, PixelSdfPath);
        EmbedFontSubAssets(asset);
        BindMaterialToAtlas(asset);

        if (liberation != null)
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { liberation };

        ConfigurePixelAtlas(asset);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        message = "Đã tạo " + PixelSdfPath + " (atlas + material OK).";
        return true;
    }

    private static void EmbedFontSubAssets(TMP_FontAsset asset)
    {
        if (asset == null)
            return;

        if (asset.material != null)
            AssetDatabase.AddObjectToAsset(asset.material, asset);

        Texture2D[] textures = asset.atlasTextures;
        if (textures == null)
            return;

        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D tex = textures[i];
            if (tex == null)
                continue;

            tex.name = textures.Length == 1 ? "PixelGame SDF Atlas" : "PixelGame SDF Atlas " + i;
            AssetDatabase.AddObjectToAsset(tex, asset);
        }
    }

    private static bool HasValidAtlas(TMP_FontAsset asset)
    {
        if (asset == null)
            return false;

        Texture2D[] textures = asset.atlasTextures;
        if (textures == null || textures.Length == 0)
            return false;

        for (int i = 0; i < textures.Length; i++)
        {
            if (textures[i] != null)
                return true;
        }

        return false;
    }

    private static void EnsureFontMaterial(TMP_FontAsset asset)
    {
        if (asset == null)
            return;

        if (asset.material == null)
        {
            Shader shader = Shader.Find("TextMeshPro/Distance Field");
            if (shader != null)
            {
                asset.material = new Material(shader);
                asset.material.name = "PixelGame SDF Material";
            }
        }
    }

    private static void BindMaterialToAtlas(TMP_FontAsset asset)
    {
        if (asset == null || asset.material == null)
            return;

        Texture2D[] textures = asset.atlasTextures;
        if (textures == null || textures.Length == 0 || textures[0] == null)
            return;

        Texture2D atlas = textures[0];
        asset.material.mainTexture = atlas;
        asset.material.SetFloat(ShaderUtilities.ID_TextureWidth, atlas.width);
        asset.material.SetFloat(ShaderUtilities.ID_TextureHeight, atlas.height);
        asset.material.SetFloat(ShaderUtilities.ID_GradientScale, asset.atlasPadding + 1f);
        EditorUtility.SetDirty(asset.material);
    }

    private static void ConfigurePixelAtlas(TMP_FontAsset asset)
    {
        if (asset?.atlasTextures == null)
            return;

        for (int i = 0; i < asset.atlasTextures.Length; i++)
        {
            Texture2D tex = asset.atlasTextures[i];
            if (tex == null)
                continue;

            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(tex);
        }
    }

    public static bool TryCreateTimesNewRomanAsset(out string message)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LegacySerifPath);
        if (existing != null)
        {
            message = "Đã có " + LegacySerifPath;
            return true;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        TMP_FontAsset asset = TryCreateFromOsFonts();
        if (asset == null)
            asset = CreateFromBundledFallback();

        if (asset == null)
        {
            message = "Không tạo được font SDF. Kiểm tra TextMesh Pro đã import đủ.";
            return false;
        }

        asset.name = "TimesNewRoman SDF";
        AssetDatabase.CreateAsset(asset, LegacySerifPath);
        AssetDatabase.SaveAssets();
        message = "Đã tạo " + LegacySerifPath;
        return true;
    }

    private static TMP_FontAsset TryCreateFromOsFonts()
    {
        TMP_FontAsset template = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);
        string[] names = { "Times New Roman", "Times", "Liberation Serif", "Georgia", "DejaVu Serif" };

        for (int i = 0; i < names.Length; i++)
        {
            Font os = Font.CreateDynamicFontFromOSFont(names[i], 90);
            if (os == null)
                continue;

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                os, 44, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (asset == null)
                continue;

            if (template != null && template.material != null)
                asset.material = template.material;

            return asset;
        }

        return null;
    }

    private static TMP_FontAsset CreateFromBundledFallback()
    {
        TMP_FontAsset template = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);
        if (template == null)
            return null;

        TMP_FontAsset copy = Object.Instantiate(template);
        copy.name = "TimesNewRoman SDF";
        return copy;
    }
}
