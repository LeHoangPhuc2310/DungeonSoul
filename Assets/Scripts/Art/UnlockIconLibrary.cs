using UnityEngine;

/// <summary>Icon ổ khóa từ Assets/Art/GUI/UI_Buttons/Icon/Unlock.</summary>
public static class UnlockIconLibrary
{
    private const string Root = "Assets/Art/GUI/UI_Buttons/Icon/Unlock/";

    private static Sprite commonIcon;
    private static Sprite rareIcon;
    private static Sprite epicIcon;
    private static Sprite legendaryIcon;
    private static Sprite lockedIcon;

    public static Sprite ForRarity(SkillRarity rarity)
    {
        return rarity switch
        {
            SkillRarity.Legendary => legendaryIcon ??= Load("A_Unlock1"),
            SkillRarity.Epic => epicIcon ??= Load("C_Buttons1"),
            SkillRarity.Rare => rareIcon ??= Load("B_Button2"),
            _ => commonIcon ??= Load("B_Button1")
        };
    }

    public static Sprite LockedBadge => lockedIcon ??= Load("A_Unlock3") ?? Load("C_Buttons3");

    private static Sprite Load(string fileName)
    {
#if UNITY_EDITOR
        Sprite fromEditor = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(Root + fileName + ".png");
        if (fromEditor != null)
            return fromEditor;
#endif
        return CreateFallbackIcon(fileName);
    }

    private static Sprite CreateFallbackIcon(string fileName)
    {
        bool gold = fileName.StartsWith("A_");
        bool purple = fileName.StartsWith("C_");
        Color body = gold
            ? new Color(0.95f, 0.72f, 0.2f, 1f)
            : purple
                ? new Color(0.62f, 0.38f, 0.92f, 1f)
                : new Color(0.45f, 0.62f, 0.92f, 1f);

        const int s = 32;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0f, 0f, 0f, 0f);
        Vector2 center = new Vector2(s * 0.5f, s * 0.48f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                bool shackle = x >= 11 && x <= 20 && y >= 20 && y <= 27;
                float dx = x - center.x;
                float dy = y - center.y;
                bool arch = dx * dx + (dy + 2f) * (dy + 2f) <= 64f && dy < 4f;
                bool bodyRect = x >= 9 && x <= 22 && y >= 8 && y <= 19;
                tex.SetPixel(x, y, shackle || arch || bodyRect ? body : clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), s);
    }
}
