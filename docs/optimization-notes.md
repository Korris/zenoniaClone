# Ghi chú tối ưu hiệu năng (FPS)

Sổ tay mọi việc tối ưu FPS đã làm, cách chỉnh và số đo. Người dùng (22/11): "note lại các cái bạn đã làm để sau này tui hỏi lại còn biết".
Cập nhật mỗi lần có việc tối ưu mới (thêm mục, thêm dòng vào bảng số).

## 0. Tóm tắt nhanh

- Game nghẽn ở **tô pixel (fill-bound)**, không phải CPU hay draw call: map 3D ở 640x360 chạy gấp 3 lần 1280x720. Đắt nhất: lá cây (nhiều lớp card alpha-test chồng nhau) và bóng realtime.
- Đã làm: bóng FakeSun nửa độ phân giải, Graphics Low / Medium / High, bóng nướng 1 lần (Medium), depth pre-pass cho lá, terrain vẽ sau lá, bớt vật đổ bóng, LOD FX theo đám đông, giới hạn số damage, **render scale, dynamic resolution, impostor cây xa (22/11 tối)**.
- Chỉnh trong game: Settings > **Graphics** (Low / Medium / High), **Resolution** (Auto / 100 / 85 / 70 %), **Dynamic resolution** (bật / tắt). F12 = bảng FPS (có % render scale).
- Cờ test: `--gfx low|medium|high`, `--render-scale 0.85`, `--dynres off|on`, `--fps-log`, `--map3d`, `--view25d off`, `--map3d-test useimpostor|noshadow|nobake|noprepass|noleaves|nograss|noborder`, `--impostor-dist M`, `--impostor-dump`.

## 1. Các việc đã làm

### 1.1 Bóng FakeSun (map 2D / 2.5D) — `Assets/Scripts/Shadows/GroundShadowRenderer.cs`
- Mọi bóng dưới đất (blob nhân vật / quái + bóng prop nướng sẵn) vẽ vào **một mask R8 nửa độ phân giải màn hình** (`MaskResolution = 0.5`), trộn bằng **BlendOp Max** (chồng nhau giữ bóng đậm nhất, không cộng dồn), rồi **một quad** nhân màu đất theo mask (giới hạn `FakeSun.AlphaCap`).
- Chi phí: dựng lại ~4 đỉnh / blob trên CPU, 2-3 lần vẽ vào mask nhỏ, 1 quad toàn màn. Không particle, không đèn.
- Chỉnh: `MaskResolution` (0,5 = nửa màn hình; nhỏ hơn thì rẻ hơn nhưng mép bóng nhoè hơn), `Margin`. Tắt: `--shadows off` (về bóng elip cũ).

### 1.2 Chi phí của 2.5D (so với 2D)
- Đo 20/11 (40 hero, `plans/reports/proto-25d-hydra-261120.md`): 2D ~2,5-3,3 ms, 2.5D ~4,1-4,5 ms (+1,2-1,6 ms / khung).
- Nguyên nhân chính: bóng nắng nhân vật (`View25DDropShadow` chép mọi sprite của từng nhân vật mỗi khung), pass không khí toàn màn (`View25DAtmosphere`: sương xa, vignette), camera phối cảnh vẽ nhiều đất hơn, MSAA 2x.
- Đã gỡ: Medium / Low tắt MSAA (+~19 % trên 2.5D, hình gần như không đổi vì sprite không có cạnh polygon cứng). Còn có thể: cập nhật bóng nhân vật 2-3 khung / lần, hoặc chỉ hero mình + boss.
- So 2.5D với 2D: `--view25d off` (map 2.5D vẽ phẳng như cũ).

### 1.3 Graphics Low / Medium / High (22/11) — `GraphicsQuality.cs`, Settings hàng "Graphics"
| Mức | Map 3D | Mọi map |
|---|---|---|
| High | bóng realtime 2 cascade 2048 (bản đã duyệt), mọi prop + terrain đổ bóng, cỏ 100 % | MSAA của project (2x) |
| Medium (mặc định PC) | **không** bóng realtime; bóng mặt trời của prop tĩnh **nướng 1 lần** khi vào map (`Map3DBakedShadow`, RFloat 4096, shader đọc `M3DBakedSun`, 4 mẫu / pixel), cỏ ~70 % | MSAA tắt |
| Low (mặc định mobile) | không bóng mặt trời (dùng bóng silhouette 2.5D), cỏ ~40 % | MSAA tắt, bỏ ép anisotropic |
- Lưu PlayerPrefs `zenonia.gfx.quality`; đổi là áp dụng ngay (không vào lại map). Mesh / sprite mới trên map 3D phải qua `Map3DBatcher.Add` / `Register` để theo mức.
- Bóng nướng mất: lá lay trong bóng, nhân vật không đổ bóng (ở High cũng không: dùng blob).

