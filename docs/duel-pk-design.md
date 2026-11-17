# Duel / PK: thiết kế của ChatGPT

Ngày: 17/11/2026. Người dùng muốn có Duel / PK, đánh ở khu vực riêng, không đánh trên map đang có. Nguồn: ChatGPT (tài khoản Tay), chat https://chatgpt.com/c/6af57fff-c4f8-83ec-8f05-0c4eb76c4cde. Ảnh mockup (chụp từ chat, chỉ là bố cục): `docs/reference/duel-pk/`.

Trạng thái: thiết kế đề xuất, CHƯA code, chờ người dùng duyệt.

## Chốt của ChatGPT

Duel 1v1 hai bên đồng ý, trong instance đấu trường riêng (giống instance Boss Truy Nã), chuẩn hoá sức mạnh, không thưởng, tắt Auto, cho pet. KHÔNG làm PK tự do trên map farm. Muốn có chữ "PK" thì đặt tên "PK Arena" nhưng vẫn là đấu đồng ý.

| Chế độ | Bản chất | Làm khi nào |
|---|---|---|
| Duel 1v1 | 2 người đồng ý, sang đấu trường riêng, không mất đồ / EXP | Bản 1 |
| PK tự do | Đánh người khác không cần đồng ý | Không làm trên map farm |
| Arena 1v1 | Hàng chờ ghép trận, xếp hạng, thưởng mùa | Bản 3 |
| Arena 2v2 / 3v3 | Đấu đội | Sau bản 3 |

## Mời và nhận

- Bấm vào nhân vật khác → "Thách đấu" (PC chuột phải / menu, mobile chạm rồi chọn). Mời từ danh sách party: bản 2. NPC đấu trường chỉ để xem luật, không bắt buộc.
- Điều kiện: cả hai Lv ≥ 10, không giới hạn chênh cấp; cùng map; không ở trong instance boss hay trận khác; không đang đánh, chết, chuyển map, giao dịch.
- Chờ nhận 15 s, hết giờ tự huỷ; từ chối không phạt. Chống spam: 1 lời mời chờ / người, 30 s giữa 2 lần mời cùng người, tuỳ chọn chặn lời mời Duel.
- Đấu với người cùng party được, không giải tán party.
- Bấm đồng ý thì server kiểm tra lại toàn bộ điều kiện (trong 15 s chờ trạng thái có thể đã đổi).

## Khu vực riêng: Duel Instance

- Mỗi trận 1 instance 2 người (không làm một map chung nhiều sàn), tự huỷ khi cả hai đã rời.
- Sàn 20×14 ô, bản 1 không vật cản, 2 điểm xuất phát đối xứng cách 10-12 ô, chưa có khán giả, camera như cũ nhưng giới hạn trong sàn.
- Vào: server lưu map, zone, toạ độ, hướng của từng người → tắt Auto, huỷ mục tiêu, đạn / skill đang bay, hiệu ứng → MapTransfer đưa cả hai vào cùng DuelInstanceId → chờ cả hai client tải xong (quá 15 s thì huỷ, trả người đã vào về) → khoá điều khiển, đếm 3-2-1-FIGHT.
- Ra: hiện kết quả 5 s, về đúng map + toạ độ cũ (không hợp lệ thì ô an toàn gần nhất). Auto KHÔNG tự bật lại.
- Rớt mạng: trước khi bắt đầu thì huỷ trận; đang đấu thì chờ reconnect 10 s (đứng yên, không nhận sát thương), quá hạn xử thua; cả hai rớt hoặc server lỗi thì trận vô hiệu. Đăng nhập lại mà trận đã xong thì đưa về điểm đã lưu, không để kẹt trong instance đã huỷ.

## Luật trận (bản 1)

| Luật | Chốt |
|---|---|
| Thời gian | 120 s, hết giờ chưa ai gục là hoà |
| HP / MP | Đầy khi bắt đầu; ra trận trả lại đúng giá trị trước trận |
| Combo | Giữ nguyên, nhát 4 đẩy lùi theo thông số PvP riêng |
| Skill / ultimate | Cho dùng; ultimate hồi chiêu 15 s đầu trận, skill thường sẵn sàng |
| Thuốc | Cấm |
| Pet | 1 pet đang mang, chỉ số chuẩn hoá, chỉ nhắm đối thủ trong cùng instance |
| Auto | Tắt hẳn (cả auto skill, auto thuốc) |
| Buff | Chỉ buff class + pet trong trận, xoá buff PvE mang vào |
| Thua | HP về 0 = KO, không chết thật, không mất EXP / vàng / đồ, không rơi đồ |
| Đầu hàng | Có nút; mobile đặt trong menu tạm dừng để khỏi bấm nhầm |
| Khác | Không giao dịch, đổi đồ, đổi pet, đổi skill trong trận |

