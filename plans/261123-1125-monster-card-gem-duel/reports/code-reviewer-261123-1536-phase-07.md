# Code review phase 07 Ranked (c8b24a99, 0a163497)

## Phạm vi
- RankedRules.cs, RankedQueue.cs, DuelService.Ranked.cs, DuelService.Pvp.cs/DuelService.cs (hook), DuelMessages/NetOpcodes (v33), DuelWire, NetClient, DuelClient, CardDuelRankedPanel, CardDuelBattleView.Ranked, CardDuelDeckPage, CardCollection/CardSaveData, item-validation.js (zod).
- Test: ZenoniaSkills `~Ranked` 38/38 pass, ZenoniaNet DuelPvp+RoundTrip 34/34 pass, vitest card-state-schema 32/32 pass (cây đang có sửa đổi của agent khác).

## Đánh giá chung
Thuần, gọn, đúng spec phần lớn. LP/MMR math đúng (bằng MMR: +25/-20, clamp 20..30 / 15..25, LP >= 0, lpChange thực). Epoch 1793491200000 = 2026-11-01 00:00 UTC đúng. Ranked không escrow, không pick, không gọi API wager (StartMatch stakes=false). Client chỉ gửi QueueRanked/LeaveQueue, không gửi số; internal save route chỉ cho zone. UI dùng art (panel-inner/detail, btn-*, progress-*, rank-*.png có trong `Sprites/ui/paper/cards/ranked/`), không vẽ art bằng code.

## High

### H1. Người chơi offline mà THẮNG / HOÀ vẫn bị ghi thua tạm
`DuelService.Ranked.cs:143-145` (`if (p == null ...) continue;`) + `DuelService.Pvp.cs:80`.
Khi A rớt mạng, A bị ghi thua tạm và lưu. Trong 60 s A vẫn có thể thắng hoặc hoà: DuelMatch tự đặt bài thay A khi hết 20 s (`DuelPvpMatch.cs:7`), còn B có thể forfeit, chết hoặc rời map an toàn (`TickPvp` forfeit). Khi đó SettleRanked bỏ qua A vì offline, nên A giữ trận thua trong khi kết quả thật là thắng. Nếu cả hai cùng rớt mạng thì cả hai đều bị ghi thua. Run không có stakes đóng ngay sau đó, nên A vào lại cũng không sửa được.
Sửa: khi bên offline có kết quả khác "thua tạm", lưu `pendingRank[charKey] = after` (kèm matchId) và áp vào `OnJoin` trước `PushState` + `RequestSave`. Tốt hơn nữa là lưu offline qua API (cùng đường save với rev hiện tại). Ít nhất phải log WARN để đối soát. Thêm test: A rớt mạng, B forfeit, kiểm tra A nhận +LP.

### H2. Thiếu chặn cùng tài khoản: tự cày LP bằng nhân vật phụ
`RankedQueue.Allowed` (`RankedQueue.cs:203-211`) chỉ so `Key` theo nhân vật. Khoá phiên ở server-api chỉ tính theo nhân vật (`character-service.js:108-120` `lockSession`), nên một tài khoản có thể online 2 nhân vật trên 2 client. Card Wager có chặn việc này (`card-wager-service.js:127` `wager_self` theo userId) nhưng Ranked thì không. Cộng thêm việc forfeit ngay lượt 0 vẫn được tính, một người có N nhân vật phụ Lv20 cày được 2N trận thắng mỗi ngày (~25 LP mỗi trận).
Sửa: đưa userId của nhân vật (đã có trong doc load) vào `NetPlayer.Saved`, thêm `Waiter.Account` và từ chối ghép khi cùng account. Giới hạn cặp/ngày cũng nên tính theo cặp account. Cân nhắc: trận forfeit trước N nước (ví dụ < 3 nước) thì vẫn trừ LP bên bỏ nhưng bên thắng chỉ nhận 0 hoặc nửa LP.

## Medium

### M1. MMR "ẩn" bị gửi về client
`ServerReplication.cs:222` gửi toàn bộ save JSON trong RpgState, trong đó có `cardRank.mmr` (`CardCollection.cs:159`). `DebugCapture.NetDuel.cs:99` còn in mmr ra. Spec nói MMR ẩn, và doc `DuelRankedMsg` ghi "never put in this message" là đúng nhưng không đủ. Hiện chỉ lộ MMR của chính mình, MMR đối thủ không lộ.
Sửa: đặt `mmr = 0` trong bản gửi client (sau khi build save JSON cho RpgState, trước Write), hoặc chấp nhận và sửa lại doc/spec.