### 1.4 Lá: depth pre-pass + terrain vẽ sau lá (22/11) — `Map3DFoliage.shader`, `Map3DTerrain.shader`
- Pass `LEAFDEPTH` chỉ ghi depth (có cắt alpha + dither quanh hero), pass màu chạy `ZTest Equal` không clip: mỗi pixel lá chỉ tô màu 1 lần thay vì mỗi lớp card. +7 %.
- Terrain đổi queue sang AlphaTest+20 (vẽ sau lá): đất bị tán che không tô nữa.
- Đổi vị trí đỉnh lá thì sửa `M3DLeafWorld` / `M3DLeafClipLod` dùng chung (2 pass phải ra depth y hệt). Tắt để so: `--map3d-test noprepass`.

### 1.5 Bớt vật đổ bóng (shadow-caster pruning) (22/11) — `Map3DMeshKit.CastsShadow`, `Map3DBatcher`
- Card lá chia 2 submesh: chỉ 6/16 ô atlas đổ bóng (bóng tán lốm đốm), 10/16 ô không đổ bóng nằm ở renderer bỏ hẳn pass ShadowCaster (không vào shadow map, không vào depth pre-pass của bóng screen-space).
- Rừng viền ngoài map: chỉ cây trong 6 m đầu đổ bóng. Cỏ không đổ bóng.

### 1.6 LOD FX skill theo đám đông — `ClericLightFxLod.cs`, `ClericLightFx.DimFor`
- 40 người đánh boss: FX của mình (hoặc trúng mình) đủ 100 %; của người cùng party: hình chính 90 %, quầng sáng 50 %, hạt 50 %; người khác: hình chính 55 %, quầng 15 %, bỏ lớp chi tiết, hạt 20 %, không vệt. Cảnh báo boss không bao giờ bị làm mờ. `DimFor` trả 1 / 0,7 / 0,35.

### 1.7 Giới hạn số damage — `DamageNumber.cs`, `DamageNumber.Stream.cs`
- Pool cố định 256 số (không cấp phát mỗi đòn), vẽ IMGUI. Mỗi quái tối đa 3 số đang hiện (`MaxVisible`, số cũ bị đẩy ra mờ trong 80 ms), tối đa 3 làn cạnh nhau, quái thứ 4 cùng chỗ dùng chung làn.

### 1.7b Số sát thương: pool + vẽ gộp (22/11 tối) — `DamageNumber.cs`, `DamageNumberBatch.cs`, `Shaders/DamageNumberQuad.shader`
- Pool: mảng 256 struct cấp 1 lần (prewarm), vòng tròn đè số cũ nhất khi đầy (trần số đang sống); không GameObject, không Instantiate / Destroy mỗi hit (từ trước, nay ghi rõ).
- Vẽ gộp (mới): trước đây mỗi glyph / hạt / vòng là 1 lệnh IMGUI `DrawTexture` = 1 draw call (~360 lệnh khi 90 số đang sống). Nay mọi quad gom theo (lớp, atlas) rồi vẽ bằng GL 1 lần mỗi nhóm trong lượt Repaint: thường 3-8 draw call cho mọi số. Viền nằm sẵn trong atlas nên không tốn thêm (không như `RoTextOutline` 8 bản chữ). Thiếu shader thì tự về IMGUI.
- Chưa đo FPS riêng (cần cảnh đông số: `--dummies 10 --dummy-grid` + skill nhiều hit).

