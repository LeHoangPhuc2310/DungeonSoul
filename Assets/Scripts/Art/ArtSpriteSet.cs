using UnityEngine;

[CreateAssetMenu(fileName = "ArtSpriteSet", menuName = "DungeonSoul/Art Sprite Set")]
public class ArtSpriteSet : ScriptableObject
{
    public Sprite enemyGrunt;
    public Sprite enemyRunner;
    public Sprite enemyBrute;
    public Sprite enemyElite;
    public Sprite chest;
    public Sprite heroWarrior;
    public Sprite heroRanger;
    public Sprite heroMage;

    [Header("Weapons (tiles 103–131)")]
    public Sprite weaponDagger;
    public Sprite weaponShortSword;
    public Sprite weaponCurvedBlade;
    public Sprite weaponBroadsword;
    public Sprite weaponGreatsword;
    public Sprite weaponHammer;
    public Sprite weaponBattleAxe;
    public Sprite weaponWoodAxe;
    public Sprite weaponStaffPurple;
    public Sprite weaponStaffBlue;
    public Sprite weaponSpear;
}
