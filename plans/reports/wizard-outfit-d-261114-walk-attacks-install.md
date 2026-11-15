# Wizard outfit D: cài walk + attack1-4 (bước 3-4) + sửa lỗi idle xuống

Việc giao: cài WALK và ATTACK1-4 (4 hướng) bộ D theo `plans/261114-0821-wizard-outfit-d-redesign/plan.md`. Giữa chừng người dùng báo thêm lỗi idle hướng xuống (trượng sát hông + xuyên chân) và gửi ảnh vẽ lại tay giơ ra; đã xử lý cả hai trước khi đóng việc.

## Tool mới (giữ nguyên bản Northwind cũ, không sửa)
- `Tools/build-wizard-outfit-d-walk.py` — copy `build-wizard-northwind-walk.py`, nguồn `walk-board/outfit-d-walk/`.
- `Tools/build-wizard-outfit-d-attacks.py` — copy `build-wizard-northwind-attacks.py`, nguồn `attack-board/outfit-d/` (hướng lên/trái/phải).
- `Tools/build-wizard-outfit-d-combo-attacks.py` — copy `build-wizard-combo-v2-attacks.py`, cùng nguồn (hướng xuống).
- Sửa `Tools/build-wizard-outfit-d-idle-dirs.py`: thêm `SRC_ARM_OUT` (lấy riêng figure hướng xuống từ ảnh vẽ lại tay giơ ra, 3 hướng kia vẫn nguồn cũ) + `STAFF_ART_Y` (artY cho riêng `idle-stance` hướng xuống, dời điểm cầm xuống hàng 260/392 để trượng không xuyên chân — hướng xuống luôn góc 0° nên artY không có vấn đề xoay, xem mục dưới).
- Sửa `Tools/build-wizard-hair-on-bald-body.py`: `painted()` trỏ cả walk/attack1-4 sang 3 tool D mới (trước đó chỉ idle).

## Thứ tự chạy (QUAN TRỌNG, ghi vào Sổ lỗi)
Phải chạy `build-wizard-outfit-d-attacks.py write` (lên/trái/phải) TRƯỚC `build-wizard-outfit-d-combo-attacks.py write` (xuống). Chạy ngược lại một lần làm `ensure_sheet` của tool nền (`build-wizard-attack-board-repaint.py`) tưởng sheet sai cỡ (combo đã nới 6/8 cột) rồi thay bằng khung trắng 4/6 cột — mất sạch dữ liệu hướng xuống. Phát hiện qua `build-wizard-hair-on-bald-body.py verify` (toàn bộ "same pixels" 0.0), sửa bằng cách chạy lại `combo-attacks write` lần nữa (đúng thứ tự) sau khi đã có đủ lên/trái/phải trên sheet cỡ gốc.

## Lỗi idle xuống (người dùng báo giữa chừng) + 2 lỗi tự gây ra lúc sửa
Trượng cầm sát hông (không giống concept, tay không giơ ra) + đầu dưới trượng xuyên qua đế 12-27 px. Hai phần:
1. Vẽ lại: dùng `wizard-outfit-d-t0-idle-4dirs-bald-chatgpt-2-arm-out.png` (người dùng gửi) chỉ cho hướng xuống.
2. `artY` riêng cho `idle-stance` hướng xuống: dời điểm cầm trên art trượng (92x392) từ giữa (hàng 196) xuống hàng 260 (trong đoạn cán trơn dưới pha lê/dây leo) — còn ~16 px cách đế. Hướng xuống luôn góc 0° nên cách này không có vấn đề gì.

Tôi tự ý áp CÙNG Ý TƯỞNG (`artY`) sang walk/attack1-4/idle lên-trái-phải cho đỡ xuyên chân (hệ neo chung `held-wizard-melee.json`, không phải `idle-stance`) — và gây 2 lỗi:
- **Lỗi 1 (người dùng phát hiện, báo lại):** `artY` ở hệ neo chung dịch THẲNG XUỐNG MÀN HÌNH trước khi xoay theo góc frame (khác `idle-stance` tự tính affine nên artY ở đó đúng là "dọc theo thân gậy"). Góc 0° (artY trùng "dọc thân") thì đúng, góc lớn (idle PHẢI 70°) thì trượng xoay lệch hẳn, nằm ngang cắt qua mặt. Đã bỏ `artY` ở hệ neo chung, thay bằng dịch thẳng `x, y` theo đúng góc frame (`x += dist*sin(góc), y -= dist*cos(góc)`, giống công thức `slide` có sẵn trong code combo) — đúng với mọi góc, đã kiểm `attack2`/`attack4` phải góc 80-100° không còn vấn đề.
- **Lỗi 2 (tự phát hiện sau khi sửa lỗi 1):** đổi sang dịch `x, y` làm idle TRÁI gần như mất hẳn trượng (chỉ còn 1 chấm pha lê cạnh đầu). Trượng trái/lên vẽ `behind: true` (sau thân), dịch nắm tay lên ~39 px kéo luôn đoạn thò ra ngoài dáng người (phần duy nhất còn thấy được) thụt vào trong bóng thân. Sửa: bỏ qua mọi frame có `behind: true` ở cả 4 tool, giữ y nguyên như trước khi sửa hôm nay.

