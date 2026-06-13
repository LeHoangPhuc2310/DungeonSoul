// DungeonSoul — Vẽ procedural VFX vũ khí (đạn / muzzle / hit) bằng Texture2D.SetPixels().
// Không cần file PNG. Mỗi WeaponType có animation riêng biệt.

using UnityEngine;

public static class ProceduralWeaponVfxPainter
{
    private const int S = 32;   // kích thước frame (32×32 px)
    private const float PPU = 32f;

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API — Projectile frames (8 frames mỗi loại)
    // ═══════════════════════════════════════════════════════════════

    public static Sprite[] BuildArrow(int n = 8)          => Build(n, DrawArrow);
    public static Sprite[] BuildArrowStorm(int n = 8)     => Build(n, DrawArrowStorm);
    public static Sprite[] BuildFireball(int n = 8)       => Build(n, DrawFireball);
    public static Sprite[] BuildDragonOrb(int n = 8)      => Build(n, DrawDragonOrb);
    public static Sprite[] BuildIceShard(int n = 8)       => Build(n, DrawIceShard);
    public static Sprite[] BuildBlizzardOrb(int n = 8)    => Build(n, DrawBlizzardOrb);
    public static Sprite[] BuildPoisonBlade(int n = 8)    => Build(n, DrawPoisonBlade);
    public static Sprite[] BuildDeathBlade(int n = 8)     => Build(n, DrawDeathBlade);
    public static Sprite[] BuildHolyCross(int n = 8)      => Build(n, DrawHolyCross);
    public static Sprite[] BuildHolyNova(int n = 8)       => Build(n, DrawHolyNova);
    public static Sprite[] BuildThunderBolt(int n = 8)    => Build(n, DrawThunderBolt);
    public static Sprite[] BuildZeusBolt(int n = 8)       => Build(n, DrawZeusBolt);

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API — Muzzle flash (6 element, 6 frames mỗi loại)
    // ═══════════════════════════════════════════════════════════════

    public static Sprite[] BuildMuzzleArrow(int n = 6)    => Build(n, DrawMuzzleArrow);
    public static Sprite[] BuildMuzzleFire(int n = 6)     => Build(n, DrawMuzzleFire);
    public static Sprite[] BuildMuzzleIce(int n = 6)      => Build(n, DrawMuzzleIce);
    public static Sprite[] BuildMuzzlePoison(int n = 6)   => Build(n, DrawMuzzlePoison);
    public static Sprite[] BuildMuzzleHoly(int n = 6)     => Build(n, DrawMuzzleHoly);
    public static Sprite[] BuildMuzzleLightning(int n = 6)=> Build(n, DrawMuzzleLightning);

    // ═══════════════════════════════════════════════════════════════
    //  PUBLIC API — Hit burst (6 element, 8 frames mỗi loại)
    // ═══════════════════════════════════════════════════════════════

    public static Sprite[] BuildHitArrow(int n = 8)       => Build(n, DrawHitArrow);
    public static Sprite[] BuildHitFire(int n = 8)        => Build(n, DrawHitFire);
    public static Sprite[] BuildHitIce(int n = 8)         => Build(n, DrawHitIce);
    public static Sprite[] BuildHitPoison(int n = 8)      => Build(n, DrawHitPoison);
    public static Sprite[] BuildHitHoly(int n = 8)        => Build(n, DrawHitHoly);
    public static Sprite[] BuildHitLightning(int n = 8)   => Build(n, DrawHitLightning);

    // ═══════════════════════════════════════════════════════════════
    //  PROJECTILE DRAW METHODS
    // ═══════════════════════════════════════════════════════════════

