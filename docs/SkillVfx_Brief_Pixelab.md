# DungeonSoul — Skill VFX Brief for Pixelab

> **Mục đích:** Tài liệu này mô tả toàn bộ 28 kỹ năng của game **DungeonSoul** (2D top-down roguelike kiểu Vampire Survivors) để Pixelab / AI pixel-art tạo **VFX đẹp hơn** thay thế asset hiện tại.
>
> **Ngôn ngữ prompt:** Dùng **tiếng Anh** khi gõ vào Pixelab. Phần mô tả tiếng Việt giúp bạn đối chiếu gameplay.

---

## 1. Bối cảnh game

| Thuộc tính | Giá trị |
|------------|---------|
| Thể loại | 2D top-down survival roguelike |
| Góc nhìn | Từ trên xuống, nhân vật nhỏ ở giữa màn |
| Phong cách | **Pixel art** tối, dungeon fantasy, tương phản cao |
| Nền map | Tím đen / gạch dungeon tối — VFX cần **sáng, rõ viền** |
| Vũ khí | Cung, staff, dagger — skill là **buff + hiệu ứng phép** |

**Quan trọng:** VFX là **hiệu ứng năng lượng / đạn / nổ / vòng sáng** đặt tại **một điểm trên map**. Clone, player, enemy do **code game spawn** — **KHÔNG vẽ vào PNG**.

---

## 2. Quy tắc bắt buộc (đọc trước khi vẽ)

### ✅ PHẢI có
- Chỉ **vật lý hiệu ứng**: lửa, sét, băng, khói, rune, tia đạn, vòng aura, nổ, portal gương…
- **Nền trong suốt** (alpha channel)
- **Tâm hiệu ứng** nằm giữa khung hình (center pivot)
- **Pixel art sắc nét** — không blur, không anti-alias mềm
- Animation **đọc được trong ~0.4 giây** (6 frame burst)

### ❌ TUYỆT ĐỐI KHÔNG
- Nhân vật / hero / silhouette người / tay / chân / mặt
- Storyboard nhiều ô (1→2→3 clone)
- Icon tĩnh UI (khiên đứng yên như icon skill)
- Con rồng, quái vật, boss trong frame VFX
- Chữ / số / watermark
- Viền trắng dày kiểu sticker

### Master prompt (dán đầu mỗi lần gen)

```
2D pixel art game VFX sprite, top-down action RPG, transparent background,
effect ONLY no character no creature no humanoid silhouette,
centered composition, crisp pixels no blur, dark fantasy dungeon palette,
high contrast glow, 256x256 pixels, single animation frame [FRAME X of 6],
```

---

## 3. Spec kỹ thuật giao file

### 3.1 Burst VFX (bắt buộc — 28 skill)

| Thuộc tính | Giá trị |
|------------|---------|
| Số frame | **6** (`frame_00` … `frame_05`) |
| Kích thước | **256 × 256 px** mỗi frame |
| Định dạng | PNG RGBA |
| Đặt tên file | `frame_00.png`, `frame_01.png`, … |
| Thư mục | `PerSkill/{SkillType}/` |
| Pivot / anchor | Giữa ảnh (50%, 50%) |
| Filter | Point (nearest neighbor) — không smooth |

**Timeline 6 frame (áp dụng mọi skill):**

| Frame | Vai trò | Gợi ý |
|-------|---------|-------|
| `frame_00` | Spawn | Hiệu ứng bắt đầu xuất hiện (~10% cường độ) |
| `frame_01` | Build-up | Tăng sáng / mở rộng |
| `frame_02` | Peak | Đỉnh sáng, dễ đọc nhất |
| `frame_03` | Sustain | Giữ peak hoặc lan rộng |
| `frame_04` | Fade | Bắt đầu tan |
| `frame_05` | Residue | Tàn dư mờ, gần trong suốt |

### 3.2 Aura loop (tùy chọn — 15 skill epic/legendary + một số buff)

