# DungeonSoul — Sơ đồ luồng Gameplay

Tài liệu mô tả luồng chơi từ lúc khởi động đến thắng/thua, dựa trên code trong `Assets/Scripts/`.

## Scene & điểm vào

| Build Index | Scene | Vai trò |
|-------------|-------|---------|
| 0 | `CharacterSelectScene` | Scene khởi động mặc định |
| 1 | `SampleScene` | Gameplay chính (Wave Arena / Dungeon) |

---

## Sơ đồ tổng quan — Start → Win / Lose

```mermaid
flowchart TD
    START([Unity Play / Khởi động game]) --> CS[CharacterSelectScene]

    CS --> CS_UI[CharacterSelectUI: lưới 20 nhân vật]
    CS_UI --> CS_PICK[Chọn nhân vật]
    CS_PICK --> CS_CONFIRM{Bấm Chọn?}
    CS_CONFIRM -->|Không| CS_PICK
    CS_CONFIRM -->|Có| CS_SAVE[HeroRunStats.SetCharacter<br/>RunEntryGate.ConfirmCharacterSelect]
    CS_SAVE --> LOAD_SS[LoadScene SampleScene]

    LOAD_SS --> RESET[SceneRunReset: timeScale=1<br/>RunManager.ResetForNewRun<br/>HUDManager.ResetForNewRun]

    RESET --> BOOT{GameRunBootstrap<br/>có trong scene?}
    BOOT -->|Không| WAVE_DEFAULT[Wave Arena mặc định<br/>HeroRunStats + RunManager.Start]
    BOOT -->|Có, chưa confirm| CS
    BOOT -->|Có, đã confirm| BOOT_INIT[EnsureManagers + MetaRunModifiers<br/>ApplyAtRunStart]

    BOOT_INIT --> MODE{GameRunMode?}
    MODE -->|WaveArena| WAVE_MODE[RoomManager.SetWaveMode true]
    MODE -->|ProceduralDungeon| DUNGEON_MODE[DungeonRunController.BeginDungeonRun<br/>BSP map + room triggers]

    WAVE_DEFAULT --> INIT_PLAYER
    WAVE_MODE --> INIT_PLAYER
    DUNGEON_MODE --> INIT_PLAYER

    INIT_PLAYER[Khởi tạo Player<br/>HeroRunStats.ApplyToPlayer<br/>HUD + Achievement.OnRunStarted<br/>CameraFollow + VirtualJoystick]

    INIT_PLAYER --> SPAWN[EnemySpawner: Wave 1<br/>spawn quái + PlayCombatMusic]

    SPAWN --> CORE_LOOP

    subgraph CORE_LOOP["Vòng lặp gameplay chính"]
        COMBAT[Combat realtime<br/>PlayerController di chuyển<br/>AutoAttack + WeaponManager<br/>vs EnemyAI contact damage]

        COMBAT --> KILL{Quái chết?}
        KILL -->|Có| REWARD[+EXP gem + Score + Coins<br/>LootDrop coin<br/>EnemyAliveTracker -1]
        REWARD --> COMBAT

        REWARD --> LVL{EXP đủ level up?}
        LVL -->|Có| SKILL_UP[SkillSelectionUI.Show<br/>timeScale=0, invuln<br/>chọn Skill/Weapon/Passive]
        SKILL_UP --> COMBAT

        COMBAT --> PLAYER_DEAD{Player HP ≤ 0?}
        PLAYER_DEAD -->|Có| LOSE

        COMBAT --> WAVE_CLEAR{Tất cả quái chết?<br/>không phải boss wave}
        WAVE_CLEAR -->|Có| CHEST_SHOW[Rương thường xuất hiện<br/>ChestController]
        CHEST_SHOW --> CHEST_TOUCH[Player chạm rương]
        CHEST_TOUCH --> CHEST_SKILL[SkillSelectionUI.ShowChest<br/>chọn 1 phần thưởng]
        CHEST_SKILL --> CHEST_DONE[CompleteChestReward]

        COMBAT --> BOSS_WAVE{Wave = 3/6/9/10?}
        BOSS_WAVE -->|Có| BOSS_FIGHT[BossSpawnManager.SpawnForWave<br/>Goblin King / Stone Golem<br/>Shadow Witch / Dragon Lord]
        BOSS_FIGHT --> BOSS_PHASE[BossController: phases + abilities<br/>BossHPBarUI]
        BOSS_PHASE --> BOSS_DEAD{Boss HP = 0?}
        BOSS_DEAD -->|Có| RED_CHEST[RedChestController spawn<br/>EventBus.OnBossDefeated<br/>+ coin/score thưởng]
        RED_CHEST --> RED_TOUCH[Player chạm rương đỏ]
        RED_TOUCH --> RED_SKILL[SkillSelectionUI.ShowChest treasure]
        RED_SKILL --> CHEST_DONE

        BOSS_DEAD -->|Floor ≥ 10| WIN_DIRECT[RunManager.OnBossDefeated<br/>→ EndRun true]

        CHEST_DONE --> WAVE_CHECK{CurrentWave ≥ 10?}
        WAVE_CHECK -->|Không| NEXT_WAVE[EnemySpawner.BeginNextWave<br/>waveIndex++]
        NEXT_WAVE --> COMBAT
        WAVE_CHECK -->|Có| WIN_CHEST[RunManager.EndRun true]
    end

    LOSE([THẤT BẠI — GAME OVER])
    WIN_DIRECT --> WIN
    WIN_CHEST --> WIN
    WIN([CHIẾN THẮNG — VICTORY])

    LOSE --> END_RUN_LOSE[RunManager.EndRun false<br/>runActive=false]
    WIN --> END_RUN_WIN[RunManager.EndRun true<br/>runActive=false]

    END_RUN_LOSE --> META_LOSE[MetaProgression.AddMetaCoins runCoins<br/>+10% bonus qua GameOverUI]
    END_RUN_WIN --> META_WIN[MetaProgression.AddMetaCoins runCoins<br/>Achievement OnRunEnded true]

    META_LOSE --> RESULT_UI
    META_WIN --> RESULT_UI

    RESULT_UI[GameOverUI / HUDManager<br/>timeScale=0<br/>THẤT BẠI hoặc CHIẾN THẮNG]

    RESULT_UI --> END_CHOICE{Người chơi chọn?}
    END_CHOICE -->|Chơi lại| LOAD_SS
    END_CHOICE -->|Thoát / Về menu| CS
    END_CHOICE -->|Pause → Về menu| CS
```

