# Style guide V1.0 "RO Soft Adventure (hướng RO3)" (ChatGPT, 22/11/2026)

Chat (Tay, "Tân Korris"): https://chatgpt.com/c/6b0243dc-91b8-83ec-afe6-bec308abaf1d
Ảnh gửi: Warrior idle xuống hiện tại, túi đồ RO Soft Fantasy trong game, `ui-style-ro-soft-adventure/04-character-create.png`, `02-bag.png`.
Lý do đổi (người dùng 22/11): "tui thấy Adventure này có vẻ giống style Ragnarok 3 hơn kiểu Fantasy". Đích: cảm giác RO3.

Phán quyết ChatGPT: chuyển sang Adventure, lấy `02-bag.png` làm master UI (9/10), `04-character-create.png` làm master concept nhân vật (8,5/10), nhưng sprite KHÔNG bê nguyên key art: **70 % Adventure + 20 % vật liệu / ánh sáng RO3 + 10 % dễ thương RO**. Giữ độ rõ, độ dễ thương của sprite hiện tại; đổi màu, chất liệu, ánh sáng, trọng lượng.

## A. Fantasy hiện tại vs Adventure

| Phần | Fantasy (đang có) | Adventure | Chốt |
|---|---|---|---|
| Tổng thể | Pastel, sáng, dễ thương | Kem ấm, phiêu lưu, trưởng thành hơn | Adventure |
| Khung UI | Trắng xanh, viền xanh phát sáng | Giấy ngà, viền nâu, chi tiết đồng cổ | Adventure |
| Ánh sáng | Trắng lạnh, phẳng | Nắng vàng ấm, bóng có chiều sâu | Adventure |
| Màu | Cyan, tím pastel nhiều | Nâu đất, đồng, kem, đỏ trầm | Adventure |
| Nhân vật | Mặt tròn, mắt to, cười | Mắt sắc hơn, tư thế mạnh | Kết hợp |
| Nét | Viền rõ, cel | Painterly, viền mềm, nhiều sắc độ | Painterly có kiểm soát |
| Trang bị | Vàng sáng, đỏ tươi, kim loại bóng | Đồng cũ, thép, da thuộc, vải dày | Adventure |
| Icon | Nhiều màu, viền tím sáng | Có khối, viền trung tính | Adventure |
| Chữ | Sans tròn hiện đại | Chữ có chân, cảm giác sách phiêu lưu | Adventure |

Adventure còn thiếu / cần tránh: key art `04` quá chi tiết cho sprite 192 px; `02` trang trí hơi dày; mặt `04` hơi nghiêm (RO3 vẫn anime gần gũi); UI RO3 thật không nhất thiết là giấy kem: giấy / gỗ / chữ cổ là diễn giải riêng.

## B1. Palette

| Token | Dùng | Hex |
|---|---|---|
| Paper | nền panel, cửa sổ | #F4EBDD |
| PaperLight | ô thông tin, phần nổi, hộp thoại | #FFF8ED |
| PaperDark | ô phụ, vùng lõm | #DFD0BA |
| WoodLight | thanh trang trí | #B58A60 |
| WoodDark | khung gỗ, gờ, viền ngoài | #75523C |
| InkLine | nét chính | #514137 |
| Text | chữ chính | #342F2B |
| TextSoft | chữ phụ | #776C60 |
| Bronze | góc, đường nhấn | #B88A4B |
| WarmGlow | highlight ấm | #F2D39A |
| Teal / hover | nút chính | #328C81 / #45A79A |
| Gold / light | nâng cấp, phần thưởng | #C8923D / #EBC16C |
| Red | nguy hiểm | #B85D50 |
| Blue | điều hướng | #527E9C |
| Disabled | | #B8B0A5 |
| Selected | | #E8CA86 |
| Common / Uncommon / Rare | | #8D8980 / #648D56 / #4F83B4 |
| Epic(SR) / Legendary(SSR) / Mythic(UR) | | #8F65B0 / #CB9437 / #BF665B |
| HP / MP / EXP | thanh: viền nâu 1 px, rãnh tối, highlight mảnh trên, không neon | #C95C55 / #4B85B4 / #C8A04C |
| Tăng / giảm chỉ số | | #39824F / #B65349 |

