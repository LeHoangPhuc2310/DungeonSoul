// DungeonSoul — HudKeepAlive.cs — Bảo đảm HUDManager luôn enabled (chống lỗi reload giữa Play).

using UnityEngine;

/// <summary>
/// Lightweight watchdog created by <see cref="HUDManager"/>. A mid-play script
/// recompile / domain reload can leave the HUDManager component disabled while the
/// GameObject stays active, which silently stops every per-frame HUD update.
/// This separate component re-asserts the enabled flag so the bars keep refreshing.
/// </summary>
[DefaultExecutionOrder(-500)]
public class HudKeepAlive : MonoBehaviour
{
    public HUDManager target;

    private void LateUpdate()
    {
        if (target == null)
        {
            target = HUDManager.Resolve();
            if (target == null)
                return;
        }

        if (!target.enabled)
            target.enabled = true;
    }
}
