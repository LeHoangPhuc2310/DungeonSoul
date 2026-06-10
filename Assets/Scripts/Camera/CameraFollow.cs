using UnityEngine;

[RequireComponent(typeof(GameplayPresentation))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10);

    private Vector3 followPosition;

    private void Awake()
    {
        if (GetComponent<GameplayPresentation>() == null)
            gameObject.AddComponent<GameplayPresentation>();
        GameJuice.Ensure();
        followPosition = transform.position;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            // Lerp dùng unscaledDeltaTime để hit-stop (timeScale=0) không làm camera "đơ giật".
            followPosition = Vector3.Lerp(followPosition, desiredPosition, smoothSpeed * Time.unscaledDeltaTime);
        }

        // Vị trí thực = vị trí bám mượt + offset rung. Tách riêng để shake không tích lũy vào followPosition.
        transform.position = followPosition + GameJuice.CurrentShakeOffset;
    }
}
