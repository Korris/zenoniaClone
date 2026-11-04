# Đề xuất vị trí khung chat: mobile và web/PC

Ngày 2026-11-04. Chỉ là đề xuất, chưa sửa code.

## 1. Ảnh chụp thật (exe `C:\temp\ofmap-player\Zenonia.exe`, greenvale, warrior)

- `C:\temp\chat-ui-shots\pc720_1.png` (1280x720)
- `C:\temp\chat-ui-shots\phone_1.png` (1600x720, tỉ lệ 20:9 như điện thoại)
- `C:\temp\chat-ui-shots\desk1080_1.png` (1920x1080)

Thấy được: chat mở sẵn ở (8,134) dưới thanh HP, rộng gần bằng 1/4 màn hình 20:9, cách hero khoảng 1 thân. Đáy giữa màn hình (giữa bình máu và vòng skill) đang trống. Phải-giữa cũng trống.

## 2. Code hiện có

- `Assets/Scripts/ChatWindow.cs:14`: W=310, mở 196, thu gọn 60, mờ sau 7 s. `:63` đặt ở góc trên-trái (8,134). `:38` mở sẵn trên PC, thu gọn trên điện thoại (`Application.isMobilePlatform`). Khi thu gọn vẫn nằm đúng chỗ đó, hiện 3 dòng kênh World.
- `ChatWindow.cs:79-84`: 4 tab World/Map/Party/Whisper. Không có số tin chưa đọc, không có ô nhập tên người nhận khi chat mật, không cuộn lại tin cũ (8 dòng cuối).
- `ChatWindow.cs:155`: chat ẩn khi mở modal, inventory hoặc màn đổi hero.
- `Assets/Scripts/ChatHub.cs:35`: lưu 40 dòng, mỗi dòng tối đa 120 ký tự. Mới chỉ tự vọng lại tin của mình (`LocalChatTransport`), chưa có mạng. Kênh System hiện trong tab World.
- `Assets/Scripts/WindowsHost.cs:89`: Enter mở chat và đặt con trỏ vào ô nhập. `InputAdapter.cs:186`: khi đang gõ, phím của hero bị khóa.
- Khung UI: `Assets/Scripts/UiKit.cs:13,35-39`, chuẩn 960x640, khớp theo chiều cao, nên chiều cao luôn là 640 đơn vị. Chiều ngang 16:9 khoảng 1138 đơn vị, 20:9 khoảng 1422, 4:3 khoảng 853.
- Nút chạm `Assets/Scripts/TouchControls.cs:59`: joystick tâm (128,128) cạnh 200, góc dưới-trái. Bình HP/MP bên phải joystick, mép phải ở khoảng x≈300. Nút đánh ở dưới-phải (-146,112) bán kính 78 (`TouchControls.cs:~105`). Dải skill ở dưới-phải (-215,160) rộng 240 (`SkillMagazineView.cs:30`), mép trái cách phải ≈335. F7 bật/tắt nút chạm (lưu PlayerPrefs). Nút chạm cũng hiện trên PC.
- Minimap ở trên-phải (`Minimap.cs:54`). Nút Quest/Bag/Settings nằm dưới minimap (`TouchControls.Menu.cs:17-27`). HUD ở trên-trái (8,6), rộng 304 (`RpgHud.cs:42`).
- Chưa có bản WebGL. Chỉ có Android trong `ProjectSettings` và chỉ có một nhánh `isMobilePlatform`. Không chỗ nào dùng `Screen.safeArea`.
- Phím PC hiện có (`InputAdapter.cs:42-80`): WASD/mũi tên đi, J/Space đánh, K/Shift lướt, U/I(L)/O ba ô skill, 1/2 bình, Tab đổi trang skill, B túi, T cây skill, E nói chuyện, Enter chat, Esc đóng.

## 3. Game khác làm thế nào (tóm tắt)

