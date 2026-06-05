# Dungeon Soul — Tiến độ vs Kịch bản (docx)

Cập nhật: tích hợp **mobile**, **meta 11 upgrade**, **boss**, **BSP dungeon (tùy chọn)**, **wave arena (mặc định)**.

---

## Chạy nhanh trong Unity (1 lần)

Menu: **`DungeonSoul → Setup → Full Project Setup (Mobile + GDD)`**

Sau đó trong scene chính:

1. Chọn object **GameManagers** → component **Game Run Bootstrap**
   - **Wave Arena** — chơi như hiện tại (đợt 1–10, boss 3/6/9/10, thắng tầng 10).
   - **Procedural Dungeon** — cần gán **Dungeon Generator** + tilemap Floor/Wall.
2. Player tag `Player`: `PlayerController`, `HealthSystem`, `AutoAttack`, `WeaponManager`, `PlayerSkillHandler`, `PlayerWeaponVisual`.
3. **Enemy prefab** tag `Enemy`: `HealthSystem`, `EnemyAI`, `EnemyReward`, `EnemyPhysicsSetup`.
4. Play — joystick + safe area tự áp dụng.

---

## Đã làm trong code

| Hệ thống | Mô tả |
|----------|--------|
| **GameRunBootstrap** | Tạo manager, mobile 60 FPS, chọn Wave / Dungeon |
| **MetaRunModifiers** | 11 meta: HP, dmg, ASPD, move, starter skill, coin, regen phòng, độ hiếm skill, loot, forge reroll, weapon CD |
| **RoomClearBridge** | Diệt quái → `RoomController` clear; boss → EventBus |
| **EnemyAliveTracker** | Đếm quái (không `FindGameObjectsWithTag` mỗi frame) |
| **Boss** | 4 boss data, phase, HP bar, red chest, wave 3/6/9/10 |
| **Dungeon BSP** | `DungeonGenerator`, `DungeonRunController`, `RoomController`, minimap |
| **RoomManager** | 10 loại phòng; wave mode tắt auto-roll |
| **HUD** | EXP, xu/score, skill panel, weapon slots màu, victory |
| **Hero 3 class** | Warrior / Ranger / Mage + menu chọn |
| **Art** | Tiles enemy, chest `0089`, hero/weapon |
| **Font** | Times New Roman SDF (menu Fonts) |
| **Mobile** | Portrait wizard, safe area, multitouch, sleep off |

---

## Luồng chơi

### Wave Arena (khuyên dùng — đã test)

```
Spawn wave → diệt hết → rương → chọn skill → wave tiếp
Boss wave 3,6,9,10 → thắng boss → skill (trừ wave boss không rương thường)
Wave 10 + boss → Victory
```

### Procedural Dungeon (beta)

```
GameRunBootstrap → ProceduralDungeon → Generate map → Room triggers
Vào phòng → spawn quái → clear → chest / heal
```

---

## Meta shop (11 upgrade)

`MetaShopManager` + UI — mua bằng meta xu (lưu `PlayerPrefs` / `MetaProgression`).

Hiệu lực khi vào run: **MetaRunModifiers.ApplyAtRunStart()** (tự gọi từ bootstrap).

---

## Chưa đủ theo docx đầy đủ

| Tính năng | Trạng thái |
|-----------|------------|
| 8 skill behavior đặc biệt (boomerang, chain lightning…) | Phần lớn stat modifier |
| 12 vũ khí + 8 giáp + 8 nhẫn inventory | WeaponManager cơ bản; chưa inventory đầy đủ |
| Tutorial scene, daily, ads/IAP | Chưa |
| 10 tầng layout tilemap cố định | BSP có; layout GDD từng tầng chưa |
| NG+ / Ascension | Chưa |

---

## Build mobile

1. `DungeonSoul → Setup → Configure Mobile Player Settings`
2. **File → Build Settings** → Android hoặc iOS
3. Orientation: **Portrait**
4. Test trên máy: notch → **MobileSafeArea** chỉnh canvas HUD

---

## File tham chiếu kịch bản

Kịch bản gốc: `DungeonSoul-Kichban-DayDu.docx` (ngoài repo). Logic wave/boss/meta bám `PROGRESS` + code trong `Assets/Scripts/`.
