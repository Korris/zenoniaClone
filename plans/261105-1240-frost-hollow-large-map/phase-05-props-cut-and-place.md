# Phase 05: Prop - cắt khu cũ, prop sheet mới, bảng đặt

## Context
- Đọc trước `docs/sprite-rules.md` (quy tắc tự xem ảnh, Sổ lỗi) dù là prop map.
- Mẫu: `Tools/build-willowbrook-props.py` (cắt prop từ ảnh gốc theo box tay, alpha theo mode tree/rock/struct, so với ảnh chỉ-đất, pad để pivot dưới-giữa footprint; lệnh `check` overlay footprint), `Tools/cut-willowbrook-extra-props.py` (prop sheet #FF00FF -> `wbx*`, dùng `build-meadow-monsters.key_magenta`), bảng `Maps/WillowbrookMeadows{,Frame,Scatter}Props.cs`, defs `Maps/VillagePropDefs.cs` (93 dòng; Den đã có `icePillar, frozenStalagmite, frostCrystal, frozenBones` dòng 42-43 - dùng lại được ở khu mới).
- Frost Hollow hiện **không có prop** (cây vẽ trong nền; `FrostHollowLayout` không override `Props`).

## Overview
Priority P1. Cần A0 (khu cũ) + A8 (khu mới).

## Script 1: `Tools/build-frost-hollow-props.py` (mới, theo mẫu build-willowbrook-props.py; import helper chung bằng importlib, không chép)
- ORIG = `ArtLibrary/hd/map/frost-hollow/frost-hollow-ground-original.png` **2304x1536** (cắt ở độ phân giải gốc, nét hơn bản 1536), GROUND = A0 phóng x1.5; mask từ diff ORIG vs GROUND + mode (tree: tán tối; rock/crystal: khác màu; struct: hàng rào/xương mảnh).
- Bảng box tay ~35-50 prop cũ: thông (lẻ, cụm), cây khô, đá, pha lê, bia rune (2 bia ở ~(3,5) và (13,9)), bia gần cầu, xương, hàng rào. Tên `fh<Name>` (vd `fhPineNW`, `fhCrystalW`, `fhRuneStone1`).
- Prop bị mép ảnh cũ cắt phẳng (hàng 0, cột 35, hàng 23) -> thay bằng `fhx*` cùng loại (như Willowbrook thay 13 prop).
- Ra `Assets/Resources/Sprites/village/props/fh*.png` + snippet PropDef + bảng `P(...)` khu cũ; `check` overlay footprint + rows.

## Script 2: `Tools/cut-frost-hollow-extra-props.py` (mới, theo cut-willowbrook-extra-props.py)
- A8 -> `fhx*` (~20-25 món: `fhxPineBig/Mid/Slim/Clump`, `fhxDeadTree`, `fhxBush`, `fhxRock*`, `fhxCrystal{S,M,L}`, `fhxRune`, `fhxBones`, `fhxFence`, `fhxLog`, `fhxTowerWall`, `fhxTowerPillar`), SCALE để thông ~4-6 ô cao như prop cũ (so `fh*` cạnh nhau).

## Bảng đặt (C#, `Maps/FrostHollowProps.cs` partial + `FrostHollowFrameProps.cs` + `FrostHollowScatterProps.cs`, mỗi file < 200 dòng)
- OldSection: prop cắt từ tranh, đúng ô gốc (toạ độ không dịch vì offset 0,0).
- Frame: rừng viền mới (phủ hết ô T viền; ô T không có tán cây phủ = lỗi nhìn thấy), dải thông chia khu, tường/cột tháp canh, pha lê lớn quanh hang Den, đá bờ hồ.
- Scatter: bụi, đá, xương, gỗ đổ rải đồng tuyết (tránh rect cụm quái quá dày, chừa hành lang >= 3 ô).
- Cấm đặt footprint: ô boss (7,5) + vòng 2 ô, start (4,21), cổng (2,21), (71,30-32), (69,31), cầu cũ hàng 11-12 cột 15-23, ô capture (11,6).
- `VillagePropDefs.cs`: thêm ~60 dòng def -> có thể vượt 200 dòng: tách `FrostHollowPropDefs` (partial hoặc bảng riêng gộp trong `TryGet`).
- Chạy lại script 1 P4 vẽ prop thu nhỏ vào minimap (đọc bảng, như Willowbrook composite).

## Todo
- [ ] script 1 + fh* + check  - [ ] script 2 + fhx*  - [ ] 3 bảng prop  - [ ] PropDefs (tách file nếu > 200 dòng)  - [ ] minimap có prop  - [ ] composite preview (`preview-willowbrook-large-composite.py` hoá tham số hoặc bản FH) + tự xem

## Success criteria
Composite nền + prop khu cũ giống tranh gốc (diff thấp, xem 1:1 4 góc); mọi ô T viền có tán phủ; không footprint ở ô cấm; prop mới cùng tông prop cũ khi đặt cạnh.

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Mask cây trên nền tuyết trắng khó tách (tuyết trên tán) | Cao x TB | diff với A0 thay vì màu; xem sheet kiểm tra; sửa box tay |
| A0 lệch tranh gốc -> prop cắt không khớp chỗ | TB x TB | cắt từ ORIG nhưng đặt theo ô; kiểm composite |
| Quá nhiều instance prop (draw call) | Thấp x TB | đo ở P8; cây cụm thay cây lẻ ở viền |

## Next
P6 nối bảng prop + chặn footprint.