- Game nhập vai hành động trên điện thoại (Lineage 2 Revolution, MU Origin, Ragnarok M, Toram): chat để mặc định thu gọn thành 1-2 dòng chạy hoặc một nút. Chạm vào thì mở một ngăn trượt từ cạnh trái, không có popup giữa màn hình. Khi gõ thì ẩn nút điều khiển. Người chơi Lineage 2 Revolution phàn nàn chat và thông báo che tới 70% màn hình.
- OSRS mobile: chuyển chat thành thanh bên trái, mở ra bằng nút ở dưới-trái, cho thu gọn để màn hình thoáng.
- MMO trên PC (WoW, FFXIV, MapleStory, Lost Ark): chat ở dưới-trái, thanh skill ở giữa-đáy. Enter để gõ, Esc để thoát. Tin cũ tự mờ, rê chuột thì hiện nền và cho cuộn. Có tab kênh, gõ `/w tên` để chat mật. Có nút đổi giữa chat gọn và chat mở rộng.
- Roblox DevForum: nên thiết kế giao diện quanh vị trí chat, hoặc ẩn chat khi mở menu (game đã ẩn chat khi mở modal).

## 4. MOBILE (có D-pad và nút chạm)

Áp dụng cho mọi phương án:
- Quanh hero (khoảng 1/3 giữa màn hình) không đặt chat.
- Đặt chat theo vùng an toàn `Screen.safeArea`, vì tai thỏ khi cầm ngang nằm ở cạnh trái hoặc phải.
- Mỗi chỗ chạm tối thiểu 44 pt.

### A1. Dải chat thu gọn ở giữa-đáy, mở ra thành ngăn kéo trái (ĐỀ XUẤT)

- Thu gọn: dải cao khoảng 60 đơn vị ở giữa-đáy, nằm giữa bình (x≈300) và dải skill (cách phải ≈335). Màn 16:9 còn khoảng 480 đơn vị để dùng. Bên trái có nút chat với số tin chưa đọc (đỏ), bên phải hiện 2 dòng mới nhất. Nền gần trong suốt và mờ dần sau 7 s; khi không có tin mới thì chỉ còn nút chat.
- Chạm vào thì mở ngăn kéo trái rộng khoảng 40% màn hình, từ dưới thanh HP xuống mép bàn phím. Tab nằm ngang ở trên cùng (Thế giới, Bản đồ, Tổ đội, Mật, Hệ thống), mỗi tab có số tin chưa đọc riêng. Ô nhập nằm sát ngay trên bàn phím. Đóng bằng X hoặc vuốt sang trái. Đang gõ thì ẩn D-pad và skill để không bấm nhầm; nếu cần thì game vẫn chạy.
- Ưu: không che hero lẫn nút; vị trí giống các game trên điện thoại mà người chơi đã quen; dòng chạy giữa-đáy dễ liếc khi đánh.
- Nhược: màn 4:3 (tablet) chỉ còn khoảng 220 đơn vị ở giữa-đáy, nên trên 4:3 chỉ hiện nút và 1 dòng ngắn. Dải chat nằm gần quái ở dưới hero, nên phải trong suốt và để chạm xuyên qua những chỗ không có chữ.
- Mockup: `C:\Project\zenoniaUnity\plans\reports\chat-ui-mockup-mobile-1.png` (thu gọn), `C:\Project\zenoniaUnity\plans\reports\chat-ui-mockup-mobile-2.png` (mở và có bàn phím).

### A2. Chỉ có nút chat cạnh Quest/Bag/Settings, cộng bong bóng chữ trên đầu người nói

- Không có dòng chạy thường trực. Tin mới chỉ làm hiện số trên nút và bong bóng 3 s trên đầu người nói (chỉ khi người đó đang trên màn hình). Chạm nút thì mở ngăn kéo như A1.
- Ưu: màn hình thoáng nhất, hợp phong cách Zenonia gốc (game chơi một mình).
- Nhược: tin chat thế giới và tổ đội dễ bị bỏ lỡ; game online thì cần thấy tin tổ đội.

### A3. Giữ góc trái nhưng thu nhỏ và hạ xuống trên joystick

- Dải 2 dòng cao khoảng 50 đơn vị, đặt ngay trên joystick (y≈330-380 tính từ đáy), rộng bằng HUD (304).
- Ưu: ít đổi code nhất (chỉ đổi tọa độ và kích thước), giữ ý tưởng hiện tại.
- Nhược: vẫn ở nửa trái, gần đường hero đi sang trái; ngón cái trái dễ chạm nhầm khi kéo joystick lên.

Đề xuất chọn A1. Nếu muốn ít việc nhất thì làm A3 trước, sau đó lên A1.

