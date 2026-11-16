# Phase 11: Wanted UI - bảng truy nã, thẻ HUD, thông báo, dấu minimap, dẫn đường

## Context Links
- Mockup: `chatgpt/wanted-board.png`, `wanted-notifications.png`, `boss-hp-bar.png` (đã bỏ Hunters), `wanted-fight-full-mobile.png` (đang vẽ lại, bỏ Hunters, đai vòng cung)
- Code: `WantedBossBoard.cs` (`WantedBoardData`, mở qua hộp thoại NPC Guard Rook / Leila), `WantedBossTracker.cs` (đăng nhập, đã trả thưởng, `Status` từ phase 10), `WantedWarningSprites.cs`, `HeroController.WantedOverlay.cs` / `WantedStatus.cs` (icon trạng thái trên đầu), `Minimap.cs` (chấm boss đỏ to), `NotificationToast.cs` (phase 03 đổi khung), `TownNpc.cs`
- Phase 03 (BossBarView, toast), 04 (`HudAnchors`), 10 (Status, Schedule, Hint, WantedRoute); sheet W1

## Overview
- Ưu tiên: P1. Effort: L. Status: code xong 16/11, bản dựng từ bản sao OK, đã chụp + xem ảnh offline (dữ liệu demo) và 1 lần online thật (chưa commit, chưa code review, công tắc `UiScreen.Wanted` / `UiScreen.Hud` vẫn TẮT mặc định chờ người dùng duyệt).
- Bảng truy nã toàn cảnh (mở từ Guard Rook / Leila và từ thẻ HUD), thẻ Wanted dưới More, 7 loại thông báo có thứ tự ưu tiên, đầu lâu trên minimap, mũi tên + khoảng cách dẫn tới boss.

## Key Insights
- Không có bước nhận nhiệm vụ: bảng chỉ là trung tâm thông tin + dẫn đường. Không có số Hunters ở bất kỳ đâu.
- Wanted chỉ chạy online (`WantedBossBoard.Current()` trả null offline). Offline: ẩn thẻ HUD, NPC nói câu thường (giữ). Chụp ảnh qua host / `--net-bot`.
- "Show on Map": game chưa có bản đồ thế giới -> hiểu là bật **dẫn đường**: mũi tên ở mép màn + khoảng cách tới ô boss (cùng map) hoặc tới cổng kế tiếp (`WantedRoute`), và đầu lâu trên minimap. Cần người dùng xác nhận.
- Bảng thưởng "BOUNTY CLAIMED!": client thấy `WantedDay` tăng (đã có) -> mở bảng + OK. "Already claimed today": client thấy boss chết trong tầm + đã nhận hôm nay -> toast. Nếu phase 12 duyệt personal loot, bảng thưởng thêm cột "Your Loot" + Collect All (`loot-rules-ui.png`) - dựng bảng có chỗ cho cột này, chưa làm.
- Ưu tiên thông báo (ChatGPT): cảnh báo vùng nguy hiểm > WARNING (2 s) > bảng thưởng (tới khi OK) > hồi sinh (4 s) > vào map (3 s) > đăng nhập (5 s) > đã nhận (2,5 s). Hàng đợi `NotificationToast` hiện có (max 4) cần thêm mức ưu tiên.

