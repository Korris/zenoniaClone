# Cleric skill icons, RO Soft Fantasy (20/11/2026)

Tài khoản: Tay ("Tân Korris", Plus). Chat: https://chatgpt.com/c/6b00431c-0d78-83ec-9376-b77b5415c87c

**Trạng thái 20/11 19:55: CHƯA có ảnh.** Đã gửi prompt + 3 ảnh dưới đây vào chat trên; ChatGPT trả "You're out of image creations for now. Try again at 9:32 PM" (Tay hết lượt tới 21:32). Tan Pham không kết nối Chrome lúc đó. Sau 21:32: mở lại chat trên (3 ảnh mẫu đã nằm trong tin đầu), gửi lại nguyên prompt.

Ảnh gửi kèm (theo thứ tự):
1. `docs/reference/ui-style-ro-soft-fantasy/04-skill.png` (kiểu icon tròn trong cửa sổ Kỹ năng)
2. `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png` (tông RO Soft Fantasy)
3. `docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png` (tông nhân vật người dùng chọn)

Thứ tự skill = `SkillTable.Cleric.cs` index 0-7 (heal, spirit-blessing, divine-aegis, divine-light, sanctuary, guardian-spirit, purification, divine-ascension). Game dùng `Sprites/classes/cleric/skills/<slug>/icon.png` 128 px (`SkillIcons.Get`, mọi nút skill RO Soft, cửa sổ Kỹ năng, buff); nút tròn che góc (`RoHudSkillSlot`, mask tròn) nên icon là đĩa tròn kín, chi tiết chính trong 80 % giữa. `skill-icons/0..14.png` là bộ 15 icon web cũ (chỉ `TouchControls` web hotbar), không thay.

Sau khi có ảnh:
1. Tải ảnh bằng fetch blob + `<a download>` (sprite-rules mục art ChatGPT điều 7), lưu `skill-icons-src/cleric-skill-icons-soft-fantasy-chatgpt-1.png`.
2. Xem ảnh: đúng tông (sáng, pastel, ngọc + kem), 8 hình khác nhau, đúng thứ tự, nền magenta phẳng, không vòng viền.
3. `py -3 Tools/build-cleric-soft-skill-icons.py cut Assets/ArtLibrary/classes/cleric/skill-icons-src/cleric-skill-icons-soft-fantasy-chatgpt-1.png` (ghi đè 8 `icon.png`, giữ `.meta`; nền magenta nối mép bị xoá, khói tím trong đĩa giữ lại).
4. `py -3 Tools/build-cleric-soft-skill-icons.py preview docs/reference/cleric-skill-icons-soft-fantasy/after-soft-fantasy-icons.png`, so với `before-current-icons.png`; phóng to soi viền hồng.

Prompt (một dòng, gửi nguyên văn):

```
Create an image. Vẽ mới hoàn toàn từ đầu, không dựa trên ảnh trước. CLERIC SKILL ICONS cho game RPG mobile, tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối (xem ảnh 2 và ảnh 3). Kiểu icon y như các icon tròn trong ảnh 1 (cửa sổ Kỹ năng): mỗi icon là MỘT ĐĨA TRÒN vẽ kín, nền trong đĩa là gradient màu mềm, biểu tượng to rõ ở giữa, khối tròn đầy, có highlight mềm, nhưng KHÔNG vẽ vòng viền/khung/nhẫn quanh đĩa (game tự thêm vòng ngà). Bố cục: lưới 4 cột x 2 hàng, 8 đĩa CÙNG CỠ, cách nhau rộng, trên nền phẳng một màu #FF00FF (magenta đặc, không caro, không trong suốt, không bóng đổ ra nền). Màu chủ đạo class Cleric: xanh ngọc bão hoà #35B69F / #48C9AE, điểm vàng kem #FFE6A0 và mint #B3FFE5, viền nâu đậm mềm; không dùng vàng chanh làm màu chính. Mỗi icon khác hẳn hình dáng: Hàng 1: (1) Heal: chữ thập chữa lành bo tròn màu mint-ngọc với giọt sáng và trái tim nhỏ, lấp lánh; (2) Spirit Blessing (tăng sát thương cho cả nhóm): một tinh linh nhỏ có cánh màu ngọc ôm một thanh kiếm dựng đứng mũi lên, có mũi tên chevron hướng lên màu vàng kem; (3) Divine Aegis (tăng phòng thủ cả nhóm): khiên tim viền vàng kem, mặt ngọc, huy hiệu chữ thập, hai cánh nhỏ hai bên; (4) Divine Light: tia sáng thánh từ trên giáng xuống thành một ngôi sao 8 cánh màu kem-ngọc nổ trên mặt đất. Hàng 2: (5) Sanctuary (vùng hồi máu trên đất): vòng phù văn tròn nhìn nghiêng trên cỏ, cột sáng mint dịu bốc lên, lá nhỏ bay; (6) Guardian Spirit (khiên hấp thụ): một tinh linh hộ mệnh dạng chim/cánh lớn màu ngọc dang cánh bao quanh một bong bóng khiên tròn trong; (7) Purification (sát thương thánh quanh người, thanh tẩy): vòng sóng sáng hình chữ thập nổ ra, xua tan các làn khói tím nhỏ ở mép; (8) Divine Ascension (tuyệt kỹ): đôi cánh thiên thần lớn màu kem-ngọc mở rộng sau một cột sáng thẳng đứng, hào quang vòng tròn phía trên. Không chữ, không số, không ký tự, không khung vuông.
```