## 5. WEB/PC (không D-pad, không nút chạm)

Thay D-pad và nút chạm:
- Di chuyển bằng WASD hoặc mũi tên (đã có sẵn). Click để đi là tùy chọn về sau.
- Thanh skill ở giữa-đáy: Space đánh, Shift lướt, U/I/O (sau có thể thành 3-6) skill, 1/2 bình; về sau cho đổi phím.
- Thanh EXP sát mép đáy.
- Tắt TouchControls mặc định trên PC và WebGL (đã có F7 và PlayerPrefs).
- Minimap và các nút menu giữ nguyên ở trên-phải, nút ghi kèm phím tắt.

### W1. Bố cục MMO cổ điển: chat ở dưới-trái, thanh skill ở giữa-đáy (ĐỀ XUẤT)

- Khi rảnh: chỉ có chữ, không nền, khoảng 5-8 dòng, tin cũ mờ dần. Ô nhập chỉ hiện khi nhấn Enter.
- Rê chuột vào thì hiện nền, tab và thanh cuộn. Kéo góc để đổi kích thước (lưu PlayerPrefs).
- Enter gõ, Enter gửi, Esc thoát. Trong ô nhập, Tab đổi kênh; `/w tên`, `/p`, `/m` chọn kênh nhanh. Tab trên cùng có số tin chưa đọc.
- Ưu: người chơi PC quen ngay; màn rộng nên chữ không che hero; góc phải-dưới để trống cho log loot và thông báo.
- Nhược: Tab đang dùng để đổi trang skill (`InputAdapter`), nên khi đang gõ phải đổi nghĩa của Tab.
- Mockup: `C:\Project\zenoniaUnity\plans\reports\chat-ui-mockup-web-1.png`.

### W2. Chat nằm trong thanh đáy, bên trái thanh skill

- Một khung đáy liền: chat ở bên trái, skill ở giữa, túi đồ nhanh ở bên phải, kiểu MU hoặc Ragnarok cũ.
- Ưu: gọn và đều, không có gì nổi trên bản đồ.
- Nhược: chỉ chứa được 3-4 dòng; trông kiểu game cũ; tốn nhiều đồ họa khung.

### W3. Chat ở dưới-phải, thanh skill ở dưới-trái hoặc giữa

- Ưu: tránh xung đột với HUD bên trái.
- Nhược: không giống thói quen của người chơi PC; đụng chỗ của log loot và thông báo.

Đề xuất chọn W1.

## 6. Thứ tự làm sau khi chủ dự án duyệt (gợi ý, chưa làm)

1. Tách cách bố trí chat theo nền tảng, ví dụ `ChatLayout.Mobile` và `ChatLayout.Desktop`, chọn bằng `isMobilePlatform` hoặc dấu hiệu nút chạm.
2. Mobile A1: dải thu gọn ở giữa-đáy, ngăn kéo trái, số tin chưa đọc, ẩn nút điều khiển khi gõ, dùng safe area.
3. PC: thanh skill giữa-đáy, chat dưới-trái mờ dần, Enter/Esc/Tab.
4. Thêm kênh System và Mật (ô tên người nhận hoặc lệnh `/w`) vào ChatHub.

## Câu hỏi mở cho chủ dự án

1. Mobile chọn A1 (dải giữa-đáy) hay A2 (chỉ nút + bong bóng)? Có cần bong bóng chữ trên đầu nhân vật không?
2. Khi đang gõ chat trên điện thoại thì game có tạm dừng không? (Game online thì không dừng được, chỉ ẩn nút.)
3. Bản web sẽ là WebGL chạy trong trình duyệt hay bản exe cho PC? Ô nhập của Unity trên WebGL gõ tiếng Việt kiểu Telex/IME không ổn định, có thể phải dùng ô nhập HTML đè lên.
4. Bản PC có cần click chuột để đi không, hay chỉ WASD?
5. Thanh skill PC dùng U/I/O như hiện tại hay chuyển sang 1-6 (lúc đó bình máu chuyển sang F1/F2 hoặc Q/E)?
6. Có cần tab Hệ thống riêng và tab "Tất cả" không, hay giữ 4 tab?
7. Chat có cần lọc từ xấu, chặn người chơi, chống spam ngay ở bản đầu không?
