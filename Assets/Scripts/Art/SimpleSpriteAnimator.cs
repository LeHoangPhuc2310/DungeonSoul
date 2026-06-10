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
    private Sprite[] attackFront;
    private Sprite[] attackBackFrames;
    private Sprite[] attackSideFrames;
    private Sprite[] attackSideLeftFrames;
    private bool fourWayAttack;
    private Sprite[] activeFrames;
    private float frameDuration = 0.12f;
    private float timer;
    private int current;
    private SpriteRenderer sr;

    private Transform moveTarget;     // transform để đo di chuyển (mặc định chính nó)
    private Vector3 lastPos;
    private bool autoFlip = true;
    private bool fourWayMode;
    private Sprite[] idleFront;
    private Sprite[] walkFront;
    private Sprite[] idleBack;
    private Sprite[] walkBack;
    private Sprite[] idleSide;
    private Sprite[] walkSide;
    private Sprite[] idleSideLeft;
    private Sprite[] walkSideLeft;
    private bool hasSideLeft;
    private Vector2 lastFacingDir = Vector2.down;
    private enum Facing { Front, Back, Side }
    private Facing currentFacing = Facing.Front;
    private bool faceByTargetDir;     // lật mặt về phía 1 transform (cho quái đuổi player)
    private Transform faceTarget;
    private bool playingOneShot;
    private bool deathPlaying;
    private bool attackPlaying;
    private bool stabilizeVisualCenter;
    private System.Action attackHitCallback;
    private System.Action attackCompleteCallback;

    public bool IsAttacking => attackPlaying;

    /// <summary>Giữ tâm hình cố định khi đổi frame (tránh giật do pivot khác nhau giữa idle/attack).</summary>
    public void SetStabilizeVisualCenter(bool value) => stabilizeVisualCenter = value;

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

    public bool UsesFourDirections => fourWayMode;

    /// <summary>Bật idle/walk theo 4 hướng: front (xuống), back (lên), side trái/phải.</summary>
    public void SetFourWaySprites(Sprite[] idleF, Sprite[] walkF, Sprite[] idleB, Sprite[] walkB,
        Sprite[] idleSideR, Sprite[] walkSideR, Sprite[] idleSideL = null, Sprite[] walkSideL = null)
    {
        idleFront = idleF;
        walkFront = walkF;
        idleBack = idleB;
        walkBack = walkB;
        idleSide = idleSideR;
        walkSide = walkSideR;
        idleSideLeft = idleSideL;
        walkSideLeft = walkSideL;
        hasSideLeft = idleSideLeft != null && idleSideLeft.Length > 0
            && walkSideLeft != null && walkSideLeft.Length > 0;
        fourWayMode = idleBack != null && idleBack.Length > 0
            && walkBack != null && walkBack.Length > 0
            && idleSide != null && idleSide.Length > 0
            && walkSide != null && walkSide.Length > 0;
        if (fourWayMode)
        {
            idleFrames = idleFront;
            walkFrames = walkFront;
            currentFacing = Facing.Front;
            lastFacingDir = Vector2.down;
        }
    }

    /// <summary>Cập nhật hướng nhìn theo input di chuyển (gọi từ PlayerController).</summary>
    public void SetFacingFromInput(Vector2 input)
    {
        if (!fourWayMode || sr == null || deathPlaying || attackPlaying)
            return;

        if (input.sqrMagnitude > 0.001f)
            lastFacingDir = input;
    }

    public void SetCombatFrames(Sprite[] hurt, Sprite[] death)
    {
        hurtFrames = (hurt != null && hurt.Length > 0) ? hurt : null;
        deathFrames = (death != null && death.Length > 0) ? death : null;
    }

    public void SetAttackFrames(Sprite[] attack)
    {
        attackFront = (attack != null && attack.Length > 0) ? attack : null;
        attackFrames = attackFront;
        fourWayAttack = false;
    }

    /// <summary>Attack theo 4 hướng (front = attack hiện có).</summary>
    public void SetFourWayAttackFrames(Sprite[] front, Sprite[] back, Sprite[] sideRight, Sprite[] sideLeft = null)
    {
        attackFront = (front != null && front.Length > 0) ? front : null;
        attackBackFrames = (back != null && back.Length > 0) ? back : null;
        attackSideFrames = (sideRight != null && sideRight.Length > 0) ? sideRight : null;
        attackSideLeftFrames = (sideLeft != null && sideLeft.Length > 0) ? sideLeft : null;
        fourWayAttack = attackFront != null
            && attackBackFrames != null
            && attackSideFrames != null;
        attackFrames = attackFront;
    }

    /// <summary>Hướng nhìn cardinal hiện tại (dùng cho melee arc / slash VFX).</summary>
    public Vector2 GetFacingDirection()
    {
        Vector2 dir = lastFacingDir.sqrMagnitude > 0.001f ? lastFacingDir : Vector2.down;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return dir.x < 0f ? Vector2.left : Vector2.right;
        return dir.y > 0f ? Vector2.up : Vector2.down;
    }

    /// <summary>Ép hướng nhìn trước khi đánh (snap cardinal).</summary>
    public void ForceFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Vector2 cardinal = SnapToCardinal(direction);
        lastFacingDir = cardinal;
        if (!fourWayMode || sr == null || deathPlaying)
            return;

        ApplyFacingSprites(cardinal, moving: false);
    }

    /// <summary>Phát animation tấn công; gọi onHit ~55% clip, onComplete khi xong.</summary>
    public bool PlayAttack(System.Action onHit, System.Action onComplete, float fps = 10f)
    {
        if (attackPlaying || deathPlaying || sr == null)
            return false;

        if (!TrySelectAttackClip(out Sprite[] clip, out bool flip))
            return false;

        attackPlaying = true;
        playingOneShot = true;
        attackHitCallback = onHit;
        attackCompleteCallback = onComplete;
        CancelInvoke(nameof(InvokeAttackHit));
        CancelInvoke(nameof(InvokeAttackComplete));
        CancelInvoke(nameof(EndHurt));
        SetActiveAttack(clip, flip, fps);

        float hitDelay = Mathf.Max(0.05f, clip.Length * frameDuration * 0.55f);
        float completeDelay = Mathf.Max(hitDelay + 0.05f, clip.Length * frameDuration);
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
        RestoreLocomotionAfterAttack();
        done?.Invoke();
    }

    private void RestoreLocomotionAfterAttack()
    {
        if (fourWayMode)
        {
            SyncFacingRefs(GetFacingDirection());
            if (idleFrames != null && idleFrames.Length > 0)
                SetActive(idleFrames, 8f);
            return;
        }

        if (idleFrames != null && idleFrames.Length > 0)
            SetActive(idleFrames, 8f);
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
            ApplySprite(activeFrames[0]);
    }

    private void SetActiveAttack(Sprite[] frames, bool flip, float fps)
    {
        if (fourWayMode)
            SyncFacingRefs(GetFacingDirection());

        activeFrames = frames;
        frameDuration = 1f / Mathf.Max(1f, fps);
        current = 0;
        timer = 0f;
        if (sr == null || activeFrames == null || activeFrames.Length == 0)
            return;

        sr.flipX = flip;
        ApplySprite(activeFrames[0]);
    }

    private void ApplySprite(Sprite sprite)
    {
        if (sr == null || sprite == null)
            return;

        sr.sprite = sprite;
        if (!stabilizeVisualCenter)
            return;

        Vector3 center = sprite.bounds.center;
        transform.localPosition = new Vector3(-center.x, -center.y, 0f);
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

        if (fourWayMode)
            ApplyFacingSprites(lastFacingDir, moving);
        else if (!playingOneShot && walkFrames != null)
        {
            Sprite[] want = moving ? walkFrames : idleFrames;
            if (want != activeFrames && want != null && want.Length > 0)
            {
                activeFrames = want;
                current = 0;
                timer = 0f;
            }
        }

        // Lật mặt theo hướng (chỉ khi không dùng 4 hướng).
        if (!fourWayMode && autoFlip)
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

    private void ApplyFacingSprites(Vector2 dir, bool moving)
    {
        dir = dir.sqrMagnitude > 0.001f ? dir : Vector2.down;
        Facing want;
        bool flip;

        if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x))
        {
            want = dir.y > 0f ? Facing.Back : Facing.Front;
            flip = false;
        }
        else
        {
            want = Facing.Side;
        }

        Sprite[] newIdle;
        Sprite[] newWalk;
        switch (want)
        {
            case Facing.Back:
                newIdle = idleBack;
                newWalk = walkBack;
                flip = false;
                break;
            case Facing.Side:
                if (hasSideLeft && dir.x < 0f)
                {
                    newIdle = idleSideLeft;
                    newWalk = walkSideLeft;
                    flip = false;
                }
                else if (hasSideLeft)
                {
                    newIdle = idleSide;
                    newWalk = walkSide;
                    flip = false;
                }
                else
                {
                    newIdle = idleSide;
                    newWalk = walkSide;
                    flip = dir.x < 0f;
                }
                break;
            default:
                newIdle = idleFront;
                newWalk = walkFront;
                flip = false;
                break;
        }

        bool facingChanged = want != currentFacing;
        currentFacing = want;
        sr.flipX = flip;

        idleFrames = newIdle;
        walkFrames = newWalk;

        if (playingOneShot)
            return;

        Sprite[] wantFrames = moving ? walkFrames : idleFrames;
        if (wantFrames == null || wantFrames.Length == 0)
            return;

        if (facingChanged || wantFrames != activeFrames)
        {
            activeFrames = wantFrames;
            current = 0;
            timer = 0f;
            ApplySprite(activeFrames[0]);
        }
    }

    private bool TrySelectAttackClip(out Sprite[] clip, out bool flip)
    {
        clip = null;
        flip = false;

        if (!fourWayAttack)
        {
            clip = attackFrames;
            flip = sr != null && sr.flipX;
            return clip != null && clip.Length > 0;
        }

        Vector2 dir = GetFacingDirection();
        if (dir == Vector2.up)
        {
            clip = attackBackFrames ?? attackFront;
            flip = false;
            return clip != null && clip.Length > 0;
        }

        if (dir == Vector2.left || dir == Vector2.right)
        {
            if (hasSideLeft && attackSideLeftFrames != null && dir == Vector2.left)
            {
                clip = attackSideLeftFrames;
                flip = false;
                return clip.Length > 0;
            }

            if (attackSideFrames != null)
            {
                clip = attackSideFrames;
                flip = !hasSideLeft && dir == Vector2.left;
                return clip.Length > 0;
            }
        }

        clip = attackFront;
        flip = false;
        return clip != null && clip.Length > 0;
    }

    /// <summary>Cập nhật flip + bộ idle/walk theo hướng, không đổi frame đang hiển thị.</summary>
    private void SyncFacingRefs(Vector2 dir)
    {
        if (!fourWayMode || sr == null)
            return;

        dir = dir.sqrMagnitude > 0.001f ? dir : Vector2.down;
        Facing want;
        bool flip;

        if (Mathf.Abs(dir.y) >= Mathf.Abs(dir.x))
        {
            want = dir.y > 0f ? Facing.Back : Facing.Front;
            flip = false;
        }
        else
        {
            want = Facing.Side;
        }

        switch (want)
        {
            case Facing.Back:
                idleFrames = idleBack;
                walkFrames = walkBack;
                flip = false;
                break;
            case Facing.Side:
                if (hasSideLeft && dir.x < 0f)
                {
                    idleFrames = idleSideLeft;
                    walkFrames = walkSideLeft;
                    flip = false;
                }
                else if (hasSideLeft)
                {
                    idleFrames = idleSide;
                    walkFrames = walkSide;
                    flip = false;
                }
                else
                {
                    idleFrames = idleSide;
                    walkFrames = walkSide;
                    flip = dir.x < 0f;
                }
                break;
            default:
                idleFrames = idleFront;
                walkFrames = walkFront;
                flip = false;
                break;
        }

        currentFacing = want;
        sr.flipX = flip;
    }

    private static Vector2 SnapToCardinal(Vector2 direction)
    {
        direction.Normalize();
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x < 0f ? Vector2.left : Vector2.right;
        return direction.y > 0f ? Vector2.up : Vector2.down;
    }

    private void AdvanceFrame()
    {
        if (activeFrames == null || activeFrames.Length <= 1 || sr == null)
            return;

        timer += Time.deltaTime;
        if (timer < frameDuration)
            return;

        timer -= frameDuration;
        if (deathPlaying || attackPlaying)
        {
            current++;
            if (current >= activeFrames.Length)
                current = activeFrames.Length - 1;
        }
        else
        {
            current = (current + 1) % activeFrames.Length;
        }

        ApplySprite(activeFrames[current]);
    }
}
