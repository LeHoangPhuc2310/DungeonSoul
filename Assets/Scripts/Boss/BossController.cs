// DungeonSoul — BossController.cs — Phase system, abilities, defeat rewards.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(EnemyAI))]
public class BossController : MonoBehaviour
{
    [SerializeField] private BossData bossData;
    [SerializeField] private GameObject redChestPrefab;

    private HealthSystem health;
    private EnemyAI ai;
    private EnemyReward reward;
    private Transform player;
    private int currentPhaseIndex = -1;
    private float baseSpeed;
    private float baseDamage;
    private readonly Dictionary<BossAbility, float> nextAbilityTime = new Dictionary<BossAbility, float>();
    private readonly List<Coroutine> runningAbilities = new List<Coroutine>();
    private bool defeated;
    public BossData Data => bossData;
    public bool IsDefeated => defeated;

    public void Initialize(BossData data)
    {
        bossData = data;
        ApplyData();
    }

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
        ai = GetComponent<EnemyAI>();
        reward = GetComponent<EnemyReward>();
        if (reward == null)
            reward = gameObject.AddComponent<EnemyReward>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (bossData != null)
            ApplyData();
        else if (health != null)
            BossHPBarUI.Track(health, "Boss");
    }

    private void ApplyData()
    {
        if (bossData == null || health == null)
            return;

        health.MaxHP = bossData.totalHP;
        health.CurrentHP = bossData.totalHP;
        reward.Configure(bossData.scoreReward, bossData.coinMinFallback(), bossData.coinMaxFallback(), true);

        if (ai != null)
        {
            baseSpeed = ai.MoveSpeed;
            baseDamage = ai.ContactDamage;
            ai.StopDistance = 1.4f;
        }

        EnemyPhysicsSetup physics = GetComponent<EnemyPhysicsSetup>();
        if (physics == null)
            gameObject.AddComponent<EnemyPhysicsSetup>();

        BossHPBarUI.Track(health, bossData.bossName);
        SortPhases();
        EnterPhase(0);
    }

    private void Update()
    {
        if (defeated || bossData == null || health == null || health.CurrentHP <= 0f)
            return;

        float hpRatio = health.CurrentHP / Mathf.Max(1f, health.MaxHP);
        for (int i = currentPhaseIndex + 1; i < bossData.phases.Count; i++)
        {
            if (hpRatio <= bossData.phases[i].hpThreshold)
                EnterPhase(i);
        }
    }

    private void EnterPhase(int index)
    {
        if (bossData == null || index < 0 || index >= bossData.phases.Count)
            return;
        if (index <= currentPhaseIndex)
            return;

        currentPhaseIndex = index;
        BossPhase phase = bossData.phases[index];

        if (ai != null)
        {
            ai.MoveSpeed = baseSpeed * phase.speedMultiplier;
            ai.ContactDamage = baseDamage * phase.damageMultiplier;
        }

        Debug.Log("[Boss] PHASE " + (index + 1) + " BEGIN — " + bossData.bossName);
        EventBus.InvokeBossPhaseChanged(index + 1);
        HUDManager.Resolve()?.ShowWaveAnnouncement(bossData.bossName + " — Phase " + (index + 1));

        StopAllAbilityCoroutines();
        for (int a = 0; a < phase.abilities.Count; a++)
            runningAbilities.Add(StartCoroutine(AbilityLoop(phase.abilities[a])));
    }

    private IEnumerator AbilityLoop(BossAbility ability)
    {
        if (ability == null)
            yield break;

        float wait = Mathf.Max(0.5f, ability.cooldown);
        yield return new WaitForSeconds(Random.Range(0.2f, 1f));

        while (!defeated && health != null && health.CurrentHP > 0f)
        {
            ExecuteAbility(ability);
            yield return new WaitForSeconds(wait);
        }
    }

    private void ExecuteAbility(BossAbility ability)
    {
        if (player == null || ability == null)
            return;

        switch (ability.type)
        {
            case BossAbilityType.AoeSlam:
                if (Vector2.Distance(transform.position, player.position) <= ability.aoeRadius)
                    DamagePlayer(ai != null ? ai.ContactDamage * ability.damageMultiplier : 15f);
                break;
            case BossAbilityType.SpawnMinions:
                BossSpawnManager.SpawnMinionsNear(transform.position, ability.spawnCount);
                break;
            case BossAbilityType.RageDash:
                StartCoroutine(DashRoutine(ability));
                break;
            case BossAbilityType.DarknessTeleport:
                TeleportNearPlayer(2f);
                break;
            case BossAbilityType.SummonClones:
                BossSpawnManager.SpawnMinionsNear(transform.position, Mathf.Max(1, ability.spawnCount));
                break;
            case BossAbilityType.FireBreath:
            case BossAbilityType.MeteorRain:
            case BossAbilityType.SpiralShot:
                if (Vector2.Distance(transform.position, player.position) <= ability.aoeRadius + 1f)
                    DamagePlayer(12f * ability.damageMultiplier);
                break;
            case BossAbilityType.DragonRage:
                if (ai != null)
                    ai.MoveSpeed = baseSpeed * ability.dashSpeedMultiplier;
                DamagePlayer(20f * ability.damageMultiplier);
                break;
            default:
                if (ai != null)
                    ai.MoveSpeed = baseSpeed * ability.dashSpeedMultiplier;
                break;
        }
    }

    private IEnumerator DashRoutine(BossAbility ability)
    {
        if (ai == null || player == null)
            yield break;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            yield break;

        float dashSpeed = baseSpeed * ability.dashSpeedMultiplier;
        float end = Time.time + ability.dashDuration;
        while (Time.time < end && health.CurrentHP > 0f)
        {
            Vector2 dir = ((Vector2)player.position - rb.position).normalized;
            Vector2 next = rb.position + dir * (dashSpeed * Time.deltaTime);
            float minDist = ai != null ? ai.StopDistance : 1.2f;
            if (Vector2.Distance(next, player.position) < minDist)
                break;
            rb.MovePosition(next);
            yield return null;
        }
    }

    private void TeleportNearPlayer(float radius)
    {
        if (player == null)
            return;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Vector2 offset = Random.insideUnitCircle.normalized * radius;
        Vector2 pos = (Vector2)player.position + offset;
        if (rb != null)
            rb.MovePosition(pos);
        else
            transform.position = pos;
    }

    private void DamagePlayer(float amount)
    {
        if (player == null)
            return;
        HealthSystem ph = player.GetComponent<HealthSystem>();
        ph?.TakeDamage(amount);
    }

    private void StopAllAbilityCoroutines()
    {
        for (int i = 0; i < runningAbilities.Count; i++)
        {
            if (runningAbilities[i] != null)
                StopCoroutine(runningAbilities[i]);
        }
        runningAbilities.Clear();
    }

    private void SortPhases()
    {
        bossData.phases.Sort((a, b) => b.hpThreshold.CompareTo(a.hpThreshold));
    }

    public float GetProjectileDamageMultiplier(bool isMagic)
    {
        if (bossData == null)
            return 1f;

        if (bossData.projectileDamageResist > 0f && !isMagic)
            return 1f - bossData.projectileDamageResist;

        if (bossData.weakToMagic && isMagic)
            return 1.35f;

        return 1f;
    }

    public void OnBossDefeated()
    {
        if (defeated)
            return;

        defeated = true;
        StopAllAbilityCoroutines();
        BossHPBarUI.HideBar();

        Debug.Log("[Boss] Defeated: " + (bossData != null ? bossData.bossName : name));
        EventBus.InvokeBossDefeated();

        RunManager run = RunManager.Instance;
        if (run != null && bossData != null)
        {
            run.AddRunCoins(bossData.coinReward);
            run.AddRunScore(bossData.scoreReward);
        }

        SpawnRedChest();
        RunManager.Instance?.OnBossDefeated();
        RoomManager.Instance?.OnRoomCleared();
    }

    private void SpawnRedChest()
    {
        Vector3 pos = transform.position + Vector3.down * 0.5f;
        if (redChestPrefab != null)
        {
            Instantiate(redChestPrefab, pos, Quaternion.identity);
            return;
        }

        GameObject chest = new GameObject("RedChest");
        chest.transform.position = pos;
        chest.tag = "Untagged";
        SpriteRenderer sr = chest.AddComponent<SpriteRenderer>();
        sr.sprite = ArtSpriteLibrary.GetChestSprite();
        sr.color = new Color(1f, 0.25f, 0.2f);
        sr.sortingOrder = 8;
        BoxCollider2D col = chest.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        chest.AddComponent<RedChestController>();
        // Rương thưởng boss to hơn rương thường một chút (dùng chuẩn GameScale).
        GameScale.Fit(chest.transform, sr.sprite, GameScale.ChestHeight * 1.2f);
    }

    private void OnDestroy()
    {
        StopAllAbilityCoroutines();
    }
}

public static class BossDataExtensions
{
    public static int coinMinFallback(this BossData d) => Mathf.Max(10, d.coinReward / 2);
    public static int coinMaxFallback(this BossData d) => d.coinReward;
}
