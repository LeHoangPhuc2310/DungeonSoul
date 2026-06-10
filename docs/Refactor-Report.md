# BÁO CÁO TINH CHỈNH — DUNGEON SOUL

**Ngày:** 10/06/2026
**Phạm vi:** Đơn giản hóa hệ thống phần thưởng & xu, bỏ Meta Shop, dọn code thừa.

---

## 1. Mục tiêu

Trước khi tinh chỉnh, game có **2 hệ thống xu chồng chéo** và **3 nguồn phần thưởng skill** gây rối vòng chơi:

- Xu trong run (RunCoins) + Meta-xu vĩnh viễn (MetaCoins).
- Skill nhận từ: level-up, rương sau mỗi wave thường, rương boss.
- Meta Shop mở **giữa trận** (qua nút Pause) — phá nguyên tắc roguelike (nâng cấp vĩnh viễn phải làm giữa các run).

Mục tiêu: vòng chơi gọn, logic rõ, biên dịch sạch.

---

## 2. Quyết định thiết kế (đã chốt với chủ dự án)

| # | Quyết định |
|---|-----------|
| 1 | **Xóa hoàn toàn** hệ thống Meta — chỉ còn **1 loại xu = RunCoins** |
| 2 | **Bỏ nút Cửa Hàng** khỏi menu Pause — Pause chỉ còn: Tiếp tục / Chọn nhân vật / Về Menu |
| 3 | **Bỏ rương sau mỗi wave thường** — clear wave thường thì **tự sang wave kế** sau ~1.5s |
| 4 | **Chỉ boss** (wave 3/6/9/10) mới rơi rương skill |
| 5 | Skill nhận qua **2 nguồn**: level-up + rương boss |

---

## 3. Thay đổi đã thực hiện

### 3.1 Xóa file
- `Assets/Scripts/Meta/MetaShopManager.cs`
- `Assets/Scripts/Meta/MetaUpgradeData.cs`
- `Assets/Scripts/Meta/MetaRunModifiers.cs`
- `Assets/Scripts/Managers/MetaProgression.cs`
- `Assets/Scripts/UI/MetaShopUI.cs`
- `Assets/Scripts/Chest/ChestController.cs` (rương wave thường — code chết)
- Thư mục rỗng `Assets/Scripts/Meta/` đã được dọn.
- (Kèm tất cả file `.meta` tương ứng.)

### 3.2 Patch các file gỡ tham chiếu Meta
| File | Thay đổi |
|------|----------|
| `Combat/HealthSystem.cs` | Bỏ `MetaRunModifiers.ScaleCoins` — coin giữ giá trị gốc |
| `Managers/HeroRunStats.cs` | Bỏ `MetaProgression.ApplyToPlayer` |
| `Managers/RunManager.cs` | Bỏ Start() rỗng + bỏ `AddMetaCoins` khi EndRun |
| `Managers/AchievementManager.cs` | `Unlock()` chỉ còn log, bỏ phần thưởng meta-xu |
| `UI/GameOverUI.cs` | Bỏ dòng "Meta", chỉ hiện "Xu: N" |
| `Enemy/LootDrop.cs` | Bỏ bonus tỉ lệ rơi đồ từ Meta — dùng `dropChance` gốc |
| `Skills/SkillSelectionUI.cs` | Bỏ `ExtraForgeRerolls` — số reroll = cấu hình tĩnh |
| `Skills/SkillSelectionPoolBuilder.cs` | Bỏ điều chỉnh rarity theo Meta |
| `Managers/GameRunBootstrap.cs` | Bỏ Ensure 3 manager Meta + `ApplyAtRunStart` |
| `Managers/RoomManager.cs` | Bỏ heal-theo-meta + bỏ mở MetaShop ở phòng Shop; gỡ `HealPlayerFlat` không dùng |
| `UI/HUDManager.cs` | Bỏ spawn MetaShopUI + MetaShopManager |
| `Art/GameIconLibrary.cs` | Sửa comment (bỏ tên MetaShopUI) |

> Lưu ý: biến `metaText` trong `SkillTooltipUI.cs` là **thông tin rarity/độ hiếm của thẻ**, KHÔNG liên quan Meta-coin → giữ nguyên.

### 3.3 Pause Menu
`UI/PauseMenuUI.cs`: xóa nút "CỬA HÀNG" + 2 method `OpenMetaShop()` và `ReopenFromShop()`.

### 3.4 Vòng wave
`Enemy/EnemySpawner.cs`:
- Bỏ field `chest` + cờ `chestSpawned`, thay bằng cờ `waveAdvancing`.
- Thêm `waveBreakSeconds` (mặc định 1.5s) cấu hình trong Inspector.
- `Update()`: clear wave thường → nghỉ ngắn → tự gọi `BeginNextWave()`.
- Boss wave vẫn dùng `RedChestController` → `CompleteChestReward()` → `BeginNextWave()` như cũ.

---

## 4. Flow mới (sau tinh chỉnh)

```
Menu → Chọn nhân vật (20) → Chọn vũ khí (5) → Wave Arena
  ↓
Wave thường (1,2,4,5,7,8): spawn quái → clear → tự sang wave kế (~1.5s)
  ↓ (xen kẽ bất kỳ lúc nào)
Level-up → panel chọn 3 skill (reroll bằng RunCoins)
  ↓
Boss wave (3,6,9,10): boss → chết → RƯƠNG BOSS → chọn skill → wave kế
  ↓
Sau rương boss wave 10 → CHIẾN THẮNG
HP = 0 bất kỳ lúc nào → THẤT BẠI
  ↓
GameOverUI: hiện "Xu: N" (chỉ RunCoins, không còn Meta)
```

---

## 5. Dọn scene `SampleScene` (đã xong — 10/06/2026)

Đã chỉnh trực tiếp `Assets/Scenes/SampleScene.unity` (không cần MCP Unity):

- Xóa GameObject **`Chest`** (`ChestController` — script đã gỡ).
- Gỡ component **`MetaProgression`** khỏi **`GameManager`**.
- Cập nhật **`EnemySpawner`**: bỏ field `chest`, thêm `waveBreakSeconds: 1.5`.
- Không có `MetaShopUI` / `MetaShopManager` trong scene.

> PlayerPrefs cũ `ds_meta_coins` còn trên máy người chơi là vô hại — code không còn đọc nữa.

---

## 6. Kiểm thử (Play)

- [ ] Biên dịch sạch — Console không còn lỗi `Meta*` không tìm thấy.
- [ ] Esc/Pause: chỉ thấy **TIẾP TỤC / chọn nhân vật / VỀ MENU** (không còn CỬA HÀNG).
- [ ] Clear wave thường: **không hiện rương**, tự sang wave kế kèm thông báo "Đợt N".
- [ ] Level-up: hiện panel 3 skill; reroll trừ RunCoins.
- [ ] Boss wave: boss chết → **hiện rương boss** → chọn skill → wave kế.
- [ ] Wave 10 sau rương boss → màn thắng; GameOverUI hiện "Xu: N".

---

## 7. Hướng phát triển tiếp (gợi ý)

- **Passive item pool đang rỗng** — điền data để level-up ra passive.
- **5 skill placeholder** (Boomerang, LightningChain, PoisonCloud, BladeStorm, MirrorImage) logic còn yếu — hoàn thiện.
- Cân nhắc xóa nốt code dungeon BSP / `RoomManager` phòng đặc biệt nếu chắc chắn chỉ chơi Wave Arena.
