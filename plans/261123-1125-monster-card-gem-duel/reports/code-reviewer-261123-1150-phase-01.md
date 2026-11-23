# Review phase 01: card data, drops, album, save v6 (284a4866 + 8ac6dd3c)

Phạm vi: 284a4866 (code C# + server-api + test). 8ac6dd3c chỉ sửa plan.md, bỏ qua.
Test: ZenoniaSkills 532/532 pass; ZenoniaNet 487/489 (2 FrostHollowLargeMapTests fail sẵn từ trước); vitest 220/220 pass.

## Tổng quan
Chắc tay. Zone giữ quyền: không có InvOp nào cho client thêm card/ngọc/socket. Mọi đường nạp save (API redeem, KeptPath, ClientRpgMirror) đi qua `SaveSerializer.Parse`, nên được `CardCollection.Load` + `SanitizeSockets` làm sạch. Save v5 thiếu field thì C# và zod đều về rỗng. Zod khai báo đủ 8 field mới + `sockets`, có vitest round-trip. Thứ tự 40 id được khoá bằng danh sách cứng (CardTableTests:12) và CardIdsExportTests. NetProtocol 27 khớp test round-trip. Event CardDrop chỉ tới chủ (`OwnerHero`). Không có lỗi Critical.

## High
1. **Thứ tự deploy có thể làm mất tiến độ.** `item-validation.js:35` (`version max 6`) và `card-state-schema.test.js:104` (gặp card lạ thì từ chối cả save). Nếu zone v27 lên trước server-api: API cũ (`max(5)`) từ chối MỌI save v6. Mỗi lần save lỗi, mọi tiến độ chỉ còn nằm trong KeptPath trên đĩa zone. Sau này thêm card mới (append) mà zone lên trước API thì cũng y như vậy.
   Fix: ghi luật "deploy server-api trước, zone sau" vào `docs/deploy.md` và vào bước 8 của phase. Có thể thêm log/alert khi save trả 400 hoặc 422 liên tục.

## Medium
2. **Shape save thiếu cờ "Lock Card" và trạng thái "đang trên chợ".** Design-notes:58 có nút Lock Card. Luật cược (dòng 74) cấm "card khoá / card đang trên chợ". `CardEntry {id,n}` (`CardSaveData.cs:14`) không lưu được số bản bị khoá. v6 chưa phát hành nên nên thêm ngay để chỉ bump một lần, đúng ý plan. Ví dụ `CardEntry.locked` (số bản khoá, ≤ n) hoặc `cardLocked: [id]`, kèm zod + vitest.
3. **Item có socket đi qua trade mà không bị kiểm tra.** `server-api/src/rpg/trade-move.js:9-17` (`takeItems`) chuyển nguyên item kèm `sockets`. Luật design (dòng 32, 68): ngọc đã khảm bị khoá giao dịch vĩnh viễn. Khi phase 04 bật khảm, đây là đường lách để chuyển ngọc khoá sang nhân vật khác. Phase 04 phải chặn trade item có `sockets.length > 0` (refuse `trade_socketed`), hoặc chốt rõ luật. Nên ghi vào phase-04 ngay.
4. **Chưa có gì canh `MaxSaveJsonLength` (60000).** `ServerReplication.cs:222` gọi `w.Put(SaveJson, Max)`. LiteNetLib cắt chuỗi im lặng, client parse JSON hỏng và mirror vỡ. v6 thêm tối đa khoảng 9-10 KB: 80 dòng ngọc × ~58 B, 40 card × ~33 B, `"sockets":[..2 id dài..]` × 58 item. Rủi ro hiện thấp nhưng không có test.
   Fix: test dựng save tệ nhất (50 bag + 8 equip, mỗi món 2 socket id dài nhất, 40 card, 80 ngọc, mọi quest/chest), serialize bằng System.Text.Json để ước lượng, assert < ~45000. Hoặc ở server: log lỗi khi `SaveJson.Length > MaxSaveJsonLength`, thay vì để bị cắt.

## Low
5. `AlbumRules.cs:144` `p.Gold += Gold10` không chặn trần. Gold gần 2e9 thì vượt `MAX_INT` của API, API từ chối save liên tục. Nên kẹp `Math.Min(int.MaxValue-ish / MAX_INT)` theo cách làm chung của gold, nếu đã có helper.
6. `CardCollection.Add` (`:191`) kẹp ở 999 nhưng `PlayerRpg.CardDrop` vẫn bắn `CardDropped`, nên toast hiện "Card: X" dù bản đó bị bỏ. Nên để `Add` trả thêm trạng thái "bị kẹp" và báo "stack full", hoặc ít nhất không toast/save. Tương tự `AddGem` kẹp im lặng ở 999.
7. `MaxGemRows = 80`, nhưng số tổ hợp (id, kind, locked) tối đa là 40×2×2 = 160 dòng. `AddGem` trả false khi đầy. Phase 04 (convert, tháo ngọc) phải kiểm tra false TRƯỚC khi trừ card hoặc gỡ socket, nếu không sẽ mất ngọc. Ghi chú vào phase-04.
8. `KillRewards.Resolve:20` return sớm khi `LootTable.For(type) == null`, nên card của quái không có loot def sẽ không bao giờ rơi. Hiện 5 boss đều có def. Nên thêm test thuần (LootTable thuộc asm Rpg): mọi `CardTable.All[i].Id` đều có `LootTable.For`.
9. Wanted: `KillRewards.cs:54` dùng `UtcNow` lúc giết. Boss giết ngay sau khi qua ngày sẽ mất card. Hành vi này khớp `WantedBossCredit` và spawner cũng huỷ boss cũ, nên chỉ cần biết, không cần sửa.

## Edge case đã rà, OK
- Load: id trùng gộp lại, n ≤ 0 bị bỏ, kind ngoài 0/1 bị bỏ, album index ngoài dải bị bỏ, bit claim giả bị gỡ, deck chỉ giữ card còn sở hữu và không quá số bản, counter âm bị kẹp về 0.
- Socket: regex và độ dài khớp giữa C# và zod. Hat/pet bị ép rỗng. `null` an toàn.
- Mỗi contributor roll riêng, ngưỡng ≥ 1 % máu, bỏ qua WasCaptured (return đầu Resolve). Event index -1 → byte 255 → `ByIndex` null, không toast.
- SR+/boss: ZonePersistence `RequestSave` ngay và log. Offline `SaveNow`. Client online không thể tự thả card (chỉ authority).
- `import ... with { type: 'json' }` cần Node ≥ 20.10. `engines >=20.12`: OK.

## Việc nên làm (theo thứ tự)
1. Ghi thứ tự deploy API → zone (High 1).
2. Thêm field lock card vào save v6 trước khi phát hành (M2).
3. Ghi vào phase-04: chặn trade item có socket (M3), kiểm tra `AddGem` false (L7).
4. Test kích thước save tệ nhất (M4), test card ↔ LootTable (L8).
5. Kẹp gold ở Album (L5), không toast khi stack đã đầy (L6).

## Câu hỏi còn mở
- Sát thương của turret mechanic và pet có vào `DamageBy` không? Nếu không, mechanic giết bằng turret có thể không đủ 1 % và mất lượt roll card (giống luật pet reward có sẵn).
- Card đầy 999: bỏ im lặng, hay tự đổi sang thứ khác (gold, ngọc)?

**Status:** DONE_WITH_CONCERNS
