# Phase 03: Art (lead, ChatGPT)

## Context
- Đọc trước: `CLAUDE.md` (Art trên ChatGPT, Asset để đúng chỗ), `docs/sprite-weapon-rules.md` (Sổ lỗi; lỗi mới ghi vào đó), `docs/pending-tasks.md` (dòng Frost Hollow 72x48: chat "Edit Map Background" https://chatgpt.com/c/6aec23f6-1d14-83ec-bea4-44e11798c652, quái ở "Create Rabbit Sprite Sheet").
- Tài khoản **Tay** (Plus); hết lượt -> Tan Pham, chat mới, kiểm tên tài khoản. Không xaochai / Tieu Duy Phuong.
- Hình học khung: `layout/outpaint-geometry.md` (phase-01).

## Overview
Lead làm, đang chạy (nền chỉ-đất). Ảnh gốc lưu `Assets/ArtLibrary/hd/map/frost-hollow/large/<tên>-chatgpt.png` (như Willowbrook `large/`), quái lưu `Assets/ArtLibrary/monsters/ice/`.

## Danh sách art
| # | Ảnh | Cỡ | Ghi chú |
|---|---|---|---|
| A0 | `ground-chatgpt.png` nền chỉ-đất khu cũ | 1536x1024 | xoá cây, đá rời, pha lê, bia rune, xương, hàng rào; **giữ** vách, sông băng, cầu, bậc, hang. Vách/sông không được xê dịch (so với `frost-hollow-1536.png`) |
| A1-A7 | `e1, s1, se1, s2, se2, fa, fb -chatgpt.png` | 1536x1024 / 1024x1536 | outpaint, chỉ nền (không cây/đá rời), cùng pixel-art tuyết; khung đầu vào do script P4 ghép (`--canvas <mảnh>`) |
| A8 | `prop-sheet-chatgpt.png` | 1536x1024 | nền #FF00FF trơn, vật cách nhau, cùng góc nhìn/ánh sáng: 4-5 thông tuyết (to, vừa, mảnh, cụm 2-3 cây), 2 cây khô trụi, 3 bụi tuyết, 4 đá tuyết các cỡ, 3 cụm pha lê (nhỏ, vừa, lớn), 2 bia rune, 2 bộ xương, 2 hàng rào gỗ, 1 khúc gỗ đổ, cột/tường tháp canh đổ (2-3 mảnh) |
| A9-11 | Ice Wraith, Snow Yeti, Crystal Golem | walk 768x768 (4x4 ô 192, hàng xuống/lên/trái/phải), attack 1024x1024 (ô 256, chân y=224) | định dạng như quái đồng cỏ (`Tools/build-meadow-monsters.py`), nền #FF00FF |

## Nội dung từng mảnh outpaint (theo sketch phase-01)
- E1 (c36-59, r0-23): gờ cao nối từ bậc đông bắc cũ chạy sang đông ở hàng 2-6; tháp canh đổ nát (nền đá lát, chỉ phần nền; tường/cột là prop) quanh c50-58 r2-8; đồng tuyết mở rộng từ đồng đông cũ (r5-11).
- S1 (c0-35, r24-35): bậc tây nam cũ đi xuống đồng tuyết; sông tiếp tục xuống nam (vào ở x ~750-890 px tại y=1024), cầu gỗ 2 ở r~31; vách ngăn hốc cổng làng (c0-1 r22-23).
- SE1 (c36-59, r24-35): bờ bắc và nửa trên hồ băng lớn (c36-56 r24-35), đồng tuyết đông.
- S2 (c0-35, r36-47): sông ra mép dưới; đồng tây nam; rừng thông thưa (chỉ nền tuyết dưới tán, cây là prop). 3 hàng cuối (r45-47) nằm dưới HUD: vẽ vách/rừng.
- SE2 (c36-59, r36-47): nửa dưới hồ, đồng tuyết nam.
- FA (c60-71, r0-35): đồng đông bắc; **miệng hang băng ở mép đông r29-33** (lối vào Den, như cửa hang trong Den nhìn từ ngoài), đường tuyết dẫn tới.
- FB (c60-71, r36-47): góc đông nam, vách khép kín mép đông/nam.
Mỗi prompt: "giữ nguyên phần đã có, chỉ vẽ vùng trống, cùng phong cách pixel-art, cùng ánh sáng, không thêm cây/đá rời/pha lê (chúng là vật riêng)"; gửi kèm A0 làm mẫu style.

## Todo
- [ ] A0  - [ ] E1  - [ ] S1  - [ ] SE1  - [ ] S2  - [ ] SE2  - [ ] FA  - [ ] FB  - [ ] prop sheet  - [ ] 3 quái  - [ ] ghi link chat vào `docs/pending-tasks.md`

## Success criteria
Phần đã biết trong mỗi mảnh khớp ảnh đầu vào (diff thấp ở vùng chồng); không có cây/đá rời trong nền; prop sheet nền trơn, không ô caro giả trong suốt.

## Risk
| Rủi ro | KN x TĐ | Giảm |
|---|---|---|
| ChatGPT vẽ lại cả phần đã biết (xê dịch) | Cao x TB | script chỉ lấy vùng mới + vùng chồng làm seam; match màu |
| Cỡ pixel/độ đậm lệch giữa mảnh | TB x TB | gửi A0 + mảnh kề làm mẫu; từ chối mảnh lệch rõ |
| Lại vẽ cây trong nền | TB x Thấp | yêu cầu sửa, hoặc xoá bằng edit; cây chồng prop là lỗi nhìn thấy được |
| Ô caro giả / quá trong suốt (Sổ lỗi) | TB x Thấp | xin nền #FF00FF trơn |

## Next
P4 (A0-A7), P5 (A0 + A8), P7 (A9-11).
