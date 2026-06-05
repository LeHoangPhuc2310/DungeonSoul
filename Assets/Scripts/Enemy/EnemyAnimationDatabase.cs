// DungeonSoul — EnemyAnimationDatabase.cs — Registry tất cả quái + map archetype.

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAnimationDatabase", menuName = "DungeonSoul/Enemy Animation Database")]
public class EnemyAnimationDatabase : ScriptableObject
{
    public List<EnemyAnimationSet> enemies = new List<EnemyAnimationSet>();

    [Header("Legacy slots (giữ tương thích)")]
    public EnemyAnimationSet skeleton1;
    public EnemyAnimationSet skeleton2;
    public EnemyAnimationSet vampire;

    public IReadOnlyList<EnemyAnimationSet> All => enemies;

    public EnemyAnimationSet GetById(string setId)
    {
        if (string.IsNullOrEmpty(setId) || enemies == null)
            return null;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].id == setId)
                return enemies[i];
        }

        return null;
    }

    public EnemyAnimationSet GetRandom(EnemyArchetype archetype)
    {
        List<EnemyAnimationSet> pool = CollectPool(archetype);
        if (pool.Count > 0)
            return pool[Random.Range(0, pool.Count)];

        return GetLegacy(archetype);
    }

    public EnemyAnimationSet Get(EnemyArchetype archetype) => GetRandom(archetype);

    private List<EnemyAnimationSet> CollectPool(EnemyArchetype archetype)
    {
        List<EnemyAnimationSet> pool = new List<EnemyAnimationSet>();
        if (enemies == null)
            return pool;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyAnimationSet set = enemies[i];
            if (set != null && set.defaultArchetype == archetype && set.PreviewSprite != null)
                pool.Add(set);
        }

        return pool;
    }

    private EnemyAnimationSet GetLegacy(EnemyArchetype archetype)
    {
        switch (archetype)
        {
            case EnemyArchetype.Runner:
                return skeleton2 != null ? skeleton2 : skeleton1;
            case EnemyArchetype.Brute:
                return vampire != null ? vampire : skeleton2;
            case EnemyArchetype.Elite:
                return skeleton2 != null ? skeleton2 : vampire;
            default:
                return skeleton1;
        }
    }
}
