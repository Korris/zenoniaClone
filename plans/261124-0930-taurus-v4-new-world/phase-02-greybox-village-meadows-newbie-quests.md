# Phase 02 (P0b): Greybox Willowbrook Village + Clover Meadows (C01-C02), NPC, quest Rowan / Bram

## Context Links
- **Đọc hết `docs/map-design-rules.md`** (đường trống, bãi quái, 2.5D điều 1-17, cổng `const int`, kiểm tra trước khi báo xong).
- BIBLE part1 IV (làng 44x32, 5 khu, 6 NPC, luật an toàn), V (Clover Meadows 96x64, 7 khu, sơ đồ camp, khoảng đệm), VI MON-T01, VII; part2 V Q01-Q03A; part3 XII.1 (C01 chỉ phản công), XIV.
- Mẫu code: `Maps/OakhavenVillageMap.cs` (làng, `NpcList`, `BlockTiles`), `Maps/ScorpionGorgeMap.cs` (map săn), `MapSpawnTable.cs` (`ZonesFor` l.207, `BossFor` l.231), `NpcDefs.cs`, `Rpg/QuestLog.cs`, `Rpg/Enhance.cs` (+1..+3 chắc chắn thành công, 1 đá / lần), `Rpg/LootTable.cs`, `View25D/View25D.cs:25`.

## Overview
P1. Dựng 2 map greybox chạy được cả offline lẫn online, đủ vòng tân thủ: tạo nhân vật -> Rowan -> 5 Clover Slime -> 5 đá + 300 G -> Bram nâng cấp lần đầu. Art tạm (màu phẳng / FallbackSheet), thay ở phase 04-05.

## Key Insights
- Map = class C# (`TileMapBase` + `Rows` + ký tự chặn), rows sinh bằng script Python. 96x64 lớn hơn mọi map hiện có (72x48): ground chia chunk `GroundChunkCols/Rows` 3x3 hoặc 4x4; zone server chỉ chạy map có hero nên chi phí CPU chấp nhận được, đo ở phase 07.
- Camp = `FrostHollowMap.Zone(type, count, rect, respawnMs)`; camp hỗn hợp = 2 Zone cùng rect. Leash = `MonsterDef.LeashRange` từ điểm sinh (đv; 1 m = 16 đv). Level 1 loại quái 1 mức (C01 / C02 cùng def Slime; BIBLE "Lv 1 / Lv 2" gộp lại, KISS).
- "Chỉ phản công" chưa có: thêm `MonsterDef.Passive` (aggro = 0 tới khi bị đánh) trong `Monster.Ai.cs`; C01 dùng def `tauCloverSlime` passive? -> 1 def chỉ có 1 cờ: tạo theo camp bằng cờ trên Zone (`Zone.Passive`) thay vì def thứ 2 (không tốn index wire / card).
- Quest có sẵn đủ cho Q02 (`MonsterType`, `RewardStones`, `RewardGold`). Q01 "đi qua cổng" gộp vào thoại Rowan của Q02 (KISS, bỏ 1 quest). Q03A cần mục tiêu mới `Enhance`.
- Đá: 5 đá đủ 3 lần chắc chắn +1..+3 (Enhance hiện tại) -> "lần đầu 100 %" đã đúng, không cần code bảo đảm.

