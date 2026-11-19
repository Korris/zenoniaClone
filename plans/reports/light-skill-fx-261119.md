# Hiệu ứng ánh sáng kiểu RO3 "Cột Ánh Sáng / Mưa Sao Băng" (19/11/2026)

Branch: worktree `worktree-agent-a819ebcffc0ad67a3`, dựa trên `unity` (1bc7ffb0). Chưa push, chưa merge.

## Gắn vào skill nào

- **Wizard skill 7 "Astral Cataclysm"** (`wizard.astral-cataclysm`): Rain 8 sao băng, cách nhau 200 ms, vùng rơi bán kính 4 ô, mỗi vụ nổ bán kính 1,5 ô.
- Chỉ đổi phần hình. Không đổi số liệu trong `SkillTable`, không cần build lại server.
- Bản cũ là choreography màu tím (3,1 s). Màu tím sai quy tắc màu Wizard, và các cú nổ không trùng lúc sát thương. Bản mới dùng xanh băng, mỗi sao băng chạm đất đúng bước sát thương của nó.

## Kiến trúc

| File | Vai trò |
|---|---|
| `Assets/Resources/Shaders/LightBeamScroll.shader` | Cột sáng + chùm tia nổ: trượt UV, uốn bằng nhiễu, dissolve |
| `Assets/Resources/Shaders/LightTrail.shader` | Đuôi sao băng cho `TrailRenderer` |
| `Assets/Resources/Shaders/LightBlob.shader` | Ánh sáng mặt đất (thay `Light2D`), đầu sao băng, vòng nổ |
| `Assets/Scripts/AstralLightFx.cs` | Điều phối: cột sáng giữa vùng, 8 sao băng, 8 vụ nổ |
| `Assets/Scripts/LightMeteor.cs` | "Prefab" sao băng: đầu sáng + `TrailRenderer` |
| `Assets/Scripts/LightImpact.cs` | Vụ nổ: ánh sáng đất, vòng, tia, cột sáng ngắn |
| `Assets/Scripts/LightSkillAssets.cs` | Material, texture, mesh quad dùng chung |
| `Assets/Scripts/Rpg/Skills/LightSkillTiming.cs` | Thời gian, không phụ thuộc engine, có test |
| `Tools/build-light-skill-textures.py` | Sinh texture tạm (`gen`), nhập ảnh vẽ (`import`), ảnh xem trước (`preview`) |
| `Tools/build-light-skill-capture-sheet.py` | Ghép chuỗi ảnh chụp trong game |

Cách nối vào game:
- `SkillExecutor.Cast` gọi `AstralLightFx.Cast` thay cho `SkillChoreoCastHook.Play`.
- Preload khi spawn Wizard.
- Dọn khi đổi map: thêm vào `ProxySpawner.ClearForTransfer`.
- Chỉ chạy ở `NetMode.RendersFx`. Sát thương vẫn do `SkillRun` / `SkillEffects.Rain` làm như cũ.

Vị trí và thời gian:
- Vị trí rơi tính bằng `SkillScatter.InDisc` với cùng seed như `SkillEffects.Rain`, nên vụ nổ nằm đúng vòng sát thương.
- Giờ chạm đất lấy từ dữ liệu skill: `LightSkillTiming.LandMs = AtMs + i × IntervalMs`. Không có số gõ tay.
- Test `LightSkillTimingTests` so khớp giờ này với `SkillSchedule`.

Các mốc thời gian:
- Sao băng rơi trong 320 ms.
- Sao băng của giọt 0 chạm đất đúng lúc cast. Vì vậy cột sáng mở ra trong 0,15 s ngay khi cast để "đỡ" cú đầu.
- Cột sáng tan (dissolve) trong 380 ms sau giọt cuối.

## Vì sao không dùng `Light2D` và Bloom chuẩn

