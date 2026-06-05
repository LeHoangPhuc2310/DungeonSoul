#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
internal static class PlayableCharacterPlayModeBootstrap
{
    static PlayableCharacterPlayModeBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += TryBuildIfMissing;
    }

    private static void TryBuildIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (UnityEngine.Resources.Load<PlayableCharacterDatabase>("PlayableCharacters/Database") != null)
            return;

        PlayableCharacterBuilder.BuildSilent();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (UnityEngine.Resources.Load<PlayableCharacterDatabase>("PlayableCharacters/Database") != null)
            return;

        PlayableCharacterBuilder.BuildSilent();
    }
}
#endif
