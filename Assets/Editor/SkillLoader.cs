using UnityEditor;
using UnityEngine;
using System.IO;

public class SkillLoader : Editor
{
    [MenuItem("Tools/Fix Skill Path")]
    static void FixPath()
    {
        // Move all SkillData from Resources/Skills/
        // to Resources/ directly (not subfolder)
        string from = "Assets/Resources/Skills";
        string to = "Assets/Resources";

        var assets = AssetDatabase.FindAssets(
            "t:SkillData", new[] { from });
        foreach (var guid in assets)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileName(path);
            AssetDatabase.MoveAsset(path, to + "/" + name);
        }
        AssetDatabase.Refresh();
        Debug.Log("Done! Moved " + assets.Length + " skills");
    }
}