- Project chạy **Built-in render pipeline**, không cài URP. `Light2D` chỉ có trong URP 2D Renderer, và Post-Processing Bloom chuẩn cũng không có.
- Bloom: dùng `BloomEffect` + `BloomLite` có sẵn. `Bootstrap` tự gắn vào camera khi `FeelConfig.BloomOn = true`, camera chạy HDR, ngưỡng 1,25.
  - Màu `[HDR] _EmissionColor` vượt 1,25 thì bloom bắt.
- Ánh sáng mặt đất là đèn giả: quad `LightBlob` hình tròn mềm, vẽ ở `UltimateFx.GroundFxOrder + 2` (trên nền đất, dưới mọi sprite xếp theo y), gồm 2 pass:
  - lớp tint xanh alpha 0,4: thấy được trên tuyết;
  - lớp cộng HDR: vũng sáng trên nền tối.
- Đường cường độ theo `LightSkillTiming.GroundLight`: từ 0 lên đỉnh trong 0,15 s (ease-out), rồi giảm về 0 lúc vụ nổ kết thúc (0,56 s, giảm bậc 2).
- Muốn có `Light2D` thật (chiếu cả lên sprite nhân vật và quái) thì phải chuyển cả project sang URP + 2D Renderer. Đây là việc lớn:
  - mọi shader tự viết (`ZenoniaProto/*`, CG, Built-in) phải viết lại;
  - material sprite phải chuyển sang Sprite-Lit;
  - `BloomEffect` (dùng `OnRenderImage`) không chạy trên URP, phải thay bằng Volume Bloom.

## Tham số shader

`ZenoniaProto/LightBeamScroll` (quad `MeshRenderer`; không dùng `SpriteRenderer` vì nó ghi đè `_MainTex`):

| Tham số | Ý nghĩa | Cột sáng / chùm tia |
|---|---|---|
| `_MainTex` | vân tia sáng xám, liền mạch theo chiều dọc | `beam-streaks` |
| `_NoiseTex` | nhiễu mây xám, liền mạch | `noise-clouds` |
| `_ScrollSpeed` | tốc độ trượt tia (uv/s); tia đi lên, hoặc toả ra ở chế độ polar | 1,6 / 2,4 |
| `_NoiseScroll`, `_Distort` | nhiễu trôi và độ uốn tia | 0,45 / 0,12 |
| `[HDR] _EmissionColor` | màu cộng phát sáng | (0,1, 0,5, 1,35) |
| `_CoreWhite` | lõi trắng nóng | 0,55 |
| `_BodyColor` | lớp alpha xanh bão hoà dưới ánh sáng, để đọc được trên tuyết | (0,08, 0,38, 0,95, a 0,6) |
| `_Dissolve`, `_DissolveEdge`, `[HDR] _EdgeColor` | tan theo nhiễu, viền sáng chỗ đang tan | đổi theo thời gian |
| `_Polar`, `_Ring`, `_RingWidth` | chế độ chùm tia toả tròn | chùm tia: `_Polar` = 1 |
| `_Fade` | mờ tổng (đặt riêng từng renderer bằng `MaterialPropertyBlock`) | |

- `LightTrail`: `_NoiseTex`, `_NoiseScroll`, `[HDR] _EmissionColor` (nhân với gradient), `_CoreWhite`, `_BodyColor`.
- `LightBlob`: `_MainTex` (mask), `_Falloff`, `[HDR] _EmissionColor`, `_CoreWhite`, `_BodyColor`, `_Fade`.

Đuôi sao băng (`LightMeteor`):
- `TrailRenderer`, time 0,22 s, minVertexDistance 0,5, 4 cap vertex, rộng 7 đơn vị, thon dần.
- Gradient: trắng → xanh băng (0,55, 0,88, 1) → xanh (0,2, 0,6, 1), alpha 1 → 0.
- Sao băng bay thẳng, mỗi frame cập nhật vị trí, nên ở 60 FPS đuôi mượt, không gãy khúc.
- Project không dùng prefab asset (mọi thứ dựng bằng code), nên "prefab" là một template GameObject dựng một lần rồi `Instantiate` cho từng sao băng.

