using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpEffect : MonoBehaviour
{
    public static LevelUpEffect Instance { get; private set; }

    [Header("Screen Flash")]
    [SerializeField] private float screenFlashDuration = 0.2f;
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Floating Text")]
    [SerializeField] private float textFloatDuration = 1.5f;
    [SerializeField] private float textFloatDistance = 2.0f;

    private Canvas flashCanvas;
    private Image flashImage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureScreenFlashUI();
    }

    public void Play(Transform playerTransform, int currentLevel, float hpIncrease, float damageIncrease)
    {
        if (playerTransform == null)
            return;

        StopAllCoroutines();
        StartCoroutine(PlayScreenFlash());
        CreateFloatingText(playerTransform, currentLevel, hpIncrease, damageIncrease);
        CreateParticleRing(playerTransform.position);
    }

    private IEnumerator PlayScreenFlash()
    {
        if (flashImage == null)
            yield break;

        flashImage.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < screenFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / screenFlashDuration);
            Color color = flashColor;
            color.a = Mathf.Lerp(flashColor.a, 0f, t);
            flashImage.color = color;
            yield return null;
        }

        flashImage.gameObject.SetActive(false);
    }

    private void CreateFloatingText(Transform playerTransform, int currentLevel, float hpIncrease, float damageIncrease)
    {
        GameObject floatingTextObject = new GameObject("LevelUpFloatingText");
        floatingTextObject.transform.position = playerTransform.position + Vector3.up * 1.5f;

        TextMeshPro text = floatingTextObject.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 36f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.yellow;
        text.text = $"LEVEL UP! LV.{currentLevel}";

        StartCoroutine(AnimateFloatingText(text));
    }

    private IEnumerator AnimateFloatingText(TextMeshPro text)
    {
        if (text == null)
            yield break;

        Transform textTransform = text.transform;
        Vector3 startPos = textTransform.position;
        Vector3 targetPos = startPos + Vector3.up * textFloatDistance;
        Color startColor = text.color;

        float elapsed = 0f;
        while (elapsed < textFloatDuration && text != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / textFloatDuration);

            textTransform.position = Vector3.Lerp(startPos, targetPos, t);
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t);
            text.color = c;

            Camera cam = Camera.main;
            if (cam != null)
                textTransform.rotation = cam.transform.rotation;

            yield return null;
        }

        if (text != null)
            Destroy(text.gameObject);
    }

    private static void CreateParticleRing(Vector3 worldPosition)
    {
        GameObject ringObject = new GameObject("LevelUpParticleRing");
        ringObject.transform.position = worldPosition;
        ParticleSystem ps = ringObject.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.55f;
        main.loop = false;
        main.startLifetime = 0.55f;
        main.startSpeed = 3.5f;
        main.startSize = 0.14f;
        main.startColor = new Color(1f, 0.95f, 0.35f, 1f);
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 64) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;
        shape.radiusThickness = 0f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient alphaGradient = new Gradient();
        alphaGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.3f), 0f),
                new GradientColorKey(new Color(0.5f, 1f, 0.6f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = alphaGradient;

        ps.Play();
        Destroy(ringObject, main.duration + main.startLifetime.constant + 0.1f);
    }

    private void EnsureScreenFlashUI()
    {
        if (flashCanvas == null)
        {
            GameObject canvasObject = new GameObject("LevelUpFlashCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            flashCanvas = canvasObject.GetComponent<Canvas>();
            flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            flashCanvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform canvasRect = flashCanvas.transform as RectTransform;
        Transform existing = canvasRect.Find("FlashImage");
        if (existing == null)
        {
            GameObject imageObject = new GameObject("FlashImage", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasRect, false);

            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            flashImage = imageObject.GetComponent<Image>();
        }
        else
        {
            flashImage = existing.GetComponent<Image>();
        }

        flashImage.color = flashColor;
        flashImage.raycastTarget = false;
        flashImage.gameObject.SetActive(false);
    }
}
