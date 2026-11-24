# Phase 04: Props / địa hình camp

## Context Links
- **Đọc hết `docs/map-design-rules.md`** (đặc biệt mục "Map 2.5D" điều 2-7, 12, 18, 24 map 3D) và `docs/sprite-rules.md` mục art ChatGPT.
- `Assets/Scripts/Maps/WillowbrookMeadows*Props.cs`, `BramblewoodForestProps.cs`, `VillagePropDefs.cs`, `Shadows/StaticPropShadows.cs` (`Classify`), `Map3D/Map3DProps.cs` (`Classify`), `Tools/place-map-25d-forest.py`, `Tools/build-map-25d-ground.py`, `Tools/build-map-3d-splat.py`, `Tools/cut-magenta-prop-sheet.py`.
- Chat map Tay: Willowbrook 2.5D forest https://chatgpt.com/c/6b00f11a-1148-83ec-bd9e-0d255d3f0211 ; map 3D https://chatgpt.com/c/6b029c3a-4a48-83ec-affb-a9e9bf71b449 ; Bramblewood ground https://chatgpt.com/c/6aff4715-5740-83ec-b3c1-98168f0cf042

## Overview
P2. Mỗi camp khác nhau về hình ảnh, không chỉ Lv. Rows / ô chặn KHÔNG đổi nếu được (server khỏi đổi ô); prop chặn chỉ đặt trên ô đã chặn hoặc ven bãi.

## Art cần ChatGPT vẽ (tấm prop nền #FF00FF, cùng cỡ cây cũ, vẽ cho camera 58°)
| Camp | Prop | Ghi chú |
|---|---|---|
| Đồng Cỏ Ba Lá | cụm cỏ ba lá, hoa nhỏ | đồ phẳng `FootH` 0, ≤ 3 ô, vẽ dưới hero |
| Đồi Thỏ | hang đất, gò đất thấp, bụi cây | hang = chặn, đặt ven bãi |
| Rừng Nấm | nấm to (2-3 loại), khúc gỗ rêu, gốc cây, nấm nhỏ | tên có `shroom` -> thêm từ khoá vào 2 `Classify`; mảng nền tối hơn (mask ground, không vẽ lại cả map) |
| Bờ Suối | lau sậy, đá suối | đã có `wbReed*`, `wbRockStream*`, cầu `wbBridge`: tái dùng, chỉ thêm nếu ChatGPT đòi |
| Rừng Gai (Bramblewood) | bụi gai mâm xôi, đá | Bramblewood chưa 2.5D: prop 2D theo map đó |
| Gốc Cây Cổ (Bramblewood) | cây cổ thụ lớn, rễ di tích, đá rune quanh cửa hang | điểm nhấn map; cửa hang có sẵn |
≈ 3-4 tấm prop + 1 texture mảng nền rừng nấm (nếu cần) ≈ 5 ảnh.

## Implementation Steps
1. Theo production sheet chọn prop; brief ChatGPT gửi kèm ảnh map hiện tại + cây mẫu cùng cỡ; ChatGPT tự quyết hình.
2. Cắt `Tools/cut-magenta-prop-sheet.py` (neo giữa đáy), thêm vào `VillagePropDefs` + bảng props map; đặt bằng tay / `place-map-25d-forest.py` (cách rect bãi / đường ≥ 1,5-2,5 ô, cổng ≥ 5 ô).
3. 3D: prop đứng không có mesh -> cutout sprite; kiểm `--map3d --cam-yaw 0/90/180/270`. Mảng nền rừng nấm: thêm lớp / mask trong `build-map-3d-splat.py` cấu hình willowbrook và `build-map-25d-ground.py`.
4. Rà ô `T` lẻ (điều 7), flood fill (điều 6), `preview-*-composite.py` không in `PROBLEM`.
5. Đổi ô chặn (nếu có) -> ghi file override `plans/<map>/layout/`, build lại server.

## Todo
- [ ] Danh sách prop chốt
- [ ] Vẽ 3-5 ảnh, cắt, cài
- [ ] Đặt prop 2 map, ảnh tô ô chặn
- [ ] Kiểm 2.5D + 3D 4 yaw, bóng FakeSun

## Success Criteria
Ảnh ghép có prop + ảnh tô ô chặn đỏ cả map: đường / cầu / bãi trống, mọi ô đi được nối cổng; chụp trong game từng camp (`--map willowbrook-meadows --tp x,y --solids --shots`) thấy rõ khác nhau; quái đọc được trên nền (bible VI).

## Risk
- Prop chồng rect bãi -> quái kẹt: preview in `PROBLEM`.
- Nền rừng nấm tối làm FX / quái khó đọc: giữ chênh sáng ≤ 10-12 %.
- Prop vẽ phối cảnh (hang, rễ lớn) méo khi xoay 3D (Sổ lỗi 22/11): ưu tiên vật dạng đứng; nếu méo thì mesh theo `Map3DProps` hoặc bỏ khỏi 3D.
