using UnityEngine;

/// <summary>
/// Vẽ viên kinh nghiệm dạng pha lê (kim cương 4 cạnh) bằng code — phân biệt rõ với xu vàng.
/// Common = xanh lá ngọc, Rare = xanh dương sáng. Có lõi sáng + viền đậm + đốm highlight.
/// </summary>
public static class ExpGemVisual
{
    private static Sprite commonGem;
    private static Sprite rareGem;

    public static Sprite GetGemSprite(bool rare)
    {
        if (rare)
        {
            if (rareGem == null)
                rareGem = BuildGem(
                    edge: new Color(0.05f, 0.22f, 0.55f, 1f),
                    body: new Color(0.18f, 0.55f, 1f, 1f),
                    core: new Color(0.75f, 0.92f, 1f, 1f),
                    "ExpGemRare");
            return rareGem;
        }

        if (commonGem == null)
            commonGem = BuildGem(
                edge: new Color(0.05f, 0.35f, 0.15f, 1f),
                body: new Color(0.25f, 0.85f, 0.4f, 1f),
                core: new Color(0.8f, 1f, 0.85f, 1f),
                "ExpGemCommon");
        return commonGem;
    }

    private static Sprite BuildGem(Color edge, Color body, Color core, string name)
    {
        const int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = name
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        // Kim cương: |dx|/halfW + |dy|/halfH <= 1, hơi cao hơn rộng cho dáng pha lê.
        float halfW = size * 0.34f;
        float halfH = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - cx);
                float dy = Mathf.Abs(y - cy);
                float d = dx / halfW + dy / halfH;

                if (d > 1f)
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }

                Color c;
                if (d > 0.82f)
                    c = edge;                       // viền ngoài đậm
                else if (d < 0.34f)
                    c = core;                        // lõi sáng
                else
                    c = body;                        // thân

                // Đốm highlight chéo trên-trái cho cảm giác phản quang.
                if (x - cx < -1f && y - cy > 1f && d < 0.6f)
                    c = Color.Lerp(c, Color.white, 0.55f);

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 24f);
    }
}
