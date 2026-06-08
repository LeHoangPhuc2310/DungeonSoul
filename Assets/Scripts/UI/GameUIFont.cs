using TMPro;

using UnityEngine;



/// <summary>Font UI dễ đọc (LiberationSans) — hỗ trợ tiếng Việt, dùng toàn game.</summary>

public static class GameUIFont

{

    private static TMP_FontAsset cachedUi;



    public static TMP_FontAsset UI => cachedUi ??= ResolveUiFont();

    public static TMP_FontAsset Pixel => UI;

    public static TMP_FontAsset Serif => UI;



    public enum Role

    {

        HeaderTitle,

        HeaderHint,

        CardRarity,

        CardTitle,

        CardBody,

        CardStack,

        Button,

        HudLabel,

        TooltipTitle,

        TooltipBody

    }



    public static void Apply(TMP_Text text, Role role)

    {

        if (text == null)

            return;



        text.font = UI;

        if (text.font == null)

            return;



        ConfigureReadableText(text);



        switch (role)

        {

            case Role.HeaderTitle:

                text.fontSize = 32f;

                text.fontStyle = FontStyles.Bold;

                text.color = new Color(0.96f, 0.84f, 0.32f, 1f);

                text.alignment = TextAlignmentOptions.Center;

                text.lineSpacing = 0f;

                text.enableWordWrapping = false;

                break;

            case Role.HeaderHint:

                text.fontSize = 18f;

                text.fontStyle = FontStyles.Italic;

                text.color = new Color(0.86f, 0.9f, 0.96f, 1f);

                text.alignment = TextAlignmentOptions.Center;

                text.lineSpacing = 2f;

                text.enableWordWrapping = true;

                break;

            case Role.CardRarity:

                text.fontSize = 13f;

                text.fontStyle = FontStyles.Bold;

                text.color = new Color(0.92f, 0.94f, 1f, 1f);

                text.alignment = TextAlignmentOptions.Center;

                text.enableWordWrapping = false;

                break;

            case Role.CardTitle:

                text.fontSize = 20f;

                text.fontStyle = FontStyles.Bold;

                text.color = new Color(1f, 0.98f, 0.92f, 1f);

                text.alignment = TextAlignmentOptions.Center;

                text.enableWordWrapping = true;

                text.lineSpacing = -2f;

                break;

            case Role.CardBody:

                text.fontSize = 15f;

                text.fontStyle = FontStyles.Normal;

                text.color = new Color(0.88f, 0.9f, 0.96f, 1f);

                text.alignment = TextAlignmentOptions.TopLeft;

                text.enableWordWrapping = true;

                text.lineSpacing = 2f;

                text.paragraphSpacing = 2f;

                text.overflowMode = TextOverflowModes.Truncate;

                break;

            case Role.CardStack:

                text.fontSize = 12f;

                text.fontStyle = FontStyles.Italic;

                text.color = new Color(0.72f, 0.82f, 1f, 1f);

                text.alignment = TextAlignmentOptions.Center;

                text.enableWordWrapping = false;

                break;

            case Role.Button:

                text.fontSize = 18f;

                text.fontStyle = FontStyles.Bold;

                text.color = Color.white;

                text.alignment = TextAlignmentOptions.Center;

                text.enableWordWrapping = false;

                break;

            case Role.HudLabel:

                text.fontSize = 14f;

                text.fontStyle = FontStyles.Normal;

                text.color = Color.white;

                text.alignment = TextAlignmentOptions.Left;

                text.enableWordWrapping = false;

                break;

            case Role.TooltipTitle:

                text.fontSize = 15f;

                text.fontStyle = FontStyles.Bold;

                text.color = new Color(1f, 0.96f, 0.82f, 1f);

                text.alignment = TextAlignmentOptions.TopLeft;

                text.enableWordWrapping = true;

                break;

            case Role.TooltipBody:

                text.fontSize = 13f;

                text.fontStyle = FontStyles.Normal;

                text.color = new Color(0.9f, 0.93f, 0.98f, 1f);

                text.alignment = TextAlignmentOptions.TopLeft;

                text.enableWordWrapping = true;

                text.lineSpacing = 2f;

                break;

        }



        text.ForceMeshUpdate();

    }



    public static void ApplyUiFont(TMP_Text text)

    {

        if (text == null)

            return;



        text.font = UI;

        ConfigureReadableText(text);

        text.ForceMeshUpdate();

    }



    public static void ApplyHud(TMP_Text text, float fontSize = 14f)

    {

        if (text == null)

            return;



        ApplyUiFont(text);

        text.fontSize = fontSize;

        text.fontStyle = FontStyles.Normal;

        text.enableWordWrapping = false;

    }

    public static void ApplyPixelFont(TMP_Text text) => ApplyUiFont(text);

    private static TMP_FontAsset ResolveUiFont()

    {

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)

            return font;



        return TMP_Settings.defaultFontAsset;

    }



    private static void ConfigureReadableText(TMP_Text text)

    {

        text.enableAutoSizing = false;

        text.extraPadding = true;

        text.characterSpacing = 0.2f;

        text.wordSpacing = 0f;

        text.outlineWidth = 0f;

    }

}

