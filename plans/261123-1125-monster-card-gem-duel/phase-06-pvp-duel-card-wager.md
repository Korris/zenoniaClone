# Phase 06: PvP thách đấu + Card Wager v2 ("thắng lấy 1 card", escrow cả deck ở server-api)

## Context Links
- Spec: `design-notes-chatgpt.md` mục "Cược card (Card Wager)" + thưởng PvP
- Giao dịch nguyên tử có sẵn: `server-api/src/services/character-service.js` (`commitTrade`, collection `ledger`), `server-api/src/rpg/trade-move.js`, `server-api/src/routes/internal-routes.js`, `Net/Server/ZonePersistence.cs`, `Net/Server/ZoneApiHttp.cs`
- Menu bấm người chơi: `Assets/Scripts/PartyPlayerMenu.cs`; lời mời: `TradeRequestWindow.cs`
- Luật UI: `docs/ui-layout-rules.md` (đọc hết); asset phase 02 (G cược)

## PHASE 06 v2 (người dùng chốt 23/11 chiều: THAY MỌI LUẬT CƯỢC / THƯỞNG BÊN DƯỚI)
Bản v1 (cược 1 card N/R chọn trước, nút Wager của người mời, Lock / Confirm, thưởng Gold PvP 300 / 100 / 150, đấu NPC) đã làm rồi BỎ. Luật hiện hành (spec đầy đủ: `design-notes-chatgpt.md` mục "Luật đấu card v2"):
- Mọi trận PvP thường: người thắng chọn BẤT KỲ 1 lá trong deck 8 lá người thua đã dùng ở trận (kể cả SR / SSR / boss / card Lock). Hoà: không ai mất gì. Không thưởng Gold. Không còn đấu NPC, offline Card Duel bị tắt có lý do.
- **API:** `escrow` giữ cả deck 8 lá của HAI bên (một transaction); `settle` `{result, pick}`: người thắng nhận lại 8 lá + lá chọn, người thua 7 lá, hoà đủ; idempotent theo matchId (pick sai / chọn lần 2 khác -> 409 `wager_already_settled`); quá hạn hoàn đủ; ledger. Chống lạm dụng: Lv30+, tài khoản ≥ 7 ngày, không cùng tài khoản, cùng cặp 2 trận / ngày, 10 trận / tài khoản / ngày. Hợp đồng chi tiết ở mục "Hợp đồng HTTP v2" cuối file.
- **Zone:** `DuelService.Pvp.cs` / `.Wager.cs`: escrow 2 deck -> ván -> `DuelPickRound` (30 s, hết giờ server chọn ngẫu nhiên) -> settle. `PvpRun` mở tới khi settle xong (khoá sửa deck / mời trận khác). Rớt mạng 60 s rồi xử thua, người thắng vẫn chọn; nối lại vào đúng ván (kể cả đang chọn). API tắt cược (403 `wager_disabled`) hoặc `WagerRules.Enabled=false`: vẫn chơi, không lấy card.
- **Client:** hộp mời ghi "Winner takes 1 card from the loser's deck" + cảnh báo deck (kể cả Lock) có thể mất; trang deck có hộp "Cards at stake"; màn chọn `CardDuelPickPanel` (8 lá ngửa, 30 s); người thua thấy "Waiting for X to pick..." rồi lá bị lấy; nhạc kết quả `DuelMusic` + `GameMusic.PlayJingle`. `CardWagerPanel`, nút Wager, `LocalDuelHost`, `DuelNpc*`, `DuelRewards` đã xoá.
- Wire: `NetProtocol.Version` 32 (DuelOp đánh số lại: `TakeCard`; lobby chỉ còn cờ `Stakes`; state có `WagerState` 0..5 + `PickedCard` + `PickCards`).
- Sửa kèm: `ZoneHost.ClockMs` float -> double (float mất ms sau vài giờ chạy; đồng hồ ván so bằng ms).
- Ranked (phase 07): KHÔNG lấy card.

