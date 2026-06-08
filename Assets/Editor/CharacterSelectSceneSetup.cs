// DungeonSoul — CharacterSelectSceneSetup.cs — Tạo đủ flow scene + Build Settings.
// Flow: MainMenu (0) → CharacterSelect (1) → WeaponSelect (2) → SampleScene (3).
// Menu: DungeonSoul → Setup → Tạo Đầy Đủ Flow Màn Hình

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CharacterSelectSceneSetup
{
    private const string SceneFolder = "Assets/Scenes";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SelectScenePath = "Assets/Scenes/CharacterSelectScene.unity";
    private const string WeaponScenePath = "Assets/Scenes/WeaponSelectScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("DungeonSoul/Setup/Tạo Đầy Đủ Flow Màn Hình")]
    public static void CreateFullFlow()
    {
        if (!AssetDatabase.IsValidFolder(SceneFolder))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        // Tạo các scene UI còn thiếu (Additive — không đóng scene đang mở).
        EnsureUiScene(MainMenuScenePath, "MainMenuManager", typeof(MainMenuManager));
        EnsureUiScene(SelectScenePath, "CharacterSelectUI", typeof(CharacterSelectUI));
        EnsureUiScene(WeaponScenePath, "WeaponSelectUI", typeof(WeaponSelectUI));

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SetPlayModeStartScene();

        EditorUtility.DisplayDialog("DungeonSoul Flow",
            "Đã tạo/cập nhật flow màn hình:\n\n" + DescribeBuildOrder() +
            "\nBấm Play sẽ bắt đầu từ MainMenu.", "OK");
    }

    // Giữ menu cũ để tương thích.
    [MenuItem("DungeonSoul/Setup/Tạo Character Select Scene")]
    public static void CreateCharacterSelectScene() => CreateFullFlow();

    private static void EnsureUiScene(string path, string goName, System.Type uiComponent)
    {
        if (System.IO.File.Exists(path))
            return;

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
        cam.orthographic = true;
        camGo.tag = "MainCamera";
        EditorSceneManager.MoveGameObjectToScene(camGo, scene);

        GameObject uiGo = new GameObject(goName);
        uiGo.AddComponent(uiComponent);
        EditorSceneManager.MoveGameObjectToScene(uiGo, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[FlowSetup] Đã tạo scene: " + path);
    }

    [MenuItem("DungeonSoul/Setup/Đặt Play bắt đầu từ MainMenu")]
    public static void SetPlayModeStartScene()
    {
        // Bắt đầu Play từ MainMenu nếu có, không thì CharacterSelect.
        string startPath = System.IO.File.Exists(MainMenuScenePath) ? MainMenuScenePath : SelectScenePath;
        if (!System.IO.File.Exists(startPath))
            return;

        SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(startPath);
        if (startScene != null && EditorSceneManager.playModeStartScene != startScene)
        {
            EditorSceneManager.playModeStartScene = startScene;
            Debug.Log("[FlowSetup] Play Mode Start Scene = " + startPath);
        }
    }

    [MenuItem("DungeonSoul/Setup/Bỏ Play Mode Start Scene")]
    public static void ClearPlayModeStartScene()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[FlowSetup] Đã bỏ Play Mode Start Scene.");
    }

    private static void ConfigureBuildSettings()
    {
        EditorBuildSettings.scenes = BuildOrderedScenes().ToArray();
        Debug.Log("[FlowSetup] Build order: " + DescribeBuildOrder());
    }

    /// <summary>Thứ tự: MainMenu → CharacterSelect → WeaponSelect → SampleScene.</summary>
    private static List<EditorBuildSettingsScene> BuildOrderedScenes()
    {
        List<EditorBuildSettingsScene> ordered = new List<EditorBuildSettingsScene>();
        AddIfExists(ordered, MainMenuScenePath);
        AddIfExists(ordered, SelectScenePath);
        AddIfExists(ordered, WeaponScenePath);
        AddIfExists(ordered, SampleScenePath);

        // Giữ scene khác đã có trong build (không trùng).
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing.path == MainMenuScenePath || existing.path == SelectScenePath
                || existing.path == WeaponScenePath || existing.path == SampleScenePath)
                continue;
            ordered.Add(existing);
        }

        return ordered;
    }

    private static void AddIfExists(List<EditorBuildSettingsScene> list, string path)
    {
        if (System.IO.File.Exists(path))
            list.Add(new EditorBuildSettingsScene(path, true));
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
