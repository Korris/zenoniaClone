# Phase 04: Chuyển hoá Soul Gem, khảm, tháo ngọc, chỉ số option

## Context Links
- Mockup: `docs/reference/card-gem-ui/03-card-to-gem.png`, `04-gem-socket.png`, `05-gem-remove.png`; góp ý người dùng: không bảng Current/After, option ở ô "Gem Info -> Option" riêng
- Luật UI: `docs/ui-layout-rules.md` (đọc hết); asset phase 02 (E ngọc, A, B)
- Chỉ số: `Assets/Scripts/Rpg/PlayerState.cs` (`RecomputeStats`), `Rpg/Enhance.cs` (`EnhanceStats.Effective`)
- Giao dịch: `Assets/Scripts/Net/Protocol/TradeBook.cs`, `server-api/src/rpg/trade-move.js`
- Bán / bỏ đồ: `Assets/Scripts/BagActions.cs`, `World/PlayerRpg.cs` (`Discard`)

## Overview
P1, **code + test xong 23/11; UI 3 trang dựng bằng asset ChatGPT phase 02, đã chụp trong game (offline); CHƯA kiểm online, CHƯA nối vào CardWindow (phase 03)**. 3 trang của `CardWindow`: Convert, Gem Socketing, Gem Removal; option ngọc cộng vào chỉ số nhân vật; khoá giao dịch vĩnh viễn sau lần khảm đầu.

## Key Insights
- Phí đổi N 500 / R 2.000 / SR 8.000 / SSR 25.000 G, 100 %, mất 1 card; phí tháo N 200 / R 800 / SR 3.000 / SSR 10.000 G, giữ ngọc (vẫn khoá). Khảm miễn phí 100 %.
- Ô: vũ khí 2, armor 2, dây chuyền (`helmet`) 1, còn lại 0 (câu hỏi 4 plan.md). Không tăng theo tier.
- Ngọc đã khảm sống trong `item.sockets` -> đi theo món đồ. Lối ra hiện có là Trade P2P, nên **món có ngọc bị chặn trade** (TradeBook + `trade-move.js`), nếu không ngọc khoá lọt sang người khác. Bỏ / bán món có ngọc cũng bị chặn (phải tháo trước) để không mất ngọc ngoài ý muốn.
- Option là cộng phẳng riêng, KHÔNG nhân enhance / tier; hiện riêng trong chi tiết đồ ("Gem Option").

## Requirements
- Convert: chọn card (không đang trong deck / cược), chọn Power hoặc Guard, xem phí, Convert -> ngọc `locked=false`.
- Socket: danh sách trang bị Equip / Bag, mỗi món hiện số ô; chọn ô trống + ngọc (lọc All/ATK/DEF/HP/Utility) -> Socket Gem -> ngọc vào `sockets`, từ nay khoá.
- Remove: chọn ô có ngọc, hiện phí theo rarity + ô đỏ "Trade Locked" -> Remove Gem -> ngọc về kho `locked=true`.
- Không cho 2 ngọc cùng card cùng kind trên 1 món (tránh chồng một option; đề xuất, có thể bỏ).
- Option cộng ngay vào Atk / Def / MaxHp / MaxMp / Crit / Speed (vẫn chịu cap tốc độ `ItemDefs.MaxSpeedBonus`).
- Constraints UI: chỉ dùng asset phase 02, đọc hết `ui-layout-rules.md`, chụp so mockup.

## Architecture
```
Assets/Scripts/Rpg/Cards/CardFees.cs     bảng phí đổi / tháo theo rarity
Assets/Scripts/Rpg/Cards/GemSockets.cs   SlotCount(slot), Parse("id:p"), Option(gem) -> ItemStats, Sum(item)
Assets/Scripts/Rpg/Cards/CardOps.cs      + Convert, Socket, Remove (thuần, trả ActionResult)
Assets/Scripts/CardWindow.Convert.cs / CardWindow.Socket.cs / CardWindow.Remove.cs  (Socket + Remove dùng chung khung danh sách đồ: CardWindow.GearList.cs)
```
Ops: `InvOp.GemConvert = 32` (ItemUid = cardId, Slot = 0 Power / 1 Guard), `GemSocket = 33` (ItemUid = "itemUid>gemId:p", Slot = ô), `GemRemove = 34` (ItemUid = itemUid, Slot = ô). Server: `ServerInventoryActions` -> `PlayerRpg.ApplyInvAction` -> `CardOps` -> `RecomputeStats` -> `RpgState` + `RequestSave` (tiền lớn). Log server mỗi lần đổi / khảm / tháo (spec chống RMT).
`RecomputeStats`: cộng `GemSockets.Sum(item)` của đồ đang mặc vào atkAdd/defAdd/... sau `EnhanceStats.Effective`.

