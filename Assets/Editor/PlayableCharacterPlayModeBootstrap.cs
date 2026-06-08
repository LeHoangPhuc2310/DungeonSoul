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

        if (!DatabaseNeedsRebuild())
            return;

        PlayableCharacterBuilder.BuildSilent();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        if (!DatabaseNeedsRebuild())
            return;

        PlayableCharacterBuilder.BuildSilent();
    }

    private static bool DatabaseNeedsRebuild()
    {
        PlayableCharacterDatabase db =
            UnityEngine.Resources.Load<PlayableCharacterDatabase>("PlayableCharacters/Database");
        if (db == null || db.entries == null || db.entries.Count == 0)
            return true;

        for (int i = 0; i < db.entries.Count; i++)
        {
            PlayableCharacterEntry entry = db.entries[i];
            if (entry == null || !entry.HasAttackAnimation)
                return true;
        }

        return false;
    }
}
#endif
