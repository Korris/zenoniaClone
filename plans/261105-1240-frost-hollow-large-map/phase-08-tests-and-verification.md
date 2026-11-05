# Phase 08: Test + kiểm tra

## Context
- Test map: `Tests/dotnet/ZenoniaNet.Tests/` biên dịch `Assets/Scripts/Maps/*.cs` với stub (`ZenoniaNet.Tests.csproj:17`, `MapGraphStubs.cs`); `MapSpawnTable.cs` không biên dịch được ở đây -> đọc bằng regex như `WillowbrookLargeMapTests.cs:80-89`.
- `MapSizeTests.cs:53-54` khẳng định map mặc định (Frost Hollow) là 36x24 -> **sẽ fail**, đổi thành 72x48.
- `MapGraphTests.cs:102-104` InlineData kiểm thông mạch + hàng dưới HUD chỉ có Thatch/Willowbrook -> thêm Frost Hollow. `MapGraphTests.cs:142-158` (Den gate) dùng ô Den, không đổi. Test portal chung (dòng 21-98) tự phủ cổng mới.
- `Editor/FrostSmokeTest.cs:69-91` (start, boss post, (18,0)/(0,0) đặc, cầu (19,12), qua cầu từ (17,11), bờ sông chặn): toạ độ ô cũ, phải còn đúng sau khi tách prop/sửa rows.
- Tải: `Tools/net-load-test.ps1:12,45` (mặc định `--no-portals`, mọi bot ở Frost Hollow - zone giờ ~75 quái thay 18), `Tools/net-local-test.ps1:10`. Build: `Tools/build-from-copy.ps1` (không build trên project đang mở).

## Overview
Priority P1. Sau P6.

## Test mới: `FrostHollowLargeMapTests.cs`
1. 72x48, chunk 2x2, `GroundTexture` đúng.
2. Toạ độ cũ còn đi được: start (4,21), cổng làng (2,21), King (7,5) + ô quanh, (11,6), cầu (17-21,12); hàng 45-47 và cột 70-71 đặc trừ (71,30-32).
3. Cổng: (2,21)->Oakhaven (16,19); (71,30-32)->Den (2,10); Den BackPortals -> (69,31) đi được; không còn portal tại (33,2).
4. Flood fill từ (4,21) phủ mọi ô đi được và tới (71,31).
5. Zone (regex `FrostHollowOuter =` trong MapSpawnTable.cs + `Zones =` trong FrostHollowMap.cs): zone cũ đúng 5 dòng và giá trị cũ; cụm mới 8-12, tổng mới 45-65, hồi 7000-11000, rect trong map, >= 2x SL ô đi được, cách start/cổng làng/cổng Den/ô đến >= 80 đơn vị; zone cũ cũng cách mọi lối vào >= 80 (vì bỏ miễn trừ NearEntrance).
6. Prop không chặn ô cấm (đọc `IsSolidTile` sau ctor).
7. Sửa `MapSizeTests.cs:53-54`; thêm FH vào InlineData `MapGraphTests.cs:102-104`.
8. ZenoniaSkills: test migrate save (P2).

## Kiểm tra thủ công
1. Build Windows từ bản sao. Chụp (`--map frost-hollow --tp x,y --solids`): cổng làng + bậc tây nam, mép khu cũ (34-38, 5-12) và (0-12, 22-26), tháp canh, hồ, cầu 2, hang Den, 4 góc, King. Tự xem từng ảnh: seam, hero đi sau tán, footprint khớp gốc, không vùng T trống tán.
2. Đi tay: Oakhaven -> FH -> Den -> FH (đến (69,31)) -> về làng; camera kẹp 4 mép.
3. Save cũ: dùng một save v2 đứng giữa khu cũ (offline + online `net-persist-test.ps1`) -> vào đúng chỗ cũ.
4. Tải: `net-load-test.ps1 -Steps 50,100,200`: tick p99 < 33 ms, <= 24 KB/s/client, `snapshotsOverCap` (`NetStats.cs:63`); ghi baseline mới (zone FH nhiều quái hơn). Bot NetNav vẫn tuần tra 5 zone cũ.
5. HeadlessBench (`Net/HeadlessBench.cs`): số đo đổi vì spawner FH nhiều quái hơn -> ghi baseline mới.
6. Bộ nhớ: Windows/WebGL sau chuỗi Oakhaven -> FH -> Den -> Willowbrook: nền map trước được unload.
7. FrostSmokeTest (Editor, trên bản sao).
8. `dotnet test` cả hai bộ; cập nhật `docs/pending-tasks.md` (kết quả, link chat, phần chưa xem trong game); người dùng xem rồi mới commit.

## Todo
- [ ] FrostHollowLargeMapTests  - [ ] MapSizeTests + MapGraphTests  - [ ] chụp + tự xem  - [ ] đi tay  - [ ] save cũ  - [ ] tải 50/100/200  - [ ] bench  - [ ] bộ nhớ  - [ ] FrostSmokeTest  - [ ] pending-tasks

## Success criteria
Hai bộ dotnet test qua; tải 200 bot đạt ngân sách; save v2 về đúng ô cũ; không thấy seam/vùng lỗi ở ảnh 1:1.

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Tick server vượt ngân sách với ~75 quái | TB x TB | giảm SL cụm xa / tăng hồi; quái không mục tiêu tốn ít |
| Bot load test gặp quái Lv7-9 | Thấp x Thấp | NetNav chỉ đi zone cũ |

## Câu hỏi còn mở
1. Cấp quái mới: pending-tasks ghi Lv7-9, brief ghi Lv6-10 -> chốt 7/8/9? Có cần bậc giữa Lv3-5 (khoảng trống Lv2 -> Lv7)?
2. Cổng Den mép đông (71,30-32) trong miệng hang có được không, hay muốn góc đông nam hẳn (vd (71,40-42))?
3. Bậc đông bắc cũ (33,2) thành lối lên gờ tháp canh: đồng ý?
4. Protocol 14 đã phát hành chưa (bump 15)? Save v3 + server-api phát hành cùng lúc được không?
5. Xoá `Sprites/tiles/frost-hollow-ground.png` khỏi Resources sau khi xong (Willowbrook vẫn để file cũ `willowbrook-meadows-ground.png`, cũng nên dọn)?
6. Có muốn đổi map xuất phát load test (`-Map`) không, hay giữ FH làm ca nặng nhất?
