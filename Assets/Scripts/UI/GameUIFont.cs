using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>Times New Roman (hoặc serif gần giống) cho UI — dễ đọc hơn pixel font.</summary>
public static class GameUIFont
{
    private static TMP_FontAsset cached;

    public static TMP_FontAsset Serif => cached ??= ResolveSerifFont();

    public enum Role
    {
        HeaderTitle,
        HeaderHint,
        CardRarity,
        CardTitle,
        CardBody,
        CardStack,
        Button
    }

    public static void Apply(TMP_Text text, Role role)
    {
        if (text == null)
            return;

        TMP_FontAsset font = Serif;
        if (font == null)
            return;

        text.font = font;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        text.extraPadding = true;
        text.characterSpacing = 0.4f;
        // Runtime OS fonts may lack outline shader — setting outline causes NullReferenceException.
        text.outlineWidth = 0f;

        switch (role)
        {
            case Role.HeaderTitle:
                text.fontSize = 38f;
                text.fontStyle = FontStyles.Bold;
                text.color = new Color(0.96f, 0.84f, 0.32f, 1f);
                text.alignment = TextAlignmentOptions.Center;
                text.lineSpacing = 0f;
                text.enableWordWrapping = false;
                break;
            case Role.HeaderHint:
                text.fontSize = 19f;
                text.fontStyle = FontStyles.Italic;
                text.color = new Color(0.86f, 0.9f, 0.96f, 1f);
                text.alignment = TextAlignmentOptions.Center;
                text.lineSpacing = 2f;
                text.enableWordWrapping = true;
                break;
            case Role.CardRarity:
                text.fontSize = 13f;
                text.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
                text.color = new Color(0.92f, 0.94f, 1f, 1f);
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = false;
                break;
            case Role.CardTitle:
                text.fontSize = 21f;
                text.fontStyle = FontStyles.Bold;
                text.color = new Color(1f, 0.98f, 0.92f, 1f);
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = true;
                text.lineSpacing = -2f;
                break;
            case Role.CardBody:
                text.fontSize = 16f;
                text.fontStyle = FontStyles.Normal;
                text.color = new Color(0.88f, 0.9f, 0.96f, 1f);
                text.alignment = TextAlignmentOptions.TopLeft;
                text.enableWordWrapping = true;
                text.lineSpacing = 6f;
                text.paragraphSpacing = 2f;
                text.overflowMode = TextOverflowModes.Truncate;
                break;
            case Role.CardStack:
                text.fontSize = 13f;
                text.fontStyle = FontStyles.Italic;
                text.color = new Color(0.72f, 0.82f, 1f, 1f);
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = false;
                break;
            case Role.Button:
                text.fontSize = 20f;
                text.fontStyle = FontStyles.Bold;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                text.enableWordWrapping = false;
                break;
        }

        if (text.font != null)
            text.ForceMeshUpdate();
    }

    private static TMP_FontAsset ResolveSerifFont()
    {
        TMP_FontAsset bundled = Resources.Load<TMP_FontAsset>("TimesNewRoman SDF");
        if (bundled != null)
            return bundled;

        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fallback == null)
            fallback = TMP_Settings.defaultFontAsset;

        string[] names = { "Times New Roman", "Times", "Liberation Serif", "Georgia", "DejaVu Serif" };
        for (int i = 0; i < names.Length; i++)
        {
            Font os = Font.CreateDynamicFontFromOSFont(names[i], 90);
            if (os == null || os.name.IndexOf("arial", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(
                os, 44, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (created == null)
                continue;

            created.name = "TimesNewRoman_Runtime";
            if (fallback != null && fallback.material != null)
                created.material = fallback.material;

            return created;
        }

        return fallback;
    }
}
