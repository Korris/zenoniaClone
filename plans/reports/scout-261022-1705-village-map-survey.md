# Village Map Survey – Oakhaven Village (khảo sát, chưa sửa code)

Date: 2026-10-22 | Scope: đọc code + kiểm tra asset + đề xuất bố cục/kế hoạch. Không thay đổi engine.

## 1. Entry point & luồng render hiện tại

| Bước | File | Ghi chú |
|---|---|---|
| Boot | `src/main.js` | canvas 960x640, `PIXEL_SCALE=4` → view 240x160 world px = **15x10 tile** (TILE 16) |
| Load ảnh | `src/graphics/asset-loader.js` ← `ASSET_FILES` (`asset-atlas.js`, `asset-atlas-hd-props.js`) | `Promise.allSettled`: file thiếu chỉ warn, không crash |
| Cắt sprite | `src/graphics/sprite-registry.js` → `tile-sprite-builder.js`, `prop-sprite-builder.js` | HD (pixelsPerUnit=4) merge đè pixel-art theo tên |
| Map def | `src/world/maps/oakhaven-village-map.js` (36x26) qua helpers `map-builder.js` | rows = chuỗi ký tự/tile (`tile-char-info.js`), props = `{type,tx,ty}` |
| Load map | `src/world/map-manager.js::loadMap` | tạo `Tilemap`, add solid footprint cho prop + NPC, spawn, đặt hero |
| Render | `src/graphics/world-renderer.js` | tiles → drops → **y-sort** (prop/npc/monster/hero theo `y` = đáy footprint / chân) → fx → text |
| Tile | `tilemap-renderer.js` | kind: variants / flowers / block (3x3 autotile: path `d`, water `w`) / cliff `C` / animated `P` |
| Prop | `prop-renderer.js::drawProp` | vẽ canvas tại `(x - w/2, y - h)`; dest size = `PROP_DEFINITIONS` (world unit), HD bật smoothing |
| Collision | `src/world/tilemap.js` | solid = tile solid ∪ `blockedByProps` (footprint) ∪ ngoài map; `moveBox` quét từng px, tách trục X/Y |
| Hero box | `hero-stats-config.js` `HERO_HITBOX {w:10,h:8}` neo ở chân | `player-controller.js:18-21` |

Kết luận: engine đã hỗ trợ đầy đủ yêu cầu (tile riêng + prop riêng, collision theo footprint, depth sort theo vị trí). **Không cần sửa hệ thống nào**; chỉ cần dữ liệu map + prop mới.

## 2. Hiện trạng Oakhaven Village
- Đã tồn tại, là map khởi đầu (`FIRST_MAP_ID`), 3 nhà (house/house2/house3), sông + cầu phía Đông, farm rào kín (không có cổng), plaza = `rockSmallGrey` + vòng hoa thay giếng, 6 torch thay cột đèn, 2 stump thay đống củi. 3 NPC, 1 portal Nam → Verdant Meadow.
- Screenshot `tools/screenshots/15-village.png`: hợp lệ nhưng thưa, chưa có giếng, ruộng, bến thuyền, quầy hàng.
- Tests: `npm test` → 107/107 pass (baseline).
- Map file 106 dòng (<200). Minimap thiếu màu cho `fence`, `torch`, `stump`, `bush2`, `stones`, `signpost`, `crystal`, `cactus`, `bones`.

## 3. Kiểm tra asset (kích thước / alpha / sheet)

### HD props (`assets/hd/props/normalized/`, RGBA, alpha 0–255, đúng `def*4`)
25 loại: pine, pineDark, roundTree, bigTree, smallTree, deadTree, bush, bush2, stones, rockBrown, rockGrey, rockSmallGrey, rockSmallBrown, stump, palm, cactus, bones, house(64x48), house2, house3, fence(16x24), signpost, pillar, torch, crystal.
Raw sheets (RGBA, nền trong suốt, halo alpha<=48 được script lọc): houses 2172x724, village 2172x724, bushes_rocks 2172x724, small_trees 2172x724, big_rocks 1774x887, pine 1024x1536, round 1254x1254, desert 1448x1086.

### HD tiles (`assets/hd/tiles/normalized/`, RGBA alpha=255 trừ portal)
grass 4 variant + 4 flowers; path 3x3 block; water 3x3 block + 2 anim + bridge; floors: sand/castleFloor/caveFloor/lava; walls: cliff top/wall/bottom, castleWall, caveWall; portal 3 frame.

### Pixel pack (Ninja Adventure, 16px, alpha nhị phân)
`TilesetHouse.png` 528x368 có: barrel, crate, chum/chậu rau, mảnh mái bạt quầy hàng, bàn gỗ, hàng rào gỗ, bia đá. **Không có giếng có mái.** `TilesetNature.png`: cây/bụi/đá/hoa (đã dùng).
Lưu ý: mọi prop đang dùng đều là HD; chèn prop pixel-art 16px sẽ lệch phong cách rõ (crisp vs painted).

### Reference `assets/hd/map/village_reference.png` 1536x1024 RGB (không alpha; ảnh minh hoạ, không dùng làm background)
Thành phần: 3 nhà (đỏ NW, nâu N, đá xanh NE), giếng trung tâm + vòng đá lát, cột đèn lồng, cây tròn/thông, bụi, đá, hàng rào, ruộng rau + bù nhìn (SW), quầy hàng mái sọc (W), lán củi + lửa trại (S), sông + cầu đá (NE) + bến gỗ + thuyền (SE), bậc thang đá (NE).

