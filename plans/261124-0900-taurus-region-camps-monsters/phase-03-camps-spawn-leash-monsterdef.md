# Phase 03: Camp / spawn / leash / MonsterDef

## Context Links
- **Đọc hết `docs/map-design-rules.md`** (điều 5-6 bãi quái, "Đổi bảng quái, map, protocol").
- `Assets/Scripts/MapSpawnTable.cs` (`WillowbrookMeadows`, `BramblewoodForest`), `Assets/Scripts/MonsterDef.cs`, `Assets/Scripts/Monster.Ai.cs` (leash từ điểm sinh), `Assets/Scripts/Rpg/MonsterRangedRules.cs`, `Assets/Scripts/FrostHollowMap.cs` (`Zone`).

## Overview
P1. Code + data, không art (quái mới dùng `FallbackSheet` + tint tới khi phase 02 xong). Số liệu theo production sheet; bảng dưới là nháp.

## Camp nháp (Willowbrook 72x48, cấp tăng theo đường đi từ cổng tây)
| Camp | Vị trí (cụm cũ) | Quái | Lv |
|---|---|---|---|
| Đồng Cỏ Ba Lá | cụm 1 (5,14), 2 (13,27) cạnh cổng | Clover Slime (meadowSlime) | 1 |
| Đồi Thỏ | cụm 3 (25,15), 4 (5,3), 5 (28,3) | Horn Rabbit | 3 |
| Bờ Suối | dọc suối hàng 26-38 + cụm 8 (46,31) trên hồ | Water Slime, Reed Frog | 4-5 |
| Rừng Nấm | cụm 6, 7 (giữa bắc), 9 (58,4), A (58,16), B (61,31) | Moss Mushroom (+ Horn Rabbit xen ở 6/7) | 6 |
Bramblewood: Rừng Gai = khoảnh tây bắc + đông bắc (mantis 6, wolf 7, Bramble Boar elite 8-9, ít con), Gốc Cây Cổ = khoảnh nam (Root Guardian 8, thornSprite 9, Thornfang Alpha giữ) cạnh cửa hang Rootburrow.

## Requirements
- Zone rect ≥ 2x số quái ô trống, cách cổng / điểm xuất hiện / chỗ đến ≥ 5 ô; không rect nào trên đường (điều 1, 5).
- Leash: dùng `MonsterDef.LeashRange` (từ điểm sinh, mặc định 150 đv ~ 9 ô). Camp kề nhau cách < 2 x leash thì hạ leash quái camp cao (vd 110) để không kéo sang camp thấp. Không thêm hệ SpawnZone mới (Zone đã là SpawnZone).
- Kích hoạt AI theo khoảng cách / chunk streaming: KHÔNG làm (YAGNI; zone không có hero đã pause).

## Implementation Steps
1. `MonsterDef.cs`: đổi `Name` (Clover Slime, Bramble Boar, Root Guardian, Elder Root Guardian); thornBoar lên Lv8-9 theo đường HP 14+22L / Atk 3+4L / Def 1+L x hệ số elite (production sheet), `RenderScale` ~1.3.
2. Thêm 3 def MỚI, APPEND cuối `MonsterDef.All` (không chèn: index là byte wire), `ByType` case. Moss Mushroom: `Ranged = MonsterRangedProfile` mới (bào tử chậm, như `CliffHarpyFeather`). `FallbackSheet` + tint tới khi có sheet.
3. `MapSpawnTable.WillowbrookMeadows` + `BramblewoodForest`: viết lại Zone theo camp, comment tên camp. Respawn: thường 6-10 s, elite ~90 s (comment cũ "Elites (90 s) are not in the game yet").
4. Đổi tên trong text: quest `MonsterName`, card name/desc, pet name (phase 05), comment map.
5. `NetProtocol.Version` bump nếu 35 đã phát hành; chưa thì gộp + ghi chú vào comment.
6. Kiểm ảnh tô ô chặn + rect (`Tools/preview-willowbrook-large-composite.py --minimap`), test map / spawn có sẵn (regex đọc rect trong Tools / tests).

## Related Files
Sửa: `MonsterDef.cs`, `MapSpawnTable.cs`, `Rpg/MonsterRangedRules.cs`, `Rpg/LootTable.cs` (phase 05), test spawn trong `Tests/dotnet/*`.

## Todo
- [ ] Đổi tên + restat thornBoar
- [ ] 3 def mới append
- [ ] Zone Willowbrook + Bramblewood
- [ ] Leash camp kề
- [ ] Ảnh tô ô chặn + rect, dotnet test

## Success Criteria
Đi từ cổng tây: cấp quái tăng dần không nhảy cóc; ảnh rect không đè đường / cổng; quái camp cao không đuổi vào camp thấp (thử `--map willowbrook-meadows --tp`).

## Risk
- Lv Willowbrook lên 1-6: save hero đang đứng giữa map gặp quái mạnh hơn: chấp nhận (map test).
- Thorn Boar rời Willowbrook: deck tutorial / reward quest (`thornBoar`) vẫn đúng id.
- `ServerHeroFactory` dev deck liệt kê id: không đổi.
