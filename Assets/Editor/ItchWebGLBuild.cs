#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Build Web (Unity 6) / WebGL + zip sẵn để upload lên itch.io.</summary>
public static class ItchWebGLBuild
{
    private const string OutputFolder = "Builds/WebGL";
    private const string ZipPath = "Builds/DungeonSoul-itch-webgl.zip";

    [MenuItem("DungeonSoul/Build/Web for itch.io (Browser Play)")]
    public static void BuildForItch()
    {
        if (!IsWebBuildInstalled())
        {
            EditorUtility.DisplayDialog(
                "Dungeon Soul",
                "Chưa cài module build Web.\n\n"
                + "Unity Hub → Installs → Unity 6.4 → Add modules\n"
                + "→ tick \"Web Build Support\" → Continue\n\n"
                + "Sau đó mở lại project và build lại.",
                "OK");
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.WebGL, BuildTarget.WebGL);
            if (!switched)
            {
                EditorUtility.DisplayDialog(
                    "Dungeon Soul",
                    "Không chuyển được sang platform Web.\n"
                    + "Thử: File → Build Profiles → chọn Web → Switch Platform.",
                    "OK");
                return;
            }
        }

        ApplyWebGLSettings();

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Dungeon Soul", "Không có scene nào trong Build Settings.", "OK");
            return;
        }

        if (Directory.Exists(OutputFolder))
            Directory.Delete(OutputFolder, true);

        Directory.CreateDirectory("Builds");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputFolder,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        Debug.Log("[ItchWebGLBuild] Đang build Web (trình duyệt)… có thể mất 5–15 phút.");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Soul",
                "Build Web thất bại.\n\nXem tab Console (Ctrl+Shift+C) để biết lỗi chi tiết.",
                "OK");
            return;
        }

        CreateItchZip();

        EditorUtility.RevealInFinder(Path.GetFullPath(ZipPath));
        EditorUtility.DisplayDialog(
            "Dungeon Soul",
            "Build xong!\n\n"
            + "File upload itch.io:\n"
            + ZipPath
            + "\n\nTrên itch.io:\n"
            + "• Kind: HTML\n"
            + "• Upload file .zip\n"
            + "• Tick \"This file will be played in the browser\"\n"
            + "• Publish → gửi link trang game",
            "OK");
    }

    [MenuItem("DungeonSoul/Build/Switch Platform to Web")]
    public static void SwitchToWeb()
    {
        if (!IsWebBuildInstalled())
        {
            EditorUtility.DisplayDialog(
                "Dungeon Soul",
                "Cài \"Web Build Support\" trong Unity Hub trước.",
                "OK");
            return;
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        Debug.Log("[ItchWebGLBuild] Platform hiện tại: Web (Unity 6 — tên cũ là WebGL).");
    }

    private static bool IsWebBuildInstalled()
    {
        return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);
    }

    private static void ApplyWebGLSettings()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 600;
        PlayerSettings.runInBackground = true;
    }

    private static void CreateItchZip()
    {
        if (File.Exists(ZipPath))
            File.Delete(ZipPath);

        string indexHtml = Path.Combine(OutputFolder, "index.html");
        if (!File.Exists(indexHtml))
        {
            Debug.LogError("[ItchWebGLBuild] Không tìm thấy index.html trong " + OutputFolder);
            return;
        }

        ZipFile.CreateFromDirectory(OutputFolder, ZipPath, CompressionLevel.Optimal, false);
        Debug.Log("[ItchWebGLBuild] Created " + ZipPath);
    }
}
#endif
