# Fighter skill icons, RO Soft Fantasy (21/11/2026)

Làm theo mẫu Cleric (`../../cleric/skill-icons-src/prompt-soft-fantasy-icons.md`). Tài khoản: Tay ("Tân Korris", Plus) hoặc Tan Pham (Plus). Chat (21/11, Tan Pham, chung 1 chat cho 5 class, 4 ảnh mẫu ở tin đầu, các class sau mở đầu bằng "Vẫn dùng 4 ảnh mẫu ở tin đầu."): https://chatgpt.com/c/6b0106e1-80d0-83ec-9ff9-7bb6b5ee32fa

Ảnh gửi kèm (theo thứ tự, ở tin đầu của chat):
1. `docs/reference/ui-style-ro-soft-fantasy/04-skill.png` (kiểu icon tròn cửa sổ Kỹ năng)
2. `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png` (tông RO Soft Fantasy)
3. `docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png` (tông nhân vật người dùng chọn)
4. `docs/reference/cleric-skill-icons-soft-fantasy/after-soft-fantasy-icons.png` (bộ icon Cleric đã duyệt, kiểu phải khớp)

Thứ tự skill = `SkillTable.Fighter.cs` index 0-7. Game dùng `Sprites/classes/fighter/skills/<slug>/icon.png` 128 px (`SkillIcons.Get`). Nền #FF00FF.

Sau khi có ảnh: lưu `fighter-skill-icons-soft-fantasy-chatgpt-1.png` ở thư mục này, xem ảnh, rồi
`py -3 Tools/build-soft-fantasy-skill-icons.py cut fighter Assets/ArtLibrary/classes/fighter/skill-icons-src/fighter-skill-icons-soft-fantasy-chatgpt-1.png`
và `py -3 Tools/build-soft-fantasy-skill-icons.py preview fighter docs/reference/skill-icons-soft-fantasy/fighter-after-soft-fantasy-icons.png`.

Prompt (một dòng, gửi nguyên văn):

```
Create an image. Vẽ mới hoàn toàn từ đầu, không dựa trên ảnh trước. FIGHTER (võ sĩ đấm găng) SKILL ICONS cho game RPG mobile, tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối (xem ảnh 2 và ảnh 3). Kiểu icon y hệt bộ icon Cleric ở ảnh 4 (cùng cỡ đĩa, cùng độ sáng, cùng nét vẽ, cùng highlight) và các icon tròn trong ảnh 1: mỗi icon là MỘT ĐĨA TRÒN vẽ kín, nền trong đĩa là gradient màu mềm, biểu tượng to rõ ở giữa, khối tròn đầy, có highlight mềm, nhưng KHÔNG vẽ vòng viền/khung/nhẫn quanh đĩa (game tự thêm vòng ngà). Bố cục: lưới 4 cột x 2 hàng, 8 đĩa CÙNG CỠ, cách nhau rộng, trên nền phẳng một màu #FF00FF (magenta đặc, không caro, không trong suốt, không bóng đổ ra nền). Màu chủ đạo class Fighter: cam mơ pastel #F39A4E / #FFBC7A, đào #FFD9B8, điểm vàng kem #FFE6A0, viền nâu đậm mềm; khác Warrior (đỏ san hô) nên dùng cam mơ, không đỏ. Mỗi icon khác hẳn hình dáng, đọc rõ ở cỡ 64 px: Hàng 1: (1) Dash Punch (lao tới đấm thẳng): nắm đấm đeo găng lao ngang sang phải với vệt tốc độ; (2) Iron Fist (tự tăng công): nắm đấm giơ lên bọc kim loại sáng bóng, hào quang vàng kim và 2 mũi chevron hướng lên; (3) Ground Slam (đập đất hất tung quái): hai nắm đấm đập xuống mặt đất, đá vụn bắn lên, vòng sóng trên đất; (4) Rapid Jabs (5 cú đấm liên hoàn): 4-5 nắm đấm nhỏ chồng nối tiếp xoè quạt với các ngôi sao nổ nhỏ. Hàng 2: (5) Shockwave (sóng đấm bay xuyên): sóng xung kích hình vòng cung bay ngang ra từ một nắm đấm; (6) Tiger Rush (3 cú lao kèm móc): đầu hổ cách điệu dễ thương gầm với 3 vệt cào móng vuốt; (7) Iron Body (khiên + tăng phòng thủ): giáp ngực sắt sáng với vòng bảo vệ xanh lam nhạt #9DDAEC và viền vàng; (8) Dragon Fist (tuyệt kỹ nhảy lên đấm xuống): nắm đấm có con rồng phương Đông cam vàng cuộn quanh tay, lửa cam. Không chữ, không số, không ký tự, không khung vuông.
```
