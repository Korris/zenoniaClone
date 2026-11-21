# Assassin skill icons, RO Soft Fantasy (21/11/2026)

Làm theo mẫu Cleric (`../../cleric/skill-icons-src/prompt-soft-fantasy-icons.md`). Tài khoản: Tay ("Tân Korris", Plus) hoặc Tan Pham (Plus). Chat (21/11, Tan Pham, chung 1 chat cho 5 class, 4 ảnh mẫu ở tin đầu, các class sau mở đầu bằng "Vẫn dùng 4 ảnh mẫu ở tin đầu."): https://chatgpt.com/c/6b0106e1-80d0-83ec-9ff9-7bb6b5ee32fa

Ảnh gửi kèm (theo thứ tự, ở tin đầu của chat):
1. `docs/reference/ui-style-ro-soft-fantasy/04-skill.png` (kiểu icon tròn cửa sổ Kỹ năng)
2. `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png` (tông RO Soft Fantasy)
3. `docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png` (tông nhân vật người dùng chọn)
4. `docs/reference/cleric-skill-icons-soft-fantasy/after-soft-fantasy-icons.png` (bộ icon Cleric đã duyệt, kiểu phải khớp)

Thứ tự skill = `SkillTable.Assassin.cs` index 0-7. Game dùng `Sprites/classes/assassin/skills/<slug>/icon.png` 128 px (`SkillIcons.Get`). Nền #00FF00.

Sau khi có ảnh: lưu `assassin-skill-icons-soft-fantasy-chatgpt-1.png` ở thư mục này, xem ảnh, rồi
`py -3 Tools/build-soft-fantasy-skill-icons.py cut assassin Assets/ArtLibrary/classes/assassin/skill-icons-src/assassin-skill-icons-soft-fantasy-chatgpt-2.png`
và `py -3 Tools/build-soft-fantasy-skill-icons.py preview assassin docs/reference/skill-icons-soft-fantasy/assassin-after-soft-fantasy-icons.png`.

Prompt (một dòng, gửi nguyên văn):

```
Create an image. Vẽ mới hoàn toàn từ đầu, không dựa trên ảnh trước. ASSASSIN SKILL ICONS cho game RPG mobile, tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối (xem ảnh 2 và ảnh 3). Kiểu icon y hệt bộ icon Cleric ở ảnh 4 (cùng cỡ đĩa, cùng độ sáng, cùng nét vẽ, cùng highlight) và các icon tròn trong ảnh 1: mỗi icon là MỘT ĐĨA TRÒN vẽ kín, nền trong đĩa là gradient màu mềm, biểu tượng to rõ ở giữa, khối tròn đầy, có highlight mềm, nhưng KHÔNG vẽ vòng viền/khung/nhẫn quanh đĩa (game tự thêm vòng ngà). Bố cục: lưới 4 cột x 2 hàng, 8 đĩa CÙNG CỠ, cách nhau rộng, trên nền phẳng một màu #00FF00 (xanh lá chói đặc, không caro, không trong suốt, không bóng đổ ra nền). Màu chủ đạo class Assassin: tím oải hương pastel #9C7FD9 / #BBA2EE, tử đinh hương #E8DCFF, điểm vàng kem #FFE6A0, viền nâu tím đậm mềm; không đen kịt, không tông tối. Mỗi icon khác hẳn hình dáng, đọc rõ ở cỡ 64 px: Hàng 1: (1) Shadow Strike (2 nhát chém nhanh): một con dao găm với hai vệt chém chéo hình chữ X màu tím nhạt; (2) Shadow Step (dịch chuyển ra sau lưng địch): bóng người tím tan thành làn khói ở trái, mũi tên cong vòng sang phải và dấu chân sáng; (3) Blade Dance (múa dao quanh người): 4 con dao găm nhỏ bay vòng tròn quanh một xoáy tím; (4) Phantom Rush (3 cú lao liên tiếp): 3 dáng dao găm mờ dần nối tiếp theo đường zigzag lao về phía trước. Hàng 2: (5) Venom Daggers (ném dao độc hình quạt): 3 con dao phi xoè quạt, lưỡi nhỏ giọt độc màu ngọc lam-tím, vài giọt độc tròn; (6) Shadow Clone (phân thân): hai ninja nhỏ dễ thương đứng cạnh nhau, một người thật, một bản sao tím trong mờ; (7) Death Mark (ấn đánh dấu tăng sát thương): ấn ký đầu lâu cách điệu dễ thương màu đỏ san hô #E65A52 nằm trong vòng ấn phép tròn, không đáng sợ; (8) Nightfall Execution (tuyệt kỹ chém quanh người): vầng trăng lưỡi liềm lớn màu kem trên nền đêm tím, nhiều vệt chém tím cắt ngang qua trăng. Không chữ, không số, không ký tự, không khung vuông.
```

**Kết quả 21/11:** lượt 1 (`assassin-skill-icons-soft-fantasy-chatgpt-1.png`) đạt trừ Phantom Rush (3 bóng người tím đậm trên nền tím, chìm ở 64 px). Lượt 2 (đang cài, `-chatgpt-2.png`), prompt nguyên văn:

```
Create an image. Bộ ASSASSIN (nền xanh lá) bạn vừa vẽ: giữ nguyên các đĩa 1, 2, 3, 5, 6, 7, 8 và bố cục 4 x 2, nền ngoài phẳng #00FF00. Chỉ vẽ lại đĩa (4) Phantom Rush (3 cú lao liên tiếp) vì bóng người tím đậm trên nền tím bị chìm, khó đọc ở 64 px: thay bằng MỘT con dao găm sáng màu kem-trắng lưỡi to chĩa sang phải, phía sau là 2 bản sao dao mờ dần (tím nhạt, trong mờ) nối theo đường zigzag 3 đoạn màu vàng kem #FFE6A0, không vẽ người. Tương phản rõ, hình khối đặc, tông RO Soft Fantasy sáng như ảnh 4, không vòng viền quanh đĩa, không chữ.
```
