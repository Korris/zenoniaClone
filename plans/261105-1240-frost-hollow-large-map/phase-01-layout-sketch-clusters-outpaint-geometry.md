# Phase 01: Layout 72x48, cụm quái, hình học outpaint

## Context
- Rows cũ: `Assets/Scripts/Maps/FrostHollowLayout.cs:14-39` (T thông, C vách/đá/phế tích, w sông băng, B cầu, s tuyết; solid "TCw" dòng 53).
- Nền cũ: `Assets/Resources/Sprites/tiles/frost-hollow-ground.png` 2304x1536 (= `ArtLibrary/hd/map/frost-hollow/frost-hollow-ground-original.png`); bản 1536 cho ChatGPT: `ArtLibrary/hd/map/frost-hollow/large/frost-hollow-1536.png`.
- Mốc trong tranh cũ (ô): sông cột 17-20 chảy bắc -> nam, ra mép dưới ở cột ~18-20; cầu hàng 12 cột 17-21; bậc đông bắc lên (33,2-4) = cổng Den cũ; bậc tây nam (3-4, 20-22) đi xuống mép dưới; hang mỏ góc tây bắc (~3,2) và đông nam (~32,20) (trang trí).
- Mẫu: `plans/261105-1052-willowbrook-large-hunting-map/layout/` (sketch, clusters.md/json, script layout).

## Overview
Priority P1. Làm ngay, không cần art. Ra trong `layout/`: `frost-hollow-36x24-old-rows.txt`, `frost-hollow-72x48-sketch.txt`, `frost-hollow-72x48-clusters.md`, `outpaint-geometry.md`. Sketch dùng làm mô tả nội dung cho prompt outpaint (P3); rows thật lấy từ tranh ở P4.

## Phác thảo (1 ký tự = 3x3 ô; cột c -> ô 3c..3c+2, hàng r -> ô 3r..3r+2)
```
       c 0         1         2
         012345678901234567890123
  r 0    OOOOOOOOOOOO^^^^^^^^^^^#   O khu cũ (ô 0-35 x 0-23, giữ nguyên)  ^ vách bắc
  r 1    OOOOOOOOOOOS....W6W..TT#   S bậc đông bắc cũ (33,2) dẫn lên gờ cao  W tháp canh đổ nát
  r 2    OOOOOOOOOOOO=====.....T#   = đường tuyết / đá lát
  r 3    OOOOOOOOOOOO..4....7...#
  r 4    OOOOOOOOOOOO......=.cc.#   c cụm pha lê lớn (prop)
  r 5    OOOOOOOOOOOO.TT...=....#
  r 6    OOOOOOOOOOOO.TT.5.=....#
  r 7    OGvOOO~OOOOO......=....#   G cổng làng (2,21)  v bậc tây nam đi xuống  ~ sông ra khỏi khu cũ
  r 8    #.v...~~.......LLL.=...#
  r 9    #..1..~~....LLLLLL..9..#   L hồ băng (giữa nam)
  r10    #.....H~..3.LLLLLLL..I>   H cầu 2 (nam)  I miệng hang băng  > cổng Den (71,30-32)
  r11    #.....~~.....LLLL....I#
  r12    #..2...~~........=.....#
  r13    #.TT....~~...8...=..A..#
  r14    #.......~~~......TT....#
  r15    ########################   ô 45-47: dưới HUD, đặc
```
- Lối từ khu cũ ra: (a) bậc tây nam -> đồng tuyết nam (dễ nhất, gần cổng làng); (b) mở mép đông cũ ở hàng 5-11 (đồng tuyết đông cũ) -> giữa bắc; (c) bậc đông bắc cũ -> gờ cao -> tháp canh. Sông tiếp tục xuống nam, cầu 2 ở hàng ~31.
- Cổng làng (2,21) giữ trong hốc: hàng 22-23 cột 0-1 vẫn đặc (vách), lối xuống nam qua bậc cột 2-5.
- Viền: hàng 45-47 T (HUD), cột 70-71 T trừ cổng Den hàng 30-32, cột 0 T (hàng 24-44).

