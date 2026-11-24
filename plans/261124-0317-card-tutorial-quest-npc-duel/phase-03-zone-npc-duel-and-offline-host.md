# Phase 03: Zone DuelService.Npc + offline LocalDuelHost

## Context Links
- `Assets/Scripts/Net/Server/DuelService.cs` (+ `.Pvp.cs`, `.Ranked.cs`, `.Wager.cs`), `ServerInventoryActions.cs` (`DistanceToNpc`, `QuestRefusal`), `ZoneServer.cs` (OnLeave dòng ~142).
- Offline cũ: `git show 5647505d^:Assets/Scripts/LocalDuelHost.cs`, `git show 5647505d^:Assets/Scripts/RpgSession.cs` (chỗ gọi `LocalDuelHost.Tick`).
- Phase 01 `DuelNpcSession`, phase 02 op.

## Overview
Priority P2. Done (code + tests, 24/11). Server quyết ván NPC + tiến độ quest. Offline dùng cùng session.

## Key Insights
- `DuelService` 1 instance / zone, `Entry` mỗi người. `Running()` chặn trade / SaveDeck: phải tính cả ván NPC.
- Rớt mạng / đổi zone / chết / sang map Hostile: bỏ ván, KHÔNG cộng quest -> chơi lại (deck mượn nên không có gì để hoàn).
- Quest chỉ đổi khi ván Tutorial kết thúc: đánh hết 16 lượt hoặc người chơi bấm Forfeit (mặc định tính, xem câu hỏi plan).
- Không cần API / Mongo / escrow: tiến độ quest đi theo save định kỳ + `RequestSave`.

## Requirements
- `PlayNpc(A)`: điều kiện `StartRefusal` (map an toàn, sống, không trade, không đánh 5 s) + không đang PvP / lobby / hàng Ranked + cách Orin <= `QuestCatalog.ReachUnits` (vị trí server) + Tutorial: quest `first-card-duel` Active; Practice: quest Done. Sai -> `DuelFail` tương ứng.
- `Place`, `Forfeit`, `SkipScript` định tuyến tới ván NPC nếu có.
- Tick mỗi frame: `session.Tick(now)` -> gửi view khi đổi; hero chết / map Hostile -> huỷ ván (không quest), gửi view kết thúc Lose với EndReason 1 hoặc chỉ `Reset` (chọn: gửi view thua để UI đóng gọn).
- Kết thúc Tutorial hợp lệ: `p.Rpg.QuestCardDuel()` -> `PushState(p)` + `ZonePersistence.Active.RequestSave(p)` đúng 1 lần (`TakeResult`).
- Offline: `LocalDuelHost` chỉ PlayNpc / Place / Forfeit / SkipScript; quest đổi qua `PetActions.Me` (PlayerState local) + `RpgSession.SaveNow()`; khoảng cách = `TownNpc` gần nhất là Orin (client là chủ state offline).

## Architecture
```
Client DuelClient.Send(PlayNpc) --online--> ZoneServer -> DuelService.OnRequest -> DuelService.Npc.StartNpc -> DuelNpcSession
                                 --offline-> LocalDuelHost.Handle -> DuelNpcSession
Tick: DuelService.Tick -> TickNpc(now) ; RpgSession.Update -> LocalDuelHost.Tick()
Hết ván Tutorial -> PlayerRpg.QuestCardDuel -> RpgState + save
```
`Entry` thêm `DuelNpcSession Npc`. `Running(id)` = PvP mở HOẶC Npc chưa xong.

## Related Code Files
- Tạo: `Assets/Scripts/Net/Server/DuelService.Npc.cs` (StartNpc, PlaceNpc, ForfeitNpc, SkipScript, TickNpc, FinishNpc), `Assets/Scripts/LocalDuelHost.cs` (phục hồi rút gọn, ~90 dòng).
- Sửa: `DuelService.cs` (Entry.Npc, Running, OnLeave bỏ ván, switch op PlayNpc/SkipScript, Place/Forfeit định tuyến, Tick gọi TickNpc, comment đầu file: NPC chỉ cho tutorial + practice), `ServerInventoryActions.cs` (`DistanceToNpc` -> `internal static`), `RpgSession.cs` (gọi `LocalDuelHost.Tick()` khi offline).
- Ownership: `Net/Server/DuelService*.cs`, `ServerInventoryActions.cs`, `LocalDuelHost.cs`, `RpgSession.cs` (chỉ 1 dòng tick).

