# Thử nghiệm 03: Power Slash quét rộng và gom quái

Người dùng thấy Power Slash nhàm và đồng ý thử quét rộng, kéo nhẹ quái để đánh tiếp. Bản Windows: `Build/FarmTrial03-PowerSlash/Zenonia.exe`.

## Thay đổi

- Cung đánh trước mặt rộng 160° (trước 120°), tầm 3 ô (trước 2,5 ô).
- Giữ sát thương 170% ATK ở rank 1, một lần trúng; không thêm đợt sát thương ẩn.
- Quái thường còn sống được kéo tối đa 0,75 ô trong 120 ms về điểm cách nhân vật 1,25 ô phía trước. Dùng va chạm của map, không teleport; không cộng lực tách đám đông trong lúc kéo.
- Boss vẫn nhận sát thương nhưng miễn kéo. Quái sau lưng hoặc ngoài tầm không bị chiêu này tác động.
- Hồi chiêu 3 giây thay vì 1,5 giây ở bản thử 02. Giữ nút vẫn đánh thường trong lúc chờ và dùng lại skill khi sẵn sàng.
- Hiệu ứng chém hiện có được tăng cỡ theo bán kính skill; thời điểm trúng vẫn khớp hiệu ứng ở mốc 0 ms. Giữ cast lock 350 ms và hiệu ứng kết thúc ở 360 ms. Không đổi thân, vũ khí hoặc anchor sprite.
- Giữ HP quái x2, EXP yêu cầu x5, nhân vật mới level 1 và học đủ skill, không tốn MP. Không reset save trong mục này.

## Kiểm tra

Đã build Windows thành công, 0 lỗi. Runtime kiểm tra ở cả bốn hướng down/up/left/right đều PASS (đa mục tiêu, khoảng kéo khoảng 12 world units, phía sau an toàn, boss trúng nhưng không bị kéo). Đã mở xem ảnh trong game `preview-<hướng>_gather.png` ở cả bốn hướng: hiệu ứng quay đúng hướng và cooldown hiển thị khoảng 2,9 giây sau khi dùng. Preview chọn nền trống để không đặt dummy vào tường/cầu thang; bản chụp 4 hướng đầu tiên dùng nhầm hướng mặc định đã được thay bằng ảnh kiểm tra đúng hướng.

- 229 test C# qua, gồm vùng quét hai bên, loại mục tiêu phía sau/ngoài tầm, cooldown và mô tả kéo.
- Runtime `--class warrior --power-slash-check`: quét nhiều mục tiêu, vị trí gần điểm gom hơn và không đi quá 0,75 ô, quái sau lưng không mất máu, boss nhận damage nhưng không bị kéo.
- Cần người dùng đánh giá cảm giác ở bãi quái và thao tác chuột/cảm ứng thực tế. Online cần build lại zone server cùng bộ dữ liệu trước khi kiểm tra.
