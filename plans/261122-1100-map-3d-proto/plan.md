# Map 3D thật kiểu RO3 (prototype Willowbrook Meadows, 22/11/2026)

Nhánh `ro-soft-adventure`. Người dùng: "thử làm cho tui map 3D giống Ragnarok 3 luôn đi, hỏi ChatGPT".
Spec ChatGPT: `docs/reference/willowbrook-3d-proto/map-3d-ro3-spec-chatgpt.md`; 3 vòng chấm: `docs/reference/willowbrook-3d-proto/willowbrook-3d-ro3-review-chatgpt.md`.

## Trạng thái

| Bước | Trạng thái |
|---|---|
| Hỏi ChatGPT cách làm (terrain, cây leaf-card, đá, ánh sáng, sprite, camera, texture, ngân sách) | XONG |
| 7 texture ChatGPT (lá sồi, lá thông, vỏ cây, đá, sân lát, cụm cỏ) + dùng lại cỏ / đất 2.5D | XONG |
| Code prototype sau cờ `--map3d` | XONG |
| 3 vòng chấm: 6,9 -> 7,73 -> 8,0 / 10 | XONG (đạt ngưỡng 8) |
| Benchmark 40 người thật, LOD mobile | CHƯA |

## Cách chạy

`D:\temp\player-map3d\Zenonia.exe --map3d` rồi vào Willowbrook Meadows (hoặc thêm `--map willowbrookMeadows`). Không có `--map3d` thì mọi thứ như cũ (map 2.5D). Bản phát hành bỏ qua cờ.
Chuột phải / giữa kéo = xoay 360° + nghiêng 38-70°, `,` / `.` xoay, PageUp / PageDown nghiêng, Home về mặc định (pitch 52°). Thử nhanh: `--cam-yaw 180 --cam-pitch 52`, `--map3d-test noshadow|lowshadow|noleaves|noborder|nograss` (đo FPS).

## Kiến trúc (chỉ phần hình; rows, ô chặn, server, toạ độ không đổi)

- `Assets/Scripts/Map3D/Map3D.cs`: cờ, `IsMap3D`, `Build` (gọi từ `WorldBuilder` thay ảnh nền), `Finish` (gộp mesh sau `MapScenery`).
- `Map3DHeightField`: lưới đỉnh 0,5 m, map + 30 m viền; ô đi được = 0 m, bờ ô `T` 0,3-1,2 m, ngoài map đồi tới ~3 m, lòng suối / ao -0,55 m theo kênh nước của nền 2.5D, hành lang phẳng ra ngoài ở cổng mép; texture điều khiển (đất, rừng, sân lát, nước) + texture độ cao cho nước.
- `Map3DTerrain` (chunk 32 m, hạ 1,5 cm dưới z = 0 để decal / vòng / FX mặt đất không z-fight), `Map3DWater` (độ sâu từng pixel), `Map3DLighting` (mặt trời 48° cố định theo thế giới, bóng mềm 2 cascade 2048, fog linear, trả lại cài đặt khi rời map).
- `Map3DTreeBuilder` (sồi, sồi non, sồi cổ thụ, bạch dương, liễu, thông, 3 loại bụi; 4 biến thể), `Map3DRockBuilder` (đá icosphere, cụm đá, khúc gỗ, gốc cây), `Map3DRuinBuilder` (cột, tường), `Map3DGrass` (~1300 cụm), `Map3DBorderForest` (rừng ngoài mép), `Map3DProps` (bảng prop -> mesh theo TÊN; còn lại giữ sprite cutout nhận / đổ bóng), `Map3DWoodBuilder` + `Map3DShedBuilder` (22/11: cầu ván + lan can ở cao độ 0, nhà kho, rào, biển, rào chắn, đống rơm, thùng; atlas gỗ ChatGPT `Resources/Map3D/wood.png`, shader `Map3DPainted` _Mode 2; sậy = card chéo), `Map3DBatcher` (gộp theo vật liệu x ô 16 m), `Map3DAssets`, `Map3DMeshKit`.
- Shader `Assets/Resources/Shaders/Map3D*.shader` + `Map3DCommon.cginc` (ramp mềm 3 bậc, dither làm mờ lá / thân che hero, gió).
- Tool: `Tools/build-map-3d-splat.py` (splat từ nền 2.5D), `Tools/build-map-3d-textures.py` (tách nền #FF00FF, xếp atlas 4x4, làm liền mạch).
- Sửa nhỏ code cũ: `WorldBuilder`, `MapScenery`, `GroundShadowRenderer` (bỏ lớp bóng prop nướng sẵn trên map 3D), `View25DGroundApron` (tắt trên map 3D), `View25DProfile.Map3DFeel` (pitch 52, FOV 32), `View25DCameraControl` (yaw 360 trên map 3D).

## Công sức mở rộng ra mọi map (ước lượng, 1 agent + ChatGPT)

| Việc | Công |
|---|---|
| Map cỏ / rừng như Willowbrook (Bramblewood, Sunbreak, Oakhaven ngoài làng): splat + bảng tên prop -> mesh | 0,5-1 ngày / map |
| Map có vách vẽ trong nền (Frost Hollow, Scorpion Gorge, Sunbreak): vách 3D từ rows (mesh bậc / triplanar), cầu thang 3D | 2-3 ngày / map, cần generator vách trước (~3 ngày) |
| Biome mới (tuyết, sa mạc, đầm, hang): atlas lá / đá / nền mới từ ChatGPT, thông số cây | 1-2 ngày / biome |
| Làng / nhà (Thatchwick, Oakhaven): nhà hiện là sprite; 3D thật cần generator nhà hoặc giữ sprite cutout | 3-5 ngày cho bộ nhà cơ bản |
| Dungeon tối (Rootburrow, Frostfang Den): đèn điểm thật thay DungeonLights, vách hang 3D | 3-4 ngày |
| LOD + mobile (card lá LOD1, cỏ theo khoảng cách, bóng tán decal, preset chất lượng) | 3-5 ngày |
| Benchmark 40 người thật + tối ưu | 2-3 ngày |

Tổng cho ~16 map: khoảng 6-9 tuần, chưa tính QA mobile.

## Rủi ro

- Hiệu năng: 3D tốn gấp ~3-4 lần thời gian khung hình so với 2.5D trên GPU tích hợp (54-64 so với ~200 FPS); bóng là phần đắt nhất. Mobile chưa đo.
- Hero 2D trong cảnh 3D: ChatGPT vẫn chấm 7,6 (hơi giống sticker); xoay 360° làm sprite 8 hướng đổi frame theo yaw.
- Che khuất: cây gần camera rất to khi xoay; đang dùng dither quanh hero, tên quái vẫn vẽ xuyên tán.
- Bảo trì 2 bản hình cho cùng một map nếu giữ cả 2.5D và 3D.
- `dotnet test` không chạy được trên máy này (SDK 9, project net10); phần 3D không đụng logic dùng chung.

## Bước tiếp theo (nếu người dùng muốn tiếp)

1. Người dùng chạy thử `--map3d`, chọn tiếp hay dừng.
2. Benchmark 40 người (server cục bộ + bot) trên PC và một máy Android tầm trung.
3. LOD mobile, rồi mới thử map thứ hai (Bramblewood, cùng biome).
