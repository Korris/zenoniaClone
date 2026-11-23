# Phase 08: NPC Card Master, lối vào, hoàn thiện, docs

## Context Links
- Spec: `design-notes-chatgpt.md` mục "Chỗ mở đấu card"
- NPC: `Assets/Scripts/NpcDefs.cs`, `UiScreens.cs` (`BindNpc`), `Maps/OakhavenVillageMap.cs` (`Npcs`), `Maps/WillowbrookMeadowsMap.cs`
- **Đọc hết `docs/map-design-rules.md` trước khi đặt NPC trên map** (đường / cầu / bãi quái phải trống, rà ảnh tô ô chặn).
- **Đọc hết `docs/sprite-rules.md`** trước khi cài sprite NPC (asset H phase 02).
- **Đọc hết `docs/ui-layout-rules.md`** cho hộp thoại NPC / hướng dẫn.
- Deploy: `docs/deploy.md`; việc dở: `docs/pending-tasks.md`

## Overview
P2, **xong code 23/11 chiều** (chưa deploy, chưa kiểm online). Card Master: hướng dẫn luật, mở trang Card Duel (đấu NPC), nhận thưởng album / đấu; menu tính năng "Cards" ở mọi map an toàn (đã có từ phase 03); dọn điểm yếu mockup; cập nhật docs.

## Key Insights
- Willowbrook Meadows là map săn (`Hostile`), luật cấm đấu ở map có quái -> mặc định đặt Card Master ở Oakhaven, cạnh cổng nam sang Willowbrook (câu hỏi 1 plan.md). Đổi chỗ chỉ là sửa toạ độ + map.
- `UiScreens.BindNpc(npcId, UiScreen.Card)` là đủ để NPC mở cửa sổ; thêm trang mở sẵn = Card Duel.
- Không thêm nút thường trực trên HUD (đã chốt).

## Requirements
- NPC `cardMaster` (tên đề xuất "Card Master Orin"), idle HD + mặt từ phase 02 H, 2-3 câu thoại; nói chuyện -> hộp chọn: Luật đấu (trang hướng dẫn ngắn, ảnh chụp bàn đấu thật) / Đấu NPC / Album.
- Hướng dẫn lần đầu: lần đầu mở trang Duel hiện 4 bước luật (lật, thắng 9+, 20 s, deck 1 boss + 2 SR+).
- Chặn mở trận khi: đang đánh, ở Wanted Boss, trong dungeon (Rootburrow), map có quái -> thông báo rõ lý do.
- Polish: đồng bộ màu menu trái (mockup lệch xanh / vàng: dùng 1 bộ asset), số hướng lấy từ bảng, NPC đấu dùng chân dung F.
- Docs: `docs/pending-tasks.md` (trạng thái), mục card trong `README.md` (ngắn), `docs/deploy.md` (công tắc `CARD_WAGER_ENABLED`, rebuild zone + API), Sổ lỗi sprite / UI nếu có lỗi mới.
- Constraints UI: chỉ asset phase 02.

## Architecture
`NpcDefs` thêm def -> `OakhavenVillageMap.NpcList` thêm `MapNpc` ở ô trống ven đường -> `UiScreens.BindNpc("cardMaster", UiScreen.Card)` (+ tham số trang mở đầu) -> `CardWindow.Open(page: Duel)`. Kiểm điều kiện ở server (`DuelService`) như phase 05, client chỉ hiện lý do.

## Related Code Files
Sửa: `NpcDefs.cs`, `Maps/OakhavenVillageMap.cs`, `UiScreens.cs` (mở trang), `CardWindow.cs` (trang đầu + hướng dẫn), `Net/Server/DuelService.cs` (lý do từ chối chi tiết), `README.md`, `docs/deploy.md`, `docs/pending-tasks.md`.
Tạo: `Assets/Scripts/CardDuelTutorial.cs`; sprite NPC (từ phase 02) `Assets/Resources/Sprites/village/npcs/cardMaster*.png`.
Test: thêm vào `Tests/dotnet/ZenoniaNet.Tests` một test ô NPC không chặn đường (mẫu các `*MapTests.cs`).

