---
title: "Willowbrook Meadows thành bãi săn lớn 72x48"
description: "Mở Willowbrook từ 36x24 lên 72x48 ô, 70 quái chia 11 cụm, nền texture-splat 2x2 chunk, ~180 prop y-sort"
status: pending
priority: P2
effort: 20h (+10h phase 8 tuỳ chọn)
branch: unity
tags: [map, art, engine, spawn, perf]
created: 2026-11-05
---

# Willowbrook Meadows 72x48

Willowbrook 36x24 hiện tại (nền ground-only + 45 prop `wb*`) thành **section tây** (cột 0-35, hàng 12-35) của map 72x48. Cổng tây về Thatchwick giữ, đổi sang hàng 23-24. Mép đông là ngõ cụt có rào chắn (sau này làm lối ra).

## Quyết định chính
- **Engine**: kích thước map lấy theo từng map (`IGameMap.Width/Height`), `GameConfig.TilesW/TilesH/MapW/MapH` thành property đọc `MapRegistry.Current`. Không còn hằng 36x24.
- **Nền**: phương án (a) texture-splat bằng python, 64 px/ô như các map khác -> 4608x3072, cắt **2x2 chunk 2304x1536** (đúng cỡ backdrop hiện có, Android vẫn 2048 ASTC). Ảnh nền 36x24 cũ dán nguyên vào section tây, làm mềm mép 3 ô. Không chọn (b) 4 tranh ChatGPT ghép: lệch màu/ánh sáng, mép không khớp.
- **Minimap**: map chunk dùng ảnh `-minimap` vẽ sẵn (nền + prop + bóng), vẽ bằng script.
- **Quái**: 70 con / 11 cụm, hồi sinh 6-10 s, chỉ dùng frostSlime/iceGoblin/spectralWolf. Quái đồng cỏ mới là phase 8 tuỳ chọn.
- **Kênh (channel)**: ngoài phạm vi. Server vẫn 1 zone/map, tối đa 200 người.
- Willowbrook và protocol 14 **chưa commit/chưa phát hành** -> không cần migrate save. Vị trí save cũ lệch thì `RectBlocked` + điểm vào cổng tự xử lý (`RpgSession.cs:111`, `ServerHeroFactory.cs:36`).

## Các phase
| # | Phase | Effort | Status | File |
|---|---|---|---|---|
| 1 | Engine: kích thước theo map, nền chunk, minimap, giải phóng texture | 3h | pending | [phase-01](phase-01-engine-per-map-size-and-ground-chunks.md) |
| 2 | Thiết kế layout 72x48, rows, cụm quái | 2h | pending | [phase-02](phase-02-layout-design-rows-and-clusters.md) |
| 3 | Art: texture nền, stamp, prop sheet, script splat + cắt prop | 7h | pending | [phase-03](phase-03-ground-splat-and-prop-art.md) |
| 4 | Code map: rows, ~180 prop, cổng, chunk | 3h | pending | [phase-04](phase-04-map-code-props-portals.md) |
| 5 | Bảng quái 11 cụm | 1h | pending | [phase-05](phase-05-spawn-table-clusters.md) |
| 6 | Unit test | 1h | pending | [phase-06](phase-06-unit-tests.md) |
| 7 | Kiểm tra: build bản sao, ảnh chụp, minimap, tải 80 quái | 3h | pending | [phase-07](phase-07-verify-build-captures-perf.md) |
| 8 | (Tuỳ chọn) quái đồng cỏ Lv3-6 | 10h | pending | [phase-08](phase-08-optional-meadow-monsters.md) |

## Phụ thuộc
**Mọi phase chỉ bắt đầu sau khi agent đang tách prop Willowbrook 36x24 xong và commit** (đang sửa `WillowbrookMeadowsMap.cs`, `VillagePropDefs.cs`, `MapSpawnTable.cs`, `willowbrook-meadows-ground.png`, `wb*.png`, `Tools/build-willowbrook-props.py`).
Sau đó: (1 song song 2) -> 3 -> 4 -> 5 -> 6 -> 7 -> 8. Bước bóng cây trên nền của phase 3 chạy lại cuối phase 4 (cần PropList).

## Sở hữu file (không chồng nhau)
- P1: `GameConfig.cs`, `IGameMap.cs`, `TileMapBase.cs`, `FrostHollowMap.cs`, `WorldBuilder.cs`, `Minimap.cs`, `SpriteSheet.cs`, `World/Zone.cs`, `Maps/MapPortals.cs`, `DebugCapture.NetNav.cs`, `Tests/.../MapGraphStubs.cs`, `MapGraphTests.cs` (chỉ đổi W/H), `MapSizeTests.cs` (mới)
- P2: chỉ file trong plan (`layout/`)
- P3: `Tools/build-willowbrook-large-ground.py`, `Tools/cut-willowbrook-prop-sheet.py` (mới), `ArtLibrary/hd/map/willowbrook-meadows/large/`, ảnh `tiles/willowbrook-meadows-ground-r*c*.png`, `-minimap.png`, prop `wb2*.png`
- P4: `WillowbrookMeadowsMap.cs`, `WillowbrookMeadowsProps.cs` (mới), `VillagePropDefs.cs`, `ThatchVillageMap.cs`, (`NetOpcodes.cs` nếu cần)
- P5: `MapSpawnTable.cs`. P6: `WillowbrookLargeMapTests.cs` (mới). P7: `Tools/net-load-test.ps1`, `docs/pending-tasks.md`, (`ResourceTextureImportSettings.cs` có điều kiện)

## Rủi ro chính (chi tiết trong phase)
- Bộ nhớ texture: 4 chunk RGBA32 = 56 MB trên Windows/WebGL; `SpriteSheet` giữ mọi texture mãi -> P1 giải phóng nền map cũ; P7 đo, cần thì nén DXT cho WebGL.
- Sót chỗ còn giả định 36x24 -> lỗi va chạm/portal lệch hàng. Giảm: đổi hằng thành property để compiler chỉ ra hết chỗ dùng `const`; test map 72x48 giả.
- Đường nối style giữa nền cũ và nền splat; lặp texture. Giảm: lấy mẫu texture từ chính ảnh cũ, noise màu tần số thấp, rắc hoa lấy từ ảnh cũ, xem ảnh từng góc.
- Băng thông snapshot với 80 quái: AOI 5x5 ô lưới (320x320 đơn vị) không đổi, quái rải 11 cụm -> mỗi client thấy ~10-20. Đo `snapshotsOverCap`.

## Rollback
Mỗi phase 1 commit. P1 tự đứng được (các map 36x24 chạy như cũ). Revert P4 = Willowbrook về 36x24 (ảnh chunk thừa vô hại). P5 revert = bảng quái cũ.

## Câu hỏi còn mở
Cuối phase-07 và phase-08.
