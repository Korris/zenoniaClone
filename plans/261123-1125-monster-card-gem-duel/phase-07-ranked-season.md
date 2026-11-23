# Phase 07: Ranked (mùa 30 ngày, 5 bậc, MMR ẩn)

## Context Links
- Spec: `design-notes-chatgpt.md` mục "Ranked (ChatGPT)"
- Nền: phase 05 (`DuelMatch`, `DuelService`), phase 06 (PvP, reconnect 60 s)
- Luật UI: `docs/ui-layout-rules.md` (đọc hết); asset phase 02 (G huy hiệu bậc)

## GHI CHÚ 23/11 chiều (luật đấu card v2)
- **Ranked KHÔNG lấy card**: không escrow deck, không `DuelPickRound`; PvP thường mới "thắng lấy 1 card". Ranked dùng lại `DuelPvpMatch` nhưng `DuelService` phải tạo `PvpRun` với `Stakes = false` và không gọi API wager.
- PvP thường không còn thưởng Gold (300 / 100 / 150, 5 trận / ngày bị bỏ); dòng "thưởng Gold dùng chung bộ đếm PvP" bên dưới KHÔNG còn đúng: Ranked cần bảng thưởng Gold riêng nếu muốn có.

## Overview
P2, **xong code + UI 23/11 chiều (chưa deploy)**. Hàng chờ Ranked trong zone process, ghép theo MMR ẩn, điểm hiện (LP) theo bậc Bronze -> Diamond, thưởng Gold như PvP, mùa 30 ngày reset một phần. Không cược.

## Key Insights
- Một zone process host mọi map -> hàng chờ trong bộ nhớ `DuelService` là đủ cho V1, không cần dịch vụ ghép riêng.
- Lưu `cardRank {season, mmr, lp, wins, losses, draws, best}` trong state nhân vật (field đã thêm ở phase 01) -> không cần collection mới, không cần migration; reset mùa làm lười khi nhân vật đụng Ranked lần đầu trong mùa mới.
- Mùa = `floor((utcMs - SeasonEpoch) / 30 ngày)`, thuần, test được.
- Bảng xếp hạng toàn server KHÔNG có trong V1 (YAGNI); chỉ hiện bậc / LP của mình.

## Requirements
- Điều kiện: xong tutorial (thắng NPC lần đầu), Lv20+, deck 8 lá hợp lệ, map an toàn.
- Ghép: |MMR| <= 150, mỗi 30 s nới thêm 100; không ghép cùng party (game chưa có hệ bạn bè); không ghép lại đối thủ vừa gặp trong 10 phút.
- Điểm: MMR Elo K=32 (ẩn); LP thắng +20..30, thua -15..25 theo kỳ vọng Elo; hoà 0; bỏ / rớt quá 60 s = thua. LP không âm.
- Bậc theo LP: Bronze 0, Silver 300, Gold 700, Platinum 1.200, Diamond 1.800 (đề xuất).
- Reset mùa: `mmr = 1000 + (mmr - 1000) / 2`, `lp = lp / 2`, lưu `best` của mùa trước (để trao cosmetic sau, câu hỏi 9).
- Thưởng Gold dùng chung bộ đếm PvP (300 / 100 / 150, tối đa 5 / ngày).
- Dàn xếp: cùng cặp > 2 trận Ranked / ngày -> trận thứ 3+ không tính điểm; log cặp lặp và chuỗi thua nhanh (< 60 s) ra server log.
- UI: trang Card Duel thêm khối Ranked (huy hiệu bậc, LP, mùa còn N ngày, nút Find Match / Cancel, đếm giây chờ).
- Constraints UI: chỉ asset phase 02, đọc hết `ui-layout-rules.md`.

## Architecture
```
Assets/Scripts/Rpg/Cards/RankedRules.cs        Season(utcMs), Tier(lp), Apply(result, mmrA, mmrB) -> delta, SeasonReset, CanQueue(...)
Assets/Scripts/Net/Server/DuelService.Ranked.cs hàng chờ, ghép mỗi 1 s, nới khoảng theo thời gian chờ, chống cặp lặp
Assets/Scripts/CardWindow.Ranked.cs            khối Ranked trong trang Duel
```
Luồng: QueueRanked -> `RankedRules.CanQueue` -> hàng chờ -> ghép -> `DuelMatch` (như PvP, không cược) -> Result -> `RankedRules.Apply` cho 2 bên -> `cardRank` + Gold -> `RpgState` + `RequestSave`.
Wire: thêm QueueRanked / LeaveQueue vào `DuelRequestMsg`, DuelNotice "Matched" / "QueueTime"; Version +1.

## Related Code Files
Tạo: file ở Architecture; `Tests/dotnet/ZenoniaSkills.Tests/RankedRulesTests.cs`.
Sửa: `Net/Server/DuelService.cs` (tick hàng chờ, gỡ khỏi hàng khi rời / vào trận khác), `Net/Protocol/DuelMessages.cs`, `NetOpcodes.cs` (Version), `CardWindow.Duel.cs`, `server-api/src/rpg/item-validation.js` chỉ nếu phải chỉnh giới hạn `cardRank` đã thêm ở phase 01.

## Implementation Steps
1. `RankedRules` + test: biên LP (+20..30 / -15..25), LP không âm, bậc, mùa, reset, cặp lặp.
2. `DuelService.Ranked`: hàng chờ, ghép, nới, chống cặp lặp / cùng party.
3. Message + Version; UI khối Ranked.
4. Build zone; thử 2-3 client (bot `Tests/dotnet/ZenoniaSwarm` nếu dùng được) chờ ghép, nới sau 30 s, rớt mạng giữa trận Ranked.

