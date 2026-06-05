using UnityEngine;

/// <summary>Plays coin_1..coin_4 spin frames from 2D Pixel Dungeon pack.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CoinPickupAnimator : MonoBehaviour
{
    [SerializeField] private float framesPerSecond = 10f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private int frameIndex;
    private float frameTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (frames == null || frames.Length == 0)
            frames = DungeonPackLibrary.GetCoinSpinFrames();

        frameIndex = 0;
        frameTimer = 0f;
        ApplyFrame();
    }

    public void SetFrames(Sprite[] spinFrames)
    {
        frames = spinFrames;
        frameIndex = 0;
        frameTimer = 0f;
        ApplyFrame();
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);
        if (frameTimer < frameDuration)
            return;

        frameTimer = 0f;
        frameIndex = (frameIndex + 1) % frames.Length;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        Sprite frame = frames[frameIndex % frames.Length];
        if (frame != null)
            spriteRenderer.sprite = frame;
    }
}