### 1.8 Render scale (22/11 tối) — `RenderScale.cs`, `RenderScaleCamera.cs`, `RenderScaleBlit.cs`, `Shaders/RenderScaleUpscale.shader`
- Camera thế giới vẽ vào texture nhỏ hơn màn hình rồi phóng lên bằng bilinear + làm nét nhẹ (unsharp 4 điểm, `_Sharpen` 0 ở 100 %, ~0,1 ở 85 %, ~0,25 ở 60 %). Đúng cho 2D, 2.5D và `--map3d`.
- **HUD (canvas overlay) và chữ IMGUI (tên, số damage, bảng FPS) vẫn vẽ ở độ phân giải gốc.** Nhãn quái dưới chân (sprite thế giới) thuộc thế giới nên mờ theo scale.
- Cách làm (built-in pipeline): cuối LateUpdate (execution order 30000) gán `camera.targetTexture` = texture nhỏ; camera thứ hai `RenderScaleBlit` (không vẽ gì) trả target về màn hình ở pre-cull của nó rồi chép texture lên màn hình trong `OnRenderImage`. Ngoài lúc vẽ, camera vẫn là cỡ màn hình nên click chuột / tên / damage không lệch. Bloom và pass không khí 2.5D chạy ở cỡ nhỏ (rẻ thêm).
- Lỗi đã gặp: gán `targetTexture` trong `OnPreCull` **không có tác dụng** (Unity đã chọn target trước đó) -> màn hình đen. Phải gán trước khi camera bắt đầu vẽ.
- Settings > **Resolution**: Auto (theo Graphics: High 100 %, Medium 85 %, Low 75 %) / 100 / 85 / 70 %. Đây là **trần**; dynamic resolution không vượt quá. PlayerPrefs `zenonia.gfx.renderScale` (0 = Auto, 1..3).
- Chỉnh: `RenderScale.ForQuality` (giá trị Auto), `RenderScale.Steps`, độ nét trong `RenderScaleCamera.Swap`. Test: `--render-scale 0.85` (cố định trần cho lần chạy, không lưu).

### 1.9 Dynamic resolution (22/11 tối) — `RenderScaleDynamic.cs`
- Mỗi 0,5 s so FPS với mục tiêu (`RenderScale.TargetFps`: **55 PC**, **30 mobile**): 2 cửa sổ chậm liên tiếp (1 s) -> giảm 5 % (giảm 10 % nếu dưới 70 % mục tiêu), sàn **60 %** (`RenderScale.Floor`); dư sức (>= mục tiêu x 1,08) giữ 3 s -> tăng 5 %, không quá trần.
- Chống nhấp nháy: vừa tăng mà < 4 s đã phải giảm lại thì thời gian chờ trước lần tăng sau gấp đôi (3 -> 60 s); 30 s không giảm thì về 3 s. Khung > 0,25 s (load map, alt-tab) bỏ qua.
- Settings > **Dynamic resolution** (mặc định bật), PlayerPrefs `zenonia.gfx.dynamicRes`. Test: `--dynres off|on`.
- Bảng FPS (F12) hiện `... ms  85%` và chữ `auto` khi dynamic đang giữ dưới trần.
- Lưu ý: vsync 60 Hz thì FPS tối đa ~60 nên "dư sức" = chạm trần vsync; `--fps-log` tắt vsync nên dynamic luôn ở trần (đo số cố định thì thêm `--dynres off`).

