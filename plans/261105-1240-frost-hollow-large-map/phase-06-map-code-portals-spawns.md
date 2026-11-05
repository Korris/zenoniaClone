# Phase 06: Code map 72x48 - rows, chunk, cổng Den, prop, cụm quái

## Context
- `Maps/FrostHollowLayout.cs`: rows 14-39, portal 45-49 (`(2,21)->Oakhaven (16,19)`, `(33,2)->Den (2,10)`), start (4,21) dòng 52, ground dòng 59, music 60.
- `FrostHollowMap.cs:23-31` zone cũ + `BossTile (7,5)`; TileFeet 39-42.
- `Maps/FrostfangDenMap.cs:58` BackPortals Den -> FH `(33,4)` (3 trigger cột 0 hàng 9-11).
- `Maps/OakhavenVillageMap.cs:78` cổng làng -> FH `PlayerStartTile` (4,21): không đổi.
- `MapSpawnTable.cs`: `ZonesFor` default -> `FrostHollowMap.Zones` (dòng 83; chỉ Frost Hollow rơi vào default trong các map thật: Amber/Den/Emberfen/Reliquary/Willowbrook có case riêng, Oakhaven/Thatch/Sunspire không hostile - `MapTransfer.cs:97`); `BossFor` default King, `BossTileFor` default (7,5).
- Mẫu: `Maps/WillowbrookMeadowsMap.cs` (rows 48 hàng, chặn footprint trong ctor dòng 75-83, chunk 2x2 dòng 87-89).

## Overview
Priority P1. Cần P4 (rows, chunk), P5 (prop), P2 (MonsterDef). Commit chung phần migrate save của P2.

## Steps
1. `FrostHollowLayout`: rows 48x72 từ `layout/frost-hollow-72x48-rows-painted.txt` (solid "TCw" giữ). `GroundTexture` = `Sprites/tiles/frost-hollow-large-ground`, `GroundChunkCols/Rows` = 2/2. Ctor chặn footprint prop như Willowbrook; `Props` => `FrostHollowProps.All`. Cập nhật doc comment (dòng 5-10, 41-44).
2. Portal: giữ `(2,21)->Oakhaven`; xoá `(33,2)`; thêm `(71,30),(71,31),(71,32) -> Den (2,10)`. Den `BackPortals` -> FH `(69,31)` (ô đến cách portal 2 ô, quy tắc "arrival 2 ô vào trong").
3. `MapSpawnTable`: thêm `const string FrostHollowId = "frostHollow"`; mảng `FrostHollowOuter` (10 cụm phase-01, chỉnh rect theo rows thật); `ZonesFor`: `case FrostHollowId: return FrostHollowAll;` (`FrostHollowMap.Zones` + `FrostHollowOuter`, ghép lười như `WillowbrookMeadowsProps.All`); default giữ `FrostHollowMap.Zones` (stub/test). Sửa doc comment dòng 5-9.
4. `FrostHollowMap.cs`: Zones giữ nguyên 5 zone cũ (NetNav `DebugCapture.NetNav.cs:89`, `Net/HeadlessBench.cs:56`, `Editor/FrostSmokeTest.cs:77` dùng thẳng -> không đổi hành vi bot/bench); sửa comment "36x24" -> "khu cũ ở góc tây bắc".
5. Ảnh nền cũ `Sprites/tiles/frost-hollow-ground.png`: không còn dùng -> sau P8, `git rm` (bản giống hệt đã có ở `ArtLibrary/hd/map/frost-hollow/frost-hollow-ground-original.png`; kiểm hash trước) để Resources khỏi nặng 14 MB.
6. Music giữ `frost-hollow`. Minimap tự lấy `-minimap`.
7. Protocol: 14 chưa phát hành -> giữ (sửa comment). Đã phát hành -> 15 (client 36x24 cũ sẽ lệch va chạm/toạ độ).
8. `dotnet test` 2 bộ + Unity biên dịch.

## Không cần đổi (đã rà, toạ độ ô giữ)
`DebugCapture.Boss.cs:27,40`, `DebugCapture.Choreo.cs:92-97`, `DebugCapture.Combo.cs:21`, `DebugCapture.cs:254,287` (TileFeet (11,6), `--tp`), `DebugCapture.NetPersist.cs:229,268,305,422`, `QuestLog.cs:31` (giết 5 frostSlime, khu cũ có 10), `GameSession.Online.cs:74,89`, `NetLaunchArgs.cs:46,93` (zone-id), `Tools/net-persist-test.ps1:273-393`, `Tools/play-online.ps1:51`. `DebugCapture.NetTurret.cs:139,172` dùng ngưỡng x thế giới (80, 400) chỉ để chọn hướng đi trong khu cũ - vẫn đúng. `NetNav` mảng dist cỡ Max 72x48 (`DebugCapture.NetNav.cs:15-16`).

## Todo
- [ ] rows + chunk + props  - [ ] portal Den mới + Den BackPortals  - [ ] FrostHollowOuter + ZonesFor  - [ ] comment  - [ ] protocol quyết định  - [ ] test + biên dịch

## Success criteria
Map dựng không LogError prop; Oakhaven -> FH đến (4,21); FH (71,31) -> Den (2,10) -> FH (69,31) khứ hồi; King vẫn ở (7,5); MapGraphTests qua.

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Quên Den BackPortals -> về FH vào ô (33,4) giữa gờ | Thấp x Cao | test khứ hồi P8 |
| Map test lạ rơi vào default lấy zone FH | Thấp x Thấp | default chỉ zone cũ (trong 36x24) |
| Prop chặn bậc/cầu/hốc cổng | TB x TB | ô cấm P5 + flood fill test P8 |

## Rollback
Revert commit = FH 36x24 (ảnh nền cũ còn tới khi P8 xong; migrate save revert cùng commit).
