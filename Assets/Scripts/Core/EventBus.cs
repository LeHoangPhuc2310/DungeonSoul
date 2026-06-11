// DungeonSoul — EventBus.cs — Static game events (boss, room, run, combat).
// ĐIỂM CẮM CHUẨN cho chức năng mới: muốn "khi X xảy ra thì làm Y" → subscribe event,
// KHÔNG sửa file phát event (HealthSystem, EnemySpawner, ExpSystem, RunManager).
// Subscriber nhớ unsubscribe trong OnDisable/OnDestroy (event static sống xuyên scene).

using System;
using UnityEngine;

/// <summary>Dữ liệu khi một quái/boss chết. GameObject sẽ bị Destroy ngay sau frame này — đừng giữ tham chiếu.</summary>
public readonly struct EnemyKilledInfo
{
    public readonly GameObject Enemy;
    public readonly Vector3 Position;
    public readonly float MaxHp;
    public readonly bool IsBoss;

    public EnemyKilledInfo(GameObject enemy, Vector3 position, float maxHp, bool isBoss)
    {
        Enemy = enemy;
        Position = position;
        MaxHp = maxHp;
        IsBoss = isBoss;
    }
}

public static class EventBus
{
    // ----- Boss / Room (có sẵn) -----
    public static event Action OnBossDefeated;
    public static event Action<int> OnBossPhaseChanged;
    public static event Action<RoomType> OnRoomEntered;
    public static event Action<RoomType> OnRoomCleared;

    // ----- Combat / Run (điểm cắm cho chức năng mới: quest, combo, analytics, drop...) -----
    /// <summary>Một quái (kể cả boss) vừa chết. Phát từ HealthSystem.Die.</summary>
    public static event Action<EnemyKilledInfo> OnEnemyKilled;
    /// <summary>Wave mới bắt đầu (1-based). Phát từ EnemySpawner.</summary>
    public static event Action<int> OnWaveStarted;
    /// <summary>Player vừa lên cấp. Phát từ ExpSystem.</summary>
    public static event Action<int> OnPlayerLevelUp;
    /// <summary>Run kết thúc (true = thắng). Phát từ RunManager.EndRun.</summary>
    public static event Action<bool> OnRunEnded;

    public static void InvokeBossDefeated() => OnBossDefeated?.Invoke();

    public static void InvokeBossPhaseChanged(int phaseIndex) => OnBossPhaseChanged?.Invoke(phaseIndex);

    public static void InvokeRoomEntered(RoomType type) => OnRoomEntered?.Invoke(type);

    public static void InvokeRoomCleared(RoomType type) => OnRoomCleared?.Invoke(type);

    public static void InvokeEnemyKilled(EnemyKilledInfo info) => OnEnemyKilled?.Invoke(info);

    public static void InvokeWaveStarted(int waveIndex) => OnWaveStarted?.Invoke(waveIndex);

    public static void InvokePlayerLevelUp(int level) => OnPlayerLevelUp?.Invoke(level);

    public static void InvokeRunEnded(bool victory) => OnRunEnded?.Invoke(victory);
}
