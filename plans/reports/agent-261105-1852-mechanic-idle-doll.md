# Mechanic idle doll 4 hướng từ ảnh ChatGPT (2026-11-05)

## Trạng thái: CHƯA `write`, chờ quyết định về cỡ đầu hướng lên

Brief yêu cầu: đầu lệch quá ~6% so với frame 0 hiện tại thì báo thay vì ghi. Hướng lên lệch 10%, nên chưa ghi gì vào `Assets/` (json `held-mechanic-dual.json` đang `M` trong git là sửa từ trước khi tôi bắt đầu, tôi không đụng).

Tỉ lệ bề ngang tóc (hàng rộng nhất, frame 0 mới / frame 0 hiện tại cùng hàng):

| Hướng | Tóc mới | Tóc cũ | Tỉ lệ | Bề ngang thân (mới/cũ) y115, y135, y155 |
|---|---|---|---|---|
| down | 70 | 69 | 1,014 | 60/61, 86/73, 115/107 |
| up | 75 | 68 | **1,103** | 61/81, 85/60, 100/81 (hàng cũ hai tay giơ nên không so được) |
| left | 73 | 72 | 1,014 | 82/62, 100/88, 80/72 |
| right | 73 | 72 | 1,014 | 82/62, 100/88, 80/72 |

Hướng lên: hai hình xuống/lên cùng một ảnh ChatGPT, cùng chiều cao đầu-đế (hàng 35-834), nhưng tóc nhìn từ sau xoè rộng hơn mặt trước (75 so với 70, +7%). Hai lựa chọn:
1. Chấp nhận: `HEAD_OK=1 python Tools/build-mechanic-idle-doll-from-chatgpt.py write` (rồi `check` lại và chạy `compose-mechanic-cannon-check.py` như brief).
2. Co hướng lên theo tóc (×0,93, cao 153 px thay vì 164): tôi sửa tool thêm hệ số riêng cho `up`.

## Đã làm

