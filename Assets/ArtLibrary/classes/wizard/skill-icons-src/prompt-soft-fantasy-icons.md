# Wizard skill icons, RO Soft Fantasy (21/11/2026)

Làm theo mẫu Cleric (`../../cleric/skill-icons-src/prompt-soft-fantasy-icons.md`). Tài khoản: Tay ("Tân Korris", Plus) hoặc Tan Pham (Plus). Chat (21/11, Tan Pham, chung 1 chat cho 5 class, 4 ảnh mẫu ở tin đầu, các class sau mở đầu bằng "Vẫn dùng 4 ảnh mẫu ở tin đầu."): https://chatgpt.com/c/6b0106e1-80d0-83ec-9ff9-7bb6b5ee32fa

Ảnh gửi kèm (theo thứ tự, ở tin đầu của chat):
1. `docs/reference/ui-style-ro-soft-fantasy/04-skill.png` (kiểu icon tròn cửa sổ Kỹ năng)
2. `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png` (tông RO Soft Fantasy)
3. `docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png` (tông nhân vật người dùng chọn)
4. `docs/reference/cleric-skill-icons-soft-fantasy/after-soft-fantasy-icons.png` (bộ icon Cleric đã duyệt, kiểu phải khớp)

Thứ tự skill = `SkillTable.Wizard.cs` index 0-7. Game dùng `Sprites/classes/wizard/skills/<slug>/icon.png` 128 px (`SkillIcons.Get`). Nền #FF00FF.

Sau khi có ảnh: lưu `wizard-skill-icons-soft-fantasy-chatgpt-1.png` ở thư mục này, xem ảnh, rồi
`py -3 Tools/build-soft-fantasy-skill-icons.py cut wizard Assets/ArtLibrary/classes/wizard/skill-icons-src/wizard-skill-icons-soft-fantasy-chatgpt-1.png`
và `py -3 Tools/build-soft-fantasy-skill-icons.py preview wizard docs/reference/skill-icons-soft-fantasy/wizard-after-soft-fantasy-icons.png`.

Prompt (một dòng, gửi nguyên văn):

```
Create an image. Vẽ mới hoàn toàn từ đầu, không dựa trên ảnh trước. WIZARD SKILL ICONS cho game RPG mobile, tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối (xem ảnh 2 và ảnh 3). Kiểu icon y hệt bộ icon Cleric ở ảnh 4 (cùng cỡ đĩa, cùng độ sáng, cùng nét vẽ, cùng highlight) và các icon tròn trong ảnh 1: mỗi icon là MỘT ĐĨA TRÒN vẽ kín, nền trong đĩa là gradient màu mềm, biểu tượng to rõ ở giữa, khối tròn đầy, có highlight mềm, nhưng KHÔNG vẽ vòng viền/khung/nhẫn quanh đĩa (game tự thêm vòng ngà). Bố cục: lưới 4 cột x 2 hàng, 8 đĩa CÙNG CỠ, cách nhau rộng, trên nền phẳng một màu #FF00FF (magenta đặc, không caro, không trong suốt, không bóng đổ ra nền). Màu chủ đạo class Wizard: xanh trời pastel #5BA8E6 / #86C6F2, băng #D4F2FF, điểm vàng kem #FFE6A0, viền nâu đậm mềm; lửa cam chỉ là điểm nhấn ở icon lửa; không dùng tím làm màu chính. Mỗi icon khác hẳn hình dáng, đọc rõ ở cỡ 64 px: Hàng 1: (1) Arcane Bolt (đạn phép): một viên đạn phép ngôi sao 4 cánh màu xanh trời bay chéo lên phải với đuôi sao nhỏ; (2) Fireball: quả cầu lửa tròn cam-đỏ pastel có đuôi lửa bay chéo, trên nền đĩa xanh; (3) Ice Storm (bão băng vùng): một đám mây xanh xám nhỏ phía trên rơi xuống nhiều mũi băng nhọn và bông tuyết; (4) Arcane Lance (giáo phép xuyên thủng): một ngọn giáo pha lê dài màu xanh lam bay ngang xuyên qua một vòng tròn sáng. Hàng 2: (5) Mana Shield (khiên phép hấp thụ): bong bóng khiên tròn trong vân lục giác tổ ong màu xanh nhạt bao quanh một viên ngọc mana hình giọt; (6) Frost Nova (vòng băng nổ quanh người, đóng băng): bông tuyết lớn 6 cánh ở giữa với vòng tinh thể băng nhọn bung ra xung quanh trên mặt đất; (7) Elemental Burst (nổ lửa và băng): một quả cầu nửa lửa cam nửa băng xanh xoáy vào nhau như âm dương, tia sáng nổ ra; (8) Astral Cataclysm (tuyệt kỹ mưa sao băng): nhiều thiên thạch có đuôi sáng rơi chéo từ bầu trời đêm xanh lam nhạt có vầng trăng khuyết nhỏ. Không chữ, không số, không ký tự, không khung vuông.
```