## Requirements
- Map `tauWillowbrookVillage` 44x32, an toàn (`Hostile=false`), 5 khu quanh quảng trường cây sồi; cổng nam -> Clover Meadows. NPC (art tạm = `DefArt` của NPC cũ, ghi TODO phase 04): Elder Rowan (GIỮ id `elderRowan`, đổi tên hiển thị "Elder Rowan", QuestGiver), Card Master Orin (GIỮ id `cardMaster` để code card / `CardMasterTalk` / `DuelService.Npc` (kiểm khoảng cách tới Orin) khỏi đổi; vị trí mới trong Card Corner), Bram the Smith (`tauBram`, mở Enhance), Lumi (`tauLumi`, Pet Gacha), Mira (`tauMira`, shop), Gatekeeper Finn (`tauFinn`, thoại chỉ đường).
- Map `tauCloverMeadows` 96x64, `Hostile=true`, cổng nam về làng ở mép nam; P0 chỉ khu Z1 Cloverfield có C01 (9 Slime, 12 s, passive) + C02 (7 Slime, 12 s); phần còn lại là cỏ trống / chặn tạm.
- `tauCloverSlime` APPEND cuối `MonsterDef.All` + `ByType`; melee, speed 0,65x, cooldown 2000 ms, aggro 2 m (32 đv), leash 4-5 m (~72 đv); TTK 2-4 s với Warrior Lv1 T0 (đo). Loot `LootTable` (EXP / Gold thấp, đá 5 %).
- `MapSpawnTable`: case `ZonesFor` cho 2 map (làng = rỗng), `BossFor` = null (không rơi về Frostfang King).
- Quest (file mới `Rpg/TaurusQuests.cs`): `tau-meadow-trial` (Rowan, AutoStart KHÔNG; Rowan có dấu từ đầu; 5 `tauCloverSlime` -> 5 đá + 300 G), `tau-first-upgrade` (Bram, Requires trial, mục tiêu `Enhance` 1 lần thành công; đã có món +1 lúc nhận thì xong ngay; thưởng 100 G).
- Làng không spawn quái, không PvP, skill không gây sát thương (dùng `Hostile=false` có sẵn).
- 2.5D: thêm 2 id vào `View25D.Maps`; rừng viền `build-map-25d-ground.py <map> border`.

## Architecture
Client + zone dùng chung `Assets/Scripts` -> đăng ký `MapRegistry.Live` là zone host được. Quest tiến độ + thưởng do zone (`InvOp.QuestAccept/TurnIn`). Enhance đã chạy ở zone -> zone gọi `PlayerRpg.QuestEnhanced()` sau lần thành công.

## Related Code Files
- Tạo: `Assets/Scripts/Maps/TauWillowbrookVillageMap.cs`, `Maps/TauCloverMeadowsMap.cs` (+ `*Props.cs` khi có prop), `Rpg/TaurusQuests.cs`, `Tools/build-tau-greybox-rows.py` (rows + nền màu phẳng + minimap), `Tests/dotnet/ZenoniaNet.Tests/TaurusMapTests.cs`, `Tests/dotnet/ZenoniaSkills.Tests/TaurusNewbieQuestTests.cs`.
- Sửa: `Maps/MapRegistry.cs` (Live), `MonsterDef.cs`, `MapSpawnTable.cs`, `FrostHollowMap.cs` (`Zone.Passive`), `Monster.Ai.cs`, `Rpg/LootTable.cs`, `NpcDefs.cs`, `Rpg/QuestLog.cs` (`QuestDef.Enhance`, `Counts`), `Rpg/PlayerState.Quests.cs`, `World/PlayerRpg.cs`, chỗ zone xử lý Enhance (`Net/Server/ServerInventoryActions.cs`), `View25D/View25D.cs`, `TownNpc.cs` / `ElderNpc.cs` nếu cần mở Enhance từ Bram.