## Bật / tắt để so sánh

- Mặc định bật bản mới.
- Chạy với `--light-fx off` thì dùng lại choreography tím cũ.
- Khi đang chạy có thể đặt `AstralLightFx.Enabled = false`.
- Shader thiếu hoặc không hỗ trợ thì tự quay về bản cũ.

## Texture

Mọi texture là mask xám trên nền đen; shader chỉ đọc kênh đỏ.

| Texture | Nguồn hiện tại | Ghi chú |
|---|---|---|
| `beam-streaks` 128x256 | **tạm, Python** | wrap Repeat |
| `noise-clouds` 256x256 | **tạm, Python** (fBm + domain warp) | wrap Repeat |
| `meteor-core` 128x128 | **tạm, Python** | |
| `burst-ring` 256x256 | **tạm, Python** | |

- Nguồn: `Assets/ArtLibrary/fx/light-skill/*-procedural.png`.
- Bản game: `Assets/Resources/Sprites/fx/light-skill/`, kèm `.meta` (Default texture, không mipmap, không nén).
- Ảnh xem trước: `docs/reference/light-skill-fx/textures-preview.png` (lát 2x2, không thấy đường nối).
- Lúc làm Tay đang hết lượt (đến 15:58), nên làm bản tạm trước.

### Bản ChatGPT (Tan Pham): CHƯA LÀM ĐƯỢC

- Đã làm:
  - mở tab mới của riêng mình trên Chrome đang dùng;
  - đọc `/api/auth/session` ra "Tan Pham";
  - không đụng tab chat nhân vật 6afe7669.
- Bị chặn: lệnh JS dán hàm gửi prompt vào trang ChatGPT bị bộ phân loại quyền của Claude Code (auto mode) từ chối với lý do "Unauthorized Access".
  - Theo quy định, tôi không tìm cách khác để vượt chặn. Chưa gửi prompt nào, chưa tốn lượt vẽ. Tab của tôi đã đóng.
- Muốn làm tiếp:
  - người dùng cho phép thao tác JS / gửi tin trên chatgpt.com (thêm quyền cho Claude in Chrome trong cài đặt), hoặc
  - tự gửi 4 prompt dưới đây rồi chạy `python Tools/build-light-skill-textures.py import <tên> <ảnh>` cho từng ảnh.
- Prompt đề xuất (mỗi ảnh một prompt, nền đen phẳng, không trong suốt, không ô caro):
  1. `beam-streaks`: "Texture vệt tia sáng dọc, trắng xám trên nền ĐEN phẳng #000000, nhiều tia mảnh dài song song chạy thẳng đứng, độ sáng và độ rộng khác nhau, đứt quãng nhẹ; liền mạch theo chiều dọc (mép trên nối khít mép dưới); không màu, không chữ, không ô caro, không nền trong suốt."
  2. `noise-clouds`: "Texture nhiễu mây xoáy uốn lượn, xám trung tính trên nền đen, phân bố đều khắp ảnh, liền mạch cả 4 cạnh (tileable); không chữ, không ô caro."
  3. `meteor-core`: "Đầu sao băng nhìn chính diện ở giữa ảnh: lõi tròn trắng nóng, quầng mờ xanh băng toả mềm ra, 4 tia loé mảnh; nền đen phẳng #000000, không trong suốt."
  4. `burst-ring`: "Vòng nổ ánh sáng tròn đều ở giữa ảnh, dải sáng trắng xanh băng ở khoảng 78 % bán kính, mép hơi lởm chởm, vài tia toả từ tâm mờ; nền đen phẳng #000000."
- Lệnh `import` lấy kênh sáng nhất làm mask xám và đổi cỡ. Nếu ảnh ChatGPT không liền mạch thì phải thêm bước làm liền mạch (trộn với bản dời nửa ảnh); hiện lệnh `import` chưa có bước này.

