# Mechanic skill icons, RO Soft Fantasy (21/11/2026)

Làm theo mẫu Cleric (`../../cleric/skill-icons-src/prompt-soft-fantasy-icons.md`). Tài khoản: Tay ("Tân Korris", Plus) hoặc Tan Pham (Plus). Chat (21/11, Tan Pham, chung 1 chat cho 5 class, 4 ảnh mẫu ở tin đầu, các class sau mở đầu bằng "Vẫn dùng 4 ảnh mẫu ở tin đầu."): https://chatgpt.com/c/6b0106e1-80d0-83ec-9ff9-7bb6b5ee32fa

Ảnh gửi kèm (theo thứ tự, ở tin đầu của chat):
1. `docs/reference/ui-style-ro-soft-fantasy/04-skill.png` (kiểu icon tròn cửa sổ Kỹ năng)
2. `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png` (tông RO Soft Fantasy)
3. `docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png` (tông nhân vật người dùng chọn)
4. `docs/reference/cleric-skill-icons-soft-fantasy/after-soft-fantasy-icons.png` (bộ icon Cleric đã duyệt, kiểu phải khớp)

Thứ tự skill = `SkillTable.Mechanic.cs` index 0-7. Game dùng `Sprites/classes/mechanic/skills/<slug>/icon.png` 128 px (`SkillIcons.Get`). Nền #FF00FF.

Sau khi có ảnh: lưu `mechanic-skill-icons-soft-fantasy-chatgpt-N.png` ở thư mục này, xem ảnh, rồi
`py -3 Tools/build-soft-fantasy-skill-icons.py cut mechanic Assets/ArtLibrary/classes/mechanic/skill-icons-src/mechanic-skill-icons-soft-fantasy-chatgpt-2.png`
và `py -3 Tools/build-soft-fantasy-skill-icons.py preview mechanic docs/reference/skill-icons-soft-fantasy/mechanic-after-soft-fantasy-icons.png`.

Prompt (một dòng, gửi nguyên văn):

```
Create an image. Vẽ mới hoàn toàn từ đầu, không dựa trên ảnh trước. MECHANIC SKILL ICONS cho game RPG mobile, tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối (xem ảnh 2 và ảnh 3). Kiểu icon y hệt bộ icon Cleric ở ảnh 4 (cùng cỡ đĩa, cùng độ sáng, cùng nét vẽ, cùng highlight) và các icon tròn trong ảnh 1: mỗi icon là MỘT ĐĨA TRÒN vẽ kín, nền trong đĩa là gradient màu mềm, biểu tượng to rõ ở giữa, khối tròn đầy, có highlight mềm, nhưng KHÔNG vẽ vòng viền/khung/nhẫn quanh đĩa (game tự thêm vòng ngà). Bố cục: lưới 4 cột x 2 hàng, 8 đĩa CÙNG CỠ, cách nhau rộng, trên nền phẳng một màu #FF00FF (magenta đặc, không caro, không trong suốt, không bóng đổ ra nền). Màu chủ đạo class Mechanic: vàng mật ong ấm pastel #F2B544 / #FFD36E, đồng thau #C8893A, điểm kem #FFF1C9, viền nâu đậm mềm; không dùng xanh dương làm màu chính. Mỗi icon khác hẳn hình dáng, đọc rõ ở cỡ 64 px: Hàng 1: (1) Power Shot (phát bắn xuyên): một viên đạn đồng thau lớn bay ngang với vòng xung kích ở nòng và vệt sáng xuyên thẳng; (2) Plasma Barrage (6 tia toả quạt): 6 viên plasma vàng kem bắn toả hình quạt từ một nòng súng nhỏ ở góc dưới trái; (3) Rocket Barrage (thảm tên lửa): 3 quả tên lửa nhỏ vây đỏ-kem bay chéo lên với vệt khói tròn trắng; (4) Target Lock (đánh dấu nhiều mục tiêu): tâm ngắm tròn với 4 ngoặc khoá góc màu đỏ san hô #E65A52 và chấm đỏ ở giữa. Hàng 2: (5) Rocket Punch: một nắm đấm máy đồng thau bay ngang với lửa phản lực phía sau; (6) Deploy Turret (đặt tháp súng): một tháp súng máy nhỏ dễ thương trên ba chân đứng trên cỏ, nòng ngắn, đèn vàng; (7) EMP Burst (sóng điện gây choáng quanh người): một bánh răng ở giữa với vòng sóng điện tròn nổ ra và tia sét zigzag vàng; (8) Orbital Cannon (tuyệt kỹ): một vệ tinh nhỏ trên cao bắn cột tia sáng vàng thẳng xuống đất tạo vòng nổ. Không chữ, không số, không ký tự, không khung vuông.
```

**Kết quả 21/11:** lượt 1 (`mechanic-skill-icons-soft-fantasy-chatgpt-1.png`) đúng 8 hình nhưng nền đĩa đỏ cam / nâu hồng, gần Warrior và Fighter. Lượt 2 (đang cài, `-chatgpt-2.png`), prompt nguyên văn:

```
Create an image. Bộ MECHANIC (ảnh thứ 3 bạn vừa vẽ): giữ nguyên 8 biểu tượng, thứ tự và bố cục 4 x 2, nhưng nền trong mỗi đĩa đổi sang VÀNG MẬT ONG SÁNG pastel (trên #FFE08A, dưới #F2B544, bóng đồng thau #C8893A), highlight kem #FFF1C9; KHÔNG đỏ, KHÔNG hồng, KHÔNG nâu tím, KHÔNG cam đậm, để khác hẳn bộ Warrior (đỏ) và Fighter (cam). Biểu tượng giữ màu đồng thau / kem / trắng, lửa và tia sáng vàng kem (không đỏ), riêng 4 ngoặc khoá của Target Lock vẫn đỏ san hô. Tông RO Soft Fantasy sáng như bộ Cleric ở ảnh 4. Nền ngoài phẳng một màu #FF00FF, không caro, không vòng viền quanh đĩa, không chữ, không số.
```
