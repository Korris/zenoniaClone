# Phase 05: Duel core (luật 4x4, deck), đấu NPC, UI deck + bàn đấu

## Context Links
- Spec: `design-notes-chatgpt.md` mục "Đấu card"; mockup `06a-card-duel-deck.png`, `06b-card-duel-battle.png` (log / tỉ số trong 06b SAI, lấy logic từ luật)
- Luật UI: `docs/ui-layout-rules.md` (đọc hết); asset phase 02 (F bàn đấu, C card)
- Mẫu wire + sổ thuần: `Net/Protocol/TradeBook.cs`, `TradeMessages.cs`, `Net/Server/TradeService.cs`, `Net/Client/NetClient.Trade.cs`, `TradeWindow.cs`
- Opcode: `Net/Protocol/NetOpcodes.cs`; tick: `Net/Server/ZoneServer.cs`

## CẬP NHẬT 23/11 chiều (v2)
Đấu NPC BỎ HẲN theo quyết định người dùng: xoá `DuelNpcAi / DuelNpcRun / DuelNpcSession / DuelNpcTable / DuelRewards / LocalDuelHost`, thưởng thắng lần đầu + nhiệm vụ 500 G, nút Play NPC, test liên quan (chân dung NPC giữ làm asset, chưa dùng). Card Duel chỉ chạy online; offline trang Card Duel hiện "Card Duel is offline" kèm lý do. Các mục "NPC", "offline đấu NPC", "thưởng" bên dưới là LỊCH SỬ. Luật ván 4x4, deck, đồng hồ vẫn nguyên (`DuelMatch`, `DeckRules`). Trường lưu `cardNpcFirstWins`, `cardDaily.npcWins / pvpRewards` giữ trong save cho tương thích (không còn dùng).

## Overview
P1, pending. Toàn bộ luật ván đấu thuần C#, server chạy ván + đồng hồ; NPC luyện (3 NPC), thưởng thắng lần đầu + nhiệm vụ ngày; trang Card Duel (deck) trong `CardWindow` và cửa sổ bàn đấu riêng.

## Key Insights
- `ZenoniaProto.Net` (Protocol) không tham chiếu Rpg -> luật ván ở `Rpg/Cards/` (test `ZenoniaSkills.Tests`), Protocol chỉ có message byte (test round-trip `ZenoniaNet.Tests`), `DuelService` ở `Net/Server` (Assembly-CSharp, thấy cả hai) như `TradeService`.
- Ván xác định hoàn toàn từ seed + nước đi -> test được, replay được, log gọn.
- Offline vẫn đấu NPC được: client chạy cùng `DuelMatch` cục bộ (như bag offline), PvP / Ranked chỉ online.
- Zone process host mọi map (`World/ZoneHost`) -> `DuelService` một instance cho cả process.

## Requirements
- Deck 8 lá từ card sở hữu (trùng id được tới số đang có), tối đa 1 boss + 2 SR/SSR không-boss (câu hỏi 7). Lưu `cardDeck`; lá trong deck có tag In Deck, bị chặn bán / đổi / cược nếu làm số còn lại < số trong deck.
- Bàn 4x4 (A-D x 1-4). Server xáo deck, mỗi bên tay 5 lá, đặt xong rút bù (câu hỏi 8). Người đi trước ngẫu nhiên. 20 s / lượt; hết giờ server đặt lá ngẫu nhiên vào ô trống ngẫu nhiên.
- Đặt lá: với mỗi lá địch kề 4 hướng, số cạnh mình > số cạnh đối diện -> lật. Không chuỗi, không ngẫu nhiên, không combo. Đầy 16 ô: 9+ thắng, 8-8 hoà.
- NPC AI (server): Easy chọn ngẫu nhiên trong 3 nước tốt nhất; Normal / Hard tham lam số lật, hoà thì chọn nước ít cạnh yếu lộ ra; Hard có deck mạnh hơn.
- Thưởng NPC: thắng lần đầu mỗi NPC (1.000 / 2.500 / 5.000 G đề xuất) + nhiệm vụ ngày "thắng 3 ván NPC" = 500 G (đếm `cardDaily`, ngày theo `WantedBoss.DayKey`). Thắng NPC lần đầu bất kỳ = xong tutorial (Ranked dùng).
- Chỉ mở trận ở map an toàn (`IGameMap.Hostile == false`), không chết, không trade, không trong trận khác, không trúng đòn / đánh trong 5 s. Hero đứng yên khi cửa sổ đấu mở.
- Rời mạng giữa ván NPC: huỷ ván, không thưởng.
- Constraints UI: chỉ asset phase 02, đọc hết `ui-layout-rules.md`, chụp so mockup 06a / 06b.