## Implementation Steps
1. Script greybox: rows 44x32 / 96x64 theo sơ đồ BIBLE (đường chính nam-bắc, quảng trường, suối Z3 để dành), viền chặn, 3 hàng đáy chặn, flood fill; nền 2 màu (cỏ #88A85D / đường #CBA679) + minimap.
2. 2 map class, cổng `const int` hai chiều, `PlayerStart` làng gần quảng trường.
3. `tauCloverSlime` + `Zone.Passive` + AI chỉ phản công (unit test thuần nếu tách được rule, không thì capture).
4. Spawn C01 / C02 (cách cổng ≥ 5 ô, ô trống ≥ 2x số quái), loot.
5. NPC + quest `tau-meadow-trial`, `tau-first-upgrade`, mục tiêu `Enhance`.
6. Test: `TaurusMapTests` (kích thước, portal hai chiều đi được, flood fill, rect không đè đường / cổng), `TaurusNewbieQuestTests` (5 kill -> 5 đá + 300 G đúng 1 lần, kill khi chưa nhận không đếm, Enhance xong quest, đã +1 trước thì xong ngay).
7. Build bản sao, chụp `--map tau-willowbrook-village --solids --shots`, đi cổng, đánh C01, online 2 client (`Tools/play-online*.ps1`).

## Làm khác kế hoạch (24/11, đã làm xong)
- Rows sinh bằng `Tools/build-tau-greybox-rows.py` (dán giữa `// BEGIN ROWS` / `// END ROWS` trong 2 class). Nền greybox = màu phẳng 16 px/ô, 1 chunk (làng 704x512, đồng 1536x1024) + `-border.png` màu phẳng; KHÔNG prop (nhà / cây / lều = ô chặn tô màu xám / xanh đậm). Rừng viền 2.5D vẫn dùng cây `wf*` cũ (TEMP, phase 04 thay).
- Meadows: chỉ Z1 (hàng 38-60) đi được; phía bắc hàng 0-37 là rào tạm `x`; đường dừng ở hàng 38. Cổng làng = hàng 60 (4 ô), làng cổng nam = hàng 28 (tường nam hàng 27-28).
- `GameConfig.MaxTilesW/H` 72x48 -> 96x64 (cả stub test).
- Zone.Passive thêm làm tham số cuối của `Zone(...)` (struct); `Monster.Passive` + `provoked` (Monster.Ai.cs); đo trong game: đứng 30 s giữa C01 không mất máu, C02 mất máu.
- `MonsterDef.SheetFile` (đường dẫn sheet trong thư mục con `taurus/tauCloverSlime/`) tách khỏi `Sheet` (= id loại quái cho loot / quest); sheet thật đã nối, FallbackSheet frostSlime phòng thiếu. Tôi tạo `.meta` cho 2 PNG slime (sao từ hornRabbit, guid mới).
- NPC: `tauBram` (shop gear 0-1 qua `GearShops` + quest; xong quest thì `ElderNpc.Talk` trả false để mở shop), `tauMira` (shop thuốc, `PotionShops`), `tauLumi` (chỉ thoại, gacha đóng tới phase 08), `tauFinn` (thoại). Bram KHÔNG có cửa sổ Enhance riêng: nâng cấp ở túi như cũ.
- Quest Enhance: `QuestDef.Enhance`, `QuestLog.CountEnhance`, `PlayerState.HoldsEnhancedPiece` (đã +1 khi nhận thì xong ngay), gọi từ `PlayerRpg` (offline) + `ZonePersistence.OnEnhanceAnswer` (online, chỉ khi API báo thành công). Thưởng Bram 100 G, không EXP riêng.
- Sửa `QuestTests.Hero()` (làm xong trial của Rowan trước vì chung giver khi bật cả 2 thế giới trong test) và `SaveResetTests` (Live = 2 quest).

## Todo List
- [x] Script greybox + 2 map class + 2.5D + border
- [x] tauCloverSlime (append), Zone.Passive, loot
- [x] Spawn C01/C02, BossFor null
- [x] 6 NPC (art tạm)
- [x] Quest trial + first-upgrade (+ mục tiêu Enhance)
- [x] Test + chụp offline + online

## Success Criteria
Nhân vật mới online: ở làng, Rowan có dấu quest; ra cổng nam gặp Slime C01 không tự đánh; 5 kill -> trả quest +5 đá +300 G; Bram -> nâng +1 thành công, quest xong. Ảnh tô ô chặn không có ô chặn trên đường / bãi. Test xanh.

## Risk Assessment
- Map 96x64 đè giới hạn: `MapSizeTests`, minimap, `WorldMapData` giả định ≤ 72x48? -> chạy test sớm, sửa giới hạn.
- Nhiều người cùng C01 (BIBLE XIV): 9 Slime có thể thiếu -> số đo ở phase 07, tăng count nếu cần.
- Art tạm dùng sprite NPC cũ trái quyết định "không tái dùng": chỉ tạm, phase 04 thay; ghi rõ trong code `// TEMP art`.

## Security Considerations
Kill credit + thưởng do zone; quest Done chặn nhận lặp (có sẵn). Enhance mục tiêu chỉ đếm khi zone xác nhận thành công.

## Next Steps
Phase 03 (Orin) dùng map + quest trial làm điều kiện; phase 05 lấp đủ map.
