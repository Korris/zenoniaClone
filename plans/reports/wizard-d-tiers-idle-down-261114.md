# Wizard bộ D: áo T1–T9 idle hướng XUỐNG + trượng/sách 10 bậc + T0 tay ngửa đỡ sách (14/11/2026)

Chưa commit. Không đổi số liệu skill, không đổi `dev-start.json`, không đụng `heads/hair`.

## Cách game vẽ (đã chọn)
- Trước: T0 (Lv1) = `body/bald` + lớp tóc + `hair-anchors.json`; T1–T9 = `body/set-tN/headless` + `heads/face` (đường cũ, `HeroHeadLayer.Apply` tắt bald khi mặc giáp).
- Giờ: `HeroHeadLayer.BaldBody.cs` có nhánh bộ áo: khi mặc set-tN và có `body/set-tN/bald/<anim>.png` + `body/set-tN/hair-anchors.json` cho frame đó thì vẽ thân trọc của bộ + tóc như T0 (đổi kiểu/nhuộm vẫn chạy). Chỉ có anchor cho **idle/down** → hướng lên/trái/phải, walk, attack của T1–T9 vẫn đường cũ (thân bộ cũ, đầu cắt).
- Mỗi set-tN: `bald/idle.png` (hàng 0, 4 frame thở như T0), `hand/idle.png` hàng 0 (nắm tay trượng + lòng bàn tay), `bare/idle.png` hàng 0 (trọc + tóc 0), `held-anchors.json` (trượng `idle-stance` artY -28.6 như T0, sách `idle-stance` trên lòng bàn tay), `hair-anchors.json`.

## T0 sửa tư thế (yêu cầu giữa chừng)
Tay trái (phải màn hình) bẻ khuỷu, bàn tay ngửa ngang ngực dưới, sách đứng trên lòng bàn tay. Nguồn `outfit-d-t0/src/wizard-outfit-d-t0-idle-down-bald-palm-up-chatgpt-1.png` (cạnh `-chatgpt-2-arm-out`), `SRC_PALM_UP` trong `Tools/build-wizard-outfit-d-idle-dirs.py`; 3 hướng kia giữ nguyên (đích co đầu vẫn lấy đầu hình arm-out). `idle-stance` sách đổi tâm sang đáy giữa sách, neo = đỉnh lòng bàn tay + 2 px, góc 0.

## ChatGPT (tài khoản Tay)
Chat: https://chatgpt.com/c/6af870fd-e614-83ec-b7a5-d59f2b382980. 11 ảnh: T0 tay ngửa, T1–T9 (mỗi bậc 1 ảnh, ảnh 1 = T0 tay ngửa, ảnh 2 = mảnh bảng -4 phóng 3x, ảnh 3 = `hero-idle-down-5-classes.png`; T1 nền #FF00FF), 1 ảnh trượng T1 + sách T1 + trượng T4 lồng vàng (#FF00FF).
Nguồn + prompt: `Assets/ArtLibrary/classes/wizard/outfits/outfit-d-tiers/` (`src/`, `ref/`, `prompt-01..11-*.txt`).
T8: bỏ vương miện pha lê trên đầu (sẽ đè lớp tóc); T9: cánh thu gọn, không rồng trên đầu.

## Tool mới
- `Tools/build-wizard-outfit-d-tiers.py` (`fit`, `install`, `check`): khớp cằm→đế + tâm sọ theo T0, co/giãn đầu nếu lệch >3 %, thở như T0, dò tay quanh tay T0, ghi set-tN, backup `outfit-d-tiers/old/`.
- `Tools/build-wizard-outfit-d-tier-gear.py` (`cut`, `install`): tách 10 trượng (lan theo cột cán) + 10 sách từ bảng, trượng 160+9·bậc px, khoảng cầm→chót như T0 (đi/đánh/idle giữ cùng chỗ cầm, chót không thấp hơn T0), cán chỗ cầm 4–5,8 px (≤ 6, nhỏ hơn nắm tay), sách 40+1,5·bậc px; ghi base/enhanced/idle-stance, grip trong `held-wizard.json`, icon `weapon/offhand-wizard-N`; backup `outfit-d-tiers/old-gear/`.