Mỗi panel chỉ 1 nút chính (teal). UR không dùng cầu vồng.

## B2. Vật liệu khung (1280x720)

| Phần | Quy chuẩn |
|---|---|
| Khung lớn | Paper, viền nâu 2-3 px, bo 12-16 |
| Hộp thoại vừa | PaperLight, viền 2, bo 10-12 |
| Toast / tooltip | PaperLight, viền 1-2, bo 8 |
| Viền ngoài | WoodDark + viền sáng trong 1 px |
| Bóng khung | nâu đen 18-25 %, lệch Y 3-5, blur 8-12 |
| Hoa văn góc | Bronze, tối đa 4 góc, 12-24 px |
| Tiêu đề | dải giấy sáng + kẻ đồng mảnh |
| Ô item | PaperLight, viền xám nâu 1-2, bo 6-8; chọn = viền vàng 2-3, không glow lan |
| Divider | nâu nhạt 1 px, có thể thoi đồng giữa |

3 cấp trang trí: (1) màn lớn (túi, tạo nhân vật, skill, pet) được góc + tiêu đề cầu kỳ; (2) panel thường (quest, shop, trade, party, settings) 1 hoạ tiết nhỏ; (3) HUD chiến đấu gần như không hoa văn. "Sách phiêu lưu chế tác tinh tế, không phải bảng gỗ nặng, không phải kính pha lê."

## B3. Chữ

Tiêu đề: Noto Serif SemiBold / Bold. Thân, nhãn, số: Be Vietnam Pro (Regular / SemiBold / ExtraBold cho số dam). Cả hai free, đủ tiếng Việt. Tối đa 2 họ font.
Cỡ (1280x720): tiêu đề cửa sổ 28-32, nhóm 20-23, tên item 19-22, nội dung 16-18, tab / nút 16-18, chỉ số 17-20, phụ 14-16, dam 18-26. Chữ nâu đen (không đen tuyệt đối), tiêu đề không viền trắng / bóng lớn, chữ trên nút teal màu #FFF8ED + bóng nâu mảnh, viền chữ combat 1-2 px.

## B4. Icon

3/4 xoay 15-30°, đọc được ở 40-48 px, viền nâu #59463A 1-2 px (icon 64), 3 cấp sáng tối + highlight vật liệu, bão hoà giảm 10-20 % ở màu quá rực, nền ô giấy ngà (bỏ tím pastel), viền hiếm chỉ quanh ô / tên. Vũ khí / giáp: thép xám ấm, da nâu, đồng cổ, không phủ vàng bóng. Icon skill GIỮ màu hệ (băng xanh, lửa cam, độc lục, thánh vàng kem).

## B5. Nhân vật sprite 8 hướng

| Mục | Chuẩn |
|---|---|
| Tỷ lệ | 2,4-2,7 đầu; đầu 37-42 % chiều cao; vai Warrior 1,15-1,25 x bề ngang mặt; tay hơi to; giày có khối |
| Mắt | anime, có thể nhỏ hơn bản hiện tại 5-10 % |
| Viền | ngoài #49372E, trong #795849 / màu tối vật liệu, 2-3 px ở 192 px, chi tiết nhỏ 1 px hoặc bỏ |
| Shading | 3 cấp + 1 highlight (tóc / kim loại), sáng trên-trái màn hình (cố định, kiểm khi lật), bóng nghiêng nâu tím nhẹ; chuyển sắc mềm trong mảng lớn, cạnh silhouette rõ |
| Da | peach ấm, má hồng nhẹ, bóng nâu hồng |
| Tóc | mảng lớn 3-4 cấp; xám nguồn #484848 / #858585 / #B8B8B8 / #E4E4E4 + mask để nhuộm |
| Vải / da thuộc | nếp bản lớn; da nâu đỏ / hạt dẻ, cạnh mòn |
| Kim loại | thép xám xanh nhạt highlight ngắn (không chrome), đồng vàng nâu bóng vừa, vàng chỉ trang trí / đồ quý |
| Áo choàng | đỏ gạch / đỏ rượu, phần khuất tối rõ |
| Biểu cảm | Warrior tự tin hơi nghiêm; Wizard điềm tĩnh; Mechanic tinh nghịch; Assassin sắc, cảnh giác; Cleric dịu dàng |

