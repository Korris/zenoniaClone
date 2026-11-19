# Báo cáo: HUD trong game theo `02-hud.png` (RO Soft Fantasy), 19/11/2026

Nhánh làm: worktree, merge vào `ro-fantasy-heroes`. Exe: `D:\temp\player-hud-final\Zenonia.exe` (build từ bản sao `D:\temp\zu-copy-hud`).

## Bật khi nào
- HUD mới là mặc định của bản cảm ứng (exe Windows dev, Android/iOS) khi `UiStyle.Ro` (mặc định) và HUD Adventure Paper tắt (`--ui-new hud` / `--ui new` vẫn ra HUD Paper). `--ui-style classic` = HUD cũ. Bản web (`--hud web`, WebGL) giữ HUD cũ (hotbar).
- Cổng: `RoHud.Active` (đọc mỗi frame).

## Art (ChatGPT, tài khoản Tay, chat https://chatgpt.com/c/6aff0d5c-f0a0-83ec-8726-10aa0543ada0)
- 4 sheet nền `#FF00FF`, prompt có câu tông RO Soft Fantasy + kèm `02-hud.png`: `Assets/ArtLibrary/ui/ro-soft-hud/src/ro-hud-{a-status-party-menu,b-map-wanted-potion,c-controls-chat,d-icons}-chatgpt-1.png`.
- Cắt bằng `Tools/cut-ui-paper-sheet.py` (`Tools/ui-ro-soft-sheets/ro-hud-*.json`) ra 62 mảnh `Assets/Resources/Sprites/ui/ro-soft/hud-*` (9-slice ở `ro-soft-borders.json` -> `RoSoftSprites.Borders.cs`). Contact: `docs/reference/ro-soft-hud/art-contact/`. Không vẽ hình nào bằng code.

## Code (mới)
| File | Việc |
|---|---|
| `RoHud.cs` | cổng `Active`, bảng bố cục, helper `Art`, `Pill` (box model padding 3/12, gap 4, co theo chữ: `docs/ui-layout-rules.md`), `Bar` (rãnh + lõi trượt trong RectMask2D, đầu bo giữ nguyên) |
| `RoHudStatus.cs` | chân dung tròn + sao, pill Lv, HP/MP/EXP có số và EXP %, anchor `StatusIcons` (buff, HudBuffRow) |
| `RoHudParty.cs` | "Party (n/5)" + mũi tên thu gọn, thẻ đồng đội: chân dung, icon vai trò (vương miện trưởng nhóm / kiếm / chữ thập Cleric), Lv + tên, thanh HP; anchor `StatusIcons` mỗi dòng; chạm tiêu đề mở bảng Party cũ |
| `RoHudMenu.cs` | Bag, Skills, Pet, Quest, More (More: Gacha, Settings), badge điểm skill / "!" nhiệm vụ / đồ mới; tự co ở 4:3 |
| `RoHudMapPanel.cs` + `Minimap.RoRound.cs` | pill tên map, pill kết nối (ping ms online, "Offline"), minimap tròn theo hero, nút "+" đổi zoom (1.6 / 2.4 / 1) |
| `RoHudWanted.cs` | thẻ WANTED: mặt boss, tên, "Lv. N (Boss)" / "Respawns mm:ss" / "Bounty claimed", map; chạm mở bảng Wanted |
| `RoHudControls.cs`, `RoHudSkillSlot.cs`, `TouchControls.RoSoft.cs` | joystick, bình HP/MP + số, nút Attack (giữ, art lúc nhấn), 4 skill trên vòng = 8 ô loadout chia 2 trang (Tab / vuốt), Auto (AutoBattle, art bật/tắt), Run (= Dash) |
| `RoHudBossBar.cs` | thanh máu boss RO (thay thanh IMGUI cũ khi HUD RO bật) |
| `RoHudChatBar.cs` | nút chat + thanh 1 dòng "kênh \| tin mới nhất \| ^", **nằm giữa đáy** (người dùng yêu cầu), co rộng để không đè joystick / cụm skill; khung chat mở ra cũng ở giữa trên |
| `NameTags.RoSoft.cs` | pill tên NPC (búa cho thợ rèn, mặt cười cho NPC khác; ẩn trong dải HUD trên), huy hiệu vàng + thanh HP nhỏ dưới tên mình |
Sửa nhỏ: `RpgHud`, `TouchControls`, `ChatWindow`, `ChatPanelView`, `Minimap` (partial), `PartyHudList` / `PartyPanel` (bảng Party chỉ mở khi chạm tiêu đề), `Hud` (tắt thanh boss cũ), `NetClient.RttMs`, `TownNpc.Villagers`.

