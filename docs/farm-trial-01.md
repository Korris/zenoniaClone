# Thử nghiệm 01: nhịp farm Warrior

Ngày: 06/11/2026. Chờ người dùng chơi thử trước khi làm mục tiếp theo.

## Phạm vi

Cập nhật lượt test EXP: tăng yêu cầu EXP mỗi cấp lên đúng 5 lần đường cong cũ (`5 * floor(50 * level^1.5)`), giữ nguyên phần thưởng quái. Level 1 → 2: 50 → 250 EXP (18 Frost Slime thay vì 4); level 5 → 6: 559 → 2795; level 10 → 11: 1581 → 7905. Bản mới: `Build/FarmTrial01-HPx2-EXPx5/Zenonia.exe`, vẫn có HP quái x2. Giữ level và EXP hiện có của save; phần trăm thanh EXP giảm theo ngưỡng mới. Đây là thông số thử nghiệm dựa trên phần thưởng hiện tại của project, chưa đo thời gian lên cấp thực tế. Online cần build lại zone server để dùng cùng đường cong.

Cập nhật theo lượt test: tăng gấp đôi HP tất cả quái và boss qua `MonsterDef` (Frost Slime 36 → 72, Ice Goblin 58 → 116, Frostfang King 580 → 1160). Giữ nguyên ATK, DEF, EXP và loot. Bản Windows mới: `Build/FarmTrial01-HPx2/Zenonia.exe`. Khi đưa lên online cần build lại zone server với cùng định nghĩa HP.

- Windows: giữ chuột trái ngoài giao diện để nối combo, kể cả khi dùng HUD mobile mặc định. WASD hoặc phím mũi tên di chuyển; J/Space cũng giữ để đánh liên tục.
- Điện thoại: giữ nút đánh; joystick điều khiển hướng và di chuyển.
- Warrior đổi hướng theo WASD/joystick giữa các nhát combo; hit box của nhát đang đánh không đổi hướng giữa chừng.
- Power Slash: giữ phím của ô skill đang gán để dùng lại khi sẵn sàng. Trên vòng skill cảm ứng, giữ ô Power Slash từ 0,3 giây để lặp; chạm nhanh vẫn dùng một lần, vuốt vẫn chuyển trang.
- Giữ nguyên thông số Power Slash: hồi chiêu 3 giây và khóa cast. Bản test hiện mở sẵn skill và không tốn MP theo yêu cầu. Các skill khác vẫn dùng từng lần.
- Chưa có click quái để chạy tới/tự đánh, tự chọn mục tiêu hoặc ngắm theo vị trí chuột. Đang chờ xác nhận kiểu chuột PC mong muốn.
- Không đổi sprite, VFX, chỉ số sát thương hay giao thức mạng. Yêu cầu lặp skill đi qua bộ lấy input mạng hiện có; chưa xác nhận bằng phiên online thực tế.

## Bản build thử

Build từ bản sao bằng `Tools/build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\Project\zenoniaUnity\Build\FarmTrial01\Zenonia.exe -FarmTrial`.

Theo yêu cầu cập nhật: bắt đầu level 1, học đủ skill, `noCooldowns=false` (hồi chiêu thật), `noMpCost=true` (không tốn MP). Cấu hình project và `-FarmTrial` cùng dùng thiết lập này. Chế độ test allSkills cho phép học/giữ skill vượt yêu cầu level; bản release vẫn dùng điều kiện học bình thường. Giữ Power Slash vẫn dùng lại khi hết hồi chiêu. Save level 60 cũ cần xóa để bắt đầu level 1; cấu hình level không tự hạ cấp save đã chơi.

## Các điểm cần chơi thử

Đã xác nhận: build Windows thành công, 0 lỗi; chạy EXE offline với bot Warrior, 6 dummy và hai lần dùng skill ở giây 2/6 đều thành công; tiến trình tự thoát với mã 0. Log tại `Build/FarmTrial01/runtime-smoke.log`. Đây là kiểm tra khởi động/combat bằng bot, chưa xác nhận thao tác giữ chuột, giữ skill, cảm ứng hoặc phiên online thực tế.

1. Giữ chuột trái ngoài UI: combo nối liên tục; nhấn UI không phát sinh đòn đánh.
2. Giữ đánh rồi đổi hướng bằng WASD: hướng thay đổi giữa các nhát, không phải đợi hết chuỗi.
3. Giữ phím/ô Power Slash: dùng lại khi hết hồi chiêu, không tốn MP trong bản test này; thả nút thì ngừng yêu cầu dùng lại.
4. Cảm ứng: vừa joystick vừa giữ đánh; giữ Power Slash; vuốt trang skill; mở menu rồi đóng không bị kẹt nút.
5. Đánh giá cảm giác chơi, chuyển hướng và độ gián đoạn khi xen Power Slash vào đánh thường. Chưa đổi cân bằng cho đến khi có phản hồi.

## Thứ tự tiếp theo

Sau khi người dùng test và chỉnh mục 1: thử cảm giác mạnh lên với trang bị → kiểm tra vòng farm online do server quyết định sát thương/phần thưởng → ngoại hình bộ giáp. Mỗi mục hoàn thành cần build Windows và gửi người dùng test.
