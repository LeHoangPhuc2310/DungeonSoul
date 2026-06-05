// DungeonSoul — HeroKnightLibrary.cs — Sprite animation từ asset pack TheHeroKnight.
// Nguồn: github.com/DinoTang/TheHeroKnight (dùng học tập — không rõ license, không phát hành thương mại).
// Load các frame idle đã cắt sẵn trong Assets/Art/HeroKnight để hero/boss có animation.

using System.Collections.Generic;
using UnityEngine;

public static class HeroKnightLibrary
{
    private const string Root = "Assets/Art/HeroKnight/";
    private const string AnimSetRoot = "Assets/Enemy_Animations_Set/";

    private static readonly Dictionary<string, Sprite[]> frameCache = new Dictionary<string, Sprite[]>();

    // --- HERO: mỗi lớp nhân vật một bộ frame idle ---
    // Hiện asset chỉ có 1 hero (Knight). Dùng chung sheet, phân biệt bằng màu tint cho 3 lớp.
    public static Sprite[] GetHeroIdleFrames(HeroType hero)
    {
        // Knight cầm kiếm (Warrior) / cung (Ranger) / kiếm tint tím (Mage).
        switch (hero)
        {
            case HeroType.Ranger:
                return LoadFrames(Root + "Player/Sprite/Bow_State/Bow_Front/Bow_Front_Idle_Shadow.png",
                    Root + "Player/Sprite/Sword_State/Sword_Front/Sword_Front_Idle_Shadow.png");
            default:
                return LoadFrames(Root + "Player/Sprite/Sword_State/Sword_Front/Sword_Front_Idle_Shadow.png");
        }
    }