- `Tools/build-mechanic-idle-doll-from-chatgpt.py` (241 dòng) + `Tools/mechanic-idle-doll-helpers.py` (251 dòng): lệnh `grid`, `check`, `write`. `write` sao lưu 3 sheet idle + json vào `dual-src/cannon-back-lr/old/` rồi ghi hàng 0-3 của `bare`/`hand`/`armed` và anchor idle của `cannon`/`gun`/`gunL`, giữ nguyên mọi entry khác, json ghi `separators=(",", ":")`.
- Khớp cỡ từng hướng theo đầu-đế (đầu 25, đế 188), đứng trên cùng hàng đế, giày canh tâm giày hàng cũ (96,5 / 95 / 90,5 / 100,5). Thở: cao thêm 0/+1/+2/+1 px từ đế, anchor y dời cùng tỉ lệ (y' = 189 − (189 − y)·(164+g)/164).
- Hình có pháo khớp riêng theo đầu-đế của chính nó rồi đổi số đo qua phép biến đổi của nó (ảnh "up strap" k = 0,147 so với bare k = 0,204).

## Ảnh kiểm tra (`C:/temp/cap-cannon/doll/`)

- `check.png`: mỗi hướng 4 frame ghép như game | bản vẽ có pháo cùng cỡ | frame 0 armed hiện tại | frame 0 bare hiện tại.
- `check-zoom-<dir>.png`: frame 0 mới cạnh bản vẽ, phóng 4 lần. Đã xem cả 4: miệng nòng không che mặt, súng nằm trong nắm tay, nắm tay đè chuôi, đuôi pháo hướng ngang khuất sau hông, hướng xuống/lên đuôi ló đúng chỗ như bản vẽ.
- `grid-<dir>.png`: frame 0 bare | armed trên lưới 10 px, có đánh dấu blob xanh (lens) và hồng xám (nắp đuôi).
- `zoom-bare-<dir>.png`, `zoom-armed-<dir>.png`: phóng 8x/6x để đọc nắm tay và đầu pháo.
- `far-front/`: bản thử súng xa vẽ trước thân (đã chọn làm bản chính).
- `arts-marks.png`: lens và nắp của 3 art pháo (side: lens (328,39) nắp (14,37) = 314 px; front: (231,34)-(16,234) = 294 px, hướng −42,9°; back: (66,61)-(236,230) = 240 px, hướng −135,2°).

## Anchor sẽ ghi (frame 0; frame 1-3 chỉ đổi y theo thở)

| part | dir | x | y | angle | s | sx | behind | art |
|---|---|---|---|---|---|---|---|---|
| cannon | down | 95,5 | 103,5 | 3,44 | 0,5 | 0,994 | true | front |
| gun | down | 65 | 135 | 0 | 0,195 | 1 | false | pistol-front |
| gunL | down | 128 | 135 | 0 | 0,195 | 1 | false | pistol-front |
| cannon | up | 91 | 101,5 | 0,35 | 0,5 | 0,978 | false | back |
| gun | up | 130 | 135 | 0 | 0,195 | 1 | false | pistol-back |
| gunL | up | 59 | 134 | 0 | 0,195 | 1 | false | pistol-back |
| cannon | right | 76,08 | 85,08 | −135 | 0,5 | 0,9 | true | side |
| gun (gần) | right | 136 | 133 | 35 | 0,26 | 1 | false | – |
| gunL (xa) | right | 88 | 136 | −40 | 0,26 | −1 | false | – |
| cannon | left | 114,92 | 85,08 | −45 | 0,5 | 0,9 | true | side |
| gunL (gần) | left | 56 | 134 | −35 | 0,26 | −1 | false | – |
| gun (xa) | left | 103 | 135 | 40 | 0,26 | 1 | false | – |

Số đo pháo trên bản vẽ (px frame sau khớp): xuống lens (151,56) nắp (40,151) → dài 146, aim −40,6°; lên lens (50,60) nắp (132,143) → dài 117, aim −134,7°. Blob lens dò tự động trùng số đọc tay trong 2 px.

## Chỗ khác brief, cần người dùng biết

1. **Pháo hướng ngang:** đuôi bị thân che nên không đo được chiều dài vẽ; giữ `sx` 0,9 như walk/attack (một cỡ). Miệng nòng phải vẽ ở (38,41) thì đuôi ló ở hông → dời lên-ra sau 12 px thành (26,35). Hướng trái bản vẽ miệng nòng ở (134,60) sát đầu, pháo chúi xuống thân (phối cảnh): với pháo dài 151 px đuôi sẽ ló trước ống chân ở mọi góc → lấy đối xứng của hướng phải qua tâm giày (miệng (165,35), aim −45). Cả hai hướng nòng chĩa lên-sau như bản vẽ.
2. **Súng tay xa hướng ngang `behind: false`** (brief ghi true): nắm tay xa nằm trên áo choàng, vẽ sau thân thì áo choàng che gần hết, chỉ lòi mũi nòng dưới mép áo; bản vẽ cũng để súng xa trên áo choàng. Đổi lại trong `PISTOL_BEHIND` nếu muốn.
3. Súng hướng ngang theo bản vẽ: súng gần chĩa về trước chúc 35°, súng xa chĩa ra sau chúc 40° (lens hồng ở đầu nòng trong bản vẽ). Khác anchor hiện tại (15°/0°, hai súng cùng chĩa trước).
4. Walk/attack hướng trái vẫn là vá "nòng lên-trước" ngày 2026-11-05; idle mới theo bản vẽ nòng lên-sau → sẽ nhảy khi bắt đầu đi. Phải vá lại 27 frame `cannon/left` đó (chưa làm, ngoài phạm vi).
5. Pháo trong bản vẽ nhỏ hơn art game (vòng miệng nòng khoảng 29 px so với 40 px ở `s` 0,5); giữ `s` 0,5 theo quy tắc một cỡ nên ảnh ghép pháo to hơn bản vẽ.
6. Hàng 0 `armed` sẽ là ảnh ghép tier 0 (chân dung túi đồ dùng hàng này).

## Chưa làm (vì chưa `write`)

- Chạy `write`, `check` lại, và `python Tools/compose-mechanic-cannon-check.py C:/temp/cap-cannon/after-doll.png --anim idle --dirs right,left,down,up --tiers scrap,brass,dragon --zoom 2 --frame 0`, xem tier 1-9.
- Xem trong game.

## Sổ lỗi

Đã thêm 4 dòng ngày 2026-11-05 (Mechanic) vào `docs/sprite-rules.md`: đầu hướng lên, đuôi pháo ngang, súng xa sau áo choàng, mask màu đuôi/nắm tay.

## Câu hỏi chưa giải quyết

1. Hướng lên: chấp nhận tóc rộng hơn 10% (`HEAD_OK=1`) hay co 0,93?
2. Có vá lại walk/attack trái cho nòng lên-sau khớp idle mới không?
