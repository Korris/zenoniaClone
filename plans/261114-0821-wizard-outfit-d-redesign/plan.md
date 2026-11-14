# Wizard bộ áo mới "D" (14/11/2026)

Người dùng: tóc mới gắn lên bộ áo Wizard cũ trông lượm thượm, mặt lim dim không giống bảng đầu. Chọn bộ **D** trong
`Assets/ArtLibrary/classes/wizard/head-redesign-20261114/wizard-fullbody-concepts-chatgpt-1.png`: tóc trắng dựng to, áo xanh trắng,
giáp vai vàng góc cạnh, thắt lưng nâu, vạt trắng, mắt to sáng xanh băng. Giữ hệ thống đổi kiểu tóc + nhuộm (thân đầu trọc + lớp tóc).

Bắt buộc đọc `docs/sprite-rules.md` trước mỗi bước (điều 8: cỡ đầu như idle; Sổ lỗi Wizard). ChatGPT: tài khoản Tay, chat World Map.

| Bước | Việc | Trạng thái |
|---|---|---|
| 1 | Idle 4 hướng đầu trọc + mặt mới (bản thiết kế chuẩn), người dùng duyệt | vẽ xong `Assets/ArtLibrary/classes/wizard/outfits/outfit-d-t0/src/wizard-outfit-d-t0-idle-4dirs-bald-chatgpt-1.png` (chat World Map, Việc 54), chờ duyệt |
| 2 | Cài idle: thay `build-wizard-northwind-idle-dirs.py` nguồn mới -> body/bald idle + hair-anchors, xem trong game | chưa |
| 3 | Walk 4 hướng (như `build-wizard-northwind-walk.py`) | chưa |
| 4 | Attack1-4 x 4 hướng (như `build-wizard-combo-v2-attacks.py`, `build-wizard-northwind-attacks.py`), giữ thời gian SkillTable | chưa |
| 5 | Tóc: co / vẽ lại 5 kiểu v4 cho hợp đầu mới (đầu ~40 % chiều cao như Warrior/Mechanic) | chưa |
| 6 | hurt, dash; ảnh chọn nhân vật; bậc áo T1-T9 theo bộ D | chưa |

Mỗi bước: xem ảnh ghép đúng cách game vẽ + chụp trong game trước khi báo xong; ghi lỗi vào Sổ lỗi.
