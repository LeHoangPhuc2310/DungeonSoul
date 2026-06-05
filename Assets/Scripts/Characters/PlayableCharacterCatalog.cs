using System.Collections.Generic;
using UnityEngine;

public static class PlayableCharacterCatalog
{
    private const string DefaultId = "knight";
    private const string PrefsKey = "ds_selected_character";

    private static PlayableCharacterDatabase cached;

    public static PlayableCharacterDatabase Database
    {
        get
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<PlayableCharacterDatabase>("PlayableCharacters/Database");
#if UNITY_EDITOR
            if (cached == null)
                cached = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayableCharacterDatabase>(
                    "Assets/Resources/PlayableCharacters/Database.asset");
#endif
            return cached;
        }
    }

    public static IReadOnlyList<PlayableCharacterEntry> All =>
        Database != null && Database.entries != null ? Database.entries : System.Array.Empty<PlayableCharacterEntry>();

    public static string SelectedId
    {
        get => PlayerPrefs.GetString(PrefsKey, DefaultId);
        set
        {
            PlayerPrefs.SetString(PrefsKey, value);
            PlayerPrefs.Save();
        }
    }

    public static PlayableCharacterEntry Get(string id) =>
        Database != null ? Database.GetById(id) : null;

    public static PlayableCharacterEntry GetSelected() =>
        Get(SelectedId) ?? (All.Count > 0 ? All[0] : null);

    public static void InvalidateCache() => cached = null;

    public static string GetClassLabel(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Ranger: return "Cung thủ";
            case HeroType.Mage: return "Pháp sư";
            default: return "Chiến binh";
        }
    }

    public static IEnumerable<PlayableCharacterEntry> ByClass(HeroType hero)
    {
        for (int i = 0; i < All.Count; i++)
        {
            PlayableCharacterEntry e = All[i];
            if (e != null && e.combatClass == hero)
                yield return e;
        }
    }
}