## Overview (v1, đã bị v2 thay thế ở các điểm trên)
P1, pending. Thách người chơi gần mình đấu PvP (có hoặc không cược); thưởng Gold PvP; cược: mỗi bên 1 card N/R cùng rarity, server giữ (escrow) trước trận, chuyển chủ nguyên tử sau trận.

## Key Insights
- `commitTrade` cần cả 2 session sống; sau trận người thua có thể đã rớt mạng -> **escrow phải lấy card ra khỏi cả 2 nhân vật TRƯỚC trận** (lúc cả 2 đều online), giữ trong collection `card_wagers`; settle chỉ cần cộng card cho người nhận (online qua zone, offline ghi thẳng doc vì không ai đang giữ session).
- Zone sập giữa trận: wager `held` quá hạn -> hoàn cho 2 bên (quét khi zone reset-sessions và khi redeem ticket của nhân vật đó). Không bao giờ mất card.
- Card đếm theo id nên escrow chỉ là {characterId, cardId, 1}.
- API chưa trả tuổi tài khoản cho zone: thêm `account.createdAt` vào redeem để kiểm ">= 7 ngày".

## Requirements
- Thách đấu: `PartyPlayerMenu` thêm "Card Duel" (người chơi khác cùng map an toàn). Lời mời 15 s, 1 lời mời chờ / người, 30 s giữa 2 lần mời cùng người. Cả 2 có deck hợp lệ.
- Thưởng PvP: thắng 300 / thua 100 / hoà 150 G, tối đa 5 trận có thưởng / ngày (`cardDaily.pvpRewards`).
- Cược (chỉ PvP thường): mỗi bên đúng 1 card N hoặc R, 2 lá cùng rarity, không boss, không lá bị Reserved bởi deck nếu làm thiếu deck; không Gold / ngọc / pet. Hai bên xem card nhau, Lock rồi Confirm; đổi card -> huỷ xác nhận cả hai. Thắng lấy cả 2, thua mất, hoà trả lại.
- Chống chuyển đồ: Lv30+, tài khoản >= 7 ngày, tối đa 3 trận cược / ngày, cùng 1 cặp tối đa 2 trận cược / ngày, log mọi lần chuyển (ledger `card_wager`), công tắc khẩn cấp (`CARD_WAGER_ENABLED` ở API + hằng số zone, mặc định BẬT).
- Có cược hay không: NGƯỜI MỜI chọn khi gửi lời mời (ô "Wager" bật / tắt, người dùng chốt 23/11); lời mời hiện rõ "Card Wager" cho người nhận trước khi Accept; không cược thì đấu PvP thường như cũ.
- Rớt mạng: giữ ván 60 s chờ nối lại (theo characterId); quá hạn -> xử thua. Đồng hồ lượt của người rớt vẫn chạy (server đặt ngẫu nhiên).
- Bỏ trận (Forfeit) = thua.

## Architecture
```
server-api/src/rpg/card-wager-move.js        thuần: takeWagerCard(state, cardId), giveCards(state, ids) + kiểm rarity N/R, cùng rarity, không boss
server-api/src/services/card-wager-service.js escrow / settle / refundExpired (transaction Mongo, ledger)
server-api/src/routes/internal-routes.js      POST /internal/card-wagers/escrow, POST /internal/card-wagers/:matchId/settle
server-api/src/db/indexes.js                  card_wagers{_id=matchId, status held|settled|refunded, a{characterId,cardId}, b{...}, zoneId, createdAt, expiresAt}; index {status,expiresAt}, {a.characterId}, {b.characterId}
Assets/Scripts/Rpg/Cards/WagerRules.cs        kiểm điều kiện phía zone (thuần, test)
Assets/Scripts/Net/Server/DuelService.Pvp.cs  mời / nhận / panel cược / reconnect 60 s
Assets/Scripts/Net/Server/DuelService.Wager.cs gọi API escrow / settle qua ZonePersistence
Assets/Scripts/CardDuelInviteWindow.cs, CardWagerPanel.cs
```
Luồng cược: A mời (wager) -> B nhận -> panel: chọn card -> Lock -> Confirm cả 2 -> zone `ZonePersistence.EscrowWager(matchId, A{state,rev,session,card}, B{...})` -> API transaction: kiểm session + rev, bớt card 2 bên, ghi `card_wagers` held + ledger -> zone áp state mới (rev+1) như `TradeBook.FinishCommit` -> bắt đầu ván. Hết ván -> `settle(matchId, kết quả, người nhận online: state+rev+session)` -> API cộng card, status settled, ledger -> zone áp state. API lỗi khi settle -> zone thử lại theo `reqId` (idempotent theo matchId + status).
Dev server (không ZonePersistence): chuyển trong bộ nhớ, không lưu (như trade).

