using System.Collections;
using UnityEngine;

/// <summary>
/// Trung tâm "game feel": screen shake + hit-stop (freeze frame).
/// Tự gắn lên camera chính khi cần. Mọi nơi gọi qua API tĩnh:
/// GameJuice.Shake(...), GameJuice.HitStop(...).
/// Shake chạy theo unscaled time để hit-stop (timeScale=0) không làm đơ shake.
/// CameraFollow đọc CurrentShakeOffset và tự cộng vào vị trí cuối (tránh tranh chấp LateUpdate).
/// </summary>
public class GameJuice : MonoBehaviour
{
    public static GameJuice Instance { get; private set; }

    /// <summary>Offset rung hiện tại (world units) — CameraFollow cộng vào sau khi tính vị trí.</summary>
    public static Vector3 CurrentShakeOffset { get; private set; }

    private const float MaxHitStopSeconds = 0.1f;
    private const float HitStopCooldown = 0.05f;

    private float shakeAmplitude;
    private float shakeDuration;
    private float shakeTimer;
    private float shakeFrequency = 28f;
    private Vector2 shakeSeed;

    private Coroutine hitStopRoutine;
    private bool hitStopActive;
    private float hitStopUntilRealtime;
    private float hitStopCooldownUntilRealtime;

    /// <summary>Đảm bảo tồn tại + bám vào camera chính.</summary>
    public static GameJuice Ensure()
    {
        if (Instance != null)
            return Instance;

        Instance = Object.FindAnyObjectByType<GameJuice>(FindObjectsInactive.Include);
        if (Instance == null)
        {
            GameObject go = new GameObject("GameJuice");
            Instance = go.AddComponent<GameJuice>();
        }

        Instance.ResolveShakeTarget();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        shakeSeed = new Vector2(Random.value * 100f, Random.value * 100f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        CurrentShakeOffset = Vector3.zero;
    }

    private void ResolveShakeTarget()
    {
        // Không cần giữ tham chiếu camera nữa — CameraFollow tự đọc CurrentShakeOffset.
    }

    /// <summary>Rung màn hình. amplitude theo world units, duration theo giây.</summary>
    public static void Shake(float amplitude, float duration, float frequency = 28f)
    {
        GameJuice juice = Ensure();
        if (juice != null)
            juice.DoShake(amplitude, duration, frequency);
    }

    private void DoShake(float amplitude, float duration, float frequency)
    {
        // "Lấy cái mạnh hơn" để nhiều hit liên tiếp không cộng dồn quá đà.
        shakeAmplitude = Mathf.Max(shakeTimer > 0f ? shakeAmplitude : 0f, amplitude);
        shakeDuration = Mathf.Max(shakeTimer > 0f ? shakeDuration : 0f, duration);
        shakeFrequency = frequency;
        shakeTimer = shakeDuration;
    }

    /// <summary>Freeze-frame cực ngắn khi đánh trúng — tạo cảm giác "đấm có lực".</summary>
    public static void HitStop(float seconds)
    {
        GameJuice juice = Ensure();
        if (juice != null)
            juice.DoHitStop(seconds);
    }

    private void DoHitStop(float seconds)
    {
        if (seconds <= 0f)
            return;

        // Không hit-stop khi game đang pause thật (panel skill, pause menu).
        if (IsGameplayPaused())
            return;

        float now = Time.unscaledTime;
        if (now < hitStopCooldownUntilRealtime)
            return;

        float duration = Mathf.Clamp(seconds, 0.01f, MaxHitStopSeconds);
        float newEnd = now + duration;

        if (!hitStopActive)
        {
            hitStopUntilRealtime = newEnd;
            hitStopActive = true;
            Time.timeScale = 0f;
            if (hitStopRoutine != null)
                StopCoroutine(hitStopRoutine);
            hitStopRoutine = StartCoroutine(HitStopRoutine());
            return;
        }

        // Đang freeze: chỉ kéo dài tối đa MaxHitStopSeconds — tránh đứng vĩnh viễn khi AOE trúng hàng loạt.
        hitStopUntilRealtime = Mathf.Min(
            Mathf.Max(hitStopUntilRealtime, newEnd),
            now + MaxHitStopSeconds);
    }

    private IEnumerator HitStopRoutine()
    {
        while (Time.unscaledTime < hitStopUntilRealtime)
            yield return null;

        EndHitStop();
    }

    private void EndHitStop()
    {
        if (Time.timeScale == 0f && !IsGameplayPaused())
            Time.timeScale = 1f;

        hitStopActive = false;
        hitStopCooldownUntilRealtime = Time.unscaledTime + HitStopCooldown;
        hitStopRoutine = null;
    }

    private static bool IsGameplayPaused()
    {
        if (SkillSelectionUI.Instance != null && SkillSelectionUI.Instance.IsPanelOpen)
            return true;

        if (PauseMenuUI.Instance != null && PauseMenuUI.Instance.IsPaused)
            return true;

        return false;
    }

    private void Update()
    {
        // Safety net: luôn gỡ hit-stop nếu coroutine bị gián đoạn.
        if (hitStopActive && Time.unscaledTime >= hitStopUntilRealtime)
        {
            if (hitStopRoutine != null)
                StopCoroutine(hitStopRoutine);
            EndHitStop();
        }

        if (shakeTimer <= 0f)
        {
            CurrentShakeOffset = Vector3.zero;
            return;
        }

        shakeTimer -= Time.unscaledDeltaTime;
        if (shakeTimer <= 0f)
        {
            shakeAmplitude = 0f;
            CurrentShakeOffset = Vector3.zero;
            return;
        }

        float strength = shakeAmplitude * (shakeTimer / Mathf.Max(0.0001f, shakeDuration));
        float t = Time.unscaledTime * shakeFrequency;
        float offsetX = (Mathf.PerlinNoise(shakeSeed.x, t) - 0.5f) * 2f;
        float offsetY = (Mathf.PerlinNoise(shakeSeed.y, t) - 0.5f) * 2f;
        CurrentShakeOffset = new Vector3(offsetX, offsetY, 0f) * strength;
    }
}
