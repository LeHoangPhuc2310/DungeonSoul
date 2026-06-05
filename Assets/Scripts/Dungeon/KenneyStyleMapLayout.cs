// DungeonSoul — KenneyStyleMapLayout.cs — Layout map dungeon kín, không cửa ra.

using UnityEngine;

public static class KenneyStyleMapLayout
{
    public const int Width = 25;
    public const int Height = 20;

    /// <summary>
    /// Arena kín 25x20: viền # liên tục, không cửa, chướng ngại dạng khối đặc.
    /// # tường, . sàn, @ spawn player, G mộ, T thùng.
    /// </summary>
    public static readonly string[] Rows = new[]
    {
        "#########################",
        "#.......................#",
        "#..GG...........GG......#",
        "#..GG...........GG......#",
        "#.......................#",
        "#.......TT.....TT.......#",
        "#.......TT.....TT.......#",
        "#.......................#",
        "#......####...####......#",
        "#......####...####......#",
        "#.......................#",
        "#..........@............#",
        "#.......................#",
        "#......####...####......#",
        "#......####...####......#",
        "#.......................#",
        "#.......TT.....TT.......#",
        "#.......TT.....TT.......#",
        "#.......................#",
        "#########################",
    };

    public static Vector2Int PlayerSpawnCell => FindChar('@', new Vector2Int(Width / 2, Height / 2));

    public static Vector2Int ChestCell => FindChar('C', new Vector2Int(Width - 3, Height / 2));

    public static bool IsWall(char c) => c == '#';

    public static bool IsFloor(char c)
    {
        return c == '.' || c == '@' || c == 'C' || c == 'P' || c == 'G' || c == 'T';
    }

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
