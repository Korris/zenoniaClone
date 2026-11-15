# Số nổi (damage numbers) — spec ChatGPT chốt 15/11/2026

Chat Tay: https://chatgpt.com/c/6af98129-e4e8-83ec-88ef-2c765b7f4edf (gửi ảnh chụp game cũ `Assets/ArtLibrary/ui/damage-font/ref/ingame-before-crop-x2.png` + `docs/reference/hero-idle-down-5-classes.png`). Người dùng giao ChatGPT tự quyết.

Hướng: "Fantasy Arcade Chibi": số tròn, nét dày, bo góc, nghiêng phải 6°, viền sắc, cel-shading 2 tông. 1 có chân, 4 mở đỉnh, 7 có gạch, 0 oval, "!" đầu to. Không font pixel vuông, không kim loại 3D. Thứ tự nổi bật: Crit > Finisher > Hero bị đánh > Thường > Heal/Potion > EXP > người khác.

Cao ký tự tính ở 1280x720:

| Loại | Cao | Fill trên → dưới | Viền | Bóng |
|---|---|---|---|---|
| Thường | 25 | #FFFFFF → #DCE8F6 | #26364E 2 px | #111B30 |
| Kết combo | 32 | #FFE58A → #FF941F | #713015 2 px | #321914 |
| Crit | 43 | #FFF9C0 → #FFD029 → #FF7400 | #4F1A18 3 px | #2A1018 |
| Người khác | 20 | #BEC9D6 → #8799AF | #34455C 1 px | không |
| Hero mất HP | 27 | #FFB0A2 → #FF354B | #64172C 2 px | #280C18 |
| Hồi máu | 24 | #E7FFB0 → #42DC79 | #145B42 2 px | #103D35 |
| Thuốc HP | 24 | #FFFFFF → #55F28B | #166A43 2 px | #103D35 |
| Thuốc MP | 24 | #EDFFFF → #33C8FF | #175385 2 px | #102944 |
| EXP | 21 | #F5D9FF → #B77AFF | #46316C 2 px | #241A40 |

Bóng lệch xuống phải (1, 2) px, đục 65 %; crit (2, 3) px, 85 %.

Chuyển động (Y âm = bay lên, px ở 720p):

| Loại | Tổng ms | Scale | Di chuyển | Mờ |
|---|---|---|---|---|
| Thường | 650 | 0,7 → 1,15 → 1,0 | Y -35 | 450–650 |
| Kết combo | 850 | 0,6 → 1,30 → 1,0 | Y -48 | 650–850 |
| Crit | 1100 | 0,45 → 1,50 → 1,12 → 1,0 | Y -62 | 850–1100 |
| Người khác | 500 | 0,9 → 1,0 | Y -24 | 250–500 |
| Hero mất HP | 750 | 0,7 → 1,25 → 1,0 | Y -20, X ±8 | 550–750 |
| Heal | 900 | 0,8 → 1,10 → 1,0 | Y -52 | 700–900 |
| Thuốc HP/MP | 1000 | 0,8 → 1,10 → 1,0 | Y -58 | 750–1000 |
| EXP | 900 | 0,85 → 1,0 | Y -45 | 600–900 |

Pop EaseOutBack 0–130 ms, bay EaseOutQuad (hero mất HP EaseOutCubic), mờ EaseInQuad. Không trọng lực. Hero mất HP rung ngang 3 lần trong 120 ms đầu, ±3 px giảm dần.

Crit: 0–70 ms nổ 45 % → 150 % + lóe trắng vàng 50 ms; 70–160 ms co 112 %, bật 118 % rồi 100 %; 0–200 ms 4 tia vàng trắng dài 8–16 px; 80–500 ms nhãn CRITICAL cao 13 px trên số; 160–1100 ms cả cụm bay lên 62 px. "!" của crit cao 70 % số, hơi nhô lên. Không rung màn hình. Crit trùng kết combo: chỉ hiện crit. Người khác crit: không nhãn CRITICAL.

Vị trí: X luân phiên -12 / 0 / +12 px (không random). Người khác: alpha tối đa 55 %.

Sprite: Sheet A glyph trắng/xám để tô màu bằng code (game nướng sẵn từng bảng màu bằng tool), Sheet B crit vẽ sẵn màu + nhãn CRITICAL.

## Spec V2 (15/11 tối, người dùng: "chưa đủ wow") — KHÔNG dùng, thay bằng V3

Tin đầu vòng 2 có kèm gợi ý hướng của mình; người dùng muốn ChatGPT tự đề xuất nên đã bảo ChatGPT bỏ qua gợi ý (xem V3). Ghi lại để tham khảo.

Gửi ChatGPT ảnh đánh thật V1 (`Assets/ArtLibrary/ui/damage-font/ref/ingame-v1-fight-4shots.png`) + ảnh so trước/sau. Chẩn đoán: crit chưa khác biệt đủ (chỉ to hơn chút, nhãn CRITICAL nhỏ không nền, tia mảnh), số chồng thành đống quanh một điểm, chuyển động nhẹ thiếu lực, số người khác / EXP đọc kém. Giữ glyph sheet A/B; 80 % "wow" đến từ FX + bố cục + chuyển động.

