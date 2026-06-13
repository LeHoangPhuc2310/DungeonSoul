using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [Tooltip("Thời gian nghỉ giữa khi clear wave thường và spawn wave kế (giây).")]
    [SerializeField] private float waveBreakSeconds = 1.5f;
    public int minEnemies = 12;
    public int maxEnemies = 18;
    [SerializeField] private int maxEnemiesPerWave = 36;
    [SerializeField] private int extraEnemiesPerTwoWaves = 2;
    public float spawnDelay = 2f;
    [SerializeField] private float minDistanceFromPlayer = 2.8f;
    [Tooltip("0 = tự động theo camera + kích thước map.")]
    [SerializeField] private float maxDistanceFromPlayer = 0f;
    [Tooltip("Số hướng chia đều khi spawn wave (8 = N/NE/E/SE/S/SW/W/NW).")]
    [SerializeField] private int spawnSectorCount = 8;
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
    [Tooltip("Bật = Vampire Survivors (spawn liên tục + timer). Tắt = 10 wave arena.")]
    [SerializeField] private bool survivalMode = true;

    private bool initialSpawnDone;
    private bool waveAdvancing;
    private Coroutine waveBreakRoutine;
    private Coroutine survivalSpawnRoutine;
    private int waveIndex = 1;
    private int nextSpawnSector;

    private void Awake()
    {
        ResolveTilemaps();
    }

    private void Start()
    {
        RefreshWalkablePositions();
        RegisterEnemyPool();
        if (IsSurvivalMode())
            StartSurvivalRun();
        else
            SpawnInitialEnemies();
        initialSpawnDone = true;
        EnsureTrapSpawner();
    }

    private void EnsureTrapSpawner()
    {
        if (Object.FindAnyObjectByType<TrapSpawner>() != null)
            return;
        if (GetComponent<TrapSpawner>() == null)
            gameObject.AddComponent<TrapSpawner>();
    }

    private void RegisterEnemyPool()
    {
        if (enemyPrefab == null || ObjectPooler.Instance == null)
            return;

        ObjectPooler.Instance.RegisterRuntimePool(EnemyPoolable.PoolKey, enemyPrefab, 96);
    }

    private bool IsSurvivalMode()
    {
        if (GameRunBootstrap.Instance != null)
        {
            if (GameRunBootstrap.Instance.Mode == GameRunMode.WaveArena)
                return false;
            if (GameRunBootstrap.Instance.Mode == GameRunMode.Survival)
                return true;
        }

        if (SurvivalRunManager.Instance != null)
            return SurvivalRunManager.Instance.IsActive;
        return survivalMode;
    }

    public void RefreshWalkablePositions()
    {
        ResolveTilemaps();
        CacheWalkablePositions();
    }

    private void Update()
    {
        if (!initialSpawnDone || waveAdvancing || IsSurvivalMode())
            return;

        // Boss wave: rương boss (RedChestController) tự xử lý → bỏ qua auto-advance.
        if (BossSpawnManager.IsBossWave(waveIndex))
            return;

        // Wave thường: hết quái → không có rương, tự nghỉ ngắn rồi sang wave kế.
        if (EnemyAliveTracker.Count <= 0)
        {
            waveAdvancing = true;
            AudioManager.PlayRoomClear();
            AudioManager.PlayBackgroundMusic();
            Debug.Log("[EnemySpawner] Clear wave " + waveIndex + " — tự sang wave kế.");
            if (waveBreakRoutine != null)
                StopCoroutine(waveBreakRoutine);
            waveBreakRoutine = StartCoroutine(WaveBreakThenAdvance());
        }
    }

    private IEnumerator WaveBreakThenAdvance()
    {
        // WaitForSecondsRealtime — wave vẫn tiếp tục khi panel skill pause (timeScale=0).
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, waveBreakSeconds));
        waveBreakRoutine = null;
        BeginNextWave();
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

    private void StartSurvivalRun()
    {
        waveIndex = 1;
        SurvivalRunManager.Instance?.ResetForNewRun();

        int burst = SurvivalRunManager.Instance != null
            ? SurvivalRunManager.Instance.GetInitialBurstCount()
            : 36;

        EnemyAliveTracker.Reset(0);
        int sectors = Mathf.Max(4, spawnSectorCount);
        int spawned = 0;
        for (int i = 0; i < burst; i++)
        {
            if (SpawnEnemy(i % sectors))
                spawned++;
        }

        if (spawned > 0)
            AudioManager.PlayCombatMusic();

        EventBus.InvokeWaveStarted(1);
        if (survivalSpawnRoutine != null)
            StopCoroutine(survivalSpawnRoutine);
        survivalSpawnRoutine = StartCoroutine(SurvivalSpawnLoop());
        Debug.Log("[EnemySpawner] Survival mode — burst " + spawned);
    }

    private IEnumerator SurvivalSpawnLoop()
    {
        while (IsSurvivalMode())
        {
            SurvivalRunManager mgr = SurvivalRunManager.Instance;
            float interval = mgr != null ? mgr.GetSpawnInterval() : 2.5f;
            yield return new WaitForSeconds(interval);

            if (!IsSurvivalMode())
                yield break;

            int maxOnScreen = mgr != null ? mgr.GetMaxEnemiesOnScreen() : 140;
            if (EnemyAliveTracker.Count >= maxOnScreen)
                continue;

            int tier = mgr != null ? mgr.GetDifficultyTier() : waveIndex;
            waveIndex = tier;
            int sector = nextSpawnSector++ % Mathf.Max(4, spawnSectorCount);
            SpawnEnemy(sector);
        }
    }

    public Vector3 GetRandomSpawnPosition()
    {
        return PickSpawnPosition();
    }

    private void SpawnInitialEnemies()
    {
        waveIndex = 1;
        int firstCount = SpawnWaveEnemies();
        if (firstCount > 0)
            AudioManager.PlayCombatMusic();
        EventBus.InvokeWaveStarted(waveIndex);
    }

    /// <summary>Spawn wave kế: gọi tự động khi clear wave thường, hoặc sau rương boss.</summary>
    public void BeginNextWave()
    {
        waveAdvancing = false;

        waveIndex++;
        int count = SpawnWaveEnemies();
        if (count > 0)
            AudioManager.PlayCombatMusic();
        EventBus.InvokeWaveStarted(waveIndex);
        Debug.Log("[EnemySpawner] Wave " + waveIndex + " — spawned " + count + " enemies.");

        HUDManager hud = HUDManager.Resolve();
        if (hud != null)
            hud.UpdateFloor(Mathf.Min(waveIndex, 10));

        if (FloorManager.Instance != null && waveIndex <= 10)
            FloorManager.Instance.SetFloor(waveIndex);
    }

    private int SpawnWaveEnemies()
    {
        if (BossSpawnManager.IsBossWave(waveIndex))
        {
            Vector3 pos = PickSpawnPosition();
            if (!IsValidSpawnWorldPosition(pos))
                return 0;

            EnemyAliveTracker.Reset(0);
            BossSpawnManager.SpawnForWave(waveIndex, pos, enemyPrefab);
            return 1;
        }

        int waveBonus = Mathf.Max(0, (waveIndex - 1) / 2) * extraEnemiesPerTwoWaves;
        int min = minEnemies + waveBonus;
        int max = maxEnemies + waveBonus;
        int count = Mathf.Clamp(Random.Range(min, max + 1), min, maxEnemiesPerWave);
        EnemyAliveTracker.Reset(0);
        int sectors = Mathf.Max(4, spawnSectorCount);
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            if (SpawnEnemy(i % sectors))
                spawned++;
        }

        return spawned;
    }

    public int CurrentWave => waveIndex;

    public void EnableSurvivalMode(bool enabled)
    {
        survivalMode = enabled;
    }

    public bool SpawnEnemy()
    {
        return SpawnEnemy(-1);
    }

    public bool SpawnEnemy(int sectorIndex)
    {
        int difficulty = GetCurrentDifficulty();
        Vector3 pos = sectorIndex >= 0
            ? PickSpawnPositionInSector(sectorIndex)
            : PickSpawnPosition();
        if (!IsValidSpawnWorldPosition(pos))
        {
            Debug.LogWarning("[EnemySpawner] Skipped spawn outside walkable map.");
            return false;
        }

        if (enemyPrefab != null)
        {
            EnemyArchetype archetype = EnemyArchetypeUtility.RollForWave(difficulty);
            GameObject enemy = TrySpawnFromPool(pos, difficulty, archetype);
            if (enemy == null)
            {
                enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
                ApplySpawnedEnemy(enemy, difficulty, archetype);
            }

            EnemyAliveTracker.Add(1);
        }
        else
        {
            SpawnRuntimeEnemy(pos, difficulty);
            EnemyAliveTracker.Add(1);
        }

        return true;
    }

    private static GameObject TrySpawnFromPool(Vector3 pos, int difficulty, EnemyArchetype archetype)
    {
        if (ObjectPooler.Instance == null)
            return null;

        GameObject enemy = ObjectPooler.Instance.Get(EnemyPoolable.PoolKey, pos, Quaternion.identity);
        if (enemy == null)
            return null;

        ApplySpawnedEnemy(enemy, difficulty, archetype);
        return enemy;
    }

    private static void ApplySpawnedEnemy(GameObject enemy, int difficulty, EnemyArchetype archetype)
    {
        RuntimeSpawnGuard.Mark(enemy);

        EnemyPoolable poolable = enemy.GetComponent<EnemyPoolable>();
        if (poolable == null)
            poolable = enemy.AddComponent<EnemyPoolable>();

        poolable.PrepareForSpawn(difficulty, archetype);
    }

    private int GetCurrentDifficulty()
    {
        if (IsSurvivalMode() && SurvivalRunManager.Instance != null)
            return SurvivalRunManager.Instance.GetDifficultyTier();
        return waveIndex;
    }

    private float MinSpawnDistance => minDistanceFromPlayer;

    private float MaxSpawnDistanceFor(Transform player)
    {
        if (maxDistanceFromPlayer > 0f)
            return maxDistanceFromPlayer;

        float cameraMax = GameScale.GetSpawnMaxDistanceFromPlayer(IsSurvivalMode() ? 1.2f : 0.92f);
        if (player == null || spawnWorldPositions.Count == 0)
            return cameraMax;

        float mapMax = 0f;
        for (int i = 0; i < spawnWorldPositions.Count; i++)
        {
            float d = Vector2.Distance(spawnWorldPositions[i], player.position);
            if (d > mapMax)
                mapMax = d;
        }

        // Wave arena: dùng phần lớn map để spawn đều 4/8 hướng; survival: xa hơn một chút.
        float mapFactor = IsSurvivalMode() ? 0.88f : 0.78f;
        return Mathf.Max(minDistanceFromPlayer + 1.5f, Mathf.Min(cameraMax, mapMax * mapFactor));
    }

    private bool IsWithinSpawnDistance(Vector3 pos, Transform player, float maxDistance)
    {
        if (player == null || maxDistance <= 0f)
            return true;

        return Vector2.Distance(pos, player.position) <= maxDistance;
    }

    private static float AngleFromPlayer(Vector3 worldPos, Vector3 playerPos)
    {
        Vector2 delta = worldPos - playerPos;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        return angle < 0f ? angle + 360f : angle;
    }

    private Vector3 PickSpawnPositionInSector(int sectorIndex)
    {
        int sectors = Mathf.Max(4, spawnSectorCount);
        sectorIndex = ((sectorIndex % sectors) + sectors) % sectors;

        if (spawnWorldPositions.Count == 0)
            RefreshWalkablePositions();

        Transform player = FindPlayer();
        float minD = MinSpawnDistance;
        float maxD = MaxSpawnDistanceFor(player);
        float sectorWidth = 360f / sectors;
        float sectorCenter = sectorIndex * sectorWidth;

        sectorCandidates.Clear();
        for (int i = 0; i < spawnWorldPositions.Count; i++)
        {
            Vector3 pos = spawnWorldPositions[i];
            if (!IsValidSpawnWorldPosition(pos))
                continue;

            if (player == null)
            {
                sectorCandidates.Add(pos);
                continue;
            }

            float dist = Vector2.Distance(pos, player.position);
            if (dist < minD || dist > maxD)
                continue;

            float angle = AngleFromPlayer(pos, player.position);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, sectorCenter)) <= sectorWidth * 0.55f)
                sectorCandidates.Add(pos);
        }

        if (sectorCandidates.Count > 0)
            return sectorCandidates[Random.Range(0, sectorCandidates.Count)];

        // Hướng này ít ô — nới lỏng sang hướng kề.
        for (int i = 0; i < spawnWorldPositions.Count; i++)
        {
            Vector3 pos = spawnWorldPositions[i];
            if (!IsValidSpawnWorldPosition(pos))
                continue;

            if (player == null)
            {
                sectorCandidates.Add(pos);
                continue;
            }

            float dist = Vector2.Distance(pos, player.position);
            if (dist < minD * 0.85f || dist > maxD * 1.05f)
                continue;

            float angle = AngleFromPlayer(pos, player.position);
            if (Mathf.Abs(Mathf.DeltaAngle(angle, sectorCenter)) <= sectorWidth * 0.95f)
                sectorCandidates.Add(pos);
        }

        if (sectorCandidates.Count > 0)
            return sectorCandidates[Random.Range(0, sectorCandidates.Count)];

        return PickFromTilemap();
    }

    private readonly List<Vector3> sectorCandidates = new List<Vector3>(48);

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
                float maxD = MaxSpawnDistanceFor(player);
                if (dist < MinSpawnDistance || !IsWithinSpawnDistance(point, player, maxD))
                    continue;
            }

            valid.Add(point);
        }

        return valid;
    }

    private readonly List<Vector3> fallbackCandidates = new List<Vector3>(64);

    private Vector3 PickFromTilemap()
    {
        Transform player = FindPlayer();
        float maxD = MaxSpawnDistanceFor(player);

        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector3 pos = spawnWorldPositions[Random.Range(0, spawnWorldPositions.Count)];
            if (!IsValidSpawnWorldPosition(pos))
                continue;

            if (player == null)
                return pos;

            float dist = Vector2.Distance(pos, player.position);
            if (dist >= MinSpawnDistance && IsWithinSpawnDistance(pos, player, maxD))
                return pos;
        }

        // Map nhỏ hơn vành đai spawn lý tưởng: ưu tiên các ô XA player nhất còn lại,
        // chọn ngẫu nhiên trong nhóm đó — tuyệt đối không dồn cả wave về một ô cố định.
        fallbackCandidates.Clear();
        float bestDist = 0f;
        for (int i = 0; i < spawnWorldPositions.Count; i++)
        {
            Vector3 pos = spawnWorldPositions[i];
            if (!IsValidSpawnWorldPosition(pos))
                continue;

            float dist = player != null ? Vector2.Distance(pos, player.position) : float.MaxValue;
            if (dist > bestDist)
                bestDist = dist;

            if (player == null || dist >= minDistanceFromPlayer)
                fallbackCandidates.Add(pos);
        }

        if (fallbackCandidates.Count > 0)
        {
            // Giữ lại nửa xa nhất của các ứng viên để quái vẫn xuất hiện ở rìa.
            float cutoff = bestDist * 0.6f;
            for (int i = fallbackCandidates.Count - 1; i >= 0; i--)
            {
                if (player != null && Vector2.Distance(fallbackCandidates[i], player.position) < cutoff)
                    fallbackCandidates.RemoveAt(i);
            }

            if (fallbackCandidates.Count > 0)
                return fallbackCandidates[Random.Range(0, fallbackCandidates.Count)];
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
        GameObject enemy = RuntimeSpawnGuard.Mark(new GameObject("Enemy"));
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
