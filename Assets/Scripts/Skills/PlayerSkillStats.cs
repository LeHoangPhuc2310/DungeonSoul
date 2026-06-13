using UnityEngine;

/// <summary>Runtime combat stats derived from stacked skills on the player.</summary>
public class PlayerSkillStats : MonoBehaviour
{
    public float CritChance { get; private set; }
    public float CritMultiplier { get; private set; } = 2f;
    public int PierceCount { get; private set; }
    public float PierceDamageFalloff { get; private set; } = 0.2f;
    public float LifeStealPercent { get; private set; }
    public float FireDotDamage { get; private set; }
    public float FireDotDuration { get; private set; }
    public float ExplosionOnKillRadius { get; private set; }
    public float ExplosionOnKillDamageRatio { get; private set; }
    public float SlowAuraRadius { get; private set; }
    public float SlowAuraStrength { get; private set; }
    public float CoinDropBonus { get; private set; } = 1f;
    public float PassiveBurnChance { get; private set; }
    public float ExplosionOnHitRadius { get; private set; }
    public float ExplosionOnHitDamageRatio { get; private set; }
    public int ChainJumpCount { get; private set; }
    public float ChainDamageRatio { get; private set; }
    public float PoisonCloudRadius { get; private set; }
    public float PoisonCloudDps { get; private set; }
    public bool BoomerangEnabled { get; private set; }
    public bool DeathMarkEnabled { get; private set; }

    public void Recalculate(PlayerSkillHandler handler)
    {
        CritChance = 0f;
        CritMultiplier = 2f;
        PierceCount = 0;
        LifeStealPercent = 0f;
        FireDotDamage = 0f;
        FireDotDuration = 0f;
        ExplosionOnKillRadius = 0f;
        ExplosionOnKillDamageRatio = 0f;
        SlowAuraRadius = 0f;
        SlowAuraStrength = 0f;
        CoinDropBonus = 1f;
        PassiveBurnChance = 0f;
        ExplosionOnHitRadius = 0f;
        ExplosionOnHitDamageRatio = 0f;
        ChainJumpCount = 0;
        ChainDamageRatio = 0f;
        PoisonCloudRadius = 0f;
        PoisonCloudDps = 0f;
        BoomerangEnabled = false;
        DeathMarkEnabled = false;

        if (handler == null)
            return;

        int pierce = handler.GetStack(SkillType.PiercingArrow);
        if (pierce > 0)
        {
            PierceCount = pierce == 1 ? 2 : pierce == 2 ? 3 : 5;
            PierceDamageFalloff = 0.2f;
        }

        int crit = handler.GetStack(SkillType.CriticalHit);
        if (crit > 0)
        {
            CritChance = crit == 1 ? 0.2f : crit == 2 ? 0.35f : 0.5f;
        }

        int lifeSteal = handler.GetStack(SkillType.LifeSteal);
        if (lifeSteal > 0)
        {
            LifeStealPercent = lifeSteal == 1 ? 0.1f : lifeSteal == 2 ? 0.18f : 0.25f;
        }

        int fire = handler.GetStack(SkillType.FireArrow);
        if (fire > 0)
        {
            FireDotDamage = 3f;
            FireDotDuration = fire == 1 ? 2f : fire == 2 ? 3f : 4f;
        }

        int explosion = handler.GetStack(SkillType.Explosion);
        if (explosion > 0)
        {
            ExplosionOnKillRadius = explosion == 1 ? 2.5f : 3.5f;
            ExplosionOnKillDamageRatio = explosion == 1 ? 0.6f : 0.7f;
        }

        int ice = handler.GetStack(SkillType.IceAura);
        if (ice > 0)
        {
            SlowAuraRadius = 8f;
            SlowAuraStrength = ice == 1 ? 0.4f : 0.6f;
        }

        int explosiveRounds = handler.GetStack(SkillType.ExplosiveRounds);
        if (explosiveRounds > 0)
        {
            ExplosionOnHitRadius = explosiveRounds == 1 ? 1.5f : explosiveRounds == 2 ? 1.8f : 2.1f;
            ExplosionOnHitDamageRatio = explosiveRounds == 1 ? 0.5f : explosiveRounds == 2 ? 0.6f : 0.7f;
        }

        int chain = handler.GetStack(SkillType.LightningChain);
        if (chain > 0)
        {
            ChainJumpCount = chain == 1 ? 2 : chain == 2 ? 3 : 4;
            ChainDamageRatio = 0.6f;
        }

        int poison = handler.GetStack(SkillType.PoisonCloud);
        if (poison > 0)
        {
            PoisonCloudRadius = poison == 1 ? 1.8f : poison == 2 ? 2.2f : 2.6f;
            PoisonCloudDps = poison == 1 ? 4f : poison == 2 ? 6f : 8f;
        }

        BoomerangEnabled = handler.HasSkill(SkillType.Boomerang);
        DeathMarkEnabled = handler.HasSkill(SkillType.DeathMark);

        int coinMagnet = handler.GetStack(SkillType.CoinMagnet);
        if (coinMagnet > 0)
            CoinDropBonus = 1f + (coinMagnet == 1 ? 0.1f : coinMagnet == 2 ? 0.15f : 0.2f);

        // Cộng thêm từ passive item
        PassiveItemManager passives = PassiveItemManager.Instance;
        if (passives != null)
        {
            CritChance = Mathf.Clamp01(CritChance + passives.CritChanceBonus);
            LifeStealPercent = Mathf.Clamp01(LifeStealPercent + passives.LifeStealBonus);
            PassiveBurnChance = passives.BurnChanceBonus;
        }
    }
}
