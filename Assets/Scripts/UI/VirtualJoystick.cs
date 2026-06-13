using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Drop this on any GameObject in the gameplay scene.
// On mobile (or when showOnDesktop = true) it creates a bottom-left virtual joystick
// and feeds input to the PlayerController automatically.
public class VirtualJoystick : MonoBehaviour
{
    public static VirtualJoystick Instance { get; private set; }

    [SerializeField] private float radius = 75f;
    [SerializeField] private bool showOnDesktop = false;
    [Tooltip("Joystick nổi: chạm bất kỳ đâu trong vùng điều khiển thì cần xuất hiện ngay tại đó.")]
    [SerializeField] private bool floatingJoystick = true;

    private RectTransform handle;
    private RectTransform bgRect;
    private Vector2 inputVector;
    private PlayerController cachedPlayer;
    private GameObject joystickUiRoot;
    private Image bgImage;
    private Image handleImage;

    public Vector2 InputVector => inputVector;

    public static void SetChromeVisible(bool visible)
    {
        if (Instance != null)
        {
            if (!visible)
                Instance.HideFloating();
            if (Instance.joystickUiRoot != null)
                Instance.joystickUiRoot.SetActive(visible);
        }

        GameObject canvas = GameObject.Find("JoystickCanvas");
        if (canvas == null)
            return;

        canvas.SetActive(visible);
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = visible;

        JoystickTouchArea touch = canvas.GetComponentInChildren<JoystickTouchArea>(true);
        if (touch != null)
        {
            Image touchImg = touch.GetComponent<Image>();
            if (touchImg != null)
                touchImg.raycastTarget = visible;
        }
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
        EnsureBuilt(showOnDesktop);
    }

    public void EnsureBuilt(bool showOnDesktop = false)
    {
        if (GetComponentInChildren<JoystickTouchArea>() != null)
            return;

        this.showOnDesktop = showOnDesktop;
#if UNITY_ANDROID || UNITY_IOS
        BuildUI();
#else
        if (this.showOnDesktop)
            BuildUI();
#endif
    }

    private void Update()
    {
        if (cachedPlayer == null)
            cachedPlayer = Object.FindAnyObjectByType<PlayerController>();
        if (cachedPlayer != null)
            cachedPlayer.VirtualJoystickInput = inputVector;
    }

    // Called by JoystickTouchArea
    internal void SetInput(Vector2 value) => inputVector = value;
    internal void MoveHandle(Vector2 offset)
    {
        if (handle != null)
            handle.anchoredPosition = offset;
    }

    internal bool FloatingMode => floatingJoystick;
    internal RectTransform BgRect => bgRect;

    /// <summary>Joystick nổi: dời nền cần đến điểm chạm rồi hiện nó.</summary>
    internal void ShowAt(Vector2 anchoredPos)
    {
        if (bgRect != null)
            bgRect.anchoredPosition = anchoredPos;
        SetJoystickVisible(true);
    }

    internal void HideFloating()
    {
        SetInput(Vector2.zero);
        MoveHandle(Vector2.zero);
        if (floatingJoystick)
            SetJoystickVisible(false);
    }

    private void SetJoystickVisible(bool visible)
    {
        if (bgImage != null)
            bgImage.enabled = visible;
        if (handleImage != null)
            handleImage.enabled = visible;
    }

    private void BuildUI()
    {
        // Dedicated canvas so sorting order doesn't conflict
        GameObject canvasGO = new GameObject("JoystickCanvas");
        joystickUiRoot = canvasGO;
        DontDestroyOnLoad(canvasGO);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // Vùng chạm điều khiển — phủ NỬA TRÁI màn hình (nửa phải để cho UI/nút khác).
        // Đây là raycast target nhận chạm; joystick nổi sẽ hiện tại điểm chạm trong vùng này.
        GameObject areaGO = new GameObject("JoystickTouchArea", typeof(RectTransform));
        areaGO.transform.SetParent(canvasGO.transform, false);
        RectTransform areaRT = areaGO.GetComponent<RectTransform>();
        areaRT.anchorMin = new Vector2(0f, 0f);
        areaRT.anchorMax = new Vector2(0.5f, 1f);   // nửa trái
        areaRT.offsetMin = areaRT.offsetMax = Vector2.zero;
        Image areaImg = areaGO.AddComponent<Image>();
        areaImg.color = new Color(0f, 0f, 0f, 0f);  // trong suốt nhưng vẫn nhận chạm
        areaImg.raycastTarget = true;

        // Background circle — vị trí mặc định (góc trái dưới); chế độ nổi sẽ dời theo điểm chạm.
        GameObject bgGO = new GameObject("JoystickBG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(200f, 220f);
        bgRect.sizeDelta = new Vector2(radius * 2f, radius * 2f);

        bgImage = bgGO.AddComponent<Image>();
        bgImage.sprite = MakeCircleSprite(128);
        bgImage.color = new Color(0.45f, 0.72f, 1f, 0.18f);
        bgImage.raycastTarget = false;

        // Handle circle — child of background
        GameObject handleGO = new GameObject("JoystickHandle", typeof(RectTransform));
        handleGO.transform.SetParent(bgGO.transform, false);
        handle = handleGO.GetComponent<RectTransform>();
        handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(radius * 0.7f, radius * 0.7f);
        handle.anchoredPosition = Vector2.zero;

        handleImage = handleGO.AddComponent<Image>();
        handleImage.sprite = MakeCircleSprite(64);
        handleImage.color = new Color(0.92f, 0.96f, 1f, 0.6f);
        handleImage.raycastTarget = false;

        // Touch handler nằm trên VÙNG CHẠM (không phải nền tròn) → chạm đâu cũng điều khiển được.
        JoystickTouchArea touchArea = areaGO.AddComponent<JoystickTouchArea>();
        touchArea.Init(this, bgRect, radius);

        // Chế độ nổi: ẩn cần lúc chưa chạm.
        if (floatingJoystick)
            SetJoystickVisible(false);
    }

    private static Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= r ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}

// Nhận chạm trên VÙNG điều khiển (nửa trái màn hình). Chế độ nổi: cần joystick xuất
// hiện ngay tại điểm chạm rồi kéo từ đó — không bị kẹt một chỗ ở góc.
public class JoystickTouchArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private VirtualJoystick joystick;
    private RectTransform bgRect;
    private RectTransform parentRect;
    private float radius;

    public void Init(VirtualJoystick owner, RectTransform bg, float r)
    {
        joystick = owner;
        bgRect = bg;
        radius = r;
        parentRect = bg != null ? bg.parent as RectTransform : null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (joystick.FloatingMode && parentRect != null)
        {
            // Dời nền cần tới điểm chạm (toạ độ local trong canvas).
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, eventData.pressEventCamera, out Vector2 canvasLocal))
                joystick.ShowAt(canvasLocal);
        }

        UpdateDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData) => UpdateDrag(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        joystick.HideFloating();
    }

    private void UpdateDrag(PointerEventData eventData)
    {
        if (bgRect == null)
            return;

        // Đo lệch so với TÂM nền cần (đã được dời tới điểm chạm ở chế độ nổi).
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bgRect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Vector2 clamped = Vector2.ClampMagnitude(local, radius);
        joystick.SetInput(clamped / radius);
        joystick.MoveHandle(clamped);
    }
}
