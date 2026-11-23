# Review phase 03 + sửa review phase 04 (81c474e4, 2ce4f351)

Phạm vi: CardWindow / CardMyCardsPage / CardAlbumPage / CardDetailPanel / CardPageWidgets / CardConfirmBox, `GemPage` pending + confirm, InvOp 30/31/35, `CardOps.Sell/SetLock/RemoveGem`, `AlbumRules.Claim`, NetProtocol 29, log gem, `PlayerState.RecomputeStats`, zod/JS. Chỉ đọc.
Test (cây làm việc có thay đổi dở phase 05/06): ZenoniaSkills 642/642 pass; ZenoniaNet 499/501 (2 FrostHollowLargeMapTests fail sẵn). Không chạy vitest.

## Tổng quan
Phía server chắc, không thấy dupe / mất card / tạo Gold:
- `AlbumRules.Claim`: kiểm milestone 0-3, bit đã nhận, `AlbumCount` (bit album vĩnh viễn, chỉ server set qua drop). Nhận 2 lần bị từ chối. zod `albumClaimed <= 15`.
- `CardOps.Sell`: `n<1` từ chối, `Spare` (trừ bản khoá + trong deck), Gold tính `long`, vượt `GoldCap` từ chối trước khi trừ. Slot là byte nên n <= 255, tổng tối đa 637.500 G: không tràn. `Remove` khi có đủ spare không bao giờ để `locks` lệch.
- `SetLock`: giá trị tuyệt đối (1 = khoá hết ngoài deck, 0 = mở) nên gửi lặp an toàn.
- Cả 3 op bị `BlocksInventoryOp` chặn khi đang trade, log chỉ lệnh ok + `LogSafe`, có `RequestSave`.
- NetProtocol 29: enum, `InvOpInfo.Last = CardLock`, round-trip test, Version, comment InvOp khớp. Flag CardDrop bit2 (skipped) có ở server, client, offline.
- Art: không thấy khung / nút vẽ bằng code. Chỉ có 2 lớp veil màu (`CardWindow.cs:96`, `CardConfirmBox.cs:28`) theo mẫu cũ. Silhouette Album là chân dung tô tối + `icon-unknown` ChatGPT. Asset `hud-icon-cards`, `btn-gold`, `icon-unknown` đều có.

Không có lỗi Critical / High.