### Asset CÒN THIẾU so với reference (không có cả HD lẫn pixel)
| Prop | Size world (w x h) | Footprint | Solid |
|---|---|---|---|
| well (giếng mái) | 32 x 48 | 2x2 | yes |
| lanternPost (cột đèn lồng) | 16 x 32 | 1x1 | yes |
| marketStall (quầy mái sọc) | 48 x 48 | 3x2 | yes |
| cropPlot (luống rau) | 32 x 16 | 2x1 | yes |
| scarecrow | 16 x 32 | 1x1 | yes |
| woodpile (lán củi) | 32 x 32 | 2x1 | yes |
| campfire | 16 x 16 | 1x1 | yes |
| barrel / crate | 16 x 16 | 1x1 | yes |
| rowboat | 32 x 16 | 2x1 | yes (đặt trên water) |

Bến gỗ: **không cần art** – dùng tile `B` (bridge, đi được trên nước) kéo ra sông. Vòng lát quanh giếng: tile `f` (castleFloor HD) hoặc `d`.

Pipeline thêm prop đã sẵn: thả raw sheet `assets/hd/props/village2.png` (RGBA, nền trong suốt, các vật cách nhau >=2 cột trống, trái→phải) → thêm `SHEETS`/`PROP_TARGETS`/`CELL_NOTES` trong `tools/build-hd-props.py` → `npm run hd:props` → thêm `PROP_DEFINITIONS`, `HD_PROP_ASSET_FILES`, `HD_PROPS`. `hd-props-config.test.js` tự kiểm tra kích thước.

## 4. Đề xuất bố cục (grid 40x28, viền cliff `C`)
```
cột:  0   4   8   12  16  20  24  28  32  36 39
r1   CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
r3   ..pine..  [house2 nâu x16-19]  ..pine..          www
r7   [house đỏ x3-6]  .. roundTree ..  [house3 đá x28-31] www
r10  ====== đường Bắc (y10-12) x4..31 ================ www
r13  [stall x3-5]  roundTree   PLAZA f x15-21   lantern  www
r15  ====== đường ngang (y15-17) x6..33 → cầu B x34-36 ====
r16                [WELL x18-19]  elderRowan(21,17)
r18  +fence farm x3-10 y18-23, cổng Đông (10,20-21)+  lantern
r20   crop crop crop  scarecrow | spine x17-19 | woodpile(22,21) campfire(24,22) barrel  bến B x34-37 y20-21
r22   crop crop crop            | start(18,22)   rockGrey                     rowboat(37,23) www
r24   hunterLena(15,23)         | portal P (18,25)   pine/bush rải               www
r27  CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
```
- Đường: `carvePath` width 3 (quirk fill extents đã ghi ở report `fullstack-261022-1540`).
- Sông `w` x34-36 y1..26; cầu `B` y15-17; bến `B` x34-37 y20-21 (cụt trong nước).
- NPC: elderRowan (21,17) cạnh giếng; villagerMia (20,8) trước nhà nâu; hunterLena (15,23) gần cổng Nam.
- Giữ 36x26 nếu muốn ít rủi ro; 40x28 để có chỗ cho sân rào + bến.

## 5. Kế hoạch triển khai
1. **Art**: tạo raw sheet mới trong `assets/hd/props/` (tên tuỳ chọn, vd `village-props-2.png`; 9 vật, nền trong suốt, cách nhau >=2 cột trống; style theo `houses.png`/`village.png` hiện có và `assets/hd/map/village_reference.png`) → cập nhật `build-hd-props.py` → `npm run hd:props` → xem contact-sheet.
2. **Config**: `prop-definitions.js` (bảng trên), `asset-atlas-hd-props.js` (files + HD_PROPS), `minimap-renderer.js` PROP_COLORS (thêm cả fence/torch còn thiếu).
3. **Map**: sửa trực tiếp `oakhaven-village-map.js` theo §4; nếu >200 dòng tách `oakhaven-village-layout-helpers.js` (farm/plaza/dock builders). Giữ id map, portal target, NPC id. Cập nhật `targetTx/Ty` trong `verdant-meadow-map.js` nếu đổi toạ độ.
4. **Verify**: `node --check`, `npm test` (map-invariants + hd-props-config tự phủ), `npm run check:browser` + screenshot thêm tại plaza/farm/bến (teleport qua `window.__zenonia.session.player`). Kiểm tra hero đi sau/trước giếng, nhà, cây; không kẹt ở cổng farm; portal reachable.
5. **Docs**: `docs/codebase-summary.md`, `docs/system-architecture.md`, `README.md` (prop list); changelog/roadmap.

## 6. Rủi ro
- Không có raw sheet thì giếng/quầy/ruộng không thể đúng style; fallback pixel-pack sẽ lệch style.
- `scatterProps` chạy sau placeProp: phải `reserve()` mọi ô trống chủ đích (cổng farm, bến, lối quanh giếng).
- Prop 2x2 footprint (well): NPC đứng sát giếng cần ô kề đi được (test reachability bắt lỗi này).

## Unresolved questions
1. Nguồn art cho 9 prop thiếu: user cung cấp raw sheet (như 8 sheet HD hiện có) hay generate bằng `ai-multimodal` (Imagen) với `houses.png` + `village_reference.png` làm tham chiếu style?
2. Giữ 36x26 hay mở rộng 40x28?
3. Vòng lát quanh giếng dùng `f` (castleFloor, cạnh cứng không autotile) hay chỉ `d` path?
4. Thoại elderRowan vẫn nói "Verdant Village" – có sửa text không?
