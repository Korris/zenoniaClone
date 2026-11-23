# Phase 02: Xin asset từ ChatGPT (card, ngọc, khung, nút, bàn đấu)

## Context Links
- Mockup gốc: `docs/reference/card-gem-ui/01..06b-*.png`, quái của game: `docs/reference/card-gem-ui/our-monsters-contact-sheet.png`
- Luật art: `docs/sprite-rules.md` (mục "Quy tắc art lấy từ ChatGPT", điều 1, 2, 3, 5, 7), `docs/ui-layout-rules.md` (mục 3: 9-slice, không nướng chữ vào nút)
- Chat ChatGPT card (tài khoản Tay): https://chatgpt.com/c/6b031e60-ffa0-83ec-903d-9449063ac0e0 ; tài khoản theo `CLAUDE.md` mục "Art trên ChatGPT"
- Công cụ cắt: `Tools/cut-ui-paper-sheet.py` + config `Tools/ui-paper-sheets/<sheet>.json`

## Overview
P1, pending, chạy song song phase 01 (không đụng code). Yêu cầu người dùng 23/11: MỌI icon, khung card, nút, nền, hình quái trên card, viên ngọc, badge độ hiếm... phải do ChatGPT vẽ, cắt thành file riêng nền trong suốt theo mockup RO Soft Fantasy. KHÔNG tự vẽ bằng code / script / shape.

## Key Insights
- Bắt buộc đọc hết `docs/sprite-rules.md` và `docs/ui-layout-rules.md` trước khi làm; ghi lỗi mới vào "Sổ lỗi".
- ChatGPT không cho nền trong suốt thật (hay vẽ ô caro): xin nền phẳng `#FF00FF` (art xanh lá thì `#00FF00`), rồi `cut-ui-paper-sheet.py` key ra PNG trong suốt. "Nền trong suốt" = kết quả sau khi cắt.
- Mỗi prompt: "vẽ mới hoàn toàn từ đầu", tông RO Soft Fantasy, hình đặc, không chữ trong hình (chữ do UI vẽ bằng font), các mảnh cách nhau >= 24 px, không chạm mép.
- Được phép trong code: ghép lớp, 9-slice, tint màu, scale/fade/flip animation trên asset có sẵn. Không được: vẽ hình chữ nhật/tròn/viền bằng shape, gradient code thay khung.
- Tải bằng `fetch(img.src)` + thẻ `<a download>` (sprite-rules điều 7), không chụp màn hình; mỗi ảnh tên mới.

## Requirements: danh sách asset
Cỡ = cỡ xuất (px, tham chiếu 1280x720, vẽ gấp 2 rồi cắt co). "9s" = 9-slice, ghi border vào config.

