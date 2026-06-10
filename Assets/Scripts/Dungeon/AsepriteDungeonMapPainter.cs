// DungeonSoul — AsepriteDungeonMapPainter.cs — Vẽ map demo từ tile Assets/Tiles/TileData.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AsepriteDungeonMapPainter : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap decorTilemap;
    [SerializeField] private Tilemap hazardsTilemap;

    [Header("walls_floor.png — index ô 16x16")]
    [SerializeField] private int floorTileIndex = 118;
    [SerializeField] private int wallTileIndex = 14;
    [SerializeField] private int wallCapTileIndex = 14;

    [Header("doors_lever_chest_animation.png — cửa trang trí (-1 = bỏ)")]
    [SerializeField] private int doorDecorTileIndex = 0;

    [Header("Placement")]
    [SerializeField] private Vector2Int mapOrigin = Vector2Int.zero;
    [SerializeField] private bool movePlayerToSpawn = true;

    private const string WallsFloorFolder = "Assets/Tiles/AsepriteMap/TileData/walls_floor";
    private const string DoorsFolder = "Assets/Tiles/AsepriteMap/TileData/doors_lever_chest_animation";

    private readonly Dictionary<string, Tile[]> tileCaches = new Dictionary<string, Tile[]>();

    [ContextMenu("Paint ASEPRITE Demo Room")]
    public void PaintDemoRoom()
    {
        ResolveTilemaps();
        if (floorTilemap == null)
        {
            Debug.LogError("[AsepriteMap] Không tìm thấy FloorLayer.");
            return;
        }

        floorTilemap.ClearAllTiles();
        wallTilemap?.ClearAllTiles();
        decorTilemap?.ClearAllTiles();
        hazardsTilemap?.ClearAllTiles();

        bool[,] floor = new bool[DungeonDemoRoomLayout.Width, DungeonDemoRoomLayout.Height];
        for (int y = 0; y < DungeonDemoRoomLayout.Height; y++)
        {
            for (int x = 0; x < DungeonDemoRoomLayout.Width; x++)
            {
                char c = DungeonDemoRoomLayout.Get(x, y);
                Vector3Int cell = OriginCell(x, y);
                if (!DungeonDemoRoomLayout.IsFloor(c))
                    continue;

                floor[x, y] = true;
                SetTile(floorTilemap, cell, floorTileIndex, WallsFloorFolder);

                if (c == '-' && doorDecorTileIndex >= 0)
                    SetTile(decorTilemap, cell, doorDecorTileIndex, DoorsFolder);
            }
        }

        PaintWallsAroundFloors(floor);
        PlacePlayer();
        RefreshEnemySpawner();
        Debug.Log("[AsepriteMap] Đã vẽ demo room " + DungeonDemoRoomLayout.Width + "x" + DungeonDemoRoomLayout.Height);
    }

    private void PaintWallsAroundFloors(bool[,] floor)
    {
        if (wallTilemap == null)
            return;

        int w = DungeonDemoRoomLayout.Width;
        int h = DungeonDemoRoomLayout.Height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (floor[x, y])
                    continue;

                char layoutChar = DungeonDemoRoomLayout.Get(x, y);
                bool layoutWall = DungeonDemoRoomLayout.IsWall(layoutChar);
                if (!layoutWall && !HasFloorNeighbor(floor, x, y, w, h))
                    continue;

                int tile = layoutWall ? wallTileIndex : wallCapTileIndex;
                SetTile(wallTilemap, OriginCell(x, y), tile, WallsFloorFolder);
            }
        }
    }

    private static bool HasFloorNeighbor(bool[,] floor, int x, int y, int w, int h)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                    continue;
                if (floor[nx, ny])
                    return true;
            }
        }

        return false;
    }

    private void PlacePlayer()
    {
        if (!movePlayerToSpawn)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || floorTilemap == null)
            return;

        Vector2Int spawn = DungeonDemoRoomLayout.PlayerSpawnCell;
        player.transform.position = floorTilemap.GetCellCenterWorld(OriginCell(spawn.x, spawn.y));
    }

    private Vector3Int OriginCell(int x, int y) => new Vector3Int(mapOrigin.x + x, mapOrigin.y + y, 0);

    private void SetTile(Tilemap map, Vector3Int cell, int tileIndex, string folder)
    {
        if (map == null)
            return;

        TileBase tile = GetTile(tileIndex, folder);
        if (tile != null)
            map.SetTile(cell, tile);
    }

    private TileBase GetTile(int index, string folder)
    {
        if (index < 0)
            return null;

        if (!tileCaches.TryGetValue(folder, out Tile[] cache))
        {
            cache = LoadTilesFromFolder(folder);
            tileCaches[folder] = cache;
        }

        if (cache == null || index >= cache.Length)
            return null;

        return cache[index];
    }

    private static Tile[] LoadTilesFromFolder(string folder)
    {
#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Tile", new[] { folder });
        if (guids.Length == 0)
            return System.Array.Empty<Tile>();

        int maxIndex = -1;
        var byIndex = new Dictionary<int, Tile>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            Tile tile = UnityEditor.AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
                continue;

            int idx = ParseIndex(tile.name);
            if (idx < 0)
                continue;

            byIndex[idx] = tile;
            if (idx > maxIndex)
                maxIndex = idx;
        }

        Tile[] result = new Tile[maxIndex + 1];
        foreach (var kv in byIndex)
            result[kv.Key] = kv.Value;
        return result;
#else
        return System.Array.Empty<Tile>();
#endif
    }

    private static int ParseIndex(string tileName)
    {
        int underscore = tileName.LastIndexOf('_');
        if (underscore < 0 || underscore + 1 >= tileName.Length)
            return -1;
        return int.TryParse(tileName.Substring(underscore + 1), out int idx) ? idx : -1;
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
        if (hazardsTilemap == null)
            hazardsTilemap = grid.Find("HazardsLayer")?.GetComponent<Tilemap>();
    }

    private void RefreshEnemySpawner()
    {
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner == null)
            return;

#if UNITY_EDITOR
        UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(spawner);
        so.FindProperty("floorTilemap").objectReferenceValue = floorTilemap;
        so.FindProperty("wallTilemap").objectReferenceValue = wallTilemap;
        so.FindProperty("useManualSpawnPoints").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
#endif
        spawner.RefreshWalkablePositions();
    }
}
