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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void ApplySet(EnemyAnimationSet animationSet)
    {
        set = animationSet;
        if (set == null || spriteRenderer == null)
            return;

        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 8;
        Play(AnimState.Idle, true);
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

        spriteRenderer.sprite = currentFrames[frameIndex];
    }

    private void Play(AnimState next, bool forceRestart = false)
    {
        if (!forceRestart && state == next && currentFrames != null)
            return;

        state = next;
        currentFrames = GetFrames(next);
        frameIndex = 0;
        frameTimer = 0f;

        if (currentFrames != null && currentFrames.Length > 0)
            spriteRenderer.sprite = currentFrames[0];
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
