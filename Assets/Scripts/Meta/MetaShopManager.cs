// DungeonSoul — MetaShopManager.cs — Meta upgrades load/save/apply (11 GDD upgrades).

using System.Collections.Generic;
using UnityEngine;

public class MetaShopManager : MonoBehaviour
{
    public static MetaShopManager Instance { get; private set; }

    [SerializeField] private List<MetaUpgradeData> upgrades = new List<MetaUpgradeData>();

    private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

    public int MetaCoins
    {
        get => MetaProgression.Instance != null ? MetaProgression.Instance.MetaCoins : PlayerPrefs.GetInt("ds_meta_coins", 0);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureDefaultUpgrades();
        LoadAll();
    }

    public int GetLevel(string upgradeName)
    {
        return levels.TryGetValue(upgradeName, out int lv) ? lv : 0;
    }

    public int GetNextCost(MetaUpgradeData data)
    {
        if (data == null)
            return int.MaxValue;
        int lv = GetLevel(data.upgradeName);
        return data.baseCost + lv * data.costPerLevel;
    }

    public bool BuyUpgrade(string upgradeName)
    {
        MetaUpgradeData data = Find(upgradeName);
        if (data == null)
            return false;

        int lv = GetLevel(upgradeName);
        if (lv >= data.maxLevel)
            return false;

        int cost = GetNextCost(data);
        if (MetaCoins < cost)
            return false;

        if (MetaProgression.Instance != null)
            MetaProgression.Instance.SpendMetaCoins(cost);
        else
        {
            int coins = PlayerPrefs.GetInt("ds_meta_coins", 0);
            if (coins < cost)
                return false;
            PlayerPrefs.SetInt("ds_meta_coins", coins - cost);
        }

        levels[upgradeName] = lv + 1;
        Save(upgradeName);
        return true;
    }

    public void ApplyAllUpgrades()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        HealthSystem hs = player.GetComponent<HealthSystem>();
        AutoAttack atk = player.GetComponent<AutoAttack>();
        PlayerController move = player.GetComponent<PlayerController>();

        float hpBonus = GetEffect(MetaUpgradeType.HP);
        if (hs != null && hpBonus > 0f)
        {
            hs.MaxHP += hpBonus;
            hs.CurrentHP += hpBonus;
        }

        if (atk != null)
        {
            atk.ProjectileDamage *= 1f + GetEffect(MetaUpgradeType.Damage) * 0.01f;
            float aspd = GetEffect(MetaUpgradeType.AttackSpeed) * 0.01f;
            atk.FireInterval = Mathf.Max(0.05f, atk.FireInterval / (1f + aspd));
        }

        if (move != null)
            move.MoveSpeed += GetEffect(MetaUpgradeType.MoveSpeed);
    }

    public float GetEffect(MetaUpgradeType type)
    {
        float total = 0f;
        for (int i = 0; i < upgrades.Count; i++)
        {
            MetaUpgradeData u = upgrades[i];
            if (u == null || u.upgradeType != type)
                continue;
            total += GetLevel(u.upgradeName) * u.effectPerLevel;
        }
        return total;
    }

    public IReadOnlyList<MetaUpgradeData> AllUpgrades => upgrades;

    /// <summary>Nạp lại level upgrade từ PlayerPrefs — gọi khi mở shop để hiển thị dữ liệu mới nhất.</summary>
    public void ReloadFromSave()
    {
        LoadAll();
    }

    private void EnsureDefaultUpgrades()
    {
        if (upgrades.Count >= 11)
            return;

        upgrades.Clear();
        upgrades.Add(Make("Vital", MetaUpgradeType.HP, 10, 50, 25, 20f, "+20 HP max"));
        upgrades.Add(Make("Power", MetaUpgradeType.Damage, 10, 80, 40, 15f, "+15% damage"));
        upgrades.Add(Make("Rapid", MetaUpgradeType.AttackSpeed, 8, 70, 35, 12f, "+12% attack speed"));
        upgrades.Add(Make("Swift", MetaUpgradeType.MoveSpeed, 6, 60, 30, 0.5f, "+0.5 move speed"));
        upgrades.Add(Make("Starter", MetaUpgradeType.StarterSkill, 3, 100, 50, 1f, "Start with Rare skills"));
        upgrades.Add(Make("Wealth", MetaUpgradeType.CoinBonus, 5, 90, 45, 20f, "+20% xu/run"));
        upgrades.Add(Make("Regen", MetaUpgradeType.RoomRegen, 5, 120, 60, 2f, "Heal HP per room"));
        upgrades.Add(Make("Lucky", MetaUpgradeType.SkillRarity, 3, 150, 75, 5f, "Better skill rarity"));
        upgrades.Add(Make("LootLuck", MetaUpgradeType.LootLuck, 5, 100, 50, 5f, "+5% drop rate"));
        upgrades.Add(Make("ForgeMaster", MetaUpgradeType.ForgeMaster, 3, 200, 100, 1f, "+1 forge reroll"));
        upgrades.Add(Make("WeaponMastery", MetaUpgradeType.WeaponMastery, 5, 150, 75, 10f, "-10% weapon CD"));
    }

    private static MetaUpgradeData Make(string name, MetaUpgradeType type, int max, int baseCost, int perLv, float effect, string desc)
    {
        MetaUpgradeData d = ScriptableObject.CreateInstance<MetaUpgradeData>();
        d.upgradeName = name;
        d.upgradeType = type;
        d.maxLevel = max;
        d.baseCost = baseCost;
        d.costPerLevel = perLv;
        d.effectPerLevel = effect;
        d.description = desc;
        return d;
    }

    private MetaUpgradeData Find(string name)
    {
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i] != null && upgrades[i].upgradeName == name)
                return upgrades[i];
        }
        return null;
    }

    private void LoadAll()
    {
        levels.Clear();
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i] == null)
                continue;
            string key = "meta_" + upgrades[i].upgradeName;
            levels[upgrades[i].upgradeName] = PlayerPrefs.GetInt(key, 0);
        }
    }

    private void Save(string name)
    {
        if (!levels.TryGetValue(name, out int lv))
            return;
        PlayerPrefs.SetInt("meta_" + name, lv);
        PlayerPrefs.Save();
    }
}
