using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Gắn vào ô HUD — hiện tooltip khi rê chuột.</summary>
public class HudHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private SkillData skill;
    private WeaponType? weaponType;
    private PassiveItemData passiveData;
    private int stackCount = 1;
    private bool weaponEvolved;
    private RectTransform rectTransform;

    public void ConfigureSkill(SkillData data, int stack)
    {
        skill = data;
        weaponType = null;
        passiveData = null;
        weaponEvolved = false;
        stackCount = Mathf.Max(1, stack);
    }

    public void ConfigureWeapon(WeaponType type, int copies, bool evolved = false)
    {
        skill = null;
        weaponType = type;
        passiveData = null;
        weaponEvolved = evolved;
        stackCount = Mathf.Max(1, copies);
    }

    public void ConfigurePassive(PassiveItemData data, int level)
    {
        skill = null;
        weaponType = null;
        passiveData = data;
        weaponEvolved = false;
        stackCount = Mathf.Max(1, level);
    }

    public void ConfigureEmpty()
    {
        skill = null;
        weaponType = null;
        passiveData = null;
        weaponEvolved = false;
        stackCount = 1;
    }

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SkillTooltipUI tip = SkillTooltipUI.Instance;
        if (tip == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            tip = SkillTooltipUI.GetOrCreate(canvas);
        }

        if (tip == null)
            return;

        if (skill != null)
            tip.ShowSkill(skill, stackCount, rectTransform);
        else if (weaponType.HasValue)
            tip.ShowWeapon(weaponType.Value, stackCount, weaponEvolved, rectTransform);
        else if (passiveData != null)
            tip.ShowPassive(passiveData, stackCount, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();
    }
}