## Ảnh chụp trong game (đã tự xem)

Ảnh ở `docs/reference/light-skill-fx/`. Mỗi tấm có 12 khung: 0, 100, 200, 300, 450, 600, 800, 1000, 1200, 1400, 1600, 1800 ms sau cast.

| Ảnh | Nội dung |
|---|---|
| `snow-new-procedural.jpg` | bản mới, Frost Hollow (tuyết) |
| `snow-old.jpg` | bản cũ, tuyết |
| `dark-new-procedural.jpg` | bản mới, Rootburrow (rừng tối) |
| `dark-old.jpg` | bản cũ, Rootburrow |

Đã thấy trên ảnh:
- **Tuyết:**
  - Cột sáng xanh có vân dọc, ra khoảng 100 ms.
  - Vòng nổ xanh đọc rõ.
  - Đuôi sao băng chéo, mảnh, nhạt trên tuyết. Đầu sao băng gần như chìm trên tuyết.
  - Không còn mảng trắng loá.
- **Lần chụp đầu trên tuyết bị trắng loá toàn bộ:** phần cộng HDR quá cao. Đã hạ xuống và ghi vào Sổ lỗi `docs/sprite-rules.md`.
- **Nền tối:**
  - Sao băng có đầu sáng và đuôi xanh rõ.
  - Vòng nổ và chùm tia sáng rõ, có quầng bloom.
  - Cột sáng có vân trượt.
  - Tan dần hết ở khoảng 1800 ms.
- **So với bản cũ:**
  - Bản cũ là vòng tròn tím đặc, che kín vùng, đúng là dạng sprite sheet.
  - Bản mới nhẹ hơn và xanh đúng màu Wizard, nhưng kém "đặc" hơn trên tuyết.

Chưa xem / còn tồn:
- Ở Rootburrow, script chụp không tìm được chỗ trống (log: "no open spot"). Vùng rơi bị kẹp sát tường nên cột sáng đè lên chính hero, che một phần người.
- Ánh sáng mặt đất trên nền tối có thấy (vũng xanh dưới vòng nổ) nhưng bị vòng nổ lấn. Chưa chụp riêng để đo đường cong; đường cong mới chỉ kiểm bằng test.
- Chưa xem lúc chơi online (client replay) và chưa xem trên Android.

## Test và build

- `dotnet test Tests/dotnet/ZenoniaSkills.Tests`: 422 đạt, có thêm `LightSkillTimingTests`.
- `dotnet test Tests/dotnet/ZenoniaNet.Tests`: 422 đạt.
  - Dùng SDK 10 `C:\Users\korri\.dotnet\dotnet.exe`.
- Build từ bản sao `D:\temp\zu-copy-lightfx`: Succeeded, 0 lỗi.
  - Exe: `D:\temp\player-lightfx\Zenonia.exe`.
- Lệnh chụp:
  - Bản mới: `Zenonia.exe --record-choreo astral-cataclysm [--map rootburrow] --shot-path <dir> -screen-width 1280 -screen-height 720 -screen-fullscreen 0`.
  - Bản cũ: thêm `--light-fx off`.

## Câu hỏi chưa chốt

1. Giữ cột sáng giữa vùng không? Nếu vùng rơi sát hero thì cột sáng che hero. Có thể bỏ cột sáng, hoặc hạ độ đục khi gần caster.
2. Trên tuyết, sao băng còn nhạt. Có muốn đậm hơn không (đầu to hơn, lớp alpha đậm hơn), dù sẽ bớt kiểu "ánh sáng" RO3?
3. Có áp cùng bộ shader cho skill khác không (ví dụ cột sáng xanh lá + trắng cho Cleric)?
4. Bản cũ (choreography tím) có xoá không, hay giữ để so sánh?
5. Có cho phép agent thao tác JS trên chatgpt.com (Tan Pham) để vẽ 4 texture không? Hiện bị chặn quyền, xem mục Texture.
