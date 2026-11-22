# Tỷ lệ nhân vật + lực đòn đánh (ChatGPT, 22/11/2026)

Người dùng 22/11: "có vẻ nhân vật lùn lùn, khi đánh hoạt ảnh sprite nhìn nó không lực, nhân vật cao lên tí như Ragnarok".
Chat (Tay, "Tân Korris"): https://chatgpt.com/c/6b0243dc-91b8-83ec-afe6-bec308abaf1d (tiếp chat style guide Adventure).
Ảnh gửi: Warrior Adventure idle xuống (`adv-warrior-idle-down-chatgpt-1.png`), 8 frame đánh xuống đang dùng (hàng đầu `ro-fantasy-heroes-ingame/warrior-attack-8dir.png`), dải chụp trong game 2 lần chém (`warrior-attack-8dir/ingame-down-right-2-swings.jpg`).

**Trạng thái: ĐỀ XUẤT, chờ người dùng duyệt.** Chưa đổi code / sprite trong game.

## Phán quyết

Nên cao lên, nhưng VỪA PHẢI (+0,2-0,3 đầu). Đòn đánh thiếu lực là vấn đề lớn hơn chuyện lùn: kiếm vung rộng nhưng chân, hông, trọng tâm không truyền lực ("giống đổi tư thế cầm kiếm hơn là dùng toàn thân chém").

## 1. Tỷ lệ các bản Ragnarok (ước lượng art-direction, không phải số chính thức)

| Game | Chiều cao (số đầu) | Đầu / chiều cao | Cảm giác |
|---|---|---|---|
| Ragnarok Online (RO1, sprite) | ~2,3-2,7 | 37-43 % | chibi cổ điển, thân nhỏ |
| Ragnarok M: Eternal Love | ~2,5-2,9 | 34-40 % | chibi 3D, thân và chân rõ hơn |
| Ragnarok Origin | ~2,5-2,9 | 34-40 % | anime chibi, gọn, năng động |
| Ragnarok 3 (RO3) | ~2,5-2,9 | 34-40 % | RO cổ điển nhưng khối rõ (trailer 2025, test 06/2026: vẫn chibi, không anime người lớn) |
| Warrior hiện tại | 2,33-2,44 | 41-43 % | dễ thương nhưng nén chiều cao |
| **Warrior V2 đề xuất** | **2,65** | **37,7 %** | chibi chiến binh, vẫn dễ thương |

Ta không sai tỷ lệ RO, chỉ nằm ở đầu "đầu to, thân ngắn" của vùng RO1; muốn cảm giác RO3 + đòn nặng thì thêm 0,2-0,3 đầu.

## 2. Tỷ lệ đích V2 (Adventure)

| Phần | Hiện tại | V2 |
|---|---|---|
| Số đầu | 2,33-2,44 | 2,65 |
| Đầu / chiều cao | 41-43 % | 37,7 % |
| Tổng chiều cao (giữ cỡ đầu) | - | +8-12 % |
| Đầu | to, tròn | giữ gần nguyên cỡ |
| Thân cằm-hông | hơi ngắn | dài thêm ~6-8 % |
| Chân hông-bàn chân | hơi nén | dài thêm ~12-16 % |
| Chân / tổng cao | ~31-33 % | 35-37 % |
| Vai / bề ngang đầu | ~1,15-1,2 | 1,20-1,25 |
| Bàn chân | tròn, rộng | vẫn to, bớt ủng cồng kềnh |

Giới hạn khoá (vượt là thành "anime mini-adult"): ≤ 2,75 đầu; đầu ≥ 36 %; chân ≤ 38 %; vai ≤ 1,3 lần bề ngang đầu.

Camera 58° / hero 69 px: giữ world scale, cao thêm 10 % => hero ~76 px. Tăng số đầu không tự thêm pixel: nếu giữ 69 px thì đầu còn ~26 px (thay vì 29), thân/chân có thêm chỗ diễn. Hiển thị 75-76 px thì mặt giữ độ rõ, vai/hông/bước chân dễ đọc hơn, lunge và follow-through rõ hơn. ChatGPT chọn test 76 px trước, kiểm thêm 69 px ở boss đông người (che VFX, mục tiêu).

## 3. Vì sao 8 frame đánh hiện tại không có lực

