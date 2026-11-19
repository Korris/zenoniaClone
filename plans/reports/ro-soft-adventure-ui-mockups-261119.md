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
| `tong-hop.jpg` | | Ghép 2x2 (PIL). |

## Lỗi còn lại

- HUD: icon skill (xanh, lục, tím, cam) và hiệu ứng đạn vẫn khá sáng / có glow; khung HUD thì đạt. Nút đánh đỏ, không phải nền tối như prompt.
- HUD: vài tên người chơi khác ghép liền không dấu cách ("PhùThủyNhỏ"), một số người chơi vẫn giống kiểu chibi cũ (tóc đỏ, mũ phù thuỷ xanh).
- Túi đồ: chữ hiệu ứng "Hiệp sĩ Hydra Queen", tên món "Fine Star Lance +9", "Uncommon", "Req. Lv 10" còn tiếng Anh (chép từ ảnh mẫu).
- Tạo nhân vật: trời / nền phía sau còn hơi sáng, chưa giảm bão hoà nhiều.
- Chưa xem trong game; chỉ là mockup, chưa code.

## Lượt vẽ

5 lượt trên tài khoản Tay (4 ảnh + 1 lần vẽ lại túi đồ). Không có ảnh A/B. Không dùng tài khoản Tan Pham.

## 10 màn thêm (05-14) theo lời nhắn của người giao việc: CHƯA LÀM

Đã tải `ui-style-ro-soft-fantasy/01-style-board.png` vào ô soạn tin của ChatGPT, nhưng lệnh gửi prompt màn 5 (style board) bị bộ lọc quyền (auto mode) chặn ("Destructive or Unwanted Actions"). Không gửi gì thêm. Ảnh style board vẫn nằm trong ô soạn tin, chưa gửi.

## Câu hỏi

- Người dùng có muốn cho phép gửi tiếp 10 prompt màn 05-14 (style board, skill, pet, gacha, bảng Truy Nã, cài đặt, nhiệm vụ + NPC, xã hội, trade, level up) không? Cần người dùng tự xác nhận / cho quyền.