Kết quả cuối: hướng xuống (idle/walk/attack) và idle PHẢI hết xuyên chân/lỗi xoay; idle LÊN/TRÁI và vài frame walk/attack lên/trái quay lại đúng dáng ban đầu (không sửa, vì `behind` che bớt nên không đáng mạo hiểm). Chi tiết + công thức: Sổ lỗi `docs/sprite-rules.md` (4 dòng 2026-11-14, đọc theo thứ tự).

## Ảnh đã xem (offline, ghép đúng cách game vẽ)
- Idle xuống 4 frame thở (đã sửa): `plans/reports/wizard-outfit-d-261114-check/idle-down-4frames.png`
- Walk: `plans/reports/wizard-outfit-d-261114-check/walk-check2.png` (+ `walk2-{down,up,left,right}.png` phóng to)
- Attack1-4 xuống (sau khi thêm artY): `plans/reports/wizard-outfit-d-261114-check/attack-down-check2.png` (+ `attack-down2-row{0,6,9}.png`, `a1b-f*.png`, `a3b-f*.png`, `a4-f*.png`)
- Attack1-4 lên/trái/phải: `plans/reports/wizard-outfit-d-261114-check/attacks-ulr-check.png` (+ `ulr-row{0,9,27}.png`)
- Grid tay nắm (kiểm fist-detect trước khi ghi): `plans/reports/wizard-outfit-d-261114-check/walk-grid/*.png`, `attacks-grid/*.png`

## Ảnh đã xem trong game (build từ bản sao, không đụng project đang mở)
Build: `C:\temp\player-wizard-d\Zenonia.exe` (từ `C:\temp\zu-copy`, `Tools/build-from-copy.ps1`, không Release). Build lại 3 lần trong lúc sửa (idle xuống xong → lỗi 1 → lỗi 2), ảnh cuối cùng (đã sửa hết) có tiền tố `f2-`:
- Idle 4 hướng (bản cuối): `shots\f2-idle-down_1.png`, `f2-idle-up_1.png`, `f2-idle-left_1.png`, `f2-idle-right_1.png` — xuống + phải cầm trượng chắc không xuyên chân không cắt mặt; lên + trái về lại dáng ban đầu (trượng phần lớn khuất sau thân, không sửa).
- Idle xuống, 2 kiểu tóc (0, 2): `shots\idle-down-h0_3.png`, `idle-down-h2_2.png`.
- Walk phải/trái (`--record-walk wizard --face {right,left}`, bản cuối): `shots\walk-rec-right\frame_00120.png`, `shots\walk-rec-left\frame_00120.png` — trượng cầm chắc trong tay, không cắt mặt, không chạm đất.
- Walk xuống (`--record-walk wizard --face down`): `shots\walk-rec\frame_00120.png` — trượng cầm chắc, không xuyên chân.
- Combo xuống (`--record-combo wizard --face down`, đánh Frostfang King): `shots\combo-rec\frame_{00060,00150,00250,00350}.png` — đòn 1 (đâm ngang), đòn cắm băng (Glacier Slam, cố ý chạm đất), 2 tư thế đứng cầm trượng bình thường — đều đúng, không xuyên chân.
- Offline (ghép đúng cách game vẽ) attack2/attack4 phải, góc 80-100° — không bị cắt mặt như idle phải trước khi sửa: `plans/reports/wizard-outfit-d-261114-check/attack2-4-right-recheck.png`.

