# Phase 06 (P4): Ancient Root Shrine + Elder Root Guardian

## Context Links
- **Đọc hết `docs/map-design-rules.md` (điều 19-21 dungeon 2.5D, vách là prop đứng) + `docs/sprite-rules.md` (boss, VFX không sprite).**
- BIBLE part2 I-III (3 khu 24x22 / 28x24 / 30x28, hành lang 3-5 ô, 18 quái + 1 boss, Memory Amber, boss 3 kỹ năng + Awakening), VI T-M05 + cảnh kết; part3 XIII (timing, telegraph, arena, chết / thoát).
- Code có sẵn: `MonsterDef.SkillBoss` + `Rpg/WantedSkills/WantedSkillTable.cs` (Ambercap Warden: Amber Pulse Circle, Spore Fan Cone...), `WantedSkillArea` (Circle / Cone / Line), enrage < 30 % (`WantedSkillClock.EnrageHpFraction`), `Monster.WantedTelegraphVisuals.cs`, `WantedSkillTelegraphFx.cs`, `Rpg/DungeonPoints.cs` (pha lê dịch chuyển, rương), `ThornheartChamberMap` (cổng boss theo từng hero, `PortalsFor(gateOpen)`), `MapSpawnTable.BossFor/BossTileFor/HasOwnBoss/BossRespawnMs`.

## Overview
P2. 1 map dungeon, 2 quái mới, boss dùng lại hệ SkillBoss, quest kết chương.

## Quyết định (KISS)
- **1 map** `tauAncientRootShrine` (~72x48): Area 01 -> 02 -> 03 tuyến tính, hành lang 3-5 ô (BIBLE). Không làm 3 tầng như Rootburrow (HỎI người dùng vì luật map 21).
- **Không instance:** map công cộng; quái thường respawn chậm (literal `120000f`), boss = `BossFor` + respawn 5 phút (`DungeonBossRespawnMs`) như các dungeon hiện có. "Cổng đá mở sau 3 Rune Bark Guardian" = cổng sảnh boss theo hero có sẵn nếu tái dùng được; không thì cổng mở theo quest step.
- **Boss = SkillBoss mới**, bảng trong `WantedSkillTable`: Root Slam = đòn thường (windup 500 ms); Ancient Sweep = Cone 120°, cảnh báo 1,2 s, ~10 s; Amber Pulse = Circle quanh boss, 1,5 s, ~16 s; Awakening = enrage 30 % có sẵn, giảm khoảng cách kỹ năng 10-15 % (thêm hệ số nếu enrage hiện chỉ đổi icon). Màu cảnh báo nâu đỏ theo luật đỏ = boss.
- **Checkpoint:** 1 pha lê dịch chuyển trước sảnh boss (`DungeonPoints`) -> chết không phải dọn lại 18 quái.
- **Memory Amber:** điểm chạm (`WaypointId`) ở Area 02 + đoạn thoại ngắn; cảnh 13 ngôi sao = panel thoại + 1 ảnh ChatGPT (không cutscene động). Memory Amber Fragment = cờ quest, không làm item (YAGNI; item không giao dịch chưa có hệ).
- Profile ánh sáng mới `View25DProfile.RootShrine` (ấm hổ phách / rêu, sáng hơn `DarkDungeon`, đèn theo tên prop `tauAmberShroom`, `tauRune*`).

## Requirements
- Quái: `tauAmberSporeling` (ranged, profile `AmberSpore`), `tauRuneBarkGuardian` (melee chậm HP cao), `tauElderRootGuardian` (boss 3,8H, `WalkCellPx` 384 / `AttackCellPx` 512, Stationary tới khi aggro) APPEND cuối.
- Spawn theo bảng part2 II (6 + 9 + 3 = 18) + boss arena ~30x28 tròn, spawn hero phía nam, cửa thoát không bị che.
- Quest T-M05 chuỗi: `tau-shrine-enter` (chạm cửa / pha lê Area 01) -> `tau-shrine-amber` (chạm Memory Amber) -> `tau-shrine-guardian` (hạ boss, `MonsterType` boss) -> `tau-shrine-report` (về Rowan, thoại 13 sao + Orin nhắc Gemini). Boss card SR rơi 3 % (CardDrops boss).
- Mục tiêu độ khó: solo Lv10 T0 +vài: 90-150 s, 2 đòn AoE né được.

