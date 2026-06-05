using UnityEngine;

public static class HeroVisualLibrary
{
    public const float TargetWorldHeight = 2.4f;

    public static float ResolveDisplayScale(Sprite sprite, float fallbackScale = 2f)
    {
        return ResolveDisplayScale(sprite, TargetWorldHeight, fallbackScale);
    }

    public static float ResolveDisplayScale(Sprite sprite, float targetHeight, float fallbackScale)
    {
        if (sprite == null)
            return fallbackScale;

        float height = sprite.bounds.size.y;
        if (height < 0.02f)
            return fallbackScale;

        return targetHeight / height;
    }
}