Lưu trạng thái PvE riêng, tạo trạng thái PvP tạm thời; không ghi đè HP / MP / hồi chiêu / buff PvE (dễ lỗi khi rớt mạng).

## Cân bằng (số khởi điểm để test)

| Phần | Đề xuất |
|---|---|
| Cấp | Chuẩn hoá về Lv 90 |
| Trang bị | Chỉ số PvP chung theo class, giữ ngoại hình T0-T9 |
| Cường hoá +15 | Không tính trong Duel bản 1 |
| Pet | Giữ loài, kiểu đánh; chỉ số chuẩn hoá |
| Sát thương PvP | ×0,35 (test) |
| Hồi máu skill | ×0,5 |
| Đẩy lùi | ×0,5, không đẩy ra ngoài sân, không cộng dồn khi đang bị đẩy |
| Khống chế | Tối đa 0,6 s mỗi lần, sau đó miễn khống chế cùng loại 2 s |
| Thời gian hạ đối thủ | Mục tiêu 20-45 s khi đánh liên tục |

Lưu ý: nếu công thức đã trừ giáp thì đừng vừa ×0,35 vừa giữ giáp PvE (nhiều đòn chỉ còn 1 sát thương). Sau này có thể thêm "Duel giữ đồ thật" song song với bản chuẩn hoá.

## Phần thưởng

Bản 1 không thưởng gì (chỉ màn thắng / thua / hoà, có thể báo trong chat). Bản 2: thống kê thắng thua, danh hiệu không chỉ số. Bản 3 (Arena): MMR ẩn để ghép trận, điểm mùa, ngoại trang; giới hạn thưởng tuần, kiểm tra đấu lặp lại một đối thủ, không thưởng đồ giao dịch được.

## Giao diện (kiểu Adventure Paper)

- Bảng mời: "DUEL CHALLENGE", "<Class> Lv90 muốn thách đấu với bạn!", dòng "Đấu 1v1 • Chuẩn hoá chỉ số • Không mất vật phẩm", Chấp nhận / Từ chối, đếm 15 s.
- HUD trận: thanh HP / MP mình bên trái, đối thủ bên phải, đồng hồ giữa trên, nút Đầu hàng.
- Màn kết quả: VICTORY / DEFEAT / DRAW, "<đối thủ> bị hạ sau N giây", "Không mất EXP hoặc vật phẩm", nút Trở về bản đồ.

## Chia bản

| Bản | Phạm vi | Chưa làm |
|---|---|---|
| V1 Friendly Duel | Mời / nhận 1v1, instance riêng, luật 120 s, chuẩn hoá, HUD, thắng / thua / hoà, về map cũ, xử lý rớt mạng | Xếp hạng, thưởng, khán giả, NPC, ghép trận |
| V2 Duel mở rộng | Mời từ party, chọn bộ luật, thống kê, đấu lại, NPC đấu trường, chế độ giữ đồ thật | Elo, Arena mùa |
| V3 Ranked Arena | Ghép trận 1v1, MMR, mùa, chống farm, thưởng ngoại trang, lịch sử trận | Guild war, PvP mở trên map farm |

Phía server (tên gợi ý): DuelInviteManager (mời, hết hạn, spam, điều kiện), DuelInstanceManager (tạo / huỷ instance, lưu điểm về), DuelMatchState (Pending → Loading → Countdown → Fighting → Finished → Returning), PvPRuleset (chuẩn hoá, hệ số, khống chế, thuốc, Auto, pet), DuelResultHandler (KO, hết giờ, đầu hàng, rớt mạng). Client chỉ gửi yêu cầu và hiển thị; InviteAccept, TransferReady, MatchStart, MatchEnd đều do server xác thực.

## Rủi ro chính

- Kẹt trong instance: lưu điểm về trước khi chuyển, khôi phục khi đăng nhập.
- Sai HP / đồ sau trận: tách trạng thái PvP.
- Spam lời mời.
- Đạn / skill / pet lọt giữa các zone: lọc theo instance ID.
- Cleric hồi máu kéo dài trận: giảm hồi máu, giới hạn giờ.
- Thoát game để né thua.
- Lag: server quyết định trúng đòn, sát thương, đẩy lùi, kết quả.

ChatGPT nhấn mạnh: phần cần test kỹ nhất là chuyển hai người vào / ra an toàn, cô lập trạng thái PvP và trả đúng trạng thái sau trận, không phải vẽ đấu trường.
