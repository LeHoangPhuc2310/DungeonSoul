#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Xem trước burst + aura của cả 28 SkillType trong Editor (không cần Play mode).</summary>
public class SkillVfxPreviewWindow : EditorWindow
{
    private const string BurstRoot = "Assets/Resources/GeneratedSkillVfx/PerSkill";
    private const string AuraRoot = "Assets/Resources/GeneratedSkillVfx/Auras/Skills";
    private const int ExpectedBurstFrames = 6;
    private const int ExpectedAuraFrames = 4;

    private static readonly SkillType[] AllSkills = (SkillType[])Enum.GetValues(typeof(SkillType));
    private static readonly HashSet<SkillType> SkillsWithAura = new HashSet<SkillType>
    {
        SkillType.SpeedBoost, SkillType.IronBody, SkillType.ToughSkin, SkillType.CoinMagnet,
        SkillType.IceAura, SkillType.GhostForm, SkillType.BladeStorm, SkillType.Vampire,
        SkillType.PoisonCloud, SkillType.LightningChain, SkillType.DeathMark, SkillType.TimeFreeze,
        SkillType.DragonStrike, SkillType.SoulHarvest, SkillType.MirrorImage
    };

    private Vector2 gridScroll;
    private SkillType selected = SkillType.FireArrow;
    private float burstScale = 1.25f;
    private float auraScale = 1.1f;
    private float fps = 14f;
    private bool previewLoop;
    private bool playAllSlideshow;
    private double playAllNextAt;
    private int playAllIndex;
    private double animStartTime;
    private bool animating;
    private Sprite[] animFrames;
    private bool animLoop;

    [MenuItem("Tools/DungeonSoul/Skill VFX Preview (All 28)")]
    public static void Open()
    {
        SkillVfxPreviewWindow window = GetWindow<SkillVfxPreviewWindow>("Skill VFX Preview");
        window.minSize = new Vector2(520f, 640f);
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopAllPreview();
    }

    private void OnEditorUpdate()
    {
        if (playAllSlideshow)
            TickPlayAll();

        if (animating)
            Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Preview toàn bộ VFX kỹ năng (28 skill)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Chọn skill trong lưới → xem burst/aura trong cửa sổ này và Scene view.\n" +
            "「Play All」 tự chạy lần lượt từng skill. 「Play In Game」 cần đang Play mode + có Player.",
            MessageType.Info);

        DrawSummaryBar();
        EditorGUILayout.Space(4f);

        gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.Height(220f));
        DrawSkillGrid();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);
        DrawSelectedDetail();
    }

    private void DrawSummaryBar()
    {
        int burstOk = 0, burstBad = 0, auraOk = 0, auraMissing = 0;
        foreach (SkillType type in AllSkills)
        {
            int burst = LoadBurstFrames(type).Length;
            if (burst >= ExpectedBurstFrames) burstOk++;
            else burstBad++;

            if (SkillsWithAura.Contains(type))
            {
                int aura = LoadAuraFrames(type).Length;
                if (aura >= ExpectedAuraFrames) auraOk++;
                else auraMissing++;
            }
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Burst OK: {burstOk}/28", GUILayout.Width(100f));
        EditorGUILayout.LabelField($"Thiếu burst: {burstBad}", GUILayout.Width(110f));
        EditorGUILayout.LabelField($"Aura OK: {auraOk}/15", GUILayout.Width(100f));
        EditorGUILayout.LabelField($"Thiếu aura: {auraMissing}", GUILayout.Width(110f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillGrid()
    {
        const int columns = 4;
        float cellW = (position.width - 24f) / columns;
        int row = 0;
        int col = 0;

        EditorGUILayout.BeginHorizontal();
        foreach (SkillType type in AllSkills)
        {
            if (col >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                col = 0;
                row++;
            }

            DrawSkillCell(type, cellW);
            col++;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSkillCell(SkillType type, float width)
    {
        int burstCount = LoadBurstFrames(type).Length;
        bool burstOk = burstCount >= ExpectedBurstFrames;
        bool wantsAura = SkillsWithAura.Contains(type);
        int auraCount = wantsAura ? LoadAuraFrames(type).Length : 0;
        bool auraOk = !wantsAura || auraCount >= ExpectedAuraFrames;

        bool isSelected = selected == type;
        Color bg = isSelected
            ? new Color(0.25f, 0.45f, 0.75f, 0.35f)
            : burstOk && auraOk
                ? new Color(0.2f, 0.55f, 0.3f, 0.12f)
                : new Color(0.75f, 0.3f, 0.2f, 0.15f);

        Rect rect = GUILayoutUtility.GetRect(width - 4f, 72f, GUILayout.Width(width - 4f));
        EditorGUI.DrawRect(rect, bg);

        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        {
            selected = type;
            BeginWindowAnim(LoadBurstFrames(type), false);
        }

        float pad = 4f;
        Rect iconRect = new Rect(rect.x + pad, rect.y + pad, 28f, 28f);
        Sprite icon = GeneratedPixelIconLibrary.Skill(type);
        if (icon != null)
            EffectPreviewDrawer.DrawSprite(iconRect, icon);
        else
            EditorGUI.DrawRect(iconRect, new Color(0.3f, 0.3f, 0.3f));

        Rect nameRect = new Rect(rect.x + 34f, rect.y + 4f, rect.width - 38f, 16f);
        GUI.Label(nameRect, type.ToString(), EditorStyles.miniLabel);

        string status = burstOk ? $"B:{burstCount}" : $"B:{burstCount}!";
        if (wantsAura)
            status += auraOk ? $" A:{auraCount}" : $" A:{auraCount}!";

        Rect statusRect = new Rect(rect.x + 34f, rect.y + 22f, rect.width - 38f, 14f);
        GUI.Label(statusRect, status, EditorStyles.centeredGreyMiniLabel);

        Rect styleRect = new Rect(rect.x + 4f, rect.yMax - 18f, rect.width - 8f, 14f);
        GUI.Label(styleRect, SkillVfxLibrary.MapSkill(type).ToString(), EditorStyles.miniLabel);
    }

    private void DrawSelectedDetail()
    {
        EditorGUILayout.LabelField(selected.ToString(), EditorStyles.boldLabel);

        Sprite[] burst = LoadBurstFrames(selected);
        Sprite[] aura = LoadAuraFrames(selected);
        bool hasAura = SkillsWithAura.Contains(selected);

        EditorGUILayout.LabelField("Burst PerSkill",
            burst.Length >= ExpectedBurstFrames
                ? $"{burst.Length} frame ✓"
                : burst.Length > 0
                    ? $"{burst.Length} frame (cần {ExpectedBurstFrames})"
                    : "THIẾU — chạy Bake Skill VFX");

        if (hasAura)
        {
            EditorGUILayout.LabelField("Aura loop",
                aura.Length >= ExpectedAuraFrames
                    ? $"{aura.Length} frame ✓"
                    : aura.Length > 0
                        ? $"{aura.Length} frame (cần {ExpectedAuraFrames})"
                        : "THIẾU aura");
        }
        else
        {
            EditorGUILayout.LabelField("Aura loop", "— (buff thường, không có aura)");
        }

        burstScale = EditorGUILayout.Slider("Burst scale (Scene)", burstScale, 0.5f, 3f);
        auraScale = EditorGUILayout.Slider("Aura scale (Scene)", auraScale, 0.5f, 3f);
        fps = EditorGUILayout.Slider("FPS", fps, 4f, 24f);

        DrawPreviewBoxes(burst, aura, hasAura);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = burst.Length > 0;
            if (GUILayout.Button("Preview Burst", GUILayout.Height(30f)))
                PreviewBurst();

            GUI.enabled = hasAura && aura.Length > 0;
            if (GUILayout.Button("Preview Aura", GUILayout.Height(30f)))
                PreviewAura();

            GUI.enabled = selected == SkillType.LightningChain && burst.Length > 0;
            if (GUILayout.Button("Preview Directed", GUILayout.Height(30f)))
                PreviewDirected(burst);

            GUI.enabled = true;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(playAllSlideshow ? "■ Stop Play All" : "▶ Play All (slideshow)", GUILayout.Height(28f)))
            {
                if (playAllSlideshow)
                    StopPlayAll();
                else
                    StartPlayAll();
            }

            if (GUILayout.Button("Stop Scene Preview", GUILayout.Height(28f), GUILayout.Width(140f)))
                StopAllPreview();

            if (GUILayout.Button("Ping Folder", GUILayout.Height(28f), GUILayout.Width(100f)))
                PingBurstFolder(selected);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Play In Game (tại Player)", GUILayout.Height(28f)))
                PlayInGame();

            GUI.enabled = true;
            if (GUILayout.Button("Bake lại VFX", GUILayout.Height(28f), GUILayout.Width(110f)))
            {
                if (EditorUtility.DisplayDialog("Bake Skill VFX",
                        "Xóa và bake lại toàn bộ PerSkill + Auras? (mất vài giây)", "Bake", "Hủy"))
                {
                    SkillVfxBaker.BakeAll();
                    AssetDatabase.Refresh();
                    Repaint();
                }
            }
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Vào Play mode để test VFX đúng như in-game (Play In Game).", MessageType.None);
    }

    private void DrawPreviewBoxes(Sprite[] burst, Sprite[] aura, bool hasAura)
    {
        EditorGUILayout.BeginHorizontal();
        float halfW = (position.width - 40f) * 0.5f;
        DrawAnimBox("Burst", burst, 200f, hasAura ? halfW : position.width - 20f);
        if (hasAura)
            DrawAnimBox("Aura", aura, 200f, halfW);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAnimBox(string label, Sprite[] frames, float height, float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width));
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

        Rect box = GUILayoutUtility.GetRect(10f, height, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(box, new Color(0.1f, 0.1f, 0.12f));

        Sprite[] source = animating && animFrames == frames && frames != null && frames.Length > 0
            ? frames
            : frames;

        if (source == null || source.Length == 0)
        {
            GUI.Label(box, "Không có frame", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        int index = 0;
        if (animating && animFrames == frames)
        {
            double elapsed = EditorApplication.timeSinceStartup - animStartTime;
            index = animLoop
                ? Mathf.FloorToInt((float)(elapsed * fps)) % source.Length
                : Mathf.Min(source.Length - 1, Mathf.FloorToInt((float)(elapsed * fps)));
        }

        Sprite sprite = source[index];
        if (sprite != null)
        {
            float pad = 10f;
            float size = Mathf.Min(box.width, box.height) - pad * 2f;
            Rect dest = new Rect(
                box.x + (box.width - size) * 0.5f,
                box.y + (box.height - size) * 0.5f,
                size, size);
            EffectPreviewDrawer.DrawSprite(dest, sprite);
        }

        GUI.Label(new Rect(box.x + 6f, box.yMax - 18f, box.width - 12f, 16f),
            $"Frame {index + 1}/{source.Length}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void BeginWindowAnim(Sprite[] frames, bool loop)
    {
        animFrames = frames;
        animLoop = loop;
        previewLoop = loop;
        animStartTime = EditorApplication.timeSinceStartup;
        animating = frames != null && frames.Length > 0;
    }

    private void PreviewBurst()
    {
        Sprite[] frames = LoadBurstFrames(selected);
        float scale = selected == SkillType.DragonStrike ? burstScale * 1.4f : burstScale;
        BeginWindowAnim(frames, false);
        EffectPreviewPlayer.Play(frames, scale, fps, loop: false);
    }

    private void PreviewAura()
    {
        Sprite[] frames = LoadAuraFrames(selected);
        BeginWindowAnim(frames, true);
        EffectPreviewPlayer.Play(frames, auraScale, 7f, loop: true);
    }

    private void PreviewDirected(Sprite[] burst)
    {
        BeginWindowAnim(burst, false);
        // Tia dọc sprite → xoay -90° để nằm ngang như LightningChain in-game.
        EffectPreviewPlayer.Play(burst, Mathf.Clamp(burstScale * 2f, 1f, 4f), fps, loop: false, rotationZ: -90f);
    }

    private void StartPlayAll()
    {
        playAllSlideshow = true;
        playAllIndex = 0;
        playAllNextAt = EditorApplication.timeSinceStartup;
        PlayAllStep();
    }

    private void StopPlayAll()
    {
        playAllSlideshow = false;
    }

    private void TickPlayAll()
    {
        if (EditorApplication.timeSinceStartup < playAllNextAt)
            return;

        playAllIndex++;
        if (playAllIndex >= AllSkills.Length)
        {
            StopPlayAll();
            return;
        }

        PlayAllStep();
    }

    private void PlayAllStep()
    {
        selected = AllSkills[playAllIndex];
        Sprite[] burst = LoadBurstFrames(selected);
        float scale = selected == SkillType.DragonStrike ? burstScale * 1.4f : burstScale;
        double duration = burst.Length > 0
            ? burst.Length / Mathf.Max(1f, fps) + 0.35
            : 0.8;

        BeginWindowAnim(burst, false);
        if (burst.Length > 0)
            EffectPreviewPlayer.Play(burst, scale, fps, loop: false);

        playAllNextAt = EditorApplication.timeSinceStartup + duration;
        Repaint();
    }

    private void StopAllPreview()
    {
        animating = false;
        animFrames = null;
        playAllSlideshow = false;
        EffectPreviewPlayer.Stop();
    }

    private void PlayInGame()
    {
        Transform player = FindPlayerTransform();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Skill VFX Preview",
                "Không tìm thấy Player trong scene. Chạy SampleScene và bấm Play.", "OK");
            return;
        }

        SkillVfxLibrary.PlayForSkill(selected, player.position, burstScale);
    }

    private static Transform FindPlayerTransform()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            return tagged.transform;

        PlayerController pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        return pc != null ? pc.transform : null;
    }

    private static void PingBurstFolder(SkillType type)
    {
        string path = $"{BurstRoot}/{type}";
        UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (folder != null)
        {
            EditorGUIUtility.PingObject(folder);
            Selection.activeObject = folder;
            return;
        }

        EditorUtility.DisplayDialog("Skill VFX Preview", $"Không có folder: {path}", "OK");
    }

    private static Sprite[] LoadBurstFrames(SkillType type) =>
        LoadSpritesAtFolder($"{BurstRoot}/{type}");

    private static Sprite[] LoadAuraFrames(SkillType type) =>
        LoadSpritesAtFolder($"{AuraRoot}/{type}");

    private static Sprite[] LoadSpritesAtFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return Array.Empty<Sprite>();

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        if (guids == null || guids.Length == 0)
            return Array.Empty<Sprite>();

        List<Sprite> sprites = new List<Sprite>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null)
                sprites.Add(s);
        }

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
