---
title: "Thatchwick Village + Willowbrook Meadows"
description: "Hai map mới phía đông Oakhaven: làng mái tranh an toàn (không NPC) và bãi săn Lv thấp, cả hai là một tấm backdrop vẽ sẵn"
status: pending
priority: P2
effort: 9h
branch: unity
tags: [map, art, portal, spawn, server]
created: 2026-11-05
---

# Thatchwick Village + Willowbrook Meadows

Tuyến: Oakhaven (cổng đông, hàng 10-11) -> Thatchwick Village (vào phía tây) -> cổng đông -> Willowbrook Meadows (vào cổng gỗ phía tây). Willowbrook là ngõ cụt, chỉ có cổng về.

## Quyết định chính
- Id `thatchVillage` (tên hiển thị đề xuất "Thatchwick Village"), id `willowbrookMeadows` ("Willowbrook Meadows"). Không id nào dài hơn 32 ký tự.
- Map 36x24 ô, 64 px nguồn mỗi ô -> backdrop 2304x1536. Art ChatGPT 1536x1024 (đúng tỉ lệ 3:2) -> phóng 1.5x Lanczos, không cắt.
- Nhà, giếng, sạp, cầu đã vẽ sẵn trong art -> không dùng MapProp; cả bóng nhà (mái + tường) là `T` để hero không đi lên mái.
- Ký tự ô: `T` đặc, `g` cỏ, `d` đường, `p` quảng trường, `w` nước (đặc), `B` cầu. Hàng 21-23 nằm dưới HUD -> chặn hết.
- Quái dùng lại: frostSlime (Lv1), iceGoblin (Lv2), spectralWolf (Lv2). Không boss. Nhạc dùng lại (`oakhaven-village` / `frost-hollow`).
- Server: MapRegistry tự sinh zone, server-api không có whitelist map -> chỉ cần build lại zone server + tăng protocol version.
- Không có bản web trong repo này -> không sửa `src/world/maps/*.js`.

## Các phase
| # | Phase | Effort | Status | File |
|---|---|---|---|---|
| 1 | Xử lý art + phân loại hàng va chạm | 3h | done | [phase-01](phase-01-art-backdrop-and-collision-rows.md) |
| 2 | Hai map client + cổng Oakhaven | 1.5h | done | [phase-02](phase-02-client-maps-and-portals.md) |
| 3 | Bảng quái Willowbrook | 1h | done | [phase-03](phase-03-willowbrook-spawn-table.md) |
| 4 | Server, protocol, bot test mạng | 1h | done | [phase-04](phase-04-server-protocol-and-net-bots.md) |
| 5 | Unit test | 0.5h | done | [phase-05](phase-05-unit-tests.md) |
| 6 | Kiểm tra bằng build bản sao + ảnh chụp | 2h | pending | [phase-06](phase-06-verify-build-from-copy-captures.md) |
| 7 | Willowbrook tách prop riêng (nền ground + 45 prop y-sort) | 3h | done | [phase-07](phase-07-willowbrook-separate-props.md) |

## Phụ thuộc
1 -> 2 -> 3 -> (4 song song 5) -> 6. Phase 3 cần hàng va chạm của phase 1 để đặt vùng quái.
Sở hữu file: mỗi phase sửa bộ file riêng (xem từng phase), không chồng nhau.

## Rủi ro chính
- Tỉ lệ art vs nhân vật chibi: cửa nhà ~1.5-2 ô (24-32 đơn vị), hero ~18-20 đơn vị -> chấp nhận được; phải xem ảnh chụp thật (phase 6).
- Vật cao (ngọn cây, mái giếng, ống khói) vẽ chết trong backdrop -> hero đi phía sau sẽ bị vẽ đè lên; giải bằng cách đánh `T` cả bóng vật, không làm cutout (YAGNI).
- Bot `WalkOutOfTown` (net-persist-test) chọn cổng theo slot: thêm cổng đông có thể đi lạc sang Willowbrook -> test kỳ vọng `frostHollow` hỏng. Sửa ở phase 4.
- `MapSpawnTable.ZonesFor/BossFor` mặc định trả Frost Hollow + Frostfang King: quên thêm case = Willowbrook có boss Lv3. Phase 3.
- Client cũ + server mới: server chuyển hero sang map client không có -> tăng `NetProtocol.Version` 13 -> 14.
- Lệch style với Oakhaven cũ: người dùng đã chấp nhận.

## Rollback
Mỗi phase một commit. Revert phase 2 (bỏ đăng ký MapRegistry + cổng Oakhaven) là game về như cũ; ảnh backdrop thừa không ảnh hưởng. Save đứng ở map đã xoá: `RpgSession` gặp id lạ -> `SetCurrent` log lỗi và giữ map mặc định (cần kiểm tra ở phase 6).

## Câu hỏi còn mở
Xem cuối phase-06.