## Related Code Files
Tạo: file ở Architecture; `server-api/test/card-wager.test.js` (escrow, settle thắng / hoà, settle khi người nhận offline, refund hết hạn, replay idempotent, refuse SR / khác rarity / boss / tắt công tắc); `Tests/dotnet/ZenoniaSkills.Tests/WagerRulesTests.cs`; `Tests/dotnet/ZenoniaNet.Tests/DuelPvpMessageTests.cs`.
Sửa: `server-api/src/schemas.js` (schema 2 route), `server-api/src/config.js` (`CARD_WAGER_ENABLED`), `routes/internal-routes.js` (redeem trả `account.createdAt`, gọi `refundExpiredFor`; reset-sessions gọi refund theo zone), `services/account-service.js` (createdAt), `Net/Server/ZonePersistence.cs` + `ZoneApiHttp.cs` (2 call mới), `Net/Server/DuelService.cs`, `Net/Protocol/DuelMessages.cs` (Challenge / Accept / Decline / WagerPick / WagerLock / WagerConfirm; Version +1), `PartyPlayerMenu.cs`, `Net/Client/NetClient.Duel.cs`.

## Implementation Steps
1. `card-wager-move.js` + `card-wager-service.js` + route + vitest (Mongo test có sẵn `global-mongo-setup.js`).
2. Redeem trả createdAt; refund quá hạn ở redeem + reset-sessions.
3. `WagerRules` + test (Lv, tuổi tài khoản, số trận / ngày, cặp lặp, rarity).
4. Mở rộng `DuelMessages` + round-trip; `DuelService.Pvp` (mời, nối lại 60 s), `DuelService.Wager`.
5. UI: mục menu người chơi, cửa sổ mời, panel cược (Lock / Confirm, hiện card địch).
6. Thưởng PvP + giới hạn ngày.
7. Build zone + deploy API (**server-api TRƯỚC, zone SAU**); thử 2 client: thắng / thua / hoà có cược, rút dây mạng người thua 70 s, tắt zone giữa ván rồi vào lại (card được hoàn).

## Todo List
- [x] API escrow / settle / refund + vitest (23/11, server-api xong, 248 vitest pass, chưa deploy)   - [x] ~~redeem createdAt~~ KHÔNG cần: API tự kiểm tuổi tài khoản (`users.createdAt`) khi escrow
- [x] WagerRules + test (`WagerRulesTests`)   - [x] DuelLobby / DuelPvpMatch / DuelService.Pvp / DuelService.Wager + messages (Version 31)
- [x] UI mời (`CardDuelLobbyWindow`) + panel cược (`CardWagerPanel`) + mục menu người chơi + nút Challenge Player   - [x] Thưởng PvP / ngày (300 / 100 / 150, 5 trận)
- [x] Thử 2 client trên dev server (plain / wager / drop 60 s)   - [ ] Build + deploy (API trước, zone sau) + thử với server-api thật: escrow / settle qua HTTP, zone sập giữa ván, nối lại trong 60 s
- [x] **v2 (23/11 chiều):** API escrow cả deck + settle có pick + vitest card-wager viết lại (250 pass cả bộ); zone `DuelPickRound` + `DuelService` v2; UI mời / cảnh báo deck / màn chọn / banner thua; nhạc kết quả; xoá NPC + thưởng Gold + panel cược; thử 2 client dev server (win / lose / draw / drop / rejoin / nopick); CHƯA thử qua API + Mongo thật, chưa deploy

