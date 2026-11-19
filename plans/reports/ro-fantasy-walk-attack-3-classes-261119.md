# RO Fantasy: đi + đánh thật cho Warrior, Wizard, Mechanic (19/11/2026)

Branch `worktree-agent-a3faab4c60f35f93a` (gốc `ro-fantasy-heroes` fb95f731). Exe: `D:\temp\player-rofantasy2\Zenonia.exe` (Build Succeeded, 0 lỗi). Bản sao build: `D:\temp\zu-copy-rofantasy2`.

## Đã làm
- Chạy `write` → `fists` → `grips` → `check` cho 3 class. Game có sheet `walk.png`, `attack.png` (5 hàng x 4 frame), thay cho frame đứng nhún.
- Nắm tay đi/đánh: đọc tay trên lưới 4x, ghi ở `Tools/ro_fantasy_motion_fists.py` (gộp vào `OVERRIDES`). Dò tự động sai ở phần lớn frame: lấy khoá thắt lưng hay túi làm nắm tay, đổi tay R/L.
- Sửa tư thế riêng từng class trong `Tools/ro_fantasy_grips.py`:
  - `sword_down` (Warrior):
    - Idle, đi, thu đòn ở hướng xuống và chéo xuống: kiếm chĩa xuống chếch ra ngoài, mũi ở hàng 240 (đường chân 244).
    - Đi ngang, tay vung ra sau: lưỡi kéo theo sau.
    - Các frame khác: mũi không vượt đường chân.
  - `steady_staff` (Wizard):
    - Khi đi: đầu gậy giữ chỗ như idle (hướng ngang dời ra trước 22 px), không nhảy trước/sau thân.
    - Khi đánh: đầu gậy không xuống dưới hàng 226.
  - `aim_raised` (Mechanic): súng ở tay giơ cao chĩa theo cánh tay, súng ở tay thấp để nghỉ, áp dụng cả hướng trái.
- Đầu lệch 5-6 % (theo quyết định điều phối): `Tools/ro_fantasy_head_fix.py`. Thân khớp theo chiều cao idle, chỉ co/giãn vùng đầu cho bằng bề ngang sọ idle:
  - Wizard đánh lên: x1,044.
  - Mechanic đi phải: x1,035.
  - Mechanic đánh chéo xuống: x1,039.
- `check`:
  - cắt nguyên ô 320 px (bản cũ cắt mất mũi kiếm);
  - thêm ảnh `<class>-<walk|attack>-8dir.png`: 4 frame x 8 hướng.
- Sổ lỗi `docs/sprite-rules.md`: thêm 4 dòng, cập nhật dòng kiếm idle và dòng tool RO Fantasy.

## Ảnh (đã xem)
- Offline, trong `docs/reference/ro-fantasy-heroes-ingame/`:
  - `{warrior,wizard,mechanic}-{walk,attack}-8dir.png`;
  - `<class>-idle.png`;
  - lưới nắm tay `fists-<class>-<anim>.png`.
- Trong game, trong `docs/reference/ro-fantasy-heroes-ingame/ingame/`:
  - `walk-art-*.png`, `attack-art-*.png`: đám đông 10 hero đi chéo trái, đi phải, đánh nhiều hướng, đánh trái;
  - Warrior đánh chéo trái và chéo lên trái, từng khung;
  - `attack-art-crowd-dirs-headfix.png`: bản build sau khi sửa đầu.
- Đã thấy tận mắt:
  - kiếm idle của Warrior chĩa xuống ra ngoài, không còn nằm ngang;
  - kiếm, gậy, súng nằm trong nắm tay khi đi và khi đánh, vung theo đòn;
  - hướng trái: vũ khí vẫn ở tay phải.

## Còn sai / chưa xem
- Frame còn sai:
  - Warrior đánh chéo lên-phải f0: người xoay ra trước (do art), kiếm cầm thấp sau thân.
  - Wizard đi xuống / chéo xuống: gậy dựng sát mép mặt bên trái (như idle, không đè mắt).
  - Wizard đánh phải / chéo lên f1: đầu gậy chúi sát đường chân (đã giới hạn nhưng vẫn sát).
  - Mechanic đánh chéo xuống-phải f0-f2: súng ở tay giơ chĩa lên, phần lớn khuất sau nắm tay.
  - Đánh hướng lên: tay duỗi sang ngang (do art, đã chấp nhận).
- Vị trí nắm tay đọc bằng mắt nên có thể lệch khoảng 3 px.
- Chưa xem: chơi online thật, skill, lướt, bị đánh.
- **Assassin, Cleric chưa cài.** Lệnh merge `ro-fantasy-heroes` vào worktree và lệnh chép art của 2 class từ repo chính đều bị chặn quyền (auto mode).
  - Cần người dùng merge (hoặc chép art) rồi chạy lại pipeline cho `assassin`, `cleric`, kèm thêm `OVERRIDES` nắm tay.
  - `HEAD_FIX` đã có sẵn dòng cho Assassin đánh chéo xuống và Cleric đánh phải.
  - Sau khi cắt phải xem chấm xanh ở Cleric đi lên và Assassin đi phải f1.

## Kiểm tra
- `ZenoniaSkills.Tests` 426/426, `ZenoniaNet.Tests` 422/422 (SDK 10).

## Câu hỏi
1. Cho phép merge `ro-fantasy-heroes` vào worktree (hoặc chép art Assassin, Cleric) để cài nốt 2 class không?
