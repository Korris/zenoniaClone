# Phase 04: Ghép nền 72x48, chunk, minimap, rows

## Context
- Mẫu: `Tools/build-willowbrook-large-painted.py` (match màu `match_color_with`, `min_cut_mask`, `ramp`, `over`, `save` 2x2 chunk + minimap 432x288), `Tools/build-willowbrook-large-rows.py` (rows từ tranh + rows cũ dán), `Tools/preview-willowbrook-large-composite.py`.
- Engine đã hỗ trợ: `IGameMap.GroundChunkCols/Rows` (`Maps/IGameMap.cs:52-55`), ghép tên `GroundTexture-r{r}c{c}` (`WorldBuilder.cs:49`), unload (`WorldBuilder.cs:38-46`), minimap ưu tiên `-minimap` (`Minimap.cs:127-139`, map chunk thiếu -> cảnh báo). Import: `/tiles/` = không mipmap, Android 2048 ASTC (`ResourceTextureImportSettings.cs:47, 62-92`) - áp cho `Sprites/tiles/` như `village/tiles/`.

## Overview
Priority P1. Cần A0-A7.

## Script 1: `Tools/build-frost-hollow-large-painted.py` (mới, < 200 dòng)
- DRY: import helper từ `build-willowbrook-large-painted.py` bằng importlib (cách `cut-willowbrook-extra-props.py` import `build-meadow-monsters.py`). Nếu helper cần chỉnh thì tách ra `Tools/large-map-paint-helpers.py` và sửa cả script Willowbrook import theo (chạy lại Willowbrook, so ảnh không đổi).
- Lệnh `--canvas <mảnh>`: ghép khung đầu vào cho ChatGPT từ các mảnh đã có (phần chưa có tô #808080 hoặc trắng), lưu `large/<mảnh>-canvas-input.png`.
- Lệnh build: canvas 3072x2048. Lớp (sau -> trước): A0 tại (0,0) chính xác < E1 (match màu theo vùng chồng với A0, blend FEATHER ở x1440-1536) < S1 (match với A0 dải y512-1024, blend y≥928) < SE1 < S2 < SE2 < FA < FB; seam min-cut ở chỗ hai mảnh mới gặp nhau (E1|SE1 ngang, S1|SE1 dọc, SE1|SE2, S2|SE2, cột x2048-2560 với FA/FB). Khu cũ (x<1536, y<1024) luôn là A0 (trừ dải feather mép phải/dưới).
- Ra: `Assets/Resources/Sprites/tiles/frost-hollow-large-ground-r{0,1}c{0,1}.png` (2304x1536 RGB), `frost-hollow-large-ground-minimap.png` (432x288; sau P5 vẽ thêm prop thu nhỏ), `--preview` ảnh seam vào `plans/.../visuals/`.

## Script 2: `Tools/build-frost-hollow-large-rows.py` (mới)
- Khu cũ: rows cũ (`layout/frost-hollow-36x24-old-rows.txt`) đã sửa theo đánh dấu P1 (T/C rời -> s); mép cũ (hàng 22-23, cột 34-35) lấy từ phân loại tranh.
- Khu mới, phân loại theo ô (64x64 px trên ảnh 4608x3072): sông/hồ băng `w` = lam/cyan (B > R+30, G > R) >= 35%; vách `C` = đá xám tối (độ sáng thấp, bão hoà thấp) >= 40%; còn lại `s`. Cầu `B` và bậc đá: tay (file override).
- File override tay `layout/frost-hollow-72x48-overrides.txt` (ô -> ký tự) để sửa chỗ phân loại sai (đá lát tháp canh xám nhưng đi được, như Willowbrook dòng `frac[2:13,52:68]=0`).
- Viền: hàng 45-47 T, cột 70-71 T trừ (70-71, 30-32), cột 0 T từ hàng 22 trở xuống; hốc cổng làng hàng 22-23 cột 0-1 T.
- Ra `layout/frost-hollow-72x48-rows-painted.txt` + `--overlay` lên nền (`visuals/rows-painted-overlay.jpg`).

## Steps
1. Script 1 `--canvas` cho từng mảnh theo thứ tự P1 (phục vụ P3).
2. Build khi đủ mảnh; xem seam 1:1 (mỗi đường nối 1 ảnh crop), tự xem; đặc biệt mép khu cũ x=1536 và y=1024.
3. Kiểm A0 so ảnh gốc: diff biên vách (Canny) khu cũ, lệch > 1 ô ở đâu thì ghi lại để sửa rows cũ.
4. Script 2, overlay, sửa override tới khi vách/sông/hồ trùng ô.
5. Flood fill từ cổng làng (scratchpad): mọi ô đi được thông nhau, tới được (71,31).

## Todo
- [ ] script 1 `--canvas`  - [ ] build + seam preview  - [ ] so A0 với gốc  - [ ] script 2 + override + overlay  - [ ] flood fill

## Success criteria
4 chunk đúng 2304x1536, không thấy nối chunk; seam không lộ ở crop 1:1; overlay: ô `w/C` trùng sông/vách trong tranh, đường/bậc đi được; flood fill đạt mọi ô đi được.

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Phân loại vách tuyết sai nhiều | Cao x TB | override tay; overlay từng vùng |
| A0 xê dịch vách so rows cũ | TB x Cao | bước 3; sửa rows cũ theo A0 (toạ độ ô zone/boss/start vẫn phải đi được - P8 test) |

## Next
P5 dùng nền; P6 dán rows.
