# Phase 03: Art - nền texture-splat 2x2 chunk + prop bổ sung

## Context
- Đọc trước: `CLAUDE.md` (mục Art trên ChatGPT, Asset để đúng chỗ), `docs/sprite-rules.md` (quy tắc xem ảnh, Sổ lỗi; ghi lỗi mới vào đó).
- Nguồn: `Assets/ArtLibrary/hd/map/willowbrook-meadows/willowbrook-meadows-ground-chatgpt.png` (1536x1024, ground-only), backdrop 2304x1536 hiện tại, 45 prop `wb*` (cắt bởi `Tools/build-willowbrook-props.py`, dùng numpy/scipy/opencv/PIL - đã có trên máy).
- Chat ChatGPT: tài khoản **Tay** (Plus), tiếp tục chat "Map Design Request" (`docs/pending-tasks.md`); hết lượt thì Tan Pham, chat mới, kiểm tên tài khoản. Không dùng xaochai / Tieu Duy Phuong.

## Overview
Priority P1. Status pending. Ra 4 chunk nền 2304x1536 + 1 ảnh minimap + 15-25 prop mới cùng style.

## So sánh phương án nền
| | (a) Texture-splat (CHỌN) | (b) 4 tranh ChatGPT ghép |
|---|---|---|
| Độ nét | 64 px/ô thật, như map khác | mỗi tranh 1536x1024 phóng 1.5x: ngang (a) |
| Đường nối | không có (1 canvas liền) | ánh sáng/màu/tỉ lệ mỗi tranh khác, mép không khớp, outpaint trôi dần |
| Khớp layout | theo đúng rows (suối, đường, cầu đúng ô) | ChatGPT không vẽ đúng ô, phải sửa rows theo tranh |
| Sửa layout về sau | chạy lại script | vẽ lại tranh |
| Nhược | dễ nhìn "lặp", kém chi tiết | tốn lượt vẽ, khó lặp lại |
-> Chọn (a); bù chi tiết bằng stamp vẽ tay (ao, phế tích, ruộng, thác, cầu) và rắc hoa/bụi cỏ lấy từ ảnh cũ.

## Art cần xin ChatGPT (gửi kèm ảnh nền cũ làm mẫu style, mỗi ảnh 1536x1024)
1. Bảng texture liền mép (seamless, nhìn thẳng từ trên, không bóng đổ, không vật): cỏ xanh nắng, cỏ sẫm dưới tán rừng, đường đất, nước suối có đá cuội, bờ đất/cỏ ven nước. Hoặc tự cắt từ ảnh cũ (vùng cỏ rộng có sẵn; làm liền mép bằng offset + blend chéo) - thử tự cắt trước, chỉ xin khi lặp lộ.
2. Stamp ground-only, nền cỏ cùng màu, mép mềm: ao nhỏ có lau sậy; nền phế tích đá lát; ruộng bỏ hoang có luống; thác nhỏ chảy từ vách bắc; cầu gỗ hướng N-S (nếu suối chạy ngang; cầu cũ là E-W, cắt được từ ảnh cũ).
3. Prop sheet (nền trơn một màu dễ tách, vật cách nhau, cùng góc nhìn và ánh nắng như prop `wb*`, có bóng mềm dưới chân): 4-5 kiểu sồi/thông, 3 bụi, 3 cụm hoa, 4 đá các cỡ, 2 khúc gỗ đổ, 2 gốc cây, cột phế tích + tường đổ, rào chắn gỗ (cho ngõ cụt đông), biển báo.
Ảnh gốc lưu `Assets/ArtLibrary/hd/map/willowbrook-meadows/large/` (tên có nghĩa).

