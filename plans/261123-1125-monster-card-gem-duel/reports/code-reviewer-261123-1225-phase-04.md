# Review phase 04 + sửa review phase 01 (1c78c53d, b2fd78b4, 3d5b12d2)

Phạm vi: CardOps / GemSockets / CardFees / CardCollection lock, RecomputeStats, InvOp 32-34, TradeBook + trade-move.js, zod GEM_SLOTS, Discard, UI 3 trang (chỉ phần gửi op). Chỉ đọc, không sửa code.
Test (cây làm việc, có thay đổi dở của agent phase 03 như `CardOps.Sell/SetLock`, `CardSellAlbumClaimTests.cs`): ZenoniaSkills 568/568 pass; ZenoniaNet 492/494 (2 FrostHollowLargeMapTests fail sẵn); vitest 230/230 pass.

## Tổng quan
Chắc. Thứ tự "kiểm hết rồi mới đổi" đúng ở cả 3 op: Convert kiểm Spare, Gold, `CanAddGem` rồi mới trừ. Remove kiểm Gold, `CanAddGem(locked)` rồi mới gỡ ô. Socket gỡ ngọc khỏi kho chỉ khi đã qua hết kiểm tra ô. Không thấy đường nhân bản (dupe) hay làm mất card / ngọc / Gold.
- Phí khớp design-notes:31-33. Số ô: weapon 2, armor 2, helmet 1, còn lại 0. C# `SlotCount`, `SanitizeSockets` và zod `GEM_SLOTS` khớp nhau.
- Mã ngọc từ client: `TryDecode` dùng `ByIndex(code/2)` nên mọi byte 80-255 đều trả null và bị từ chối. Card id lạ ra `Code` = 254/255 và cũng bị từ chối.
- Khoá trade đủ cả 2 tầng:
  - TradeBook chặn lúc offer và lúc confirm.
  - `BlocksInventoryOp` chặn mọi InvOp gem trong phiên trade.
  - API `takeItems` trả `trade_socketed`. State dùng ở đây do zone gửi, không phải bản cũ trong DB.
  - Không trade được đồ đang mặc.
- Không có đường bán đồ cho NPC, kho đồ hay thư. Discard đã chặn ở cả server (`PlayerState.Discard`) lẫn client.
- Enhance phía zone chỉ áp phần chênh lệch (`OnEnhanceAnswer`), không nạp lại state từ API, nên không làm mất thao tác ngọc.
- NetProtocol 28: thêm `TradeItemView.Sockets` (1 byte) + `TradeFail.Socketed`, có round-trip test. `InvOpInfo.Last` = GemRemove. Hai số 30 / 31 rơi vào `default` ("Unknown action").
- RecomputeStats: option ngọc là số cộng phẳng, không nhân enhance. Có kẹp Hp / Mp khi tháo ngọc MaxHp.

Không có lỗi Critical / High.

