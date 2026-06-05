// DungeonSoul — EventBus.cs — Static game events (boss, room, run).

using System;

public static class EventBus
{
    public static event Action OnBossDefeated;
    public static event Action<int> OnBossPhaseChanged;
    public static event Action<RoomType> OnRoomEntered;
    public static event Action<RoomType> OnRoomCleared;

    public static void InvokeBossDefeated() => OnBossDefeated?.Invoke();

    public static void InvokeBossPhaseChanged(int phaseIndex) => OnBossPhaseChanged?.Invoke(phaseIndex);

    public static void InvokeRoomEntered(RoomType type) => OnRoomEntered?.Invoke(type);

    public static void InvokeRoomCleared(RoomType type) => OnRoomCleared?.Invoke(type);
}