## Architecture
```
Assets/Scripts/Rpg/Cards/DeckRules.cs     Validate(deck, owned) -> lỗi; Reserved(id)
Assets/Scripts/Rpg/Cards/DuelBoard.cs     16 ô, Place -> danh sách ô lật, Score
Assets/Scripts/Rpg/Cards/DuelMatch.cs     state machine: Create(seed, deckA, deckB, nowMs), Place(side, hand, cell, nowMs), Tick(nowMs), Result
Assets/Scripts/Rpg/Cards/DuelNpcAi.cs     Pick(match, side, level, rand)
Assets/Scripts/Rpg/Cards/DuelNpcTable.cs  3 NPC: id, tên, deck, độ khó, thưởng
Assets/Scripts/Net/Protocol/DuelMessages.cs  DuelRequestMsg, DuelNoticeMsg, DuelStateMsg (Write/Read)
Assets/Scripts/Net/Server/DuelService.cs     phiên đấu theo PlayerId, Tick từ ZoneServer, thưởng, save
Assets/Scripts/Net/Client/NetClient.Duel.cs  gửi / nhận, DuelClientState
Assets/Scripts/LocalDuelHost.cs              offline: cùng API như DuelService cho NPC
Assets/Scripts/CardWindow.Duel.cs            trang deck (06a)
Assets/Scripts/CardDuelWindow.cs (+ .Board.cs, .Hand.cs, .Log.cs)  bàn đấu (06b)
```
Wire: `NetOp.DuelRequest = 7` (C->S: SaveDeck(8 id), PlayNpc(npc), Place(hand, cell), Forfeit), `NetOp.DuelNotice = 26`, `NetOp.DuelState = 27` (S->C: 16 ô {cardIdx, chủ}, tay của mình, số lá còn của địch, tỉ số, lượt k/16, ai đi, deadline ms, ô vừa lật, kết quả). `NetProtocol.Version` +1. Kênh reliable 1.
Luồng: client PlayNpc -> `DuelService` kiểm điều kiện + `DeckRules` -> `DuelMatch.Create` -> DuelState -> client Place -> kiểm lượt / ô / lá -> lật -> NPC đi (trễ 0,6-1,2 s cho tự nhiên) -> ... -> Result -> thưởng `PlayerState` -> `RpgState` + `RequestSave`.

## Related Code Files
Tạo: file ở Architecture; `Tests/dotnet/ZenoniaSkills.Tests/{DeckRulesTests,DuelBoardTests,DuelMatchTests,DuelNpcAiTests}.cs`; `Tests/dotnet/ZenoniaNet.Tests/DuelMessageRoundTripTests.cs`.
Sửa: `Net/Protocol/NetOpcodes.cs`, `Net/Server/ZoneServer.cs` (route opcode, tick, OnJoin/OnLeave), `Net/Client/NetClient.cs` (route), `Rpg/Cards/CardOps.cs` (Sell / Convert tôn trọng `DeckRules.Reserved`), `CardWindow.cs` (bật mục Card Duel + Add to Deck), `UiWindows`/`UiScreens.cs` nếu cần modal mới.

## Implementation Steps
1. `DuelBoard` + test bảng chân lý: lật 0-4 hướng, cạnh bằng không lật, mép bàn, lá mình không lật.
2. `DuelMatch` + test: lượt xen kẽ, sai lượt / ô đầy / lá không trong tay bị từ chối, timeout đặt ngẫu nhiên theo seed, 16 nước -> kết quả 9+/8-8, cùng seed cùng kết quả.
3. `DeckRules` + test (boss / SR+ / sở hữu / Reserved).
4. `DuelNpcAi`, `DuelNpcTable` + test (AI không bao giờ đi nước sai; Hard thắng Easy > 70 % qua 500 ván seed).
5. `DuelMessages` + round-trip test; Version bump.
6. `DuelService` (+ điều kiện map an toàn, combat 5 s, trade) và `LocalDuelHost`.
7. UI deck 06a rồi bàn 06b (lật = animation scale X trên asset, đổi viền xanh / đỏ; đồng hồ = fill của asset vòng).
8. Build zone từ bản sao, thử 2 client: đấu NPC, rớt mạng giữa ván, hết giờ.

## Todo List
- [ ] DuelBoard / DuelMatch / DeckRules / NpcAi + test   - [ ] DuelMessages + round-trip
- [ ] DuelService + LocalDuelHost   - [ ] Trang deck 06a   - [ ] Bàn đấu 06b
- [ ] Thưởng lần đầu + nhiệm vụ ngày   - [ ] Build zone + thử online / offline   - [ ] Ảnh so mockup