## Kiểm lại review phase 01
| # | Kết quả |
|---|---|
| H1 deploy order | Đã sửa: `docs/deploy.md` + `SaveRejectWatch` báo động khi bị từ chối từ 3 lần trong 10 phút. |
| M2 lock card | Đã sửa: `CardEntry.locked` (C# + zod `locked<=n`). `Spare` trừ cả bản khoá lẫn bản trong deck. |
| M3 trade item có socket | Đã sửa (2 tầng, có test). |
| M4 kích thước save | Đã sửa: `CardSaveSizeTests` + log lỗi khi vượt `MaxSaveJsonLength`. |
| L5 trần Gold album | Đã sửa: `AddGoldCapped`. |
| L6 stack 999 | Đã sửa: `CardOps.Receive` bán lấy Gold, có toast "full". |
| L7 AddGem trả false | Đã sửa: có `CanAddGem` và được gọi trước. **Chưa sửa** phần trần 80 dòng (xem L2). |
| L8 card ↔ LootTable | Đã có test (`CardLockAndOverflowTests`). |

## Medium
1. **Tháo ngọc theo chỉ số ô: bấm 2 lần thì tháo nhầm ngọc thứ hai và mất phí oan.**
   - `CardOps.cs:88` (`RemoveAt(socket)`) dồn các ô còn lại lên. `GemRemovePage.cs:48` gửi `(byte)socket` mà không có cờ chờ.
   - Món có 2 ngọc, bấm "Remove" ô 0 hai lần trước khi RpgState về: lệnh thứ hai tháo luôn ngọc vốn ở ô 1 và trừ thêm phí (SSR mất 10.000 G).
   - Ngọc không mất, nhưng Gold mất, ngọc bị gỡ ngoài ý muốn, và lần khảm lại sẽ trúng ô khác.
   - Fix (gọn nhất): `GemRemove` đổi Slot = `GemSockets.Code` của ngọc cần tháo. Một món không bao giờ chứa 2 ngọc trùng (`Socket` đã chặn `Contains`), nên server tìm đúng chuỗi `Encode(card, kind)` rồi tháo. Lệnh lặp lại sẽ trả "No gem" nên an toàn khi gửi trùng. Nhớ sửa comment InvOp ở `NetOpcodes.cs`.
   - Thêm nữa: khoá nút (pending) đến khi RpgState về. `GemConvertPage.cs:98` cũng vậy: bấm 2 lần là đổi 2 card và trả 2 phí.
2. **Chưa có bước xác nhận cho thao tác mất tiền nhiều hoặc không đảo lại được.** Ví dụ Convert SSR 25.000 G, Socket thì khoá ngọc vĩnh viễn. Hiện chỉ có dòng ghi chú. Nên có hộp "Sure?" như Discard (`BagActions` "The view asks sure?"), ít nhất cho SR+ và cho lần khảm ngọc chưa khoá.

## Low
1. `PlayerRpg.cs:161`: `GemConvert` hiểu mọi Slot khác 1 (kể cả 2-255) là Power. Nên từ chối khi `slot > 1` cho chặt hợp đồng op.
2. `MaxGemRows = 80` (C#) và `GEM_MAX_ROWS = 80` (zod), trong khi tối đa có 40 × 2 × 2 = 160 tổ hợp. Kho đủ 80 dòng thì Remove và Convert bị từ chối. Không mất đồ, nhưng người chơi kẹt cho tới khi khảm bớt. Đề xuất nâng cả hai lên 160 lúc v6 còn chưa phát hành (CardSaveSizeTests vẫn đủ chỗ: ~58 B/dòng).
3. `ServerInventoryActions.cs:36`: log mọi lệnh gem, kể cả lệnh bị từ chối. Log chèn nguyên `m.ItemUid` của client (tối đa 40 ký tự, có thể chứa `\n`), và không giới hạn tần suất. Có thể dùng để spam log hoặc giả dòng log ledger. Nên lọc ký tự điều khiển, chỉ log lệnh ok, hoặc log lệnh bị từ chối theo kiểu có tần suất.
4. `inventory-ops.js:22` (`addItem`) nhận item có `sockets` khác rỗng. Route này nội bộ (service key) nên rủi ro thấp, nhưng nên chặn giống `enhance_not_allowed`: item được cấp phải có `sockets: []`. Ngọc chỉ được sinh ra qua save của zone.
5. Offline: sau Convert / Remove không gọi `SaveNow`, chỉ chờ autosave. Game crash ngay sau đó thì mất thao tác (lùi lại trạng thái trước, không dupe). Có thể thêm `SaveNow` cho op gem ở `PetActions.Answered` hoặc `RpgSession`.
6. `CardOps.Receive` khi Gold đã ở `GoldCap`: card bị bán ra 0 G một cách im lặng (`AddGoldCapped` cắt). Đây là ca biên cực hiếm, chỉ cần biết.
7. Option MaxHp / Def của ngọc vẫn nhân `Second.HpPct/DefPct` và pet passive (`PlayerState.cs:85-86`), giống đồ thường. Spec chỉ cấm nhân enhance / tier nên vẫn đúng spec. Cần chốt có chủ ý không, vì số trên UI "+X" sẽ lệch một chút so với chỉ số thật.

## Edge case đã rà, OK
- Socket vào gloves / boots / offhand / hat / pet: SlotCount 0, bị từ chối.
- Ô đầy hoặc ngọc trùng: bị từ chối trước khi trừ ngọc. Ngọc đã khoá được dùng trước, nên ngọc chưa khoá vẫn bán được.
- Load save: số bản khoá ≤ số bản đang có; deck ≤ Count − Locked; stack ngọc > 999 bị cắt; ô vượt giới hạn slot bị bỏ (C#), còn zod thì từ chối. Hai bên khác hành vi, nhưng zone luôn sanitize trước khi gửi.
- Trade: món được khảm sau khi đã offer bị bắt ở confirm. Gem op trong lúc Committing bị chặn vì `IsTrading`.
- Sửa đồ đang mặc: `GetEquipped(slot) == item` rồi RecomputeStats. Server và client mirror đều tính lại từ RpgState.
- Vá phase 01 L6: `CardDrop` event bit1 có ý nghĩa ở cả host lẫn client.

## Việc nên làm
1. M1: GemRemove nhận mã ngọc thay vì chỉ số ô, thêm khoá pending cho 3 nút (sửa NetOpcodes comment + test).
2. M2: hộp xác nhận cho Convert SR+ và Socket.
3. L1, L2, L3, L4 (ít việc).

## Câu hỏi còn mở
- Có giữ việc option ngọc nhân với `Second.*Pct` và pet passive (L7) không?
- Nâng trần dòng ngọc lên 160 ngay ở v6?

**Status:** DONE_WITH_CONCERNS
**Summary:** Phase 04 đúng luật, không có dupe / mất đồ, khoá trade đủ 2 tầng; review phase 01 đã sửa hết trừ trần 80 dòng ngọc. Cần sửa M1 (tháo theo chỉ số ô, bấm 2 lần mất phí) trước khi kiểm online.
