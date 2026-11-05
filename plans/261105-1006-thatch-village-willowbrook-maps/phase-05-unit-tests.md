# Phase 05: Unit test

## Context
- `Tests/dotnet/ZenoniaNet.Tests/MapGraphTests.cs` duyệt `MapRegistry.All`: cổng tới map có thật, ô cổng/ô đến không đặc, ô đến đúng ô theo luật `PortalTile`, không đứng trên cổng, mọi map tới được từ Frost Hollow, id duy nhất và <= `MaxMapIdLength` (32). Map mới tự được phủ.
- `ZenoniaNet.Tests.csproj:17` biên dịch `Assets/Scripts/Maps/*.cs` (trừ MapScenery, MapTransfer) -> 2 file map mới tự vào. `MapSpawnTable.cs` không được biên dịch trong test.

## Overview
Thêm 1 test riêng cho tuyến mới; còn lại dựa vào test chung.

## Requirements
Test mới `NewEastRouteGoesBothWays` trong `MapGraphTests.cs`:
- Oakhaven có cổng tới `thatchVillage`; Thatchwick có cổng tới `oakhavenVillage` và `willowbrookMeadows`; Willowbrook chỉ có cổng tới `thatchVillage`.
- Đi một cổng rồi đứng ở ô đến: từ đó có cổng quay về map cũ (tìm cổng trong map đích có `TargetMapId == map nguồn`).
- `Hostile`: Thatchwick false, Willowbrook true.
- Mỗi map có rows đúng 24 x 36 (constructor `TileMapBase` sẽ ném `IndexOutOfRange` nếu thiếu cột; test đảm bảo bắt sớm, rẻ).

## Related code files
- Sửa: `Tests/dotnet/ZenoniaNet.Tests/MapGraphTests.cs` (chỉ file này).

## Implementation steps
1. Viết test theo Requirements, dùng `ThatchVillageMap.MapId`, `WillowbrookMeadowsMap.MapId`.
2. Chạy:
   ```
   dotnet test Tests/dotnet/ZenoniaNet.Tests
   dotnet test Tests/dotnet/ZenoniaSkills.Tests
   ```
3. Không sửa test cũ để cho qua; lỗi thì sửa dữ liệu map.

## Todo
- [x] Test tuyến đông
- [x] Hai bộ test xanh

## Success criteria
Hai lệnh test xanh; số test ZenoniaNet tăng đúng số theory case mới (+4 cặp cổng x 2 theory) + 1 fact.

## Risks
Thấp. Test không phủ hình ảnh / vị trí quái -> phase 6.

## Next
Phase 06.
