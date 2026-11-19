# RO Fantasy heroes, art đợt A (19/11/2026)

- Tài khoản: **Tan Pham** (Plus). Trước mỗi lần gửi đều đọc `/api/auth/session`, lần nào cũng ra "Tan Pham". Không dùng Tay.
- Chat mới: https://chatgpt.com/c/6afe7669-88e4-83ec-8049-f8c7ea9ceaaf
- Lượt đầu gửi kèm ảnh mẫu tông màu (`sample-hero-user-pick.png`, `ui-style-ro-soft-fantasy/01-style-board.png`). Prompt thân nào cũng có câu tông màu RO Soft Fantasy.
- Branch `ro-fantasy-heroes`, chưa push. Các commit: 6636a38a (thân), 2 commit tóc + check, 1 commit báo cáo.

## Ảnh đã nhận

Thư mục `Assets/ArtLibrary/ro-fantasy-heroes/src/`:

| Ảnh | File | Ghi chú |
|---|---|---|
| Thân Warrior | body-warrior-idle-5dir-chatgpt-1 | Đạt lần 1: đầu trọc, tay rỗng, 5 hướng đúng, tông sáng ấm |
| Thân Wizard | body-wizard-idle-5dir-chatgpt-1 | Đạt lần 1, đã bỏ mũ |
| Thân Mechanic | body-mechanic-idle-5dir-chatgpt-1 | Đạt lần 1, đã bỏ kính |
| Thân Assassin (nữ) | body-assassin-idle-5dir-chatgpt-1 | Đạt lần 1, giữ khăn che mặt, bỏ băng đô |
| Thân Cleric (nữ) | body-cleric-idle-5dir-chatgpt-1 | Đạt lần 1, đã bỏ mũ lễ |
| Tóc Warrior 1, xù | hair-warrior-style1-…-2 | Vẽ lại 1 lần (xem dưới) |
| Tóc Warrior 2, vuốt ngược | …-1 | |
| Tóc Warrior 3, buộc thấp | …-1 | Hơi thành khối "mũ bảo hiểm" |
| Tóc Wizard 1, mái che trán | …-2 | Vẽ lại 1 lần |
| Tóc Wizard 2, buộc sau | …-1 | Màu bạc ngả ấm, đuôi tóc ngắn, có dính mẩu cổ áo xanh |
| Tóc Wizard 3, rẽ ngôi | …-1 | |
| Tóc Mechanic 1, xù nhọn + kính | …-1 | |
| Tóc Mechanic 2, undercut + kính | …-1 | |
| Tóc Mechanic 3, ngắn rối + kính | …-1 | Có viền trắng (tool đã xử lý) |
| Tóc Assassin 1, đuôi ngựa cao + băng đô | …-1 | Có cả nơ đỏ như ảnh mẫu |
| Tóc Assassin 2, bob + băng đô | …-1 | |
| Tóc Assassin 3, hai bím + băng đô | …-1 | Hướng chéo lên-phải ló một mắt, nhưng lớp tóc không lấy mặt nên không ảnh hưởng |
| Tóc Cleric 1, ngang vai | …-1 | |
| Tóc Cleric 2, dài thẳng | …-2 | Vẽ lại 1 lần |
| Tóc Cleric 3, búi tròn | …-1 | |

Ảnh mẫu đầu trọc: `Assets/ArtLibrary/ro-fantasy-heroes/ref/bald-head-<class>-5dir.png`, phóng x3. Ảnh mẫu cắt theo class: `ref/sample-<class>-x2.png`.

## Ảnh vẽ lại

- **Warrior tóc 1:** khối tóc rộng 1,36-1,44 lần sọ. Vẽ lại có gửi kèm ảnh cũ và con số, còn 1,09-1,16.
- **Wizard tóc 1:** tóc bạc ngả tím hồng, gần trắng, có vẽ cổ áo. Vẽ lại thì ra xám trung tính, không còn cổ áo.
- **Cleric tóc 2:** đuôi tóc bị cắt bằng ở mép ảnh. Vẽ lại thì ngọn tóc đủ, nằm trong khung.

Đã xoá các bản -1 bị loại.

## Ảnh check

`docs/reference/ro-fantasy-heroes/check-<class>.png`. Hàng 0 là thân 5 hướng. Mỗi kiểu tóc có 2 hàng: tóc tách nền, đặt lên đầu, nhuộm nâu `#A8693F` và tím `#B49CE2`.

Tool tạo ảnh: `Tools/build-ro-fantasy-hero-art-check.py heads|check <class>`, kèm `Tools/ro_fantasy_hair_fit.py`.

## Lỗi còn lại

- **Cỡ tóc chưa đạt yêu cầu ≤ 1,15 lần sọ** (đo bằng bề ngang tóc / bề ngang sọ có tai):
  - Đạt: Warrior 1 (1,09-1,16), Mechanic 2, Cleric 2, Cleric 3.
  - Từ 1,2 đến 1,35: phần lớn các kiểu còn lại.
  - Vượt 1,4: Warrior 3 (1,40-1,51), Assassin 1 (1,46-1,78, đuôi ngựa xoè như ảnh mẫu), Cleric 1 (1,39-1,50, tóc ngang vai xoè).
  - Chưa vẽ lại vì dáng vẫn hợp ảnh mẫu. Nếu muốn ôm sát hơn: vẽ lại kèm ảnh cũ và con số, cách này đã hiệu quả với Warrior 1.
- **Tóc ChatGPT có tông ngả ấm/beige, không đúng 4 mã xám.** Cần lượng tử về 4 tông khi cài vào game. Ảnh check chỉ chuẩn hoá độ sáng.
- **Ảnh check chỉ đặt tóc gần đúng:**
  - Vài hướng ngang/chéo bị lệch vài px (thấy mắt đôi nhẹ ở Assassin bob, Cleric ngang vai).
  - Tóc vẽ đè lên thân, chưa tách phần tóc sau lưng.
  - Kính và băng đô lấy theo cụm màu bão hoà lớn chạm tóc.
  - Đây chưa phải anchor để dùng trong game.
- **Kích thước ảnh nguồn:**
  - Ảnh tóc ra 2170-2172x724, theo tỉ lệ ảnh mẫu, không phải 1536x1024.
  - Thân đúng 1536x1024.
  - Đầu ở hai ảnh khác nhau không cùng cỡ: tool khớp lại tỉ lệ.
- **Tông màu:** Assassin vẫn đồ tối (đen tím) như ảnh mẫu. Bóng vẽ mềm, không bị trầm hơn mẫu. 4 class còn lại sáng ấm.
- Chưa xem trong game: chưa có sheet, chưa có code.

## Lượt vẽ

- 23 ảnh trên Tan Pham: 5 thân, 15 tóc, 3 vẽ lại.
- Không có A/B. Không hết lượt.

## Câu hỏi

1. Có cần vẽ lại các kiểu tóc vượt 1,3 lần sọ (Warrior 3, Assassin 1, Cleric 1, Wizard 1) cho ôm sát hơn không? Hay giữ độ xoè như ảnh mẫu?
2. Brief ghi khăn che mặt Assassin vừa "BỎ khỏi thân" vừa "giữ lại". Tôi đã giữ khăn trên thân. Đúng ý chưa?
