// DungeonSoul — WallsFloorArenaPainter.cs — Vẽ lại arena KenneyStyleMapLayout bằng tileset walls_floor.png.
//
// Dùng chung layout arena 25x20 (KenneyStyleMapLayout) nhưng lấy tile từ bộ
// walls_floor.png đã được slice 16x16 (Assets/Art/Tiles/Palettes/TileData/WallsFloor).
// Tile được tham chiếu THEO TÊN sprite gốc (walls_floor_<index>) nên không phụ thuộc
// thứ tự sắp xếp của tile asset.

using UnityEngine;
using UnityEngine.Tilemaps;

public class WallsFloorArenaPainter : MonoBehaviour
{
    [Header("Tilemaps (tự tìm trong DungeonGrid nếu để trống)")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap decorTilemap;

    [Header("Sprite index trong walls_floor.png (grid 16x16, đếm trái→phải, trên→xuống)")]
    [Tooltip("Ô sàn lát nền. (đoán mặc định; chỉnh sau khi xem slice trong Unity)")]
    [SerializeField] private int floorTileIndex = 118;
    [Tooltip("Ô tường thân.")]
    [SerializeField] private int wallTileIndex = 14;
    [Tooltip("Ô tường viền/đỉnh (cạnh giáp sàn nhưng không phải '#').")]
    [SerializeField] private int wallCapTileIndex = 14;
    [Tooltip("Ô đặt thay cho thùng 'T'. -1 = bỏ qua.")]
    [SerializeField] private int crateTileIndex = -1;
    [Tooltip("Ô đặt thay cho mộ 'G'. -1 = bỏ qua.")]
    [SerializeField] private int tombTileIndex = -1;

    [Header("Placement")]
    [SerializeField] private Vector2Int mapOrigin = Vector2Int.zero;
    [SerializeField] private bool movePlayerToSpawn = true;

    // walls_floor.png = 208x368 px @16 → 13 cột.
    private const int Columns = 13;
    private const string SpritePath = "Assets/ASEPRITE/ASEPRITE_MAP/PNG/walls_floor.png";

    private Tile[] tileCache;

    [ContextMenu("Paint WallsFloor Arena")]
    public void PaintMap()
    {
        ResolveTilemaps();
        LoadTiles();

        if (floorTilemap == null)
        {
            Debug.LogError("[WallsFloorArena] Không tìm thấy FloorLayer tilemap.");
            return;
        }

        floorTilemap.ClearAllTiles();
        if (wallTilemap != null)
            wallTilemap.ClearAllTiles();
        if (decorTilemap != null)
            decorTilemap.ClearAllTiles();

        bool[,] floor = new bool[KenneyStyleMapLayout.Width, KenneyStyleMapLayout.Height];

        for (int y = 0; y < KenneyStyleMapLayout.Height; y++)
        {
            for (int x = 0; x < KenneyStyleMapLayout.Width; x++)
            {
                char c = KenneyStyleMapLayout.Get(x, y);
                Vector3Int cell = OriginCell(x, y);

                if (KenneyStyleMapLayout.IsFloor(c))
                {
                    floor[x, y] = true;
                    SetTile(floorTilemap, cell, floorTileIndex);
                    PaintDecor(cell, c);
                }
            }
        }

        PaintWallsAroundFloors(floor);
        PlacePlayer();
        RefreshEnemySpawner();
        Debug.Log("[WallsFloorArena] Đã vẽ arena bằng tileset walls_floor.png ("
            + KenneyStyleMapLayout.Width + "x" + KenneyStyleMapLayout.Height + ").");
    }

    private void PaintWallsAroundFloors(bool[,] floor)
    {
        if (wallTilemap == null)
            return;

        for (int y = 0; y < KenneyStyleMapLayout.Height; y++)
        {
            for (int x = 0; x < KenneyStyleMapLayout.Width; x++)
            {
                if (floor[x, y])
                    continue;

                char layoutChar = KenneyStyleMapLayout.Get(x, y);
                bool layoutWall = KenneyStyleMapLayout.IsWall(layoutChar);
                bool nearFloor = HasFloorNeighbor(floor, x, y);
                if (!layoutWall && !nearFloor)
                    continue;

                Vector3Int cell = OriginCell(x, y);
                int tile = layoutWall ? wallTileIndex : wallCapTileIndex;
                SetTile(wallTilemap, cell, tile);
            }
        }
    }

    private static bool HasFloorNeighbor(bool[,] floor, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= KenneyStyleMapLayout.Width || ny >= KenneyStyleMapLayout.Height)
                    continue;
                if (floor[nx, ny])
                    return true;
            }
        }

        return false;
    }

    private void PaintDecor(Vector3Int cell, char marker)
    {
        if (decorTilemap == null)
            return;

        switch (marker)
        {
            case 'T':
                if (crateTileIndex >= 0)
                    SetTile(decorTilemap, cell, crateTileIndex);
                break;
            case 'G':
                if (tombTileIndex >= 0)
                    SetTile(decorTilemap, cell, tombTileIndex);
                break;
        }
    }

    private void PlacePlayer()
    {
        if (!movePlayerToSpawn)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || floorTilemap == null)
            return;

        Vector2Int spawn = KenneyStyleMapLayout.PlayerSpawnCell;
        Vector3Int cell = OriginCell(spawn.x, spawn.y);
        player.transform.position = floorTilemap.GetCellCenterWorld(cell);
    }

    private Vector3Int OriginCell(int x, int y) =>
        new Vector3Int(mapOrigin.x + x, mapOrigin.y + y, 0);

    private void SetTile(Tilemap map, Vector3Int cell, int tileIndex)
    {
        TileBase tile = GetTile(tileIndex);
        if (tile != null)
            map.SetTile(cell, tile);
    }

    private TileBase GetTile(int index)
    {
        if (tileCache == null || index < 0 || index >= tileCache.Length)
            return null;
        return tileCache[index];
    }

    private void LoadTiles()
    {
        if (tileCache != null)
            return;

#if UNITY_EDITOR
        // Load tile asset theo tên sprite gốc (walls_floor_<index>), không phụ thuộc
        // thứ tự sắp xếp khi tạo asset.
        const string folder = "Assets/Art/Tiles/Palettes/TileData/WallsFloor";
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Tile", new[] { folder });
        if (guids.Length == 0)
        {
            Debug.LogError("[WallsFloorArena] Chưa có tile WallsFloor. Chạy menu "
                + "DungeonSoul → Map → Slice + Build WallsFloor Tiles (16x16) trước.");
            tileCache = new Tile[0];
            return;
        }

        int maxIndex = -1;
        var byIndex = new System.Collections.Generic.Dictionary<int, Tile>();
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

        tileCache = new Tile[maxIndex + 1];
        foreach (var kv in byIndex)
            tileCache[kv.Key] = kv.Value;
#else
        tileCache = new Tile[0];
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
        if (floorTilemap != null && wallTilemap != null)
            return;

        Transform grid = GameObject.Find("DungeonGrid")?.transform;
        if (grid == null)
            grid = transform;

        if (floorTilemap == null)
        {
            Transform floor = grid.Find("FloorLayer");
            if (floor != null)
                floorTilemap = floor.GetComponent<Tilemap>();
        }

        if (wallTilemap == null)
        {
            Transform wall = grid.Find("WallLayer");
            if (wall != null)
                wallTilemap = wall.GetComponent<Tilemap>();
        }

        if (decorTilemap == null)
        {
            Transform decor = grid.Find("DecorLayer");
            if (decor != null)
                decorTilemap = decor.GetComponent<Tilemap>();
        }
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
