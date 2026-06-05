using UnityEditor;
using UnityEngine;

public static class ArtSpriteSetBuilder
{
    [MenuItem("DungeonSoul/Art/Link Art Tiles to Runtime Set")]
    public static void BuildArtSpriteSet()
    {
        const string folder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Resources");

        const string assetPath = "Assets/Resources/ArtSpriteSet.asset";
        ArtSpriteSet set = AssetDatabase.LoadAssetAtPath<ArtSpriteSet>(assetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<ArtSpriteSet>();
            AssetDatabase.CreateAsset(set, assetPath);
        }

        set.enemyGrunt = LoadTileSprite(62);
        set.enemyRunner = LoadTileSprite(63);
        set.enemyBrute = LoadTileSprite(64);
        set.enemyElite = LoadTileSprite(120);
        set.chest = LoadTileSprite(89);
        set.heroWarrior = LoadTileSprite(87);
        set.heroRanger = LoadTileSprite(98);
        set.heroMage = LoadTileSprite(84);

        set.weaponDagger = LoadTileSprite(103);
        set.weaponShortSword = LoadTileSprite(104);
        set.weaponCurvedBlade = LoadTileSprite(105);
        set.weaponBroadsword = LoadTileSprite(106);
        set.weaponGreatsword = LoadTileSprite(107);
        set.weaponHammer = LoadTileSprite(117);
        set.weaponBattleAxe = LoadTileSprite(118);
        set.weaponWoodAxe = LoadTileSprite(119);
        set.weaponStaffPurple = LoadTileSprite(129);
        set.weaponStaffBlue = LoadTileSprite(130);
        set.weaponSpear = LoadTileSprite(131);

        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArtSpriteSetBuilder] Linked enemy/chest/hero tiles → Resources/ArtSpriteSet.asset");
    }

    private static Sprite LoadTileSprite(int index)
    {
        string path = $"Assets/Art/Tiles/tile_{index:0000}.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        Debug.LogWarning("[ArtSpriteSetBuilder] No sprite at " + path);
        return null;
    }
}
