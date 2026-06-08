# DUNGEON SOUL — KẾ HOẠCH PHÁT TRIỂN THEO GIAI ĐOẠN (GDD)

**Phiên bản:** 1.1  
**Ngày:** 08/06/2026  
**Nền tảng:** Android / iOS (Unity 2022 LTS)  
**Thị trường mục tiêu:** Việt Nam → Toàn cầu (EN)  
**Nguyên tắc sản xuất:** Ưu tiên **logic đúng + kết quả đúng**; **tái sử dụng UI/asset** được chấp nhận  
**Đối tượng đọc:** Team dev, art, QA, marketing

---

## 0. TÓM TẮT ĐIỀU HÀNH

Dungeon Soul là roguelite top-down 16-bit cho mobile, lấy cảm hứng từ Soul Knight, Archero, Vampire Survivors và Hades. Vision đầy đủ cần **600–800 asset** và **12+ tháng** — quá rủi ro cho indie solo.

**Chiến lược:** Ship MVP trong **2–3 tháng** (~30% vision), đo KPI thật, mở rộng chỉ khi số liệu đạt ngưỡng.

**Phát hiện quan trọng từ codebase hiện tại:** Project đã có nhiều hệ thống "full game" (20 nhân vật, 3 class, 10 tầng, 28 skill, meta shop 11 upgrade). MVP **không phải build từ zero** — mà **cắt scope hiển thị + cân bằng + ship**.

### Nguyên tắc tái sử dụng (bắt buộc team tuân theo)

| Ưu tiên | Không ưu tiên |
|---------|----------------|
| Gameplay loop chạy đúng | UI độc nhất từng màn |
| Số liệu combat/meta chính xác | Icon riêng mỗi skill |
| Feedback rõ (damage, level up, thắng/thua) | Theme visual khác nhau từng room |
| Crash-free, FPS ổn định mobile | Polish art trước khi có KPI |

> **UI trùng nhau là OK** nếu ngữ cảnh và kết quả khác nhau (vd: cùng card frame cho skill/rương/forge — logic reward khác nhau). Không làm UI mới chỉ để "đẹp hơn" khi chưa validate retention.

---

## 1. VISION ĐẦY ĐỦ vs MVP

| Hạng mục | Vision đầy đủ | Phase 1 MVP (~30%) |
|----------|---------------|---------------------|
| Class | 3 (Warrior/Ranger/Mage) | **1 class: Warrior** |
| Nhân vật chơi được | 20 | **1** (Kiếm sĩ) |
| Tầng hầm | 10, 4 theme | **5 tầng, 1 theme** |
| Boss | 4+ | **2** |
| Enemy archetype | 10+ loại | **3** |
| Skill | 28+, skill tree 3 nhánh | **12 skill cards** |
| Vũ khí | 12, evolve | **3 khởi đầu**, evolve giữ |
| Equipment 4 slot × 5 rarity | Có | **Cắt hoàn toàn** |
| Forge 6 chức năng | Có | **Forge = reroll skill card** |
| Achievement | 30+ | **5** |
| Meta progression | Phức tạp | **6 upgrade vĩnh viễn** |
| Chế độ map | Dungeon + Wave | **Wave arena** (đã là default) |

---

## 2. PHASE 1 — MVP (2–3 THÁNG)

### 2.1 Mục tiêu Phase 1

> Người chơi hiểu loop trong **60 giây**, hoàn thành 1 run trong **8–12 phút**, muốn chơi lại vì meta upgrade + build skill khác.

**Core loop:**

```
Main Menu → Chọn Kiếm sĩ → Chọn vũ khí → Vào hầm
→ Di chuyển + auto-attack → Nhận EXP → Chọn skill card
→ Clear wave → Rương/Shop → Boss tầng 3 & 5
→ Thắng / Chết → Meta coin → Upgrade vĩnh viễn → Chơi lại
```

---

### 2.2 Class khởi đầu: WARRIOR — và vì sao không Ranger/Mage

