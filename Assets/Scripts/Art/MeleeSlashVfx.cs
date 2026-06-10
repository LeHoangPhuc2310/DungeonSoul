// DungeonSoul — MeleeSlashVfx.cs — Vệt chém nhỏ theo hướng đánh (bổ sung animation body).

using UnityEngine;

public static class MeleeSlashVfx
{
    private const float BaseScale = 0.5f;

    /// <summary>Vệt chém tại điểm trúng — không đè lên sprite nhân vật.</summary>
    public static void PlayAtHit(Vector3 hitPosition, Vector2 facing, float scaleMultiplier = 1f)
    {
        Vector2 dir = facing.sqrMagnitude > 0.01f ? facing.normalized : Vector2.down;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Color tint = new Color(1f, 0.95f, 0.78f, 0.55f);
        SkillVfxLibrary.Play(SkillVfxStyle.Slash, hitPosition, BaseScale * scaleMultiplier, tint, 22, angle);
    }
}
