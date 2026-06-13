using UnityEngine;

/// <summary>Icon Soul Points (meta) — Resources/GeneratedIcons/Meta/Soul hoặc procedural.</summary>
public static class SoulIconLibrary
{
    private const string ResourcePath = "GeneratedIcons/Meta/Soul";
    private static Sprite cached;

    public static Sprite Get()
    {
        if (cached != null)
            return cached;

        cached = Resources.Load<Sprite>(ResourcePath);
        if (cached == null)
        {
            Texture2D tex = Resources.Load<Texture2D>(ResourcePath);
            if (tex != null)
                cached = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        if (cached == null)
            cached = CreateProceduralSoul();

        return cached;
    }

    public static Color Tint => new Color(0.72f, 0.88f, 1f, 1f);

    private static Sprite CreateProceduralSoul()
    {
        const int s = 48;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Vector2 center = new Vector2(s * 0.5f, s * 0.52f);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float dx = (x - center.x) / 14f;
                float dy = (y - center.y) / 18f;
                float body = dx * dx + dy * dy;
                float tail = Mathf.Max(0f, 1f - (y - center.y - 10f) / 10f) * Mathf.Exp(-dx * dx * 3f);

                Color c = Color.clear;
                if (body <= 1f)
                {
                    float edge = Mathf.Clamp01((1f - body) * 2.2f);
                    c = Color.Lerp(new Color(0.35f, 0.55f, 0.95f, 0.35f), new Color(0.85f, 0.95f, 1f, 1f), edge);
                    if (body < 0.35f)
                        c = Color.Lerp(c, Color.white, 1f - body / 0.35f);
                }
                else if (tail > 0.12f && y > center.y)
                {
                    c = new Color(0.45f, 0.65f, 1f, tail * 0.75f);
                }

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.45f), 100f);
    }
}
