using System.Collections.Generic;
using UnityEngine;

public class PassiveItemManager : MonoBehaviour
{
    public static PassiveItemManager Instance { get; private set; }

    [SerializeField] private List<PassiveItem> availableItems = new List<PassiveItem>();
    [SerializeField] private int maxPassiveItems = 8;

    private readonly List<PassiveItem> pickedItems = new List<PassiveItem>(8);

    private PlayerController playerController;
    private HealthSystem playerHealth;
    private WeaponManager weaponManager;

    public IReadOnlyList<PassiveItem> PickedItems => pickedItems;

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
        CacheReferences();
        UpdateHud();
    }

    public List<PassiveItem> GetRandomCandidates(int count)
    {
        List<PassiveItem> pool = new List<PassiveItem>();
        for (int i = 0; i < availableItems.Count; i++)
        {
            PassiveItem item = availableItems[i];
            if (item != null && !pickedItems.Contains(item))
                pool.Add(item);
        }

        List<PassiveItem> result = new List<PassiveItem>(count);
        int pickCount = Mathf.Min(count, pool.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return result;
    }

    public bool ApplyPassive(PassiveItem item)
    {
        if (item == null || pickedItems.Count >= maxPassiveItems || pickedItems.Contains(item))
            return false;

        CacheReferences();
        pickedItems.Add(item);

        switch (item.itemType)
        {
            case PassiveItemType.Spinach:
                if (weaponManager != null) weaponManager.DamageMultiplier *= 1.1f;
                break;
            case PassiveItemType.Armor:
                if (playerHealth != null) playerHealth.AddFlatDamageReduction(1f);
                break;
            case PassiveItemType.Wings:
                if (playerController != null) playerController.MoveSpeed += 0.3f;
                break;
            case PassiveItemType.EmptyTome:
                if (weaponManager != null) weaponManager.CooldownMultiplier *= 0.92f;
                break;
            case PassiveItemType.Candelabrador:
                if (weaponManager != null) weaponManager.AreaMultiplier *= 1.1f;
                break;
            case PassiveItemType.Bracer:
                if (weaponManager != null) weaponManager.ProjectileSpeedMultiplier *= 1.1f;
                break;
            case PassiveItemType.HollowHeart:
                if (playerHealth != null)
                {
                    playerHealth.MaxHP += 20f;
                    playerHealth.Heal(20f);
                }
                break;
            case PassiveItemType.Pummarola:
                if (playerHealth != null) playerHealth.AddRegen(0.2f);
                break;
        }

        UpdateHud();
        return true;
    }

    private void CacheReferences()
    {
        if (playerController != null && playerHealth != null && weaponManager != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (playerController == null) playerController = player.GetComponent<PlayerController>();
            if (playerHealth == null) playerHealth = player.GetComponent<HealthSystem>();
            if (weaponManager == null) weaponManager = player.GetComponent<WeaponManager>();
        }
    }

    private void UpdateHud()
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.UpdatePassiveSlots(pickedItems, maxPassiveItems);
    }
}