    // Mũi tên gỗ — thân nâu + đầu nhọn trắng
    private static void DrawArrow(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float wobble = Mathf.Sin(t * Mathf.PI * 4f) * 0.6f;

        // Thân mũi tên (ngang)
        for (int dx = -10; dx <= 4; dx++)
        {
            int x = cx + dx, y = cy + Mathf.RoundToInt(wobble);
            BlendPixel(px, x, y, new Color(0.55f, 0.35f, 0.15f, 0.95f));
            BlendPixel(px, x, y + 1, new Color(0.45f, 0.28f, 0.10f, 0.6f));
        }
        // Đầu nhọn
        for (int dy = -2; dy <= 2; dy++)
        {
            int len = 3 - Mathf.Abs(dy);
            for (int dx = 0; dx < len + 3; dx++)
            {
                float a = 1f - (float)dx / (len + 3);
                BlendPixel(px, cx + 4 + dx, cy + dy + Mathf.RoundToInt(wobble),
                    new Color(0.92f, 0.88f, 0.75f, a * 0.95f));
            }
        }
        // Lông đuôi
        BlendPixel(px, cx - 10, cy - 1 + Mathf.RoundToInt(wobble), new Color(0.7f, 0.2f, 0.2f, 0.85f));
        BlendPixel(px, cx - 10, cy + 1 + Mathf.RoundToInt(wobble), new Color(0.7f, 0.2f, 0.2f, 0.85f));
        BlendPixel(px, cx - 11, cy - 2 + Mathf.RoundToInt(wobble), new Color(0.7f, 0.2f, 0.2f, 0.7f));
        BlendPixel(px, cx - 11, cy + 2 + Mathf.RoundToInt(wobble), new Color(0.7f, 0.2f, 0.2f, 0.7f));
    }

    // Mũi tên bão — tương tự + trails sét
    private static void DrawArrowStorm(Color[] px, float t)
    {
        DrawArrow(px, t);
        int cx = S / 2, cy = S / 2;
        int seed = Lcg((int)(t * 997));
        for (int i = 0; i < 5; i++)
        {
            seed = Lcg(seed);
            int dx = (seed % 8) - 12;
            seed = Lcg(seed);
            int dy = (seed % 5) - 2;
            float a = 0.5f + 0.4f * (float)(Lcg(seed) & 0xFF) / 255f;
            BlendPixel(px, cx + dx, cy + dy, new Color(0.7f, 0.75f, 1f, a));
        }
        // Glow xanh quanh đầu
        FillCircleAlpha(px, cx + 6, cy, 3, new Color(0.6f, 0.7f, 1f, 0.35f - t * 0.25f));
    }

    // Quả cầu lửa
    private static void DrawFireball(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float pulse = 0.82f + 0.18f * Mathf.Sin(t * Mathf.PI * 6f);
        int r = Mathf.RoundToInt(7 * pulse);

        FillRadialGradient(px, cx, cy, r,
            new Color(1f, 0.98f, 0.7f, 0.98f),
            new Color(1f, 0.45f, 0.05f, 0.85f),
            new Color(0.8f, 0.1f, 0f, 0f));

        // 6 tia lửa nhỏ quay
        float baseAngle = t * Mathf.PI * 4f;
        for (int i = 0; i < 6; i++)
        {
            float a = baseAngle + i * Mathf.PI / 3f;
            int len = 4 + Mathf.RoundToInt(2f * Mathf.Sin(t * Mathf.PI * 3f + i));
            DrawRayFade(px, cx, cy, a, r + 1, r + len,
                new Color(1f, 0.6f, 0.1f, 0.7f - t * 0.4f));
        }
    }

    // Quả cầu rồng — to hơn, màu đỏ đậm + xoáy
    private static void DrawDragonOrb(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float pulse = 0.8f + 0.2f * Mathf.Sin(t * Mathf.PI * 5f);
        int r = Mathf.RoundToInt(9 * pulse);

        FillRadialGradient(px, cx, cy, r,
            new Color(1f, 0.9f, 0.5f, 1f),
            new Color(0.95f, 0.25f, 0f, 0.9f),
            new Color(0.5f, 0f, 0f, 0f));

        // Xoáy rồng
        float sw = t * Mathf.PI * 6f;
        for (int i = 0; i < 3; i++)
        {
            float a = sw + i * Mathf.PI * 2f / 3f;
            for (int step = 0; step < 8; step++)
            {
                float sr2 = (float)step / 8f * r;
                float sa = a + step * 0.3f;
                int px2 = cx + Mathf.RoundToInt(Mathf.Cos(sa) * sr2);
                int py2 = cy + Mathf.RoundToInt(Mathf.Sin(sa) * sr2);
                BlendPixel(px, px2, py2, new Color(1f, 0.8f, 0.2f, 0.6f * (1f - (float)step / 8f)));
            }
        }
    }

