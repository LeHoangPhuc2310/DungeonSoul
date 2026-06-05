using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayableCharacterDatabase", menuName = "DungeonSoul/Playable Character Database")]
public class PlayableCharacterDatabase : ScriptableObject
{
    public List<PlayableCharacterEntry> entries = new List<PlayableCharacterEntry>();

    public PlayableCharacterEntry GetById(string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
            return null;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].id == entryId)
                return entries[i];
        }

        return null;
    }
}
