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

    private RectTransform handle;
    private Vector2 inputVector;
    private PlayerController cachedPlayer;

    public Vector2 InputVector => inputVector;

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
#if UNITY_ANDROID || UNITY_IOS
        BuildUI();
#else
        if (showOnDesktop)
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

    private void BuildUI()
    {
        // Dedicated canvas so sorting order doesn't conflict
        GameObject canvasGO = new GameObject("JoystickCanvas");
        DontDestroyOnLoad(canvasGO);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        // Background circle — bottom-left corner
        GameObject bgGO = new GameObject("JoystickBG", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.anchoredPosition = new Vector2(160f, 160f);
        bgRT.sizeDelta = new Vector2(radius * 2f, radius * 2f);

        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = MakeCircleSprite(128);
        bgImg.color = new Color(1f, 1f, 1f, 0.18f);
        bgImg.raycastTarget = true;

        // Handle circle — child of background
        GameObject handleGO = new GameObject("JoystickHandle", typeof(RectTransform));
        handleGO.transform.SetParent(bgGO.transform, false);
        handle = handleGO.GetComponent<RectTransform>();
        handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(radius * 0.7f, radius * 0.7f);
        handle.anchoredPosition = Vector2.zero;

        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.sprite = MakeCircleSprite(64);
        handleImg.color = new Color(1f, 1f, 1f, 0.45f);
        handleImg.raycastTarget = false;

        // Touch handler on the background image
        JoystickTouchArea touchArea = bgGO.AddComponent<JoystickTouchArea>();
        touchArea.Init(this, bgRT, radius);
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

// Handles touch/pointer events on the joystick background image.
public class JoystickTouchArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private VirtualJoystick joystick;
    private RectTransform bgRect;
    private float radius;

    public void Init(VirtualJoystick owner, RectTransform bg, float r)
    {
        joystick = owner;
        bgRect = bg;
        radius = r;
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateDrag(eventData);

    public void OnDrag(PointerEventData eventData) => UpdateDrag(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        joystick.SetInput(Vector2.zero);
        joystick.MoveHandle(Vector2.zero);
    }

    private void UpdateDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                bgRect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Vector2 clamped = Vector2.ClampMagnitude(local, radius);
        joystick.SetInput(clamped / radius);
        joystick.MoveHandle(clamped);
    }
}
