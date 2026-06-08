using UnityEngine;

[CreateAssetMenu(fileName = "GuiSpriteSet", menuName = "DungeonSoul/GUI Sprite Set")]
public class GuiSpriteSet : ScriptableObject
{
    [Header("HUD Bars")]
    public Sprite hpBarFrame;
    public Sprite expBarFrame;

    [Header("Panels")]
    public Sprite menuPanel;
    public Sprite dialogPanel;

    [Header("Buttons")]
    public Sprite buttonPrimary;
    public Sprite buttonSecondary;
    public Sprite buttonDanger;
    public Sprite iconPause;
    public Sprite iconPlay;
    public Sprite iconClose;
    public Sprite iconSettings;
    public Sprite iconBack;

    [Header("Skill Cards")]
    public Sprite cardCommon;
    public Sprite cardRare;
    public Sprite cardEpic;
    public Sprite cardLegendary;

    [Header("Character Select — khung theo lớp")]
    public Sprite cardWarrior;
    public Sprite cardRanger;
    public Sprite cardMage;
    public Sprite cardSelected;

    [Header("Weapon Select — khung theo loại vũ khí")]
    public Sprite cardWeaponBow;
    public Sprite cardWeaponStaff;
    public Sprite cardWeaponBlade;
    public Sprite cardWeaponHoly;
    public Sprite cardWeaponThunder;
}
