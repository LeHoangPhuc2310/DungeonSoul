// DungeonSoul — BossRoom.cs — Lock doors on enter, unlock + chest on boss death.

using UnityEngine;

public class BossRoom : MonoBehaviour
{
    [SerializeField] private Collider2D[] doorColliders;
    [SerializeField] private BossController roomBoss;
    [SerializeField] private GameObject redChestSpawnPoint;

    private bool playerInside;
    private bool cleared;

    private void OnEnable()
    {
        EventBus.OnBossDefeated += OnBossDefeated;
    }

    private void OnDisable()
    {
        EventBus.OnBossDefeated -= OnBossDefeated;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (cleared || !other.CompareTag("Player"))
            return;

        playerInside = true;
        LockDoors(true);

        if (roomBoss != null && roomBoss.GetComponent<HealthSystem>() != null)
            BossHPBarUI.Track(roomBoss.GetComponent<HealthSystem>(), roomBoss.Data != null ? roomBoss.Data.bossName : "Boss");
    }

    private void OnBossDefeated()
    {
        if (!playerInside || cleared)
            return;

        cleared = true;
        LockDoors(false);
    }

    private void LockDoors(bool locked)
    {
        for (int i = 0; i < doorColliders.Length; i++)
        {
            if (doorColliders[i] != null)
                doorColliders[i].enabled = locked;
        }
    }
}
