# Code review: quest hướng dẫn card + đấu Orin (phase 01-04)

## Phạm vi
- Server: `Net/Server/DuelService.Npc.cs` (mới), `DuelService.cs`, `ServerInventoryActions.cs` (internal)
- Luật: `Rpg/Cards/DuelNpcSession.cs`, `DuelTutorialScript.cs`, `DuelNpcAi.cs`, `DuelMatch.cs`, `DuelView.cs`, `Rpg/CardDuelQuest.cs`, `QuestLog.cs`, `PlayerState*.cs`, `World/PlayerRpg.cs`
- Wire: `DuelMessages.cs`, `NetOpcodes.cs` (v35), `DuelWire.cs`, `DuelClient.cs`
- Offline: `LocalDuelHost.cs`, `RpgSession.cs`
- Client: `CardMasterTalk.cs`, `CardDuelBattleView*.cs`, `CardDuelPvpRiskConfirm.cs`, `CardDuelDeckPage.cs`, `CardDuelLobbyWindow.cs`, `PartyPlayerMenu.cs`, `CardConfirmBox.cs`, `TownNpc.cs`
- server-api: `item-validation.js` (cardHelp max 3)
- Test: ZenoniaSkills 722 pass, ZenoniaNet 559 pass + 2 FrostHollow fail sẵn có, vitest 265 pass (chạy lại 24/11).

## Đánh giá chung
Thiết kế chắc: deck mượn cố định không đụng `CardCollection` -> không thể mất/nhân bài; thưởng chỉ trả ở TurnIn qua `QuestLog.Close` (Done giữ ở server) -> không farm được. Server kiểm đủ: map an toàn, không combat, không trade, không lobby/queue, không đang đấu, khoảng cách Orin, trạng thái quest (Active cho tutorial, Done cho practice). Practice không thưởng, không lưu. `Running()` gồm NPC -> trade, Challenge (cả 2 phía), Accept, QueueRanked, SaveDeck đều bị chặn khi đang đấu Orin. Không thấy lỗ Critical/High.

## Critical
Không có.

## High
Không có.

## Medium
1. **Bảng kết quả nói sai khi ván bị huỷ (chết / sang map Hostile)** - `CardDuelBattleView.Tutorial.cs:146-164` (`IsTutorial()` = quest != Done). `TickNpc` (`DuelService.Npc.cs:70`) và offline `LocalDuelHost.cs:81` Forfeit mà KHÔNG đổi quest, nhưng client vẫn hiện "Return the borrowed deck to Orin for your reward" + nút Talk to Orin, EndReason = 1 "You left the duel." (giống bỏ cuộc thật). Người chơi tưởng xong quest.
   - Sửa: client dùng `log.StateOf(First) == ReadyToTurnIn` cho dòng "Return deck"; còn Active thì "The duel was cut short. Talk to Orin to play again." Đồng thời ở `FinishNpc` (`DuelService.Npc.cs:55-59`) đổi thứ tự: `QuestCardDuel()` + `PushState` TRƯỚC `SendView`, để view cuối đến sau RpgState (offline `Publish` cũng gọi `QuestCardDuel` trước `DuelClient.Apply`). Nếu không đổi thứ tự thì banner phải redraw khi `QuestChanged`.
2. **Ván NPC không có giới hạn thời gian ở server** - `DuelService.Npc.cs` / `DuelNpcSession`: không đồng hồ, không idle timeout. Người chơi đóng cửa sổ hoặc treo máy -> entry "đang đấu" mãi trong lúc còn kết nối: bị chặn trade/PvP/SaveDeck/Ranked tới khi Forfeit, chết hay rớt mạng. Không phải exploit nhưng là soft-lock ẩn (client chỉ tự mở lại board ở `DuelClient.Apply` lần sau, mà Orin chỉ gửi view sau khi người chơi đánh).
   - Sửa: trong `TickNpc` huỷ ván (không đổi quest) khi `now - lastPlayerActionMs > 10 phút`; hoặc cho `PlayNpc`/PvP Challenge tự huỷ ván NPC cũ thay vì `InDuel`.

