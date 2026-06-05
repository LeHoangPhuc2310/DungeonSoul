// DungeonSoul — KenneyStyleMapPainter.cs — Vẽ map Kenney lên FloorLayer / WallLayer.

using UnityEngine;
using UnityEngine.Tilemaps;

public class KenneyStyleMapPainter : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap decorTilemap;

    [Header("Tile indices (Assets/Art/Tiles/Data/tile_XXXX)")]
    [SerializeField] private int floorTileIndex = 2;
    [SerializeField] private int wallTileIndex = 11;
    [SerializeField] private int wallCapTileIndex = 17;
    [SerializeField] private int crateTileIndex = 40;
    [SerializeField] private int chestTileIndex = 89;
    [SerializeField] private int tombTileIndex = 120;

    [Header("Placement")]
    [SerializeField] private Vector2Int mapOrigin = Vector2Int.zero;
    [SerializeField] private bool movePlayerToSpawn = true;

    private TileBase[] tileCache = new TileBase[140];

    [ContextMenu("Paint Kenney Demo Map")]
    public void PaintMap()
    {
        ResolveTilemaps();
        LoadTiles();

        if (floorTilemap == null)
        {
            Debug.LogError("[KenneyMap] Không tìm thấy FloorLayer tilemap.");
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
        Debug.Log("[KenneyMap] Đã vẽ map Kenney kín (" + KenneyStyleMapLayout.Width + "x" + KenneyStyleMapLayout.Height + ").");
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
            case 'C':
                SetTile(decorTilemap, cell, chestTileIndex);
                break;
            case 'T':
                SetTile(decorTilemap, cell, crateTileIndex);
                break;
            case 'G':
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
        if (index < 0 || index >= tileCache.Length)
            return null;
        return tileCache[index];
    }

    private void LoadTiles()
    {
        for (int i = 0; i < tileCache.Length; i++)
        {
            if (tileCache[i] != null)
                continue;

            string path = $"Assets/Art/Tiles/Data/tile_{i:0000}.asset";
#if UNITY_EDITOR
            tileCache[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(path);
#else
            tileCache[i] = Resources.Load<TileBase>("Tiles/Data/tile_" + i.ToString("0000"));
#endif
        }
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

    public Vector3 GetSpawnWorldPosition()
    {
        ResolveTilemaps();
        if (floorTilemap == null)
            return Vector3.zero;

        Vector3Int cell = OriginCell(
            KenneyStyleMapLayout.PlayerSpawnCell.x,
            KenneyStyleMapLayout.PlayerSpawnCell.y);
        return floorTilemap.GetCellCenterWorld(cell);
    }
}