## Kiểm tra
- `~/.dotnet/dotnet.exe test Tests/dotnet/ZenoniaSkills.Tests` — pass 313/313.
- `~/.dotnet/dotnet.exe test Tests/dotnet/ZenoniaNet.Tests` — pass 404/404.
- Không sửa file C# nào (chỉ Python/json/png), không đổi số liệu skill, không đổi `dev-start.json`.

## Phần CHƯA xem / chưa làm
- Walk/combo hướng lên/trái/phải: chưa chụp trong game (chỉ xem offline qua `walk-check2.png`/`attacks-ulr-check.png`); đã sửa `artY` như hướng xuống, offline không còn xuyên chân.
- attack2-right frame 1 tay sau không giơ; attack3-up frame 3-4 tay trái dang hơn — lỗi đã biết từ lúc vẽ (ghi trong plan.md), chấp nhận, không sửa.
- Bước 5 (tóc v4 co theo đầu mới), bước 6 (hurt, dash, ảnh chọn nhân vật, bậc áo T1-T9 theo bộ D): chưa làm, giữ nguyên trạng thái "chưa" của plan.
- `Assets/ArtLibrary/classes/wizard/attack-board/body/old/{armed,bare,hand}-attack4.png` (file mới, không theo dõi git trước đó): bản backup phụ do `ensure_sheet` của tool nền tự tạo khi dò thấy sheet sai cỡ lúc tôi chạy nhầm thứ tự (mục trên); không ảnh hưởng tới kết quả cuối, có thể xoá nếu người dùng muốn gọn.

## File đổi
Tool mới: `Tools/build-wizard-outfit-d-{walk,attacks,combo-attacks}.py`. Sửa: `Tools/build-wizard-outfit-d-idle-dirs.py`, `Tools/build-wizard-hair-on-bald-body.py`. Docs: `docs/sprite-rules.md` (Sổ lỗi), `plans/261114-0821-wizard-outfit-d-redesign/plan.md` (bước 2-4). Resources Wizard: `body/{bald,over-hair,bare,hand,headless,armed}/{idle,walk,attack1,attack2,attack3,attack4}.png`, `body/held-anchors.json`, `Data/classes/wizard/{hair-anchors,head-anchors,held-wizard-melee}.json`. Nguồn mới: `Assets/ArtLibrary/classes/wizard/{attack-board/outfit-d,walk-board/outfit-d-walk}/`, `outfits/outfit-d-t0/src/*-arm-out.png`. KHÔNG commit (theo yêu cầu).

## Việc thêm: idle TRÁI/PHẢI cao hơn Warrior/Mechanic (người dùng báo sau, đo trong game)
Đo thẳng trên `body/bald/idle.png` (không qua camera, chính xác hơn): cằm→đế XUỐNG và LÊN/TRÁI/PHẢI **giống hệt nhau** (51 px) — thân đặt đúng, không phải lỗi "thân dài hơn" như người dùng đoán qua ảnh chụp. Lệch nằm ở ĐẦU: đỉnh sọ→cằm hướng lên/trái/phải = 105 px, hướng xuống chỉ 91 px (~15% to hơn) — do ảnh ChatGPT vẽ nguồn `-chatgpt-1.png` (lên/trái/phải, vẽ đợt đầu) có đầu to hơn thân so với ảnh riêng hướng xuống (vẽ đợt sau, tay giơ ra); không phải lỗi logic tool.

Không làm đúng y lời dặn "co đều 2 trục" (sẽ co luôn phần thân cổ→đế đang ĐÚNG xuống sai theo) — chỉ co riêng phần ĐẦU bằng hàm `warp()` có sẵn (cùng cách đã dùng sửa đầu to Mechanic), giữ nguyên thân dưới cằm. Hàm mới `shrink_head()` trong `Tools/build-wizard-outfit-d-idle-dirs.py`, hệ số co tự tính mỗi lần chạy (đầu lên/trái/phải ÷ đầu xuống, đo trong chính lần `fit` đó, không chỉnh tay). Đã kiểm walk + attack1-4 lên/trái/phải (nguồn vẽ khác, tool khớp cỡ khác hẳn — `BODY_H`/`drawn_chin`, không qua `chin_src` ước lượng của idle): chênh đầu dưới 15 px, không đáng kể, KHÔNG sửa.

Tiện thể sửa `HAIR_STYLES` 5→4 trong tool này và mặc định `styles` của `build-wizard-hair-on-bald-body.py check()` — `fit`/`preview` báo lỗi `FileNotFoundError: heads/hair/4/down.png` vì tóc Wizard vừa rút còn 4 kiểu (0-3, theo người dùng dặn). Không đụng gì trong `heads/hair/`, chỉ sửa số đếm trong 2 tool.