## Low
3. **Bỏ cuộc ngay = xong quest không cần đánh** - `DuelService.Npc.cs:39-43`, `LocalDuelHost.cs:43-47`: PlayNpc(0) -> Forfeit -> ReadyToTurnIn -> 300 G + 3 lá N. Đúng quyết định (câu 2 plan), chỉ 1 lần / nhân vật. Rủi ro farm qua alt nhỏ (cần lụm card đầu, thưởng 3 lá N). Nếu muốn chặn: chỉ tính khi `Match.Moves.Count >= số bước script` hoặc đã đi ≥1 lượt.
4. **`CardDuelPvpRiskConfirm.acknowledgedThisSession` là static chung mọi nhân vật** - `CardDuelPvpRiskConfirm.cs:19,31,89`: đổi nhân vật trong cùng phiên thì nhân vật thứ 2 không bao giờ thấy hộp và không lưu bit. Sửa: lưu `PlayerState` đã xác nhận (`static PlayerState acknowledgedFor`) thay cho bool. Chỉ là UX, server không phụ thuộc bit này (không bypass gì).
5. **Offline chạy cả ở RunMode.Host** - `DuelClient.cs:125`: `!NetMode.IsClient` gồm Host -> host dùng `LocalDuelHost` chứ không phải `DuelService`. Nếu Host chỉ dùng dev thì bỏ qua; không thì đổi điều kiện thành `NetMode.IsOffline`.
6. **Offline không kiểm `DuelService.Enabled`; online Disabled làm quest kẹt Active** (`DuelService.cs:70` refuse trước switch). Nếu tắt card duel ở server thì Orin vẫn mời "Ready?" mà không đấu được. Cân nhắc cho `PlayNpc/Place/Forfeit/SkipScript` của NPC đi trước kiểm `Enabled`, hoặc ghi vào deploy note.
7. **Lệch nhỏ offline/online**: offline `Combat` dùng literal `5000f` (`LocalDuelHost.cs:58`) thay vì `DuelService.CombatBlockMs`; map lỗi Place offline thiếu `Over -> NoMatch` (`LocalDuelHost.cs:36`) - nên tái dùng `DuelService.PlayFailure` (đổi thành internal static) cho DRY.
8. **Nhãn bài học lệch khi đang animate** - `CardDuelBattleView.Tutorial.cs:101`: tag dùng `v.ScriptStep` (server, đã tiến) còn nội dung dùng `presented` (đang animate) -> có lúc "Lesson 2/3" với nội dung bài 1 trong ~1 s. Sửa: tính số bài từ `DuelTutorialScript.LessonAt(presented)`.
9. **Accept quest rồi hỏi Duel ngay** - `CardMasterTalk.cs:43`: `QuestTracker.Accept` online là bất đồng bộ; bấm Duel trước khi server trả lời -> NoQuest nếu hai gói khác kênh/thứ tự. Xác nhận InvAction và DuelRequest cùng kênh reliable-ordered; nếu không thì chờ `QuestChanged`.
10. **`FinishNpc` khi `p.Hero == null`** (`DuelService.Npc.cs:56`): `TakeResult()` đã tiêu result rồi mới return -> quest không đổi (chơi lại được, không mất gì). Chấp nhận được; nếu muốn chặt, kiểm Hero trước `TakeResult`.

## Edge case đã kiểm, OK
- Farm: TurnIn lặp -> `Close` chỉ true khi ReadyToTurnIn, sau đó Done; tutorial chỉ khi Active; practice không gọi quest; không có op huỷ quest.
- Bắt đầu xa Orin / map khác: `DistanceToNpc` = MaxValue khi Orin không ở map; Hostile + combat + dead + trading bị chặn bởi `StartRefusal`.
- PvP/lobby/queue: `PlayNpc` chặn `lobby.IsBusy` (cả Starting) và queue; Challenge/Accept/QueueRanked chặn bằng `Running()` có NPC; trade chặn bằng `IsDueling`.
- Mất/nhân bài: tutorial dùng `DuelTutorialScript.PlayerDeck`, practice copy `cards.Deck` (`new List`), không cược, không ghi collection.
- Rớt mạng / rời zone: `OnLeave` xoá entry, quest giữ Active. Chết / Hostile: Forfeit không `TakeResult`.
- Kịch bản: server chặn đúng lá + ô (`PlayerPlace`), `pos` chỉ tăng khi Place Ok; Skip chỉ khi còn bước; Orin lá kịch bản không có trong tay -> rơi về AI; lá bài học luôn nằm trong 5 lá đầu (deck không xáo).
- Wire v35: cờ bit 16 + `flags > 31`, `ScriptStep` byte cuối, kiểm `<= MaxScriptStep` và `AvailableBytes == 0`; `DuelWire` kẹp 0..5; op > SkipScript bị từ chối. Client v34 sẽ bị chặn bởi bump version.
- Quest chỉ mở sau card đầu: `Unlocked` chặn khi Available và `AlbumCount == 0`, `HasCards` gán trong ctor duy nhất của `PlayerState`, `Quests` readonly; server `AcceptQuest` cũng qua `Unlocked`.
- cardHelp bit 2: `HelpMask = 3`, `MarkHelp` từ chối bit lạ, zod max 3 (bit 0|1). Deploy server-api TRƯỚC zone là bắt buộc (zod cũ max 1 sẽ từ chối save có bit 2).
- Hộp rủi ro PvP chỉ là UX client; server không cần bit -> không bypass.

## Việc nên làm (theo thứ tự)
1. Sửa #1 (banner theo ReadyToTurnIn + thứ tự quest trước view).
2. Thêm idle timeout #2.
3. #4, #8 (nhỏ, nhanh). #7 DRY nếu tiện.
4. Phase 05: thử online với dev server các luồng chết / sang map / rớt mạng giữa ván.

## Kết luận
GO cho phase 05 (không có lỗ server-authoritative). Nên sửa #1 trước khi deploy.

## Câu hỏi chưa giải quyết
- RunMode.Host có dùng ngoài dev không (#5)?
- Có muốn chặn "Forfeit ngay = xong quest" (#3) không, hay giữ theo quyết định cũ?
- InvAction và DuelRequest có cùng kênh reliable-ordered không (#9)?
