# Wizard: mẫu người dùng 08/11/2026, idle hướng xuống

Nguồn: `9670d96f-6102-4692-8a05-79ebebdb31ad.png`, bảng 2 hàng × 5 bậc. Bảng mới này thay mẫu áo/gậy/sách cũ của Wizard. Tay phụ là sách phép đúng hình mẫu (T3 bìa vỏ sò), dùng slot offhand hiện có.

## Art và ảnh kiểm tra

- `Assets/ArtLibrary/classes/wizard/outfits/reference-20261108/ladder-large.png`: bảng gốc phóng 3× nearest.
- Cùng thư mục: `tN-design-large.png`, `tN-equipment-sheet.png`, `tN-{body,staff,book}.png` cho T0–T9; ảnh được tạo bằng built-in image_gen, prompt trong `prompts.json`. Mỗi bậc dùng ảnh pose idle game và mảnh thiết kế phóng lớn riêng.
- `docs/reference/wizard-reference-20261108-vs-sprites.png`: hình mẫu cạnh sprite đã ghép đủ áo, gậy, sách và nắm tay.
- `breathing-all-tiers.png`: 10 bậc × 4 frame thở trên nền tối.
- `hairstyles-all-tiers.png`: 10 bậc × 5 kiểu tóc, dùng lớp đầu hiện có.
- `before-install/`: bản sao art và manifest trước khi cài, giữ nguyên đường dẫn tương đối của từng loại để không trùng tên gậy/sách.

## Cài vào game

`Tools/build-wizard-reference-down.py` tách sheet, khớp chiều cao/tâm mặt với pose game, căn mềm hai nắm tay về điểm cầm, tạo 4 frame thở, cắt lớp đầu bằng tool hiện có. Áo ghi vào hàng 0 của `body/{bare,hand,headless}/idle.png` (T0) và `body/set-t1..9/`.

Gậy và sách ghi vào manifest/tên art hiện có, kèm icon gậy, sách và áo của từng bậc. Sau phản hồi người dùng, **không ép mọi bộ/trang bị về cùng cỡ**: bề ngang áo tăng từ khoảng 125 px tới 180 px, sách từ 0,25 tới 0,38 chiều cao nhân vật chuẩn, gậy tăng theo bậc. Vai và mép áo choàng mở rộng, phần giữa ngực và giày giữ tỉ lệ tự nhiên. Các bộ sau nâng cổ/đầu tới 15 px nhưng giữ cỡ đầu; `HeroHeadLayer` đọc `head-anchors.json` riêng của bộ khi có. Art gậy/sách được dùng chung giữa các hướng và động tác theo hệ trang bị hiện tại; phần thân mới chỉ có idle hướng xuống.

Kiểu mặc định 0 hướng xuống lấy tóc trắng mềm, mặt và mắt xanh đúng hình người dùng gửi (`default-head-user-design.png`, built-in image_gen). `pose-head-before-user-design.png` chỉ là mẫu căn khung cũ, không được dùng làm đầu mặc định trở lại.

Lệnh tái tạo:

```powershell
python Tools/build-wizard-reference-down.py split
python Tools/build-wizard-reference-down.py head
python Tools/build-wizard-reference-down.py write
python Tools/build-wizard-reference-down.py verify
```

`verify` kiểm tra 30 sheet giữ nguyên các hàng hướng khác, toàn bộ anchor ngoài idle/down giữ nguyên (kể cả sách khi đánh), đủ 40 mặt nạ nắm tay, đúng kích thước sheet và metadata Unity. Không chạy trực tiếp tool gậy/sách cũ để cài bộ này: nó đọc nguồn bảng cũ và xóa các anchor sách khi đánh; wrapper mới giữ lại các anchor đó.

Bản build thử dùng bản sao ở `Build/wizard-reference/project`, không build qua project đang mở trong Unity. Bản đã sửa độ lớn và kiểu tóc: `Build/wizard-reference/player-grandeur/Zenonia.exe`. Thư mục `player` là bản cũ người dùng chê các bộ sau quá nhỏ.

Build bản sửa đã thành công, 0 lỗi (`Build/wizard-reference/build-grandeur-final.log`). Kiểm tra ảnh trong game bằng `--offscreen-capture` (camera/canvas render vào texture vì cửa sổ ẩn không có framebuffer đọc được), ảnh mới ở `Build/wizard-reference/captures-grandeur/`. Đã xem đủ T0–T9 với tóc mới; bảng ảnh thật trong game ở `docs/reference/wizard-reference-20261108-ingame-v2.png`, so trước/sau cùng camera ở `docs/reference/wizard-reference-20261108-before-after.png`. Bản chạy giữ nguyên toàn bộ thư mục `player-grandeur` đi kèm exe.

Kiểm tra cuối: 33 sheet giữ nguyên hàng hướng khác, mọi anchor ngoài idle/down giữ nguyên, 40 mặt nạ nắm tay hợp lệ; bề ngang áo T0/T9 thực tế 124/181 px. Đầu mặc định mới 83×78 px đặt trong ô 192×192, alpha thật; prompt trong `reference-20261108/default-head-prompt.txt`.

Theo yêu cầu tắt bảng Debug Look: bản mới nhất ở `Build/wizard-reference/player-clean/Zenonia.exe`. `Hud` luôn mở game với bảng debug tắt, không đọc trạng thái bật đã lưu; F8 chỉ bật/tắt trong phiên hiện tại. Tóc và toàn bộ art Wizard của bản grandeur được giữ lại.
