# RO Fantasy: đi + đánh thật cho Assassin, Cleric (19/11/2026)

Branch `worktree-agent-a5de8217cf5b476f8` (gốc `ro-fantasy-heroes` a3dd6bf5). Exe: `D:\temp\player-rofantasy3\Zenonia.exe` (Build Succeeded, 0 lỗi). Bản sao build: `D:\temp\zu-copy-rofantasy3` (Library chép từ `zu-copy-rofantasy2`).

## Đã làm
- Chạy `write` → `fists` → `grips` → `check` cho `assassin`, `cleric`. Game có `walk.png`, `attack.png` (5 hàng x 4 frame) + lớp nắm tay.
- Nắm tay 80 frame đọc trên lưới 4x, ghi vào `Tools/ro_fantasy_motion_fists.py`:
  - Assassin: f0 hai tay bắt chéo (lấy đà), f1 chém, f2 hai dao dang ra hai bên, f3 thu. Hướng lên / chéo lên f0 hai tay khuất: dao vẽ sau thân.
  - Cleric: tay phải (gậy) giơ cạnh mặt ở f0, f1, f3, đâm ra ở f2; tay trái giữ khiên ở hông. Đi hướng phải: tay gần là tay phải, vung ra sau ở f0, ra trước ở f2.
- Chỉnh tư thế Cleric trong `Tools/ro_fantasy_grips.py`:
  - Đi: dùng `steady_staff` như Wizard. Đầu gậy giữ chỗ idle, hướng ngang dời ra trước 36 px, hạ 8 px. Trước đó gậy nhảy ra sau thân ở 3/4 frame hướng phải.
  - Đánh: `CLERIC_STAFF_ATTACK`. Đánh xuống f2 đặt -110°, chéo xuống f1 đặt -45°. Chéo xuống-trái f1 lấy góc của f0. Trước đó pha lê chĩa xuống chân / vắt ngang thân.
- Chấm xanh: thêm `despeck` trong `Tools/ro_fantasy_body_cut.py`, chạy trên ảnh nguồn và chạy lại sau khi co về cỡ game.
  - Lý do: hàng đi / đánh cắt bằng `key_green` không qua bước lọc chấm (chỉ idle có).
  - Đã hết vệt ở mép vạt áo Cleric hướng lên (cả đi và đánh) và cọng ô liu cạnh khăn Assassin đi phải f0.
- Tóc dài bạc Cleric (lỗi có từ đợt tóc): thanh xám ngang cằm che miệng ở hướng chéo xuống. `ro_fantasy_hair_cells.behind_neck` cắt từ cằm vẽ thật, chỉ áp cho Cleric. Với Assassin, lỗ khăn che mặt làm cắt ngang tóc bob / bím nên không áp; tóc Assassin giữ nguyên như HEAD.
- `HEAD_FIX` sẵn có đã chạy: Assassin đánh chéo xuống x1,044, Cleric đánh phải x0,957.
- Sổ lỗi `docs/sprite-rules.md`: thêm 4 dòng (chấm xanh, gậy đi, gậy đánh, tóc bạc), cập nhật dòng tool RO Fantasy và dòng `HEAD_FIX`.

## Ảnh
- Ghép offline, đã xem từng frame, trong `docs/reference/ro-fantasy-heroes-ingame/`:
  - `{assassin,cleric}-{walk,attack}-8dir.png`: 4 frame x 8 hướng;
  - `<class>-{idle,walk,attack}.png`;
  - lưới nắm tay `fists-<class>-<walk|attack>.png`.
- Đã thấy tận mắt trên ảnh ghép:
  - Assassin: hai dao trong nắm tay, lưỡi chĩa ra ngoài khi đi, khi đánh vung theo tay (dang hai bên ở f2). Hướng trái vẫn đúng tay. Dao không đè mặt.
  - Cleric: gậy tay phải, khiên tay trái, cả hướng trái. Gậy không đè mặt.
- Trong game (`--record-layered-real`, zoom 62; 10 hero, 2 mỗi class), đã xem, trong `docs/reference/ro-fantasy-heroes-ingame/ingame/`:
  - `walk-art-assassin-cleric-crowd-down-left.png`, `walk-art-assassin-cleric-crowd-right.png`;
  - `attack-art-assassin-cleric-crowd-left.png`, `-dirs.png`, `-dirs-late.png`. Hướng đánh lấy theo hero, nên có cả hướng chéo, lên, xuống.
- Thấy trong game:
  - Assassin hai dao trong tay khi đi chéo trái và đi phải, khi đánh trái và đánh các hướng (dao dang hai bên, vệt chém tím).
  - Cleric gậy tay phải, khiên tay trái khi đi chéo trái. Đi phải gậy chĩa ra trước, không đè mặt. Đánh trái giơ gậy cạnh mặt. Đánh chéo xuống-phải và lưng thấy gậy ở tay.
  - Cả 5 class cùng một cảnh.

## Còn sai / chưa xem
- Cleric đánh hướng trái f2: khiên nằm ở nắm tay đang đâm ra (art lật từ hướng phải), gậy ở nắm tay thấp phía sau. Vẫn đúng tay nhưng nhìn như khiên trôi giữa cán gậy.
- Cleric đánh phải / chéo lên / lên f3: gậy dựng sau thân, pha lê ló trên đỉnh tóc.
- Cleric đi hướng trái f2: gậy sau thân, chỉ ló pha lê cạnh mặt (giống idle trái).
- Assassin đánh lên / chéo lên f0: hai tay khuất nên dao gần như ẩn sau thân.
- Lỗi tóc có từ trước, chưa sửa:
  - Assassin tóc bob bạc hướng xuống có một con mắt vẽ thừa trên tóc.
  - Cleric tóc bạc hướng chéo xuống: lọn trong mờ cạnh tai, một đường sáng mờ 1 px dưới cằm.
- `despeck` chưa chạy lại cho Warrior / Wizard / Mechanic. Hàng đi / đánh của 3 class này có thể còn chấm xanh nhỏ.
- Nắm tay đọc bằng mắt, có thể lệch khoảng 3 px.
- Chưa xem: online thật, skill, lướt, bị đánh.

## Kiểm tra
- `ZenoniaSkills.Tests` 426/426, `ZenoniaNet.Tests` 422/422 (SDK 10).

## Câu hỏi
1. Cleric đánh trái f2 (khiên ở tay đang đâm): chấp nhận, hay giấu khiên sau thân ở frame đó?
2. Có cho chạy lại `write` 3 class đầu để lọc chấm xanh hàng đi / đánh không?
