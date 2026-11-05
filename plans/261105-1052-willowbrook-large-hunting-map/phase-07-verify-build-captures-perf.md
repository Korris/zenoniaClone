# Phase 07: Kiểm tra - build bản sao, ảnh chụp, minimap, tải

## Context
- Không build trên project đang mở: `Tools/build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe ...` (thêm `-WebGL` cho bản web, `PlayerBuildScript.cs:45-55`).
- Chụp: `DebugCapture` `--map willowbrook-meadows --tp x,y --face ... --shots 2 --shot-path P --solids` (`DebugCapture.cs:13-18, 113-121`; `--map` ở `MapRegistry.cs:55-70`).
- Tải: `Tools/net-load-test.ps1` (bot ZenoniaSwarm, ngân sách tick p99 < 33 ms, <= 24 KB/s/client; mặc định `--no-portals` và mọi bot ở Frost Hollow, dòng 12, 45). Server nhận `--map` làm map xuất phát (`ZoneHost.cs:21-39`).

## Overview
Priority P1. Status pending.

## Steps
1. Build Windows từ bản sao. Chụp lưới 12 điểm (4x3: tp tại tâm mỗi ô 18x16) + cổng tây + 3 cầu + rào đông + mép section cũ (ô 34-38, hàng 11-14 và 33-37), mỗi điểm 1 ảnh thường + 1 `--solids`. Lưu `plans/.../visuals/`.
2. Tự xem từng ảnh: nối chunk (x=576, y=384 đơn vị), mép section cũ, hero đi sau tán cây, footprint khớp gốc, quái không sát cổng, minimap thấy cây/suối và chấm hero đúng chỗ khi đi tới 4 góc.
3. Đi tay: Thatch -> Willowbrook -> rào đông -> về; camera kẹp đúng 4 mép, không thấy nền đen.
4. Tải: thêm tham số `-Map <id>` vào `net-load-test.ps1` (chuyền `--map` cho server; giữ `--no-portals`). Chạy `-Map willowbrook-meadows -Steps 50,100,200 -StepSeconds 60`. Ghi tick p99, KB/s trung bình, `snapshotsOverCap` (`NetStats.cs:63`, cap 1000 B `NetOpcodes.cs:17`), RAM server. So với cùng bước ở Frost Hollow. Lưu ý: bot "fighter" có thể đi tìm quái theo toạ độ Frost Hollow -> kiểm hành vi bot trước (ZenoniaSwarm).
5. Bộ nhớ client: Profiler/`Texture memory` trên Windows build sau khi đi Oakhaven -> Thatch -> Willowbrook -> Frost Hollow: nền map cũ phải được giải phóng (P1 `SpriteSheet.Unload`). WebGL: mở trên Chrome desktop, xem RAM tab. Nếu > ngân sách (đề xuất 512 MB tab) -> thêm override WebGL cho `/tiles/` trong `ResourceTextureImportSettings.cs:62-81` (DXT1/BC: 4 chunk RGB 56 MB -> ~7 MB), áp cho mọi nền map.
6. Android (nếu còn build): chunk tự xuống 2048 ASTC 6x6 (`ResourceTextureImportSettings.cs:84-87`) = ~3.5 MB/chunk; chấp nhận nét ~57 px/ô như map khác.
7. `dotnet test` cả hai bộ; ghi kết quả + link chat vào `docs/pending-tasks.md`; người dùng xem rồi mới commit.

## Todo
- [ ] build Windows  - [ ] 12+ điểm chụp + solids  - [ ] tự xem  - [ ] đi tay  - [ ] `-Map` + tải 50/100/200  - [ ] bộ nhớ (Windows, WebGL)  - [ ] dotnet test  - [ ] pending-tasks

## Success criteria (đo được)
- 200 bot trên Willowbrook: tick p99 < 33 ms, KB/s trung bình <= 24, `snapshotsOverCap` = 0 hoặc không tăng so với Frost Hollow, không ai bị ngắt.
- Không thấy vạch nối chunk / mép section ở ảnh 1:1; minimap đúng 4 góc.
- Texture memory sau chuỗi 4 map không cộng dồn nền cũ.
- Draw call khi đứng giữa rừng (Frame Debugger/Stats) < 150 (prop mỗi loại 1 texture, ngoài màn bị cull). Nếu vượt: gom prop `wb*`/`wb2*` vào SpriteAtlas (để sau, YAGNI).

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| WebGL hết bộ nhớ | Trung bình x Cao | bước 5 (giải phóng + nén) |
| Bot không vào được vùng quái map mới -> số đo sai | Trung bình x Trung bình | chạy bot wanderer, xem log vị trí |
| Snapshot quá cap khi nhiều người đứng cụm đông | Thấp x Trung bình | cap có sẵn + ưu tiên gần; đo |

## Câu hỏi còn mở
1. WebGL/Android còn là nền tảng phát hành? (quyết định có nén nền WebGL không)
2. Protocol 14 đã phát hành chưa (bump 15 hay giữ)?
3. Người dùng muốn duyệt sketch layout (phase 2) trước khi xin art không?
4. Có muốn kênh (channel) sau này cho bãi săn - chỉ ghi nhận, ngoài phạm vi.