## Implementation Steps
1. `DistanceToNpc` internal.
2. `DuelService.Npc.cs`: StartNpc kiểm điều kiện, seed `NewSeed()`, Tutorial -> `DuelNpcSession.Tutorial(now)`, Practice -> `DuelNpcSession.Practice(seed, deckHợpLệHoặcMượn, now)`; `SendView`.
3. `DuelService.cs` định tuyến; `OnLeave` bỏ ván NPC (không quest).
4. `TickNpc`: CanKeepDueling sai -> Forfeit + bỏ, không quest; session đổi -> SendView; Finished + Tutorial + kết thúc hợp lệ (EndReason 0 hoặc Forfeit chủ động) -> QuestCardDuel + PushState + RequestSave; xoá `Entry.Npc` khi xong (người chơi bấm Close chỉ là UI).
5. `LocalDuelHost` phục hồi: cùng kiểm tra (map an toàn, sống, 5 s combat, gần Orin, trạng thái quest), cùng session, SaveNow khi quest đổi; đổi nhân vật -> bỏ ván.
6. `DuelClient.Send`: offline + op NPC -> `LocalDuelHost.Handle` (sửa dòng `if (NetMode.IsClient) ... else Notify(OfflineReason)`). (File `DuelClient.cs` thuộc phase 04 nhưng dòng định tuyến này làm ở đây; phase 04 không sửa dòng đó.)
7. Biên dịch: csproj tạm hoặc đọc `Editor.log` lần biên dịch mới nhất; không build vào project đang mở.

## Todo List
- [x] DistanceToNpc internal
- [x] DuelService.Npc.cs
- [x] DuelService.cs định tuyến + Running + OnLeave + Tick
- [x] Quest progress + PushState + RequestSave 1 lần
- [x] LocalDuelHost + RpgSession tick + DuelClient định tuyến offline
- [x] Biên dịch 0 lỗi

## Success Criteria
Dev server: `--talk-npc cardMaster` -> nhận quest -> PlayNpc(0) -> đánh hết -> RpgState quest ReadyToTurnIn; rớt mạng giữa ván -> vào lại quest vẫn Active, đánh lại được; PlayNpc(1) trước khi xong quest -> NoQuest; đứng xa -> TooFar; trade lúc đang đấu NPC bị từ chối. Offline cùng kết quả + file save có quest.

## Risk Assessment
- `DuelService.cs` vượt 200 dòng: mọi logic NPC nằm ở `.Npc.cs`, file chính chỉ thêm định tuyến.
- Ván NPC không đồng hồ giữ RAM: 1 session / người, xoá khi rời -> không đáng kể.
- Gửi quest 2 lần: `TakeResult` 1 lần + `QuestLog` chỉ chuyển Active -> Ready.

## Security Considerations
Khoảng cách / trạng thái quest / kết quả tính ở server; client không thể tự báo "đã thắng". Rate limit 10 req/s hiện có áp cho op mới. Thưởng chỉ ở TurnIn (có kiểm khoảng cách Orin sẵn).

## Next Steps
Phase 04 UI.

## Làm khác kế hoạch (24/11)
- Quyết định: Forfeit giữa ván tutorial TÍNH hoàn thành quest; offline được đấu tập; quest chỉ mở sau card đầu.
- `DuelClient.cs` sửa thêm (ngoài dòng route): `PlayNpc(bool)`, `SkipScript()`, `Place` bỏ kiểm đồng hồ khi `View.Npc`, 3 dòng `FailText` (NoQuest / TooFar / ScriptMove). Phase 04 không cần làm lại.
- `ServerInventoryActions`: `HeroMap` cũng thành `internal`.
- Offline quest đổi qua `RpgSession.Owner.QuestCardDuel()` (PlayerRpg, để bắn QuestChanged) rồi `SaveNow()`. Khoảng cách offline = `TownNpc.Villagers` có id cardMaster trong `QuestCatalog.ReachUnits`.
- Hero chết / sang map Hostile: ván bị bỏ, gửi view thua, KHÔNG đổi quest. Rớt mạng / rời zone: bỏ entry.
- Biên dịch: `Tools/build-from-copy.ps1` -> `C:\temp\zu-copy`, BUILD RESULT Succeeded errors 0. Chưa test chạy thật dev server / offline trong game.
