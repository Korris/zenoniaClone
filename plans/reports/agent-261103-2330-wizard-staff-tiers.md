# Wizard: vẽ lại gậy tier 2..10 theo sheet 9 cây của người dùng

Ngày: 2026-11-03 23:30 đến 2026-11-04 00:40. Chat ChatGPT (Tay, Plus): https://chatgpt.com/c/6aea0e41-46f4-83ec-8942-3b7200747a01

## Đã làm
- Tải sheet 9 cây cuối (1774x887) về `Assets/ArtLibrary/classes/wizard/new-style/staff-src/ref-user-9-staffs.png`. Thứ tự trái sang phải = tier 2..10 (wood, crystal, moon, arcane, flame, ice, nature, holy, void); chat không nói khác.
- 9 prompt, mỗi tier một ảnh 1024x1536, trái Normal, phải Enhanced cùng dáng cùng cỡ, nền #00FF00 (nature: #FF00FF). Cả 9 ảnh đều đúng bố cục ngay lần đầu, không phải vẽ lại. Không hết hạn mức.
- Backup 9 `<tier>-src.png` cũ vào `staff-src/old-20261103/`, chép ảnh mới đè lên.
- Sửa `Tools/build-wizard-new-style.py`: `STAFF_KEY` chỉ còn `nature`; `staff_grip` nhận thân tới 24 px, bỏ mũi gậy, chọn đoạn trơn gần hàng 161 (`GRIP_TARGET_Y`); `STAFF_GRIP_Y = {"arcane": 168}`; bản cường hoá lấy hàng tay của bản thường.
- `staff-check` rồi `staff-write wood crystal moon arcane flame ice nature holy void`: 18 sprite `Sprites/classes/wizard/weapons/staff/`, grip 9 tier trong `Data/classes/wizard/held-wizard.json` (file 1 dòng, chỉ đổi 9 entry; `apprentice` giữ nguyên), icon `Sprites/icons/weapon-wizard-1..9.png` (bản cũ ở `new-style/old/icons/`).
- Ghi `docs/pending-tasks.md` và 4 dòng "Sổ lỗi" trong `docs/sprite-rules.md`.

## Đã xem bằng mắt
- 9 ảnh tải về (bản nửa cỡ): đúng thiết kế sheet, hai cây thẳng đứng, bản cường hoá không rộng hơn bản thường (w bằng nhau ở cả 9 tier).
- Ảnh cắt trên nền tối ở 2x: `nature` không còn viền hồng, `void`, `holy` sạch.
- Ảnh ghép `staff-check` (idle, walk 4 hướng, attack1, attack3) và `compose-hero-frame` với dữ liệu đã ghi (tier 1, 3, 4, 7, 9): tay nắm trên thân gậy, gậy dựng thẳng, idle xuống/trái và attack ổn.
- Chưa xem trong game.

## Grip đo được (gripY, art 269 px)
wood 145, crystal 170, moon 150, arcane 168 (ghi tay), flame 168, ice 166, nature 163, holy 160, void 165. apprentice 161 (cũ).

## Còn lại / câu hỏi
- Lỗi cũ chưa sửa: `walk` phải frame 1 và `idle` phải frame 1 đầu gậy đè má/cằm ở mọi tier, rõ nhất ở arcane (mắt), holy (cánh), void (đầu rồng). Cần đổi `ANGLE` walk phải hoặc vẽ lại idle phải rồi chạy `write` (ngoài phạm vi lần này).
- `crystal`: 3 mảnh pha lê bay rời quanh đầu bị bỏ vì tool giữ 2 khối liền lớn nhất. Có cần giữ không?
- `wood` tay ở hàng 145 (đoạn cán vàng), thấp hơn các tier khác 15-25 px: đầu gậy nằm thấp hơn một chút trên tay. Có cần ép về 161 không?
- Sidebar ChatGPT hiện "Tay / Plus", không thấy chữ "Tân Korris" như brief; vẫn là tài khoản Tay nên tiếp tục.
- Nhóm tab Chrome của agent bị mất một lần giữa chừng (ảnh moon vẫn vẽ xong, mở lại chat lấy được).
