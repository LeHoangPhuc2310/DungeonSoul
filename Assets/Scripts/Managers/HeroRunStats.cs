using UnityEngine;

public enum HeroType
{
    Warrior = 0,
    Ranger = 1,
    Mage = 2
}

/// <summary>Applies GDD B.1 base stats for the selected hero at run start.</summary>
public class HeroRunStats : MonoBehaviour
{
    public static HeroRunStats Instance { get; private set; }

    private const string KeyHero = "ds_selected_hero";

    public HeroType SelectedHero { get; private set; } = HeroType.Warrior;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SelectedHero = (HeroType)Mathf.Clamp(PlayerPrefs.GetInt(KeyHero, 0), 0, 2);

        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        if (entry != null)
            SelectedHero = entry.combatClass;
    }

    private void Start()
    {
        ApplyToPlayer();
        MetaProgression.Instance?.ApplyToPlayer();
        Invoke(nameof(ApplyToPlayer), 0.15f);
    }

    public static void SetHero(HeroType hero)
    {
        PlayerPrefs.SetInt(KeyHero, (int)hero);
        PlayerPrefs.Save();
        if (Instance != null)
        {
            Instance.SelectedHero = hero;
            Instance.ApplyToPlayer();
        }
    }

    public static void SetCharacter(PlayableCharacterEntry entry)
    {
        if (entry == null)
            return;

        PlayableCharacterCatalog.SelectedId = entry.id;
        PlayerPrefs.SetInt(KeyHero, (int)entry.combatClass);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.SelectedHero = entry.combatClass;
            Instance.ApplyToPlayer();
        }
    }

    public void ApplyToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayableCharacterEntry entry = PlayableCharacterCatalog.GetSelected();
        if (entry != null)
        {
            SelectedHero = entry.combatClass;

            HealthSystem hs = player.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.MaxHP = entry.hp;
                hs.CurrentHP = entry.hp;
            }

            AutoAttack atk = player.GetComponent<AutoAttack>();
            if (atk != null)
                atk.ApplyHeroBaseStats(entry.damage, entry.fireRate, entry.crit);

            PlayerController moveCtrl = player.GetComponent<PlayerController>();
            if (moveCtrl != null)
            {
                moveCtrl.MoveSpeed = entry.moveSpeed;
                moveCtrl.ApplyPlayableCharacter(entry);
            }
        }
        else
        {
            GetBaseStats(SelectedHero, out float hp, out float dmg, out float move, out float fireRate, out float crit);

            HealthSystem hs = player.GetComponent<HealthSystem>();
            if (hs != null)
            {
                hs.MaxHP = hp;
                hs.CurrentHP = hp;
            }

            AutoAttack atk = player.GetComponent<AutoAttack>();
            if (atk != null)
                atk.ApplyHeroBaseStats(dmg, fireRate, crit);

            PlayerController moveCtrl = player.GetComponent<PlayerController>();
            if (moveCtrl != null)
            {
                moveCtrl.MoveSpeed = move;
                moveCtrl.ApplyHeroVisual(SelectedHero);
            }
        }

        PlayerWeaponVisual weaponVisual = player.GetComponent<PlayerWeaponVisual>();
        if (weaponVisual == null)
            weaponVisual = player.AddComponent<PlayerWeaponVisual>();
        weaponVisual.RefreshFromLoadout();

        if (GameplayPresentation.Instance != null)
            GameplayPresentation.Instance.ApplyPlayerScale();

        PlayerSkillHandler handler = player.GetComponent<PlayerSkillHandler>();
        if (handler != null && handler.activeSkills.Count > 0)
            handler.RefreshStats();

        PlayerStatsUI.NotifyChanged();
    }

    public static void GetBaseStats(HeroType hero, out float hp, out float dmg, out float move, out float fireRate, out float crit)
    {
        switch (hero)
        {
            case HeroType.Ranger:
                hp = 100f;
                dmg = 20f;
                move = 5.5f;
                fireRate = 1.8f;
                crit = 0.15f;
                break;
            case HeroType.Mage:
                hp = 80f;
                dmg = 28f;
                move = 4f;
                fireRate = 0.9f;
                crit = 0.08f;
                break;
            default:
                hp = 150f;
                dmg = 15f;
                move = 4.5f;
                fireRate = 1.2f;
                crit = 0.05f;
                break;
        }
    }

    public static string GetDisplayName(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Ranger: return "Hiệp sĩ";
            case HeroType.Mage: return "Pháp sư";
            default: return "Chiến binh";
        }
    }

    public static int GetHeroTileIndex(HeroType hero)
    {
        switch (hero)
        {
            case HeroType.Ranger: return 98;
            case HeroType.Mage: return 84;
            default: return 87;
        }
    }
}
