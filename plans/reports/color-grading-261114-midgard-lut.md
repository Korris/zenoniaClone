# Bộ lọc màu Midgard (LUT) - 2026-11-14

## Việc đã làm
1. `Tools/build-color-grade-lut.py` (py, có numpy+scipy+PIL sẵn trong máy): từ cặp ảnh `docs/reference/palette-test-our-frost-hollow-fight-ingame.png` (1600x900) và `palette-test-midgard-fight-regrade-1.png` (1672x941, tự resize LANCZOS về 1600x900) suy LUT 32^3. Mỗi ô = median màu đích của pixel nguồn rơi vào ô (median chịu được số ít pixel lệch do nội dung khác nhau, vd số sát thương/EXP đổi giữa 2 ảnh, thanh máu khác độ dài). Ô trống (~75% tổng số ô, ảnh chụp không phủ hết gam màu) = hồi quy affine toàn cục `dst = src@A + b` (least-squares) qua mọi pixel đã đo.
   - Sai số trung bình tuyệt đối trên các ô đo được (thang 0..255): **14.60**.
   - Xuất LUT dạng strip 1024x32 (32 lát 32x32, lát = mức blue) vào `Assets/Resources/Sprites/fx/color-grade-midgard-lut.png` + `.meta` tay (uncompressed, không mipmap, bilinear, clamp, sRGB off) — **nhưng lưu ý bên dưới, `.meta` này không có tác dụng khi build từ bản sao**.
   - Ảnh kiểm `plans/reports/color-grade-check.png`: trái = LUT áp lên ảnh gốc (nội suy trilinear bằng scipy, đúng công thức shader dùng), phải = ảnh đích ChatGPT. Đã xem: khớp tông khá tốt (tuyết hồng nhạt, nước xanh ngọc, gỗ cầu ấm).

2. Shader + hiệu ứng Unity (pipeline Built-in, giống `BloomEffect.cs` dùng `OnRenderImage`, không phải URP):
   - `Assets/Resources/Shaders/ColorGradeLut.shader`: lấy mẫu LUT 2 lát blue kề nhau rồi lerp theo phần dư, có `_Strength` (0..1, đang set cứng 1 khi bật).
   - `Assets/Scripts/ColorGradeEffect.cs`: `[RequireComponent(Camera)]`, nạp shader + texture từ Resources, `OnRenderImage` blit qua material. Tắt bật qua `enabled` (giống Bloom).
   - `Assets/Scripts/ColorGradeConfig.cs`: cờ `Enabled` lưu PlayerPrefs (`zenonia.colorgrade.midgard`), mặc định **TẮT**. Có thêm `SetForThisRunOnly` để `--grade` của capture không ghi đè PlayerPrefs của người chơi thật.
   - Phím tắt: **F12** (chưa dùng — F9 đã là SolidDebug, F10 là DevBuffKey, F11 là Switch6; thêm `GameKey.ColorGrade` cuối enum + map trong cả 2 nhánh `InputAdapter.cs` — legacy Input Manager và Input System mới).
   - `FeelHotkeys.cs`: F12 lật `ColorGradeConfig.Enabled` + `colorGrade.enabled`, cùng chỗ với F6 (bloom).
   - `Bootstrap.cs`: gắn `ColorGradeEffect` lên camera cạnh `BloomEffect`, truyền vào `FeelHotkeys.Init`.
   - `SettingsWindow.cs`: thêm dòng "Midgard color grade" (ô thứ 3, cửa sổ cao thêm 70px: 300→370), bật/tắt áp dụng ngay (không cần đổi map).
   - `DebugCapture.cs`: cờ `--grade midgard` gọi `ColorGradeEffect.ApplyDebugFlag` (bật tạm thời, không lưu PlayerPrefs).

## Lỗi gặp và cách sửa (ghi lại để khỏi lặp)
- Lần build đầu ảnh "bật" bị nhiễu lốm đốm nặng (không phải tông Midgard mượt như ảnh mẫu). Nguyên nhân: file LUT nằm dưới `Assets/Resources/Sprites/` nên bị `Assets/Editor/ResourceTextureImportSettings.cs` (AssetPostprocessor có sẵn trong repo) tự ép bật mipmap/trilinear bất kể `.meta` tôi viết tay — và `build-from-copy.ps1` robocopy `/XF "*.meta"` nên bản copy build luôn tự tạo `.meta` riêng, `.meta` tay của tôi vô tác dụng ở đó. `tex2D` chọn mip theo đạo hàm UV màn hình; toạ độ LUT nhảy phi tuyến theo màu cảnh (không theo vị trí màn hình) nên đạo hàm cực lớn → GPU chọn mip mờ/sai → ra nhiễu. Sửa: đổi `tex2D` thành `tex2Dlod(...,0,0)` ép LOD 0 trong shader, không phụ thuộc cấu hình mipmap của importer nữa. Build lại, hết nhiễu, khớp ảnh mẫu.