| Tiêu chí | Warrior | Ranger | Mage |
|----------|---------|--------|------|
| Dễ hiểu mobile | ★★★★★ | ★★★☆☆ | ★★☆☆☆ |
| Độ phức tạp VFX | Thấp | Trung bình | Cao |
| Cân bằng auto-attack | Ổn định | Phụ thuộc aim | AoE lag risk |
| Giống Soul Knight | Rất gần | Archero-like | Khác biệt |
| Code sẵn có | Có (9 warrior) | Có | Có |

**Quyết định: Launch với Warrior.**

- Mobile VN ưu tiên **session ngắn, feedback tức thì** — đánh gần dễ đọc hơn projectile bay khắp màn hình.
- Ranger/Mage cần thêm tuning FPS, readability, nhiều VFX hơn — để Phase 2/3.
- **Giữ code 3 class** nhưng **ẩn UI** — mở Ranger ở Phase 2, Mage ở Phase 3.

**Nhân vật duy nhất MVP:** **Kiếm sĩ (Swordsman)**

- HP base: **140**, Damage: **16**, Move: **4.6**, Fire rate: **1.25/s**, Crit: **6%**
- Bonus: +18% sát thương / -8% máu — identity rõ: aggressive melee

---

### 2.3 Cấu trúc hầm MVP: 5 TẦNG (không phải 10)

| Tầng | Nội dung | Boss | Thời gian mục tiêu |
|------|----------|------|---------------------|
| 1 | Tutorial combat, 2 wave nhẹ | — | 1.5 phút |
| 2 | Elite đầu tiên, rương skill | — | 2 phút |
| 3 | Spike difficulty | **Goblin King** | 2 phút |
| 4 | Shop + forge room | — | 1.5 phút |
| 5 | Final | **Stone Golem** | 2–3 phút |

**Tổng run:** **8–12 phút** (session mobile lý tưởng)

**Vì sao 5 tầng, không 10?**

- Run 10 tầng ≈ 18–25 phút → retention mobile VN giảm (commute gaming 5–10 phút).
- 5 tầng = đủ arc: học → build → mid-boss → shop → finale.
- Code đã có `FloorManager` 10 tầng → MVP set `maxFloor = 5`, ẩn UI "/10" thành "/5".
- Phase 2 mở tầng 6–10 khi D7 ổn.

**Theme MVP:** 1 theme — **Ngục đá cổ** (dungeon stone) — palette xám/nâu/đỏ. Phase 2 thêm theme 2 (rừng), Phase 3 thêm 2 theme nữa.

---

### 2.4 Enemy & Boss MVP

#### Enemy archetypes (3)

| Archetype | HP T1 | Speed | Damage | Spawn weight | Vai trò |
|-----------|-------|-------|--------|--------------|---------|
| **Grunt** (Xương) | 28 | 2.8 | 6 | 55% | Đông, dễ |
| **Runner** (Goblin) | 18 | 4.5 | 4 | 30% | Áp lực di chuyển |
| **Brute** (Orc nhỏ) | 55 | 1.8 | 12 | 15% | Tank zone |

**Wave scaling mỗi tầng:** `HP × (1 + 0.18 × floor)`, `Damage × (1 + 0.12 × floor)`

**Enemy visual MVP:** 3 animation set (Skeleton, Goblin palette-swap, Orc) — dùng `EnemyAnimationBuilder` đã có.

#### Boss (2)

| Boss | Tầng | HP | Phase | Ability |
|------|------|-----|-------|---------|
| **Goblin King** | 3 | 420 | 1 | Summon 4 grunt, slam AoE 20 dmg |
| **Stone Golem** | 5 | 680 | 2 (60% HP) | Rock throw 15 dmg, ground slam 25 dmg |

**Drop boss:** 15–25 meta coin + guaranteed skill reroll token.

---

### 2.5 Hệ thống tiến triển: CHỌN SKILL CARDS

#### So sánh 3 hệ thống

| Hệ thống | Ưu điểm | Nhược điểm | Effort MVP | Code hiện tại |
|----------|---------|-----------|------------|---------------|
| **Skill Cards** ★ | Mỗi run khác, VS-like, session dopamine | Balance khó | **Thấp** | **Đã có 80%** |
| Equipment 4 slot | Depth lâu dài | UI phức tạp, 40+ icon, inventory | Cao | **Chưa có** |
| Skill Tree 3 nhánh | Build identity | Ít variety/run, cần respec UI | Trung bình | **Chưa có** |

