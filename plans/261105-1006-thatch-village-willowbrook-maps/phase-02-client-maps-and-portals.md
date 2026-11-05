# Phase 02: Hai map client + cổng Oakhaven

## Context
- Mẫu: `Assets/Scripts/Maps/SunspireOasisMap.cs` (thị trấn an toàn), `AmberDunesMap.cs` (bãi săn), `TileMapBase.cs` (36x24, `solidChars`), `IGameMap.cs`, `MapRegistry.cs`.
- Cổng: `MapPortal(tx, ty, targetId, arriveTx, arriveTy)`; đường rộng 2 ô -> cặp cổng; `MapScenery` chỉ thắp đèn ô trên của cặp; ô đến cách cổng đích 2 ô (quy ước cũ).
- `MapRegistry.FindLaunch` bỏ dấu `-` và không phân biệt hoa thường -> `--map thatch-village` / `willowbrook-meadows` tự chạy, không cần sửa.
- Minimap, nhạc (`MapTransfer.cs:98`), nền (`WorldBuilder.cs:37`), zone server (`ZoneHost.cs:41`) đều đọc từ `IGameMap` -> không sửa.

## Overview
Thêm 2 class map, đăng ký, thêm cổng đông Oakhaven.

## Requirements
- `ThatchVillageMap`: `MapId = "thatchVillage"`, Name "Thatchwick Village", `GroundTexture = "Sprites/village/tiles/thatch-village-ground"`, Music `"oakhaven-village"`, `Hostile = false`, không Props, không Npcs, solid `"Tw"`.
- `WillowbrookMeadowsMap`: `MapId = "willowbrookMeadows"`, Name "Willowbrook Meadows", ground `.../willowbrook-meadows-ground`, Music `"frost-hollow"`, `Hostile = true`, không Props.
- Cổng (toạ độ dự kiến, thay bằng số chốt ở phase 1):
  - Oakhaven: `(35,10)`, `(35,11)` -> thatchVillage `(2,7)`, `(2,8)`. Ô cột 34-35 hàng 10-11 của Oakhaven đã là `d` -> không đổi hàng.
  - Thatchwick tây: `(0,7)`, `(0,8)` -> oakhavenVillage `(33,10)`, `(33,11)` (ô `B` cầu, đi được, không phải cổng).
  - Thatchwick đông: `(35,11)`, `(35,12)` -> willowbrookMeadows `(2,11)`, `(2,12)`.
  - Willowbrook tây: `(0,11)`, `(0,12)` -> thatchVillage `(33,11)`, `(33,12)`.
- PlayerStart: Thatchwick ở quảng trường (~`(17,13)`), Willowbrook ngay trong cổng (~`(3,11)`). Không trùng ô cổng.

## Architecture
```
MapRegistry.Maps += ThatchVillageMap, WillowbrookMeadowsMap
  -> offline: MapTransfer đọc Portals -> Instant(target) -> WorldBuilder/MapScenery/Minimap/GameMusic
  -> server: ZoneHost tạo 1 zone / map; ZoneTransferService dùng PortalsFor
  -> tests: MapGraphTests duyệt MapRegistry.All (tự phủ map mới)
```
Map mới để cuối mảng `Maps` (Maps[0] = Frost Hollow là mặc định, không đổi thứ tự).

## Related code files
- Tạo: `Assets/Scripts/Maps/ThatchVillageMap.cs`, `Assets/Scripts/Maps/WillowbrookMeadowsMap.cs` (Unity tự tạo `.meta`; nếu tạo ngoài Editor thì chấp nhận Unity sinh khi mở).
- Sửa: `Assets/Scripts/Maps/OakhavenVillageMap.cs` (thêm 2 cổng, sửa summary "South gate" thành mô tả cả 2 cổng), `Assets/Scripts/Maps/MapRegistry.cs` (thêm 2 map; cập nhật doc comment `ParseLaunch`).
- Không sửa: `IGameMap.cs`, `TileMapBase.cs`, `MapScenery.cs`, `MapTransfer.cs`, `Minimap.cs`, `WorldBuilder.cs`.

## Implementation steps
1. Copy khung `SunspireOasisMap.cs` -> `ThatchVillageMap.cs`; bỏ PropList/NpcList và vòng `BlockTiles` (không có prop/NPC); dán rows từ `rows/thatch-village-rows.txt`; summary ghi rõ "nhà, giếng, sạp, cầu vẽ sẵn trong backdrop, rows là va chạm duy nhất".
2. Copy khung `AmberDunesMap.cs` -> `WillowbrookMeadowsMap.cs`; bỏ PropList; rows từ txt; summary ghi "MapSpawnTable keys on MapId", "ngõ cụt, đường đông để dành".
3. Oakhaven: thêm 2 `MapPortal` vào `PortalList`.
4. `MapRegistry.Maps`: thêm `new ThatchVillageMap(), new WillowbrookMeadowsMap()` cuối mảng.
5. Biên dịch: `dotnet test Tests/dotnet/ZenoniaNet.Tests` (biên dịch `Maps/*.cs`); với Unity đang mở, đọc lần biên dịch mới nhất trong `%LOCALAPPDATA%\Unity\Editor\Editor.log`.

## Todo
- [x] ThatchVillageMap.cs
- [x] WillowbrookMeadowsMap.cs
- [x] Cổng đông Oakhaven
- [x] Đăng ký MapRegistry
- [x] Biên dịch sạch

## Success criteria
- ZenoniaNet.Tests xanh (MapGraphTests chạy cả cổng mới: ô cổng / ô đến không đặc, không đứng trên cổng, mọi map tới được từ Frost Hollow).
- Editor.log không lỗi mới.

## Risks
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Ô đến rơi vào ô cổng của map đích -> nảy qua lại | TB x Cao | test `NeitherAStartTileNorAnArrivalTileIsATrigger` |
| Willowbrook không có case trong MapSpawnTable giữa phase 2 và 3 -> có Frostfang King | Cao x TB | làm phase 3 ngay, cùng PR; không build/phát hành giữa chừng |

## Security
Không liên quan.

## Next
Phase 03.
