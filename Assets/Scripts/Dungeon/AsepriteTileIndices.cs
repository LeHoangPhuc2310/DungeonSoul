// DungeonSoul — Chỉ số tile 16x16 (row-major) theo tên sprite trong ASEPRITE_MAP/PNG.
// Tham chiếu từ Dungeon1.tmx + gợi ý pack Craftpix.

public static class AsepriteTileIndices
{
    public const int PixelsPerUnit = 16;

    // walls_floor.png (17 cột) — chỉ số khớp Dungeon1.tmx (firstgid 377)
    public const int Floor = 138;       // TMX gid 515
    public const int FloorAltA = 139;
    public const int FloorAltB = 250;   // TMX gid 627 (sàn tối hơn)
    public const int FloorCrackDecor = 250;
    public const int Wall = 36;         // TMX gid 413
    public const int WallCap = 14;
    public const int WallCorner = 0;

    // Objects.png (24 cột)
    public const int ChestClosed = 5;
    public const int ChestOpen = 6;
    public const int Barrel = 39;
    public const int Crate = 40;
    public const int GoldPile = 113;
    public const int Key = 12;
    public const int Ladder = 24;
    public const int Pot = 174;

    // doors_lever_chest_animation.png
    public const int WoodDoor = 18;

    // fire_animation.png — frame đầu đuốc
    public const int Torch = 0;

    // trap_animation.png (TMX tile 14)
    public const int Trap = 14;

    // Water_coasts_animation.png — tile bờ
    public const int WaterCoast = 30;
    // water_detilazation_v2.png — mặt nước (frame gốc anim, TMX tile 40)
    public const int WaterFill = 40;

    // decorative_cracks_floor.png — vết nứt sàn (tùy chọn)
    public const int FloorCrack = 4;
}
