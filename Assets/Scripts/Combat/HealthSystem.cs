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
    private float passivePercentReduction;
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

    public void TakeDamage(float amount) => TakeDamage(amount, false);

    public void TakeDamage(float amount, bool isCrit)
    {
        if (amount <= 0f || currentHP <= 0f || invulnerable)
            return;

        float afterFlat = Mathf.Max(0f, amount - flatDamageReduction - skillFlatReduction);
        float finalDamage = afterFlat * (1f - Mathf.Clamp01(damageReductionPercent + skillPercentReduction + passivePercentReduction));
        finalDamage *= GetBossDamageMultiplier();
        if (finalDamage <= 0f)
            return;

        currentHP = Mathf.Max(0f, currentHP - finalDamage);
        if (isPlayer || CompareTag("Player"))
        {
            HUDManager.Resolve()?.UpdateHp();
            // Player ăn đòn = phản hồi mạnh để người chơi "cảm" được nguy hiểm.
            GameJuice.Shake(0.22f, 0.18f);
            HitFeedback.Play(gameObject);
            AudioManager.PlayPlayerHurt();
        }
        else
        {
            HUDManager.SpawnDamageNumber(transform.position, finalDamage, isCrit);
            GetComponent<EnemySpriteAnimator>()?.PlayHurt();
            GetComponent<SimpleSpriteAnimator>()?.PlayHurt();

            // Juice khi đánh trúng quái: nháy trắng + freeze-frame cực ngắn (đậm hơn nếu crit).
            HitFeedback.Play(gameObject);
            // Chỉ crit mới hit-stop — AOE đánh nhiều quái không được reset freeze liên tục.
            if (currentHP > 0f && isCrit)
            {
                GameJuice.HitStop(0.04f);
                GameJuice.Shake(0.12f, 0.1f);
            }
        }

        if (currentHP <= 0f)
            Die();
    }

    /// <summary>Reset HP khi lấy từ object pool.</summary>
    public void ResetForPool()
    {
        currentHP = maxHP;
        invulnerable = false;
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

    /// <summary>Giảm sát thương nhận từ passive (không reset khi skill recalculate).</summary>
    public void SetPassiveDamageReduction(float percent)
    {
        passivePercentReduction = Mathf.Clamp01(percent);
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

            BossController boss = GetComponent<BossController>();
            if (boss != null)
                boss.OnBossDefeated(); // VFX nổ + hit-stop + shake nằm trong BossController.

            EnemyAliveTracker.Add(-1);

            if (RoomClearBridge.Instance != null)
                RoomClearBridge.Instance.OnEnemyKilled();

            // Điểm cắm cho mọi phản ứng "khi quái chết" (skill on-kill, quest, combo, analytics...).
            // Thêm chức năng mới = subscribe EventBus.OnEnemyKilled, KHÔNG sửa file này.
            EventBus.InvokeEnemyKilled(new EnemyKilledInfo(gameObject, transform.position, maxHP, boss != null));

            float deathDelay = 0f;
            bool hasDeathAnim = false;

            EnemySpriteAnimator kenneyAnim = GetComponent<EnemySpriteAnimator>();
            if (kenneyAnim != null && kenneyAnim.TryPlayDeath(out float kenneyDelay))
            {
                deathDelay = kenneyDelay;
                hasDeathAnim = true;
            }
            else
            {
                SimpleSpriteAnimator knightAnim = GetComponent<SimpleSpriteAnimator>();
                if (knightAnim != null && knightAnim.TryPlayDeath(out float knightDelay))
                {
                    deathDelay = knightDelay;
                    hasDeathAnim = true;
                }
            }

            EnemyPoolable poolable = GetComponent<EnemyPoolable>();
            if (poolable != null && boss == null)
            {
                poolable.ScheduleReturn(hasDeathAnim ? deathDelay : 0.05f);
                return;
            }

            if (hasDeathAnim)
            {
                Destroy(gameObject, deathDelay);
                return;
            }
        }
        else if (isPlayer || CompareTag("Player"))
        {
            if (PassiveItemManager.Instance != null && PassiveItemManager.Instance.TryConsumeRevive())
            {
                CurrentHP = MaxHP * 0.5f;
                HUDManager.Resolve()?.UpdateHp();
                return;
            }

            // Player chết = cú rung dứt khoát đánh dấu kết thúc run.
            GameJuice.Shake(0.6f, 0.5f, 18f);

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
