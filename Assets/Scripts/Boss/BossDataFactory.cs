// DungeonSoul — BossDataFactory.cs — Runtime fallback boss definitions.

using System.Collections.Generic;
using UnityEngine;

public static class BossDataFactory
{
    public static BossData CreateGoblinKing() => Build(
        "Goblin King", 3, 600f, 80, 400,
        new[]
        {
            Phase(0.66f, 1f, Ab("Chase", BossAbilityType.ChaseBoost, 3f), Ab("Slam", BossAbilityType.AoeSlam, 5f, 2.8f)),
            Phase(0.33f, 1.3f, Ab("Goblins", BossAbilityType.SpawnMinions, 6f, spawn: 3)),
            Phase(0.01f, 1.8f, Ab("Rage", BossAbilityType.RageDash, 4f, dash: 3f))
        });

    public static BossData CreateStoneGolem() => Build(
        "Stone Golem", 6, 1200f, 120, 600,
        new[]
        {
            Phase(0.66f, 0.8f, Ab("Slam", BossAbilityType.AoeSlam, 5f, 3.2f, 1.2f)),
            Phase(0.33f, 1f, Ab("Quake", BossAbilityType.AoeSlam, 4f, 4f, 1.5f)),
            Phase(0.01f, 1.2f, Ab("Spawn", BossAbilityType.SpawnMinions, 7f, spawn: 2))
        },
        0.5f);

    public static BossData CreateShadowWitch() => Build(
        "Shadow Witch", 9, 1800f, 150, 800,
        new[]
        {
            Phase(0.66f, 1.1f, Ab("Spiral", BossAbilityType.SpiralShot, 4f)),
            Phase(0.33f, 1.2f, Ab("Clones", BossAbilityType.SummonClones, 6f, spawn: 2)),
            Phase(0.01f, 1.5f, Ab("Teleport", BossAbilityType.DarknessTeleport, 3.5f))
        });

    public static BossData CreateDragonLord() => Build(
        "Dragon Lord", 10, 5000f, 300, 1500,
        new[]
        {
            Phase(0.75f, 1f, Ab("Fire", BossAbilityType.FireBreath, 4f, 3.5f, 1.3f)),
            Phase(0.5f, 1.15f, Ab("Summon", BossAbilityType.SpawnMinions, 6f, spawn: 4)),
            Phase(0.25f, 1.3f, Ab("Meteor", BossAbilityType.MeteorRain, 5f, 4f, 1.6f)),
            Phase(0.01f, 1.5f, Ab("Rage", BossAbilityType.DragonRage, 8f, dash: 1.5f, dmg: 1.5f))
        });

    private static BossPhase Phase(float threshold, float speed, params BossAbility[] abilities)
    {
        return new BossPhase
        {
            hpThreshold = threshold,
            speedMultiplier = speed,
            damageMultiplier = 1f,
            abilities = new List<BossAbility>(abilities)
        };
    }

    private static BossAbility Ab(string name, BossAbilityType type, float cd, float radius = 2f, float dmg = 1f, int spawn = 0, float dash = 2f)
    {
        return new BossAbility
        {
            abilityName = name,
            type = type,
            cooldown = cd,
            aoeRadius = radius,
            damageMultiplier = dmg,
            spawnCount = spawn,
            dashSpeedMultiplier = dash
        };
    }

    private static BossData Build(string name, int floor, float hp, int coins, int score, BossPhase[] phases, float resist = 0f)
    {
        BossData data = ScriptableObject.CreateInstance<BossData>();
        data.bossName = name;
        data.floorGate = floor;
        data.totalHP = hp;
        data.coinReward = coins;
        data.scoreReward = score;
        data.projectileDamageResist = resist;
        data.phases = new List<BossPhase>(phases);
        return data;
    }
}
