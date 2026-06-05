// DungeonSoul — RoomController.cs — Locked / Active / Cleared room lifecycle.

using UnityEngine;

public enum RoomState
{
    Locked,
    Active,
    Cleared
}

public class RoomController : MonoBehaviour
{
    [SerializeField] private RoomType roomType = RoomType.Normal;
    [SerializeField] private RoomData roomData;
    [SerializeField] private Collider2D[] doorColliders;
    [SerializeField] private Transform enemySpawnRoot;
    [SerializeField] private GameObject chestPrefab;

    private static readonly System.Collections.Generic.List<RoomController> ActiveRooms =
        new System.Collections.Generic.List<RoomController>();

    private RoomState state = RoomState.Locked;
    private int enemiesAlive;

    public RoomType Type => roomType;
    public RoomState State => state;

    public void Configure(RoomType type, RoomData data)
    {
        roomType = type;
        roomData = data != null ? data : roomData;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || state != RoomState.Locked)
            return;

        ActivateRoom();
    }

    public void ActivateRoom()
    {
        if (state != RoomState.Locked)
            return;

        state = RoomState.Active;
        if (!ActiveRooms.Contains(this))
            ActiveRooms.Add(this);
        SetDoors(true);
        SpawnEnemies();
        EventBus.InvokeRoomEntered(roomType);
        MinimapManager.Instance?.RevealRoom(transform.position);
    }

    private void SpawnEnemies()
    {
        if (roomType == RoomType.Boss)
            return;

        int min = roomData != null ? roomData.minEnemies : 3;
        int max = roomData != null ? roomData.maxEnemies : 5;
        enemiesAlive = Random.Range(min, max + 1);

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        for (int i = 0; i < enemiesAlive; i++)
        {
            if (spawner != null)
                spawner.SpawnEnemy();
        }
    }

    public void NotifyEnemyKilled()
    {
        if (state != RoomState.Active)
            return;

        enemiesAlive--;
        if (enemiesAlive > 0)
            return;

        ClearRoom();
    }

    public void ClearRoom()
    {
        if (state == RoomState.Cleared)
            return;

        state = RoomState.Cleared;
        ActiveRooms.Remove(this);
        SetDoors(false);
        SpawnChestForType();
        EventBus.InvokeRoomCleared(roomType);
    }

    public static void NotifyAnyEnemyKilledInActiveRoom()
    {
        for (int i = ActiveRooms.Count - 1; i >= 0; i--)
        {
            if (ActiveRooms[i] == null)
                ActiveRooms.RemoveAt(i);
            else
                ActiveRooms[i].NotifyEnemyKilled();
        }
    }

    private void SpawnChestForType()
    {
        if (roomType == RoomType.Healing)
        {
            HealPlayer(0.35f);
            return;
        }

        if (chestPrefab == null)
            return;

        Vector3 pos = enemySpawnRoot != null ? enemySpawnRoot.position : transform.position;
        Instantiate(chestPrefab, pos, Quaternion.identity);
    }

    private static void HealPlayer(float pct)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;
        HealthSystem hs = player.GetComponent<HealthSystem>();
        if (hs != null)
            hs.Heal(hs.MaxHP * pct);
    }

    private void SetDoors(bool locked)
    {
        for (int i = 0; i < doorColliders.Length; i++)
        {
            if (doorColliders[i] != null)
                doorColliders[i].enabled = locked;
        }
    }
}
