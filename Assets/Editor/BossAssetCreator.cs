// DungeonSoul — BossAssetCreator.cs — Create 4 boss ScriptableObjects in Resources/Boss.

using UnityEditor;
using UnityEngine;

public static class BossAssetCreator
{
    [MenuItem("DungeonSoul/Boss/Create All Boss Data Assets")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/Resources/Boss");
        Save("GoblinKing", BossDataFactory.CreateGoblinKing());
        Save("StoneGolem", BossDataFactory.CreateStoneGolem());
        Save("ShadowWitch", BossDataFactory.CreateShadowWitch());
        Save("DragonLord", BossDataFactory.CreateDragonLord());
        AssetDatabase.SaveAssets();
        Debug.Log("[BossAssetCreator] Created 4 boss assets under Resources/Boss/");
    }

    private static void Save(string fileName, BossData data)
    {
        string path = "Assets/Resources/Boss/" + fileName + ".asset";
        AssetDatabase.CreateAsset(data, path);
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Boss");
        }
    }
}
