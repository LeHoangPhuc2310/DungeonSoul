// DungeonSoul — RedChestController.cs — Boss reward chest (skill pick).

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RedChestController : MonoBehaviour
{
    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        SkillSelectionUI ui = SkillSelectionUI.GetOrFind();
        if (ui != null)
            ui.ShowChest(RoomType.Treasure);

        Destroy(gameObject);
    }
}