---

## Luồng khởi tạo chi tiết

```mermaid
flowchart LR
    subgraph ENTRY["Điểm vào"]
        A1([App Start]) --> A2[CharacterSelectScene]
        A3[MainMenu PLAY<br/>nếu có scene] --> A2
    end

    subgraph CONFIRM["Xác nhận nhân vật"]
        A2 --> B1[Chọn 1/20 hero<br/>Warrior / Ranger / Mage]
        B1 --> B2[OnConfirm]
        B2 --> B3[RunEntryGate = true]
        B3 --> B4[Load SampleScene]
    end

    subgraph SAMPLE["SampleScene load"]
        B4 --> C1[SceneRunReset]
        C1 --> C2[FloorManager + MetaProgression<br/>RunManager + GameManager]
        C2 --> C3{Bootstrap?}
        C3 -->|Bypass gate| A2
        C3 -->|OK| C4[MetaRunModifiers.ApplyAtRunStart]
        C4 --> C5[WaveArena hoặc ProceduralDungeon]
        C5 --> C6[Apply hero stats lên Player]
        C6 --> C7[EnemySpawner Wave 1]
    end
```

---

## Vòng combat & tiến trình Wave

```mermaid
flowchart TD
    subgraph COMBAT_SYS["Hệ thống combat"]
        P1[PlayerController<br/>keyboard + VirtualJoystick] --> P2[AutoAttack: melee/projectile]
        P2 --> P3[WeaponManager: tối đa 6 slot auto-fire]
        P3 --> DMG[HealthSystem.TakeDamage]
        E1[EnemyAI: chase player] --> E2[contact damage → Player HP]
        E2 --> DMG
        DMG --> DIE{HP ≤ 0?}
    end

    subgraph ENEMY_DIE["Khi quái chết"]
        DIE -->|Enemy| ED1[ExpGem spawn]
        ED1 --> ED2[RunManager score/coins]
        ED2 --> ED3[BossController.OnBossDefeated nếu boss]
        ED3 --> ED4[EnemyAliveTracker.Add -1]
    end

    subgraph WAVE_PROG["Tiến trình Wave 1→10"]
        W1[Wave 1: 8–13+ quái] --> W2[Clear → Rương thường]
        W2 --> W3[Chọn skill → BeginNextWave]
        W3 --> W4{Wave index}
        W4 -->|1,2,4,5,7,8| W1
        W4 -->|3| B1[BOSS: Goblin King]
        W4 -->|6| B2[BOSS: Stone Golem]
        W4 -->|9| B3[BOSS: Shadow Witch]
        W4 -->|10| B4[BOSS: Dragon Lord]
        B1 & B2 & B3 & B4 --> BR[Red chest → skill → next wave hoặc WIN]
    end

    DIE -->|Player| GO[EndRun false → GAME OVER]
    BR -->|Wave 10 cleared| WIN([VICTORY])
    B4 -->|Boss defeated + Floor≥10| WIN
```

