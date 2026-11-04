# Wizard `attack3` xuống, lên, trái: vẽ lại theo thiết kế mũ trùm (2026-11-04 0:20-0:40)

## Làm gì
- ChatGPT (tài khoản Tay, chat mới https://chatgpt.com/c/6aea1832-fccc-83ec-b1f4-f1276f848a42) vẽ 3 dải 6 frame, mỗi dải 1 lượt, không cần vẽ lại. Mẫu tải lên: `src/wizard-attack3-right.png` (tư thế) + `src/wizard-attack2-down.png` (thiết kế nhìn thẳng). Không tải `hero-select/wizard.png`: chân dung đội mũ nhọn, các hàng mới dùng mũ trùm. Hàng lên và trái chỉ tả bằng chữ trong cùng chat.
- Ảnh về `Downloads/wizard-attack3-{down,up,left}-v1.png` (1536x1024), chép vào `Assets/ArtLibrary/classes/wizard/new-style/src/wizard-attack3-{down,up,left}.png` (+ `.meta`).
- `Tools/build-wizard-new-style.py`: thêm 3 hàng vào `FISTS` (xuống: tay phải = bên trái màn hình, `behind` False; lên: bên phải màn hình, `behind` True; trái: tay xa đấm, `behind` True); `ANGLE_FRAME` thêm xuống 4, 5 = -12 và lên 0, 2, 4, 5 = 30/35 vì góc cũ khớp tay cũ (đầu gậy chúi xuống chân, gậy nằm ngang đè bụng).
- Chạy khô `write_sheets` ra thư mục tạm trước khi thêm hàng: mọi hàng và anchor trùng từng pixel với Resources, idle xuống không bị đụng. Sau `write` đối chiếu bản sao: chỉ hàng 0-2 của `body/{bare,armed}/attack3.png`, hàng 0 `body/hand/attack3.png` và 3 anchor `attack3` đổi.

## Đã xem bằng mắt
- 3 dải ChatGPT (bản nửa cỡ): đúng mũ trùm, áo choàng, mặt, tóc; tư thế 6 frame khớp dải phải.
- Lưới 4x có dấu nắm tay từng frame.
- Ảnh ghép `preview` và ảnh ghép từ Resources thật (`compose-hero-frame`, tier 9 `void`, pad 60) cả 18 frame + zoom 3 frame: gậy trong nắm tay, đầu gậy trên tay hoặc chĩa theo hướng đấm, không cắt mặt.
- Chưa xem trong game.

## Files
- `Assets/ArtLibrary/classes/wizard/new-style/src/wizard-attack3-{down,up,left}.png` (+ .meta) mới
- `Assets/Resources/Sprites/classes/wizard/body/{bare,armed,hand}/attack3.png`
- `Assets/Resources/Data/classes/wizard/held-wizard-melee.json` (anchor `attack3` down/up/left)
- `Tools/build-wizard-new-style.py` (`FISTS`, `ANGLE_FRAME`)
- `docs/pending-tasks.md`, `docs/sprite-weapon-rules.md` (3 dòng Sổ lỗi)

## Còn tồn
- Hàng lên lộ tóc trắng dưới mũ trùm, khác `attack1`, `attack2` lên (chỉ mũ). Chấp nhận, ghi Sổ lỗi.
- Xuống frame 3 (góc cũ -58) đoạn dưới gậy chạy chéo qua chân; chấp nhận như các hàng attack khác.
- `attack1`, `attack2` trái vẫn lật từ phải; `idle` phải chưa vẽ lại; lỗi đầu gậy đè má ở `walk`/`idle` phải frame 1 chưa sửa.
- Không commit, không build.
