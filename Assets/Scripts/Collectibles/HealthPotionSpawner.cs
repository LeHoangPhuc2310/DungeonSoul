using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Định kỳ thả bình thuốc hồi máu lên các ô sàn dungeon (vị trí ngẫu nhiên), tránh đặt
/// quá sát player và giới hạn số bình tồn tại cùng lúc để map không bị ngập. Tự tìm
/// FloorLayer/WallLayer như TrapSpawner. Gắn runtime bởi GameRunBootstrap.
/// </summary>
public class HealthPotionSpawner : MonoBehaviour
{
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [Tooltip("Khoảng cách (giây) giữa 2 lần thử thả bình thuốc.")]
    [SerializeField] private float spawnInterval = 14f;
    [Tooltip("Số bình thuốc tối đa tồn tại cùng lúc trên map.")]
    [SerializeField] private int maxActivePotions = 3;
    [Tooltip("Không thả bình trong bán kính này quanh player.")]
    [SerializeField] private float safeRadiusFromPlayer = 2.5f;
    [Tooltip("% máu tối đa mỗi bình hồi.")]
    [Range(0f, 1f)]
    [SerializeField] private float healFraction = 0.25f;
    [SerializeField] private float minHeal = 20f;

    private float timer;
    private List<Vector3> floorCells;

    private void Start()
    {
        ResolveTilemaps();
        // Thả sớm lần đầu cho người chơi thấy ngay, sau đó theo interval.
        timer = Mathf.Min(5f, spawnInterval);
    }

    private void Update()
    {
        if (floorTilemap == null)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;
        timer = spawnInterval;

        TrySpawnPotion();
    }

    private void TrySpawnPotion()
    {
        if (CountActivePotions() >= maxActivePotions)
            return;

        if (floorCells == null)
            floorCells = CollectFloorCells();
        if (floorCells.Count == 0)
            return;

        Vector3 playerPos = GetPlayerPosition();
        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 pos = floorCells[Random.Range(0, floorCells.Count)];
            if (Vector2.Distance(pos, playerPos) < safeRadiusFromPlayer)
                continue;

            HealthPotion.Spawn(pos, healFraction, minHeal);
            return;
        }
    }

    private static int CountActivePotions()
    {
        return Object.FindObjectsByType<HealthPotion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    private void ResolveTilemaps()
    {
        if (floorTilemap != null && wallTilemap != null)
            return;

        Transform grid = transform;
        if (grid.name != "DungeonGrid")
        {
            GameObject found = GameObject.Find("DungeonGrid");
            if (found != null)
                grid = found.transform;
        }

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
    }

    private List<Vector3> CollectFloorCells()
    {
        List<Vector3> result = new List<Vector3>(128);
        if (floorTilemap == null)
            return result;

        BoundsInt bounds = floorTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!floorTilemap.HasTile(cell))
                    continue;
                if (wallTilemap != null && wallTilemap.HasTile(cell))
                    continue;
                result.Add(floorTilemap.GetCellCenterWorld(cell));
            }
        }
        return result;
    }

    private static Vector3 GetPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform.position : Vector3.zero;
    }
}
