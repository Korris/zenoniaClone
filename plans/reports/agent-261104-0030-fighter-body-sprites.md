# Fighter phase 2: thân sprite và anchor găng (2026-11-04 00:30-01:35)

Plan: `plans/261104-0025-fighter-class/phase-02-body-sprites.md` (đã cập nhật, status done). Không commit, không build exe.

## Đã làm
- ChatGPT (tài khoản Tay "Tân Korris", Plus) vẽ 24 dải trong một chat "Create Brawler Sprite Strip" https://chatgpt.com/c/6aea1911-3668-83ec-ab96-0fafd90ba8a6: đính kèm `hero-select/boxing.png` + crop mặt ở tin đầu, các dải sau tả bằng chữ. Thứ tự: idle, walk, melee1-4, hurt, dash hướng phải; rồi xuống; rồi lên. 24 lượt, không dải nào phải vẽ lại, không hết lượt, không cần sang Tan Pham. Nhân vật đúng: nam, tóc nâu đen dựng, khăn đỏ, ngực trần dây da, giáp đỏ vàng, quần đen, tay không (không dải nào có găng).
- Nguồn: `Assets/ArtLibrary/classes/fighter/new-style/src/<anim>-<dir>.png` (1536x1024 hoặc 2172x724; `hurt-down` 1774x887).
- Tool `Tools/build-fighter-new-style.py` (406 dòng) + `Tools/fighter-strip-helpers.py` (107 dòng, key green, split, place, breathe, grid, meta): `cut OUT` (lưới + ứng viên nắm tay da, không ra ứng viên nào vì cẳng tay cũng là da), `fists OUT`, `preview OUT`, `write`, `armed`. Tỉ lệ hàng: idle/walk/dash theo chiều cao 164, hàng khác theo bề ngang tóc so với idle cùng hướng. Idle = frame 0 thở 4 frame (`LIFT`). Trái = phải lật, anchor đổi tay (`flipped`: angle -a, sx -sx).
- `FISTS` 24 hàng đọc bằng mắt từ crop phóng to của lưới `cut` (script tạm `fistcrop.py` trong scratchpad, cắt theo vùng và nhãn vạch 12 px). Hướng knuckle: theo cánh tay từ đầu ra nắm tay, kẹp vào `CONE` của hướng nhìn, thế thủ dùng `GUARD`; `ANGLE_FIX` cho móc và uppercut hàng xuống. `behind`: phải = tay trái phía sau lưng; lên = tay gần đầu hoặc trong thân; `BEHIND` ghi riêng 11 frame hàng lên. `sx` -1 cho tay trái hàng xuống/lên (ngón cái quay vào giữa).
- Ghi: `Assets/Resources/Sprites/classes/fighter/body/{bare,armed}/{idle,walk,hurt,death,melee1,melee2,melee3,melee4,dash}.png` (+ `.meta` chép từ `assassin/body/bare/idle.png.meta` guid mới, folder meta `fighter.meta`, `body.meta`, `bare.meta`, `armed.meta`), `armed` = thân + găng `leather` ghép theo anchor, `death` = bản sao `hurt`. `held-fighter.json` chỉ đổi `anchors` (64 mục), đọc lại file ngay trước khi ghi, `weapon` 10 tier của agent găng giữ nguyên, file vẫn một dòng.
- Tóc: hàng xuống/lên kéo phần đỏ tối về nâu như hàng phải (`match_hair`).

## Đã xem bằng mắt
- 24 dải tải về (bản 1/2): số frame, hướng mặt, tay không, trang phục.
- Ảnh ghép `preview` 32 hàng (8 anim x 4 hướng) với găng `leather-base.png` thật (grip 122, 100) ở `s` 0,18: găng nằm trên nắm tay, hướng đấm đúng, tay xa sau thân, hàng lên găng ló bên đầu, uppercut lên trời, không frame nào găng đè mặt. Hàng lên xem từng frame sau khi sửa `BEHIND`. Sheet `armed/melee2` sau khi ghi.
- Chưa xem trong game (không build theo brief).

## Còn tồn
- Hàng lên tóc còn đỏ tươi (vệt sáng cùng màu khăn, không tách bằng màu được); muốn hết phải vẽ lại 8 dải hàng lên.
- `melee1`, `melee2` xuống frame 2: nắm tay phối cảnh to hơn găng, lòi ra dưới găng.
- `hurt` lên frame 1 găng phải hơi lệch trái so với nắm tay vài px.
- Găng cỡ `s` 0,18 (36 px) trông vừa ở ảnh ghép; người dùng từng chê dao, gậy, súng nhỏ nên có thể muốn to hơn: đổi `GLOVE_S` rồi `write`.
- `death` chỉ là `hurt`; chưa có hàng ngã.

## Sổ lỗi mới
7 dòng Fighter 2026-11-04 trong `docs/sprite-weapon-rules.md` (dash co nhỏ vì tóc bay, đọc nhầm lưới 12 px, `behind` hàng lên, góc móc/uppercut, tóc đỏ, nắm tay phối cảnh, mất nhóm tab).

## Câu hỏi chưa giải quyết
- Cỡ găng 0,18 có đủ to với người dùng không (so găng lửa ở màn chọn tướng to gấp 3)?
- Hàng lên có cần vẽ lại cho tóc nâu không, hay chấp nhận?
