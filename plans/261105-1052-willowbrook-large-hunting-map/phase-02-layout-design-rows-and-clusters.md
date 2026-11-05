# Phase 02: Thiết kế layout 72x48, rows, cụm quái

## Context
- Nền cũ (`Sprites/village/tiles/willowbrook-meadows-ground.png`, 2304x1536): đường đất từ cổng tây (hàng cũ 11) sang cầu gỗ (cột ~23-27, hàng cũ 10) rồi ra mép đông hàng cũ ~10; suối vào ở mép trên cột ~19-23, ra mép phải hàng cũ ~15-19; nhánh đường xuống tây nam.
- Rows hiện tại: `WillowbrookMeadowsMap.cs` (sau khi agent tách prop commit). Hàng cũ 21-23 bị chặn vì dưới HUD -> trong map mới là hàng 33-35 giữa map: mở ra (art đã vẽ đủ).

## Overview
Priority P1. Status pending. Ra 3 file trong `layout/`: `willowbrook-72x48-sketch.txt`, `willowbrook-72x48-rows.txt` (48 hàng x 72 ký tự), `willowbrook-72x48-clusters.md`.

## Đặt section cũ
Section tây = cột 0-35, **hàng 12-35** (dịch +12 hàng). Lý do: cổng ở giữa mép tây (hàng 23-24), map mở ra bắc, nam, đông -> độ khó tăng dần theo khoảng cách. Cổng Thatchwick đích đổi `(2,11)/(2,12)` -> `(2,23)/(2,24)`.

## Phác thảo (1 ký tự = 3x3 ô; cột c = ô 3c..3c+2, hàng r = ô 3r..3r+2)
```
     c 0         1         2
       012345678901234567890123
r 0    #######~################   thác nhỏ từ vách bắc
r 1    #.r.....~...T.....RRR..#
r 2    #..4...=H=5.T..6..RRR9.#   cầu 2 (bắc)
r 3    #.......~...T.....RRR..#
r 4    #.....T.~...T......T...#   <- hàng ô 12: đầu section cũ
r 5    #..1....~.3.T..7...T...#
r 6    #,,.....~~..T......T.A.#
r 7    G=======H=============X#   G cổng tây (ô 0,23-24), H cầu cũ, X rào chắn mép đông
r 8    #,......~~.....=.......#
r 9    #..r..2...~~...=...T...#
r10    #...F======H===....T.B.#   cầu 3 (suối chảy nam vào ao)
r11    #..FFF...T..~..=.8.T...#   <- hàng ô 35: cuối section cũ
r12    #..FFF...r..~~ooo..T...#
r13    #.,,.....r...oooo......#
r14    #.T..,,.....Too.....r..#
r15    ########################   ô 45-47: viền + dưới HUD
```
Chú giải: `#` rừng dày/vách (T đặc), `T` dải cây (prop, chỉ gốc chặn), `.` cỏ, `,` cỏ sẫm, `=` đường đất, `~` suối (w), `H` cầu (B), `o` ao (w), `R` phế tích, `F` ruộng bỏ hoang + lán/rơm cũ, `r` cụm đá, số/chữ = tâm cụm quái.
- 3 dải: Tây (c0-11, section cũ + dải bắc, dễ nhất), Giữa (c13-18), Đông (c20-22). Dải cây c12 (hở ở r7 đường, r8-14 phía nam) và c19 (hở ở r0-3, r7, r13-14) chia khu, không chặn kín.
- Ngõ cụt đông: đường chính r7 kết thúc ở rào chắn X (ô 69-71, hàng 21-23) = prop rào + `T`; sau này mở thành cổng.
- Va chạm: viền `T` bằng rows; cây/đá/phế tích chặn footprint (gốc cây 1 ô); suối/ao `w`; cầu `B`; ký tự `d` (đường) thêm vào rows chỉ để script art đọc mask, không đặc.

## Cụm quái (11 cụm, 70 con, ô)
| # | Khu | Loại | SL | tx,ty,w,h | Hồi (ms) |
|---|---|---|---|---|---|
| 1 | Tây bắc (cũ) | frostSlime | 6 | 4,15,9,5 | 6000 |
| 2 | Tây nam (cũ) | frostSlime | 6 | 12,27,12,5 | 6500 |
| 3 | Tây, bên kia suối (cũ) | iceGoblin | 5 | 25,15,8,6 | 7000 |
| 4 | Dải bắc tây | iceGoblin | 6 | 4,3,12,6 | 7500 |
| 5 | Dải bắc, đông suối | frostSlime | 5 | 28,3,7,6 | 6000 |
| 6 | Giữa bắc | iceGoblin | 7 | 40,4,12,7 | 8000 |
| 7 | Giữa, bắc đường | spectralWolf | 6 | 39,14,14,6 | 8500 |
| 8 | Bờ đông ao | spectralWolf | 6 | 45,30,9,7 | 9000 |
| 9 | Phế tích đông bắc | spectralWolf | 8 | 57,3,11,8 | 9500 |
| A | Đồng cỏ đông | iceGoblin | 8 | 57,15,11,8 | 9000 |
| B | Đông nam | spectralWolf | 7 | 60,30,10,9 | 10000 |
Cụm 1 bắt đầu ở cột 4: cổng (0,23-24) và start (3,23) cách >80 đơn vị (`MapSpawnTable.cs:13` EntranceClearance). Cụm cách nhau >= 1 màn (~15 ô) để không kéo dây chuyền.

## Implementation steps
1. Vẽ sketch -> phóng 3x thành rows 72x48, chỉnh tay: dán rows cũ (sau khi mở hàng 0, cột 35, hàng 21-23) vào cột 0-35 hàng 12-35; khớp suối/đường tại mép section với vị trí trong ảnh cũ (đo bằng pixel).
2. Script nhỏ (một lần, trong scratchpad) kiểm: mọi ô đi được thông nhau, mỗi rect cụm có >= 2x SL ô đi được.
3. Ghi `clusters.md` (bảng trên, chỉnh theo rows thật).

## Todo
- [ ] sketch.txt  - [ ] rows.txt (48x72)  - [ ] clusters.md  - [ ] kiểm thông + ô trống mỗi cụm

## Success criteria
Rows 48 hàng đúng 72 ký tự; flood fill từ cổng phủ mọi ô đi được; 3 hàng cuối đặc; người dùng duyệt sketch.

## Risk
Layout quá thoáng -> nhàm, quá kín -> kẹt quái: giữ hành lang >= 3 ô, mỗi khu 2 lối.

## Next
P3 đọc rows làm mask; P5 dùng bảng cụm.
