# Phase 03: HUD trạng thái người chơi, toast, thanh máu boss

## Context Links
- Mockup: `chatgpt/hud-player-status.png`, `hud-full-mobile.png`, `hud-full-pc.png`, `boss-hp-bar.png` (bỏ "Hunters"), `wanted-fight-full-mobile.png`
- Hiện tại: `ingame/hud-{mobile,web}-{idle,with-pet}.png`
- Code: `RpgHud.cs` (khối trên trái, LEVEL UP), `HudBuffRow.cs`, `NotificationToast.cs`, `Hud.cs` (thanh boss IMGUI, `BarBoss()`), `Monster.Visuals.cs` + `Monster.cs` (thanh máu trên đầu SpriteRenderer, `RenderWantedCastBar`), `Monster.WantedSkills.cs` (`WantedEnraged`, ngưỡng 30 %), `WantedSkillTelegraphFx.cs` (thời điểm niệm chú)
- Phase 01 (kit), 02 (sheet K*, H1, B1)

## Overview
- Ưu tiên: P1 (màn thử kit). Effort: M. Status: code xong 16/11, build bản sao OK, đã chụp và xem ảnh (chưa commit, chưa code review, công tắc vẫn TẮT mặc định chờ người dùng duyệt).
- Thay khối trạng thái trên trái bằng huy hiệu khiên; toast LEVEL UP / nhặt đồ theo mockup; thanh boss chuyển từ IMGUI sang uGUI với 5 trạng thái; thanh nhỏ trên đầu quái.

## Key Insights
- Dữ liệu đã có hết: HP / MP / EXP / Level (`PlayerState`), buff + giây (`HeroBuffs.ActiveFor`), khiên (kiểm tra buff khiên pet / skill trong `HeroBuffs`), mất máu gần đây (so `lastHp`), boss gần (`Hud.BarBoss()`), máu boss, `WantedEnraged`, telegraph skill (`WantedSkillTelegraph` event, `TelegraphMs` trong `WantedSkillTable`).
- KHÔNG có: số người đánh boss (người dùng bỏ, không làm); quái elite (game chưa có, `MapSpawnTable` ghi rõ) -> chỉ làm thanh thường + boss, khung elite để dành.
- Thanh boss map thường (Frostfang King) không có niệm chú: thanh cast chỉ hiện với Wanted Boss.
- Mobile: huy hiệu to có EXP (cao tối đa ~130 px ở 1080p, không tính hàng buff); PC: bản gọn không EXP (EXP nằm dưới hotbar, phase 04). Giả định 4: PC dùng huy hiệu khiên, không dùng khung tròn của `hud-full-pc.png`.
- Toast: ưu tiên theo ChatGPT (cảnh báo vùng > WARNING > bảng thưởng > ...) thuộc phase 11; phase này chỉ đổi khung toast chung + LEVEL UP.

## Requirements
- Huy hiệu: chân dung (`ui/faces/<class>`), thẻ Lv, thanh HP (tim) / MP (giọt) / EXP (mobile), số "818 / 818", hàng tối đa 6 buff có giây.
- Trạng thái: máu < 30 % viền đỏ nhấp nháy nhẹ; MP = 0 thanh xám; có khiên lớp khiên xanh; vừa mất máu: phần mất màu sáng tụt dần + số "-142" bay lên.
- Thanh boss trên giữa: (1) boss map: huy chương, tên, Lv, số + %; (2) Wanted: ruy băng WANTED, chân dung to, vạch RAGE 30 %, KHÔNG có Hunters; (3) niệm chú: tên chiêu + giây, chạy theo `TelegraphMs`; (4) ENRAGED dưới 30 %: viền lửa + nhãn; (5) DEFEATED hiện ~2 s khi boss chết.
- Thanh trên đầu: quái thường (thanh nhỏ), boss (thanh + tên + cast nhỏ dưới) theo mục 6 của `boss-hp-bar.png`.
- Chân dung boss: cắt từ sheet quái có sẵn (`Sprites/monsters/<BossType>.png`) đặt trong mặt nạ tròn, offset mặt ghi trong bảng nhỏ theo BossType.
- Layout: mobile và PC theo khung vị trí ở `boss-hp-bar.png`; thanh boss không đè huy hiệu hay minimap ở 4:3 (co ngang).

