# Hệ pet + gacha + bắt pet (bản thu gọn y chang Ragnarok: Midgard Senki / Pocket Saga)

Nguồn: ChatGPT chốt 16/11/2026, `docs/reference/gacha-discussion-chatgpt.md` Lần 6–8 (chat Tay). Mọi số là số thiết kế cho game mình trừ khi ghi "RO". Không tiền nạp, không trade: mọi tài nguyên pet gắn tài khoản. Code: `Assets/Scripts/Rpg/PetConfig.cs`, `Rpg/Account/*`, `HeroController.Pet.cs`, `HeroPet*.cs`, `PetWindow*.cs`, `server-api/src/rpg/account-validation.js`.

## 1. Pet và sở hữu
- 10 loài: gacha 8 (N Frost Slime, Horn Rabbit; R Ice Goblin, Thorn Boar; SR Spectral Wolf, Marsh Stalker; SSR Crystal Golem; UR Frostfang King) + Legacy Spirit (Lv 5), Imp (3000 G). Mỗi loài sở hữu 1 lần theo tài khoản (không nhiều cá thể, không talent RNG).
- Mỗi pet: 1 nội tại (+5 % HP, +3 % sát thương PvE, +3 % tốc chạy, -3 % sát thương nhận, +5 % thuốc HP, +3 % tốc đánh...) và 1 skill 20 s (bảng Lần 7).
- Pet cũ (item độ bền): mở 1★ + 10 mảnh loài + 3000 G / loài, 1 lần.

## 2. Pet xuất chiến trên màn hình (Lần 8, câu 1)
- Thực thể có vị trí thật, KHÔNG Lerp theo chủ. Nghỉ 1,5 ô sau lưng chủ, lệch 0,6 ô. Chạy khi cách chủ > 2 ô, dừng khi cách đích <= 0,25 ô; 110 % tốc chủ, > 3 ô: 160 %, > 7 ô: teleport về cách chủ 1 ô. Đổi map: đặt cạnh chủ.
- Sheet quái gốc: Walk 4 hướng x 4 frame 8 FPS, Attack 4 frame 10 FPS (0,4 s; sát thương ở frame 3 = 300 ms), idle = frame đứng, hướng theo vector chạy. Legacy Spirit / Imp: ảnh tĩnh nhấp 2 px.
- Đánh: mục tiêu = quái chủ vừa đánh trúng, nhớ 4 s, nhận trong 6 ô; chạy tới (cận 1,2 ô, xa 4 ô) rồi Attack; **đánh thường: cú đầu ngay khi chủ vừa đánh trúng, rồi mỗi 1,5 s, = 25 % ĐÒN SAU GIÁP của chủ (tối thiểu 1; ChatGPT 16/11 06:41, trước đó 10 % trước giáp ra 1–2)**; skill mỗi 20 s (55–75 % đòn sau giáp); **giữa các cú pet đứng bám cạnh quái (Guard), không chạy về**; mục tiêu chết / chủ ngừng 4 s / chủ đi xa 6 ô -> về theo chủ; không tự chọn quái. Bị vách / bậc chắn (pet không có tìm đường): 250 ms không tiến thì hiện ra cạnh quái. Không trần DPS cứng, mục tiêu pet góp 15–25 % DPS chủ.
- Số sát thương pet: đánh thường xanh ngọc #66D5D0 cỡ 70 %, skill vàng nhạt #FFD77A cỡ 80 %; không burst to; có tuỳ chọn ẩn pet + số của người khác.
- Bất tử: không HP, không bị đánh, không chết, không hồi sinh, không nhặt đồ, không aggro, không chặn; sát thương pet không tính đóng góp (loot / EXP / vé / quyền bắt). Tên trên đầu chỉ pet của mình. 1 SFX nhỏ.
- Server: pet đang mặc, mục tiêu, nhịp đánh, cooldown, damage; client: Walk/Attack/Idle; máy trạng thái FOLLOW → CHASE → ATTACK → FOLLOW; chỉ gửi sự kiện bắt đầu đòn.

## 3. Đội hình 1 + 2 + 2 (RO: 1 + 2 + 5)
| Ô | Mở ở Lv | Hiệu lực |
|---|---|---|
| Xuất chiến | 5 | 100 % nội tại + đánh thường + skill (AI, vị trí) |
| Hỗ trợ 1 / 2 | 20 / 30 | skill tự động 50 % hiệu lực (FX tại chủ / mục tiêu, không chạy); làm chậm giữ thời lượng, giảm nửa mức |
| Thuộc tính 1 / 2 | 35 / 45 | 25 % nội tại |
- Một loài chỉ ở 1 ô. Đổi đội hình ở làng hoặc ngoài giao tranh 10 s. Đội hình theo nhân vật; sở hữu / level / sao theo tài khoản. Cùng loại nội tại không cộng quá 2 nguồn hỗ trợ.
- Trần tổng từ pet (sau mọi ô + Bond): +10 % sát thương PvE, +12 % HP, +10 % tốc đánh / chạy, -10 % sát thương nhận.

