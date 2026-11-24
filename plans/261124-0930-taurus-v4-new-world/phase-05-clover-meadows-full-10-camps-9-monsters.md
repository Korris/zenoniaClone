# Phase 05 (P3): Clover Meadows đủ 7 khu, 10 camp, 68 quái, 9 loài, quest Lv3-9

## Context Links
- **Đọc hết `docs/map-design-rules.md` + `docs/sprite-rules.md`** (brief agent).
- BIBLE part1 V.1-V.4 (7 khu, bảng camp C01-C10, khoảng đệm), VI (MON-T01..T09), VII (độ khó, mật độ); part2 VI (T-M03, T-M04); part3 XII (aggro / interval / speed / leash / hitbox / TTK), XIV (spawn server).
- Code: `MonsterDef.cs` (ctor l.33, `All` l.198 append-only, `ByType` l.205), `Monster.Ai.cs`, `Monster.Ranged.cs`, `Rpg/MonsterRangedRules.cs` (profile `CliffHarpyFeather`, `SandWraithOrb`), `MapSpawnTable.cs`, `Rpg/LootTable.cs`, `Rpg/DungeonPoints.cs` (điểm chạm cho quest `WaypointId`), `Rpg/Cards/CardDrops.cs`.

## Overview
P2. Lấp đủ map + 8 quái còn lại + quest giữa vùng. Code camp / def làm được trước art (FallbackSheet + tint), art theo pipeline phase 04.

## Đánh giá AI (BIBLE XII so với code)
| Yêu cầu | Có sẵn? | Việc |
|---|---|---|
| Melee chase / windup / cooldown / return | Có (`Monster.Ai.cs`) | Chỉ số theo bảng |
| Ranged projectile thẳng, không homing (Mushroom, Thorn Sprite, Sporeling) | Có (`MonsterRangedProfile`) | 2 profile mới: `SporeShot`, `ThornSeed` (+ `AmberSpore` phase 06) |
| Leash từ điểm sinh | Có (`LeashRange`) | Theo bảng 4-7 m |
| Chỉ phản công (C01) | Thêm ở phase 02 (`Zone.Passive`) | - |
| Reset bất tử ngắn khi về spawn | Chưa rõ: kiểm `Return` có hồi HP / bỏ qua sát thương chưa | Nếu chưa: hồi đầy HP khi về, không nhận sát thương trong `Return` (vài dòng) |
| Charge / dash / formation / trói / elite | Không có, BIBLE nói KHÔNG cần | Không làm (YAGNI) |
| Telegraph | Có cho boss (`SkillBoss`) | Phase 06 |
| Collision nhẹ, không body-block cứng | Kiểm hiện trạng shove giữa quái | Giữ nếu không kẹt; đo ở camp 9 Slime |

## Requirements
- 8 def APPEND theo thứ tự: `tauHornRabbit`, `tauReedFrog`, `tauMossMushroom`, `tauBrambleBoar`, `tauLeafMantis`, `tauRootling`, `tauThornSprite`, `tauAncientBarkling` (+ phase 06: `tauAmberSporeling`, `tauRuneBarkGuardian`, `tauElderRootGuardian`). Chỉ số: speed tương đối (chuẩn = speed Slime / 0,65), aggro / interval / leash theo bảng part3 XII (m x 16 đv), HitW / HitH theo bảng hitbox, HP chỉnh theo TTK mục tiêu (đo bằng log sát thương Warrior / Wizard / Cleric Lv tương ứng, T0 +vài).
- 10 camp đúng bảng part1 V.3 (tổng 68), cấp tăng từ nam lên bắc, đường tân thủ không qua camp mạnh, hành lang tới cửa dungeon (Z7) trống, không > 3 quái chủ động sát nhau trên đường; quái Lv8-10 không đuổi tới khu Lv1-3 (leash + khoảng đệm 7-10 ô).
- Địa hình 7 khu: suối + cầu gỗ (Z3), đồi thỏ (Z2), rừng thưa nấm (Z4), bụi gai (Z5), rừng cổ (Z6), gốc cây cổ + cửa dungeon (Z7); lối Gemini ở mép đông bắc = cổng đóng (biển "Coming soon"), không làm map Gemini.
- Loot từng loài; card rơi (id quái = id card đã thêm ở phase 03).
- Quest (tách nhiều mục tiêu thành chuỗi `Requires`, KISS, không thêm hệ đa mục tiêu): T-M03 = `tau-brook-frogs` (4 frog) -> `tau-brook-mushrooms` (4 mushroom) -> `tau-brook-runestone` (chạm phiến đá rune, `WaypointId`, điểm trong `DungeonPoints`); T-M04 = `tau-forest-boars` (5) -> `tau-forest-mantis` (4) -> `tau-forest-rootlings` (3) -> `tau-forest-roots` (chạm điểm Ancient Roots). Thưởng Gold + đá theo kinh tế (EXP quest chưa có hệ: bỏ, ghi Sổ việc).

