# Phase 04: HUD nút vào (Bag / Skill / More), điều khiển cảm ứng, hotbar PC

## Context Links
- Mockup: `chatgpt/hud-entry-buttons.png`, `hud-touch-controls.png`, `hud-pc-hotbar.png`, `hud-full-mobile.png`, `hud-full-pc.png`
- Hiện tại: `ingame/hud-mobile-*.png`, `hud-web-*.png`
- Code: `TouchControls.cs` (joystick, bình, nút đánh, vòng cung skill), `TouchControls.Menu.cs` (nút tròn dưới minimap: Pet?, Quests, Bag + badge 80, Settings), `TouchControls.WebHotbar.cs` (hotbar web + EXP), `TouchJoystick.cs`, `UiSprites.cs` (đĩa vẽ bằng code), `UiArt.cs` (`hud-*.png`), `InputAdapter.cs` (`GameKey`), `HudLayout.cs`
- Phase 01, 03; sheet H2, H3, K4

## Overview
- Ưu tiên: P1. Effort: M. Status: code xong 16/11, build bản sao OK, đã chụp và xem ảnh (chưa commit, chưa code review, công tắc `UiScreen.Hud` vẫn TẮT mặc định chờ người dùng duyệt).
- Thẻ treo Bag + Skill dưới minimap (1 chạm), túi More bung quạt Pet / Gacha / Quests / Settings (2 chạm), joystick / bình / nút đánh / đai vòng cung kiểu gỗ-da, hotbar PC đai da + EXP dài 3 trạng thái.

## Key Insights
- Logic điều khiển giữ nguyên: TouchControls chỉ bơm `GameKey` vào `InputAdapter`; phase này đổi hình + bố cục nút vào, không đổi luồng input.
- Vị trí cảm ứng giữ như hiện tại (giả định 3); kích thước theo mockup ở 1080p: đánh 150-160 px (~92u), skill 100-110 px (~63u), bình 80 px (~47u), joystick 180-200 px (~113u), hở >= 16 px.
- Trạng thái ô skill đã có dữ liệu: sẵn sàng / hồi (giây) / thiếu MP / khoá (chưa học) / trống. Bình: có / hết / hồi.
- Badge: Skill = SP chưa dùng (hiện đang ở nút Bag, chuyển sang Skill); Quests "!" = có nhiệm vụ trả được (`QuestLog`); Bag chấm đỏ = có món mới **chưa có logic** -> thêm cờ "món mới" phía client (đặt khi món vào túi, xoá khi mở thùng đồ; không lưu, không gửi server).
- Phím: B / T / P / G đã có; Enter = Chat đã có. Quests đề xuất J nhưng **J đang là phím đánh** (Hud ghi "Attack J/Space") -> cần người dùng chọn phím khác. Esc mở Settings chỉ khi không có cửa sổ nào mở (Esc hiện là Close).
- Gamepad: không làm HUD riêng; sau này dùng lại bố cục PC, đổi nhãn phím (ngoài phạm vi, chỉ ghi lại).
- `UiSprites` (đĩa vẽ code) hết dùng khi xong phase này -> xoá ở phase 13 nếu không còn ai gọi.

## Requirements
- Mobile: thẻ treo Bag / Skill + túi More dưới minimap; quạt 4 cánh mở / đóng có hoạt ảnh ngắn (<= 0,2 s), chạm ngoài thì đóng; joystick 8 hướng (idle / kéo); 2 bình bên phải joystick; nút đánh to + đai vòng cung 3 ô + 3 chấm trang; vuốt lên / xuống trên đai đổi trang (đã có).
- PC: thẻ Bag [B] / Skill [T] + More dưới minimap; hotbar giữa đáy: Q, E (bình), LMB / Space (đánh), 1-2-3 (skill), biển "Tab 1/3"; EXP dài dưới: thường lục / >= 95 % vàng "gần lên cấp" / max tím "Lv. 90 (MAX)".
- Nhãn phím trên hotbar lấy từ binding thật (`InputAdapter`), không viết cứng.
- Không che nhau ở 4 tỉ lệ; trong `Screen.safeArea`.

