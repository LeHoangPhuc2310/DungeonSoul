// Thư mục TileData tương ứng từng PNG đã slice.

public static class AsepriteSheetFolders
{
    public const string Root = "Assets/Art/Tiles/Palettes/TileData";

    public static string FolderFor(string sheetFileName)
    {
        switch (sheetFileName)
        {
            case "walls_floor": return "WallsFloor";
            case "decorative_cracks_floor": return "DecorativeCracksFloor";
            case "decorative_cracks_walls": return "DecorativeCracksWalls";
            case "decorative_cracks_coasts_animation": return "DecorativeCracksCoasts";
            case "doors_lever_chest_animation": return "DoorsLeverChest";
            case "water_detilazation_v2": return "WaterDetilazation";
            case "Water_coasts_animation": return "WaterCoasts";
            case "trap_animation": return "TrapAnimation";
            case "fire_animation": return "FireAnimation";
            case "fire_animation2": return "FireAnimation2";
            case "Objects": return "Objects";
            default: return sheetFileName;
        }
    }

    public static string SheetForFolder(string folder)
    {
        switch (folder)
        {
            case "WallsFloor": return "walls_floor";
            case "DecorativeCracksFloor": return "decorative_cracks_floor";
            case "DecorativeCracksWalls": return "decorative_cracks_walls";
            case "DecorativeCracksCoasts": return "decorative_cracks_coasts_animation";
            case "DoorsLeverChest": return "doors_lever_chest_animation";
            case "WaterDetilazation": return "water_detilazation_v2";
            case "WaterCoasts": return "Water_coasts_animation";
            case "TrapAnimation": return "trap_animation";
            case "FireAnimation": return "fire_animation";
            case "FireAnimation2": return "fire_animation2";
            case "Objects": return "Objects";
            default: return folder;
        }
    }
}
