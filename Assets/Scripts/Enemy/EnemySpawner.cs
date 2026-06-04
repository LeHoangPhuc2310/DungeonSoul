using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Tilemap floorTilemap;
    public GameObject chest;
    public int minEnemies = 3;
    public int maxEnemies = 5;
    public float spawnDelay = 2f;
    public List<Vector3> manualSpawnPoints = new List<Vector3>()
    {
        new Vector3(0, 3, 0),
        new Vector3(3, 2, 0),
        new Vector3(-3, 2, 0),
        new Vector3(2, -2, 0),
        new Vector3(-2, -2, 0)
    };
    public bool useManualSpawnPoints = true;

    private List<Vector3Int> walkableTiles = new List<Vector3Int>();
    private bool initialSpawnDone = false;
    private bool chestSpawned = false;

    private void Start()
    {
        if (chest != null)
            chest.SetActive(false);

        if (floorTilemap != null)
            FindWalkableTiles();

        SpawnInitialEnemies();
        initialSpawnDone = true;
    }

    private void Update()
    {
        if (!initialSpawnDone || chestSpawned || chest == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
        {
            chest.SetActive(true);
            chestSpawned = true;
            Debug.Log("All enemies defeated! Chest appeared.");
        }
    }

    private void FindWalkableTiles()
    {
        BoundsInt bounds = floorTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (floorTilemap.HasTile(pos))
                    walkableTiles.Add(pos);
            }
        }
    }

    private void SpawnInitialEnemies()
    {
        int count = Random.Range(minEnemies, maxEnemies + 1);
        for (int i = 0; i < count; i++)
            SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        Vector3 pos = PickSpawnPosition();

        if (enemyPrefab != null)
            Instantiate(enemyPrefab, pos, Quaternion.identity);
        else
            SpawnRuntimeEnemy(pos);
    }

    private Vector3 PickSpawnPosition()
    {
        if (useManualSpawnPoints && manualSpawnPoints.Count > 0)
            return manualSpawnPoints[Random.Range(0, manualSpawnPoints.Count)];

        if (walkableTiles.Count > 0)
            return floorTilemap.GetCellCenterWorld(walkableTiles[Random.Range(0, walkableTiles.Count)]);

        // Fallback: random scatter around spawner
        float angle = Random.value * 360f * Mathf.Deg2Rad;
        float dist = Random.Range(3f, 5f);
        return transform.position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
    }

    private static void SpawnRuntimeEnemy(Vector3 position)
    {
        GameObject enemy = new GameObject("Enemy");
        enemy.tag = "Enemy";
        enemy.transform.position = position;

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = WeaponVisualLibrary.GetCircleSprite();
        sr.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        sr.sortingOrder = 5;
        enemy.transform.localScale = Vector3.one * 0.7f;

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;
        col.isTrigger = false;

        enemy.AddComponent<HealthSystem>();
        enemy.AddComponent<EnemyAI>();
        enemy.AddComponent<EnemyReward>();
    }
}
