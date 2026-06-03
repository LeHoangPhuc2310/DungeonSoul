# Dungeon Soul — Tiến độ code vs Kịch bản

Cập nhật sau phiên code gần nhất. Dùng file này để biết **đã xong phần nào** và **cần gắn gì trong Unity Editor**.

---

## Đã làm trong code (session này)

| Hệ thống | File mới / sửa | Trạng thái |
|----------|----------------|------------|
| **28 Skill (runtime)** | `PlayerSkillHandler`, `PlayerSkillStats`, `SkillBehaviors` | Stack 1–3 + legendary tick (Ghost, TimeFreeze, DragonStrike, IceAura, Vampire, SoulHarvest, Explosion on kill) |
| **Crit / Pierce / DOT** | `Projectile`, `BurnDebuff` | Crit, pierce falloff, fire DOT |
| **Multi-target bắn** | `AutoAttack` | `MultiTargetCount` |
| **Loot xu** | `LootDrop`, `CoinPickup`, `EnemyReward` | Drop coin + score khi enemy chết |
| **Meta xu (lưu máy)** | `MetaProgression` | PlayerPrefs: meta coins, Vital/Power upgrade |
| **Kết thúc run** | `RunManager` | Chuyển xu run → meta khi chết/thắng boss tầng 10 |
| **Phòng dungeon** | `RoomManager`, `RoomType` | Roll loại phòng, healing/treasure hook |
| **Boss phase** | `BossController` | 3 phase theo % HP |
| **Rương theo độ hiếm** | `SkillSelectionUI.ShowChest` | Weight theo Normal / Elite / Treasure |
| **Enemy** | `EnemyAI` (MoveSpeed/ContactDamage public) | Cho boss phase |

---

## Đã có từ trước (giữ nguyên)

- Di chuyển player, auto attack, projectile pool  
- EXP / level up, `ExpBarUI`, `LevelUpEffect`  
- `WeaponManager` + evolution (12 weapon types)  
- `PassiveItemManager`  
- `EnemyWaveManager` + `SurvivalTimer` (chế độ survival horde)  
- `EnemySpawner` + `ChestController` (chế độ clear phòng)  
- `HUDManager` (HP, wave, timer, DPS, combo, slots)  

---

## Chưa làm / cần bạn gắn trong Unity

### Bắt buộc để chạy scene

1. **Player** (tag `Player`):
   - `PlayerController`, `HealthSystem`, `AutoAttack`, `WeaponManager`, `ExpSystem`
   - `PlayerSkillHandler`, `PlayerSkillStats`, `SkillBehaviors` (tự add nếu thiếu)
   - `Rigidbody2D` + collider

2. **Managers** (empty GameObject `GameManagers`):
   - `RunManager`
   - `MetaProgression` (DontDestroyOnLoad)
   - `HUDManager` (+ canvas con nếu chưa có)
   - `SkillSelectionUI` + canvas 3 nút
   - `ExpSystem`, `FloorManager` (tùy mode)
   - **Chọn 1 mode spawn:**
     - Survival: `EnemyWaveManager` + `SurvivalTimer`
     - Room: `EnemySpawner` + `RoomManager` + tilemap

3. **Enemy prefab**:
   - Tag `Enemy`
   - `HealthSystem`, `EnemyAI`, `EnemyReward`, `LootDrop` (tùy chọn)
   - Boss: thêm `BossController`, tick **Is Boss** trên `EnemyReward`

4. **Skill assets**:
   - `Assets/Resources/SkillData/*.asset` (28 skill) — chạy menu Editor `CreateAllSkills` nếu thiếu

5. **Chest**:
   - `ChestController` + trigger; gọi `Show()` hoặc `ShowChest(RoomType.Elite)`

### Chưa có code (theo kịch bản docx)

| Tính năng kịch bản | Ghi chú |
|--------------------|---------|
| Main Menu / Tutorial | Chưa có scene UI |
| 12 vũ khí đủ passive/special | Chỉ enum + `WeaponManager` cơ bản |
| 8 giáp + 8 nhẫn equip | Chưa inventory |
| 8 enemy + 4 elite khác biệt AI | 1 prefab chung + AI chase |
| 10 tầng × 10 loại phòng layout | `RoomManager` chỉ logic, chưa gen map |
| Meta Shop UI | `MetaProgression` API only |
| Daily Challenge, Ads, IAP | Chưa |
| Game Over / Victory screen đẹp | Chỉ text HUD + log |
| NG+ / Ascension | Chưa |

---

## Gợi ý thứ tự làm tiếp trong Unity

1. Gắn components bảng trên → Play → kill enemy → thấy **Coins/Score** tăng.  
2. Mở rương / level up → chọn skill → kiểm tra stack (chọn cùng skill 2–3 lần).  
3. Thêm `MetaProgression` + `RunManager` → chết → xu meta lưu (`PlayerPrefs`).  
4. Boss prefab + `BossController` → HP bar / announcement phase.  
5. Tắt `EnemyWaveManager` nếu muốn pure dungeon: bật `RoomManager` + `EnemySpawner`.  

---

## Skill chưa có hiệu ứng riêng (chỉ stack stat / TODO)

`Boomerang`, `LightningChain`, `PoisonCloud`, `ExplosiveRounds`, `BladeStorm`, `TwinArrows`, `DeathMark`, `MirrorImage` — cần thêm VFX/projectile behavior sau.

---

## File script mới

```
Assets/Scripts/Skills/PlayerSkillStats.cs
Assets/Scripts/Skills/SkillBehaviors.cs
Assets/Scripts/Enemy/EnemyReward.cs
Assets/Scripts/Enemy/BossController.cs
Assets/Scripts/Managers/MetaProgression.cs
Assets/Scripts/Managers/RunManager.cs
Assets/Scripts/Managers/RoomManager.cs
```

---

*Kịch bản gốc: `Downloads/DungeonSoul-Kichban-DayDu.docx`*
