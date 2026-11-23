# Review phase 06 PvP + Card Wager v2 (cb11ac67, 988fb7e8, 5647505d)

Phạm vi: server-api `card-wager-service.js` / `card-wager-move.js` / schema / route; zone `CharacterSaveQueue.Wager.cs`, `WagerApiClient`, `DuelService{,.Pvp,.Wager}.cs`, `DuelPickRound`, `CardCollection` escrow, `WagerRules`, `DuelMessages` (protocol 32), `ZoneHost.ClockMs`; client `CardDuelPickPanel`, `DuelMusic` / `GameMusic`, UI flip. Chỉ đọc. Dòng theo HEAD 5647505d (agent phase 07 đang sửa `DuelService.Pvp.cs`, `DuelMessages.cs`, `NetOpcodes.cs`...).

Test (cây có việc dở phase 07):
- ZenoniaSkills 643/643 pass.
- ZenoniaNet 534/537. 2 FrostHollowLargeMap fail từ trước. `MessageRoundTripTests.SkillInvActions...` fail vì Version 32 -> 33 của phase 07 đang dở, không phải lỗi phase 06.
- vitest server-api 253/253 pass.

## Tổng quan
- **API đúng và chặt.** Escrow cả 2 deck nằm trong 1 transaction. Settle idempotent theo matchId: pick khác thì 409. Pick phải nằm trong deck người thua. Hoà mà có pick thì 422. Refund và settle loại trừ nhau nhờ transaction cùng ghi doc wager + nhân vật. API kiểm lại hết điều kiện chống lạm dụng: Lv30, tài khoản 7 ngày, cùng tài khoản, cặp 2 trận / ngày, 10 trận / ngày. Công tắc tắt chỉ chặn escrow.
- **Zone validate pick tốt.** Pick kiểm theo id trong `run.Deck` của người thua. Chỉ người thắng được chọn, chỉ trong pha Picking, không chọn được lần 2. Client không giả được.
- **Lỗ nằm ở chỗ ghép zone với API.** Zone áp delta lên bản đang giữ, còn hàng đợi save vẫn chạy xen giữa các lần thử lại của wager. `AdoptOwnWrite` lại giả định lần ghi bị mất câu trả lời là lần save của chính nó. Thêm vào đó, nhân vật vừa rớt mạng (`Hero == null`) bị bỏ qua khi bớt / cộng card. Kết quả là có 2 đường nhân card và 2 đường mất card.

## Critical
1. **Rớt mạng lúc escrow đang bay: nhân 7-8 card** (`DuelService.Wager.cs:97`, `ZoneServer.cs:148`).
   - Diễn biến:
     - A Accept rồi thoát game ngay, trong lúc API đang trả lời escrow.
     - `OnPeerDisconnected` gọi `ServerHeroFactory.Destroy`, nên `p.Hero = null`. `queue.Leave` xếp save cuối, chờ escrow xong.
     - Escrow OK: `A.Rev = R+1`. Nhưng `TakeDecks` gặp `p.Hero == null` nên `continue`: deck vẫn nằm trong `p.Rpg.State`.
     - Save cuối chụp state lúc gửi (lazy) và ghi đè lên API ở rev R+1. API giờ có đủ deck mà wager vẫn `held`.
   - Hậu quả: settle ghi thẳng cho người offline (7 lá cộng thêm), hoặc 60 s sau người thắng chọn. Nếu cả 2 cùng rời thì `SettleLoose` hoà. Mỗi bên nhân +7 / +8 lá, kể cả SSR / boss.
   - Cố ý làm được: 2 tài khoản thông đồng, script ngắt kết nối đúng lúc Accept.
   - Fix: `TakeDecks` luôn chạy `EscrowDeck` trên `seat[i].Owner.Rpg.State` miễn `Owner != null && Owner.Saved != null`. Chỉ gọi `Zone.Enter` / `PushState` khi còn Hero.
