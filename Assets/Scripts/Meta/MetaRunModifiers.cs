// DungeonSoul — MetaRunModifiers.cs — Hiệu lực 11 meta upgrade trong run.

using System.Collections.Generic;
using UnityEngine;

public class MetaRunModifiers : MonoBehaviour
{
    public static MetaRunModifiers Instance { get; private set; }

    public float CoinRunMultiplier { get; private set; } = 1f;
    public float LootDropBonus { get; private set; }
    public float SkillRarityBonus { get; private set; }
    public float RoomHealAmount { get; private set; }
    public int ExtraForgeRerolls { get; private set; }
    public float WeaponCooldownReduction { get; private set; }
    public int StarterRareSkills { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshFromMeta();
    }

    public void RefreshFromMeta()
    {
        MetaShopManager shop = MetaShopManager.Instance;
        if (shop == null)
            return;

        CoinRunMultiplier = 1f + shop.GetEffect(MetaUpgradeType.CoinBonus) * 0.01f;
        LootDropBonus = shop.GetEffect(MetaUpgradeType.LootLuck) * 0.01f;
        SkillRarityBonus = shop.GetEffect(MetaUpgradeType.SkillRarity) * 0.01f;
        RoomHealAmount = shop.GetEffect(MetaUpgradeType.RoomRegen);
        ExtraForgeRerolls = Mathf.RoundToInt(shop.GetEffect(MetaUpgradeType.ForgeMaster));
        WeaponCooldownReduction = shop.GetEffect(MetaUpgradeType.WeaponMastery) * 0.01f;
        StarterRareSkills = Mathf.RoundToInt(shop.GetEffect(MetaUpgradeType.StarterSkill));
    }

    public void ApplyAtRunStart()
    {
        RefreshFromMeta();
        MetaShopManager.Instance?.ApplyAllUpgrades();

        if (WeaponManager.Instance != null)
            WeaponManager.Instance.CooldownMultiplier = Mathf.Max(0.5f, 1f - WeaponCooldownReduction);

        GrantStarterSkills();
    }

    private void GrantStarterSkills()
    {
        if (StarterRareSkills <= 0)
            return;

        PlayerSkillHandler handler = PlayerSkillHandler.Instance;
        if (handler == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                handler = p.GetComponent<PlayerSkillHandler>();
        }

        if (handler == null)
            return;

        SkillData[] pool = Resources.LoadAll<SkillData>("SkillData");
        List<SkillData> rares = new List<SkillData>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null && pool[i].rarity >= SkillRarity.Rare)
                rares.Add(pool[i]);
        }

        for (int i = 0; i < StarterRareSkills && rares.Count > 0; i++)
        {
            SkillData pick = rares[Random.Range(0, rares.Count)];
            handler.ApplySkill(pick);
            rares.Remove(pick);
        }
    }

    public static int ScaleCoins(int baseAmount)
    {
        if (Instance == null)
            return baseAmount;
        return Mathf.Max(0, Mathf.RoundToInt(baseAmount * Instance.CoinRunMultiplier));
    }
}
