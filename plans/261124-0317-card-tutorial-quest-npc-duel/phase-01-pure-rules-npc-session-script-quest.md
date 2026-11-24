# Phase 01: Luật thuần C# (NPC session, kịch bản, AI, quest CardDuel)

## Context Links
- Spec: `docs/pending-tasks.md` mục "24/11 03:15"; plan tổng `plan.md`.
- Code cũ để phục hồi: `git show 5647505d^:Assets/Scripts/Rpg/Cards/DuelNpcAi.cs`, `...DuelNpcSession.cs`, test `git show 5647505d^:Tests/dotnet/ZenoniaSkills.Tests/DuelNpcAiTests.cs`.
- Hiện có: `Rpg/Cards/DuelMatch.cs`, `DuelBoard.cs`, `DuelView.cs`, `DeckRules.cs`, `CardCollection.cs`, `CardOps.cs` (`MarkHelp`), `Rpg/QuestLog.cs` (QuestDef/QuestCatalog/QuestLog), `Rpg/PlayerState.Quests.cs`, `World/PlayerRpg.cs`.

## Overview
Priority P2. Done (code + tests, 24/11). Toàn bộ luật nằm ở `Assets/Scripts/Rpg/` (zone, offline, `ZenoniaSkills.Tests` dùng chung). Không Unity, không mạng.

## Key Insights
- Game ĐÃ có quest system (First Hunt, Rootburrow chain dùng `WaypointId` = mục tiêu không phải giết quái). Thêm mục tiêu `CardDuel` cùng kiểu.
- `DuelMatch.Create` xáo deck theo seed + chọn người đi trước ngẫu nhiên -> kịch bản cần bản không xáo, người chơi đi trước.
- 8 lá N trong bảng: meadowSlime, hornRabbit, thornBoar, meadowMantis, forestWolf, brambleTreant, thornSprite, caveMantis -> đủ 1 deck mượn.
- `TurnInQuest` chỉ trả Gold / đá / bình / mũ: cần thêm trả card (`Cards.Add`, cap `MaxStack`, tự đánh dấu Album).
- `CardOps.MarkHelp` từ chối bit ngoài `HelpMask` -> nới mask là đủ cho bit PvP risk.

## Requirements
- Chức năng: ván Tutorial (deck cố định, không xáo, người chơi đi trước, 3 lượt kịch bản), ván Practice (deck người chơi nếu `DeckRules.Validate` ok, không thì deck mượn; xáo bình thường), AI dễ, gợi ý 1 nước, bỏ qua kịch bản, không đồng hồ lượt; quest `first-card-duel` hoàn thành khi ván Tutorial kết thúc hợp lệ; trả 300 G + 3 lá đúng 1 lần.
- Phi chức năng: deterministic theo seed; file < 200 dòng; không phát sinh field save.

## Architecture
- `DuelMatch`: thêm tham số tạo `CreateFixed(deck0, deck1, firstSide, nowMs)` (không xáo) và cờ `turnMs` (<= 0 = không đồng hồ: `DeadlineMs = long.MaxValue`, `Tick` không bao giờ tự đặt). `Create` cũ giữ nguyên chữ ký (PvP / Ranked không đổi).
- `DuelNpcAi` (phục hồi, gọn): `Best(DuelBoard b, IList<int> hand, int side, DuelRng rng, bool careful, out hand, out cell)`. NPC: `careful=false` (nhiều lật nhất, hoà thì ngẫu nhiên). Gợi ý: `careful=true` (lật nhiều nhất, hoà thì ít cạnh yếu hướng ô trống nhất). Nhận board + hand thay vì DuelMatch để client gọi được từ `DuelView`.
- `DuelTutorialScript` (mới, dữ liệu): tên "Card Master Orin", deck người chơi / Orin theo thứ tự cố định, 5 bước (3 của người chơi, 2 của Orin), mỗi bước: side, cardId, cell, `LessonTitle`, `LessonBody` (English, UI dùng). Bản nháp (test phải xác nhận mask lật):
  - Deck người chơi: thornBoar, hornRabbit, brambleTreant, meadowMantis, forestWolf, meadowSlime, thornSprite, caveMantis. Deck Orin: meadowSlime, forestWolf, caveMantis, thornSprite, hornRabbit, thornBoar, meadowMantis, brambleTreant.
  - L1 (bước 1 "Place a card"): Thorn Boar -> A1 (ô 0), góc che 2 cạnh.
  - L2 Orin: Meadow Slime -> B2 (ô 5), không lật.
  - L3 (bước 2 "Capture"): Horn Rabbit -> B1 (ô 4): Phải 7 > Trái slime 4 -> lật B2.
  - L4 Orin: Forest Wolf -> C1 (ô 8): Trên 7 > Dưới rabbit 2 -> lật B1 (giải thích "cạnh yếu hướng ô trống bị chiếm").
  - L5 (bước 3 "Defend"): Meadow Mantis -> A2 (ô 1): trên = mép, trái = boar mình, dưới = slime mình, chỉ cạnh Phải 8 hướng ô trống.
  - Từ lượt 6: tự do, Orin dùng AI.