## Nút Bag (agent túi đồ)
Nút Bag chỉ gọi `TouchControls.Press(GameKey.Inventory)`: mở đúng cửa sổ túi mới của agent túi đồ.

## Anchor icon trạng thái (agent Venom Drain)
- Dưới thanh HP mình: `RoHudStatus.StatusIcons` (GameObject "RoHudStatus/StatusIcons", đang chứa HudBuffRow).
- Mỗi dòng Party: `RoHudParty.StatusIconsOf(i)` ("RoHudParty/Mate{i}/StatusIcons", bên phải tên).
- Code poison của agent đó chưa có trên `ro-fantasy-heroes` lúc merge, nên chưa nối.

## So sánh với ChatGPT (cùng chat)
| Vòng | Điểm | Ảnh |
|---|---|---|
| 1 | 7.5/10 (thiếu Party vì offline, HUD trên nhỏ, menu lệch) | `docs/reference/ro-soft-hud/compare-round1.png` |
| 2 | 8.6/10 | `compare-round2.png`, `wanted-fight-party-1280x853-round2.png` |
| 3 | 9.0/10, "đủ tốt để chơi thử", không còn lỗi nặng (16:9 9.1, online + Party 8.9, 4:3 8.6) | `compare-round3.png`, `town-oakhaven-960x720-4x3-round3.png`, `town-oakhaven-1600x720-20x9-round3.png` |
Sau vòng 3 đã sửa thêm: chân dung thu nhỏ thấy vai, thẻ Party cao hơn + thanh HP dày hơn, chữ Wanted / Auto / Run to hơn, chữ Attack nhích lên, joystick trong hơn, pill NPC to hơn. Chưa gửi ChatGPT chấm lại bản cuối.

## Ảnh đã tự xem
`docs/reference/ro-soft-hud/`: làng Oakhaven 1280x720 (3 vòng + `town-oakhaven-1280x720-final.png`), 960x720 (4:3), 1600x720 (20:9), map farm Willowbrook (Wizard đang đánh), trận Wanted Mire Hydra Queen online có Party (1280x853 vòng 2-3, bản cuối 3440x1440 ultrawide `wanted-fight-party-3440x1440-ultrawide-final.jpg`).

## Kiểm tra
- `dotnet test`: ZenoniaSkills 442/442, ZenoniaNet 455/455.
- Build Unity từ bản sao: 0 lỗi.

## Chưa kiểm trong game
- Bấm thật từng nút (Bag, Skills, Pet, Quest, More, "+", Auto, Run, đổi trang skill bằng vuốt, chạm thẻ Wanted, thu gọn Party, mở chat) — chỉ chụp ảnh, chưa thao tác.
- Khung chat mở (panel cũ màu vàng navy, chỉ dời ra giữa), thanh boss khi HP > 65535 (giao thức chặn 65535 nên thanh gần rỗng, lỗi cũ).
- Máy thật Android/iOS, tai thỏ / safe area.

## Câu hỏi chưa giải quyết
1. Kênh "Ch.1": game không có kênh, đang hiện ping / "Offline". Có làm hệ thống kênh không?
2. Party "n/5": mockup ghi 3/3 nhưng game cho 5 người; thẻ chỉ hiện đồng đội (quy tắc 17/11). Có hiện cả mình như mockup không?
3. Bản web (PC hotbar) có chuyển sang HUD RO không?
4. Khung chat mở ra và toast thông báo vẫn là art cũ; có xin ChatGPT vẽ theo RO không?
5. Run đang là Dash (lướt). Có cần chế độ chạy bật/tắt riêng không?
