# Menu RO Soft Fantasy trong game (19/11/2026)

Yêu cầu người dùng: "sửa lại menu trong game luôn" theo style RO Soft Fantasy (`docs/reference/ui-style-ro-soft-fantasy/`).

## Hiện trạng UI
- Cửa sổ "classic" (navy + viền vàng): dựng bằng `UiKit` (rect, label, bar), `UiWinKit` (sprite `Sprites/ui/win-*.png` 9-slice), `PartyArt`, `TradeArt`, `BagWidgets`/`BagSprites` (vẽ bằng code). Font: LegacyRuntime (Arial), túi đồ CrimsonText. Đây là màn mặc định: shop đồ / thuốc, sổ nhiệm vụ, nhận nhiệm vụ, hộp thoại NPC, Trade, Party, Cài đặt, túi đồ cũ (`InventoryPanel`), bảng skill cũ (`SkillTreeWindow`), pet cũ.
- Màn "Adventure Paper" (`PaperKit`, `PaperTheme`, sprite `ui/paper/`): Armory, Training Hall, Pet Camp, Lumi gacha, Wanted, HUD gọn; chỉ bật bằng `--ui new` / `--ui-new ...` (`UiScreens`).

## Cách làm (ít asset, không đổi logic)
1. `UiStyle.cs`: công tắc `--ui-style ro|classic`, mặc định `ro`. Màn Adventure Paper vẫn bật bằng `--ui new` như cũ, nên so được cả 3 style.
2. `RoSoftTheme.cs`: token màu (8 màu README + màu chữ / đường kẻ suy ra), font Nunito, hàm đổi màu chữ "trên nền tối" sang "trên nền sáng".
3. `RoSoftSprites.cs`: vẽ bằng code các khối bo góc 9-slice (card 20 px có bóng, section 16, row 12, ô item 10, nút mint / sky / coral / vàng có viền dưới đậm, tab viên thuốc, nút X tròn, tấm HUD xanh đen 90 %). Không thêm PNG.
4. `RoSoftSkin.cs`: lượt "skin" chạy mỗi frame khi cửa sổ hiện: thay sprite classic bằng khối RO, chữ sang Nunito + màu mực, chữ trên nút thành trắng, nút Discard/Cancel/Decline thành coral. Gắn 1 dòng ở `WindowsHost` (các layer menu), `InventoryPanel`, `SkillTreeWindow`.
5. HUD classic (`RpgHud`): tấm trạng thái thành tấm xanh đen bo góc 90 %, chữ trắng.

## Thứ tự
Bag → Shop (đồ + thuốc) → Trade → Quest log → Cài đặt, Party, NPC, bảng skill, pet cũ (cùng lượt skin) → HUD.

## Kiểm tra
- `dotnet test` ZenoniaSkills + ZenoniaNet.
- Build từ bản sao `D:\temp\zu-copy-ui` → `D:\temp\player-ro-menu\Zenonia.exe`.
- Chụp bằng cờ có sẵn (`--open-inventory`, `--open-shop`, `--open-quests`, `--open-settings`, `--ui-open skill`, `Tools/net-trade-test.ps1 -Ui`), lưu `docs/reference/ro-soft-menu-ingame/`, ghép mockup | trong game.

## Không làm
Không đổi bố cục từng cửa sổ cho giống hệt mockup (thêm cột lọc, so sánh lớn...) trong lượt này; không đụng Adventure Paper, sprite nhân vật, map, `dev-start.json`.
