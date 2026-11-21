<!-- ChatGPT (Tay "Tân Korris") 21/11/2026 https://chatgpt.com/c/6b018f05-1e84-83ec-bd2a-6e9ddc093520 . Gửi kèm: Warrior FX trên tuyết cũ / trên cỏ, Willowbrook 2.5D, bố cục Frost Hollow, nền Frostfang Den. Tóm tắt các số (bản đầy đủ trong chat). -->

# Frost Hollow + Frostfang Den 2.5D: art direction V1 (ChatGPT)

Kết luận: đổi palette map, KHÔNG tăng độ đục FX. Tuyết cũ L ~0,85-0,95 ăn mất thân quạt / vòng vàng nhạt; trên cỏ đọc tốt vì nền tối hơn.
Y = độ sáng tương đối sRGB tuyến tính (0 đen, 1 trắng).

## Frost Hollow (ngoài trời, ngày đông xám xanh)

| Vật liệu | Hex | Y mục tiêu |
|---|---|---|
| Tuyết chính | #8999AF | 0,29-0,36 (65 % diện tích) |
| Mảng tuyết sáng | #B2BED0 | 0,45-0,55 (15 %) |
| Tuyết bóng / oải hương | #657891 / #8C8DAA | 0,16-0,24 (20 %) |
| Điểm sáng nhỏ | | ≤ 0,70, tối đa 3 % |
| Đường tuyết nén | #747D8B, giữa #9098A5, mép #A6B0BE | 0,19-0,34; mép mờ 0,25-0,45 ô, không viền trắng |
| Khối đá dưới vách / đỉnh cao nguyên | #65758B (đá #626B7A, khe #424F65, đỉnh #8492A6, mũ tuyết #B9C5D6) | mặt ngang 0,27-0,36, mặt vách 0,10-0,22, chân vách 0,06-0,12 |
| Sông băng | #417F9D, lòng sâu #315B7A | 0,10-0,19 |
| Hồ băng đi được | #638CA5, mảng sương #8FAABB, viền #425B73, nứt #486F88 | 0,24-0,38; không tấm băng trắng |

- Vách đứng cho góc 58°: mô-đun 2 ô (phụ 3 ô, đầu / góc 1 ô), cao 1,6-2,2H (điểm nhấn 2,5-3H), đá 65 % / băng 35 %, mũ tuyết 8-14 % chiều cao; đá #66748B, chân #414F68, băng #6BADC4 / #386783, sáng #A6D8E6, mũ #B3BFD1. Không kéo dài một sprite cả bức vách.
- Cây / đồ: lá thông #344F53 (sáng #527477), tuyết trên thông ≤ 30 % #ADBCCC, gỗ chết #675E61, đá băng #526B80, pha lê #65B5C9. Thông to 2,5-3,5H; đá thấp mặt trên 35-45 %. Thân / sườn đá Y 0,07-0,18; tuyết trên đồ 0,43-0,58.
- Ánh sáng: nắng #FFF0D7 yếu (0,60-0,75 x Willowbrook), ambient #B6C8DC, bóng #667B8E (giữ) 0,25-0,38, fog #8799AE 10-18 % ở xa, vignette 0,06-0,10, tuyết rơi thưa alpha 0,10-0,25. Không tô sáng thêm lên tuyết đã vẽ.

## Frostfang Den (hang băng tối)

| Vật liệu | Hex | Y |
|---|---|---|
| Sàn chính | #53697D | 0,13 |
| Mảng sàn sáng | #72899E | 0,23 |
| Hành lang tối | #3D5268 | 0,08 |
| Thân vách | #334B66, chân #253A50 | 0,04-0,07 |
| Khoảng tối (void) | #192B40 | 0,02 |
| Pha lê | #4A92AB, mép #9AE1E8 | 0,25 / 0,65 |
| Sàn đấu trường boss | #60778A, giữa #6D7C91 (xám lệch oải hương), viền #344D65 | 0,15-0,25, không vũng cyan |

- Ambient #344F69 x 0,28-0,34; đèn pha lê #79C6D9 / teal #58B9B5 / hiếm #BAF2EF, bán kính 2,5-4 ô; fog #263E56 (0-5 % gần hero, 8-14 % xa); vignette 0,12-0,18; hạt bụi băng xanh thưa.
- Vách: 60 % đá tối / 40 % băng, mảnh 2-3 ô, cao 2-3H; cyan chỉ ở đầu pha lê / vân. Đèn pha lê quanh viền đấu trường, không ở giữa.

## Checklist chấm ảnh (tóm tắt)

Quạt Power Slash nhận ra qua thân (không chỉ viền); vòng Whirlwind / Blade Cyclone đủ vòng ở 720p; Earth Break / Inferno: impact + sóng + dấu đất tách bạch; Charging Strike thấy hướng; Wizard xanh tách khỏi tuyết / băng / sàn hang; Cleric vàng thấy trên mảng tuyết sáng; thanh HP + số dam đọc được; tuyết vùng đánh Y 0,25-0,39 (ngoài trời), sàn đấu trường 0,15-0,25; không đổi ô đi được.

## Chấm trong game (ChatGPT, cùng chat, 21/11)

Ảnh: `before-frost-hollow-warrior-fx-strip.jpg` (A), `after-frost-hollow-warrior-fx-strip.jpg` (B, bản vách chưa tối), `after-frost-hollow-5-spots.jpg` (C), `after-frostfang-den-warrior-fx-strip.jpg` (D), `after-frostfang-den-spots.jpg` (E).

| Mục | Điểm |
|---|---|
| A: đọc FX trên tuyết cũ | 5,8 |
| B: đọc FX trên tuyết mới Frost Hollow | 8,7 ("vấn đề đọc FX trên tuyết coi như đã giải quyết") |
| D: đọc FX trong Frostfang Den | 9,2 |
| Cảm giác RO / RO3 Frost Hollow | 8,2: ĐẠT CÓ SỬA |
| Cảm giác RO / RO3 Frostfang Den | 8,5: ĐẠT CÓ SỬA |

Đã sửa theo góp ý (vòng sau, chưa chấm lại): dải tiếp đất tối #52647A ~0,2 ô ở chân vách; mũ tuyết vách -10 % (ramp 0,83 -> 0,70); mảng tuyết oải hương #9299B0 ~15 %; thêm vết nứt hồ #527A91; Den ambient 0,30 -> 0,34, đèn pha lê -20 %, viền đấu trường #405970 a 0,3 ~0,3 ô.
Chưa sửa: vách vẫn là các cột rời (muốn: lệch bóng 10-15 %, mảnh góc lồi / lõm), bóng tiếp đất quanh bậc thang, đường nối chân vách Den #253A50. Cột sáng xanh "như cổng" trong Den là tia cổng (`MapPortalBeam`), không phải pha lê.
Giữ nguyên: palette / alpha FX Warrior, tuyết #8999AF + đường, độ tối mặt vách, thông / đá / sông / cầu, sàn Den #53697D, đấu trường xám xanh, camera 58° / FOV 27 / hero 69 px, không tăng cỡ / alpha / bloom FX.