| Thuộc tính | Giá trị |
|------------|---------|
| Số frame | **4** (`frame_00` … `frame_03`) |
| Kích thước | **256 × 256 px** |
| Thư mục | `Auras/Skills/{SkillType}/` |
| Nội dung | **Vòng tròn rỗng ở giữa** — player đứng trong vòng, không vẽ người |
| Loop | Frame 04 nối mượt về frame 01 (ornament xoay nhẹ) |

### 3.3 Skill icon UI (tham khảo, không bắt buộc trong batch này)

- 28 icon riêng: `GeneratedIcons/Skills/{SkillType}.png`
- Có thể là icon tĩnh — **khác** với burst VFX

### 3.4 Cấu trúc thư mục Unity

```
Assets/Resources/GeneratedSkillVfx/
├── PerSkill/
│   ├── DoubleShot/
│   │   ├── frame_00.png … frame_05.png
│   ├── FireArrow/
│   └── … (28 folders, tên = SkillType enum)
└── Auras/
    └── Skills/
        ├── SpeedBoost/
        │   ├── frame_00.png … frame_03.png
        └── … (15 folders)
```

### 3.5 Skill đặc biệt — hướng vẽ

| Skill | Lưu ý orientation |
|-------|-------------------|
| **LightningChain** | Tia sét vẽ **theo trục DỌC** sprite (từ đáy lên đỉnh). Game xoay -90° để nối 2 enemy ngang. Để glow ở **hai đầu** dọc. |
| **DragonStrike** | Cột lửa **thẳng đứng** từ trên xuống + nổ ở chân. **Không** vẽ rồng. |
| **PiercingArrow / TwinArrows / DoubleShot** | Có thể vẽ tia **ngang** (trái → phải) — game đặt tại điểm bắn. |

---

## 4. Bảng tổng quan 28 skill

| # | SkillType (folder) | Tên EN | Độ hiếm | Element | Burst | Aura |
|---|-------------------|--------|---------|---------|-------|------|
| 1 | DoubleShot | Double Shot | Common | Physical | 6 | — |
| 2 | SpeedBoost | Speed Boost | Common | Wind | 6 | 4 |
| 3 | IronBody | Iron Body | Common | Holy/Steel | 6 | 4 |
| 4 | QuickReload | Quick Reload | Common | Gold/Time | 6 | — |
| 5 | CoinMagnet | Coin Magnet | Common | Gold | 6 | 4 |
| 6 | ToughSkin | Tough Skin | Common | Earth | 6 | 4 |
| 7 | FireArrow | Fire Arrow | Common | Fire | 6 | — |
| 8 | SteadyAim | Steady Aim | Common | Physical | 6 | — |
| 9 | PiercingArrow | Piercing Arrow | Rare | Ice/Physical | 6 | — |
| 10 | MultiTarget | Multi-Target | Rare | Physical | 6 | — |
| 11 | CriticalHit | Critical Hit | Rare | Physical/Gold | 6 | — |
| 12 | LifeSteal | Life Steal | Rare | Blood | 6 | — |
| 13 | Boomerang | Boomerang | Rare | Physical | 6 | — |
| 14 | LightningChain | Lightning Chain | Rare | Lightning | 6 | 4 |
| 15 | PoisonCloud | Poison Cloud | Rare | Poison | 6 | 4 |
| 16 | ExplosiveRounds | Explosive Rounds | Rare | Fire | 6 | — |
| 17 | Explosion | Explosion | Epic | Fire | 6 | — |
| 18 | IceAura | Ice Aura | Epic | Ice | 6 | 4 |
| 19 | GhostForm | Ghost Form | Epic | Arcane | 6 | 4 |
| 20 | QuadShot | Quad Shot | Epic | Physical | 6 | — |
| 21 | BladeStorm | Blade Storm | Epic | Physical | 6 | 4 |
| 22 | Vampire | Vampire | Epic | Blood | 6 | 4 |
| 23 | TwinArrows | Twin Arrows | Epic | Physical | 6 | — |
| 24 | DeathMark | Death Mark | Legendary | Dark | 6 | 4 |
| 25 | TimeFreeze | Time Freeze | Legendary | Ice/Time | 6 | 4 |
| 26 | DragonStrike | Dragon Strike | Legendary | Fire | 6 | 4 |
| 27 | SoulHarvest | Soul Harvest | Legendary | Arcane | 6 | 4 |
| 28 | MirrorImage | Mirror Image | Legendary | Arcane | 6 | 4 |