**Quyết định Phase 1: Skill Cards only.**

- `SkillSelectionUI`, `PlayerSkillHandler`, 28 skill assets — ship 12, cắt 16.
- Equipment & skill tree → Phase 2–3.

#### 12 Skill MVP (cân bằng)

| # | Skill | Rarity | Effect/stack | Max stack |
|---|-------|--------|--------------|-----------|
| 1 | Double Shot | Common | +1 projectile | 3 |
| 2 | Iron Body | Common | +12% max HP | 5 |
| 3 | Quick Reload | Common | +10% fire rate | 5 |
| 4 | Tough Skin | Common | -8% damage taken | 3 |
| 5 | Speed Boost | Common | +8% move speed | 4 |
| 6 | Critical Hit | Rare | +6% crit | 4 |
| 7 | Life Steal | Rare | 4% lifesteal | 3 |
| 8 | Multi Target | Rare | +1 target | 2 |
| 9 | Fire Arrow | Rare | +20% dmg, burn 3 dmg/s 2s | 3 |
| 10 | Explosion | Epic | 15 AoE on kill | 2 |
| 11 | Time Freeze | Epic | 1.5s freeze, 25s CD | 1 |
| 12 | Dragon Strike | Legendary | 80 dmg line, 30s CD | 1 |

**Cắt 5 skill placeholder** (Boomerang, LightningChain, PoisonCloud, BladeStorm, MirrorImage) — data only, không logic.

**Level-up curve:**

| Level | EXP cần | Tổng EXP | Skill pick |
|-------|---------|----------|------------|
| 2 | 80 | 80 | 1 |
| 3 | 120 | 200 | 2 |
| 4 | 180 | 380 | 3 |
| 5 | 260 | 640 | 4 |
| 6 | 360 | 1000 | 5 |
| 7 | 480 | 1480 | 6 |
| 8 | 620 | 2100 | 7 |
| 9 | 780 | 2880 | 8 |
| 10 | 960 | 3840 | 9 (cap) |

**MVP cap level 10** — đủ build trong 8–12 phút.

**Chest skill rarity (Normal room):**

| Rarity | Rate |
|--------|------|
| Common | 55% |
| Rare | 30% |
| Epic | 12% |
| Legendary | 3% |

---

### 2.6 Vũ khí MVP (3 lựa chọn khởi đầu)

| Vũ khí | Style | Damage | Fire rate | Evolve (3 copies) |
|--------|-------|--------|-----------|-------------------|
| **Kiếm sắt** (melee) | Melee arc | 18 | 1.4/s | Death Blade (+30% dmg) |
| **Cung sắt** | Ranged | 14 | 1.8/s | Storm Bow (chain 2) |
| **Gậy lửa** | Magic bolt | 16 | 1.2/s | Dragon Staff (explode) |

Giữ `WeaponManager` evolve — depth gameplay, dùng icon/UI có sẵn.

---

### 2.7 Meta Progression MVP (6 upgrade)

**Currency:** Meta Coin — kiếm từ run, persist `PlayerPrefs`.

| Upgrade | Max Lv | Cost/formula | Effect/lv |
|---------|--------|--------------|-----------|
| **Sức bền** | 10 | 50 + 25×lv | +8 HP |
| **Lưỡi kiếm** | 10 | 50 + 25×lv | +2 damage |
| **Tốc đánh** | 8 | 60 + 30×lv | +3% fire rate |
| **Giày rơm** | 8 | 40 + 20×lv | +2% move |
| **May mắn skill** | 5 | 100 + 50×lv | +3% rare+ skill |
| **Túi vàng** | 10 | 30 + 15×lv | +5% coin/run |

**Coin economy/run:**

| Nguồn | Coin |
|-------|------|
| Kill thường | 1 (30% drop) |
| Elite | 3 |
| Boss | 15–25 |
| Clear tầng | 5 |
| Victory bonus | 50 |
| **Trung bình/run** | **35–55** (thua), **80–120** (thắng) |

**Ẩn 5 meta upgrade còn lại** trong code — mở Phase 2.

---

### 2.8 Room types MVP (5 loại)

