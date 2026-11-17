# Warrior skill V2: đề xuất ChatGPT (17/11/2026)

Chat ChatGPT (tài khoản Tay, Plus): https://chatgpt.com/c/6afbe1d8-7f60-83ec-ba96-11ce40bfc96d

Người dùng nói: "hệ thống skill khi xài skill dam chưa đủ phê, warrior nhảy dam lên ít quá, chơi 1 cái xong hết skill". Đây mới là đề xuất, chưa sửa code.

**Người dùng chỉnh lại ý (12:14):** "dam nổ lên ít quá, phải nổ số lần dam lên nhiều lên". Tức là mỗi skill phải bật ra NHIỀU con số dam, không phải tăng %. Bản đúng ý là **V3** ở cuối file. V2 chỉ giữ để lấy tổng %, MP, CD, cách trừ DEF và hạ đòn 4 combo.

## V3 "Multi-Hit Barrage" (bản đúng ý người dùng)
Mỗi skill có 8-18 hit, hit nhỏ cách nhau 35-65 ms. Tổng % giữ như V2, MP / CD cũng giữ như V2. Hit cuối mạnh nhất, có một quãng ngắt ngắn trước hai hit cuối. Không cần một animation chém cho mỗi hit: một lần quét kiếm sinh nhiều hit bằng lưỡi kiếm phụ hoặc sóng.

| Skill | Hit nhỏ | Áp chót | Cuối | Tổng | Mốc hit (ms) | Hết VFX |
|---|---|---|---|---|---|---|
| Power Slash | 6 x 55 | 100 | 180 | 610 | 90,135,180,225,270,315,365,440 | 550 |
| Whirlwind | 10 x 55 | 80 | 160 | 790 | 90→675 bước 65, 760, 900 | 1050 |
| Earth Break | 8 x 60 | 100 | 300 | 880 | 180→495 bước 45, 570, 720 | 900 |
| Flame Cleave | 7 x 50 | 90 | 220 | 660 | 130→460 bước 55, 540, 680 | 820 |
| Charging Strike | 8 x 60 | 100 | 180 | 760 | 80→395 bước 45, 480, 600 | 730 |
| Blade Cyclone | 16 x 45 | 100 | 260 | 1080 | 100→850 bước 50, 930, 1100 | 1280 |
| Inferno Crash | 13 x 50 | 140 | 350 | 1140 | 450→870 bước 35, 940, 1100 | 1300 |
| Berserker | 8 x 15 (thêm sóng xung kích tròn 1,8 ô) | - | - | 120 + buff | 150→430 bước 40 | 550 |

- Vùng đánh, hút, đẩy, choáng, khoá như V2. DOT tính riêng: Earth Break 4 x 20, Flame Cleave 3 x 30, Inferno 4 x 30.
- Hit vẫn chạy tiếp khi nhân vật đã đi được. Cast skill mới thì hit của Zone / Projectile chạy tiếp, phần Strike chưa ra thì bị bỏ. Đang có Whirlwind / Cyclone (zone bám người) thì không cho cast skill tấn công khác.
- Trừ DEF: mỗi mục tiêu bị trừ 1 lần cho cả chuỗi, chia cho các hit theo hệ số (như V2). Random và crit tính từng hit. Làm tròn có giữ phần dư để tổng dam không lệch.

