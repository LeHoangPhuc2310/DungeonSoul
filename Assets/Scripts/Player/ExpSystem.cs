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

        currentExp += amount;
        RaiseExpChanged();
        ExpBarUI.Instance?.UpdateBar();

        if (currentExp >= ExpToNextLevel)
            OnLevelUp();
    }

    private void OnLevelUp()
    {
        currentLevel++;
        currentExp = 0f;

        CachePlayerStatsComponents();

        if (playerHealth != null)
        {
            playerHealth.MaxHP += LevelUpMaxHpIncrease;
            playerHealth.CurrentHP += LevelUpMaxHpIncrease;
        }

        if (playerAttack != null)
            playerAttack.ProjectileDamage += LevelUpDamageIncrease;

        LevelUpEffect levelUpEffect = LevelUpEffect.Instance;
        if (levelUpEffect != null)
        {
            Transform target = playerHealth != null ? playerHealth.transform : transform;
            levelUpEffect.Play(target, currentLevel, LevelUpMaxHpIncrease, LevelUpDamageIncrease);
        }

        Debug.Log("Level Up! Now level " + currentLevel);
        OnLevelUpEvent?.Invoke(currentLevel);
        RaiseExpChanged();
        ExpBarUI.Instance?.UpdateBar();
        // Level up = auto stats only. Skill pick comes from chest (see kich ban).
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

    private static float CalculateExpToNextLevel(int level)
    {
        return 100f * Mathf.Pow(Mathf.Max(1, level), 1.5f);
    }

    private void RaiseExpChanged()
    {
        OnExpChanged?.Invoke(currentExp, ExpToNextLevel);
        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdateExp(currentExp, ExpToNextLevel, currentLevel);
    }
}
