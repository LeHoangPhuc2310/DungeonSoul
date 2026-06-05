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
        {
            isPlayer = CompareTag("Player");
            if (!isPlayer)
                EnemyOverheadHPBar.Ensure(gameObject);
        }
    }

    private void Update()
    {
        if (currentHP > 0f && hpRegenPerSecond > 0f)
            currentHP = Mathf.Min(maxHP, currentHP + hpRegenPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount) => TakeDamage(amount, false);

    public void TakeDamage(float amount, bool isCrit)
    {
        if (amount <= 0f || currentHP <= 0f || invulnerable)
            return;

        float afterFlat = Mathf.Max(0f, amount - flatDamageReduction - skillFlatReduction);
        float finalDamage = afterFlat * (1f - Mathf.Clamp01(damageReductionPercent + skillPercentReduction));
        finalDamage *= GetBossDamageMultiplier();
        if (finalDamage <= 0f)
            return;

        currentHP = Mathf.Max(0f, currentHP - finalDamage);
        if (isPlayer || CompareTag("Player"))
            HUDManager.Resolve()?.UpdateHp();
        else
        {
            HUDManager.SpawnDamageNumber(transform.position, finalDamage, isCrit);
            GetComponent<EnemySpriteAnimator>()?.PlayHurt();
            GetComponent<SimpleSpriteAnimator>()?.PlayHurt();
        }

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
        if (IsEnemyUnit())
        {
            float killExp = Mathf.Max(2f, maxHP * 0.05f);
            ExpSystem.Instance?.AddExp(killExp);
            HUDManager.SpawnExpNumber(transform.position, killExp);
            SpawnExpGem(maxHP);

            EnemyReward reward = GetComponent<EnemyReward>();
            int score = reward != null ? reward.ScoreReward : 10;
            int coins = reward != null ? reward.RollCoins() : Random.Range(3, 6);
            coins = MetaRunModifiers.ScaleCoins(coins);
            PlayerSkillStats stats = PlayerSkillHandler.Instance != null
                ? PlayerSkillHandler.Instance.GetComponent<PlayerSkillStats>()
                : null;
            if (stats != null)
                coins = Mathf.Max(0, Mathf.RoundToInt(coins * stats.CoinDropBonus));

            HUDManager hud = HUDManager.Resolve();
            if (hud != null)
                hud.RegisterEnemyKilled(score, coins);

            LootDrop loot = GetComponent<LootDrop>();
            if (loot != null)
                loot.TryDrop(transform.position);

            SkillBehaviors behaviors = FindPlayerSkillBehaviors();
            if (behaviors != null)
                behaviors.OnEnemyKilled(transform.position, maxHP);

            BossController boss = GetComponent<BossController>();
            if (boss != null)
                boss.OnBossDefeated();

            // VFX nổ chỉ cho BOSS (nổ xanh to). Quái thường không nổ để tránh rối khi chết hàng loạt.
            if (boss != null)
                EffectLibrary.Play(EffectKind.BlueExplosion, transform.position, 3.2f, Color.white, 22f, 25);

            EnemyAliveTracker.Add(-1);

            if (RoomClearBridge.Instance != null)
                RoomClearBridge.Instance.OnEnemyKilled();

            EnemySpriteAnimator kenneyAnim = GetComponent<EnemySpriteAnimator>();
            if (kenneyAnim != null && kenneyAnim.TryPlayDeath(out float kenneyDelay))
            {
                Destroy(gameObject, kenneyDelay);
                return;
            }

            SimpleSpriteAnimator knightAnim = GetComponent<SimpleSpriteAnimator>();
            if (knightAnim != null && knightAnim.TryPlayDeath(out float knightDelay))
            {
                Destroy(gameObject, knightDelay);
                return;
            }
        }
        else if (isPlayer || CompareTag("Player"))
        {
            if (RunManager.Instance != null)
                RunManager.Instance.EndRun(false);
            else
                HUDManager.Resolve()?.ShowGameOver();

            Destroy(gameObject, 0.05f);
            return;
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

    private float GetBossDamageMultiplier()
    {
        BossController boss = GetComponent<BossController>();
        if (boss == null)
            return 1f;

        bool magic = HeroRunStats.Instance != null && HeroRunStats.Instance.SelectedHero == HeroType.Mage;
        return boss.GetProjectileDamageMultiplier(magic);
    }

    private bool IsEnemyUnit()
    {
        if (CompareTag("Enemy"))
            return true;

        return GetComponent<EnemyAI>() != null
            || GetComponent<EnemyReward>() != null
            || GetComponent<BossController>() != null;
    }

    private static SkillBehaviors FindPlayerSkillBehaviors()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<SkillBehaviors>() : null;
    }

    private void SpawnExpGem(float enemyMaxHp)
    {
        bool rare = Random.value < 0.18f;
        float gemExp = rare
            ? Mathf.Max(14f, enemyMaxHp * 0.32f)
            : Mathf.Max(8f, enemyMaxHp * 0.14f);

        if (expGemPrefab != null)
        {
            GameObject gemObject = Instantiate(expGemPrefab, transform.position, Quaternion.identity);
            ExpGem gem = gemObject.GetComponent<ExpGem>();
            if (gem != null)
                gem.Initialize(rare ? ExpGem.GemRarity.Rare : ExpGem.GemRarity.Common, gemExp);
            return;
        }

        GameObject runtimeGem = new GameObject("ExpGem");
        runtimeGem.transform.position = transform.position;
        ExpGem runtimeExpGem = runtimeGem.AddComponent<ExpGem>();
        runtimeExpGem.Initialize(rare ? ExpGem.GemRarity.Rare : ExpGem.GemRarity.Common, gemExp);
    }
}
