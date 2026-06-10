#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using UObject = UnityEngine.Object;

/// <summary>Builds Tile Palette prefabs from project tilesets.</summary>
public static class TilePaletteFactory
{
    private const string PaletteFolder = "Assets/Art/Tiles/Palettes";
    private const string TileDataFolder = "Assets/Art/Tiles/Palettes/TileData";
    private const int DefaultCellSize = 16;
    private const int PaletteColumns = 16;

    private struct PaletteSource
    {
        public string id;
        public string texturePath;
        public string tileFolder;
        public int gridCellSize;
        public bool skipLargePreviewSprite;
        public int minSpriteSize;
    }

    private static readonly PaletteSource[] Sources =
    {
        new PaletteSource
        {
            id = "KenneyTiles",
            tileFolder = "Assets/Art/Tiles/Data",
            gridCellSize = 0,
            minSpriteSize = 4
        },
        new PaletteSource
        {
            id = "DungeonPack",
            texturePath = "Assets/2D Pixel Dungeon Asset Pack/character and tileset/Dungeon_Tileset.png",
            gridCellSize = 16,
            skipLargePreviewSprite = true,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "DungeonPackAT",
            texturePath = "Assets/2D Pixel Dungeon Asset Pack/Dungeon_Tileset_at.png",
            gridCellSize = 16,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "Tileset11",
            texturePath = "Assets/Art/TileSets/Tileset 11 Sheet.png",
            gridCellSize = 16,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "CursedLandGround",
            texturePath = "Assets/Art/TileSets/Free-Cursed-Land-Top-Down-Pixel-Art-Tileset/PNG/Ground.png",
            gridCellSize = 16,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "UndeadGround",
            texturePath = "Assets/Art/TileSets/Free-Undead-Tileset-Top-Down-Pixel-Art/PNG/Ground_rocks.png",
            gridCellSize = 16,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "RpgStone",
            texturePath = "Assets/Art/TileSets/rpgstonetilemap.png",
            gridCellSize = 16,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "BigTile",
            texturePath = "Assets/Art/TileSets/Big Tile.png",
            gridCellSize = 16,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "DesertTile",
            texturePath = "Assets/Art/TileSets/Desert Tile.png",
            gridCellSize = 16,
            minSpriteSize = 8
        },
        new PaletteSource
        {
            id = "DungeonPackItems",
            texturePath = "Assets/2D Pixel Dungeon Asset Pack/items and trap_animation",
            gridCellSize = 0,
            minSpriteSize = 6
        },
        new PaletteSource
        {
            id = "WallsFloor",
            texturePath = "Assets/ASEPRITE/ASEPRITE_MAP/PNG/walls_floor.png",
            gridCellSize = 16,
            minSpriteSize = 8
        }
    };

    [MenuItem("DungeonSoul/Map/Generate All Tile Palettes")]
    public static void GenerateAllWithDialog()
    {
        int count = GenerateAll();
        EditorUtility.DisplayDialog(
            "Dungeon Soul",
            $"Đã tạo/cập nhật {count} Tile Palette.\n\nMở Tile Palette → dropdown để chọn palette mới.\nThư mục: {PaletteFolder}",
            "OK");
    }

    [MenuItem("DungeonSoul/Map/Generate All Tile Palettes (Silent)")]
    public static int GenerateAllSilent() => GenerateAll();

