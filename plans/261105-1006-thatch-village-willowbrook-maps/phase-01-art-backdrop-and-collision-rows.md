# Phase 01: Backdrop và hàng va chạm

## Context
- Art gốc: `Assets/ArtLibrary/hd/map/thatch-village/thatch-village-chatgpt.png`, `Assets/ArtLibrary/hd/map/willowbrook-meadows/willowbrook-meadows-chatgpt.png` (1536x1024 RGB).
- Mẫu: `Assets/Resources/Sprites/village/tiles/*-ground.png` đều 2304x1536; `WorldBuilder.cs:37` tạo sprite với PPU = `tex.width / MapW` nên chỉ cần đúng tỉ lệ 3:2.
- Import tự động: `Assets/Editor/ResourceTextureImportSettings.cs` (path có `/tiles/` -> bilinear, không mip, tối đa 4096).
- Tiền lệ hàng va chạm: `plans/261030-2130-world-expansion-design/*-rows.txt`. Chưa có script upscale nào trong `Tools/` (map cũ làm tay).
- Trước khi làm: đọc `docs/sprite-rules.md` (quy định CLAUDE.md cho mọi việc art).

## Overview
Priority P1 trong plan (chặn mọi phase sau). Tạo 2 backdrop + 2 bộ 24 hàng x 36 ký tự.

## Key insights
- 1536/36 = 42.7 px/ô ở ảnh gốc. Cửa nhà ~65-80 px = 1.5-1.9 ô; hero thấy ~18-20 đơn vị (khung 24) ~ 1.2 ô -> cửa cao hơn hero một chút, hợp lý.
- Ước lượng từ lưới (sẽ chốt khi phân loại):
  - Thatchwick: đường tây ra mép ở cột 0 hàng 7-8; đường đông ra mép cột 35 hàng 11-12; đường bắc (cột 14-16) và nam (cột 14-18) và góc đông bắc qua cầu -> chặn ở mép (không có cổng). Suối + cầu gỗ cột 27-35 hàng 0-5. Giếng ~cột 17-19 hàng 8-11, sạp ~cột 6-11 hàng 8-11.
  - Willowbrook: hàng rào cọc cột 0-2 hàng 7-13, cổng gỗ cột 2 hàng 9-11, đường ra mép tây cột 0 hàng 11-12; cầu gỗ ~cột 23-27 hàng 9-11; suối chéo từ cột 20 hàng 0 xuống cột 35 hàng 18; lán + đống rơm cột 23-30 hàng 17-21; đường đông ra mép cột 35 hàng 9-10 (để dành, không cổng).
- Hàng 21-23 dưới HUD: luôn `T` toàn bộ (Thatchwick có đường nam chạy xuống đó -> chặn, như Oakhaven).

## Requirements
- Backdrop 2304x1536, RGB/RGBA PNG, ở `Assets/Resources/Sprites/village/tiles/`.
- Hàng: đúng 24 x 36, chỉ dùng `T g d p w B`; ô cổng và ô đến phải đi được; bóng nhà/giếng/sạp/xe/hàng rào/đá lớn/thân + tán cây che đất là `T`; bậc cửa trước nhà để đi được tới sát cửa.
- Mọi vùng đi được phải liên thông với ô bắt đầu (flood-fill).

## Architecture / data flow
```
ArtLibrary/*-chatgpt.png --(upscale 1.5x Lanczos)--> Resources/Sprites/village/tiles/<id>-ground.png --> WorldBuilder (ground) + Minimap
                         --(grid preview 36x24, 4 góc phóng to)--> phân loại bằng mắt --> rows/<id>-rows.txt
rows.txt + backdrop --(overlay tô màu ô đặc + flood-fill)--> ảnh kiểm tra --> dán vào Map.cs (phase 2)
```

## Related code files
- Tạo: `Tools/build-map-backdrop.py` (một script, 3 lệnh: `backdrop`, `grid`, `check`).
- Tạo: `Assets/Resources/Sprites/village/tiles/thatch-village-ground.png`, `willowbrook-meadows-ground.png` (Unity tự tạo `.meta` khi mở; build bản sao không cần meta).
- Tạo: `plans/261105-1006-thatch-village-willowbrook-maps/rows/thatch-village-rows.txt`, `willowbrook-meadows-rows.txt`, ảnh `rows/*-check.png` (xem, không bắt buộc commit).

## Implementation steps
1. Viết `Tools/build-map-backdrop.py` (Pillow, <200 dòng, docstring ghi cách dùng):
   - `backdrop <src> <dst>`: mở, `resize((2304,1536), LANCZOS)`, lưu PNG (giữ RGB như `oakhaven-ground.png`). Kiểm tra src đúng 3:2, sai thì báo lỗi.
   - `grid <src> <out-dir>`: vẽ lưới 36x24 có số cột/hàng lên ảnh, xuất ảnh toàn bộ + 4 góc (mỗi góc 18x12 ô, phóng 2x) để nhìn rõ khi phân loại.
   - `check <backdrop> <rows.txt> <out.png>`: kiểm tra 24x36 + ký tự hợp lệ, tô đỏ mờ ô `T`/`w`, xanh ô `B`, in ra ô đi được không nối với ô bắt đầu (tham số `--start tx,ty`) và danh sách ô mép đi được (gợi ý chỗ cổng).
2. Chạy `backdrop` cho 2 ảnh.
3. Chạy `grid`, xem từng góc, viết `rows/*.txt` theo luật ở Requirements.
4. Chạy `check`, xem ảnh overlay từng góc; sửa tới khi bóng nhà/vật khớp, đường cổng mở, flood-fill sạch.
5. Ghi toạ độ cổng + ô đến + ô bắt đầu chốt được vào cuối mỗi `rows/*.txt` (phase 2 dùng).

## Todo
- [x] `Tools/build-map-backdrop.py`
- [x] 2 backdrop 2304x1536
- [x] `thatch-village-rows.txt` + check sạch
- [x] `willowbrook-meadows-rows.txt` + check sạch
- [x] Chốt toạ độ cổng / start

## Success criteria
- `python Tools/build-map-backdrop.py check ...` không báo lỗi cho cả 2 map; ảnh overlay đã xem 4 góc.
- Backdrop đúng 2304x1536.

## Risks
| Rủi ro | Khả năng x Tác động | Giảm thiểu |
|---|---|---|
| Phân loại sai mép nhà -> đi xuyên tường / kẹt ở cửa | Cao x TB | overlay + chụp `--solids` ở phase 6 |
| Phóng 1.5x làm mờ | TB x Thấp | chấp nhận (map cũ cũng từ 1536x1024); không dùng AI upscale (YAGNI) |
| Vật cao bị hero vẽ đè khi đi phía sau | TB x TB | đánh `T` thêm 1 hàng phía bắc tán cây / mái nếu nhìn lỗi |

## Security
Không liên quan.

## Next
Phase 02 dùng rows + toạ độ.
