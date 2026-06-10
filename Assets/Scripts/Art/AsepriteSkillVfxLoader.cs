// DungeonSoul — Load sprite animation từ ASEPRITE_skill_effect (Resources + Editor).

using System.Collections.Generic;
using UnityEngine;

public static class AsepriteSkillVfxLoader
{
    private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

    public static Sprite[] LoadFolders(params string[] folderNames)
    {
        if (folderNames == null || folderNames.Length == 0)
            return System.Array.Empty<Sprite>();

        string cacheKey = string.Join("|", folderNames);
        if (Cache.TryGetValue(cacheKey, out Sprite[] cached) && cached != null && cached.Length > 0)
            return cached;

        List<Sprite> merged = new List<Sprite>();
        for (int i = 0; i < folderNames.Length; i++)
        {
            Sprite[] part = LoadFolder(folderNames[i]);
            if (part != null && part.Length > 0)
                merged.AddRange(part);
        }

        Sprite[] result = SortSprites(merged.ToArray());
        Cache[cacheKey] = result;
        return result;
    }

    public static Sprite[] LoadFolder(string folderName)
    {
        if (string.IsNullOrEmpty(folderName))
            return System.Array.Empty<Sprite>();

        if (Cache.TryGetValue(folderName, out Sprite[] cached) && cached != null && cached.Length > 0)
            return cached;

        string resourcesPath = $"{AsepriteSkillEffectPaths.ResourcesRoot}/{folderName}";
        Sprite[] fromResources = Resources.LoadAll<Sprite>(resourcesPath);
        if (fromResources != null && fromResources.Length > 0)
        {
            Sprite[] sorted = SortSprites(fromResources);
            Cache[folderName] = sorted;
            return sorted;
        }

#if UNITY_EDITOR
        string assetPath = $"{AsepriteSkillEffectPaths.SourceRoot}/{folderName}";
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { assetPath });
        if (guids != null && guids.Length > 0)
        {
            List<Sprite> list = new List<Sprite>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null)
                    list.Add(s);
            }

            if (list.Count > 0)
            {
                Sprite[] sorted = SortSprites(list.ToArray());
                Cache[folderName] = sorted;
                return sorted;
            }
        }
#endif

        Cache[folderName] = System.Array.Empty<Sprite>();
        return System.Array.Empty<Sprite>();
    }

    public static Sprite LoadSkillIcon(int iconIndex)
    {
        Sprite[] icons = LoadFolder(AsepriteSkillEffectPaths.IconsFolder);
        if (icons == null || icons.Length == 0)
            return null;

        int idx = Mathf.Clamp(iconIndex, 0, icons.Length - 1);
        return icons[idx];
    }

    public static void ClearCache() => Cache.Clear();

    private static Sprite[] SortSprites(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length <= 1)
            return sprites ?? System.Array.Empty<Sprite>();

        System.Array.Sort(sprites, (a, b) =>
        {
            int cmp = FrameSortKey(a != null ? a.name : "").CompareTo(FrameSortKey(b != null ? b.name : ""));
            return cmp != 0 ? cmp : string.Compare(a != null ? a.name : "", b != null ? b.name : "",
                System.StringComparison.OrdinalIgnoreCase);
        });
        return sprites;
    }

    private static string FrameSortKey(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "00000000";

        int underscore = name.LastIndexOf('_');
        if (underscore >= 0 && underscore < name.Length - 1)
        {
            string tail = name.Substring(underscore + 1);
            if (int.TryParse(tail, out int frame))
                return name.Substring(0, underscore).PadRight(32) + frame.ToString("D8");
        }

        if (name.StartsWith("Icon", System.StringComparison.OrdinalIgnoreCase)
            && int.TryParse(name.Substring(4), out int iconNum))
            return "Icon" + iconNum.ToString("D8");

        int digits = 0;
        int start = name.Length;
        for (int i = name.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(name[i]))
                break;
            start = i;
            digits++;
        }

        if (digits > 0 && int.TryParse(name.Substring(start), out int num))
            return name.Substring(0, start).PadRight(32) + num.ToString("D8");

        return name;
    }
}