## Trạng thái 23/11 (zone + client xong, CHƯA deploy)
Làm khác kế hoạch:
- Lời mời / bảng cược là **`DuelLobby`** (thuần, `Net/Protocol`, test ở ZenoniaNet.Tests) thay vì nhét vào `DuelService`; trận là **`DuelPvpMatch`** (thuần, `Rpg/Cards`, nối lại 60 s theo nhân vật). `DuelService` tách 3 file: `.cs` (NPC + chung), `.Pvp.cs`, `.Wager.cs`.
- Gọi API không qua `TradeBook.FinishCommit` mà qua hàng đợi ghi: `CharacterSaveQueue.Wager.cs` (escrow chờ cả 2 nhân vật rảnh + online, thử lại cùng matchId, bỏ cuộc sau 25 s và báo `MayHaveLanded` để zone settle hoà; settle thử lại tới khi API trả lời dứt khoát, chờ `receiver_online`). `WagerApiClient` + `ZoneApiReplyParser` (thuần) dựng JSON / đọc câu trả lời, test bằng `post` giả.
- Zone KHÔNG ghi đè state bằng `state` API trả về: áp `rev` (hàng đợi) và bớt / cộng card theo delta trên bản đang giữ, để card rơi trong lúc chờ không mất (settle replay chỉ áp khi rev API lớn hơn rev zone: `WagerCredit.Apply`).
- Thua do rớt mạng / bỏ trận: KHÔNG có thưởng Gold PvP (như đấu NPC); thắng vì đối thủ bỏ vẫn nhận 300 G. Panel cược hết hạn sau 60 s (lời mời 15 s).
- `Place` mang thêm `Turn` + `Card` (review phase 05): server từ chối `StaleMove`; client khoá đặt khi còn < 0,5 s.
- Điều kiện mở trận: không bị đánh VÀ không tấn công trong 5 s (`HeroController.MsSinceAttack`).
- Animation lật bài FF8 (yêu cầu người dùng 23/11, áp cả NPC và PvP): `CardDuelBattleView.Flip.cs`, ghi trong Sổ lỗi `docs/ui-layout-rules.md`; chưa có âm thanh (kit không có sfx card).
- Đã sửa kèm review phase 05 (low 1-10 trừ ClockMs float, giữ nguyên).
- Chỉ thử trên dev server (không tài khoản, cược chuyển trong bộ nhớ): `Tools/net-duel-test.ps1 -Scenario plain|wager|drop`, cờ server `--dev-cards`. Chưa thử escrow / settle qua HTTP với server-api thật (có unit test), chưa thử nối lại trong 60 s.

