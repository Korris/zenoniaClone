# Willowbrook Forest 2.5D: định hướng art (ChatGPT, 21/11/2026)

Chat (Tay, "Tân Korris"): https://chatgpt.com/c/6b00f11a-1148-83ec-bd9e-0d255d3f0211
Gửi kèm: `docs/reference/ro3-skill-style/midgard-senki-forest-map-reference.png`, khung RO3 (`D:\temp\ro3-frames\sheet-04.jpg`, chỉ có trên máy KorrisPC), ảnh trong game hiện tại `docs/reference/cleric-fx-ro3/willowbrook-meadows-ingame-cleric.jpg`, bố cục `docs/reference/willowbrook-redraw-composite.jpg`.

Tóm tắt (số là mục tiêu ChatGPT đề xuất, không đo từ ảnh):

## 1. Bố cục rừng
- Rừng sáng, kín đáo kiểu fantasy, không tối. Giữ đường, suối, cầu, ao, cổng tây / nam, phế tích, chòi, tâm bãi quái.
- Tỉ lệ: tán rừng 38 %, bãi cỏ trống 32 %, nền rừng chuyển tiếp 18 %, đường đất 12 %.
- 5-6 khối rừng liền nhau bao 4-5 khoảng trống lớn, không rải cây đều.
- Tây bắc: vành tán dày; bắc / đông bắc: cây lá rộng + thông quanh phế tích; đông: tường cây dọc mép; suối: cây lùi khỏi bờ, sậy cụm nhỏ; cầu: hai đầu trống, cây đóng khung; ao: bờ cỏ trống, rừng hình lưỡi liềm; cổng nam: lối sáng rộng có cây đóng khung.
- Đường chính: hành lang trống nhìn thấy 5-7 ô. Bãi quái: khoảng trống 8-12 ô, không cây trong vòng đánh.

## 2. Nền đất
- Cỏ chính #76B45B, cỏ sáng #8DC56B, cỏ vừa #689C50, nền rừng #527D43, rừng sâu #426F40.
- Đường #D2A765, chỗ sáng #E4BC7D, mép #B58F58. Đường chính 2,5-3,5 ô, phụ 1,5-2,5 ô; mép răng cưa 0,2-0,6 ô; đất gần như một màu, 1-2 mảng sáng; không viền mảnh, không vẽ từng viên đá.
- 70-80 % cỏ trống là một màu xanh vừa, mảng sáng / tối rộng 2-6 ô, chênh độ sáng ±8-12 %. Nền rừng tối hơn 15-25 %. Bỏ ánh vàng chanh.
- Chi tiết tối đa: búi cỏ 5-8 %, hoa 0,5-1,5 % (bớt 75-85 % so với hiện tại, hoa ven đường / bờ ao / quanh chòi), lá rụng 1-2 % trong rừng, đá nhỏ 0,5-1 %; lòng bãi quái 80-85 % trơn.

## 3. Bộ cây (sprite đứng riêng)
- T1 sồi Willowbrook tròn nhiều tầng, tán 3-4 ô, cao 4-5 ô, ~40 %.
- T2 sồi cổ thụ lệch, rễ nổi, tán 5-6 ô, cao 5-7, ~10 % (điểm nhấn ở mép).
- T3 cây non tròn nhỏ sáng màu, tán 2-3, cao 2,5-3,5, ~20 % (ven đường).
- T4 thông mềm tầng tròn, tán 2-3, cao 4-6, ~15 % (đông bắc, mép map).
- T5 bạch dương thân kem, tán vàng xanh, ~10 % (điểm màu).
- T6 liễu bên suối, rủ, ~5 % (chỉ gần suối / ao).
- Màu lá: sáng #88BF62, chính #579B4F, bóng vừa #407F42, bóng sâu #326A39, highlight #A4CC77; vỏ #846040, bóng vỏ #60472F. 3 dải bóng + 1 highlight, viền nâu xanh, không viền đen.
- Bụi 3 kiểu (0,6-1,5 ô), đá 3 kiểu (xám ấm + rêu ô liu), 2 gốc + 2 khúc gỗ, 2 dương xỉ + 2 sậy.
- Mật độ đầu: ~110-150 cây (bắt đầu 125), 60-90 bụi / dương xỉ, 20-30 cụm đá, 10-15 gốc / khúc gỗ.

## 4. Bóng cây 2.5D
- Màu #304C3B, alpha 0,20-0,30 (rừng dày tối đa 0,35, có trần), hướng xuống-phải 35-45°, mép mềm, dài 0,45-0,9 x chiều cao cây, bóng tán thành nhiều thuỳ (không một elip), thêm bóng tiếp xúc tối nhỏ ở gốc (0,5-0,9 ô). Không cộng dồn bóng (dùng mask có trần hoặc nướng tĩnh). Không động.

## 5. Đọc được với 40 người
- Lòng bãi quái 6-8 ô không thân cây, đá to, bụi; bóng trong vùng đánh < 0,20.
- **Bắt buộc:** cây che hero mình phải mờ còn 20-35 %, mờ đi 0,15-0,2 s, hiện lại 0,25-0,35 s; bóng dưới đất giữ nguyên. Không làm mờ mọi cây chỉ vì người khác đi qua.
- Tránh: cỏ nhiễu khắp nơi, hoa dày trong bãi, cây rải đều kiểu vườn ươm, tán khổng lồ đứng lẻ sát camera, cỏ chanh neon, bóng đen nặng, lá kiểu ảnh thật, bụi trên đường, cây cao giữa bãi, bloom / sương phủ cả map.

## 6. Lớp sản xuất
- A: ảnh nền (cỏ, nền rừng, đường, lòng suối / ao), KHÔNG nướng bóng cây hay đồ đứng. B: trang trí thấp thưa. C: sprite đứng (cây, bụi, gốc, đá, chòi, phế tích) neo ở chân + bóng riêng.
