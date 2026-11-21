# Warrior skill icons, RO Soft Fantasy (21/11/2026)

Làm theo mẫu Cleric (`../../cleric/skill-icons-src/prompt-soft-fantasy-icons.md`). Tài khoản: Tay ("Tân Korris", Plus) hoặc Tan Pham (Plus). Chat (21/11, Tan Pham, chung 1 chat cho 5 class, 4 ảnh mẫu ở tin đầu, các class sau mở đầu bằng "Vẫn dùng 4 ảnh mẫu ở tin đầu."): https://chatgpt.com/c/6b0106e1-80d0-83ec-9ff9-7bb6b5ee32fa

Ảnh gửi kèm (theo thứ tự, ở tin đầu của chat):
1. `docs/reference/ui-style-ro-soft-fantasy/04-skill.png` (kiểu icon tròn cửa sổ Kỹ năng)
2. `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png` (tông RO Soft Fantasy)
3. `docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png` (tông nhân vật người dùng chọn)
4. `docs/reference/cleric-skill-icons-soft-fantasy/after-soft-fantasy-icons.png` (bộ icon Cleric đã duyệt, kiểu phải khớp)

Thứ tự skill = `SkillTable.Warrior.cs` index 0-7. Game dùng `Sprites/classes/warrior/skills/<slug>/icon.png` 128 px (`SkillIcons.Get`). Nền #FF00FF.

Sau khi có ảnh: lưu `warrior-skill-icons-soft-fantasy-chatgpt-1.png` ở thư mục này, xem ảnh, rồi
`py -3 Tools/build-soft-fantasy-skill-icons.py cut warrior Assets/ArtLibrary/classes/warrior/skill-icons-src/warrior-skill-icons-soft-fantasy-chatgpt-1.png`
và `py -3 Tools/build-soft-fantasy-skill-icons.py preview warrior docs/reference/skill-icons-soft-fantasy/warrior-after-soft-fantasy-icons.png`.

Prompt (một dòng, gửi nguyên văn):

```
Create an image. Vẽ mới hoàn toàn từ đầu, không dựa trên ảnh trước. WARRIOR SKILL ICONS cho game RPG mobile, tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối (xem ảnh 2 và ảnh 3). Kiểu icon y hệt bộ icon Cleric ở ảnh 4 (cùng cỡ đĩa, cùng độ sáng, cùng nét vẽ, cùng highlight) và các icon tròn trong ảnh 1: mỗi icon là MỘT ĐĨA TRÒN vẽ kín, nền trong đĩa là gradient màu mềm, biểu tượng to rõ ở giữa, khối tròn đầy, có highlight mềm, nhưng KHÔNG vẽ vòng viền/khung/nhẫn quanh đĩa (game tự thêm vòng ngà). Bố cục: lưới 4 cột x 2 hàng, 8 đĩa CÙNG CỠ, cách nhau rộng, trên nền phẳng một màu #FF00FF (magenta đặc, không caro, không trong suốt, không bóng đổ ra nền). Màu chủ đạo class Warrior: đỏ san hô ấm pastel #E8665C / #F4917A, điểm vàng kem #FFE6A0 và đào #FFD3B8, lửa đỏ cam dịu, viền nâu đậm mềm; không đỏ thẫm, không đen. Mỗi icon khác hẳn hình dáng, đọc rõ ở cỡ 64 px: Hàng 1: (1) Power Slash (chuỗi chém vòng cung trước mặt): một thanh kiếm rộng bản chém xéo với 3 vệt chém lưỡi liềm chồng nhau màu kem-san hô; (2) Whirlwind (xoay người chém quanh mình): xoáy gió tròn nằm ngang bao quanh một thanh kiếm dựng đứng mũi lên ở giữa, lá nhỏ bay; (3) Earth Break (đập đất): một thanh kiếm cắm xuống mặt đất nứt toác hình sao, khối đá vụn bay lên, chút lửa đỏ trong khe nứt; (4) Flame Cleave (chém lửa gây bỏng): lưỡi kiếm bốc lửa đỏ cam cuộn thành một ngọn lửa hình lưỡi liềm lớn. Hàng 2: (5) Charging Strike (lao tới đâm thẳng): mũi kiếm chĩa ngang sang phải với các vệt tốc độ ngang và bụi phía sau; (6) Blade Cyclone (tuyệt kỹ lốc kiếm): 6 thanh kiếm nhỏ xếp vòng tròn quay như cánh quạt quanh một tâm sáng, nhìn từ trên xuống; (7) Inferno Crash (nhảy lên bổ xuống, hố lửa): thanh kiếm lớn từ trên cao cắm xuống gây vụ nổ lửa hình vòm và vòng sóng xung kích trên đất; (8) Berserker Awakening (tự tăng công + tốc đánh): đầu sư tử gầm cách điệu màu vàng kim với hào quang lửa đỏ phía sau và 2 mũi chevron hướng lên (không vẽ mặt quỷ). Không chữ, không số, không ký tự, không khung vuông.
```
