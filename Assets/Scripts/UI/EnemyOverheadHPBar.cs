// DungeonSoul — EnemyOverheadHPBar.cs — Thanh máu nhỏ cố định trên đầu quái (world-space child).

using UnityEngine;

[DisallowMultipleComponent]
public class EnemyOverheadHPBar : MonoBehaviour
{
    private static Sprite whiteSprite;

    private HealthSystem health;
    private SpriteRenderer bodyRenderer;
    private Transform barRoot;
    private Transform bgTransform;
    private Transform fillTransform;
    private float barWidthLocal = 0.36f;
    private const float BarHeightLocal = 0.055f;
    private const float YGapLocal = 0.08f;

    public static void Ensure(GameObject enemy)
    {
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
        BuildBar();
    }

    private void LateUpdate()
    {
        if (health == null || barRoot == null)
            return;

        if (health.CurrentHP <= 0f)
        {
            barRoot.gameObject.SetActive(false);
            return;
        }

        barRoot.gameObject.SetActive(true);
        PositionBar();
        RefreshFill();
    }

    private void BuildBar()
    {
        GameObject root = new GameObject("OverheadHP");
        root.transform.SetParent(transform, false);
        barRoot = root.transform;

        GameObject bgGo = new GameObject("BG");
        bgGo.transform.SetParent(barRoot, false);
        bgTransform = bgGo.transform;
        SpriteRenderer bg = bgGo.AddComponent<SpriteRenderer>();
        bg.sprite = GetWhiteSprite();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        bg.sortingOrder = 19;
        bgTransform.localScale = new Vector3(barWidthLocal, BarHeightLocal, 1f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(barRoot, false);
        fillTransform = fillGo.transform;
        SpriteRenderer fill = fillGo.AddComponent<SpriteRenderer>();
        fill.sprite = GetWhiteSprite();
        fill.color = new Color(0.92f, 0.22f, 0.24f, 1f);
        fill.sortingOrder = 20;
    }

    private void PositionBar()
    {
        float headLocalY = 0.75f;
        if (bodyRenderer != null && bodyRenderer.sprite != null)
        {
            float uniformScale = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.001f);
            float worldTop = bodyRenderer.bounds.max.y - transform.position.y;
            headLocalY = worldTop / uniformScale + YGapLocal;
        }

        barRoot.localPosition = new Vector3(0f, headLocalY, 0f);
        barRoot.localRotation = Quaternion.identity;
        barRoot.localScale = Vector3.one;
    }

    private void RefreshFill()
    {
        if (fillTransform == null || health == null)
            return;

        float ratio = Mathf.Clamp01(health.CurrentHP / Mathf.Max(1f, health.MaxHP));
        float fillW = barWidthLocal * ratio;
        fillTransform.localScale = new Vector3(Mathf.Max(0.001f, fillW), BarHeightLocal, 1f);
        fillTransform.localPosition = new Vector3(-(barWidthLocal - fillW) * 0.5f, 0f, 0f);

        SpriteRenderer fillSr = fillTransform.GetComponent<SpriteRenderer>();
        if (fillSr != null)
        {
            fillSr.color = ratio > 0.45f
                ? new Color(0.92f, 0.22f, 0.24f, 1f)
                : new Color(1f, 0.55f, 0.15f, 1f);
        }
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
        return whiteSprite;
    }
}
