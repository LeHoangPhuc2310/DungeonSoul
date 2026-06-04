using UnityEngine;
using UnityEngine.Tilemaps;

// Attach to any Tilemap that should act as a solid wall.
// Automatically adds TilemapCollider2D + CompositeCollider2D + Static Rigidbody2D
// if they are missing, ensuring the player cannot walk through.
[RequireComponent(typeof(Tilemap))]
public class TilemapWallSetup : MonoBehaviour
{
    private void Awake()
    {
        // Static Rigidbody required by CompositeCollider2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        TilemapCollider2D tileCol = GetComponent<TilemapCollider2D>();
        if (tileCol == null)
            tileCol = gameObject.AddComponent<TilemapCollider2D>();
        tileCol.isTrigger = false;
        tileCol.usedByComposite = true;

        CompositeCollider2D composite = GetComponent<CompositeCollider2D>();
        if (composite == null)
            composite = gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.isTrigger = false;
    }
}