## Related Code Files
- Tạo: `Maps/TauAncientRootShrineMap.cs`, `Maps/TauAncientRootShrineProps.cs`, `Tools/build-tau-ancient-root-shrine-rows.py`, art (`Resources/Sprites/monsters/tauElderRootGuardian*`, quái, prop, nền dungeon), `docs/reference/taurus-v4-art/shrine/`, test `TaurusShrineMapTests.cs`, `TaurusBossSkillTests.cs`.
- Sửa: `MonsterDef.cs`, `Rpg/WantedSkills/WantedSkillTable.cs` (+ hệ số enrage nếu cần `WantedSkillClock.cs`), `Rpg/MonsterRangedRules.cs`, `MapSpawnTable.cs` (ZonesFor, BossFor, BossTileFor, HasOwnBoss, BossRespawnMs), `Maps/MapRegistry.cs` (Live), `Maps/TauCloverMeadowsMap.cs` (cổng Z7), `Rpg/DungeonPoints.cs`, `Rpg/TaurusQuests.cs`, `Rpg/LootTable.cs`, `View25D/View25D.cs` + `View25DProfile.cs`, `DungeonLights` theo tên prop, `BossPortraits.cs`.

## Implementation Steps
1. Rows dungeon (3 phòng + hành lang), cổng 2 chiều với Clover Meadows Z7, flood fill, ảnh ô chặn.
2. 3 def + profile ranged + spawn + boss tile.
3. Bảng skill boss + test hình vùng / timing (`TaurusBossSkillTests`: Cone 120° trúng trước, không trúng sau; Circle; enrage dưới 30 %).
4. Pha lê checkpoint + Memory Amber + quest chuỗi.
5. Art: concept boss đa góc trước (part3 XV.6), sheet, VFX boss bằng Particle / shader theo luật VFX; telegraph dùng `WantedSkillTelegraphFx` có sẵn.
6. Profile ánh sáng; chụp `--record-choreo` / boss capture có sẵn (`DebugCapture.Boss.cs`), `--god`.
7. Đo thời gian solo 3 class; chỉnh HP / sát thương.

