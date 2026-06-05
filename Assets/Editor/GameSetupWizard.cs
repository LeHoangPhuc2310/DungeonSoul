// DungeonSoul — GameSetupWizard.cs — Một lần bấm: asset + mobile + GameManagers.

using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class GameSetupWizard
{
    [MenuItem("DungeonSoul/Setup/Full Project Setup (Mobile + GDD)")]
    public static void FullSetup()
    {
        BossAssetCreator.CreateAll();
        ArtSpriteSetBuilder.BuildArtSpriteSet();
        DungeonPackSetupEditor.BuildSilent();
        if (!GameUIFontBuilder.TryCreateTimesNewRomanAsset(out string fontMessage))
            Debug.LogWarning("[GameSetupWizard] Font: " + fontMessage);

        ConfigureMobilePlayerSettings();
        EnsureGameManagersInScene();
        KenneyMapPainterMenu.PaintKenneyMap();
        EnemyAnimationBuilder.BuildAll();

        EditorUtility.DisplayDialog(
            "Dungeon Soul",
            "Đã tạo Boss/Meta/Font assets và gắn GameRunBootstrap.\n\n" +
            "Trong scene game:\n" +
            "• GameManagers → Run Mode: Wave Arena (mặc định) hoặc Procedural Dungeon\n" +
            "• 2D Pixel Dungeon Pack → HUD/rương/xu/hero\n" +
            "• Map Kenney đã vẽ (menu Map → Paint Kenney Demo Map)\n" +
            "• Gán Floor/Wall Tilemap cho DungeonGenerator nếu dùng dungeon\n" +
            "• Build Android/iOS: Portrait, IL2CPP",
            "OK");
    }

    [MenuItem("DungeonSoul/Setup/Configure Mobile Player Settings")]
    public static void ConfigureMobilePlayerSettings()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, true);
        PlayerSettings.SetMobileMTRendering(NamedBuildTarget.iOS, true);
        Debug.Log("[GameSetupWizard] Mobile player settings applied (portrait).");
    }

    [MenuItem("DungeonSoul/Setup/Ensure GameManagers In Active Scene")]
    public static void EnsureGameManagersInScene()
    {
        GameRunBootstrap bootstrap = Object.FindAnyObjectByType<GameRunBootstrap>(FindObjectsInactive.Include);
        if (bootstrap == null)
        {
            GameObject go = GameObject.Find("GameManagers") ?? new GameObject("GameManagers");
            bootstrap = go.GetComponent<GameRunBootstrap>() ?? go.AddComponent<GameRunBootstrap>();
            EditorUtility.SetDirty(go);
        }

        Debug.Log("[GameSetupWizard] GameRunBootstrap ready on: " + bootstrap.gameObject.name);
    }
}
