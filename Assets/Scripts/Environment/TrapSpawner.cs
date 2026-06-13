using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Rải bẫy chông (SpikeTrap) lên các ô sàn nội thất khi vào màn. Tự tìm FloorLayer/WallLayer
/// như EnemySpawner; tránh đặt bẫy quá gần điểm xuất phát của player. Lệch pha chu kỳ giữa
/// các bẫy để chông không nhô đồng loạt.
/// </summary>
public class TrapSpawner : MonoBehaviour
{
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [Tooltip("Số bẫy rải mỗi màn.")]
    [SerializeField] private int trapCount = 10;
    [SerializeField] private float trapDamage = 12f;
    [SerializeField] private float trapWorldSize = 0.85f;
    [Tooltip("Không đặt bẫy trong bán kính này quanh player lúc bắt đầu.")]
    [SerializeField] private float safeRadiusFromPlayer = 3f;
    [Tooltip("Khoảng cách tối thiểu giữa 2 bẫy.")]
    [SerializeField] private float minTrapSpacing = 2f;

    private readonly List<Vector3> placed = new List<Vector3>(16);

    private void Start()
    {
        ResolveTilemaps();
        // Chờ 1 frame để map/painter dựng xong tilemap trước khi rải bẫy.
        Invoke(nameof(SpawnTraps), 0.1f);
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

    private void SpawnTraps()
    {
        if (floorTilemap == null || SpikeTrap.LoadFrames().Length == 0)
            return;

        List<Vector3> candidates = CollectFloorCells();
        if (candidates.Count == 0)
            return;

        Vector3 playerPos = GetPlayerPosition();
        int target = Mathf.Min(trapCount, candidates.Count);
        int placedCount = 0;
        int safety = 0;

        while (placedCount < target && safety++ < target * 12)
        {
            Vector3 pos = candidates[Random.Range(0, candidates.Count)];

            if (Vector2.Distance(pos, playerPos) < safeRadiusFromPlayer)
                continue;

            bool tooClose = false;
            for (int i = 0; i < placed.Count; i++)
            {
                if (Vector2.Distance(placed[i], pos) < minTrapSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
                continue;

            // Lệch pha để mỗi bẫy nhô chông ở thời điểm khác nhau.
            float phaseOffset = Random.Range(0f, 2.4f);
            SpikeTrap trap = SpikeTrap.Spawn(pos, trapWorldSize, trapDamage, phaseOffset);
            if (trap != null)
            {
                placed.Add(pos);
                placedCount++;
            }
        }

        Debug.Log($"[TrapSpawner] Đã rải {placedCount} bẫy chông.");
    }

    private List<Vector3> CollectFloorCells()
    {
        List<Vector3> result = new List<Vector3>(128);
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
                // Chỉ ô nội thất (4 cạnh đều là sàn) — bẫy không nằm sát mép tường.
                if (!floorTilemap.HasTile(cell + Vector3Int.right) ||
                    !floorTilemap.HasTile(cell + Vector3Int.left) ||
                    !floorTilemap.HasTile(cell + Vector3Int.up) ||
                    !floorTilemap.HasTile(cell + Vector3Int.down))
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