## Requirements
- Bảng truy nã: áp phích boss hôm nay (art monster có sẵn, Lv, map), trạng thái ALIVE / Respawns mm:ss / CLAIMED, thưởng 4 ô (vàng, đá, bình HP, bình MP + cỡ), dấu NOT CLAIMED YET / CLAIMED, Show on Map, lịch 3 boss + thưởng, 4 chiêu (icon + tên + kiểu + Hint + sơ đồ theo Shape), "Recommended Lv. N+, bring a party" + "My Lv", "Next wanted in hh:mm:ss (Change at 05:00)". Guard Rook bên trái (art W1).
- Thẻ HUD: chân dung boss + ALIVE + đếm tới reset / Respawns + đếm / CLAIMED; chạm mở bảng; đặt dưới More (`HudAnchors`).
- Thông báo: đăng nhập (Today's Wanted + thưởng), hồi sinh ("X has returned!"), vào map có boss (mũi tên + mét), WARNING khi vào gần (bán kính = `Hud.BossBarRange`), BOUNTY CLAIMED + OK, Already claimed.
- Minimap: đầu lâu đỏ nhấp nháy khi sống, xám khi chờ hồi (cùng map boss).
- Icon trạng thái trên đầu hero (Frozen / Poison / Slow + "!" khi đứng trong vùng): đổi art theo W1, logic giữ.
- Mobile và PC cùng bảng (toàn cảnh, co 16:9); thẻ HUD và toast theo bố cục mockup mục 10.

## Architecture
```
WantedBossBoard.Current() + Tracker.Status + WantedBoss.Schedule + WantedSkillTable(Hint) ──> WantedBoardWindow
Tracker (đăng nhập / claim / Status đổi) ──> WantedNotices ──> NotificationToast (thêm mức ưu tiên)
Status + map hiện tại + WantedRoute ──> WantedGuide (mũi tên mép màn + mét) ; Minimap.SetWantedMarker(state)
HUD: WantedHudCard (dưới More) --click--> UiScreens.Open(WantedBoard)
TownNpc Guard Rook / Leila ──> UiScreens.Open(WantedBoard) (cờ bật) | hộp thoại cũ (cờ tắt)
```

## Related Code Files
- Tạo: `WantedBoardWindow.cs`, `WantedBoardWindow.Poster.cs`, `WantedBoardWindow.ScheduleSkills.cs`, `WantedHudCard.cs`, `WantedNotices.cs`, `WantedGuide.cs`, `DebugCapture.UiWanted.cs`
- Sửa: `WantedBossBoard.cs` (thêm trường cho bảng, `TryOpen` gọi UiScreens khi cờ bật), `WantedBossTracker.cs` (chỉ thêm sự kiện cho WantedNotices; Status do phase 10 thêm trước), `Minimap.cs` (marker đầu lâu), `NotificationToast.cs` (mức ưu tiên; phase 03 đã xong phần khung), `WantedWarningSprites.cs` / `HeroController.WantedOverlay.cs` (art W1)
- Không sửa: file của phase 10 (luật / protocol / server).

## Implementation Steps
1. Nhận áp phích 2 trạng thái + bản vẽ lại `wanted-fight-full-mobile.png` (phase 02).
2. `WantedBoardWindow` (áp phích, lịch, chiêu + sơ đồ, gợi ý level, đếm ngược).
3. `WantedHudCard` + mở bảng từ HUD và từ NPC.
4. `NotificationToast` mức ưu tiên + `WantedNotices` 7 loại.
5. `WantedGuide` (mũi tên + mét, cùng map / cổng kế tiếp) + marker minimap.
6. Art icon trạng thái trên đầu.
7. Cờ chụp (thêm vào `DebugCapture.UiWanted.cs`): `--open-wanted-board`, `--wanted-notice login|respawn|area|warning|claimed|already`, `--wanted-guide`; online dùng sẵn `--net-bot` + `--wanted-shots`, `--wanted-target`, `--wanted-respawn-check`, `--wanted-respawn-sec`, `--wanted-boss <type>`.
8. Build server + client bản sao, chạy host, chụp: bảng (3 boss x sống / hồi / đã nhận), thẻ HUD 3 trạng thái, từng thông báo, cảnh đánh Wanted đầy đủ HUD (so `wanted-fight-full-mobile.png` bản mới) ở mobile + PC. Tự xem.

## Todo List
- [x] Mockup bổ sung (`chatgpt/wanted-board-poster-states-respawning-claimed-reward-panel.png`, sheet W1 đã cắt)
- [x] Bảng truy nã (3 trạng thái, lịch 3 boss, 4 chiêu + sơ đồ, gợi ý level, đếm ngược)
- [x] Thẻ HUD + mở từ HUD / NPC
- [x] Ưu tiên toast + thông báo (login, respawn, area, warning, bảng thưởng, already)
- [x] Dẫn đường + đầu lâu minimap
- [ ] Icon trạng thái trên đầu: CHƯA làm, sheet W1 không có mảnh Frozen / Poison / Slow / "!" (vẫn dùng `fx/wanted/warning-*`)
- [x] Cờ chụp, ảnh, tự xem (offline demo đủ; online 1 lần: cảnh đánh + bảng thưởng)
- [x] `dotnet test` 2 bộ: 358 + 422 pass (chạy sau khi thêm code, trước các lần chỉnh layout; phase không sửa file nào hai bộ test biên dịch)

## Ghi chú sau khi làm (16/11)
- Ảnh: `docs/reference/ui-redesign-261116/phase-11-wanted/`. Chụp: `--ui-new hud,boss,wanted` + cờ trong `DebugCapture.UiWanted.cs` (`--wanted-demo alive|respawn|claimed`, `--wanted-demo-boss`, `--wanted-demo-flip`, `--open-wanted-board`, `--wanted-talk`, `--wanted-notice ...|all`, `--wanted-panel-ok`, `--wanted-guide`, `--wanted-tap-card`). Offline các cờ này bơm đúng tin `WantedStatus` / `Observe` mà zone gửi.
- File mới: `PaperWanted.cs` (trạng thái chung + `WantedRewardRow`), `WantedBoardWindow.cs` + `.Poster` + `.ScheduleSkills`, `WantedHud.cs` (gốc safe-area, tạo từ `RpgHud.Create`), `WantedHudCard.cs`, `WantedNotices.cs` + `.Banners`, `WantedRewardPanel.cs`, `WantedGuide.cs`, `WantedMinimapMark.cs`, `DebugCapture.UiWanted.cs`. Sửa: `WantedBossBoard.cs` (TryOpen mở màn mới khi cờ bật + Status.Known), `WantedBossTracker.cs` (toast cũ chỉ khi HUD cũ), `NotificationToast.cs` (`Suspended`, `AreaTop`), `Net/Client/TargetMarker.cs` (thanh mục tiêu dời xuống dưới `BossBarView.Bottom`, thêm `Bottom`), `Minimap.cs` (3 dòng: `WantedMinimapMark`), `RpgHud.cs` (1 dòng `WantedHud.Create`), `PaperArt.Pieces.cs` (khối W1).
- Công tắc: bảng = `UiScreen.Wanted`; thẻ, thông báo, bảng thưởng, mũi tên, đầu lâu = `UiScreen.Hud`. Tất cả chỉ hiện khi `WantedBossTracker.Status.Known` (offline / chưa có tin: ẩn, NPC nói câu thường).
- Khác kế hoạch: (1) thông báo Wanted là banner riêng trong vùng toast, không đi qua hàng đợi `NotificationToast`; toast thường chờ (`Suspended`) khi banner / bảng thưởng đang hiện; (2) không có loại "cảnh báo vùng nguy hiểm" riêng (đã có vòng đỏ + "!" trên đầu); (3) vào map sát boss: WARNING nuốt thông báo vào map (gộp); banner chờ tên map (`MapNameBanner`) tắt rồi mới hiện; (4) bảng thưởng không tạm dừng game (đang giữa trận), nằm dưới vùng toast; không có "Your Loot" / thanh đóng góp; (5) mobile: thẻ ẩn khi quạt More mở (quạt bung đúng chỗ thẻ); (6) mũi tên dẫn đường là chevron vàng trên vòng quanh giữa màn + mét + đích, tắt khi nhận thưởng, không lưu; (7) `--wanted-notice claimed` thay cho cờ bảng thưởng riêng.
- Chưa có art / dữ liệu: icon skill của boss (huy chương trơn xanh / đỏ), sơ đồ "vòng tròn quanh boss" (mượn hình multi-circle), ảnh boss trên áp phích là frame đi bộ đầu tiên của sheet (không có chân dung vẽ riêng), nhãn đỏ REWARD dùng `kit/label-ribbon-red` kéo giãn.
- Chưa xem: thanh `TargetMarker` online khi đánh quái thường cạnh thanh boss mới; mũi tên qua cổng trên map thật có nhiều cửa; "Already claimed" và "has returned" online; bảng mở từ Guard Rook online; máy có tai thỏ; 21:9.

## Success Criteria
- Người chơi online biết boss hôm nay, sống hay chờ hồi (đếm ngược đúng ±2 s), đã nhận chưa, đường tới boss, mà không phải hỏi NPC.
- Không có số người đánh boss; không có bước nhận.
- Thông báo không chồng nhau, theo đúng thứ tự ưu tiên.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Thông báo dồn lúc vào map boss (vào map + WARNING + hồi sinh) | Trung bình x Trung bình | Hàng đợi ưu tiên, gộp vào map + WARNING khi < 1 s |
| Thẻ HUD + quạt More + thanh boss chật ở 4:3 / 20:9 | Trung bình x Trung bình | Thẻ thu nhỏ còn chân dung + trạng thái khi thiếu chỗ |
| Mũi tên chỉ sai khi đường qua cổng bị khoá | Thấp x Trung bình | `WantedRoute` dùng `PortalsFor(gateOpen)` của hero |

## Security Considerations
- Chỉ hiển thị; thưởng vẫn do server (`WantedBossCredit`).

## Next Steps
- Phase 12 có thể thêm cột "Your Loot" vào bảng thưởng.

## Câu hỏi mở
- "Show on Map" = bật dẫn đường (mũi tên + minimap) vì game chưa có bản đồ thế giới: đúng ý không?