Đo lại: đầu còn 91-92 px cả 4 hướng (trước: 91/105/105/105), ghép có tóc (`compose`) cao 164/163/164/163 px xuống/lên/trái/phải — khớp Warrior (164) / Mechanic (163) mọi hướng. Ảnh offline: `plans/reports/wizard-outfit-d-261114-check/idle-headfix-check.png` (12 ô: 4 hướng x 3 kiểu tóc), `walk-attack1-headcheck.png` (walk + attack1 cả 4 hướng, không sửa, để đối chiếu). Ảnh trong game 3 class cạnh nhau (crop từ `C:\temp\player-wizard-d\shots\hcheck\{wizard,warrior,mechanic}-{up,left,right}_1.png`, map Oakhaven, cùng zoom): `plans/reports/wizard-outfit-d-261114-check/hcheck-{up,left,right}.png` — cả 3 hướng Wizard giờ cao bằng Warrior/Mechanic.

Build lại `C:\temp\player-wizard-d\Zenonia.exe` sau fix, chạy lại `~/.dotnet/dotnet.exe test` (313/313, 404/404) — vẫn pass. Không đổi `dev-start.json`, không commit. Ghi Sổ lỗi `docs/sprite-rules.md` dòng cuối (2026-11-14).

## Việc thêm: idle chỉ hướng xuống có đủ trượng + sách (người dùng báo sau cùng)
Trước: xuống đủ cả hai; lên/trái thiếu hẳn sách (không có anchor); phải thiếu sách, trượng cầm chéo ~70° gần nằm ngang; trái trượng gần như khuất hoàn toàn sau lưng.

Nguyên nhân (đọc code trước khi sửa): `held-wizard-melee.json` chỉ có "book" cho hướng xuống (code cũ chỉ tính `book_fist` cho `d=="down"`). Hàm dò nắm tay `fists()` bỏ sót nắm tay phải hướng LÊN vì blob đúng 19px, dưới ngưỡng 20px cũ (hạ xuống 15px để bắt đủ). `staff_fist()` cũ không theo quy ước "tay nào cầm trượng theo hướng" của hệ attack (`build-wizard-attack-board-repaint.py.fists_of`: lên/phải = tay phải ở x lớn hơn, xuống/trái = tay phải ở x nhỏ hơn) nên hướng PHẢI gán nhầm trượng vào tay lẽ ra cầm sách.

Cách sửa, `Tools/build-wizard-outfit-d-idle-dirs.py`:
- Hàm mới `hand_fists(d, fs)` thay `staff_fist`/`book_fist`, đúng quy ước tay như hệ attack.
- `STAFF_ANGLE` đổi hết: up 0°, left -8°, right 8° (trước 18/-20/70°) — mọi hướng trượng gần thẳng đứng, pha lê lên, ≤15°.
- `STAFF_BEHIND` mới: up/right = false (thấy rõ cả hai tay), left = true (trượng ở tay xa, theo đúng quy tắc tư thế cầm 4 — không đổi tay khi quay trái).
- Hướng TRÁI chỉ có 1 nắm tay vẽ sẵn (tay sách); tay trượng (tay xa) KHÔNG có hình vẽ trong thân — không vẽ lại art (không cần Chrome): đặt `SYNTH_STAFF_XY = (85, 100)` ước lượng gần vai, đủ cao để tự nhiên không xuyên đế (không cần dịch `STAFF_GRIP_DIST`), chỉ phần pha lê phía trên ló ra cạnh tóc/vai — đúng yêu cầu "trượng sau thân nhưng vẫn thấy phần trên". Không vẽ đĩa nắm tay ở vị trí giả lập này (không có da tay thật ở đó).
- `cmd_install()`: ghi thêm anchor "book" cho lên/trái/phải vào `held-wizard-melee.json` (tạo mới nếu chưa có, thay vì chỉ cập nhật như trước — trước đây 3 hướng này chưa từng có entry "book"); `fr["behind"]` của trượng giờ lấy từ `STAFF_BEHIND` thay vì giữ nguyên giá trị cũ trong file.
- `cmd_fit()`: vẽ thêm đĩa nắm tay (`body/hand/idle.png`) cho cả 2 tay thật mỗi hướng (trước: lên/trái không vẽ đĩa nào, phải chỉ vẽ 1).

