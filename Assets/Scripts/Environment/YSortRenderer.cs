using UnityEngine;

/// <summary>Sắp xếp sprite theo trục Y — tường Tilemap (Individual) có thể che nhân vật phía trên.</summary>
public class YSortRenderer : MonoBehaviour
{
    [SerializeField] private int orderOffset;
    [SerializeField] private int sortMultiplier = 100;
    [SerializeField] private SpriteRenderer targetRenderer;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (targetRenderer == null)
            return;

        targetRenderer.sortingOrder = orderOffset - Mathf.RoundToInt(transform.position.y * sortMultiplier);
    }
}
