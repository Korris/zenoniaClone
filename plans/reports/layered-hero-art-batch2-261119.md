# Art nhân vật nhiều lớp, đợt 2 (19/11/2026)

Tài khoản: Tay ("Tân Korris", Plus), chat `6afe1ebe-b668-83ec-8233-c010a9354ef0`. Không hết lượt, không dùng Tan Pham. Không có A/B.

## Ảnh đã nhận (src: `Assets/ArtLibrary/hero-layered/src/`)
| Ảnh | File | Ghi chú |
|---|---|---|
| Nữ đánh 1 xuống / chéo xuống-phải | body-female-attack1-down--down-right-chatgpt-1 | đạt lần 1 |
| Nữ đánh 1 phải / chéo lên-phải | body-female-attack1-right--up-right-chatgpt-2 | vẽ lại 1 lần: thắt lưng tô hồng tím |
| Nữ đánh 1 lên | body-female-attack1-up--none-chatgpt-1 | đạt lần 1 |
| Nữ đánh 2 xuống / chéo xuống-phải | body-female-attack2-down--down-right-chatgpt-1 | đạt; frame 1 gần như dáng đứng (không rút tay về hông rõ) |
| Nữ đánh 2 phải / chéo lên-phải | body-female-attack2-right--up-right-chatgpt-1 | đạt lần 1 |
| Nữ đánh 2 lên | body-female-attack2-up--none-chatgpt-1 | đạt; frame 3 tay chĩa chéo lên-phải chứ không thẳng ra xa |
| Nữ đánh 3 xuống / chéo xuống-phải | body-female-attack3-down--down-right-chatgpt-1 | đạt lần 1, tay giơ nằm sau sọ |
| Nữ đánh 3 phải / chéo lên-phải | body-female-attack3-right--up-right-chatgpt-2 | vẽ lại 1 lần: frame 1 tay đè lên sọ + vệt xanh lá trên găng |
| Nữ đánh 3 lên | body-female-attack3-up--none-chatgpt-1 | đạt lần 1 |
| Tóc Warrior | hair-warrior-5dir-chatgpt-2 | vẽ lại 1 lần: tóc rộng hơn sọ 31-38 % |
| Tóc Wizard | hair-wizard-5dir-chatgpt-1 và -2 | -1 tự nhiên hơn nhưng rộng 1,15-1,23 và thiếu đuôi tóc; -2 có đuôi tóc, rộng 1,01-1,06, nhưng mượt như mũ bảo hiểm. Giữ cả hai, tool sau lấy bản mới nhất (-2) |
| Tóc Mechanic | hair-mechanic-5dir-chatgpt-1 | rộng 1,14-1,20 (tính cả kính và lọn nhọn), chấp nhận; kính đồng + tròng xanh |
| Tóc Assassin | hair-assassin-5dir-chatgpt-1 | đuôi ngựa cao, nơ đỏ; phần trên sọ gọn (1,01-1,02 ở hướng xuống/lên) |
| Tóc Cleric | hair-cleric-5dir-chatgpt-1 | rộng 1,05-1,14; mái hơi dày chứ không "thưa" |

Bản bị loại không giữ trong repo (trừ Wizard -1).

Sheet + anchor nữ: `Assets/ArtLibrary/hero-layered/sheets/female/` có đủ idle, walk, attack1-3. Ảnh check: `docs/reference/hero-layered-sheets/female-check.png`.

Commit (chưa push): bc753a57, 171dba3a, d09add43, f8643d3d, cb36627d + commit báo cáo.

## Kiểm tra
- Thân nữ: cỡ sọ (w) 73-77 so với idle 74-75, lệch dưới 5 %. Đỉnh sọ (y) 48-61 so với idle 55. Ví dụ đánh 2 phải 61: người thấp hơn khoảng 3 %, vẫn trong ngưỡng. Vạch đỏ đỉnh sọ đúng ở mọi frame, kể cả đánh 3 frame 1: tool mới không bắt vào nắm tay.
- Thắt lưng: phóng to từng ảnh, đều nâu sau khi sửa. Hướng đúng ở mọi ảnh. Hàng chéo lên-phải không thấy mắt. Hình không chạm nhau.
- Tóc: ChatGPT trả ảnh 2172x724 theo tỉ lệ ảnh mẫu chứ không phải 1536x1024. Ảnh ra là ảnh mẫu thu nhỏ khoảng 0,86, 5 đầu đúng thứ tự và vị trí. Đo bề ngang tóc so với sọ bằng script nháp (scale lấy theo khoảng cách đầu 1 tới đầu 5).
- Màu tóc: phần trên đầu chỉ có xám, trừ kính Mechanic và nơ Assassin. Tông xám là dải chuyển chứ không phải đúng 4 màu phẳng, tool cắt tóc sẽ phải lượng tử hoá.
- Chưa xem trong game; chưa ghép tóc lên thân (chưa có tool cắt tóc).

## Lỗi còn thấy
- Đánh 3 frame 1: nắm tay giơ trên đỉnh đầu, chỗ chồng lên tóc sẽ bị lớp tóc che, vì tóc vẽ sau thân.
- Tóc Cleric và đuôi ngựa Assassin rủ xuống quá cổ áo, đè lên vai thân. Cần xem khi ghép, nhất là lúc đánh.
- `__ready` trong brief luôn trả false: footer ChatGPT giờ có chữ "Thinking effort", khớp regex `Thinking`. Đã sửa tại chỗ thành `Thinking(?! effort)`, thêm `Finishing up`, và dùng `__newReady` như đợt 1. Lần upload ảnh mẫu thứ 2 chậm nên `__send` trả false; gửi lại thì được, không gửi trùng.

## Số lượt vẽ
18 prompt: thân 9 + 2 vẽ lại, tóc 5 + 2 vẽ lại. Không A/B. Tất cả trên Tay.

## Câu hỏi
- Tóc Wizard: dùng -1 (tự nhiên, hơi phồng, không đuôi) hay -2 (gọn, có đuôi, hơi giống mũ)?
- Tóc Mechanic rộng tới 1,20 lần sọ (lọn nhọn + kính): chấp nhận hay vẽ gọn hơn?
- Đợt 3 (mũ Prompt 6, vũ khí Prompt 7) có tiếp tục trong chat này không?