## Sửa lỗi review 23/11 (mất / nhân card khi mạng lỗi), phần "Zone KHÔNG ghi đè state" ở trên vẫn đúng
Thiết kế chọn: **delta + chặn ghi + áp đúng 1 lần**, KHÔNG nhận lại nguyên state API. Lý do: sau escrow / settle, bản zone = state API + các thay đổi cục bộ (drop, exp, vàng...); nhận nguyên state phải gộp từng trường và chính chỗ gộp sinh lỗi. Bất biến chứng minh được:
1. Mọi lần card đổi chỗ ở API nằm trong một lời gọi wager (escrow / settle, idempotent theo matchId). Từ lúc gọi đi tới lúc có câu trả lời DỨT KHOÁT, `SavedCharacter.WagerHold` > 0: không save / enhance / trade / end nào của 2 nhân vật đó được gửi (`CharacterSaveQueue.Tick`). Nên không có bản ghi nào chen vào để ghi đè bản API bằng state chưa thấy escrow / payout.
2. Lời gọi chưa rõ kết quả (không trả lời, 5xx) KHÔNG BAO GIỜ bỏ: thử lại cùng matchId tới khi API nói có / không. Chỉ lời gọi chắc chắn không đổi gì (429 / 401...) mới bỏ sau 6 lần / 25 s (escrow), 40 lần (settle).
3. Bản zone được áp delta tương ứng đúng 1 lần, từ `done` chạy 1 lần với câu trả lời dứt khoát (`StakeMatch`, `Net/Protocol/StakeMatch.cs`): escrow Ok -> `TakeDeck`; settle Ok -> credit `Apply` -> `GiveCards` (cờ `paid` theo bên). `Apply` = zone đã gửi state của nhân vật đó (bản zone chưa có card); KHÔNG so rev. Rev nhân vật = rev API trả.
4. Áp lên `NetPlayer.Rpg.State` của chủ (`run.Owner`), không cần Hero hay kết nối (người đã thoát vẫn bị bớt / cộng; save cuối chụp sau đó). Người đang thoát vẫn được gửi state trong settle (save cuối chờ settle xong).
5. Escrow quá 25 s chưa có trả lời: người chơi nhận ApiBusy, trận bị `Abandon` nhưng escrow vẫn được hỏi lại; nếu hoá ra đã landed: bớt deck rồi tự settle hoà. Replay trả status khác `held` = từ chối `wager_not_held` (không bớt gì, nhận rev API).
6. Settle sau khi một lần gọi chưa rõ kết quả: chỉ nhân vật đã được gửi state ở lần đó được gửi tiếp (`Pin`), người vào sau chờ `receiver_online`. `session_mismatch` / `rev_conflict` (API ghi rõ bên nào trong `details.side`): bỏ nhân vật đó (Lost), lần sau gửi không state.
API: replay escrow trả lời kể cả khi công tắc tắt; xoá nhân vật khi còn trận held = 409 `wager_held`; refund / settle bỏ qua bên đã mất doc; quét hoàn hết hạn mỗi `CARD_WAGER_SWEEP_SEC` (120 s, 0 = tắt) cho trận held quá `CARD_WAGER_HOLD_SEC`; giới hạn ngày dùng doc đếm `card_wager_counters` ($inc trong transaction, TTL 3 ngày, refund trả lại slot); Lock trả lại cả khi có bản mới rơi trong lúc đấu. Client: bóng / loé lá dùng sprite `ui/ro-soft/pet-glow`, `timerText` chỉ gán một lần sau ván; API tắt cược -> cả hai nhận "NoStakes" (`DuelFail.NoStakes = 32`, trong cửa sổ Version 34 chưa phát hành).
Test: `WagerConservationTests` (ZenoniaNet, API giả mất request / mất trả lời / giữ trả lời: rớt mạng lúc escrow, escrow mất trả lời rồi thành công, escrow im lặng lâu, settle mất trả lời có save định kỳ, settle replay người đã thoát, người thắng đóng game sau Take, zone sập, sweep đua settle muộn, nhân vật mất doc, session lạ) + `card-wager-hardening.test.js` (vitest: counter, sweep, xoá khi held, mồ côi, replay khi tắt cược, Lock). Mỗi test kiểm tổng card bảo toàn.

## Hợp đồng HTTP v2 (23/11 chiều; thay mục "Hợp đồng HTTP" v1 ngay dưới)
Header `x-service-key`. **Deploy server-api TRƯỚC zone** (zone v2 gửi `state` không có `cardId` và `pick`; API cũ từ chối). Code: `src/rpg/card-wager-move.js`, `src/services/card-wager-service.js`, test `server-api/test/card-wager.test.js`.

**`POST /internal/card-wagers/escrow`** `{ matchId, a:{characterId, sessionId, rev, state}, b:{...} }`: `state.cardDeck` = deck bị cược (đúng 8 lá, luật 1 boss + 2 SR+, mỗi lá phải sở hữu đủ bản). 200 `{ replayed, status:'held', a:{rev,state}, b:{rev,state} }`: cả 8 lá của mỗi bên đã rời nhân vật (`cards` giảm, `cardDeck` giữ nguyên, rev + 1); zone ÁP ngay. Lỗi 422: `wager_self` (cùng nhân vật / cùng tài khoản), `wager_level`, `wager_deck` (thiếu lá / luật deck / lá không sở hữu), `wager_unknown_card`, `wager_account_age`, `wager_daily_limit` (10 / tài khoản / ngày UTC), `wager_pair_limit` (2 / cặp / ngày), `wager_invalid`. 403 `wager_disabled`. 409 `rev_conflict` / `session_mismatch` / `wager_match_conflict`. Lỗi = không đổi gì.

