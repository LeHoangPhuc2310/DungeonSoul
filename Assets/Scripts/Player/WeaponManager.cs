using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class WeaponManager : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSlot
    {
        public WeaponType weaponType;
        public int copies = 1;
        public bool evolved;
        public float cooldown;
        public float baseCooldown = 1f;
        public float baseDamage = 10f;
        public float baseRange = 8f;
    }

    public static WeaponManager Instance { get; private set; }

    [SerializeField] private int maxWeapons = 6;
    [SerializeField] private float defaultRange = 9f;
    [SerializeField] private float defaultCooldown = 1f;
    [SerializeField] private float defaultDamage = 10f;

    private readonly List<WeaponSlot> activeWeapons = new List<WeaponSlot>(6);
    private readonly Collider2D[] enemyCache = new Collider2D[96];

    private int unlockedSlots = 1;

    public float DamageMultiplier { get; set; } = 1f;
    public float CooldownMultiplier { get; set; } = 1f;
    public float AreaMultiplier { get; set; } = 1f;
    public float ProjectileSpeedMultiplier { get; set; } = 1f;
    public IReadOnlyList<WeaponSlot> ActiveWeapons => activeWeapons;
    public int UnlockedSlots => unlockedSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        AddOrUpgradeWeapon(WeaponType.IronBow);
        if (ExpSystem.Instance != null)
            ExpSystem.Instance.OnLevelUpEvent += HandleLevelUp;

        UpdateHud();
    }

    private void OnDestroy()
    {
        if (ExpSystem.Instance != null)
            ExpSystem.Instance.OnLevelUpEvent -= HandleLevelUp;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < activeWeapons.Count; i++)
        {
            WeaponSlot slot = activeWeapons[i];
            slot.cooldown -= dt;
            if (slot.cooldown > 0f)
                continue;

            Transform target = FindNearestEnemy(slot.baseRange);
            FireWeapon(slot, target);
            slot.cooldown = Mathf.Max(0.08f, slot.baseCooldown * Mathf.Max(0.2f, CooldownMultiplier));
        }
    }

    public bool AddOrUpgradeWeapon(WeaponType type)
    {
        WeaponSlot existing = FindSlot(type);
        if (existing != null)
        {
            existing.copies++;
            existing.baseDamage += 3f;
            existing.baseCooldown *= 0.95f;
            TryEvolve(existing);
            UpdateHud();
            RefreshPlayerWeaponVisual();
            return true;
        }

        if (activeWeapons.Count >= Mathf.Min(maxWeapons, unlockedSlots))
            return false;

        WeaponSlot slot = new WeaponSlot
        {
            weaponType = type,
            copies = 1,
            evolved = false,
            baseCooldown = defaultCooldown,
            baseDamage = defaultDamage,
            baseRange = defaultRange
        };
        ApplyWeaponPreset(slot, type);
        activeWeapons.Add(slot);
        UpdateHud();
        RefreshPlayerWeaponVisual();
        return true;
    }

    public List<WeaponType> GetRandomWeaponChoices(int count)
    {
        List<WeaponType> pool = new List<WeaponType>
        {
            WeaponType.IronBow,
            WeaponType.FireStaff,
            WeaponType.FrostWand,
            WeaponType.PoisonDagger,
            WeaponType.HolyCross,
            WeaponType.ThunderRod
        };

        List<WeaponType> result = new List<WeaponType>(count);
        int pickCount = Mathf.Min(count, pool.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return result;
    }

    private void HandleLevelUp(int level)
    {
        if (unlockedSlots < maxWeapons)
            unlockedSlots++;

        UpdateHud();
    }

    private WeaponSlot FindSlot(WeaponType type)
    {
        for (int i = 0; i < activeWeapons.Count; i++)
        {
            if (activeWeapons[i].weaponType == type)
                return activeWeapons[i];
        }

        return null;
    }

    private void TryEvolve(WeaponSlot slot)
    {
        if (slot.evolved || WeaponEvolution.Instance == null)
            return;

        WeaponType evolvedType;
        if (!WeaponEvolution.Instance.TryGetEvolution(slot.weaponType, slot.copies, out evolvedType))
            return;

        slot.weaponType = evolvedType;
        slot.evolved = true;
        slot.copies = 1;
        slot.baseDamage *= 1.8f;
        slot.baseCooldown *= 0.8f;
        slot.baseRange *= 1.2f;

        WeaponEvolution.Instance.PlayEvolutionFx(transform, evolvedType);
    }

    private Transform FindNearestEnemy(float range)
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, range, enemyCache);
        float minDistanceSqr = range * range;
        Transform nearest = null;

        for (int i = 0; i < count; i++)
        {
            Collider2D c = enemyCache[i];
            if (c == null || !c.CompareTag("Enemy"))
                continue;

            float dist = (c.transform.position - transform.position).sqrMagnitude;
            if (dist <= minDistanceSqr)
            {
                minDistanceSqr = dist;
                nearest = c.transform;
            }
        }

        return nearest;
    }

    private void FireWeapon(WeaponSlot slot, Transform target)
    {
        if (target == null)
            return;

        float damage = slot.baseDamage * Mathf.Max(0.2f, DamageMultiplier);
        switch (slot.weaponType)
        {
            case WeaponType.StormBow:
                DamageNearbyEnemies(target.position, damage, 2.8f * AreaMultiplier, 5);
                break;
            case WeaponType.DragonStaff:
                DamageNearbyEnemies(target.position, damage * 1.6f, 2.2f * AreaMultiplier, 3);
                break;
            case WeaponType.BlizzardWand:
                DamageAllEnemies(damage * 1.1f, 0.35f);
                break;
            case WeaponType.DeathDagger:
                TryDamageInstantKill(target, damage);
                break;
            case WeaponType.HolyNova:
                DamageNearbyEnemies(transform.position, damage, 4.2f * AreaMultiplier, 999);
                break;
            case WeaponType.ZeusRod:
                DamageAllEnemies(damage * 0.9f, 0f);
                break;
            default:
                DamageTarget(target, damage);
                break;
        }
    }

    private void DamageTarget(Transform target, float damage)
    {
        HealthSystem health = target != null ? target.GetComponent<HealthSystem>() : null;
        if (health == null)
            return;

        health.TakeDamage(damage);
        if (HUDManager.Instance != null)
            HUDManager.Instance.RegisterDamageDealt(damage);
    }

    private void TryDamageInstantKill(Transform target, float damage)
    {
        HealthSystem health = target != null ? target.GetComponent<HealthSystem>() : null;
        if (health == null)
            return;

        if (Random.value <= 0.2f)
            health.TakeDamage(99999f);
        else
            health.TakeDamage(damage);

        if (HUDManager.Instance != null)
            HUDManager.Instance.RegisterDamageDealt(damage);
    }

    private void DamageNearbyEnemies(Vector3 center, float damage, float radius, int maxHits)
    {
        int count = Physics2D.OverlapCircleNonAlloc(center, radius, enemyCache);
        int hits = 0;
        for (int i = 0; i < count && hits < maxHits; i++)
        {
            Collider2D c = enemyCache[i];
            if (c == null || !c.CompareTag("Enemy"))
                continue;

            HealthSystem health = c.GetComponent<HealthSystem>();
            if (health == null)
                continue;

            health.TakeDamage(damage);
            hits++;
            if (HUDManager.Instance != null)
                HUDManager.Instance.RegisterDamageDealt(damage);
        }
    }

    private void DamageAllEnemies(float damage, float slowFactor)
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, 100f, enemyCache);
        for (int i = 0; i < count; i++)
        {
            Collider2D c = enemyCache[i];
            if (c == null || !c.CompareTag("Enemy"))
                continue;

            HealthSystem health = c.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage);
                if (HUDManager.Instance != null)
                    HUDManager.Instance.RegisterDamageDealt(damage);
            }

            if (slowFactor > 0f)
            {
                EnemyAI ai = c.GetComponent<EnemyAI>();
                if (ai != null)
                    ai.ApplySlow(slowFactor, 1.2f);
            }
        }
    }

    private void ApplyWeaponPreset(WeaponSlot slot, WeaponType type)
    {
        switch (type)
        {
            case WeaponType.FireStaff:
                slot.baseDamage = 14f;
                slot.baseCooldown = 1.2f;
                break;
            case WeaponType.FrostWand:
                slot.baseDamage = 10f;
                slot.baseCooldown = 1.4f;
                break;
            case WeaponType.PoisonDagger:
                slot.baseDamage = 9f;
                slot.baseCooldown = 0.75f;
                break;
            case WeaponType.HolyCross:
                slot.baseDamage = 12f;
                slot.baseCooldown = 1.35f;
                break;
            case WeaponType.ThunderRod:
                slot.baseDamage = 16f;
                slot.baseCooldown = 1.8f;
                break;
        }
    }

    private void UpdateHud()
    {
        HUDManager hud = HUDManager.Resolve();
        if (hud == null)
            return;

        List<WeaponType> weapons = new List<WeaponType>(activeWeapons.Count);
        for (int i = 0; i < activeWeapons.Count; i++)
            weapons.Add(activeWeapons[i].weaponType);

        hud.UpdateWeaponSlots(weapons, unlockedSlots);
    }

    private static void RefreshPlayerWeaponVisual()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayerWeaponVisual visual = player.GetComponent<PlayerWeaponVisual>();
        visual?.RefreshFromLoadout();
    }
}
