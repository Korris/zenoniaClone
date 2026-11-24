# Phase 08 (P6): Content lock: Pet Lumi, World Map, minimap, cân bằng, dọn legacy

## Context Links
- BIBLE part3 XI (Pet Taurus: Horn Rabbit N, Bramble Boar R, Gacha; Lumi Lv5, lượt hướng dẫn cố định ra Horn Rabbit), XVII P6, XVIII toàn bộ (44 mục).
- Code: `Rpg/PetConfig.cs` (thứ tự = wire id, khớp `PET_IDS` server-api `rpg-constants.js`), `World/PetCaptureService.cs` (bắt pet theo `MonsterType`: BIBLE không cho bắt ngoài đồng), `WorldMapWindow.cs` + `WorldMapData.cs` (chỉ map hiện tại, camp từ `MapSpawnTable`), `Minimap.cs:152` (`<GroundTexture>-minimap`), `MapNameBanner.cs`, `RoHudMapPanel.cs`, `GameMusic.cs`.
- **UI: đọc hết `docs/ui-layout-rules.md`.**

## Overview
P3. Hoàn thiện để chuyển Gemini. Phần lớn là dữ liệu + kiểm, ít code mới.

## Requirements
- Pet: APPEND `tauHornRabbit` (N), `tauBrambleBoar` (R) vào `PetConfig` + `PET_IDS`; art pet đồng hành (từ sheet quái, chỉnh vai trò đi theo); pool Gacha live = theo câu trả lời phase 01 Q3; tắt bắt pet cho quái Taurus (`PetOf` không khớp vì không pet nào có `MonsterType` capture -> thêm cờ `Capturable=false` nếu cần).
- Quest Lumi `tau-lumi-companion` (Lv ≥ 5, không chặn chuỗi chính): gặp Lumi -> 1 lượt triệu hồi hướng dẫn miễn phí, kết quả cố định Horn Rabbit N, ghi rõ trên UI, không tính pity -> đưa vào Combat Slot. Cần mục tiêu quest mới `PetSlot` hoặc hoàn thành ngay khi nhận pet (chọn cách đơn giản hơn khi code).
- World Map: `WorldMapData` đọc camp đúng (tên camp / level từ `MonsterDef`), cổng Gemini hiện "chưa mở"; minimap 3 map có `-minimap` đã bake prop.
- Nhạc 3 map (`GameMusic`), banner tên map, thoại NPC soát lại (English trong game, BIBLE tiếng Việt chỉ là spec).
- Cân bằng Lv1-10: EXP từng loài để Lv10 khi xong T-M05 (đo bằng 1 lượt chơi), kinh tế Gold / đá.
- Dọn legacy (hỏi người dùng): xoá map / quái / quest / card cũ + test + capture cũ, hoặc giữ tới khi các Tinh Vực sau thay thế. Mặc định: GIỮ (index `MonsterDef.All` / card không được xoá giữa danh sách dù xoá code: chỉ thay bằng def rỗng).
- Docs: README (Maps, Monster Cards), `docs/map-design-rules.md` (mẫu Taurus 96x64), `docs/pending-tasks.md`, `docs/codebase-summary.md` nếu có.

## Related Code Files
- Sửa: `Rpg/PetConfig.cs`, `server-api/src/rpg/rpg-constants.js`, `World/PetCaptureService.cs`, gacha (`PetGacha*`), `Rpg/TaurusQuests.cs`, `WorldMapData.cs`, `GameMusic.cs`, `NpcDefs.cs`, `Rpg/LootTable.cs`, README + docs.
- Tạo: art pet `Resources/Sprites/pets/tau*` (đường dẫn theo pet hiện có), test `TaurusPetTests.cs`.

## Implementation Steps
1. Pet append + PET_IDS + art + gacha pool + quest Lumi.
2. World Map / minimap / nhạc / banner.
3. Một lượt chơi Lv1 -> 10 (ghi thời gian từng chặng), chỉnh EXP / Gold.
4. Đi hết checklist 44 mục part3 XVIII, ghi kết quả vào `docs/reference/taurus-v4-art/acceptance.md`.
5. Docs + quyết định dọn legacy.

## Todo List
- [ ] Pet Horn Rabbit / Bramble Boar + Lumi quest
- [ ] World Map, minimap, nhạc
- [ ] Cân bằng Lv1-10
- [ ] Checklist 44 mục
- [ ] Docs, dọn legacy (theo người dùng)

## Success Criteria
Checklist 44 mục part3 XVIII đạt hoặc có lý do ghi rõ; người dùng duyệt chơi thử Lv1-10.

## Risk Assessment
- Thứ tự `PetConfig` lệch `PET_IDS` -> pet sai trên server: test so 2 danh sách (nếu chưa có thì thêm).
- Lượt gacha cố định bị hiểu là lừa tỉ lệ -> UI ghi rõ "Guided summon: Horn Rabbit".

## Security Considerations
Lượt hướng dẫn chỉ 1 lần / nhân vật (quest Done), zone quyết kết quả.

## Next Steps
Gemini — Mirrorwood (plan riêng, cùng khuôn BIBLE). Wanted Boss Verdant Horn (sau, độc lập).
