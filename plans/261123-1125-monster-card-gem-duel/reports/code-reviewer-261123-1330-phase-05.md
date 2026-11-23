# Review phase 05 Card Duel (00751a28, 2d7d2573)

Phạm vi: DuelBoard / DuelMatch / DuelRng / DeckRules / DuelNpcAi / DuelNpcTable / DuelNpcSession / DuelNpcRun / DuelRewards / DuelView, DuelMessages + protocol 30, DuelService + route ZoneServer, LocalDuelHost / DuelClient / DuelWire, UI deck + bàn đấu, khoá theo loại (C#, zod, `card-wager-move.js`), sửa review phase 03. Chỉ đọc.
Test (cây có việc dở của phase 06): ZenoniaSkills 647/647 pass. ZenoniaNet 499/501, 2 FrostHollowLargeMapTests fail từ trước. vitest card-wager + card-state-schema 45/45 pass.

## Tổng quan
Luật đúng, server giữ quyền quyết định. Không thấy cách nào nhận thưởng hai lần hay tạo Gold.
- **Lật:** cạnh Top của lá đặt so với Bottom của lá trên, Right so với Left của lá phải, và ngược lại. Có chặn mép bàn. Cạnh bằng nhau không lật, lá của mình không lật, không lật dây chuyền (`DuelBoard.cs:37-58`).
- **Tỉ số và lượt:** 16 ô luôn đủ chủ nên `>=9` thắng, còn lại là 8-8 hoà. Hai bên đi xen kẽ, mỗi bên đặt đúng 8 lá. Tay không bao giờ rỗng khi tới lượt, và ô trống không bao giờ hết trước nước thứ 16, nên timeout không thể chia cho 0.
- **Server chặn:** đi sai lượt, chỉ số tay ngoài khoảng, ô đã có lá, ván đã xong (`DuelMatch.cs:164-169`). Client chỉ gửi chỉ số tay và ô. Tay của địch và thứ tự rút không bao giờ gửi xuống. Mỗi người tối đa 10 request/s, và `TryRead` phải đọc hết byte, nếu không thì kick.
- **Thưởng:** `TakeResult` và `Settle` mỗi thứ chỉ chạy một lần. Chỉ thắng mới có thưởng. Forfeit, rời mạng, chết hay sang map có quái đều tính thua và không có thưởng. Bit thắng lần đầu được lưu và `RequestSave` ngay. Thưởng ngày chỉ trả khi `npcWins == 3`, ngày lấy theo đồng hồ UTC của server.
- **Offline:** save offline nằm ở `SaveStore` cục bộ, không có đường nào đẩy lên server-api. Nên thưởng offline không lọt vào save online.
- **Deck:** kiểm khi lưu và kiểm lại lúc bắt đầu ván (`requireFull`). Không lưu deck được khi đang đấu. Muốn bán hay chuyển hoá lá trong deck thì phải còn dư bản (`Spare`).
- **Khoá theo loại:** C#, JS và zod khớp nhau. Save cũ có `locked > 1` vẫn đọc được, vì C# chỉ xét `> 0`. server-api trả `doc.state` thô, không parse lại, và lần save sau ghi lại thành 0/1.
- **Art:** không có art vẽ bằng code. Chỉ có veil trong suốt, `Outline` cho chữ, và ô bấm `Color.clear`.

Không có lỗi Critical / High.

## Kiểm lại review phase 03
| # | Kết quả |
|---|---|
| M1 khoá với bản mới về | Đã sửa: cờ theo loại, bản rơi sau cũng bị khoá. |
| M2 bản khoá không vào deck | Đã sửa: `Spare = locked ? 0 : n - deck`. `DeckRules` không xét khoá. `Load` cho lá khoá vào deck. |
| M3 chồng chữ | Đã sửa: thẻ to bỏ tên, Album `GridY` 202 (cách tab 12), My Cards pitch 140 / card 110. Có ảnh chụp. |
| M4 ô Owned 3 dòng | Đã sửa: gộp "Locked, in deck n" thành 1 dòng. |
| L1 Album bị cắt ở cap | Đã sửa (`AlbumRules.cs:24`). |
| L2 SetLock khi cả deck | Không còn áp dụng (khoá theo loại). |
| L4 StateKey mỗi frame | Đã sửa: 5 lần/s, cộng thêm khi đổi object mirror. |
| L5 Remove SR+ không hỏi | Đã sửa: có hộp xác nhận. |
| L6 cỡ chữ 13 | Đã sửa ở mốc Album và chip. |
| L8 Esc khi đang hỏi / resize | Đã sửa: Esc chỉ huỷ hộp, `FitWindow` chạy lại khi màn đổi cỡ. |
| L10 test qua dispatch | Một phần: thêm `SellBySlot` / `LockBySlot` mà PlayerRpg gọi thẳng. |
| Ghi chú wire Sell all | Đã sửa comment InvOp và Version 30. |

## Medium
1. **Place gửi chỉ số tay, không kèm số lượt, nên có thể đặt nhầm lá** (`DuelMessages.cs:7`, `DuelService.cs:131`).
   - Đồng hồ client bắt đầu đếm từ lúc nhận gói, nên chậm hơn server khoảng RTT/2.
   - Kịch bản: người chơi bấm sát giờ, server đã tự đặt (`Tick`), rồi NPC đi (0,6 s) trước khi gói Place tới. Lúc đó lại đúng lượt người chơi. Nhưng tay đã đổi: lá bị rút ra, lá bù vào cuối tay. Server đặt một lá khác vào ô người chơi chọn.
   - Với NPC thì hiếm. Phase 06 PvP thì đối thủ là người, có thể đi rất nhanh, nên dễ gặp hơn.
   - Fix (protocol 30 chưa phát hành nên còn sửa được): thêm byte `Turn` (bằng `Moves.Count + 1` client đang thấy) vào DuelRequest Place. Server từ chối nếu khác lượt hiện tại. Có thể thêm card index để kiểm `hand[h] == card`.
   - Thêm vài trăm ms ân hạn, hoặc cho client tự khoá khi `SecondsLeft < 0,5`.

## Low
1. **`UiScreens.Close(UiScreen.Card)` gọi thẳng `CardWindow.Close()`** (`UiScreens.cs:75`, nút HUD `RoHudMenu.cs:46` gọi Toggle). Đường này bỏ qua `RequestClose`, nên cửa sổ đóng trong khi ván vẫn chạy.
   - Online: server tự đặt lá thay người chơi tới hết ván.
   - Offline: `LocalDuelHost.Tick` chỉ chạy trong `CardWindow.Update`, nên ván đứng hình. Mở lại thì ván chạy tiếp.
   - Fix: Close ở `UiScreens` gọi `RequestClose`. Hoặc gọi `DuelClient.Tick` từ một chỗ luôn chạy, như `RpgSession` / `WindowsHost`.
2. **`LocalDuelHost.Tick` không xử thua khi sang map có quái** (`LocalDuelHost.cs:70-76`), trong khi zone có xử (`DuelService.cs:148`).
   - Biến `run` là static nên còn sống qua lần đổi nhân vật offline. Lúc đó `Settle` trả thưởng vào `PlayerState` cũ (mất thưởng, không dupe). `DuelClient.View` cũng chỉ bị reset khi kết nối mạng thay đổi.
   - Fix: thêm kiểm `MapRegistry.Current.Hostile`. Reset `run` và `DuelClient` khi `RpgSession` tạo nhân vật mới.
3. **Thắng mà không có thưởng thì `npcWins++` không được lưu ngay và không đẩy RpgState** (`DuelService.cs:162`). Đó là ván thắng thứ 1, 2, và 4 trở đi trong ngày.
   - Chỉ chờ autosave. Nếu zone sập thì mất bộ đếm, người chơi phải thắng thêm, không lợi.
   - Fix: luôn `RequestSave` khi `Settle` trả true và kết quả là Win.
4. **Thưởng lần đầu bị cắt ở `GoldCap` nhưng bit vẫn được set** (`DuelRewards.cs:195-203`). Khác cách Album / Sell đã sửa.
   - Fix: nếu vượt cap thì chưa set bit, để lần thắng sau trả. Hoặc báo "Gold full".
5. **Điều kiện "không đánh trong 5 s" chỉ kiểm bị trúng đòn** (`MsSinceHurt`), không kiểm mình vừa tấn công. Spec yêu cầu cả hai.
6. **Trade với người đang đấu: request của họ bị bỏ im lặng** (`ZoneServer.cs:185`), không có notice.
   - Người khác vẫn mời họ được. Lời mời treo tới khi hết hạn.
   - Fix: `TradeBook` coi `IsDueling` là bận ở phía người nhận, và trả `TradeFail`.
7. **Mỗi lần SaveDeck đều `PushState` cả RpgState.** Với 10 request/s là 10 gói RpgState/s cho một client. Cùng mẫu với InvOp nên chấp nhận được; có thể giới hạn chỉ push khi deck thực sự đổi.
8. **GC mỗi frame khi đang đấu:** `timerText.text = CeilToInt().ToString()` (`CardDuelBattleView.cs:43`) và `RefreshGold()` (`CardWindow.cs:189`) tạo chuỗi và rebuild Text mỗi frame. Fix: chỉ gán khi số đổi.
9. `DuelService.cs:136` và `LocalDuelHost.cs:29` phân loại lỗi bằng so chuỗi `r.Error == "Not your turn"`, dễ gãy khi đổi chữ. Nên thêm mã lỗi vào `DuelPlay`.
10. Comment `DuelNpcRun.cs:6` vẫn ghi "owned and unlocked", sai với nghĩa khoá mới.
11. `host.ClockMs` là float, đã có từ trước, dùng chung với Trade. Zone chạy nhiều tuần thì độ phân giải ms giảm, nhưng 20 s / 0,6 s vẫn ổn. Nên ghi nhớ cho PvP phase 06.

## Edge case đã rà, không lỗi
- Bấm Place hai lần: lần 2 bị trả "Not your turn" và server gửi lại state.
- Ván xong mới Place: trả `NoMatch`.
- Bấm Play liên tục: `MinStartGapMs` + `InDuel` chặn.
- Rời mạng giữa ván: `OnLeave` → Forfeit → `Settle` thua. `duel.OnLeave` chạy trước `persistence.OnLeave`.
- Đang trade thì không mở được trận (`IsTrading`).
- SaveDeck với byte card không hợp lệ: `ByIndex` null nên bị từ chối. Deck trong save bị sửa tay (quá boss / SR+) cũng bị `Start` chặn.
- NPC hết nước: Forfeit NPC, ván không treo.

## Việc nên làm
1. M1: thêm `Turn` vào Place trước khi chốt protocol 30. Phase 06 PvP cần cái này.
2. L1 / L2: đóng cửa sổ phải đi qua `RequestClose`; tick offline độc lập với cửa sổ, có kiểm map có quái.
3. L3 / L4: save khi thắng; thưởng lần đầu khi Gold sát cap.
4. L8: cache chuỗi đồng hồ / Gold.
5. Còn lại khi rảnh.

## Câu hỏi còn mở
- Có cần đúng spec "không tấn công trong 5 s" không, hay chỉ "không bị đánh" là đủ?
- Thắng lần đầu khi Gold sát cap: giữ bit để trả lần sau, hay chấp nhận mất?

**Status:** DONE_WITH_CONCERNS
**Summary:** Luật ván, quyền server, thưởng một lần, khoá theo loại đều đúng. Đã sửa hết review phase 03. Còn rủi ro đặt nhầm lá do Place không có số lượt (nên sửa trước PvP), và vài lỗ nhỏ ở đường đóng cửa sổ / offline tick / lưu bộ đếm ngày.