## Số đo đầu (sọ trọc, đỉnh→cằm, px trong ô 192)
T0 52 · T1 52 · T2 52 · T3 52 · T4 52 · T5 52 · T6 52 · T7 52 · T8 52 · T9 52 (không bậc nào cần co; lệch 0 %). Thân trọc đỉnh sọ→đế 144–145 px mọi bậc (T0 142–144). Không bị cắt mép ô (T9 cánh vừa 192).

## File đổi
- Code: `Assets/Scripts/HeroHeadLayer.cs`, `HeroHeadLayer.BaldBody.cs`.
- Tool: mới 2 tool trên; sửa `Tools/build-wizard-outfit-d-idle-dirs.py` (`SRC_PALM_UP`, `single_figure`, `palm_seat`, sách góc 0).
- Resources: `body/{bald,headless,bare,hand}/idle.png`, `body/held-anchors.json`, `Data/classes/wizard/{hair-anchors,held-wizard-melee,held-wizard}.json`, `body/bald|over-hair/*` (ghi lại bằng `build-wizard-hair-on-bald-body.py write`, chỉ idle/down đổi anchor), `body/set-t1..9/{bald/idle.png(+meta),hand/idle.png,bare/idle.png,held-anchors.json,hair-anchors.json(+meta)}`, `weapons/{staff,book}/*-{base,enhanced}.png`, `weapons/{staff,book}/<tên>/idle-stance.png`, `Sprites/icons/{weapon,offhand}-wizard-0..9.png`.
- Docs: `docs/sprite-rules.md` (dòng bảng tool + 4 dòng Sổ lỗi 14/11), `plans/261114-0821-wizard-outfit-d-redesign/plan.md` (bước 6), `docs/pending-tasks.md`.