- `DuelNpcSession` (phục hồi + sửa): `Mode` (Tutorial / Practice), `ScriptStep` (index bước kịch bản kế tiếp của người chơi, 0 = hết / bỏ qua). `PlayerPlace(handIndex, cell, expectTurn, expectCard)`: khi đang kịch bản chỉ nhận đúng (card, cell) của bước, sai -> `DuelPlayError.Script` (mới). `SkipScript()`. `Tick(now)`: tới lượt Orin sau 0,6-1,2 s: nếu bước kịch bản là của Orin thì đặt đúng lá / ô, không thì AI. `Forfeit()`. `TakeResult()` 1 lần. `View(now)` -> `DuelView` có `Npc = true`, `ScriptStep`.
- Quest: `QuestDef.CardDuel` (bool) + `QuestDef.RewardCards` (string[]); `RewardText` liệt kê tên lá; `Objective` = "Play a card duel with Orin". `QuestLog.CountCardDuel()` (như `CountReach`). `PlayerState.TurnInQuest` thêm vòng `Cards.Add(id)` cho `RewardCards`. `PlayerRpg.QuestCardDuel()` gọi + bắn `QuestChanged`. Quest mới trong `QuestCatalog.All` (THÊM CUỐI, id `first-card-duel`, giver `cardMaster`, GiverName "Card Master Orin", Count 1, RewardGold 300, RewardCards 3 lá, OfferLines / ActiveLines / ReadyLines (luật PvP: winner takes 1 card of the loser's deck, Lv30, draw nobody loses) / DoneLines).
- `CardCollection`: `HelpPvpRisk = 2`, `HelpMask = 3`.

## Related Code Files
- Sửa: `Assets/Scripts/Rpg/Cards/DuelMatch.cs`, `DuelView.cs` (thêm `Npc`, `ScriptStep`), `CardCollection.cs`, `Assets/Scripts/Rpg/QuestLog.cs` (QuestDef + catalog + CountCardDuel; nếu > 200 dòng tách quest mới ra `Rpg/CardDuelQuest.cs` như `RootburrowQuests`), `Rpg/PlayerState.Quests.cs`, `World/PlayerRpg.cs`.
- Tạo: `Assets/Scripts/Rpg/Cards/DuelNpcAi.cs`, `DuelNpcSession.cs`, `DuelTutorialScript.cs`, `Assets/Scripts/Rpg/CardDuelQuest.cs`; test `Tests/dotnet/ZenoniaSkills.Tests/DuelNpcAiTests.cs`, `DuelNpcSessionTests.cs`, `DuelTutorialScriptTests.cs`, `CardDuelQuestTests.cs`.
- Xoá: không.
- Ownership phase này: `Assets/Scripts/Rpg/**`, `World/PlayerRpg.cs`, `Tests/dotnet/ZenoniaSkills.Tests/**`.

## Implementation Steps
1. `git show 5647505d^:...` lấy `DuelNpcAi.cs`, `DuelNpcSession.cs`, `DuelNpcAiTests.cs` làm điểm khởi đầu; bỏ `DuelNpcLevel`, `DuelNpcDef`, `DuelReward`.
2. `DuelMatch`: `CreateFixed` + turnMs (không đồng hồ). Giữ `Create` cũ.
3. `DuelNpcAi.Best` trên board + hand.
4. `DuelTutorialScript` + test chạy hết 5 bước bằng `DuelMatch` thật, assert mask lật từng bước như trên (sửa ô nếu test lệch).
5. `DuelNpcSession` Tutorial / Practice, `DuelPlayError.Script`, Skip, Tick (kịch bản rồi AI).
6. `DuelView.Npc`, `ScriptStep`; `DuelView.From` không đổi, session tự gán.
7. Quest: QuestDef fields, `CountCardDuel`, `TurnInQuest` trả card, `PlayerRpg.QuestCardDuel`, quest `first-card-duel` (thêm CUỐI mảng).
8. `HelpPvpRisk`, `HelpMask = 3`.
9. `dotnet test Tests/dotnet/ZenoniaSkills.Tests`.

## Todo List
- [x] DuelMatch CreateFixed + no-clock
- [x] DuelNpcAi (board/hand, careful flag)
- [x] DuelTutorialScript + test mask từng bước
- [x] DuelNpcSession (Tutorial/Practice, Script error, Skip, Tick)
- [x] DuelView Npc / ScriptStep
- [x] QuestDef CardDuel + RewardCards, CountCardDuel, TurnInQuest cards, quest first-card-duel
- [x] HelpPvpRisk / HelpMask
- [x] Test: AI chọn lật nhiều nhất, không nước khi đầy; session từ chối nước lệch kịch bản, Skip -> AI, không đồng hồ (Tick sau 10 phút không tự đặt cho người chơi), Orin đi sau 0,6-1,2 s; Practice deck sai -> deck mượn; quest Accept -> CountCardDuel -> TurnIn +300 G +3 lá (Album đánh dấu) -> TurnIn lần 2 bị từ chối; Sanitize giữ entry; MarkHelp(2) ok, MarkHelp(4) từ chối; deck mượn / deck Orin chỉ lá N có trong CardTable.

## Success Criteria
ZenoniaSkills pass toàn bộ (698 cũ + test mới). PvP / Ranked test cũ không đổi.

## Risk Assessment
- Kịch bản ô lệch luật lật -> test step 4 bắt, sửa dữ liệu.
- Thêm quest giữa mảng làm lệch thứ tự hiển thị: thêm cuối.
- `QuestLog.cs` vượt 200 dòng: tách quest data sang `CardDuelQuest.cs`.

## Security Considerations
Không tin client: kịch bản / kết quả / thưởng chỉ do session + `TurnInQuest` (phía server) quyết. Thưởng chỉ khi `Close` trả true (1 lần).

## Next Steps
Phase 02 (wire mang `Npc` / `ScriptStep`).

## Làm khác kế hoạch (24/11)
- Quest chỉ mở SAU khi có card đầu (người dùng chốt): `QuestDef.NeedsCard` + `QuestLog.HasCards` (gán trong ctor PlayerState = `Cards.AlbumCount > 0`, Album nhớ vĩnh viễn nên bán card đầu vẫn trả quest được). Quest đã nhận thì không bị khoá lại.
- `DuelNpcSession` tách Tutorial / Practice bằng factory (`Tutorial(now)`, `Practice(seed, deck, now)`), `PracticeDeck(CardCollection)` chọn deck lưu hợp lệ hoặc deck mượn. Thêm `PlayerGaveUp`, `CurrentStep`. `ScriptStep` trong view = số bài học 1..3 (0 = tự do), `DuelTutorialScript.LessonAt`.
- `DuelMatch.Create(..., turnMs = TurnMs)`: turnMs 0 = không đồng hồ; `HasClock`; `DuelView.MsLeft` = 0 khi không đồng hồ.
- Tách `QuestLog.cs` không làm (file đã 274 dòng từ trước, quest mới ở `CardDuelQuest.cs`).
- Sửa test cũ: `CardCollectionTests` (bit 2 giờ là hợp lệ, HelpMask 3).
- Test mới: DuelNpcAiTests (4), DuelTutorialScriptTests (5), DuelNpcSessionTests (8), CardDuelQuestTests (7). ZenoniaSkills 698 -> 722.