Một mảng giáp lớn dễ đọc hơn mười chi tiết khắc nhỏ. Không bắt buộc kéo dài nhân vật đã duyệt.

## B6. CẤM

Nền trắng xanh / cyan / tím pastel chiếm đa số UI; glow xanh / trắng quanh khung; viền vàng kim sáng mọi chỗ; kim loại vàng bóng như nhựa; tóc nhiều lọn nhỏ mỗi lọn 1 highlight; chi tiết painterly dưới 2 px; cả nền giấy ngả nâu đậm; bóng đen, tương phản gắt, xám bẩn (grimdark); mặt quá nghiêm, mắt quá nhỏ; mọi nút gradient mạnh + bóng dày; đổi màu VFX cho khớp giấy; mỗi class độ đậm viền khác nhau.

## B7. Checklist (đạt khi ≥ 9/10 VÀ bắt buộc 4, 5, 7, 10)

1. ≥ 70 % diện tích UI trung tính giấy kem / nâu, không cyan / tím pastel thống trị.
2. Khung có chiều sâu vừa, không glow kính, không trang trí dày.
3. Tiêu đề / nội dung / nhãn / nút phân cấp rõ ở 1280x720.
4. Nhân vật chibi thống nhất, silhouette nhận ra class.
5. Viền nâu rõ, không quá đen, không quá mảnh khi thu nhỏ.
6. Sáng trên-trái, 3 cấp shading nhất quán.
7. Thép, da, vải, gỗ, đồng có đặc tính vật liệu riêng.
8. Icon đọc tốt ở 40-48 px, viền hiếm không lấn vật phẩm.
9. Ấm, có sức sống, không pastel lạnh, không grimdark.
10. Kiểm ở cỡ hiển thị thật: sprite, icon, chữ, UI vẫn rõ.

## Thứ tự ChatGPT đề xuất

P0 túi đồ + Warrior; P1 tạo nhân vật, Stats / Skill / Pet / Gacha / Shop dùng chung panel + token nút; P2 HUD chiến đấu (gọn), icon item / skill. Warrior: giữ tóc, mặt, khăn đỏ, kiếm, khiên; giảm bóng giáp, thêm vải / da, bớt vàng, chỉnh biểu cảm, nét, ánh sáng.

## Ảnh test (22/11) + ChatGPT chấm

| Ảnh | File | Điểm | Ghi chú |
|---|---|---|---|
| Warrior idle xuống Adventure | `adv-warrior-idle-down-chatgpt-1.png`; so `warrior-idle-fantasy-vs-adventure.png` (+ `-game-size.png` ở 200 px) | 88,6 / 100, đạt hướng nghệ thuật | mục 7 sát ngưỡng, mục 10 chờ thử trong game |
| Túi đồ Adventure | `bag-adventure-chatgpt-1.png`; so `bag-fantasy-vs-adventure.jpg` | 86 / 100, đạt, cần polish | giữ đúng bố cục 3 cột / 6 tab / 8 cột |
| Đổi tông bằng tool (không ChatGPT) | `warrior-idle-regrade-tool-test.png` | (Claude) ấm hơn nhưng thiết kế vẫn Fantasy | chỉ dùng tạm cho walk / attack / vũ khí |

Sửa khi làm thật: Warrior giảm bóng vàng / đồng 20-30 % (viền khiên, chuôi kiếm, khoá, vai), bớt 15-20 % chi tiết tóc / giáp nhỏ, góc nhìn 3/4 từ trên rõ hơn, kiểm ở 192 px. Túi đồ giảm vàng ~25 % (viền panel, nút Enhance, ô chọn) về đồng cổ #B88A4B, bớt 30-40 % hoa văn góc ở panel con, độ hiếm bằng VIỀN ô (Legendary viền đồng 2-3 px, nền giấy ngà, không nền vàng phát sáng), icon phân cấp rõ (thường thép mờ / Rare 1 điểm màu / Legendary hoa văn kim loại riêng).

Master gửi kèm MỌI prompt ("RO Soft Adventure V1.0 Master Reference Pack"): Warrior test 1 (đã polish) + túi đồ test 2 (đã polish) + palette / font / vật liệu / viền / ánh sáng của guide này; không gửi lẻ 1 ảnh.
