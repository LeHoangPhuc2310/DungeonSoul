using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Popup đơn giản: thay passive nào khi đủ 6 ô.</summary>
public class PassiveSwapUI : MonoBehaviour
{
    public static PassiveItemData PendingItem { get; private set; }
    public static bool PendingChestReward { get; set; }

    private static PassiveSwapUI instance;
    private Action<int> onChosen;
    private static Action onClosed;
    private Button[] slotButtons;

    public static void Show(PassiveItemData incoming, IReadOnlyList<PassivePick> current, Action<int> callback,
        Action closedCallback = null, bool fromChest = false)
    {
        if (incoming == null || current == null || current.Count == 0)
            return;

        PendingItem = incoming;
        PendingChestReward = fromChest;
        onClosed = closedCallback;
        EnsureInstance();
        instance.onChosen = callback;
        instance.BuildChoices(current, incoming);
        instance.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        Canvas canvas = HUDManager.Resolve() != null
            ? HUDManager.Resolve().GetComponentInParent<Canvas>()
            : UnityEngine.Object.FindAnyObjectByType<Canvas>();

        GameObject root = new GameObject("PassiveSwapUI");
        if (canvas != null)
            root.transform.SetParent(canvas.transform, false);

        RectTransform rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;

        instance = root.AddComponent<PassiveSwapUI>();
        instance.BuildShell();
        root.SetActive(false);
    }

    private GameObject panelRoot;
    private TMP_Text titleText;

    private void BuildShell()
    {
        panelRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        panelRoot.transform.SetParent(transform, false);
        RectTransform prt = panelRoot.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(520f, 320f);
        panelRoot.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.98f);

        VerticalLayoutGroup vlg = panelRoot.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panelRoot.transform, false);
        titleText = titleGo.GetComponent<TextMeshProUGUI>();
        GameUIFont.Apply(titleText, GameUIFont.Role.HeaderTitle);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "Thay trang bị nào?";

        GameObject row = new GameObject("Choices", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(panelRoot.transform, false);
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        slotButtons = new Button[2];
        for (int i = 0; i < 2; i++)
        {
            slotButtons[i] = CreateChoiceButton(row.transform, i);
        }
    }

    private Button CreateChoiceButton(Transform parent, int index)
    {
        GameObject go = new GameObject("SwapChoice" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220f, 200f);
        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.14f, 0.16f, 0.22f, 1f);
        return go.GetComponent<Button>();
    }

    private void BuildChoices(IReadOnlyList<PassivePick> current, PassiveItemData incoming)
    {
        if (titleText != null)
            titleText.text = "Đổi \"" + incoming.displayName + "\" thay cho:";

        int a = UnityEngine.Random.Range(0, current.Count);
        int b = a;
        if (current.Count > 1)
        {
            do { b = UnityEngine.Random.Range(0, current.Count); }
            while (b == a);
        }

        ConfigureButton(slotButtons[0], current[a], 0, a);
        ConfigureButton(slotButtons[1], current[b], 1, b);
    }

    private void ConfigureButton(Button btn, PassivePick pick, int btnIndex, int slotIndex)
    {
        if (btn == null)
            return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => Confirm(slotIndex));

        TMP_Text label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
        {
            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btn.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(8f, 8f);
            trt.offsetMax = new Vector2(-8f, -8f);
            label = textGo.GetComponent<TextMeshProUGUI>();
            GameUIFont.Apply(label, GameUIFont.Role.CardBody);
            label.alignment = TextAlignmentOptions.Center;
        }

        if (pick?.data != null)
            label.text = pick.data.displayName + "\nCấp " + pick.level + "/" + pick.data.maxLevel;
        else
            label.text = "Ô trống";
    }

    private void Confirm(int slotIndex)
    {
        gameObject.SetActive(false);
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;

        onChosen?.Invoke(slotIndex);
        PendingItem = null;
        bool chest = PendingChestReward;
        PendingChestReward = false;
        onClosed?.Invoke();
        onClosed = null;

        if (chest)
            SkillSelectionUI.GetOrFind()?.CompleteChestAfterSwap();
    }
}