| Room | Tần suất/floor | Reward |
|------|----------------|--------|
| Normal | 60% | EXP + coin |
| Elite | 15% | 2× EXP, chest skill |
| Treasure | 10% | Skill card 3 chọn |
| Shop | 10% | 1 meta upgrade tạm (run buff, 20 coin) |
| Forge | 5% | Reroll skill 1 lần |

**Cắt:** Healing, Curse, Mystery, Challenge — Phase 2.

---

### 2.9 Achievement MVP (5)

| ID | Điều kiện | Reward |
|----|-----------|--------|
| Bước đầu | Hoàn thành tầng 1 | 30 coin |
| Sát thủ | 50 kill/run | 50 coin |
| Vua Goblin | Đánh bại Goblin King | 80 coin |
| Bất bại | Clear 5 tầng | 150 coin |
| Nhà sưu tầm | 8 skill khác nhau 1 run | 100 coin |

---

### 2.10 Core loop timing

| Metric | Target MVP |
|--------|------------|
| **Run length (win)** | 8–12 phút |
| **Run length (lose)** | 4–8 phút |
| **Session length** | 12–18 phút (1–2 run) |
| **Time to first fun** | < 60 giây |
| **Time to first skill pick** | 45–90 giây |
| **Time to first boss** | ~5 phút |
| **Daily play (target)** | 2 session × 15 phút |

---

### 2.11 Asset & UI — tái sử dụng tối đa (không ràng buộc ngân sách)

#### Nguyên tắc

1. **Dùng lại trước, làm mới sau** — chỉ tạo asset mới khi reuse gây hiểu nhầm gameplay.
2. **Một bộ UI cho nhiều màn** — card frame, panel, nút dùng chung (`GuiSpriteSet`, `GuiArtLibrary`).
3. **Palette swap > model mới** — enemy/boss khác màu, cùng skeleton animation.
4. **Placeholder icon OK** — skill chưa có icon riêng dùng `GameIconLibrary` fallback (axe/dagger) miễn tooltip/description đúng.

#### Bảng map UI/asset tái sử dụng

| Màn / hệ thống | UI dùng chung | Logic phải khác biệt |
|----------------|---------------|----------------------|
| Level-up skill | `SkillSelectionUI` card | Pool skill theo rarity, apply stat |
| Rương / Forge | **Cùng** `SkillSelectionUI` | Forge = reroll; chest = weighted roll |
| Meta Shop | `MetaShopUI` panel | Persist coin, permanent stat |
| Shop room (in-run) | **Cùng** `MetaShopUI` hoặc buff popup đơn giản | Buff chỉ trong run, không persist |
| Character select | Card frame blank theo class | Stats/bonus từ `PlayableCharacterEntry` |
| Weapon select | Card frame theo weapon type | `RunLoadout` weapon stats |
| Game Over / Victory | Cùng panel style pause | Reward khác: meta coin vs continue |
| Boss HP bar | `BossHPBarUI` | Boss data từ `BossController` |
| HUD skill bar | `SkillsPanelUI` | Stack count từ `PlayerSkillHandler` |

#### Asset có sẵn — dùng trực tiếp MVP

| # | Asset | Nguồn trong project | Ghi chú |
|---|-------|---------------------|---------|
| 1 | Hero Kiếm sĩ | Tiny RPG Swordsman hoặc Craftpix ASEPRITE | Chọn 1, không cần cả hai |
| 2 | Enemy ×3 | Tiny RPG + `EnemyAnimationBuilder` | Palette swap đủ |
| 3 | Boss ×2 | `Resources/Boss/` | Goblin King, Stone Golem |
| 4 | Tileset | 2D Pixel Dungeon Pack / Kenney | 1 theme |
| 5 | Skill icon ×12 | `GameIconLibrary` fallback | Icon trùng OK nếu tên + mô tả đúng |
| 6 | Weapon icon | `WeaponIconLibrary` | Đã có 12 |
| 7 | UI kit | `GuiSpriteSet.asset` | Card, panel, button chung |
| 8 | VFX | `EffectsPack14`, `SkillVfxLibrary` | Recolor OK |
| 9 | Font | LiberationSans / `GameUIFont` | Đã chuẩn hóa |
| 10 | Audio | `Resources/Audio/` hoặc 1 pack free | 1 BGM loop + SFX pack đủ MVP |

