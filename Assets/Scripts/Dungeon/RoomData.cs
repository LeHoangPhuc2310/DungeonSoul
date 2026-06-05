// DungeonSoul — RoomData.cs — Room type config (enemies, chest, traps).

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomData", menuName = "DungeonSoul/Room Data")]
public class RoomData : ScriptableObject
{
    public RoomType roomType = RoomType.Normal;
    public int minEnemies = 8;
    public int maxEnemies = 12;
    public bool trapEnabled;
    public RoomChestKind chestType = RoomChestKind.Normal;

    [Tooltip("Optional weighted enemy prefabs — empty uses spawner default.")]
    public List<GameObject> enemyPool = new List<GameObject>();
}

public enum RoomChestKind
{
    Normal,
    Treasure,
    Red,
    None
}