### 1.10 Impostor cây xa trên map 3D (22/11 tối) — `Map3D/Map3DImpostors.cs`, `Map3DImpostorBaker.cs`, `Shaders/Map3DImpostor.shader`, `Map3DImpostorBake.shader`
- Khi vào map: mỗi loại cây / bụi đang dùng (35 loại x biến thể ở Willowbrook) được nướng 8 góc nhìn (mỗi 45°) vào atlas albedo 3584x3024 (khung 224 px cây, 112 px bụi, có mipmap) + atlas normal / độ sâu nửa cỡ. ~15 ms. Nướng ở góc ngẩng camera trừ FOV/4 (~44°), nướng lại khi camera nghiêng > 4°.
- Mỗi cây (934 cây ở Willowbrook) có thêm 1 billboard hướng camera trong mesh gộp. Cây có gốc xa hơn **hero + StartAhead** (High 6 m, Medium 3 m, Low 0 m; trễ 0,5 m) thì đổi sang impostor: chuyển trong 0,35 s bằng dither bù nhau (mesh giữ ô dither > fade, impostor giữ ô <= fade), xong thì mesh cây **bị gập trong vertex shader** (không tô pixel nào).
- Lỗi đã gặp: lần đầu dùng dải khoảng cách cố định để trộn -> cây đứng yên trong dải bị dither lỗ chỗ mãi. Sửa: mỗi cây tự chuyển hẳn theo thời gian (0,35 s), có trễ.
- Dữ liệu: gốc cây ghi vào mọi đỉnh của mesh gộp qua lightmap offset của `CombineMeshes` (uv2 = gốc x, y; uv3 = gốc z, số thứ tự cây + 1); fade từng cây trong texture R8 `_M3DImpFade` (CPU cập nhật). Log `Map3D tree-root uv OK` khi vào map.
- Màu / ánh sáng: impostor dùng đúng công thức lá (`M3DShade` wrap 0,35, ánh qua lá, viền), xoay normal theo góc cây, bóng nướng lấy mẫu tại vị trí bề mặt thật (độ sâu trong atlas) ở Medium và **High** (`_M3DBakedImp`; ở High bóng nướng chạy thêm chỉ cho impostor), sương như mesh.
- **Bóng lá không đổi**: shadow map của mặt trời (ortho) và bản nướng vẫn vẽ đủ mọi cây mesh; chỉ depth texture camera (perspective) theo LOD (`M3DImpFadeCaster`). Impostor không đổ bóng.
- Giới hạn: impostor hơi mờ / phẳng hơn mesh ở cùng cỡ (khung 224 px); không đặt gần hơn hero được vì phối cảnh nhìn cây gần từ góc quá khác (thử `--impostor-dist -8`: FPS gần bằng tắt lá nhưng cây trước mặt sai hình). Atlas ~70 MB VRAM (PC ổn; mobile nên giảm khung / số góc / nén).
- **TẮT mặc định từ 22/11 22:45** (người dùng: xoay camera thì cây "thoát ẩn thoát hiện"). Lý do: mốc đổi tính theo khoảng cách **camera** tới cây, xoay camera quanh hero thì một bên cây vượt mốc → đổi mesh ↔ billboard liên tục; lợi FPS lại nằm trong sai số. Bật lại để thử: `--map3d-test useimpostor`. Muốn dùng lại phải đổi mốc theo khoảng cách tới hero (không đổi khi xoay) và chỉ áp cho cây ngoài vùng nhìn thấy gần.
- Chỉnh: `Map3DImpostors.StartAhead`, `SwapSeconds`, `Hysteresis`, `RebakeDeg`; `Map3DImpostorBaker.TreePx / BushPx / Frames`. Test: `--map3d-test noimpostor`, `--impostor-dist M` (mét sau hero), `--impostor-dump` (ghi `impostor-atlas.png` ở persistentDataPath).

### 1.11 Nhãn quái (thanh máu + tên) chỉ hiện khi đánh (22/11) — `MonsterLabelPrefs.cs`, `Monster.Visuals.cs`, `Monster.RoFootLabel.cs`
- Người dùng: "cái ống máu và tên quái không hiện trước mặt lính nữa mà khi nào ấn vào mới hiện thì có tăng FPS được không". Mặc định nhãn dưới chân quái thường chỉ hiện cho quái đang nhắm (`TargetMarker.Shown`) và quái mình vừa đánh trúng trong 2 s. Settings > **All monster names / HP** (mặc định tắt) bật lại kiểu luôn hiện. Boss / Wanted không đổi.
- Mỗi nhãn = 11 renderer (khung + thanh máu sliced, 1 chữ + 8 bản chữ viền `RoTextOutline`), TextMesh / sprite sliced không gộp draw call. Nhãn dựng lúc hiện lần đầu: quái chưa từng hiện không có renderer nào.
- **Số đo** (2.5D Willowbrook, cầu 27,21, Medium, render 100 %, `--dynres off --no-hud`, 140 quái trên map = 70 sẵn + 70 dummy lưới 10x7 trong khung hình `--dummies 70 --dummy-grid`, chỉ 1 game mở, 3 lần xen kẽ, `D:\temp\perf-lvlup\run.sh`):

| Chế độ (`--monster-labels`) | FPS từng lần | Trung vị | ms / khung |
|---|---|---|---|
| `all` (như trước 22/11) | 86 (lần đầu máy bận) / 186 / 200 | **186** | 5,4 |
| `all-nooutline` (bỏ 8 bản viền) | 211 / 194 / 196 | 196 | 5,1 |
| `focus` (mặc định mới, không đánh con nào) | 320 / 283 / 311 | **311** | 3,2 |