2. **Settle mất câu trả lời, save chen giữa: mất 9 lá (thắng) và 7 lá (thua)** (`CharacterSaveQueue.Wager.cs:155`, `CharacterSaveQueue.cs:190` / `:274`).
   - Diễn biến:
     - Settle landed ở R+1 nhưng mất câu trả lời. `RetryWager` set `Uncertain`, nhân vật hết `Busy` giữa 2 lần thử.
     - Save định kỳ hoặc `RequestSave` gửi rev R, bị `rev_conflict`. `AdoptOwnWrite` nhận rev R+1 rồi ghi bản zone (chưa có payout) lên R+2.
     - Settle thử lại: API trả `replayed`, `creditedNow` cho rev R+2 bằng đúng rev của zone. `MarkCredits` (`:147`) không Apply.
   - Hậu quả: wager đã `settled` nên không bao giờ được hoàn. Card mất hẳn.
   - Cùng lỗi: hết 40 lần (`:104` `gave_up`) mà có một lần đã landed thì save sau cũng ghi đè.
   - Gặp tự nhiên mỗi khi API chậm hoặc timeout.
   - Fix:
     - (a) Khi nhân vật có wager job chưa có kết quả dứt khoát (InFlight hoặc Unknown), `Tick` không gửi Save / Enhance / Trade cho nhân vật đó. Thêm cờ `WagerHold` và kiểm cạnh `c.Busy`.
     - (b) Không `gave_up` settle khi `j.Unknown`: thử lại mãi như save.
     - (c) Quyết Apply theo "zone đã trả payout của matchId này chưa" (cờ trong `PvpRun`), không theo so rev.
3. **Escrow bỏ cuộc sau 25 s hoặc 6 lần mà thật ra đã landed: settle hoà trên state chưa bớt, nhân +8 lá mỗi bên** (`CharacterSaveQueue.Wager.cs:83`, `DuelService.Wager.cs:77`).
   - Diễn biến:
     - Zone không gọi `TakeDecks`, nên bản zone còn đủ deck.
     - `SettleLoose` gửi rev R, bị `rev_conflict`. `AdoptOwnWrite` (Uncertain còn từ escrow) nhận R+1.
     - Settle gửi lại với `state` = bản zone có đủ deck. `giveWagerCards` cộng thêm 8. Credit R+2 lớn hơn R+1 nên Apply, `Give` cộng 8 vào bản zone nữa.
     - Save định kỳ chen giữa cũng gây ra đúng chuyện này.
   - Gặp khi API sập hoặc chậm ngay sau commit.
   - Fix: escrow `Unknown` thì KHÔNG bỏ cuộc. Thử lại cùng matchId (replay idempotent) tới khi có kết quả dứt khoát.
     - Nếu Ok: `TakeDecks` rồi mới `SettleLoose` hoà.
     - Nếu 404 / 4xx lúc replay: không làm gì.
     - Trong lúc chờ: chặn save như C2(a).

## High
1. **Người thắng thoát game ngay sau Take: mất payout** (`DuelService.Wager.cs:166-167`).
   - Diễn biến: settle gửi kèm state của W. W thoát trong lúc chờ trả lời. `MarkCredits` vẫn tìm thấy SavedCharacter đang `Leaving`, nâng `sc.Rev` và đặt `Apply`. Nhưng `Give` dùng `OnlineByKey` (entries đã xoá) và kiểm `Hero`, nên return.
   - Hậu quả: save cuối ghi bản không có payout lên rev mới, đè credit. W mất 8 lá của mình cộng lá vừa chọn. Người thua cũng vậy nếu thoát đúng lúc đó.
   - Đây là thao tác rất thường gặp: bấm Take xong đóng game.
   - Fix: `Give` áp lên `run.Owner[side]` (NetPlayer giữ `Rpg.State` mà Capture đọc), chỉ cần `Owner.Saved` còn trong queue. `Zone.Enter` / `PushState` / `RequestSave` chỉ khi còn Hero. Cùng gốc với C1.
2. **Xoá nhân vật khi wager còn `held` làm kẹt card bên kia mãi mãi** (`card-wager-service.js:187`, `character-service.js:101`).
   - `refundOne` thấy `!d` thì return false, settle trả 404. Bên còn lại không bao giờ nhận lại 8 lá.
   - Bên thua cố ý xoá nhân vật (lúc offline) trước khi zone settle xong: người thắng mất deck của mình.
   - Fix (chọn một):
     - `deleteCharacter` từ chối 409 khi còn wager `held`.
     - Hoặc refund / settle chỉ cho bên còn doc, ghi ledger `missing`.