## Todo List
- [x] RankedRules + test (`RankedRulesTests`, 25 test)   - [x] Hàng chờ + ghép (`RankedQueue`, `DuelService.Ranked.cs`)   - [x] Message (Version 33) + UI (`CardDuelRankedPanel`, banner LP)   - [x] Build + thử 2 client dev server (`net-duel-test.ps1 -Scenario ranked | ranked3`)
- [ ] Deploy (API trước) + thử với API / Mongo thật, rớt mạng giữa trận Ranked, nhiều client (nới sau 30 s)

## Làm khác kế hoạch (23/11 chiều)
- Không có thưởng Gold, không "xong tutorial" (đấu NPC đã bỏ). Thưởng cuối mùa = cờ `cardRank.rewards` (bit theo bậc) + `prevBest`; chưa có cosmetic.
- Không cần sửa `DuelMatch` / lobby: Ranked dùng `StartMatch(..., ranked: true)` của `DuelService.Pvp.cs` (`PvpRun.Ranked`, không stakes). Hàng chờ ghép theo MMR + party + gặp lại 10 phút (`RankedQueue`, thuần, test ở `RankedRulesTests`).
- Kết quả luôn tính từ rank lúc bắt đầu trận (`PvpRun.RankBefore`): rớt mạng lưu thua tạm lúc rời, nối lại xong thì thay bằng kết quả thật. Người thua offline sau 60 s giữ thua tạm; người thắng online nhận thắng.
- Cùng cặp > 2 trận / ngày: vẫn ghép và đấu nhưng không đổi LP (cờ `RankNotCounted`, banner báo); bộ đếm ở bộ nhớ zone (mất khi zone khởi động lại).
- `--dev-cards`: cooldown gặp lại = 0 để thử nhiều trận cùng cặp.
- Sửa kèm `CardCollection.ToSave / Load` (chỉ 2 dòng `Rank`) cho 2 field mới.

## Success Criteria
- Test: thắng đối thủ MMR bằng -> +25 LP; thua -> -20; hoà 0; reset mùa đúng công thức.
- 2 client MMR chênh 400 không ghép trong 30 s đầu, ghép sau khi nới đủ.
- Không ghép cùng party; trận 3 cùng cặp trong ngày không đổi LP.

## Risk Assessment
| Rủi ro | KxT | Giảm thiểu |
|---|---|---|
| Ít người -> chờ mãi | Cao x TB | nới khoảng theo thời gian, hiện thời gian chờ, cho huỷ |
| Dàn xếp điểm | TB x TB | giới hạn cặp / ngày, log |
| Mùa lệch giữa zone / client | Thấp x Thấp | mùa tính từ giờ server, client chỉ hiển thị |

Rollback: tắt nút Find Match bằng `DuelService.RankedEnabled`; `cardRank` giữ nguyên.

## Security Considerations
Kết quả và LP chỉ tính ở server; client không gửi số. Ranked không có cược (WagerRules từ chối).

## Next Steps
Phase 08 (lối vào ở NPC + menu). Cosmetic cuối mùa: chờ câu hỏi 9.

## Sửa lỗi review 23/11 (report `code-reviewer-261123-1536-phase-07.md`)
- H1 kết quả cho người offline: `RankedPending` (Rpg/Cards, thuần) giữ rank thật theo charKey khi trận kết thúc lúc họ offline; `DuelService.OnJoin -> ApplyPendingRank` áp trước PushState + RequestSave. Hết hạn sau 24 h (thua tạm giữ nguyên); mất khi zone khởi động lại (thua tạm giữ nguyên). Cả hai offline: mỗi người nhận kết quả thật khi quay lại.
- H2 cùng tài khoản: server-api `/internal/tickets/redeem` trả thêm `userId`; zone đọc vào `LoadedCharacter.UserId` -> `NetPlayer.UserId` (dev server không có tài khoản: mỗi tên là một tài khoản). `RankedQueue.Waiter.Account`: không ghép cùng tài khoản, cooldown gặp lại tính theo cặp tài khoản; ledger cặp/ngày theo cặp tài khoản. Forfeit trước 3 nước đánh tay (auto-đặt 20 s không tính, `RankedRules.MinMovesForForfeit`): trận không tính cho cả hai (không đổi LP/MMR, không tính vào giới hạn cặp), banner "Same opponent too often, or it ended too early". Wire không đổi.
- M1 `RankedRules.HideRating` đặt `mmr = 0` trong bản save gửi client (`ServerReplication.SendRpgState`); state server giữ nguyên; client chỉ đọc, không gửi state lên.
- M2 chỉ `season == 0` là "chưa chơi"; MMR kẹp >= 1 (Apply và EnsureSeason). M3 `PairLedger(RankedRules.MaxMatchesPerPairPerDay)`.
- Low: `--no-ranked` (NetLaunchArgs -> `RankedRules.Enabled = false`, TickRanked không ghép); `BeginRanked` mã lỗi riêng từng bên; `counted` tính một lần lúc bắt đầu (`PvpRun.Counted`, thua tạm và kết quả cuối dùng chung); thanh LP ẩn khi tiến độ 0; `prevBest` = best của mùa vừa xong (0 nếu bỏ mùa); MMR của người đang chờ làm mới mỗi giây (và đổi mùa).
- Test: `RankedReviewFixesTests` (17), vitest `redeem-user-id`. Scenario `Tools/net-duel-test.ps1 -Scenario rankeddc` (cả hai rớt mạng, người offline thắng nhận kết quả khi quay lại).
