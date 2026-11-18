# Báo cáo: thay khung / nút RO Soft vẽ code bằng art ChatGPT (19/11/2026)

Branch `worktree-agent-a73366876b67d4e76` (đặt lại `git reset --hard unity` = 1258393d trước khi sửa). Exe: `D:\temp\player-ro-menu2\Zenonia.exe`.

## Cắt art
- Tool: mở rộng `Tools/cut-ui-paper-sheet.py` (không tạo tool mới): `borders_file`, `contact_dir`, `ui_meta` (.meta không nén, không mipmap), `rim_from_body` (2 px mép lấy màu bên trong, hết viền tím), `flatten_edges` (mép dải 9-slice đồng đều, hết đốm khi kéo rộng). Cấu hình: `Tools/ui-ro-soft-sheets/ro1-buttons-slots.json`, `ro2-panels-bars.json`; border → `ro-soft-borders.json` → `Assets/Scripts/RoSoftSprites.Borders.cs` (`python Tools/export-ui-paper-borders-to-csharp.py ro-soft`).
- 26 mảnh trong `Assets/Resources/Sprites/ui/ro-soft/` (cắt ở 2x cỡ hiển thị, sprite 200 ppu cho nét):
  - nút: `btn-mint`, `btn-mint-pressed`, `btn-sky`, `btn-sky-pressed`, `btn-coral`, `btn-gold`, `btn-grey`, `btn-ivory`
  - `tab-on`, `tab-off`, `btn-close`, `btn-plus`, `toggle-on`, `toggle-off`, `slot`, `slot-selected`, `slot-empty`
  - `window-header`, `panel`, `panel-glass`, `hud-plate`, `bar-red`, `bar-blue`, `bar-gold`, `dropdown`, `divider-gem`
- Đếm điểm hồng còn sót: 0 ở mọi mảnh. Contact: `docs/reference/ro-soft-menu-ingame/art-contact/ro1-buttons-slots-contact.png`, `ro2-panels-bars-contact.png` (đã xem, nền tối + sáng + mẫu kéo giãn).

## Code
- `RoSoftSprites.cs`: bỏ toàn bộ phần vẽ code; mỗi `Shape` lấy 1 mảnh art (giữ API `Get`, `ShapeOf`, `IsButton`, `Close`, `ResetState`; `RoSoftSkin`, `GearShopWindow`, `RpgHud` không phải sửa). Thiếu mảnh thì log 1 lần, trả null, không vẽ thay.
- Bảng: Card / Section → `panel`; Row → `btn-ivory`; Cell → `slot`; Primary → `btn-mint`; Secondary → `btn-sky`; Danger → `btn-coral`; Gold → `btn-gold`; Disabled → `btn-grey`; TabOn / TabOff → `tab-on` / `tab-off`; Pill → `tab-off`; SectionLocked → `slot-selected`; Night / NightCell → `hud-plate`; Glass → `panel-glass`; nút X → `btn-close`.
- `SettingsToggle.cs`: RO dùng công tắc art `toggle-on/off` (bỏ chữ ON/OFF).

## Màn đã xem tận mắt (`docs/reference/ro-soft-menu-ingame/*-art.jpg`, so sánh `so-sanh/*-art.jpg`: mockup | trước | sau)
Túi đồ, shop đồ, shop thuốc, sổ nhiệm vụ, cài đặt, bảng skill, pet, HUD (tấm trạng thái), Trade: lời mời, menu Invite/Trade, xem đồ, cả hai khoá. Chưa xem: Party panel, Quest offer, hộp thoại NPC (không chụp lượt này).
- Ghi chú: bảng skill có dòng chữ đè nhau ("MP 17 Cooldown 3s" đè chữ mờ) — lỗi bố cục có từ trước, không do art.

## Kiểm tra
- Build từ `D:\temp\zu-copy-ui`: Succeeded, 0 lỗi (2 lần, lần 2 sau khi sửa đốm mép).
- `net-trade-test -Ui`: 50 passed, 0 failed.
- dotnet test: ZenoniaSkills 361/361, ZenoniaNet 422/422.
- Sổ lỗi `docs/sprite-rules.md`: thêm dòng "UI RO Soft (sheet cắt)".

## Mảnh art còn thiếu (đang mượn mảnh gần nhất)
1. Khung section / row riêng (nhẹ hơn panel, không bóng) — đang dùng `panel` (panel lồng panel) và `btn-ivory` cho dòng danh sách.
2. Ô item tối cho HUD (NightCell) — đang dùng `hud-plate`.
3. Khung khoá giao dịch (viền bạc hà, nền bạc hà nhạt) — đang dùng `slot-selected` viền vàng.
4. Ô nhãn / thanh tài nguyên viên thuốc (nameplate, ô vàng, tiêu đề Trade) — đang dùng `tab-off`.
5. Thanh máu: khung rỗng + lõi tách riêng (art hiện lõi dính khung tối) — HUD vẫn dùng thanh phẳng cũ, chưa gắn.
6. Thanh cuộn (rãnh + nút kéo), nút mũi tên trang trái/phải (Trade, túi đồ vẫn art vàng cũ), nút vuông nhỏ chỉ có icon, tab nhỏ / tab dọc, checkbox / radio, tooltip có mũi nhọn, huy hiệu số đỏ, viền độ hiếm kiểu RO.
- Đã cắt nhưng chưa dùng: `window-header` (khung có dải tiêu đề + cánh), `btn-plus`, `slot-empty`, `btn-*-pressed`, `dropdown`, `divider-gem`, 3 `bar-*`.

## Câu hỏi
1. Dùng `window-header` (dải tiêu đề bạc hà + cánh) cho cửa sổ lớn không? Cần chỉnh vị trí tiêu đề từng cửa sổ cho khớp dải.
2. Nút đang nhấn: có đổi sang `btn-*-pressed` khi bấm (thay tint mặc định) không?
3. HUD: gắn art thanh máu khi có khung rỗng + lõi riêng?
