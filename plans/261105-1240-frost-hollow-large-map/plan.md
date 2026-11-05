---
title: "Frost Hollow lên 72x48"
description: "Mở Frost Hollow 36x24 thành bãi săn 72x48: khu cũ giữ góc tây bắc, nền outpaint pixel-art 2x2 chunk, cây/đá thành prop, ~57 quái mới, cổng Den ra mép đông"
status: pending
priority: P2
effort: 24h
branch: unity
tags: [map, art, engine, spawn, save-migration]
created: 2026-11-05
---

# Frost Hollow 72x48

Dùng lại pipeline Willowbrook 72x48 (`5ff855aa`, `fd0f0f21`, `c60a43ce`). Khu 36x24 cũ = cột 0-35, hàng 0-23 (offset 0,0): mọi toạ độ ô cũ giữ nguyên (zone cũ, King (7,5), start (4,21), cổng làng (2,21), capture `--tp`, bot). Mở hàng 22-23 và cột 34-35 cũ theo tranh. Cổng Den chuyển từ (33,2) sang **mép đông (71,30-32)**, miệng hang băng; Den đến (2,10) không đổi.

## Quyết định chính
- **Toạ độ thế giới đổi** dù toạ độ ô giữ: y-up, `TileFeet` dùng `MapH` (`FrostHollowMap.cs:39-42`) -> ô cũ cao thêm 384 đơn vị. Save cũ (x,y thế giới) phải dịch: `SaveVersion` 2 -> 3, Parse cộng 384 vào y cho save Frost Hollow v<3 (phase 2). Không làm -> hero hiện ở hàng 45-47 hoặc giữa khu Lv9.
- `FrostHollowMap.Zones` giữ **đúng 5 zone cũ** (bot NetNav, HeadlessBench, FrostSmokeTest dùng thẳng). Cụm mới ở mảng riêng `FrostHollowOuter` trong `MapSpawnTable`, `ZonesFor("frostHollow")` = cũ + mới.
- Nền: 7 mảnh outpaint tỉ lệ 1 (không dùng dải dọc 0.75 như Willowbrook: pixel-art phóng 1.33 lần sẽ to pixel hơn phần còn lại) -> canvas nguồn 3072x2048 -> x1.5 -> 4608x3072 -> 4 chunk `Sprites/tiles/frost-hollow-large-ground-r{r}c{c}` + `-minimap`. Chunk dùng được ở `Sprites/tiles/` (`WorldBuilder.cs:49` ghép tên, import theo `/tiles/` ở `ResourceTextureImportSettings.cs:47`).
- Quái mới (pending-tasks): iceWraith Lv7, snowYeti Lv8, crystalGolem Lv9; ship ngay bằng `FallbackSheet` tô màu (`Monster.Visuals.cs:14-21`), sheet thật cài sau.
- Protocol 14 chưa phát hành -> giữ 14; nếu đã phát hành trước khi xong -> 15.

## Các phase
| # | Phase | Effort | Cần art? | Status | File |
|---|---|---|---|---|---|
| 1 | Layout: rows cũ, sketch, cụm quái, hình học outpaint | 2h | không | done (rows from ground-only image; lead sign-off pending) | [phase-01](phase-01-layout-sketch-clusters-outpaint-geometry.md) |
| 2 | Code làm ngay: migrate save v3, NearEntrance, 3 MonsterDef fallback | 3h | không | done (save migration staged for P6) | [phase-02](phase-02-code-now-save-migration-monsterdefs.md) |
| 3 | Art (lead, ChatGPT): nền chỉ đất, 7 mảnh outpaint, prop sheet, 3 quái | lead | - | in-progress | [phase-03](phase-03-art-requests-chatgpt.md) |
| 4 | Ghép nền + chunk + minimap + rows | 4h | ground + outpaint | pending | [phase-04](phase-04-assemble-ground-chunks-rows.md) |
| 5 | Prop: cắt khu cũ (diff) + prop sheet mới + bảng đặt | 5h | ground + sheet | pending | [phase-05](phase-05-props-cut-and-place.md) |
| 6 | Code map: rows, chunk, cổng Den, prop, cụm quái | 3h | P4, P5 | pending | [phase-06](phase-06-map-code-portals-spawns.md) |
| 7 | Cài sheet 3 quái băng | 3h | sheet quái | pending | [phase-07](phase-07-ice-monster-sheets.md) |
| 8 | Test + kiểm tra (build bản sao, chụp, tải, bộ nhớ) | 4h | P6 | pending | [phase-08](phase-08-tests-and-verification.md) |

