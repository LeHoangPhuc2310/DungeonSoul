// DungeonSoul — WallsFloorArenaMenu.cs — Menu vẽ arena bằng tileset walls_floor.png.

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class WallsFloorArenaMenu
{
    [MenuItem("DungeonSoul/Map/Paint Arena (WallsFloor tileset)")]
    public static void PaintArena()
    {
        GameObject grid = GameObject.Find("DungeonGrid");
        if (grid == null)
        {
            grid = new GameObject("DungeonGrid");
            Grid g = grid.AddComponent<Grid>();
            g.cellSize = Vector3.one;
            EnsureTilemapLayer(grid.transform, "FloorLayer", 0);
            EnsureTilemapLayer(grid.transform, "WallLayer", 1);
            EnsureTilemapLayer(grid.transform, "DecorLayer", 2);
        }

        WallsFloorArenaPainter painter = grid.GetComponent<WallsFloorArenaPainter>();
        if (painter == null)
            painter = grid.AddComponent<WallsFloorArenaPainter>();

        Undo.RegisterFullObjectHierarchyUndo(grid, "Paint WallsFloor Arena");
        painter.PaintMap();
        EditorUtility.SetDirty(grid);

        Debug.Log("[DungeonSoul] Arena WallsFloor đã vẽ. Chỉnh floor/wall index trong Inspector nếu cần.");
    }

    private static void EnsureTilemapLayer(Transform parent, string name, int sortOrder)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Tilemap>();
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortOrder;
        if (name == "WallLayer" && go.GetComponent<TilemapWallSetup>() == null)
            go.AddComponent<TilemapWallSetup>();
    }
}
