using System.Collections.Generic;
using UnityEngine;

public static class PlayableCharacterCatalog
{
    private const string DefaultId = "swordsman_lvl1";
    private const string PrefsKey = "ds_selected_character";
    private const string QuickPlayPrefsKey = "ds_quick_play_bundle";

    /// <summary>Nhân vật mặc định 4 hướng cầm vũ khí sẵn (ASEPRITE Kiếm sĩ I).</summary>
    public const string BundledFourWayHeroId = "swordsman_lvl1";

    /// <summary>Chỉ hiện 8 nhân vật ASEPRITE (Swordsman/Orc/Slime) trong màn chọn.</summary>
    private static readonly string[] VisibleIds =
    {
        "swordsman_lvl1",
        "swordsman_lvl2",
        "swordsman_lvl3",
        "orc_ase_1",
        "orc_ase_2",
        "orc_ase_3",
        "slime_ase_2",
        "slime_ase_3"
    };

    private static PlayableCharacterDatabase cached;
    private static List<PlayableCharacterEntry> visibleCache;

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

    public static IReadOnlyList<PlayableCharacterEntry> Visible
    {
        get
        {
            if (visibleCache != null)
                return visibleCache;

            visibleCache = new List<PlayableCharacterEntry>(VisibleIds.Length);
            for (int i = 0; i < VisibleIds.Length; i++)
            {
                PlayableCharacterEntry entry = Get(VisibleIds[i]);
                if (entry != null)
                    visibleCache.Add(entry);
            }

            return visibleCache;
        }
    }

    public static bool IsVisible(PlayableCharacterEntry entry) =>
        entry != null && System.Array.IndexOf(VisibleIds, entry.id) >= 0;

    public static bool IsUnlocked(PlayableCharacterEntry entry) =>
        entry != null && MetaRunProgress.IsCharacterUnlocked(entry.id);

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

    public static PlayableCharacterEntry GetSelected()
    {
        if (UseQuickPlayBundle)
        {
            PlayableCharacterEntry bundled = Get(BundledFourWayHeroId);
            if (bundled != null)
                return bundled;
        }

        PlayableCharacterEntry entry = Get(SelectedId);
        if (entry != null && IsVisible(entry))
            return entry;

        return Visible.Count > 0 ? Visible[0] : null;
    }

    /// <summary>Bỏ qua màn chọn — luôn dùng nhân vật 4 hướng cầm vũ khí sẵn + loadout đã lưu.</summary>
    public static bool UseQuickPlayBundle
    {
        get => PlayerPrefs.GetInt(QuickPlayPrefsKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(QuickPlayPrefsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void ApplyQuickPlayBundle()
    {
        SelectedId = BundledFourWayHeroId;
        UseQuickPlayBundle = true;
        RunEntryGate.ConfirmCharacterSelect();
    }

    public static void InvalidateCache()
    {
        cached = null;
        visibleCache = null;
    }

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
        IReadOnlyList<PlayableCharacterEntry> list = Visible;
        for (int i = 0; i < list.Count; i++)
        {
            PlayableCharacterEntry e = list[i];
            if (e != null && e.combatClass == hero)
                yield return e;
        }
    }

    public static bool HasVisibleInClass(HeroType hero)
    {
        foreach (PlayableCharacterEntry entry in ByClass(hero))
        {
            if (entry != null)
                return true;
        }

        return false;
    }
}
