// DungeonSoul — EnemySpriteAnimator.cs — Phát animation sprite sheet (Kenney enemies).

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    public enum AnimState
    {
        Idle,
        Move,
        Hurt,
        Death
    }

    private SpriteRenderer spriteRenderer;
    private EnemyAnimationSet set;
    private AnimState state = AnimState.Idle;
    private Sprite[] currentFrames;
    private float frameTimer;
    private int frameIndex;
    private float hurtTimer;
    private bool deathPlaying;
    private bool moving;

    public bool IsDeathPlaying => deathPlaying;
    public float DeathDuration => set != null ? set.DeathDuration : 0.4f;

    private int visualFixFrames;

    private void Awake()
    {
        ResolveRenderer();
        ClearPrefabPlaceholder();
    }

    private void Start()
    {
        EnsureVisualReady();
    }

    private void LateUpdate()
    {
        if (visualFixFrames >= 10)
            return;

        visualFixFrames++;
        if (EnemyVisualUtil.NeedsVisualFix(spriteRenderer))
            EnsureVisualReady();
    }

    private void ClearPrefabPlaceholder()
    {
        if (!ResolveRenderer())
            return;

        if (EnemyVisualUtil.IsPlaceholderSprite(spriteRenderer.sprite)
            || (spriteRenderer.color.r > 0.95f && spriteRenderer.color.g < 0.12f && spriteRenderer.color.b < 0.12f))
        {
            spriteRenderer.sprite = null;
            spriteRenderer.color = Color.white;
        }
    }

    private void EnsureVisualReady()
    {
        if (!ResolveRenderer() || !EnemyVisualUtil.NeedsVisualFix(spriteRenderer))
            return;

        EnemyArchetype archetype = EnemyArchetype.Grunt;
        EnemyAnimationSet animSet = set;
        EnemyArchetypeMarker marker = GetComponent<EnemyArchetypeMarker>();
        if (marker != null)
        {
            archetype = marker.Archetype;
            if (marker.AnimationSet != null)
                animSet = marker.AnimationSet;
        }

        if (animSet == null)
            animSet = EnemyVisualLibrary.PickRandomSet(archetype);

        if (animSet != null)
        {
            EnemyVisualLibrary.ApplySet(gameObject, animSet, archetype);
            if (marker != null && marker.AnimationSet == null)
                marker.Set(archetype, animSet);
            return;
        }

        EnemyVisualUtil.ApplyStaticFallback(gameObject, archetype);
    }

    /// <summary>
    /// Quái tạo runtime có thể add component này TRƯỚC SpriteRenderer → Awake lấy null.
    /// Gọi lại trước mỗi lần dùng; nếu vẫn null thì caller phải bỏ qua (không được NRE —
    /// NRE trong PlayHurt sẽ ngắt TakeDamage trước Die() → quái kẹt 0 HP bất tử).
    /// </summary>
    private bool ResolveRenderer()
    {
        if (spriteRenderer != null)
            return true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null;
    }

    public void ApplySet(EnemyAnimationSet animationSet)
    {
        set = animationSet;
        if (set == null || !ResolveRenderer())
            return;

        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 8;

        Sprite preview = set.PreviewSprite;
        if (preview != null)
            spriteRenderer.sprite = preview;

        Play(AnimState.Idle, true);

        if (preview != null && (spriteRenderer.sprite == null || EnemyVisualUtil.IsPlaceholderSprite(spriteRenderer.sprite)))
            spriteRenderer.sprite = preview;
    }

    public void SetMoving(bool value)
    {
        moving = value && !deathPlaying;
        if (deathPlaying || state == AnimState.Hurt)
            return;

        Play(moving ? AnimState.Move : AnimState.Idle);
    }

    public void PlayHurt()
    {
        if (deathPlaying || set == null || set.hurt == null || set.hurt.Length == 0)
            return;

        hurtTimer = set.hurt.Length / Mathf.Max(1f, set.hurtFps);
        Play(AnimState.Hurt, true);
    }

    public bool TryPlayDeath(out float destroyDelay)
    {
        destroyDelay = DeathDuration;
        if (set == null || set.death == null || set.death.Length == 0)
            return false;

        deathPlaying = true;
        moving = false;
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Play(AnimState.Death, true);
        return true;
    }

    private void Update()
    {
        if (set == null || currentFrames == null || currentFrames.Length == 0)
            return;

        if (state == AnimState.Hurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f && !deathPlaying)
                Play(moving ? AnimState.Move : AnimState.Idle);
        }

        float fps = GetFps(state);
        frameTimer += Time.deltaTime;
        if (frameTimer < 1f / fps)
            return;

        frameTimer = 0f;
        frameIndex++;
        if (frameIndex >= currentFrames.Length)
        {
            if (state == AnimState.Death)
                frameIndex = currentFrames.Length - 1;
            else
                frameIndex = 0;
        }

        if (ResolveRenderer())
        {
            Sprite frame = GetFrameSprite(currentFrames, frameIndex);
            if (frame != null)
                spriteRenderer.sprite = frame;
        }
    }

    private void Play(AnimState next, bool forceRestart = false)
    {
        if (!forceRestart && state == next && currentFrames != null)
            return;

        state = next;
        currentFrames = GetFrames(next);
        frameIndex = 0;
        frameTimer = 0f;

        if (ResolveRenderer() && currentFrames != null && currentFrames.Length > 0)
        {
            Sprite frame = GetFrameSprite(currentFrames, frameIndex);
            if (frame != null)
                spriteRenderer.sprite = frame;
        }
    }

    private static Sprite GetFrameSprite(Sprite[] frames, int index)
    {
        if (frames == null || frames.Length == 0)
            return null;

        int i = Mathf.Clamp(index, 0, frames.Length - 1);
        if (frames[i] != null)
            return frames[i];

        for (int j = 0; j < frames.Length; j++)
        {
            if (frames[j] != null)
                return frames[j];
        }

        return null;
    }

    private Sprite[] GetFrames(AnimState s)
    {
        if (set == null)
            return null;

        return s switch
        {
            AnimState.Move => set.move != null && set.move.Length > 0 ? set.move : set.idle,
            AnimState.Hurt => set.hurt,
            AnimState.Death => set.death,
            _ => set.idle
        };
    }

    private float GetFps(AnimState s)
    {
        if (set == null)
            return 10f;

        return s switch
        {
            AnimState.Move => set.moveFps,
            AnimState.Hurt => set.hurtFps,
            AnimState.Death => set.deathFps,
            _ => set.idleFps
        };
    }
}
