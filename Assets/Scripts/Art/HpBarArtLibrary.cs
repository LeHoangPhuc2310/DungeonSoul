// DungeonSoul — HpBarArtLibrary.cs — Sprite thanh máu từ pack zip (Art/HP_Bar).

using UnityEngine;

public static class HpBarArtLibrary
{
    private static Sprite bg;
    private static Sprite fillHigh;
    private static Sprite fillLow;

    public static Sprite Background => bg ??= Load("bg");
    public static Sprite FillHigh => fillHigh ??= Load("red");
    public static Sprite FillLow => fillLow ??= Load("green");

    private static Sprite Load(string name)
    {
        Sprite fromResources = Resources.Load<Sprite>($"UI/HPBar/{name}");
        if (fromResources != null)
            return fromResources;

#if UNITY_EDITOR
        string path = $"Assets/Art/HP_Bar/{name}.png";
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }
}
