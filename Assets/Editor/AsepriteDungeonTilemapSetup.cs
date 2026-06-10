using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UObject = UnityEngine.Object;

/// <summary>
/// Slice + palette + vẽ dungeon từ Assets/ASEPRITE/ASEPRITE_MAP/PNG.
/// Palette: Assets/Art/Tiles/Palettes/WallsFloorPalette.prefab
/// </summary>
[InitializeOnLoad]
public static class AsepriteMapTilemapSetup
{
    public const int CellSize = 16;
    public const int PixelsPerUnit = 16;

    private const string PngFolder = "Assets/ASEPRITE/ASEPRITE_MAP/PNG";
    private const string TileDataRoot = "Assets/Art/Tiles/Palettes/TileData";
    private const string PaletteFolder = "Assets/Art/Tiles/Palettes";
    public const string PalettePath = PaletteFolder + "/WallsFloorPalette.prefab";
    public const string PaletteName = "WallsFloorPalette";
    private const string AnimatedPalettePath = PaletteFolder + "/AsepriteAnimatedPalette.prefab";
    private const string AnimatedPaletteName = "AsepriteAnimatedPalette";
    private const int PaletteColumns = 16;
    private const int AnimatedPaletteColumns = 8;

    private static readonly string[] TextureFiles =
    {
        "walls_floor.png",
        "decorative_cracks_floor.png",
        "decorative_cracks_walls.png",
        "decorative_cracks_coasts_animation.png",
        "doors_lever_chest_animation.png",
        "water_detilazation_v2.png",
        "Water_coasts_animation.png",
        "trap_animation.png",
        "fire_animation.png",
        "fire_animation2.png",
        "Objects.png"
    };

    private static readonly AnimDef[] AnimatedDefs =
    {
        new("fire_torch", "fire_animation", AsepriteAnimatedTileDefs.FireTorch),
        new("trap_spikes", "trap_animation", AsepriteAnimatedTileDefs.TrapSpikes),
        new("water_ripple", "water_detilazation_v2", AsepriteAnimatedTileDefs.WaterRipple),
        new("water_coast", "Water_coasts_animation", AsepriteAnimatedTileDefs.WaterCoast),
        new("wood_door", "doors_lever_chest_animation", AsepriteAnimatedTileDefs.WoodDoor)
    };

    private readonly struct AnimDef
    {
        public readonly string AssetName;
        public readonly string SheetId;
        public readonly int[] FrameIndices;

        public AnimDef(string assetName, string sheetId, int[] frameIndices)
        {
            AssetName = assetName;
            SheetId = sheetId;
            FrameIndices = frameIndices;
        }
    }

    [InitializeOnLoadMethod]
    private static void ScheduleAutoBuild()
    {
        EditorApplication.delayCall += TryAutoBuild;
    }

    private static void TryAutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string marker = $"{TileDataRoot}/Objects/Objects_0.asset";
        if (File.Exists(marker))
            return;

