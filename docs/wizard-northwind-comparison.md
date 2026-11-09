# Northwind Runesmith V7: bản chạy so sánh riêng

## Chốt dùng Northwind: cán thon, tóc Astral

Người dùng chọn Northwind và yêu cầu không build Astral tiếp. Cán trượng được fit riêng quanh trục trong exporter (Northwind only), giữ chiều dài và phần đầu/núm; bề ngang chỗ cầm tối đa khoảng8px trong khung nhân vật160px, thay vì13–29px. Khoảng đầu40% và đáy16% không bị co; chuyển tiếp mượt để giữ khớp nối. Cùng art được dùng cho idle/đi/đánh; icon và grip xuất lại. Report `Build/wizard-northwind/shaft-width-report.json`.

Đầu mặc định Northwind dùng tóc trắng của Astral (`heads/0/down` nguồn main), khớp cằm94 bằng scale đồng đều. Lựa chọn thứ ba (index2, trước là cam) thay bằng đầu mặc định cho down/up/right để không quay lại tóc cam lúc di chuyển/đánh. Đầu, thân và Astral player chính không bị ghi đè; chỉ bản copy Northwind thay đổi.

## V8: T9 cuối và trượng lớn dần

Đã cài T9 mới giáp băng trắng/bạc/vàng, vai pha lê, áo choàng rộng và trượng bông tuyết lớn; nguồn/prompt imagegen `Assets/ArtLibrary/classes/wizard/outfits/northwind-v8/`. T0–T8 giữ trang phục V7. Scale thân đồng đều theo cằm94–đế189, dùng đầu T0, người cao160px mọi bộ, không bóp tay chân.

Trượng tăng theo160+9*tier px từ T0 tới T9, đo alpha và kiểm tăng nghiêm ngặt bằng installer; `Build/wizard-northwind/staff-height-report.json`. Nguồn gậy được xuất cùng cỡ ở mọi động tác của một tier. Launcher `Play-Northwind.cmd` vào T9 ngay, zoom42 tránh đầu trượng chạm HUD. E/Q hoặc X/Z đổi bộ. Ảnh V8 `Build/wizard-northwind/captures-v8/` (check-wizard-height -IdleOnly -Zoom 42); bảng `docs/reference/wizard-northwind-v8-all-tiers-ingame.png`, ghép bằng `Tools/compose-astral-v8.py` cùng camera với Astral V8. Áo mới chỉ idle/down, thân đi/đánh/hướng khác vẫn cũ.

Theo yêu cầu người dùng 09/11/2026, dùng `docs/reference/wizard-northwind-runesmith-10-tiers-design-v7.png` làm mẫu 10 bộ giáp + gậy + sách. Tóc trắng có mái cyan, mắt xanh, cổ lông và rune; các cặp gậy/sách giữ chủ đề V7 theo bậc.

## Chạy

- Northwind: `Build/wizard-northwind/player/Zenonia.exe` hoặc `Build/wizard-northwind/Play-Northwind.cmd` (vào Wizard ngay).
- Wizard hiện tại: `Build/wizard-reference/player-battle-mage/Zenonia.exe` hoặc `Build/wizard-northwind/Play-Current.cmd`.
- Phím thử E/Q (hoặc X/Z) đổi bậc trang bị/skin; cả hai launcher dùng cùng class, zoom và bậc bắt đầu để dễ so sánh.

Đây là cùng phạm vi với bản Wizard hiện tại: đủ 10 bộ **idle nhìn xuống**, 4 frame thở, đầu tách và anchor tay/trang bị theo bộ. Các hướng đi/đánh vẫn dùng art cũ. Không thay file/save của bản Wizard hiện tại.

## Nguồn và cài đặt

Nguồn imagegen và prompt ở `Assets/ArtLibrary/classes/wizard/outfits/northwind-v7/`: hai sheet cuối `body-sheet.png`, `equipment-keyed.png`, hai prompt gốc và `export-fixes-prompt.txt`. T0–T9 lấy nguyên phom trang phục V7 để so sánh, không áp dụng dáng battle-mage mới của Wizard hiện tại lên Northwind.

`Tools/install-wizard-northwind-v7.py` tách mảng alpha, khử viền magenta chỉ ở mép để giữ ngọc tím bên trong, cài thân/headless/hand, đầu0/down, 10 gậy/sách và icon vào **project copy dưới Build**. Dò tay theo vị trí Northwind để tránh nhận ngọc/viền áo màu da. Người luôn cao160px; sách cầm theo mép dưới, gậy neo bàn tay và không xuyên đất.

`Tools/build-from-copy.ps1 -WizardNorthwind` chạy installer sau khi đồng bộ bản sao và trước khi Unity build. Không có switch thì hành vi build bình thường giữ nguyên. Ví dụ:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/build-from-copy.ps1 -CopyDir D:/zenoniaClone/Build/wizard-reference/project -OutExe D:/zenoniaClone/Build/wizard-northwind/player/Zenonia.exe -Log D:/zenoniaClone/Build/wizard-northwind/build.log -WizardNorthwind
```

Installer chặn target ngoài Build, so hash mọi Resources của Wizard trong project chính trước/sau. Kết quả `Build/wizard-northwind/main-asset-hashes.json`; các sheet hướng khác và anchor đầu ngoài idle/down được giữ. Bản exe battle-mage đã build trước vẫn nằm riêng, không ghi đè.

### Sửa đồ bị bẹt theo phản hồi 09/11

Bản đầu ép phần cằm–đế xuống82px nhưng khớp ngang theo đầu83px, khiến thân áo bị nén dọc18–23%. Bản sửa dùng **cùng hệ số ngang/dọc** cho cả thân áo, cằm94–đế189 (95px), đầu79×65 ở hàng29. Tổng người vẫn160px; không kéo cao nhân vật để làm áo dài hơn. Bốn kiểu tóc thay thế cũng được căn về mốc đầu/cằm mới trong bản copy, không sửa tóc ở project chính. Gậy/sách và điểm cầm được xuất lại theo bàn tay mới.

## Kiểm tra

Build 0 lỗi; verify 30 sheet giữ hướng khác, 40 frame/anchor, cùng chiều cao, đủ10bộ. Capture dùng `Tools/check-wizard-height.ps1 -Exe .../wizard-northwind/player/Zenonia.exe -Output .../wizard-northwind/captures`, cùng camera như bản hiện tại. Ảnh so sánh và tổng 10 bộ lưu ở docs/reference.