| Frame | Quan sát | Sửa |
|---|---|---|
| f0 | kiếm giơ chéo, người thẳng | hạ người 3-4 %, vai xoay ngược chiều chém |
| f1 | kiếm rất cao, chân nhấc | anticipation tốt nhưng như sắp nhảy: chân trụ chắc, chân sau chỉ nhấc gót |
| f2 | kiếm ngang đầu, bắt đầu lao | tay tách khỏi thân: xoay ngực, hông; đầu nhìn mục tiêu |
| f3 | kiếm đã xuống thấp | thiếu hình nối cao-thấp: smear hẹp, mạnh (không cần VFX lớn) |
| f4 | khiên che thân, kiếm xuống-phải | khiên che mất lực xoay: vai kiếm ra trước, khiên lệch sang bên |
| f5 | gần giống f4 | thiếu overshoot: kiếm vượt thêm 10-15°, vai + áo choàng trễ theo |
| f6 | về tư thế thường nhanh | cho hồi người, giày sau tiếp đất |
| f7 | gần idle | giữ đủ lâu để thấy đòn đã xong |

Lỗi gốc: thiếu chuỗi truyền lực **chân đạp -> hông xoay -> vai kéo -> tay vung -> va chạm -> lực dư -> hồi**. Thêm trail hay rung camera một mình không chữa được.

### Timeline đề xuất (8 frame, 480 ms, impact ~210 ms)

| Frame | ms | Việc |
|---|---|---|
| f0 | 0-60 | nén gối, chuẩn bị kéo kiếm |
| f1 | 60-130 | wind-up cực trị, kiếm cao |
| f2 | 130-175 | vai/hông xoay, đạp chân |
| f3 | 175-210 | smear tốc độ cao, tiến sát mục tiêu |
| f4 | 210-255 | IMPACT, kiếm qua mục tiêu |
| f5 | 255-320 | follow-through, vượt đà |
| f6 | 320-400 | đặt chân, thu vai/kiếm |
| f7 | 400-480 | về idle |

Sát thương server khớp lúc kiếm chạm (impact), không cố định máy móc.

**Đối chiếu code hiện tại (Claude đọc, ChatGPT chưa biết):** `ComboConfigs.WarriorHits` = 1 chém **400 ms**, sát thương ở **120 ms** (comment code: hết windup = art f1); hit-stop thường 45 ms (`FeelConfig.HitStopNormalMs`), rung `TraumaNormal` 0,05 (gần như không thấy), đòn thường không rung/flash finisher (`HeroController.LockTarget.cs`). ChatGPT đề xuất impact ở ~45 % chu kỳ (210/480; ở 400 ms ~ 175 ms) và tư thế impact f4 trùng đúng lúc đó: cần kiểm frame nào đang hiện lúc hit-stop trong game (chưa kiểm) rồi khớp tư thế impact với mốc sát thương. Đổi thời gian này là đổi bảng dùng chung với server => build lại server.

### Sửa trong sprite

- Anticipation: nén thân 3-4 % ở f0-f1, vai xoay ngược 10-15°; f1 kiếm cao nhưng khuỷu tay không che đầu.
- Smear f3: kiếm / vệt dài 1,15-1,25 lần bình thường. Squash/stretch chỉ 3-5 % (thân, áo choàng).
- Impact f4: thân nghiêng trước 8-12°, vai kiếm vươn mạnh. Overshoot f5: +10-15°.
- Chân trước tiến, chân sau đạp + xoay gót; hông và ngực dịch theo hướng chém (không giữ một tâm mọi frame).
- Khiên lùi/lệch ở f2-f5, không che ngực và tay kiếm; áo choàng trễ 1 frame, bung ở f5, rơi ở f6-f7.
- Giữ 8 frame (không cần 10-12); vẽ lại chủ yếu f1, f3, f4, f5. Hướng chéo giữ cùng timing va chạm nhưng xoay vai / chân / áo theo hướng đó, không mirror nguyên hướng xuống.

### Làm bằng FX / engine (theo H = chiều cao hero)

| Hiệu ứng | Thông số | Lúc |
|---|---|---|
| Lunge (dịch root) | 0,18-0,25H ≈ 13-19 px | f2-f4 |
| Hit-stop | 35 ms (nằm TRONG chu kỳ, không cộng vào cooldown/server) | impact |
| Rung camera | 1,5-2 px, 65-85 ms, tắt dần | impact |
| Mesh trail chém | quạt 100-130°, bán kính 1,0-1,3H, lõi sáng mảnh | f3-f5 |
| Flash quái trúng | trắng ngà 60-80 %, 65 ms | trúng |
| Sao impact | 0,25-0,4H, ngắn | impact |
| Knockback quái | 0,10-0,16H ≈ 7-12 px trong 80-110 ms | sau impact |
| Bụi chân | 0,15-0,25H, alpha 0,2-0,35 | f2, f4 |
| Số damage | sau hit 30-50 ms | |

