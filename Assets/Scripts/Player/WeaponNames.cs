/// <summary>Tên hiển thị vũ khí — dùng chung HUD / synergy / tooltip.</summary>
public static class WeaponNames
{
    public static string Display(WeaponType type)
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
            case WeaponType.ThunderRod: return "Gậy Sấm";
            case WeaponType.ZeusRod: return "Gậy Zeus";
            default: return type.ToString();
        }
    }
}