    // Mảnh băng — hình kim cương xoay
    private static void DrawIceShard(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float angle = t * Mathf.PI * 3f;
        int r = 7;

        // 6 spike tinh thể
        for (int i = 0; i < 6; i++)
        {
            float a = angle + i * Mathf.PI / 3f;
            float lenF = (i % 2 == 0) ? r : r * 0.6f;
            for (int step = 0; step <= Mathf.RoundToInt(lenF); step++)
            {
                float alpha = 0.9f - (float)step / lenF * 0.6f;
                Color c = Color.Lerp(new Color(0.95f, 1f, 1f, alpha),
                    new Color(0.4f, 0.75f, 1f, alpha * 0.7f), (float)step / lenF);
                int bx = cx + Mathf.RoundToInt(Mathf.Cos(a) * step);
                int by = cy + Mathf.RoundToInt(Mathf.Sin(a) * step);
                BlendPixel(px, bx, by, c);
                BlendPixel(px, bx, by + 1, new Color(c.r, c.g, c.b, c.a * 0.4f));
            }
        }
        // Lõi sáng
        FillCircleAlpha(px, cx, cy, 3, new Color(0.9f, 0.97f, 1f, 0.9f));
    }

    // Orb blizzard — to hơn + tuyết xoay
    private static void DrawBlizzardOrb(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float pulse = 0.85f + 0.15f * Mathf.Sin(t * Mathf.PI * 4f);
        int r = Mathf.RoundToInt(9 * pulse);

        FillRadialGradient(px, cx, cy, r,
            new Color(0.95f, 1f, 1f, 1f),
            new Color(0.45f, 0.75f, 1f, 0.9f),
            new Color(0.15f, 0.4f, 0.9f, 0f));

        // Vòng tuyết xoay
        float sw = t * Mathf.PI * 5f;
        for (int i = 0; i < 8; i++)
        {
            float a = sw + i * Mathf.PI / 4f;
            int nx = cx + Mathf.RoundToInt(Mathf.Cos(a) * (r + 2));
            int ny = cy + Mathf.RoundToInt(Mathf.Sin(a) * (r + 2));
            BlendPixel(px, nx, ny, new Color(0.8f, 0.92f, 1f, 0.8f));
            BlendPixel(px, nx + 1, ny, new Color(0.8f, 0.92f, 1f, 0.4f));
        }
    }

    // Dao độc — hình lưỡi dao + trail xanh
    private static void DrawPoisonBlade(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float wobble = Mathf.Sin(t * Mathf.PI * 5f) * 0.8f;

        // Lưỡi dao
        for (int dx = -8; dx <= 6; dx++)
        {
            float frac = (dx + 8f) / 14f;
            int thick = Mathf.Max(1, Mathf.RoundToInt(2f * (1f - frac)));
            for (int dy = -thick; dy <= thick; dy++)
            {
                Color c = Color.Lerp(
                    new Color(0.7f, 1f, 0.4f, 0.9f),
                    new Color(0.3f, 0.85f, 0.2f, 0.7f), frac);
                BlendPixel(px, cx + dx, cy + dy + Mathf.RoundToInt(wobble), c);
            }
        }
        // Cán dao
        for (int dy = -1; dy <= 1; dy++)
            BlendPixel(px, cx - 9, cy + dy + Mathf.RoundToInt(wobble),
                new Color(0.5f, 0.35f, 0.1f, 0.9f));

        // Trail độc
        int seed = Lcg((int)(t * 1337));
        for (int i = 0; i < 4; i++)
        {
            seed = Lcg(seed);
            int tx = cx - 8 - (seed % 6);
            seed = Lcg(seed);
            int ty = cy + (seed % 5) - 2 + Mathf.RoundToInt(wobble);
            BlendPixel(px, tx, ty, new Color(0.4f, 1f, 0.2f, 0.5f - t * 0.3f));
        }
    }

