# Mockup UI RO Soft Adventure (19/11/2026)

- Chat (Tay, đã kiểm tra `/api/auth/session` = "Tân Korris"): https://chatgpt.com/c/6afe1ebe-b668-83ec-8233-c010a9354ef0
- Ảnh: `docs/reference/ui-style-ro-soft-adventure/` (1672x941)
- Bảng màu: khung #F4F0E7, ô phụ #E4E7E2, viền #A5ACA9, chữ #35434B, xác nhận #368F87, chức năng #5685AE, huỷ #B85E59, thưởng #D6A54D, trang trí #B99A65, HUD #263B43, HP #C94F4F, MP #498EC4.

## Ảnh

| File | Ảnh gửi kèm | Nhận xét |
|---|---|---|
| `01-hud-boss-fight.png` | `crowd-vs-mire-hydra-queen-muted-chatgpt-1.png` | HUD nền than xanh tối, thanh boss đỏ, tên boss kem "Nữ Hoàng Hydra Đầm Lầy", HP/MP góc trái, minimap, joystick mờ, nút đánh đỏ đất lớn. Nhân vật mình "LữMinh" tên xanh lá + vòng bạc hà mảnh. Khoảng 25 người tông trầm. |
| `02-bag.png` | `tui-do-art.jpg` (lần 1); lần 2 kèm ảnh lần 1 + `5-class-muted-lineup-concept.png` | Giữ bố cục trái / giữa / phải, có ô Mũ, tab "Tất cả" teal trầm, ô xám bạc, nút Mặc teal / Cường hoá vàng đồng / Vứt đỏ đất. Lần 2 thay Warrior tóc đỏ kiểu cũ bằng Warrior tóc nâu đúng concept; phần UI giữ nguyên. |
| `03-shop.png` | `ui-style-ro-soft-fantasy/08-shop.png` (chỉ lấy bố cục) | Tiêu đề "Thợ Rèn" có hoạ tiết đồng, NPC râu nâu, tab Vũ khí / Giáp / Mũ, 5 món + giá vàng, so sánh Đang mặc >> Sau khi mua, nút Mua teal / Huỷ đỏ đất. Đẹp nhất trong 4 ảnh. |
| `04-character-create.png` | `5-class-muted-lineup-concept.png` | Warrior lớn trên bệ, mũi tên xoay, IDLE / ATTACK, 5 class có chân dung đúng concept, ô thông tin nghề + 3 thanh chỉ số, ô tên, Kiểu 1/6, 8 màu tóc, Quay lại xanh thép, Bắt đầu teal. Không có chọn mũ. |
| `tong-hop.jpg` | | Lưới 4 cột gồm cả 14 ảnh (PIL), làm lại sau đợt 05-14. |

## Lỗi còn lại

- HUD: icon skill (xanh, lục, tím, cam) và hiệu ứng đạn vẫn khá sáng / có glow; khung HUD thì đạt. Nút đánh đỏ, không phải nền tối như prompt.
- HUD: vài tên người chơi khác ghép liền không dấu cách ("PhùThủyNhỏ"), một số người chơi vẫn giống kiểu chibi cũ (tóc đỏ, mũ phù thuỷ xanh).
- Túi đồ: chữ hiệu ứng "Hiệp sĩ Hydra Queen", tên món "Fine Star Lance +9", "Uncommon", "Req. Lv 10" còn tiếng Anh (chép từ ảnh mẫu).
- Tạo nhân vật: trời / nền phía sau còn hơi sáng, chưa giảm bão hoà nhiều.
- Chưa xem trong game; chỉ là mockup, chưa code.

## Lượt vẽ

5 lượt trên tài khoản Tay (4 ảnh + 1 lần vẽ lại túi đồ). Không có ảnh A/B. Không dùng tài khoản Tan Pham.

## 10 màn thêm (05-14), làm ngày 19/11 lúc 09:05-09:50

Người dùng cho phép trong chat chính ("keu nó làm mấy menu kia luôn đi", "làm tiếp luôn di").

- 05-06 vẽ bằng Tay (chat trên). Lượt 07 thì Tay báo hết lượt vẽ (có lại lúc 15:58).
- 07-14 vẽ bằng **Tan Pham** (Plus, đã kiểm tra `/api/auth/session` = "Tan Pham"), chat mới: https://chatgpt.com/c/6afe5bbc-57d8-83ec-a0cf-eafd358c644d. Ảnh đính kèm lượt đầu: `05-style-board.png` (chuẩn màu) + `hero-layered-pilot-combat/5-class-muted-lineup-concept.png` (nhân vật) + ảnh mẫu cũ. Prompt ghi lại đủ 12 mã hex vì chat mới không có ngữ cảnh.
- Mỗi màn gửi kèm ảnh mẫu cũ tương ứng trong `ui-style-ro-soft-fantasy/`, chỉ lấy bố cục.