## Ảnh đã xem
- Offline ghép đúng cách game vẽ (thân trọc, tóc, trượng, sách, lớp tay), 10 bậc × 4 frame × tóc 0 và 2: `plans/reports/wizard-d-tiers-261114-check/tiers-idle-down-4frames-styles-0-2.png`; phóng to T1, T9 × 4 frame × tóc 1, 3: `t1-…`, `t9-4frames-styles-1-3-x2.png`; bảng trượng/sách `outfit-d-tiers/staging/gear-preview.png`.
- Trong game (`C:\temp\player-wizard-d\Zenonia.exe`, build từ `C:\temp\zu-copy`, `--class wizard --face down --map oakhaven --tp 20,10 --god --level 100 --worn-tier N --wear-tiers N,N,N --zoom 30`): T0–T9, T5 tóc 2, T9 tóc 3, Warrior: `docs/reference/wizard-d-tiers-idle-down-ingame.png` (ảnh gốc `C:\temp\player-wizard-d\shots\tiers\`). Đúng bộ, đúng trượng/sách từng bậc, sách nằm trên lòng bàn tay, trượng không xuyên đế, cao bằng Warrior.
- Chưa xem trong game: frame thở 2–4 (chỉ 1 ảnh mỗi bậc; 4 frame xem offline), đi/đánh với trượng/sách mới (cùng chỗ cầm như T0 nên kỳ vọng không đổi, chưa chụp), màn Equipment/icon mới.

## Kiểm tra
`dotnet test` ZenoniaSkills 313/313, ZenoniaNet 404/404.

## Việc còn
- T1–T9 hướng lên/trái/phải idle, walk, attack1-4 (vẫn thân bộ cũ + đầu cắt, khác màu với idle xuống mới).
- T0 hướng xuống tay ngửa chỉ ở idle; walk/attack giữ tư thế cũ.
- `glow` trượng/sách trong `held-wizard.json` vẫn màu bậc cũ (vd T1 vàng gỗ, T2 hồng) — ảnh hưởng vệt chém/hiệu ứng cường hoá.

## Câu hỏi mở
1. Đổi `glow` theo màu bậc mới (T1 xanh lá, T2 xanh nhạt, T3 ngọc lam, T4 vàng, T5 cyan, T6 đỏ, T7 hồng, T8 vàng sao, T9 tím)?
2. T8 không có vương miện (để tóc đổi được) — chấp nhận?
3. Sách bậc cao (T8–T9 rộng 52–54 px) che một phần má/tóc bên phải — giữ cỡ hay thu nhỏ?
4. Làm tiếp hướng/động tác nào cho T1–T9 trước?

## Vòng 2 (15/11): trượng to như bảng (người dùng: "bé tí")
- Nguyên nhân: art lấy từ bảng trượng riêng (cán dài, đầu nhỏ) rồi co cả cây theo 160 + 9·bậc px → đầu trượng ~0,08 H; bảng toàn thân -4: 0,17 H (T0) → 0,41 H (T9).
- Đo trên bảng -4 từng bậc (H = đỉnh tóc→đế): trượng/H T0 0,92 · T1 0,97 · T2 0,95 · T3 1,03 · T4 0,95 · T5 1,11 · T6 1,15 · T7 1,11 · T8 1,09 · T9 1,10 (dùng 150 + 4,5·bậc px); đầu trượng/H 0,17 · 0,26 · 0,24 · 0,31 · 0,31 · 0,31 · 0,31 · 0,24 · 0,34 · 0,41.
- Art mới (game px): cao 150 → 190, đầu rộng 28 · 42 · 39 · 50 · 50 · 49 · 50 · 39 · 56 · 67; cán chỗ cầm 5,8–8,9 px (cho phép hơi dày hơn trước). Dựng = đầu + cán trơn + chót nhỏ (≤ 24 px), bỏ hoa văn giữa cán. Khoảng cầm→chót như T0 → không xuyên đế; đi/đánh cầm cùng chỗ.
- Đã xem: offline idle 10 bậc `plans/reports/wizard-d-tiers-261114-check/tiers-idle-down-bigstaff.png`; walk + attack1-4 hướng xuống T0/T5/T9 `walk-attack-down-bigstaff-t0-t5-t9.png` (cầm đúng tay, đầu trượng cạnh đầu, không che mặt; các frame cắm đất/đâm như cũ). Trong game (build lại `C:\temp\player-wizard-d\Zenonia.exe`): `docs/reference/wizard-d-tiers-idle-down-ingame.png` (10 bậc + T9 tóc 3 + Warrior), `docs/reference/wizard-d-tiers-design-vs-ingame.png` (bảng -4 trên, game dưới); walk xuống T0 trong game `C:\temp\player-wizard-d\shots\tiers2\walk9\frame_00120.png` (`--record-walk` không áp `--worn-tier`, nên chỉ thấy T0).
- Chưa xem trong game: walk/attack T1–T9 (chỉ offline), attack trong game.
- Sách T8–T9 giữ cỡ cũ theo lệnh.

## Vòng 3 (15/11): sách MỞ + chót trượng chạm đất (idle xuống, 10 bậc)
- Sách mở: ChatGPT vẽ 10 sách mở (cùng chat, Tay; `src/wizard-d-tiers-open-books-t0-t4-chatgpt-1.png` nền #FF00FF, `-t5-t9-chatgpt-1.png` nền #00FF00; prompt `prompt-12/13-*.txt`), kiểu như sách ở ảnh chọn nhân vật (mở chữ V, trang trắng phía trên, bìa hướng người xem). Chỉ dùng cho idle xuống (`idle-stance`, tâm đáy giữa, đặt trên lòng bàn tay); walk/attack và idle hướng khác giữ sách đóng. Rộng 48 + 0,6·bậc px (bản 1,2× sách đóng che nửa mặt T9).
- Chót trượng: lệnh mới `py Tools/build-wizard-outfit-d-tiers.py ground` đặt `artY` từng frame để chót đúng hàng đế (đo trên sheet bald từng bậc: 190; T2 189), cả 4 frame thở, cả T0. Chỉ đổi anchor idle xuống, art + grip giữ nên walk/attack cầm y như trước (đã xem offline T0/T5/T9: `walk-attack-down-bigstaff-t0-t5-t9.png`).
- Vì trượng tụt xuống, đầu trượng to sát mắt trái: cán kéo dài cho bậc có đầu rộng (đầu kết thúc trên hàng 78, mắt ở hàng 84–88), chiều cao không giảm theo bậc. Cao trượng (px): 150 · 155 · 159 · 176 · 182 · 194 · 204 · 204 · 206 · 222; đầu trượng 28–67 px như vòng 2.
- Đã xem: offline `plans/reports/wizard-d-tiers-261114-check/tiers-idle-down-openbook-grounded.png` (10 bậc), `t6-…`, `t9-4frames-openbook-grounded-x2.png` (4 frame thở, chót luôn ở đế, mặt không bị che); trong game (build lại `C:\temp\player-wizard-d\Zenonia.exe`, mỗi lần chụp tắt hết Zenonia trước): `docs/reference/wizard-d-tiers-idle-down-ingame.png` (10 bậc + T9 tóc 3 + Warrior), `docs/reference/wizard-d-tiers-design-vs-ingame.png`. Mọi bậc đúng bộ, chót trượng ngang đế giày, sách mở trên tay, thấy đủ hai mắt.
- Chưa xem trong game: frame thở 2–4 (chỉ offline), walk/attack T1–T9.

## Vòng 4 (15/11): T9 vẽ lại tư thế tay theo bảng -4 (chờ người dùng duyệt trước khi làm T0–T8)
- Nguyên nhân: tay của thân trọc khác bảng (ngực, sát mặt) nên sách/trượng neo vào tay luôn đè mặt.
- Đo bảng -4 T9 (H = đỉnh tóc→đế): nắm tay phải 0,47 H, cách tâm 0,29 H; lòng bàn tay trái 0,62 H, cách tâm 0,27 H; sách rộng 0,27 H, mép trên 0,43 H (dưới cằm); trượng 1,14 H, đầu rộng 0,35 H, đầu kết thúc 0,25 H (trên mắt).
- ChatGPT (Tay, cùng chat) vẽ lại T9 trọc: `src/wizard-outfit-d-t9-idle-down-bald-chatgpt-2.png` (ảnh 1 = T9 cũ, ảnh 2 = `ref/board4-t9-x3.png`), cánh to như bảng. Đầu vẽ nhỏ hơn T0 6 %, tool giãn đầu về đúng 52 px.
- Trong game: nắm tay (48, 118), lòng bàn tay (134, 131); sách mở 40 px, lệch ra ngoài 4 px, mép trên dưới cằm; trượng 222 px, đầu rồng 57 px nằm trên/bên trái tóc, chót ở đế cả 4 frame.
- Đã xem: offline cạnh crop bảng `plans/reports/wizard-d-tiers-261114-check/t9-board-pose-vs-offline.png` (4 frame thở + tóc 2, 3); trong game `docs/reference/wizard-d-t9-board-pose-vs-ingame.png` (bảng | T9 tóc 0 | T9 tóc 2 | Warrior), ảnh gốc `C:\temp\player-wizard-d\shots\t9pose\`. Sách và trượng không chạm mặt.
- Khác bảng: trượng game cao hơn (đầu rồng nằm TRÊN đầu nhân vật, không ngang đầu như bảng) vì đầu nhân vật game to hơn bảng; hạ xuống thì đè tóc/mặt. Cánh trong game nhỏ hơn bảng một chút (ô 192).

## Việc thêm (15/11): kiểu tóc 1 = tóc ảnh chọn nhân vật
- Bỏ kiểu 1 cũ (đen lọn xanh, bản lưu `Assets/ArtLibrary/classes/wizard/heads/layered/old/hair-1-v5-black-blue/`). ChatGPT (Tay, chat thiết kế) vẽ hàng 1 trên canvas đầu trọc: `heads/layered/src/wizard-hair-v6-hero-select-chatgpt-1.png`, prompt `heads/layered/ref/prompt-v6-hero-select-style-1.txt`.
- `Tools/build-hair-same-volume-sheets.py`: kiểu 1 lấy từ sheet v6, thêm `style_grow` {1: 1.12} (tóc vẽ nhỏ hơn 3 kiểu kia ~15 %: đầu+tóc 85 → 96 px, kiểu khác 100–108). Kiểu 0, 2, 3 cài lại trùng từng pixel với bản cũ. Màu mặc định kiểu 1 = #ECECF4 (`hair-dyes.json`).
- Đã xem: offline idle 4 hướng T0 + T9 xuống `plans/reports/wizard-d-tiers-261114-check/hair-1-hero-select-idle-check.png`; trong game `--hairstyle 1` T0/T9 xuống + phải `docs/reference/wizard-hair-1-hero-select-ingame.png`. Không lộ da đầu, cỡ ngang các kiểu khác. (T9 hướng phải vẫn thân bộ cũ — chưa làm.)

## Vẽ lại kiểu tóc 1 (15/11, người dùng: "vẽ xấu")
- Nguồn mới `heads/layered/src/wizard-hair-v6b-hero-select-2-takes-chatgpt-1.png` (prompt `heads/layered/ref/prompt-v6b-hero-select-style-1.txt`), gửi crop đầu ảnh chọn nhân vật x3; 2 phương án, dùng hàng 1 (hàng 2 đầu vẽ to hơn canvas, hướng ngang/lưng lệch).
- `build-hair-same-volume-sheets.py`: kiểu 1 = v6b hàng 1, `style_grow` 1,2, thêm `cleaned()` (bỏ mảnh vụn, khử xanh lá) chỉ cho kiểu 1; kiểu 0, 2, 3 trùng từng pixel. Màu mặc định kiểu 1 #DCD6F0.
- Đầu + tóc ~107 x 95 px (hướng xuống), lọn lưỡi liềm cao ~0,6 sọ trên đỉnh.
- Đã xem: đầu phóng to 4 hướng cạnh crop ảnh chọn nhân vật `plans/reports/wizard-d-tiers-261114-check/hair-1-v6b-heads-vs-hero-select.png`; trong game T0/T9 xuống + phải `docs/reference/wizard-hair-1-hero-select-ingame.png`. Có lọn lưỡi liềm to, chỏm to, mái chéo, bóng tím nhạt; không lộ da đầu. Viền tối mảnh hơn ảnh chọn nhân vật (co nhỏ về cỡ game).

## Kiểu tóc 1 lần 3 (15/11, người dùng: "không tự nhiên")
- Vẽ thẳng lên đầu trọc thật của game: canvas = frame idle 0 của `body/bald` (xuống, phải, lưng; đầu + vai, x4) `Assets/ArtLibrary/classes/wizard/heads/layered/on-body/canvas-idle-bald-down-right-up-x4.png`; kết quả ChatGPT `on-body/wizard-hair-v7-on-body-natural-chatgpt-1.png`, prompt `on-body/prompt-v7-on-body-natural.txt`.
- Tool mới `Tools/build-wizard-hair-on-body-canvas.py` (khớp ô theo thân dưới cằm: độ trùng 0,96–0,97; tách tóc; nghịch đảo anchor idle → `heads/hair/1/{down,right,up}.png`; bản cũ `on-body/old-1/`). `build-hair-same-volume-sheets.py` không ghi kiểu 1 nữa.
- Màu mặc định kiểu 1 #E6EEF8.
- Đã xem: offline idle 4 hướng + walk/attack1 xuống, cạnh ảnh chọn nhân vật và kiểu 3 (`plans/reports/wizard-d-tiers-261114-check/hair-1-v7-vs-hero-select-and-style-3.png`); trong game `docs/reference/wizard-hair-1-hero-select-ingame.png`. Tóc ôm sọ, mái rủ tự nhiên, lọn lưỡi liềm nhỏ, không lộ da đầu; nhỏ gọn hơn kiểu 3.

## T9 to/ngầu như Warrior T9 (15/11 chiều)
- Mẫu: Warrior T9 idle xuống trong game (`outfit-d-tiers/ref/warrior-t9-idle-down-ingame.png`): rộng 171 px, cao 164 px trong ô 192. Wizard T9 trước: 134 px.
- ChatGPT (Tay) vẽ lại: `outfit-d-tiers/src/wizard-outfit-d-t9-idle-down-bald-chatgpt-3-big.png` (prompt `prompt-14-t9-big-like-warrior.txt`).
- Sau khớp: rộng 169 px (x 12–180), ngang vai 160 px, ngang hông 121–123 px; đầu giãn 38 → 51 px (T0 52, lệch 2 %). Trượng rồng 236 px, đầu 68 px (`HEAD_W` T9 0,42); chót đúng đế hàng 190 cả 4 frame.
- Tool: `build-wizard-outfit-d-tiers.py` lặp giãn đầu, `ground` kẹp đế ≤ 190; `build-wizard-outfit-d-tier-gear.py` `HEAD_W`/`POSE_STAFF` T9.
- Đã xem: offline 4 frame + tóc 1, 3 `plans/reports/wizard-d-tiers-261114-check/t9-big-4frames-styles.png`; trong game cạnh Warrior `docs/reference/wizard-d-t9-big-vs-warrior-t9-ingame.png`. Ngang tầm to với Warrior; sách và trượng không che mặt; đầu rồng trượng chạm nhẹ mép tóc trái phía trên.

## Cánh rồng T9 "cong queo" (15/11)
- Nguyên nhân do tool: giãn đầu 1,45 lần bằng `warp()` kéo cong cả dải trên cổ, gồm cánh (cánh thật 133 px bị kéo thành 169 px). Sửa `head_warp()`: chỉ lấy vùng đầu từ ảnh đã warp.
- Vẽ lại T9 (ChatGPT, Tay): `outfit-d-tiers/src/wizard-outfit-d-t9-idle-down-bald-chatgpt-4-wings.png`, prompt `prompt-15-t9-dragon-wings.txt`: đầu đúng tỉ lệ, cánh rồng đối xứng 3–4 xương thẳng, màng cong đều.
- Sau khớp: rộng 157 px (x 17–173), đầu 53 px (T0 52), tay phải (52, 125), tay trái (134, 132); trượng rồng 236 px, chót ở đế 189.
- Đã xem: offline `plans/reports/wizard-d-tiers-261114-check/t9-big-4frames-styles.png`; trong game cạnh Warrior `docs/reference/wizard-d-t9-big-vs-warrior-t9-ingame.png`. Cánh thẳng, đối xứng; hẹp hơn Warrior (157 so với 171).

## T0–T8 cầm sách thấp như T9 (15/11 chiều)
- ChatGPT (Tay) vẽ lại 9 thân trọc theo tư thế T9 v4: `outfit-d-tiers/src/wizard-outfit-d-tN-idle-down-bald-chatgpt-2-pose.png`, T0 `outfit-d-t0/src/wizard-outfit-d-t0-idle-down-bald-board-pose-chatgpt-1.png` (prompt `prompt-16-t0-t8-board-pose.txt`).
- Khớp: đầu 51–52 px mọi bậc (T0 52); nắm tay trượng ~ (52–57, 118–126), lòng bàn tay ~ (130–134, 120–132); sách mở 35 + 0,6·bậc px lệch ra ngoài 4 px; chót trượng ở đế (188–190) cả 4 frame.
- Đã xem: offline 10 bậc `plans/reports/wizard-d-tiers-261114-check/tiers-board-pose-offline.png`, T6–T8 thân trọc `pose-t6-t8-fit.png`; trong game 10 bậc + Warrior T9 `docs/reference/wizard-d-tiers-idle-down-ingame.png`. Sách nằm thấp cạnh cằm, trượng và sách không che mặt ở mọi bậc.
