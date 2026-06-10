// DungeonSoul — DungeonDemoRoomLayout.cs — Map demo nhỏ: 1 phòng + cửa thông.

using UnityEngine;

public static class DungeonDemoRoomLayout
{
    public const int Width = 15;
    public const int Height = 11;

    /// <summary>
    /// # tường, . sàn, @ spawn player, - cửa (hở trên viền tường trong).
    /// </summary>
    public static readonly string[] Rows =
    {
        "###############",
        "#.............#",
        "#..#########..#",
        "#..#.......#..#",
        "#..#...-...#..#",
        "#..#..@...#..#",
        "#..#.......#..#",
        "#..###...###..#",
        "#.............#",
        "#.............#",
        "###############",
    };

    public static Vector2Int PlayerSpawnCell => FindChar('@', new Vector2Int(Width / 2, Height / 2));

    public static bool IsWall(char c) => c == '#';

    public static bool IsFloor(char c) => c == '.' || c == '@' || c == '-' || c == 'D';

    public static char Get(int x, int y)
    {
        if (x < 0 || y < 0 || y >= Rows.Length)
            return '#';
        string row = Rows[y];
        if (x >= row.Length)
            return '#';
        return row[x];
    }

    private static Vector2Int FindChar(char target, Vector2Int fallback)
    {
        for (int y = 0; y < Rows.Length; y++)
        {
            string row = Rows[y];
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] == target)
                    return new Vector2Int(x, y);
            }
        }

        return fallback;
    }
}