### Hiện số kiểu "Damage Stream"
| | Hit nhỏ | Hit cuối | Crit |
|---|---|---|---|
| Cao chữ (720p) | 22 px | 33 px | 42 px |
| Sống / bắt đầu mờ | 450 / 220 ms | 650 / 400 ms | 780 / 480 ms |
| Bay lên (EaseOutCubic) | 34 px | 42 px | 46 px |
| Scale đầu → đỉnh / thời gian pop | 0,80 → 1,08 / 95 ms | 0,65 → 1,30 / 135 ms | 0,55 → 1,45 / 160 ms |
| Viền | 2 px | 3 px | 3 px |
| Vị trí | zigzag ±18-28 px | giữa | giữa, vẽ đè trên stream |
- Hit trong cùng một chuỗi bỏ cách tìm 6 chỗ trống. Thay bằng 12 slot zigzag cố định cho mỗi mục tiêu: `slot = hitIndex % 12; row = slot / 2; x = (slot % 2 == 0 ? -1 : 1) * (row % 2 == 0 ? 18 : 24); y = -46 - slot * 8`. Số được đè lên nhau, số mới vẽ trên số cũ.
- `hitIndex` reset khi bắt đầu cast mới hoặc sau 600 ms không có hit. Quái sát mép trên màn hình thì cụm số đổi hướng.
- Pool số dam: 40 lên 256 (máy yếu 192, máy mạnh hoặc đánh boss đông người 384-512). 10 quái ăn Blade Cyclone ra khoảng 180 số, đỉnh khoảng 90 số cùng lúc.
- Pool đầy thì giữ theo thứ tự: crit của mình → hit cuối của mình → hit thường của mình → số của người khác. Mục tiêu đang khoá và boss hiện chữ 100 %, quái gần 90 %, quái xa 75-80 %. Mỗi cụm số dịch ngang tối đa ±30 px để không đè cụm của quái bên cạnh.
- Không dùng TOTAL (bỏ đề xuất ở V2). Crit: pop đỉnh 1,65 → 1,45. Burst lớn chỉ cho 1 crit mỗi 500 ms trên toàn màn hình, các crit khác chỉ hiện số cam to.
- Hitstop / rung màn hình: hit nhỏ 0 ms, không rung. Áp chót 15 ms, 1 px 45 ms. Hit cuối Power / Flame 30 ms, 2 px 65 ms. Cyclone / Earth 45 ms, 3 px 90 ms. Inferno 60 ms, 5 px 120 ms. Crit trong chuỗi không thêm hitstop, chỉ rung chữ ±2 px.
- Hit nhỏ có tia lửa va chạm nhỏ 45-65 ms, cứ 3-4 hit thì có một VFX to hơn chút.
- ChatGPT lưu ý: quái thường chết sau 2-3 hit thì không thấy được 18 số. Thử trên boss / quái tinh anh trước. Làm trước Power Slash (8 hit) và Blade Cyclone (18 hit).

## Số liệu đã gửi (lấy từ code ngày 17/11)
- `SkillTable.Warrior.cs` (rank 1), mỗi rank +12% (`SkillDef.DefaultDamageGrowth`), `CombatMath.Damage` = ATK x hệ số x 0,9-1,1 - DEF x 0,5, crit 8 % x1,6.
- Combo thường Warrior (`ComboConfigs.cs`): 100 / 115 / 125 / 180 %, đòn 4 + 5 %/level (`ComboSystem.FinisherMultPerLevel`), nên ở Lv60 ra 475 %.
- Warrior là class duy nhất có hồi chiêu thật (`FarmSkillRules`). Power Slash spam được, các class khác `CooldownScale = 0`.

## ChatGPT phân tích
- Đòn 4 combo thường (475 % ở Lv60) ngang hoặc hơn 5/7 skill gây dam.
- DEF bị trừ ở từng hit nên skill nhiều hit bị thiệt. Ví dụ ATK 160, DEF 80, tổng 600 %: đánh 1 hit ra 920, 6 hit ra 720, 12 hit ra 480.
- Skill thiếu cao trào: chia đều hit (Blade Cyclone 6 x 150 %) nên số nào cũng nhỏ. Một phần dam nằm ở DOT nên không thấy bùng nổ.

## Bảng V2 (rank 1, chưa tính DEF / crit)
| Skill | Hit (% từng hit) | Tổng | MP | CD | Timeline hit (ms) / ghi chú |
|---|---|---|---|---|---|
| Power Slash | 220 + 390 | 610 | 10 | 2,5 s | 110 / 310; hit 1 hút 0,6 ô, hit 2 stagger 180 ms; khoá 320 |
| Whirlwind | 130+150+170+340 | 790 | 18 | 8 s | 100/280/460/700; bám người; hit cuối làm chậm 30 % 1,5 s; khoá 350 rồi đi 80 % tốc |
| Earth Break | 150+230+500 | 880 (+zone 4x20 = 960) | 26 | 12 s | 130/300/560; nón 100° dài 5; hit cuối choáng 0,6 s (quái thường); khoá 420 |
| Flame Cleave | 200 + 460 | 660 (+bỏng 3x30 = 750) | 22 | 9 s | 140/430; vòng cung 140° tầm 3,5; khoá 400 |
| Charging Strike | 160+210+390 | 760 | 24 | 10 s | dash 0-250; hit 100/270/450; hit cuối đẩy 0,8 ô |
| Blade Cyclone | 90+110+130+150+180+420 | 1080 | 38 | 14 s | 120/280/440/600/760/1050; 5 đợt hút nhẹ, hit cuối bán kính 3 đẩy 1 ô; khoá 450 rồi đi 70 % tốc |
| Inferno Crash | 180+260+700 | 1140 (+bỏng 4x30 = 1260) | 48 | 22 s | nhảy 0-400; hit 450/550/760 tại điểm đáp; hit cuối đẩy 1,2 ô; kết thúc 900 |
| Berserker | buff ATK +25 %, tốc đánh +15 %, tốc chạy +8 %, 12 s | - | 55 | 48 s | rank: ATK +1 điểm %/rank (25 lên 34) |

