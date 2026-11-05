# Phase 05: Bảng quái 11 cụm (70 con)

## Context
`Assets/Scripts/MapSpawnTable.cs`: mảng `WillowbrookMeadows` (4 cụm, 21 con, hồi 8-11 s), `NearEntrance` loại điểm cách cổng/start < 80 đơn vị (dòng 13, 115-123), `TryZoneSpot` bỏ ô đặc (`FrostHollowMap.cs:62-74`, 30 lần thử). `BossFor` Willowbrook = null (giữ).

## Overview
Priority P2. Status pending. Thay mảng bằng 11 cụm của `layout/willowbrook-72x48-clusters.md` (bảng ở phase-02), hồi 6000-10000 ms, gần cổng dễ, xa cổng đông hơn và hồi chậm hơn.

## Key insights
- 3 loại có sẵn chỉ Lv1-2 -> "độ dốc" tạm thời bằng mật độ + tỉ lệ sói/goblin; độ dốc cấp thật cần phase 8.
- Rect rộng có ô đặc: `TryZoneSpot` thử 30 lần; mỗi rect phải có >= 2x SL ô đi được (P6 kiểm) để không hụt quái.
- Tâm rect cụm 1 cách cổng/start > 80 đơn vị (5 ô) - `NearEntrance` vẫn giữ làm lưới an toàn.

## Implementation steps
1. Thay mảng `WillowbrookMeadows`, sửa doc comment (dòng 50).
2. Không đổi `EntranceClearance` (80 đơn vị = hơi hơn aggro mặc định).
3. Server dùng chung bảng: build lại zone server (`Tools/build-linux-server.ps1` khi phát hành).

## Todo
- [ ] mảng 11 cụm  - [ ] comment  - [ ] dotnet test

## Success criteria
P6 test: tổng 50-80, 8-12 cụm, hồi 6000-10000, rect trong map, đủ ô trống, không ô nào của cụm 1 trong 80 đơn vị từ cổng. Ảnh chụp P7: không quái sát cổng.

## Risk
70 quái tỉnh cùng lúc khi zone thức: chi phí tick server - đo ở P7 (zone ngủ khi không người: `ZoneIdleClock`).