---

## Luồng Level Up (song song combat)

```mermaid
flowchart LR
    EXP[Enemy die → ExpGem pickup] --> ADD[ExpSystem.AddExp]
    ADD --> CHECK{exp ≥ 260×level^1.62?}
    CHECK -->|Không| EXP
    CHECK -->|Có| UP[ApplyLevelUp<br/>+HP +dmg permanent<br/>LevelUpEffect + Audio]
    UP --> PANEL[SkillSelectionUI.Show<br/>pause game]
    PANEL --> PICK[Chọn 1 trong 3:<br/>Skill / Weapon / Passive]
    PICK --> RESUME[Hide panel → tiếp combat]
    RESUME --> EXP
```

---

## Điều kiện Thắng / Thua

```mermaid
flowchart TD
    subgraph WIN_COND["Điều kiện THẮNG"]
        V1[Hạ Dragon Lord wave 10<br/>RunManager.OnBossDefeated<br/>FloorManager.CurrentFloor ≥ 10]
        V2[Mở rương sau wave 10<br/>CompleteChestReward<br/>CurrentWave ≥ 10]
        V1 --> VICTORY
        V2 --> VICTORY
        VICTORY([CHIẾN THẮNG<br/>Hoàn thành 10 tầng!])
    end

    subgraph LOSE_COND["Điều kiện THUA"]
        L1[HealthSystem.Die player<br/>HP ≤ 0]
        L2[HUDManager.Update backup<br/>HP ≤ 0 && !runEnded]
        L1 --> GAMEOVER
        L2 --> GAMEOVER
        GAMEOVER([THẤT BẠI<br/>Hiện floor hiện tại])
    end

    subgraph POST["Sau kết thúc run"]
        VICTORY --> P1[timeScale = 0]
        GAMEOVER --> P1
        P1 --> P2[Lưu run coins → meta coins]
        P2 --> P3[AchievementManager.OnRunEnded]
        P3 --> P4[GameOverUI: Chơi lại / Thoát]
    end
```

---

## Luồng Dungeon Mode (ProceduralDungeon — tùy chọn)

Kích hoạt khi `GameRunBootstrap.runMode = ProceduralDungeon`.

