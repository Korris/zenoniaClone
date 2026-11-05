# Phase 03: Bảng quái Willowbrook

## Context
- `Assets/Scripts/MapSpawnTable.cs`: `ZonesFor` / `BossFor` / `BossTileFor` / `NearEntrance` / `HasOwnBoss` / `HasBossGate` / `GatePassedOn`.
- **Bẫy**: `ZonesFor` mặc định trả `FrostHollowMap.Zones`, `BossFor` mặc định trả `FrostfangKing`. Map hostile mới phải có case riêng.
- Quái Lv thấp có sẵn (`MonsterDef.cs`, `LootTable.cs`): `frostSlime` (Lv1, 14 exp), `iceGoblin` (Lv2, 28 exp), `spectralWolf` (Lv2, 26 exp). Không có quái Lv3-5.
- `FrostHollowMap.Zone(type, count, tx, ty, w, h, respawnMs)`; `TryZoneSpot` bỏ qua ô đặc nên vùng được phép rộng.

## Overview
Willowbrook: 3 loại quái trên, không boss, không cổng boss, giữ khoảng trống quanh cổng vào.

## Requirements
- Thêm `WillowbrookMeadowsId = "willowbrookMeadows"` (hoặc dùng `WillowbrookMeadowsMap.MapId` như `GatePassedOn` đang dùng `SunspireOasisMap.MapId`; chọn một, nhất quán với const sẵn có -> thêm vào dòng const).
- Vùng (dự kiến, chỉnh theo rows phase 1; tổng ~16 con, ít hơn Frost Hollow 18 vì là map đầu):
  - frostSlime 5 ở đồng cỏ tây bắc ~`(3,3, 9x5)`
  - frostSlime 4 ở đồng cỏ giữa nam ~`(10,13, 12x6)`
  - iceGoblin 4 ở đồng cỏ đông bắc (bên kia suối) ~`(25,2, 8x6)`
  - spectralWolf 3 ở khoảng đông nam gần lán ~`(28,12, 6x5)` hoặc giữa nam nếu chỗ đó toàn `T/w`.
  - respawn 8000-11000 ms như Frost Hollow.
- `BossFor(willowbrook) = null`, `HasOwnBoss` false, `HasBossGate` false, `GatePassedOn` không đổi (map nằm trước Den).
- `NearEntrance` phải áp dụng cho Willowbrook (cổng tây nằm sát đồng cỏ).

## Architecture
Đơn giản hoá DRY: `NearEntrance` hiện liệt kê 4 id; đổi thành "map có bảng quái riêng" = `ZonesFor(map.Id) != FrostHollowMap.Zones`. Map mới sau này tự được bảo vệ, không phải sửa thêm danh sách. `BossFor`: thêm `WillowbrookMeadowsId` vào nhánh `return null` cạnh Amber/Emberfen.

## Related code files
- Sửa: `Assets/Scripts/MapSpawnTable.cs` (chỉ file này). Cập nhật summary đầu class.

## Implementation steps
1. Thêm const id + mảng `WillowbrookMeadows` có summary mô tả vị trí từng vùng.
2. `ZonesFor`: case mới. `BossFor`: thêm vào nhánh null.
3. `NearEntrance`: đổi điều kiện như trên (hành vi 4 map cũ giữ nguyên, Frost Hollow vẫn trả false).
4. Biên dịch (Editor.log). Kiểm số con và vị trí ở phase 6 bằng ảnh chụp.

## Todo
- [x] Const + vùng quái
- [x] ZonesFor / BossFor
- [x] NearEntrance refactor
- [x] Biên dịch sạch

## Success criteria
- Vào Willowbrook offline: không boss, ~16 quái, không con nào trong 80 đơn vị quanh cổng tây / ô bắt đầu.
- Frost Hollow, Amber Dunes vẫn đúng số quái như trước (FrostSmokeTest 19 = 18 + boss không đổi).

## Risks
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| "Lv 1-5" nhưng chỉ có Lv1-2 | Chắc chắn x Thấp | ghi rõ; Lv3-5 cần MonsterDef mới (câu hỏi mở) |
| Quái băng trên đồng cỏ xanh lệch không khí | Cao x Thấp | người dùng đã chọn dùng lại; tint sau nếu muốn |
| Vùng toàn ô đặc -> quái không sinh | TB x TB | chọn vùng theo rows đã chốt; đếm quái ở phase 6 |

## Security
Server dùng cùng bảng (zone server biên dịch file này) -> phải build lại server (phase 4).

## Next
Phase 04, 05.
