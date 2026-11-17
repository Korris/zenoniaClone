# Tương tác người chơi: Party / Duel / Trade (UX của ChatGPT)

Ngày: 17/11/2026. Người dùng: "chụp màn hình cái menu invite và trade như hiện tại và kêu nó đề xuất cách nào để tối ưu ux ui khi party, duel, trade". Chat ChatGPT (Tay): https://chatgpt.com/c/6af57fff-c4f8-83ec-8f05-0c4eb76c4cde. Đi cùng `docs/duel-pk-design.md`.

Trạng thái: đề xuất, CHƯA code.

## Ảnh

`docs/reference/party-duel-trade-ux/`:
- `current-*.png`: giao diện hiện tại, chụp bằng `Tools/net-party-test.ps1 -Ui` và `Tools/net-trade-test.ps1 -Ui` (bản build 17/11 `D:/temp/player-261117-menu`).
- `chatgpt-social-menu-and-invite-mockup.png`: mockup 4 ô của ChatGPT (Player Card, Người chơi gần đây, Lời mời thách đấu, Đang gửi + Cài đặt tương tác).

Hiện tại: chạm / click trái vào nhân vật khác hoặc tên trên đầu → hộp nhỏ trên đầu (tên + Invite + Trade), tự đóng sau 6 s (`PartyPlayerMenu.cs`). Lỗi thấy trên ảnh: tên hiện 2 lần, không có class / level / party, tên nhân vật đè lên nút Accept của bảng mời, khó chọn người khi đứng chồng.

## Chốt của ChatGPT

Thay hộp 2 nút trên đầu bằng **Player Card** chung cho 6 hành động; Party, Duel, Trade dùng **một mẫu bảng lời mời**.

### Mở menu

| Thao tác | PC | Mobile |
|---|---|---|
| Chọn người | Click trái vào nhân vật / tên | Chạm vào nhân vật / tên |
| Mở nhanh | Click phải | Chạm giữ ~0,4 s |
| Đứng chồng | Tab đổi mục tiêu, hoặc danh sách Nearby | Chạm vào cụm người → danh sách chọn |
| Đóng | Esc, click ra ngoài | Chạm ra ngoài, nút X |

- Card đặt cố định ở vùng trống trên giữa màn hình (không bám trên đầu), lớp UI cao hơn tên nhân vật.
- Nearby Players: tối đa 5-6 người gần nhất, mỗi dòng avatar + tên + Lv + class + party, có cuộn; tuỳ chọn "Chỉ hiện cùng map", "Ẩn người đã chặn". Không thêm nút thường trực lên HUD.

### Player Card (~280×230 ở 960×640)

Đầu card: avatar, tên (tối đa 2 dòng), class, Lv, trạng thái party, nút X. Nút theo thứ tự: **Party → Duel → Trade → Whisper (Nhắn riêng) → Inspect (Xem thông tin) → Block**. Không thêm Friend / Follow / Report / Guild lúc này (sau này cho vào menu phụ ···).

Nút không dùng được thì **làm mờ, giữ chỗ** (chỉ ẩn khi vô nghĩa):

| Tình trạng | Hiển thị |
|---|---|
| Đã cùng party | Party → "In Party" mờ |
| Mục tiêu đang trade / duel | Trade / Duel mờ |
| Khác map, ngoài tầm | Trade, Duel mờ; Whisper, Inspect vẫn dùng |
| Dưới Lv 10 | Duel mờ, ghi cần Lv 10 |
| Mục tiêu tắt nhận lời mời | Không gửi được, báo ngắn |
| Đã chặn | Unblock thay Block |
| Đang ở Wanted Boss | Duel, Trade mờ |
| Chọn chính mình | Không mở card |

### Bảng lời mời đến (chung một kiểu)

| Loại | Tiêu đề | Nội dung | Hạn |
|---|---|---|---|
| Party | Party Invitation | X invites you to join a party | 30 s |
| Duel | Duel Challenge | X challenges you to a 1v1 duel + "1v1 · Equalized Stats · No Loss" | 15 s |
| Trade | Trade Request | X wants to trade with you | 30 s |

- Cùng khung, vị trí (giữa trên, x ~320-660, y ~125-300), nút Accept vàng / Decline xanh. Mockup Duel có 2 avatar "A VS B".
- Không đóng khi chạm ra ngoài; chỉ đóng khi nhận, từ chối, hết giờ.
- Nhiều lời mời: hiện 1 bảng, hàng đợi tối đa 3 ("1/3 requests"); hạn tính từ lúc server gửi; hết hạn thì chuyển cái sau. Không gửi thêm khi đã có lời mời chờ tới cùng người. Đang duel / trade thì server từ chối yêu cầu mới.
- Settings → Social: Allow Party Invites (Everyone / Nobody), Allow Duel / Trade (Everyone / Party Only / Nobody), danh sách chặn. Không cần làm hệ bạn bè.

### Bên gửi

Giữ bảng Pending góc trên phải nhưng dùng chung cho 3 loại: "Duel → Allie · 12s" + Cancel. Kết quả bằng toast (accepted / declined / expired). Bỏ toast "invite sent" vì trùng với Pending. Mỗi nhân vật chỉ 1 yêu cầu gửi đi đang chờ.

### Việc cần làm theo ưu tiên

| Ưu tiên | Vấn đề | Sửa |
|---|---|---|
| P0 | Tên đè bảng mời | Bảng mời render trên mọi tên nhân vật, nền đủ đặc |
| P0 | Tên trùng 2 lần | Card không bám đầu nhân vật, hoặc ẩn tên của người đang chọn |
| P0 | Chỉ có Invite / Trade | Player Card 6 nút, có Duel |
| P0 | Không có class / level / party | Avatar + thông tin ở đầu card |
| P1 | 2 kiểu bảng mời | Một SocialRequestDialog |
| P1 | Pending + toast trùng | Chỉ giữ Pending, toast cho kết quả |
| P1 | Khó chọn khi đứng chồng | Nearby Picker + Tab trên PC |
| P1 | Nhiều lời mời chồng nhau | 1 bảng + hàng đợi 3 |
| P2 | Không chặn được Duel / Trade | Social Settings |
| P2 | Chưa có Inspect / Whisper / Block | Làm sau khi 3 việc chính ổn |

Code gợi ý: PlayerContextMenu, SocialRequestDialog, OutgoingRequestBadge dùng chung enum Party / Duel / Trade; server vẫn kiểm tra riêng từng loại và mỗi lời mời có mã riêng để Accept không nhầm lời mời đã hết hạn.