```mermaid
flowchart TD
    D_START[BeginDungeonRun] --> D_GEN[DungeonGenerator: BSP 64×48<br/>8–12 phòng]
    D_GEN --> D_TP[Teleport player → spawn cell]
    D_TP --> D_TRIG[Spawn RoomController triggers]
    D_TRIG --> D_WAVE[EnemySpawner.BeginNextWave]
    D_WAVE --> D_EXPLORE[Player khám phá map]

    D_EXPLORE --> D_ENTER{Vào phòng Locked?}
    D_ENTER -->|Có| D_ACTIVE[ActivateRoom<br/>Locked → Active<br/>spawn enemies]
    D_ACTIVE --> D_FIGHT[Combat trong phòng]
    D_FIGHT --> D_CLEAR{Tất cả quái chết?}
    D_CLEAR -->|Có| D_CLEARED[ClearRoom<br/>Active → Cleared<br/>chest/heal theo RoomType]
    D_CLEARED --> D_EVT[EventBus.OnRoomCleared]
    D_EVT --> D_RM[RoomManager.HandleRoomCleared<br/>heal / curse / mystery chest]
    D_RM --> D_NEXT[EnterNextRoom<br/>roll RoomType]
    D_NEXT --> D_EXPLORE

    D_EVT -->|Boss room| D_FLOOR[FloorManager.NextFloor]
    D_FLOOR --> D_CHECK{Floor ≥ 10?}
    D_CHECK -->|Có| D_WIN([VICTORY])
    D_CHECK -->|Không| D_NEXT

    D_FIGHT -->|Player HP=0| D_LOSE([GAME OVER])
```

### Loại phòng (Dungeon mode)

| RoomType | Hiệu ứng |
|----------|----------|
| Normal / Elite | Combat + rương |
| Treasure | Rương skill |
| Healing | Hồi 35% HP |
| Shop | MetaShopUI |
| Forge | SkillSelectionUI reroll |
| Curse | -10% max HP |
| Mystery | 50% bonus chest |
| Challenge | Combat khó |
| Boss | NextFloor khi clear |

---

## Luồng Pause & Meta (ngoài combat)

```mermaid
flowchart LR
    PAUSE[HudPauseButton] --> PM[PauseMenuUI.Toggle<br/>timeScale=0]
    PM --> RESUME[Tiếp tục → timeScale=1]
    PM --> SHOP[Cửa hàng → MetaShopUI<br/>11 meta upgrade]
    PM --> MENU[Về menu → CharacterSelectScene]
    SHOP --> PM
```

---

## Class / file tham chiếu chính

| Giai đoạn | File |
|-----------|------|
| Chọn nhân vật | `CharacterSelectUI.cs`, `RunEntryGate.cs` |
| Bootstrap | `GameRunBootstrap.cs`, `SceneRunReset.cs` |
| Run state | `RunManager.cs`, `FloorManager.cs`, `HeroRunStats.cs` |
| Combat | `PlayerController.cs`, `AutoAttack.cs`, `WeaponManager.cs`, `EnemyAI.cs`, `HealthSystem.cs` |
| Wave | `EnemySpawner.cs`, `EnemyAliveTracker.cs` |
| Boss | `BossSpawnManager.cs`, `BossController.cs` |
| Phần thưởng | `ChestController.cs`, `RedChestController.cs`, `SkillSelectionUI.cs`, `ExpSystem.cs` |
| Dungeon | `DungeonRunController.cs`, `RoomController.cs`, `RoomManager.cs` |
| Kết thúc | `HUDManager.cs`, `GameOverUI.cs`, `MetaProgression.cs` |

---

## Tóm tắt

1. **Vào game:** `CharacterSelectScene` → chọn hero → `SampleScene`.
2. **Chơi:** Survive 10 wave; diệt quái, lên level, chọn skill từ level-up và rương.
3. **Boss:** Wave 3, 6, 9, 10 — mỗi boss có phase; thưởng rương đỏ.
4. **Thắng:** Hạ boss tầng 10 **hoặc** hoàn tất chọn skill từ rương sau wave 10.
5. **Thua:** HP player về 0.
6. **Sau run:** Xu run chuyển meta xu; Chơi lại hoặc về màn chọn nhân vật.
