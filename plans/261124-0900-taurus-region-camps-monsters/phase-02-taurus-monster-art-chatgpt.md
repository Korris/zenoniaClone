# Phase 02: Art quái Taurus (ChatGPT vẽ)

## Context Links
- **Đọc hết `docs/sprite-rules.md` trước** (khối RO SOFT ADVENTURE đầu file, mục art ChatGPT điều 0-8, "Quy tắc thiết kế quái", Sổ lỗi quái 20/11). Ghi yêu cầu này trong brief agent.
- `docs/monster-redesign-bible.md` mục 1 (style, B1 Meadow, B2 Forest), mục 2 (01 Meadow Slime, 02 Horn Rabbit, 03 Thorn Boar, 06 Bramble Treant, 09 Thornheart Treant), mục 7-9 (ràng buộc, mẫu prompt 8.1-8.5, thứ tự)
- Production sheet phase 01

## Overview
P1. 8 quái: 5 vẽ THIẾT KẾ MỚI (giữ id) + 3 quái mới. Sheet đang cài của meadowSlime / hornRabbit / thornBoar / brambleTreant / thornheartTreant là bản TẠM chỉ đổi màu.

## Danh sách art
| # | id | Tên | Việc | Ảnh ChatGPT |
|---|---|---|---|---|
| 1 | meadowSlime | Clover Slime | thiết kế mới (bible 01 + motif cỏ ba lá) | đi + đánh |
| 2 | hornRabbit | Horn Rabbit | thiết kế mới (bible 02) | đi + đánh |
| 3 | waterSlime | Water Slime | MỚI (khác hẳn Clover Slime về hình, không chỉ đổi màu: bible mục III.2) | đi + đánh |
| 4 | reedFrog | Reed Frog | MỚI | đi + đánh |
| 5 | mossMushroom | Moss Mushroom | MỚI, tầm xa phun bào tử (đạn game vẽ) | đi + đánh |
| 6 | thornBoar | Bramble Boar (elite) | thiết kế mới (bible 03 + gai mâm xôi) | đi + đánh |
| 7 | brambleTreant | Root Guardian | thiết kế mới (bible 06) | đi + đánh |
| 8 | thornheartTreant | Elder Root Guardian (boss) | thiết kế mới (bible 09), ô 192/256 + RenderScale | đi + đánh |
Tổng: 2 concept lineup (thường 7 con; boss 1) + 16 sheet + ~15 % vẽ lại ≈ 21 ảnh. Card portrait + pet art ở phase 05.

## Chat ChatGPT
- Thiết kế chữ cho 3 quái mới: chat art director bible quái (Tay, chỉ chữ) https://chatgpt.com/c/6aff88da-2f60-83ec-9211-1d9de14ecaa8 ; thêm mục vào `docs/monster-redesign-bible.md` (cùng khung 9 mục). Hoặc để ChatGPT tự thiết kế từ production sheet, mình chỉ chuyển yêu cầu.
- Vẽ: chat vẽ quái Tay https://chatgpt.com/c/6aff410c-b5c4-83ec-8e5a-00ce76667f4a hoặc chat mới; tin đầu gửi 3 ảnh mẫu theo bible "Quy tắc cứng" 3 (+ khối tông theo câu trả lời câu hỏi 4 phase 01) + `docs/reference/hero-idle-down-5-classes.png`. Tay hết lượt -> Tan Pham (kiểm tên tài khoản, chat mới).
- KHÔNG gửi frame quái cũ làm mẫu; cỡ / chân ghi bằng số (`ro-soft/heights.json`; quái mới thêm dòng heights).

## Implementation Steps
1. Concept lineup (mẫu 8.1): ảnh 1 = 7 quái thường cạnh hero; ảnh 2 = Elder Root Guardian cạnh hero. Agent tự chốt (người dùng 20/11 "ko cần duyệt"), lưu `docs/reference/monster-redesign-concepts/taurus-concept-N.png`, ghi link vào `pending-tasks.md`.
2. Mỗi quái, thứ tự thấp cấp trước: sheet đi (8.2) rồi đánh (8.3), mắt theo 8.5, một dòng mỗi prompt. Tải bằng `fetch(blob)` + `<a download>` vào `Assets/ArtLibrary/monsters/ro-soft/<id>-{walk,attack}-chatgpt-N.png`.
3. `Tools/build-ro-soft-monsters.py build|check <id>` (quái mới thêm cấu hình: heights, `ATK_MATCH` nếu thân sheet đánh nhỏ; slime / ếch frame 4 bẹp thì KHÔNG bật).
4. Xem TỪNG frame trên ảnh trước/sau + nền tối / sáng: hướng, hàng lên không thấy mắt, cỡ đánh = cỡ đi, con ngươi đen, không caro, không bóng vẽ sẵn. Chụp trong game `--dummy-types <id>` (build từ bản sao).
5. Lỗi mới -> Sổ lỗi `sprite-rules.md` ngay. Commit từng quái.

## Todo
- [ ] Thiết kế chữ 3 quái mới
- [ ] 2 concept, chốt
- [ ] 8 x (đi, đánh), build, check, xem từng frame
- [ ] Chụp trong game, ghi pending-tasks

## Success Criteria
8 sheet cài ở `Resources/Sprites/monsters/<id>{,-attack}.png`, ảnh trước/sau `docs/reference/monsters-ro-soft-redraw/`, đã tự xem từng frame và trong game; báo rõ phần chưa xem online.

## Risk
- Hết lượt giữa chừng: ghi quái dở + chat vào `pending-tasks.md`, dừng.
- Thiết kế mới đổi hình -> card portrait + pet art lệch (phase 05 vẽ lại).
- Pet dùng chung sheet quái (`HeroPet.Sheets.cs`): pet Horn Rabbit / Thorn Boar tự đổi, kiểm.
- Boss ô 192 phóng 2.0-2.4x: nét viền dày hơn (bible mục 7).
