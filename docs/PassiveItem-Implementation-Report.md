# Passive Item System — Báo cáo triển khai Phase 1

**Ngày:** 2026-06-08  
**Phạm vi:** Wave Arena — passive kiểu Vampire Survivors / Brotato (không inventory, không equipment slots).

---

## 1. Audit ban đầu (trước khi code)

| Thành phần | Trạng thái trước | Hành động |
|------------|------------------|-----------|
| `PassiveItemData.cs` | Không tồn tại (chỉ có `PassiveItem.cs` enum cũ) | **Tạo mới** |
| `PassiveItemManager.cs` | Có — pool rỗng, max 8, không stack level | **Refactor** |
| `SkillSelectionUI.BuildChoices()` | Có nhánh passive nhưng pool rỗng | **Sửa** |
| `HUDManager.UpdatePassiveSlots()` | Stub rỗng | **Implement** |
| `WeaponManager` evolve | Chỉ 3 copies → evolve | **Mở rộng** passive + cấp 8 |
| `Resources/PassiveItems/` | Không có | **Tạo 12 SO** |

---

## 2. Files tạo mới

| File | Mô tả |
|------|--------|
| `Assets/Scripts/Items/PassiveItemData.cs` | SO + enum stat + `PassivePick` runtime |
| `Assets/Scripts/UI/PassiveSwapUI.cs` | Popup thay 1 trong 2 passive khi đủ 6 ô |
| `Assets/Editor/PassiveItemBuilder.cs` | Menu tạo 12 asset + debug spawn theo rarity |
| `Assets/Resources/PassiveItems/*.asset` | 12 passive Phase 1 |
| `docs/PassiveItem-Implementation-Report.md` | Báo cáo này |

## 3. Files sửa đổi

| File | Thay đổi chính |
|------|----------------|
| `PassiveItemManager.cs` | Stack level, rarity roll, apply stats, evolve check, swap |
| `SkillSelectionUI.cs` | 1 skill + 1 weapon + 1 passive; chest 10%; boss 50/50 |
| `HUDManager.cs` | Passive bar 6×32px, level corner, tooltip |
| `HudHoverTooltip.cs` / `SkillTooltipUI.cs` | Tooltip passive |
| `GameIconLibrary.cs` | Icon/tint theo `PassiveStatModifierType` |
| `WeaponManager.cs` | `TryEvolveWithPassive`, `ExtraProjectileCount`, lifesteal hook |
| `WeaponEvolution.cs` | (giữ nguyên — FX dùng chung) |
| `HealthSystem.cs` | `SetPassiveDamageReduction`, revive passive |
| `PlayerSkillStats.cs` | Crit / lifesteal / burn từ passive |
| `PlayerSkillHandler.cs` | Gọi `ApplyAggregatedToPlayer` sau skill recalc |
| `ExpSystem.cs` | Nhân EXP theo `ExpGainMultiplier` |
| `Projectile.cs` | Burn proc từ passive |
| `SkillBehaviors.cs` | Lifesteal không cần skill LifeSteal |

## 4. Files xóa

| File | Lý do |
|------|--------|
| `Assets/Scripts/Items/PassiveItem.cs` | Thay bằng `PassiveItemData` |

---

## 5. Danh sách 12 passive (balance cuối)

| # | ID | Tên | Stat | Mỗi cấp | Max | Rarity | Tổng tối đa |
|---|-----|-----|------|---------|-----|--------|-------------|
| 1 | `ao_giap_da` | Áo Giáp Da | Giảm ST nhận | −4% | 5 | Common | −20% |
| 2 | `tim_rong` | Tim Rỗng | HP max | +20 | 5 | Common | +100 HP |
| 3 | `canh_quat` | Cánh Quạ | Move speed | +5% | 5 | Common | +25% |
| 4 | `mong_vuot` | Móng Vuốt | Damage | +10% | 5 | Common | ×1.61 (~+61%) |
| 5 | `dong_ho_cat` | Đồng Hồ Cát | Cooldown | −8% | 5 | Rare | ~−34% CD |
| 6 | `vuong_mien_exp` | Vương Miện | EXP gain | +8% | 5 | Rare | +40% EXP |
| 7 | `luoi_sac` | Lưỡi Sắc | Crit chance | +5% | 5 | Rare | +25% crit |
| 8 | `tui_tham_lam` | Túi Tham Lam | Magnet radius | +0.5 | 5 | Rare | +2.5 |
| 9 | `luoi_lua` | Lưỡi Lửa | Burn chance | +10% | 3 | Epic | +30% burn |
| 10 | `hon_quy` | Hồn Quỷ | Lifesteal | +3% | 3 | Epic | +9% LS |
| 11 | `manh_hon_rong` | Mảnh Hồn Rồng | Projectile | +1 | 3 | Epic | +3 projectiles |
| 12 | `vuong_mien_vinh_cuu` | Vương Miện Vĩnh Cửu | Revive 1× @ 50% HP | — | 1 | Legendary | 1 lần/run |

