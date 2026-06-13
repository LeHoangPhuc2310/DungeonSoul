// DungeonSoul — Vẽ VFX animation pixel-art bằng code thuần (không cần file PNG).
// Mỗi style sinh ra Sprite[] có thể dùng trực tiếp với HeroKnightVfxRunner.

using UnityEngine;

public static class ProceduralVfxPainter
{
    private const int Size = 64;
    private const float Half = Size / 2f;
    private const float PPU = 64f;

    // ─── Public API ───────────────────────────────────────────────────────────

    public static Sprite[] BuildFire(int frames = 8)
    {
        var result = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            float t = i / (float)(frames - 1);
            result[i] = MakeSprite(DrawFire(t), $"fire_{i:D2}");
        }
        return result;
    }

    public static Sprite[] BuildIce(int frames = 8)
    {
        var result = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            float t = i / (float)(frames - 1);
            result[i] = MakeSprite(DrawIce(t), $"ice_{i:D2}");
        }
        return result;
    }

    public static Sprite[] BuildLightning(int frames = 8)
    {
        var result = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            float t = i / (float)(frames - 1);
            result[i] = MakeSprite(DrawLightning(t), $"lightning_{i:D2}");
        }
        return result;
    }

    public static Sprite[] BuildPoison(int frames = 8)
    {
        var result = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            float t = i / (float)(frames - 1);
            result[i] = MakeSprite(DrawPoison(t), $"poison_{i:D2}");
        }
        return result;
    }

    public static Sprite[] BuildArcane(int frames = 8)
    {
        var result = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            float t = i / (float)(frames - 1);
            result[i] = MakeSprite(DrawArcane(t), $"arcane_{i:D2}");
        }
        return result;
    }

    public static Sprite[] BuildSlash(int frames = 8)
    {
        var result = new Sprite[frames];
        for (int i = 0; i < frames; i++)
        {
            float t = i / (float)(frames - 1);
            result[i] = MakeSprite(DrawSlash(t), $"slash_{i:D2}");
        }
        return result;
    }

    // ─── Fire ─────────────────────────────────────────────────────────────────
    // Quả cầu lửa nở ra rồi tan thành tàn lửa.

    private static Color[] DrawFire(float t)
    {
        Color[] px = Clear();
        // Đường cong radius: lớn nhất ở t=0.45
        float radius = t < 0.5f
            ? Mathf.Lerp(4f, 24f, t * 2f)
            : Mathf.Lerp(24f, 10f, (t - 0.5f) * 2f);
        float alpha = t < 0.85f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.85f) / 0.15f);

        // Lõi sáng
        float core = radius * 0.45f;
        FillCircle(px, Half, Half, core, new Color(1f, 0.98f, 0.7f, alpha));
        // Vòng giữa cam
        FillCircleRing(px, Half, Half, core, radius * 0.75f, new Color(1f, 0.55f, 0.1f, alpha * 0.92f));
        // Vòng ngoài đỏ
        FillCircleRing(px, Half, Half, radius * 0.75f, radius, new Color(0.9f, 0.15f, 0.05f, alpha * 0.75f));

        // Tia lửa 8 hướng
        if (t > 0.15f && t < 0.8f)
        {
            float rayLen = radius * 1.4f;
            float rayAlpha = alpha * (1f - Mathf.Abs(t - 0.45f) * 2.5f);
            rayAlpha = Mathf.Max(0f, rayAlpha);
            for (int r = 0; r < 8; r++)
            {
                float angle = r * Mathf.PI * 0.25f + t * 1.2f;
                DrawRay(px, Half, Half, angle, radius * 0.9f, rayLen, 2, new Color(1f, 0.7f, 0.2f, rayAlpha));
            }
        }

        // Tàn lửa bay ra khi t > 0.5
        if (t > 0.5f)
        {
            int seed = 42;
            for (int e = 0; e < 12; e++)
            {
                seed = Lcg(seed);
                float angle = (seed % 360) * Mathf.Deg2Rad;
                seed = Lcg(seed);
                float dist = radius * 1.1f + (seed % 100) / 100f * radius * 0.6f;
                float ex = Half + Mathf.Cos(angle) * dist;
                float ey = Half + Mathf.Sin(angle) * dist;
                float ea = alpha * (1f - (t - 0.5f) * 1.8f);
                if (ea > 0f)
                    SetPixelSafe(px, Mathf.RoundToInt(ex), Mathf.RoundToInt(ey), new Color(1f, 0.5f, 0.1f, Mathf.Clamp01(ea)));
            }
        }

        PixelOutline(px, new Color(0.5f, 0.05f, 0f, alpha * 0.4f));
        return px;
    }

    // ─── Ice ──────────────────────────────────────────────────────────────────
    // Tinh thể băng nở, hình ngôi sao 6 cánh.

    private static Color[] DrawIce(float t)
    {
        Color[] px = Clear();
        float radius = t < 0.4f
            ? Mathf.Lerp(3f, 22f, t / 0.4f)
            : Mathf.Lerp(22f, 12f, (t - 0.4f) / 0.6f);
        float alpha = t < 0.8f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f);

        // Lõi trắng
        FillCircle(px, Half, Half, radius * 0.3f, new Color(1f, 1f, 1f, alpha));
        // Vòng xanh băng
        FillCircleRing(px, Half, Half, radius * 0.3f, radius * 0.65f, new Color(0.6f, 0.9f, 1f, alpha * 0.9f));
        FillCircleRing(px, Half, Half, radius * 0.65f, radius, new Color(0.35f, 0.7f, 1f, alpha * 0.7f));

        // 6 cánh tia băng
        for (int r = 0; r < 6; r++)
        {
            float angle = r * Mathf.PI / 3f;
            DrawRay(px, Half, Half, angle, radius * 0.6f, radius * 1.55f, 3, new Color(0.8f, 0.95f, 1f, alpha * 0.85f));
            // Mấu nhỏ trên cánh
            DrawRay(px, Half, Half, angle, radius * 0.9f, radius * 1.15f, 2,
                new Color(1f, 1f, 1f, alpha * 0.7f), Mathf.PI * 0.18f);
            DrawRay(px, Half, Half, angle, radius * 0.9f, radius * 1.15f, 2,
                new Color(1f, 1f, 1f, alpha * 0.7f), -Mathf.PI * 0.18f);
        }

        // Mảnh vụn bay ra
        if (t > 0.4f)
        {
            int seed = 17;
            for (int s = 0; s < 8; s++)
            {
                seed = Lcg(seed);
                float angle = (seed % 360) * Mathf.Deg2Rad;
                seed = Lcg(seed);
                float dist = radius * 1.2f + (seed % 100) / 100f * 10f;
                float sx = Half + Mathf.Cos(angle) * dist;
                float sy = Half + Mathf.Sin(angle) * dist;
                float sa = alpha * 0.9f * (1f - (t - 0.4f) * 1.5f);
                if (sa > 0f)
                    SetPixelSafe(px, Mathf.RoundToInt(sx), Mathf.RoundToInt(sy), new Color(0.85f, 0.97f, 1f, Mathf.Clamp01(sa)));
            }
        }

        PixelOutline(px, new Color(0.1f, 0.3f, 0.6f, alpha * 0.3f));
        return px;
    }

    // ─── Lightning ────────────────────────────────────────────────────────────
    // Tia sét zíc-zắc bùng sáng rồi tắt.

    private static Color[] DrawLightning(float t)
    {
        Color[] px = Clear();
        float alpha = t < 0.3f ? Mathf.Lerp(0f, 1f, t / 0.3f)
                    : t < 0.55f ? 1f
                    : Mathf.Lerp(1f, 0f, (t - 0.55f) / 0.45f);

        // Vầng sáng trung tâm
        float glow = Mathf.Lerp(3f, 14f, Mathf.Sin(t * Mathf.PI));
        FillCircle(px, Half, Half, glow, new Color(1f, 1f, 0.8f, alpha * 0.6f));

        // 4 tia sét zíc-zắc
        int seed = 7 + Mathf.RoundToInt(t * 100f);
        for (int bolt = 0; bolt < 4; bolt++)
        {
            float baseAngle = bolt * Mathf.PI * 0.5f + t * 0.5f;
            float len = Mathf.Lerp(8f, 26f, Mathf.Sin(t * Mathf.PI));
            DrawZigzagBolt(px, Half, Half, baseAngle, len, seed + bolt * 13,
                new Color(0.95f, 0.95f, 1f, alpha),
                new Color(0.5f, 0.6f, 1f, alpha * 0.7f));
        }

        // Tia nhánh nhỏ
        if (t > 0.2f && t < 0.7f)
        {
            for (int b = 0; b < 6; b++)
            {
                float ang = b * Mathf.PI / 3f + t * 2f;
                float blen = 8f + (seed % 7) * 1.5f;
                DrawZigzagBolt(px, Half, Half, ang, blen, seed + b * 5,
                    new Color(0.7f, 0.8f, 1f, alpha * 0.5f),
                    new Color(0.4f, 0.5f, 0.9f, alpha * 0.35f));
            }
        }

        return px;
    }

    private static void DrawZigzagBolt(Color[] px, float ox, float oy, float angle, float length,
        int seed, Color coreColor, Color edgeColor)
    {
        int steps = Mathf.Max(4, Mathf.RoundToInt(length / 3f));
        float dx = Mathf.Cos(angle);
        float dy = Mathf.Sin(angle);
        float px2 = ox, py2 = oy;

        for (int s = 0; s < steps; s++)
        {
            seed = Lcg(seed);
            float jitter = ((seed % 100) / 100f - 0.5f) * 4.5f;
            float nx = px2 + dx * (length / steps) + (-dy) * jitter;
            float ny = py2 + dy * (length / steps) + dx * jitter;

            DrawThickLine(px, Mathf.RoundToInt(px2), Mathf.RoundToInt(py2),
                Mathf.RoundToInt(nx), Mathf.RoundToInt(ny), coreColor, edgeColor, 2);
            px2 = nx; py2 = ny;
        }
    }

    // ─── Poison ───────────────────────────────────────────────────────────────
    // Đám mây độc bong bóng nở rồi tan.

    private static Color[] DrawPoison(float t)
    {
        Color[] px = Clear();
        float radius = t < 0.45f
            ? Mathf.Lerp(5f, 20f, t / 0.45f)
            : Mathf.Lerp(20f, 26f, (t - 0.45f) / 0.55f);
        float alpha = t < 0.75f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.75f) / 0.25f);

        // Mây độc lốm đốm
        Color darkGreen = new Color(0.1f, 0.45f, 0.1f, alpha * 0.85f);
        Color brightGreen = new Color(0.35f, 0.9f, 0.2f, alpha * 0.9f);
        Color yellow = new Color(0.8f, 0.95f, 0.1f, alpha * 0.7f);

        FillCircle(px, Half, Half, radius * 0.5f, brightGreen);
        FillCircleRing(px, Half, Half, radius * 0.5f, radius * 0.8f, darkGreen);
        // Cụm mây nhỏ lệch tâm
        int seed = 31;
        for (int c = 0; c < 6; c++)
        {
            seed = Lcg(seed);
            float a2 = (seed % 360) * Mathf.Deg2Rad;
            seed = Lcg(seed);
            float d = radius * 0.3f + (seed % 100) / 100f * radius * 0.55f;
            float cx = Half + Mathf.Cos(a2) * d;
            float cy = Half + Mathf.Sin(a2) * d;
            seed = Lcg(seed);
            float cr = 3f + (seed % 100) / 100f * 6f;
            FillCircle(px, cx, cy, cr, c % 2 == 0 ? brightGreen : darkGreen);
        }

        // Bong bóng nhỏ xíu
        seed = 53;
        for (int b = 0; b < 10; b++)
        {
            seed = Lcg(seed);
            float ba = (seed % 360) * Mathf.Deg2Rad;
            seed = Lcg(seed);
            float bd = radius * (0.85f + (seed % 100) / 100f * 0.6f);
            float bx = Half + Mathf.Cos(ba) * bd;
            float by = Half + Mathf.Sin(ba) * bd;
            float bAlpha = alpha * (0.4f + (seed % 100) / 100f * 0.5f);
            DrawCircleOutline(px, bx, by, 2.5f + (b % 3), new Color(0.6f, 1f, 0.3f, bAlpha));
        }

        // Giọt nhỏ rơi
        if (t > 0.3f)
        {
            seed = 77;
            for (int d = 0; d < 6; d++)
            {
                seed = Lcg(seed);
                float da = (seed % 360) * Mathf.Deg2Rad;
                seed = Lcg(seed);
                float dd = radius + (seed % 100) / 100f * 8f + (t - 0.3f) * 15f;
                float drx = Half + Mathf.Cos(da) * dd;
                float dry = Half + Mathf.Sin(da) * dd - (t - 0.3f) * 12f;
                float da2 = alpha * (1f - (t - 0.3f) * 1.4f);
                if (da2 > 0f)
                    SetPixelSafe(px, Mathf.RoundToInt(drx), Mathf.RoundToInt(dry), new Color(0.4f, 0.95f, 0.15f, Mathf.Clamp01(da2)));
            }
        }

        PixelOutline(px, new Color(0.05f, 0.25f, 0.05f, alpha * 0.35f));
        return px;
    }

    // ─── Arcane ───────────────────────────────────────────────────────────────
    // Vòng rune phép thuật nở, xoay, biến mất.

    private static Color[] DrawArcane(float t)
    {
        Color[] px = Clear();
        float radius = t < 0.35f
            ? Mathf.Lerp(2f, 22f, t / 0.35f)
            : t < 0.7f ? 22f
            : Mathf.Lerp(22f, 26f, (t - 0.7f) / 0.3f);
        float alpha = t < 0.8f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f);
        float rotation = t * Mathf.PI * 1.5f;

        // Orb trung tâm
        FillCircle(px, Half, Half, radius * 0.28f, new Color(0.9f, 0.7f, 1f, alpha));
        FillCircle(px, Half, Half, radius * 0.14f, new Color(1f, 0.95f, 1f, alpha));

        // Vòng rune
        DrawCircleOutline(px, Half, Half, radius * 0.72f, new Color(0.8f, 0.5f, 1f, alpha * 0.9f), 3);
        DrawCircleOutline(px, Half, Half, radius, new Color(0.6f, 0.35f, 0.9f, alpha * 0.7f), 2);

        // 6 ký tự rune trên vòng
        for (int r = 0; r < 6; r++)
        {
            float a = r * Mathf.PI / 3f + rotation;
            float rx = Half + Mathf.Cos(a) * radius * 0.72f;
            float ry = Half + Mathf.Sin(a) * radius * 0.72f;
            DrawRuneGlyph(px, rx, ry, r, new Color(0.95f, 0.85f, 1f, alpha * 0.9f));
        }

        // Tia sáng 4 hướng từ tâm
        for (int r = 0; r < 4; r++)
        {
            float a = r * Mathf.PI * 0.5f + rotation * 0.7f;
            DrawRay(px, Half, Half, a, radius * 0.28f, radius * 1.1f, 2, new Color(0.85f, 0.6f, 1f, alpha * 0.8f));
        }

        // Hạt lấp lánh bay ra
        int seed = 61;
        for (int s = 0; s < 12; s++)
        {
            seed = Lcg(seed);
            float sa = (seed % 360) * Mathf.Deg2Rad;
            seed = Lcg(seed);
            float sd = radius * 1.0f + (seed % 100) / 100f * radius * 0.7f;
            float sx = Half + Mathf.Cos(sa + rotation * 0.5f) * sd;
            float sy = Half + Mathf.Sin(sa + rotation * 0.5f) * sd;
            float starAlpha = alpha * (0.5f + (seed % 100) / 100f * 0.4f);
            Color starColor = s % 3 == 0 ? new Color(1f, 0.85f, 1f, starAlpha)
                            : s % 3 == 1 ? new Color(0.7f, 0.5f, 1f, starAlpha)
                            : new Color(1f, 0.6f, 0.9f, starAlpha);
            SetPixelSafe(px, Mathf.RoundToInt(sx), Mathf.RoundToInt(sy), starColor);
        }

        return px;
    }

    // ─── Slash ────────────────────────────────────────────────────────────────
    // Lưỡi kiếm cong quét qua, để lại vệt sáng.

    private static Color[] DrawSlash(float t)
    {
        Color[] px = Clear();
        float alpha = t < 0.1f ? t / 0.1f
                    : t < 0.5f ? 1f
                    : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);

        // Góc quét lưỡi kiếm: từ -120° → +60° trong t=0→0.5
        float sweepT = Mathf.Clamp01(t / 0.5f);
        float startAngle = -120f * Mathf.Deg2Rad;
        float endAngle = 60f * Mathf.Deg2Rad;
        float currentAngle = Mathf.Lerp(startAngle, endAngle, Mathf.SmoothStep(0f, 1f, sweepT));

        float innerR = 8f;
        float outerR = 26f;

        // Vẽ vùng quét (arc)
        int arcSteps = 32;
        float arcStart = startAngle;
        float arcEnd = currentAngle;
        if (arcEnd > arcStart)
        {
            for (int s = 0; s < arcSteps; s++)
            {
                float fa = Mathf.Lerp(arcStart, arcEnd, s / (float)(arcSteps - 1));
                float fb = Mathf.Lerp(arcStart, arcEnd, (s + 1) / (float)(arcSteps - 1));
                float ageRatio = 1f - (fa - arcStart) / (arcEnd - arcStart + 0.001f);
                float segAlpha = alpha * Mathf.Lerp(0.2f, 1f, ageRatio);

                Color slashColor = new Color(
                    Mathf.Lerp(0.6f, 1f, ageRatio),
                    Mathf.Lerp(0.6f, 0.95f, ageRatio),
                    Mathf.Lerp(0.5f, 0.85f, ageRatio),
                    segAlpha);

                // Điền dải arc
                for (float r = innerR; r <= outerR; r += 1.2f)
                {
                    float edgeFade = 1f - Mathf.Abs((r - innerR) / (outerR - innerR) * 2f - 1f);
                    Color c = new Color(slashColor.r, slashColor.g, slashColor.b, slashColor.a * edgeFade);
                    float ax = Half + Mathf.Cos(fa) * r;
                    float ay = Half + Mathf.Sin(fa) * r;
                    SetPixelSafe(px, Mathf.RoundToInt(ax), Mathf.RoundToInt(ay), c);
                }
            }
        }

        // Cạnh lưỡi kiếm sáng nhất
        for (float r = innerR; r <= outerR; r += 0.8f)
        {
            float ex = Half + Mathf.Cos(currentAngle) * r;
            float ey = Half + Mathf.Sin(currentAngle) * r;
            SetPixelSafe(px, Mathf.RoundToInt(ex), Mathf.RoundToInt(ey), new Color(1f, 1f, 1f, alpha));
        }

        // Tia sáng nhỏ tại đầu lưỡi
        if (sweepT > 0.05f)
        {
            float tipX = Half + Mathf.Cos(currentAngle) * outerR;
            float tipY = Half + Mathf.Sin(currentAngle) * outerR;
            FillCircle(px, tipX, tipY, 2.5f, new Color(1f, 0.98f, 0.85f, alpha * 0.9f));
        }

        return px;
    }

    // ─── Drawing Primitives ───────────────────────────────────────────────────

    private static Color[] Clear() => new Color[Size * Size]; // RGBA32 init = transparent

    private static void SetPixelSafe(Color[] px, int x, int y, Color c)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size) return;
        Color existing = px[y * Size + x];
        // Alpha compositing: over operator
        float a = c.a + existing.a * (1f - c.a);
        if (a < 0.001f) { px[y * Size + x] = Color.clear; return; }
        px[y * Size + x] = new Color(
            (c.r * c.a + existing.r * existing.a * (1f - c.a)) / a,
            (c.g * c.a + existing.g * existing.a * (1f - c.a)) / a,
            (c.b * c.a + existing.b * existing.a * (1f - c.a)) / a,
            a);
    }

    private static void FillCircle(Color[] px, float cx, float cy, float radius, Color c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
        int x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + radius));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
        int y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + radius));
        float r2 = radius * radius;

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d2 = dx * dx + dy * dy;
                if (d2 > r2) continue;
                // Soft edge 1.5px
                float edge = Mathf.Clamp01((r2 - d2) / (radius * 3f + 0.001f));
                SetPixelSafe(px, x, y, new Color(c.r, c.g, c.b, c.a * edge));
            }
    }

    private static void FillCircleRing(Color[] px, float cx, float cy, float innerR, float outerR, Color c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - outerR));
        int x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + outerR));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - outerR));
        int y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + outerR));
        float r2In = innerR * innerR, r2Out = outerR * outerR;

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d2 = dx * dx + dy * dy;
                if (d2 < r2In || d2 > r2Out) continue;
                float edgeOut = Mathf.Clamp01((r2Out - d2) / (outerR * 3f + 0.001f));
                float edgeIn = Mathf.Clamp01((d2 - r2In) / (innerR * 3f + 0.001f));
                SetPixelSafe(px, x, y, new Color(c.r, c.g, c.b, c.a * edgeOut * edgeIn));
            }
    }

    private static void DrawCircleOutline(Color[] px, float cx, float cy, float radius, Color c, int thickness = 2)
    {
        float r2Out = (radius + thickness * 0.5f) * (radius + thickness * 0.5f);
        float r2In = (radius - thickness * 0.5f) * (radius - thickness * 0.5f);
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radius - thickness));
        int x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + radius + thickness));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radius - thickness));
        int y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + radius + thickness));

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d2 = dx * dx + dy * dy;
                if (d2 < r2In || d2 > r2Out) continue;
                SetPixelSafe(px, x, y, c);
            }
    }

    private static void DrawRay(Color[] px, float ox, float oy, float angle, float startDist, float endDist,
        int thickness, Color c, float angleOffset = 0f)
    {
        float a = angle + angleOffset;
        float cosA = Mathf.Cos(a), sinA = Mathf.Sin(a);
        float steps = (endDist - startDist) / 0.8f;
        for (int s = 0; s <= Mathf.RoundToInt(steps); s++)
        {
            float d = Mathf.Lerp(startDist, endDist, s / steps);
            float wx = ox + cosA * d;
            float wy = oy + sinA * d;
            float fade = Mathf.Clamp01(1f - (d - startDist) / (endDist - startDist + 0.001f) * 0.7f);
            Color fc = new Color(c.r, c.g, c.b, c.a * fade);
            SetPixelSafe(px, Mathf.RoundToInt(wx), Mathf.RoundToInt(wy), fc);
            if (thickness > 1)
            {
                SetPixelSafe(px, Mathf.RoundToInt(wx + 1), Mathf.RoundToInt(wy), fc);
                SetPixelSafe(px, Mathf.RoundToInt(wx), Mathf.RoundToInt(wy + 1), fc);
            }
        }
    }

    private static void DrawThickLine(Color[] px, int x0, int y0, int x1, int y1,
        Color coreC, Color edgeC, int halfW)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            SetPixelSafe(px, x0, y0, coreC);
            for (int w = 1; w <= halfW; w++)
            {
                SetPixelSafe(px, x0 + w, y0, edgeC);
                SetPixelSafe(px, x0 - w, y0, edgeC);
                SetPixelSafe(px, x0, y0 + w, edgeC);
                SetPixelSafe(px, x0, y0 - w, edgeC);
            }
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void DrawRuneGlyph(Color[] px, float cx, float cy, int glyphIndex, Color c)
    {
        // 6 kiểu glyph rune nhỏ 5x5 pixel
        int[,] glyphs = new int[6, 5]
        {
            { 0b01110, 0b10001, 0b11111, 0b10001, 0b01110 }, // rune 0: diamond
            { 0b11111, 0b10001, 0b11111, 0b10001, 0b11111 }, // rune 1: grid
            { 0b01110, 0b11011, 0b10101, 0b11011, 0b01110 }, // rune 2: cross
            { 0b11100, 0b10010, 0b11110, 0b10010, 0b11100 }, // rune 3: E-shape
            { 0b11111, 0b00100, 0b11111, 0b00100, 0b11111 }, // rune 4: H-shape
            { 0b10101, 0b01110, 0b11111, 0b01110, 0b10101 }, // rune 5: star
        };

        int g = glyphIndex % 6;
        for (int row = 0; row < 5; row++)
            for (int col = 0; col < 5; col++)
                if ((glyphs[g, row] & (1 << (4 - col))) != 0)
                    SetPixelSafe(px, Mathf.RoundToInt(cx) - 2 + col, Mathf.RoundToInt(cy) - 2 + row, c);
    }

    private static void PixelOutline(Color[] px, Color outlineColor)
    {
        Color[] copy = (Color[])px.Clone();
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                if (copy[y * Size + x].a > 0.05f) continue;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (nx < 0 || nx >= Size || ny < 0 || ny >= Size) continue;
                    if (copy[ny * Size + nx].a > 0.3f)
                    {
                        SetPixelSafe(px, x, y, outlineColor);
                        break;
                    }
                }
            }
    }

    // LCG random (deterministic, no System.Random allocation)
    private static int Lcg(int seed) => seed * 1664525 + 1013904223;

    // ─── Sprite Factory ───────────────────────────────────────────────────────

    private static Sprite MakeSprite(Color[] pixels, string name)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = name;
        Sprite s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), PPU);
        s.name = name;
        return s;
    }
}
