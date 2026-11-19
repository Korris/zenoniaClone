# Quy tắc UI: nút, tab, pill (làm như HTML/CSS)

**Trước mọi việc làm hoặc sửa UI (HUD, túi đồ, menu), kể cả khi giao agent: đọc hết file này.** Ghi rõ yêu cầu này trong brief.

Người dùng, 2026-11-19, khi xem túi đồ RO Soft: các nút All, Weapon dính sát đáy, icon cách chữ quá xa, không giống `03-bag.png`; nút "Common" (pill độ hiếm) xấu. Yêu cầu: "làm như html ấy padding đầy đủ rồi border-radius".

## 1. Mỗi nút là một "box" như CSS

Mỗi nút, tab, pill, ô có đủ các thuộc tính sau và ghi thành hằng số, không đặt tay từng chỗ:

| Thuộc tính | Unity uGUI |
|---|---|
| `padding` (top, right, bottom, left) | `HorizontalLayoutGroup` / `VerticalLayoutGroup.padding`, hoặc inset của RectTransform con |
| `gap` giữa icon và chữ | `spacing` của LayoutGroup |
| `border-radius` | sprite 9-slice (`Sprite.border`) có góc bo; mọi cỡ nút dùng cùng bán kính |
| `border` (viền) + `background` | lớp viền + lớp nền trong sprite, hoặc 2 Image chồng |
| căn giữa | `childAlignment = MiddleCenter`; chữ `TextAlignmentOptions.Center` (cả dọc lẫn ngang) |
| `min-height` / `min-width` | `LayoutElement.minHeight/minWidth` |
| co theo nội dung | `ContentSizeFitter` (pill độ hiếm, nhãn) |

Số mặc định (đơn vị tham chiếu 1280x720, scale theo `CanvasScaler`):
- Tab có icon trên chữ (All, Weapon...): padding 8 trên / 8 dưới / 6 trái-phải, gap icon-chữ 2-4, icon 28-32, chữ 13-14. Khối icon+chữ căn giữa dọc trong tab, khoảng trống trên = dưới.
- Nút chữ (Equip, Enhance, Discard, Stats): cao ≥ 44, padding 10 / 18, gap icon-chữ 8, bo góc 12.
- Pill (độ hiếm, Lv, nhãn NPC): cao 22-26, padding 3 / 12, bo góc = nửa chiều cao (hình viên thuốc), co theo chữ.
- Ô đồ: vuông, bo góc 8, gap giữa ô 6, số lượng góc dưới phải cách viền 3.

## 2. Đo trước khi báo xong

- Chụp nút trong game, phóng to, so với mockup cùng cỡ. Khoảng cách trên và dưới nội dung phải bằng nhau (lệch ≤ 2 px); icon-chữ phải sát như mockup.
- Không có chữ hay icon nào chạm viền. Không có nút nào cao hơn hay thấp hơn nút cùng hàng.
- Chữ dài nhất (tiếng Anh) phải vừa: thử "Consumable", "Accessory", "Material".

## 3. Hình nút: ChatGPT hay code

- Ưu tiên: cắt khung nút/tab/pill từ ChatGPT thành sprite **9-slice** (góc bo nằm trong vùng border), rồi bố cục bằng LayoutGroup như mục 1. Không nướng chữ hay icon vào hình nút.
- Được phép làm nút bằng code nếu đạt chất lượng như HTML/CSS: bo góc mượt (khử răng cưa), viền 1-2 px màu nâu đậm mềm, nền gradient dọc nhẹ, bóng đổ mềm 2-3 px, trạng thái normal / hover / pressed / selected / disabled. Không được ra hình chữ nhật phẳng hay góc vuông răng cưa (lần trước người dùng chê "nút xấu quá" là vì thế).
- Màu theo tông RO Soft Fantasy (`docs/sprite-rules.md` điều 0) và lấy mẫu màu từ mockup (`docs/reference/ui-style-ro-soft-fantasy/`).

## Sổ lỗi

- 2026-11-19 túi đồ RO Soft: tab All/Weapon chữ dính đáy, icon cách chữ xa; pill "Common" xấu. Nguyên nhân: đặt vị trí tay, không có padding/gap/căn giữa dọc. Sửa: bố cục theo mục 1.
