// DungeonSoul — RoomClearBridge.cs — Báo room clear khi diệt quái / boss.

using UnityEngine;

public class RoomClearBridge : MonoBehaviour
{
    public static RoomClearBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.OnBossDefeated += OnBossDefeated;
    }

    private void OnDisable()
    {
        EventBus.OnBossDefeated -= OnBossDefeated;
    }

    public void OnEnemyKilled()
    {
        RoomController.NotifyAnyEnemyKilledInActiveRoom();
    }

    private void OnBossDefeated()
    {
        EventBus.InvokeRoomCleared(RoomType.Boss);
    }
}
