# Hiệu ứng ánh sáng kiểu RO3 cho Ice Storm (19/11/2026)

Branch: worktree `worktree-agent-a819ebcffc0ad67a3`, dựa trên `unity` (1bc7ffb0). Chưa push, chưa merge.

## Gắn vào skill nào

- **Wizard skill 2 "Ice Storm"** (người dùng chốt lúc 12:45: thay hình Ice Storm hiện tại).
- Ice Storm là skill cũ viết tay (`IceStormUltimate`):
  - vùng tròn bán kính 44 đơn vị, tâm cách hero 26 đơn vị theo hướng nhìn;
  - 4 đợt sát thương ở 650 / 1050 / 1450 / 1850 ms, mỗi đợt có 3 mũi rơi, tổng 12 mũi;
  - hình mờ hết ở 2700 ms.
- Chỉ đổi phần hình. Sát thương, làm chậm, âm thanh, rung màn hình, hit-stop, flash vẫn nằm trong `IceStormUltimate`. Không đổi số liệu skill, không cần build lại server.
  - Ba hằng thời gian (650 / 400 / 850 ms) chỉ được chuyển từ `IceStormUltimate` sang `Rpg/LegacySkillNumbers` để test dùng được. Giá trị không đổi.
- **Astral Cataclysm đã trả về hình cũ** (choreography tím). Hook vào `SkillExecutor` và dòng thêm vào `ProxySpawner` đã gỡ, hai file đó giống hệt `unity`.

## Kiến trúc

| File | Vai trò |
|---|---|
| `Assets/Resources/Shaders/LightBeamScroll.shader` | Tia sáng: trượt UV, uốn bằng nhiễu, dissolve; có chế độ chùm tia toả tròn |
| `Assets/Resources/Shaders/LightTrail.shader` | Đuôi sao băng cho `TrailRenderer` |
| `Assets/Resources/Shaders/LightBlob.shader` | Ánh sáng mặt đất (thay `Light2D`), đầu sao băng, vòng |
| `Assets/Scripts/IceStormLightFx.cs` | Điều phối Ice Storm: vòng sáng mặt đất, 12 sao băng, 12 vụ nổ, 4 nhịp sáng theo đợt |
| `Assets/Scripts/LightMeteor.cs` | "Prefab" sao băng: đầu sáng + `TrailRenderer` |
| `Assets/Scripts/LightImpact.cs` | Vụ nổ (ánh sáng đất, vòng, chùm tia, cột sáng ngắn), hoặc nhịp sáng cả vùng (`full = false`) |
| `Assets/Scripts/LightSkillAssets.cs` | Material, texture, mesh quad dùng chung |
| `Assets/Scripts/Rpg/Skills/LightSkillTiming.cs` | Thời gian, không phụ thuộc engine, có test |
| `Tools/build-light-skill-textures.py` | Sinh texture tạm / nhập ảnh vẽ / ảnh xem trước |
| `Tools/build-light-skill-capture-sheet.py` | Ghép chuỗi ảnh chụp trong game |

Cách nối vào game:
- `IceStormUltimate.Begin`: nếu `IceStormLightFx.Active` (cờ bật, có `RendersFx`, đủ shader) thì tạo `IceStormLightFx` làm con của object storm (huỷ cùng storm) và không dựng các lớp sprite cũ.
- `Strike` vẫn gây sát thương và giữ phần cảm giác (rung, flash, âm thanh), chỉ bỏ ShockRing / mảnh băng / bụi của bản cũ.
- Preload trong `GameSession` cạnh `IceStormUltimate.Preload`.

Hình theo thời gian:
- **0-650 ms:** vòng sáng mặt đất mở dần trên vùng skill:
  - chùm tia polar `LightBeamScroll` + vòng `burst-ring`;
  - nằm ở dải mặt đất, dưới hero và quái;
  - vành vòng trùng bán kính 44.
- **12 sao băng:**
  - rơi chéo từ trên trái, cùng hướng các mũi băng cũ;
  - rơi trong 320 ms;
  - chạm đất đúng đợt sát thương của mình (`LightSkillTiming.LandMs(i)` = 650 + 400 × (i / 3));
  - vị trí theo `IceStormUltimate.SpearImpact` (chung vòng xoắn với mũi băng cũ).
- **Mỗi lần chạm đất:** `LightImpact` gồm ánh sáng đất bán kính khoảng 20, vòng, chùm tia, cột sáng ngắn.
- **Mỗi đợt:** thêm một nhịp ánh sáng đất trên cả vùng.
- **Vòng mặt đất:** tan dần (dissolve) từ 2000 ms đến 2700 ms.

