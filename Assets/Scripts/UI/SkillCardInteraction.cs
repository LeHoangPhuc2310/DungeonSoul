using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Hover / long-press trên thẻ chọn skill (mobile tooltip).</summary>
public class SkillCardInteraction : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private SkillSelectionChoice boundChoice;
    private RectTransform rectTransform;
    private float pressStart;
    private bool longPressFired;
    private const float LongPressDuration = 0.55f;

    public void Bind(SkillSelectionChoice choice) => boundChoice = choice;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = Vector3.one;
        pressStart = 0f;
        SkillTooltipUI.Instance?.Hide();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressStart = Time.unscaledTime;
        longPressFired = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Thả tay = ẩn tooltip long-press, tránh nó treo lại theo con trỏ.
        pressStart = 0f;
        longPressFired = false;
        SkillTooltipUI.Instance?.Hide();
    }

    private void Update()
    {
        if (longPressFired || pressStart <= 0f)
            return;

        if (Time.unscaledTime - pressStart >= LongPressDuration)
        {
            longPressFired = true;
            ShowFullTooltip();
        }
    }

    private void ShowFullTooltip()
    {
        if (boundChoice == null)
            return;

        SkillTooltipUI tip = SkillTooltipUI.Instance;
        if (tip == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            tip = SkillTooltipUI.GetOrCreate(canvas);
        }

        if (tip == null)
            return;

        switch (boundChoice.kind)
        {
            case SkillSelectionChoiceKind.SkillUpgrade:
                if (boundChoice.skill != null)
                {
                    int stack = PlayerSkillHandler.Instance != null
                        ? PlayerSkillHandler.Instance.GetStack(boundChoice.skill.skillType)
                        : 0;
                    tip.ShowSkill(boundChoice.skill, stack + 1, rectTransform);
                }
                break;
            case SkillSelectionChoiceKind.PassiveItem:
                if (boundChoice.passiveItem != null)
                {
                    int lv = PassiveItemManager.Instance != null
                        ? PassiveItemManager.Instance.GetLevel(boundChoice.passiveItem) + 1
                        : 1;
                    tip.ShowPassive(boundChoice.passiveItem, lv, rectTransform);
                }
                break;
            case SkillSelectionChoiceKind.WeaponPickup:
                int copies = WeaponManager.Instance != null
                    ? WeaponManager.Instance.GetWeaponCopies(boundChoice.weaponType) + 1
                    : 1;
                tip.ShowWeapon(boundChoice.weaponType, copies, false, rectTransform);
                break;
        }
    }
}
