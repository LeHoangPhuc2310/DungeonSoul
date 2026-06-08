# SkillSelectionUI — Báo cáo nâng cấp 3-source pool + Card UI

**Ngày:** 2026-06-08

---

## 1. Audit trước khi sửa

| Khả năng | Trạng thái cũ |
|----------|----------------|
| `Show()` level-up | Có — `BuildChoices()` cố định 1 skill + 1 weapon + 1 passive |
| `ShowChest(room)` | Có — skill-only, boss 50/50 passive (logic riêng) |
| Pool 3 nguồn có trọng số | **Không** — không có weight skill/passive/weapon |
| Loại trừ maxed / owned | **Một phần** — passive qua manager; skill không check max |
| Card UI | Icon 68px, rarity text, mô tả, stack đơn giản |
| Reroll | Có — 1 lần + meta bonus, **không tốn xu** |
| Skip | **Không** |
| Synergy evolve glow | **Không** |
| Context title / gradient | Cố định "CHỌN KỸ NĂNG" |
| Mobile confirm / long-press | Chạm 1 lần = chọn ngay |
| Fallback pool < 3 | Fill bằng skill thừa |

---

## 2. Sau khi sửa — tóm tắt

### Pool (`SkillSelectionPoolBuilder.cs`)

| Context | Skill | Passive | Weapon |
|---------|-------|---------|--------|
| Level-up | 50% | 30% | 20% |
| Rương thường / Elite | 70% | 30% | 0% |
| Rương Boss | 40% | 40% | 20% |

**Rarity:** Common 55% · Rare 30% · Epic 12% · Legend 3%  
**Boss rarity:** Rare 40% · Epic 50% · Legend 10%

**Lọc pool:**
- Skill: loại max stack (3, legendary = 1)
- Passive: loại max; slot đầy chỉ còn upgrade
- Weapon: loại đã sở hữu max cấp 8; boss có thể offer evolved

**Edge cases:**
- Level-up đầu (level ≤ 2): chỉ skill (tutorial)
- Pool rỗng: 1 thẻ Heal 30 HP + ghi chú
- Pool < 3: fill +10 HP / +5 xu

Bật debug log: `SkillSelectionConfig.logPoolWeights = true`

### Card UI

- Icon **96×96**, badge loại (SKILL / WEAPON / PASSIVE) + màu chuẩn
- Viền rarity 4px (gray / green / purple / gold)
- Badge cấp: MỚI! / Lv X → Y / MAX
- Dòng stat in đậm, mô tả italic tối đa 2 dòng
- Progress bar footer `current/max`
- Synergy: viền vàng + "Có thể evolve!"

### Reroll / Skip

- **Reroll** góc trái dưới: **50 xu**, tối đa **3** lần/panel (+ meta `ExtraForgeRerolls`)
- **Skip** góc phải: popup xác nhận → hồi **10% HP max**
- **Xác nhận** giữa dưới: chọn thẻ trước, rồi bấm (hoặc double-tap thẻ)

### Animation

- Thẻ slide-in từ bottom-right (stagger 0.1s)
- Reroll: shuffle wobble
- Chọn: scale 1.15 + fade thẻ khác; panel slide down khi đóng
- Hover: scale 1.05 (`SkillCardInteraction`)

### Context visuals

| Context | Tiêu đề | Nền |
|---------|---------|-----|
| Level-up | `Lên cấp N!` | Xanh |
| Rương thường | `Rương báu` | Nâu |
| Elite | `Rương Elite` | Tím |
| Boss | `Rương Boss — [Tên]` | Vàng |

---

## 3. Files tạo mới

| File | Vai trò |
|------|---------|
| `SkillSelectionTypes.cs` | Enum context + `SkillSelectionChoice` |
| `SkillSelectionConfig.cs` | SO cấu hình reroll/skip/fallback |
| `SkillSelectionPoolBuilder.cs` | Weighted pool 3 nguồn |
| `SkillSelectionSynergy.cs` | Detect evolve synergy |
| `SkillCardInteraction.cs` | Hover + long-press tooltip |
| `Resources/SkillSelectionConfig.asset` | Config mặc định |

## 4. Files sửa

| File | Thay đổi |
|------|----------|
| `SkillSelectionUI.cs` | Refactor lớn — UI, animation, reroll/skip, card |
| `PassiveItemManager.cs` | `GetEligibleForSelection()` |
| `RunManager.cs` | `TrySpendRunCoins()` |
| `WeaponManager.cs` | `HasWeapon`, `GetWeaponCopies` |

---

## 5. Before / After (mô tả UI — chụp screenshot trong Editor)

### Trước
- Header cố định "CHỌN KỸ NĂNG"
- 3 thẻ đồng nhất, chạm = chọn ngay
- Reroll miễn phí 1 lần, không skip
- Level-up luôn 1+1+1 cố định

### Sau
- Header + gradient theo ngữ cảnh (level / rương / boss)
- Thẻ có badge loại, viền rarity, progress bar, synergy glow
- Reroll 50 xu (x3), Skip + confirm, double-tap / nút Xác nhận
- Pool random có trọng số từ 3 nguồn

**Screenshot đề xuất khi test:**
1. Level-up wave 3 — mix skill/passive/weapon
2. Rương boss wave 3 — title vàng + evolved weapon glow
3. Reroll disabled khi < 50 xu
4. Skip confirm popup

---

## 6. Test checklist

- [ ] Level-up: log pool khi `logPoolWeights=true`
- [ ] Rương wave: ~70% skill trong 20 mở
- [ ] Boss chest: passive + weapon evolved xuất hiện
- [ ] Reroll trừ 50 xu, disable khi thiếu xu
- [ ] Skip hồi 10% HP + đóng panel + next wave
- [ ] Double-tap / Confirm chọn thẻ
- [ ] Long-press hiện tooltip đầy đủ
- [ ] Synergy glow khi có Kiếm + Móng Vuốt evolve combo
- [ ] Level 2 đầu: chỉ skill

---

## 7. Designer workflow

1. Chỉnh reroll/skip: `Assets/Resources/SkillSelectionConfig.asset`
2. Bật log pool: `logPoolWeights = true`
3. Thêm skill/passive mới → tự vào pool nếu chưa max
4. Synergy evolve: gán `enablesWeaponEvolve` trên `PassiveItemData`
