# Review cuối trước push: e32e606e, 9901ed99, 0bb2f661, f7a6de9b, fd23b7e9

Phạm vi: 5 commit theo thứ tự task yêu cầu. Đối chiếu từng Critical/High của `code-reviewer-261123-1517-phase-06.md` và `code-reviewer-261123-1536-phase-07.md`. Rà thêm lỗi mới trên diff. Chạy lại test.

## Môi trường (ghi chú, không phải lỗi code)
Máy này chỉ có .NET SDK 9.0.203, project target `net10.0` (`dotnet test` báo NETSDK1045). Đã tự cài .NET SDK 10.0.404 tạm vào `C:\temp\dotnet10` (dotnet-install.ps1, không cần admin) để chạy test. Máy khác của người dùng có thể cũng thiếu SDK 10 — nên cài cố định hoặc ghi vào `docs/pending-tasks.md`.

## Test (SDK 10.0.404 local)
- `ZenoniaSkills.Tests`: **698/698 pass**.
- `ZenoniaNet.Tests`: **557/557 pass** (dotnet test báo "559 total, 2 failed" — đúng 2 `FrostHollowLargeMapTests` biết trước, không liên quan card).
- `server-api` `npm test` (vitest): **264/264 pass**.
- Chạy riêng `card-wager-hardening.test.js` + `redeem-user-id.test.js`: 10/10 pass. `WagerConservationTests` (349 dòng, 16 test) nằm trong 557 — đã soát từng test, khớp đúng 3 Critical + H1 phase-06.
- `RankedReviewFixesTests` (15 test) nằm trong 698 — khớp đúng H1/H2/M1/M2/M3 phase-07.

Số liệu khớp với `docs/pending-tasks.md` ghi lại (698 / 557 / 264). Không cheat, không mock giả, test chạy thật qua Mongo (makeCtx) và fake API có lỗi mạng giả lập (WagerFakeApi).

## Đối chiếu Critical/High phase-06 (API escrow/settle zone)

| # | Mức | Tóm tắt | Đã sửa ở | Xác nhận |
|---|---|---|---|---|
| C1 | Critical | Rớt mạng lúc escrow: nhân card | `DuelService.Wager.cs` (`StakeHolder.TakeDeck` áp lên `p.Rpg.State` không cần `Hero`), `StakeMatch.OnEscrow` gọi `holderOf(s).TakeDeck` cho cả 2 bên bất kể online | ĐÃ SỬA. Test `ADisconnectWhileTheEscrowIsInFlightTakesTheDeckFromHisCopyBeforeHisLastSave` |
| C2 | Critical | Settle mất câu trả lời + save chen giữa: mất card | `CharacterSaveQueue.cs` (`WagerHold` chặn Tick khi >0), `CharacterSaveQueue.Wager.cs` (`SendSettle` không `gave_up` khi `j.Unknown`), `MarkCredits`/`StakeMatch.Pay` (`paid[]` áp đúng 1 lần) | ĐÃ SỬA (cách khác bản đề xuất gốc nhưng cùng mục đích: chặn ghi bằng `WagerHold`, dedupe bằng `paid[]` cục bộ + `c.Apply` từ server). Test `ALostSettleAnswerWithPeriodicSavesDueIsReplayedAndPaidOnce`, `ALostSettleAnswerNeverGivesUpEvenAfterManyTriesAndAnApiOutage` |
| C3 | Critical | Escrow bỏ cuộc rồi settle hoà trên state chưa bớt: nhân card | `DuelService.Wager.cs` (`TickStakes`/`EscrowPatienceMs` chỉ `Abandon()` báo ApiBusy, KHÔNG dừng escrow thật; `StakeMatch.OnEscrow`: nếu `Abandoned` thì tự `Settle("draw")` sau khi `TakeDeck`) | ĐÃ SỬA. Test `AnEscrowThatGetsNoAnswerForAWhileIsNeverGivenUpAndIfItLandedTheMatchIsSettledAsADraw` (2 biến thể) |
| H1 | High | Người thắng thoát ngay sau Take: mất payout | `StakeMatch.Pay` dùng `holderOf(side)` (tức `run.Owner`, giữ qua disconnect) không qua `OnlineByKey` | ĐÃ SỬA, cùng cơ chế C1. Test `TheWinnerWhoClosesTheGameRightAfterTakeStillGetsThePayout` |
| H2 | High | Xoá nhân vật khi wager `held` kẹt card bên kia | `character-service.js: remove()` chặn 409 `wager_held` khi còn match held | ĐÃ SỬA (đúng lựa chọn đề xuất "chọn một: 409"). Test `ACharacterCannotBeDeletedWhileItsMatchIsHeldAndOnlyAfterTheRefund` |
| M1 | Medium | Escrow replay không kiểm `status` | `WagerApiClient.EscrowReply` giờ đọc `status`, không phải `held` thì trả `wager_not_held` | ĐÃ SỬA kèm bonus (không bắt buộc với GO nhưng đã làm) |
| M4 | Medium | Giới hạn ngày write-skew (`countDocuments`) | `card-wager-service.js`: đổi sang `card_wager_counters` + `$inc` trong transaction | ĐÃ SỬA, có test race thật (`Promise.all` 2 escrow cùng lúc) |

