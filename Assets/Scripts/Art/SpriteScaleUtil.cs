// DungeonSoul — SpriteScaleUtil.cs — Chuẩn hoá kích thước mọi sprite về cùng chiều cao thế giới.
// Giải quyết vấn đề "cái to cái nhỏ" do mỗi nguồn sprite có PPU/kích thước frame khác nhau.

using UnityEngine;

public static class SpriteScaleUtil
{
    /// <summary>
    /// Tính localScale để sprite hiển thị đúng chiều cao thế giới mong muốn.
    /// Dùng bounds.size.y (đã tính theo PPU) — nhất quán cho mọi nguồn sprite.
    /// </summary>
    public static float ScaleForHeight(Sprite sprite, float targetWorldHeight)
    {
        if (sprite == null)
            return 1f;

        float h = sprite.bounds.size.y;
        if (h <= 0.001f)
            return 1f;

        return targetWorldHeight / h;
    }

    /// <summary>Đặt localScale đồng đều cho transform sao cho sprite cao đúng targetWorldHeight.</summary>
    public static void FitHeight(Transform t, Sprite sprite, float targetWorldHeight)
    {
        if (t == null)
            return;
        float s = ScaleForHeight(sprite, targetWorldHeight);
        t.localScale = new Vector3(s, s, 1f);
    }
}