**Độ hiếm:** Common (xám) → Rare (xanh) → Epic (tím) → Legendary (vàng cam). Skill hiếm hơn = VFX **lớn hơn, sáng hơn, chi tiết hơn** — không đổi kích thước file.

---

## 5. Chi tiết từng skill (dùng cho Pixelab)

Mỗi skill gồm:
- **Gameplay (VI)** — khi nào VFX chạy trong game
- **Màu chủ đạo**
- **Burst prompt** — mô tả 6 frame
- **Aura prompt** — nếu có

---

### COMMON (8)

#### 1. DoubleShot
- **Gameplay:** Bắn 2 viên song song. VFX khi **nhận skill** (level-up).
- **Màu:** Vàng cam `#FFB840`, lõi trắng vàng
- **Burst:** Hai tia đạn song song bắn **ra phải** từ tâm, chớp nòng lúc đầu, trail mờ dần frame 4–5
- **Pixelab prompt:**
```
parallel golden arrow energy trails shooting right from center, muzzle flash at start,
two horizontal projectile streaks, warm orange gold pixels, frame [N] of 6 burst animation
```

#### 2. SpeedBoost
- **Gameplay:** +tốc chạy. VFX khi nhận skill + **aura loop** quanh player.
- **Màu:** Xanh mint `#73F2CC`, cyan nhạt
- **Burst:** Vòng gió pulse lan ra + 3 cung xoáy
- **Aura:** Vòng tròn rỗng, dash gió xoay quanh, ornament: **wind dashes**
```
turquoise wind ring expanding pulse, swirling air arcs, speed lines, no runner silhouette,
frame [N] of 6 / OR hollow circular wind aura loop frame [N] of 4
```

#### 3. IronBody
- **Gameplay:** +HP max. VFX khi nhận skill + aura.
- **Màu:** Thép xanh `#99C7FF`
- **Burst:** Khiên năng lượng tròn bung ra (overshoot) rồi vỡ thành mảnh sáng
- **Aura:** Vòng thép, ornament: **crescent shields**
```
steel blue energy shield ring expanding then shattering into light shards,
holy metal glow, hollow protective ring aura, frame [N]
```

#### 4. QuickReload
- **Gameplay:** Giảm cooldown bắn. Chỉ burst lúc nhận skill.
- **Màu:** Vàng đồng `#FFE666`
- **Burst:** Kim đồng hồ vàng quét nhanh 2 vòng quanh tâm
```
golden clock hand arc sweeping fast around center, tick marks on ring,
reload speed magic, frame [N] of 6
```

#### 5. CoinMagnet
- **Gameplay:** Hút xu/EXP xa hơn. Burst + aura.
- **Màu:** Vàng `#FFD940`
- **Burst:** Hạt vàng xoáy spiral vào tâm
- **Aura:** Vòng vàng, chấm vàng orbit
```
gold coin sparkle particles spiraling inward to center, magnetic swirl,
hollow gold ring aura with orbiting dots, frame [N]
```

#### 6. ToughSkin
- **Gameplay:** Giảm damage nhận. Burst + aura.
- **Màu:** Nâu đá `#9E7A52`
- **Burst:** Mảnh đá ghép thành vòng rồi rơi vụn
- **Aura:** Vòng đá, ornament dots
```
stone rock fragments forming cracked earth ring then crumbling,
brown rocky armor aura ring, frame [N]
```

#### 7. FireArrow
- **Gameplay:** Đạn gây burn. VFX trên enemy khi trúng (combat).
- **Màu:** Cam đỏ lửa `#FF6600`, lõi vàng
- **Burst:** Ngọn lửa nhỏ bùng tại điểm trúng, lưỡi lửa lắc, tàn lửa bay lên
```
small fire burst on ground hit, flickering flame tongue, ember particles rising,
orange red fire pixels, frame [N] of 6
```