## Related Code Files
Tạo: file ở Architecture; `Tests/dotnet/ZenoniaSkills.Tests/{CardFeesTests,GemSocketTests,GemStatTests}.cs`; `server-api/test/gem-trade-lock.test.js`.
Sửa: `Rpg/PlayerState.cs` (RecomputeStats), `Rpg/ItemInfo.cs` (dòng Gem Option trong chi tiết đồ), `World/PlayerRpg.cs` (switch op + chặn Discard món có ngọc), `Net/Protocol/NetOpcodes.cs` (op + Version), `Net/Protocol/TradeBook.cs` (từ chối offer món có `sockets`, chặn op mới khi trade), `server-api/src/rpg/trade-move.js` (refuse `trade_socketed`), `server-api/src/rpg/item-validation.js` (số ô <= `SlotCount(slot)`; cần `GEM_SLOTS` trong `rpg-constants.js`).

## Implementation Steps
1. Đọc hết `ui-layout-rules.md`; kiểm asset E.
2. `CardFees`, `GemSockets`, `CardOps.Convert/Socket/Remove` + test: thiếu Gold, card đang khoá, ô sai, ô đầy, món không có ô, tháo trả locked, đổi xong album vẫn giữ.
3. `RecomputeStats` cộng option + test chỉ số (enhance không nhân option).
4. Chặn trade / discard / bán món có ngọc (C# + node) + test.
5. 3 trang UI; chi tiết đồ trong túi hiện "Gem Option".
6. zod: `sockets` theo slot. Build zone + deploy API (**server-api TRƯỚC, zone SAU**, `docs/deploy.md`).
7. Chụp so mockup 03/04/05.

## Todo List
- [x] CardFees / GemSockets / CardOps + test (`GemSocketTests`, `GemStatTests`, `CardLockAndOverflowTests`)   - [x] RecomputeStats + test
- [x] Chặn trade / discard món có ngọc (C# `TradeBook` + `TradeFail.Socketed`, node `trade_socketed`, `PlayerState.Discard`) + test
- [x] 3 trang UI (`GemConvertPage` / `GemSocketPage` / `GemRemovePage`, host tạm `GemWindow`, phím thử F8)   - [x] ItemInfo Gem Option (`ItemInfo.GemOptionLine`, chi tiết đồ trong `ArmoryWindow`)
- [x] zod (`GEM_SLOTS`) + build player từ bản sao OK   - [ ] Build zone + deploy API (người dùng; API trước, zone sau)
- [x] Ảnh chụp so mockup: `docs/reference/card-gem-ui/phase-04-shots/{convert,socket,remove}.png` (offline, 1280x720)
- [x] Chân dung hiện trong card / ngọc (chụp lại)
- [x] Nối 3 trang vào `CardWindow` (host các `GemPage`, đã xoá `GemWindow` + phím F8)
- [ ] Kiểm online (client + zone thật): đổi / khảm / tháo, relog giữ ngọc, thử trade món có ngọc

## Làm khác kế hoạch
- `InvOp.GemSocket`: ItemUid = uid món, Slot = `GemSockets.Code` (chỉ số card x 2 + kind, 0-79); ngọc vào ô trống kế tiếp. Lý do: cột uid trên dây tối đa 40 ký tự, "itemUid>gemId:p" có thể vượt. `GemConvert` ItemUid = card id, Slot 0 Power / 1 Guard; `GemRemove` ItemUid = uid, Slot = ô. Số 30 / 31 giữ cho `AlbumClaim` / `CardSell` (phase 03). `InvOpInfo.Last` = `GemRemove`. `NetProtocol.Version` 28.
- Ngọc đã tháo (locked) khảm lại được, dùng stack locked trước. Không cho 2 ngọc cùng card + kind trên 1 món (đề xuất giữ lại).
- Trade: `TradeItemView.Sockets` (1 byte trên dây) + `TradeFail.Socketed = 32`; chặn lúc thêm vào offer và lúc chốt. Node `takeItems` trả 422 `trade_socketed`.
- Quá giới hạn ô theo slot bị cắt ở `ItemFactory.SanitizeSockets` (C#) và từ chối ở zod (`GEM_SLOTS`: weapon 2, armor 2, helmet 1, còn lại 0).
- Card đang khoá (`CardEntry.locked`) hoặc nằm trong deck không đổi được (`CardCollection.Spare`).
- UI: lọc chip ATK / DEF / HP / Utility; 3 dropdown Rarity / Element / Area của mockup 03 CHƯA làm (My Cards phase 03 làm chung). Tab Bag chỉ liệt kê món có ô (vũ khí, giáp, dây chuyền).
- Fix chung: `PaperArt.Get` tìm border cho mảnh `cards/...` bằng khoá không có tiền tố `cards/` (tool cắt ghi borders kiểu `frame/btn-teal`); `PaperArt.Borders.cs` sinh lại từ `paper-borders.json`.

## Sửa theo review 23/11 (`reports/code-reviewer-261123-1225-phase-04.md`)
- M1: `GemRemove` Slot = `GemSockets.Code` của ngọc (không phải chỉ số ô), server `CardOps.RemoveGem`; nút 3 trang khoá tới khi RpgState về (`GemPage.Send` / `Pending`).
- M2: hộp xác nhận `CardConfirmBox` cho Convert SR+ và Socket. Low 1: GemConvert từ chối slot > 1. Low 2: trần dòng ngọc 160 (C# `MaxGemRows` + zod `GEM_MAX_ROWS`). Low 3: log gem chỉ khi ok, lọc ký tự điều khiển. Low 4: `addItem` ép `sockets: []`. Low 6: Gold ở trần thì lá thừa bị bỏ + toast. Low 7: option ngọc phẳng, không nhân Second.*Pct / pet passive (test `GemStatTests`). Low 5 (offline SaveNow) chưa làm.

## UI còn chờ art (phase 02)
- Chân dung 40 quái `Sprites/monsters/card-portraits/<id>.png`: đã có (phase 02 giao 23/11), ảnh chụp mới đã hiện chân dung (vẽ ĐÈ LÊN khung vì cửa sổ chân dung của khung / vỏ ngọc là màu kem đặc).
- Icon element "Poison" (Bog Slime, Mire Toad, ... hệ Poison) chưa có trong kit: hiện không icon. Hệ "Shadow" dùng `element-dark`.
- Icon HUD "Cards" ở cột More (`hud-icon-cards.png`) thuộc phase 03.

## Success Criteria
- Đổi N trừ đúng 500 G + 1 card, thêm 1 ngọc; tháo trừ 200 G, ngọc về `locked` (test).
- Khảm ATK +6 làm Atk tăng đúng 6 và sát thương online tăng tương ứng; tháo giảm lại.
- Món có ngọc: Trade P2P bị từ chối cả ở zone lẫn API; Discard bị từ chối.
- Relog giữ ngọc trong ô và option.

## Risk Assessment
| Rủi ro | KxT | Giảm thiểu |
|---|---|---|
| Ngọc khoá lọt qua trade món đồ | TB x Cao | chặn 2 tầng (TradeBook + trade-move.js), test |
| Option làm lệch cân bằng (Power SSR +32 ATK) | TB x TB | số trong `GemSockets`, chỉnh không migration |
| Mất ngọc khi bán / bỏ món | TB x TB | chặn, bắt tháo trước |
| Tràn 60 KB do sockets | Thấp x Thấp | chuỗi ngắn, <= 5 ô toàn bộ đồ mặc + túi 50 x 2 |

Rollback: tắt 3 op bằng cờ `CardOps.GemsEnabled`; ngọc đã khảm giữ nguyên, option có thể tắt trong RecomputeStats.

## Security Considerations
Server kiểm: sở hữu card / ngọc, đủ Gold, ô hợp lệ, không đang trade / đấu / cược. `locked` chỉ chuyển false -> true. Log ledger-style trên zone (charId, op, card, phí).

## Next Steps
Phase 05 (deck khoá card khỏi đổi / bán). Phase 06 (card cược bị khoá).