- Cỡ so với thường: người khác 0,92, hero bị đánh 1,08, kết combo 1,22, crit 1,75 (4+ chữ số 1,62).
- Crit thành "sự kiện", vẽ theo lớp: burst 8 cánh sau số (120x80 px, 100 % tới 90 ms, hết ở 220 ms) → vệt chém cong (90–120 px, góc ±18°) → số → nhãn CRITICAL trên ribbon (chữ 20 px, lệch trái 8 px) → 4–6 hạt bay 18–32 px, sống 220–320 ms.
- Crit: scale 0,35 → 1,85 (70 ms) → 1,58 (90 ms) → 1,72 → 1,64 (120 ms), giữ 180 ms rồi mới bay Y -26 px, tổng 900 ms, mờ 620–900; rung ±3 X / ±2 Y trong 120 ms đầu.
- Thường 0,72 → 1,08 → 1, 520 ms, Y -28, mờ 280–520. Kết combo 0,65 → 1,24 → 1,02, giữ 70 ms, Y -34, 650 ms, rung ±2 px 90 ms + nền nhấn finisher. Hero bị đánh 0,8 → 1,18 → 1, Y -16, rung ±5 px 3 nhịp 140 ms, 620 ms. Heal/thuốc 0,75 → 1,08, Y -44, 760 ms. EXP 0,88 → 1, Y -22, 540 ms, alpha 85 %.
- Bố cục 3 làn quanh mục tiêu: giữa dành cho crit (0, -8), trái (-34, +2), phải (+34, +2); cùng làn trong 220 ms thì tầng mới lên -18 px, lệch thêm ±10 px, tối đa 3 tầng. Có crit thì số khác né ±44 px trong 260 ms. EXP không vào hộp crit + 18 px, gộp EXP nhiều quái chết trong 250 ms, hiện trễ 120 ms gần crit. Người khác chỉ làn ngoài.
- Người khác: fill #F2F5FA → #B9C8DA, viền #314259, alpha 78 %; crit người khác chỉ vàng 1,18x, không burst. EXP fill #F7E8FF → #B56CFF, viền #4A2C73.
- Hit-stop đề xuất 35/55/70/90 ms (thường / kết / crit / crit kết): game đã có hit-stop riêng (`FeelConfig`), không đổi ở vòng này.
- Art mới: crit burst, vệt chém, ribbon CRITICAL, nền nhấn finisher, 4 hạt spark (`damage-font-fx-v2-chatgpt-1.png`).

## V3 "Comic Impact" (ChatGPT tự đề xuất, đang dùng)

Mockup: `Assets/ArtLibrary/ui/damage-font/damage-numbers-v2-mockup-chatgpt-1.png`; asset `damage-font-fx-v3-chatgpt-1.png` (burst, banner CRITICAL đỏ, vệt chém, vòng finisher, chớp trúng, 4 hạt). Số là một phần của hiệu ứng trúng đòn: số vàng cam dày, nền nổ sắc cạnh, chữ CRITICAL như tiêu đề truyện tranh. Giữ sheet A/B. Tương phản: đòn thường gọn, crit nổi bật.

| Loại | Cao (720p) | Hiệu ứng | ms |
|---|---|---|---|
| Thường | 24 | pop nhẹ, bay 26 px | 520 |
| Kết combo | 32 | vệt chém nhỏ sau số (+ vòng) | 700 |
| Crit | 46 | burst + CRITICAL + scale punch | 950 |
| Hero mất HP | 28 | rung ngang 4 px | 650 |
| Người khác | 21 | trắng xanh, viền navy | 500 |
| Heal | 27 | xanh lá, 2 hạt | 850 |
| EXP | 20 | tím, cạnh hero | 650 |

Crit: 0–70 ms bật 0,5 → 1,65 + burst; 70–170 co về 1,0, rung ±3 px, hiện CRITICAL; 170–310 giữ để đọc; 310–950 bay 30 px, mờ từ 650. Burst 90x60 (crit) / 125x85 (crit lớn); chỉ crit đầu trong 300 ms có burst lớn. Hit-stop đề xuất 60 ms (game đã có 45/80 ms, không đổi).

Chống chồng: khung bao trên màn hình, thử trên giữa → trái → phải → tầng cao hơn, cách nhau 8 px, xét số trong bán kính 120 px; crit ưu tiên, số thường né crit, EXP sang cạnh hero, hiện trễ 120–180 ms; hết chỗ thì bỏ số người khác. Không gộp các nhát combo của người chơi.

Đã làm khác spec: burst to hơn (170 px / 1,9x bề ngang số; 125 px bị số che gần hết trong game); asset mỗi món 1 hình (code scale/xoay) thay vì 4 frame; chưa gộp hit cùng nguồn (Spawn không biết người đánh); số người khác vẫn giới hạn 4.
