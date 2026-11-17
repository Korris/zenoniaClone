# Danh sách party trên HUD gọn: ChatGPT (17/11/2026)

Người dùng: "kêu chatgpt design cái danh sách party nằm đâu cho cả 2 bản mobile và pc". Đã hỏi trong chat thiết kế HUD gọn (tài khoản Tay): https://chatgpt.com/c/6afa7ea3-b060-83ec-86af-1fa45497cea5. Gửi kèm ảnh HUD trong game: `../../../monster-foot-labels/boss-top-bar-only.png` (mobile) và `hud-pc-current.png` (PC).
ChatGPT trả lời bằng 3 ảnh, không có chữ. Chưa viết code.

- `party-hud-chatgpt-1.png`: mockup MOBILE.
- `party-hud-chatgpt-2.png`: mockup PC.
- `party-hud-chatgpt-3.png`: bảng thông số.

## Thông số (đọc từ bảng 3)
| | Mobile | PC |
|---|---|---|
| Vị trí | Dưới cụm trạng thái, góc trái trên | Dưới cụm trạng thái, góc trái trên |
| Khung | rộng 18-20 % W (345-385 px ở 1080p) x cao 20-24 % H (216-259 px) | rộng 15-17 % W (288-326 px) x cao 18-22 % H (194-238 px) |
| 1 dòng | cao 4,5-5 % H (48-54 px) | cao 4-4,5 % H (43-49 px) |
| Khoảng cách | cách cụm trạng thái 10-14 px, cách thanh boss khoảng 24 px, cách joystick hơn 120 px | cách cụm trạng thái 12-16 px, cách chat hơn 24 px |

- **1 dòng gồm:** chân dung tròn viền theo màu class; vương miện cho trưởng nhóm; "Lv.N" và tên; thanh HP đỏ, dưới là MP xanh; icon buff bên phải (tối đa 3-4, nhiều hơn thì thu gọn).
- **Trạng thái:**
  - đang chọn: viền vàng sáng;
  - đã chết: ảnh xám, dấu X, thanh trống;
  - ngoài phạm vi: ảnh và thanh mờ khoảng 60 %, không hiện buff, có icon ghim vị trí.
- **Thu gọn / mở rộng:** nút chevron ở mép trái hoặc ở tiêu đề "Party 4/4". Thu gọn thì chỉ còn nút nhỏ và số thành viên.
- **Không có party:** nút tròn "Party" (icon nhóm người) cạnh cụm nút chức năng góc phải để tạo / tìm party, hoặc ẩn danh sách cho tới khi có lời mời.
- **Mockup có thêm:**
  - dải buff giữa đáy (ví dụ "ATK +20% / DEF +20% (30s)");
  - kênh chat [Party];
  - bản PC: thêm icon Party vào hàng icon góc phải, viên tên map "Frost Hollow" cạnh minimap, hàng buff của mình ngay dưới cụm trạng thái, dời chat sang góc trái dưới.

## Người dùng đã chốt
- 17/11 15:45: KHÔNG thêm viên tên map cạnh minimap ("ko cần thêm tên map cạnh minimap").
- 17/11 15:48 ("ok" theo đề xuất của Claude): danh sách party trên HUD CHỈ hiện đồng đội (tối đa 3 dòng), không có dòng của chính mình, vì máu / mana của mình đã ở cụm trạng thái. Bảng Party mở bằng nút Party (để mời, đuổi, đổi trưởng nhóm) thì hiện đủ mọi người, kể cả mình.

## Điểm còn ngờ (Claude)
- Mockup 1, 2 có cả dòng của chính mình (4 dòng): ĐÃ CHỐT bỏ dòng đó (xem trên). Tiêu đề vẫn ghi số người cả party, ví dụ "Party 4/4".
- Số trong dải buff mockup (+20 %, 30 s) chỉ để minh hoạ. Cleric thật cho +15 % ATK / +15 % DEF trong 12 s, bán kính 4 ô.
- Game chưa có kênh chat Party chạy qua mạng (chat hiện chỉ có `LocalChatTransport`).