#### 8. SteadyAim
- **Gameplay:** +damage flat. Burst lúc nhận skill.
- **Màu:** Đỏ ngắm `#FF4D40`
- **Burst:** 4 ngoặc crosshair siết vào tâm + chấm đỏ pulse
```
red crosshair brackets converging to center, targeting reticle flash,
precision aim effect, frame [N] of 6
```

---

### RARE (8)

#### 9. PiercingArrow
- **Gameplay:** Đạn xuyên nhiều enemy. Burst lúc nhận skill.
- **Màu:** Cyan băng nhạt `#99D9FF`
- **Burst:** Mũi tên lớn xuyên ngang, 2 vòng shock pierce giãn nở phía sau
```
large icy arrow piercing horizontally right, cyan trail, expanding pierce shock rings,
frame [N] of 6
```

#### 10. MultiTarget
- **Gameplay:** Bắn nhiều mục tiêu. Burst lúc nhận skill.
- **Màu:** Cam `#FFA640`
- **Burst:** 3 tia phân nhánh 120°, đầu mỗi tia có reticle flash
```
three orange energy beams splitting from center at 120 degrees,
target lock rings at tips, frame [N] of 6
```

#### 11. CriticalHit
- **Gameplay:** Tỉ lệ crit. VFX khi crit (combat) — 2 nhát chém chéo vàng.
- **Màu:** Vàng crit `#FFD94D`
- **Burst:** Chữ X slash vàng trắng + spark burst
```
golden critical hit X slash cross, bright impact sparks, crit damage flash,
frame [N] of 6
```

#### 12. LifeSteal
- **Gameplay:** Hút máu theo damage. Burst lúc nhận skill.
- **Màu:** Đỏ máu `#E62633` → xanh hồi `#59FF80`
- **Burst:** Giọt máu hút vào tâm → pulse xanh hồi máu
```
red blood droplets sucked to center then green heal cross pulse ring,
life steal vampiric heal, frame [N] of 6
```

#### 13. Boomerang
- **Gameplay:** Đạn quay về. VFX khi đổi hướng (combat).
- **Màu:** Gỗ vàng `#E6BF73`
- **Burst:** Vệt cung boomerang quét nửa vòng + sparkle tại điểm đổi hướng
```
golden boomerang energy arc curving around center, return path trail sparkle,
frame [N] of 6
```

#### 14. LightningChain
- **Gameplay:** Crit chain sét sang enemy khác. VFX **nối 2 điểm** (directed).
- **Màu:** Tím xanh sét `#8099FF`, lõi trắng
- **Burst:** Tia sét **DỌC** (đáy→đỉnh), zigzag, glow 2 đầu, nhánh phụ frame giữa
- **Aura:** Vòng sét, dash điện
```
vertical lightning bolt zigzag top to bottom, electric purple blue glow nodes at ends,
branch sparks, IMPORTANT draw bolt along vertical axis not horizontal, frame [N] of 6
```

#### 15. PoisonCloud
- **Gameplay:** Enemy chết để vùng độc. VFX tại vị trí chết (combat) + aura.
- **Màu:** Xanh độc `#66E633`, xanh đen `#1F6B1A`
- **Burst:** Mây độc phồng, bong bóng nổ, giọt độc rơi
- **Aura:** Vòng khí độc, chấm xanh
```
toxic green poison cloud puff, bubbling pop, dripping acid particles,
hollow poison mist ring aura, frame [N]
```

#### 16. ExplosiveRounds
- **Gameplay:** Đạn nổ khi trúng. VFX nhỏ hơn Explosion (scale ~70%).
- **Màu:** Cam lửa `#FF6600`
- **Burst:** Nổ lửa **nhỏ** — flash lõi → vòng lửa → khói
```
small fiery explosion burst, compact orange fire rings, less radius than big explosion,
frame [N] of 6
```

---

### EPIC (6)

#### 17. Explosion
- **Gameplay:** Enemy chết gây nổ AoE. VFX tại corpse (combat).
- **Màu:** Đỏ cam `#FF3300`, vàng flash
- **Burst:** Nổ lửa **lớn** — flash trắng → vòng lửa giãn → khói
```
large fire explosion burst, white core flash expanding fire shockwave rings smoke puffs,
epic scale, frame [N] of 6
```

