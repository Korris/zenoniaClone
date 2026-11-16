# Phase 05: Thùng đồ "Armory Carousel"

## Context Links
- Mockup: `chatgpt/bag-final-mockup.png` (+ mockup trạng thái thiếu xin ở phase 02)
- Hiện tại: `ingame/gear-equipment-view.png`, `gear-equipment-item-selected.png`, `gear-bag-full-item-selected.png`
- Code: `InventoryPanel.cs` (252 dòng, đã quá 200: mở / đóng, điều hướng phím, Activate, Enhance), `InventoryPanel.Bag.cs` (lưới 5x4, supply, lọc, sort, trang), `InventoryPanel.Detail.cs` (so sánh, enhance preview, Discard), `InventoryPanel.Loadout.cs` (hero + 7 ô mặc + Stats), `InventoryPanel.DevDowngrade.cs`, `BagWidgets.cs`, `LootSprites.cs`, `docs/bag-redesign.md`
- Phase 01 (kit, UiScreens), 02 (A1, K3, mockup thiếu)

## Overview
- Ưu tiên: P1 (người dùng ưu tiên nhất). Effort: L. Status: code xong 16/11, công tắc Bag vẫn TẮT (chờ người dùng duyệt; chưa commit, chưa code review, chưa chạy online).
- Màn toàn cảnh phòng vũ khí: 7 bệ trang bị quanh hero, kệ thẻ cuộn ngang "Drag to Equip", lọc Type + Sort, bảng giấy ghim so sánh, Equip / Discard, Stats, Enhance Equipped, Bag x/50, Page.

## Key Insights
- Logic thùng đồ đang trộn trong các partial view của `InventoryPanel`. Bước đầu: **tách logic ra lớp không có view** (refactor thuần, cửa sổ cũ vẫn chạy y nguyên) để view mới và cũ cùng gọi (DRY), đồng thời đưa `InventoryPanel.cs` về < 200 dòng.
- Dữ liệu đã có: 7 ô mặc (Necklace, Armor, Gloves, Boots, Weapon, Shield, Pet; đúng 7 bệ mockup), túi 50 ô (`PlayerState.InventorySize`), vàng / bình / đá, so sánh chỉ số với món đang mặc, enhance preview (`Preview`), yêu cầu Lv, Discard, Sort, lọc theo ô mặc, trang, online qua `ServerInventoryActions`.
- Game CHƯA có: kéo thả lên bệ; Sort nhiều tiêu chí (mockup "Rarity (High to Low)"); lọc theo loại Consumable / Material (hiện lọc theo ô mặc); cờ "món mới" (phase 04). Pet trong túi: pet đã là dữ liệu tài khoản (16/11) -> bệ Pet hiện pet Combat, chạm mở Pet Camp (giả định 6); lọc "Pet" chỉ hiện nếu túi còn loại món pet.
- Tên / số trong mockup là minh hoạ (Dragon Sword, gem 9,999): thanh trên chỉ hiện vàng, đá cường hoá, bình HP / MP (giả định 5). Nút "Skill" trên cùng mở Skill (thay tab Skill cũ).
- Kệ thẻ thấy ~10 món: giữ trang (Page 1/3) + nút chuyển dạng lưới xem đủ 50 ô (giả định 1, cần mockup).

## Requirements
- Chức năng: chọn thẻ -> bảng so sánh (ATK, CRIT... món này vs đang mặc, chênh lệch đỏ / lục); Equip (hoặc kéo thẻ lên bệ, hoặc chạm đúp); Discard có xác nhận; chọn bệ -> bảng món đang mặc + Unequip + Enhance preview (mockup thiếu); Enhance Equipped mở enhance món bệ đang chọn; Stats popup; lọc Type 10 loại; Sort dropdown (Rarity, Level, Type); trang + dạng lưới; thẻ khoá "Lv. 70 Required"; thẻ bình / đá có số lượng.
- Phím: mũi tên di chuyển trên kệ / lên bệ, J / Space kích hoạt, B / I bật tắt, Esc đóng (giữ như cũ). Game tạm dừng khi mở (giữ).
- Mobile = PC cùng bố cục; khung nội dung 16:9 co theo 4:3, nền phủ kín 21:9.
- Online: mọi hành động đi qua đường hiện có (offline = state + save, online = request server). Không đổi protocol.

