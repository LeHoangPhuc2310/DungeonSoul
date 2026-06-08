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
}
