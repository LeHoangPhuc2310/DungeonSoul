// DungeonSoul — DungeonRunController.cs — Chạy map BSP + teleport player.

using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonRunController : MonoBehaviour
{
    [SerializeField] private DungeonGenerator generator;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private GameObject roomTriggerPrefab;

    public void BeginDungeonRun()
    {
        if (generator == null)
            generator = Object.FindAnyObjectByType<DungeonGenerator>();
        if (generator == null)
        {
            GameObject go = new GameObject("DungeonGenerator");
            generator = go.AddComponent<DungeonGenerator>();
        }

        generator.Generate();

        if (spawner == null)
            spawner = Object.FindAnyObjectByType<EnemySpawner>();

        TeleportPlayerToSpawn(generator.SpawnCell, generator);
        SpawnRoomTriggers(generator);

        RoomManager room = RoomManager.Instance;
        if (room != null)
            room.SetWaveMode(false);

        if (spawner != null)
            spawner.BeginNextWave();
    }

    private static void TeleportPlayerToSpawn(Vector2Int cell, DungeonGenerator gen)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        Tilemap map = gen.GetComponent<DungeonGenerator>() != null
            ? Object.FindAnyObjectByType<Tilemap>()
            : null;

        Vector3 world = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        if (map != null)
            world = map.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

        player.transform.position = world;
    }

    private void SpawnRoomTriggers(DungeonGenerator gen)
    {
        var rooms = gen.Rooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            DungeonRoomNode node = rooms[i];
            Vector3 center = new Vector3(node.centerCell.x + 0.5f, node.centerCell.y + 0.5f, 0f);

            GameObject go = roomTriggerPrefab != null
                ? Instantiate(roomTriggerPrefab, center, Quaternion.identity)
                : CreateDefaultRoomTrigger(center);

            RoomController rc = go.GetComponent<RoomController>();
            if (rc == null)
                rc = go.AddComponent<RoomController>();
            rc.Configure(node.roomType, null);
        }
    }

    private static GameObject CreateDefaultRoomTrigger(Vector3 pos)
    {
        GameObject go = new GameObject("Room_" + pos);
        go.transform.position = pos;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(8f, 6f);
        return go;
    }
}
