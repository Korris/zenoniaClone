---
title: "Quest hướng dẫn Monster Card + đấu tập với Card Master Orin"
description: "Quest 'The First Card Duel': mượn deck 8 lá N, 1 ván 4x4 có 3 lượt kịch bản + AI dễ, thưởng 300 G + 3 lá Common 1 lần; sau đó đấu tập Orin không thưởng; xác nhận rủi ro trước PvP đầu tiên"
status: pending
priority: P2
effort: 15h
branch: ro-soft-adventure
tags: [card, duel, quest, tutorial, npc, ui]
created: 2026-11-24
---

# Quest hướng dẫn card + đấu tập Orin

Spec: `docs/pending-tasks.md` mục "24/11 03:15" (ChatGPT, người dùng "ok làm đi"). Nền: `plans/261123-1125-monster-card-gem-duel/` (V1 xong code, CHƯA deploy), luật `docs/reference/card-gem-ui/design-notes-chatgpt.md` "Luật đấu card v2" (sửa điều 1: mở lại NPC CHỈ cho ván hướng dẫn + đấu tập Orin).

## Quyết định kiến trúc (KISS, tái dùng)
- **Quest = hệ quest có sẵn** (`QuestCatalog` / `QuestLog` / `InvOp.QuestAccept/TurnIn` / `ElderNpc` marker). Thêm loại mục tiêu `CardDuel` (giống `WaypointId`) + `RewardCards`. Chống nhận lặp = trạng thái quest `Done` (đã có, server giữ). **Không thêm field save cho quest** (zod `quests.id` là chuỗi tự do).
- **Deck mượn = deck cố định 8 lá N của Orin**, không đụng `CardCollection` -> không có "thu hồi" thật, rớt mạng không thể mất / nhân bài. (Thay cho "mượn phần còn thiếu".)
- **Server-authoritative:** online chạy ở zone (`DuelService.Npc.cs`), offline chạy `LocalDuelHost` (phục hồi bản rút gọn từ `5647505d^`). Cả hai dùng chung `DuelNpcSession` thuần C#.
- Phục hồi + sửa từ git `5647505d^`: `DuelNpcAi` (1 mức: tham số lật nhiều nhất), `DuelNpcSession` (bỏ thưởng, thêm kịch bản + bỏ đồng hồ), `LocalDuelHost`. KHÔNG phục hồi `DuelRewards` / `DuelNpcTable` / `DuelNpcRun`.
- Ván NPC **không có đồng hồ lượt** (người mới đọc thoải mái); gợi ý tính ở client (thuần C#, cùng `DuelNpcAi`).
- Đấu tập (sau quest): deck đã lưu nếu hợp lệ, không thì deck mượn; không thưởng, không cược, không đổi quest.
- Xác nhận rủi ro PvP lần đầu: bit `cardHelp` 2 (`HelpPvpRisk`) -> zod `cardHelp` max 1 -> 3.
- Danh hiệu "Card Novice": BỎ (không có hệ danh hiệu).
- Thưởng: 300 G + `hornRabbit` (cạnh mạnh Phải 7), `thornBoar` (Dưới 7), `forestWolf` (Trên 7): 3 lá N vùng đầu, 3 hướng mạnh khác nhau.
- Wire `NetProtocol.Version` 34 -> **35**.

## Phase
| # | Phase | Effort | Phụ thuộc | Trạng thái |
|---|---|---|---|---|
| 01 | [Luật thuần C#: NPC session, kịch bản, AI, quest CardDuel](phase-01-pure-rules-npc-session-script-quest.md) | 4h | - | done (code+test) |
| 02 | [Wire v35 + server-api zod cardHelp](phase-02-wire-protocol-and-server-api.md) | 1.5h | 01 | done (code+test) |
| 03 | [Zone DuelService.Npc + offline LocalDuelHost](phase-03-zone-npc-duel-and-offline-host.md) | 3h | 01, 02 | done (code+test) |
| 04 | [UI: Orin, bàn hướng dẫn, gợi ý / bỏ qua, xác nhận PvP](phase-04-client-ui-orin-tutorial-board-pvp-confirm.md) | 5h | 02, 03 | done (code+shots, chưa thử online) |
| 05 | [Kiểm thử tích hợp, docs, deploy](phase-05-integration-test-docs-deploy.md) | 1.5h | 04 | pending |

01 -> 02 tuần tự (02 cần DuelView mới). 03 và 04 chia file rõ (bảng file ownership từng phase), 04 bắt đầu sau khi 03 chốt API `DuelClient`.

## Luồng người chơi
Lụm card đầu -> `CardTutorial` (3 bước cũ + bước 4 "gặp Orin") -> Orin "?" -> offer quest -> Accept -> nói tiếp: "Ready?" -> `DuelOp.PlayNpc(Tutorial)` -> 3 lượt kịch bản (đặt bài / chiếm bài / phòng thủ) -> AI dễ -> hết ván (thắng / hoà / thua / bỏ cuộc) -> quest `ReadyToTurnIn` -> banner "Return the deck to Orin" -> thoại ReadyLines (luật PvP thắng lấy 1 lá) -> TurnIn: +300 G + 3 lá -> Done. Sau đó Orin: thoại cũ + chọn "Practice Duel" / "Open Cards". Lần PvP đầu: hộp xác nhận liệt kê 8 lá deck.

## Thứ tự deploy
Gộp chung đợt deploy V1 card (chưa deploy): **server-api TRƯỚC** (zod cardHelp max 3), **zone SAU** (v35, quest mới). `docs/deploy.md`.

## Kiểm tra
`dotnet test Tests/dotnet/ZenoniaSkills.Tests`, `dotnet test Tests/dotnet/ZenoniaNet.Tests` (2 FrostHollowLargeMapTests fail sẵn có), `cd server-api && npx vitest run`. Build từ bản sao `Tools/build-from-copy.ps1`.

## Câu hỏi chưa giải quyết
1. Quest mở ngay từ đầu (mặc định) hay chỉ sau khi có card đầu tiên (`Cards.OwnedKinds > 0`)?
2. Bỏ cuộc giữa ván hướng dẫn có tính hoàn thành quest không? Mặc định: CÓ (= thua); rớt mạng / chết / đổi map: KHÔNG (chơi lại).
3. Đấu tập Orin có cho offline không? Mặc định: có (cùng LocalDuelHost).
