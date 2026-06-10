using System.Collections;
using UnityEngine;

/// <summary>
/// Phản hồi trúng đòn cho một đơn vị (quái/boss): nháy trắng sprite trong tích tắc.
/// Gắn runtime khi cần (HealthSystem gọi HitFeedback.Play). Chỉ đụng tới màu SpriteRenderer
/// nên an toàn tuyệt đối với mọi loại rigidbody / AI di chuyển.
/// </summary>
[DisallowMultipleComponent]
public class HitFeedback : MonoBehaviour
{
    private static readonly Color FlashColor = Color.white;
    private const float FlashSeconds = 0.07f;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Coroutine flashRoutine;

    public static void Play(GameObject target)
    {
        if (target == null)
            return;

        HitFeedback fb = target.GetComponent<HitFeedback>();
        if (fb == null)
            fb = target.AddComponent<HitFeedback>();
        fb.Trigger();
    }

    private void CacheRenderers()
    {
        if (renderers != null)
            return;

        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
    }

    private void Trigger()
    {
        CacheRenderers();
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetFlash(true);
        yield return new WaitForSecondsRealtime(FlashSeconds);
        SetFlash(false);
        flashRoutine = null;
    }

    private void SetFlash(bool on)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null)
                continue;
            // Re-sync base color phòng khi animator vừa đổi sprite/màu giữa chừng.
            if (!on)
                sr.color = baseColors[i];
            else
                sr.color = FlashColor;
        }
    }
}
