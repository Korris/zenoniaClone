# Thử nghiệm 02: skill farm thường xuyên, mỗi nhóm có vai trò

Người dùng chọn ý số 2 trong `docs/online-grinding-ideas.md`, tạm gác tranh luận đổi combo. Làm trên Warrior trước, build Windows rồi chờ test.

## Cách chơi thử

- Giữ phím của ô Power Slash (U/I hoặc L/O tùy ô đang gán): dùng Power Slash khi sẵn sàng, đánh thường trong thời gian hồi. Không cần giữ thêm nút đánh.
- Giữ ô Power Slash bằng chuột hoặc cảm ứng từ 0,3 giây: cùng hành vi như giữ phím. Chạm nhanh dùng một lần; vuốt trên vòng skill vẫn chuyển trang.
- Thả nút để ngừng yêu cầu dùng skill/đánh tiếp. Nhát đánh đã bắt đầu hoặc đã được combo đệm có thể hoàn tất.
- Skill khác vẫn nhấn từng lần, không tự lặp. Giữ nguyên combo, sát thương, phạm vi, animation và VFX.
- Không có tự tìm/chạy đến quái; vẫn tự điều khiển hướng và vị trí bằng WASD hoặc joystick.

## Hồi chiêu Warrior thực tế

| Skill | Vai trò | Hồi chiêu |
| --- | --- | --- |
| Power Slash | Skill farm chính | 1,5 giây |
| Whirlwind | Diện rộng khi gom quái | 8 giây |
| Earth Break | Đòn mạnh phía trước | 12 giây |
| Flame Cleave | Đánh lan và đốt | 10 giây |
| Charging Strike | Áp sát | 12 giây |
| Blade Cyclone | Nhiều đợt sát thương diện rộng | 12 giây |
| Inferno Crash | Nhảy đánh và đốt | 22 giây |
| Berserker Awakening | Tăng sức mạnh | 50 giây |

Phát hiện khi làm mục này: `SkillTable.CooldownScale = 0` khiến bảng skill thực tế không có hồi chiêu, kể cả khi `dev-start.noCooldowns=false`. Vì vậy lời báo trước đây rằng Power Slash hồi 3 giây chưa đúng với bản chạy. Bản này áp dụng các thời gian trên cho Warrior; class khác giữ cấu hình hiện có, chờ duyệt thử nghiệm Warrior trước khi mở rộng.

## Thiết lập và bản build

- Nhân vật mới level 1, học sẵn skill, không tốn MP; giữ HP quái x2 và EXP cần lên cấp x5.
- Không reset hoặc hạ level save hiện có trong mục này.
- Build: `Build/FarmTrial02-Skills/Zenonia.exe`.
- Giữ nguyên cả thư mục khi chép game. Quy tắc hồi chiêu được chia sẻ với server; cần build lại zone server trước khi test online.

## Kiểm tra

Đã chạy: 227 test C# đều qua; build Windows thành công 0 lỗi. Runtime `farm-hold-check.log` báo `PASS casts=4 attackBetween=True stopped=True`: trong 6 giây giữ nút có 4 lần dùng Power Slash, có đánh thường xen kẽ, thả nút thì không cast tiếp. Tiến trình thoát mã 0. Đây là kiểm tra đường đầu vào giữ nút bằng script; chuột/phím/cảm ứng vật lý và chơi online vẫn cần test trực tiếp.

- Test thuần C#: thời gian hồi từng skill Warrior; không dùng lại trước khi hết hồi chiêu; timer skill khác không bị reset; class khác giữ cấu hình cũ.
- Kiểm tra runtime bằng `--class warrior --farm-hold-check`: giữ đầu vào cảm ứng 6 giây, xác nhận có dùng skill theo hồi chiêu và đánh thường xen kẽ, thả nút rồi xác nhận ngừng cast. Chế độ này không đọc/ghi save người chơi.
- Cần người dùng test thao tác chuột/giữ phím, cảm ứng thật, vuốt trang, cảm giác farm và thời gian chờ các skill lớn. Chưa xác nhận phiên online thực tế.