    // Dao tử thần — đen + aura tím
    private static void DrawDeathBlade(Color[] px, float t)
    {
        DrawPoisonBlade(px, t);
        int cx = S / 2, cy = S / 2;
        // Override màu lưỡi dao thành tối hơn
        for (int dx = -8; dx <= 6; dx++)
        {
            float frac = (dx + 8f) / 14f;
            BlendPixel(px, cx + dx, cy,
                new Color(0.15f, 0.05f, 0.25f, 0.4f)); // darken
        }
        // Aura tử thần
        FillCircleAlpha(px, cx, cy, 9, new Color(0.5f, 0.1f, 0.8f, 0.15f + 0.1f * Mathf.Sin(t * Mathf.PI * 4f)));
        // Particle tím
        int seed = Lcg((int)(t * 777) + 1);
        for (int i = 0; i < 5; i++)
        {
            seed = Lcg(seed);
            int rx = cx + (seed % 12) - 6;
            seed = Lcg(seed);
            int ry = cy + (seed % 12) - 6;
            BlendPixel(px, rx, ry, new Color(0.7f, 0.2f, 1f, 0.5f - t * 0.3f));
        }
    }

    // Thánh giá — hình cross phát sáng vàng
    private static void DrawHolyCross(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float spin = t * Mathf.PI * 2f;
        float glow = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 6f);