    public static Color GetHeroTint(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Mage: return new Color(0.75f, 0.6f, 1f);   // tím
            case HeroType.Ranger: return new Color(0.7f, 1f, 0.75f); // xanh lá nhạt
            default: return Color.white;                              // warrior gốc
        }
    }

    // --- BOSS: mỗi boss một loại quái idle ---
    public static Sprite[] GetBossIdleFrames(string bossName)
    {
        string n = string.IsNullOrEmpty(bossName) ? "" : bossName.ToLowerInvariant();

        if (n.Contains("golem") || n.Contains("stone") || n.Contains("dragon") || n.Contains("lord"))
            return LoadFrames(Root + "Boss/Sprite/Minotaurus_Idle.png");   // boss bò tót cho boss lớn
        if (n.Contains("witch") || n.Contains("shadow"))
            return LoadFrames(Root + "Skeleton/Sprite/Skeleton_Idle.png");
        // Goblin / mặc định
        return LoadFrames(Root + "Orc/Sprite/Orc-Idle.png");
    }

    // --- ENEMY thường: orc / skeleton idle ---
    public static Sprite[] GetEnemyIdleFrames(bool elite)
    {
        if (elite)
        {
            Sprite[] pack = LoadFrames(
                AnimSetRoot + "enemies-skeleton2_idle.png",
                AnimSetRoot + "enemies-skeleton1_idle.png",
                Root + "Skeleton/Sprite/Skeleton_Idle.png");
            if (pack.Length > 0)
                return pack;
        }

        return LoadFrames(Root + "Orc/Sprite/Orc-Idle.png");
    }

    // --- WALK animation ---
    public static Sprite[] GetEnemyWalkFrames(bool elite)
    {
        if (elite)
        {
            Sprite[] pack = LoadFrames(
                AnimSetRoot + "enemies-skeleton2_movemen.png",
                AnimSetRoot + "enemies-skeleton1_movement.png",
                Root + "Skeleton/Sprite/Skeleton_Idle.png");
            if (pack.Length > 0)
                return pack;
        }

        return LoadFrames(Root + "Orc/Sprite/Orc-Walk.png", Root + "Orc/Sprite/Orc-Idle.png");
    }

    public static Sprite[] GetEnemyAttackFrames(bool elite)
    {
        if (elite)
        {
            Sprite[] pack = LoadFramesMerged(
                AnimSetRoot + "enemies-skeleton2_attack.png",
                AnimSetRoot + "enemies-skeleton1_attack.png",
                Root + "Skeleton/Sprite/Skeleton_other.png");
            if (pack.Length > 0)
                return pack;
        }

        return LoadFramesMerged(
            Root + "Orc/Sprite/Orc-Attack01.png",
            Root + "Orc/Sprite/Orc-Attack02.png");
    }

    public static Sprite[] GetEnemyHurtFrames(bool elite)
    {
        if (elite)
        {
            Sprite[] pack = LoadFrames(
                AnimSetRoot + "enemies-skeleton2_take_damage.png",
                AnimSetRoot + "enemies-skeleton1_take_damage.png",
                Root + "Skeleton/Sprite/Skeleton_Idle.png");
            if (pack.Length > 0)
                return pack;
        }

        return LoadFrames(Root + "Orc/Sprite/Orc-Hurt.png", Root + "Orc/Sprite/Orc-Idle.png");
    }

    public static Sprite[] GetEnemyDeathFrames(bool elite)
    {
        if (elite)
        {
            Sprite[] pack = LoadFramesMerged(
                AnimSetRoot + "enemies-skeleton2_death.png",
                AnimSetRoot + "enemies-skeleton2_death2.png",
                AnimSetRoot + "enemies-skeleton1_death.png");
            if (pack.Length > 0)
                return pack;
        }

        return LoadFrames(Root + "Orc/Sprite/Orc-Death.png", Root + "Orc/Sprite/Orc-Idle.png");
    }

    public static Sprite[] GetHealFxFrames()
    {
        return LoadFramesMerged(
            Root + "FX/Healing/Sprite/Heal1.png",
            Root + "FX/Healing/Sprite/Heal2.png",
            Root + "FX/Healing/Sprite/Heal3.png",
            Root + "FX/Healing/Sprite/Heal4.png",
            Root + "FX/Healing/Sprite/Heal5.png",
            Root + "FX/Healing/Sprite/Heal6.png",
            Root + "FX/Healing/Sprite/Heal7.png");
    }

    public static Sprite GetArrowProjectileSprite()
    {
        Sprite[] frames = LoadFrames(Root + "Player/Sprite/Arrow/Arrow(projectile)/Arrow01(32x32).png",
            Root + "Player/Sprite/Arrow/Arrow(projectile)/Arrow01(100x100).png");
        return frames.Length > 0 ? frames[0] : null;
    }

    public static Sprite[] GetBossAttackEffectFrames()
    {
        return LoadFrames(Root + "Boss/Sprite/Attack2Effect.png", Root + "Boss/Sprite/Attack1.png");
    }

    public static Sprite[] GetBossWalkFrames(string bossName)
    {
        string n = string.IsNullOrEmpty(bossName) ? "" : bossName.ToLowerInvariant();
        if (n.Contains("golem") || n.Contains("stone") || n.Contains("dragon") || n.Contains("lord"))
            return LoadFrames(Root + "Boss/Sprite/Walk.png", Root + "Boss/Sprite/Minotaurus_Idle.png");
        if (n.Contains("witch") || n.Contains("shadow"))
            return LoadFrames(Root + "Skeleton/Sprite/Skeleton_Idle.png");
        return LoadFrames(Root + "Orc/Sprite/Orc-Walk.png", Root + "Orc/Sprite/Orc-Idle.png");
    }

    public static Sprite[] GetBossAttackFrames(string bossName)
    {
        string n = string.IsNullOrEmpty(bossName) ? "" : bossName.ToLowerInvariant();
        if (n.Contains("golem") || n.Contains("stone") || n.Contains("dragon") || n.Contains("lord"))
            return LoadFrames(Root + "Boss/Sprite/Attack1.png");
        if (!n.Contains("witch") && !n.Contains("shadow"))
            return LoadFrames(Root + "Orc/Sprite/Orc-Attack01.png");
        return System.Array.Empty<Sprite>();
    }

    public static Sprite[] GetBossHurtFrames(string bossName)
    {
        string n = string.IsNullOrEmpty(bossName) ? "" : bossName.ToLowerInvariant();
        if (n.Contains("golem") || n.Contains("stone") || n.Contains("dragon") || n.Contains("lord"))
            return LoadFrames(Root + "Boss/Sprite/Angry.png", Root + "Boss/Sprite/Minotaurus_Idle.png");
        return GetEnemyHurtFrames(false);
    }

    public static Sprite[] GetBossDeathFrames(string bossName)
    {
        string n = string.IsNullOrEmpty(bossName) ? "" : bossName.ToLowerInvariant();
        if (n.Contains("golem") || n.Contains("stone") || n.Contains("dragon") || n.Contains("lord"))
            return LoadFrames(Root + "Boss/Sprite/Dead.png", Root + "Boss/Sprite/Minotaurus_Idle.png");
        return LoadFrames(Root + "Orc/Sprite/Orc-Death.png", Root + "Orc/Sprite/Orc-Idle.png");
    }

    public static bool HasPack => GetHeroIdleFrames(HeroType.Warrior).Length > 0;

    // ── Load helpers ─────────────────────────────────────────────────────────

    /// <summary>Gộp frame từ nhiều file riêng (vd Heal1..Heal7).</summary>
    private static Sprite[] LoadFramesMerged(params string[] sheetPaths)
    {
        List<Sprite> merged = new List<Sprite>();
        for (int p = 0; p < sheetPaths.Length; p++)
        {
            Sprite[] part = LoadFrames(sheetPaths[p]);
            if (part == null || part.Length == 0)
                continue;
            for (int i = 0; i < part.Length; i++)
                merged.Add(part[i]);
        }

        return merged.Count > 0 ? merged.ToArray() : System.Array.Empty<Sprite>();
    }

    /// <summary>Load frame từ nhiều đường dẫn (lấy cái đầu có frame). Trả tất cả sprite con theo thứ tự tên.</summary>
    private static Sprite[] LoadFrames(params string[] sheetPaths)
    {
        for (int p = 0; p < sheetPaths.Length; p++)
        {
            string path = sheetPaths[p];
            if (frameCache.TryGetValue(path, out Sprite[] cached) && cached != null && cached.Length > 0)
                return cached;

#if UNITY_EDITOR
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite s)
                    sprites.Add(s);
            }

            if (sprites.Count > 0)
            {
                // Sắp theo số cuối tên (_0, _1, ...) để frame đúng thứ tự.
                sprites.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
                Sprite[] arr = sprites.ToArray();
                frameCache[path] = arr;
                return arr;
            }
#endif
        }

        return System.Array.Empty<Sprite>();
    }

    private static int FrameIndex(string name)
    {
        int u = name.LastIndexOf('_');
        if (u >= 0 && u < name.Length - 1 && int.TryParse(name.Substring(u + 1), out int idx))
            return idx;
        return 0;
    }
}
