using System.Collections.Generic;

/// <summary>Thẻ đã loại khỏi pool cả run (Banish kiểu Vampire Survivors).</summary>
public static class BanishRegistry
{
    private static readonly HashSet<string> banishedKeys = new HashSet<string>();
    private static int banishesUsed;

    public static int BanishesUsed => banishesUsed;

    public static void ResetForNewRun()
    {
        banishedKeys.Clear();
        banishesUsed = 0;
    }

    public static bool IsBanished(string key)
    {
        return !string.IsNullOrEmpty(key) && banishedKeys.Contains(key);
    }

    public static bool TryBanish(string key, int maxPerRun)
    {
        if (string.IsNullOrEmpty(key) || banishedKeys.Contains(key))
            return false;

        if (maxPerRun > 0 && banishesUsed >= maxPerRun)
            return false;

        banishedKeys.Add(key);
        banishesUsed++;
        return true;
    }
}
