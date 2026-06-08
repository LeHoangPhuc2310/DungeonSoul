using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Quản lý passive item trong một run Wave Arena (tối đa 6 ô, reset mỗi run).</summary>
public class PassiveItemManager : MonoBehaviour
{
    public static PassiveItemManager Instance { get; private set; }

    [SerializeField] private List<PassiveItemData> availableItems = new List<PassiveItemData>();
    [SerializeField] private int maxPassiveItems = 6;

    private readonly List<PassivePick> pickedItems = new List<PassivePick>(6);

    private PlayerController playerController;
    private HealthSystem playerHealth;
    private WeaponManager weaponManager;
    private AutoAttack autoAttack;

    private float baseMoveSpeed = -1f;
    private float baseMaxHp = -1f;
    private bool reviveAvailable;

    // Chỉ số tổng hợp — các hệ thống khác đọc qua property
    public float ExpGainMultiplier { get; private set; } = 1f;
    public float CritChanceBonus { get; private set; }
    public float LifeStealBonus { get; private set; }
    public float BurnChanceBonus { get; private set; }
    public int ExtraProjectileCount { get; private set; }
    public float DamageMultiplierBonus { get; private set; } = 1f;
    public float CooldownMultiplierBonus { get; private set; } = 1f;

    public IReadOnlyList<PassivePick> PickedItems => pickedItems;
    public int MaxPassiveItems => maxPassiveItems;

