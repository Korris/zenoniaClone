# Báo cáo: túi đồ RO Soft Fantasy y hệt mockup 03-bag (19/11/2026)

Exe: `D:\temp\player-inventory\Zenonia.exe` (build từ bản sao `D:\temp\zu-copy-inv`, 0 lỗi). Mockup chuẩn: `docs/reference/ui-style-ro-soft-fantasy/03-bag.png` (người dùng chỉ định).
Chat ChatGPT (Tay, đã kiểm `/api/auth/session` = "Tân Korris"): https://chatgpt.com/c/6aff0808-b4e0-83ec-8fa9-206300f2de5c

## Diễn biến
1. Lượt đầu giữ bố cục cũ, chỉ thay art: ChatGPT vẽ mockup theo bố cục cũ (`mockup-chatgpt-1-adapted-layout-not-used.jpg`). Bỏ khi người dùng bảo làm y hệt 03-bag kể cả chức năng.
2. Dựng lại cửa sổ theo 03-bag. Người dùng chê tab dính đáy, icon xa chữ, pill "Common" xấu -> dựng lại tab / nút / pill bằng LayoutGroup theo `docs/ui-layout-rules.md`.
3. So sánh với ChatGPT 3 vòng: 7.5 -> 8.7 -> 9.1/10 ("ĐÃ ĐẠT mức giống bản duyệt", không còn lỗi lớn). Ảnh `docs/reference/ro-soft-inventory/compare-round1..3.png`.
   - Vòng 1 sửa: phông cột trái mới (ChatGPT vẽ lại: xanh băng, vòm Gothic mờ, vòng phép mỏng), hero to hơn, bảng chỉ số có nền hàng + đường kẻ, khung Effect riêng, ô item lớn nền nâu vàng (art mới), tiêu đề đậm + sao, Lv / Power to hơn.
   - Vòng 2 sửa: hero to thêm, Boots/Hat hạ xuống, bảng hạ thấp, padding Effect, glow nhẹ hơn, mascot nhỏ lại.
   - Vòng 3 sửa nhỏ: hero -4 %, viền khung phải nhạt hơn, nền hàng bớt xanh.

## Chức năng (thật, dữ liệu game)
| Mockup | Game |
|---|---|
| Vàng + nút + | `p.Gold`; nút + báo nguồn vàng (game không bán vàng) |
| Gem + nút + | Pet Crystals (`p.Account.crystals`); nút + báo nguồn crystal |
| 8 ô trang bị | Weapon, Offhand, Armor, Necklace (id "helmet"), Gloves, Pet, Boots, **Hat ở chỗ Ring** (game không có nhẫn) |
| Lv, Lực chiến | Lv thật; Power = ATK x3 + DEF x2 + HP/2 + MP/4 + crit% x20 + level x10 (chỉ hiển thị) |
| Chỉ số / Cường hoá | Stats popup (8 chỉ số thật); Enhance = đồ đang chọn trong ô trang bị, mặc định vũ khí |
| 6 tab | All / Weapon (vũ khí + tay phụ) / Armor (giáp, găng, giày) / Accessory (dây chuyền, mũ, pet) / Consumable / Material (`BagFilter` thêm 3 nhóm) |
| Túi (n/50), Sắp xếp | số thật; Sort: Bag order, Rarity, Level, Type (chỉ đổi cách xem) + Tidy bag (sắp xếp túi thật, online do server) |
| Lưới 8 cột, ô "+" | 8x8 = thuốc / đá trước rồi 50 ô túi; "+" = ô trống, mờ = quá sức chứa |
| Thẻ item, Equipped vs Selected | tên +N, pill độ hiếm, loại, Req. Lv; ATK, DEF, HP, Crit, Speed (+ MP nếu có), mũi tên xanh / đỏ |
| Hiệu ứng | xem trước cường hoá / nguồn mũ / mô tả thuốc |
| Mặc / Cường hoá / Vứt | Equip (Unequip, Use), Enhance, Discard hỏi "Sure?"; online đều qua `BagActions` (server quyết, như cũ) |

Bàn phím / nhấp đúp giữ như cũ. Túi không còn tab Skill (mockup không có): HUD cảm ứng thêm nút Skill mang badge điểm skill; nút Bag dùng balo mới.

## File
- Code: `InventoryPanel.cs`, `.Loadout.cs`, `.Bag.cs`, `.Detail.cs`, `.Input.cs`, `.DevDowngrade.cs` (viết lại), `InventoryRoWidgets.cs` (ô đồ, ảnh, chữ), `InventoryRoBoxes.cs` (tab / nút / pill kiểu CSS), `BagModel.cs` (3 nhóm lọc), `RoSoftSprites.cs` (`NamedAs`), `TouchControls.Menu.cs`, `HudEntryButtons.cs` (balo RO), `DebugCapture.cs` (`--bag-ui stats|sort|discard|tab:N`).
- Art ChatGPT: `Assets/ArtLibrary/ui/ro-soft-inventory/src/` (4 ảnh) -> `Assets/Resources/Sprites/ui/ro-soft/bag-*` bằng `Tools/cut-ui-paper-sheet.py` + `Tools/ui-ro-soft-sheets/ro3-inventory.json`, `ro4-inventory-stage.json`, `ro5-inventory-icons.json`. Tool thêm `glyph_inset` (tách icon tối khỏi ô sáng thành icon trắng tô màu được, cắt sát).
- Ảnh: `docs/reference/ro-soft-inventory/` (`before-ingame.jpg`, `after-*.jpg`, `mockup-03-bag-vs-ingame.jpg`, `compare-round1..3.png`, `art-contact/`).
- Sổ lỗi: `docs/ui-layout-rules.md`, `docs/sprite-rules.md`.

## Kiểm tra
- Build 0 lỗi; đã tự xem: màn chính, Stats popup, Sort list, chọn vũ khí đang mặc (Unequip), tab Consumable (thuốc + Use), Discard "Sure?", HUD (nút Skill có badge, balo mới), so mockup cùng cỡ.
- dotnet test: ZenoniaSkills 442/442, ZenoniaNet 455/455.
- CHƯA xem: online thật với zone server (equip / enhance / discard / tidy qua mạng), màn 4:3 / 21:9 / điện thoại, HUD bản web (`HudEntryButtons`), nhấp đúp / phím mũi tên trên lưới 8 cột, Pet ô (mở cửa sổ Pet).

## Câu hỏi chưa giải quyết
1. Ô Ring trong mockup: game không có nhẫn, đang đặt Hat ở đó. Có muốn thêm loại đồ Ring thật (cần server + dữ liệu đồ) không?
2. Power: công thức tạm chỉ để hiển thị. Có muốn công thức chính thức (dùng cho bảng xếp hạng / server)?
3. Nút + của Gold / Gem chỉ báo nguồn; có muốn nối vào shop / gacha?
4. `--ui-style classic` không còn đưa túi về kiểu navy cũ (túi chỉ còn bản RO). Giữ vậy?
5. Ô thuốc / đá: chi tiết bên phải còn khoảng trống lớn (bảng so sánh ẩn); có cần bố cục riêng?
