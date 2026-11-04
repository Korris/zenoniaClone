# Hỏi ChatGPT: điều khiển di chuyển bản web

- Tài khoản đã kiểm tra: "Tân Korris" (tài khoản Tay, Plus); ChatGPT chào "Hey, Tay".
- Chat: https://chatgpt.com/c/6aeafcd2-560c-83ec-b5b0-ccc95d5b3c25 (tiêu đề "Recommend PC Controls"), có đính kèm ảnh `ingame-web-hud-1080p.png`, độ suy nghĩ Medium.
- Không đổi code.

## Khuyến nghị
Hỗ trợ CẢ HAI, mặc định WASD:
- Chính: WASD/mũi tên để đi, chuột để ngắm (kiểu action RPG, như Hades).
- Phụ: chuột phải để click-to-move, giữ chuột phải để đi theo con trỏ (kiểu MU/Ragnarok). Có tuỳ chọn tắt trong Settings.
- Mobile giữ D-pad ảo.
- Lý do: WASD cho phép vừa chạy hướng này vừa đánh hướng khác, né chính xác, hợp Mechanic/Wizard/Assassin. Click-to-move tiện khi đi xa giữa các làng.

## Sơ đồ phím đề xuất
| Phím / chuột | Việc |
|---|---|
| WASD / mũi tên | Đi 8 hướng |
| Chuột trái (LMB) | Đánh thường về phía con trỏ, giữ để đánh liên tục |
| Chuột phải (RMB) | Click-to-move, giữ để đi theo con trỏ |
| 1 / 2 / 3 | Skill về phía con trỏ |
| Q / E | Bình HP / MP |
| F | Tương tác NPC, loot, vật gần nhất |
| LMB vào NPC / loot | Chọn, tự đi tới nếu xa |
| Tab | Đổi mục tiêu quái gần |
| Shift (giữ) | Đứng yên khi đánh |
| Enter | Mở/gửi chat; Enter hoặc Esc thoát chat |
| B / K | Túi đồ / bảng skill |
| Esc | Đóng cửa sổ, huỷ hành động |
| Space (nếu sau này có dash) | Lướt theo hướng WASD hoặc hướng đi cuối |

Hướng đánh: mặc định theo con trỏ chuột. Warrior xoay theo con trỏ khi chém; Assassin lao về con trỏ; Mechanic bắn về con trỏ khi đang chạy ngang/lùi; Wizard nhắm vị trí con trỏ; Cleric: skill tấn công nhắm con trỏ, hồi máu chọn đồng đội hoặc tự buff. Melee có chút hỗ trợ ngắm vào quái gần. Khi chỉ đi bộ thì hero không xoay theo con trỏ; chỉ đánh/skill mới đổi hướng chiến đấu.

Click-to-move: WASD huỷ ngay đường đi chuột; chuột phải vào quái thì đi xuyên qua (không đánh nhầm); cần tìm đường vòng qua rào, nhà, cây, đá; đánh chỉ huỷ đường đi khi đòn đánh cần đứng lại.

## Lỗi cần tránh
- Đừng gộp đi và đánh vào chuột trái theo mặc định (đánh hụt thành chạy vào quái).
- WASD phải ghi đè click-to-move ngay, không để hai input tranh nhau.
- Tắt menu chuột phải của trình duyệt trên canvas game.
- Khi đang chat, gõ W/A/S/D/Q/E không được làm hero đi hay uống bình.
- Không auto-target quá mạnh: melee dùng nón nhắm nhỏ, ranged để người chơi tự ngắm.
- Nhặt loot đừng mệt: F nhặt gần, hoặc tuỳ chọn tự nhặt.
- Đừng bắt buộc dùng chuột: có Tab chọn mục tiêu và F tương tác cho người dùng touchpad laptop.

## Ghi chú
- ChatGPT dẫn Ragnarok (đi bằng chuột), Diablo IV (click, có thể tách đi/đánh), Hades (đi trực tiếp + ngắm chuột). Chưa kiểm chứng các nguồn đó.
- Bảng so sánh của ChatGPT: WASD hơn ở chiến đấu, né, đánh khi chạy, ngắm, touchpad; click-to-move hơn ở đi xa và quen thuộc với dân MMORPG.

## Câu hỏi còn mở
- Có làm dash (Space) không; B/K và Tab/F có xung đột phím hiện có của game không (chưa kiểm tra code).