## 4. Gacha (tỉ lệ RO)
- Tiền: **Pet Crystal** (không nạp, không trade); 150 Crystal = 1 lượt, x10 = 1500 (không giảm giá). Vé thưởng trực tiếp = Coin.
- Tỉ lệ (RO): N 12 / R 24 / SR 36 / SSR 6,25 / UR 2,5 / **Mảnh Pet Chung 19,25 %** (5 mảnh). Trong độ hiếm chia đều. Legacy không vào pool.
- Mới -> 1★; trùng -> 10 mảnh loài; mảnh chung đổi 1:1 ra mảnh loài (chỉ để nâng sao).
- Pity (RO): 20 lượt chắc SSR+ (SSR 71,43 % : UR 28,57 %), 50 lượt chắc UR; SSR/UR reset đếm SSR, UR reset cả hai; đếm từng lượt, lưu tài khoản; x10 không bảo hiểm thêm; không banner. Pity cũ (10/30/60) migrate: giữ đếm, ghi lịch sử.
- Thu nhập Crystal (theo tài khoản): tân thủ 10 Coin (1 lần); Wanted lần đầu / ngày 150 (<= 4 / tuần); boss map 5 lần hợp lệ / tuần 150; mốc Lv 10..90 (2 vé/mốc cũ -> 300 Crystal / mốc); quái thường không rơi. Lặp lại = 5 lượt / tuần. Mảnh UR cũ (120 = Frostfang King) giữ.

## 5. Nuôi dưỡng
- **Level 1–60** bằng **Thức Ăn Pet** (quái thường 8 % rơi 1 túi, boss map 3 túi / lần hợp lệ, Wanted 10 túi / ngày / tài khoản; gắn tài khoản). Lv L -> L+1 tốn 2 + floor(L/10) túi, 100 %. Không vượt level cao nhất tài khoản. Mỗi level +0,5 % sát thương pet tự gây (đòn thường + skill đánh); nội tại không tăng theo level. Chỉ ảnh hưởng pet xuất chiến.
- **Sao 1–5** bằng mảnh loài + vàng: 10 + 5k, 20 + 10k, 30 + 20k, 40 + 30k (tổng 100 mảnh, 65k). Hệ số nội tại 100 / 110 / 120 / 130 / 140 %; skill không đổi. (Thay luật cũ +1 điểm % / sao.)
- Talent: KHÔNG. Bond: 4 cặp +2 % khi cả hai trong đội hình (Frost Slime + Ice Goblin: -2 % sát thương nhận; Horn Rabbit + Spectral Wolf: +2 % tốc chạy; Thorn Boar + Marsh Stalker: +2 % HP; Crystal Golem + Frostfang King: +2 % sát thương lên boss). Legacy không Bond.
- Reset level / sao: hoàn 100 % Thức Ăn / mảnh / vàng đã dùng theo sổ ghi, phí 0, không giới hạn. Trùng tự thành mảnh; không phân giải pet đang có.

## 6. Bắt pet ngoài map
- **Bóng Thường**: NPC 500 G, 5 / ngày / tài khoản; boss map rơi 10 %. **Bóng Hiếm**: 1 bóng / 28 lần nhận thưởng Wanted hợp lệ (tích luỹ), không bán; 100 % nếu đủ điều kiện + cấp sổ.
- Quái thường: còn sống, HP <= 50 %, người bắt đã gây sát thương trong 10 s, cách <= 4 ô; thất bại mất bóng; server khoá mục tiêu khi xử lý; quái chết trước khi xác nhận -> từ chối, không trừ bóng. Tỉ lệ Bóng Thường theo HP 26–50 % / 1–25 %: N 40/60, R 25/40, SR 12/20, SSR 4/8 %; UR không bắt khi sống.
- Boss map (MINI): chết -> xác bắt được 30 s; điều kiện >= 1 % MaxHP (hoặc party >= 3 % và có gây sát thương); 1 lần thử / tài khoản / lần chết; nhiều người cùng bắt được; tối đa 1 pet boss / ngày / tài khoản; Bóng Thường SSR 5 %, UR 1 % (Frostfang King); Bóng Hiếm 100 %.
- Wanted Boss (MVP): chết -> 45 s, chỉ người có quyền thưởng, 1 lần / ngày / tài khoản; SSR 3 %, UR 0,5 %; V1 chỉ chuẩn bị (3 Wanted Boss chưa có PetDef / sprite pet).
- Chỉ loài có PetDef mới bắt được. Loot boss giữ nguyên.

## 7. Sổ pet (Pet Collection)
| Cấp | Điều kiện (số loài khác nhau) | Mở |
|---|---|---|
| 1 | mặc định | bắt N / R |
| 2 | 3 | bắt SR |
| 3 | 5 | bắt SSR |
| 4 | 7 | bắt UR |
| 5 | 10 | danh hiệu Pet Master |
Loài chỉ tính lần đầu, mọi nguồn. Chưa sở hữu hiện bóng tối.

## 8. UI: 1 cửa sổ Pet, 5 tab
Đội hình (5 ô, tổng bonus, Bond) · Bộ sưu tập (10 pet, độ hiếm, nguồn, cấp sổ) · Nuôi dưỡng (level, sao, nguyên liệu, reset) · Gacha (x1/x10, tỉ lệ, pity, lịch sử, đổi mảnh) · Bắt Pet (số bóng, điều kiện, tỉ lệ). Bắt ngoài map: chọn quái, bấm biểu tượng bóng, server kiểm rồi xác nhận.

## 9. Luật server
Một tài khoản một bộ tài nguyên; quyền bắt boss theo `bossSpawnId` + số lần thử; nhiều người bắt cùng boss; transaction nguyên tử trừ bóng / tiền + cộng pet / mảnh; requestId chống retry; sát thương pet không tính đóng góp / quyền bắt; không trade; chỉ pet xuất chiến có AI.

## 10. Thứ tự làm
1 đội hình + Bond → 2 nuôi dưỡng (Thức Ăn, level, sao 5, reset) → 3 gacha mới (migrate pity) → 4 bắt quái thường → 5 bắt boss + sổ pet.
