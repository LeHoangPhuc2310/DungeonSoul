#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EffectPreviewWindow : EditorWindow
{
    private EffectKind effectKind = EffectKind.FireExplosion;
    private float worldSize = 2f;
    private float fps = 6f;
    private bool previewLoop;
    private float lastAppliedFps = -1f;
    private Vector2 scroll;

    private Sprite[] animFrames;
    private double animStartTime;
    private bool animating;

    [MenuItem("DungeonSoul/Art/Effect Preview")]
    public static void Open()
    {
        EffectPreviewWindow window = GetWindow<EffectPreviewWindow>("Effect Preview");
        window.minSize = new Vector2(300f, 360f);
    }

    private void OnEnable()
    {
        EditorApplication.update += RepaintIfAnimating;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintIfAnimating;
        EffectPreviewPlayer.Stop();
        animating = false;
    }

    private void RepaintIfAnimating()
    {
        if (animating)
            Repaint();
    }

    public void BeginPreview(Sprite[] frames, float size, float frameRate, bool loop = false)
    {
        animFrames = frames;
        worldSize = size;
        fps = frameRate;
        previewLoop = loop;
        animStartTime = EditorApplication.timeSinceStartup;
        animating = true;
        lastAppliedFps = frameRate;
        EffectPreviewPlayer.Play(frames, worldSize, fps, loop);
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Xem trước hiệu ứng sprite", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Chọn sprite sheet (Walk, Attack, Idle…) → chuột phải → DungeonSoul → Preview Sprite Animation.\n" +
            "Hoặc chọn hiệu ứng → bấm Preview. Animation chạy trong cửa sổ này + Scene view.",
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        effectKind = (EffectKind)EditorGUILayout.EnumPopup("Hiệu ứng", effectKind);
        worldSize = EditorGUILayout.Slider("Kích thước (Scene)", worldSize, 0.5f, 6f);
        float newFps = EditorGUILayout.Slider("FPS (chậm ← → nhanh)", fps, 2f, 16f);
        EditorGUILayout.LabelField("Tốc độ", newFps.ToString("0.0") + " fps");
        if (animating && animFrames != null && Mathf.Abs(newFps - lastAppliedFps) > 0.05f)
        {
            fps = newFps;
            lastAppliedFps = newFps;
            animStartTime = EditorApplication.timeSinceStartup;
            EffectPreviewPlayer.Play(animFrames, worldSize, fps, previewLoop);
        }
        else
        {
            fps = newFps;
        }

        Sprite[] frames = EffectLibrary.GetFrames(effectKind);
        EditorGUILayout.LabelField("Số frame", frames != null ? frames.Length.ToString() : "0");

        DrawPreviewBox(frames);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = frames != null && frames.Length > 0;
            if (GUILayout.Button("Preview", GUILayout.Height(32f)))
                BeginPreview(frames, worldSize, fps);

            if (GUILayout.Button("Stop", GUILayout.Height(32f), GUILayout.Width(64f)))
            {
                animating = false;
                animFrames = null;
                EffectPreviewPlayer.Stop();
            }

            GUI.enabled = true;
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawPreviewBox(Sprite[] frames)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        Rect box = GUILayoutUtility.GetRect(260f, 180f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(box, new Color(0.12f, 0.12f, 0.12f, 1f));

        Sprite[] source = animating && animFrames != null && animFrames.Length > 0 ? animFrames : frames;
        if (source == null || source.Length == 0)
        {
            GUI.Label(box, "Bấm Preview để xem animation", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        int index = 0;
        if (animating)
        {
            double elapsed = EditorApplication.timeSinceStartup - animStartTime;
            index = Mathf.FloorToInt((float)(elapsed * fps)) % source.Length;
        }

        Sprite sprite = source[index];
        if (sprite == null)
            return;

        float pad = 12f;
        float size = Mathf.Min(box.width, box.height) - pad * 2f;
        Rect dest = new Rect(
            box.x + (box.width - size) * 0.5f,
            box.y + (box.height - size) * 0.5f,
            size,
            size);
        EffectPreviewDrawer.DrawSprite(dest, sprite);

        Rect label = new Rect(box.x + 6f, box.yMax - 20f, box.width - 12f, 16f);
        GUI.Label(label, $"Frame {index + 1}/{source.Length}", EditorStyles.miniLabel);
    }
}

internal static class EffectPreviewDrawer
{
    public static void DrawSprite(Rect dest, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(
            tr.x / tex.width,
            tr.y / tex.height,
            tr.width / tex.width,
            tr.height / tex.height);
        GUI.DrawTextureWithTexCoords(dest, tex, uv, true);
    }
}

public static class EffectPreviewPlayer
{
    private static GameObject previewRoot;
    private static SpriteRenderer previewRenderer;
    private static Sprite[] frames;
    private static float frameDuration;
    private static int frameIndex;
    private static double playStartTime;
    private static bool playing;
    private static bool loopPlayback;
    private static readonly List<EditorApplication.CallbackFunction> stopWaiters = new List<EditorApplication.CallbackFunction>();

    [InitializeOnLoadMethod]
    private static void CleanupOrphanPreviewOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            GameObject orphan = GameObject.Find("~EffectPreview");
            if (orphan != null)
                Object.DestroyImmediate(orphan);
            Stop();
        };
    }

    [MenuItem("Assets/DungeonSoul/Preview Sprite Animation", false, 1999)]
    public static void PreviewSelectedSpriteSheet()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        Sprite[] sprites = LoadSpritesAtPath(path);
        if (sprites == null || sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Sprite Preview", "Không tìm thấy frame. Kiểm tra Texture Type = Sprite (2D), Sprite Mode = Multiple.", "OK");
            return;
        }

        float fps = GuessFpsFromName(path);
        OpenAndPlay(sprites, 1.2f, fps, loop: true);
    }

    [MenuItem("Assets/DungeonSoul/Preview Sprite Animation", true)]
    public static bool ValidatePreviewSelectedSpriteSheet()
    {
        return HasMultipleSpritesAtPath(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Assets/DungeonSoul/Preview Effect", false, 2000)]
    public static void PreviewSelectedAsset()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        Sprite[] sprites = LoadSpritesAtPath(path);
        if (sprites == null || sprites.Length == 0)
        {
            EditorUtility.DisplayDialog("Effect Preview", "Không tìm thấy sprite frame trong file này.", "OK");
            return;
        }

        OpenAndPlay(sprites, 2f, 20f, loop: false);
    }

    [MenuItem("Assets/DungeonSoul/Preview Effect", true)]
    public static bool ValidatePreviewSelectedAsset()
    {
        Object obj = Selection.activeObject;
        if (obj == null)
            return false;

        string path = AssetDatabase.GetAssetPath(obj);
        return !string.IsNullOrEmpty(path)
            && path.StartsWith("Assets/Art/Sprite/Effect")
            && (path.EndsWith(".png") || path.EndsWith(".PNG"));
    }

    private static void OpenAndPlay(Sprite[] spriteFrames, float size, float frameRate, bool loop)
    {
        EffectPreviewWindow window = EditorWindow.GetWindow<EffectPreviewWindow>("Sprite Preview");
        window.minSize = new Vector2(300f, 360f);
        window.BeginPreview(spriteFrames, size, frameRate, loop);
    }

    public static void Play(Sprite[] spriteFrames, float size, float frameRate, bool loop = false)
    {
        Play(spriteFrames, size, frameRate, loop, 0f);
    }

    public static void Play(Sprite[] spriteFrames, float size, float frameRate, bool loop, float rotationZ)
    {
        Stop();
        if (spriteFrames == null || spriteFrames.Length == 0)
            return;

        frames = spriteFrames;
        frameDuration = 1f / Mathf.Max(1f, frameRate);
        frameIndex = 0;
        playStartTime = EditorApplication.timeSinceStartup;
        playing = true;
        loopPlayback = loop;

        previewRoot = EditorUtility.CreateGameObjectWithHideFlags(
            "~EffectPreview",
            HideFlags.HideInHierarchy | HideFlags.DontSave | HideFlags.NotEditable,
            typeof(SpriteRenderer));
        previewRoot.transform.position = SceneViewCenter();
        previewRoot.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);

        previewRenderer = previewRoot.GetComponent<SpriteRenderer>();
        previewRenderer.sprite = frames[0];
        previewRenderer.sortingOrder = 200;
        ApplyScale(frames[0], size);

        // Không select object preview — Inspector sẽ lỗi SerializedObject null.
        FramePreviewInSceneView();
        EditorApplication.update += Tick;

        if (!loopPlayback)
        {
            float life = Mathf.Max(frameDuration, frames.Length * frameDuration) + 0.15f;
            ScheduleStop(life);
        }
    }

    public static void Stop()
    {
        playing = false;
        EditorApplication.update -= Tick;
        ClearStopWaiters();

        if (previewRoot != null)
        {
            if (Selection.activeGameObject == previewRoot)
                Selection.activeGameObject = null;

            Object.DestroyImmediate(previewRoot);
            previewRoot = null;
            previewRenderer = null;
        }

        frames = null;
        SceneView.RepaintAll();
    }

    private static void Tick()
    {
        if (!playing || previewRenderer == null || frames == null || frames.Length <= 1)
            return;

        double elapsed = EditorApplication.timeSinceStartup - playStartTime;
        int targetFrame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt((float)(elapsed / frameDuration)));

        if (targetFrame != frameIndex)
        {
            frameIndex = targetFrame;
            previewRenderer.sprite = frames[frameIndex];
        }

        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();

        if (elapsed >= frames.Length * frameDuration)
        {
            if (loopPlayback)
                playStartTime = EditorApplication.timeSinceStartup;
            else
                Stop();
        }
    }

    private static void ScheduleStop(float delay)
    {
        double stopAt = EditorApplication.timeSinceStartup + delay;
        EditorApplication.CallbackFunction waiter = null;
        waiter = () =>
        {
            if (!playing)
            {
                EditorApplication.update -= waiter;
                stopWaiters.Remove(waiter);
                return;
            }

            if (EditorApplication.timeSinceStartup >= stopAt)
            {
                EditorApplication.update -= waiter;
                stopWaiters.Remove(waiter);
                Stop();
            }
        };

        stopWaiters.Add(waiter);
        EditorApplication.update += waiter;
    }

    private static void ClearStopWaiters()
    {
        for (int i = 0; i < stopWaiters.Count; i++)
            EditorApplication.update -= stopWaiters[i];
        stopWaiters.Clear();
    }

    private static void FramePreviewInSceneView()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view == null || previewRenderer == null)
            return;

        view.Focus();
        Bounds bounds = previewRenderer.bounds;
        if (bounds.size.sqrMagnitude > 0.0001f)
            view.Frame(bounds, false);
        else
            view.LookAt(previewRoot.transform.position, view.rotation, Mathf.Max(1f, view.size));

        view.Repaint();
    }

    private static Vector3 SceneViewCenter()
    {
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
            return view.pivot;

        return Vector3.zero;
    }

    private static void ApplyScale(Sprite sprite, float targetHeight)
    {
        if (sprite == null || previewRoot == null)
            return;

        float h = sprite.bounds.size.y;
        float s = h > 0.001f ? targetHeight / h : targetHeight;
        s = Mathf.Clamp(s, 0.05f, 20f);
        previewRoot.transform.localScale = Vector3.one * s;
    }

    private static bool HasMultipleSpritesAtPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            return false;

        Sprite[] sprites = LoadSpritesAtPath(path);
        return sprites != null && sprites.Length > 1;
    }

    private static float GuessFpsFromName(string path)
    {
        string lower = path.ToLowerInvariant();
        if (lower.Contains("attack") || lower.Contains("hurt") || lower.Contains("death"))
            return 5f;
        if (lower.Contains("walk") || lower.Contains("idle"))
            return 4f;
        if (lower.Contains("block"))
            return 4f;
        return 5f;
    }

    private static Sprite[] LoadSpritesAtPath(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
            return System.Array.Empty<Sprite>();

        sprites.Sort((a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
        return sprites.ToArray();
    }

    private static int FrameIndex(string name)
    {
        if (string.IsNullOrEmpty(name))
            return 0;

        int i = name.Length - 1;
        while (i >= 0 && !char.IsDigit(name[i]))
            i--;

        int end = i + 1;
        while (i >= 0 && char.IsDigit(name[i]))
            i--;

        if (end > i + 1 && int.TryParse(name.Substring(i + 1, end - i - 1), out int n))
            return n;

        return 0;
    }
}
#endif