### M2. MMR chạm 0 thì bị reset toàn bộ
`RankedRules.cs:127-128` (`r.mmr <= 0` thì mmr=1000, lp=0, best=0) và `:109`. `Apply` cho MMR xuống đúng 0 (`Math.Max(0, ...)`). Lần EnsureSeason kế tiếp sẽ coi đây là nhân vật mới: xoá LP và best, nhưng không xoá wins/losses. Đây là đường "reset smurf". Khó xảy ra nhưng là lỗi logic.
Sửa: chỉ dùng `season == 0` để nhận biết "chưa từng chơi", và kẹp MMR >= 1 (hoặc một sàn như 100) trong Apply.

### M3. Hai hằng số giới hạn cặp/ngày tách nhau
`DuelService.Ranked.cs:19,132` dùng `WagerRules.PairLedger`, ledger này đọc `WagerRules.MaxPerPairPerDay`. Còn `RankedRules.MaxMatchesPerPairPerDay` chỉ xuất hiện trong log và test. Đổi một bên thì Ranked lệch mà test không bắt được.
Sửa: cho `PairLedger` nhận giới hạn qua ctor (`new PairLedger(RankedRules.MaxMatchesPerPairPerDay)`).

## Low
- L1 `RankedRules.Enabled` (`RankedRules.cs:19`) không nối vào launch arg hay config nào (grep không thấy chỗ set), nên "rollback switch" trong plan muốn tắt phải sửa code. Nên nối vào `NetLaunchArgs`.
- L2 `BeginRanked` (`DuelService.Ranked.cs:88-93`) dùng chung `err` cho cả 2 bên, nên Extra của fa bị fb ghi đè (BeginPvp cũng mắc cùng lỗi).
- L3 `ProvisionalRankLoss` tính `counted` lúc rớt mạng, còn SettleRanked tính lúc kết thúc. Nếu ngày UTC đổi giữa trận thì hai lần có thể khác nhau. Vô hại vì kết quả settle ghi đè bản tạm khi người chơi online.
- L4 Thanh LP (`CardDuelRankedPanel.cs:111-112`) có độ rộng tối thiểu 48 px, nên ở đúng mốc bậc (lp=300, tiến độ 0) vẫn hiện 48 px. Nên ẩn thanh khi `prog == 0`.
- L5 Rollover nhảy nhiều mùa: `prevBest` = best của mùa chơi cuối, không phải "mùa vừa xong" (mùa đó bị bỏ, best = 0). Hợp với doc, chỉ cần chốt ngữ nghĩa trước khi làm cosmetic.
- L6 Waiter giữ MMR từ lúc vào hàng. Nếu mùa đổi trong lúc chờ thì ghép bằng MMR trước decay, lệch nhỏ.

## Đã kiểm, không có lỗi
- Reset mùa: lười, idempotent, giới hạn 10 mùa, đồng hồ lùi thì giữ nguyên. Rewards chỉ OR bit theo tier của best và chỉ khi đã chơi, nên không farm được. Client `DisplayRank` dùng cùng công thức để hiển thị LP đã halve.
- Mùa đổi giữa trận: kết quả tính từ RankBefore (mùa cũ), lần queue sau mới rollover. Đúng.
- Thua tạm rồi vào lại: mọi kết quả tính từ `RankBefore`, rớt mạng lặp lại vẫn idempotent, không cộng hai lần.
- Hàng chờ: bị gỡ khi OnLeave, chết, map hostile, trade. Combat giữ chỗ (Ready=false). Không có entry trùng. Không ghép chính mình, cùng party hay đối thủ vừa gặp 10 phút. `recent` được dọn khi > 256 entry, PairLedger clear theo ngày, nên không rò bộ nhớ.
- Đang queue thì Challenge/Accept/SaveDeck bị từ chối (Busy/TargetNotReady). Đang ở lobby hay đang có run (đến khi Closed) thì không queue được. BeginRanked kiểm lại EligibleToStart, ai không hợp lệ thì người kia giữ chỗ.
- Protocol v33: opcode 29 mới, flags bit 4/8, LpChange short, kẹp ±999, TryRead kiểm `AvailableBytes==0`, `DuelOp.LeaveQueue` là bound. Round-trip test pass.

## Việc nên làm (theo ưu tiên)
1. H1: áp kết quả cho bên offline (pending theo charKey hoặc save qua API) + test.
2. H2: chặn cùng account trong RankedQueue/BeginRanked, giới hạn theo account, xử lý forfeit sớm.
3. M1: không gửi mmr ra client, hoặc sửa spec.
4. M2/M3, rồi các mục Low.

## Câu hỏi mở
- Có muốn trận forfeit quá sớm (< 3 nước) vẫn cho bên kia +LP không?
- Ẩn MMR là yêu cầu cứng, hay chỉ là "không hiển thị"?

**Status:** DONE_WITH_CONCERNS
**Summary:** Phase 07 sạch phần math, protocol và UI. Có 2 lỗi High: bên offline thắng vẫn bị ghi thua, và cày LP bằng nhân vật phụ cùng tài khoản.
