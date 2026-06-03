using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ChestController : MonoBehaviour
{
    private void Awake()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        SkillSelectionUI ui = SkillSelectionUI.GetOrFind();
        if (ui != null)
            ui.ShowChest(RoomType.Normal);

        gameObject.SetActive(false);
    }
}