Tất cả Critical + High phase-06 đã sửa đúng, đủ, có test race/disconnect giả lập qua `WagerFakeApi` (bắt mất request, mất response, giữ lại, outage dài) khớp với mô tả lỗi gốc. Không thấy patch nửa vời hay chỉ sửa comment.

## Đối chiếu High phase-07 (Ranked)

| # | Mức | Tóm tắt | Đã sửa ở | Xác nhận |
|---|---|---|---|---|
| H1 | High | Offline mà thắng/hoà vẫn bị ghi thua tạm | `RankedPending.cs` (mới) + `DuelService.Ranked.cs::ApplyPendingRank` gọi từ `OnJoin` (login/reconnect chung, không chỉ PvP) | ĐÃ SỬA. Test `AnOfflinePlayerWhoWinsKeepsTheWinNotTheProvisionalLoss`, `BothOfflineEachGetsTheRealResultWhenTheyReturn`, `AParkedResultIsACopyAndExpiresAfterADay` (hạn 24h, không mất vĩnh viễn nếu quay lại trong ngày) |
| H2 | High | Cày LP bằng nhân vật phụ cùng tài khoản | `RankedQueue.Waiter.Account` (từ `NetPlayer.UserId`, đọc từ `redeem` trả thêm `userId`), `Allowed()` chặn `a.Account == b.Account`; `EarlyForfeit`/`MinMovesForForfeit=3` chặn farm bằng forfeit sớm | ĐÃ SỬA cả 2 phần (chặn ghép + chặn farm-forfeit). Test `TwoCharactersOfOneAccountNeverMatchButAStrangerDoes`, `AForfeitBeforeThreeMovesByHandCountsForNobody` |
| M1 | Medium | MMR ẩn gửi về client | `ServerReplication.cs` gọi `RankedRules.HideRating(save)` trước khi gửi (chỉ trên **bản capture/copy**, state server giữ nguyên — đã soát `CardCollection.Capture` dùng `RankedRules.Copy` nên không dính tham chiếu) | ĐÃ SỬA. Test `TheSaveSentToAClientHasNoRatingAndTheServersOwnStateKeepsIt` |
| M2 | Medium | MMR chạm 0 bị reset toàn bộ (đường "reset smurf") | `RankedRules.Apply`: sàn `Math.Max(1, ...)`; `EnsureSeason` chỉ dùng `season==0` làm dấu "chưa chơi" | ĐÃ SỬA. Test `AnMmrOfZeroIsRaisedToOneNotResetToANewCharacter`, `MmrNeverFallsBelowOne`, `OnlySeasonZeroGetsTheStartRating` |
| M3 | Medium | 2 hằng số giới hạn cặp/ngày lệch nhau | `WagerRules.PairLedger` nhận `maxPerDay` qua ctor, Ranked dùng `RankedRules.MaxMatchesPerPairPerDay` | ĐÃ SỬA. Test `TheRankedLedgerUsesTheRankedLimit` |

