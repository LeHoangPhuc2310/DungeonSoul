using UnityEngine;

[CreateAssetMenu(fileName = "DungeonPackSpriteSet", menuName = "DungeonSoul/Dungeon Pack Sprite Set")]
public class DungeonPackSpriteSet : ScriptableObject
{
    [Header("HUD")]
    public Sprite hpBarFrame;
    public Sprite hpBarFill;
    public Sprite expBarFrame;
    public Sprite expBarFill;

    [Header("World")]
    public Sprite chestClosed;
    public Sprite coinCommon;
    public Sprite coinRare;
    public Sprite[] coinSpin;

    [Header("Heroes (priest idle)")]
    public Sprite heroWarrior;
    public Sprite heroRanger;
    public Sprite heroMage;
}
