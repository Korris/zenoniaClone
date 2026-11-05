# Phase 07: Willowbrook tách prop riêng (nền chỉ đất + prop y-sort)

## Overview
Status: DONE (chưa xem trong game).
Nền `willowbrook-meadows-ground.png` = ảnh ground-only (x1.5). 45 prop `wb*` cắt từ ảnh gốc bằng `Tools/build-willowbrook-props.py build` (bảng box ở đầu file), lưu `Assets/Resources/Sprites/village/props/wb*.png`. Cạnh trái `wbPalisade` không có footprint; hàng rows chặn cọc rào, vách đá lớn, mép map, HUD, nước (w), cầu (B).
Ảnh cũ giữ ở `Assets/ArtLibrary/hd/map/willowbrook-meadows/old-painted-backdrop.png`.

## Todo
- [x] Nền từ ảnh ground
- [x] Cắt 45 prop, composite + diff + sheet (visuals/props-debug)
- [x] VillagePropDefs + PropList + ctor chặn footprint
- [x] Rows mới (đi được dưới tán cây), cổng tây/start giữ nguyên
- [x] `check` -> visuals/willowbrook-props-check.png, composite -> visuals/willowbrook-props-composite.png
- [x] MapSpawnTable giữ nguyên (vùng rộng, TryZoneSpot bỏ ô đặc)
- [x] dotnet test: Net 298, Skills 188 pass