## Medium
1. **Escrow replay không kiểm `status`** (`card-wager-service.js:108`, `OnEscrowReply`). Zone coi mọi 200 là held rồi `TakeDecks`. Nếu replay trả `refunded` / `settled`, zone sẽ bớt card thật. Hiếm, vì refund cần cả 2 offline. Fix: zone chỉ coi là Ok khi `status == "held"`, còn lại coi là refusal.
2. **Settle thất bại (`WagerFailed`): người chơi online không có deck tới khi logout và qua hạn 1800 s rồi redeem.** Refund chỉ chạy khi redeem lúc đã hết hạn, hoặc khi zone reset. Trong khi đó `PvpRun` mở tới 40 lần thử (khoảng 20 phút) nên không trade / đấu / sửa deck được.
   - Fix: thêm sweep refund định kỳ ở API, hoặc zone gọi lại settle hoà.
   - Báo rõ trạng thái cho client.
3. **`session_mismatch` / `rev_conflict` (không Uncertain) khi settle bị thử lại 40 lần vô ích** (`CharacterSaveQueue.Wager.cs` `wait`). Mất session thì nên `Lose` như save và gửi settle không kèm state (API ghi thẳng).
4. **Giới hạn ngày ở API có write skew:** `countDocuments` trong snapshot transaction. Hai escrow song song của cùng tài khoản (2 nhân vật / 2 client) đều thấy 9 và cùng qua. Vượt tối đa 1-2 trận, không mất card.
   - Fix: doc đếm `{userId, day}` dùng `$inc` có điều kiện trong cùng transaction (ghi cùng doc nên tạo write-conflict).