#### Không cần làm cho MVP

- App icon độc quyền (dùng screenshot crop tạm)
- Trailer motion graphic
- Icon riêng từng skill
- Theme map thứ 2
- Animation hero mới nếu Tiny RPG đã đủ

---

### 2.12 Scope cut checklist (dev)

| Việc | Action |
|------|--------|
| 20 nhân vật | Ẩn UI, chỉ Swordsman |
| 10 tầng | Cap 5 |
| 28 skill | Ship 12 |
| 5 skill placeholder | Remove khỏi pool |
| 11 meta upgrade | Hiện 6 |
| Character select | Skip → auto Swordsman (hoặc 1 card) |
| Dungeon BSP mode | Ẩn, chỉ Wave |
| Passive items 8 loại | Disable |
| EN language | String table sẵn, ship VI |

---

### 2.13 KPI — CỔNG PHASE 2

Đo **4 tuần sau launch** (Android VN soft launch trước):

#### KPI bắt buộc (ALL phải đạt)

| KPI | Ngưỡng GO | Ngưỡng KILL |
|-----|-----------|-------------|
| **D1 Retention** | ≥ **38%** | < 28% |
| **D7 Retention** | ≥ **10%** | < 6% |
| **Avg session length** | ≥ **7 phút** | < 4 phút |
| **Runs/session** | ≥ **1.6** | < 1.2 |
| **Win rate (tầng 5)** | **12–22%** | > 40% hoặc < 5% |
| **Crash-free sessions** | ≥ **99.2%** | < 98% |
| **Store rating (VN)** | ≥ **4.0** | < 3.5 |
| **Install → D1 complete 1 run** | ≥ **65%** | < 45% |

#### KPI thương mại (1 trong 2)

| KPI | Ngưỡng GO |
|-----|-----------|
| **Organic CPI** | < $0.80 (VN) |
| **D30 revenue/user** | > $0.15 (nếu có IAP) |
| **D30 ad ARPU** | > $0.08 (nếu ad-only) |

**Sample size tối thiểu:** 1.000 install organic + 500 paid test → mới quyết định Phase 2.

---

### 2.14 Rủi ro & mitigation (Phase 1 miss KPI)

| Tình huống | Nguyên nhân có thể | Hành động |
|------------|-------------------|-----------|
| D1 < 28% | Onboarding dài, confusing | Bỏ character/weapon select, tutorial 30s |
| Session < 4 phút | Run quá khó/dễ | Tune HP enemy -15% hoặc player +10% HP |
| Win rate > 40% | Quá dễ, thiếu challenge | Tăng boss HP 20%, giảm skill rate |
| Win rate < 5% | Quá khó | Nerf boss phase 2, thêm 1 free skill tầng 1 |
| Rating < 3.5 | Bug, ad spam | Hotfix 48h, giảm ad frequency |
| Retention OK, revenue thấp | Monetization chưa có | Thêm rewarded ad (revive 1 lần/run) |
| **KILL pivot** | D7 < 6% sau 2 tune | Pivot sang **survivor endless 1 tầng** hoặc pause project |

---

## 3. PHASE 2 — EXPANSION (3–4 THÁNG, CHỈ KHI KPI ĐẠT)

### 3.1 Điều kiện mở Phase 2

- Tất cả KPI bắt buộc Phase 1 đạt GO
- ≥ 5.000 MAU VN
- Team có thời gian dev tiếp ≥ 3 tháng

### 3.2 Hệ thống thêm — theo thứ tự ưu tiên

| # | Hệ thống | Vì sao | Effort |
|---|----------|--------|--------|
| 1 | **Class Ranger** + 3 nhân vật | Data Phase 1 chứng minh combat fun; Ranger mở ranged build | 3 tuần |
| 2 | **Tầng 6–10** + 2 boss | Content retention D7–D30 | 2 tuần |
| 3 | **Theme map 2** (Rừng) | Visual freshness | 2 tuần |
| 4 | **+8 skill** (total 20) | Build variety | 2 tuần |
| 5 | **Equipment lite** (2 slot: Weapon + Armor) | Depth không cần 4 slot | 4 tuần |
| 6 | **5 meta upgrade** ẩn | Long-term hook | 1 tuần |
| 7 | **Achievement +10** | Completionist | 1 tuần |
| 8 | **Healing/Curse room** | Risk/reward | 1 tuần |

