// DungeonSoul — EnemySpawnerMenu.cs — Mở nhanh Enemy Spawner trong scene.

using UnityEditor;
using UnityEngine;

public static class EnemySpawnerMenu
{
    [MenuItem("DungeonSoul/Gameplay/Select Enemy Spawner")]
    public static void SelectEnemySpawner()
    {
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            EditorUtility.DisplayDialog(
                "Dungeon Soul",
                "Không tìm thấy Enemy Spawner trong scene hiện tại.\n\n" +
                "• Mở scene: Assets/Scenes/SampleScene.unity\n" +
                "• Hoặc chạy menu: DungeonSoul → Setup → Full Project Setup",
                "OK");
            return;
        }

        Selection.activeGameObject = spawner.gameObject;
        EditorGUIUtility.PingObject(spawner.gameObject);
        Debug.Log(
            "[DungeonSoul] Enemy Spawner trên object \"" + spawner.gameObject.name +
            "\" — chỉnh Min/Max Enemies trong Inspector.");
    }
}
