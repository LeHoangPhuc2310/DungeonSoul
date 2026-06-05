// DungeonSoul — GameScale.cs — BẢNG KÍCH THƯỚC CHUẨN duy nhất cho toàn game.
// Mọi nhân vật/quái/boss/đạn quy về chiều cao thế giới (world units) ở đây — nhất quán,
// không phụ thuộc PPU hay kích thước frame của từng nguồn sprite.

using UnityEngine;

public static class GameScale
{
    // --- Chiều cao thế giới chuẩn (world units) ---
    // Camera orthographicSize ~5.5 → màn hình cao ~11 unit. Tỉ lệ chuẩn survivor-like:
    public const float PlayerHeight = 1.85f;
    public const float EnemyHeight = 1.15f;   // quái thường — nhỏ hơn người chơi một chút
    public const float EliteHeight = 1.35f;
    public const float BruteHeight = 1.28f;
    public const float RunnerHeight = 1.05f;
    public const float BossHeightBase = 3.3f; // boss tầng thấp
    public const float BossHeightMax = 4.6f;  // boss tầng 10

    // --- ITEM rơi trong game (đường kính world units) ---
    public const float CoinSize = 0.5f;       // xu
    public const float ExpGemSize = 0.45f;    // ngọc EXP
    public const float ProjectileSize = 0.2f; // đạn (nhỏ gọn — giảm cho bớt to)
    public const float ChestHeight = 1.6f;    // rương (chiều cao world — to & rõ, ~2/3 chiều cao người chơi)

    /// <summary>Chiều cao quái theo archetype.</summary>
    public static float EnemyHeightFor(EnemyArchetype a)
    {
        switch (a)
        {
            case EnemyArchetype.Elite: return EliteHeight;
            case EnemyArchetype.Brute: return BruteHeight;
            case EnemyArchetype.Runner: return RunnerHeight;
            default: return EnemyHeight;
        }
    }

    /// <summary>Chiều cao boss theo wave (3/6/9/10).</summary>
    public static float BossHeightFor(int wave)
    {
        if (wave >= 10) return BossHeightMax;
        if (wave >= 9) return 3.3f;
        if (wave >= 6) return 3.0f;
        return BossHeightBase;
    }

    /// <summary>localScale đồng đều để sprite cao đúng targetHeight (dùng bounds thật của sprite).</summary>
    public static float ScaleFor(Sprite sprite, float targetHeight)
    {
        if (sprite == null)
            return 1f;
        float h = sprite.bounds.size.y;
        return h > 0.001f ? targetHeight / h : 1f;
    }

    public static void Fit(Transform t, Sprite sprite, float targetHeight)
    {
        if (t == null)
            return;
        float s = ScaleFor(sprite, targetHeight);
        t.localScale = new Vector3(s, s, 1f);
    }

    /// <summary>Scale quái — tránh frame pixel nhỏ (Tiny RPG) bị phóng quá to.</summary>
    public static void FitEnemy(Transform t, Sprite sprite, EnemyArchetype archetype)
    {
        if (t == null)
            return;

        float target = EnemyHeightFor(archetype);
        float s = ScaleFor(sprite, target);
        s = Mathf.Min(s, 3.2f);
        t.localScale = new Vector3(s, s, 1f);
    }
}
