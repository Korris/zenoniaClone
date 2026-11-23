# Phase 06: PvP thách đấu + cược card (Card Wager, escrow ở server-api)

## Context Links
- Spec: `design-notes-chatgpt.md` mục "Cược card (Card Wager)" + thưởng PvP
- Giao dịch nguyên tử có sẵn: `server-api/src/services/character-service.js` (`commitTrade`, collection `ledger`), `server-api/src/rpg/trade-move.js`, `server-api/src/routes/internal-routes.js`, `Net/Server/ZonePersistence.cs`, `Net/Server/ZoneApiHttp.cs`
- Menu bấm người chơi: `Assets/Scripts/PartyPlayerMenu.cs`; lời mời: `TradeRequestWindow.cs`
- Luật UI: `docs/ui-layout-rules.md` (đọc hết); asset phase 02 (G cược)

## Overview
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
- [ ] API escrow / settle / refund + vitest   - [ ] redeem createdAt
- [ ] WagerRules + test   - [ ] DuelService.Pvp / Wager + messages
- [ ] UI mời + panel cược   - [ ] Thưởng PvP / ngày   - [ ] Build + deploy + thử 3 kịch bản lỗi

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
