// DungeonSoul — BossData.cs — ScriptableObject boss definition (phases & abilities).

using System;
using System.Collections.Generic;
using UnityEngine;

public enum BossAbilityType
{
    ChaseBoost,
    AoeSlam,
    SpawnMinions,
    RageDash,
    SpiralShot,
    SummonClones,
    DarknessTeleport,
    FireBreath,
    MeteorRain,
    DragonRage
}

[Serializable]
public class BossAbility
{
    public string abilityName = "Ability";
    public BossAbilityType type = BossAbilityType.AoeSlam;
    public float cooldown = 4f;
    public float damageMultiplier = 1f;
    public float aoeRadius = 2.5f;
    public int spawnCount = 2;
    public float dashSpeedMultiplier = 2.5f;
    public float dashDuration = 0.6f;
}

[Serializable]
public class BossPhase
{
    [Range(0f, 1f)] public float hpThreshold = 0.66f;
    public float speedMultiplier = 1f;
    public float damageMultiplier = 1f;
    public List<BossAbility> abilities = new List<BossAbility>();
}

[CreateAssetMenu(fileName = "BossData", menuName = "DungeonSoul/Boss Data")]
public class BossData : ScriptableObject
{
    public string bossName = "Boss";
    public int floorGate = 3;
    public float totalHP = 600f;
    public int coinReward = 80;
    public int scoreReward = 400;
    [Range(0f, 1f)] public float projectileDamageResist;
    public bool weakToMagic;
    public List<BossPhase> phases = new List<BossPhase>();
}
