using System.Collections;
using UnityEngine;

/// <summary>
/// Phản hồi trúng đòn: quái nháy trắng rồi fade đỏ; player chỉ nháy trắng.
/// </summary>
[DisallowMultipleComponent]
public class HitFeedback : MonoBehaviour
{
    private static readonly Color WhiteFlash = Color.white;
    private static readonly Color RedTint   = new Color(1f, 0.15f, 0.15f, 1f);

    private const float WhiteSeconds = 0.06f;   // trắng chớp
    private const float RedSeconds   = 0.18f;   // đỏ fade-out sau đó

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Coroutine flashRoutine;
    private bool isEnemy;

    public static void Play(GameObject target)
    {
        if (target == null) return;
        HitFeedback fb = target.GetComponent<HitFeedback>();
        if (fb == null) fb = target.AddComponent<HitFeedback>();
        fb.Trigger();
    }

    /// <summary>Player dính sát thương: nháy ĐỎ rõ rồi fade về màu gốc (mạnh hơn nháy trắng
    /// mặc định) để người chơi cảm nhận rõ mình đang mất máu.</summary>
    public static void PlayHurt(GameObject target)
    {
        if (target == null) return;
        HitFeedback fb = target.GetComponent<HitFeedback>();
        if (fb == null) fb = target.AddComponent<HitFeedback>();
        fb.TriggerHurt();
    }

    private void TriggerHurt()
    {
        CacheRenderers();
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HurtFlash());
    }

    // Player ăn đòn: đỏ đậm → fade về base.
    private IEnumerator HurtFlash()
    {
        SetColor(RedTint);
        yield return new WaitForSecondsRealtime(WhiteSeconds);

        float elapsed = 0f;
        while (elapsed < RedSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / RedSeconds;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = Color.Lerp(RedTint, baseColors[i], t);
            }
            yield return null;
        }

        RestoreBase();
        flashRoutine = null;
    }

    private void Awake()
    {
        isEnemy = !CompareTag("Player");
    }

    private void CacheRenderers()
    {
        if (renderers != null) return;

        SpriteRenderer[] all = GetComponentsInChildren<SpriteRenderer>(true);
        int count = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && !IsOverheadRenderer(all[i])) count++;

        renderers  = new SpriteRenderer[count];
        baseColors = new Color[count];
        int w = 0;
        for (int i = 0; i < all.Length; i++)
        {
            SpriteRenderer sr = all[i];
            if (sr == null || IsOverheadRenderer(sr)) continue;
            renderers[w]  = sr;
            baseColors[w] = sr.color;
            w++;
        }
    }

    private void Trigger()
    {
        CacheRenderers();
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(isEnemy ? EnemyFlash() : PlayerFlash());
    }

    // Quái: trắng → đỏ fade → base
    private IEnumerator EnemyFlash()
    {
        SetColor(WhiteFlash);
        yield return new WaitForSecondsRealtime(WhiteSeconds);

        // Fade đỏ → màu gốc
        float elapsed = 0f;
        while (elapsed < RedSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / RedSeconds;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = Color.Lerp(RedTint, baseColors[i], t);
            }
            yield return null;
        }

        RestoreBase();
        flashRoutine = null;
    }

    // Player: chỉ nháy trắng ngắn
    private IEnumerator PlayerFlash()
    {
        SetColor(WhiteFlash);
        yield return new WaitForSecondsRealtime(WhiteSeconds);
        RestoreBase();
        flashRoutine = null;
    }

    private static bool IsOverheadRenderer(SpriteRenderer sr)
    {
        Transform t = sr.transform;
        while (t != null)
        {
            if (t.name == "OverheadHP") return true;
            t = t.parent;
        }
        return false;
    }

    private void SetColor(Color c)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = c;
    }

    private void RestoreBase()
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].color = baseColors[i];
    }
}
