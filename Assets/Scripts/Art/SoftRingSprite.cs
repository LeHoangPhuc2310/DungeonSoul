using UnityEngine;

/// <summary>Vòng sáng mềm tạo bằng code — nhìn tự nhiên hơn sprite AI cắt từ sheet.</summary>
public static class SoftRingSprite
{
    private static readonly System.Collections.Generic.Dictionary<int, Sprite> Cache =
        new System.Collections.Generic.Dictionary<int, Sprite>();

    public static Sprite Get(int size = 64, float inner = 0.55f)
    {
        int key = size * 1000 + (int)(inner * 100f);
        if (Cache.TryGetValue(key, out Sprite cached) && cached != null)
            return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(dist - inner) * 5f);
                float fade = Mathf.Clamp01(1.2f - dist);
                float a = ring * fade;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }

        tex.Apply();
        Sprite s = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        Cache[key] = s;
        return s;
    }
}