**`POST /internal/card-wagers/:matchId/settle`** `{ result:'a'|'b'|'draw', pick?:cardId, a?:{sessionId,rev,state}, b?:{...} }`: `pick` BẮT BUỘC khi có người thắng (phải nằm trong deck bị cược của người THUA), cấm khi hoà. Người thắng nhận lại 8 lá + pick, người thua 7 lá (bỏ 1 bản pick), hoà đủ 2 bên; lá nào quay về sau khi hết bản cuối thì giữ cờ Lock; `cardDeck` cắt theo số bản đang có (người thua còn 7). Cả hai bên đều cần credit: bên online gửi `a`/`b` (state + rev hiện tại), bên không gửi mà còn session -> 409 `receiver_online`, offline -> API ghi thẳng. 200 `{ replayed, status:'settled', result, pick, credited:[{side, characterId, cards, rev, state}] }`. 422 `wager_pick_required` / `wager_pick_invalid`; 409 `wager_already_settled` (đã settle kết quả / pick khác = chọn lần 2), `wager_refunded`; 404. Vẫn chạy khi công tắc TẮT.

**Hoàn tự động** như v1 (hết `CARD_WAGER_HOLD_SEC` 1800 s ở lần redeem kế, hoặc reset-sessions của zone): trả đủ deck 8 lá cho cả hai bên. Dữ liệu `card_wagers`: `a/b:{characterId,userId,deck[8],lockedIds}`, `result`, `pick`; ledger `escrow` / `settle` / `refund` có `deck`, `pick`, `gave`.

## Hợp đồng HTTP server-api v1 (xong 23/11 trưa; ĐÃ THAY bằng v2 ngay trên, giữ để tham khảo)
Header `x-service-key`. Mã code ở `src/rpg/card-wager-move.js`, `src/services/card-wager-service.js`. **Deploy server-api TRƯỚC zone.**
`state` = SaveData đầy đủ giống `/save` (zod `stateSchema`); `sessionId` 32 hex; `matchId` 8-64 ký tự `[A-Za-z0-9._-]`; `cardId` thuộc 40 id card.

**`POST /internal/card-wagers/escrow`** `{ matchId, a:{characterId, sessionId, rev, state, cardId}, b:{...} }` (state = đang giữ ở zone, như `trades/commit`)
- 200 `{ replayed, status:'held', a:{rev, state}, b:{rev, state} }`: zone ÁP ngay rev + state mới (card đã bị bớt, rev+1) trước khi bắt đầu ván. Gửi lại cùng matchId -> `replayed:true` (rev/state hiện tại, không bớt lần 2; `status` có thể là settled / refunded).
- 403 `wager_disabled` (`CARD_WAGER_ENABLED=false`). 409 `rev_conflict` (`details.currentRev`), `session_mismatch`, `wager_match_conflict` (matchId của trận khác). 400 `validation_error`.
- 422: `wager_self` (cùng nhân vật hoặc cùng tài khoản), `wager_unknown_card`, `wager_boss_card`, `wager_rarity` (không phải N/R), `wager_rarity_mismatch`, `wager_level` (<30), `wager_no_spare` (không có lá thừa: khoá / trong deck / không sở hữu), `wager_account_age` (<7 ngày), `wager_daily_limit` (3 trận cược / ngày / tài khoản), `wager_pair_limit` (2 / ngày / cặp tài khoản), `wager_stack_full`, `wager_invalid`, `enhance_tamper`... Lỗi = không đổi gì ở cả 2 bên.
- "Ngày" = ngày UTC, đếm từ chính các dòng `card_wagers` (held + settled; refunded không tính). Zone vẫn nên tự kiểm sớm (WagerRules) để ẩn nút, nhưng API là chốt cuối.

