# Phase 01: Chốt "Taurus Monster & Map Production Sheet" với ChatGPT

## Context Links
- `docs/reference/world-design/asteria-world-monster-bible-chatgpt.txt` (Taurus, mục III nguyên tắc quái / Pet / Card, mục VI Giai đoạn 1)
- `docs/reference/world-design/asteria-one-big-map-camps-chatgpt.txt` (camp theo địa hình, SpawnZone, leash)
- `docs/monster-redesign-bible.md` mục 2 (Meadow 01-03, Forest 06, boss 09), mục 7 (ràng buộc sheet)
- `docs/pending-tasks.md` mục 24/11 (quest Orin ĐANG BÀN, chat https://chatgpt.com/c/6b03f69b-a4f8-83ec-af99-547d92463f83)

## Overview
Priority P1 (chặn mọi phase sau). Chỉ chữ, không vẽ. Gửi ChatGPT hiện trạng THẬT của game, xin bảng sản xuất chốt, lưu nguyên văn vào `docs/reference/world-design/taurus-production-sheet-chatgpt.md`.

## Hiện trạng thật (dán vào brief)
| Thành phần | Game hiện tại |
|---|---|
| Làng | Oakhaven Village 36x24 (`OakhavenVillageMap`): Elder Thomas (quest `first-hunt`), Card Master Orin (16,21), cổng nam (13-14,21) -> Willowbrook, cổng đông -> Thatchwick -> Frost Hollow |
| Map săn 1 | Willowbrook Meadows 72x48, 2.5D (phát hành) + 3D (bản test). 11 cụm, 70 quái Lv1-5: meadowSlime Lv1 (2 cụm gần cổng tây), hornRabbit Lv3 (4 cụm), thornBoar Lv5 (6 cụm). Suối chéo từ (22,2) xuống hồ (31-46, 39-45), cầu hàng 23 (23-27), di tích góc đông bắc, rào cuối đường đông. Cổng nam (46-47,44) -> Bramblewood |
| Map săn 2 | Bramblewood Forest 36x24 Lv6-10: meadowMantis 6, forestWolf 7, brambleTreant 8, thornSprite 9, boss map Thornfang Alpha Lv10 (24,18); cửa hang (2,13) -> Rootburrow |
| Dungeon | Rootburrow 1F-3F (72x48 mỗi tầng) Lv6-14, rương + pha lê, quest Elara; boss Ambercap Warden Lv14 (3F); Thornheart Chamber (từ 3F) boss Thornheart Treant Lv10 |
| Monster AI | cận chiến (aggro, leash 150 đv từ điểm sinh, windup / cooldown); tầm xa qua `MonsterRangedProfile` (đạn). CHƯA có: lao (charge), vòng cảnh báo cho quái thường (chỉ boss skill có telegraph), Elite |
| Card | 40 lá (`CardTableData.cs`), Willowbrook 3 lá N; luật 4x4 so 4 cạnh |
| Pet | gacha theo tài khoản (`PetConfig`): hornRabbit N, thornBoar R; không có bắt pet ngoài đồng |
| Quest tân thủ | `first-hunt` Elder (AutoStart, 5 quái bất kỳ, 30 G + mũ khởi đầu); `first-card-duel` Orin (cần có 1 lá, ván hướng dẫn deck mượn, 300 G + 3 lá) |
| Art quái | sheet meadow / forest hiện là bản TẠM (chỉ đổi màu, người dùng chê); bible thiết kế mới có sẵn (chưa vẽ) |

## Brief gửi ChatGPT (nội dung bắt buộc)
1. Hiện trạng trên + ảnh: minimap Willowbrook có ô tô cụm quái, ảnh Bramblewood, Oakhaven. Mô hình chốt: 1 khu dân cư + map săn chia camp + 1 dungeon, Taurus được dùng 2 map ngoài trời (Willowbrook + Bramblewood) như "Giai đoạn 1" của bible.
2. **Làng đầu có chuỗi quest tân thủ**: (a) quest đánh quái camp đầu, thưởng ĐÁ NÂNG CẤP (upgrade stone) để tự ép đồ, KHÔNG thưởng vũ khí / off-hand (nhân vật mới đã có đồ khởi đầu); (b) quest đánh bài với Card Master Orin: 5 quái -> 5 lá -> đấu Orin -> thắng chọn 1 lá của Orin. Xin ChatGPT số đá, quái nào, số lá, rarity.
3. Đề xuất KISS của mình (bảng đối chiếu trong `plan.md`): giữ id + tên map, đổi tên hiển thị 4 quái, 3 quái mới. ChatGPT được quyền sửa; không ép nội dung thiết kế (memory: ChatGPT tự quyết thiết kế).
4. Xin bảng: mỗi quái (tên, id đề xuất, camp, Lv, cỡ so hero 69 px / cao thân px ô 192, AI: cận / tầm xa / lao, đòn, tốc độ, Card rarity + 4 cạnh "cân bằng, dễ dùng", Pet có / không, số lượng + hồi sinh mỗi camp, leash), camp (vị trí trên ảnh, địa hình, prop cần), Elite (nếu không làm cơ chế mới thì dùng gì), boss story.
5. Ràng buộc kỹ thuật phải ghi: sheet 4x4 ô 192 đi / 256 đánh, trái = lật phải (không chi tiết bất đối xứng), không bóng vẽ sẵn, nền #FF00FF; quái bay thì HOVER; Card không thêm luật mới.

## Implementation Steps
1. Chuẩn bị ảnh: `Tools/preview-willowbrook-large-composite.py --minimap` + tô rect `MapSpawnTable.WillowbrookMeadows`; ảnh Bramblewood tương tự.
2. Mở chat ChatGPT Tay ra World Bible Asteria (link chưa ghi trong `pending-tasks.md`: tìm trong Recents, ghi lại link), gửi brief một dòng mỗi tin.
3. Lưu nguyên văn trả lời, so với code: số liệu không khớp engine (vd ô lớn hơn, luật card mới) thì hỏi lại 1 vòng.
4. Ghi quyết định vào `docs/pending-tasks.md` (mục mới 24/11 Taurus) + cập nhật bảng ở `plan.md` nếu đổi.

## Todo
- [ ] Ảnh minimap + rect cụm
- [ ] Brief (có quest stone + quest card)
- [ ] Nhận + lưu Production Sheet
- [ ] Đối chiếu engine, chốt, ghi pending-tasks

## Success Criteria
File production sheet có đủ 8-9 quái + 6 camp + quest tân thủ, mọi số map được vào `MonsterDef` / `Zone` / `CardTableData` không cần cơ chế ngoài danh sách đã duyệt.

## Risk
- ChatGPT đề xuất đổi tên map / làng -> save id, quest text, Elara chain lệch. Giữ id, chỉ đổi tên hiển thị nếu người dùng muốn.
- ChatGPT đòi charge / vòng cảnh báo / Elite: mỗi cái là tính năng AI mới cả client + server; tách thành việc riêng, hỏi người dùng.

## Câu hỏi mở cho người dùng
1. Tên làng: giữ "Oakhaven" (đề xuất, đã có NPC / quest / save) hay đổi "Willowbrook Village" như ChatGPT? Map săn giữ "Willowbrook Meadows" hay đổi "Clover Meadows"?
2. Rootburrow đổi tên hiển thị "Ancient Root Shrine"? Boss story Elder Root Guardian = Thornheart Treant (Lv10) nằm sau tầng 3F Lv12-13: chấp nhận hay dời cửa Thornheart lên 1F?
3. Bramble Boar elite cần đòn LAO theo đường thẳng (AI mới) hay v1 chỉ là quái to, máu cao, hồi 90 s?
4. Tông vẽ quái trên nhánh này: khối "RO Soft Adventure" (đầu `sprite-rules.md`) hay "RO Soft Fantasy" (bible quái điều 2)? Hai file đang mâu thuẫn.
5. Thêm Pet Clover Slime vào gacha (art + PetConfig) hay để sau?
6. Quest Elder: thêm đá và GIỮ mũ khởi đầu (đề xuất) hay bỏ mũ?
