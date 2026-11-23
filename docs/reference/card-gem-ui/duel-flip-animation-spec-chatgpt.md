# Card Duel: animation lật bài kiểu FF8 (spec ChatGPT, 23/11/2026)

Người dùng (23/11): "tôi muốn lá bài khi bị ăn bởi người khác có hiệu ứng flip nguyên cái tấm bài giống ff8, hỏi chatgpt để làm". Chat Tay: https://chatgpt.com/c/6b031e60-ffa0-83ec-903d-9449063ac0e0

## Lật bài bị ăn — 360 ms
| Pha | Thời gian | Việc |
|---|---|---|
| Nâng | 0–50 ms | nâng 8 px, scale 1 → 1.06 |
| Xoay nửa đầu | 50–180 ms | 0° → 90°, thu hẹp theo trục |
| Đổi chủ | 180 ms | đổi viền xanh ↔ đỏ |
| Xoay nửa sau | 180–310 ms | 90° → 180°, mở lại |
| Tiếp đất | 310–360 ms | hạ về chỗ, scale 1.06 → 1 |
- Easing: InOutCubic khi xoay; OutBack nhẹ khi tiếp đất (vượt ≤ 3 %). Xoay đủ 180°, không chỉ đổi màu.
- Bị ăn theo hướng ngang (trái / phải): xoay trục Y (thu scaleX). Theo hướng dọc: trục X (thu scaleY). Bị ăn nhiều hướng: lấy hướng chênh cạnh lớn nhất; bằng nhau ưu tiên ngang.

## Giả 3D trong UI 2D
- Card nằm trong FlipRoot (art, khung, 4 số, viền chủ). Ngang: scaleX = max(0.04, |cos θ|); dọc: scaleY tương tự. Qua 90° thì đổi viền chủ.
- Nghiêng giả phối cảnh skew / Z ±3–5°, về 0 khi xong. Bóng mềm dưới card: alpha 0 → 0.18 → 0, lệch Y −5 px.
- Đưa card lên lớp FlipOverlay để không bị lá bên cạnh che. Ở 90°: vệt sáng mảnh 2–3 px dọc cạnh đang thu.
- KHÔNG scale layout ô bàn; chỉ animate FlipRoot bên trong ô.

## Nhiều lá cùng lúc
- So le 70 ms / lá; lá gần lá vừa đặt trước, bằng nhau theo thứ tự ↑ → ↓ ←. Tối đa 4 lá (~570 ms). Khoá đặt lá tiếp tới khi xong. (Combo V2: +100–120 ms / tầng.)

## Hiệu ứng phụ
- Chớp sáng ở 90°: #FFF8DF alpha 0.45, 70 ms. Viền phát sáng nhẹ 100–140 ms (xanh #78B7F0 / đỏ #E46C77), không neon.
- 6–10 hạt sao / kim cương nhỏ bay 12–25 px, sống 180–250 ms (dùng sprite có sẵn / ChatGPT, không tự vẽ).
- Lắc Z ±2° một lần 50 ms khi tiếp đất; không rung cả bàn.
- Âm: whoosh khi xoay, chime khi đổi chủ, tap khi tiếp đất (0.35–0.45 s); tối đa 2–3 chime chồng nhau.

## Thứ tự
Server quyết trước, client chỉ diễn: đặt card → animation đặt → +40 ms → lật theo danh sách server → mỗi lá đổi viền ở 90° → xong hết mới tween tỉ số (150 ms) → thêm dòng log ("Turn 7: Frost Slime placed at C3. Flipped B3 and C2.") → mở khoá lượt.

## Đặt card — 230 ms
- Chọn: card trên tay nâng 12 px, viền vàng nhẹ. Bay vào ô 0–150 ms (OutCubic), scale 1.10 → 1. Chạm 150–190 ms scale 1 → 0.95 + flash viền nhẹ. Nảy 190–230 ms 0.95 → 1, 4–6 hạt bụi sáng. Âm "slap" mềm.