## Script 1: `Tools/build-willowbrook-large-ground.py` (mới, < 200 dòng, tách helper nếu dài)
Data flow: rows 72x48 (đọc từ `WillowbrookMeadowsMap.cs` như lệnh `check` của script cũ, hoặc `layout/willowbrook-72x48-rows.txt` trước P4) -> mask từng vật liệu theo ô (cỏ, cỏ sẫm `,`, đường `d`, nước `w`, cầu `B`) -> phóng 64x -> làm mềm: gaussian blur + noise (perlin/value noise numpy) để mép cong tự nhiên -> trọng số chuẩn hoá -> trộn texture (lát ngẫu nhiên offset/lật mỗi ô 4x4 để giấu lặp) -> nhân noise màu tần số thấp (+-6% sáng/hue) -> bờ nước tối dần theo distance transform (scipy) -> dán stamp (alpha mép mềm) -> rắc 300-500 mảng hoa/cỏ cắt từ ảnh cũ -> **dán nền cũ 2304x1536 tại px (0, 768)**, alpha 1 bên trong, mép trên/phải mờ dần 3 ô (192 px) -> (tuỳ chọn, sau P4) bóng mềm elip dưới mỗi prop theo PropList -> canvas 4608x3072.
Output:
- `Assets/Resources/Sprites/village/tiles/willowbrook-meadows-ground-r0c0.png`, `-r0c1`, `-r1c0`, `-r1c1` (2304x1536, RGB).
- `.../tiles/willowbrook-meadows-minimap.png` (432x288: nền + prop thu nhỏ + bóng).
- Ảnh debug thu nhỏ `plans/.../visuals/ground-full.png`, 4 góc 1:1.
Lệnh: `build`, `check` (vẽ lưới ô + ô đặc + footprint prop lên nền, như script cũ).

Giấu đường nối section cũ: (1) texture cỏ lấy mẫu từ chính ảnh cũ nên cùng màu; (2) dải mờ 3 ô; (3) suối/đường splat bắt đầu đúng pixel nơi ảnh cũ ra mép (đo trong ảnh), cùng bề rộng; (4) đặt cây/bụi đè lên dải nối (cột 34-37, hàng 11-13).

## Script 2: `Tools/cut-willowbrook-prop-sheet.py` (mới)
Prop sheet nền trơn -> tách nền (chroma/khác màu nền + flood từ viền, như mode `rock/struct` của script cũ, import helper bằng importlib) -> connected components -> box tự động, đặt tên theo bảng khai báo -> phóng về 64 px/ô theo cỡ đích (cây ~3.5x3.75 ô như `villageOak`) -> pad để chân footprint nằm đúng pivot dưới-giữa (như `MapScenery.BuildProp`) -> `Assets/Resources/Sprites/village/props/wb2<Name>.png` + in snippet PropDef. Xuất sheet kiểm tra (mọi prop trên nền cỏ, có khung footprint).

## Implementation steps
1. Thử cắt texture liền mép từ ảnh cũ; nếu lặp lộ, xin bảng texture (mục 1).
2. Xin stamp (mục 2) và prop sheet (mục 3); ghi link chat vào `docs/pending-tasks.md`.
3. Viết script 1, chạy với rows P2, xem ảnh full + 4 góc 1:1 + mép section cũ.
4. Viết script 2, cắt prop, xem sheet kiểm tra.
5. Lỗi art mới -> ghi Sổ lỗi `docs/sprite-rules.md`.

## Todo
- [ ] texture  - [ ] stamp  - [ ] prop sheet  - [ ] script 1 + 4 chunk + minimap  - [ ] script 2 + prop wb2*  - [ ] tự xem ảnh

## Success criteria
4 chunk đúng 2304x1536; ghép lại không thấy vạch nối chunk; mép section cũ không thấy ở ảnh 1:1; suối/cầu/đường trùng ô `w/B/d` trong overlay `check`; prop mới cùng tông với `wb*` khi đặt cạnh nhau.

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| Nền lặp, nhạt so với tranh vẽ tay | Cao x Trung bình | noise màu, rắc hoa từ ảnh cũ, stamp, nhiều prop |
| Prop sheet ChatGPT lệch style/góc nhìn | Trung bình x Trung bình | gửi kèm ảnh prop `wb*` + nền; chỉ nhận cái hợp |
| Ảnh ChatGPT bị vẽ ô caro giả trong suốt | Trung bình x Thấp | xin nền trơn một màu, không xin PNG trong suốt (Sổ lỗi) |

## Next
P4 đặt prop; chạy lại script 1 bước bóng + minimap khi có PropList.