**Không thêm Phase 2:** Skill tree 3 nhánh, forge 6 chức năng, legendary items — để Phase 3.

### 3.3 Ranger — logic mở class

**Unlock điều kiện in-game:** Đánh bại Stone Golem **3 lần** HOẶC meta level Sức bền ≥ 5.

**3 nhân vật Ranger:**

| Nhân vật | Identity |
|----------|----------|
| Archer | Tốc bắn, glass cannon |
| Skeleton Archer | Crit build |
| Werewolf | Tốc chạy + sustain |

**HP/DMG baseline Ranger:** HP ×0.75, DMG ×1.15, Fire rate ×1.3 so với Warrior.

### 3.4 Content Phase 2

| Content | Số lượng |
|---------|----------|
| Tầng | 5 → **10** |
| Boss mới | +2 (Shadow Witch T9, Dragon Lord T10) |
| Enemy archetype | +2 (Ranged, Summoner) |
| Theme | +1 (Forest) |
| Skill | 12 → **20** |
| Equipment slot | **2** (Weapon augment + Armor) |
| Rarity equipment | 3 (Common/Rare/Epic) — chưa Legendary |

### 3.5 Asset Phase 2 — reuse mở rộng

| Hạng mục | Cách làm (không cần art mới) |
|----------|------------------------------|
| Ranger 3 nhân vật | Mở UI character select, dùng Tiny RPG có sẵn |
| Tầng 6–10 | Tăng `maxFloor`, boss có sẵn trong Resources |
| Theme Rừng | Recolor tile palette hoặc Craftpix free tileset |
| +8 skill | Bật skill đã có logic, icon reuse |
| Equipment 2 slot | UI dùng card frame + icon weapon/armor chung |
| EN text | String table, không cần art mới |

### 3.6 KPI cổng Phase 3

| KPI | Ngưỡng |
|-----|--------|
| D30 Retention | ≥ 4% |
| MAU | ≥ 20.000 |
| Revenue MAU | ≥ $0.25 (blended) |
| Phase 2 content completion | ≥ 40% player reach T10 |

---

## 4. PHASE 3 — FULL GAME (6+ THÁNG)

### 4.1 Điều kiện

- Phase 2 đạt KPI thương mại
- MAU ≥ 50.000

### 4.2 Hoàn thiện vision gốc

| Hạng mục | Target |
|----------|--------|
| Class | 3 (thêm **Mage**) |
| Nhân vật | **20** |
| Tầng + theme | 10 tầng × **4 theme** |
| Equipment | **4 slot** × **5 rarity** + legendary |
| Skill tree | 3 nhánh × 3 class |
| Forge | **6 chức năng** |
| Achievement | **30+** |
| Consumables | 10+ loại |
| Legendary items | 8–12 |
| Boss | 6–8 unique |
| Enemy types | 12+ |

### 4.3 Mage unlock logic

Unlock sau khi clear T10 với cả Warrior và Ranger, hoặc achievement "Master of Blades".

### 4.4 Live ops & dài hạn

- Season 4 tuần: modifier (double skill, curse week)
- Battle pass nhẹ ($2.99) — cosmetic + meta coin
- 1 hero mới / 2 tháng (palette + skill tweak)
- Cloud save
- Leaderboard weekly

### 4.5 Asset Phase 3 — khi nào mới làm art riêng

Chỉ đầu tư art độc quyền khi Phase 2 chứng minh retention + revenue. Thứ tự ưu tiên:

1. Mage class visuals (mở UI + Tiny RPG có sẵn)
2. 4 theme map (palette + tileset swap)
3. Legendary item frame (1 sprite × recolor 5 rarity)
4. Skill tree UI (dùng lại card/panel kit)
5. Store icon + screenshot (khi global launch)

---

## 5. BẢNG CÂN BẰNG COMBAT MVP (REFERENCE)

### Player (Kiếm sĩ, meta 0)

| Stat | Value |
|------|-------|
| HP | 140 |
| Damage | 16 |
| Move speed | 4.6 u/s |
| Fire rate | 1.25/s |
| Crit | 6% × 1.5 dmg |

