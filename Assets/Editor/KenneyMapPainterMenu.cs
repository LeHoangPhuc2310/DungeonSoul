// DungeonSoul — KenneyMapPainterMenu.cs — Menu vẽ map Kenney vào scene.

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class KenneyMapPainterMenu
{
    [MenuItem("DungeonSoul/Map/Paint Kenney Demo Map (Multi-Room)")]
    public static void PaintKenneyMap()
    {
        KenneyStyleMapPainter painter = Object.FindAnyObjectByType<KenneyStyleMapPainter>(FindObjectsInactive.Include);
        if (painter == null)
        {
            GameObject grid = GameObject.Find("DungeonGrid") ?? new GameObject("DungeonGrid");
            Grid g = grid.GetComponent<Grid>() ?? grid.AddComponent<Grid>();
            g.cellSize = Vector3.one;

            EnsureTilemapLayer(grid.transform, "FloorLayer", 0);
            EnsureTilemapLayer(grid.transform, "WallLayer", 1);
            EnsureTilemapLayer(grid.transform, "DecorLayer", 2);

            painter = grid.GetComponent<KenneyStyleMapPainter>() ?? grid.AddComponent<KenneyStyleMapPainter>();
            EditorUtility.SetDirty(grid);
        }

        Undo.RegisterFullObjectHierarchyUndo(painter.gameObject, "Paint Kenney Map");
        painter.PaintMap();
        EditorUtility.SetDirty(painter.gameObject);

        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            Transform floor = painter.transform.Find("FloorLayer");
            Transform wall = painter.transform.Find("WallLayer");
            SerializedObject so = new SerializedObject(spawner);
            if (floor != null)
                so.FindProperty("floorTilemap").objectReferenceValue = floor.GetComponent<Tilemap>();
            if (wall != null)
                so.FindProperty("wallTilemap").objectReferenceValue = wall.GetComponent<Tilemap>();
            so.FindProperty("useManualSpawnPoints").boolValue = false;
            so.ApplyModifiedProperties();
            spawner.RefreshWalkablePositions();
        }

        Debug.Log("[DungeonSoul] Map Kenney đã vẽ. Spawn player tại @ trong layout.");
    }

    [MenuItem("DungeonSoul/Map/Add Kenney Map Painter To DungeonGrid")]
    public static void AddPainterComponent()
    {
        GameObject grid = GameObject.Find("DungeonGrid");
        if (grid == null)
        {
            EditorUtility.DisplayDialog("Dungeon Soul", "Không tìm thấy DungeonGrid trong scene.", "OK");
            return;
        }

        if (grid.GetComponent<KenneyStyleMapPainter>() == null)
            grid.AddComponent<KenneyStyleMapPainter>();

        EditorUtility.SetDirty(grid);
    }

    private static void EnsureTilemapLayer(Transform parent, string name, int sortOrder)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Tilemap map = go.AddComponent<Tilemap>();
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortOrder;
        if (name == "WallLayer" && go.GetComponent<TilemapWallSetup>() == null)
            go.AddComponent<TilemapWallSetup>();
    }
}
