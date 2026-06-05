using UnityEngine;

/// <summary>Shared setup for spinning coin pickups (EXP gem + loot coin).</summary>
public static class WorldPickupVisual
{
    public const float DefaultCoinWorldSize = GameScale.CoinSize;

    public static bool TrySetupSpinningCoin(GameObject target, float worldSize = DefaultCoinWorldSize, bool rareTint = false)
    {
        if (target == null)
            return false;

        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = target.AddComponent<SpriteRenderer>();

        CoinPickupAnimator anim = target.GetComponent<CoinPickupAnimator>();
        if (anim == null)
            anim = target.AddComponent<CoinPickupAnimator>();

        Sprite[] frames = DungeonPackLibrary.GetCoinSpinFrames();
        if (frames == null || frames.Length == 0)
            return false;

        anim.SetFrames(frames);
        sr.color = rareTint ? new Color(0.75f, 0.9f, 1f, 1f) : Color.white;
        sr.sortingOrder = 12;

        float height = Mathf.Max(0.02f, frames[0].bounds.size.y);
        float scale = worldSize / height;
        target.transform.localScale = Vector3.one * (rareTint ? scale * 1.12f : scale);
        return true;
    }
}
