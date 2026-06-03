using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillHandler : MonoBehaviour
{
    public static PlayerSkillHandler Instance { get; private set; }

    public List<SkillData> activeSkills = new List<SkillData>();

    private PlayerController playerController;
    private HealthSystem healthSystem;
    private AutoAttack autoAttack;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheComponents();
    }

    public void ApplySkill(SkillData skill)
    {
        if (skill == null)
            return;

        CacheComponents();

        switch (skill.skillType)
        {
            case SkillType.SpeedBoost:
                if (playerController != null)
                    playerController.MoveSpeed += skill.value;
                break;

            case SkillType.IronBody:
                if (healthSystem != null)
                {
                    healthSystem.MaxHP += skill.value;
                    healthSystem.CurrentHP += skill.value;
                }
                break;

            case SkillType.QuickReload:
                if (autoAttack != null)
                    autoAttack.FireInterval -= skill.value;
                break;

            case SkillType.DoubleShot:
                if (autoAttack != null)
                    autoAttack.ProjectileCount = 2;
                break;

            case SkillType.CriticalHit:
                if (autoAttack != null)
                    autoAttack.CritChance += skill.value;
                break;
        }

        activeSkills.Add(skill);
    }

    private void CacheComponents()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();
        if (autoAttack == null)
            autoAttack = GetComponent<AutoAttack>();
    }
}
