# Sửa màn Customize and Name (19/11/2026)

Gốc `ro-fantasy-heroes` 1902f085. Exe: `C:\temp\player-custfix\Zenonia.exe`, bản sao build `C:\temp\zu-copy-custfix` (Library chép từ `zu-copy-offline`), Succeeded, 0 lỗi.
Không build vào `D:\`: ổ D chỉ còn 1,7-5,8 GB (bản sao 16 GB, exe 3,7 GB). Lần build đầu ra `D:\temp\player-custfix` hỏng vì hết chỗ, đã xoá.

## 1. Hình mờ
- **Nguyên nhân:** `HeroPreviewStage` vẽ hero vào RenderTexture cố định 16 px/đơn vị (384x480, bilinear). Canvas lại phóng texture đó lên 742 px cao (1080p), tức 1,55x; ở 1440p là 2x. Sprite bị lọc hai lần: texture -> RT, rồi RT -> màn hình. Trong game sprite chỉ lọc một lần.
- **Sửa:** mỗi frame texture đổi cỡ theo chiều cao RawImage trên màn hình (1 texel = 1 pixel), lọc point, có mipmap. Texture giữ nguyên object, nên các thẻ nhỏ ở màn chọn slot vẫn dùng chung và đọc mip.
- Kiểm: `preview texture (816, 742) for a view 742.4 px tall`. Ảnh crop phóng 3x (`custfix-04`): nét viền mảnh và rõ hơn trước, độ mềm giờ ngang hero trong game.
- Ảnh vẫn hơi mềm khi phóng to, vì art 200 px được phóng ~2,5x bằng bilinear, giống hệt trong game.

## 2. Vũ khí không nhún theo nhịp thở
- **Nguyên nhân:** `HeroLayeredView.PlaceWeapon` không nhân `breathY`, trong khi tóc, mũ và lớp nắm tay đều đã nhân. Lỗi có cả trong game, ở mọi class và mọi hướng, không riêng màn chọn.
- **Sửa:** grip y nhân `breathY`. Vũ khí chỉ dời theo nắm tay, không co giãn.
- Kiểm trong game bằng capture mới `--class warrior --record-layered-breath --shot-path DIR`:
  - Bố trí: 5 class x 8 hướng, chung nhịp thở.
  - Đo: tỉ lệ y / scale của vũ khí, tay phụ, nắm tay và tóc không đổi; vũ khí dời khoảng 0,1 đơn vị giữa đỉnh và đáy nhịp thở.
  - Kết quả: **40/40 PASS**.
- Ảnh màn chọn (`custfix-05`, `custfix-06`): thấy rõ kiếm, khiên, gậy và nắm tay lên xuống theo lưới đỏ.
- Ảnh trong game (`custfix-07`): mắt gần như không thấy vì biên độ chỉ 1-2 px ở zoom game. Phần này dựa vào số đo.

## 3. Bỏ nút
- Bỏ nút xoay `<` `>` và tab IDLE / WALK / ATTACK. Preview chỉ còn idle hướng xuống (Dir8 0), có thở.
- `HeroPreviewStage` bỏ luôn chế độ walk / attack. Màn chọn slot vẫn có nút xoay riêng, vẫn dùng `SetPose(slot, dir)`.
- Cột preview rộng 33 đơn vị, bằng cả ô thân 320 px. Hero vẽ trên khung màu, nên mũi kiếm Warrior không còn bị cắt (bản cũ bị cắt thẳng ở mép texture).

## Code
- `HeroLayeredView.cs`: thêm `breathY` cho vũ khí.
- `HeroPreviewStage.cs`: viết lại phần texture, chỉ còn idle, thêm `ViewIn`, `TextureSize`, `ViewScreenHeight`.
- `Net/Client/CharacterCustomizeScreen*.cs`: bỏ nút và tab, cố định hướng xuống, cột 33 đơn vị.
- `CharacterListScreen.View.cs`: sửa `SetPose`.
- Capture:
  - `DebugCapture.AppearanceBreath.cs` (mới): kiểm nút đã bỏ, hướng xuống, cỡ texture, nhịp thở.
  - `DebugCapture.LayeredBreath.cs` (mới).
  - `DebugCapture.Appearance.cs`: bỏ các lần bấm nút đã bỏ.
  - `DebugCapture.CharacterSelect.cs`: sửa test "tên trùng" như ghi ở phần dưới.
- `docs/sprite-rules.md`: thêm 2 dòng Sổ lỗi.

## Kiểm tra
- dotnet (SDK 10): ZenoniaSkills 435/435, ZenoniaNet 422/422.
- `--appearance-test`: 41 PASS, 0 FAIL.
- `--character-select-test`: 27 PASS, 0 FAIL.
  - Test "tên trùng bị từ chối" đã FAIL sẵn ngay trên exe gốc `ro-fantasy-261119`: lời từ chối hiện ở dòng hint, còn dòng status để trống.
  - Đã sửa test để chấp nhận hint, không đổi game.
- `--record-layered-breath`: 40/40 PASS.

## Ảnh `docs/reference/character-select-ro-fantasy/` (đã xem)
- `custfix-01-before-warrior.jpg`, `custfix-02-after-warrior.jpg`, `custfix-03-after-cleric.jpg`: toàn màn hình trước và sau.
- `custfix-04-sharpness-before-left-after-right-zoom3x.png`: crop mặt phóng 3x, trước (trái) và sau (phải).
- `custfix-05/06-preview-breath-*`: 4 khung đỉnh/đáy nhịp thở, kèm zoom 4x vũ khí và nắm tay.
- `custfix-07-ingame-breath-5-classes-down-top-vs-bottom.jpg`: hero hướng xuống trong game, đỉnh và đáy nhịp thở.
- `custfix-08-ingame-mechanic-8-dirs.jpg`: Mechanic đủ 8 hướng trong game.
- Chưa xem:
  - Màn hẹp / điện thoại, 4K.
- Màn chọn slot với texture mới: đã xem ảnh `07b` của `--character-select-test`. Hero lớn và 4 thẻ nhỏ đều nét, không vỡ hình.
  - Online.

## Câu hỏi
- Có cần tăng biên độ thở (hiện ±0,75 %) cho dễ thấy không?
