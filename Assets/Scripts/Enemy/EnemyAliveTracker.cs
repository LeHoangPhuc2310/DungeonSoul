// DungeonSoul — EnemyAliveTracker.cs — Đếm quái sống (tránh FindGameObjectsWithTag mỗi frame, mobile).

using UnityEngine;

public static class EnemyAliveTracker
{
    public static int Count { get; private set; }

    public static void Reset(int value = 0)
    {
        Count = Mathf.Max(0, value);
    }

    public static void Add(int delta)
    {
        Count = Mathf.Max(0, Count + delta);
    }
}
