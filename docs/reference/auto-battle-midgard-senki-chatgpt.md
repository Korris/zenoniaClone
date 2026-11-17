# Auto-battle + mana: hỏi ChatGPT (17/11/2026)

Chat ChatGPT (tài khoản Tay): https://chatgpt.com/c/6afbff66-fe24-83ec-ad12-ef821100b6ac ("Đề xuất Auto Midgard Senki").
Người dùng hỏi: "game Ragnarok Midgard Senki hình như không xài mana, hỏi ChatGPT thử". Đây mới là đề xuất, chưa viết code.

## Midgard Senki có dùng mana không
- ChatGPT **không xác minh được**: không tìm thấy nguồn nói có hay không có SP, skill tốn gì hay có bình SP không. Nó chỉ chắc skill đi theo vũ khí đang cầm (nguồn 4Gamers).
- Đã xác minh được: Midgard Senki ra mắt Đài Loan / HK / Macau ngày 23/10/2026; có nút Auto-battle tự đánh và tự dùng skill; chỉnh được phạm vi tuần tra; có phần thưởng AFK khi offline (tối đa 12 giờ); có MVP ngoài map và boss trong phụ bản. Hướng dẫn 4Gamers khuyên đánh boss phụ bản bằng tay.
- Chưa xác minh: thứ tự skill khi Auto, tự uống bình, tự nhặt đồ, Auto có đánh MVP không.
- Muốn biết chắc có mana không thì phải xem gameplay. Video người dùng đang mở: https://www.youtube.com/watch?v=tZvgARUCKm4

## Đề xuất về MP
| Cách | Điểm | Ghi chú |
|---|---|---|
| A. Giữ MP + hồi chiêu như hiện tại | 7/10 | Bật Auto dễ cạn MP rồi chỉ đánh thường |
| B. Bỏ MP, chỉ còn hồi chiêu | 8/10 | Auto mượt nhưng mất phần quản lý tài nguyên |
| **C. Giữ MP + hồi chiêu, thêm hồi MP tự nhiên** (ChatGPT chọn) | 9/10 | Đang đánh hồi 0,8 % MaxMP/s; ngoài chiến đấu (5 s không đánh / không bị đánh) hồi 2 % MaxMP/s. Thiếu MP thì bỏ qua skill, đánh thường |

ChatGPT chỉ khuyên bỏ MP nếu game chuyển hẳn sang kiểu idle RPG.

## Đề xuất Auto V1
- **Phạm vi:** điểm gốc = chỗ bấm Auto. Mặc định chế độ Tuần tra, bán kính 7 ô (chọn 5 / 7 / 10). Chế độ "Đứng gần" chỉ đánh quái trong 3 ô. Đuổi mục tiêu tối đa ra ngoài bán kính 2 ô. Tìm mục tiêu mới mỗi 0,4 s. Hết quái thì về điểm gốc chờ quái hồi sinh. Không qua cổng map. Ưu tiên quái đang đánh mình.
- **Skill:** ưu tiên theo điều kiện, không xoay vòng cố định: buff (khi sắp hết, dưới 3 s) → phòng thủ (HP < 50 %) → AoE (có từ 3 quái) → skill đơn mục tiêu mạnh (quái còn trên 40 % HP) → skill hồi chiêu ngắn → khống chế → ultimate → đánh thường. Đang hồi chiêu hoặc thiếu MP thì bỏ qua. Skill hồi chiêu 30-50 s không dùng vào quái sắp chết. Auto đọc thẳng danh sách skill, không lật trang băng skill; mặc định lấy skill đang gán, bật / tắt được từng skill.
- **Bình:** uống HP dưới 55 % (chỉnh 20-90 %), uống MP dưới 35 % (chỉnh 10-80 %). Không uống khi bình sẽ phí quá nhiều. Hết bình HP thì ngừng tìm quái khi HP dưới 30 %.
- **Nhặt đồ:** bật sẵn, bán kính 3 ô, chờ 0,3 s sau khi quái chết. Ưu tiên đồ hiếm → trang bị → đá → thuốc → vàng. Không rời vùng Auto để nhặt. Quyền nhặt do server quyết.
- **Boss / MVP:** không tự săn. Boss đánh mình thì rút lui. Người chơi tự đánh boss rồi bật Auto thì Auto vẫn hỗ trợ. Không đánh người chơi khác.
- **Nút:** AUTO gần cụm skill, cỡ khoảng 58 px ở 1280x720. Bấm để bật / tắt, giữ 0,5 s để mở cài đặt. Cài đặt gồm 3 nhóm: Chiến đấu, Tự hồi phục, Nhặt đồ.
- **Tự tắt Auto khi:** người chơi di chuyển bằng joystick / WASD, mở NPC / cửa hàng, chết, chuyển map, mất kết nối. Bấm skill bằng tay thì Auto chỉ tạm dừng 1 s.
- **Chưa làm ở V1:** Auto qua map, tự mua bình, tự hồi sinh, tự làm quest, farm offline.
- **Thứ tự làm:** đánh thường + tìm quái → skill → bình → nhặt đồ + lọc mục tiêu. Sau đó chơi thử Warrior Lv10 / 30 / 60, mỗi lần 10 phút.

## Điểm còn ngờ (Claude)
- ChatGPT tự đoán map "36 x 24 ô"; map của game lớn tới 72 x 48. Bán kính 7 ô cần xem lại trên map thật.
- Code đã có cơ chế cho bot bấm phím thay người chơi (`InputAdapter.BotHeld` / `BotPressed`, đang dùng trong `DebugCapture.EnableFightBot`). Auto có thể dùng lại cơ chế này, server vẫn tự tính dam nên chạy được cả online. Tìm đường hiện chỉ kiểm tra tầm nhìn (`HasLineOfSight`), chưa có pathfinding.
- Hình nút AUTO: theo quy định của project, mọi thiết kế giao diện phải xin ChatGPT vẽ.
