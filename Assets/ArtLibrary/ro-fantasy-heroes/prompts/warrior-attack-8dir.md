# Warrior RO Fantasy: đòn chém nặng 8 hướng x 8 frame (20/11/2026, vẽ xong 22/11)

Chat đạo diễn động tác (tài khoản Tay "Tân Korris", chỉ chữ; ảnh bị chặn tới 21:32): https://chatgpt.com/c/6afffe9b-6ea4-83ec-8332-cef3cf9d50b2
Ảnh gửi kèm ở tin đầu: `src/body-warrior-idle-5dir-chatgpt-1.png`, `docs/reference/hero-idle-down-5-classes.png`,
`docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png`, `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png`.

## Nhịp (400 ms, `ComboConfigs.WarriorHits` 120 / 90 / 190; frame = PhaseProgress x 8, mỗi frame 50 ms)

| Frame | ms | Pose (bản 22/11: cả người chém, KHÔNG đứng yên vung tay) |
|---|---|---|
| 0 | 0-50 | lấy đà: dồn trọng tâm về chân sau, gối khuỵu, vai + hông vặn ngược, nắm tay phải kéo về sau vai phải |
| 1 | 50-100 | đỉnh lấy đà (KEY): nắm tay phải cao nhất sau/trên vai phải (cách viền sọ), thân cuộn như lò xo, chân trước nhấc lên |
| 2 | 100-150 | chém (KEY, sát thương 120 ms): BƯỚC DÀI chân trước về hướng chém, vai + hông xoay mạnh, tay quật xuống, áo choàng bay ra sau |
| 3 | 150-200 | chạm: thế lao sâu nhất, thân chúi ~15°, tay phải duỗi hết về mục tiêu ngang ngực-bụng |
| 4 | 200-250 | đà chém (KEY): nắm tay phải quét thấp qua hông bên kia, vai xoay quá đà |
| 5 | 250-300 | giữ đà: vẫn thấp, gót chân sau nhấc, áo choàng văng tới nhiều nhất |
| 6 | 300-350 | thu thế (KEY): đứng thẳng dần, chân sau bước theo, nắm tay phải về cạnh hông |
| 7 | 350-400 | tư thế thủ như idle |

Bảng 20/11 (4 key pose, tay vung nhưng chân gần như đứng yên) bị người dùng chê 22/11: "chém giống đứng yên cầm kiếm múa quá".

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

### Ảnh gửi kèm mỗi hướng (đã chuẩn bị 20/11 19:00)

Ảnh 1 = idle CÙNG HƯỚNG `ref/warrior-attack8/idle-<hướng>.png` (cắt từ `src/body-warrior-idle-5dir-chatgpt-1.png`; 3 hướng trái là
ảnh lật của hướng phải tương ứng, chỉ để làm mẫu cỡ), ảnh 2 = `docs/reference/hero-idle-down-5-classes.png`, ảnh 3-4 = ảnh tông
`sample-hero-user-pick.png` + `01-style-board.png` (chỉ cần ở tin đầu của chat). Đo trên idle: đầu rộng 41-43 % chiều cao người
(xuống 0,43, chéo xuống 0,42, ngang 0,41, chéo lên 0,42, lên 0,43).

