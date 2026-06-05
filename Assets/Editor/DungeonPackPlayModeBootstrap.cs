#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class DungeonPackPlayModeBootstrap
{
    static DungeonPackPlayModeBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (!AssetDatabase.IsValidFolder("Assets/2D Pixel Dungeon Asset Pack"))
            return;

        DungeonPackSpriteSet set = Resources.Load<DungeonPackSpriteSet>("DungeonPackSpriteSet");
        if (set != null && DungeonPackLibrary.HasValidCoinSpinInSet(set))
            return;

        DungeonPackSetupEditor.BuildSilent();
        DungeonPackLibrary.InvalidateCache();
    }
}
#endif
