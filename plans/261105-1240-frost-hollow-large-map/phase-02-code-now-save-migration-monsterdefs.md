# Phase 02: Code làm ngay - migrate save, NearEntrance, 3 MonsterDef băng

## Context
- Save lưu toạ độ thế giới y-up: `Rpg/SaveSerializer.cs:16-17` (`x, y, hasPosition`), `SaveVersion = 2` (dòng 32), Parse từ chối version > SaveVersion (dòng 62). Dùng lại: offline `RpgSession.cs:104-112`, online `Net/Server/ServerHeroFactory.cs:33-36` (cả hai có `RectBlocked` fallback).
- `TileFeet` = `MapH - ((ty+1)*16 - lift)` (`FrostHollowMap.cs:39-42`): khi MapH 384 -> 768, ô cũ (tx,ty) có y thế giới +384.
- server-api: `server-api/src/rpg/item-validation.js:34` `version max(2)` -> save v3 bị từ chối nếu không sửa.
- `MapSpawnTable.NearEntrance` (`MapSpawnTable.cs:128-135`) miễn Frost Hollow bằng so sánh tham chiếu `ZonesFor(map.Id) == FrostHollowMap.Zones`; sau P6 ZonesFor(FH) trả mảng ghép -> so sánh này hết đúng.
- Quái: `MonsterDef.cs:62-77` (đường Lv: HP 14+22L, Atk 3+4L, Def 1+L, EXP 14L, gold L..2L+1), `All` append-only (wire index, dòng 74), `FallbackSheet/FallbackTint` (`Monster.Visuals.cs:14-21`); loot `Rpg/LootTable.cs:29+`.

## Overview
Priority P1. Không cần art. Phần NearEntrance + MonsterDef commit được ngay; phần migrate save viết + test ngay nhưng **commit chung với P6**.

## Requirements / steps
1. **Migrate save** (`SaveSerializer.cs`): `SaveVersion = 3` (comment "3: Frost Hollow 72x48, old section y +384"); trong Parse: `if (raw.version < 3 && d.mapId == MapId && d.hasPosition) d.y += 384f;` (24 hàng x 16; hằng có tên `FrostHollowGrowUnits`). Áp cho cả offline và server (cùng Parse). server-api `item-validation.js:34` max(3) + test server-api (`server-api/test/characters.test.js` nếu kiểm version).
2. **NearEntrance**: bỏ dòng miễn trừ 130. Lý do: zone cũ đã cách start/cổng làng > 80 đơn vị (zone gần nhất (8,13,5,4) cách (4,21) ~6.4 ô); zone goblin (25,5) gần cổng Den cũ (33,2) chỉ ~3 ô nhưng cổng đó bị xoá ở P6. Kiểm: P8 test tính khoảng cách mọi zone FH tới mọi lối vào. Trước P6, bỏ miễn trừ làm zone goblin/slime (25,5) mất vài ô gần (33,2) - vô hại.
3. **3 MonsterDef** (append vào `All` sau BrambleTreant, cùng đường Lv):
   - `iceWraith` Lv7 "Ice Wraith": nhanh, giòn (HP 134, Atk 37, Def 4, speed 66, aggro 100, cooldown 900) - Fallback `spectralWolf`, tint xanh nhạt.
   - `snowYeti` Lv8 "Snow Yeti": nặng (HP 266, Atk 42, Def 9, speed 34, windup 600, cooldown 1500, hitbox rộng, RenderScale 1.2) - Fallback `iceGoblin`, tint trắng.
   - `crystalGolem` Lv9 "Crystal Golem": tank (HP 382, Atk 43, Def 16, speed 26, windup 700, cooldown 1800, RenderScale 1.25) - Fallback `frostSlime`, tint lam.
   - `ByType` thêm 3 case; `LootTable` thêm 3 dòng (Exp 98/112/126, gold L..2L+1, ItemLevel = L).
   - `NetOpcodes.cs:13` sửa comment v14 (thêm "ice monsters"); không bump nếu 14 chưa phát hành.
4. Server dùng chung bảng: build lại zone server khi phát hành.

## Todo
- [ ] SaveVersion 3 + dịch y + test ZenoniaSkills (save v2 FH y=34 -> 418; v2 map khác không đổi; v3 không đổi)  - [ ] server-api max(3) + test  - [ ] bỏ miễn trừ NearEntrance  - [ ] 3 MonsterDef + ByType + loot  - [ ] dotnet test 2 bộ

## Success criteria
Test: save v2 FH dịch đúng 384; Parse save v3 không dịch hai lần; `MonsterDef.ByType` trả 3 loại mới, index cũ không đổi; Unity biên dịch (đọc Editor.log lần mới nhất).

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Migrate vào trước map mới -> save 36x24 bị dịch | TB x TB | commit chung P6 (chỉ ảnh hưởng máy dev) |
| Client cũ (v2) đọc save v3 -> null | Thấp x Cao | client + server + server-api phát hành cùng lúc (như protocol 14) |
| Seed `net-persist-test.ps1:273-320` (`--x 1 --y 1`) | Thấp x Thấp | (1,1) và (1,385) đều rơi ô đặc -> vẫn về lối vào |

## Security
Parse vẫn sanitize như cũ; chỉ cộng hằng vào y đã `Finite`.

## Next
P6 (spawn dùng loại mới), P7 (sheet thật thay fallback).