Đo lại: không đo số (không có "kích thước" cần đo ở việc này, chỉ là anchor/tư thế), chỉ xem ảnh ghép + trong game.

Ảnh đã xem:
- Preview tư thế (ghép như game, phóng to): `plans/reports/wizard-outfit-d-261114-check/pose-{up,left,right}.png`, zoom đầu hướng trái để thấy rõ pha lê ló ra: `pose-left-head-zoom.png`.
- Ghép offline đúng cách game vẽ, cả 4 frame thở, cả 4 hướng: `plans/reports/wizard-outfit-d-261114-check/idle-bookstaff-4frames.png`.
- Trong game (build lại `C:\temp\player-wizard-d\Zenonia.exe`), map Oakhaven: `shots\f3-{down,up,left,right}_1.png` — cả 4 hướng thấy rõ trượng + sách, trượng không che mặt, không xuyên đế.

dotnet test vẫn pass (313/313, 404/404). Không đổi `dev-start.json`, không commit, không đụng `heads/hair/`.

## Việc thêm: idle PHẢI vẫn ngược tay sau lần sửa trước (người dùng báo lại)
Lần sửa trước (mục trên) mượn quy ước "lên/phải = tay phải ở x lớn hơn" từ hệ attack (`build-wizard-attack-board-repaint.py.fists_of`) cho CẢ lên lẫn phải — đúng cho lên nhưng SAI cho phải (quy ước đó tính cho tư thế đòn đánh, không phải đứng im).

Đo lại bằng số (không đoán qua ảnh): tính tỉ lệ pixel thân/vải trong vòng bán kính 10-20px quanh mỗi nắm tay. Nắm tay x=89.7 có 75% vòng là thân (tay GẦN, "đè lên thân" đúng như người dùng mô tả); nắm tay x=128.7 chỉ 47% (tay XA, tách khỏi thân). Vậy hướng PHẢI: x NHỎ hơn mới là tay phải/gần (ngược với hướng lên).

Sửa `hand_fists()`: tách "right" ra khỏi nhánh x-lớn-là-tay-phải, chỉ còn "up" dùng nhánh đó; down/left/right giờ đều "x nhỏ hơn = tay phải". Thêm `BOOK_BEHIND = {"right": True}` để sách hướng phải vẽ sau thân (tay xa), đối xứng với trái (trượng tay xa sau thân).

Kiểm walk/attack hướng phải bằng CÙNG phép đo vòng pixel: `body/bald/walk.png` phải — nắm tay đang gán cho trượng có vòng fill 99.8% (gần như trùm hết thân) → ĐÃ đúng tay gần sẵn, không cần sửa (khớp lời người dùng "walk-right ChatGPT vẽ đúng tay gần"). `attack1-4` hướng phải: các tư thế đánh (thrust, swing...) có nắm tay giơ ra xa theo chủ ý từng đòn, không theo quy ước "đứng im" này — không đổi, đã xem ảnh trước đó (`attack2-4-right-recheck.png`) không có gì bất thường.

Ảnh đã xem: `plans/reports/wizard-outfit-d-261114-check/right-fists-marked.png` (đánh dấu 2 nắm tay trước khi đo), `idle-rightfix-4frames.png` (ghép offline 4 frame thở, phải + trái đối chiếu), trong game: `shots\f4-right_1.png`, `f4-left_1.png` (không hồi quy), `shots\walk-rec-right2\frame_00120.png`.

dotnet test vẫn pass (313/313, 404/404). Không đổi dev-start.json, không commit, không đụng heads/hair/.

## Câu hỏi mở
1. Idle LÊN/TRÁI và vài frame walk/attack lên/trái (đòn vẽ `behind: true`) giờ y nguyên như trước khi tôi đụng vào hôm nay (trượng phần lớn khuất sau thân, có thể vẫn hơi chạm đất ở đoạn thò ra như idle trái) — có muốn sửa tiếp không, hay giữ vậy (an toàn hơn, vì mục trên cho thấy sửa "behind" dễ làm trượng biến mất)?
2. Xoá 3 file backup phụ ở `attack-board/body/old/` (mục trên) hay giữ?
3. Walk/attack1-4 lên/trái/phải chênh đầu nhẹ (dưới 15 px, không rõ bằng mắt) so với hướng xuống — chấp nhận hay muốn co nốt cho khớp tuyệt đối?
