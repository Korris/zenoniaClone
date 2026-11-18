# Báo cáo: menu RO Soft Fantasy trong game (19/11/2026)

Branch `worktree-agent-a1d61b976843642b1` (đặt lại lên `unity` 3278e4be trước khi làm). Exe: `D:\temp\player-ro-menu\Zenonia.exe`. Kế hoạch: `plans/261119-ro-soft-menu/plan.md`.

## Cách làm
- Không vẽ PNG mới. 4 file mới trong `Assets/Scripts/`:
  - `UiStyle.cs`: công tắc `--ui-style ro|classic`, **mặc định ro**.
  - `RoSoftTheme.cs`: 8 màu của style board + màu chữ / đường kẻ suy ra, tấm HUD xanh đen 90 %, font Nunito.
  - `RoSoftSprites.cs`: vẽ bằng code khối bo góc 9-slice (card 20 px có bóng, section, row, ô item 10 px, nút mint / sky / coral / vàng / xám, tab viên thuốc, nút X tròn, tấm HUD tối).
  - `RoSoftSkin.cs`: lượt skin chạy mỗi frame khi cửa sổ hiện (chỉ phần đang hiện): thay sprite navy/vàng bằng khối RO, chữ sang Nunito + màu mực (vàng / xanh / đỏ giữ nghĩa, đậm hơn), tiêu đề màu mực, chữ trên nút màu trắng, nút Discard / Cancel / Decline màu coral, lớp tối sau cửa sổ nhẹ hơn.
- Sửa nhỏ: `WindowsHost` (gắn skin cho các layer menu + nút Talk), `InventoryPanel`, `SkillTreeWindow` (1 dòng mỗi file), `GearShopWindow` (tab Tier mint / trắng), `SettingsToggle` (ON mint / OFF xám), `RpgHud` (tấm trạng thái xanh đen bo góc, viền chân dung sáng). Logic không đổi.

## Bật / tắt
- Mặc định: RO Soft Fantasy.
- `--ui-style classic`: navy + vàng như cũ.
- `--ui new` / `--ui-new bag,...`: màn Adventure Paper như trước (không bị skin). Vậy so được cả 3 style.

## Màn đã làm và đã xem tận mắt trong ảnh chụp (`docs/reference/ro-soft-menu-ingame/`, so sánh mockup | game trong `so-sanh/`)
| Màn | Ảnh | Còn lệch mockup |
|---|---|---|
| Túi đồ | `tui-do.jpg` (`tui-do-classic-de-so.jpg` để so) | Bố cục cũ 3 cột (giống mockup), chưa có tab lọc dạng icon, Lực chiến, ô trống dấu +; nhãn độ hiếm vẫn là art vàng cũ; ô lưới 5x4 có trang chứ không thấy 50 ô một lần |
| Shop đồ / thuốc | `shop-do.jpg`, `shop-thuoc.jpg` | Chưa có chân dung NPC + bóng thoại, cột lọc, khung so sánh + số lượng; vàng chưa có dấu phẩy |
| Trade | `trade-ca-hai-khoa.jpg`, `trade-xem-do.jpg`, `trade-loi-moi.jpg`, `menu-nguoi-choi-invite-trade.jpg` | Chưa có chân dung + tên lớp lớn, tab túi, dòng lưu ý; nút Unlock màu coral (mockup mint); mũi tên trang vẫn art vàng; `net-trade-test -Ui` 50/50 PASS |
| Sổ nhiệm vụ | `so-nhiem-vu.jpg` | Danh sách 1 cột, chưa có tab Đang làm / Có thể nhận / Đã xong và khung chi tiết + phần thưởng dạng ô |
| Cài đặt | `cai-dat.jpg` | Chưa có nhóm Tương tác (mời Party / Duel / Trade) như mockup |
| Bảng skill (cũ) | `bang-skill.jpg` | Gần mockup về màu; bố cục cũ |
| Pet (cũ) | `pet.jpg` | Vài chữ nhỏ màu nhạt (coins / crystals / food) hơi khó đọc |
| Hộp thoại NPC | `hoi-thoai-npc.jpg` | Chưa có nút "Nhận nhiệm vụ / Để sau" kiểu mockup (chỉ skin) |
| HUD | `hud.jpg` | Chỉ tấm trạng thái thành nền xanh đen bo góc; joystick, nút đánh, skill, minimap vẫn art cũ |

Party panel / lời mời party, Quest offer, Trade request đã gắn skin nhưng chỉ thấy Trade request + menu Invite/Trade trong ảnh; Party panel và Quest offer CHƯA xem.
Chưa làm: chọn class / Level Up, gacha, Wanted board, các màn Adventure Paper (Armory, Training Hall, Pet Camp, Lumi, Wanted) vì chúng dùng PaperKit riêng.

## Kiểm tra
- `dotnet test` ZenoniaSkills 361/361, ZenoniaNet 422/422 (máy này cần SDK 10 ở `C:\Users\korri\.dotnet\dotnet.exe`; SDK 9 ở Program Files báo NETSDK1045).
- Build từ `D:\temp\zu-copy-ui`: Succeeded, 0 lỗi.

## Câu hỏi chưa giải quyết
1. Giữ RO là mặc định hay để sau cờ `--ui-style ro` cho tới khi người dùng duyệt?
2. Có làm lượt 2 đổi bố cục theo mockup (shop có NPC + so sánh, quest log có tab + chi tiết, trade có chân dung) không? Lượt này chỉ đổi phần nhìn.
3. HUD chiến đấu (joystick, nút skill, minimap) có đổi sang tấm tối RO không, hay giữ HUD gọn Adventure Paper?
4. Các màn Adventure Paper (Armory, Training Hall, Pet Camp, gacha, Wanted) có chuyển sang RO không (cần viết lại theo PaperKit hoặc thêm skin cho PaperKit)?
5. Nút Unlock trong Trade: coral (hiện tại) hay mint như mockup?