## Success Criteria
- Mọi test luật xanh; ván 16 nước luôn kết thúc, không treo khi client im lặng (timeout tự đặt).
- Online: đấu NPC trọn ván, tỉ số trên bàn khớp số ô mỗi màu, thưởng lần đầu chỉ 1 lần qua relog.
- Không mở được trận ở map có quái / khi đang trade.

## Risk Assessment
| Rủi ro | KxT | Giảm thiểu |
|---|---|---|
| Client và server lệch kết quả lật | TB x TB | client chỉ vẽ DuelState, không tự tính (offline dùng cùng DuelMatch) |
| Đồng hồ trôi do tick zone | Thấp x TB | deadline tuyệt đối ms, Tick so `nowMs` |
| Deck khoá card làm người chơi tưởng mất card | TB x Thấp | tag In Deck + thông báo khi bị từ chối |
| `ZoneServer.cs` (519 dòng) phình | TB x Thấp | logic ở `DuelService`, ZoneServer chỉ route |

Rollback: tắt mục Card Duel + từ chối opcode bằng `DuelService.Enabled`; dữ liệu deck vô hại.

## Security Considerations
Client chỉ gửi chỉ số tay + ô; server kiểm lượt, sở hữu, deadline. Không gửi tay của địch xuống client. Rate limit DuelRequest (như trade: bỏ qua > 10 / s).

## Next Steps
Phase 06 PvP + cược dùng lại `DuelMatch` / `DuelService`. Phase 08 gắn NPC Card Master mở trang Duel.

## Trạng thái 23/11: XONG code + UI offline (chưa deploy zone, chưa kiểm online 2 client)
Todo: [x] DuelBoard / DuelMatch / DeckRules / NpcAi + test  [x] DuelMessages + round-trip  [x] DuelService + LocalDuelHost  [x] Trang deck 06a  [x] Bàn đấu 06b  [x] Thưởng lần đầu + nhiệm vụ ngày  [ ] Build zone + thử online (chưa)  [x] Ảnh (`docs/reference/card-gem-ui/phase-05-shots/`)

## Làm khác kế hoạch
- Thêm `DuelView` (Rpg): board, tỉ số, số lá còn, log ĐỀU suy ra từ danh sách nước đi (FlipMask do server quyết); wire chỉ gửi nước đi + tay của mình, không gửi ô / tỉ số / log. Thêm `DuelNpcSession` + `DuelNpcRun` (Rpg, dùng chung zone và offline: kiểm deck, NPC nghĩ 0,6-1,2 s, timeout, trả thưởng một lần). Thêm `DuelClient` (cửa chung online / offline), `DuelWire`, `CardDuelBoardLayout`.
- Bàn đấu là một màn của CardWindow (che menu trái), không phải cửa sổ riêng: dùng chung khung, vàng, hộp xác nhận, Esc. Esc / X khi đang đấu hỏi "Leave the duel?" (= thua, không thưởng).
- Deck lưu ngay mỗi lần thêm / bớt (`SaveDeck` cả deck, server kiểm `DeckRules`); không có nút Save Deck / Ready (Ready để PvP). Thêm = bấm card bên trái hoặc nút Add to Deck ở My Cards; bớt = bấm card trong deck. Clear Deck thay Save Deck. Challenge Player xám (phase 06).
- Bàn: bấm card trong tay rồi bấm ô trống để đặt (không có nút Place Card). Lật là animation scale X 0,4 s, đổi viền xanh / đỏ lúc card nằm cạnh.
- Mở trận ở map an toàn, không chết, không trade, không trúng đòn 5 s; zone ép thua nếu đang đấu mà chết / sang map có quái; trade bị bỏ qua khi đang đấu. `DuelService.Enabled` là công tắc.
- Wire: `NetOp.DuelRequest = 7`, `DuelNotice = 26`, `DuelState = 27`, `NetProtocol.Version` 30 (gộp "Sell all spare": `CardSell` Slot 0 = mọi lá thừa).
- Khoá card đổi nghĩa (người dùng chốt 23/11): cờ THEO LOẠI card, chỉ chặn Sell / Convert / cược; card khoá VẪN vào deck. `CardEntry.locked` = 0 / 1 (zod max 1, `card-wager-move.js` spare = 0 nếu khoá, else n - deck). `Spare` = 0 nếu khoá, else Count - InDeck.
- Chân dung người chơi (vòng avatar) không có: kit không có mặt hero; chỉ có chân dung NPC.
