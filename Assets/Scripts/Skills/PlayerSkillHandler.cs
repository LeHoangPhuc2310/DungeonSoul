using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillHandler : MonoBehaviour
{
    public static PlayerSkillHandler Instance { get; private set; }

    public List<SkillData> activeSkills = new List<SkillData>();

    private PlayerController playerController;
    private HealthSystem healthSystem;
    private AutoAttack autoAttack;
    private WeaponManager weaponManager;
    private PlayerSkillStats skillStats;
    private SkillBehaviors skillBehaviors;
    private float baseMoveSpeed = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        skillStats = GetComponent<PlayerSkillStats>();
        if (skillStats == null)
            skillStats = gameObject.AddComponent<PlayerSkillStats>();
        skillBehaviors = GetComponent<SkillBehaviors>();
        if (skillBehaviors == null)
            skillBehaviors = gameObject.AddComponent<SkillBehaviors>();
        CacheComponents();
    }

    public int GetStack(SkillType type)
    {
        int count = 0;
        for (int i = 0; i < activeSkills.Count; i++)
        {
            if (activeSkills[i] != null && activeSkills[i].skillType == type)
                count++;
        }
        return count;
    }

    public bool HasSkill(SkillType type) => GetStack(type) > 0;

    public void RefreshStats() => RecalculateAllStats();

    public void ApplySkill(SkillData skill)
    {
        if (skill == null)
            return;

        if (IsLegendary(skill.skillType) && HasSkill(skill.skillType))
            return;

        CacheComponents();
        activeSkills.Add(skill);
        RecalculateAllStats();
        PlaySkillAcquireVfx(skill.skillType);

        if (SkillsPanelUI.Instance != null)
            SkillsPanelUI.Instance.AddOrUpdateSkill(skill);

        GlobalStats.Refresh();
        HUDManager.Resolve()?.RefreshVsLoadoutPanel();
    }

    private void PlaySkillAcquireVfx(SkillType type)
    {
        Transform t = transform;
        if (t == null)
            return;

        SkillVfxLibrary.PlayForSkill(type, t.position, 1.25f);
        AudioManager.PlayUiTap();
    }

    private void RecalculateAllStats()
    {
        if (playerController != null)
        {
            if (baseMoveSpeed < 0f)
                baseMoveSpeed = playerController.MoveSpeed;
            playerController.MoveSpeed = baseMoveSpeed;
        }

        if (autoAttack != null)
        {
            autoAttack.FireInterval = autoAttack.BaseFireInterval;
            autoAttack.ProjectileDamage = autoAttack.BaseProjectileDamage;
            autoAttack.ProjectileCount = 1;
            autoAttack.CritChance = 0f;
            autoAttack.MultiTargetCount = 1;
        }

        if (healthSystem != null)
            healthSystem.ResetSkillModifiers();

        ApplyStackedSkill(SkillType.SpeedBoost, (stack, value) =>
        {
            if (playerController == null)
                return;
            float bonus = stack == 1 ? 1f : stack == 2 ? 1.5f : 2f;
            playerController.MoveSpeed = baseMoveSpeed + bonus;
        });

        ApplyStackedSkill(SkillType.IronBody, (stack, value) =>
        {
            if (healthSystem == null)
                return;
            float bonus = stack == 1 ? 20f : stack == 2 ? 40f : 60f;
            healthSystem.MaxHP += bonus;
            healthSystem.CurrentHP += bonus;
        });

        ApplyStackedSkill(SkillType.QuickReload, (stack, value) =>
        {
            if (autoAttack == null)
                return;
            float reduction = stack == 1 ? 0.2f : stack == 2 ? 0.35f : 0.5f;
            autoAttack.FireInterval = Mathf.Max(0.05f, autoAttack.BaseFireInterval * (1f - reduction));
        });

        ApplyStackedSkill(SkillType.DoubleShot, (stack, value) =>
        {
            if (autoAttack == null)
                return;
            autoAttack.ProjectileCount = stack == 1 ? 2 : stack == 2 ? 3 : 4;
        });

        ApplyStackedSkill(SkillType.CoinMagnet, (stack, value) =>
        {
            MagnetRadiusUpgrade magnet = GetComponent<MagnetRadiusUpgrade>();
            if (magnet == null)
                magnet = gameObject.AddComponent<MagnetRadiusUpgrade>();
            magnet.SetBonus(stack == 1 ? 3f : stack == 2 ? 5f : 7f);
        });

        ApplyStackedSkill(SkillType.ToughSkin, (stack, value) =>
        {
            if (healthSystem == null)
                return;
            float pct = stack == 1 ? 0.08f : stack == 2 ? 0.15f : 0.2f;
            healthSystem.AddDamageReductionPercent(pct);
        });

        ApplyStackedSkill(SkillType.SteadyAim, (stack, value) =>
        {
            float bonus = stack == 1 ? 5f : stack == 2 ? 10f : 15f;
            if (autoAttack != null)
                autoAttack.ProjectileDamage += bonus;
            if (weaponManager != null)
                weaponManager.DamageMultiplier += bonus * 0.02f;
        });

        ApplyStackedSkill(SkillType.MultiTarget, (stack, value) =>
        {
            if (autoAttack != null)
                autoAttack.MultiTargetCount = stack == 1 ? 2 : stack == 2 ? 3 : 4;
        });

        ApplyStackedSkill(SkillType.QuadShot, (stack, value) =>
        {
            if (autoAttack != null)
                autoAttack.ProjectileCount += stack == 1 ? 2 : 4;
        });

        ApplyStackedSkill(SkillType.Vampire, (stack, value) =>
        {
            if (healthSystem != null)
                healthSystem.AddRegen(stack >= 2 ? 1.5f : 1f);
        });

        ApplyStackedSkill(SkillType.FireArrow, (stack, value) =>
        {
            if (autoAttack != null)
                autoAttack.ProjectileDamage += stack * Mathf.Max(1f, value);
        });

        ApplyStackedSkill(SkillType.PiercingArrow, (stack, value) =>
        {
            if (autoAttack != null)
                autoAttack.ProjectileDamage += stack * 2f;
        });

        ApplyStackedSkill(SkillType.TwinArrows, (stack, value) =>
        {
            if (autoAttack != null)
                autoAttack.ProjectileDamage += stack * 3f;
        });

        ApplyStackedSkill(SkillType.ExplosiveRounds, (stack, value) =>
        {
            if (autoAttack != null)
                autoAttack.ProjectileDamage += stack * 4f;
        });

        ApplyLegendaryDamageBonuses();

        if (skillStats != null)
            skillStats.Recalculate(this);
        if (autoAttack != null && skillStats != null)
            autoAttack.CritChance = skillStats.CritChance;

        PassiveItemManager.Instance?.ApplyAggregatedToPlayer();

        PlayerStatsUI.NotifyChanged();
    }

    private void ApplyLegendaryDamageBonuses()
    {
        if (autoAttack == null)
            return;

        int deathMark = GetStack(SkillType.DeathMark);
        if (deathMark > 0)
            autoAttack.ProjectileDamage += deathMark * 5f;

        int dragon = GetStack(SkillType.DragonStrike);
        if (dragon > 0)
            autoAttack.ProjectileDamage *= 1f + dragon * 0.1f;

        int soul = GetStack(SkillType.SoulHarvest);
        if (soul > 0)
            autoAttack.ProjectileDamage += soul * 4f;
    }

    private static bool IsLegendary(SkillType type)
    {
        return type == SkillType.DeathMark || type == SkillType.TimeFreeze ||
               type == SkillType.DragonStrike || type == SkillType.SoulHarvest ||
               type == SkillType.MirrorImage;
    }

    private void ApplyStackedSkill(SkillType type, System.Action<int, float> apply)
    {
        int stack = GetStack(type);
        if (stack <= 0)
            return;
        SkillData data = activeSkills.Find(s => s != null && s.skillType == type);
        apply(Mathf.Min(stack, 3), data != null ? data.value : 0f);
    }

    private void CacheComponents()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();
        if (autoAttack == null)
            autoAttack = GetComponent<AutoAttack>();
        if (weaponManager == null)
            weaponManager = GetComponent<WeaponManager>();
    }
}
