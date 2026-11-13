# Sunbreak Foothills vào game + đổi tuyến World Map 2.1 (13/11/2026)

Chưa commit. Build thử: `C:\temp\player-sunbreak\Zenonia.exe` (từ bản sao, có khoá `C:/temp/zu-copy.lock`, đã nhả).

## Đã làm

- **Map** `sunbreakFoothills` / `SunbreakFoothillsMap`, 48x32, Hostile, nền 2x2 mảnh 1536x1024 (`Sprites/village/tiles/sunbreak-foothills-ground-r{r}c{c}.png`) + `-minimap.png` 288x192 (đã bake prop). Nhạc tạm dùng `frost-hollow`.
- **Ghép 4 góc** (`Tools/build-sunbreak-foothills-painted.py`): 4 góc không có dải trùng nên không cắt min-cut được; dùng kéo lệch cục bộ `WARPS` (suối dưới cầu tây +78 px; bờ suối ở mép dọc r1c0/r1c1 +40 px), san màu hai bên mép, trộn gương ±14 px. `PATCHES` có sẵn nhưng không cần dùng.
- **Ô chặn**: `build-sunbreak-foothills-rows.py` + `plans/261113-1010-world-map-new-maps/layout-sunbreak/sunbreak-foothills-rows-override.txt`. Script tự kiểm: flood fill, suối / vách sân boss / vách hang chỉ qua được ở cầu và bậc. 3 hàng đáy chặn, cổng nam ở hàng 28. Đường ra mép TÂY: cột 0 chặn + 2 đống đá (ngõ cụt).
- **Prop**: 20 món `sb*` (`cut-sunbreak-foothills-props.py`), 67 chỗ đặt (`SunbreakFoothillsProps.cs`), def ở `SunbreakFoothillsPropDefs.cs`. Thông bên tây, sa thạch bên đông; bụi / cỏ / sọ / khúc gỗ / đá suối đi xuyên.
- **Cổng (7)**: nam (20-22,28) → Frost Hollow (68,22); hang (23-24,3) → Frostfang Den (2,10); đông (47,15-16) → Sunspire (2,10-11).
- **Đổi tuyến**: hang băng Frost Hollow (67-69,20) → Sunbreak (21,26). Cổng tây Sunspire → Sunbreak (45,15-16). Frostfang Den: cửa tây → Sunbreak (23,6).
  - **Lựa chọn cho cửa đông của Den (sau boss, trước đây sang Sunspire): giữ cổng, cho về cùng chỗ (23,6) ở Sunbreak.** Lý do: bỏ thì đánh xong boss phải đi ngược cả hang; để sang Sunspire thì Den vẫn là đường tắt bỏ qua Sunbreak.
- **Quái**: 50 con / 8 bãi (ibex 13 Lv16 phía nam, lizard 12 Lv17, harpy 13 Lv18 phía bắc, guardian 12 Lv20 phía đông), boss `ridgehornBehemoth` ở (38,4) trên sân cao, `HasOwnBoss` = true (lưu theo map như các boss map khác). Mỗi bãi ≥ 4x ô trống, cách điểm đến / cổng ≥ 5 ô.
- **Test**: `SunbreakFoothillsMapTests.cs` (mới, 6 test), sửa `FrostHollowLargeMapTests.VillageGateAndIceCavePortalsRoundTrip`, `MapGraphTests.DenForwardPortalIsAlwaysOpenNow`, thêm `WorldMap21Route...`. ZenoniaNet 387/387, ZenoniaSkills 297/297.

## Lỗi gặp và đã sửa

- **Sunbreak → Frost Hollow đưa hero về ô (0,0)** ở bản build đầu, `dotnet test` vẫn qua. Nguyên nhân: hai class map đọc `static readonly Vector2Int` của nhau trong bảng `MapPortal` (vòng khởi tạo static, Mono đọc ra 0). Sửa: toạ độ điểm đến thành `const int` (`SunbreakFoothillsMap.SouthArrivalTx...`, `FrostHollowLayout.DenArrivalTx/Ty`), thêm test rà nguồn, ghi vào `docs/map-design-rules.md`. Build lại, log xác nhận hero ở (1096,402) = ô (68,22).

## File dùng chung đã chạm (chỉ dòng của mình)

`MapRegistry.cs` (thêm map), `MapSpawnTable.cs` (khối Sunbreak + 4 case), `VillagePropDefs.cs` (nối `SunbreakFoothillsPropDefs.TryGet`, ngoài danh sách được giao nhưng bắt buộc để prop có def), dòng cổng của `FrostHollowLayout.cs` / `FrostfangDenMap.cs` / `SunspireOasisMap.cs`, `docs/map-design-rules.md` (2 gạch đầu dòng), 1 hàng trong `docs/world-map-2-art-progress.md`.

## Đã xem (tự mở ảnh)

- Ghép: ảnh tổng + crop 1:1 quanh cả hai mép. Ảnh ô chặn đỏ + prop cả map và 4 góc 1:1.
- Trong game (`--map --tp --solids --shots`): 3 điểm đến; 7 lượt qua cổng (toạ độ hero trong log khớp cả 7) và map bên kia; 2 cầu, bậc sân boss, bậc hang, ngõ cụt tây (có tô đỏ); 7 vùng quái có quái, boss + thanh máu; 5 chỗ mép nối giữa các mảnh (không thấy đường nối).
- Ảnh: `docs/reference/sunbreak-foothills-{stitched-overview,solids-overlay,props-composite}.png`, `sunbreak-foothills-ingame-{portals-both-sides,monster-zones-and-boss,chunk-seams,solids-arrivals-bridges-stairs}.png`.

## CHƯA kiểm

- Chưa đi bộ thật qua cầu / bậc (chỉ teleport + ảnh tô đỏ + flood fill trong test).
- Chưa chạy online: zone server chưa build lại, chưa thử cổng qua server (bảng map + quái đổi → phải build lại server, phát hành cùng client).
- Chưa đánh boss, chưa xem đòn bắn của Cliff Harpy, chưa thử Android / WebGL.
- Bãi 8 (guardian phía đông) không có ảnh riêng, chỉ thấy ở rìa ảnh bãi 7 và trên minimap.
- Unity của người dùng chưa import 25 PNG mới (chưa có `.meta` trong repo; Unity tự sinh khi mở).

## Câu hỏi còn mở

- Cửa đông của Den về Sunbreak: giữ hay bỏ hẳn?
- Hai mặt cầu vẽ chéo nhưng ô đi được là 4 ô bậc thang (2x2 lệch nhau): đủ dùng hay cần va chạm chéo?
- `ThatchVillageMap` vẫn đọc `FrostHollowLayout.PlayerStartTile.x` trong `MapPortal` (một chiều, đang chạy đúng); có đổi sang const cho chắc không? (file của agent khác, chưa chạm)
- Tên `DenArrivalTile` / `DenPortalTx` ở Frost Hollow giờ là cổng sang Sunbreak; chưa đổi tên để khỏi đụng test / agent khác.