## Phụ thuộc
1, 2 làm ngay song song. 3 (lead) song song. 4 cần nền chỉ-đất + 7 mảnh. 5 cần nền chỉ-đất (khu cũ) + prop sheet (khu mới). 6 cần 4+5. 7 độc lập (chỉ cần sheet). 8 sau 6 (và 7 nếu có).

## Sở hữu file
- P1: `layout/` trong plan. P2: `Rpg/SaveSerializer.cs`, `server-api/src/rpg/item-validation.js`, `MonsterDef.cs`, `Rpg/LootTable.cs`, `MapSpawnTable.cs` (chỉ `NearEntrance`), test save trong `Tests/dotnet/ZenoniaSkills.Tests/`.
- P4: `Tools/build-frost-hollow-large-painted.py`, `Tools/build-frost-hollow-large-rows.py`, ảnh `Sprites/tiles/frost-hollow-large-ground-*`.
- P5: `Tools/build-frost-hollow-props.py`, `Tools/cut-frost-hollow-extra-props.py`, `Sprites/village/props/fh*.png`, `fhx*.png`, `VillagePropDefs.cs`.
- P6: `Maps/FrostHollowLayout.cs`, `Maps/FrostHollowProps*.cs` (mới), `Maps/FrostfangDenMap.cs`, `FrostHollowMap.cs` (comment), `MapSpawnTable.cs` (sau P2, nối tiếp không song song).
- P7: `Tools/build-ice-monsters.py`, `Sprites/monsters/{iceWraith,snowYeti,crystalGolem}*.png`, `MonsterDef.cs` (bỏ fallback nếu cần, sau P2).
- P8: `Tests/dotnet/ZenoniaNet.Tests/{FrostHollowLargeMapTests.cs (mới), MapSizeTests.cs, MapGraphTests.cs}`, `docs/pending-tasks.md`.

## Rủi ro chính
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Save cũ lệch 24 hàng | Cao x Cao | P2 migrate v3 + test; `RectBlocked` vẫn là lưới an toàn |
| Nền chỉ-đất ChatGPT xê dịch vách/sông -> rows cũ sai | TB x Cao | P4 overlay rows lên nền mới, so diff vách với ảnh gốc, sửa rows tay |
| Pixel-art 7 mảnh lệch màu/cỡ pixel, đường nối | TB x TB | tỉ lệ 1, match màu, min-cut seam, xem 1:1 từng đường nối |
| Rows tự động nhầm vách xám/tuyết | Cao x TB | phân loại + file override tay + overlay |
| Tải server: 18 -> ~75 quái trên zone mặc định (load test giữ bot ở đây) | TB x TB | P8 đo lại p99 tick, ghi baseline mới |
| Bộ nhớ: 4 chunk RGBA ~56 MB (Windows/WebGL) | TB x TB | `WorldBuilder` unload (đã có); đo như Willowbrook P7 |

## Rollback
Mỗi phase 1 commit. Revert P6 = Frost Hollow 36x24 cũ (ảnh chunk thừa vô hại; giữ `frost-hollow-ground.png` tới khi P8 xong). Phần migrate save của P2 **commit chung với P6** (vào trước thì save 36x24 bị dịch sai); sau khi phát hành không revert được (save v3 đã ghi y mới).

## Câu hỏi còn mở
Cuối phase-08.
