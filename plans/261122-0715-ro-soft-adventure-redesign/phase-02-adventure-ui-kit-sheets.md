# Phase 02: UI kit Adventure (sheet ChatGPT -> 9-slice)

Ưu tiên: P0. Trạng thái: chưa. Đọc hết `docs/ui-layout-rules.md` + `docs/sprite-rules.md` trước khi làm.

## Bối cảnh
- 272 mảnh RO Soft hiện có trong `Assets/Resources/Sprites/ui/ro-soft/` (tiền tố: hud 75, bag 36, pet 53, gacha 23, hotbar 23, wanted 18, wm 19, btn 10, bar 3, tab/toggle/slot/panel/window/divider/dropdown). Sheet JSON cắt ở `Tools/ui-paper-sheets/` + `Tools/cut-ui-paper-sheet.py` (đã có `flatten_edges` đúng trục, `glyph_inset`, `hard_edge`, `body_alpha`).
- Mẫu: `docs/reference/ui-style-ro-soft-adventure/05-style-board.png` (bảng kit Adventure), `02-bag.png`; test túi đồ `ro-soft-adventure-redesign/bag-adventure-chatgpt-1.png`.

## Việc (mỗi sheet = 1 ảnh ChatGPT nền phẳng #00FF00, cùng TÊN mảnh với ro-soft để thay thế 1-1)
1. Sheet K1 khung: cửa sổ lớn (góc đồng), hộp thoại vừa, toast / tooltip, panel lõm, divider thoi đồng, dải tiêu đề.
2. Sheet K2 nút + tab + pill + toggle + dropdown: teal / vàng / đỏ / xanh / disabled x normal / pressed / selected; tab icon-trên-chữ; pill viên thuốc (rarity, Lv).
3. Sheet K3 ô item (thường + 6 viền độ hiếm + chọn), ô trống dấu +, ô trang bị nhân vật, X đóng, pill tiền (vàng / gem / pet crystal).
4. Sheet K4 thanh HP / MP / EXP / cast + rãnh, badge số, chevron / mũi tên.
5. Cắt bằng `cut-ui-paper-sheet.py` -> `Sprites/ui/ro-adventure/`, border 9-slice theo bán kính guide (khung lớn 12-16, vừa 10-12, toast 8, ô 6-8).
6. Bảng contact so 4 sheet với `05-style-board.png`, chấm checklist 10 điểm (ChatGPT chấm hộ).

## Tái dùng
Không tái dùng sprite Fantasy (xanh) hay Paper cũ (gỗ dày) nguyên trạng. Icon tab túi đồ (`bag-cat-*`) và icon menu HUD có thể đổi tông bằng tool tạm nếu hình vẫn hợp, vẽ lại sau ở phase 06.

## Thành công
`--ui-skin adventure` hiện túi đồ với khung / nút / ô mới, khớp mockup ở cùng cỡ (lệch padding ≤ 2 px).