## Architecture
```
BagModel (thuần, không UnityEngine.UI): danh sách ô (supply + túi), lọc Type, sort, trang, ô đang chọn
BagActions: Equip / Unequip / Enhance / Discard / UsePotion  -> offline PlayerState+save | online ServerInventoryActions (code hiện có, chỉ dời chỗ)
ArmoryWindow (view mới) ─┬─ .Pedestals (7 bệ + hero xem trước, dùng lại cách vẽ hero của InventoryPanel.Loadout)
                         ├─ .Shelf (kệ thẻ, mũi tên, trang, dạng lưới)
                         ├─ .ComparePanel (giấy ghim: so sánh / món đang mặc + enhance preview)
                         ├─ .Filters (Type chip + Sort dropdown) ; .Drag (IBeginDrag/IDrag/IEndDrag -> thả lên bệ / bệ trung tâm)
InventoryPanel (cũ) -> gọi BagModel/BagActions ; UiScreens.Bag chọn cũ / mới
```
Luồng: chạm / kéo / phím -> ArmoryWindow -> BagActions -> PlayerState đổi (hoặc server đẩy RpgState) -> dấu vân tay đổi -> vẽ lại.

## Related Code Files
- Tạo: `BagModel.cs`, `BagModel.Sort.cs`, `BagActions.cs`, `ArmoryWindow.cs`, `ArmoryWindow.Pedestals.cs`, `ArmoryWindow.Shelf.cs`, `ArmoryWindow.ComparePanel.cs`, `ArmoryWindow.Filters.cs`, `ArmoryWindow.Drag.cs`, `ArmoryWindow.GridView.cs`, `DebugCapture.UiBag.cs`
- Sửa: `InventoryPanel.cs`, `InventoryPanel.Bag.cs`, `InventoryPanel.Detail.cs`, `InventoryPanel.Loadout.cs` (chỉ đổi sang gọi BagModel / BagActions)
- Xoá (phase 13, sau khi duyệt): `InventoryPanel*.cs` view cũ, `BagWidgets.cs` (nếu hết người dùng), font Crimson Text (nếu hết người dùng)
- Sở hữu độc quyền: các file trên. Phase 06 dùng lại thẻ / kệ qua PaperKit, không sửa file Armory.

## Implementation Steps
1. Refactor: tách `BagModel` + `BagActions` khỏi InventoryPanel; chụp lại 3 ảnh `ingame/gear-*.png` cùng cờ (`--open-inventory --fill-bag --bag-item --select-worn`) và so bằng mắt: phải giống trước.
2. Có mockup thiếu (phase 02) mới làm bước 3+.
3. `ArmoryWindow` khung + nền A1 + thanh trên (tiêu đề, tài nguyên, Skill, Close).
4. Bệ + hero xem trước; chọn bệ -> ComparePanel chế độ "đang mặc".
5. Kệ thẻ + trang + mũi tên; thẻ theo độ hiếm (K3a / K3b), khoá Lv, số lượng.
6. ComparePanel: bảng so sánh, mô tả, Equip / Discard (+ xác nhận), Unequip / Enhance preview.
7. Filters + Sort; Drag; dạng lưới.
8. Điều hướng phím và chạm đúp như cũ.
9. Cờ chụp: dùng sẵn `--open-inventory`, `--fill-bag`, `--bag-item`, `--select-worn`, `--select-pet`, `--give-stones`, `--plus`; mới `--bag-view grid`, `--bag-filter <type>`, `--bag-sort <key>`, `--bag-confirm discard`, `--bag-drag-demo` (dừng giữa lúc kéo).
10. Build bản sao, chụp 4 tỉ lệ (offline + 1 lần online `--net-bot` vai `enhance`), tự xem từng ảnh so mockup; test tay kéo thả bằng chuột.

## Todo List
- [x] Tách BagModel / BagActions (+ `BagText.cs`, `InventoryPanel.Input.cs`), cửa sổ cũ chụp lại giống trước
- [x] Nhận mockup trạng thái thiếu (`bag-state-equipped-item-enhance-preview.png`, `bag-state-grid-50-slots-consumable-selected.png`)
- [x] Khung + thanh trên + nền A1
- [x] 7 bệ + hero; kệ thẻ + trang
- [x] ComparePanel (món trong túi / món đang mặc + enhance / bình, đá / gợi ý) + xác nhận Discard
- [x] Lọc Type, Sort, kéo thả, dạng lưới
- [x] Phím + chạm đúp
- [x] Cờ chụp, ảnh 16:9 / 20:9 / 4:3, tự xem (`docs/reference/ui-redesign-261116/phase-05-bag/`)
- [ ] Online (`--net-bot` vai enhance): CHƯA chạy
- [ ] Test tay kéo thả bằng chuột thật: CHƯA (mới chạy qua cờ `--bag-drag-drop`, cùng handler)
- [ ] `dotnet test` 2 bộ: không chạy, phase này không sửa file nào hai bộ test biên dịch (Rpg/, Net/Protocol, Maps/)