## Cụm quái (ô; zone cũ giữ, rect mới chỉnh lại theo rows thật ở P6)
| # | Khu | Loại | Lv | SL | tx,ty,w,h | Hồi ms |
|---|---|---|---|---|---|---|
| cũ x5 | khu cũ (`FrostHollowMap.cs:23-30`) | slime/goblin/wolf | 1-2 | 18 | không đổi | 8000-11000 |
| 1 | Nam cổng làng | frostSlime | 1 | 6 | 2,27,10,5 | 7000 |
| 2 | Tây nam | iceGoblin | 2 | 6 | 3,37,12,6 | 8000 |
| 3 | Bờ nam, đông sông | spectralWolf | 2 | 5 | 26,28,10,6 | 8500 |
| 4 | Đông khu cũ | iceGoblin | 2 | 5 | 38,8,10,7 | 8000 |
| 5 | Bờ bắc hồ | spectralWolf | 2 | 6 | 40,19,12,5 | 9000 |
| 6 | Tháp canh (gờ cao) | iceWraith | 7 | 7 | 50,2,12,7 | 9500 |
| 7 | Đồng đông bắc | iceWraith | 7 | 6 | 58,10,10,8 | 9500 |
| 8 | Nam hồ | snowYeti | 8 | 6 | 40,37,14,6 | 10000 |
| 9 | Lối vào hang Den | crystalGolem | 9 | 5 | 54,24,9,9 | 10500 |
| A | Góc đông nam | snowYeti | 8 | 5 | 58,38,10,6 | 10000 |
Tổng mới 57 + cũ 18 = 75 (+ King). Ràng buộc: rect cách start (4,21), cổng làng (2,21), cổng Den (71,30-32) và ô đến (69,31) >= 5 ô (80 đơn vị, `MapSpawnTable.cs:13`); mỗi rect >= 2x SL ô đi được; cụm cách nhau >= ~12 ô.

## Hình học outpaint (px nguồn, 42.667 px/ô; canvas đích 3072x2048 = 72x48 ô)
Ảnh cũ chỉ-đất 1536x1024 ở (0,0). Mỗi khung ChatGPT 1536x1024 (ngang) hoặc 1024x1536 (dọc), tỉ lệ 1, phần đã biết dán sẵn, phần trống để vẽ.
| Mảnh | Khung | Đặt tại (x,y) | Phần đã biết | Vẽ mới (px) | Ô mới | Cần trước |
|---|---|---|---|---|---|---|
| E1 | 1536x1024 | 1024,0 | trái 512 (cũ) | x1536-2560, y0-1024 | c36-59, r0-23 | nền chỉ-đất |
| S1 | 1536x1024 | 0,512 | trên 512 (cũ) | x0-1536, y1024-1536 | c0-35, r24-35 | nền chỉ-đất |
| SE1 | 1536x1024 | 1024,512 | trên 512 (cũ+E1), trái 512 (S1) | x1536-2560, y1024-1536 | c36-59, r24-35 | E1, S1 |
| S2 | 1536x1024 | 0,1024 | trên 512 (S1) | x0-1536, y1536-2048 | c0-35, r36-47 | S1 |
| SE2 | 1536x1024 | 1024,1024 | trên 512 (SE1), trái 512 (S2) | x1536-2560, y1536-2048 | c36-59, r36-47 | SE1, S2 |
| FA | 1024x1536 dọc | 2048,0 | trái 512 (E1, SE1) | x2560-3072, y0-1536 | c60-71, r0-35 | SE1 |
| FB | 1024x1536 dọc | 2048,512 | trên 1024 (FA), trái 512 (SE1, SE2) | x2560-3072, y1536-2048 | c60-71, r36-47 | FA, SE2 |
Thứ tự: (E1 ‖ S1) -> (SE1 ‖ S2) -> SE2 -> FA -> FB. Mỗi khung đầu vào ghép bằng script (`--canvas <mảnh>`, như `fareast-canvas-input.png`), tô phần trống màu trơn. Vùng chồng dùng để match màu + cắt min-cut.
Phương án ít mảnh hơn (6, như Willowbrook): thay FA+FB bằng 1 dải dọc tỉ lệ 0.75 phủ x1707-3072 -> pixel to hơn 1.33 lần ở 12 cột đông. Không chọn cho pixel-art.

## Steps
1. Xuất rows cũ ra `frost-hollow-36x24-old-rows.txt`.
2. Đánh dấu ô cũ sẽ đổi khi tách prop: T đơn lẻ/cụm nhỏ -> s (+ footprint gốc cây); C là đá/bia/xương/hàng rào -> s (+ footprint prop); T rừng viền (cột 0-3, hàng 18-21 cột 8-17...) giữ T, phủ bằng prop cây dày.
3. Viết sketch + clusters.md (bảng trên) + outpaint-geometry.md (bảng trên + mô tả nội dung từng mảnh cho P3).
4. Lead duyệt sketch trước khi gửi prompt outpaint.

## Todo
- [ ] old-rows.txt  - [ ] đánh dấu ô đổi  - [ ] sketch  - [ ] clusters.md  - [ ] outpaint-geometry.md  - [ ] lead duyệt

## Success criteria
Sketch có đủ: lối ra 3 hướng từ khu cũ, sông + cầu 2, hồ, tháp canh, hang Den mép đông; bảng cụm đáp ứng ràng buộc khoảng cách (tính tay hoặc script nhỏ trong scratchpad).

## Risk
Tranh outpaint không theo sketch -> rows/cụm phải chỉnh theo tranh (như Willowbrook: rows từ tranh). Chấp nhận; sketch chỉ định hướng.

## Next
P3 dùng outpaint-geometry; P6 dùng clusters.