- **+68 % FPS (5,4 -> 3,2 ms)** khi đứng giữa đám đông không đánh. Lúc đánh chỉ thêm 1-vài nhãn.
- Viền 8 bản chữ (`RoTextOutline`) chỉ ~5 % (~0,3 ms); phần đắt là chính số renderer / draw call của 140 nhãn. `MonsterFootLabelCrowd` (sắp xếp + chiếu ra màn hình + kiểm chồng) tốn 0,36-0,48 ms CPU / khung ở 140 nhãn (log `LabelCrowd` khi `--fps-log`), 0 khi không có nhãn. Nếu sau này cần "luôn hiện" rẻ hơn: gộp nhãn vào 1 mesh (mục 4 "nameplate batching"), viền bằng shader thay 8 bản.
- Ảnh: `docs/reference/levelup-light-fx/labels-all-70.jpg`, `labels-focus-70.jpg`, `labels-only-hit.jpg`.

### 1.12 Cổng dịch chuyển 3D (22/11 tối) — `Map3D/Map3DPortals.cs`, `Map3DPortalMesh.cs`, `Shaders/Map3DPortalStone.shader`, `Map3DPortalGlow.shader`
- Mỗi cổng trên map 3D: ~700 đỉnh đá (1 renderer, không gộp batch vì cần pass ForwardAdd), vortex + cột sáng + vòng rune đất (3 draw trong suốt, 1 shader), 1 particle system tối đa 24 hạt, 1 đèn point ForcePixel không bóng. Shader Map3D khác không có ForwardAdd nên đèn chỉ tốn thêm 1 pass cho mesh đá của chính cổng (ánh sáng trên đất là vòng glow cộng thêm, không phải đèn). Willowbrook 2 cổng ~ 10-12 draw khi cả 2 trong khung hình; chưa đo FPS riêng (ước < 0,1 ms).

## 2. Số đo FPS

Máy: AMD Radeon(TM) Graphics tích hợp (D3D11), cửa sổ 1280x720, `--fps-log` (tắt vsync), warrior, 70 quái, 1 hero, `--no-hud`, `--dynres off`. **Chỉ 1 game mở** (số buổi chiều trong `docs/reference/willowbrook-3d-proto/perf/fps-numbers.md` bị một cửa sổ game khác tranh GPU). Dao động giữa các lần chạy rất lớn (GPU tích hợp đổi xung, lúc đo có agent khác build cùng máy), nên ghi **trung vị / lần tốt nhất**; chênh dưới ~15 % coi như bằng nhau.

### 2.1 Map 3D Willowbrook (`--map3d`)

"Trước" = exe commit 3b7e94d3 (2 lần). "Sau, tắt" = exe mới với `--render-scale 1 --map3d-test noimpostor` (3 lần).

| Chỗ (tile, yaw) | Mức | Trước | Sau, tắt | + impostor | + impostor + render 85 % |
|---|---|---|---|---|---|
| Cổng tây (3,23), 0 | Medium | 187 / 204 | 91 / 129 (*) | 109 / 170 (*) | 105 / 148 (*) |
| Cầu (27,21), 0 | Medium | 176 / 208 | 213 / 222 | 187 / 229 | 224 / 237 |
| Ao (38,36), 300 | Medium | 149 / 172 | 170 / 172 | 152 / 181 | 207 / 209 |
| Cổng tây (3,23), 0 | High | 77 / 89 | 42 / 75 (*) | 70 / 81 (*) | 75 / 80 (*) |
| Cầu (27,21), 0 | High | 73 / 98 | 97 / 97 | 75 / 78 | 91 / 94 |
| Ao (38,36), 300 | High | 63 / 67 | 70 / 70 | 69 / 69 | 79 / 80 |

(*) chỗ chạy đầu tiên của lượt đo, có lần chỉ 28-36 FPS (máy đang bận) -> bỏ qua dòng này khi so.

Đọc bảng:
- **Render scale 85 %**: map 3D +5-20 % (ao Medium 172 -> 209, High 70 -> 80). Ít hơn mức "pixel ít đi 28 %" vì phần cố định (CPU, bóng, hậu kỳ) không giảm theo.
- **Impostor**: trong sai số ở khoảng cách hiện tại. A/B sạch cùng chỗ cổng tây, Medium: tắt 173, impostor bắt đầu ngay ở hero 198 (+15 %), bắt đầu 8 m trước hero 235 (+36 %, nhưng cây gần sai hình nên không dùng), tắt hẳn lá 254. Phần đắt là lá ở cây GẦN camera, impostor không thay được.
- Medium hiện 150-230 FPS ở 1280x720; High 70-100.