Tránh: lunge root + lunge vẽ trong sprite cộng dồn; rung mỗi frame (chỉ impact, giảm khi boss đông, có tuỳ chọn tắt rung); flash dài; phóng FX chém to để bù anim yếu (4-6H để dành cho skill); khiên trượt khỏi tay.
(Ghi chú: knockback quái đi ngược quyết định 20/11 "đòn khoá mục tiêu không đẩy" - chỉ cân nhắc dạng giật hình ngắn, không đẩy vị trí thật.)

Ba việc tác động lớn nhất theo thứ tự: (1) tỷ lệ 2,65 đầu, (2) sửa f1 + f4 cho toàn thân truyền lực, (3) lunge + hit-stop + flash khớp impact. Quan trọng hơn tăng mật độ VFX.

## 4. Ảnh TEST

- `warrior-v2-taller-proportions-attack-keys-chatgpt-1.png` (1536x1024, nền #00FF00): trái -> phải: bản hiện tại, V2 idle, V2 wind-up, V2 impact (smear vẽ trên lưỡi), V2 follow-through.
- `warrior-proportions-current-vs-v2-game-size.png`: Claude ghép, hàng 1x cỡ game (hero hiện tại 69 px; V2 cùng cỡ đầu ~74 px; thêm V2 ép về 69 px) và hàng 2x nearest; trái là sprite Fantasy đang chạy (idle + f1/f3/f5).

Đo trên ảnh ChatGPT (Claude): bản hiện tại cao 423 px, đầu 172 px = 40,7 % (2,46 đầu); V2 idle cao 455 px, đầu 174 px = 38,2 % (**2,61 đầu**), cùng cỡ đầu, cao hơn 7,6 % => gần đúng đích 2,65.

Claude xem ảnh (1x và 2x):
- Đạt: V2 ra dáng chiến binh hơn rõ, chân và vai đọc được ở 1x; vẫn chibi RO, mặt giữ độ rõ; wind-up đọc tốt (kiếm cao, trọng tâm thấp, chân dạng).
- Chưa đạt: impact và follow-through gần giống nhau (cùng kiếm thấp bên phải), cung vung chưa đủ rộng; smear quá mảnh, ở 1x gần như không thấy; kiếm V2 ngắn hơn kiếm Fantasy đang chạy nên ở 1x tầm vung nhỏ hơn => khi vẽ thật phải giữ chiều dài kiếm như sprite game (hoặc dài hơn) và tách hẳn 3 tư thế (wind-up cao sau vai -> impact thấp trước mặt, thân đổ trước -> follow-through vượt sang bên kia).
- Đây là ảnh tỷ lệ, chưa phải frame cài: cài thật phải theo ô 320x256, feetY 244, anchor tay / tóc nhiều lớp (đổi chiều cao thân => đổi `head-anchors.json`, `held-anchors`, tool `build-ro-fantasy-hero-game-assets.py`), và kiểm tỷ lệ đầu từng frame ≤ 5 % (điều 8).

## Prompt test (ChatGPT viết, dùng lại khi vẽ thật)

Tóm tắt: dùng idle Adventure làm master + style guide V1.0; sheet 1536x1024 nền #00FF00 phẳng, 5 nhân vật cùng đường đáy, không chữ / particle; (1) bản hiện tại ~2,38 đầu, đầu 42 %; (2) V2 idle 2,65 đầu, đầu 37,7 %, giữ cỡ đầu, thân +6-8 %, chân +12-16 % (chân ~36 %), vai 1,23 lần đầu, giày gọn hơn, cao hơn ~10 %; (3) wind-up: kiếm cao về sau, hạ trọng tâm 3-4 %, vai xoay ngược, chân trụ chắc, gót sau nhấc, khiên lùi không che ngực, không nhảy; (4) impact: chân trước bước ra, hông ngực xoay mạnh, nghiêng trước ~10°, chém cao xuống thấp, smear vẽ trực tiếp ~1,2 lần lưỡi, khiên lệch lộ thân; (5) follow-through: kiếm vượt thêm ~12°, vai hông còn xoay theo đà, áo choàng bung trễ, chưa đứng thẳng. Nhấn: đầu 5 nhân vật cùng cỡ tuyệt đối. Bản đầy đủ trong canvas của chat trên.
