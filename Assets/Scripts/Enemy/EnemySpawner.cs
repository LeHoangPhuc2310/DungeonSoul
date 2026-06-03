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
        new Vector3(0,3,0),
        new Vector3(3,2,0),
        new Vector3(-3,2,0),
        new Vector3(2,-2,0),
        new Vector3(-2,-2,0)
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
        {
            FindWalkableTiles();
            SpawnInitialEnemies();
            initialSpawnDone = true;
        }
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
                {
                    walkableTiles.Add(pos);
                }
            }
        }
    }

    private void SpawnInitialEnemies()
    {
        int count = Random.Range(minEnemies, maxEnemies + 1);
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
        }
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector3 spawnPos;
        if (useManualSpawnPoints && manualSpawnPoints.Count > 0)
        {
            spawnPos = manualSpawnPoints[Random.Range(0, manualSpawnPoints.Count)];
        }
        else if (walkableTiles.Count > 0)
        {
            Vector3Int randomTile = walkableTiles[Random.Range(0, walkableTiles.Count)];
            spawnPos = floorTilemap.GetCellCenterWorld(randomTile);
        }
        else
        {
            return;
        }

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}