| File | Ảnh mẫu | Tài khoản | Nhận xét |
|---|---|---|---|
| `05-style-board.png` | `01-style-board.png` | Tay | 12 ô màu có mã hex, nút 4 loại thường / nhấn, tab, ô đồ theo độ hiếm, cửa sổ, tooltip, tấm HUD tối, thanh HP/MP/EXP, font. Không mascot. Warrior tông trầm. |
| `06-skill.png` | `04-skill.png` | Tay | Cây skill 3 tầng, ô khoá, chi tiết skill, Nâng cấp vàng đồng / Gán vào ô xanh thép, thanh skill nhanh 6 ô + trang. |
| `07-pet.png` | `05-pet.png` | Tan Pham | Danh sách pet, sói băng UR ở giữa, thân thiết, chỉ số, 4 skill pet, Cho ăn / Xuất chiến / Thả đi đúng 3 màu. |
| `08-gacha.png` | `06-gacha.png` | Tan Pham | Banner 3 nhân vật tông trầm, tỉ lệ rơi, thưởng tích luỹ, x1 / x10 vàng đồng. |
| `09-wanted-board.png` | `07-wanted.png` | Tan Pham | Bảng gỗ 6 tờ truy nã, tem "ĐÃ HẠ GỤC", chi tiết boss, Nhận nhiệm vụ / Dịch chuyển. |
| `10-settings.png` | `09-settings.png` | Tan Pham | 5 tab, âm lượng, công tắc teal, chất lượng đồ hoạ, ngôn ngữ, Lưu / Mặc định / Đăng xuất. |
| `11-quest-npc.png` | `10-quest-npc.png` | Tan Pham | Sổ nhiệm vụ 3 tab, tiến độ 3/10, hội thoại "Trưởng Làng Thomas", 2 lựa chọn, Nhận / Từ chối. |
| `12-social.png` | `11-social.png` | Tan Pham | Thẻ người chơi, lời mời đấu đếm giờ, 5 người chơi gần đây tên có dấu cách đúng. |
| `13-trade.png` | `12-trade.png` | Tan Pham | Hai cột Bạn / Đối phương, Đã xác nhận / Đã khoá, lưu ý giao dịch, túi đồ dưới, Khoá / Giao dịch / Huỷ. |
| `14-level-up.png` | `13-class-select-level-up.png` | Tan Pham | Banner "LÊN CẤP!", 59 → 60, 4 chỉ số tăng, điểm kỹ năng +1, skill mới, 4 phần thưởng, Nhận thưởng / Đóng. |

Cả 10 ảnh đều nhận ngay lần đầu, không vẽ lại, không có A/B.

### Lỗi còn lại (05-14)

- Style board: tooltip còn chữ tiếng Anh "Fine Star Lance +9", "Uncommon", "Req. Lv 10".
- Icon skill, pet băng, đá quý tím vẫn khá tươi và có chút glow (UI khung thì đạt).
- Gacha còn một con poring hồng ở tab "Bạn đồng hành".
- Trade: nhãn "Số vàng đặt cược" sai nghĩa (nên là "Số vàng").
- Ảnh 07-14 (Tan Pham) và 05-06 (Tay) cùng bảng màu nhưng do 2 chat khác nhau: nút và khung khá đồng đều, chỉ khác nhẹ chi tiết thanh tiền tệ phía trên.
- Chỉ là mockup, chưa code, chưa xem trong game.

### Lượt vẽ (05-14)

- Tay: 2 ảnh + 1 lượt bị từ chối vì hết lượt.
- Tan Pham: 8 ảnh.
- Tổng cả việc: Tay 7 ảnh (5 đợt đầu + 2), Tan Pham 8 ảnh.

### Ghi chú kỹ thuật

- Trên Chrome của Tan Pham, sau lần tải ảnh đầu, các lần tải tự động sau bị chặn im lặng (cả khi tải lại trang). Cách vòng: mở tab mới vào cùng chat, tải ảnh mới nhất, rồi đóng tab.
- Giao diện ChatGPT của Tan Pham bằng tiếng Việt: nút gửi "Gửi", ảnh tạo có alt "Ảnh được tạo N"; `__send` / `__grab` trong brief phải sửa theo.
- `__ready` trong brief bắt nhầm "20-30%" trong prompt là đang vẽ: chỉ xét 160 ký tự cuối và `\d+%\n`.
