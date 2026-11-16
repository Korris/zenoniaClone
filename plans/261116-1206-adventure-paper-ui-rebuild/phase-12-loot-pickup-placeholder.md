# Phase 12: Nhặt đồ (GIỮ CHỖ, chờ người dùng duyệt luật)

## Context Links
- Đề xuất ChatGPT + mockup: `docs/pending-tasks.md` mục 16/11 ("Cơ chế nhặt đồ"), `chatgpt/loot-rules-ui.png`
- Code: `LootDrop.cs` (rơi, nhặt tự động, 60 s, nhấp nháy 5 s cuối), `World/LootOwnership.cs` (top damage 3 s + party 2 s), `KillRewards.cs`, `Net/NetLootCodec.cs`, `Rpg/LootTable.cs`, `World/WantedBossCredit.cs`

## Overview
- Ưu tiên: P2. Effort: chưa ước được (S nếu chỉ UI nhãn; M-L nếu có personal loot Wanted). Status: **blocked** tới khi người dùng chốt.
- Không code gì trước khi có quyết định.

## Key Insights
- Đề xuất (chưa duyệt): Wanted = personal loot cho ai đóng góp >= 1 % máu hoặc top 10, còn ở map; boss map khoá chủ / party 8-10 s; quái thường khoá 5 s + party 3 s. UI: nhãn Yours / Party / Locked Ns / Free, đồ người khác mờ + khoá, nhấp nháy 5 s cuối, toast "Obtained", thông báo đồ hiếm toàn map (chỉ boss), Bag Full trên món và nút Bag, bảng thưởng Wanted tách "Daily Bounty" + "Your Loot" + Collect All, "Not enough contribution" + thanh đóng góp.
- Chưa rõ: tỉ lệ rơi personal loot (kinh tế), chống level cao cướp quái người mới.
- Nếu duyệt personal loot: đổi luật server (`KillRewards`, `LootOwnership`), có thể thêm trường wire (chủ / thời gian khoá trên loot, đóng góp %) -> protocol bump, test ZenoniaNet; có thể đổi save (món chờ Collect All) -> xét server-api.

## Requirements
- Chờ: (1) người dùng duyệt / sửa luật; (2) số liệu tỉ lệ rơi; (3) mockup trạng thái nếu thiếu.

## Architecture
- Sẽ viết khi có quyết định. Hướng mặc định (nếu duyệt nguyên đề xuất): luật thuần trong `World/LootOwnership.cs` (thời gian khoá theo loại quái) + `PersonalLoot` mới (danh sách đủ điều kiện từ `PartyKillShare.CollectParticipants` có sẵn); UI nhãn trên `LootDrop` (world-space), cột "Your Loot" trong bảng thưởng phase 11.

## Related Code Files
- Dự kiến sửa: `LootDrop.cs`, `World/LootOwnership.cs`, `KillRewards.cs`, `Net/NetLootCodec.cs`, `WantedBoardWindow` / bảng thưởng (phase 11), `HudBadges.cs` (Bag Full, phase 04)

## Implementation Steps
1. Người điều phối hỏi người dùng (tiếng Việt), ghi quyết định vào `docs/pending-tasks.md`.
2. Viết lại phase này (requirements, wire, test, rollback) rồi mới giao việc.

## Todo List
- [ ] Người dùng chốt luật nhặt đồ
- [ ] Viết lại phase chi tiết

## Success Criteria
- Có quyết định ghi trong git; phase được viết lại đủ mục.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Personal loot bơm kinh tế | Cao x Cao | Chốt tỉ lệ rơi trước khi code; test với `ZenoniaSwarm` nếu cần |

## Security Considerations
- Luật nhặt / khoá do server quyết; client chỉ hiện nhãn.

## Next Steps
- Mở khoá khi người dùng trả lời.

## Câu hỏi mở
- Duyệt nguyên đề xuất ChatGPT hay sửa? Tỉ lệ rơi personal loot bao nhiêu?