## Related Code Files
- Sửa: `MonsterDef.cs`, `Rpg/MonsterRangedRules.cs`, `Monster.Ai.cs` (reset khi Return nếu thiếu), `MapSpawnTable.cs`, `Rpg/LootTable.cs`, `Maps/TauCloverMeadowsMap.cs` + `TauCloverMeadowsProps.cs` (rows đủ 7 khu), `Rpg/TaurusQuests.cs`, `Rpg/DungeonPoints.cs`, `NpcDefs.cs` (thoại Rowan), test `TaurusMapTests`, `MonsterRangedRulesTests`, `TaurusQuestChainTests` (mới).
- Tạo: sheet 6 quái (`Resources/Sprites/monsters/tau*`), card art, `Tools/build-tau-clover-meadows-rows.py` (thay greybox).

## Implementation Steps
1. Rows đủ 96x64 theo sơ đồ zoning (sửa script greybox), flood fill, override cầu / bậc; build lại ảnh tô ô chặn, tự xem.
2. 8 def (FallbackSheet tạm) + 2 ranged profile + reset Return.
3. 10 camp + kiểm khoảng đệm bằng test (khoảng cách rect giữa camp chênh ≥ 3 bậc level ≥ 2 x leash).
4. Loot + card drop.
5. Quest chuỗi T-M03 / T-M04 + điểm chạm.
6. Art 6 quái + prop 6 khu theo pipeline phase 04 (prompt cũ, concept mới từng quái trước, sheet sau).
7. Đo TTK 3 class mốc (BIBLE XII.4), chỉnh HP.
8. Test + chụp từng camp `--god --map tau-clover-meadows --tp x,y --shots`, online 2 client.

