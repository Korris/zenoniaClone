# Phase 08 (tuỳ chọn): Quái đồng cỏ Lv3-6 theo khoảng cách cổng

## Context
- Quái hiện có cho Willowbrook toàn hệ băng: `MonsterDef.cs:31-33` frostSlime Lv1, iceGoblin Lv2, spectralWolf Lv2; quái kế tiếp đã nhảy lên Lv20+ (`MonsterDef.cs:39-59`) -> khoảng trống Lv3-19.
- Sprite quái: `Assets/Resources/Sprites/monsters/<id>.png` + `<id>-attack.png` (cùng khuôn với `bogSlime`, `duneScarab`).
- Đăng ký: `MonsterDef.All` (dòng 63) - **chỉ số trong mảng đi qua mạng** (`IndexOf/ByIndex`) -> chỉ thêm vào CUỐI mảng, tăng `NetProtocol.Version`, build lại server. Rơi đồ: `Rpg/LootTable.cs` (theo id).
- Đọc `docs/sprite-rules.md` trước khi làm sprite (ghi brief khi giao agent). Art: ChatGPT tài khoản **Tay**, kèm sprite quái có sẵn làm mẫu khung/kích thước và `docs/reference/hero-idle-down-5-classes.png` làm mẫu tỉ lệ.

## Overview
Priority P3. Status pending. Chỉ làm sau khi P7 qua với quái có sẵn.

## Đề xuất (người dùng chốt tên/số)
| Id gợi ý | Lv | Khu (cụm) | Kiểu AI |
|---|---|---|---|
| meadowSlime (slime xanh lá) | 1 | thay frostSlime cụm 1, 2, 5 | như frostSlime |
| fieldBoar (heo rừng) | 3 | cụm 3, 4, 6 | lao nhanh, aggro thấp |
| thornSprite (tinh linh gai) | 4 | cụm 7, 8 | bay, như amberWisp |
| banditScout (cướp) | 5 | cụm A | như iceGoblin |
| ruinGolem (golem rêu) | 6 | cụm 9, B | chậm, máu trâu |
Chỉ số: nội suy giữa spectralWolf (Lv2) và duneScarab (Lv21) theo đường cong XP hiện có.

## Steps
1. Người dùng chốt danh sách + cấp.
2. Xin art từng quái (đi + đánh, 4 hướng như quái hiện có), cắt bằng tool có sẵn (`Tools/slice-vfx-sheets.py` hoặc pipeline quái đang dùng - xác định khi làm), tự xem từng frame.
3. Thêm `MonsterDef` cuối `All`, loot, đổi bảng cụm P5 theo bảng trên.
4. Tăng protocol, build lại server, test (`ZenoniaSkills.Tests`, `ZenoniaNet.Tests`), chụp lại P7.

## Risk
Lệch style quái vs hero (cao): duyệt từng con bằng ảnh ghép trong game trước khi làm con tiếp. Đổi thứ tự `MonsterDef.All` = sai loại quái trên client cũ: chỉ thêm cuối.

## Câu hỏi còn mở
1. Danh sách quái và dải cấp cuối cùng (Lv1-6 hay cao hơn ở mép đông để nối map sau)?
2. Có boss nhỏ ở phế tích đông bắc không (hiện `BossFor` = null)?
