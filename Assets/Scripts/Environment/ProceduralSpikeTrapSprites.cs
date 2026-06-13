using System.Collections.Generic;
using UnityEngine;

/// <summary>Sprite chông procedural — fallback khi thiếu PNG trong Resources.</summary>
public static class ProceduralSpikeTrapSprites
{
    private static Sprite[] cached;

    public static Sprite[] Build()
    {
        if (cached != null && cached.Length > 0)
            return cached;

        var frames = new List<Sprite>(4);
        int[] heights = { 2, 5, 9, 9 };
        for (int f = 0; f < heights.Length; f++)
        {
            frames.Add(BuildFrame(heights[f], f));
        }

        cached = frames.ToArray();
        return cached;
    }

    private static Sprite BuildFrame(int spikeHeight, int seed)
    {
        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color metal = new Color(0.55f, 0.58f, 0.62f, 1f);
        Color tip = new Color(0.78f, 0.82f, 0.88f, 1f);
        Color baseColor = new Color(0.35f, 0.32f, 0.3f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);
        }

        int baseY = 2;
        for (int x = 2; x < size - 2; x++)
            tex.SetPixel(x, baseY, baseColor);

        if (spikeHeight > 1)
        {
            int[] xs = { 4, 7, 10, 13 };
            for (int i = 0; i < xs.Length; i++)
            {
                int cx = xs[i] + ((seed + i) % 2);
                int h = Mathf.Max(1, spikeHeight - (i % 2));
                for (int dy = 0; dy < h; dy++)
                {
                    int y = baseY + 1 + dy;
                    if (y >= size)
                        break;
                    Color c = dy == h - 1 ? tip : metal;
                    tex.SetPixel(cx, y, c);
                    if (dy > 0 && cx > 0)
                        tex.SetPixel(cx - 1, y, metal);
                    if (dy > 0 && cx < size - 1)
                        tex.SetPixel(cx + 1, y, metal);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.2f), 16f);
    }
}
