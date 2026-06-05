using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneRunReset
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetState();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetState();
    }

    private static void ResetState()
    {
        Time.timeScale = 1f;
        EnemyVisualLibrary.InvalidateCache();
        DungeonPackLibrary.InvalidateCache();
        RunManager.Instance?.ResetForNewRun();
        HUDManager.Resolve()?.ResetForNewRun();
    }
}