| Sheet | Asset | Số | Cỡ xuất | Đích |
|---|---|---|---|---|
| A khung | khung cửa sổ lớn (9s), panel trong (9s), panel chi tiết (9s), ô lưới card trống (9s) | 4 | 9s, góc 24-32 | `Sprites/ui/paper/cards/frame/` |
| A | nút menu trái normal / selected (9s), tab vùng normal / selected (9s), pill lọc normal / selected (9s) | 6 | cao 52 / 40 / 28 | `.../cards/frame/` |
| A | nút chính teal, vàng (thưởng/Claim), đỏ (nguy hiểm), xanh (điều hướng), disabled (9s) | 5 | cao 48 | `.../cards/frame/` |
| A | nút đóng, mũi tên trang, thanh tiến độ (track + fill 9s), nút mốc album (khoá / đạt / đã nhận), pill Gold | 7 | 32-48 | `.../cards/frame/` |
| B icon | menu trái: My Cards, Monster Album, Convert, Gem Socketing, Gem Removal, Card Duel | 6 | 48x48 | `.../cards/icons/` |
| B | lọc: All, ATK, DEF, HP, Utility; hệ (Earth, Nature, Water, Ice, Fire, Wind, Dark, Holy: chốt theo bảng phase 01) | 5 + 8 | 32x32 / 24x24 | `.../cards/icons/` |
| B | vương miện boss, ổ khoá, badge "Trade Locked", tag "In Deck", badge xN, dấu "?" chưa sưu tầm, icon Gold | 7 | 24-40 | `.../cards/icons/` |
| B | icon "Cards" cho cột More của HUD (cùng kiểu `hud-icon-*`) | 1 | như `hud-icon-more` | `Sprites/ui/ro-soft/hud-icon-cards.png` |
| C card | khung card 4 độ hiếm N/R/SR/SSR (cửa sổ chân dung rỗng, ô 4 số hướng rỗng) + bản boss viền vàng | 5 | 192x256 | `.../cards/card/` |
| C | badge độ hiếm N/R/SR/SSR (không chữ, hoặc chữ là logo chốt) | 4 | 48x24 | `.../cards/card/` |
| C | mặt sau card, viền chủ bàn đấu xanh (mình) / đỏ (địch) (9s), viền chọn vàng (9s) | 4 | 192x256 | `.../cards/card/` |
| D chân dung | 40 chân dung quái, nền chroma, khớp hình quái trong game | 40 | 256x256 | `Sprites/monsters/card-portraits/<monsterId>.png` |
| E ngọc | vỏ ngọc Power (đỏ cam) / Guard (xanh) x 4 độ hiếm, có cửa sổ tròn cho chân dung (dùng `hole`) | 8 | 96x96 | `Sprites/ui/paper/cards/gems/` |
| E | ô ngọc trống / đã khảm / khoá (trên trang bị) | 3 | 56x56 | `.../cards/gems/` |
| F bàn đấu | nền bàn 4x4 có nhãn A-D / 1-4 trống, ô trống / hover / hợp lệ, khay tay bài (9s), khung log (9s) | 6 | bàn 560x560 | `.../cards/board/` |
| F | bảng tỉ số mình / địch, bảng lượt, vòng đồng hồ (track + fill), khung avatar mình / NPC, banner Win / Lose / Draw (không chữ) | 9 | 64-480 | `.../cards/board/` |
| F | chân dung NPC đấu (3 NPC luyện, gồm "Frost Scout Nia") | 3 | 128x128 | `.../cards/board/npc-*.png` |
| G Ranked + cược | huy hiệu bậc Bronze / Silver / Gold / Platinum / Diamond, khung ô cược, dấu Lock / Confirm | 5 + 3 | 96 / 9s | `.../cards/ranked/` |
| G | khung toast "nhận card" (9s) | 1 | cao 64 | `.../cards/frame/` |
| H NPC | Card Master: sheet idle HD + mặt, theo NPC làng hiện có | 2 | như `Sprites/village/npcs/*` | `Sprites/village/npcs/cardMaster*.png` |

Ảnh gốc ChatGPT: `Assets/ArtLibrary/ui/cards/<sheet>/` (đặt tên có nghĩa). Config cắt: `Tools/ui-paper-sheets/cards-<sheet>.json`; 9-slice border: `Tools/ui-paper-sheets/paper-borders.json`. Ảnh xem lại: `docs/reference/card-gem-ui/art-contact/`. Mọi đường dẫn trên ở dưới `Assets/Resources/`.

## Architecture
ChatGPT sheet (chroma) -> tải về `Downloads` -> `git mv`/copy vào `Assets/ArtLibrary/ui/cards/<sheet>/` -> `cut-ui-paper-sheet.py cards-<sheet>.json` -> PNG trong suốt + borders + contact sheet -> mở Unity cho import (không build vào project đang mở).

## Related Code Files
Tạo: `Tools/ui-paper-sheets/cards-{a-frame,b-icons,c-card,d-portraits-1..5,e-gems,f-board,g-ranked}.json`, các PNG ở trên, `docs/reference/card-gem-ui/art-contact/*.png`, mục mới trong `docs/reference/card-gem-ui/design-notes-chatgpt.md` (link chat + prompt dùng).
Sửa: `Tools/ui-paper-sheets/paper-borders.json`. Nếu `cut-ui-paper-sheet.py` không cho `out` ngoài `ui/paper/` (chân dung ở `Sprites/monsters/card-portraits/`) thì thêm tham số đường dẫn đầy đủ, giữ tương thích cũ.
Không đụng code game.

