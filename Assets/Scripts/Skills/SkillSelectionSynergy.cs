using UnityEngine;

/// <summary>Phát hiện synergy evolve giữa passive và vũ khí đang có.</summary>
public static class SkillSelectionSynergy
{
    public static bool HasSynergy(SkillSelectionChoice choice)
    {
        if (choice == null)
            return false;

        switch (choice.kind)
        {
            case SkillSelectionChoiceKind.PassiveItem:
                return PassiveCanEvolveOwnedWeapon(choice.passiveItem);
            case SkillSelectionChoiceKind.WeaponPickup:
                return WeaponCanEvolveWithOwnedPassive(choice.weaponType);
            default:
                return false;
        }
    }

    /// <summary>Passive có combo evolve và người chơi đang giữ vũ khí tương ứng.</summary>
    public static bool PassiveCanEvolveOwnedWeapon(PassiveItemData passive)
    {
        if (passive == null || !passive.HasEvolveCombo || WeaponManager.Instance == null)
            return false;

        if (!WeaponManager.Instance.HasWeapon(passive.evolveTargetWeapon))
            return false;

        PassivePick pick = PassiveItemManager.Instance != null
            ? PassiveItemManager.Instance.FindPick(passive)
            : null;

        // Sáng khi đã có vũ khí + (passive max hoặc đang nâng cấp passive đó)
        return pick != null || passive.enablesWeaponEvolve;
    }

    /// <summary>Vũ khí có thể tiến hóa nhờ passive max đang giữ.</summary>
    public static bool WeaponCanEvolveWithOwnedPassive(WeaponType weapon)
    {
        if (PassiveItemManager.Instance == null || WeaponManager.Instance == null)
            return false;

        var picks = PassiveItemManager.Instance.PickedItems;
        for (int i = 0; i < picks.Count; i++)
        {
            PassivePick pick = picks[i];
            if (pick?.data == null || !pick.IsMaxed || !pick.data.HasEvolveCombo)
                continue;

            if (pick.data.evolveTargetWeapon == weapon && WeaponManager.Instance.HasWeapon(weapon))
                return true;
        }

        return false;
    }

    /// <summary>Nhãn công thức tiến hóa hiển thị trên thẻ (kiểu VS).</summary>
    public static string GetEvolutionLabel(SkillSelectionChoice choice)
    {
        if (choice == null || !HasSynergy(choice))
            return string.Empty;

        switch (choice.kind)
        {
            case SkillSelectionChoiceKind.PassiveItem:
                return FormatEvolveRecipe(choice.passiveItem);
            case SkillSelectionChoiceKind.WeaponPickup:
                return FormatWeaponEvolveRecipe(choice.weaponType);
            default:
                return string.Empty;
        }
    }

    private static string FormatEvolveRecipe(PassiveItemData passive)
    {
        if (passive == null || !passive.HasEvolveCombo)
            return string.Empty;

        string baseW = WeaponNames.Display(passive.evolveTargetWeapon);
        string result = WeaponNames.Display(passive.evolveResultWeapon);
        return baseW + " + " + passive.displayName + " → " + result;
    }

    private static string FormatWeaponEvolveRecipe(WeaponType weapon)
    {
        if (PassiveItemManager.Instance == null)
            return string.Empty;

        var picks = PassiveItemManager.Instance.PickedItems;
        for (int i = 0; i < picks.Count; i++)
        {
            PassivePick pick = picks[i];
            if (pick?.data == null || !pick.IsMaxed || !pick.data.HasEvolveCombo)
                continue;
            if (pick.data.evolveTargetWeapon != weapon)
                continue;

            return WeaponNames.Display(weapon) + " + " + pick.data.displayName
                + " → " + WeaponNames.Display(pick.data.evolveResultWeapon);
        }

        return string.Empty;
    }
}
