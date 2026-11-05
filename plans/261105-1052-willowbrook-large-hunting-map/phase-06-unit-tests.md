# Phase 06: Unit test

## Context
`Tests/dotnet/ZenoniaNet.Tests` biên dịch thật `Assets/Scripts/Maps/*.cs` (csproj dòng 17) với stub `MapGraphStubs.cs`; `MapGraphTests.cs` đã có flood fill thông mạch (dòng 96-116) và kiểm Willowbrook ngõ cụt (74-78). `MapSpawnTable.cs` chưa được biên dịch trong project test -> kiểm dữ liệu cụm cần thêm `<Compile Include>` cho `MapSpawnTable.cs` (cần stub `MonsterDef`, `Mathf/Random`?) hoặc copy bảng cụm sang file dữ liệu thuần không phụ thuộc Unity. Chọn: thêm include + stub tối thiểu; nếu stub quá nhiều thì tách `WillowbrookSpawnZones` thành file dữ liệu thuần (chỉ `FrostHollowMap.Zone[]`).

## Overview
Priority P2. Status pending. File mới `Tests/dotnet/ZenoniaNet.Tests/WillowbrookLargeMapTests.cs` (không sửa file test của phase khác).

## Test cases
1. Willowbrook `Width == 72`, `Height == 48`, 3 hàng cuối đặc hết.
2. Khứ hồi cổng: Thatch đông -> ô đích Willowbrook đi được và cách cổng tây 2 ô; Willowbrook tây -> Thatch tương tự.
3. Cụm: 8-12 cụm, tổng 50-80, hồi 6000-10000 ms, rect nằm trong map, mỗi rect >= 2x SL ô đi được, ô đi được của cụm gần cổng không nằm trong 80 đơn vị (dùng `TileFeet` stub).
4. Mọi prop có PropDef, footprint trong map, không đè ô cổng/start.
5. Các map 36x24 cũ vẫn `Width 36/Height 24` (chống hồi quy P1).

## Todo
- [ ] include/stub  - [ ] 5 nhóm test  - [ ] `dotnet test` cả hai bộ

## Success criteria
Hai bộ test qua, không test cũ nào bị tắt.
