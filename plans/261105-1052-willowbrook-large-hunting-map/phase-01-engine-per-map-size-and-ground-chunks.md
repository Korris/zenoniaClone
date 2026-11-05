# Phase 01: Engine - kích thước theo map, nền chunk, minimap, giải phóng texture

## Context
- Mọi map hiện cố định 36x24: `TileMapBase.cs:11` `public const int W = 36, H = 24;`, `GameConfig.cs:13-16` (TilesW/TilesH/MapW/MapH là `const`), `IGameMap.cs:30` ("Every map is 36x24").
- Phải xong trước P4. Chạy song song P2.

## Overview
Priority P1 (chặn mọi thứ). Status pending. Cho mỗi map có kích thước riêng, nền nhiều chunk, minimap vẽ sẵn. Các map 36x24 cũ chạy y như trước.

## Kết quả khảo sát (chỗ giả định 36x24)
| Chỗ | Dòng | Việc |
|---|---|---|
| `TileMapBase` | 11, 22-23, 39, 49, 54, 61 | W/H thành field theo `rows.Length`/`rows[0].Length`; `RectBlocked` dùng `W*Tile` thay `GameConfig.MapW`; kiểm mọi hàng cùng độ dài (LogError) |
| `IGameMap` | 30-50 | thêm `int Width`, `int Height`, `int GroundChunkCols`, `int GroundChunkRows` (TileMapBase: virtual 1/1) |
| `GameConfig` | 13-16 | `TilesW/TilesH/MapW/MapH` -> static property đọc `MapRegistry.Current`; thêm `const MaxTilesW = 72, MaxTilesH = 48` |
| `FrostHollowMap` | 12, 35-38 | bỏ const TilesW/TilesH (hoặc property); `TileFeet` dùng `GameConfig.MapH` động - đúng trên server vì `Zone.Enter` đổi `MapRegistry.Current` (`MapRegistry.cs:50`), `ZoneTransferService.cs:63-66` gọi `host.Wake(to)` trước `TileFeet` |
| `CameraFollow` | 71-72 | không sửa (đọc property, tự đúng) |
| `World/Zone.cs` | 29 | `InterestGrid` tạo theo `MaxTilesW*16 x MaxTilesH*16` (18x12 ô lưới); Query đã clamp (`InterestGrid.cs:68-73`) |
| `Minimap` | 17, 20, 211 | `InnerW/InnerH` cố định 198x132 (3:2, giữ cỡ UI), bake cố định 432x288; ưu tiên load `GroundTexture + "-minimap"` nếu có; map chunk không có ảnh minimap -> màu `MissingGround` + LogWarning |
| `WorldBuilder` | 35-49, 61 | nền: nếu chunk >1 load `<GroundTexture>-r{row}c{col}` (row 0 = trên), mỗi chunk 1 SpriteRenderer, ppu = `tex.width / (MapW/cols)`, đặt tại `(col*MapW/cols, MapH-(row+1)*MapH/rows)`; overlay F4 dùng W/H của map |
| `SpriteSheet` | 9-17 | cache `Textures` không bao giờ xoá -> thêm `Unload(path)` (bỏ khỏi dict + `Resources.UnloadAsset`); `WorldBuilder.Rebuild` gọi cho nền/minimap của map trước |
| `Maps/MapPortals.cs` | 11 | `TileMapBase.H` -> `GameConfig.TilesH` (map hiện tại) |
| `DebugCapture.NetNav.cs` | 13-46 | `const TW/TH` + mảng cố định -> cấp phát theo map lúc dựng BFS |
| `DebugCapture.Choreo.cs` 94, `FrostSmokeTest.cs` 90 | | đọc property, chỉ cần biên dịch lại |
| `Tests/.../MapGraphStubs.cs` | 38, 43 | stub `GameConfig.MapW/MapH` -> property theo `MapRegistry.Current` |
| `MapGraphTests.cs` | 37, 103-116 | flood fill dùng `map.Width/Height` |
| Không cần sửa | | `NetQuantize.cs:8-10` (MaxPos 4095 đơn vị > 1152), `InterestGrid.cs:83` SeeRadius 2 (cửa sổ 320x320 đơn vị ~ camera 240x160), `Monster.Ai` (lái thẳng, không lưới tìm đường), `PortalTile.OfFeet` (nhận số hàng làm tham số) |