## Build & chụp
- Build từ bản sao (không đụng project đang mở): `Tools/build-from-copy.ps1 -CopyDir C:\temp\zu-copy -OutExe C:\temp\player-wizard-d\Zenonia.exe` → **Succeeded errors 0** (2 lần, lần 2 sau khi sửa shader).
- Đã chụp & tự xem bằng mắt (không chỉ tin báo cáo số):
  - Frost Hollow, tắt grade: tông gốc bình thường (tuyết trắng xanh, HUD rõ).
  - Frost Hollow, bật `--grade midgard`: tuyết hồng nhạt, nước xanh ngọc, gỗ cầu ấm — khớp tông ảnh ChatGPT đã duyệt, mượt không nhiễu.
  - Oakhaven Village, tắt/bật: bật ngả sepia ấm (LUT học từ cảnh tuyết nên áp lên cảnh làng chỉ là một phép ngoại suy hợp lý, không tệ nhưng dĩ nhiên không phải mục tiêu chính — đây chỉ là bộ lọc thử).
  - HUD (thanh HP đỏ/MP xanh, minimap, chữ) **vẫn bị áp màu luôn** vì hiệu ứng chạy ở `OnRenderImage` của camera (toàn màn hình, gồm cả UI canvas Overlay vẽ trước khi camera blit hậu kỳ) — các thanh máu/MP vẫn đọc được rõ ràng ở cả 2 cảnh đã xem, không bị lẫn màu nền.
  - Ảnh ghép 2x2 (tắt/bật x 2 cảnh): `docs/reference/color-grade-midgard-ingame-compare.png`.
- Test: `dotnet test Tests/dotnet/ZenoniaSkills.Tests` → 313 passed. `dotnet test Tests/dotnet/ZenoniaNet.Tests` → 404 passed. (File mới không nằm trong danh sách `Compile Include` của 2 csproj test nên không cần loại trừ gì thêm.)

## File đổi / tạo
- Mới: `Tools/build-color-grade-lut.py`, `Assets/Resources/Shaders/ColorGradeLut.shader`, `Assets/Scripts/ColorGradeConfig.cs`, `Assets/Scripts/ColorGradeEffect.cs`, `Assets/Resources/Sprites/fx/color-grade-midgard-lut.png(+.meta)`, `docs/reference/color-grade-midgard-ingame-compare.png`, `plans/reports/color-grade-check.png`.
- Sửa: `Assets/Scripts/Bootstrap.cs`, `Assets/Scripts/InputAdapter.cs`, `Assets/Scripts/FeelHotkeys.cs`, `Assets/Scripts/SettingsWindow.cs`, `Assets/Scripts/DebugCapture.cs`.
- Không đụng `dev-start.json`, không đụng file Wizard bộ D (`Assets/ArtLibrary/classes/wizard/...`, `Assets/Resources/.../wizard/...` đang dở từ việc khác — đã kiểm `git status` trước/sau, chỉ các file trên đổi).
- Không commit theo yêu cầu.

## Việc còn / câu hỏi chưa chốt
- LUT hiện chỉ huấn luyện từ 1 cặp ảnh cảnh tuyết (Frost Hollow chiến đấu): tông ở cảnh khác (làng, hang, v.v.) là ngoại suy, có thể cần thêm cặp ảnh khác nếu muốn tông ổn định toàn bộ map sau này.
- Chưa thêm `--grade` vào `--settings-show`/test tự động khác; chỉ dòng Settings mới và phím F12 + cờ CLI `--grade midgard` hoạt động.
- Độ mạnh (`_Strength`) đang cứng 1.0 khi bật; shader đã có property sẵn nếu sau muốn thêm thanh trượt UI.
- `plans/reports/shots/*.png` (ảnh chụp thô dùng để ghép) đã xoá sau khi ghép xong, chỉ giữ ảnh ghép cuối + check image.
