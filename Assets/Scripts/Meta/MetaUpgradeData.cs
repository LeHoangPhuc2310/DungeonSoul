// DungeonSoul — MetaUpgradeData.cs — Meta shop upgrade definition.

using UnityEngine;

public enum MetaUpgradeType
{
    HP,
    Damage,
    AttackSpeed,
    MoveSpeed,
    StarterSkill,
    CoinBonus,
    RoomRegen,
    SkillRarity,
    LootLuck,
    ForgeMaster,
    WeaponMastery
}

[CreateAssetMenu(fileName = "MetaUpgrade", menuName = "DungeonSoul/Meta Upgrade")]
public class MetaUpgradeData : ScriptableObject
{
    public string upgradeName;
    [TextArea(2, 3)] public string description;
    public MetaUpgradeType upgradeType;
    public int maxLevel = 10;
    public int baseCost = 50;
    public int costPerLevel = 25;
    public float effectPerLevel = 1f;
}