        Debug.Log("[AsepriteMap] Chưa đủ tile ASEPRITE — tự động build WallsFloorPalette + vẽ dungeon...");
        FullSetupInternal(showDialogs: false, paintFullDungeon: true);
    }

    [MenuItem("DungeonSoul/Map/ASEPRITE Full Setup")]
    public static void FullSetup() => FullSetupInternal(showDialogs: true, paintFullDungeon: true);

    [MenuItem("DungeonSoul/Map/ASEPRITE Full Setup Silent")]
    public static void FullSetupSilent() => FullSetupInternal(showDialogs: false, paintFullDungeon: true);

    public static void FullSetupInternal(bool showDialogs, bool paintFullDungeon)
    {
        ConfigureImportInternal(showDialogs);
        BuildTilesInternal(showDialogs);
        SetupSceneInternal(showDialogs);
        if (paintFullDungeon)
            PaintFullInternal(showDialogs);
        Debug.Log("[AsepriteMap] Xong! Palette: WallsFloorPalette @ " + PalettePath + " | PPU=" + PixelsPerUnit);
    }

    private static void ConfigureImportInternal(bool showDialogs)
    {
        int count = 0;
        for (int i = 0; i < TextureFiles.Length; i++)
        {
            string texPath = $"{PngFolder}/{TextureFiles[i]}";
            if (!File.Exists(texPath))
                continue;
            ConfigureImporter(texPath);
            GridSlice(texPath, CellSize);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (showDialogs)
            EditorUtility.DisplayDialog("ASEPRITE Map", $"Đã slice {count} PNG.\nPPU={PixelsPerUnit}, cell={CellSize}px", "OK");
    }

    private static void BuildTilesInternal(bool showDialogs)
    {
        EnsureFolder(TileDataRoot);
        EnsureFolder(PaletteFolder);

        List<TileGroup> groups = new List<TileGroup>();
        for (int i = 0; i < TextureFiles.Length; i++)
        {
            string texPath = $"{PngFolder}/{TextureFiles[i]}";
            if (!File.Exists(texPath))
                continue;
            string sheetId = Path.GetFileNameWithoutExtension(TextureFiles[i]);
            string folder = AsepriteSheetFolders.FolderFor(sheetId);
            List<TileBase> tiles = CreateTileAssets(sheetId, folder, texPath);
            if (tiles.Count > 0)
                groups.Add(new TileGroup(folder, tiles));
        }

        List<AnimatedTile> animatedTiles = CreateAnimatedTiles();
        foreach (AnimatedTile anim in animatedTiles)
            groups.Add(new TileGroup("Animated", new List<TileBase> { anim }));

        if (groups.Count == 0)
        {
            Debug.LogError("[AsepriteMap] Không tạo được tile.");
            return;
        }

        // Tách palette nhỏ — 1 palette ~4000 tile làm Tile Palette window trống (Unity 6 bug).
        var animatedGroups = groups.Where(g => g.Label == "Animated").ToList();
        var mainGroups = groups.Where(g =>
            g.Label is "WallsFloor" or "Objects" or "DoorsLeverChest").ToList();

        var decorGroups = groups.Where(g =>
            g.Label.StartsWith("Decorative", StringComparison.Ordinal)).ToList();

        var waterGroups = groups.Where(g =>
            g.Label is "WaterDetilazation" or "WaterCoasts" or "WaterCoasts_anim").ToList();

        CreateOrUpdatePalette(PalettePath, PaletteName, mainGroups);
        CreateOrUpdatePalette(PaletteFolder + "/AsepriteDecorPalette.prefab", "AsepriteDecorPalette", decorGroups);
        CreateOrUpdatePalette(PaletteFolder + "/AsepriteWaterPalette.prefab", "AsepriteWaterPalette", waterGroups);
        if (animatedGroups.Count > 0)
            CreateOrUpdatePalette(AnimatedPalettePath, AnimatedPaletteName, animatedGroups, AnimatedPaletteColumns);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialogs)
        {
            int total = groups.Sum(g => g.Tiles.Count);
            string summary = string.Join("\n", groups.Select(g => $"• {g.Label}: {g.Tiles.Count} tile"));
            EditorUtility.DisplayDialog("ASEPRITE Map",
                $"Tổng {total} tile → 4 palette:\n" +
                "• WallsFloorPalette (tường/sàn/props)\n" +
                "• AsepriteAnimatedPalette (lửa, bẫy, nước, cửa)\n" +
                "• AsepriteDecorPalette\n" +
                "• AsepriteWaterPalette\n\n" +
                $"{summary}\n\nPPU={PixelsPerUnit}",
                "OK");
        }
    }

    [MenuItem("DungeonSoul/Map/ASEPRITE Rebuild Palettes Only")]
    public static void RebuildPalettesOnly() => BuildTilesInternal(showDialogs: true);

    [MenuItem("DungeonSoul/Map/ASEPRITE 4 Paint Detailed Dungeon")]
    public static void PaintDetailedOnly() => PaintFullInternal(showDialogs: true);

    [MenuItem("DungeonSoul/Map/Clear Map For Manual Paint")]
    public static void ClearMapForManualPaint() => ClearMapInternal(showDialogs: true);

    private static void ClearMapInternal(bool showDialogs)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        GameObject grid = GameObject.Find("DungeonGrid");
        if (grid == null)
            return;

        Undo.RegisterFullObjectHierarchyUndo(grid, "Clear Map For Manual Paint");

        string[] layerNames = { "FloorLayer", "ShadowLayer", "DecorLayer", "WallLayer" };
        for (int i = 0; i < layerNames.Length; i++)
        {
            Transform layer = grid.transform.Find(layerNames[i]);
            if (layer == null)
                continue;

            Tilemap tilemap = layer.GetComponent<Tilemap>();
            if (tilemap == null)
                continue;

            tilemap.ClearAllTiles();
            tilemap.CompressBounds();
        }

        FullDungeonMapPainter painter = grid.GetComponent<FullDungeonMapPainter>();
        if (painter != null)
        {
            SerializedObject so = new SerializedObject(painter);
            so.FindProperty("movePlayerToSpawn").boolValue = false;
            so.ApplyModifiedProperties();
            painter.enabled = false;
        }

        KenneyStyleMapPainter kenney = grid.GetComponent<KenneyStyleMapPainter>();
        if (kenney != null)
            kenney.enabled = false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = Vector3.zero;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (showDialogs)
        {
            EditorUtility.DisplayDialog("DungeonSoul",
                "Đã xóa toàn bộ tile trên Floor/Shadow/Decor/Wall.\n\n" +
                "Vẽ map mới: Window → 2D → Tile Palette → chọn WallsFloorPalette.\n\n" +
                "Script tự vẽ map đã tắt.",
                "OK");
        }
    }

    private static List<AnimatedTile> CreateAnimatedTiles()
    {
        string animFolder = AsepriteAnimatedTileDefs.AnimFolder;
        EnsureFolder(animFolder);

        string[] old = AssetDatabase.FindAssets("t:AnimatedTile", new[] { animFolder });
        for (int o = 0; o < old.Length; o++)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(old[o]));

        List<AnimatedTile> created = new List<AnimatedTile>();
        for (int i = 0; i < AnimatedDefs.Length; i++)
        {
            AnimDef def = AnimatedDefs[i];
            string texPath = $"{PngFolder}/{def.SheetId}.png";
            if (!File.Exists(texPath))
                continue;

            List<Sprite> sprites = LoadSprites(texPath, def.SheetId);
            if (sprites.Count == 0)
                continue;

            List<Sprite> frames = new List<Sprite>();
            for (int f = 0; f < def.FrameIndices.Length; f++)
            {
                int idx = def.FrameIndices[f];
                if (idx >= 0 && idx < sprites.Count)
                    frames.Add(sprites[idx]);
            }

            if (frames.Count == 0)
                continue;

            AnimatedTile anim = ScriptableObject.CreateInstance<AnimatedTile>();
            anim.name = def.AssetName;
            anim.m_AnimatedSprites = frames.ToArray();
            anim.m_MinSpeed = AsepriteAnimatedTileDefs.AnimFps;
            anim.m_MaxSpeed = AsepriteAnimatedTileDefs.AnimFps;
            anim.m_AnimationStartTime = 0f;
            anim.m_TileColliderType = Tile.ColliderType.None;

            string assetPath = $"{animFolder}/{def.AssetName}.asset";
            AssetDatabase.CreateAsset(anim, assetPath);
            created.Add(anim);
            Debug.Log($"[AsepriteMap] AnimatedTile: {def.AssetName} ({frames.Count} frames @ {AsepriteAnimatedTileDefs.AnimFps} fps)");
        }

        return created;
    }

    private static List<Sprite> LoadSprites(string texturePath, string sheetId)
    {
        UObject[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;
            if (!sprite.name.StartsWith(sheetId + "_", StringComparison.OrdinalIgnoreCase))
                continue;
            sprites.Add(sprite);
        }

        return sprites
            .OrderBy(s => ParseIndex(s.name))
            .ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void SetupSceneInternal(bool showDialogs)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        GameObject grid = GameObject.Find("DungeonGrid") ?? CreateDungeonGrid();
        Undo.RegisterFullObjectHierarchyUndo(grid, "Setup ASEPRITE Tilemap Layers");
        EnsureLayer(grid.transform, "FloorLayer", 0, false, false);
        EnsureLayer(grid.transform, "ShadowLayer", 1, false, false);
        EnsureLayer(grid.transform, "DecorLayer", 3, true, false);
        EnsureLayer(grid.transform, "WallLayer", 4, true, true);

        EnemySpawner spawner = UObject.FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("floorTilemap").objectReferenceValue = grid.transform.Find("FloorLayer")?.GetComponent<Tilemap>();
            so.FindProperty("wallTilemap").objectReferenceValue = grid.transform.Find("WallLayer")?.GetComponent<Tilemap>();
            so.FindProperty("useManualSpawnPoints").boolValue = false;
            so.ApplyModifiedProperties();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<YSortRenderer>() == null)
            Undo.AddComponent<YSortRenderer>(player);

        if (grid.GetComponent<FullDungeonMapPainter>() == null)
            Undo.AddComponent<FullDungeonMapPainter>(grid);

        KenneyStyleMapPainter kenney = grid.GetComponent<KenneyStyleMapPainter>();
        if (kenney != null)
            kenney.enabled = false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (showDialogs)
            EditorUtility.DisplayDialog("ASEPRITE Map", "Scene layers OK.", "OK");
    }

    private static void PaintFullInternal(bool showDialogs)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        GameObject grid = GameObject.Find("DungeonGrid");
        if (grid == null)
            return;

        FullDungeonMapPainter painter = grid.GetComponent<FullDungeonMapPainter>();
        if (painter == null)
            painter = Undo.AddComponent<FullDungeonMapPainter>(grid);

        Undo.RegisterFullObjectHierarchyUndo(grid, "Paint Full ASEPRITE Dungeon");
        painter.PaintDetailedDungeon();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (showDialogs)
            EditorUtility.DisplayDialog("ASEPRITE Map", "Đã vẽ dungeon chi tiết 6 phòng + boss.", "OK");
    }

    private static GameObject CreateDungeonGrid()
    {
        GameObject grid = new GameObject("DungeonGrid");
        Grid g = grid.AddComponent<Grid>();
        g.cellSize = Vector3.one;
        return grid;
    }

    private static void ConfigureImporter(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void GridSlice(string texturePath, int cellSize)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        ITextureDataProvider textureProvider = provider.GetDataProvider<ITextureDataProvider>();
        Texture2D readable = textureProvider.GetReadableTexture2D();
        if (readable == null)
            return;

        int width = readable.width;
        int height = readable.height;
        string baseName = Path.GetFileNameWithoutExtension(texturePath);
        List<SpriteRect> rects = new List<SpriteRect>();
        int index = 0;

        for (int y = 0; y < height; y += cellSize)
        {
            for (int x = 0; x < width; x += cellSize)
            {
                int w = Mathf.Min(cellSize, width - x);
                int h = Mathf.Min(cellSize, height - y);
                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{index++}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(x, height - y - h, w, h),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                });
            }
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static List<TileBase> CreateTileAssets(string sheetId, string folderName, string texturePath)
    {
        string folder = $"{TileDataRoot}/{folderName}";
        EnsureFolder(folder);

        string[] oldGuids = AssetDatabase.FindAssets("t:Tile", new[] { folder });
        for (int i = 0; i < oldGuids.Length; i++)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(oldGuids[i]));

        List<Sprite> sprites = LoadSprites(texturePath, sheetId);
        List<TileBase> tiles = new List<TileBase>(sprites.Count);
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                Sprite sprite = sprites[i];
                string safeName = Sanitize(sprite.name);
                string tileAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}.asset");

                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.name = safeName;
                tile.colliderType = sheetId == "walls_floor"
                    ? Tile.ColliderType.Sprite
                    : Tile.ColliderType.None;

                AssetDatabase.CreateAsset(tile, tileAssetPath);
                tiles.Add(tile);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log($"[AsepriteMap] {folderName}: {tiles.Count} tiles @ PPU {PixelsPerUnit}");
        return tiles;
    }

    private static void CreateOrUpdatePalette(string palettePath, string paletteName, List<TileGroup> groups, int columns = PaletteColumns)
    {
        if (!File.Exists(palettePath))
        {
            GameObject created = GridPaletteUtility.CreateNewPalette(
                PaletteFolder,
                paletteName,
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Automatic,
                new Vector3(1f, 1f, 0f),
                GridLayout.CellSwizzle.XYZ);

            if (created != null)
                UObject.DestroyImmediate(created);

            AssetDatabase.Refresh();
        }

        if (!File.Exists(palettePath))
        {
            Debug.LogError("[AsepriteMap] Không tạo được palette tại " + palettePath);
            return;
        }

        GameObject paletteRoot = PrefabUtility.LoadPrefabContents(palettePath);
        Tilemap tilemap = paletteRoot.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
            PrefabUtility.UnloadPrefabContents(paletteRoot);
            Debug.LogError("[AsepriteMap] Palette missing Tilemap.");
            return;
        }

        tilemap.ClearAllTiles();

        int x = 0;
        int y = 0;
        for (int g = 0; g < groups.Count; g++)
        {
            TileGroup group = groups[g];
            for (int i = 0; i < group.Tiles.Count; i++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), group.Tiles[i]);
                x++;
                if (x >= columns)
                {
                    x = 0;
                    y--;
                }
            }

            if (g < groups.Count - 1)
            {
                x = 0;
                y--;
            }
        }

        tilemap.CompressBounds();
        tilemap.RefreshAllTiles();
        EditorUtility.SetDirty(tilemap);

        PrefabUtility.SaveAsPrefabAsset(paletteRoot, palettePath);
        PrefabUtility.UnloadPrefabContents(paletteRoot);

        int total = groups.Sum(gr => gr.Tiles.Count);
        Debug.Log($"[AsepriteMap] {paletteName}: {total} tile ({groups.Count} nhóm) → {palettePath}");
    }

    private static void EnsureLayer(Transform parent, string name, int sortOrder, bool individual, bool collider)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        if (existing == null)
        {
            go.transform.SetParent(parent, false);
            go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
        }

        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortOrder;
        renderer.mode = individual ? TilemapRenderer.Mode.Individual : TilemapRenderer.Mode.Chunk;

        if (collider && go.GetComponent<TilemapWallSetup>() == null)
            go.AddComponent<TilemapWallSetup>();
    }

    private static int ParseIndex(string name)
    {
        int underscore = name.LastIndexOf('_');
        if (underscore < 0 || underscore + 1 >= name.Length)
            return 0;
        return int.TryParse(name.Substring(underscore + 1), out int idx) ? idx : 0;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private sealed class TileGroup
    {
        public string Label { get; }
        public List<TileBase> Tiles { get; }

        public TileGroup(string label, List<TileBase> tiles)
        {
            Label = label;
            Tiles = tiles;
        }
    }
}
