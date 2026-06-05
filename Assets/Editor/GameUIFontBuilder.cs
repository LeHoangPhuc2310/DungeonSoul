using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

public static class GameUIFontBuilder
{
    private const string OutputPath = "Assets/Resources/TimesNewRoman SDF.asset";
    private const string LiberationPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("DungeonSoul/Fonts/Create Times New Roman SDF (Resources)")]
    public static void CreateTimesNewRomanAsset()
    {
        if (TryCreateTimesNewRomanAsset(out string message))
            Debug.Log("[GameUIFont] " + message);
        else
            Debug.LogError("[GameUIFont] " + message);
    }

    /// <summary>Tạo hoặc tái sử dụng font SDF trong Resources. Không throw khi OS font thiếu.</summary>
    public static bool TryCreateTimesNewRomanAsset(out string message)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
        if (existing != null)
        {
            message = "Đã có " + OutputPath;
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
        AssetDatabase.CreateAsset(asset, OutputPath);
        AssetDatabase.SaveAssets();
        message = "Đã tạo " + OutputPath;
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