Câu mô tả hướng (thay <HƯỚNG>):
- down: "nhìn XUỐNG về người xem, thấy mặt; kiếm (tay phải = bên TRÁI tranh) kéo lên sau vai phải rồi chém chéo xuống về phía người xem"
- down-right: "nhìn chéo xuống-phải 3/4, thấy mặt, tay phải là tay GẦN người xem; chém chéo xuống-phải"
- right: "nhìn ngang sang PHẢI, thấy 1 mắt, tay phải là tay GẦN người xem; chém ngang sang phải"
- up-right: "3/4 LƯNG quay chéo lên-phải, thấy gáy, KHÔNG thấy mắt; tay phải ở bên PHẢI tranh, chém ra xa người xem về phía trên-phải"
- up: "nhìn LÊN, thấy lưng + gáy, KHÔNG thấy mặt; tay phải ở bên PHẢI tranh, chém ra xa người xem"
- up-left: "3/4 LƯNG quay chéo lên-trái, thấy gáy, KHÔNG thấy mắt; tay phải (cầm kiếm) ở bên PHẢI tranh, là tay XA, chém về phía trên-trái; KHÔNG lật ảnh hướng lên-phải"
- left: "nhìn ngang sang TRÁI, thấy 1 mắt; tay PHẢI (cầm kiếm) là tay XA người xem, nằm sau thân, tay trái (khiên) GẦN người xem; chém ngang sang trái bằng tay xa; KHÔNG lật ảnh hướng phải"
- down-left: "nhìn chéo xuống-trái 3/4, thấy mặt; tay PHẢI (cầm kiếm) là tay XA người xem; chém chéo xuống-trái bằng tay xa; KHÔNG lật ảnh hướng chéo xuống-phải"

## Cài sau khi có đủ 8 ảnh

1. `python Tools/build-ro-fantasy-hero-game-assets.py write warrior`, `fists warrior`; đọc nắm tay trên
   `docs/reference/ro-fantasy-heroes-ingame/fists-warrior-attack.png`, ghi sửa vào `FISTS` của `Tools/ro_fantasy_attack8_grips.py`.
2. `grips warrior`, `check warrior`; đọc góc kiếm từng frame trên `warrior-attack-8dir.png`, ghi `SWORD` / `SWORD_BEHIND`, chạy lại.
3. Lệnh `write` / `grips` cũng ghi lại idle, walk (kết quả lệch vài px so với bản đã commit kể cả khi không đổi tool): chỉ giữ phần attack,
   trả idle / walk / hand idle-walk về bản git, ghép grips + head attack mới vào `class.json` / `body.json` của git.

## Đã vẽ 22/11 (Tay "Tân Korris", tiếp trong chat đạo diễn ở trên)

Mỗi hướng 1 tin: đính kèm idle cùng hướng đã làm sạch (`ref/warrior-attack8/clean/idle-<hướng>.png`: bỏ dải đen + hình bên cạnh), tin đầu
thêm `docs/reference/hero-idle-down-5-classes.png`; prompt = mẫu ở trên với bảng 22/11 viết thành câu + câu đường nắm tay phải từng hướng
(F1 ở đâu, F2-F3 quật về đâu, F4-F5 quét sang đâu, bước dài về đâu). Ảnh dùng: down, down-right, right, left, down-left `-1`; up, up-left,
up-right `-2`. Vẽ lại vì: up-1 xoay sang phải và đấm ngang; up-left-1 tay TRÁI (vai gần) duỗi ra đấm; up-right-1 F1 nắm tay chồng lên gáy.
Câu đã giúp: "chỉ TAY PHẢI (tay XA) vung chém; tay trái (tay GẦN) luôn giữ nắm đấm trước ngực, KHÔNG duỗi ra", "F1 nắm tay giơ cao nằm HẲN
bên cạnh đầu, cách viền sọ", "lưng đối diện người xem, hai vai cân đối, KHÔNG bước / đưa tay sang ngang". Lưu ý hướng chéo xuống-trái: tay
phải ở bên TRÁI tranh (tay xa), không phải bên phải.

Cài: `Tools/ro_fantasy_attack8_cut.py` (khung 7 = cao idle, đầu lệch > 4 % thì co / giãn riêng đầu; mỗi frame thêm độ lao `TRAVEL`
dọc hướng nhìn), nắm tay đọc tay `FISTS` + góc kiếm `SWORD` / `SWORD_BEHIND` trong `Tools/ro_fantasy_attack8_grips.py`. Sau `write` /
`grips`: trả `body/walk.png`, `hand/idle|walk.png` về git, ghép phần attack của `body.json` / `class.json` vào bản git (chỉ attack).
Ảnh kiểm: `docs/reference/warrior-attack-8dir/`.
