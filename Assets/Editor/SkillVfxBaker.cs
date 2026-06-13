// DungeonSoul — Bake VFX pixel-art cho 28 SkillType (burst 6 frame 256×256 + aura loop 4 frame).
// Quy tắc: chỉ năng lượng/đạn/nổ/vòng sáng — KHÔNG nhân vật, KHÔNG storyboard, KHÔNG icon tĩnh.
// Vẽ trên lưới 64×64 rồi phóng ×4 nearest-neighbor để giữ chất pixel-art.

using System.IO;
using UnityEditor;
using UnityEngine;

public static class SkillVfxBaker
{
    private const int P = 64;          // lưới pixel-art
    private const int OUT = 256;       // kích thước file xuất
    private const float C = P / 2f;    // tâm canvas
    private const string Root = "Assets/Resources/GeneratedSkillVfx";

    private enum AuraOrnament { Dots, Dashes, Crystals, Crescents, Runes }

    [MenuItem("Tools/DungeonSoul/Bake Skill VFX (28 skills)")]
    public static void BakeAll()
    {
        // Xóa sạch asset sai cũ (PerSkill có người/icon tĩnh, Auras có nhân vật mẫu / dải trống).
        AssetDatabase.DeleteAsset($"{Root}/PerSkill");
        AssetDatabase.DeleteAsset($"{Root}/Auras");
        AssetDatabase.Refresh();

        int bursts = 0, auras = 0;
        foreach (SkillType type in System.Enum.GetValues(typeof(SkillType)))
        {
            string dir = $"{Root}/PerSkill/{type}";
            for (int i = 0; i < 6; i++)
            {
                // frame_00 đã thấy hiệu ứng xuất hiện, frame_05 còn tàn dư mờ — không frame nào trống.
                float t = Mathf.Lerp(0.07f, 0.96f, i / 5f);
                Color[] px = NewCanvas();
                DrawBurst(type, t, px);
                WriteFrame(dir, i, px);
                bursts++;
            }

            if (TryGetAuraStyle(type, out Color auraColor, out AuraOrnament ornament))
            {
                string auraDir = $"{Root}/Auras/Skills/{type}";
                for (int i = 0; i < 4; i++)
                {
                    Color[] px = NewCanvas();
                    DrawAura(px, i / 4f, auraColor, ornament);
                    WriteFrame(auraDir, i, px);
                    auras++;
                }
            }
        }

        int passiveAuras = BakePassiveAuras();

        AssetDatabase.Refresh();
        ApplyImportSettings();
        Debug.Log($"SkillVfxBaker: đã bake {bursts} burst + {auras} skill aura + {passiveAuras} passive aura frame.");
    }

    private static int BakePassiveAuras()
    {
        PassiveItemData[] items = Resources.LoadAll<PassiveItemData>("PassiveItems");
        int frames = 0;
        foreach (PassiveItemData item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.id))
                continue;