## Architecture
```
HudEntryButtons (thẻ treo + More fan) --click--> UiScreens.Open(Bag|Skill|Pet|Gacha) / QuestLogWindow / SettingsWindow
HudBadges (đọc PlayerState.SkillPointsFree, QuestLog, NewItemFlags) --> badge trên thẻ / cánh quạt
TouchControls (giữ logic) --dựng hình bằng--> PaperKit + mảnh H3 (thay UiArt hud-* và UiSprites)
WebHotbar --dựng hình bằng--> đai da H3 + thanh EXP K4
HudAnchors : hằng số neo (dưới minimap, dưới More) cho phase 07 (nút chat) và 11 (thẻ Wanted)
```

## Related Code Files
- Tạo: `HudEntryButtons.cs`, `HudEntryButtons.MoreFan.cs`, `HudBadges.cs`, `NewItemFlags.cs`, `HudAnchors.cs`, `DebugCapture.UiControls.cs`
- Sửa: `TouchControls.cs`, `TouchControls.Menu.cs` (thay nút tròn bằng HudEntryButtons khi cờ bật), `TouchControls.WebHotbar.cs`, `TouchJoystick.cs` (art), `InputAdapter.cs` (phím Quests nếu người dùng chọn), nơi thêm món vào túi để đặt `NewItemFlags` (grep chỗ gọi refresh thùng đồ khi nhặt, ví dụ `InventoryPanel.RefreshIfOpen`)
- Không đụng: `RpgHud.cs` (phase 03), `ChatCollapsedView.cs` (phase 07), `Minimap.cs` (phase 11)

## Implementation Steps
1. `HudAnchors`: tính khung dưới minimap theo `Minimap` hiện tại (đọc kích thước công khai, không sửa Minimap).
2. `HudEntryButtons` + quạt More; nối `UiScreens` (Bag, Skill, Pet, Gacha), `QuestLogWindow.Open`, `SettingsWindow.Open`.
3. `HudBadges` + `NewItemFlags`; chuyển badge SP từ Bag sang Skill.
4. Đổi art TouchControls: joystick, núm, bình (3 trạng thái), nút đánh (thường / nhấn), khung ô skill + đai vòng cung, chấm trang. Giữ vị trí, đổi cỡ theo mockup.
5. Hotbar web: đai da, nắp phím, biển Tab có mũi tên, EXP 3 trạng thái.
6. Phím: thêm GameKey Quests (sau khi người dùng chọn phím) và Esc -> Settings khi không có modal.
7. Cờ chụp: `--more-open`, `--skill-cd-demo` (ô 2 đang hồi, ô 3 thiếu MP), `--potion-demo empty|cd`, `--exp-pct N`, `--badges-demo`. Dùng sẵn `--hud web|mobile`, `--level`.
8. Build bản sao, chụp 2 HUD x 4 tỉ lệ (+ More mở), tự xem, so `hud-full-*.png`. Test tay trên PC: chuột bấm thẻ / quạt; cảm ứng giả bằng chuột (TouchControls nhận cả chuột).

## Todo List
- [x] HudAnchors, HudEntryButtons, quạt More
- [x] HudBadges + NewItemFlags (badge SP chuyển sang Skill)
- [x] Art cảm ứng: joystick, bình, nút đánh, đai vòng cung, chấm trang
- [x] Hotbar PC + EXP 3 trạng thái
- [x] Phím Quests = N (tạm chọn, chờ người dùng xác nhận) + Esc -> Settings khi không có gì mở
- [x] Cờ chụp + ảnh, tự xem: 16:9, 20:9, 4:3 (mobile + web). CHƯA chụp 21:9, chưa `--perf`, chưa thử cảm ứng thật trên điện thoại
- [ ] `dotnet test` 2 bộ: không chạy (phase này không sửa file nào trong Rpg/, Net/Protocol, Maps/)

