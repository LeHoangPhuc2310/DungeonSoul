#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Clears stuck UI and forces Play to start at Character Select.</summary>
[InitializeOnLoad]
internal static class EditorPlayModeFix
{
    static EditorPlayModeFix()
    {
        EditorApplication.delayCall += OnDelayCall;
    }

    private static void OnDelayCall()
    {
        EditorUtility.ClearProgressBar();
        CharacterSelectSceneSetup.EnsurePlayStartsAtCharacterSelect();
    }
}
#endif
