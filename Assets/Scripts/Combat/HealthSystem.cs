using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP = 100f;
    [SerializeField] private float flatDamageReduction;
    [SerializeField] private float damageReductionPercent;
    [SerializeField] private float hpRegenPerSecond;
    [SerializeField] private GameObject expGemPrefab;
    [SerializeField] private bool isPlayer;

    private bool invulnerable;
    private float skillFlatReduction;
    private float skillPercentReduction;
    private float baseRegen;
    private float skillRegen;

    public float MaxHP
    {
        get => maxHP;
        set
        {
            maxHP = Mathf.Max(1f, value);
            currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        }
    }

    public float CurrentHP
    {
        get => currentHP;
        set => currentHP = Mathf.Clamp(value, 0f, maxHP);
    }

    private void Awake()
    {
        maxHP = Mathf.Max(1f, maxHP);
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        baseRegen = hpRegenPerSecond;
        if (!isPlayer)
            isPlayer = CompareTag("Player");
    }

    private void Update()
    {
        if (currentHP > 0f && hpRegenPerSecond > 0f)
            currentHP = Mathf.Min(maxHP, currentHP + hpRegenPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHP <= 0f || invulnerable)
            return;

        float afterFlat = Mathf.Max(0f, amount - flatDamageReduction - skillFlatReduction);
        float finalDamage = afterFlat * (1f - Mathf.Clamp01(damageReductionPercent + skillPercentReduction));
        if (finalDamage <= 0f)
            return;

        currentHP = Mathf.Max(0f, currentHP - finalDamage);
        HUDManager.SpawnDamageNumber(transform.position, finalDamage);
        if (currentHP <= 0f)
Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHP <= 0f)
            return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    public void SetInvulnerable(bool value) => invulnerable = value;

    public void ResetSkillModifiers()
    {
        skillFlatReduction = 0f;
        skillPercentReduction = 0f;
        skillRegen = 0f;
        hpRegenPerSecond = baseRegen;
    }

    public void AddRegen(float amountPerSecond)
    {
        skillRegen += Mathf.Max(0f, amountPerSecond);
        hpRegenPerSecond = baseRegen + skillRegen;
    }

    private void Die()
    {
        if (CompareTag("Enemy"))
        {
            ExpSystem.Instance?.AddExp(20f);
            SpawnExpGem();

            EnemyReward reward = GetComponent<EnemyReward>();
            int score = reward != null ? reward.ScoreReward : 10;
            int coins = reward != null ? reward.RollCoins() : Random.Range(3, 6);
            PlayerSkillStats stats = PlayerSkillHandler.Instance != null
                ? PlayerSkillHandler.Instance.GetComponent<PlayerSkillStats>()
                : null;
            if (stats != null)
                coins = Mathf.RoundToInt(coins * stats.CoinDropBonus);

            if (HUDManager.Instance != null)
                HUDManager.Instance.RegisterEnemyKilled(score, coins);

            LootDrop loot = GetComponent<LootDrop>();
            if (loot != null)
                loot.TryDrop(transform.position);

            SkillBehaviors behaviors = FindPlayerSkillBehaviors();
            if (behaviors != null)
                behaviors.OnEnemyKilled(transform.position, maxHP);

            if (EnemyWaveManager.Instance != null)
                EnemyWaveManager.Instance.NotifyEnemyDied(transform.position);

            BossController boss = GetComponent<BossController>();
            if (boss != null)
                boss.OnBossDefeated();
        }
        else if (isPlayer || CompareTag("Player"))
        {
            if (RunManager.Instance != null)
                RunManager.Instance.EndRun(false);
        }

        Destroy(gameObject, 0f);
    }

    public void AddFlatDamageReduction(float amount)
    {
        flatDamageReduction = Mathf.Max(0f, flatDamageReduction + amount);
    }

    public void AddDamageReductionPercent(float amount)
    {
        skillPercentReduction = Mathf.Clamp01(skillPercentReduction + amount);
    }

    private static SkillBehaviors FindPlayerSkillBehaviors()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<SkillBehaviors>() : null;
    }

    private void SpawnExpGem()
    {
        if (expGemPrefab != null)
        {
            GameObject gemObject = Instantiate(expGemPrefab, transform.position, Quaternion.identity);
            ExpGem gem = gemObject.GetComponent<ExpGem>();
            if (gem != null)
                gem.Initialize(Random.value < 0.2f ? ExpGem.GemRarity.Rare : ExpGem.GemRarity.Common);
            return;
        }

        GameObject runtimeGem = new GameObject("ExpGem");
        runtimeGem.transform.position = transform.position;
        SpriteRenderer sr = runtimeGem.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 12;
        ExpGem runtimeExpGem = runtimeGem.AddComponent<ExpGem>();
        runtimeExpGem.Initialize(Random.value < 0.2f ? ExpGem.GemRarity.Rare : ExpGem.GemRarity.Common);
    }
}