## Ghi chú sau khi làm (16/11)
- Ảnh: `docs/reference/ui-redesign-261116/phase-04-hud-controls/`. Chụp: `--ui-new hud` + cờ trong `DebugCapture.UiHudControls.cs` (`--more-open`, `--badges-demo`, `--skill-cd-demo`, `--skill-slots-demo lock|empty`, `--skill-page N`, `--potion-demo empty|cd`, `--exp-pct N`, `--hud-tap "1=Bag;2=More;2.6=Pet"`, `--hud-hold Attack`).
- File mới: `HudAnchors`, `HudEntryButtons` (+ `.MoreFan`), `HudBadges`, `NewItemFlags`, `PaperHudControls` (gốc safe-area `PaperHudControls`, dựng lười trong `TouchControls.Paper.cs` lần đầu thấy công tắc bật; công tắc đọc mỗi frame), `PaperTouchPad`, `PaperSkillArc`, `PaperSkillSlot`, `PaperPotionButton`, `PaperHotbar` (+ `.Exp`), `PaperHudPress`, `InputAdapter.KeyLabels.cs`, `DebugCapture.UiHudControls.cs`. Sửa: `TouchControls.cs` (Update chọn lớp cũ / mới), `TouchControls.Menu.cs` (CycleMagazine), `InputAdapter.cs` (`GameKey.Quests` = N), `PaperArt.Pieces.cs` (tên mảnh H2 / H3), `DebugCapture.UiHud.cs` (1 dòng gọi hook `DebugUiHudControls`). `TouchControls.WebHotbar.cs`, `TouchJoystick.cs` không phải sửa.
- Khác kế hoạch: (1) cỡ + vị trí + vùng chạm nút cảm ứng giữ NGUYÊN như HUD cũ (đánh 156u, skill 72u, bình 58u, joystick 200u) theo quyết định của điều phối, không co về 92 / 63 / 47 / 113u; (2) `DebugCapture.UiControls.cs` đặt tên `DebugCapture.UiHudControls.cs`; (3) không cần hook ở nơi thêm món: `NewItemFlags` so uid túi với tập đã thấy (4 lần / giây), xoá cờ khi `UiScreens.IsOpen(Bag)`; (4) thẻ Bag / Skill / cánh Pet / Gacha bấm `GameKey` (đi đúng đường của phím B / T / P / G), Quests / Settings mở cửa sổ như nút cũ; (5) thứ tự hotbar theo mockup: Q, E, LMB / Space, 1, 2, 3, Tab; (6) đai vòng cung xoay 22 độ + co 0,84 để chạy dưới 3 ô ở vị trí cũ; chấm trang dời sang x = -42u; (7) ô trống "+" = ô chưa gán VÀ có skill đã học nằm ngoài băng, còn lại là ổ khoá; chạm "+" chưa làm gì; (8) nút chat thuộc phase 07 (không làm ở đây).
- Chưa có dữ liệu: game không có hồi chiêu bình, trạng thái "Cooldown" của bình chỉ hiện qua `--potion-demo cd`. Túi More vẽ theo art đã cắt (khoá vàng, không có dấu chân + mũi tên như mockup).
- Neo cho phase 11: `HudAnchors.WantedCardTopRight` (PC: dưới túi More; mobile: bên TRÁI túi More vì đai skill bắt đầu cách đáy túi 27u, không đủ chỗ cho thẻ 72u), `HudAnchors.EntryBottom` = 341u. Cột nút neo theo safe area, minimap (phase 11) vẫn neo theo canvas: máy có tai thỏ sẽ lệch nhau tới khi minimap chuyển vào safe area.

## Success Criteria
- 1 chạm mở Bag / Skill, 2 chạm mở Pet / Gacha / Quests / Settings, trên cả mobile lẫn PC; phím B / T / P / G / Enter vẫn đúng.
- Kích thước nút ở 1080p trong dải mockup; không nút nào chồng nhau ở 4 tỉ lệ; multi-touch (đi + đánh) vẫn chạy.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Quạt More mở đè nút đánh / skill | Trung bình x Trung bình | Quạt mở sang trái, đóng khi chạm ngoài; đo vùng chạm ở 20:9 |
| Đổi cỡ nút làm lệch vùng chạm vuốt đổi trang | Thấp x Trung bình | Giữ logic vùng vuốt, chỉ đổi bán kính theo cỡ mới; test tay |
| Badge "món mới" sai khi online (món đến từ server) | Trung bình x Thấp | Đặt cờ theo so sánh túi trước / sau mỗi lần RpgState đổi, chỉ phía client |

## Security Considerations
- Không có.

## Next Steps
- Phase 07 đặt nút chat theo HudAnchors; phase 11 đặt thẻ Wanted dưới More.

## Câu hỏi mở
- Phím mở Quests (J đang là phím đánh)? Đề xuất: Q đang là bình HP trên web, nên dùng `L`? (L là ô skill 2 trên bàn phím mobile-dev) -> cần người dùng chọn.
- Nút Pet tròn hiện có trên HUD (`hud-mobile-with-pet.png`, ảnh pet đầu tiên) có giữ không? Mockup không có -> đề xuất bỏ, Pet vào More.
