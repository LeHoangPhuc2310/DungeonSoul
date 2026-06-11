// DungeonSoul — HpBarArtLibrary.cs — Sprite thanh máu từ pack zip (Art/HP_Bar).

using UnityEngine;

public static class HpBarArtLibrary
{
    private static Sprite bg;
    private static Sprite fillHigh;
    private static Sprite fillLow;

    public static Sprite Background => bg ??= Load("bg");
    public static Sprite FillHigh => fillHigh ??= Load("red");
    public static Sprite FillLow => fillLow ??= Load("green");

    private static Sprite Load(string name)
    {
        Sprite fromResources = Resources.Load<Sprite>($"UI/HPBar/{name}");
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        Sprite fromArt = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/HP_Bar/{name}.png");
        if (fromArt != null)
            return fromArt;
#endif
        return CreateProcedural(name);
    }

    private static Sprite CreateProcedural(string name)
    {
        const int w = 20;
        const int h = 6;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color inner = name switch
        {
            "bg" => new Color(0.14f, 0.12f, 0.16f, 0.95f),
            "green" => new Color(0.32f, 0.9f, 0.42f, 1f),
            _ => new Color(0.92f, 0.26f, 0.26f, 1f)
        };
        Color border = new Color(0.05f, 0.05f, 0.08f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                tex.SetPixel(x, y, edge ? border : inner);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 20f);
    }
}
