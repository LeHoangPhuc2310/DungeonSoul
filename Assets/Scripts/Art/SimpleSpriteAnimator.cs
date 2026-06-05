// DungeonSoul — SimpleSpriteAnimator.cs — Animation lật frame + chuyển idle/walk theo di chuyển.

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleSpriteAnimator : MonoBehaviour
{
    private Sprite[] idleFrames;
    private Sprite[] walkFrames;
    private Sprite[] hurtFrames;
    private Sprite[] deathFrames;
    private Sprite[] attackFrames;
    private Sprite[] activeFrames;
    private float frameDuration = 0.12f;
    private float timer;
    private int current;
    private SpriteRenderer sr;

    private Transform moveTarget;     // transform để đo di chuyển (mặc định chính nó)
    private Vector3 lastPos;
    private bool autoFlip = true;
    private bool faceByTargetDir;     // lật mặt về phía 1 transform (cho quái đuổi player)
    private Transform faceTarget;
    private bool playingOneShot;
    private bool deathPlaying;
    private bool attackPlaying;
    private System.Action attackHitCallback;
    private System.Action attackCompleteCallback;

    public bool IsAttacking => attackPlaying;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // Mặc định đo di chuyển trên chính object này (đúng cho enemy/boss đặt animator trực tiếp).
        if (moveTarget == null)
            moveTarget = transform;
        lastPos = moveTarget.position;
    }

    /// <summary>Đặt transform dùng để đo di chuyển (vd HeroBody con đo theo player cha).</summary>
    public void SetMoveTarget(Transform t)
    {
        moveTarget = t != null ? t : transform;
        lastPos = moveTarget.position;
    }

    /// <summary>Animation đơn (chỉ idle, không đổi trạng thái).</summary>
    public void Play(Sprite[] frames, float framesPerSecond = 8f)
    {
        if (frames == null || frames.Length == 0)
            return;
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        idleFrames = frames;
        walkFrames = null;
        SetActive(frames, framesPerSecond);
    }

    /// <summary>Animation idle + walk, tự chuyển theo vận tốc di chuyển.</summary>
    public void PlayWithWalk(Sprite[] idle, Sprite[] walk, float framesPerSecond = 8f)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        idleFrames = (idle != null && idle.Length > 0) ? idle : walk;
        walkFrames = (walk != null && walk.Length > 0) ? walk : idle;
        SetActive(idleFrames, framesPerSecond);
    }

    /// <summary>Cho quái lật mặt về phía mục tiêu (vd player).</summary>
    public void SetFaceTarget(Transform target)
    {
        faceTarget = target;
        faceByTargetDir = target != null;
    }

    /// <summary>Tắt tự lật mặt (khi script khác đã điều khiển flipX, vd PlayerController).</summary>
    public void SetAutoFlip(bool value) => autoFlip = value;

    public void SetCombatFrames(Sprite[] hurt, Sprite[] death)
    {
        hurtFrames = (hurt != null && hurt.Length > 0) ? hurt : null;
        deathFrames = (death != null && death.Length > 0) ? death : null;
    }

    public void SetAttackFrames(Sprite[] attack)
    {
        attackFrames = (attack != null && attack.Length > 0) ? attack : null;
    }

    /// <summary>Phát animation tấn công; gọi onHit ~55% clip, onComplete khi xong.</summary>
    public bool PlayAttack(System.Action onHit, System.Action onComplete, float fps = 10f)
    {
        if (attackPlaying || deathPlaying || attackFrames == null || attackFrames.Length == 0 || sr == null)
            return false;

        attackPlaying = true;
        playingOneShot = true;
        attackHitCallback = onHit;
        attackCompleteCallback = onComplete;
        CancelInvoke(nameof(InvokeAttackHit));
        CancelInvoke(nameof(InvokeAttackComplete));
        CancelInvoke(nameof(EndHurt));
        SetActive(attackFrames, fps);

        float hitDelay = Mathf.Max(0.05f, attackFrames.Length * frameDuration * 0.55f);
        float completeDelay = Mathf.Max(hitDelay + 0.05f, attackFrames.Length * frameDuration);
        Invoke(nameof(InvokeAttackHit), hitDelay);
        Invoke(nameof(InvokeAttackComplete), completeDelay);
        return true;
    }

    private void InvokeAttackHit() => attackHitCallback?.Invoke();

    private void InvokeAttackComplete()
    {
        attackPlaying = false;
        playingOneShot = false;
        attackHitCallback = null;
        System.Action done = attackCompleteCallback;
        attackCompleteCallback = null;
        if (idleFrames != null && idleFrames.Length > 0)
            SetActive(idleFrames, 8f);
        done?.Invoke();
    }

    public void PlayHurt()
    {
        if (deathPlaying || hurtFrames == null || hurtFrames.Length == 0 || sr == null)
            return;

        playingOneShot = true;
        SetActive(hurtFrames, 12f);
        CancelInvoke(nameof(EndHurt));
        Invoke(nameof(EndHurt), Mathf.Max(0.08f, hurtFrames.Length * frameDuration));
    }

    public bool TryPlayDeath(out float destroyDelay)
    {
        destroyDelay = 0.35f;
        if (deathFrames == null || deathFrames.Length == 0 || sr == null)
            return false;

        deathPlaying = true;
        playingOneShot = true;
        CancelInvoke(nameof(EndHurt));
        CancelInvoke(nameof(InvokeAttackHit));
        CancelInvoke(nameof(InvokeAttackComplete));
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null)
            ai.enabled = false;
        SetActive(deathFrames, 10f);
        destroyDelay = Mathf.Max(0.25f, deathFrames.Length * frameDuration + 0.05f);
        return true;
    }

    private void EndHurt()
    {
        if (deathPlaying || sr == null)
            return;

        playingOneShot = false;
        if (idleFrames != null && idleFrames.Length > 0)
            SetActive(idleFrames, 8f);
    }

    private void SetActive(Sprite[] frames, float fps)
    {
        activeFrames = frames;
        frameDuration = 1f / Mathf.Max(1f, fps);
        current = 0;
        timer = 0f;
        if (sr != null && activeFrames != null && activeFrames.Length > 0)
            sr.sprite = activeFrames[0];
    }

    private void Update()
    {
        if (sr == null)
            return;

        if (deathPlaying || attackPlaying)
        {
            AdvanceFrame();
            return;
        }

        // Đo di chuyển để chọn idle/walk + hướng.
        Vector3 pos = moveTarget != null ? moveTarget.position : transform.position;
        Vector3 delta = pos - lastPos;
        lastPos = pos;
        float speedSqr = (delta / Mathf.Max(0.0001f, Time.deltaTime)).sqrMagnitude;
        bool moving = speedSqr > 0.02f;

        // Chuyển bộ frame idle/walk (không đổi khi đang chơi hurt one-shot).
        if (!playingOneShot && walkFrames != null)
        {
            Sprite[] want = moving ? walkFrames : idleFrames;
            if (want != activeFrames && want != null && want.Length > 0)
            {
                activeFrames = want;
                current = 0;
                timer = 0f;
            }
        }

        // Lật mặt theo hướng.
        if (autoFlip)
        {
            float faceX;
            if (faceByTargetDir && faceTarget != null)
                faceX = faceTarget.position.x - pos.x;
            else
                faceX = delta.x;

            if (Mathf.Abs(faceX) > 0.001f)
                sr.flipX = faceX < 0f;
        }

        AdvanceFrame();
    }

    private void AdvanceFrame()
    {
        if (activeFrames == null || activeFrames.Length <= 1 || sr == null)
            return;

        timer += Time.deltaTime;
        if (timer < frameDuration)
            return;

        timer -= frameDuration;
        if (deathPlaying)
        {
            current++;
            if (current >= activeFrames.Length)
                current = activeFrames.Length - 1;
        }
        else
        {
            current = (current + 1) % activeFrames.Length;
        }

        sr.sprite = activeFrames[current];
    }
}
