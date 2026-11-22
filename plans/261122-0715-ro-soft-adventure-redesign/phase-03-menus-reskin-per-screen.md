# Phase 03: Menu từng màn

Ưu tiên: P0 (túi đồ) / P1 (còn lại). Trạng thái: chưa. Đọc hết `docs/ui-layout-rules.md` trước khi làm; chụp trong game so mockup cùng cỡ trước khi báo xong (không cần QA vét hết: người dùng tự test UI, nhưng PHẢI tự xem ảnh).

## Cơ chế
- Màn RO riêng (`InventoryRo*`, `PetRoWindow*`, `RoGachaWindow`, `RoWantedBoard`, `WorldMapWindow*`, `RoHud*`, `CharacterListScreen`, `CharacterCustomizeScreen`, `GearShopWindow`, `SkillTreeWindow`) đọc `RoSoftTheme` + `RoSoftSprites` => tự đổi theo phase 01-02, chỉ chỉnh chỗ màu viết cứng + hoa văn riêng.
- Màn cổ điển navy (Settings, Quest, NPC, shop thuốc, Trade, Party, Waypoint...) được `RoSoftSkin.Light` phủ da qua `WindowsHost` => đổi map màu trong `RoSoftSkin` là đổi cả nhóm; màn nào cần bố cục như mockup Adventure thì dựng lại sau.
- Màn Adventure Paper cũ (`ArmoryWindow`, `TrainingHallWindow`, `PetCampWindow`, `LumiCounterWindow`, `WantedBoardWindow`, `LevelUpWordmark`, chỉ chạy với `--ui new`): bỏ / để ngoài phạm vi (RO Soft đã thay chúng làm mặc định từ 19/11), trừ `LevelUpWordmark` cần bản mới.

## Danh sách màn (31) + mẫu Adventure + cách làm

| # | Màn | Code hiện tại | Mẫu Adventure | Cách | Art mới |
|---|---|---|---|---|---|
| 1 | Túi đồ & trang bị | `InventoryRo*` | 02-bag + test `bag-adventure-chatgpt-1.png` | token + kit; chân dung nền vòm đá | kit K1-K3 |
| 2 | Kỹ năng | `SkillTreeWindow` (RO) | 06-skill | token + kit | nền huy hiệu class mờ |
| 3 | Pet | `PetRoWindow*` | 07-pet | token + kit; thay `pet-*` (53 mảnh) | sheet pet |
| 4 | Gacha / Triệu hồi | `RoGachaWindow` | 08-gacha | token + kit; sân khấu ấm | sheet gacha (23) |
| 5 | Bảng Truy nã | `RoWantedBoard` | 09-wanted-board | thẻ giấy + đinh ghim | sheet wanted (18) |
| 6 | Cài đặt | `SettingsWindow` (skin) | 10-settings | dựng lại bố cục 2 cột + toggle mới | kit |
| 7 | Sổ nhiệm vụ | `QuestLogWindow` (skin) | 11-quest-npc | dựng lại danh sách trái / chi tiết phải | kit |
| 8 | Nhận nhiệm vụ | `QuestOfferWindow` (skin) | 11-quest-npc | hộp thoại vừa + chân dung NPC | chân dung NPC |
| 9 | Hộp thoại NPC | `NpcDialogueBox` (skin) | 11-quest-npc (đáy) | thanh giấy + tên ruy băng nâu | kit |
| 10 | Shop đồ (Thợ rèn) | `GearShopWindow` (RO) | 03-shop | token + kit; chân dung NPC | chân dung |
| 11 | Shop thuốc | `PotionShopWindow` (skin) | 03-shop | chung khuôn shop đồ | |
| 12 | Giao dịch | `TradeWindow` (skin) | 13-trade | dựng lại 3 cột như mockup | kit |
| 13 | Lời mời giao dịch | `TradeRequestWindow` | 12-social (bảng lời mời) | khung lời mời chung | |
| 14 | Party: mời / đang mời / nhận | `PartyInviteWindow`, `RoHudParty` | 12-social | khung lời mời chung + pending | |
| 15 | Thẻ người chơi / Xã hội | (chưa có, Player Card ở menu Invite) | 12-social | làm mới theo `ui-overhaul-plan.md` | |
| 16 | Lên cấp | `LevelUpWordmark` (Paper) | 14-level-up | banner "LEVEL UP" đồng + bảng chỉ số tăng | wordmark mới |
| 17 | Chọn nhân vật / class | `HeroSelectScreen` | 04-character-create (trái) | danh sách class có chân dung | 6 chân dung class |
| 18 | Tạo nhân vật (tóc, màu) | `CharacterCustomizeScreen` (RO) | 04-character-create | token + kit, bệ đá giữa | nền quảng trường |
| 19 | Danh sách nhân vật | `CharacterListScreen` (RO) | 04 | token + kit | |
| 20 | Đăng nhập / tài khoản / kết nối | `AccountScreenGui`, `ConnectScreenGui` (IMGUI) | (không có) | khung vừa + nút teal | |
| 21 | Đổi hero | `HeroChangeScreen` | gộp vào #17 | bỏ màn riêng | |
| 22 | Bản đồ lớn | `WorldMapWindow*` (RO, `wm-*` 19 mảnh) | (không có; theo kit) | giấy da bản đồ, pill nâu | sheet wm |
| 23 | Danh sách dịch chuyển (warp) | `WaypointWindow` (skin) | (theo kit) | hộp thoại vừa, hàng có icon map | |
| 24 | Rương / nhặt đồ | `ChestLoot` (+ toast) | 14-level-up (phần thưởng) | popup phần thưởng ô item mới | |
| 25 | Toast thông báo | `NotificationToast` | kit toast | giấy sáng viền 1-2 px | |
| 26 | Chat mở rộng | `RoHudChatPanel`, `ChatWindow` | 01-hud (đáy trái) | khung tối nhẹ ấm cho HUD | |
| 27 | Menu "More" / menu gọn | `RoHudMenu` | 01-hud (hàng trên phải) | icon nâu-đồng | icon menu |
| 28 | Xác nhận (vứt đồ, thoát...) | trong từng màn | kit hộp xác nhận | 1 câu hỏi + teal / đỏ | |
| 29 | Tooltip item / so sánh | `InventoryRo*`, trade inspect | 02-bag (cột phải) | tooltip giấy | |
| 30 | Party list trên HUD | `RoHudParty` | 01-hud | chân dung vòng đồng | |
| 31 | Thanh boss + thẻ Wanted HUD | `RoHudBossBar`, `RoHudWanted` | 01-hud-boss-fight | thanh HP đỏ trầm viền nâu | sheet boss bar |

## Thứ tự
1 (mẫu, người dùng duyệt) -> 6, 7-9, 10-11 (hay gặp) -> 2-5 -> 12-15 -> 16-19 -> 20-31.

## Thành công
Mỗi màn: chụp 1280x720 cạnh mockup cùng số (`docs/reference/ro-soft-adventure-redesign/screens/<#>-*.jpg`), checklist ≥ 9/10, padding đúng `ui-layout-rules.md`.