#### 18. IceAura
- **Gameplay:** Làm chậm enemy quanh player. Burst định kỳ + aura loop.
- **Màu:** Băng `#99E6FF`
- **Burst:** Vòng băng giãn + 8 tinh thể gai
- **Aura:** Vòng băng, crystal ornaments
```
ice ring expanding with crystal spikes on rim, frost particles,
hollow frozen aura ring with ice crystals, frame [N]
```

#### 19. GhostForm
- **Gameplay:** Miễn nhiễm 2.5s / 15s. Burst khi kích hoạt + aura.
- **Màu:** Tím ma `#8066D9`
- **Burst:** **CHỈ khói ma** cuộn lên tan — không silhouette người
- **Aura:** Vòng khói tím mờ
```
purple ghostly smoke wisps rising and fading, ethereal mist ONLY no ghost body no face,
hollow purple smoke aura ring, frame [N]
```

#### 20. QuadShot
- **Gameplay:** Thêm 2–4 đạn vuông góc. Burst lúc nhận skill.
- **Màu:** Cam vàng `#FFB34D`
- **Burst:** 4 mũi tên fan 90° (lên phải xuống trái)
```
four arrow energy shots bursting in cardinal cross pattern from center,
orange fan spread, frame [N] of 6
```

#### 21. BladeStorm
- **Gameplay:** 3 lưỡi kiếm quay quanh player. VFX định kỳ + aura.
- **Màu:** Bạc trắng `#F2F2E6`
- **Burst:** 3 cung lưỡi chém trắng quay quanh tâm
- **Aura:** Vòng kim loại, crescent blades
```
three white silver blade slash arcs orbiting center, sword storm energy,
hollow blade ring aura, frame [N]
```

#### 22. Vampire
- **Gameplay:** Hồi HP khi kill. VFX tại player khi kill (combat) + aura.
- **Màu:** Đỏ đậm `#D91A2E`
- **Burst:** Hạt máu xoáy hút + 2 nanh trắng nhỏ (không đầu người) + pulse đỏ
- **Aura:** Vòng máu, chấm đỏ
```
crimson blood particles swirling to center, small white fang marks only no vampire face,
blood ring aura, frame [N]
```

#### 23. TwinArrows
- **Gameplay:** Đạn thứ 2 bắn ngay sau. Burst lúc nhận skill (có thể thêm combat sau).
- **Màu:** Vàng `#FFEB8C` + bạc `#CCD9FF`
- **Burst:** Mũi tên 1 bay trước, mũi tên 2 trễ 0.2s song song
```
two parallel arrow trails one gold one pale silver, staggered timing echo shot,
frame [N] of 6
```

---

### LEGENDARY (5)

#### 24. DeathMark
- **Gameplay:** Đánh dấu enemy, sau 5 hit nổ. VFX tại enemy (combat) + aura.
- **Màu:** Đỏ đen `#D91A2E`, rune đỏ
- **Burst:** Frame 0–3: rune X + vòng đỏ hiện dần → Frame 4–5: detonation flash
- **Aura:** Vòng rune đỏ
```
dark red death mark rune cross appearing then detonation flash explosion,
skull-less curse sigil, hollow red rune ring aura, frame [N]
```

#### 25. TimeFreeze
- **Gameplay:** Đóng băng toàn map 2.5s / 30s. Burst khi kích hoạt + aura.
- **Màu:** Băng sáng `#B3F2FF`
- **Burst:** Sóng băng lan + 12 tia kim đồng hồ + đồng hồ nhỏ tâm
- **Aura:** Vòng băng, dash thời gian
```
ice time freeze shockwave ring, clock hand rays, small clock face at center no numbers,
frozen time magic, hollow ice time aura, frame [N]
```

