using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Vẽ dungeon chi tiết từ tile ASEPRITE_MAP/PNG.</summary>
public class FullDungeonMapPainter : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap decorTilemap;
    [SerializeField] private Tilemap shadowTilemap;

    [Header("Placement")]
    [SerializeField] private Vector2Int mapOrigin = new Vector2Int(-6, -8);
    [SerializeField] private bool movePlayerToSpawn = true;

    private readonly Dictionary<string, TileBase[]> caches = new Dictionary<string, TileBase[]>();
    private readonly Dictionary<string, TileBase> namedTiles = new Dictionary<string, TileBase>();

    private static readonly string TileRoot = AsepriteSheetFolders.Root;
    private static readonly string AnimRoot = AsepriteAnimatedTileDefs.AnimFolder;

    [ContextMenu("Paint Full ASEPRITE Dungeon")]
    public void PaintFullDungeon() => PaintDetailedDungeon();

    [ContextMenu("Paint Detailed ASEPRITE Dungeon")]
    public void PaintDetailedDungeon()
    {
        ResolveTilemaps();
        ClearAll();
        LoadNamedAnimatedTiles();

        bool[,] walkable = new bool[DetailedDungeonLayout.Width, DetailedDungeonLayout.Height];
        CarveWalkable(walkable);

        PaintFloors(walkable);
        PaintWalls(walkable);
        PaintShadows(walkable);
        PaintWater();
        PaintDecorProps();
        PlacePlayer();
        RefreshEnemySpawner();

        Debug.Log("[FullDungeon] Đã vẽ dungeon chi tiết " + DetailedDungeonLayout.Width + "x"
            + DetailedDungeonLayout.Height + " — 6 phòng + boss + hành lang gấp khúc.");
    }

    private void ClearAll()
    {
        floorTilemap?.ClearAllTiles();
        wallTilemap?.ClearAllTiles();
        decorTilemap?.ClearAllTiles();
        shadowTilemap?.ClearAllTiles();
        caches.Clear();
        namedTiles.Clear();
    }

    private void CarveWalkable(bool[,] walkable)
    {
        for (int y = 0; y < DetailedDungeonLayout.Height; y++)
        {
            for (int x = 0; x < DetailedDungeonLayout.Width; x++)
            {
                if (DetailedDungeonLayout.IsWalkableFloor(DetailedDungeonLayout.Get(x, y)))
                    walkable[x, y] = true;
            }
        }
    }

    private void PaintFloors(bool[,] walkable)
    {
        for (int y = 0; y < DetailedDungeonLayout.Height; y++)
        {
            for (int x = 0; x < DetailedDungeonLayout.Width; x++)
            {
                char c = DetailedDungeonLayout.Get(x, y);
                if (!DetailedDungeonLayout.NeedsFloorTile(c) || c == 'W')
                    continue;

                int idx = c switch
                {
                    'f' => AsepriteTileIndices.FloorAltA,
                    'd' => AsepriteTileIndices.FloorAltB,
                    _ => AsepriteTileIndices.Floor
                };
                SetTile(floorTilemap, Cell(x, y), idx, "WallsFloor");
            }
        }
    }

    private void PaintWalls(bool[,] walkable)
    {
        if (wallTilemap == null)
            return;

        for (int y = 0; y < DetailedDungeonLayout.Height; y++)
        {
            for (int x = 0; x < DetailedDungeonLayout.Width; x++)
            {
                if (walkable[x, y])
                    continue;

                char c = DetailedDungeonLayout.Get(x, y);
                if (c != '#' && !HasAdjacentWalkable(walkable, x, y))
                    continue;

                int idx = AsepriteTileIndices.Wall;
                SetTile(wallTilemap, Cell(x, y), idx, "WallsFloor");
            }
        }
    }

    private void PaintShadows(bool[,] walkable)
    {
        if (shadowTilemap == null)
            return;

        for (int y = 0; y < DetailedDungeonLayout.Height; y++)
        {
            for (int x = 0; x < DetailedDungeonLayout.Width; x++)
            {
                if (!walkable[x, y])
                    continue;

                int ny = y - 1;
                if (ny >= 0 && !walkable[x, ny])
                    SetTile(shadowTilemap, Cell(x, y), AsepriteTileIndices.FloorCrackDecor, "WallsFloor");
            }
        }
    }

    private void PaintWater()
    {
        if (decorTilemap == null)
            return;

        var waterCells = new HashSet<Vector2Int>();
        foreach (Vector2Int w in DetailedDungeonLayout.WaterCells())
            waterCells.Add(w);

        foreach (Vector2Int w in waterCells)
        {
            bool border = false;
            for (int dy = -1; dy <= 1 && !border; dy++)
            {
                for (int dx = -1; dx <= 1 && !border; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    var n = new Vector2Int(w.x + dx, w.y + dy);
                    if (!waterCells.Contains(n))
                        border = true;
                }
            }

            if (border)
                SetAnimOrStatic(decorTilemap, Cell(w.x, w.y), "WaterCoast", AsepriteTileIndices.WaterCoast, "water_coast");
            else
                SetAnimOrStatic(decorTilemap, Cell(w.x, w.y), "WaterDetilazation", AsepriteTileIndices.WaterFill, "water_ripple");
        }
    }

    private void PaintDecorProps()
    {
        if (decorTilemap == null)
            return;

        for (int y = 0; y < DetailedDungeonLayout.Height; y++)
        {
            for (int x = 0; x < DetailedDungeonLayout.Width; x++)
            {
                char c = DetailedDungeonLayout.Get(x, y);
                Vector3Int cell = Cell(x, y);

                switch (c)
                {
                    case 'C':
                        SetTile(decorTilemap, cell, AsepriteTileIndices.ChestClosed, "Objects");
                        break;
                    case 'D':
                        SetAnimOrStatic(decorTilemap, cell, "DoorsLeverChest", AsepriteTileIndices.WoodDoor, "wood_door");
                        break;
                    case 'F':
                        SetAnimOrStatic(decorTilemap, cell, "FireAnimation", AsepriteTileIndices.Torch, "fire_torch");
                        break;
                    case 'T':
                        SetAnimOrStatic(decorTilemap, cell, "TrapAnimation", AsepriteTileIndices.Trap, "trap_spikes");
                        break;
                    case 'R':
                        SetTile(decorTilemap, cell, AsepriteTileIndices.Barrel, "Objects");
                        break;
                    case 'G':
                        SetTile(decorTilemap, cell, AsepriteTileIndices.GoldPile, "Objects");
                        break;
                    case 'K':
                        SetTile(decorTilemap, cell, AsepriteTileIndices.Key, "Objects");
                        break;
                    case 'L':
                        SetTile(decorTilemap, cell, AsepriteTileIndices.Ladder, "Objects");
                        break;
                    case 'P':
                        SetTile(decorTilemap, cell, AsepriteTileIndices.Pot, "Objects");
                        break;
                }
            }
        }
    }

    private void SetAnimOrStatic(Tilemap map, Vector3Int cell, string folder, int index, string animKey)
    {
        if (namedTiles.TryGetValue(animKey, out TileBase named) && named != null)
        {
            map.SetTile(cell, named);
            return;
        }

        TileBase tile = GetTile(index, folder + "_anim") ?? GetTile(index, folder);
        if (tile != null)
            map.SetTile(cell, tile);
    }

    private void LoadNamedAnimatedTiles()
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AnimatedTile", new[] { AnimRoot });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            TileBase tile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null)
                continue;
            string key = System.IO.Path.GetFileNameWithoutExtension(path);
            namedTiles[key] = tile;
        }