### 2.2 Map 2.5D (không `--map3d`), chỉ render scale

| Chỗ | Mức | Trước | Sau 100 % | Sau 85 % |
|---|---|---|---|---|
| Cổng tây | Medium | 313 / 343 | 243 / 350 | 359 / 361 |
| Cổng tây | High | 248 / 276 | 307 / 309 | 274 / 306 |
| Cầu | Medium | 318 / 348 | 220 / 301 | 252 / 270 |
| Cầu | High | 223 / 293 | 278 / 296 | 174 / 247 |

- 2.5D chạy ~250-350 FPS, không nghẽn ở tô pixel nên render scale **không lợi rõ** (trong sai số). Dynamic resolution chỉ hạ scale khi FPS < 55, nên trên 2.5D thực tế giữ ở trần.

### 2.3 Số buổi chiều (có 1 game khác tranh GPU)
Bản 3D cũ 27,7 -> High 36-44, Medium 67-80, Low 97-111 (tương đối đúng, tuyệt đối chỉ ~1/2).

## 3. Cờ và công tắc

| Cờ / phím | Tác dụng |
|---|---|
| Settings > Graphics | Low / Medium / High (lưu theo máy) |
| Settings > Resolution | Auto / 100 / 85 / 70 % (trần render scale) |
| Settings > Dynamic resolution | bật / tắt (mặc định bật) |
| F12 | bảng FPS + ms + % render scale (bản test) |
| `--gfx low\|medium\|high` | mức đồ hoạ cho lần chạy (không lưu) |
| `--render-scale 0.85` | trần render scale cho lần chạy |
| `--dynres off\|on` | dynamic resolution cho lần chạy |
| `--fps-log` | tắt vsync, ghi FPS trung bình mỗi 5 s vào log |
| `--map3d` | bật map 3D (Willowbrook) |
| `--view25d off` | map 2.5D vẽ phẳng kiểu 2D (so sánh) |
| `--map3d-test ...` | tắt từng phần để đo: `noimpostor`, `noshadow`, `lowshadow`, `nobake`, `noprepass`, `noleaves`, `nograss`, `noborder` |
| `--impostor-dist M` / `--impostor-dump` | khoảng đổi impostor (mét sau hero) / ghi atlas |
| `--shadows off` | bóng FakeSun về elip cũ |
| Settings > All monster names / HP | nhãn mọi quái (tắt = chỉ quái đang đánh, mặc định) |
| `--monster-labels all\|focus\|all-nooutline` | chế độ nhãn quái cho lần chạy (không lưu) |
| `--dummies N --dummy-grid` | thêm N quái lưới 10 cột quanh hero (đo đám đông trong khung hình) |

Đo lại: script `D:\temp\perf4\ab.sh <thư mục exe> <số lần> <tx,ty> <yaw> "tên|cờ" ...` (chạy xen kẽ, tự tắt Zenonia.exe còn sót trước mỗi lần, in trung vị). Đóng mọi `Zenonia.exe` khác và đừng đo khi agent khác đang build.

## 4. Ý tưởng chưa làm

- **Lightmap / bake ánh sáng tĩnh cho terrain và prop**: bóng + AO nướng vào texture thay cho bóng nướng 4 mẫu / pixel; rẻ hơn nữa và có AO.
- **Ít card lá hơn** (LOD1 cho cây gần vừa: card to hơn / ít hơn): lá cây gần camera vẫn là phần đắt nhất, impostor không đụng tới.
- **Giảm tần suất animation đám đông**: nhân vật / quái xa camera cập nhật sprite 2-3 khung một lần; bóng nhân vật 2.5D (`View25DDropShadow`) cũng vậy.
- **Gộp vẽ bảng tên (nameplate batching)**: tên / thanh máu IMGUI vẽ từng cái; chuyển sang một mesh / canvas gộp khi đông 40 người.
- **Giới hạn FPS (FPS cap)** trong Settings (30 / 60 / không giới hạn) để máy yếu / điện thoại đỡ nóng; hiện PC dùng vsync, mobile 60.
- Impostor: khung lớn hơn / 2 góc ngẩng để nét hơn; nén atlas (DXT) cho mobile.
- Benchmark 40 người thật trên map 3D (bot `ZenoniaSwarm` cần SDK net10) và đo trên điện thoại.
