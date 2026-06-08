# DungeonSoul — Luồng xử lý codebase & Hướng dẫn mở rộng

> Tài liệu này mô tả **luồng logic thực tế** của game (đối chiếu trực tiếp với code trong `Assets/Scripts`), cùng các **quy tắc khi mở rộng** để tránh lặp lại các bug đã gặp. Cập nhật khi đổi kiến trúc.

---

## 1. Tổng quan kiến trúc

DungeonSoul là **2D survival roguelike** (Unity, C#). Đặc điểm:

- **Singleton khắp nơi**: hầu hết manager là `public static T Instance` (không có DI framework).
- **ScriptableObject** cho dữ liệu: `SkillData`, `PassiveItemData`.
- **Object pooling** cho đạn (`AutoAttack`).
- **Event-driven** một phần: `ExpSystem.OnExpChanged`, `ExpSystem.OnLevelUpEvent`.
- **UI tự dựng runtime**: nhiều panel (skill select, tooltip, HUD) được build bằng code thay vì prefab, để không phụ thuộc wiring trong Inspector.

### Quy ước Singleton chuẩn trong dự án
```csharp
public static T Instance { get; private set; }
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    // ... khởi tạo
}
private void OnDestroy() { if (Instance == this) Instance = null; }
```
Nhiều hệ thống còn có `GetOrFind()` / `GetOrCreate()` để tự tạo instance runtime nếu scene thiếu (vd. `SkillSelectionUI`, `SkillTooltipUI`, `HUDManager.Resolve()`).

---

## 2. Luồng màn hình (Scene Flow)

```
MainMenu (0) → CharacterSelectScene (1) → WeaponSelectScene (2) → SampleScene (3, gameplay)
```

- `MainMenuManager`: CHƠI / CÀI ĐẶT / THOÁT.
- `CharacterSelectUI.OnConfirm` → lưu hero qua `HeroRunStats.SetHero` (PlayerPrefs) → WeaponSelectScene.
- `WeaponSelectUI` → lưu vũ khí khởi đầu qua `RunLoadout.StartingWeapon` (PlayerPrefs) → SampleScene.
- SampleScene tự đọc lựa chọn: `HeroRunStats` đọc PlayerPrefs ở `Awake` → `Start → ApplyToPlayer`; `WeaponManager.Start` dùng `RunLoadout.StartingWeapon`.

Dữ liệu lựa chọn truyền giữa scene **qua PlayerPrefs** (`RunLoadout`, `GameSettings`, `HeroRunStats` đều là static + PlayerPrefs), KHÔNG qua DontDestroyOnLoad object.

Setup build order: menu **DungeonSoul → Setup → Tạo Đầy Đủ Flow Màn Hình**.

---

## 3. Vòng lặp gameplay chính (Core Loop)

```
Spawn wave quái ──► Player auto-attack tiêu diệt ──► Quái chết: rớt EXP/coin/loot
       ▲                                                      │
       │                                                      ▼
       │                                          EnemyAliveTracker.Count == 0
       │                                                      │
       │                                                      ▼
       │                                   Chest xuất hiện (hoặc Boss ở wave boss)
       │                                                      │
       │                              Player chạm chest ──► SkillSelectionUI mở
       │                                                      │
       │                                   Chọn skill/weapon/passive reward
       │                                                      │
       └──────────────── BeginNextWave() ◄───────────────────┘
                         (wave++ , tối đa 10 tầng)
```

### 3.1 Spawn & đếm quái — `EnemySpawner` + `EnemyAliveTracker`

- `EnemySpawner.Start` → `SpawnInitialEnemies()` (wave 1).
- Số quái mỗi wave: `min/maxEnemies` + bonus theo wave (`extraEnemiesPerTwoWaves`), clamp `maxEnemiesPerWave`.
- **`EnemyAliveTracker`** (static): mỗi lần spawn gọi `Add(1)`, mỗi lần quái chết (`HealthSystem.Die`) gọi `Add(-1)`. Đây là **nguồn chân lý** cho "đã hết quái chưa" — KHÔNG dùng `FindGameObjectsWithTag` mỗi frame (lý do hiệu năng mobile).
- `EnemySpawner.Update`: nếu `initialSpawnDone && !chestSpawned && EnemyAliveTracker.Count <= 0 && !bossWave` → bật `chest`, set `chestSpawned = true`, log `"All enemies defeated! Chest appeared."`.
- Wave boss (`BossSpawnManager.IsBossWave(waveIndex)`) **không spawn chest tự động** — boss tự xử lý reward khi chết.

### 3.2 Combat — `AutoAttack` + `HealthSystem`

- `AutoAttack.Update`: cooldown bắn → tìm địch gần nhất trong tầm (`FindNearestEnemy` ranged / `FindNearestEnemyInFacingArc` melee) → `PerformAttack`.
- Đạn lấy từ **pool** (`GetPooledProjectile`), trả về pool bằng `SetActive(false)`.
- `HealthSystem.TakeDamage`: trừ giáp (flat + percent từ skill/passive) → trừ HP → nếu ≤ 0 gọi `Die()`.

### 3.3 Quái chết — `HealthSystem.Die()` (nhánh `IsEnemyUnit`)

Thứ tự xử lý khi 1 quái chết (quan trọng cho mở rộng reward):
1. Cộng EXP: `ExpSystem.AddExp(killExp)` + spawn ExpGem.
2. Tính score + coin (`EnemyReward`, scale qua `MetaRunModifiers`, bonus `PlayerSkillStats.CoinDropBonus`).
3. `HUDManager.RegisterEnemyKilled(score, coins)`.
4. `LootDrop.TryDrop`, `SkillBehaviors.OnEnemyKilled`, `BossController.OnBossDefeated` (nếu boss).
5. **`EnemyAliveTracker.Add(-1)`** ← bước này quyết định chest có xuất hiện không.
6. Phát animation chết rồi `Destroy` (có delay nếu có death anim).

### 3.4 Player chết — `HealthSystem.Die()` (nhánh `isPlayer`)

- Nếu có revive passive (`PassiveItemManager.TryConsumeRevive`) → hồi 50% HP, không chết.
- Ngược lại: `RunManager.EndRun(false)` → game over.

---

## 4. EXP & Lên cấp — `ExpSystem`

- `AddExp(amount)`: nhân `PassiveItemManager.ExpGainMultiplier` → cộng → `ProcessLevelUps()`.
- `ProcessLevelUps`: vòng `while (currentExp >= ExpToNextLevel)` → mỗi lần `ApplyLevelUp()`.
- `ApplyLevelUp`: tăng MaxHP/damage (theo `HeroType`), `RefreshStats`, VFX, **rồi mở panel chọn skill**:
  ```csharp
  SkillSelectionUI skillUi = SkillSelectionUI.GetOrFind();
  if (skillUi != null && !skillUi.IsPanelOpen)   // guard: tránh mở chồng panel
      skillUi.Show();
  ```
- `ExpToNextLevel = 260 * level^1.62` (`CalculateExpToNextLevel`).
- Event: `OnExpChanged(currentExp, expToNext)`, `OnLevelUpEvent(level)`.

> **Lưu ý mở rộng:** lên cấp và mở rương đều dùng `SkillSelectionUI`. Luôn check `IsPanelOpen` trước khi `Show()`/`ShowChest()` để không mở chồng (nếu lên cấp ngay lúc đang chọn reward rương).

---

## 5. Hệ thống chọn phần thưởng — `SkillSelectionUI`

Đây là hệ thống phức tạp nhất, đã từng có nhiều bug. **Đọc kỹ trước khi sửa.**

### 5.1 Kiến trúc Controller / View (BẮT BUỘC tuân thủ)

- **Controller** = `SkillSelectionUI` MonoBehaviour, phải sống trên GameObject **luôn active**.
- **View** = `Canvas` con (GameObject riêng tên `SkillCanvas`), được `SetActive(true/false)` để show/hide.
- KHÔNG bao giờ gộp Controller + Canvas trên một GameObject rồi để inactive — sẽ khiến `Awake` chạy muộn và phá trạng thái panel.
- `EnsureCanvasReference()` → `SeparateCanvasToChild()`: nếu phát hiện Canvas nằm chung GameObject với controller, nó tự **tách Canvas xuống child mới** và **DESTROY** (không chỉ disable) Canvas/Raycaster/Scaler còn sót trên controller — vì nếu để lại, child canvas bị coi là *nested canvas* và không render.

**Quy tắc vàng:**
- Chỉ `skillCanvas.gameObject.SetActive(false)` để ẩn — TUYỆT ĐỐI không `gameObject.SetActive(false)` lên controller.
- Trong scene, GameObject controller (`SkillSelectionController`) phải `m_IsActive: 1`.

### 5.2 Vòng đời mở panel — `OpenPanel()`

```
Show() / ShowChest(roomType)        ← entry point
  └─ OpenPanel()
       ├─ EnsureCanvasReference()    (tách canvas nếu cần, đảm bảo controller active)
       ├─ BuildWeightedChoices()     (dựng 3 lựa chọn theo context + level)
       ├─ panelOpenGeneration++      ← token chống race giữa các lần mở
       ├─ isOpen = true
       ├─ Time.timeScale = 0         (pause game)
       ├─ skillCanvas.SetActive(true)
       ├─ Bind card + layout + style
       └─ UnlockChoiceInputAfterDelay()  ← chặn click "rơi" trong 0.55s đầu
```

- **`panelOpenGeneration`**: mọi coroutine đóng/mở so sánh generation; nếu khác → bỏ qua (panel đã mở lại). Khi thêm coroutine mới liên quan panel, **luôn capture generation đầu coroutine và check lại sau mỗi `yield`**.
- **`InputUnlockDelay` (0.55s)**: chặn input ngay khi panel vừa mở (tránh cú chạm mở rương/level-up "rơi" trúng thẻ). `acceptingChoices` chỉ bật sau delay + sau khi thả chuột/chạm.
- **`Awake` guard**: `if (!isOpen) Hide();` — phòng trường hợp Awake chạy muộn ngay sau khi OpenPanel đã mở panel.

### 5.3 Chọn reward — `ConfirmChoice → ApplyChoiceReward`

`ApplyChoiceReward` phân nhánh theo `SkillSelectionChoiceKind`:
| Kind | Hành động |
|------|-----------|
| `SkillUpgrade` | `PlayerSkillHandler.ApplySkill(skill)` |
| `WeaponPickup` | `WeaponManager.AddOrUpgradeWeapon(type)` |
| `PassiveItem` | `PassiveItemManager.TryApplyPassive` (nếu đầy slot → mở `PassiveSwapUI`, **hoãn đóng panel**) |
| `BonusHp` / `HealFallback` | hồi máu |
| `BonusCoin` | `RunManager.AddRunCoins` |

- Trả về `true` (deferClose) khi cần hoãn đóng panel (vd. popup swap passive). Khi đó panel đóng sau khi swap xong qua `CompleteChestAfterSwap`.
- Sau khi chọn (không hoãn): `CloseAfterSelection(fromChest)` → animate đóng → `HideImmediate("selection")` → nếu mở từ rương thì `CompleteChestReward()`.

### 5.4 Hoàn tất rương → wave kế — `CompleteChestReward()`

- Nếu `CurrentWave >= 10` → `RunManager.EndRun(true)` (thắng game).
- Ngược lại: `PassiveItemManager.CheckPassiveEvolutionsAtWaveEnd()` → `EnemySpawner.BeginNextWave()` → HUD báo wave mới.

### 5.5 Tooltip thẻ — `SkillCardInteraction` + `SkillTooltipUI`

- Long-press (≥ 0.55s) một thẻ → `ShowFullTooltip()` → `SkillTooltipUI` hiện chi tiết.
- `SkillTooltipUI.Update` bám theo con trỏ chuột khi `visible`.
- **Phải ẩn tooltip** ở: `OnPointerExit`, `OnPointerUp` (rời/thả thẻ), và trong `SkillSelectionUI.HideImmediate` / `HidePanelVisualOnly` (đóng panel). Nếu quên → tooltip treo dính theo chuột (bug đã gặp).

---

## 6. Hệ thống chỉ số nhân vật

```
HeroRunStats (base theo hero đã chọn)
   + ExpSystem.ApplyLevelUp  (HP/damage mỗi cấp)
   + PlayerSkillHandler.RefreshStats  (tổng hợp skill đã stack)
   + PassiveItemManager  (modifier từ passive item)
   = chỉ số combat thực tế (PlayerSkillStats: crit, pierce, lifesteal, coinDropBonus...)
```

- `PlayerSkillHandler.ApplySkill` → tăng stack skill → `RefreshStats` tính lại toàn bộ.
- `PlayerSkillStats` giữ các stat dẫn xuất (crit, pierce, lifesteal, CoinDropBonus...).
- `SkillBehaviors` xử lý hiệu ứng legendary tick-based (GhostForm, TimeFreeze, DragonStrike, IceAura) và hook `OnEnemyKilled`.
- `WeaponManager`: tối đa nhiều slot, tiến hóa vũ khí khi đủ bản sao.

> **Lưu ý mở rộng:** khi thêm stat mới, nhớ reset trong `HealthSystem.ResetSkillModifiers` (skill) và phân biệt rõ modifier từ **skill** (reset mỗi lần recalc) vs từ **passive** (`SetPassiveDamageReduction` — không reset theo skill).

---

## 7. Run & Meta-progression

- **`RunManager`** (trong 1 lần chơi): `runCoins`, `runScore`, `runActive`.
  - `EndRun(victory)`: chỉ chạy 1 lần (`firstEnd` guard) → `MetaProgression.AddMetaCoins(runCoins)` → HUD hiện kết quả.
  - `ResetForNewRun()` khi chơi lại.
- **`MetaProgression`**: coin tích lũy giữa các run (meta shop). `ApplyToPlayer` ở `RunManager.Start`.
- **`MetaRunModifiers`**: buff vĩnh viễn từ meta shop (scale coin, extra reroll, ...).

---

## 8. HUD & UI — `HUDManager`

- `HUDManager.Resolve()`: lấy/tạo instance (nhiều nơi gọi để cập nhật HP/EXP/score).
- `EnsureGameplayUiSystems`: tạo runtime các hệ thống gameplay UI còn thiếu (kể cả `HeroRunStats`).
- Nhiều method là **no-op stub** cho UI chưa làm — kiểm tra trước khi giả định một method đã có hành vi.
- Damage/EXP number: `HUDManager.SpawnDamageNumber`, `SpawnExpNumber` (static).

---

## 9. Quy tắc khi MỞ RỘNG (checklist tránh bug)

### Khi thêm hệ thống UI mới (panel, popup)
- [ ] Tách **Controller (luôn active)** khỏi **View Canvas (toggle)**. Không để controller trên GameObject inactive.
- [ ] Cung cấp `GetOrFind()`/`GetOrCreate()` nếu hệ thống có thể bị gọi trước khi scene khởi tạo xong.
- [ ] Mọi coroutine đóng/mở dùng **generation token** + check sau mỗi `yield` (xem `panelOpenGeneration`).
- [ ] Pause game bằng `Time.timeScale = 0` thì animation UI phải dùng `Time.unscaledDeltaTime` / `WaitForSecondsRealtime`.
- [ ] Khi đóng panel, **ẩn mọi tooltip/popup phụ** (tránh treo dính chuột).
- [ ] Chặn input "rơi" frame đầu khi panel mở từ một cú chạm (xem `UnlockChoiceInputAfterDelay`).

### Khi thêm loại phần thưởng / chest mới
- [ ] Thêm `SkillSelectionChoiceKind` mới → xử lý trong `ApplyChoiceToCard` (hiển thị) **và** `ApplyChoiceReward` (áp dụng).
- [ ] Nếu reward có thể mở popup phụ → trả `true` (deferClose) và tự đóng panel sau.
- [ ] Tôn trọng guard `IsPanelOpen` ở mọi entry point mở panel.

### Khi đụng vào spawn / wave / điều kiện "hết quái"
- [ ] Luôn cập nhật `EnemyAliveTracker` (Add ±1) song song với spawn/Destroy. Quên `Add(-1)` ⇒ chest không bao giờ xuất hiện.
- [ ] Reset tracker đầu mỗi wave (`EnemyAliveTracker.Reset(0)` trong `SpawnWaveEnemies`).
- [ ] Wave boss xử lý reward riêng — không dựa vào chest tự động.

### Khi thêm stat / modifier
- [ ] Phân biệt modifier **skill** (reset khi `RefreshStats`) vs **passive** (bền). Reset đúng chỗ trong `HealthSystem` / `PlayerSkillStats`.

### Khi thêm singleton manager
- [ ] Theo đúng mẫu Awake-singleton + `OnDestroy` clear `Instance`.
- [ ] Nếu cần tồn tại qua scene → `DontDestroyOnLoad`, nhưng cẩn thận trùng instance khi quay lại scene cũ.

---

## 10. Bản đồ file nhanh

| Vùng | File chính |
|------|-----------|
| Spawn/wave | `Enemy/EnemySpawner.cs`, `Enemy/EnemyAliveTracker.cs`, `Enemy/BossSpawnManager.cs` |
| Combat | `Player/AutoAttack.cs`, `Combat/HealthSystem.cs`, `Combat/Projectile.cs`, `Enemy/EnemyAI.cs` |
| EXP/Level | `Player/ExpSystem.cs` |
| Reward UI | `Skills/SkillSelectionUI.cs`, `UI/SkillCardInteraction.cs`, `UI/SkillTooltipUI.cs`, `UI/PassiveSwapUI.cs` |
| Skill/stat | `Skills/PlayerSkillHandler.cs`, `Skills/PlayerSkillStats.cs`, `Skills/SkillBehaviors.cs` |
| Vũ khí/passive | `Player/WeaponManager.cs`, `Items/PassiveItemManager.cs` |
| Chest | `Chest/ChestController.cs`, `Chest/RedChestController.cs` |
| Run/Meta | `Managers/RunManager.cs`, `Managers/MetaProgression.cs`, `Managers/MetaRunModifiers.cs` |
| HUD | `UI/HUDManager.cs` |
| Scene flow | `UI/MainMenuManager.cs`, `UI/CharacterSelectUI.cs`, `UI/WeaponSelectUI.cs`, `Managers/RunLoadout.cs`, `Managers/HeroRunStats.cs` |

---

*Đối chiếu code thực tế tại thời điểm viết. Nếu sửa luồng, cập nhật lại mục tương ứng để tài liệu không lệch.*
