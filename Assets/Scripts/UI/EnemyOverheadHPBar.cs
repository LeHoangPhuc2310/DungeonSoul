// DungeonSoul — EnemyOverheadHPBar.cs — Thanh máu nhỏ trên đầu quái thường.

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyOverheadHPBar : MonoBehaviour
{
    private static Sprite panelSprite;
    private static Sprite fillSprite;

    private const bool ShowOnRegularEnemies = true;

    private const float BarWidthWorld = 0.5f;
    private const float BarHeightWorld = 0.085f;
    private const float BorderWorld = 0.016f;
    private const float YGapWorld = 0.07f;

    private HealthSystem health;
    private SpriteRenderer bodyRenderer;
    private Transform barRoot;
    private Transform fillTransform;
    private float innerWidthWorld;
    private float innerHeightWorld;
    private float headLocalY;
    private bool layoutReady;

    public static void Ensure(GameObject enemy)
    {
        if (!ShowOnRegularEnemies)
            return;

        if (enemy == null || !enemy.CompareTag("Enemy"))
            return;
        if (enemy.GetComponent<BossController>() != null)
            return;
        if (enemy.GetComponent<EnemyOverheadHPBar>() != null)
            return;

        enemy.AddComponent<EnemyOverheadHPBar>();
    }

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
        bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        innerWidthWorld = BarWidthWorld - BorderWorld * 2f;
        innerHeightWorld = BarHeightWorld - BorderWorld * 2f;
        BuildBar();
    }

    private void Start()
    {
        CacheLayout();
    }

    private void CacheLayout()
    {
        if (bodyRenderer != null && bodyRenderer.sprite != null)
        {
            float uniform = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.001f);
            headLocalY = bodyRenderer.sprite.bounds.max.y + YGapWorld / uniform;
        }
        else
        {
            headLocalY = 0.9f;
        }

        layoutReady = true;
        ApplyBarTransform();
    }

    private void LateUpdate()
    {
        if (health == null || barRoot == null)
            return;

        if (!layoutReady && bodyRenderer != null && bodyRenderer.sprite != null)
            CacheLayout();

        float ratio = Mathf.Clamp01(health.CurrentHP / Mathf.Max(1f, health.MaxHP));
        if (health.CurrentHP <= 0f)
        {
            barRoot.gameObject.SetActive(false);
            return;
        }

        barRoot.gameObject.SetActive(true);
        ApplyBarTransform();
        RefreshFill(ratio);
    }

    private void ApplyBarTransform()
    {
        float uniform = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.001f);
        barRoot.localScale = Vector3.one / uniform;
        barRoot.localPosition = new Vector3(0f, headLocalY, 0f);
        barRoot.localRotation = Quaternion.identity;
    }

    private void BuildBar()
    {
        GameObject root = new GameObject("OverheadHP");
        root.transform.SetParent(transform, false);
        barRoot = root.transform;

        CreatePanel("Border", barRoot, 18,
            new Color(0.03f, 0.03f, 0.05f, 0.98f),
            BarWidthWorld + BorderWorld * 2f,
            BarHeightWorld + BorderWorld * 2f);

        CreatePanel("BG", barRoot, 19,
            new Color(0.1f, 0.08f, 0.12f, 0.94f),
            BarWidthWorld,
            BarHeightWorld);

        fillTransform = CreatePanel("Fill", barRoot, 20,
            Color.white,
            innerWidthWorld,
            innerHeightWorld,
            useFillSprite: true);
    }

    private Transform CreatePanel(string name, Transform parent, int sortingOrder, Color color,
        float widthWorld, float heightWorld, bool useFillSprite = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Sprite sprite = useFillSprite ? GetFillSprite() : GetPanelSprite();
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        sr.drawMode = SpriteDrawMode.Simple;

        go.transform.localScale = ScaleToWorld(sprite, widthWorld, heightWorld);
        if (useFillSprite)
            go.transform.localPosition = new Vector3(-innerWidthWorld * 0.5f, 0f, -0.001f);

        return go.transform;
    }

    private void RefreshFill(float ratio)
    {
        if (fillTransform == null)
            return;

        Sprite sprite = GetFillSprite();
        float fillW = Mathf.Max(0.01f, innerWidthWorld * ratio);
        fillTransform.localScale = ScaleToWorld(sprite, fillW, innerHeightWorld);
        fillTransform.localPosition = new Vector3(-innerWidthWorld * 0.5f, 0f, -0.001f);

        SpriteRenderer fillSr = fillTransform.GetComponent<SpriteRenderer>();
        if (fillSr != null)
            fillSr.color = ResolveFillColor(ratio);
    }

    private static Vector3 ScaleToWorld(Sprite sprite, float widthWorld, float heightWorld)
    {
        Vector2 size = sprite != null ? sprite.bounds.size : Vector2.one;
        return new Vector3(
            widthWorld / Mathf.Max(0.001f, size.x),
            heightWorld / Mathf.Max(0.001f, size.y),
            1f);
    }

    private static Color ResolveFillColor(float ratio)
    {
        Color full = new Color(0.28f, 0.9f, 0.42f, 1f);
        Color mid = new Color(1f, 0.76f, 0.18f, 1f);
        Color low = new Color(0.92f, 0.26f, 0.26f, 1f);

        if (ratio > 0.55f)
            return Color.Lerp(mid, full, (ratio - 0.55f) / 0.45f);

        if (ratio > 0.28f)
            return Color.Lerp(low, mid, (ratio - 0.28f) / 0.27f);

        return low;
    }

    private static Sprite GetPanelSprite()
    {
        if (panelSprite != null)
            return panelSprite;

        panelSprite = HpBarArtLibrary.Background;
        if (panelSprite != null)
            return panelSprite;

        panelSprite = CreateSquareSprite(new Color(1f, 1f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return panelSprite;
    }

    private static Sprite GetFillSprite()
    {
        if (fillSprite != null)
            return fillSprite;

        fillSprite = CreateSquareSprite(Color.white, new Vector2(0f, 0.5f));
        return fillSprite;
    }

    private static Sprite CreateSquareSprite(Color color, Vector2 pivot)
    {
        const int s = 8;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
                tex.SetPixel(x, y, color);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, s, s), pivot, s);
    }
}
