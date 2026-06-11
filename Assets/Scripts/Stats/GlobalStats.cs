using System;
using UnityEngine;

/// <summary>Chỉ số toàn cục kiểu Vampire Survivors — đọc từ passive, vũ khí, player.</summary>
public struct VsStatsSnapshot
{
    public float MaxHealth;
    public float Recovery;
    public float Armor;
    public float MoveSpeedPercent;
    public float MightPercent;
    public float AreaPercent;
    public float SpeedPercent;
    public float DurationPercent;
    public int Amount;
    public float CooldownPercent;
    public float Luck;
    public float GrowthPercent;
    public float GreedPercent;
    public float Magnet;
}

public static class GlobalStats
{
    public static VsStatsSnapshot Current { get; private set; }
    public static event Action OnChanged;

    public static float Luck => Current.Luck;

    public static void Refresh()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        HealthSystem hp = player != null ? player.GetComponent<HealthSystem>() : null;
        PlayerController move = player != null ? player.GetComponent<PlayerController>() : null;
        AutoAttack atk = player != null ? player.GetComponent<AutoAttack>() : null;
        PassiveItemManager passives = PassiveItemManager.Instance;
        WeaponManager weapons = WeaponManager.Instance;
        PlayerSkillHandler skills = PlayerSkillHandler.Instance;
        PlayerSkillStats skillStats = player != null ? player.GetComponent<PlayerSkillStats>() : null;

        if (skillStats != null && skills != null)
            skillStats.Recalculate(skills);

        VsStatsSnapshot snap = default;

        if (hp != null)
            snap.MaxHealth = hp.MaxHP;

        if (passives != null)
        {
            snap.MightPercent = (passives.DamageMultiplierBonus - 1f) * 100f;
            snap.CooldownPercent = (1f - passives.CooldownMultiplierBonus) * 100f;
            snap.GrowthPercent = (passives.ExpGainMultiplier - 1f) * 100f;
            snap.AreaPercent = passives.AreaSizeBonus * 100f;
            snap.Luck = passives.LuckBonus;
            snap.Armor = passives.DefensePercent * 100f;
            snap.MoveSpeedPercent = passives.MoveSpeedPercent * 100f;
            snap.Amount = passives.ExtraProjectileCount;
        }

        if (weapons != null)
        {
            snap.AreaPercent += (weapons.AreaMultiplier - 1f) * 100f;
            snap.Amount = Mathf.Max(snap.Amount, weapons.ExtraProjectileCount);
        }

        if (atk != null)
        {
            snap.Amount = Mathf.Max(snap.Amount, atk.ProjectileCount - 1);
            if (atk.FireInterval > 0.001f)
            {
                float shots = 1f / atk.FireInterval;
                snap.SpeedPercent = (shots - 1f) * 10f;
            }
        }

        if (skillStats != null)
            snap.GreedPercent = (skillStats.CoinDropBonus - 1f) * 100f;

        MagnetRadiusUpgrade magnet = player != null ? player.GetComponent<MagnetRadiusUpgrade>() : null;
        if (magnet != null)
            snap.Magnet = magnet.BonusRadius;

        if (passives != null && snap.Magnet <= 0f)
            snap.Magnet = passives.MagnetRadiusBonus;

        Current = snap;
        OnChanged?.Invoke();
    }

    /// <summary>Số thẻ level-up (3 hoặc 4) — Luck cao → cơ hội 4 thẻ.</summary>
    public static int RollLevelUpChoiceCount()
    {
        if (!SurvivalRunManager.IsSurvivalMode())
            return 3;

        Refresh();
        float luck = Mathf.Clamp01(Current.Luck);
        if (luck >= 1f)
            return 4;
        if (luck <= 0f)
            return 3;

        return UnityEngine.Random.value < luck ? 4 : 3;
    }
}
