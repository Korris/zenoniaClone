# Phase 04: Code map 72x48 - rows, ~180 prop, cổng, chunk

## Context
- Cần P1 (size theo map, chunk), P2 (rows), P3 (ảnh chunk, prop wb2*).
- File hiện: `Assets/Scripts/Maps/WillowbrookMeadowsMap.cs` (rows 36x24, PropList 45, cổng `(0,11)/(0,12)` -> Thatch `(33,11/12)`, start `(3,11)`), `ThatchVillageMap.cs:49` (cổng đông -> Willowbrook `(2,11)/(2,12)`), `VillagePropDefs.cs` (71 dòng, mục Willowbrook từ dòng 51).

## Overview
Priority P1. Status pending.

## Requirements
- Rows 48x72 từ `layout/willowbrook-72x48-rows.txt`; solidChars `"Tw"` giữ; thêm `d` vào bảng ký tự (không đặc).
- `GroundTexture` = `Sprites/village/tiles/willowbrook-meadows-ground`, `GroundChunkCols/Rows` = 2/2.
- Cổng: `(0,23)/(0,24)` -> Thatch `(33,11/12)`; start `(3,23)`. Thatch: đích `(2,23)/(2,24)`.
- PropList: 45 prop cũ dịch `ty + 12` (trừ prop bỏ đi khi mở mép đông/bắc nếu chắn đường) + ~130-180 prop mới (viền rừng dùng cụm cây lớn để ít instance; dải cây c12, c19; đá; bụi; phế tích; rào chắn đông).
- File < 200 dòng: tách PropList sang `WillowbrookMeadowsProps.cs` (`partial class`), map file giữ rows + cổng.
- `VillagePropDefs`: thêm 15-25 entry `wb2*` (snippet từ script 2).
- Xoá `willowbrook-meadows-ground.png` cũ khỏi Resources sau khi chunk chạy (chuyển sang `ArtLibrary/.../large/source-west-section.png` bằng `git mv` kèm `.meta`); script P3 đọc từ chỗ mới.

## Implementation steps
1. Đổi rows, cổng, start, chunk; sửa đích cổng Thatch.
2. Tạo `WillowbrookMeadowsProps.cs`, dịch prop cũ, thêm prop mới theo sketch (đặt bằng bảng trong P2 + `check` của script 1 để xem footprint).
3. Thêm PropDef `wb2*`.
4. Chạy lại script 1 bước bóng + minimap (đọc PropList).
5. Protocol: nếu v14 đã phát hành trước khi phase này xong -> `NetOpcodes.cs:13` tăng 15 (client cũ có Willowbrook 36x24 sẽ lệch va chạm); nếu chưa -> giữ 14, phát hành cùng.
6. `dotnet test` (MapGraphTests thông mạch phải qua) + Unity biên dịch.

## Todo
- [ ] rows/cổng/start/chunk  - [ ] cổng Thatch  - [ ] props file + prop mới  - [ ] PropDef wb2*  - [ ] bóng + minimap  - [ ] protocol quyết định  - [ ] test

## Success criteria
Map dựng không LogError prop lạ; `check` overlay: mọi footprint nằm trên gốc vật; MapGraphTests: cổng thông với mọi ô đi được; Thatch <-> Willowbrook khứ hồi đúng ô.

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Prop cũ ở mép (cột 34-35, hàng 0-1 cũ) giờ chắn giữa map | Trung bình x Thấp | rà lại khi mở viền |
| Cây không có bóng nhìn "dán" | Cao x Thấp | bóng vẽ vào nền (bước 4) |
| Quên đổi đích cổng Thatch -> hero vào giữa rừng | Thấp x Cao | test khứ hồi P6 |

## Rollback
Revert commit -> map 36x24 cũ (cần khôi phục ảnh nền cũ: nằm trong cùng commit nhờ `git mv`).