#endif
    }

    private void PlacePlayer()
    {
        if (!movePlayerToSpawn)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || floorTilemap == null)
            return;

        Vector2Int spawn = DetailedDungeonLayout.PlayerSpawn;
        player.transform.position = floorTilemap.GetCellCenterWorld(Cell(spawn.x, spawn.y));
    }

    private Vector3Int Cell(int x, int y) => new Vector3Int(mapOrigin.x + x, mapOrigin.y + y, 0);

    private void SetTile(Tilemap map, Vector3Int cell, int index, string folder)
    {
        if (map == null)
            return;

        TileBase tile = GetTile(index, folder);
        if (tile != null)
            map.SetTile(cell, tile);
    }

    private TileBase GetTile(int index, string folder)
    {
        if (index < 0)
            return null;

        if (!caches.TryGetValue(folder, out TileBase[] cache))
        {
            cache = LoadTiles(folder);
            caches[folder] = cache;
        }

        if (cache == null || index >= cache.Length)
            return null;

        return cache[index];
    }

    private static TileBase[] LoadTiles(string folder)
    {
#if UNITY_EDITOR
        string path = $"{TileRoot}/{folder}";
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TileBase", new[] { path });
        if (guids.Length == 0)
            return System.Array.Empty<TileBase>();

        int max = -1;
        var byIndex = new Dictionary<int, TileBase>();
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            TileBase tile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
            if (tile == null)
                continue;

            int idx = ParseIndex(tile.name);
            if (idx < 0)
                continue;

            byIndex[idx] = tile;
            if (idx > max)
                max = idx;
        }

        TileBase[] result = new TileBase[max + 1];
        foreach (var kv in byIndex)
            result[kv.Key] = kv.Value;
        return result;
#else
        return System.Array.Empty<TileBase>();
#endif
    }

    private static int ParseIndex(string name)
    {
        int u = name.LastIndexOf('_');
        if (u < 0 || u + 1 >= name.Length)
            return -1;
        return int.TryParse(name.Substring(u + 1), out int idx) ? idx : -1;
    }

    private static bool HasAdjacentWalkable(bool[,] walkable, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= DetailedDungeonLayout.Width || ny >= DetailedDungeonLayout.Height)
                    continue;
                if (walkable[nx, ny])
                    return true;
            }
        }

        return false;
    }

    private void ResolveTilemaps()
    {
        Transform grid = GameObject.Find("DungeonGrid")?.transform ?? transform;
        if (floorTilemap == null)
            floorTilemap = grid.Find("FloorLayer")?.GetComponent<Tilemap>();
        if (wallTilemap == null)
            wallTilemap = grid.Find("WallLayer")?.GetComponent<Tilemap>();
        if (decorTilemap == null)
            decorTilemap = grid.Find("DecorLayer")?.GetComponent<Tilemap>();
        if (shadowTilemap == null)
            shadowTilemap = grid.Find("ShadowLayer")?.GetComponent<Tilemap>();
    }

    private void RefreshEnemySpawner()
    {
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner == null)
            return;

#if UNITY_EDITOR
        var so = new UnityEditor.SerializedObject(spawner);
        so.FindProperty("floorTilemap").objectReferenceValue = floorTilemap;
        so.FindProperty("wallTilemap").objectReferenceValue = wallTilemap;
        so.FindProperty("useManualSpawnPoints").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        spawner.RefreshWalkablePositions();
    }
}