- Rank: hạ từ +12 % xuống +6 % mỗi rank (rank 10 = x1,54). Không tăng số hit theo rank.
- Hit đến sau lúc hết khoá vẫn phải đánh xong. Whirlwind / Cyclone bám người, Earth Break / Flame Cleave chốt vùng lúc ra đòn. DOT không cộng dồn, áp lại thì làm mới thời gian.

## Đổi công thức
1. DEF của skill nhiều hit: trừ một lần cho mỗi mục tiêu mỗi lần cast, chia cho các hit theo tỉ lệ hệ số: `D_i = ATK x M_i x R_i - 0,5 DEF x M_i / ΣM`. Crit vẫn tính riêng từng hit.
2. Đòn 4 combo thường: đổi thành 180 % + 1 % x (Level - 1), ở Lv60 còn 239 %.
3. Đánh thường trúng quái thì hồi 2 MP, mỗi nhát tính một lần.

## Cảm giác (Canvas tham chiếu 1280x720)
- Số dam: hit thường 23-27 px #FFF3DC; hit nhỏ của skill 25-29 px; hit cuối của skill 38-46 px vàng #FFC247; crit x1,25 màu cam; crit ở hit cuối 52-60 px #FF9B28 viền đỏ.
- Số bật lên: scale 0,65 → 1,25 → 1,0 trong 130 ms. Số nhỏ trôi lên 26-38 px, sống 500-650 ms; số hit cuối trôi lên 45-60 px, sống 850 ms; lệch ngang ±20 px.
- Hitstop: hit nhỏ 12-18 ms, hit mạnh 30-45 ms, đòn kết 55-75 ms. Rung màn hình: hit thường 1-2 px trong 45 ms, đòn kết 4-6 px trong 100-140 ms. Hai hiệu ứng này chỉ chạy trên client.
- "TOTAL" (chữ nhỏ vàng nhạt, sau khi skill kết thúc) chỉ cho skill từ 4 hit trở lên, hiện trên mục tiêu chính hoặc boss. Mỗi mục tiêu hiện tối đa 5-6 số.
- Hit cuối: tụ lực 60-100 ms, chớp sáng khi trúng 45-70 ms, VFX tan dần 150-250 ms.

## Rotation / MP (Lv60, 335 MP)
- Rotation A (Power Slash, Whirlwind, Flame Cleave, Charging, Cyclone) tốn 112 MP, đủ khoảng 2 vòng. Dùng hết 7 skill + Berserker tốn 241 MP.
- ChatGPT đề nghị thử trước 3 skill Power Slash, Blade Cyclone, Inferno Crash, sau đó mới làm hết Warrior rồi đến các class khác.

## Điểm còn ngờ (Claude)
- Power Slash hiện spam được, V2 cho hồi chiêu 2,5 s. Hồi chiêu này có ép người chơi đánh thường không, hay làm vòng farm chậm đi?
- Hạ đòn 4 combo từ 475 % xuống 239 % là một lần giảm lớn: đánh thường ở level cao sẽ yếu đi rõ.
- Hệ số skill đi chung với server: đổi thì phải build lại server và sửa `SkillTableTests`. Earth Break / Blade Cyclone đang là ultimate viết tay (`LegacySkillNumbers`), không lấy số từ bảng.
- Cỡ chữ ChatGPT đưa theo 1280x720, cần quy đổi sang hệ đơn vị UI của game.
