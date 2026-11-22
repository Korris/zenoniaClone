# Willowbrook 3D thật so với RO3: 3 vòng chấm của ChatGPT (22/11/2026)

Chat (Tay "Tân Korris"): https://chatgpt.com/c/6b029c3a-4a48-83ec-affb-a9e9bf71b449 . Spec ban đầu: `map-3d-ro3-spec-chatgpt.md`.
Ảnh gửi: `round-1-sheet-sent-to-chatgpt.jpg`, `round-2-...`, `round-3-...` (3 ô 3D + 1 ô 2.5D hoặc cổng tây). Ảnh cuối: `willowbrook-3d-yaw{0,90,180,270}-nohud.jpg`, `willowbrook-3d-yaw180-hud.jpg`, `willowbrook-3d-ruins-yaw135.jpg`, `willowbrook-3d-west-gate-yaw90.jpg`, bản 2.5D cùng chỗ `willowbrook-25d-same-spot-nohud.jpg`.

## Điểm "RO3 feel" (thang 10, trọng số theo bộ tiêu chí của ChatGPT)

| Tiêu chí (trọng số) | Vòng 1 | Vòng 2 | Vòng 3 |
|---|---|---|---|
| Cây và tán lá (2,5) | 7,5 | 8,0 | 8,3 |
| Terrain và vật liệu (1,5) | 6,2 | 7,7 | 8,0 |
| Ánh sáng và bóng (1,5) | 7,0 | 7,7 | 8,1 |
| Hero hoà vào cảnh (1,5) | 6,8 | 7,2 | 7,6 |
| Camera 360° (1) | 7,5 | 7,8 | 8,2 |
| Suối, cầu, phế tích (1) | 6,3 | 7,9 | 7,9 |
| Hoàn thiện tổng thể (1) | 6,5 | 7,6 | 7,8 |
| **Tổng** | **6,9** | **7,73** | **8,0** |

ChatGPT chấm bản 2.5D hiện tại 8,2 (vòng 2).

## Nhận xét chính

- Vòng 1: "đã thoát khỏi mức low-poly nhựa của bản thử cũ, nhưng vẫn còn cảm giác prototype". 3D thắng về thể tích cây, bóng, không gian; 2.5D thắng về mặt đất, đá / suối / bờ, độ đồng nhất art style, đọc gameplay. Sửa: cỏ + cụm cỏ 3D, nước (ripple, foam bờ, bờ mềm), đá méo hơn + rêu loang, card lá nhỏ hơn, bóng nhẹ hơn, cột / tường phế tích thành mesh 3D, màu đất.
- Vòng 2: "không còn nhựa; còn ~20-25 % cảm giác prototype" (cỏ lặp, tán hơi "cardy", nền sạch quá). Sửa: cỏ 2000 -> 1300 cụm thưa dần sát đường, lõi tán tối hơn + card lõi, highlight lá #A9C45F, mảng sáng / tối nền 8 m, đất #CDA477, bóng 2 cascade 2048.
- Vòng 3: **8,0/10, đạt ngưỡng hình ảnh**. "Nhựa: không đáng kể. Prototype: vẫn còn nhưng đã giảm rõ." Còn yếu: mép tán ở vài góc lộ card, hero vẫn hơi giống 2D đặt vào 3D, phế tích còn đều và mới, bóng trên đường ở cổng tây hơi đậm.

## Phán quyết của ChatGPT

- Duyệt tiếp tục MỘT map 3D thử nghiệm; **KHÔNG duyệt chuyển toàn bộ map từ 2.5D sang 3D lúc này**. Giữ 2.5D làm bản production, 3D là nhánh phát triển có điều kiện.
- Điều kiện mở rộng: 60 FPS PC và tối thiểu 30 FPS mobile mục tiêu trong bài test 40 người cùng combat (40 hero + pet + quái + 15-25 VFX), 1 % low PC ≥ 45 FPS.
- Trước khi chuyển thêm map: benchmark 40 người thật; LOD mobile (card lá LOD1 -55 %, cỏ 500-700 cụm, shadow 12-16 m, bóng tán xa bằng decal); một generator dùng chung cho mọi biome (chỉ đổi atlas, bảng màu, thông số cây, bố trí).

## FPS đo được (máy KorrisPC, GPU tích hợp AMD Radeon, 1280x720, Willowbrook có 70 quái, `--fps-log`)

| Bản | FPS | ms / khung |
|---|---|---|
| 2.5D (máy rảnh) | ~200-210 | ~4,8 |
| 2.5D (lúc agent khác đang build) | ~140 | ~7 |
| 3D bản đầu (1000 renderer, bóng 4096 x 2 cascade) | 27-28 | ~36 |
| 3D cuối (gộp mesh theo ô 16 m, bóng 2048 x 2 cascade, 1/3 card lá đổ bóng) | 54-64 (37-38 khi máy bận) | 16-18 |
| 3D cuối + 40 dummy (110 quái) | ~62 | ~16 |

Thử tắt từng phần (bản trung gian): tắt bóng 156 FPS, tắt lá 116, tắt rừng viền 81, so với 58 đủ hết: bóng (depth pre-pass + shadow map của lá cutout) là chi phí lớn nhất.
