using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ChestController : MonoBehaviour
{
    [Header("Hình rương (để trống = dùng Art/Tiles hoặc ArtSpriteSet)")]
    [SerializeField] private Sprite customChestSprite;
    [SerializeField] private float visualScale = 2.5f;

    private void Awake()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void OnEnable()
    {
        ApplyChestArt();
    }

    private void ApplyChestArt()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        // Dùng sprite rương thật (DungeonPack chestClosed → procedural chest).
        // KHÔNG dùng LoadTile(89) vì tile đó không tồn tại → trả về sprite vũ khí sai.
        Sprite chestSprite = customChestSprite;
        if (chestSprite == null)
            chestSprite = ArtSpriteLibrary.GetChestSprite();

        sr.sprite = chestSprite;
        sr.color = Color.white;
        sr.sortingOrder = 8;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.flipX = false;
        sr.flipY = false;
        // Chuẩn hoá theo chiều cao thế giới (giữ tỉ lệ, không méo/bể).
        GameScale.Fit(transform, sr.sprite, GameScale.ChestHeight);
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
