using System.Collections.Generic;
using UnityEngine;

/// <summary>Dungeon 6 phòng bất đối xứng + boss + hành lang gấp khúc (70×42).</summary>
public static class DetailedDungeonLayout
{
    public const int Width = 70;
    public const int Height = 42;

    public static readonly Vector2Int PlayerSpawn = new Vector2Int(8, 6);

    private static readonly char[,] Grid = Build();

    public static char Get(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return '#';
        return Grid[x, y];
    }

    public static bool IsWalkableFloor(char c) =>
        c is '.' or '@' or '=' or 'f' or 'd';

    public static bool IsFloor(char c) => IsWalkableFloor(c);

    public static bool IsWall(char c) => c == '#';

    /// <summary>Ô cần tile sàn (kể cả dưới decor, trừ void).</summary>
    public static bool NeedsFloorTile(char c) => c != '#';

    public static IEnumerable<Vector2Int> WaterCells()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (Grid[x, y] == 'W')
                    yield return new Vector2Int(x, y);
    }

    public static IEnumerable<Vector2Int> WalkableCells()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (IsWalkableFloor(Grid[x, y]))
                    yield return new Vector2Int(x, y);
    }

    private static char[,] Build()
    {
        char[,] g = new char[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                g[x, y] = '#';

        // 1 — Entry (nhỏ, góc dưới-trái) 10×7
        CarveRoom(g, 3, 3, 12, 9);
        g[8, 6] = '@';

        // 2 — Armory (rộng, trên-trái) 16×9
        CarveRoom(g, 2, 22, 17, 30);
        PlaceCluster(g, 5, 25, 'R', 3);
        PlaceCluster(g, 12, 27, 'P', 2);
        g[15, 28] = 'C';

        // 3 — Shrine (hẹp dài) 8×14 dọc
        CarveRoom(g, 20, 18, 27, 31);
        g[23, 20] = 'K';
        g[24, 29] = 'F';

        // 4 — Treasury (vuông vừa) 11×8
        CarveRoom(g, 30, 24, 40, 31);
        PlaceCluster(g, 33, 26, 'G', 4);
        g[38, 27] = 'C';

        // 5 — Grotto + hồ nước (lệch phải) 14×11
        CarveRoom(g, 18, 8, 31, 18);
        CarvePond(g, 24, 11, 29, 15);
        PlaceCluster(g, 20, 10, 'R', 2);
        g[19, 16] = 'F';

        // 6 — Gauntlet (hành lang phòng tù) 13×7
        CarveRoom(g, 34, 10, 46, 16);
        g[38, 12] = 'T';
        g[42, 14] = 'T';
        PlaceCluster(g, 44, 11, 'R', 3);

        // 7 — Boss arena (lớn, cuối map) 20×16
        CarveRoom(g, 48, 6, 67, 21);
        g[58, 9] = 'C';
        PlaceCluster(g, 52, 14, 'G', 3);
        PlaceCluster(g, 62, 16, 'G', 2);
        PlaceCluster(g, 55, 18, 'R', 2);
        g[60, 19] = 'L';
        g[56, 12] = 'T';

        // Hành lang gấp khúc (không đối xứng)
        CarveCorridor(g, 13, 6, 17, 6);   // entry → grotto
        CarveCorridor(g, 17, 6, 17, 12);
        CarveCorridor(g, 17, 12, 20, 12);
        g[16, 6] = 'D';

        CarveCorridor(g, 12, 9, 12, 14);
        CarveCorridor(g, 12, 14, 18, 14);
        g[12, 11] = 'D';

        CarveCorridor(g, 27, 15, 27, 18);
        CarveCorridor(g, 27, 15, 34, 15);
        g[27, 16] = 'D';

        CarveCorridor(g, 31, 18, 31, 22);
        CarveCorridor(g, 28, 22, 31, 22);
        g[30, 21] = 'D';

        CarveCorridor(g, 40, 22, 46, 22);
        CarveCorridor(g, 46, 16, 46, 22);
        g[43, 22] = 'D';

        CarveCorridor(g, 46, 13, 48, 13);
        g[47, 13] = 'D';

        CarveCorridor(g, 17, 30, 17, 33);
        CarveCorridor(g, 17, 33, 22, 33);
        g[17, 31] = 'D';

        // Bẫy hành lang
        g[14, 14] = 'T';
        g[22, 14] = 'T';
        g[35, 15] = 'T';
        g[41, 20] = 'T';

        // Cửa các lối
        g[19, 12] = 'D';
        g[34, 15] = 'D';

        StampTorchesAlongWalls(g);
        StampFloorVariants(g);

        return g;
    }

    private static void CarveRoom(char[,] g, int x0, int y0, int x1, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (InBounds(x, y))
                    g[x, y] = '.';
    }

    private static void CarveCorridor(char[,] g, int x0, int y0, int x1, int y1)
    {
        int dx = x0 <= x1 ? 1 : -1;
        int dy = y0 <= y1 ? 1 : -1;
        for (int x = x0; x != x1 + dx; x += dx)
            if (InBounds(x, y0))
                g[x, y0] = '.';
        for (int y = y0; y != y1 + dy; y += dy)
            if (InBounds(x1, y))
                g[x1, y] = '.';
    }

    private static void CarvePond(char[,] g, int x0, int y0, int x1, int y1)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (InBounds(x, y))
                    g[x, y] = 'W';
    }

    private static void PlaceCluster(char[,] g, int cx, int cy, char marker, int count)
    {
        int placed = 0;
        for (int r = 0; r < 3 && placed < count; r++)
        {
            for (int dx = -1; dx <= 1 && placed < count; dx++)
            {
                for (int dy = -1; dy <= 1 && placed < count; dy++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (!InBounds(x, y) || g[x, y] != '.')
                        continue;
                    g[x, y] = marker;
                    placed++;
                }
            }
        }
    }

    private static void StampTorchesAlongWalls(char[,] g)
    {
        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                if (!IsWalkableFloor(g[x, y]))
                    continue;
                if (!IsWallAdjacent(g, x, y))
                    continue;
                if ((x + y) % 5 != 0)
                    continue;
                if (g[x, y] != '.')
                    continue;
                g[x, y] = 'F';
            }
        }
    }

    private static void StampFloorVariants(char[,] g)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (g[x, y] != '.')
                    continue;
                int h = Hash(x, y);
                if (h % 11 == 0)
                    g[x, y] = 'f';
                else if (h % 17 == 0)
                    g[x, y] = 'd';
            }
        }
    }

    private static bool IsWallAdjacent(char[,] g, int x, int y)
    {
        return IsWall(g[x - 1, y]) || IsWall(g[x + 1, y]) || IsWall(g[x, y - 1]) || IsWall(g[x, y + 1]);
    }

    private static bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    private static int Hash(int x, int y) => (x * 73856093) ^ (y * 19349663);
}