## Implementation Steps
1. Đọc hết `docs/sprite-rules.md`, `docs/ui-layout-rules.md`, `design-notes-chatgpt.md`.
2. Kiểm tra đúng tài khoản (Tay; hết lượt thì Tan Pham, chat mới) trước khi gửi. Ghi link chat vào `docs/pending-tasks.md`.
3. Sheet A, B, C, E, F, G: mỗi tin gửi kèm mockup liên quan (01..06b) + `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png`; prompt ghi danh sách mảnh theo hàng, nền `#FF00FF`, không chữ.
4. Sheet D (40 chân dung, 5 sheet x 8): mỗi quái gửi kèm frame idle trong game (`Sprites/monsters/<id>.png`, cắt 1 frame, phóng nearest 3-4x) + `our-monsters-contact-sheet.png`; ghi "giữ đúng màu, hình dạng, chi tiết quái ở ảnh 1, vẽ thành chân dung bán thân, cùng góc 3/4, cùng ánh sáng". Boss vẽ khí thế hơn.
5. Sheet H (NPC): làm theo mục NPC / "Quy tắc thiết kế" trong sprite-rules, gửi kèm NPC làng hiện có cùng cỡ.
6. Cắt bằng tool, xem contact sheet trên nền tối + sáng (sprite-rules điều 5), so mockup. Lỗi (caro, nhạt, lệch cỡ) -> vẽ lại, ghi Sổ lỗi.
7. Commit ảnh + config (kèm `.meta` do Unity sinh khi mở project).

## Todo List
- [ ] A khung + nút (9s)   - [ ] B icon   - [ ] C khung card + badge
- [ ] D chân dung 1-8 / 9-16 / 17-24 / 25-32 / 33-40
- [ ] E ngọc   - [ ] F bàn đấu + NPC đấu   - [ ] G Ranked + cược + toast   - [ ] H NPC Card Master
- [ ] Contact sheet đã xem, ghi Sổ lỗi nếu có

## Success Criteria
- Đủ mọi dòng trong bảng, PNG trong suốt sạch (không còn viền hồng/xanh, không caro), đúng cỡ và tên.
- 40 chân dung nhận ra đúng quái trong game khi đặt cạnh frame idle.
- Phase 03-08 dựng UI chỉ bằng các asset này (reviewer grep: không có `Texture2D.SetPixel`, không sprite tạo bằng code cho khung / nút / icon).

## Constraints (áp cho mọi phase UI 03-08)
- Chỉ dựng UI bằng asset của phase này (+ asset `Sprites/ui/ro-soft/` sẵn có nếu đúng tông). Thiếu asset -> xin thêm ChatGPT, không tự vẽ.
- Đọc hết `docs/ui-layout-rules.md` trước khi dựng.

## Risk Assessment
| Rủi ro | KxT | Giảm thiểu |
|---|---|---|
| Hết lượt vẽ | Cao x TB | chia đợt; tài khoản Tan Pham dự phòng |
| Chân dung lệch hình quái game | TB x Cao | gửi frame idle phóng to, so cạnh nhau trước khi nhận |
| Mảnh dính nhau, cắt sai | TB x Thấp | yêu cầu gap 24 px, dùng `box` / `erase` trong config |
| Tông Fantasy lệch Adventure của nhánh | TB x TB | người dùng đã chốt Fantasy (23/11) |

Rollback: chỉ là file ảnh; xoá / vẽ lại không ảnh hưởng code.

## Security Considerations
Không đưa thông tin tài khoản / dữ liệu riêng vào prompt. Không dùng tài khoản "xaochai", "Tieu Duy Phuong".

## Next Steps
Phase 03 cần A, B, C, D, G-toast. Phase 04 cần E. Phase 05 cần F. Phase 07 cần G. Phase 08 cần H.
