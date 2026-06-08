using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime sprites từ Assets/Art/GUI (qua Resources/GuiSpriteSet).</summary>
public static class GuiArtLibrary
{
    private const string ResourcePath = "GuiSpriteSet";
    private static GuiSpriteSet cached;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cached = null;

    public static GuiSpriteSet Set
    {
        get
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<GuiSpriteSet>(ResourcePath);
#if UNITY_EDITOR
            if (cached == null)
            {
                cached = UnityEditor.AssetDatabase.LoadAssetAtPath<GuiSpriteSet>(
                    "Assets/Resources/GuiSpriteSet.asset");
            }
#endif
            return cached;
        }
    }

    public static bool HasPack => Set != null;

    public static Sprite HpBarFrame => Set != null ? Set.hpBarFrame : null;
    public static Sprite ExpBarFrame => Set != null ? Set.expBarFrame : null;
    public static Sprite MenuPanel => Set != null ? Set.menuPanel : null;
    public static Sprite DialogPanel => Set != null ? Set.dialogPanel : null;
    public static Sprite ButtonPrimary => Set != null ? Set.buttonPrimary : null;
    public static Sprite ButtonSecondary => Set != null ? Set.buttonSecondary : null;
    public static Sprite ButtonDanger => Set != null ? Set.buttonDanger : null;
    public static Sprite IconPause => Set != null ? Set.iconPause : null;

    public static void InvalidateCache() => cached = null;

    public static Sprite CardFrame(SkillRarity rarity)
    {
        if (Set == null)
            return null;

        switch (rarity)
        {
            case SkillRarity.Legendary: return Set.cardLegendary != null ? Set.cardLegendary : Set.cardEpic;
            case SkillRarity.Epic: return Set.cardEpic != null ? Set.cardEpic : Set.cardRare;
            case SkillRarity.Rare: return Set.cardRare != null ? Set.cardRare : Set.cardCommon;
            default: return Set.cardCommon;
        }
    }

    public static Sprite CardFrameForHero(HeroType heroClass)
    {
        if (Set == null)
            return null;

        switch (heroClass)
        {
            case HeroType.Ranger: return Set.cardRanger != null ? Set.cardRanger : Set.cardRare;
            case HeroType.Mage: return Set.cardMage != null ? Set.cardMage : Set.cardEpic;
            default: return Set.cardWarrior != null ? Set.cardWarrior : Set.cardCommon;
        }
    }

    public static Sprite CardFrameForWeapon(WeaponType type)
    {
        if (Set == null)
            return null;

        switch (type)
        {
            case WeaponType.IronBow:
            case WeaponType.StormBow:
                return Set.cardWeaponBow != null ? Set.cardWeaponBow : Set.cardRare;
            case WeaponType.FireStaff:
            case WeaponType.DragonStaff:
            case WeaponType.FrostWand:
            case WeaponType.BlizzardWand:
                return Set.cardWeaponStaff != null ? Set.cardWeaponStaff : Set.cardEpic;
            case WeaponType.PoisonDagger:
            case WeaponType.DeathDagger:
                return Set.cardWeaponBlade != null ? Set.cardWeaponBlade : Set.cardCommon;
            case WeaponType.HolyCross:
            case WeaponType.HolyNova:
                return Set.cardWeaponHoly != null ? Set.cardWeaponHoly : Set.cardLegendary;
            case WeaponType.ThunderRod:
            case WeaponType.ZeusRod:
                return Set.cardWeaponThunder != null ? Set.cardWeaponThunder : Set.cardMage;
            default:
                return Set.cardCommon;
        }
    }

    public static Sprite CardSelected => Set != null ? Set.cardSelected : null;

    public static bool ApplyCardFrame(Image image, Sprite frame, Color fallback, bool selectedTint = false)
    {
        if (image == null)
            return false;

        if (frame != null)
        {
            image.sprite = frame;
            image.type = Image.Type.Simple;
            image.color = selectedTint ? new Color(1f, 1f, 0.92f, 1f) : Color.white;
            image.preserveAspect = false;
            return true;
        }

        image.sprite = null;
        image.color = fallback;
        return false;
    }

    public static bool ApplyBarFrame(Image image, Sprite frame)
    {
        if (image == null || frame == null)
            return false;

        image.sprite = frame;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.fillCenter = true;
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.enabled = true;
        return true;
    }

    public static bool ApplyPanel(Image image, Sprite panel)
    {
        if (image == null || panel == null)
            return false;

        image.sprite = panel;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;
        return true;
    }

    public static bool ApplyButton(Image image, Sprite sprite, Color fallback)
    {
        if (image == null)
            return false;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = false;
            return true;
        }

        image.sprite = null;
        image.color = fallback;
        return false;
    }

    public static bool ApplyIcon(Image image, Sprite icon)
    {
        if (image == null || icon == null)
            return false;

        image.sprite = icon;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = true;
        image.enabled = true;
        return true;
    }
}
