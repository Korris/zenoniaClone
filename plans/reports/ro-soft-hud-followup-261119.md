# Báo cáo: HUD RO Soft, lượt tiếp theo (19-20/11/2026)

Nhánh: `hud-followup-261119` (worktree), gộp vào `ro-fantasy-heroes`. Exe: `D:\temp\player-hud2\Zenonia.exe` (bản cảm ứng mặc định; bản PC/web thêm cờ `--hud web`; WebGL thật chưa build). Build từ bản sao `D:\temp\zu-copy-hud`.

## Đã làm theo 5 câu trả lời của người dùng
1. **Chân dung đúng ngoại hình** (`RoHudPortraits.cs`): quay nhân vật nhiều lớp thật (thân class, kiểu tóc, màu tóc / áo HeroRecolor, mũ đang đội) bằng một `HeroPreviewStage` 5 cột ngoài map, cắt bán thân idle hướng xuống. Tự đổi khi đổi tóc / màu / mũ. Hàng Party dùng cột 1-4 theo NetAppearance của đồng đội đang thấy; đồng đội ở xa vẫn là mặt vẽ sẵn.
3. **Party chỉ liệt kê người khác**, nhãn "Party (n/5)" đếm cả mình (n = tổng số thành viên). Giữ nguyên.
4. **Bản PC dùng HUD mới**: `RoHud.Active` không còn loại `HudLayout.IsWeb`; `RoHud.Pc`. PC bỏ joystick; nhãn phím trên vòng skill (1-4, trang 2: 5-8), Tab, Space (Attack), V (Auto), Shift (Run), Q / E (bình), B / T / P / N (menu), Enter (chat). Phím 1-8 bắn 8 ô loadout (`InputAdapter.RoLoadoutKeys.cs`, tắt phím ô cũ 1/2/3 khi HUD RO bật). N mở/đóng Quest. Chat PC: thanh và khung chat ở **góc trái dưới** (PC không có joystick; ở giữa là yêu cầu bản mobile), khung mở sẵn, "Press Enter to chat". `--ui-style classic` vẫn ra HUD cũ.
5. **Khung chat mở + toast vẽ lại bằng ChatGPT** (sheet E `ro-hud-e-chat-toast`, 12 mảnh 9-slice): `RoHudChatPanel.cs` (tab viên thuốc navy/kem, dải từng dòng tin, ô nhập, nút gửi san hô, nút X; cao đúng 4 dòng), toast `NotificationToast.RoSoft.cs` (pill sao 2 đầu, pill cảnh báo san hô, thẻ 2 dòng có dải tiêu đề vàng + ô icon).

## Thêm theo coordinator
- **Vạch 1 px ở 2 đầu pill**: lỗi `flatten_edges` của `Tools/cut-ui-paper-sheet.py` với 9-slice chỉ kéo ngang (sửa tool, cắt lại mọi sheet `ro-*`, 17 mảnh đổi) + khe IMGUI khi vẽ pill tên NPC 3 khúc (`NameTags.RoSoft.cs`). Ghi Sổ lỗi `docs/ui-layout-rules.md`. Đã phóng 4x kiểm.
- **Thông báo Wanted kiểu RO** (mockup ChatGPT `docs/reference/ro-soft-hud/wanted-notices-mockup-chatgpt-1.png`, sheet F `ro-hud-f-wanted`, 18 mảnh): Today's Wanted, Boss returned, A Wanted Boss lurks (+ m), WARNING, Already claimed, BOUNTY CLAIMED (ruy băng + nguyệt quế, OK / Wanted Board), toast "has fallen!" navy, thẻ Respawns có đồng hồ dưới thẻ WANTED, thanh máu boss RO có chân dung, ENRAGED, thanh niệm chiêu + bong bóng gợi ý (WantedSkillDef.Hint), Wanted Board RO gọn (`RoWantedBoard.cs`). **Lỗi cũ phát hiện**: HUD RO trước đây không hề hiện thông báo Wanted / bảng bounty (WantedHud chỉ chạy với HUD Paper) và `UiScreens.Open(Wanted)` không mở gì khi tắt switch Paper; đã sửa.
- **Icon độc**: ô trạng thái dưới HP 24 đơn vị, ô Party 22; số giây là badge navy ở góc dưới phải ("9s"), không đè icon (`HudBuffRow` kiểu `ro`, `PoisonStatusIcon`).
- Chữ IMGUI (tên NPC / nhân vật) không còn vẽ đè khung chat, banner, thanh boss, bounty, board (`NameTags.Occlude`).