#### 26. DragonStrike
- **Gameplay:** Cột lửa mỗi 8s lên enemy gần nhất. Burst + aura. **Không rồng.**
- **Màu:** Lửa cam `#FF6600`, lõi vàng trắng
- **Burst:** Cột lửa **thẳng đứng** từ trên xuống + nổ chân cột
- **Aura:** Vòng lửa, crystal flame
```
vertical fire pillar striking downward from sky to ground impact explosion,
NO dragon NO creature, meteor column flame only, hollow fire ring aura, frame [N]
```

#### 27. SoulHarvest
- **Gameplay:** 30% kill spawn orb hồi HP. VFX tại orb (combat) + aura.
- **Màu:** Tím linh hồn `#A666FF`
- **Burst:** Orb tím bay lên + wisp xoắn
- **Aura:** Vòng arcane, rune tím
```
purple soul orb rising with spiraling wisps, spirit energy harvest,
hollow arcane soul ring aura, frame [N]
```

#### 28. MirrorImage
- **Gameplay:** Tạo clone (code spawn). VFX = **cổng gương mở/khép** — không vẽ clone.
- **Màu:** Hồng tím `#F273F2`
- **Burst:** Ellipse portal tím mở → shimmer quét ngang → khép
- **Aura:** Vòng rune hồng
```
purple mirror portal ellipse opening and closing, horizontal shimmer sweep,
NO duplicate character NO clones in image, hollow pink mirror rune aura, frame [N]
```

---

## 6. Khi nào VFX phát trong game (để test đúng)

| Trigger | Skills |
|---------|--------|
| **Nhận skill (level-up)** | Tất cả 28 — `PlayForSkill` tại vị trí player |
| **Combat — on hit/kill** | FireArrow, ExplosiveRounds, Explosion, Vampire, PoisonCloud, DeathMark, LightningChain, Boomerang, CriticalHit |
| **Combat — tick định kỳ** | IceAura, GhostForm, BladeStorm, DragonStrike, TimeFreeze, MirrorImage |
| **Aura loop quanh player** | 15 skill có aura (xem bảng mục 4) |

---

## 7. Checklist giao hàng cho Pixelab

Cho **mỗi** skill trong bảng mục 4:

- [ ] Folder `PerSkill/{SkillType}/` với đúng **6** PNG
- [ ] Tên file `frame_00` … `frame_05`
- [ ] 256×256, transparent, pixel art
- [ ] Không nhân vật / quái / rồng trong ảnh
- [ ] Nếu có aura: folder `Auras/Skills/{SkillType}/` với **4** PNG
- [ ] LightningChain: bolt **dọc**
- [ ] DragonStrike: cột lửa **dọc**, không rồng
- [ ] MirrorImage: portal only, không clone

**Tổng file tối thiểu:** 28 × 6 = **168 burst** + 15 × 4 = **60 aura** = **228 PNG**

---

## 8. Prompt mẫu copy nhanh (Pixelab)

Thay `{SKILL}`, `{DESC}`, `{COLOR}`, `{N}`:

```
2D pixel art game VFX, top-down dungeon roguelike, transparent background,
effect only no character no creature, centered, 256x256, crisp pixels,
{color: {COLOR}}
{DESC}
skill name: {SKILL}, animation frame {N} of 6, dark fantasy high contrast glow
```

**Ví dụ IceAura frame 2:**
```
2D pixel art game VFX, top-down dungeon roguelike, transparent background,
effect only no character no creature, centered, 256x256, crisp pixels,
color icy cyan #99E6FF
ice ring expanding with crystal spikes on rim, frost sparkles, peak brightness frame
skill name: IceAura, animation frame 2 of 6, dark fantasy high contrast glow
```

---

## 9. Sau khi Pixelab xong

1. Copy PNG vào đúng folder `Assets/Resources/GeneratedSkillVfx/`
2. Unity: **Texture Type = Sprite (2D)**, **Filter Mode = Point**, **Compression = None**
3. Mở **Tools → DungeonSoul → Skill VFX Preview (All 28)** để xem toàn bộ
4. Hoặc chạy **Tools → DungeonSoul → Bake Skill VFX** nếu cần fallback procedural

---

*DungeonSoul — Skill VFX Brief v1.0 — 28 skills — Generated for Pixelab handoff*
