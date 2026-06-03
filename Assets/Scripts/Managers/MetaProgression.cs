using UnityEngine;

public class MetaProgression : MonoBehaviour
{
    public static MetaProgression Instance { get; private set; }

    private const string KeyMetaCoins = "ds_meta_coins";
    private const string KeyVital = "ds_up_vital";
    private const string KeyPower = "ds_up_power";

    public int MetaCoins { get; private set; }
    public int VitalLevel { get; private set; }
    public int PowerLevel { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void AddMetaCoins(int amount)
    {
        if (amount <= 0)
            return;
        MetaCoins += amount;
        Save();
    }

    public bool TryBuyVital()
    {
        int cost = 50 + VitalLevel * 25;
        if (MetaCoins < cost || VitalLevel >= 10)
            return false;
        MetaCoins -= cost;
        VitalLevel++;
        Save();
        return true;
    }

    public bool TryBuyPower()
    {
        int cost = 80 + PowerLevel * 40;
        if (MetaCoins < cost || PowerLevel >= 10)
            return false;
        MetaCoins -= cost;
        PowerLevel++;
        Save();
        return true;
    }

    public float GetBonusMaxHp() => VitalLevel * 20f;
    public float GetBonusDamageMultiplier() => 1f + PowerLevel * 0.15f;

    public void ApplyToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        HealthSystem hs = player.GetComponent<HealthSystem>();
        if (hs != null)
        {
            float bonus = GetBonusMaxHp();
            hs.MaxHP += bonus;
            hs.CurrentHP += bonus;
        }

        AutoAttack atk = player.GetComponent<AutoAttack>();
        if (atk != null)
            atk.ProjectileDamage *= GetBonusDamageMultiplier();

        if (WeaponManager.Instance != null)
            WeaponManager.Instance.DamageMultiplier *= GetBonusDamageMultiplier();
    }

    private void Load()
    {
        MetaCoins = PlayerPrefs.GetInt(KeyMetaCoins, 0);
        VitalLevel = PlayerPrefs.GetInt(KeyVital, 0);
        PowerLevel = PlayerPrefs.GetInt(KeyPower, 0);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KeyMetaCoins, MetaCoins);
        PlayerPrefs.SetInt(KeyVital, VitalLevel);
        PlayerPrefs.SetInt(KeyPower, PowerLevel);
        PlayerPrefs.Save();
    }
}