## Làm khác kế hoạch (24/11, code + test xong, ảnh trong game ở cuối)
- **Map 96x64, không phải ~72x48:** ba phòng BIBLE (24x22 + 28x24 + ellipse 30x28) xếp thẳng đứng cần 74 hàng, mà `GameConfig.MaxTilesH` = 64 => xếp ngang: Area 01 tây nam (x 2-25, y 38-59) -> hành lang A (4 ô) -> Area 02 giữa (x 30-57, y 36-59, Memory Amber 2x2 ở giữa) -> hành lang B (4 ô, phía đông, pha lê checkpoint ở vách bắc) -> tiền sảnh 3 Rune Bark Guardian (x 67-82, y 33-49) -> cổ (y 30-32) -> Sanctuary ellipse 30x28 (tâm 75,5 / 15,5), boss ở nửa bắc (75,10), hero vào từ phía nam. Rows sinh bởi `Tools/build-tau-ancient-root-shrine-rows.py` (kiểm flood fill, hành lang 3-5 ô, camp, 18 quái; vẽ ground phẳng + ảnh bố cục). Greybox: màu phẳng, KHÔNG prop; viền map tạm dùng prop vách Rootburrow (`ApronTall`).
- **Không làm cổng đá** (cửa sảnh boss mở khi hạ 3 guardian): chỉ có cổng theo từng hero của Rootburrow, không có ô chặn động chung; 3 Rune Bark Guardian đứng ngay trước cổ vào sanctuary. Ghi Sổ việc nếu muốn cổng thật.
- **Quái:** 3 `MonsterDef` APPEND (Amber Sporeling Lv9, ranged `MonsterRangedProfile.AmberSpore` hạt, không sprite; Rune Bark Guardian Lv10 chậm tank; Elder Root Guardian Lv10 `SkillBoss`). HP Elder chỉnh theo test `TaurusShrineSystemsTests` (Warrior 108 s / Wizard 116 s / Cleric 142 s, 75 % uptime, ước lượng toán, CHƯA đo trong game). 18 quái + respawn `120000f` literal (map công cộng); boss respawn `DungeonBossRespawnMs` (5 phút); `HasOwnBoss`.
- **Boss kỹ năng** (`WantedSkillTable.ElderRootGuardian`): Ancient Sweep cone 120 độ bán kính 5 ô, cảnh báo 1,2 s, 10 s; Amber Pulse vòng 5,5 ô, 1,5 s, 16 s; Awakening = skill `Enrage` + cờ MỚI `WantedSkillDef.Transition` (cảnh báo 1,0 s, không vẽ vùng, không gây sát thương, cooldown 1 giờ = một lần) + hệ số MỚI `MonsterDef.EnrageRate` (0,87: khoảng cách giữa skill và cooldown ngắn 13 % sau khi thức tỉnh; mọi boss khác = 1, không đổi). `WantedSkillClock.Reset()` khi boss về nhà hồi đầy (Awakening chơi lại). Root Slam = đòn thường (windup 0,5 s).
- **Tư thế skill (cũng cho Matriarch):** `WantedSkillDef.PoseSheetSuffix` + `Rpg/WantedSkills/WantedSkillPose.cs` + `Monster.WantedTelegraphVisuals.TryPoseSheetFrame`: sheet -charge / -stomp / -sweep / -pulse / -awaken (4x4 như sheet đánh, hàng xuống / lên / trái / phải), F1-F2 trong lúc cảnh báo, F3 lúc phát động / trúng (250 ms), F4 giữ tới hết. Skill dạng vòng quay mặt về phía hero khi khoá (không thì lộ lưng).
- **Art Elder:** cắt lại ô 384 / 512 (`CELL_MUL = 2` trong `Tools/build-taurus-monsters.py`) từ bản gốc ChatGPT 1536x1024: bản 192 phóng 3,4 lần thì mờ. `WalkCellPx` 384 / `AttackCellPx` 512, `RenderScale` 3,34. Ảnh so sánh `greybox-shots/phase06-elder-sharpness-old192-vs-new384.png`. Ghi Sổ lỗi sprite.
- **Quest T-M05 = 3 quest** (`Rpg/TaurusShrineQuests.cs`, nối `Requires` sau `tau-forest-roots`): `tau-shrine-enter` (chạm pha lê cổng) -> `tau-shrine-amber` (chạm Memory Amber, điểm QuestOnly) -> `tau-shrine-guardian` (hạ boss). Bước "báo cáo Rowan + thoại 13 sao + Orin nhắc Gemini" gộp vào lời trả quest thứ 3 (không có quest rỗng); thoại Memory Amber = dòng ReadyNotice, KHÔNG cutscene / ảnh. Memory Amber Fragment = cờ quest, không item.
- **Checkpoint:** 2 pha lê (`DungeonPoints`: cổng vào + trước sảnh boss) + hàng thoát "Clover Meadows (outside)" (`ShrineExitId`); danh sách dịch chuyển chỉ hiện pha lê cùng dungeon (`SameDungeon`), Rootburrow không lẫn. Chết = hồi ở cổng vào, chạm pha lê 1 lần để quay lại sảnh boss.
- **Card:** boss card Elder rơi 3 % (CardDrops boss có sẵn); bỏ nốt `Phase06Cards` ở `CardIdsCheck` và miễn trừ trong test loot. Thêm card Matriarch ngoài Album (phần Part A, xem `docs/pending-tasks.md` nếu ghi): `CardTable.Count` 53, `LiveStart` = 40, vùng `tauFieldBoss`, rơi 2 %.
- **Ánh sáng:** `View25DProfile.RootShrine` (ambient hổ phách #8A734A 0,55, sáng hơn Rootburrow, sương rêu) + đèn Memory Amber / cổ rune / lõi sanctuary trong `DungeonLights.Extra`.
- Map `View25D.Maps`, `MapRegistry.LiveMaps`, cổng 2 chiều với Clover Meadows (cửa hàng 2, x 56-59; const `DoorArrival`, `ArrivalTx/Ty`).
- Test: `TaurusShrineMapTests` (Net), `TaurusShrineSystemsTests` (Skills), sửa `SaveResetTests` (Live 14 quest), `CardTableTests`, portal tests Meadows. Ảnh `greybox-shots/phase06-*`.

## Todo List
- [x] Rows + cổng (cổng đá: KHÔNG làm, xem trên)
- [x] 2 quái + boss def
- [x] Bảng skill boss + test
- [x] Checkpoint, Memory Amber, quest T-M05 (3 quest)
- [~] Art boss / quái / dungeon: sheet quái + boss + 3 skill sheet đã nối (Elder cắt lại 384 / 512); nền / prop dungeon là greybox, art môi trường làm riêng
- [x] Profile RootShrine
- [~] Đo solo 90-150 s: ước lượng toán trong test (108-142 s), CHƯA đo trong game

## Success Criteria
Checklist part2 VIII mục dungeon / boss + part3 XVIII "Dungeon & Boss" đạt (trừ "spawn theo phiên": thay bằng respawn chậm đã chốt). Telegraph đúng hình vùng sát thương server (test).

## Risk Assessment
- Boss 3,8H trên arena 30x28 chật -> tăng arena, không thu kỹ năng (part3 XIII.1).
- Map công cộng: người khác dọn trước, người sau đi qua dễ -> chấp nhận (BIBLE cho phép đổi chính sách khi không instance).
- Enrage hiện có thể chỉ đổi hiển thị -> thêm hệ số khoảng cách kỹ năng, cập nhật test Wanted cũ.

## Security Considerations
Boss skill, hit, kill credit, rơi card do zone. Quest boss đếm cho người tham gia (credit có sẵn), không lặp thưởng.

## Next Steps
Phase 07 playtest party đánh boss.