## Vì sao không dùng `Light2D` và Bloom chuẩn

- Project chạy **Built-in render pipeline**, không có URP. `Light2D` chỉ có trong URP 2D Renderer, và Post-Processing Bloom chuẩn cũng không có.
- Bloom: dùng `BloomEffect` + `BloomLite` có sẵn. `Bootstrap` tự gắn vào camera (`FeelConfig.BloomOn`), camera chạy HDR, ngưỡng 1,25.
  - Màu `[HDR] _EmissionColor` vượt ngưỡng thì phát sáng.
- Ánh sáng mặt đất là đèn giả: quad `LightBlob` tròn mềm ở `UltimateFx.GroundFxOrder + 2`, gồm:
  - lớp tint xanh alpha: thấy trên tuyết;
  - lớp cộng HDR: vũng sáng trên nền tối.
- Đường cường độ `LightSkillTiming.GroundLight`: từ 0 lên đỉnh trong 0,15 s, rồi giảm về 0 lúc vụ nổ kết thúc (0,56 s).
- Muốn `Light2D` thật (chiếu cả lên sprite) thì phải chuyển cả project sang URP 2D. Đây là việc lớn:
  - viết lại mọi shader `ZenoniaProto/*`;
  - chuyển material sprite sang Sprite-Lit;
  - thay `BloomEffect` (dùng `OnRenderImage`) bằng Volume Bloom.

## Tham số shader

`ZenoniaProto/LightBeamScroll` (quad `MeshRenderer`; không dùng `SpriteRenderer` vì nó ghi đè `_MainTex`):

| Tham số | Ý nghĩa | Giá trị |
|---|---|---|
| `_MainTex` | vân tia sáng xám, liền mạch dọc | `beam-streaks` |
| `_NoiseTex` | nhiễu mây xám, liền mạch | `noise-clouds` |
| `_ScrollSpeed` | tốc độ trượt tia (uv/s) | 1,6 cột / 2,4 chùm tia |
| `_NoiseScroll`, `_Distort` | nhiễu trôi, độ uốn tia | 0,45 / 0,12 |
| `[HDR] _EmissionColor` | màu cộng phát sáng | (0,1, 0,5, 1,35) |
| `_CoreWhite` | lõi trắng | 0,55 |
| `_BodyColor` | lớp alpha xanh bão hoà, để đọc được trên tuyết | (0,08, 0,38, 0,95, a 0,6) |
| `_Dissolve`, `_DissolveEdge`, `[HDR] _EdgeColor` | tan theo nhiễu, viền sáng chỗ đang tan | |
| `_Polar`, `_Ring`, `_RingWidth` | chế độ chùm tia toả tròn | |
| `_Fade` | mờ tổng (đặt riêng từng renderer bằng `MaterialPropertyBlock`) | |

- `LightTrail`: `_NoiseTex`, `_NoiseScroll`, `[HDR] _EmissionColor`, `_CoreWhite`, `_BodyColor`.
- `LightBlob`: `_MainTex`, `_Falloff`, `[HDR] _EmissionColor`, `_CoreWhite`, `_BodyColor`, `_Fade`.

Đuôi sao băng (`LightMeteor`):
- `TrailRenderer`, time 0,22 s, minVertexDistance 0,5, rộng 7, thon dần.
- Gradient: trắng → xanh băng → xanh, alpha 1 → 0.
- Sao băng bay thẳng, mỗi frame cập nhật nên đuôi mượt ở 60 FPS.
- Project không dùng prefab asset, nên "prefab" là template GameObject dựng bằng code rồi `Instantiate`.

## Bật / tắt để so sánh

- Mặc định bật bản mới.
- `--light-fx off` dùng lại sprite Ice Storm cũ.
- Khi đang chạy có thể đặt `IceStormLightFx.Enabled = false`.
- Thiếu shader thì tự về bản cũ.

## Texture: cả 4 vẫn là bản tạm (Python)

| Texture | Ghi chú |
|---|---|
| `beam-streaks` 128x256 | wrap Repeat |
| `noise-clouds` 256x256 | fBm + domain warp, wrap Repeat |
| `meteor-core` 128x128 | |
| `burst-ring` 256x256 | |

- Nguồn: `Assets/ArtLibrary/fx/light-skill/*-procedural.png`.
- Bản game: `Assets/Resources/Sprites/fx/light-skill/`, kèm `.meta`.
- Ảnh xem trước: `docs/reference/light-skill-fx/textures-preview.png`.
- Lượt này theo lệnh không dùng ChatGPT / Chrome.
  - Lần thử trước trên Tan Pham bị chặn quyền ("Unauthorized Access") khi dán hàm JS vào chatgpt.com. Chưa gửi prompt nào.
