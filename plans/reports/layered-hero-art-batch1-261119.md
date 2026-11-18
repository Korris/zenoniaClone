# Art nhân vật nhiều lớp, đợt 1 (19/11/2026)

Tài khoản: Tay ("Tân Korris", Plus), chat `6afe1ebe-b668-83ec-8233-c010a9354ef0`. Không hết lượt, không dùng Tan Pham.

## Ảnh đã nhận (src: `Assets/ArtLibrary/hero-layered/src/`)
| Ảnh | File | Ghi chú |
|---|---|---|
| Nam đi phải / chéo lên-phải | body-male-walk-right--up-right-chatgpt-1 | đạt lần 1 |
| Nam đi lên | body-male-walk-up--none-chatgpt-1 | đạt lần 1 |
| Nữ đi xuống / chéo xuống-phải | body-female-walk-down--down-right-chatgpt-1 | đạt lần 1 |
| Nữ đi phải / chéo lên-phải | body-female-walk-right--up-right-chatgpt-1 | đạt lần 1 |
| Nữ đi lên | body-female-walk-up--none-chatgpt-1 | đạt lần 1 |
| Nam đánh 1 xuống / chéo xuống-phải | body-male-attack1-down--down-right-chatgpt-1 | đạt lần 1 |
| Nam đánh 1 phải / chéo lên-phải | body-male-attack1-right--up-right-chatgpt-1 | A/B: chọn ảnh 1 (ảnh 2 hàng chéo lên-phải còn thấy mắt, có sợi xanh trên áo) |
| Nam đánh 1 lên | body-male-attack1-up--none-chatgpt-1 | đạt lần 1 |
| Nam đánh 2 xuống / chéo xuống-phải | body-male-attack2-down--down-right-chatgpt-1 | đạt lần 1 |
| Nam đánh 2 phải / chéo lên-phải | body-male-attack2-right--up-right-chatgpt-1 | A/B: chọn ảnh 1 (ảnh 2 có túi lạ ở thắt lưng) |
| Nam đánh 2 lên | body-male-attack2-up--none-chatgpt-1 | đạt lần 1 |
| Nam đánh 3 xuống / chéo xuống-phải | body-male-attack3-down--down-right-chatgpt-2 | vẽ lại 1 lần: frame 1 nắm tay đè trán / sọ |
| Nam đánh 3 phải / chéo lên-phải | body-male-attack3-right--up-right-chatgpt-2 | vẽ lại 1 lần: frame 1 cánh tay đè lên sọ |
| Nam đánh 3 lên | body-male-attack3-up--none-chatgpt-1 | đạt lần 1 (prompt đã kèm câu "tay nằm sau đầu") |

Nam (down, down-right) đi đã có sẵn từ trước. Bản -1 bị loại của đánh 3 không chép vào repo.

Sheet + anchor: `Assets/ArtLibrary/hero-layered/sheets/{male,female}/` (male: idle, walk, attack1-3; female: idle, walk).
Ảnh check: `docs/reference/hero-layered-sheets/male-check.png`, `female-check.png`.

Commit (chưa push): d9c53553, d7ad87ff, 21149083, 7705b9aa, b9e0ad01 + commit báo cáo / Sổ lỗi.

## Kiểm tra
- Cỡ sọ (w trong head-anchors) so với idle: walk nam 75-78 / idle 73-75 (walk right 78 / 73 = ~7 %), attack 72-78, nữ 74-76 / 74. Trong ngưỡng 5 % trừ walk right nam (~7 %): đầu hơi to, mắt thường khó thấy; nếu cần thì co đầu bằng tool hoặc vẽ lại.
- Hướng đúng mọi hàng; hàng chéo lên-phải không thấy mắt; áo chỉ hồng tím; đầu trọc; hình không chạm nhau.

## Lỗi còn thấy
- `head_anchor` (Tools/build-hero-layered-sheets.py) lấy đỉnh alpha: ở đánh 3 frame 1 bắt vào nắm tay giơ cao (top 25-37, w 84-91). Phải sửa tool (bỏ phần tay trên sọ) trước khi gắn tóc. Không sửa vì ngoài phạm vi brief.
- Đánh 3 frame 1 hai nắm tay tách nhau, không chụm hẳn (chấp nhận; vũ khí 2 tay có thể cần neo giữa 2 tay).
- Đánh 1/2 hướng xuống: tay duỗi về phía người xem nên ngắn (phối cảnh), vũ khí sẽ phải tự chỉnh góc.
- Hàm `__ready` trong brief không dùng được (xem Sổ lỗi); dùng `__newReady` (không nút Stop, không "Creating image", ảnh cuối cỡ byte mới).
- Extension Chrome báo mất kết nối 1 lần lúc gửi đánh 3 lên; prompt vẫn đã gửi đúng 1 lần (đã đếm).

## Số lượt vẽ
16 prompt (14 ảnh + 2 vẽ lại), 2 lần A/B. Tất cả trên Tay.

## Câu hỏi
- Có sửa `head_anchor` để bỏ qua tay giơ trên sọ không (hay lấy anchor frame 1 từ frame 2)?
- Đợt 2 (nữ đánh 1-3, tóc, mũ, vũ khí) có làm luôn trong chat này?
