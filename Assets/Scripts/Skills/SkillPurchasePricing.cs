using UnityEngine;

/// <summary>Giá mua skill/passive/vũ khí bằng xu run — đồ xịn / hiếm thì đắt hơn.</summary>
public static class SkillPurchasePricing
{
    public static bool IsPurchasable(SkillSelectionChoice choice)
    {
        if (choice == null)
            return false;

        return choice.kind switch
        {
            SkillSelectionChoiceKind.SkillUpgrade => choice.skill != null,
            SkillSelectionChoiceKind.WeaponPickup => true,
            SkillSelectionChoiceKind.PassiveItem => choice.passiveItem != null,
            _ => false
        };
    }

    public static int GetCost(SkillSelectionChoice choice)
    {
        if (choice == null || !IsPurchasable(choice))
            return 0;

        if (choice.purchaseCoinCost > 0)
            return choice.purchaseCoinCost;

        SkillSelectionConfig cfg = SkillSelectionConfig.Get();
        SkillRarity rarity = ResolveRarity(choice);
        float mult = rarity switch
        {
            SkillRarity.Legendary => cfg.legendaryPriceMult,
            SkillRarity.Epic => cfg.epicPriceMult,
            SkillRarity.Rare => cfg.rarePriceMult,
            _ => cfg.commonPriceMult
        };

        int cost = Mathf.RoundToInt(cfg.skillPurchaseCoinCost * mult);
        if (choice.kind == SkillSelectionChoiceKind.WeaponPickup)
        {
            int copies = WeaponManager.Instance != null
                ? WeaponManager.Instance.GetWeaponCopies(choice.weaponType)
                : 0;
            if (copies > 0)
                cost = Mathf.RoundToInt(cost * 1.15f);
        }

        return Mathf.Max(cfg.minPurchaseCoinCost, cost);
    }

    public static SkillRarity ResolveRarity(SkillSelectionChoice choice)
    {
        if (choice == null)
            return SkillRarity.Common;

        if (choice.kind == SkillSelectionChoiceKind.SkillUpgrade && choice.skill != null)
            return choice.skill.rarity;

        if (choice.kind == SkillSelectionChoiceKind.PassiveItem && choice.passiveItem != null)
            return choice.passiveItem.rarity;

        if (choice.kind == SkillSelectionChoiceKind.WeaponPickup)
            return ResolveWeaponRarity(choice.weaponType);

        return SkillRarity.Common;
    }

    private static SkillRarity ResolveWeaponRarity(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.StormBow:
            case WeaponType.DragonStaff:
            case WeaponType.BlizzardWand:
            case WeaponType.DeathDagger:
            case WeaponType.HolyNova:
            case WeaponType.ZeusRod:
                return SkillRarity.Legendary;
            case WeaponType.IronBow:
            case WeaponType.FireStaff:
            case WeaponType.FrostWand:
            case WeaponType.PoisonDagger:
            case WeaponType.HolyCross:
            case WeaponType.ThunderRod:
                return SkillRarity.Rare;
            default:
                return SkillRarity.Epic;
        }
    }

    public static Color GetPriceColor(SkillRarity rarity)
    {
        return rarity switch
        {
            SkillRarity.Legendary => new Color(1f, 0.88f, 0.35f, 1f),
            SkillRarity.Epic => new Color(0.82f, 0.62f, 1f, 1f),
            SkillRarity.Rare => new Color(0.55f, 0.92f, 0.65f, 1f),
            _ => new Color(0.88f, 0.9f, 0.95f, 1f)
        };
    }
}