## Architecture
```
PlayerState / HeroBuffs ──> PlayerStatusBadge (uGUI, PaperKit) ──> top-left, safe area
Hud.BarBoss() + Monster.Hp/WantedEnraged + telegraph event ──> BossBarView (uGUI) ──> top-centre
Monster.Visuals (SpriteRenderer) ──> overhead bar dùng sprite khung B1 thay ProceduralSprites.White
RpgHud.LevelUp / NotificationToast.Show ──> khung H1 (giữ API cũ: Toast(text,color), Toast(title,sub,icon))
```
Công tắc `UiScreens.Hud` / `UiScreens.Boss`: cờ tắt thì RpgHud cũ + `Hud.DrawBossBar` IMGUI chạy như cũ.

## Related Code Files
- Tạo: `PlayerStatusBadge.cs`, `PlayerStatusBadge.States.cs` (máu thấp, khiên, mất máu), `BossBarView.cs`, `BossBarView.States.cs` (cast, enrage, defeated), `BossPortraits.cs` (offset mặt theo BossType), `DebugCapture.UiHud.cs` (thân partial hook)
- Sửa: `RpgHud.cs` (dựng badge mới khi cờ bật, giữ banner / toast API), `HudBuffRow.cs` (ô buff khung H1, cỡ 24 -> theo mockup), `NotificationToast.cs` (khung giấy), `Hud.cs` (bỏ vẽ IMGUI boss khi cờ bật), `Monster.Visuals.cs`, `Monster.cs` (sprite thanh trên đầu)
- Xoá: không (phase 13).
- Sở hữu độc quyền trong lúc chạy: các file trên. Phase 04 không đụng `RpgHud.cs`.

## Implementation Steps
1. `PlayerStatusBadge` dựng bằng PaperKit (mảnh H1 + thanh K4), 2 cỡ (mobile to / PC gọn) theo `HudLayout.IsWeb`; chỉ dựng lại chữ khi giá trị đổi (mẫu RpgHud).
2. Trạng thái: máu thấp, MP cạn, khiên, mất máu (thanh "đuổi" tụt sau 0,4 s).
3. `HudBuffRow` dùng khung mới, tối đa 6 ô.
4. LEVEL UP + toast nhặt đồ dùng ruy băng / dải H1 (chữ code vẽ).
5. `BossBarView`: chọn boss qua `Hud.BarBoss()`, 5 trạng thái; cast bar nghe event telegraph (đã có ở client cho vòng cảnh báo).
6. Thanh trên đầu: thay sprite trắng bằng khung B1, giữ logic hiện / ẩn.
7. Cờ chụp (DebugCapture.UiHud.cs): `--hp-pct` (đã có) cho máu thấp; mới `--mp-pct N`, `--status-demo shield|hit`, `--buffs-demo` (bật 6 buff mẫu như F10 DevBuffKey), `--boss-bar-demo map|wanted|cast|enrage|defeated` (đặt máu boss / ép trạng thái chỉ trong lần chụp, không lưu).
8. Build bản sao, chụp `--hud mobile` và `--hud web` ở 1920x1080, 2400x1080, 1600x1200, 3440x1440; boss thật: `--record-boss` (Frostfang King) và online `--wanted-shots` (DebugCapture.NetWanted). Tự xem ảnh, so với mockup.

## Todo List
- [x] PlayerStatusBadge 2 cỡ + 4 trạng thái
- [x] HudBuffRow khung mới (6 ô, "12s")
- [x] LEVEL UP + toast khung giấy
- [x] BossBarView 5 trạng thái (không Hunters)
- [x] Thanh trên đầu quái / boss (`Monster.PaperOverheadBar.cs`)
- [x] Cờ chụp + ảnh, tự xem: 16:9, 20:9, 4:3 (mobile + web). CHƯA chụp 21:9; chưa chạy `--record-boss`, `--wanted-shots` online, `--perf`
- [x] `dotnet test` 2 bộ: 358 + 422 pass (không file nào của phase nằm trong hai bộ test)