**`POST /internal/card-wagers/:matchId/settle`** `{ result:'a'|'b'|'draw', a?:{sessionId, rev, state}, b?:{...} }`
- `result` 'a'/'b' = bên đó thắng và nhận CẢ 2 lá; 'draw' = mỗi bên nhận lại lá của mình. Huỷ trận chưa bắt đầu = settle 'draw'.
- Người nhận ĐANG ONLINE trên zone: gửi `a`/`b` (state hiện tại, rev hiện tại). Người nhận không gửi mà còn session sống -> 409 `receiver_online` (zone thử lại); không gửi và offline -> API tự ghi thẳng doc nhân vật. Bên thua không cần gửi gì.
- 200 `{ replayed, status:'settled', result, credited:[{ side, characterId, cards:[cardId], rev, state }] }`: zone áp `rev` + `state` cho người nhận đang online. Replay (mất câu trả lời) trả `credited` với rev + state HIỆN TẠI trong DB: chỉ áp khi rev lớn hơn rev của zone.
- 404 `not_found` (matchId lạ), 409 `wager_already_settled` (đã settle kết quả khác), `wager_refunded` (đã hoàn do quá hạn / zone reset), `rev_conflict`, `session_mismatch`, `receiver_online`. Vẫn chạy khi công tắc TẮT (card đang giữ phải về).
- Card thừa 999 (stack đầy) bị cắt + ghi `overflow` vào ledger (escrow đã chặn trước bằng `wager_stack_full`).

**Hoàn tự động** (không có route riêng): wager `held` quá `CARD_WAGER_HOLD_SEC` (mặc định 1800 s) được hoàn cho cả 2 bên ở lần `tickets/redeem` kế của MỘT trong hai nhân vật (trước khi khoá session, nên state trả về đã có card); `POST /internal/zones/:zoneId/reset-sessions` hoàn mọi wager `held` của zone đó (response thêm `refunded`). Hoàn chờ nếu một bên còn session sống. Hợp lệ giữ ván: hold 1800 s lớn hơn ván dài nhất + 60 s nối lại; zone không cần gia hạn.

Dữ liệu: collection `card_wagers` `{_id=matchId, status, zoneId, createdAt, expiresAt, a/b:{characterId,userId,cardId}, result, settledAt, refundedAt, credited}`; ledger `{type:'card_wager', action:'escrow'|'settle'|'refund', matchId, characterId, userId, otherCharacterId, otherUserId, cardId, otherCardId, rev}`. Công tắc: env `CARD_WAGER_ENABLED` (mặc định true), `CARD_WAGER_HOLD_SEC`. Test: `server-api/test/card-wager.test.js` (16 test, gồm bất biến tổng card).

## Success Criteria
- Tổng số card của 2 nhân vật + escrow không đổi qua mọi kịch bản (vitest kiểm bất biến).
- Zone sập giữa ván: relog thấy lại card cược.
- Người thua rớt mạng > 60 s: người thắng nhận 2 card, người thua mất 1, ledger có 3 dòng (escrow, settle, không trùng).
- Công tắc tắt: nút cược ẩn, API từ chối 403 `wager_disabled`.

## Risk Assessment
| Rủi ro | KxT | Giảm thiểu |
|---|---|---|
| Mất / nhân đôi card khi lỗi mạng giữa settle | TB x Cao | settle idempotent theo matchId + status, transaction, test bất biến |
| Zone ghi save cũ đè escrow (rev cũ) | TB x Cao | zone áp rev+1 ngay sau escrow như trade; save rev cũ bị 409 |
| Chuyển đồ / RMT qua thua cố ý | Cao x TB | Lv30, 7 ngày, 3/ngày, cặp 2/ngày, chỉ N/R, ledger |
| Pháp lý cược vật phẩm | Thấp x Cao | người mời chọn cược từng trận (đã chốt); công tắc khẩn cấp để tắt toàn server nếu luật thị trường yêu cầu |

Rollback: `CARD_WAGER_ENABLED=false` (API + zone) -> PvP vẫn chạy không cược; wager `held` còn lại tự hoàn khi hết hạn.

## Security Considerations
Route chỉ service key. Zone gửi state đầy đủ, API kiểm session + rev + luật cược lại lần nữa (không tin zone mù quáng ở rarity / boss). Ledger lưu userId 2 bên để soi cặp bất thường.

## Next Steps
Phase 07 Ranked dùng lại PvP (không cược). Ghi cách tắt cược vào `docs/deploy.md` (phase 08).
