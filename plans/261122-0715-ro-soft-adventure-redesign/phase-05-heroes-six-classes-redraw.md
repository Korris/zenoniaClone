# Phase 05: Nhân vật 6 class

Ưu tiên: P0 (Warrior) / P1 (còn lại). Trạng thái: test Warrior idle xuống XONG 22/11 (`docs/reference/ro-soft-adventure-redesign/adv-warrior-idle-down-chatgpt-1.png`). **Đọc hết `docs/sprite-rules.md` trước khi làm** (khối Adventure đầu file + điều 8 cỡ đầu + quy tắc tư thế cầm + "Hình mẫu nhân vật").

## Hệ đang chạy (nhánh gốc ro-fantasy-heroes)
- Nhân vật nhiều lớp theo class: `Sprites/hero-layered/ro-fantasy/<class>/body/{idle,walk,attack}.png` (+ `-mask`, `hand/`), ô 320x256, feetY 244, bodyPx 200, 5 hướng vẽ (xuống, chéo xuống-phải, phải, chéo lên-phải, lên; 3 hướng trái = lật); tóc xám 3 kiểu `hair/0..2.png`; vũ khí lớp riêng (`Sprites/hero-layered/weapons/`, neo trong `Data/hero-layered/ro-fantasy/<class>/class.json`); mũ `headgear/`.
- Tool: `Tools/build-ro-fantasy-hero-game-assets.py` (`write`, `fists`, `grips`, `check`) + Warrior attack 8x8 `Tools/ro_fantasy_attack8_cut.py`, `ro_fantasy_attack8_grips.py`.
- 5 class có idle (1 frame) + walk (4) + attack (1 đòn, Warrior 8 hướng x 8 frame). **Fighter chưa có thân RO Fantasy** (còn kiểu cũ `classes/fighter/`).

## Khối lượng vẽ lại (Adventure)
| Class | Idle 5 hướng | Walk 5 x 4 | Attack | Tóc 3 kiểu x 5 | Vũ khí trên tay | Chân dung UI |
|---|---|---|---|---|---|---|
| Warrior | xuống XONG (test), 4 hướng còn | 20 ô | 8 hướng x 8 (64 ô, 11 ảnh như 22/11) | 15 | kiếm, khiên | chọn class + HUD |
| Wizard | 5 | 20 | 5 hướng | 15 | gậy, sách | |
| Mechanic | 5 | 20 | 5 | 15 (+ kính `acc`) | súng, pháo | |
| Assassin (nữ) | 5 | 20 | 5 | 15 (băng đô giữ màu) | 2 dao | |
| Cleric (nữ) | 5 | 20 | 5 | 15 | gậy, khiên tim | |
| Fighter | 5 (MỚI vào hệ nhiều lớp) | 20 | 5 | 15 | găng | |

Mỗi class ~5-7 ảnh ChatGPT (idle 5 hướng 1 ảnh, walk 2-3 ảnh, attack 2-3 ảnh, tóc 1 ảnh, vũ khí 1 ảnh). Tổng ~40 ảnh + vũ khí theo bậc (Wizard / Mechanic / Cleric / Assassin có 10 bậc vũ khí: đổi tông bằng tool trước, vẽ lại sau nếu lệch).

## Đường rẻ nhất (theo thứ tự)
1. **Tool đổi tông tạm** (đã thử: `warrior-idle-regrade-tool-test.png`): giảm bão hoà ~20 %, ấm, tối nhẹ, bóng nâu cho walk / attack / vũ khí của mọi class => cả game hết lệch tông ngay trong lúc chờ vẽ lại. Không đủ cho idle / chân dung vì thiết kế vẫn Fantasy (vàng bóng, miệng cười).
2. **ChatGPT vẽ lại idle 5 hướng** mỗi class (gửi kèm: idle Fantasy cùng hướng phóng 3x để giữ cỡ / dáng, `adv-warrior-idle-down-chatgpt-1.png` làm master tông, khối luật Adventure, cỡ đầu đo từ idle, tóc xám 4 tông, tay không vũ khí như sheet gốc). Cài bằng `build-ro-fantasy-hero-game-assets.py write` + `fists` + `grips` + `check` (ảnh `<class>-idle-8dir.png`).
3. **Walk, attack** vẽ lại theo idle mới (giữ nhịp bảng 22/11 cho Warrior attack: `ArtLibrary/ro-fantasy-heroes/prompts/warrior-attack-8dir.md`).
4. **Tóc** 3 kiểu xám trên đầu mới (`ro_fantasy_hair_cells.py`).
5. **Fighter** sau cùng (cần nối vào hệ nhiều lớp: `class.json`, găng 2 tay).

## Thứ tự
Warrior đủ bộ (người dùng duyệt) -> Cleric, Wizard (đang hay chơi) -> Mechanic, Assassin -> Fighter. Song song: chân dung chọn class 6 class (màn #17).

## Kiểm
Ghép như game (thân + tóc + vũ khí + nắm tay) mọi hướng / frame, cỡ game 200 px, cạnh nhau 6 class; tỷ lệ đầu lệch ≤ 5 % so với idle; vũ khí không đè tay, không đổi tay khi quay trái. ChatGPT chấm checklist.

## Câu hỏi mở
- Fighter có đưa vào hệ nhiều lớp luôn đợt này không?
- Giữ bộ đồ 10 bậc (T1-T9) kiểu cũ hay chỉ Lv1 trước?
