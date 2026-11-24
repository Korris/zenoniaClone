---
title: "Taurus V4: thế giới mới theo ChatGPT (Willowbrook Village, Clover Meadows, Ancient Root Shrine)"
description: "Thay toàn bộ thế giới cũ bằng Tinh Vực Taurus của TAURUS PRODUCTION BIBLE V4.0: làng + map săn 10 camp + dungeon + boss, quest tân thủ, card tutorial V4, chia P0-P6."
status: pending
priority: P1
effort: 78h
branch: ro-soft-adventure
tags: [taurus, world-redesign, map, monsters, quests, cards, boss, online]
created: 2026-11-24
---

# Taurus V4: thế giới mới

Nguồn (đọc trước mọi phase): `docs/reference/world-design/taurus-production-sheet-v3-part1/2/3-chatgpt.txt` (= BIBLE V4.0), `asteria-world-master-plan-v2-chatgpt.txt`, `docs/pending-tasks.md` mục 24/11. Thay plan `261124-0900-taurus-region-camps-monsters` (superseded).
**Brief agent bắt buộc ghi:** đọc hết `docs/map-design-rules.md` (map), `docs/sprite-rules.md` (art quái / NPC / VFX), `docs/ui-layout-rules.md` (UI). Art = ChatGPT (Tay, hết lượt Tan Pham), không vẽ bằng code.

## Quyết định chính (chi tiết ở phase 01)
- Người dùng 24/11: KHÔNG tái dùng map / quái / tên / art cũ; **save cũ bỏ hết, không migrate** (reset DB + `SaveVersion` 7).
- Code map / quái cũ **giữ trong repo, tắt khỏi luồng live** (`MapRegistry` live chỉ có map Taurus; map cũ = danh sách legacy chỉ bật bằng `--legacy-maps` cho test / capture). Xoá hẳn để sau khi Taurus xong P6 (đỡ vỡ ~hàng trăm test + capture).
- Id mới tiền tố `tau` (quái, card, map, quest) để không đụng id cũ (`hornRabbit` cũ vẫn còn trong code legacy). Chỉ APPEND cuối `MonsterDef.All` / card table.
- Map mới chạy 2.5D TRƯỚC; người dùng (24/11 12:15) đang làm map 3D giống Willowbrook 3D cũ, cho làm 2.5D trước rồi chuyển 3D sau (việc riêng). Đơn vị: 1 ô = 1 m = 16 đv.
- AI quái thường: chỉ melee + ranged có sẵn (BIBLE XII); thêm 1 cờ nhỏ "chỉ phản công" cho C01. Boss dùng hệ `SkillBoss` + telegraph Cone / Circle + enrage 30 % có sẵn (Ambercap Warden).
- Quest: dùng `QuestCatalog` có sẵn; quest nhiều mục tiêu của BIBLE tách thành chuỗi quest 1 mục tiêu (`Requires`). Thêm 2 loại mục tiêu: `Enhance` (Bram), `CardDuelWin`.
- Wire `NetProtocol.Version` 35 -> 36 (gộp nếu 35 chưa phát hành: vẫn bump để chặn client cũ).

## Phase
| # | P | Phase | Effort | Status | Phụ thuộc |
|---|---|---|---|---|---|
| 01 | P0a | [Tắt thế giới cũ, điểm xuất phát mới, reset save](phase-01-legacy-world-off-new-start-save-reset.md) | 6h | pending | - |
| 02 | P0b | [Greybox Willowbrook Village + Clover Meadows C01-C02, NPC, quest Rowan/Bram](phase-02-greybox-village-meadows-newbie-quests.md) | 12h | done (chưa commit) | 01 |
| 03 | P1 | [Card tutorial V4 (5 quái -> 5 lá, thắng Orin chọn 1 lá)](phase-03-card-tutorial-v4-orin.md) | 8h | done (chưa commit) | 02 |
| 04 | P2 | [Art lock (sau concept được duyệt)](phase-04-art-style-lock-from-approved-concepts.md) | 12h | pending | concept agent khác + 02 |
| 05 | P3 | [Clover Meadows đủ 7 khu, 10 camp, 9 quái, quest Lv3-9](phase-05-clover-meadows-full-10-camps-9-monsters.md) | 18h | code + test xong, greybox (chưa commit; prop / TTK đo thật / online 2 client chưa) | 04 |
| 06 | P4 | [Ancient Root Shrine + Elder Root Guardian](phase-06-ancient-root-shrine-dungeon-boss.md) | 12h | pending | 05 |
| 07 | P5 | [Playtest online + deploy](phase-07-online-playtest-deploy.md) | 5h | pending | 06 (deploy sớm sau 03 được) |
| 08 | P6 | [Content lock: Pet Lumi, World Map, minimap, cân bằng, dọn legacy](phase-08-content-lock-pets-worldmap-balance.md) | 5h | pending | 07 |

01 -> 02 -> 03 tuần tự (chung `QuestCatalog`, `NpcDefs`). 04 chạy song song 03 (khác file: art). 05 cần art quái từ 04 (code camp làm trước với `FallbackSheet` được).

## Kiểm tra mỗi phase
`dotnet test Tests/dotnet/ZenoniaSkills.Tests`, `dotnet test Tests/dotnet/ZenoniaNet.Tests`, `cd server-api && npx vitest run`. Build từ bản sao `Tools/build-from-copy.ps1` (không batch vào project đang mở). Chụp trong game tự xem ảnh trước khi báo xong.

## Deploy
server-api TRƯỚC, zone SAU, client cùng lúc (`docs/deploy.md`). Lần đầu: xoá collection nhân vật Mongo (người dùng đồng ý bỏ save). Chi tiết phase 07.

## Câu hỏi mở
Cuối `phase-01-legacy-world-off-new-start-save-reset.md`.
