# Warrior RO Fantasy: đòn chém nặng 8 hướng x 8 frame (20/11/2026)

Chat đạo diễn động tác (tài khoản Tay "Tân Korris", chỉ chữ; ảnh bị chặn tới 21:32): https://chatgpt.com/c/6afffe9b-6ea4-83ec-8332-cef3cf9d50b2
Ảnh gửi kèm ở tin đầu: `src/body-warrior-idle-5dir-chatgpt-1.png`, `docs/reference/hero-idle-down-5-classes.png`,
`docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png`, `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png`.

## Nhịp (400 ms, `ComboConfigs.WarriorHits` 120 / 90 / 190; frame = PhaseProgress x 8, mỗi frame 50 ms)

| Frame | ms | Pose |
|---|---|---|
| 0 | 0-50 | lấy đà 1: tay phải bắt đầu kéo lên sau vai phải, thân vặn ngược 10° |
| 1 | 50-100 | lấy đà 2 (KEY): nắm tay phải cao nhất sau/bên phải đầu, chân trụ hạ thấp |
| 2 | 100-150 | chém (KEY, sát thương 120 ms): tay phải vươn mạnh về mục tiêu, vai + hông xoay 20-30°, bước tới ngắn |
| 3 | 150-200 | chạm: tay phải duỗi hết, thân chúi về mục tiêu |
| 4 | 200-250 | đà chém (KEY): nắm tay phải thấp qua phía hông bên kia |
| 5 | 250-300 | giữ đà, áo choàng văng rõ nhất |
| 6 | 300-350 | thu thế (KEY): tay phải về cạnh hông, chân thu |
| 7 | 350-400 | tư thế thủ như idle |

Hướng F1 của kiếm (ChatGPT): xuống 240° (↙), chéo xuống-phải 315°, phải 0°, chéo lên-phải 45° ra xa camera, lên 90° ra xa camera,
chéo lên-trái 135°, trái 180°, chéo xuống-trái 225° (0° = phải màn hình, ngược chiều kim đồng hồ). Tay trái + khiên giữ trước ngực/hông,
lệch tối đa 5-8 % chiều cao. Ba hướng trái vẽ riêng, kiếm vẫn ở TAY PHẢI (tay xa camera khi quay trái).

## Ảnh cần vẽ (8 ảnh, mỗi ảnh 1 hướng)

Tên file: `src/body-warrior-attack-<hướng>-8f-chatgpt-N.png`, hướng: down, down-right, right, up-right, up, up-left, left, down-left.
1536 x 1024, nền #00FF00, 2 hàng x 4: hàng 1 = frame 0-3, hàng 2 = frame 4-7.

Mẫu prompt (thay <HƯỚNG> và câu mô tả hướng):

> VẼ ẢNH: vẽ mới hoàn toàn từ đầu, không dựa trên ảnh cũ nào. Sprite sheet ngang 1536x1024, nền một màu phẳng #00FF00 (không caro, không
> trong suốt, không bóng đổ dưới chân), 2 hàng x 4 cột, cách đều, không chạm mép, không chữ, không khung. CẢ 8 Ô CÙNG MỘT HƯỚNG: <HƯỚNG>.
> Đọc theo hàng: ô 1-8 = frame 0-7 của một nhát chém nặng một tay (bảng nhịp: lấy đà 1, lấy đà 2 tay phải cao nhất, chém, chạm, đà chém,
> giữ đà, thu thế, thủ). Chuyển động liên tục, mỗi frame đổi dần từ frame trước (in-between mượt, không nhảy pose). Nhân vật: đúng Warrior
> ảnh 1 (đầu trọc có mặt, giáp bạc viền vàng, khăn + áo choàng đỏ, găng da nâu, giày giáp), cùng cỡ, đầu rộng khoảng 43% chiều cao người,
> thân cao như ảnh 1, chân chạm cùng một đường đáy, không phóng to đầu. KHÔNG vẽ kiếm, khiên, tóc, hiệu ứng: hai tay là nắm đấm găng nâu
> khép chặt như đang cầm chuôi (game gắn kiếm vào nắm tay PHẢI, khiên vào nắm tay TRÁI). Nắm tay phải luôn thấy rõ, không bị đầu che.
> Tông RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối.

## Cài sau khi có đủ 8 ảnh

1. `python Tools/build-ro-fantasy-hero-game-assets.py write warrior`, `fists warrior`; đọc nắm tay trên
   `docs/reference/ro-fantasy-heroes-ingame/fists-warrior-attack.png`, ghi sửa vào `FISTS` của `Tools/ro_fantasy_attack8_grips.py`.
2. `grips warrior`, `check warrior`; đọc góc kiếm từng frame trên `warrior-attack-8dir.png`, ghi `SWORD` / `SWORD_BEHIND`, chạy lại.
3. Lệnh `write` / `grips` cũng ghi lại idle, walk (kết quả lệch vài px so với bản đã commit kể cả khi không đổi tool): chỉ giữ phần attack,
   trả idle / walk / hand idle-walk về bản git, ghép grips + head attack mới vào `class.json` / `body.json` của git.