Cả 2 High phase-07 và cả 3 Medium đã sửa đúng, có test riêng, pass thật.

## Rà lỗi mới trên diff (fresh-eyes)

Không thấy Critical/High mới. Vài điểm nhỏ, không chặn deploy:
- **DRY nhẹ:** `card-wager-service.js` thêm hàm `hasHeld(characterId)` nhưng `character-service.js::remove()` lại viết query riêng (`db.collection('card_wagers').findOne(...)`) thay vì gọi `wagers.hasHeld`. Không sai, chỉ trùng logic ở 2 nơi — nếu sau này đổi field tên thì dễ quên 1 chỗ. Có thể gộp sau, không gấp.
- **Edge case escrow + reconnect giữa lúc đang bay (không phải bug):** nếu người chơi rớt mạng rồi nối lại (NetPlayer mới) đúng lúc escrow call của trận đang chờ API trả lời (trước khi `OnEscrow` chạy), `StakeHolder` vẫn trỏ `pe.Seat[side].Owner` là NetPlayer CŨ (chưa được cập nhật khi reconnect, vì `OnEscrow` chỉ gán lại `seat[i].Player`, không gán `seat[i].Owner`). Đã soát: không mất/nhân card vì `ZonePersistence.Attach` nạp state mới hoàn toàn từ API khi reconnect (API lúc đó đã trừ card nếu escrow đã landed), nên bản NetPlayer cũ bị `TakeDeck` áp vào chỉ là bản mồ côi, không ai đọc/lưu. Vô hại, chỉ là code hơi khó đọc. Không cần sửa trước deploy.
- **`WagerHold` có thể treo lâu nếu server-api sập hẳn luôn** (không giới hạn trên của Unknown-retry) — đây CHÍNH LÀ rủi ro đã được liệt là "accepted" trong brief ("session hết hạn 90s khi nhân vật bị khoá ghi lâu lúc API chập chờn"). Không tính là finding mới.
- `fd23b7e9` (icon Poison): chỉ đổi 1 dòng logic (`ElementIcon` bỏ nhánh null), không rủi ro.
- zod `cardHelp` (server-api) khớp đúng C# `CardCollection.Help & HelpMask` (cả 2 chặn ở bit 0, max 1) — không lệch schema.

Không thấy double-spend card, không thấy race mới trong escrow/settle, không thấy MMR đối thủ lộ (chỉ MMR của chính mình từng lộ, đã vá), không thấy deadlock hàng đợi save (WagerHold chỉ chặn job dispatch, EndWager luôn chạy trong finally nên không kẹt job).

## Thứ tự deploy
server-api TRƯỚC (cần cho: zod `cardHelp` v6, wager hardening — sweep/counter/delete-block/status-check, redeem trả thêm `userId`), zone SAU (protocol v34: `CardHelp` InvOp 36, `DuelFail.NoStakes` 32, đọc `details.side`/`status` của escrow reply, dùng `userId` cho Ranked same-account). Đúng theo `docs/deploy.md` và `docs/pending-tasks.md` đã ghi.

## Verdict

**GO** — deploy server-api trước, rồi zone sau.

Lý do: toàn bộ Critical + High của 2 report phase-06/phase-07 đã được sửa đúng, đủ (không phải patch nửa vời, không chỉ sửa comment), mỗi lỗi có test hồi quy riêng mô tả đúng kịch bản gốc và test đó pass thật (không mock giả). Rà thêm trên diff không thấy Critical/High mới. 3 bộ test (ZenoniaSkills, ZenoniaNet trừ 2 fail biết trước, server-api vitest) đều pass.

Việc còn lại SAU khi deploy (không chặn GO, theo đúng checklist người dùng đã ghi trong pending-tasks.md mục 2-5): thử online thật với API+Mongo thật nhiều zone, build exe mới, nghe nhạc trong game, duyệt element/mô tả 40 card.

## Câu hỏi còn mở
- Máy deploy thật có .NET 10 SDK cài sẵn chưa, hay cũng cần cài như máy này?
- `card-wager-service.hasHeld()` không được gọi — giữ để dùng sau (ví dụ tool debug) hay bỏ?
