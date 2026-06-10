// Frame indices cho AnimatedTile — khớp Dungeon1.tmx (columns theo sheet).

public static class AsepriteAnimatedTileDefs
{
    public const float AnimFps = 8f;

    public static readonly int[] FireTorch = { 0, 33, 66, 99, 132, 165 };
    public static readonly int[] TrapSpikes = { 14, 92, 170, 248, 326, 248, 170, 92 };
    public static readonly int[] WaterRipple = { 40, 521, 1002, 1483, 1964, 2445 };
    public static readonly int[] WaterCoast = { 30, 59, 88 };
    public static readonly int[] WoodDoor = { 18, 48, 78 };

    public const string AnimFolder = "Assets/Tiles/Animated";
}
