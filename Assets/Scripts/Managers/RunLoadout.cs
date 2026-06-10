// DungeonSoul — RunLoadout.cs — Tên/mô tả vũ khí (UI tooltip, phần thưởng phép cho Mage).

using UnityEngine;

public static class RunLoadout
{
    private const string KeyWeapon = "ds_starting_weapon";

    /// <summary>Vũ khí khởi đầu đã chọn (mặc định IronBow nếu chưa chọn).</summary>
    public static WeaponType StartingWeapon
    {
        get => (WeaponType)Mathf.Clamp(PlayerPrefs.GetInt(KeyWeapon, (int)WeaponType.IronBow),
            0, System.Enum.GetValues(typeof(WeaponType)).Length - 1);
        set
        {
            PlayerPrefs.SetInt(KeyWeapon, (int)value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>5 vũ khí khởi đầu cho người chơi chọn (tầm xa).</summary>
    public static readonly WeaponType[] StartingChoices =
    {
        WeaponType.IronBow,
        WeaponType.FireStaff,
        WeaponType.FrostWand,
        WeaponType.PoisonDagger,
        WeaponType.ThunderRod
    };

    private static readonly WeaponType[] MeleeStartingChoices =
    {
        WeaponType.PoisonDagger
    };

    /// <summary>Vũ khí khởi đầu theo lớp — chiến binh chỉ được cận chiến.</summary>
    public static WeaponType[] GetStartingChoices(HeroType hero)
    {
        return WeaponStyleUtil.IsMeleeHero(hero) ? MeleeStartingChoices : StartingChoices;
    }

    /// <summary>Đảm bảo vũ khí đã lưu hợp lệ với lớp nhân vật.</summary>
    public static WeaponType GetValidStartingWeapon(HeroType hero, WeaponType preferred)
    {
        if (WeaponStyleUtil.HeroCanUseWeapon(hero, preferred))
            return preferred;

        WeaponType[] choices = GetStartingChoices(hero);
        return choices.Length > 0 ? choices[0] : WeaponStyleUtil.DefaultMeleeStarterWeapon;
    }

    public static string DisplayName(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.IronBow: return "Cung Sắt";
            case WeaponType.StormBow: return "Cung Bão";
            case WeaponType.FireStaff: return "Gậy Lửa";
            case WeaponType.DragonStaff: return "Gậy Rồng";
            case WeaponType.FrostWand: return "Đũa Băng";
            case WeaponType.BlizzardWand: return "Đũa Bão Tuyết";
            case WeaponType.PoisonDagger: return "Dao Độc";
            case WeaponType.DeathDagger: return "Dao Tử Thần";
            case WeaponType.HolyCross: return "Thánh Giá";
            case WeaponType.HolyNova: return "Thánh Quang";
            case WeaponType.ThunderRod: return "Trượng Sấm";
            case WeaponType.ZeusRod: return "Trượng Zeus";
            default: return type.ToString();
        }
    }

    public static string Description(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.IronBow: return "Bắn mũi tên nhanh, tầm xa. Cân bằng, dễ dùng.";
            case WeaponType.FireStaff: return "Phóng cầu lửa gây sát thương cháy theo thời gian.";
            case WeaponType.FrostWand: return "Đạn băng làm chậm kẻ địch trúng đòn.";
            case WeaponType.PoisonDagger: return "Cận chiến — gây độc, sát thương lan theo thời gian.";
            case WeaponType.ThunderRod: return "Tia sét xuyên qua nhiều kẻ địch một hàng.";
            default: return "Vũ khí khởi đầu.";
        }
    }
}