## Ghi chú sau khi làm (16/11)
- File thật: `BagModel.cs`, `BagModel.Sort.cs`, `BagActions.cs`, `BagText.cs` (dùng chung 2 giao diện); `ArmoryWindow.cs` + `.TopBar`, `.Pedestals`, `.Shelf`, `.GridView`, `.Filters`, `.ComparePanel`, `.CompareShow`, `.Actions`, `.Input`, `.Drag`, `.Capture`; widget `ArmoryPedestal.cs`, `PaperBagDropdown.cs` (lớp `PaperDropdown`, phase 06 dùng lại được); `DebugCapture.UiBag.cs`; `InputAdapter.WindowKeys.cs` (`WindowPressed`: phím cho cửa sổ modal tự đọc); `InventoryPanel.Input.cs` (tách để `InventoryPanel.cs` < 200 dòng).
- Sửa nhỏ ngoài danh sách: `InputAdapter.cs` (1 dòng: B đóng được Thùng đồ mới), `TradeWindow.cs:259` (`UiWindows.CloseRegistered()`), `PaperArt.Pieces.cs` (tên mảnh `bag/*` + `Dropdown`).
- Art A1 đã cắt và đã nối (16/11 chiều): nền `bag/armory-background`, bệ giữa `bag/dais-center`, `bag/pedestal(-selected)`, `bag/pedestal-label(-selected)`, `bag/plate-level-red`, `bag/shelf-wood`, `bag/sign-bag-pouch`, `bag/sign-enhance-anvil`, `bag/tooltip-drag-hand`. Đã cắt nhưng CHƯA dùng: `bag/icon-filter-*`, `bag/icon-stat-*` (chip lọc và bảng chỉ số chỉ có chữ: hàng lọc không đủ chỗ cho icon + ô Sort), `bag/resource-pill-dark` (chip tài nguyên của kit vẫn trỏ `kit/chip-resource`).
- Bật màn: `--ui-new bag`; duyệt xong thì đổi mặc định trong `UiScreens.useNew`.
- Khác mockup: hàng lọc ở dạng lưới nằm TRÊN bảng gỗ (mockup: trong bảng); lưới 10 x 6 ô (mockup 13 x 5) vì giữ bảng giấy bên phải cùng bề rộng; không có chip gem, nút "+", pill "Common" / "Cooldown" của bình (game không có dữ liệu đó); sao trên thẻ = bậc độ hiếm (1-4); "Tidy Bag" trong ô Sort = nút Sort cũ (xếp lại túi thật).
- Build + ảnh cuối: exe thật `C:/temp/player-ui-p05/Zenonia.exe` (BUILD RESULT: Succeeded). Giữa chừng ổ C hết chỗ, đã được dọn.

## Success Criteria
- Mọi thao tác cũ (mặc, tháo, cường hoá, bỏ, dùng bình, lọc, sort, trang, Stats) chạy trên màn mới offline + online.
- Kéo thẻ lên bệ đúng loại thì mặc; sai loại thì bật về + toast.
- Ảnh khớp bố cục `bag-final-mockup.png`; chữ đọc được ở 1600x1200; không file nào > 200 dòng.

## Risk Assessment
| Rủi ro | KN x TĐ | Giảm thiểu |
|---|---|---|
| Refactor làm hỏng thùng đồ cũ (đang dùng thật) | Trung bình x Cao | Bước 1 là refactor thuần, chụp so trước / sau, chạy test online enhance |
| Kéo thả xung đột cuộn kệ trên mobile | Trung bình x Trung bình | Kéo dọc (lên bệ) = kéo thẻ; kéo ngang = cuộn kệ; ngưỡng 12u |
| Nền toàn cảnh nặng bộ nhớ trên mobile | Thấp x Trung bình | Load khi mở, `Resources.UnloadAsset` khi đóng; nén theo nền tảng, không mipmap |
| 4:3 chật: bảng so sánh đè bệ | Trung bình x Trung bình | Co cả khung 16:9; nếu < 900u rộng thì bảng so sánh thành lớp phủ trên kệ |

## Security Considerations
- Server vẫn quyết định mọi hành động online (không đổi). Kéo thả chỉ gọi cùng hành động Equip.

## Next Steps
- Phase 06 dùng lại thẻ / bệ / ComparePanel style. Phase 13 xoá view cũ.

## Câu hỏi mở
- Discard có cần xác nhận cho mọi món hay chỉ độ hiếm từ Hiếm trở lên? Đề xuất: mọi trang bị, không cần cho bình.