    [MenuItem("DungeonSoul/Map/Slice + Build WallsFloor Tiles (16x16)")]
    public static void GenerateWallsFloor()
    {
        EnsureFolder(PaletteFolder);
        EnsureFolder(TileDataFolder);

        PaletteSource source = FindSource("WallsFloor");

        // walls_floor.png ships with 7 auto-sliced clusters; force a fresh 16x16 grid slice
        // so the painter has uniform tiles to work with.
        EnsureGridSlice(source.texturePath, source.gridCellSize, forceReslice: true);

        bool ok = BuildPalette(source);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Dungeon Soul", ok
            ? $"Đã slice walls_floor.png thành ô 16x16 và tạo tile asset.\nThư mục: {TileDataFolder}/WallsFloor"
            : "Không tạo được WallsFloor tiles — xem Console.", "OK");
    }

    [MenuItem("DungeonSoul/Map/Generate Dungeon Pack Items Palette")]
    public static void GenerateItemsOnly()
    {
        EnsureFolder(PaletteFolder);
        EnsureFolder(TileDataFolder);
        bool ok = BuildPalette(FindSource("DungeonPackItems"));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Dungeon Soul", ok
            ? "Đã tạo DungeonPackItemsPalette."
            : "Không tạo được DungeonPackItemsPalette — xem Console.", "OK");
    }

    public static int GenerateAll()
    {
        EnsureFolder(PaletteFolder);
        EnsureFolder(TileDataFolder);

        int built = 0;
        try
        {
            for (int i = 0; i < Sources.Length; i++)
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Tile Palettes", Sources[i].id, (float)i / Sources.Length);
                    if (BuildPalette(Sources[i]))
                        built++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TilePaletteFactory] Failed {Sources[i].id}: {ex.Message}");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TilePaletteFactory] Built {built} palettes in {PaletteFolder}");
        return built;
    }

    private static PaletteSource FindSource(string id)
    {
        for (int i = 0; i < Sources.Length; i++)
        {
            if (Sources[i].id == id)
                return Sources[i];
        }

        return default;
    }

    private static bool BuildPalette(PaletteSource source)
    {
        List<TileBase> tiles = CollectTiles(source);
        if (tiles.Count == 0)
        {
            Debug.LogWarning("[TilePaletteFactory] No tiles for: " + source.id);
            return false;
        }

        string palettePath = $"{PaletteFolder}/{source.id}Palette.prefab";
        CreateOrUpdatePalette(palettePath, source.id + "Palette", tiles);
        Debug.Log($"[TilePaletteFactory] {source.id}Palette → {tiles.Count} tiles");
        return true;
    }

    private static List<TileBase> CollectTiles(PaletteSource source)
    {
        if (!string.IsNullOrEmpty(source.tileFolder))
            return LoadExistingTileAssets(source.tileFolder);

        if (!string.IsNullOrEmpty(source.texturePath) && Directory.Exists(source.texturePath))
            return BuildTilesFromFolderSprites(source);

        if (!string.IsNullOrEmpty(source.texturePath) && File.Exists(source.texturePath))
            return BuildTilesFromTexture(source);

        return new List<TileBase>();
    }

    private static List<TileBase> LoadExistingTileAssets(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Tile", new[] { folder });
        List<TileBase> tiles = new List<TileBase>(guids.Length);
        List<string> paths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int i = 0; i < paths.Count; i++)
        {
            TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(paths[i]);
            if (tile != null)
                tiles.Add(tile);
        }

        return tiles;
    }

    private static List<TileBase> BuildTilesFromTexture(PaletteSource source)
    {
        try
        {
            if (source.gridCellSize > 0)
                EnsureGridSlice(source.texturePath, source.gridCellSize);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TilePaletteFactory] Grid slice skipped for {source.id}: {ex.Message}");
        }

        Sprite[] sprites = LoadSprites(source);
        return CreateTileAssets(source.id, sprites);
    }

    private static List<TileBase> BuildTilesFromFolderSprites(PaletteSource source)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { source.texturePath });
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            UObject[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int j = 0; j < assets.Length; j++)
            {
                if (assets[j] is not Sprite sprite)
                    continue;
                if (sprite.rect.width < source.minSpriteSize || sprite.rect.height < source.minSpriteSize)
                    continue;
                sprites.Add(sprite);
            }
        }

        sprites = sprites
            .OrderBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return CreateTileAssets(source.id, sprites.ToArray());
    }

    private static Sprite[] LoadSprites(PaletteSource source)
    {
        UObject[] assets = AssetDatabase.LoadAllAssetsAtPath(source.texturePath);
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is not Sprite sprite)
                continue;

            if (source.skipLargePreviewSprite && sprites.Count == 0 && sprite.rect.width > 48f)
                continue;

            if (sprite.rect.width < source.minSpriteSize || sprite.rect.height < source.minSpriteSize)
                continue;

            sprites.Add(sprite);
        }

        return sprites
            .OrderBy(s => s.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<TileBase> CreateTileAssets(string sourceId, Sprite[] sprites)
    {
        string folder = $"{TileDataFolder}/{sourceId}";
        EnsureFolder(folder);

        if (Directory.Exists(folder))
        {
            string[] oldGuids = AssetDatabase.FindAssets("t:Tile", new[] { folder });
            for (int i = 0; i < oldGuids.Length; i++)
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(oldGuids[i]));
        }

        List<TileBase> tiles = new List<TileBase>(sprites.Length);
        AssetDatabase.StartAssetEditing();
        try
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                string safeName = SanitizeFileName(sprite.name);
                string assetPath = $"{folder}/{safeName}.asset";
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.name = safeName;
                AssetDatabase.CreateAsset(tile, assetPath);
                tiles.Add(tile);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        return tiles;
    }

    private static void EnsureGridSlice(string texturePath, int cellSize, bool forceReslice = false)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            return;

        if (!forceReslice)
        {
            UObject[] existing = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            int spriteCount = 0;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] is Sprite)
                    spriteCount++;
            }

            if (spriteCount > 4)
                return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = cellSize;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

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
        if (width < cellSize || height < cellSize)
            return;

        string baseName = Path.GetFileNameWithoutExtension(texturePath);
        List<SpriteRect> rects = new List<SpriteRect>();
        int index = 0;

        for (int y = 0; y < height; y += cellSize)
        {
            for (int x = 0; x < width; x += cellSize)
            {
                int w = Mathf.Min(cellSize, width - x);
                int h = Mathf.Min(cellSize, height - y);
                SpriteRect rect = new SpriteRect
                {
                    name = $"{baseName}_{index++}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(x, height - y - h, w, h),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };
                rects.Add(rect);
            }
        }

        provider.SetSpriteRects(rects.ToArray());
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static void CreateOrUpdatePalette(string palettePath, string paletteName, List<TileBase> tiles)
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

        GameObject paletteRoot = PrefabUtility.LoadPrefabContents(palettePath);
        Tilemap tilemap = paletteRoot.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("[TilePaletteFactory] Palette missing Tilemap: " + palettePath);
            PrefabUtility.UnloadPrefabContents(paletteRoot);
            return;
        }

        tilemap.ClearAllTiles();
        int x = 0;
        int y = 0;
        for (int i = 0; i < tiles.Count; i++)
        {
            tilemap.SetTile(new Vector3Int(x, y, 0), tiles[i]);
            x++;
            if (x >= PaletteColumns)
            {
                x = 0;
                y--;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(paletteRoot, palettePath);
        PrefabUtility.UnloadPrefabContents(paletteRoot);
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

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
#endif