## Bấm thử thật trên PC (`--hud web`, raycast EventSystem như chuột, `--ro-click`)
| Nút | Kết quả |
|---|---|
| Bag / Skills / Pet / Quest | mở đúng cửa sổ (Bag, Skill, Pet, QuestLog) |
| More → Gacha | More mở cột, Gacha mở cửa sổ gacha |
| "+" minimap | zoom 1.6 → 2.4 |
| Auto (2 lần) | bật rồi tắt AutoBattle |
| Run | nhận click (Dash; log không đo được cú lướt) |
| Tab (đổi trang skill) | trang 1 → 2, nhãn phím đổi 5-8 |
| Thẻ WANTED | mở Wanted Board RO |
| X khung chat / nút chat | đóng / mở lại chat |
| Phím 1, 2 (`--ro-digit`) | bắn skill ô 1, 2 (cooldown 0 → 2933 / 7917 ms) |
| Thu gọn Party | **chưa thử**: cần online trong party |

## Nghiệm thu ChatGPT (chat HUD của Tay)
| Vòng | HUD | Wanted | Ảnh |
|---|---|---|---|
| 1 | 8.9 | 8.1 | `docs/reference/ro-soft-hud/followup-round1.png`, `-wanted.png` |
| 2 | 9.2 | 8.9 | `followup-round2*.png` |
| 3 | 9.4 | 9.2, "không còn lỗi nặng" | `followup-round3*.png` |
Còn góp ý nhẹ vòng 3: nền chat PC đặc thêm 5 %, nút X / tab hơi nhỏ, toast vật phẩm có thể to thêm 8-10 %.

## Ảnh đã tự xem
`docs/reference/ro-soft-hud/`: `pc-1920x1080-followup.png`, `pc-1280x720-followup.png`, `mobile-chat-open-followup.png`, `mobile-toast-followup.png`, `portrait-combos-followup.png` (tóc xanh / nâu + kính bay / vàng + băng đô), `pc-poison-icon-followup.png`, `wanted-notice-*.png` (login, respawn, area, warning, already, respawn-card, bossbar-cast, bossbar-enrage, fallen-toast, bounty, board), art contact `art-contact/ro-hud-e-*`, `ro-hud-f-*`.

## Kiểm tra
- `dotnet test`: ZenoniaSkills 452/452, ZenoniaNet 455/455.
- Build Unity từ bản sao: 0 lỗi.
- Cờ chụp mới (`DebugCapture.RoHud.cs`): `--look`, `--look-at`, `--ro-click`, `--ro-digit`, `--ro-poison`, `--ro-fallen`.

## Chưa kiểm
- Online thật: chân dung đồng đội theo NetAppearance, icon độc trên hàng Party, thu gọn Party, thông báo Wanted từ server thật (chỉ chạy bằng `--wanted-demo` offline), thanh boss với Mire Hydra Queen thật (ảnh dùng Rimeclaw Tyrant demo).
- Bản WebGL thật (chỉ thử exe Windows với `--hud web`); máy Android/iOS.
- Bàn phím thật (phím đã thử qua bot/ghi đè, không bấm phím vật lý).

## Câu hỏi chưa giải quyết
1. Chat PC đặt góc trái dưới, mở sẵn: có muốn mặc định thu gọn (chỉ thanh 1 dòng) không?
2. Skill boss Wanted chưa có icon riêng (board dùng đầu lâu / tam giác): có xin ChatGPT vẽ icon cho từng skill không?
3. HUD Paper (`--ui-new hud`) vẫn dùng thẻ WANTED / thông báo kiểu Paper: giữ song song hay bỏ?
