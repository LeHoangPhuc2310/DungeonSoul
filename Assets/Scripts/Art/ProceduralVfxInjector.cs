// DungeonSoul — Nạp VFX procedural vào GeneratedSkillVfxLibrary khi game khởi động.
// Chạy trước Warmup(); nếu sau đó Resources.LoadAll tìm thấy PNG thật → tự override.

using UnityEngine;

public static class ProceduralVfxInjector
{
    private static bool injected;

    public static void InjectAll()
    {
        if (injected) return;
        injected = true;

        // Đã TẮT: hiệu ứng kỹ năng nay chỉ dùng sprite PNG trong Resources/GeneratedSkillVfx/PerSkill.
        // Không bơm frame procedural (vẽ bằng code) nữa — tất cả 28 skill đã có đủ folder PerSkill.
        // Nếu muốn bật lại làm fallback, khôi phục các lời gọi InjectFrames(...) ở lịch sử git.
    }
}
