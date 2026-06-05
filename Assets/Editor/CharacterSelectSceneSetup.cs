// DungeonSoul — CharacterSelectSceneSetup.cs — Tạo CharacterSelectScene và cấu hình Build Settings.
// Menu: DungeonSoul → Setup → Tạo Character Select Scene

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CharacterSelectSceneSetup
{
    private const string SceneFolder = "Assets/Scenes";
    private const string SelectScenePath = "Assets/Scenes/CharacterSelectScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    [MenuItem("DungeonSoul/Setup/Tạo Character Select Scene")]
    public static void CreateCharacterSelectScene()
    {
        if (!AssetDatabase.IsValidFolder(SceneFolder))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        // Nếu scene đã tồn tại thì chỉ cập nhật Build Settings.
        if (!System.IO.File.Exists(SelectScenePath))
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera tối giản cho scene UI.
            GameObject camGo = new GameObject("Main Camera");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.11f, 1f);
            cam.orthographic = true;
            camGo.tag = "MainCamera";

            // GameObject mang CharacterSelectUI — script tự dựng UI lúc Play.
            GameObject uiGo = new GameObject("CharacterSelectUI");
            uiGo.AddComponent<CharacterSelectUI>();

            EditorSceneManager.SaveScene(scene, SelectScenePath);
            Debug.Log("[CharacterSelectSceneSetup] Đã tạo scene: " + SelectScenePath);
        }
        else
        {
            Debug.Log("[CharacterSelectSceneSetup] Scene đã tồn tại, chỉ cập nhật Build Settings.");
        }

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SetPlayModeStartScene();

        EditorUtility.DisplayDialog("Character Select",
            "Đã tạo/cập nhật CharacterSelectScene và Build Settings.\n\n" +
            "Mỗi lần bấm Play sẽ bắt đầu từ màn chọn nhân vật (dù đang mở scene nào).\n\n" +
            "Thứ tự scene:\n" + DescribeBuildOrder(), "OK");
    }

    /// <summary>
    /// Đặt scene khởi đầu khi bấm Play trong Editor = scene đầu Build Settings
    /// (MainMenu nếu có, không thì CharacterSelectScene). Nhờ vậy luôn vào đúng flow
    /// dù đang mở SampleScene.
    /// </summary>
    [MenuItem("DungeonSoul/Setup/Đặt Play bắt đầu từ màn đầu")]
    public static void SetPlayModeStartScene()
    {
        EnsurePlayStartsAtCharacterSelect();
    }

    /// <summary>Luôn bắt đầu Play từ CharacterSelectScene (bắt buộc chọn nhân vật).</summary>
    public static void EnsurePlayStartsAtCharacterSelect()
    {
        if (!System.IO.File.Exists(SelectScenePath))
            return;

        SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SelectScenePath);
        if (startScene == null)
            return;

        if (EditorSceneManager.playModeStartScene != startScene)
        {
            EditorSceneManager.playModeStartScene = startScene;
            Debug.Log("[CharacterSelectSceneSetup] Play Mode Start Scene = " + SelectScenePath);
        }
    }

    [MenuItem("DungeonSoul/Setup/Bỏ Play Mode Start Scene")]
    public static void ClearPlayModeStartScene()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[CharacterSelectSceneSetup] Đã bỏ Play Mode Start Scene — Play sẽ chạy scene đang mở.");
    }

    /// <summary>
    /// Đảm bảo thứ tự build: [MainMenu nếu có] → CharacterSelectScene → SampleScene.
    /// </summary>
    private static void ConfigureBuildSettingsIfNeeded()
    {
        List<EditorBuildSettingsScene> ordered = BuildOrderedScenes();
        if (BuildSettingsMatch(ordered))
            return;

        EditorBuildSettings.scenes = ordered.ToArray();
        Debug.Log("[CharacterSelectSceneSetup] Build order: " + DescribeBuildOrder());
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = BuildOrderedScenes().ToArray();
        Debug.Log("[CharacterSelectSceneSetup] Build order: " + DescribeBuildOrder());
    }

    private static List<EditorBuildSettingsScene> BuildOrderedScenes()
    {
        List<EditorBuildSettingsScene> ordered = new List<EditorBuildSettingsScene>();

        if (System.IO.File.Exists(MainMenuScenePath))
            ordered.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));

        ordered.Add(new EditorBuildSettingsScene(SelectScenePath, true));

        if (System.IO.File.Exists(SampleScenePath))
            ordered.Add(new EditorBuildSettingsScene(SampleScenePath, true));

        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing.path == MainMenuScenePath || existing.path == SelectScenePath
                || existing.path == SampleScenePath)
                continue;
            ordered.Add(existing);
        }

        return ordered;
    }

    private static bool BuildSettingsMatch(List<EditorBuildSettingsScene> desired)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (current.Length != desired.Count)
            return false;

        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path != desired[i].path || current[i].enabled != desired[i].enabled)
                return false;
        }

        return true;
    }

    private static string DescribeBuildOrder()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < scenes.Length; i++)
            sb.AppendLine(i + ": " + System.IO.Path.GetFileNameWithoutExtension(scenes[i].path));
        return sb.ToString();
    }
}
