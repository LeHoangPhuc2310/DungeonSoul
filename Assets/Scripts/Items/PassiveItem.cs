using UnityEngine;

public enum PassiveItemType
{
    Spinach,
    Armor,
    Wings,
    EmptyTome,
    Candelabrador,
    Bracer,
    HollowHeart,
    Pummarola
}

[CreateAssetMenu(fileName = "PassiveItem", menuName = "DungeonSoul/Items/Passive Item")]
public class PassiveItem : ScriptableObject
{
    public string itemName;
    [TextArea(2, 4)] public string description;
    public SkillRarity rarity = SkillRarity.Common;
    public PassiveItemType itemType;
    public Sprite icon;
}