        // Vẽ cross theo góc quay
        for (int arm = 0; arm < 4; arm++)
        {
            float a = spin + arm * Mathf.PI / 2f;
            for (int step = 0; step <= 8; step++)
            {
                float alpha = glow * (1f - (float)step / 9f);
                Color c = new Color(1f, 0.95f, 0.6f, alpha);
                int bx = cx + Mathf.RoundToInt(Mathf.Cos(a) * step);
                int by = cy + Mathf.RoundToInt(Mathf.Sin(a) * step);
                BlendPixel(px, bx, by, c);
                if (step < 5)
                {
                    BlendPixel(px, bx + 1, by, new Color(c.r, c.g, c.b, c.a * 0.5f));
                    BlendPixel(px, bx, by + 1, new Color(c.r, c.g, c.b, c.a * 0.5f));
                }
            }
        }
        // Lõi sáng
        FillCircleAlpha(px, cx, cy, 3, new Color(1f, 1f, 0.8f, glow * 0.9f));
    }

    // Holy Nova — vòng ánh sáng mở rộng
    private static void DrawHolyNova(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float pulse = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI * 5f);
        int r = Mathf.RoundToInt(8 * pulse);

        FillRadialGradient(px, cx, cy, r,
            new Color(1f, 1f, 0.85f, 0.95f),
            new Color(1f, 0.85f, 0.3f, 0.75f),
            new Color(1f, 0.6f, 0f, 0f));

        // Vòng ngoài
        DrawCircleOutlineAlpha(px, cx, cy, r + 2, new Color(1f, 0.9f, 0.5f, 0.5f * pulse));
        DrawCircleOutlineAlpha(px, cx, cy, r + 4, new Color(1f, 0.9f, 0.5f, 0.25f * pulse));

        // Tia thánh 8 hướng
        float baseA = t * Mathf.PI * 3f;
        for (int i = 0; i < 8; i++)
        {
            float a = baseA + i * Mathf.PI / 4f;
            DrawRayFade(px, cx, cy, a, r + 1, r + 5, new Color(1f, 0.95f, 0.6f, 0.6f * pulse));
        }
    }

    // Bolt sét — zigzag vàng-trắng
    private static void DrawThunderBolt(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float pulse = 0.8f + 0.2f * Mathf.Sin(t * Mathf.PI * 8f);

        // Lõi sáng
        FillCircleAlpha(px, cx, cy, 4, new Color(0.9f, 0.9f, 1f, pulse * 0.9f));

        // 3 zigzag bolt
        int seed = Lcg((int)(t * 1999));
        for (int bolt = 0; bolt < 3; bolt++)
        {
            float a = bolt * Mathf.PI * 2f / 3f + t * Mathf.PI * 2f;
            int bx = cx, by = cy;
            for (int seg = 0; seg < 5; seg++)
            {
                seed = Lcg(seed);
                int nx = bx + Mathf.RoundToInt(Mathf.Cos(a) * 3 + (seed % 3) - 1);
                seed = Lcg(seed);
                int ny = by + Mathf.RoundToInt(Mathf.Sin(a) * 3 + (seed % 3) - 1);
                DrawThickLine(px, bx, by, nx, ny,
                    new Color(0.85f, 0.88f, 1f, pulse * (0.9f - seg * 0.15f)), 1);
                bx = nx; by = ny;
            }
        }
    }

    // Zeus bolt — dày hơn + hào quang xanh
    private static void DrawZeusBolt(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float pulse = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI * 10f);

        // Aura lớn
        FillCircleAlpha(px, cx, cy, 10, new Color(0.6f, 0.65f, 1f, 0.2f * pulse));
        FillCircleAlpha(px, cx, cy, 6, new Color(0.8f, 0.85f, 1f, 0.5f * pulse));
        FillCircleAlpha(px, cx, cy, 3, new Color(1f, 1f, 1f, 0.9f * pulse));

        // 5 bolt dày
        int seed = Lcg((int)(t * 3141));
        for (int bolt = 0; bolt < 5; bolt++)
        {
            float a = bolt * Mathf.PI * 2f / 5f + t * Mathf.PI * 1.5f;
            int bx = cx, by = cy;
            for (int seg = 0; seg < 4; seg++)
            {
                seed = Lcg(seed);
                int nx = bx + Mathf.RoundToInt(Mathf.Cos(a) * 3.5f + (seed % 3) - 1);
                seed = Lcg(seed);
                int ny = by + Mathf.RoundToInt(Mathf.Sin(a) * 3.5f + (seed % 3) - 1);
                DrawThickLine(px, bx, by, nx, ny,
                    new Color(0.7f, 0.8f, 1f, pulse * (0.95f - seg * 0.18f)), 2);
                bx = nx; by = ny;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  MUZZLE FLASH
    // ═══════════════════════════════════════════════════════════════

    private static void DrawMuzzleArrow(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = t * 1.8f;
        int r = Mathf.RoundToInt(6 * expand);
        FillCircleAlpha(px, cx, cy, r, new Color(0.9f, 0.85f, 0.6f, (1f - t) * 0.7f));
        // 4 tia ngắn
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f + 0.4f;
            DrawRayFade(px, cx, cy, a, r, r + Mathf.RoundToInt(4 * (1f - t)),
                new Color(1f, 0.9f, 0.6f, (1f - t) * 0.8f));
        }
    }

    private static void DrawMuzzleFire(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.2f + t * 0.8f;
        int r = Mathf.RoundToInt(9 * expand);
        FillRadialGradient(px, cx, cy, r,
            new Color(1f, 1f, 0.6f, (1f - t) * 0.9f),
            new Color(1f, 0.4f, 0f, (1f - t) * 0.6f),
            new Color(1f, 0.1f, 0f, 0f));
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f + t * 1.5f;
            DrawRayFade(px, cx, cy, a, r, r + Mathf.RoundToInt(5 * (1f - t)),
                new Color(1f, 0.6f, 0.1f, (1f - t) * 0.7f));
        }
    }

    private static void DrawMuzzleIce(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.3f + t * 0.7f;
        int r = Mathf.RoundToInt(8 * expand);
        FillCircleAlpha(px, cx, cy, r, new Color(0.8f, 0.95f, 1f, (1f - t) * 0.8f));
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            DrawRayFade(px, cx, cy, a, r, r + Mathf.RoundToInt(6 * (1f - t)),
                new Color(0.7f, 0.9f, 1f, (1f - t) * 0.9f));
        }
    }

    private static void DrawMuzzlePoison(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.3f + t * 0.7f;
        int r = Mathf.RoundToInt(7 * expand);
        FillCircleAlpha(px, cx, cy, r, new Color(0.5f, 1f, 0.3f, (1f - t) * 0.8f));
        int seed = Lcg((int)(t * 555));
        for (int i = 0; i < 5; i++)
        {
            seed = Lcg(seed);
            int rx = cx + (seed % (r + 4)) - (r + 4) / 2;
            seed = Lcg(seed);
            int ry = cy + (seed % (r + 4)) - (r + 4) / 2;
            BlendPixel(px, rx, ry, new Color(0.3f, 0.9f, 0.1f, (1f - t) * 0.6f));
        }
    }

    private static void DrawMuzzleHoly(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.2f + t * 0.8f;
        int r = Mathf.RoundToInt(9 * expand);
        FillRadialGradient(px, cx, cy, r,
            new Color(1f, 1f, 0.9f, (1f - t) * 0.95f),
            new Color(1f, 0.9f, 0.4f, (1f - t) * 0.6f),
            new Color(1f, 0.7f, 0f, 0f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            DrawRayFade(px, cx, cy, a, r, r + Mathf.RoundToInt(5 * (1f - t)),
                new Color(1f, 0.95f, 0.5f, (1f - t) * 0.8f));
        }
    }

    private static void DrawMuzzleLightning(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.3f + t * 0.7f;
        int r = Mathf.RoundToInt(8 * expand);
        FillCircleAlpha(px, cx, cy, r, new Color(0.75f, 0.8f, 1f, (1f - t) * 0.85f));
        // Tia sét mini 4 hướng
        int seed = Lcg((int)(t * 2222));
        for (int bolt = 0; bolt < 4; bolt++)
        {
            float a = bolt * Mathf.PI / 2f;
            int bx = cx, by = cy;
            for (int s = 0; s < 4; s++)
            {
                seed = Lcg(seed);
                int nx = bx + Mathf.RoundToInt(Mathf.Cos(a) * 2 + (seed % 3) - 1);
                seed = Lcg(seed);
                int ny = by + Mathf.RoundToInt(Mathf.Sin(a) * 2 + (seed % 3) - 1);
                DrawThickLine(px, bx, by, nx, ny,
                    new Color(0.85f, 0.9f, 1f, (1f - t) * (0.9f - s * 0.18f)), 1);
                bx = nx; by = ny;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  HIT BURST
    // ═══════════════════════════════════════════════════════════════

    private static void DrawHitArrow(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float r = 3f + t * 10f;
        float alpha = t < 0.3f ? t / 0.3f : Mathf.Max(0f, 1f - (t - 0.3f) / 0.7f);
        FillCircleAlpha(px, cx, cy, Mathf.RoundToInt(r),
            new Color(0.9f, 0.85f, 0.6f, alpha * 0.6f));
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f + t * 0.5f;
            DrawRayFade(px, cx, cy, a,
                Mathf.RoundToInt(r * 0.5f), Mathf.RoundToInt(r * 1.3f),
                new Color(0.9f, 0.85f, 0.55f, alpha * 0.7f));
        }
    }

    private static void DrawHitFire(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.3f + t * 0.7f;
        float alpha = t < 0.25f ? t / 0.25f : Mathf.Max(0f, 1f - (t - 0.25f) / 0.75f);
        int r = Mathf.RoundToInt(12 * expand);

        FillRadialGradient(px, cx, cy, r,
            new Color(1f, 0.95f, 0.6f, alpha * 0.9f),
            new Color(1f, 0.4f, 0f, alpha * 0.7f),
            new Color(0.8f, 0.1f, 0f, 0f));

        if (t < 0.6f)
        {
            float rayAlpha = alpha * (1f - t / 0.6f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f + t * 2f;
                DrawRayFade(px, cx, cy, a, r, r + Mathf.RoundToInt(6 * (1f - t)),
                    new Color(1f, 0.6f, 0.1f, rayAlpha));
            }
        }
    }

    private static void DrawHitIce(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.3f + t * 0.7f;
        float alpha = t < 0.25f ? t / 0.25f : Mathf.Max(0f, 1f - (t - 0.25f) / 0.75f);
        int r = Mathf.RoundToInt(11 * expand);

        FillRadialGradient(px, cx, cy, r,
            new Color(0.9f, 1f, 1f, alpha * 0.9f),
            new Color(0.4f, 0.75f, 1f, alpha * 0.7f),
            new Color(0.15f, 0.4f, 0.9f, 0f));

        // Spike 6 hướng
        float spikeAlpha = alpha * Mathf.Max(0f, 1f - t * 1.5f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI / 3f;
            DrawRayFade(px, cx, cy, a, r, r + Mathf.RoundToInt(8 * (1f - t)),
                new Color(0.75f, 0.92f, 1f, spikeAlpha));
        }
    }

    private static void DrawHitPoison(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.3f + t * 0.7f;
        float alpha = t < 0.25f ? t / 0.25f : Mathf.Max(0f, 1f - (t - 0.25f) / 0.75f);
        int r = Mathf.RoundToInt(10 * expand);

        FillRadialGradient(px, cx, cy, r,
            new Color(0.65f, 1f, 0.3f, alpha * 0.85f),
            new Color(0.25f, 0.75f, 0.1f, alpha * 0.6f),
            new Color(0.1f, 0.4f, 0.05f, 0f));

        // Bong bóng độc
        if (t < 0.7f)
        {
            int seed = Lcg((int)(t * 911));
            for (int b = 0; b < 5; b++)
            {
                seed = Lcg(seed);
                float a = (float)(seed & 0xFF) / 255f * Mathf.PI * 2f;
                float dist = r * (0.5f + (float)(Lcg(seed) & 0xFF) / 255f * 0.5f);
                int bx = cx + Mathf.RoundToInt(Mathf.Cos(a) * dist);
                int by = cy + Mathf.RoundToInt(Mathf.Sin(a) * dist);
                DrawCircleOutlineAlpha(px, bx, by, 2, new Color(0.4f, 1f, 0.2f, alpha * 0.7f));
            }
        }
    }

    private static void DrawHitHoly(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.25f + t * 0.75f;
        float alpha = t < 0.2f ? t / 0.2f : Mathf.Max(0f, 1f - (t - 0.2f) / 0.8f);
        int r = Mathf.RoundToInt(13 * expand);

        FillRadialGradient(px, cx, cy, r,
            new Color(1f, 1f, 0.9f, alpha * 0.95f),
            new Color(1f, 0.85f, 0.35f, alpha * 0.7f),
            new Color(1f, 0.6f, 0f, 0f));

        // Cross thánh
        if (t < 0.5f)
        {
            float ca = alpha * (1f - t * 2f);
            int crossLen = r + 4;
            DrawThickLine(px, cx, cy - crossLen, cx, cy + crossLen,
                new Color(1f, 1f, 0.7f, ca), 2);
            DrawThickLine(px, cx - crossLen, cy, cx + crossLen, cy,
                new Color(1f, 1f, 0.7f, ca), 2);
        }

        // 8 tia thánh
        float rayAlpha = alpha * Mathf.Max(0f, 1f - t * 1.3f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f + t;
            DrawRayFade(px, cx, cy, a, r, r + Mathf.RoundToInt(7 * (1f - t)),
                new Color(1f, 0.95f, 0.5f, rayAlpha));
        }
    }

    private static void DrawHitLightning(Color[] px, float t)
    {
        int cx = S / 2, cy = S / 2;
        float expand = 0.25f + t * 0.75f;
        float alpha = t < 0.2f ? t / 0.2f : Mathf.Max(0f, 1f - (t - 0.2f) / 0.8f);
        int r = Mathf.RoundToInt(11 * expand);

        FillRadialGradient(px, cx, cy, r,
            new Color(0.9f, 0.9f, 1f, alpha * 0.9f),
            new Color(0.55f, 0.65f, 1f, alpha * 0.65f),
            new Color(0.3f, 0.3f, 0.9f, 0f));

        // Bolt 4 hướng
        if (t < 0.55f)
        {
            int seed = Lcg((int)(t * 1414) + 7);
            float boltAlpha = alpha * (1f - t / 0.55f);
            for (int bolt = 0; bolt < 4; bolt++)
            {
                float a = bolt * Mathf.PI / 2f + t;
                int bx = cx, by = cy;
                for (int seg = 0; seg < 5; seg++)
                {
                    seed = Lcg(seed);
                    int nx = bx + Mathf.RoundToInt(Mathf.Cos(a) * 2.8f + (seed % 3) - 1);
                    seed = Lcg(seed);
                    int ny = by + Mathf.RoundToInt(Mathf.Sin(a) * 2.8f + (seed % 3) - 1);
                    DrawThickLine(px, bx, by, nx, ny,
                        new Color(0.85f, 0.9f, 1f, boltAlpha * (1f - seg * 0.15f)), 1);
                    bx = nx; by = ny;
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CORE BUILDER
    // ═══════════════════════════════════════════════════════════════

    private delegate void FramePainter(Color[] pixels, float t);

    private static Sprite[] Build(int n, FramePainter painter)
    {
        Sprite[] frames = new Sprite[n];
        for (int i = 0; i < n; i++)
        {
            float t = n <= 1 ? 0f : (float)i / (n - 1);
            Color[] px = new Color[S * S];
            painter(px, t);
            frames[i] = MakeSprite(px);
        }
        return frames;
    }

    private static Sprite MakeSprite(Color[] px)
    {
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S),
            new Vector2(0.5f, 0.5f), PPU);
    }

    // ═══════════════════════════════════════════════════════════════
    //  DRAWING PRIMITIVES
    // ═══════════════════════════════════════════════════════════════

    private static void BlendPixel(Color[] px, int x, int y, Color c)
    {
        if (x < 0 || x >= S || y < 0 || y >= S || c.a <= 0f)
            return;
        int idx = y * S + x;
        Color dst = px[idx];
        float a = c.a + dst.a * (1f - c.a);
        if (a < 0.001f) return;
        px[idx] = new Color(
            (c.r * c.a + dst.r * dst.a * (1f - c.a)) / a,
            (c.g * c.a + dst.g * dst.a * (1f - c.a)) / a,
            (c.b * c.a + dst.b * dst.a * (1f - c.a)) / a,
            a);
    }

    private static void FillCircleAlpha(Color[] px, int cx, int cy, int r, Color c)
    {
        if (r <= 0) return;
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > r) continue;
                float edge = Mathf.Clamp01(1f - (dist - (r - 1f)));
                BlendPixel(px, cx + dx, cy + dy, new Color(c.r, c.g, c.b, c.a * edge));
            }
    }

    private static void FillRadialGradient(Color[] px, int cx, int cy, int r,
        Color inner, Color mid, Color outer)
    {
        if (r <= 0) return;
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > r) continue;
                float f = dist / r;
                Color c = f < 0.5f
                    ? Color.Lerp(inner, mid, f * 2f)
                    : Color.Lerp(mid, outer, (f - 0.5f) * 2f);
                BlendPixel(px, cx + dx, cy + dy, c);
            }
    }

    private static void DrawRayFade(Color[] px, int cx, int cy, float angle,
        int rStart, int rEnd, Color c)
    {
        if (rEnd <= rStart) return;
        float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
        for (int r = rStart; r <= rEnd; r++)
        {
            float f = (float)(r - rStart) / (rEnd - rStart);
            Color col = new Color(c.r, c.g, c.b, c.a * (1f - f));
            BlendPixel(px, cx + Mathf.RoundToInt(cos * r), cy + Mathf.RoundToInt(sin * r), col);
        }
    }

    private static void DrawCircleOutlineAlpha(Color[] px, int cx, int cy, int r, Color c)
    {
        if (r <= 0) return;
        for (int dy = -r - 1; dy <= r + 1; dy++)
            for (int dx = -r - 1; dx <= r + 1; dx++)
            {
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float edge = Mathf.Clamp01(1f - Mathf.Abs(dist - r));
                if (edge < 0.05f) continue;
                BlendPixel(px, cx + dx, cy + dy, new Color(c.r, c.g, c.b, c.a * edge));
            }
    }

    private static void DrawThickLine(Color[] px, int x0, int y0, int x1, int y1,
        Color c, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int x = x0, y = y0;
        int steps = 0;
        while (steps++ < 64)
        {
            for (int tx = -thickness; tx <= thickness; tx++)
                for (int ty = -thickness; ty <= thickness; ty++)
                    if (tx * tx + ty * ty <= thickness * thickness)
                        BlendPixel(px, x + tx, y + ty, c);
            if (x == x1 && y == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
    }

    private static int Lcg(int seed) => seed * 1664525 + 1013904223;
}
