using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveManager : MonoBehaviour
{
    public static EnemyWaveManager Instance { get; private set; }

    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private List<GameObject> bossPrefabs = new List<GameObject>();
    [SerializeField] private float waveDuration = 20f;
    [SerializeField] private float breakDuration = 5f;
    [SerializeField] private float edgeOffset = 1f;

    private int currentWave = 0;
    private float difficultyMultiplier = 1f;
    private bool bossRush;
    private bool eliteMode;

    private Transform player;
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (SurvivalTimer.Instance != null)
        {
            SurvivalTimer.Instance.OnMinutePassed += HandleMinutePassed;
            SurvivalTimer.Instance.OnBossRushStarted += HandleBossRushStarted;
            SurvivalTimer.Instance.OnEliteModeStarted += HandleEliteModeStarted;
        }

        StartCoroutine(WaveLoop());
    }

    private void OnDestroy()
    {
        if (SurvivalTimer.Instance != null)
        {
            SurvivalTimer.Instance.OnMinutePassed -= HandleMinutePassed;
            SurvivalTimer.Instance.OnBossRushStarted -= HandleBossRushStarted;
            SurvivalTimer.Instance.OnEliteModeStarted -= HandleEliteModeStarted;
        }
    }

    public void NotifyEnemyDied(Vector3 position)
    {
        if (!eliteMode)
            return;

        if (Random.value < 0.15f)
            SpawnEnemyAt(position + Random.insideUnitSphere * 0.6f, true);
    }

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            currentWave++;
            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateWaveNumber(currentWave);
                HUDManager.Instance.ShowWaveAnnouncement("WAVE " + currentWave);
            }

            yield return StartCoroutine(SpawnWave(currentWave));
            yield return new WaitForSeconds(breakDuration);
        }
    }

    private IEnumerator SpawnWave(int wave)
    {
        int spawnCount = Mathf.Max(1, wave * 5);
        if (bossRush)
            spawnCount += 8;

        bool spawnBoss = wave % 5 == 0;
        if (spawnBoss)
            SpawnBoss();

        float perSpawnDelay = waveDuration / Mathf.Max(1, spawnCount);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnEnemyAt(GetEdgeSpawnPosition(), false);
            yield return new WaitForSeconds(perSpawnDelay);
        }
    }

    private void SpawnBoss()
    {
        if (bossPrefabs == null || bossPrefabs.Count == 0)
            return;

        GameObject prefab = bossPrefabs[Random.Range(0, bossPrefabs.Count)];
        if (prefab == null)
            return;

        GameObject boss = Instantiate(prefab, GetEdgeSpawnPosition(), Quaternion.identity);
        HealthSystem health = boss.GetComponent<HealthSystem>();
        if (health != null)
            health.MaxHP *= 3f * difficultyMultiplier;
    }

    private void SpawnEnemyAt(Vector3 position, bool elite)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        if (prefab == null)
            return;

        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        if (!elite)
            return;

        enemy.transform.localScale *= 1.15f;
        HealthSystem health = enemy.GetComponent<HealthSystem>();
        if (health != null)
            health.MaxHP *= 1.8f;
    }

    private Vector3 GetEdgeSpawnPosition()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 center = player != null ? player.position : Vector3.zero;
        float vertical = mainCamera != null ? mainCamera.orthographicSize : 6f;
        float horizontal = mainCamera != null ? vertical * mainCamera.aspect : 10f;

        int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: // top
                return new Vector3(center.x + Random.Range(-horizontal, horizontal), center.y + vertical + edgeOffset, 0f);
            case 1: // bottom
                return new Vector3(center.x + Random.Range(-horizontal, horizontal), center.y - vertical - edgeOffset, 0f);
            case 2: // left
                return new Vector3(center.x - horizontal - edgeOffset, center.y + Random.Range(-vertical, vertical), 0f);
            default: // right
                return new Vector3(center.x + horizontal + edgeOffset, center.y + Random.Range(-vertical, vertical), 0f);
        }
    }

    private void HandleMinutePassed(int minute)
    {
        difficultyMultiplier += 0.15f;
    }

    private void HandleBossRushStarted()
    {
        bossRush = true;
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowWaveAnnouncement("BOSS RUSH!");
    }

    private void HandleEliteModeStarted()
    {
        eliteMode = true;
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowWaveAnnouncement("ELITE SWARM!");
    }
}
