// DungeonSoul — DungeonGenerator.cs — BSP procedural map (64×48 tiles, 8–12 rooms).

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator Instance { get; private set; }

    [SerializeField] private int gridWidth = 64;
    [SerializeField] private int gridHeight = 48;
    [SerializeField] private int minLeafCount = 8;
    [SerializeField] private int maxLeafCount = 12;
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private Vector3 worldOrigin;
    [SerializeField] private RoomData defaultRoomData;

    private readonly List<DungeonRoomNode> rooms = new List<DungeonRoomNode>();
    private bool[,] floorGrid;
    private Vector2Int spawnCell;

    public IReadOnlyList<DungeonRoomNode> Rooms => rooms;
    public Vector2Int SpawnCell => spawnCell;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
    {
        floorGrid = new bool[gridWidth, gridHeight];
        rooms.Clear();

        BspNode root = Split(new RectInt(0, 0, gridWidth, gridHeight), 0);
        CollectLeaves(root, rooms);

        while (rooms.Count < minLeafCount)
        {
            rooms.Clear();
            CollectLeaves(root, rooms);
            root = Split(new RectInt(0, 0, gridWidth, gridHeight), 0);
        }

        if (rooms.Count > maxLeafCount)
            rooms.RemoveRange(maxLeafCount, rooms.Count - maxLeafCount);

        AssignRoomTypes();
        CarveRooms();
        ConnectTree(root);
        spawnCell = rooms.Count > 0 ? rooms[0].centerCell : new Vector2Int(gridWidth / 2, gridHeight / 2);
        PaintTilemaps();
        MinimapManager.Instance?.Rebuild(rooms, spawnCell);
    }

    private BspNode Split(RectInt rect, int depth)
    {
        BspNode node = new BspNode { bounds = rect };
        if (depth > 6 || rect.width < 18 || rect.height < 16)
            return node;

        bool splitVertical = rect.width > rect.height;
        if (splitVertical)
        {
            int split = Random.Range(rect.xMin + 8, rect.xMax - 8);
            node.left = Split(new RectInt(rect.xMin, rect.yMin, split - rect.xMin, rect.height), depth + 1);
            node.right = Split(new RectInt(split, rect.yMin, rect.xMax - split, rect.height), depth + 1);
        }
        else
        {
            int split = Random.Range(rect.yMin + 8, rect.yMax - 8);
            node.left = Split(new RectInt(rect.xMin, rect.yMin, rect.width, split - rect.yMin), depth + 1);
            node.right = Split(new RectInt(rect.xMin, split, rect.width, rect.yMax - split), depth + 1);
        }

        return node;
    }

    private void CollectLeaves(BspNode node, List<DungeonRoomNode> output)
    {
        if (node.left == null && node.right == null)
        {
            int rw = Random.Range(12, 19);
            int rh = Random.Range(10, 15);
            int cx = node.bounds.xMin + node.bounds.width / 2;
            int cy = node.bounds.yMin + node.bounds.height / 2;
            RectInt roomRect = new RectInt(cx - rw / 2, cy - rh / 2, rw, rh);
            output.Add(new DungeonRoomNode
            {
                bounds = roomRect,
                centerCell = new Vector2Int(cx, cy),
                roomType = RoomType.Normal
            });
            return;
        }

        if (node.left != null) CollectLeaves(node.left, output);
        if (node.right != null) CollectLeaves(node.right, output);
    }

    private void AssignRoomTypes()
    {
        int bossIndex = FindFarthestRoomIndex();
        int healingSlot = Mathf.Max(0, bossIndex - Random.Range(1, 3));

        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == bossIndex)
                rooms[i].roomType = RoomType.Boss;
            else if (i == healingSlot || i == healingSlot + 1)
                rooms[i].roomType = RoomType.Healing;
            else
                rooms[i].roomType = RollRoomType();
        }
    }

    private RoomType RollRoomType()
    {
        float r = Random.value;
        if (r < 0.35f) return RoomType.Normal;
        if (r < 0.47f) return RoomType.Elite;
        if (r < 0.55f) return RoomType.Treasure;
        if (r < 0.63f) return RoomType.Healing;
        if (r < 0.71f) return RoomType.Shop;
        if (r < 0.76f) return RoomType.Forge;
        if (r < 0.81f) return RoomType.Curse;
        if (r < 0.86f) return RoomType.Mystery;
        if (r < 0.91f) return RoomType.Challenge;
        return RoomType.Normal;
    }

    private int FindFarthestRoomIndex()
    {
        if (rooms.Count == 0)
            return 0;

        int best = 0;
        int bestDist = -1;
        Vector2Int start = rooms[0].centerCell;
        for (int i = 0; i < rooms.Count; i++)
        {
            int d = Mathf.Abs(rooms[i].centerCell.x - start.x) + Mathf.Abs(rooms[i].centerCell.y - start.y);
            if (d > bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return best;
    }

    private void CarveRooms()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            RectInt r = rooms[i].bounds;
            for (int x = r.xMin; x < r.xMax; x++)
            {
                for (int y = r.yMin; y < r.yMax; y++)
                {
                    if (InGrid(x, y))
                        floorGrid[x, y] = true;
                }
            }
        }
    }

    private void ConnectTree(BspNode node)
    {
        if (node.left == null || node.right == null)
            return;

        Vector2Int a = GetRoomCenterNear(node.left);
        Vector2Int b = GetRoomCenterNear(node.right);
        CarveCorridorL(a, b);
        ConnectTree(node.left);
        ConnectTree(node.right);
    }

    private Vector2Int GetRoomCenterNear(BspNode node)
    {
        if (node.left == null && node.right == null)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].bounds.Overlaps(node.bounds))
                    return rooms[i].centerCell;
            }
        }

        return node.left != null ? GetRoomCenterNear(node.left) : GetRoomCenterNear(node.right);
    }

    private void CarveCorridorL(Vector2Int from, Vector2Int to)
    {
        int x = from.x;
        int y = from.y;
        while (x != to.x)
        {
            SetFloor(x, y);
            x += x < to.x ? 1 : -1;
        }
        while (y != to.y)
        {
            SetFloor(x, y);
            y += y < to.y ? 1 : -1;
        }
        SetFloor(to.x, to.y);
    }

    private void SetFloor(int x, int y)
    {
        if (!InGrid(x, y))
            return;
        floorGrid[x, y] = true;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (InGrid(nx, ny) && (dx == 0 || dy == 0))
                    floorGrid[nx, ny] = true;
            }
        }
    }

    private void PaintTilemaps()
    {
        if (floorTilemap == null)
            return;

        floorTilemap.ClearAllTiles();
        if (wallTilemap != null)
            wallTilemap.ClearAllTiles();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (floorGrid[x, y] && floorTile != null)
                    floorTilemap.SetTile(cell, floorTile);
                else if (wallTilemap != null && wallTile != null)
                    wallTilemap.SetTile(cell, wallTile);
            }
        }
    }

    private bool InGrid(int x, int y) => x >= 0 && y >= 0 && x < gridWidth && y < gridHeight;

    private class BspNode
    {
        public RectInt bounds;
        public BspNode left;
        public BspNode right;
    }
}

[System.Serializable]
public class DungeonRoomNode
{
    public RectInt bounds;
    public Vector2Int centerCell;
    public RoomType roomType;
    public bool visited;
}