            TryGetPassiveAuraStyle(item.statModifierType, out Color col, out AuraOrnament ornament);
            string auraDir = $"{Root}/Auras/Passives/{item.id}";
            for (int i = 0; i < 4; i++)
            {
                Color[] px = NewCanvas();
                DrawAura(px, i / 4f, col, ornament);
                WriteFrame(auraDir, i, px);
                frames++;
            }
        }

        return frames;
    }

    private static void TryGetPassiveAuraStyle(PassiveStatModifierType stat, out Color color, out AuraOrnament ornament)
    {
        color = GameIconLibrary.PassiveTintByStat(stat);
        switch (stat)
        {
            case PassiveStatModifierType.Defense: ornament = AuraOrnament.Crescents; break;
            case PassiveStatModifierType.HP: ornament = AuraOrnament.Dots; break;
            case PassiveStatModifierType.MoveSpeed: ornament = AuraOrnament.Dashes; break;
            case PassiveStatModifierType.Damage: ornament = AuraOrnament.Crescents; break;
            case PassiveStatModifierType.CooldownReduction: ornament = AuraOrnament.Dashes; break;
            case PassiveStatModifierType.ExpGain: ornament = AuraOrnament.Dots; break;
            case PassiveStatModifierType.CritChance: ornament = AuraOrnament.Crystals; break;
            case PassiveStatModifierType.Magnet: ornament = AuraOrnament.Dots; break;
            case PassiveStatModifierType.BurnChance: ornament = AuraOrnament.Crystals; break;
            case PassiveStatModifierType.LifeSteal: ornament = AuraOrnament.Dots; break;
            case PassiveStatModifierType.ProjectileCount: ornament = AuraOrnament.Dashes; break;
            case PassiveStatModifierType.Revive: ornament = AuraOrnament.Runes; break;
            case PassiveStatModifierType.Luck:
                color = new Color(0.45f, 0.95f, 0.4f);
                ornament = AuraOrnament.Runes;
                break;
            case PassiveStatModifierType.AreaSize:
                color = new Color(0.75f, 0.55f, 1f);
                ornament = AuraOrnament.Dots;
                break;
            default: ornament = AuraOrnament.Dots; break;
        }
    }

    // ─── Aura styles ──────────────────────────────────────────────────────────

    private static bool TryGetAuraStyle(SkillType type, out Color color, out AuraOrnament ornament)
    {
        switch (type)
        {
            case SkillType.SpeedBoost: color = new Color(0.45f, 0.95f, 0.8f); ornament = AuraOrnament.Dashes; return true;
            case SkillType.IronBody: color = new Color(0.6f, 0.75f, 0.95f); ornament = AuraOrnament.Crescents; return true;
            case SkillType.ToughSkin: color = new Color(0.7f, 0.55f, 0.35f); ornament = AuraOrnament.Dots; return true;
            case SkillType.CoinMagnet: color = new Color(1f, 0.85f, 0.3f); ornament = AuraOrnament.Dots; return true;
            case SkillType.IceAura: color = new Color(0.6f, 0.9f, 1f); ornament = AuraOrnament.Crystals; return true;
            case SkillType.GhostForm: color = new Color(0.7f, 0.5f, 1f); ornament = AuraOrnament.Dots; return true;
            case SkillType.BladeStorm: color = new Color(0.92f, 0.92f, 0.88f); ornament = AuraOrnament.Crescents; return true;
            case SkillType.Vampire: color = new Color(0.95f, 0.25f, 0.3f); ornament = AuraOrnament.Dots; return true;
            case SkillType.PoisonCloud: color = new Color(0.5f, 0.95f, 0.3f); ornament = AuraOrnament.Dots; return true;
            case SkillType.LightningChain: color = new Color(0.6f, 0.7f, 1f); ornament = AuraOrnament.Dashes; return true;
            case SkillType.DeathMark: color = new Color(0.85f, 0.15f, 0.2f); ornament = AuraOrnament.Runes; return true;
            case SkillType.TimeFreeze: color = new Color(0.7f, 0.95f, 1f); ornament = AuraOrnament.Dashes; return true;
            case SkillType.DragonStrike: color = new Color(1f, 0.55f, 0.2f); ornament = AuraOrnament.Crystals; return true;
            case SkillType.SoulHarvest: color = new Color(0.7f, 0.45f, 1f); ornament = AuraOrnament.Runes; return true;
            case SkillType.MirrorImage: color = new Color(0.95f, 0.45f, 0.95f); ornament = AuraOrnament.Runes; return true;
            default: color = Color.white; ornament = AuraOrnament.Dots; return false;
        }
    }

    // Aura loop 4 frame: vòng sáng quanh điểm trống, ornament xoay đúng 1 bước/chu kỳ để loop mượt.
    private static void DrawAura(Color[] px, float phase, Color col, AuraOrnament ornament)
    {
        float breathe = Mathf.Sin(phase * Mathf.PI * 2f);
        float radius = 21f + breathe * 1.2f;

        FillRing(px, C, C, radius - 1.6f, radius + 1.6f, new Color(col.r, col.g, col.b, 0.55f));
        FillRing(px, C, C, radius - 4.5f, radius - 2.5f, new Color(col.r, col.g, col.b, 0.18f));

        const int count = 6;
        float spacing = Mathf.PI * 2f / count;
        float rot = phase * spacing; // sau 4 frame xoay đúng 1 spacing → loop khít

        for (int i = 0; i < count; i++)
        {
            float a = i * spacing + rot;
            float ox = C + Mathf.Cos(a) * radius;
            float oy = C + Mathf.Sin(a) * radius;
            Color bright = new Color(
                Mathf.Lerp(col.r, 1f, 0.5f), Mathf.Lerp(col.g, 1f, 0.5f), Mathf.Lerp(col.b, 1f, 0.5f), 0.9f);

            switch (ornament)
            {
                case AuraOrnament.Dots:
                    Dot(px, ox, oy, 1.6f, bright);
                    break;
                case AuraOrnament.Dashes:
                    // vạch tiếp tuyến (gió/tia)
                    RayLine(px, ox, oy, a + Mathf.PI * 0.5f, -3f, 3f, 1, bright, 0f);
                    break;
                case AuraOrnament.Crystals:
                    RayLine(px, C, C, a, radius, radius + 4.5f, 1, bright, 0.4f);
                    Dot(px, ox, oy, 1.1f, Color.white * 0.9f);
                    break;
                case AuraOrnament.Crescents:
                    ArcBand(px, C, C, radius - 1.5f, radius + 1.5f, a, 0.55f, bright);
                    break;
                case AuraOrnament.Runes:
                    Glyph(px, ox, oy, i, bright);
                    break;
            }
        }
    }

    // ─── Burst dispatcher ─────────────────────────────────────────────────────

    private static void DrawBurst(SkillType type, float t, Color[] px)
    {
        switch (type)
        {
            case SkillType.DoubleShot: DrawDoubleShot(px, t); break;
            case SkillType.QuadShot: DrawQuadShot(px, t); break;
            case SkillType.SpeedBoost: DrawSpeedBoost(px, t); break;
            case SkillType.IronBody: DrawIronBody(px, t); break;
            case SkillType.ToughSkin: DrawToughSkin(px, t); break;
            case SkillType.QuickReload: DrawQuickReload(px, t); break;
            case SkillType.CoinMagnet: DrawCoinMagnet(px, t); break;
            case SkillType.SteadyAim: DrawSteadyAim(px, t); break;
            case SkillType.PiercingArrow: DrawPiercingArrow(px, t); break;
            case SkillType.MultiTarget: DrawMultiTarget(px, t); break;
            case SkillType.CriticalHit: DrawCriticalHit(px, t); break;
            case SkillType.LifeSteal: DrawLifeSteal(px, t); break;
            case SkillType.FireArrow: DrawFireArrow(px, t); break;
            case SkillType.ExplosiveRounds: DrawExplosion(px, t, 0.72f); break;
            case SkillType.Boomerang: DrawBoomerang(px, t); break;
            case SkillType.TwinArrows: DrawTwinArrows(px, t); break;
            case SkillType.LightningChain: DrawLightningChain(px, t); break;
            case SkillType.Explosion: DrawExplosion(px, t, 1f); break;
            case SkillType.IceAura: DrawIceAuraBurst(px, t); break;
            case SkillType.GhostForm: DrawGhostForm(px, t); break;
            case SkillType.BladeStorm: DrawBladeStorm(px, t); break;
            case SkillType.Vampire: DrawVampire(px, t); break;
            case SkillType.PoisonCloud: DrawPoisonCloud(px, t); break;
            case SkillType.DeathMark: DrawDeathMark(px, t); break;
            case SkillType.TimeFreeze: DrawTimeFreeze(px, t); break;
            case SkillType.DragonStrike: DrawDragonStrike(px, t); break;
            case SkillType.SoulHarvest: DrawSoulHarvest(px, t); break;
            case SkillType.MirrorImage: DrawMirrorImage(px, t); break;
        }
    }

    private static float Env(float t, float inEnd = 0.12f, float outStart = 0.62f)
    {
        if (t < inEnd) return t / inEnd;
        if (t > outStart) return Mathf.Clamp01((1f - t) / (1f - outStart));
        return 1f;
    }

    // ─── A. Buff bursts ───────────────────────────────────────────────────────

    // 2 tia đạn song song bắn ra từ tâm, có chớp đầu nòng.
    private static void DrawDoubleShot(Color[] px, float t)
    {
        float a = Env(t);
        Color core = new Color(1f, 0.95f, 0.6f, a);
        Color trail = new Color(1f, 0.75f, 0.25f, a * 0.7f);

        for (int s = -1; s <= 1; s += 2)
        {
            float y = C + s * 5f;
            float head = Mathf.Lerp(10f, 58f, t);
            for (float x = Mathf.Max(8f, head - 15f); x <= head; x += 0.8f)
            {
                float f = 1f - (head - x) / 15f;
                Px(px, Mathf.RoundToInt(x), Mathf.RoundToInt(y),
                    new Color(trail.r, trail.g, trail.b, trail.a * f));
            }
            Dot(px, head, y, 1.8f, core);
            Sparkle(px, head, y, 2, new Color(1f, 1f, 0.85f, a));
        }

        if (t < 0.35f)
            Dot(px, 10f, C, Mathf.Lerp(4.5f, 1f, t / 0.35f), new Color(1f, 0.9f, 0.5f, a * 0.8f));
    }

    // Fan 4 mũi tên vuông góc bùng ra.
    private static void DrawQuadShot(Color[] px, float t)
    {
        float a = Env(t);
        float dist = Mathf.Lerp(5f, 26f, t);
        Color col = new Color(1f, 0.7f, 0.3f, a);

        for (int i = 0; i < 4; i++)
        {
            float ang = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
            float hx = C + Mathf.Cos(ang) * dist;
            float hy = C + Mathf.Sin(ang) * dist;
            RayLine(px, C, C, ang, Mathf.Max(3f, dist - 10f), dist, 1, new Color(1f, 0.55f, 0.15f, a * 0.6f), 0.5f);
            ArrowGlyph(px, hx, hy, ang, 7f, col);
        }

        if (t < 0.3f)
            Dot(px, C, C, Mathf.Lerp(4f, 1f, t / 0.3f), new Color(1f, 0.95f, 0.7f, a));
    }

    // Vòng gió xanh pulse lan ra + vệt gió xoáy.
    private static void DrawSpeedBoost(Color[] px, float t)
    {
        float a = Env(t);
        Color wind = new Color(0.45f, 0.95f, 0.8f, a);
        float radius = Mathf.Lerp(6f, 28f, t);

        Ring(px, C, C, radius, 2f, new Color(wind.r, wind.g, wind.b, a * 0.7f));

        for (int i = 0; i < 3; i++)
        {
            float baseA = i * Mathf.PI * 2f / 3f + t * 5f;
            ArcBand(px, C, C, 13f, 18f, baseA, 1.1f, new Color(0.7f, 1f, 0.9f, a * 0.85f));
        }

        for (int i = 0; i < 5; i++)
        {
            float sa = i * 1.7f + t * 7f;
            Dot(px, C + Mathf.Cos(sa) * 9f, C + Mathf.Sin(sa) * 9f, 0.9f, new Color(0.85f, 1f, 0.95f, a * 0.7f));
        }
    }

    // Khiên năng lượng bung ra (overshoot) rồi vỡ thành tia sáng.
    private static void DrawIronBody(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.58f);
        float radius = t < 0.3f ? Mathf.Lerp(4f, 22f, t / 0.3f)
                     : t < 0.5f ? Mathf.Lerp(22f, 19f, (t - 0.3f) / 0.2f)
                     : 19f;
        Color steel = new Color(0.6f, 0.78f, 1f, a);

        Ring(px, C, C, radius, 2.5f, steel);
        Ring(px, C, C, radius - 4f, 1.2f, new Color(steel.r, steel.g, steel.b, a * 0.4f));

        for (int i = 0; i < 6; i++)
        {
            float ang = i * Mathf.PI / 3f;
            Dot(px, C + Mathf.Cos(ang) * radius, C + Mathf.Sin(ang) * radius, 1.6f, new Color(0.95f, 1f, 1f, a));
        }

        if (t > 0.55f)
        {
            float burst = (t - 0.55f) / 0.45f;
            int seed = 9;
            for (int i = 0; i < 10; i++)
            {
                float ang = Rnd01(ref seed) * Mathf.PI * 2f;
                float d = radius + burst * 9f + Rnd01(ref seed) * 4f;
                Dot(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d, 0.9f,
                    new Color(0.8f, 0.9f, 1f, a * (1f - burst)));
            }
        }
    }

    // Mảnh đá ốp vào thành vòng rồi vụn rơi xuống.
    private static void DrawToughSkin(Color[] px, float t)
    {
        float a = Env(t, 0.12f, 0.6f);
        Color rock = new Color(0.62f, 0.48f, 0.32f, a);
        Color rockDark = new Color(0.4f, 0.3f, 0.2f, a);

        int seed = 23;
        for (int i = 0; i < 8; i++)
        {
            float ang = i * Mathf.PI / 4f + Rnd01(ref seed) * 0.3f;
            float settle = Mathf.Clamp01(t / 0.4f);
            float d = Mathf.Lerp(28f, 15f, settle);
            float drop = t > 0.6f ? (t - 0.6f) * 22f : 0f;
            float fall = t > 0.6f ? (t - 0.6f) * 2.2f : 0f;
            float ox = C + Mathf.Cos(ang) * d;
            float oy = C + Mathf.Sin(ang) * d - drop * Rnd01(ref seed);
            float r = 2.2f + Rnd01(ref seed) * 1.4f;
            Dot(px, ox, oy, r, i % 2 == 0 ? rock : rockDark);
            Dot(px, ox - 1f, oy + 1f, r * 0.45f, new Color(0.85f, 0.75f, 0.6f, a * (1f - fall)));
        }

        if (t > 0.3f && t < 0.8f)
            Ring(px, C, C, 15f, 1.2f, new Color(0.75f, 0.62f, 0.45f, a * 0.45f));
    }

    // Cung tia sáng xoắn nhanh quanh tâm như kim đồng hồ tua.
    private static void DrawQuickReload(Color[] px, float t)
    {
        float a = Env(t);
        Color gold = new Color(1f, 0.9f, 0.4f, a);
        float head = -Mathf.PI * 0.5f + t * Mathf.PI * 4f; // quét 2 vòng

        ArcBand(px, C, C, 14f, 19f, head - 1.1f, 2.2f, gold);
        Dot(px, C + Mathf.Cos(head) * 16.5f, C + Mathf.Sin(head) * 16.5f, 1.7f, new Color(1f, 1f, 0.85f, a));

        for (int i = 0; i < 12; i++)
        {
            float ang = i * Mathf.PI / 6f;
            Dot(px, C + Mathf.Cos(ang) * 21f, C + Mathf.Sin(ang) * 21f, 0.8f, new Color(1f, 0.85f, 0.4f, a * 0.5f));
        }

        Dot(px, C, C, 1.6f, new Color(1f, 0.95f, 0.7f, a * 0.9f));
    }

    // Xoáy hạt vàng hút về tâm.
    private static void DrawCoinMagnet(Color[] px, float t)
    {
        float a = Env(t, 0.12f, 0.68f);
        Color gold = new Color(1f, 0.85f, 0.25f, a);

        int seed = 41;
        for (int i = 0; i < 12; i++)
        {
            float ang0 = Rnd01(ref seed) * Mathf.PI * 2f;
            float lag = Rnd01(ref seed) * 0.25f;
            float tt = Mathf.Clamp01(t - lag) / Mathf.Max(0.01f, 1f - lag);
            float d = Mathf.Lerp(27f, 3f, tt);
            float ang = ang0 + tt * 3.2f; // xoáy vào trong
            float ox = C + Mathf.Cos(ang) * d;
            float oy = C + Mathf.Sin(ang) * d;
            Dot(px, ox, oy, 1.3f, gold);
            Sparkle(px, ox, oy, 1, new Color(1f, 1f, 0.7f, a * 0.8f));
        }

        Dot(px, C, C, Mathf.Lerp(2f, 5.5f, t), new Color(1f, 0.92f, 0.5f, a * 0.85f));
        Ring(px, C, C, Mathf.Lerp(26f, 8f, t), 1.1f, new Color(1f, 0.8f, 0.3f, a * 0.35f));
    }

    // 4 ngoặc crosshair siết vào tâm + chấm đỏ pulse.
    private static void DrawSteadyAim(Color[] px, float t)
    {
        float a = Env(t);
        float d = Mathf.Lerp(23f, 10f, Mathf.Clamp01(t / 0.5f));
        Color red = new Color(1f, 0.35f, 0.25f, a);

        for (int i = 0; i < 4; i++)
        {
            float sx = (i & 1) == 0 ? -1f : 1f;
            float sy = (i & 2) == 0 ? -1f : 1f;
            float bx = C + sx * d, by = C + sy * d;
            Line(px, bx, by, bx - sx * 6f, by, red, 1);
            Line(px, bx, by, bx, by - sy * 6f, red, 1);
        }

        if (t > 0.4f)
        {
            float f = Mathf.Clamp01((t - 0.4f) / 0.25f);
            Color cross = new Color(1f, 0.55f, 0.4f, a * f * 0.8f);
            Line(px, C - 8f, C, C + 8f, C, cross, 0);
            Line(px, C, C - 8f, C, C + 8f, cross, 0);
        }

        float pulse = 1.4f + Mathf.Sin(t * Mathf.PI * 3f) * 0.6f;
        Dot(px, C, C, pulse, new Color(1f, 0.25f, 0.15f, a));
    }

    // Mũi tên lớn xuyên ngang, để lại 2 vòng "lỗ xuyên" giãn nở.
    private static void DrawPiercingArrow(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.7f);
        float head = Mathf.Lerp(4f, 60f, Mathf.Clamp01(t / 0.75f));
        Color core = new Color(0.95f, 1f, 1f, a);
        Color trail = new Color(0.45f, 0.85f, 1f, a * 0.65f);

        for (float x = 4f; x <= head; x += 0.7f)
        {
            float f = Mathf.Clamp01(1f - (head - x) / 28f);
            Px(px, Mathf.RoundToInt(x), Mathf.RoundToInt(C),
                new Color(trail.r, trail.g, trail.b, trail.a * (0.3f + f * 0.7f)));
            if (f > 0.7f)
            {
                Px(px, Mathf.RoundToInt(x), Mathf.RoundToInt(C - 1), new Color(trail.r, trail.g, trail.b, trail.a * 0.4f));
                Px(px, Mathf.RoundToInt(x), Mathf.RoundToInt(C + 1), new Color(trail.r, trail.g, trail.b, trail.a * 0.4f));
            }
        }

        ArrowGlyph(px, head, C, 0f, 8f, core);

        float[] holes = { 24f, 40f };
        for (int i = 0; i < holes.Length; i++)
        {
            if (head > holes[i])
            {
                float grow = Mathf.Clamp01((head - holes[i]) / 25f);
                Ring(px, holes[i], C, 2f + grow * 4f, 1f, new Color(0.7f, 0.95f, 1f, a * (1f - grow) * 0.8f));
            }
        }
    }

    // 3 tia phân nhánh từ tâm, mỗi đầu có reticle flash.
    private static void DrawMultiTarget(Color[] px, float t)
    {
        float a = Env(t);
        float len = Mathf.Lerp(4f, 26f, Mathf.Clamp01(t / 0.6f));
        Color col = new Color(1f, 0.65f, 0.25f, a);

        for (int i = 0; i < 3; i++)
        {
            float ang = -Mathf.PI / 6f + i * Mathf.PI * 2f / 3f;
            RayLine(px, C, C, ang, 2f, len, 1, col, 0.35f);
            float tipX = C + Mathf.Cos(ang) * len;
            float tipY = C + Mathf.Sin(ang) * len;
            if (t > 0.45f)
            {
                float f = Mathf.Clamp01((t - 0.45f) / 0.2f);
                Ring(px, tipX, tipY, 3.5f * f + 1f, 1f, new Color(1f, 0.8f, 0.4f, a * 0.9f));
                Dot(px, tipX, tipY, 1f, new Color(1f, 0.95f, 0.7f, a));
            }
        }

        Dot(px, C, C, 2f, new Color(1f, 0.85f, 0.5f, a * 0.9f));
    }

    // 2 nhát chém vàng chéo nhau + spark burst.
    private static void DrawCriticalHit(Color[] px, float t)
    {
        float a = Env(t, 0.08f, 0.62f);
        Color gold = new Color(1f, 0.85f, 0.3f, a);
        Color white = new Color(1f, 1f, 0.9f, a);

        float s1 = Mathf.Clamp01(t / 0.35f);
        if (s1 > 0f)
        {
            float x0 = C - 18f, y0 = C + 18f;
            float x1 = Mathf.Lerp(x0, C + 18f, s1), y1 = Mathf.Lerp(y0, C - 18f, s1);
            Line(px, x0, y0, x1, y1, gold, 1);
            Dot(px, x1, y1, 1.6f, white);
        }

        float s2 = Mathf.Clamp01((t - 0.18f) / 0.35f);
        if (s2 > 0f)
        {
            float x0 = C - 18f, y0 = C - 18f;
            float x1 = Mathf.Lerp(x0, C + 18f, s2), y1 = Mathf.Lerp(y0, C + 18f, s2);
            Line(px, x0, y0, x1, y1, gold, 1);
            Dot(px, x1, y1, 1.6f, white);
        }

        if (t > 0.45f)
        {
            float f = (t - 0.45f) / 0.55f;
            int seed = 77;
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f + 0.4f;
                float d = 4f + f * 14f + Rnd01(ref seed) * 3f;
                Sparkle(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d, 1, new Color(1f, 0.9f, 0.45f, a * (1f - f)));
            }
            Dot(px, C, C, 3f * (1f - f) + 1f, white);
        }
    }

    // Giọt máu đỏ hút vào tâm → pulse hồi máu xanh.
    private static void DrawLifeSteal(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.7f);
        Color blood = new Color(0.9f, 0.15f, 0.2f, a);

        int seed = 55;
        for (int i = 0; i < 8; i++)
        {
            float ang = i * Mathf.PI / 4f + Rnd01(ref seed) * 0.4f;
            float tt = Mathf.Clamp01(t / 0.6f);
            float d = Mathf.Lerp(26f, 3f, tt);
            float ox = C + Mathf.Cos(ang) * d;
            float oy = C + Mathf.Sin(ang) * d;
            Dot(px, ox, oy, 1.5f, blood);
            Dot(px, ox, oy + 1.6f, 0.8f, new Color(blood.r, blood.g, blood.b, blood.a * 0.6f)); // đuôi giọt
        }

        if (t > 0.5f)
        {
            float f = (t - 0.5f) / 0.5f;
            Color heal = new Color(0.35f, 1f, 0.5f, a * f);
            Ring(px, C, C, 4f + f * 12f, 1.5f, heal);
            Line(px, C - 3f, C, C + 3f, C, heal, 1);
            Line(px, C, C - 3f, C, C + 3f, heal, 1);
        }
        else
            Dot(px, C, C, 2.5f, blood);
    }

    // ─── B. Element bursts ────────────────────────────────────────────────────

    // Ngọn lửa nhỏ bùng cháy tại điểm trúng — lưỡi lửa lắc lư + tàn lửa bay lên.
    private static void DrawFireArrow(Color[] px, float t)
    {
        float a = Env(t, 0.12f, 0.6f);
        float h = Mathf.Lerp(6f, 18f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
        int seed = 13 + Mathf.RoundToInt(t * 60f);

        for (float y = 0f; y <= h; y += 1f)
        {
            float f = y / h;
            float width = Mathf.Lerp(6f, 0.8f, f);
            float sway = Mathf.Sin(t * 9f + y * 0.5f) * f * 2.5f;
            Color col = f < 0.3f ? new Color(1f, 0.95f, 0.6f, a)
                      : f < 0.65f ? new Color(1f, 0.55f, 0.1f, a * 0.95f)
                      : new Color(0.9f, 0.2f, 0.05f, a * 0.8f);
            Dot(px, C + sway, C - 6f + y, width, col);
        }

        for (int i = 0; i < 6; i++)
        {
            float ex = C + (Rnd01(ref seed) - 0.5f) * 14f;
            float ey = C + 4f + Rnd01(ref seed) * 10f + t * 12f;
            Dot(px, ex, ey, 0.8f, new Color(1f, 0.6f, 0.15f, a * (1f - t) * 0.9f));
        }
    }

    // Nổ lửa: lõi flash → vòng lửa giãn → khói. scale điều chỉnh ExplosiveRounds (nhỏ) vs Explosion (to).
    private static void DrawExplosion(Color[] px, float t, float scale)
    {
        float a = Env(t, 0.08f, 0.6f);
        float radius = (t < 0.5f ? Mathf.Lerp(4f, 26f, t * 2f) : Mathf.Lerp(26f, 20f, (t - 0.5f) * 2f)) * scale;

        if (t < 0.35f)
            Dot(px, C, C, radius * 0.55f, new Color(1f, 0.98f, 0.75f, a));

        FillRing(px, C, C, radius * 0.4f, radius * 0.75f, new Color(1f, 0.55f, 0.1f, a * 0.9f));
        FillRing(px, C, C, radius * 0.75f, radius, new Color(0.9f, 0.18f, 0.05f, a * 0.7f));
        Ring(px, C, C, radius * 1.15f, 1.2f, new Color(1f, 0.8f, 0.4f, a * 0.5f));

        int seed = 42;
        if (t > 0.4f)
        {
            for (int i = 0; i < 10; i++)
            {
                float ang = Rnd01(ref seed) * Mathf.PI * 2f;
                float d = radius * (1f + Rnd01(ref seed) * 0.5f);
                Dot(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d, 1f,
                    new Color(1f, 0.5f, 0.1f, a * (1f - t) * 1.4f));
            }
        }

        if (t > 0.55f)
        {
            float f = (t - 0.55f) / 0.45f;
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI / 3f + 0.5f;
                float d = radius * 0.9f;
                Dot(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d - f * 4f, 3f,
                    new Color(0.35f, 0.3f, 0.3f, a * 0.5f));
            }
        }
    }

    // Vệt cong boomerang quét quanh tâm + sparkle tại điểm đổi hướng.
    private static void DrawBoomerang(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.7f);
        Color wood = new Color(0.9f, 0.75f, 0.45f, a);
        float head = t * Mathf.PI * 1.6f; // quét gần nửa vòng

        ArcBand(px, C, C, 14f, 19f, head - 0.9f, 1.8f, wood);

        float hx = C + Mathf.Cos(head) * 16.5f;
        float hy = C + Mathf.Sin(head) * 16.5f;
        // hình boomerang chữ V nhỏ tại đầu vệt
        float tangent = head + Mathf.PI * 0.5f;
        Line(px, hx, hy, hx + Mathf.Cos(tangent + 0.5f) * 4f, hy + Mathf.Sin(tangent + 0.5f) * 4f, new Color(1f, 0.9f, 0.6f, a), 1);
        Line(px, hx, hy, hx + Mathf.Cos(tangent - 2.6f) * 4f, hy + Mathf.Sin(tangent - 2.6f) * 4f, new Color(1f, 0.9f, 0.6f, a), 1);

        if (t > 0.38f && t < 0.62f)
        {
            float f = 1f - Mathf.Abs(t - 0.5f) / 0.12f;
            Sparkle(px, hx, hy, 3, new Color(1f, 1f, 0.8f, a * f));
        }
    }

    // Mũi tên thứ 2 bay theo delay từ cùng điểm xuất phát.
    private static void DrawTwinArrows(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.7f);
        Color gold = new Color(1f, 0.92f, 0.55f, a);
        Color pale = new Color(0.8f, 0.85f, 1f, a * 0.9f);

        float h1 = Mathf.Lerp(8f, 56f, Mathf.Clamp01(t / 0.8f));
        for (float x = Mathf.Max(8f, h1 - 12f); x <= h1; x += 0.8f)
            Px(px, Mathf.RoundToInt(x), Mathf.RoundToInt(C - 3f),
                new Color(gold.r, gold.g, gold.b, gold.a * (1f - (h1 - x) / 12f)));
        ArrowGlyph(px, h1, C - 3f, 0f, 6f, gold);

        if (t > 0.2f)
        {
            float h2 = Mathf.Lerp(8f, 56f, Mathf.Clamp01((t - 0.2f) / 0.8f));
            for (float x = Mathf.Max(8f, h2 - 12f); x <= h2; x += 0.8f)
                Px(px, Mathf.RoundToInt(x), Mathf.RoundToInt(C + 3f),
                    new Color(pale.r, pale.g, pale.b, pale.a * (1f - (h2 - x) / 12f)));
            ArrowGlyph(px, h2, C + 3f, 0f, 6f, pale);
        }
        else
            Dot(px, 8f, C + 3f, 1.5f, new Color(pale.r, pale.g, pale.b, a * (t / 0.2f)));
    }

    // Tia sét nối điểm A (trái) → B (phải), vẽ ngang toàn canvas để code xoay theo hướng thật.
    private static void DrawLightningChain(Color[] px, float t)
    {
        float a = t < 0.15f ? t / 0.15f : t < 0.5f ? 1f : Mathf.Clamp01((1f - t) / 0.5f);
        float ax = 6f, bx = 58f;
        Color core = new Color(0.95f, 0.97f, 1f, a);
        Color glow = new Color(0.45f, 0.55f, 1f, a * 0.6f);

        Dot(px, ax, C, 3f + Mathf.Sin(t * Mathf.PI) * 1.5f, new Color(0.8f, 0.85f, 1f, a * 0.9f));
        Dot(px, bx, C, 3f + Mathf.Sin(t * Mathf.PI + 0.8f) * 1.5f, new Color(0.8f, 0.85f, 1f, a * 0.9f));

        int seed = 19 + Mathf.RoundToInt(t * 50f); // bolt giật khác nhau mỗi frame
        Bolt(px, ax, C, bx, C, seed, 4.5f, core, glow);

        if (t > 0.15f && t < 0.6f)
        {
            Bolt(px, (ax + bx) * 0.5f, C, (ax + bx) * 0.5f + 8f, C - 9f, seed + 7, 3f,
                new Color(core.r, core.g, core.b, a * 0.6f), new Color(glow.r, glow.g, glow.b, a * 0.3f));
            Bolt(px, (ax + bx) * 0.35f, C, (ax + bx) * 0.35f - 6f, C + 8f, seed + 13, 3f,
                new Color(core.r, core.g, core.b, a * 0.5f), new Color(glow.r, glow.g, glow.b, a * 0.3f));
        }
    }

    // ─── C. Epic bursts ───────────────────────────────────────────────────────

    // Vòng băng giãn nở với gai tinh thể trên rìa.
    private static void DrawIceAuraBurst(Color[] px, float t)
    {
        float a = Env(t, 0.12f, 0.62f);
        float radius = Mathf.Lerp(7f, 26f, t);
        Color ice = new Color(0.6f, 0.9f, 1f, a);

        Ring(px, C, C, radius, 2f, ice);
        Ring(px, C, C, radius * 0.65f, 1.2f, new Color(ice.r, ice.g, ice.b, a * 0.45f));

        for (int i = 0; i < 8; i++)
        {
            float ang = i * Mathf.PI / 4f + t * 0.6f;
            RayLine(px, C, C, ang, radius, radius + 5f, 1, new Color(0.85f, 0.97f, 1f, a * 0.9f), 0.5f);
            Dot(px, C + Mathf.Cos(ang) * (radius + 5f), C + Mathf.Sin(ang) * (radius + 5f), 0.9f, Color.white);
        }

        int seed = 67;
        for (int i = 0; i < 6; i++)
        {
            float ang = Rnd01(ref seed) * Mathf.PI * 2f;
            float d = radius * (0.3f + Rnd01(ref seed) * 0.5f);
            Sparkle(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d, 1, new Color(0.9f, 1f, 1f, a * 0.7f));
        }
    }

    // Sương ma tím cuộn lên rồi tan — CHỈ khói, không hình người.
    private static void DrawGhostForm(Color[] px, float t)
    {
        float a = Env(t, 0.15f, 0.6f);
        int seed = 29;

        for (int i = 0; i < 6; i++)
        {
            float baseAng = Rnd01(ref seed) * Mathf.PI * 2f;
            float baseDist = 4f + Rnd01(ref seed) * 9f;
            float rise = t * (8f + Rnd01(ref seed) * 7f);
            float sway = Mathf.Sin(t * 6f + i * 1.3f) * 3f;
            float ox = C + Mathf.Cos(baseAng) * baseDist + sway;
            float oy = C - 4f + Mathf.Sin(baseAng) * baseDist * 0.5f + rise;
            float r = 3.5f + Rnd01(ref seed) * 2.5f - t * 1.5f;
            if (r > 0.5f)
            {
                Dot(px, ox, oy, r, new Color(0.55f, 0.4f, 0.85f, a * 0.55f));
                Dot(px, ox + 1f, oy + 1f, r * 0.5f, new Color(0.8f, 0.7f, 1f, a * 0.5f));
            }
        }

        Ring(px, C, C, Mathf.Lerp(10f, 20f, t), 1.5f, new Color(0.65f, 0.5f, 1f, a * 0.5f));

        for (int i = 0; i < 5; i++)
        {
            float ang = Rnd01(ref seed) * Mathf.PI * 2f;
            float d = 6f + Rnd01(ref seed) * 14f;
            Sparkle(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d - t * 6f, 1, new Color(0.85f, 0.75f, 1f, a * 0.8f));
        }
    }

    // 3 lưỡi chém trắng bay quanh tâm.
    private static void DrawBladeStorm(Color[] px, float t)
    {
        float a = Env(t, 0.08f, 0.65f);
        Color blade = new Color(0.95f, 0.95f, 0.9f, a);

        for (int i = 0; i < 3; i++)
        {
            float ang = i * Mathf.PI * 2f / 3f + t * Mathf.PI * 2.4f;
            ArcBand(px, C, C, 15f, 20f, ang, 1.25f, blade);
            float hx = C + Mathf.Cos(ang + 0.62f) * 17.5f;
            float hy = C + Mathf.Sin(ang + 0.62f) * 17.5f;
            Dot(px, hx, hy, 1.5f, Color.white);
        }

        Ring(px, C, C, 9f, 1f, new Color(0.85f, 0.85f, 0.8f, a * 0.35f));
        Dot(px, C, C, 1.5f, new Color(1f, 1f, 0.95f, a * 0.6f));
    }

    // Hạt máu xoáy hút về tâm + 2 vết nanh + pulse đỏ thẫm.
    private static void DrawVampire(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.65f);
        Color blood = new Color(0.85f, 0.1f, 0.18f, a);

        int seed = 71;
        for (int i = 0; i < 10; i++)
        {
            float ang0 = Rnd01(ref seed) * Mathf.PI * 2f;
            float tt = Mathf.Clamp01(t / 0.65f);
            float d = Mathf.Lerp(26f, 3f, tt);
            float ang = ang0 + tt * 2.4f;
            Dot(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d, 1.2f, blood);
        }

        if (t < 0.45f)
        {
            float f = Env(t / 0.45f, 0.2f, 0.7f);
            Color fang = new Color(1f, 0.95f, 0.9f, f);
            Line(px, C - 4f, C + 5f, C - 2.5f, C - 2f, fang, 1);
            Line(px, C + 4f, C + 5f, C + 2.5f, C - 2f, fang, 1);
        }

        if (t > 0.5f)
        {
            float f = (t - 0.5f) / 0.5f;
            Ring(px, C, C, 4f + f * 11f, 1.6f, new Color(0.9f, 0.2f, 0.25f, a * (1f - f * 0.4f)));
            Dot(px, C, C, 3f * (1f - f) + 1f, new Color(1f, 0.4f, 0.4f, a));
        }
    }

    // Mây độc xanh phồng to, bong bóng nổ lụp bụp, giọt độc rơi.
    private static void DrawPoisonCloud(Color[] px, float t)
    {
        float a = Env(t, 0.15f, 0.62f);
        float radius = Mathf.Lerp(6f, 22f, Mathf.Clamp01(t / 0.7f));
        Color dark = new Color(0.12f, 0.42f, 0.1f, a * 0.85f);
        Color bright = new Color(0.4f, 0.9f, 0.2f, a * 0.9f);

        int seed = 31;
        for (int i = 0; i < 7; i++)
        {
            float ang = Rnd01(ref seed) * Mathf.PI * 2f;
            float d = radius * (0.2f + Rnd01(ref seed) * 0.6f);
            float r = 3f + Rnd01(ref seed) * 4f;
            Dot(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d * 0.7f, r, i % 2 == 0 ? bright : dark);
        }

        for (int i = 0; i < 5; i++)
        {
            float pop = Mathf.Repeat(t * 2f + i * 0.37f, 1f);
            float ang = i * 1.4f + 0.8f;
            float d = radius * (0.7f + (i % 3) * 0.2f);
            Ring(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d * 0.7f, 1f + pop * 2.5f, 0.8f,
                new Color(0.6f, 1f, 0.3f, a * (1f - pop)));
        }

        if (t > 0.4f)
        {
            for (int i = 0; i < 4; i++)
            {
                float dx = C + (Rnd01(ref seed) - 0.5f) * radius * 1.6f;
                float dy = C - 4f - (t - 0.4f) * 16f - Rnd01(ref seed) * 4f;
                Dot(px, dx, dy, 0.8f, new Color(0.45f, 0.95f, 0.15f, a * (1f - t) * 1.2f));
            }
        }
    }

    // Dấu rune đỏ hiện dần → kích nổ flash.
    private static void DrawDeathMark(Color[] px, float t)
    {
        if (t < 0.55f)
        {
            float f = Mathf.Clamp01(t / 0.35f);
            Color red = new Color(0.9f, 0.12f, 0.18f, f);
            Ring(px, C, C, 13f, 1.6f, red);
            Line(px, C - 8f, C - 8f, C + 8f, C + 8f, red, 1);
            Line(px, C - 8f, C + 8f, C + 8f, C - 8f, red, 1);
            for (int i = 0; i < 4; i++)
            {
                float ang = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                Glyph(px, C + Mathf.Cos(ang) * 18f, C + Mathf.Sin(ang) * 18f, i, new Color(1f, 0.3f, 0.3f, f * 0.9f));
            }
            Dot(px, C, C, 1.8f + Mathf.Sin(t * 20f) * 0.7f, new Color(1f, 0.5f, 0.4f, f));
        }
        else
        {
            float f = (t - 0.55f) / 0.45f;
            float a = 1f - f * 0.9f;
            Dot(px, C, C, Mathf.Lerp(4f, 14f, f), new Color(1f, 0.85f, 0.6f, a));
            FillRing(px, C, C, Mathf.Lerp(6f, 18f, f), Mathf.Lerp(10f, 24f, f), new Color(0.95f, 0.25f, 0.15f, a * 0.8f));
            int seed = 83;
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f + 0.3f;
                float d = Mathf.Lerp(12f, 26f, f) + Rnd01(ref seed) * 3f;
                Sparkle(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d, 1, new Color(1f, 0.45f, 0.3f, a));
            }
        }
    }

    // ─── D. Legendary bursts ──────────────────────────────────────────────────

    // Sóng băng + tia kim đồng hồ + đồng hồ nhỏ ở tâm.
    private static void DrawTimeFreeze(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.62f);
        Color ice = new Color(0.7f, 0.95f, 1f, a);

        Ring(px, C, C, Mathf.Lerp(5f, 29f, t), 1.6f, ice);
        Ring(px, C, C, Mathf.Lerp(2f, 22f, Mathf.Clamp01(t - 0.15f) / 0.85f), 1f, new Color(ice.r, ice.g, ice.b, a * 0.5f));

        for (int i = 0; i < 12; i++)
        {
            float flashT = i / 12f;
            float f = Mathf.Clamp01(1f - Mathf.Abs(t - flashT) * 4f);
            if (f <= 0f) continue;
            float ang = i * Mathf.PI / 6f - Mathf.PI * 0.5f;
            RayLine(px, C, C, ang, 17f, 21f, 1, new Color(0.85f, 1f, 1f, a * f), 0f);
        }

        Ring(px, C, C, 6.5f, 1f, new Color(0.8f, 0.95f, 1f, a * 0.9f));
        float hourA = -Mathf.PI * 0.5f + t * Mathf.PI * 4f;
        float minA = -Mathf.PI * 0.5f + t * Mathf.PI * 8f;
        Line(px, C, C, C + Mathf.Cos(hourA) * 3.5f, C + Mathf.Sin(hourA) * 3.5f, new Color(1f, 1f, 1f, a), 0);
        Line(px, C, C, C + Mathf.Cos(minA) * 5f, C + Mathf.Sin(minA) * 5f, new Color(0.9f, 1f, 1f, a * 0.8f), 0);

        int seed = 91;
        for (int i = 0; i < 7; i++)
        {
            float ang = Rnd01(ref seed) * Mathf.PI * 2f;
            float d = 10f + Rnd01(ref seed) * 16f;
            Sparkle(px, C + Mathf.Cos(ang) * d, C + Mathf.Sin(ang) * d, 1, new Color(0.95f, 1f, 1f, a * 0.8f));
        }
    }

    // Cột lửa giáng từ trên xuống vị trí enemy + nổ chân cột. KHÔNG vẽ con rồng.
    private static void DrawDragonStrike(Color[] px, float t)
    {
        float a = Env(t, 0.08f, 0.65f);
        float baseY = 18f;
        float topY = 62f;
        float reach = Mathf.Lerp(topY, baseY, Mathf.Clamp01(t / 0.35f)); // đầu cột lao xuống

        float coreW = t < 0.5f ? Mathf.Lerp(2f, 4.5f, t * 2f) : Mathf.Lerp(4.5f, 1.5f, (t - 0.5f) * 2f);
        for (float y = reach; y <= topY; y += 1f)
        {
            float fy = (y - baseY) / (topY - baseY);
            Dot(px, C, y, coreW, new Color(1f, 0.92f, 0.55f, a * (1f - fy * 0.35f)));
            Dot(px, C, y, coreW * 2.1f, new Color(1f, 0.5f, 0.1f, a * 0.45f * (1f - fy * 0.5f)));
        }

        if (t > 0.3f)
        {
            float f = Mathf.Clamp01((t - 0.3f) / 0.7f);
            FillRing(px, C, baseY, f * 6f, f * 13f, new Color(1f, 0.45f, 0.08f, a * (1f - f * 0.5f)));
            Ring(px, C, baseY, f * 16f, 1.2f, new Color(1f, 0.7f, 0.3f, a * (1f - f)));
            int seed = 37;
            for (int i = 0; i < 9; i++)
            {
                float ang = Rnd01(ref seed) * Mathf.PI; // bắn lên trên
                float d = 6f + f * 14f + Rnd01(ref seed) * 4f;
                Dot(px, C + Mathf.Cos(ang) * d, baseY + Mathf.Sin(ang) * d * 0.8f, 1f,
                    new Color(1f, 0.6f, 0.15f, a * (1f - f)));
            }
        }
    }

    // Orb linh hồn tím bay lên với wisp xoắn quanh.
    private static void DrawSoulHarvest(Color[] px, float t)
    {
        float a = Env(t, 0.12f, 0.68f);
        float oy = Mathf.Lerp(24f, 46f, t);
        Color violet = new Color(0.65f, 0.4f, 1f, a);

        Dot(px, C, oy, 4.5f + Mathf.Sin(t * 12f) * 0.6f, violet);
        Dot(px, C, oy, 2.2f, new Color(0.95f, 0.9f, 1f, a));

        for (int i = 0; i < 2; i++)
        {
            float wAng = t * 9f + i * Mathf.PI;
            float wx = C + Mathf.Cos(wAng) * 7f;
            float wy = oy + Mathf.Sin(wAng) * 3f;
            Dot(px, wx, wy, 1.4f, new Color(0.8f, 0.65f, 1f, a * 0.8f));
            Dot(px, wx - Mathf.Cos(wAng) * 2f, wy - Mathf.Sin(wAng) * 1f, 0.8f, new Color(0.8f, 0.65f, 1f, a * 0.45f));
        }

        int seed = 47;
        for (int i = 0; i < 6; i++)
        {
            float sx = C + (Rnd01(ref seed) - 0.5f) * 16f;
            float sy = oy - 6f - Rnd01(ref seed) * 12f;
            Sparkle(px, sx, sy, 1, new Color(0.75f, 0.55f, 1f, a * 0.75f));
        }
    }

    // Cổng gương tím mở → lung linh → khép. Clone do CODE spawn, KHÔNG vẽ người.
    private static void DrawMirrorImage(Color[] px, float t)
    {
        float a = Env(t, 0.1f, 0.75f);
        float open = t < 0.4f ? Mathf.Clamp01(t / 0.4f) : t > 0.7f ? Mathf.Clamp01((1f - t) / 0.3f) : 1f;
        float rx = Mathf.Lerp(1.5f, 11f, open);
        float ry = Mathf.Lerp(4f, 21f, open);
        Color rim = new Color(0.95f, 0.45f, 0.95f, a);

        FillEllipse(px, C, C, rx, ry, new Color(0.25f, 0.08f, 0.35f, a * 0.8f));
        EllipseRing(px, C, C, rx, ry, 1.6f, rim);
        EllipseRing(px, C, C, rx + 2.5f, ry + 2.5f, 0.9f, new Color(0.7f, 0.3f, 0.9f, a * 0.5f));

        // vệt shimmer quét ngang mặt gương
        float sweep = Mathf.Repeat(t * 2.2f, 1f);
        float sx0 = C + (sweep - 0.5f) * rx * 1.6f;
        for (float y = C - ry * 0.8f; y <= C + ry * 0.8f; y += 1f)
        {
            float dy = (y - C) / ry;
            float widthAt = rx * Mathf.Sqrt(Mathf.Max(0f, 1f - dy * dy));
            if (Mathf.Abs(sx0 - C) < widthAt)
                Px(px, Mathf.RoundToInt(sx0), Mathf.RoundToInt(y), new Color(1f, 0.85f, 1f, a * 0.7f * open));
        }

        int seed = 59;
        for (int i = 0; i < 6; i++)
        {
            float ang = Rnd01(ref seed) * Mathf.PI * 2f;
            Sparkle(px, C + Mathf.Cos(ang) * (rx + 4f + Rnd01(ref seed) * 4f),
                C + Mathf.Sin(ang) * (ry * 0.7f + Rnd01(ref seed) * 5f), 1, new Color(1f, 0.7f, 1f, a * 0.8f));
        }
    }

    // ─── Drawing primitives (lưới 64×64) ─────────────────────────────────────

    private static Color[] NewCanvas() => new Color[P * P];

    private static void Px(Color[] c, int x, int y, Color col)
    {
        if (x < 0 || x >= P || y < 0 || y >= P || col.a <= 0.004f) return;
        Color e = c[y * P + x];
        float a = col.a + e.a * (1f - col.a);
        if (a < 0.004f) { c[y * P + x] = Color.clear; return; }
        c[y * P + x] = new Color(
            (col.r * col.a + e.r * e.a * (1f - col.a)) / a,
            (col.g * col.a + e.g * e.a * (1f - col.a)) / a,
            (col.b * col.a + e.b * e.a * (1f - col.a)) / a, a);
    }

    private static void Dot(Color[] c, float cx, float cy, float radius, Color col)
    {
        int x0 = Mathf.FloorToInt(cx - radius), x1 = Mathf.CeilToInt(cx + radius);
        int y0 = Mathf.FloorToInt(cy - radius), y1 = Mathf.CeilToInt(cy + radius);
        float r2 = radius * radius;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 > r2) continue;
                float edge = Mathf.Clamp01((r2 - d2) / (radius * 2f + 0.001f));
                Px(c, x, y, new Color(col.r, col.g, col.b, col.a * Mathf.Clamp01(edge * 1.8f)));
            }
    }

    private static void FillRing(Color[] c, float cx, float cy, float r0, float r1, Color col)
    {
        int x0 = Mathf.FloorToInt(cx - r1), x1 = Mathf.CeilToInt(cx + r1);
        int y0 = Mathf.FloorToInt(cy - r1), y1 = Mathf.CeilToInt(cy + r1);
        float r0sq = r0 * r0, r1sq = r1 * r1;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x - cx, dy = y - cy, d2 = dx * dx + dy * dy;
                if (d2 < r0sq || d2 > r1sq) continue;
                float eo = Mathf.Clamp01((r1sq - d2) / (r1 * 2.5f + 0.001f));
                float ei = Mathf.Clamp01((d2 - r0sq) / (r0 * 2.5f + 0.001f));
                Px(c, x, y, new Color(col.r, col.g, col.b, col.a * Mathf.Clamp01(eo * ei * 2f)));
            }
    }

    private static void Ring(Color[] c, float cx, float cy, float radius, float thickness, Color col)
        => FillRing(c, cx, cy, Mathf.Max(0f, radius - thickness * 0.5f), radius + thickness * 0.5f, col);

    private static void RayLine(Color[] c, float ox, float oy, float ang, float d0, float d1, int thick, Color col, float fadeOut)
    {
        float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
        float steps = Mathf.Max(2f, (d1 - d0) / 0.7f);
        for (int s = 0; s <= Mathf.RoundToInt(steps); s++)
        {
            float f = s / steps;
            float d = Mathf.Lerp(d0, d1, f);
            float a = col.a * (1f - f * fadeOut);
            Color cc = new Color(col.r, col.g, col.b, a);
            int x = Mathf.RoundToInt(ox + cos * d), y = Mathf.RoundToInt(oy + sin * d);
            Px(c, x, y, cc);
            if (thick > 0) { Px(c, x + 1, y, cc); Px(c, x, y + 1, cc); }
        }
    }

    private static void Line(Color[] c, float x0, float y0, float x1, float y1, Color col, int halfW)
    {
        int ix0 = Mathf.RoundToInt(x0), iy0 = Mathf.RoundToInt(y0);
        int ix1 = Mathf.RoundToInt(x1), iy1 = Mathf.RoundToInt(y1);
        int dx = Mathf.Abs(ix1 - ix0), dy = Mathf.Abs(iy1 - iy0);
        int sx = ix0 < ix1 ? 1 : -1, sy = iy0 < iy1 ? 1 : -1, err = dx - dy;
        Color edge = new Color(col.r, col.g, col.b, col.a * 0.45f);
        while (true)
        {
            Px(c, ix0, iy0, col);
            for (int w = 1; w <= halfW; w++)
            {
                Px(c, ix0 + w, iy0, edge); Px(c, ix0 - w, iy0, edge);
                Px(c, ix0, iy0 + w, edge); Px(c, ix0, iy0 - w, edge);
            }
            if (ix0 == ix1 && iy0 == iy1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; ix0 += sx; }
            if (e2 < dx) { err += dx; iy0 += sy; }
        }
    }

    private static void Bolt(Color[] c, float x0, float y0, float x1, float y1, int seed, float jitter, Color core, Color glow)
    {
        float len = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
        int steps = Mathf.Max(4, Mathf.RoundToInt(len / 4f));
        float nx = -(y1 - y0) / len, ny = (x1 - x0) / len;
        float px2 = x0, py2 = y0;
        for (int s = 1; s <= steps; s++)
        {
            float f = s / (float)steps;
            float j = s == steps ? 0f : (Rnd01(ref seed) - 0.5f) * 2f * jitter;
            float tx = Mathf.Lerp(x0, x1, f) + nx * j;
            float ty = Mathf.Lerp(y0, y1, f) + ny * j;
            Line(c, px2, py2, tx, ty, core, 0);
            Line(c, px2, py2 + 1f, tx, ty + 1f, glow, 0);
            px2 = tx; py2 = ty;
        }
    }

    private static void ArcBand(Color[] c, float cx, float cy, float r0, float r1, float angCenter, float angWidth, Color col)
    {
        int steps = Mathf.Max(10, Mathf.RoundToInt(angWidth * r1 * 1.6f));
        for (int s = 0; s <= steps; s++)
        {
            float f = s / (float)steps; // 0 = đuôi mờ, 1 = đầu sáng
            float a = angCenter - angWidth * 0.5f + angWidth * f;
            float bright = Mathf.Lerp(0.25f, 1f, f);
            for (float r = r0; r <= r1; r += 0.6f)
            {
                float edge = 1f - Mathf.Abs((r - r0) / Mathf.Max(0.01f, r1 - r0) * 2f - 1f);
                Px(c, Mathf.RoundToInt(cx + Mathf.Cos(a) * r), Mathf.RoundToInt(cy + Mathf.Sin(a) * r),
                    new Color(col.r, col.g, col.b, col.a * bright * Mathf.Clamp01(edge * 1.7f)));
            }
        }
    }

    private static void ArrowGlyph(Color[] c, float x, float y, float ang, float len, Color col)
    {
        float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
        Line(c, x - cos * len, y - sin * len, x, y, col, 0);
        Line(c, x, y, x + Mathf.Cos(ang + 2.6f) * len * 0.45f, y + Mathf.Sin(ang + 2.6f) * len * 0.45f, col, 0);
        Line(c, x, y, x + Mathf.Cos(ang - 2.6f) * len * 0.45f, y + Mathf.Sin(ang - 2.6f) * len * 0.45f, col, 0);
    }

    private static void Sparkle(Color[] c, float x, float y, int size, Color col)
    {
        int ix = Mathf.RoundToInt(x), iy = Mathf.RoundToInt(y);
        Px(c, ix, iy, col);
        for (int s = 1; s <= size; s++)
        {
            Color f = new Color(col.r, col.g, col.b, col.a * (1f - s / (size + 1f)));
            Px(c, ix + s, iy, f); Px(c, ix - s, iy, f);
            Px(c, ix, iy + s, f); Px(c, ix, iy - s, f);
        }
    }

    private static void FillEllipse(Color[] c, float cx, float cy, float rx, float ry, Color col)
    {
        int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx);
        int y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / Mathf.Max(0.01f, rx), dy = (y - cy) / Mathf.Max(0.01f, ry);
                float d = dx * dx + dy * dy;
                if (d > 1f) continue;
                Px(c, x, y, new Color(col.r, col.g, col.b, col.a * Mathf.Clamp01((1f - d) * 3f)));
            }
    }

    private static void EllipseRing(Color[] c, float cx, float cy, float rx, float ry, float thickness, Color col)
    {
        int steps = Mathf.Max(24, Mathf.RoundToInt((rx + ry) * 2.5f));
        for (int s = 0; s <= steps; s++)
        {
            float a = s / (float)steps * Mathf.PI * 2f;
            float x = cx + Mathf.Cos(a) * rx, y = cy + Mathf.Sin(a) * ry;
            Dot(c, x, y, thickness * 0.7f, col);
        }
    }

    private static void Glyph(Color[] c, float cx, float cy, int index, Color col)
    {
        int[][] glyphs =
        {
            new[] { 0b010, 0b101, 0b010 },
            new[] { 0b111, 0b010, 0b111 },
            new[] { 0b101, 0b010, 0b101 },
            new[] { 0b110, 0b011, 0b110 },
            new[] { 0b011, 0b110, 0b011 },
            new[] { 0b111, 0b101, 0b111 },
        };
        int[] g = glyphs[((index % glyphs.Length) + glyphs.Length) % glyphs.Length];
        for (int row = 0; row < 3; row++)
            for (int colI = 0; colI < 3; colI++)
                if ((g[row] & (1 << (2 - colI))) != 0)
                    Px(c, Mathf.RoundToInt(cx) - 1 + colI, Mathf.RoundToInt(cy) - 1 + row, col);
    }

    private static int LcgState;
    private static float Rnd01(ref int seed)
    {
        seed = seed * 1664525 + 1013904223;
        return ((seed >> 8) & 0xFFFF) / 65535f;
    }

    // ─── File output ──────────────────────────────────────────────────────────

    private static void WriteFrame(string assetDir, int index, Color[] px)
    {
        Directory.CreateDirectory(assetDir);
        var tex = new Texture2D(OUT, OUT, TextureFormat.RGBA32, false);
        var big = new Color[OUT * OUT];
        int k = OUT / P;
        for (int y = 0; y < OUT; y++)
        {
            int sy = y / k;
            for (int x = 0; x < OUT; x++)
                big[y * OUT + x] = px[sy * P + x / k];
        }
        tex.SetPixels(big);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(assetDir, $"frame_{index:D2}.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void ApplyImportSettings()
    {
        string[] dirs = { $"{Root}/PerSkill", $"{Root}/Auras" };
        int count = 0;
        foreach (string root in dirs)
        {
            if (!Directory.Exists(root))
                continue;
            foreach (string path in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
            {
                string asset = path.Replace('\\', '/');
                if (AssetImporter.GetAtPath(asset) is not TextureImporter importer)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                count++;
            }
        }
        Debug.Log($"SkillVfxBaker: import settings áp cho {count} sprite (Point, PPU 100, alpha transparency).");
    }
}