### Scaling đề xuất

```
enemyHP = baseHP × (1 + 0.18 × floor) × (1 + 0.05 × wave)
playerDamage = baseDmg × (1 + 0.04 × (level-1)) × skillMultipliers
```

### Drop rate tổng hợp

| Item | Rate |
|------|------|
| Coin từ mob | 30% × 1 coin |
| EXP gem | 100% kill |
| Skill chest Elite | 100% |
| Revive ad | 1×/run (Phase 1 optional) |

---

## 6. LỘ TRÌNH 12 TUẦN PHASE 1

| Tuần | Dev | Asset/UI (reuse) | QA |
|------|-----|------------------|-----|
| 1–2 | Scope cut, cap 5 floor, 12 skill | Giữ GuiSpriteSet chung, 1 hero Tiny RPG | Test loop 5 phút |
| 3–4 | Balance pass, 6 meta | Skill icon fallback, không làm mới | Android internal |
| 5–6 | Tutorial ngắn, ẩn màn thừa | Screenshot từ Game view | 20 tester VN |
| 7–8 | Soft launch VN Google Play | Không block ship vì art | Firebase analytics |
| 9–10 | Tune theo data | Chỉ sửa asset nếu gây hiểu nhầm | Organic install |
| 11–12 | Phase 1 lock | — | KPI review → GO/KILL |

---

## 7. MONETIZATION MVP (KHUYẾN NGHỊ)

| Model | Phase 1 | Ghi chú |
|-------|---------|---------|
| **Rewarded ad** | Revive 1×, double coin end run | Ít gây rating thấp |
| **Interstitial** | Sau run chết (max 1/10 phút) | Nhẹ |
| **IAP** | Gói bỏ ad $2.99, starter pack $0.99 | Phase 1 optional |
| **Gacha** | Không | Phase 3 nếu có |

---

## 8. KẾT LUẬN & QUYẾT ĐỊNH CHÍNH

| Quyết định | Lựa chọn | Lý do |
|------------|----------|-------|
| Class launch | **Warrior / Kiếm sĩ** | Mobile-friendly, ít VFX, code sẵn |
| Số tầng | **5** | Session 8–12 phút |
| Progression | **Skill cards** | 80% code có sẵn |
| Equipment | **Cắt** | Chưa có code |
| Meta | **6 upgrade** | Đủ hook, dễ balance |
| Map mode | **Wave arena** | Default project |
| UI/Art | **Reuse tối đa** | Logic đúng > visual độc nhất |
| Cổng P2 | **D1 ≥ 38%, D7 ≥ 10%** | Industry indie mobile |

---

**Dungeon Soul đã có nền tảng code vượt MVP.** Phase 1 không phải "làm game mới" — mà **cắt, cân bằng, ship với UI/asset tái sử dụng** để trả lời một câu hỏi:

> *"Người chơi VN có muốn chơi lại run thứ 2, thứ 3 không?"*

Nếu câu trả lời là có — Phase 2 mở Ranger, tầng 10, equipment lite (vẫn reuse UI). Nếu không — tune balance/onboarding, không blame art.

---

## PHỤ LỤC A — TRẠNG THÁI CODEBASE (THÁNG 06/2026)

| Hệ thống | Trạng thái | Ghi chú |
|----------|------------|---------|
| 20 nhân vật + 3 class | ✅ Có | Cần ẩn cho MVP |
| 10 tầng + wave arena | ✅ Có | Cap 5 cho MVP |
| 28 skill cards | ✅ ~23 logic | Ship 12 |
| 12 vũ khí + evolve | ✅ Có | Giữ 3 starter |
| Meta shop 11 upgrade | ✅ Có | Hiện 6 |
| Equipment 4 slot | ❌ Chưa có | Phase 2 lite |
| Skill tree | ❌ Chưa có | Phase 3 |
| BSP dungeon | ⚠️ Optional | Ẩn MVP |
| 4 boss | ✅ Có | Dùng 2 cho MVP |
| 4 enemy archetype | ✅ Có | Dùng 3 cho MVP |

---

*Tài liệu nội bộ Dungeon Soul — không phân phối công khai.*