## Làm khác kế hoạch (24/11, code + test xong, ảnh trong game ở cuối)
- Map: sửa `Tools/build-tau-greybox-rows.py` (không tạo script mới): rows 96x64 đủ 7 khu, suối ngang cả map (hàng 24-26) CHỈ qua được ở cầu gỗ (đường, x 46-49), 10 camp theo bảng BIBLE (68 quái, test đếm), tổ boss Brambleback Nest ở góc tây bắc (12x10 trống, miệng ở hàng 11), hành lang cửa dungeon x 56-59 hàng 2-15 (cửa = lintel T hàng 0-2, ô cửa hàng 2 để trống cho portal phase 06), cổng Gemini = khối T đóng ở mép đông bắc, cuối nhánh đường hàng 8-11 (CHƯA có biển "Coming soon": cần art / prop phase 04). Greybox: màu phẳng theo khu (h m b e r n), không prop; cụm cây rải bằng seed chỉ nơi cách đường / camp / tổ / điểm quest >= 3 ô. Cấp tăng từ nam lên bắc theo HÀNG y (C01 y~48 ... C09-C10 y~3), không theo quãng đường đi (đường đi vòng không đơn điệu).
- 9 `MonsterDef` APPEND (8 loài + boss `tauBramblebackMatriarch` cuối bảng; phase 06 append sau): chỉ số theo bảng BIBLE (speed = hệ số x 30,8; aggro m x 16; windup + cooldown = nhịp đánh; leash theo họ 4,5 / 5,5 / 6,5 ô). HP chỉnh theo TTK: ước lượng thuần toán (test `TaurusMeadowMonsterSpecTests`: 75 % uptime, vũ khí khởi đầu, đúng Lv camp) rơi vào dải BIBLE cho Warrior / Wizard / Cleric cả 9 loài; KHÔNG phải log đo trong game. Slime phase 02 chỉnh theo bảng (HP 32 -> 64, cooldown 2000 -> 1550: nhịp 2,0 s, TTK 2-4 s).
- Reset khi về: `MonsterDef.ResetOnReturn` (mọi sheet `tau*`): trong `Return` quái không nhận sát thương, về tới nhà thì hồi đầy HP. Quái cũ không đổi.
- Quái bắn: `MonsterRangedProfile.SporeShot` / `ThornSeed`, `Sprite = null` -> `MonsterShotParticles` (Particle System: đầu sao chổi kéo dài + hạt mờ), không sprite; chỉ khi bộ công cụ ánh sáng sẵn sàng (không thì rơi về mũi tên cũ).
- Boss map (Brambleback Matriarch, TAU-FB01): `SkillBoss` + bảng `WantedSkillTable.BramblebackMatriarch` (Bramble Charge: hình chữ nhật Line 1,4H x 4H, cảnh báo 1,1 s khoá hướng, lao 0,5 s, 12 s; Thorn Stomp: vòng 1,8H, 1,4 s, 16 s; KHÔNG có skill enrage, chỉ huy hiệu dưới 30 % HP). Cơ chế MỚI: `WantedSkillDef.ChargeMs` + `Monster.WantedCharge.cs` + `Rpg/WantedSkills/WantedChargeRules.cs` (boss lao dọc hướng đã khoá, xuyên qua hero, mỗi hero bị trúng đúng 1 lần khi thân chạm tới trong hình chữ nhật; phần thuần có test). Hồi 30 phút = `BossRespawnMs` mặc định; `HasOwnBoss` (kill lưu theo map offline); đứng giữa tổ (8,5), `Stationary`, leash 6,5 ô (không ra khỏi tổ), về nhà = hồi đầy HP. HP 1500 (hơn 3 lần Barkling = quái thường mạnh nhất, ~4 lần Boar Lv9; ChatGPT ghi 3,0 lần nhưng mục tiêu solo 30-60 s: 3 lần chỉ ra ~18 s), ATK x1,25, DEF x1,1, tốc 0,9. ART: giữa lúc làm, sheet riêng của Matriarch (ChatGPT, ô 192 đi / 256 đánh, RenderScale 2,21) đã về nên nối luôn (tạo `.meta` 4 PNG); sheet `-charge` / `-stomp` CHƯA phát (tư thế skill đứng ở khung đi), TODO nối khung skill + prop tổ. Thưởng: EXP 453 (= 3 lần boar Lv9, chia theo sát thương như mọi boss), vàng 28-58, đá 35 %, trang bị Uncommon 30 % itemLevel 10 (`MonsterLootDef.GearChance / GearRarity`), loot cá nhân (`PersonalLoot`, `KillRewards.SpawnPersonalLoot`: mỗi người có >= 1 % máu boss tự roll và sở hữu đống của mình, không cần last hit). CHƯA làm: vật liệu Bramble Hide / Matriarch Tusk (chưa có hệ vật liệu), card Matriarch (CardTable 12 lá: đụng việc album của agent khác), giới hạn channel hop (chưa có channel).
- Quest: 7 quest 1 mục tiêu trong `Rpg/TaurusMeadowQuests.cs` (Rowan, `Requires` nối từ `tau-meadow-trial`); 2 bước chạm = `WaypointId` trên điểm chạm MỚI trong `DungeonPoints.Waypoints` (`QuestOnly = true`: không hiện trong danh sách dịch chuyển, không là điểm đến warp, `WarpRefusal` từ chối): Brook Runestone (38,23) + Ancient Roots (53,9). Marker TEMP: ô trắng nhỏ (`DungeonPointView`), chưa có art. Thưởng vàng + đá, EXP quest bỏ (chưa có hệ: Sổ việc).
- Card: bỏ miễn trừ `tau*` ở `CardIdsCheck` + test loot, chỉ còn 3 id phase 06 (`Phase06Cards`: Amber Sporeling, Rune Bark Guardian, Elder Root Guardian). `.meta` của 16 PNG sheet quái do tôi tạo (sao từ meta slime, guid mới).
- Test mới: `TaurusMeadowMonsterSpecTests` (bảng BIBLE + TTK + boss), `TaurusMeadowSystemsTests` (skill / charge / loot / quest / điểm chạm), `TaurusMeadowCampsTests` (68 quái, camp trống và cách đường >= aggro, đệm theo cấp, suối chỉ qua cầu, hành lang / tổ / điểm quest, khối Gemini). Sửa `SaveResetTests` (Live 11 quest), `QuestTests.MarkerFollowsTheQuest` (Rowan còn quest kế), `TaurusMapTests` (bỏ rào tạm, có boss).
- Môi trường: ổ C: đầy (353 / 367 GB) nên `zu-copy-b` ở `D:\temp\zu-copy-b`, exe ở `D:\temp\player-taurus-b\`. Lần seed đầu tôi lỡ chép nhầm cả Library / gốc repo làm đầy C: rồi D: vài phút: đã xoá phần chép nhầm (C: còn sót `C:\temp\zu-copy-b\Library\Bee` bị Access denied, của chính tôi).
- Ảnh: `docs/reference/world-design/greybox-shots/phase05-*`: `c01..c10` (từng camp, hero god giữa camp), `boss_*` (Thorn Stomp, Bramble Charge: vòng / hình chữ nhật cảnh báo, boss lao xuyên hero), `shot-spore_*` / `shot-thorn_*` (đạn hạt), `solids-*` (ô chặn đỏ: cầu, hành lang cửa, tổ, cổng Gemini, đá rune, Ancient Roots), `online_*` (2 client vào zone Taurus), `layout-clover-meadows.png` (rows + camp). Đã xem hết. Chưa xem: camp khi hero Lv thật bị đánh (toàn bộ chụp bằng --god), minimap / World Map của map mới (phase 08).

## Todo List
- [x] Rows 7 khu + ảnh ô chặn
- [x] 9 MonsterDef append (8 loài + boss) + 2 ranged profile (hạt) + reset Return
- [x] 10 camp / 68 quái + test khoảng đệm
- [x] Loot + card drop (3 id phase 06 còn thiếu, đúng kế hoạch)
- [x] Quest T-M03, T-M04 (7 quest nhỏ)
- [ ] Art 6 quái + prop: sheet quái đã có (duyệt 24/11) và đã nối vào def; PROP / nền cuối chưa có (greybox, chờ phase 04)
- [~] TTK 3 class: ước lượng toán trong test, CHƯA đo log trong game
- [~] Chụp + online: chụp offline từng camp + boss; online 2 client CHƯA

## Success Criteria
Checklist part1 (12 mục) + mục World & Map / Monster & Combat của part3 XVIII đạt (trừ multiplayer: phase 07). 68 quái đếm đúng trong zone log.

## Risk Assessment
- Map 96x64 + 68 quái + nhiều hero: CPU zone / băng thông snapshot -> interest đã có (`InterestAndBufferTests`); đo ở phase 07.
- Index wire: chèn sai chỗ làm lệch quái client cũ -> chỉ append, test `MonsterDef.All` cuối danh sách.
- Art 6 quái tốn lượt ChatGPT: BIBLE cho phép phát hành 7 loài tốt trước -> ưu tiên Slime, Rabbit, Frog, Mushroom, Boar, Mantis, Barkling.

## Security Considerations
Spawn / damage / loot / kill credit chỉ zone; quest đếm theo credit hiện có (party chia), không theo client.

## Next Steps
Phase 06 dùng cửa dungeon Z7.

## Bổ sung người dùng 24/11 09:50: boss map
- Mỗi map săn có 1 boss map: hồi sinh ~30 phút, mạnh ~3 lần quái thường của map (HP / ATK), giết xong thưởng EXP + rơi đồ xịn hơn quái thường một chút (vd itemLevel / tier cao hơn 1 bậc hoặc tỉ lệ rơi tốt hơn; chốt khi làm).
- Boss Clover Meadows (ChatGPT 24/11): Brambleback Matriarch TAU-FB01, Lv9, 2,1H cao, ở Bramble Thicket, hồi 30 phút, Melee + Charge + AoE (Charge = AI mới). Bản đầy đủ + quy tắc boss map chung: `docs/reference/world-design/taurus-field-boss-chatgpt.txt`.
- Code: tái dùng `MapSpawnTable.BossFor` (case riêng cho map mới), cơ chế boss map cũ (respawn timer, loot) nếu có; vị trí riêng, không nằm trên đường người mới (luật camp của ChatGPT).

## Art 9 quái ngoài đồng: DUYỆT (ChatGPT 24/11 10:55, `docs/reference/world-design/taurus-6-sheets-review-chatgpt.txt`)
- Sheet ở `Assets/Resources/Sprites/monsters/taurus/<id>/<id>.png` (đi 4x4 ô 192) + `<id>-attack.png` (đánh 4x4 ô 256); id: tauCloverSlime, tauHornRabbit, tauMossMushroom, tauReedFrog, tauBrambleBoar, tauLeafMantis, tauRootling, tauThornSprite (HOVER 16 px), tauAncientBarkling.
- Ancient Barkling: sheet cao 1,13H (ô 192 không vừa) -> RenderScale ~1,06 để đạt 1,20H so Warrior trong Unity, scale đi và đánh PHẢI giống nhau; ChatGPT: chỉnh scale hiển thị, không vẽ lại.
- Ếch vàng-xanh: kiểm độ nổi trên nền cỏ Clover Meadows thật.
