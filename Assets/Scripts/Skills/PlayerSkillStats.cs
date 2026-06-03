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

        int coinMagnet = handler.GetStack(SkillType.CoinMagnet);
        if (coinMagnet > 0)
            CoinDropBonus = 1f + (coinMagnet == 1 ? 0.1f : coinMagnet == 2 ? 0.15f : 0.2f);
    }
}
