using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    public GameObject chest;
    public int minEnemies = 8;
    public int maxEnemies = 13;
    [SerializeField] private int maxEnemiesPerWave = 24;
    [SerializeField] private int extraEnemiesPerTwoWaves = 1;
    public float spawnDelay = 2f;
    [SerializeField] private float minDistanceFromPlayer = 2.5f;
    [Tooltip("0 = tự động theo camera — quái spawn trong vùng nhìn thấy.")]
    [SerializeField] private float maxDistanceFromPlayer = 0f;
    [SerializeField] private bool requireInteriorTile = true;

    public List<Vector3> manualSpawnPoints = new List<Vector3>()
    {
        new Vector3(0, 2, 0),
        new Vector3(3, 1, 0),
        new Vector3(-3, 1, 0),
        new Vector3(2, -2, 0),
        new Vector3(-2, -2, 0)
    };

    [Tooltip("Off = spawn only on floor tiles from FloorLayer tilemap (recommended).")]
    public bool useManualSpawnPoints;

    private readonly List<Vector3Int> walkableTiles = new List<Vector3Int>();
    private readonly List<Vector3> spawnWorldPositions = new List<Vector3>();
    private bool initialSpawnDone;
    private bool chestSpawned;
    private int waveIndex = 1;

    private void Awake()
    {
        ResolveTilemaps();
    }

    private void Start()
    {
        if (chest != null)
            chest.SetActive(false);

        RefreshWalkablePositions();
        SpawnInitialEnemies();
        initialSpawnDone = true;
    }

    public void RefreshWalkablePositions()
    {
        ResolveTilemaps();
        CacheWalkablePositions();
    }

    private void Update()
    {
        if (!initialSpawnDone || chestSpawned || chest == null)
            return;

        if (BossSpawnManager.IsBossWave(waveIndex))
            return;

        if (EnemyAliveTracker.Count <= 0)
        {
            chest.SetActive(true);
            chestSpawned = true;
            AudioManager.PlayRoomClear();
            AudioManager.PlayBackgroundMusic();
            Debug.Log("All enemies defeated! Chest appeared.");
        }
    }

    private void CacheWalkablePositions()
    {
        walkableTiles.Clear();
        spawnWorldPositions.Clear();

        if (floorTilemap == null)
        {
            Debug.LogWarning("[EnemySpawner] FloorLayer tilemap is missing.");
            return;
        }

        BoundsInt bounds = floorTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!IsValidSpawnCell(cell))
                    continue;

                walkableTiles.Add(cell);
                spawnWorldPositions.Add(floorTilemap.GetCellCenterWorld(cell));
            }
        }

        if (spawnWorldPositions.Count == 0)
            Debug.LogWarning("[EnemySpawner] No valid interior floor cells found on FloorLayer.");
    }

    private bool IsValidSpawnCell(Vector3Int cell)
    {
        if (floorTilemap == null || !floorTilemap.HasTile(cell))
            return false;

        if (wallTilemap != null && wallTilemap.HasTile(cell))
            return false;

        if (requireInteriorTile && !IsInteriorFloorCell(cell))
            return false;

        return true;
    }

    private bool IsInteriorFloorCell(Vector3Int cell)
    {
        return floorTilemap.HasTile(cell + Vector3Int.right)
            && floorTilemap.HasTile(cell + Vector3Int.left)
            && floorTilemap.HasTile(cell + Vector3Int.up)
            && floorTilemap.HasTile(cell + Vector3Int.down);
    }

    private void SpawnInitialEnemies()
    {
        waveIndex = 1;
        int firstCount = SpawnWaveEnemies();
        if (firstCount > 0)
            AudioManager.PlayCombatMusic();
    }

    /// <summary>Called after player picks a skill from the chest — spawns the next combat wave.</summary>
    public void BeginNextWave()
    {
        chestSpawned = false;
        if (chest != null)
            chest.SetActive(false);

        waveIndex++;
        int count = SpawnWaveEnemies();
        if (count > 0)
            AudioManager.PlayCombatMusic();
        Debug.Log("[EnemySpawner] Wave " + waveIndex + " — spawned " + count + " enemies.");

        HUDManager hud = HUDManager.Resolve();
        if (hud != null)
            hud.UpdateFloor(Mathf.Min(waveIndex, 10));

        if (FloorManager.Instance != null && waveIndex <= 10)
            FloorManager.currentFloor = waveIndex;
    }

    private int SpawnWaveEnemies()
    {
        if (BossSpawnManager.IsBossWave(waveIndex))
        {
            Vector3 pos = PickSpawnPosition();
            if (!IsValidSpawnWorldPosition(pos))
                return 0;

            BossSpawnManager.SpawnForWave(waveIndex, pos, enemyPrefab);
            EnemyAliveTracker.Reset(0);
            EnemyAliveTracker.Add(1);
            chestSpawned = false;
            return 1;
        }

        int waveBonus = Mathf.Max(0, (waveIndex - 1) / 2) * extraEnemiesPerTwoWaves;
        int min = Mathf.Max(8, minEnemies) + waveBonus;
        int max = Mathf.Max(13, maxEnemies) + waveBonus;
        int count = Mathf.Clamp(Random.Range(min, max + 1), min, maxEnemiesPerWave);
        EnemyAliveTracker.Reset(0);
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            if (SpawnEnemy())
                spawned++;
        }

        return spawned;
    }

    public int CurrentWave => waveIndex;

    public bool SpawnEnemy()
    {
        Vector3 pos = PickSpawnPosition();
        if (!IsValidSpawnWorldPosition(pos))
        {
            Debug.LogWarning("[EnemySpawner] Skipped spawn outside walkable map.");
            return false;
        }

        if (enemyPrefab != null)
        {
            GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            EnemyArchetypeUtility.Apply(enemy, EnemyArchetypeUtility.RollForWave(waveIndex), waveIndex);
            EnemyAliveTracker.Add(1);
        }
        else
        {
            SpawnRuntimeEnemy(pos, waveIndex);
            EnemyAliveTracker.Add(1);
        }

        return true;
    }

    private float MaxSpawnDistance =>
        maxDistanceFromPlayer > 0f
            ? maxDistanceFromPlayer
            : GameScale.GetSpawnMaxDistanceFromPlayer(0.88f);

    private bool IsWithinSpawnDistance(Vector3 pos, Transform player)
    {
        if (player == null)
            return true;

        float max = MaxSpawnDistance;
        if (max <= 0f)
            return true;

        return Vector2.Distance(pos, player.position) <= max;
    }

    private Vector3 PickSpawnPosition()
    {
        if (useManualSpawnPoints && manualSpawnPoints.Count > 0)
        {
            List<Vector3> valid = GetValidManualPoints();
            if (valid.Count > 0)
                return valid[Random.Range(0, valid.Count)];
        }

        if (spawnWorldPositions.Count == 0)
            RefreshWalkablePositions();

        if (spawnWorldPositions.Count > 0)
            return PickFromTilemap();

        return transform.position;
    }

    private List<Vector3> GetValidManualPoints()
    {
        List<Vector3> valid = new List<Vector3>(manualSpawnPoints.Count);
        Transform player = FindPlayer();

        for (int i = 0; i < manualSpawnPoints.Count; i++)
        {
            Vector3 point = manualSpawnPoints[i];
            if (!IsValidSpawnWorldPosition(point))
                continue;

            if (player != null)
            {
                float dist = Vector2.Distance(point, player.position);
                if (dist < minDistanceFromPlayer || !IsWithinSpawnDistance(point, player))
                    continue;
            }

            valid.Add(point);
        }

        return valid;
    }

    private Vector3 PickFromTilemap()
    {
        Transform player = FindPlayer();

        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector3 pos = spawnWorldPositions[Random.Range(0, spawnWorldPositions.Count)];
            if (!IsValidSpawnWorldPosition(pos))
                continue;

            if (player == null)
                return pos;

            float dist = Vector2.Distance(pos, player.position);
            if (dist >= minDistanceFromPlayer && IsWithinSpawnDistance(pos, player))
                return pos;
        }

        for (int i = 0; i < spawnWorldPositions.Count; i++)
        {
            Vector3 pos = spawnWorldPositions[i];
            if (IsValidSpawnWorldPosition(pos))
                return pos;
        }

        return transform.position;
    }

    private bool IsValidSpawnWorldPosition(Vector3 worldPosition)
    {
        if (floorTilemap == null)
            return false;

        Vector3Int cell = floorTilemap.WorldToCell(worldPosition);
        return IsValidSpawnCell(cell);
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

    private static Transform FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    private static void SpawnRuntimeEnemy(Vector3 position, int wave)
    {
        GameObject enemy = new GameObject("Enemy");
        enemy.tag = "Enemy";
        enemy.transform.position = position;

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = WeaponVisualLibrary.GetCircleSprite();
        sr.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        sr.sortingOrder = 5;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        enemy.AddComponent<HealthSystem>();
        enemy.AddComponent<EnemyAI>();
        enemy.AddComponent<EnemyReward>();
        if (enemy.GetComponent<EnemySpriteAnimator>() == null)
            enemy.AddComponent<EnemySpriteAnimator>();
        EnemyArchetypeUtility.Apply(enemy, EnemyArchetypeUtility.RollForWave(wave), wave);
    }
}
