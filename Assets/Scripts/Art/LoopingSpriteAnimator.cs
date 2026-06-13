using UnityEngine;

/// <summary>Sprite animation lặp vô hạn — dùng cho vũ khí quay quanh.</summary>
public class LoopingSpriteAnimator : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite[] frames;
    private float frameDuration = 1f / 12f;
    private int index;
    private float timer;

    public void Begin(Sprite[] spriteFrames, float fps, float worldScale)
    {
        frames = spriteFrames;
        frameDuration = 1f / Mathf.Max(1f, fps);
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        if (frames != null && frames.Length > 0)
        {
            sr.sprite = frames[0];
            float h = Mathf.Max(0.02f, frames[0].bounds.size.y);
            transform.localScale = Vector3.one * Mathf.Clamp(worldScale / h, 0.05f, 3f);
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1 || sr == null)
            return;

        timer += Time.deltaTime;
        if (timer < frameDuration)
            return;

        timer -= frameDuration;
        index = (index + 1) % frames.Length;
        sr.sprite = frames[index];
    }
}
