#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class EnemyAnimationPlayModeBootstrap
{
    static EnemyAnimationPlayModeBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (Resources.Load<EnemyAnimationDatabase>("EnemyAnimations/Database") != null)
            return;

        EnemyAnimationBuilder.BuildAllSilent();
        EnemyVisualLibrary.InvalidateCache();
    }
}
#endif