## Low
1. Lá Lock về sau khi đã rớt bản mới cùng loại thì mất cờ Lock. `ReturnCards` / `giveWagerCards` chỉ khôi phục khi loại đó không còn bản nào. Hai bên nhất quán, chỉ là UX.
2. Khi API tắt cược (403), lời mời đã hứa "Winner takes 1 card" nhưng ván chơi không cược. Nên gửi notice "nothing at stake" cho cả hai.
3. `CardDuelBattleView.Build.cs:134/139`: `cardShadow` / `cardFlash` là Image không sprite, tức hình chữ nhật màu vẽ bằng code (alpha 0,12 / chớp #FFF8DF). Đã ghi trong Sổ lỗi UI. Nếu giữ đúng luật "không art vẽ bằng code" thì xin sprite bóng / chớp mềm từ kit.
4. Nhạc:
   - Nhạc thắng chính là `duel-victory-cozy-win-sparkle`, trong khi `phase-06` / `design-notes` vẫn ghi `duel-victory-spark`. Cập nhật spec theo lựa chọn người dùng.
   - Jingle không dừng khi đóng cửa sổ hoặc bắt đầu ván mới (chấp nhận được).
   - Ducking đúng: map music về 0 trong 0,25 s rồi fade lại 1,2 s. Tắt Music trong Settings thì jingle câm. Phát 1 lần / ván nhờ `resultSounded`.
5. Thiếu `.meta` cho thư mục `Audio/music/card-duel/` và 6 file mp3: chỉ `git add` file mp3. Load bằng đường dẫn nên chạy được, nhưng mỗi máy sẽ tự sinh GUID khác nhau. Mở Unity một lần rồi commit `.meta`.
6. `CardDuelBattleView.cs:42` gán `timerText.text = ""` mỗi frame sau khi ván xong. Nên chỉ gán khi đổi.

## ClockMs float -> double
Không có regression. Mọi chỗ dùng float (Wanted telegraph / status, Party, Trade, snapshot, Idle) đều cast `(float)`. Kết quả bằng hoặc chính xác hơn trước, vì phép cộng dồn giờ làm bằng double. Duel dùng `(long)`. Wanted vẫn là float như cũ.

## Protocol 32
`DuelOp` 0..8 khớp `TryRead` (`op > LeaveLobby`). `DuelFailInfo.Last = SameAccount`. Đọc và ghi `DuelStateMsg` đối xứng. Có chặn `WagerState <= 5`, `PickCards <= 8`, `PickedCard` là 255 hoặc tối đa 63. Round-trip test pass. Phase 07 đang lên 33.

## Kiểm lại review phase 05
| # | Kết quả |
|---|---|
| M1 Place có Turn | Đã sửa: Place mang `Turn` + `Card`, sai thì `StaleMove`. |
| L1 đóng qua `RequestClose` | Đã sửa (`UiScreens.cs:75`). |
| L2 / L3 / L4 / L10 | Không còn áp dụng (NPC và `DuelRewards` đã xoá). |
| L5 không tấn công trong 5 s | Đã sửa: thêm `MsSinceAttack`. |
| L6 trade khi đang đấu | Đã sửa: `TradeService` trả lời từ chối. |
| L7 push khi lưu deck | Đã sửa: chỉ push khi deck thật sự đổi. |
| L8 GC đồng hồ | Đã sửa: chỉ gán khi số đổi. |
| L9 so chuỗi lỗi | Đã sửa: dùng `DuelPlayError`. |
| L11 ClockMs | Đã sửa: double. |

## Edge case đã rà, không lỗi
- **Zone sập giữa ván:** API giữ state đã bớt. `reset-sessions` gọi `refundZone` trả đủ. State dự phòng trên đĩa (rev cũ) không được nạp lại vì rev đã nhích lên.
- **Settle landed rồi zone sập:** status `settled`, không refund, redeem thấy card.
- **Người nhận offline hoặc vừa login lại:** `receiver_online` thì thử lại với state. Nếu đọc lúc redeem bị cũ thì nhờ so rev, credit vẫn được Apply. Không mất.
- **Đổi deck / bán / chuyển hoá trong lúc escrow hay đấu:** `SaveDeck` bị chặn bởi `lobby.IsBusy` và `Running`. `Spare` đếm thiếu (vì deck list vẫn còn 8) nên chỉ chặt hơn. Card rơi trong lúc đấu được giữ nhờ áp delta.
- **Hết hạn refund đua với settle muộn:** refund cần cả 2 offline. Hai transaction cùng ghi doc wager nên loại trừ nhau. Settle muộn nhận 409 `wager_refunded`.
- **Kill switch:** API 403 thì vẫn chơi, không cược. Settle và refund vẫn chạy.

## Việc nên làm (thứ tự)
1. C1 + H1: `TakeDecks` / `Give` áp lên `Owner.Rpg.State`, không phụ thuộc Hero hay entries.
2. C2 + C3: chặn mọi write khác của nhân vật đang có wager chưa có kết quả dứt khoát. Escrow / settle Unknown thì không bỏ cuộc. Escrow Unknown thì replay tới khi rõ rồi mới `TakeDecks` + hoà. Apply theo cờ payout của run.
3. Thêm test cho hàng đợi bằng `post` giả:
   - (a) settle mất câu trả lời, có save chen giữa, rồi replay;
   - (b) escrow mất câu trả lời tới lúc bỏ cuộc;
   - (c) rời mạng lúc escrow hoặc settle đang bay.
   - Kiểm bất biến tổng card giữa bản zone, API và escrow.
4. H2: chặn xoá nhân vật khi còn wager `held`.
5. M1-M4, rồi Low.
6. Chỉ deploy (API trước) sau khi xong 1-3.

## Câu hỏi còn mở
- Khi settle thất bại hẳn: có muốn API sweep refund định kỳ (ví dụ 5 phút) thay vì chờ redeem sau 1800 s không?
- Nhạc thắng chính: `cozy-win-sparkle` (code) hay `spark` (spec)?

**Status:** DONE_WITH_CONCERNS
**Summary:** API escrow / settle / pick đúng và idempotent. Phần zone có 3 Critical + 1 High làm nhân hoặc mất card (rớt mạng lúc escrow / settle đang bay, save chen giữa sau khi mất câu trả lời, escrow bỏ cuộc rồi settle hoà). Phải sửa trước khi deploy.