## Key insights
- Đổi `const` -> property làm mọi chỗ dùng trong biểu thức const (Minimap, NetNav) báo lỗi biên dịch: tự liệt kê hết chỗ sót.
- `RectBlocked`/`Move` là đường nóng (bước 1 đơn vị): đọc W/H field của chính instance, không gọi qua `MapRegistry`.
- Hàng dưới HUD: camera kẹp ở mép dưới map (`CameraFollow.cs:72`) -> map lớn vẫn phải chặn 3 hàng cuối (45-47). Không cần chặn hàng nào khác.

## Data flow
rows (C#) -> TileMapBase(W,H,solid[]) -> GameConfig.* (Current) -> Camera clamp / TileFeet / Minimap / InterestGrid(max). GroundTexture + chunks -> WorldBuilder -> N SpriteRenderer; `-minimap` -> Minimap RawImage.

## Implementation steps
1. TileMapBase: field `W,H` từ rows, kiểm độ dài, `RectBlocked` dùng field; `CharAt/IsSolidTile/BlockTiles` dùng field.
2. IGameMap thêm 4 thuộc tính; TileMapBase triển khai (chunk mặc định 1x1).
3. GameConfig property + `MaxTilesW/H`; sửa FrostHollowMap const.
4. Zone grid theo Max; MapPortals; DebugCapture.NetNav cấp phát động.
5. WorldBuilder chunk + overlay; SpriteSheet.Unload + gọi khi Rebuild.
6. Minimap: kích thước UI cố định, `-minimap` override, fallback.
7. Test stubs + MapGraphTests; thêm `MapSizeTests.cs`: map giả 72x48 (rows trong test) -> `RectBlocked` mép, `TileFeet` <-> `PortalTile.OfFeet` khứ hồi ở hàng 0/47, cột 71.
8. `dotnet test` cả hai bộ; biên dịch Unity (đọc Editor.log lần mới nhất, không chạy batch trên project đang mở).

## Todo
- [x] TileMapBase / IGameMap / GameConfig / FrostHollowMap
- [x] Zone grid, MapPortals, NetNav
- [x] WorldBuilder chunk + overlay, SpriteSheet.Unload
- [x] Minimap
- [x] Stubs, MapGraphTests, MapSizeTests
- [x] Test qua (Unity biên dịch: lead build bản sao)

## Success criteria
- `grep -rn "TileMapBase\.\(W\|H\)\b"` = 0 kết quả ngoài TileMapBase.
- Hai bộ dotnet test qua (Net hiện 298, Skills 188) + MapSizeTests mới qua.
- Build bản sao: Oakhaven, Frost Hollow, Willowbrook (vẫn 36x24) giống hệt trước (chụp so sánh 1 ảnh mỗi map, minimap đúng).

## Risk
| Rủi ro | Khả năng x Tác động | Giảm |
|---|---|---|
| Chỗ dùng size của map khác map đang chạy (server) | Thấp x Cao | rà mọi caller `TileFeet`/`TryFind` trên server; Zone.Enter trước khi tính |
| Hiệu năng property trong vòng lặp | Thấp x Thấp | đường nóng dùng field instance |
| Seam 1 px giữa chunk (bilinear + Clamp) | Trung bình x Thấp | chunk cắt từ cùng 1 ảnh liền; nếu thấy vạch thì cắt chồng 2 px và đặt lệch |

## Security
Không có. Server vẫn kiểm vị trí bằng `RectBlocked` của map zone.

## Rollback
Revert 1 commit; chưa map nào dùng chunk nên không ảnh hưởng dữ liệu.

## Next
P4 dùng chunk + size 72x48.
