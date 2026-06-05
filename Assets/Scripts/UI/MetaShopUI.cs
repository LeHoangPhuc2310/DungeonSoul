// DungeonSoul — MetaShopUI.cs — Full meta shop scroll UI + banner ad slot.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetaShopUI : MonoBehaviour
{
    public static MetaShopUI Instance { get; private set; }

    private Canvas canvas;
    private TMP_Text coinsText;
    private Transform listContent;
    private readonly List<MetaShopRow> rows = new List<MetaShopRow>();
    private PauseMenuUI returnToPause;

    private class MetaShopRow
    {
        public MetaUpgradeData data;
        public TMP_Text label;
        public TMP_Text descLabel;
        public TMP_Text costLabel;
        public Button buyButton;
        public TMP_Text buyLabel;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        BuildUI();
        Hide();
    }

    public void Toggle()
    {
        if (canvas == null)
            BuildUI();
        if (canvas.gameObject.activeSelf)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        returnToPause = null;
        if (canvas == null)
            BuildUI();
        Refresh();
        canvas.gameObject.SetActive(true);
        canvas.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    /// <summary>Mở shop từ menu Pause — khi đóng sẽ quay lại Pause thay vì chạy game.</summary>
    public void ShowFromPause(PauseMenuUI pause)
    {
        returnToPause = pause;
        if (canvas == null)
            BuildUI();
        Refresh();
        canvas.gameObject.SetActive(true);
        canvas.transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        AudioManager.PlayUiTap();
        if (canvas != null)
            canvas.gameObject.SetActive(false);

        // Nếu mở từ Pause thì quay lại Pause (giữ timeScale = 0), không thì trả game về chạy.
        if (returnToPause != null)
        {
            PauseMenuUI pause = returnToPause;
            returnToPause = null;
            pause.ReopenFromShop();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void Refresh()
    {
        MetaShopManager shop = MetaShopManager.Instance;
        if (shop == null)
            return;

        // Nạp lại dữ liệu mới nhất từ save mỗi lần mở/cập nhật shop.
        shop.ReloadFromSave();

        if (coinsText != null)
            coinsText.text = "Meta xu: " + shop.MetaCoins;

        for (int i = 0; i < rows.Count; i++)
        {
            MetaShopRow row = rows[i];
            if (row.data == null)
                continue;

            int lv = shop.GetLevel(row.data.upgradeName);
            int cost = shop.GetNextCost(row.data);
            bool maxed = lv >= row.data.maxLevel;
            bool canBuy = !maxed && shop.MetaCoins >= cost;

            if (row.label != null)
                row.label.text = DisplayName(row.data.upgradeType) + "   <color=#7FB0FF>LV " + lv + "/" + row.data.maxLevel + "</color>";

            if (row.descLabel != null)
                row.descLabel.text = DisplayDesc(row.data.upgradeType, row.data.effectPerLevel);

            if (row.costLabel != null)
            {
                row.costLabel.text = maxed ? "ĐÃ TỐI ĐA" : cost + " xu";
                row.costLabel.color = maxed
                    ? new Color(0.6f, 0.62f, 0.68f, 1f)
                    : canBuy ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.5f, 0.5f, 1f);
            }

            // Trạng thái nút MUA: MAX → khoá + "MAX"; thiếu xu → khoá; đủ điều kiện → bật "MUA".
            if (row.buyButton != null)
            {
                row.buyButton.interactable = canBuy;

                Image bg = row.buyButton.targetGraphic as Image;
                if (bg != null)
                    bg.color = maxed
                        ? new Color(0.28f, 0.3f, 0.34f, 1f)        // xám: đã max
                        : canBuy ? new Color(0.2f, 0.6f, 0.32f, 1f)  // xanh: mua được
                                 : new Color(0.42f, 0.24f, 0.26f, 1f); // đỏ mờ: thiếu xu

                if (row.buyLabel != null)
                {
                    row.buyLabel.text = maxed ? "MAX" : "MUA";
                    row.buyLabel.color = maxed
                        ? new Color(0.75f, 0.77f, 0.82f, 1f)
                        : canBuy ? Color.white : new Color(0.85f, 0.78f, 0.78f, 0.8f);
                }
            }
        }
    }

    /// <summary>Tên hiển thị tiếng Việt cho từng loại upgrade.</summary>
    private static string DisplayName(MetaUpgradeType type)
    {
        switch (type)
        {
            case MetaUpgradeType.HP: return "Sinh Lực";
            case MetaUpgradeType.Damage: return "Sát Thương";
            case MetaUpgradeType.AttackSpeed: return "Tốc Đánh";
            case MetaUpgradeType.MoveSpeed: return "Tốc Chạy";
            case MetaUpgradeType.StarterSkill: return "Kỹ Năng Đầu";
            case MetaUpgradeType.CoinBonus: return "Thưởng Xu";
            case MetaUpgradeType.RoomRegen: return "Hồi Máu Phòng";
            case MetaUpgradeType.SkillRarity: return "May Mắn Skill";
            case MetaUpgradeType.LootLuck: return "May Mắn Rơi Đồ";
            case MetaUpgradeType.ForgeMaster: return "Bậc Thầy Rèn";
            case MetaUpgradeType.WeaponMastery: return "Tinh Thông Vũ Khí";
            default: return type.ToString();
        }
    }

    private static string DisplayDesc(MetaUpgradeType type, float effect)
    {
        switch (type)
        {
            case MetaUpgradeType.HP: return "+" + effect + " máu tối đa mỗi cấp";
            case MetaUpgradeType.Damage: return "+" + effect + "% sát thương mỗi cấp";
            case MetaUpgradeType.AttackSpeed: return "+" + effect + "% tốc độ đánh mỗi cấp";
            case MetaUpgradeType.MoveSpeed: return "+" + effect + " tốc chạy mỗi cấp";
            case MetaUpgradeType.StarterSkill: return "Bắt đầu với kỹ năng hiếm hơn";
            case MetaUpgradeType.CoinBonus: return "+" + effect + "% xu nhận mỗi màn";
            case MetaUpgradeType.RoomRegen: return "Hồi " + effect + " máu mỗi phòng";
            case MetaUpgradeType.SkillRarity: return "Tỉ lệ skill xịn cao hơn";
            case MetaUpgradeType.LootLuck: return "+" + effect + "% tỉ lệ rơi đồ";
            case MetaUpgradeType.ForgeMaster: return "+" + effect + " lượt rèn lại mỗi màn";
            case MetaUpgradeType.WeaponMastery: return "-" + effect + "% hồi chiêu vũ khí";
            default: return "";
        }
    }

    /// <summary>Tile index trong Assets/Art/Tiles làm icon cho từng upgrade.</summary>
    private static int IconTile(MetaUpgradeType type)
    {
        switch (type)
        {
            case MetaUpgradeType.HP: return 29;            // tile đỏ rực (máu)
            case MetaUpgradeType.Damage: return 106;       // kiếm rộng
            case MetaUpgradeType.AttackSpeed: return 103;  // dao găm
            case MetaUpgradeType.MoveSpeed: return 131;    // giáo
            case MetaUpgradeType.StarterSkill: return 129; // gậy phép tím
            case MetaUpgradeType.CoinBonus: return 89;     // rương vàng
            case MetaUpgradeType.RoomRegen: return 130;    // gậy xanh (hồi)
            case MetaUpgradeType.SkillRarity: return 118;  // rìu chiến
            case MetaUpgradeType.LootLuck: return 89;      // rương
            case MetaUpgradeType.ForgeMaster: return 117;  // búa
            case MetaUpgradeType.WeaponMastery: return 105;// kiếm cong
            default: return 106;
        }
    }

    private static Color IconTint(MetaUpgradeType type)
    {
        switch (type)
        {
            case MetaUpgradeType.HP: return new Color(1f, 0.4f, 0.4f);
            case MetaUpgradeType.Damage: return new Color(1f, 0.75f, 0.4f);
            case MetaUpgradeType.AttackSpeed: return new Color(0.7f, 1f, 0.6f);
            case MetaUpgradeType.MoveSpeed: return new Color(0.6f, 0.9f, 1f);
            case MetaUpgradeType.StarterSkill: return new Color(0.85f, 0.6f, 1f);
            case MetaUpgradeType.CoinBonus: return new Color(1f, 0.85f, 0.35f);
            case MetaUpgradeType.RoomRegen: return new Color(0.5f, 1f, 0.7f);
            case MetaUpgradeType.SkillRarity: return new Color(0.9f, 0.8f, 1f);
            case MetaUpgradeType.LootLuck: return new Color(1f, 0.9f, 0.5f);
            case MetaUpgradeType.ForgeMaster: return new Color(1f, 0.7f, 0.5f);
            case MetaUpgradeType.WeaponMastery: return new Color(0.9f, 0.95f, 1f);
            default: return Color.white;
        }
    }

    private void BuildUI()
    {
        if (canvas != null)
            return;

        // Dọn canvas mồ côi từ lần Play/reload trước, đổi tên ngay để build lại bằng code mới.
        GameObject orphan = GameObject.Find("MetaShopCanvas");
        if (orphan != null)
        {
            orphan.name = "MetaShopCanvas_OLD";
            orphan.SetActive(false);
            Destroy(orphan);
        }
        rows.Clear();

        GameObject canvasGO = new GameObject("MetaShopCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Backdrop tối phủ kín màn hình, chặn click xuyên xuống gameplay phía sau.
        GameObject backdrop = MakeRect("Backdrop", canvasGO.transform, Vector2.zero, Vector2.zero);
        RectTransform backRt = backdrop.GetComponent<RectTransform>();
        backRt.anchorMin = Vector2.zero;
        backRt.anchorMax = Vector2.one;
        backRt.offsetMin = Vector2.zero;
        backRt.offsetMax = Vector2.zero;
        backdrop.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        // Panel chính — cao, vừa khít chiều dọc màn hình.
        GameObject panel = MakeRect("Panel", canvasGO.transform, new Vector2(720f, 980f), Vector2.zero);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.09f, 0.15f, 0.99f);

        // Header bar — tiêu đề trên, số xu ngay dưới, không đè nhau.
        MakeLabel("META SHOP", panel.transform, new Vector2(0f, 430f), 40f, FontStyles.Bold, new Color(0.96f, 0.82f, 0.28f, 1f));
        coinsText = MakeLabel("Meta xu: 0", panel.transform, new Vector2(0f, 378f), 26f, FontStyles.Bold, new Color(1f, 0.85f, 0.35f, 1f));

        // Scroll view — chiếm phần giữa panel.
        GameObject scrollGo = MakeRect("Scroll", panel.transform, new Vector2(680f, 660f), new Vector2(0f, 0f));
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        Image scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0.04f, 0.05f, 0.09f, 0.9f);

        GameObject viewport = MakeRect("Viewport", scrollGo.transform, Vector2.zero, Vector2.zero);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(8f, 8f);
        vpRt.offsetMax = new Vector2(-8f, -8f);
        viewport.AddComponent<RectMask2D>();
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.004f);

        GameObject content = MakeRect("Content", viewport.transform, new Vector2(640f, 800f), Vector2.zero);
        listContent = content.transform;
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        // QUAN TRỌNG: stretch ngang (anchorMin.x=0, anchorMax.x=1) → sizeDelta.x phải = 0,
        // nếu không width = viewportWidth + sizeDelta.x làm content rộng quá khổ và đẩy text ra ngoài.
        contentRt.sizeDelta = new Vector2(0f, 800f);
        contentRt.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vpRt;
        scroll.content = contentRt;

        MetaShopManager mgr = MetaShopManager.Instance;
        if (mgr == null)
        {
            GameObject mgrGo = new GameObject("MetaShopManager");
            mgr = mgrGo.AddComponent<MetaShopManager>();
        }

        IReadOnlyList<MetaUpgradeData> list = mgr.AllUpgrades;
        for (int i = 0; i < list.Count; i++)
            rows.Add(CreateRow(listContent, list[i]));

        Button closeBtn = MakeButton("Đóng", panel.transform, new Vector2(0f, -410f), Hide);
        RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.sizeDelta = new Vector2(220f, 60f);
        Image closeImg = closeBtn.targetGraphic as Image;
        if (closeImg != null)
            closeImg.color = new Color(0.6f, 0.18f, 0.2f, 1f);
        GameObject adSlot = MakeRect("AdBanner", panel.transform, new Vector2(320f, 50f), new Vector2(0f, -465f));
        adSlot.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        MakeLabel("Ads 320×50", adSlot.transform, Vector2.zero, 14f, FontStyles.Italic, new Color(0.5f, 0.55f, 0.65f, 1f));

        // Ép layout tính ngay để các row có kích thước/đặt đúng từ frame đầu.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);

        canvas.gameObject.SetActive(false);
    }

    private MetaShopRow CreateRow(Transform parent, MetaUpgradeData data)
    {
        // Row stretch ngang theo content; layout group điều khiển width.
        GameObject rowGo = new GameObject("Row_" + data.upgradeName, typeof(RectTransform));
        rowGo.transform.SetParent(parent, false);
        rowGo.AddComponent<Image>().color = new Color(0.13f, 0.14f, 0.22f, 0.97f);
        LayoutElement le = rowGo.AddComponent<LayoutElement>();
        le.preferredHeight = 116f;
        le.minHeight = 116f;
        le.flexibleWidth = 1f;

        RectTransform rowRt = rowGo.GetComponent<RectTransform>();

        // --- ICON (trái) ---
        Image iconBg = AddChildImage(rowRt, "IconBg", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(80f, 80f));
        iconBg.color = new Color(0.06f, 0.07f, 0.12f, 1f);

        Image iconImg = AddChildImage(iconBg.rectTransform, "IconImg", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        iconImg.rectTransform.offsetMin = new Vector2(8f, 8f);
        iconImg.rectTransform.offsetMax = new Vector2(-8f, -8f);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        Sprite iconSprite = ArtSpriteLibrary.LoadTile(IconTile(data.upgradeType));
        if (iconSprite != null)
            iconImg.sprite = iconSprite;
        iconImg.color = IconTint(data.upgradeType);

        // --- TÊN + LEVEL (trên) ---
        TextMeshProUGUI label = AddChildText(rowRt, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f));
        label.rectTransform.offsetMin = new Vector2(108f, -52f);
        label.rectTransform.offsetMax = new Vector2(-160f, -10f);
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(1f, 0.98f, 0.92f, 1f);
        label.alignment = TextAlignmentOptions.Left;
        label.richText = true;

        // --- MÔ TẢ (dưới) ---
        TextMeshProUGUI desc = AddChildText(rowRt, "Desc", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, 0.5f));
        desc.rectTransform.offsetMin = new Vector2(108f, 10f);
        desc.rectTransform.offsetMax = new Vector2(-160f, -56f);
        desc.fontSize = 16f;
        desc.color = new Color(0.78f, 0.82f, 0.9f, 1f);
        desc.alignment = TextAlignmentOptions.TopLeft;
        desc.textWrappingMode = TextWrappingModes.Normal;

        // --- NÚT MUA (phải, trên) ---
        MetaUpgradeData captured = data;
        Button buy = MakeButton("MUA", rowGo.transform, Vector2.zero, () => Buy(captured));
        RectTransform buyRt = buy.GetComponent<RectTransform>();
        buyRt.anchorMin = new Vector2(1f, 0.5f);
        buyRt.anchorMax = new Vector2(1f, 0.5f);
        buyRt.pivot = new Vector2(1f, 0.5f);
        buyRt.anchoredPosition = new Vector2(-16f, 16f);
        buyRt.sizeDelta = new Vector2(132f, 52f);

        // --- GIÁ (phải, dưới) ---
        TextMeshProUGUI cost = AddChildText(rowRt, "Cost", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f));
        cost.rectTransform.anchoredPosition = new Vector2(-16f, -30f);
        cost.rectTransform.sizeDelta = new Vector2(140f, 30f);
        cost.fontSize = 17f;
        cost.fontStyle = FontStyles.Bold;
        cost.color = new Color(1f, 0.85f, 0.35f, 1f);
        cost.alignment = TextAlignmentOptions.Right;

        TMP_Text buyLabel = buy.GetComponentInChildren<TMP_Text>(true);

        return new MetaShopRow { data = data, label = label, descLabel = desc, costLabel = cost, buyButton = buy, buyLabel = buyLabel };
    }

    /// <summary>Tạo Image con với anchor/pivot/offset tường minh.</summary>
    private static Image AddChildImage(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go.AddComponent<Image>();
    }

    private static TMP_FontAsset cachedRowFont;
    private static bool rowFontResolved;

    private static TMP_FontAsset ResolveRowFont()
    {
        if (rowFontResolved)
            return cachedRowFont;
        rowFontResolved = true;

        // Ưu tiên font mặc định TMP (LiberationSans — đủ glyph tiếng Việt).
        cachedRowFont = TMP_Settings.defaultFontAsset;
        if (cachedRowFont == null)
            cachedRowFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (cachedRowFont == null)
            cachedRowFont = GameUIFont.Serif;

        return cachedRowFont;
    }

    private static TextMeshProUGUI AddChildText(RectTransform parent, string name, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.enableAutoSizing = false;
        TMP_FontAsset f = ResolveRowFont();
        if (f != null)
            tmp.font = f;
        return tmp;
    }

    private void Buy(MetaUpgradeData data)
    {
        if (data == null || MetaShopManager.Instance == null)
            return;

        bool bought = MetaShopManager.Instance.BuyUpgrade(data.upgradeName);
        if (bought)
            AudioManager.PlayUiTap();

        // Luôn refresh để cập nhật trạng thái MAX / thiếu xu, kể cả khi mua thất bại.
        Refresh();
    }

    private static GameObject MakeRect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    private static TMP_Text MakeLabel(string text, Transform parent, Vector2 pos, float size, FontStyles style, Color color)
    {
        GameObject go = MakeRect("Lbl", parent, new Vector2(640f, 48f), pos);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        // Áp font trước, rồi ghi đè style/size/color theo tham số gọi (Apply ép giá trị của Role).
        if (GameUIFont.Serif != null)
            tmp.font = GameUIFont.Serif;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontSizeMin = size;
        tmp.fontSizeMax = size;
        tmp.enableAutoSizing = false;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.outlineWidth = 0f;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button MakeButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject go = MakeRect(label, parent, new Vector2(120f, 44f), pos);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.2f, 0.32f, 1f);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(action);

        GameObject textGO = MakeRect("Text", go.transform, Vector2.zero, Vector2.zero);
        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        GameUIFont.Apply(tmp, GameUIFont.Role.Button);
        return btn;
    }
}