    public event Action OnPassivesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadAvailableItemsFromResources();
    }

    private void Start()
    {
        CacheReferences();
        UpdateHud();
    }

    private void LoadAvailableItemsFromResources()
    {
        if (availableItems != null && availableItems.Count > 0)
            return;

        PassiveItemData[] loaded = Resources.LoadAll<PassiveItemData>("PassiveItems");
        if (loaded != null && loaded.Length > 0)
            availableItems = new List<PassiveItemData>(loaded);
    }

    /// <summary>Lấy ứng viên ngẫu nhiên theo độ hiếm (level-up hoặc rương boss).</summary>
    /// <summary>Passive có thể xuất hiện trên panel chọn (mới hoặc nâng cấp).</summary>
    public List<PassiveItemData> GetEligibleForSelection()
    {
        List<PassiveItemData> pool = new List<PassiveItemData>();
        LoadAvailableItemsFromResources();

        for (int i = 0; i < availableItems.Count; i++)
        {
            PassiveItemData item = availableItems[i];
            if (item == null)
                continue;

            PassivePick existing = FindPick(item);
            if (existing == null)
            {
                if (pickedItems.Count < maxPassiveItems)
                    pool.Add(item);
            }
            else if (!existing.IsMaxed)
            {
                pool.Add(item);
            }
        }

        return pool;
    }

    public List<PassiveItemData> GetRandomCandidates(int count, bool bossChest = false)
    {
        List<PassiveItemData> result = new List<PassiveItemData>(count);
        for (int i = 0; i < count; i++)
        {
            SkillRarity rarity = RollPassiveRarity(bossChest);
            PassiveItemData pick = PickOneOfRarity(rarity);
            if (pick != null)
                result.Add(pick);
        }

        return result;
    }

    public static SkillRarity RollPassiveRarity(bool bossChest)
    {
        float roll = UnityEngine.Random.value;
        if (bossChest)
        {
            if (roll < 0.4f) return SkillRarity.Rare;
            if (roll < 0.9f) return SkillRarity.Epic;
            return SkillRarity.Legendary;
        }

        if (roll < 0.6f) return SkillRarity.Common;
        if (roll < 0.9f) return SkillRarity.Rare;
        if (roll < 0.99f) return SkillRarity.Epic;
        return SkillRarity.Legendary;
    }

    private PassiveItemData PickOneOfRarity(SkillRarity rarity)
    {
        List<PassiveItemData> pool = BuildEligiblePool();
        if (pool.Count == 0)
            return null;

        List<PassiveItemData> matched = pool.FindAll(p => p != null && p.rarity == rarity);
        if (matched.Count == 0)
            matched = pool;

        return matched[UnityEngine.Random.Range(0, matched.Count)];
    }

    private List<PassiveItemData> BuildEligiblePool()
    {
        List<PassiveItemData> pool = new List<PassiveItemData>();
        for (int i = 0; i < availableItems.Count; i++)
        {
            PassiveItemData item = availableItems[i];
            if (item == null)
                continue;

            PassivePick existing = FindPick(item);
            if (existing == null)
            {
                if (pickedItems.Count < maxPassiveItems)
                    pool.Add(item);
            }
            else if (!existing.IsMaxed)
            {
                pool.Add(item);
            }
        }

        return pool;
    }

    public PassivePick FindPick(PassiveItemData data)
    {
        if (data == null)
            return null;

        for (int i = 0; i < pickedItems.Count; i++)
        {
            if (pickedItems[i].data == data)
                return pickedItems[i];
        }

        return null;
    }

    public int GetLevel(PassiveItemData data)
    {
        PassivePick pick = FindPick(data);
        return pick != null ? pick.level : 0;
    }

    /// <summary>Nhặt hoặc nâng cấp passive. Trả về false nếu cần swap (6 ô đầy).</summary>
    public bool TryApplyPassive(PassiveItemData item, bool forceNewSlot = false)
    {
        if (item == null)
            return false;

        PassivePick existing = FindPick(item);
        if (existing != null)
        {
            if (existing.IsMaxed)
                return false;

            existing.level++;
            FinalizeApply(item);
            return true;
        }

        if (pickedItems.Count >= maxPassiveItems)
        {
            PassiveSwapUI.Show(item, pickedItems, OnSwapChosen, null, PassiveSwapUI.PendingChestReward);
            return false;
        }

        pickedItems.Add(new PassivePick { data = item, level = 1 });
        FinalizeApply(item);
        return true;
    }

    private void OnSwapChosen(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= pickedItems.Count)
            return;

        PassiveItemData incoming = PassiveSwapUI.PendingItem;
        if (incoming == null)
            return;

        pickedItems[slotIndex] = new PassivePick { data = incoming, level = 1 };
        FinalizeApply(incoming);
    }

    private void FinalizeApply(PassiveItemData item)
    {
        CacheReferences();
        RecalculateAllStats();
        PlayerSkillHandler handler = PlayerSkillHandler.Instance;
        if (handler != null)
            handler.RefreshStats();

        ApplyAggregatedToPlayer();
        UpdateHud();
        OnPassivesChanged?.Invoke();
        Debug.Log("[Passive] " + item.displayName + " cấp " + GetLevel(item));
    }

    /// <summary>Tính lại toàn bộ chỉ số từ passive đã nhặt.</summary>
    public void RecalculateAllStats()
    {
        ExpGainMultiplier = 1f;
        CritChanceBonus = 0f;
        LifeStealBonus = 0f;
        BurnChanceBonus = 0f;
        ExtraProjectileCount = 0;
        DamageMultiplierBonus = 1f;
        CooldownMultiplierBonus = 1f;
        reviveAvailable = false;

        float defensePercent = 0f;
        float hpFlat = 0f;
        float movePercent = 0f;

        for (int i = 0; i < pickedItems.Count; i++)
        {
            PassivePick pick = pickedItems[i];
            if (pick?.data == null)
                continue;

            float total = pick.data.GetTotalValueAtLevel(pick.level);
            switch (pick.data.statModifierType)
            {
                case PassiveStatModifierType.Defense:
                    defensePercent += total;
                    break;
                case PassiveStatModifierType.HP:
                    hpFlat += total;
                    break;
                case PassiveStatModifierType.MoveSpeed:
                    movePercent += total;
                    break;
                case PassiveStatModifierType.Damage:
                    DamageMultiplierBonus *= 1f + total;
                    break;
                case PassiveStatModifierType.CooldownReduction:
                    CooldownMultiplierBonus *= Mathf.Max(0.1f, 1f - total);
                    break;
                case PassiveStatModifierType.ExpGain:
                    ExpGainMultiplier += total;
                    break;
                case PassiveStatModifierType.CritChance:
                    CritChanceBonus += total;
                    break;
                case PassiveStatModifierType.Magnet:
                    break; // áp dụng trực tiếp trong ApplyAggregatedToPlayer
                case PassiveStatModifierType.BurnChance:
                    BurnChanceBonus += total;
                    break;
                case PassiveStatModifierType.LifeSteal:
                    LifeStealBonus += total;
                    break;
                case PassiveStatModifierType.ProjectileCount:
                    ExtraProjectileCount += Mathf.RoundToInt(total);
                    break;
                case PassiveStatModifierType.Revive:
                    reviveAvailable = true;
                    break;
            }
        }

        _cachedDefensePercent = defensePercent;
        _cachedHpFlat = hpFlat;
        _cachedMovePercent = movePercent;
        _cachedMagnetBonus = SumMagnetBonus();
    }

    private float _cachedDefensePercent;
    private float _cachedHpFlat;
    private float _cachedMovePercent;
    private float _cachedMagnetBonus;

    private float SumMagnetBonus()
    {
        float sum = 0f;
        for (int i = 0; i < pickedItems.Count; i++)
        {
            PassivePick pick = pickedItems[i];
            if (pick?.data == null || pick.data.statModifierType != PassiveStatModifierType.Magnet)
                continue;
            sum += pick.data.GetTotalValueAtLevel(pick.level);
        }

        return sum;
    }

    /// <summary>Áp chỉ số passive lên player sau khi skill đã recalculate.</summary>
    public void ApplyAggregatedToPlayer()
    {
        CacheReferences();

        if (playerController != null)
        {
            if (baseMoveSpeed < 0f)
                baseMoveSpeed = playerController.MoveSpeed;
            playerController.MoveSpeed = baseMoveSpeed * (1f + _cachedMovePercent);
        }

        if (playerHealth != null)
        {
            if (baseMaxHp < 0f)
                baseMaxHp = playerHealth.MaxHP;

            float targetMax = baseMaxHp + _cachedHpFlat;
            float delta = targetMax - playerHealth.MaxHP;
            playerHealth.MaxHP = targetMax;
            if (delta > 0f)
                playerHealth.CurrentHP += delta;

            playerHealth.SetPassiveDamageReduction(_cachedDefensePercent);
        }

        if (weaponManager != null)
        {
            weaponManager.DamageMultiplier = DamageMultiplierBonus;
            weaponManager.CooldownMultiplier = CooldownMultiplierBonus;
            weaponManager.ExtraProjectileCount = ExtraProjectileCount;
        }

        if (autoAttack != null)
            autoAttack.ProjectileCount = 1 + ExtraProjectileCount;

        ApplyMagnetBonus(_cachedMagnetBonus);
    }

    private static void ApplyMagnetBonus(float bonus)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        MagnetRadiusUpgrade magnet = player.GetComponent<MagnetRadiusUpgrade>();
        if (magnet == null)
            magnet = player.AddComponent<MagnetRadiusUpgrade>();
        magnet.SetBonus(bonus);
    }

    /// <summary>Hồi sinh 1 lần — gọi từ HealthSystem khi chết.</summary>
    public bool TryConsumeRevive()
    {
        if (!reviveAvailable)
            return false;

        reviveAvailable = false;
        for (int i = 0; i < pickedItems.Count; i++)
        {
            if (pickedItems[i].data != null && pickedItems[i].data.statModifierType == PassiveStatModifierType.Revive)
            {
                pickedItems[i].level = 0;
                break;
            }
        }

        RecalculateAllStats();
        ApplyAggregatedToPlayer();
        UpdateHud();
        HUDManager.Resolve()?.ShowWaveAnnouncement("HỒI SINH — Vương Miện Vĩnh Cửu!");
        return true;
    }

    /// <summary>Kiểm tra tiến hóa vũ khí cuối mỗi wave (vũ khí cấp 8 + passive max).</summary>
    public void CheckPassiveEvolutionsAtWaveEnd()
    {
        if (weaponManager == null)
            CacheReferences();
        if (weaponManager == null)
            return;

        for (int i = 0; i < pickedItems.Count; i++)
        {
            PassivePick pick = pickedItems[i];
            if (pick?.data == null || !pick.IsMaxed || !pick.data.HasEvolveCombo)
                continue;

            if (weaponManager.TryEvolveWithPassive(pick.data))
            {
                HUDManager.Resolve()?.ShowWaveAnnouncement("TIẾN HÓA — " + pick.data.evolveResultWeapon + "!");
            }
        }
    }

    public void ResetForNewRun()
    {
        pickedItems.Clear();
        baseMoveSpeed = -1f;
        baseMaxHp = -1f;
        RecalculateAllStats();
        ApplyAggregatedToPlayer();
        UpdateHud();
    }

    public void DebugGrantRandom(SkillRarity rarity)
    {
        PassiveItemData pick = PickOneOfRarity(rarity);
        if (pick == null)
        {
            Debug.LogWarning("[Passive] Không có passive độ hiếm " + rarity);
            return;
        }

        TryApplyPassive(pick, true);
    }

    private void CacheReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        if (playerController == null) playerController = player.GetComponent<PlayerController>();
        if (playerHealth == null) playerHealth = player.GetComponent<HealthSystem>();
        if (weaponManager == null) weaponManager = player.GetComponent<WeaponManager>();
        if (autoAttack == null) autoAttack = player.GetComponent<AutoAttack>();
    }

    private void UpdateHud()
    {
        HUDManager hud = HUDManager.Resolve();
        if (hud != null)
            hud.UpdatePassiveSlots(pickedItems, maxPassiveItems);
    }
}
