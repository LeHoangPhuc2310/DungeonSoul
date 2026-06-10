using UnityEngine;

// DungeonSoul — Bố cục dungeon top-down: 5 phòng + boss + hành lang.
// Ký hiệu: # tường/void, . sàn, @ spawn, = lối cửa, W nước, C rương, D cửa (decor),
// F đuốc, T bẫy, R thùng, G vàng, P chum, K key, L thang.

public static class FullDungeonLayout
{
    public const int Width = 50;
    public const int Height = 28;

    public static readonly Vector2Int PlayerSpawn = new Vector2Int(6, 5);

    private static readonly char[,] Grid = Build();

    public static char Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return '#';
        return Grid[x, y];
    }

    public static bool IsFloor(char c) =>
        c != '#';

    public static bool IsWall(char c) => c == '#';

    private static char[,] Build()
    {
        char[,] g = new char[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                g[x, y] = '#';

        // Phòng 1 — spawn (12x8)
        FillRect(g, 2, 2, 13, 9, '.');
        g[6, 5] = '@';
        g[3, 3] = 'F';
        g[3, 8] = 'F';
        g[11, 6] = 'R';

        // Hành lang → phòng 2
        FillRect(g, 14, 5, 17, 6, '.');
        g[15, 5] = 'D';
        g[16, 5] = '=';

        // Phòng 2 (12x10) — bẫy + thùng
        FillRect(g, 18, 2, 29, 11, '.');
        g[22, 4] = 'T';
        g[26, 7] = 'R';
        g[27, 8] = 'P';
        g[20, 3] = 'F';
        g[28, 3] = 'F';

        // Hành lang xuống phòng 3
        FillRect(g, 23, 12, 24, 15, '.');
        g[23, 13] = 'D';

        // Phòng 3 (14x9) — hồ nước góc phải
        FillRect(g, 18, 16, 31, 24, '.');
        FillRect(g, 27, 18, 30, 21, 'W');
        g[25, 20] = 'R';
        g[21, 22] = 'G';
        g[19, 17] = 'F';

        // Hành lang trái → phòng 4
        FillRect(g, 12, 20, 17, 21, '.');
        g[14, 20] = 'D';

        // Phòng 4 (12x10) — rương + vàng
        FillRect(g, 2, 14, 13, 23, '.');
        g[10, 15] = 'C';
        g[5, 18] = 'G';
        g[7, 20] = 'R';
        g[4, 16] = 'K';
        g[3, 15] = 'F';
        g[11, 22] = 'F';

        // Hành lang → boss
        FillRect(g, 32, 12, 35, 13, '.');
        g[33, 12] = 'D';

        // Phòng boss lớn (12x17)
        FillRect(g, 36, 7, 47, 23, '.');
        g[42, 10] = 'C';
        g[40, 14] = 'G';
        g[44, 14] = 'G';
        g[38, 18] = 'R';
        g[45, 20] = 'P';
        g[42, 22] = 'L';
        g[37, 8] = 'F';
        g[46, 8] = 'F';
        g[37, 22] = 'F';
        g[46, 22] = 'F';
        g[41, 16] = 'T';

        // Viền tường ngoài (khung map)
        for (int x = 0; x < Width; x++)
        {
            g[x, 0] = '#';
            g[x, Height - 1] = '#';
        }

        for (int y = 0; y < Height; y++)
        {
            g[0, y] = '#';
            g[Width - 1, y] = '#';
        }

        // Lớp ngoài thành sàn trang trí (một ô padding)
        for (int x = 1; x < Width - 1; x++)
        {
            if (g[x, 1] == '#')
                g[x, 1] = '.';
            if (g[x, Height - 2] == '#')
                g[x, Height - 2] = '.';
        }

        for (int y = 1; y < Height - 1; y++)
        {
            if (g[1, y] == '#')
                g[1, y] = '.';
            if (g[Width - 2, y] == '#')
                g[Width - 2, y] = '.';
        }

        return g;
    }

    private static void FillRect(char[,] g, int x0, int y0, int x1, int y1, char c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                g[x, y] = c;
    }
}
