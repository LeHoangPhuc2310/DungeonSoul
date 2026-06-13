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
    [SerializeField] private float defaultRange = 5f;
    [SerializeField] private float defaultCooldown = 1f;
    [SerializeField] private float defaultDamage = 10f;

    private readonly List<WeaponSlot> activeWeapons = new List<WeaponSlot>(6);
    private readonly Collider2D[] enemyCache = new Collider2D[96];

    private int unlockedSlots = 1;
    private bool weaponSystemEnabled = true;

    public float DamageMultiplier { get; set; } = 1f;
    public float CooldownMultiplier { get; set; } = 1f;
    public float AreaMultiplier { get; set; } = 1f;
    public float ProjectileSpeedMultiplier { get; set; } = 1f;
    public int ExtraProjectileCount { get; set; }
    public IReadOnlyList<WeaponSlot> ActiveWeapons => activeWeapons;
    public int UnlockedSlots => unlockedSlots;

    public bool HasWeapon(WeaponType type) => FindSlot(type) != null;

    public int GetWeaponCopies(WeaponType type)
    {
        WeaponSlot slot = FindSlot(type);
        return slot != null ? slot.copies : 0;
    }

    public bool IsWeaponEvolved(WeaponType type)
    {
        WeaponSlot slot = FindSlot(type);
        return slot != null && slot.evolved;
    }

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
        weaponSystemEnabled = WeaponStyleUtil.UsesWeaponPickupRewards();
        if (weaponSystemEnabled)
        {
            // Pháp sư/cung: không chọn vũ khí trước game — chỉ nhặt trong dungeon.
            if (ExpSystem.Instance != null)
                ExpSystem.Instance.OnLevelUpEvent += HandleLevelUp;
        }

        if (GetComponent<PlayerOrbitalWeapons>() == null)
            gameObject.AddComponent<PlayerOrbitalWeapons>();
        if (GetComponent<PlayerBuffAuraVisual>() == null)
            gameObject.AddComponent<PlayerBuffAuraVisual>();

        UpdateHud();
    }

    private void OnDestroy()
    {
        if (ExpSystem.Instance != null)
            ExpSystem.Instance.OnLevelUpEvent -= HandleLevelUp;
    }

    private void Update()
    {
        if (!weaponSystemEnabled || activeWeapons.Count == 0)
            return;

        float dt = Time.deltaTime;
        for (int i = 0; i < activeWeapons.Count; i++)
        {
            WeaponSlot slot = activeWeapons[i];
            slot.cooldown -= dt;
            if (slot.cooldown > 0f)
                continue;

            Transform target = FindNearestEnemy(GetEffectiveWeaponRange(slot.baseRange));
            FireWeapon(slot, target);
            slot.cooldown = Mathf.Max(0.08f, slot.baseCooldown * Mathf.Max(0.2f, CooldownMultiplier));
        }
    }

    public bool AddOrUpgradeWeapon(WeaponType type)
    {
        if (!weaponSystemEnabled)
            return false;

        if (!WeaponStyleUtil.HeroCanUseWeapon(WeaponStyleUtil.GetSelectedHeroClass(), type))
            return false;

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

        ApplyEvolutionToSlot(slot, evolvedType, 1.8f, 0.8f, 1.2f);
    }

    /// <summary>Tiến hóa khi vũ khí đạt cấp 8 + passive liên kết max cấp.</summary>
    public bool TryEvolveWithPassive(PassiveItemData passive)
    {
        if (passive == null || !passive.HasEvolveCombo)
            return false;

        WeaponSlot slot = FindSlot(passive.evolveTargetWeapon);
        if (slot == null || slot.evolved || slot.copies < 8)
            return false;

        PassivePick pick = PassiveItemManager.Instance != null
            ? PassiveItemManager.Instance.FindPick(passive)
            : null;
        if (pick == null || !pick.IsMaxed)
            return false;

        float dmgMul = passive.evolveDamageMultiplier > 0f ? passive.evolveDamageMultiplier : 1f;
        ApplyEvolutionToSlot(slot, passive.evolveResultWeapon, dmgMul, 0.85f, 1.15f);
        UpdateHud();
        RefreshPlayerWeaponVisual();
        return true;
    }

    private void ApplyEvolutionToSlot(WeaponSlot slot, WeaponType evolvedType, float damageMul, float cooldownMul, float rangeMul)
    {
        slot.weaponType = evolvedType;
        slot.evolved = true;
        slot.copies = 1;
        slot.baseDamage *= damageMul;
        slot.baseCooldown *= cooldownMul;
        slot.baseRange *= rangeMul;
        ApplyWeaponPreset(slot, evolvedType);

        if (WeaponEvolution.Instance != null)
            WeaponEvolution.Instance.PlayEvolutionFx(transform, evolvedType);
    }

    private static float GetEffectiveWeaponRange(float range) =>
        Mathf.Min(range, GameScale.GetCombatRangeFromCamera(1.08f));

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

        if (!WeaponStyleUtil.HeroCanUseWeapon(WeaponStyleUtil.GetSelectedHeroClass(), slot.weaponType))
            return;

        float damage = slot.baseDamage * Mathf.Max(0.2f, DamageMultiplier);
        float speed = 9f * Mathf.Max(0.5f, ProjectileSpeedMultiplier);

        switch (slot.weaponType)
        {
            case WeaponType.StormBow:
                SpawnWeaponProjectile(slot, target, damage, speed, hitPos =>
                {
                    WeaponVfxLibrary.PlayArea(slot.weaponType, hitPos, 2.8f * AreaMultiplier);
                    DamageNearbyEnemies(hitPos, damage, 2.8f * AreaMultiplier, 5);
                });
                break;
            case WeaponType.DragonStaff:
                SpawnWeaponProjectile(slot, target, damage * 1.6f, speed * 0.85f, hitPos =>
                {
                    WeaponVfxLibrary.PlayArea(slot.weaponType, hitPos, 2.2f * AreaMultiplier);
                    DamageNearbyEnemies(hitPos, damage * 1.6f, 2.2f * AreaMultiplier, 3);
                });
                break;
            case WeaponType.BlizzardWand:
                WeaponVfxLibrary.PlayArea(slot.weaponType, transform.position, 5f * AreaMultiplier);
                DamageAllEnemies(damage * 1.1f, 0.35f);
                break;
            case WeaponType.DeathDagger:
                SpawnWeaponProjectile(slot, target, 0f, speed * 1.35f, _ => TryDamageInstantKill(target, damage));
                break;
            case WeaponType.HolyNova:
                WeaponVfxLibrary.PlayArea(slot.weaponType, transform.position, 4.2f * AreaMultiplier);
                DamageNearbyEnemies(transform.position, damage, 4.2f * AreaMultiplier, 999);
                break;
            case WeaponType.ZeusRod:
                WeaponVfxLibrary.PlayArea(slot.weaponType, target.position, 4f);
                DamageAllEnemies(damage * 0.9f, 0f);
                break;
            default:
            {
                int shots = 1 + Mathf.Max(0, ExtraProjectileCount);
                for (int s = 0; s < shots; s++)
                    SpawnWeaponProjectile(slot, target, damage, speed, null);
                break;
            }
        }
    }

    private void SpawnWeaponProjectile(WeaponSlot slot, Transform target, float damage, float speed, System.Action<Vector3> onHit)
    {
        if (WeaponVfxLibrary.HasPack)
        {
            AnimatedWeaponProjectile.Spawn(slot.weaponType, transform.position, target, damage, speed, 2.8f, onHit);
            return;
        }

        if (onHit != null)
            onHit(target.position);
        else
            DamageTarget(target, damage);
    }

    private void DamageTarget(Transform target, float damage)
    {
        HealthSystem health = target != null ? target.GetComponent<HealthSystem>() : null;
        if (health == null)
            return;

        health.TakeDamage(damage);
        SkillBehaviors behaviors = GetComponent<SkillBehaviors>();
        behaviors?.OnPlayerDealtDamage(damage, health);
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
            // ── Bow ──────────────────────────────────────────────
            case WeaponType.IronBow:
                slot.baseDamage = 11f;
                slot.baseCooldown = 1.0f;
                break;
            case WeaponType.StormBow:
                slot.baseDamage = 8f;
                slot.baseCooldown = 0.55f;    // tốc bắn nhanh, dmg thấp
                break;
            // ── Staff / Wand ──────────────────────────────────────
            case WeaponType.FireStaff:
                slot.baseDamage = 14f;
                slot.baseCooldown = 1.2f;
                break;
            case WeaponType.DragonStaff:
                slot.baseDamage = 22f;
                slot.baseCooldown = 2.0f;    // chậm nhưng damage cao
                break;
            case WeaponType.FrostWand:
                slot.baseDamage = 10f;
                slot.baseCooldown = 1.4f;
                break;
            case WeaponType.BlizzardWand:
                slot.baseDamage = 6f;         // AOE slow — damage phụ
                slot.baseCooldown = 1.1f;
                break;
            // ── Dagger ───────────────────────────────────────────
            case WeaponType.PoisonDagger:
                slot.baseDamage = 9f;
                slot.baseCooldown = 0.75f;
                break;
            case WeaponType.DeathDagger:
                slot.baseDamage = 13f;
                slot.baseCooldown = 0.9f;    // 20% instant kill đã có sẵn
                break;
            // ── Holy ─────────────────────────────────────────────
            case WeaponType.HolyCross:
                slot.baseDamage = 12f;
                slot.baseCooldown = 1.35f;
                break;
            case WeaponType.HolyNova:
                slot.baseDamage = 18f;
                slot.baseCooldown = 2.5f;    // AOE lớn, cooldown dài
                break;
            // ── Thunder ──────────────────────────────────────────
            case WeaponType.ThunderRod:
                slot.baseDamage = 16f;
                slot.baseCooldown = 1.8f;
                break;
            case WeaponType.ZeusRod:
                slot.baseDamage = 28f;
                slot.baseCooldown = 2.8f;    // strongest single-hit
                break;
        }
    }

    private void UpdateHud()
    {
        HUDManager hud = HUDManager.Resolve();
        if (hud == null)
            return;

        if (!weaponSystemEnabled)
        {
            hud.SetWeaponBarVisible(false);
            hud.RefreshVsLoadoutPanel();
            return;
        }

        hud.SetWeaponBarVisible(true);
        hud.UpdateWeaponSlots(activeWeapons, unlockedSlots);
        hud.RefreshVsLoadoutPanel();
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
