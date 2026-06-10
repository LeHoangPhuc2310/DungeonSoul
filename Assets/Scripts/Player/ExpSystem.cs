using System;
using UnityEngine;

public class ExpSystem : MonoBehaviour
{
    public static ExpSystem Instance { get; private set; }

    [SerializeField] private float currentExp = 0f;
    [SerializeField] private int currentLevel = 1;

    public float CurrentExp => currentExp;
    public int CurrentLevel => currentLevel;
    public float ExpToNextLevel => CalculateExpToNextLevel(currentLevel);

    public event Action<float, float> OnExpChanged;
    public event Action<int> OnLevelUpEvent;

    private HealthSystem playerHealth;
    private AutoAttack playerAttack;

    private const float LevelUpMaxHpIncrease = 10f;
    private const float LevelUpDamageIncrease = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentLevel = Mathf.Max(1, currentLevel);
        currentExp = Mathf.Max(0f, currentExp);
        CachePlayerStatsComponents();
        RaiseExpChanged();
    }

    public void AddExp(float amount)
    {
        if (amount <= 0f)
            return;

        if (PassiveItemManager.Instance != null)
            amount *= PassiveItemManager.Instance.ExpGainMultiplier;

        currentExp += amount;
        ProcessLevelUps();
        RaiseExpChanged();
    }

    private void ProcessLevelUps()
    {
        while (currentExp >= ExpToNextLevel)
        {
            currentExp -= ExpToNextLevel;
            ApplyLevelUp();
        }
    }

    private void ApplyLevelUp()
    {
        currentLevel++;

        CachePlayerStatsComponents();

        float hpGain = LevelUpMaxHpIncrease;
        float dmgGain = LevelUpDamageIncrease;
        if (HeroRunStats.Instance != null)
        {
            switch (HeroRunStats.Instance.SelectedHero)
            {
                case HeroType.Ranger:
                    hpGain = 8f;
                    dmgGain = 3f;
                    break;
                case HeroType.Mage:
                    hpGain = 6f;
                    dmgGain = 4f;
                    break;
            }
        }

        if (playerHealth != null)
        {
            playerHealth.MaxHP += hpGain;
            playerHealth.CurrentHP += hpGain;
        }

        if (playerAttack != null)
            playerAttack.AddPermanentDamage(dmgGain);

        PlayerSkillHandler skills = PlayerSkillHandler.Instance;
        if (skills == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                skills = playerObj.GetComponent<PlayerSkillHandler>();
        }
        if (skills != null)
            skills.RefreshStats();

        LevelUpEffect levelUpEffect = LevelUpEffect.Instance;
        if (levelUpEffect != null)
        {
            Transform target = playerHealth != null ? playerHealth.transform : transform;
            levelUpEffect.Play(target, currentLevel, hpGain, dmgGain);
        }

        AudioManager.PlayLevelUp();
        GameJuice.Shake(0.18f, 0.22f, 16f);
        AchievementManager.Instance?.OnPlayerLevel(currentLevel);
        Debug.Log("Level Up! Now level " + currentLevel);
        OnLevelUpEvent?.Invoke(currentLevel);

        SkillSelectionUI skillUi = SkillSelectionUI.GetOrFind();
        if (skillUi != null && !skillUi.IsPanelOpen)
            skillUi.Show();
    }

    private void CachePlayerStatsComponents()
    {
        if (playerHealth != null && playerAttack != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        if (playerHealth == null)
            playerHealth = player.GetComponent<HealthSystem>();

        if (playerAttack == null)
            playerAttack = player.GetComponent<AutoAttack>();
    }

    public static float CalculateExpToNextLevel(int level)
    {
        int lv = Mathf.Max(1, level);
        return 260f * Mathf.Pow(lv, 1.62f);
    }

    private void RaiseExpChanged()
    {
        OnExpChanged?.Invoke(currentExp, ExpToNextLevel);
        HUDManager.Resolve()?.ForceRefreshFromSystems(false);
    }
}