## Implementation Steps
1. Đọc 3 file luật ở trên.
2. Chọn ô đặt NPC theo map-design-rules, kiểm ảnh tô ô chặn, viết test ô.
3. Cài sprite NPC, tự xem ảnh ghép trong game (CLAUDE.md: không tin báo cáo agent).
4. BindNpc + trang mở đầu + hướng dẫn lần đầu.
5. Lý do từ chối chi tiết; polish màu menu.
6. Build zone (bản sao), thử đường đi tới NPC + đấu NPC từ NPC; cập nhật docs.

## Làm khác kế hoạch (23/11 chiều, luật v2)
- KHÔNG đấu NPC: nói chuyện = 5 trang thoại (card rơi từ quái, My Cards + Album, Soul Gem, luật Card Duel "winner takes 1 card from the loser's deck" + Lv30 + online, Ranked không lấy card), trang cuối mời mở cửa sổ Card (hết trang thì `CardWindow.Open`, Esc thì thôi). Không có hộp 3 lựa chọn Luật / Đấu NPC / Album (không còn gì để chọn).
- Ô NPC (16,21): cỏ mép phải đường cổng nam, 2 ô cạnh cổng; đường giữ 4 ô trống; test `OakhavenCardMasterTests`.
- Hướng dẫn lần đầu = 3 bước (`CardTutorial`), không phải 4 bước luật ván: mở lần đầu cửa sổ Card hoặc card đầu tiên rơi. Cờ `cardHelp` bit 0 (SaveData + zod + `InvOp.CardHelp` 36, Version 34).
- Lý do từ chối mở trận (đang đánh, Wanted, dungeon, map có quái) đã do `DuelService` phase 06 lo; không làm thêm.
- Ẩn MMR khỏi RpgState: bỏ khỏi phase này (agent Ranked làm). Poison icon: kit không có, KHÔNG vẽ: cần ChatGPT vẽ `element-poison` (xem `GemUiArt.ElementIcon`), hiện thẻ Poison không có icon nguyên tố.
- Offline SaveNow sau mọi op card / gem (`InvOpInfo.IsCardOp`, `PetActions.Send`). Toast card dùng khung `toast-card` (`NotificationToast.ShowCard`).
- Nhịp thở idle Card Master: bỏ ở V1.

## Todo List
- [x] Đọc luật map / sprite / UI   - [x] Ô NPC + test   - [x] Sprite NPC đã xem trong game
- [x] BindNpc + hướng dẫn   - [x] Lý do từ chối (có từ phase 06)   - [x] Docs + pending-tasks

## Success Criteria
- Đi tới Card Master, nói chuyện -> mở thẳng trang Duel; đấu NPC trọn ván.
- NPC không chặn đường / cổng (test ô xanh, ảnh tô ô chặn đã xem).
- Ở Willowbrook (map săn) bấm Play NPC -> thông báo "Chỉ đấu ở khu an toàn".
- Docs cập nhật, có cách tắt cược.

## Risk Assessment
| Rủi ro | KxT | Giảm thiểu |
|---|---|---|
| NPC chặn đường / sai chỗ người dùng muốn | TB x Thấp | test ô + hỏi câu 1 trước khi làm |
| Sprite NPC lệch cỡ NPC làng | TB x Thấp | so cạnh NPC khác cùng cỡ |

Rollback: bỏ dòng NPC khỏi map; lối vào qua menu vẫn còn.

## Security Considerations
Điều kiện mở trận kiểm ở server; NPC chỉ là lối mở UI.

## Next Steps
Sau V1: chợ thật (nếu duyệt), cosmetic danh hiệu / khung / hiệu ứng, bảng xếp hạng Ranked, giới hạn đăng bán cho tài khoản mới.
- Việc sau (người dùng chốt 23/11 bỏ ở V1): nhịp thở idle cho NPC `cardMaster`. Sheet hiện có `Sprites/village/npcs/cardMaster.png` 4 frame mỗi hàng giống hệt nhau (ChatGPT vẽ 1 dáng / hướng, phase 02 H); muốn thở thì xin ChatGPT thêm frame hoặc co giãn nhẹ trên art có sẵn.