- Prompt đề xuất (nền đen phẳng, không trong suốt, không ô caro). Có ảnh rồi thì chạy `python Tools/build-light-skill-textures.py import <tên> <ảnh>`:
  1. `beam-streaks`: vệt tia sáng dọc trắng xám, liền mạch theo chiều dọc.
  2. `noise-clouds`: nhiễu mây xoáy xám, liền mạch cả 4 cạnh.
  3. `meteor-core`: lõi trắng nóng, quầng xanh băng mềm, 4 tia loé.
  4. `burst-ring`: vòng sáng trắng xanh ở khoảng 78 % bán kính.
- Lệnh `import` chưa có bước làm liền mạch.

## Ảnh chụp trong game (đã tự xem từng khung)

Ảnh ở `docs/reference/light-skill-fx/`. Mỗi tấm 12 khung: 0, 300, 550, 650, 800, 1050, 1200, 1450, 1600, 1850, 2100, 2500 ms sau cast.

| Ảnh | Nội dung |
|---|---|
| `ice-storm-snow-new-procedural.jpg` | bản mới, Frost Hollow |
| `ice-storm-snow-old.jpg` | bản cũ, Frost Hollow |
| `ice-storm-dark-new-procedural.jpg` | bản mới, Rootburrow, bãi đất trống |
| `ice-storm-dark-old.jpg` | bản cũ, Rootburrow |

- Lệnh chụp: `Zenonia.exe --record-choreo ice-storm [--map rootburrow] [--light-fx off] --shot-path <dir> -screen-width 1280 -screen-height 720 -screen-fullscreen 0`.
- Đã thêm Ice Storm vào danh sách `--record-choreo`.
- Tìm chỗ trống: thử khoảng trống 150 đơn vị, không có thì thử 90. Nhờ vậy Rootburrow ra bãi đất trống, không sát tường.

Đã thấy trên ảnh:
- **Tuyết:**
  - Vòng sáng xanh mở trên vùng từ 300 ms.
  - Sao băng chéo từ trên trái, có đuôi trắng xanh.
  - Vòng nổ xanh nhỏ ở chỗ chạm.
  - Hero vẫn thấy rõ trong vùng.
  - Vòng tan dần ở 2500 ms.
  - Không loá trắng.
  - Đầu sao băng nhạt trên tuyết, đuôi đọc được.
- **Nền tối:**
  - Vòng sáng xanh rõ, có bloom.
  - Sao băng có đầu trắng sáng và đuôi rõ.
  - Vòng nổ thấy ở mỗi điểm chạm.
  - Hero và quái không bị che.
- **So với bản cũ:**
  - Bản cũ: vòng phù văn đặc + mũi băng to vẽ tay, che hero nhiều hơn.
  - Bản mới: phẳng và "ánh sáng" hơn, vòng to bằng đúng bán kính sát thương (bản cũ vẽ vòng nhỏ hơn vùng thật), nhưng kém "đặc" trên tuyết.

Chưa xem / còn tồn:
- Đường cong ánh sáng đất mới kiểm bằng test, chưa đo trên ảnh.
- Chưa xem lúc chơi online (client replay) và trên Android.
- Hero ở Rootburrow mang ngoại hình khác (tóc nâu) ở cả bản cũ lẫn mới. Không do thay đổi này, chưa tìm hiểu.

## Test và build

- `dotnet test Tests/dotnet/ZenoniaSkills.Tests`: 422 đạt. `LightSkillTimingTests` kiểm tra:
  - sao băng chạm đất đúng 650 / 1050 / 1450 / 1850 ms;
  - đường cong ánh sáng đất.
- `dotnet test Tests/dotnet/ZenoniaNet.Tests`: 422 đạt.
- SDK 10: `C:\Users\korri\.dotnet\dotnet.exe`.
- Build từ `D:\temp\zu-copy-lightfx`: Succeeded, 0 lỗi. Exe: `D:\temp\player-lightfx\Zenonia.exe`.

## Câu hỏi chưa chốt

1. Trên tuyết đầu sao băng còn nhạt. Có làm đậm hơn không (đầu to hơn, lớp alpha đậm hơn)?
2. Vòng mặt đất bản mới to bằng vùng sát thương, to hơn vòng phù văn cũ. Giữ vậy hay thu nhỏ?
3. Có cho agent dùng ChatGPT (Tan Pham) để vẽ 4 texture không? Lần trước bị chặn quyền JS trên chatgpt.com.
4. Có áp bộ shader này cho skill khác không (ví dụ Cleric xanh lá + trắng)?