*Damage/Cooldown dùng nhân dồn (compound) giống VS.*

---

## 6. Evolve combo (weapon cấp 8 + passive max → cuối wave)

| Vũ khí gốc (cấp 8) | Passive max | Kết quả | Bonus thêm |
|--------------------|-------------|---------|------------|
| `PoisonDagger` (Kiếm) | Móng Vuốt | `DeathDagger` | ×3 damage (+200%) |
| `IronBow` (Cung) | Đồng Hồ Cát | `StormBow` | Multi-shot mặc định |
| `FireStaff` (Gậy lửa) | Lưỡi Lửa | `DragonStaff` | ×1.6 damage + nổ vùng |

Điều kiện kiểm tra trong `CompleteChestReward()` → `PassiveItemManager.CheckPassiveEvolutionsAtWaveEnd()`.

Evolve 3 copies (cũ) vẫn hoạt động song song qua `WeaponEvolution`.

---

## 7. Tỉ lệ rơi (skill selection)

**Level-up thường:** Common 60% · Rare 30% · Epic 9% · Legendary 1%

**Rương boss (`RoomType.Treasure`):** Rare 40% · Epic 50% · Legendary 10%

**Rương wave thường:** 3 skill, **10%** một ô thay bằng passive.

---

## 8. Integration checklist

| Kiểm tra | Trạng thái |
|----------|------------|
| `availableItems` có 12 entry (Resources auto-load) | ✅ |
| Level-up: 1 skill + 1 weapon + 1 passive | ✅ |
| Stack passive đến `maxLevel` | ✅ |
| Stat áp vào player (HP, speed, damage, crit, v.v.) | ✅ |
| Evolve cuối wave khi đủ điều kiện | ✅ |
| HUD 6 icon + số cấp + tooltip | ✅ |
| Swap popup khi 6 ô đầy | ✅ |
| Không sửa wave/boss spawn | ✅ |
| E2E playtest trong Editor | ⚠️ Cần chạy thủ công |

---

## 9. Debug menu

Trong Unity Editor (Play mode):

- `Tools/DungeonSoul/Debug/Spawn Passive - Common`
- `Tools/DungeonSoul/Debug/Spawn Passive - Rare`
- `Tools/DungeonSoul/Debug/Spawn Passive - Epic`
- `Tools/DungeonSoul/Debug/Spawn Passive - Legendary`

Tạo lại asset: `Tools/DungeonSoul/Create All Passive Items`

---

## 10. Known issues / TODO

1. **HP + skill IronBody:** `baseMaxHp` cache có thể lệch nếu skill HP apply sau passive — cần playtest cân bằng.
2. **Lifesteal từ vũ khí AOE** (`DamageNearbyEnemies`) chưa gọi `SkillBehaviors` — chỉ single-target `DamageTarget`.
3. **Burn từ passive** chỉ hook trên `Projectile.cs` — damage vũ khí trực tiếp chưa roll burn.
4. **Tap mobile tooltip:** hiện dùng hover (`HudHoverTooltip`) — mobile cần long-press (Phase 2).
5. **SampleScene** `maxPassiveItems` trên component có thể vẫn = 8 trong Inspector — đổi tay về 6 hoặc rely on code default khi tạo mới.

---

## 11. Designer workflow — thêm passive mới

1. **Tạo asset:** `Create → DungeonSoul → Items → Passive Item Data`  
   Hoặc thêm definition vào `PassiveItemBuilder.cs` rồi chạy menu Create All.

2. **Điền field:**
   - `id` (unique snake_case)
   - `displayName`, `description` (tiếng Việt)
   - `statModifierType`, `valuePerLevel[]`, `maxLevel`, `rarity`, `isPercent`

3. **Evolve (tùy chọn):** bật `enablesWeaponEvolve`, gán `evolveTargetWeapon` / `evolveResultWeapon` / `evolveDamageMultiplier`.

4. **Đặt file** vào `Assets/Resources/PassiveItems/` — `PassiveItemManager` tự `LoadAll` nếu list Inspector rỗng.

5. **Icon:** gán `icon` sprite hoặc để trống (dùng tile từ `GameIconLibrary.PassiveTileByStat`).

6. **Playtest:** Debug menu spawn theo rarity → kiểm tra HUD + stat + level-up pool.

---

## 12. Kiểm thử đề xuất (E2E)

1. Play `SampleScene` → level-up → thấy 3 thẻ (skill / weapon / passive).
2. Chọn passive → icon xuất hiện HUD dưới weapon bar.
3. Chọn cùng passive lần 2 → cấp tăng trên HUD.
4. Nhặt 6 passive khác → lần thứ 7 mở swap popup.
5. Cuối wave mở rương → 10% thấy passive (hoặc boss 50%).
6. Debug spawn `mong_vuot` + nâng `PoisonDagger` lên 8 → cuối wave evolve `DeathDagger`.