## Ghi chú sau khi làm (16/11)
- Ảnh: `docs/reference/ui-redesign-261116/phase-03-hud/`. Chụp: `--ui-new hud,boss` + cờ trong `DebugCapture.UiHud.cs` / `DebugCapture.UiBoss.cs` (`--mp-pct`, `--status-demo shield|hit`, `--buffs-demo`, `--toast-demo levelup|item|line|full`, `--boss-bar-demo map|wanted|cast|enrage|defeated`, `--boss-hp-pct`, `--boss-defeat-at`).
- Công tắc đọc mỗi frame (cờ chụp được áp sau khi HUD dựng): `RpgHud` dựng huy hiệu lần đầu thấy cờ bật; tắt cờ thì khối cũ + thanh boss IMGUI y như trước.
- Neo cho phase 04 / 11: `PlayerStatusBadge.Right` / `.Bottom` (mép phải, mép dưới tính cả hàng buff; mobile 292 x 150u, PC 254 x 124u), `RpgHud.BlockHeight` (giờ là property theo cờ), `BossBarView.Bottom` (0 khi ẩn), `RpgHud.ToastTop` (đỉnh vùng toast = max(98, đáy thanh boss + 6); ruy băng LEVEL UP ở đó, dải toast thấp hơn `ToastRibbonHeight` = 78u). API toast giữ nguyên: `RpgHud.Toast(text, color)`, `RpgHud.Toast(title, sub, icon)`, `NotificationToast.ShowLine / Show`; root HUD giấy là `PaperHud/Safe` (PaperLayout.CreateSafeArea) trong `RpgHud`.
- Khác kế hoạch: (1) thanh boss co tới 300u (không phải 360u) trước khi xuống dòng hai, để 4:3 mobile (khe 333u) vẫn nằm hàng trên; (2) số "-142" bay ở đầu phải thanh, từ thanh MP lên thanh HP (huy hiệu sát mép trên màn nên không bay phía trên thanh được); (3) `Monster.cs` không phải sửa: thanh trên đầu nằm ở file partial mới; (4) thêm 1 hàm đọc `WantedSkillTelegraphFx.StrikeIn` (file ngoài danh sách) để cast bar trên client dùng đúng thời điểm của event; (5) chạy lại `Tools/export-ui-paper-borders-to-csharp.py` (PaperArt.Borders.cs thiếu border `boss/*`).
- Chưa có art / dữ liệu: icon skill của boss trong huy chương cast (đang là đĩa tím), khung elite (game chưa có quái elite), ảnh chân dung boss là cắt từ sheet đi bộ (bảng offset `BossPortraits.cs`; boss chưa có trong bảng lấy giữa ô).
- Việc để lại: `TargetMarker` (online, IMGUI, y = 48u khi có thanh boss) sẽ đè lên thanh boss mới: phase 11 / 13 chuyển sang uGUI hoặc dời xuống `BossBarView.Bottom`. Thanh cast cũ trên đầu (`RenderWantedCastBar`) ở client proxy không có timer; bản mới đã dùng thời điểm event.

## Success Criteria
- Ảnh chụp khớp bố cục `hud-player-status.png` (mobile) và bản gọn (PC); thanh boss khớp mục 1-5 của `boss-hp-bar.png`, không có Hunters.
- Không che minimap / nút ở 4 tỉ lệ; số đọc được trên tuyết sáng.
- Profiler `--perf`: HUD không cấp phát GC mỗi frame.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Thanh boss + huy hiệu + minimap chật ở 4:3 | Trung bình x Trung bình | Thanh boss co theo bề rộng còn lại, tối thiểu 360u; xuống dòng dưới huy hiệu nếu vẫn thiếu |
| Cast bar lệch thời gian với vòng cảnh báo | Thấp x Trung bình | Dùng cùng `TelegraphMs` + thời điểm event, không tự đếm riêng |
| Debug HUD IMGUI (F8) còn tính vị trí theo `RpgHud.BlockHeight` | Thấp x Thấp | Giữ hằng số, cập nhật theo cao huy hiệu mới |

## Security Considerations
- Không có (chỉ hiển thị).

## Next Steps
- Phase 04 dùng neo layout của huy hiệu; phase 11 dùng BossBarView + khung toast cho thông báo Wanted.

## Câu hỏi mở
- Thanh trên đầu quái thường: hiện luôn hay chỉ khi mất máu (hiện tại: chỉ khi mất máu)? Đề xuất giữ như hiện tại.
