# Wizard: dáng đứng và tay áo, v3

## Sửa lỗi cao thấp giữa skin, 09/11/2026

`fitted()` giờ khớp đoạn cằm–đế thay vì khớp toàn chiều cao ảnh rồi cộng `LIFT` theo bậc. Cằm ở hàng 107, đế ở hàng 189, đầu giữ nguyên tại vị trí gốc. T0–T9 có cùng khung cao người 160 px (đỉnh tóc 29 tới đế 189), cả 4 frame thở có mốc tương ứng giống nhau. Độ rộng áo/giáp và cỡ trang bị vẫn theo bộ; không kéo người cao thêm theo bậc. Tool dò lại bàn tay và dựng lại anchor sau khi khớp, không uốn tay áo.

`verify` kiểm trên 40 ô ảnh đã cài: mốc tóc/đế không lệch T0 quá 1 px, cổ không dời quá 1 px, giữ các hướng khác trong 30 sheet và anchor đầu ngoài idle/down. Bản chạy mới: `Build/wizard-reference/player-height-fixed/Zenonia.exe`; chụp kiểm `Tools/check-wizard-height.ps1` (10 skin hướng xuống + quay phải/lên và đi phải). Chỉ thay dáng đứng xuống; các bộ vẫn chưa có trang phục mới riêng cho đi/đánh.

Người dùng nhận xét tay áo T0 mờ và dáng Wizard rụt rè so với Warrior/Mechanic. Nguồn lỗi: pose hai tay đưa trước ngực, chân khép, cộng thêm biến dạng cục bộ để kéo tay về anchor cũ.

## Bản thay thế

`Assets/ArtLibrary/classes/wizard/outfits/stance-v3/t0..9-body.png`: mỗi bậc vẽ lại bằng built-in image_gen, cùng dáng vai thẳng, ngực mở, hai chân tách rõ, tay đặt tự nhiên hai bên. Tóc trắng và mắt xanh mặc định được giữ nguyên. Bộ sau vẫn có giáp/vạt áo lớn hơn; không uốn tay hoặc kéo nhòe tay áo.

`Tools/build-wizard-stance-v3.py` khớp ảnh vào khung, tách đầu, dò nắm tay thật và tạo 4 frame thở. Chỉ thay hàng idle/down; các hướng/đòn đánh giữ nguyên. **Pipeline này thay lệnh `write` của `build-wizard-reference-down.py` cho idle/down.** Chạy lại tool cũ sẽ trả về dáng trước.

```powershell
python Tools/build-wizard-stance-v3.py check 0 1 2 3 4 5 6 7 8 9
python Tools/build-wizard-stance-v3.py write
python Tools/build-wizard-stance-v3.py verify
```

Trang bị đi theo bàn tay, không biến dạng tay áo để theo trang bị: `HeroRenderer.OutfitAnchors.cs` đọc `body[/set-tN]/held-anchors.json`. Chỉ các entry có trong file được thay; mọi động tác khác dùng anchor class như cũ. Art `weapons/{staff,book}/<tier>/idle-stance.png` là bản có pivot giữa khung dành riêng cho idle, tạo từ art đã chọn; art/pivot dùng khi đi và đánh giữ nguyên. Anchor đầu từng bộ vẫn dùng cơ chế v2.

`HeldAnchor.artY` (mặc định 0, pixel frame, y xuống) cho phép bàn tay thấp hơn nắm đoạn thấp hơn trên cùng cán gậy mà không đẩy đuôi gậy xuyên xuống đất. Chỉ anchor idle/down mới dùng trường này. Sách dùng pivot mép dưới phía trong và anchor của chính nắm tay từng frame; do dùng art riêng, không cộng bob theo tier vũ khí cũ nữa.

Nguồn, prompt, ảnh dò tay, ảnh 4 frame thở, ảnh 5 kiểu tóc và bản sao trước cài nằm trong thư mục `stance-v3/`. Bản chạy dự kiến ở `Build/wizard-reference/player-stance/Zenonia.exe`, build từ bản sao riêng. Debug Look tiếp tục tắt khi mở game.