## Kiểm lại review phase 04
| # | Kết quả |
|---|---|
| M1 GemRemove theo ô | Đã sửa: Slot = `GemSockets.Code`, `CardOps.RemoveGem` dùng `IndexOf`, gửi lặp thì trả "No gem". Có pending 1,5 s trên cả 3 nút. |
| M2 hộp xác nhận | Đã sửa cho Convert SR+ và Socket (`CardConfirmBox`). Remove vẫn không hỏi lại, xem L5. |
| L1 GemConvert slot>1 | Đã sửa (`PlayerRpg.cs:161`). |
| L2 160 dòng ngọc | Đã sửa (C# + JS). |
| L3 log | Đã sửa (`LogSafe`, chỉ lệnh ok). |
| L4 addItem sockets | Đã sửa (`sockets: []`). |
| L5 SaveNow offline | Chưa sửa: vẫn chờ autosave. Không dupe. |
| L6 Gold cap khi stack đầy | Đã sửa: bỏ qua bản dư, có toast "skipped". |
| L7 ngọc nhân Second / pet | Đã sửa: cộng phẳng sau mọi hệ số, kể cả `DamageAtk`. |

## Medium
1. **Lock và bản mới về: UI báo "đã khoá" trong khi vẫn bán được.**
   - `CardOps.cs:127` chỉ khoá các bản đang có. Bản rơi thêm sau đó vẫn mở.
   - UI chỉ xét `Locked>0`: lá vẫn hiện ổ khoá (`CardMyCardsPage` Refresh), nút ghi "Unlock Card" (`:163`), Sell vẫn bật.
   - Muốn khoá cả bản mới, người chơi phải Unlock rồi Lock. Trong lúc đó mọi bản đều bán được. Không phải exploit, nhưng dễ bán nhầm thứ tưởng đã khoá.
   - Fix (chỉ client): `Spare>0` thì nút là "Lock Card" và gửi 1 (server đã khoá hết phần ngoài deck). Chỉ khi `Spare==0 && Locked>0` mới là "Unlock" và gửi 0. Ổ khoá chỉ hiện khi `Spare==0`, hoặc thêm số "Locked n/N".
2. **Bản khoá không vào deck được** (đụng phase 05). `DeckRules.cs:26` (dở) tính `Count - Locked`, và `Spare` cũng coi "khoá" với "deck" là tách nhau. Người chơi khoá lá SSR để giữ thì không đưa được vào deck, phải mở khoá (lại bán được).
   - Cần chốt: khoá là "không bán / chuyển hoá / cược" (nên vẫn cho vào deck) hay "cất kho".
   - Nếu chọn cách đầu, sửa ở 3 chỗ: `Spare = Count - max(Locked, InDeck)` hoặc tương đương, `SetLocked` giới hạn ≤ Count, `DeckRules` không trừ Locked.
3. **Chồng chữ trên ảnh chụp.**
   - Thẻ card to ở `CardDetailPanel`: chữ tên (`GemCardViews.cs:33`, `h*0.835`) đè số hướng dưới, và tràn xuống dưới khung. Thấy rõ ở `my-cards.png` ("Frost Slime" dưới số 3) và `album-locked-...png` ("Dune Scarab" đè số 9).
   - Album: hàng tab thứ 2 kết thúc ở y 190 (`CardAlbumPage.cs:73`, 126 + 34 + 30), đúng chỗ lưới bắt đầu (`GridY = 190`). Gap = 0, viền chọn vàng (-4 px) sẽ đè lên tab. Cần gap ≥ 8 (ui-layout-rules mục 2).
   - My Cards: caption dưới card chỉ cách card hàng sau 1 px (PitchY 136 = 117 + 18).
4. `CardDetailPanel.cs:34/81`: ô "Owned" cao 40 px nhưng có thể có 3 dòng (Owned / Locked / In deck, cỡ 15 ≈ 54 px), đè tiêu đề "Duel Stats" ở y 200. Phase 03 chưa gặp vì deck tắt, phase 05 bật deck thì sẽ gặp (lá vừa khoá vừa nằm trong deck). Fix: gộp thành 1 dòng ("x4 · khoá 2 · deck 1"), hoặc nới ô và đẩy Duel Stats xuống.

## Low
1. `AlbumRules.cs:25`: nhận mốc 10 khi Gold sát cap thì vẫn đánh dấu đã nhận, nhưng 5.000 G bị cắt mất. Sell thì lại từ chối khi vượt cap: hai op xử lý khác nhau. Nên từ chối "Gold full" trước khi set bit.
2. `CardOps.SetLock(true)` khi mọi bản đều trong deck: `SetLocked(id,0)` vẫn trả Success "X locked" dù không khoá gì. Nên trả Fail "All copies are in the deck".
3. Pending (`GemPageParts.cs`) bị xoá khi `StateKey` đổi vì bất kỳ lý do gì (Gold từ chỗ khác, card rơi), không nhất thiết vì đã có trả lời. Đã có server idempotent / hộp confirm che nên không hại. Nếu muốn chặt thì gắn ReqId của RpgState.
4. `CardWindow.cs:186`: `StateKey` tạo chuỗi mới mỗi frame (40 card + 50 ô đồ + deck) nên sinh rác GC liên tục trên mobile. Nên dùng số revision của state mirror, hoặc chỉ tính khi nhận RpgState / sự kiện.
5. Gem Remove SSR mất 10.000 G nhưng không có hộp xác nhận (Sell 50 G lại có). Nên hỏi lại với SR+.
6. Cỡ chữ 13 (`U.TextSmall - 1`) ở nhãn thưởng mốc (`CardAlbumPage.cs:48`) và tab vùng: dưới mức 14 tối thiểu cho chữ phụ của ui-layout-rules. Bản dở cũng dùng 13 cho dòng `reason`.
7. `CardConfirmBox`: vùng chữ cao 122 px với cỡ 18, rộng 424. Câu hỏi Socket dài (tên item dài + 2 câu) có thể ra 5-6 dòng và tràn. Chưa có ảnh chụp hộp Socket / Convert, nên chụp kiểm.
8. Esc khi hộp confirm đang mở thì đóng cả cửa sổ, không chỉ huỷ hộp. `FitWindow` chỉ chạy lúc đổi trang, nên đổi độ phân giải lúc đang mở thì không co lại.
9. Bán từ bất kỳ đâu (không cần đứng gần NPC), dù nhãn ghi "to the shop". Đúng quyết định V1 (plan Q2), chỉ cần biết.
10. Test gọi thẳng `CardOps` / `AlbumRules`, chưa có test qua `PlayerRpg.ApplyInvAction` cho 3 op mới (dispatch Slot → n / milestone / lock).

## Ghi chú về bản dở (chưa commit, không chấm)
- "Sell all spare" dùng Slot 0 = mọi bản spare (`PlayerRpg.cs` dở). Đây là đổi hợp đồng wire. Cần sửa comment InvOp CardSell ở `NetOpcodes.cs` (đang ghi "Slot = copies") và thêm test. Logic đúng: stack > 255 vẫn vừa byte, vượt cap thì từ chối cả lệnh.

## Việc nên làm
1. M1: logic nút Lock theo `Spare` (client).
2. M2: chốt nghĩa của khoá so với deck trước khi phase 05 xong.
3. M3/M4: sửa chồng chữ thẻ to, gap tab ↔ lưới, ô Owned. Chụp lại.
4. L1, L2, L5 (ít việc).

## Câu hỏi còn mở
- Bản khoá có được vào deck không (M2)?
- Lock có nên tự áp cho bản rơi thêm sau này (khoá theo loại card) thay vì theo số bản?

**Status:** DONE_WITH_CONCERNS
**Summary:** Server phase 03 đúng luật: claim một lần, sell chặn bản khoá / deck / cap, lock idempotent, protocol 29 khớp. Review phase 04 đã sửa hết trừ L5 (SaveNow offline). Còn UX khoá với bản mới, nghĩa khoá so với deck, và vài chỗ chồng chữ / gap thấy trên ảnh chụp.
