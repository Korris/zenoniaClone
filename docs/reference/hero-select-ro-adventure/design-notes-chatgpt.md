# Màn chọn class RO Soft Adventure (ChatGPT, 26/11/2026)

Chat (Tay "Tân Korris"): https://chatgpt.com/c/6b07c8b6-b0c0-83ec-8cbd-ce6b591d919a
Người dùng: "chỗ chọn nhân vật để tạo chưa có archer, kêu chatgpt design lại cho hợp với phong cách ro fantasy và hiện thêm archer, bỏ boxing đi".

- Ảnh gửi: `ref-sent/` (màn cũ trong game, 04-character-create, túi đồ Adventure, Asteria Art Direction V1, turnaround V3; sau đó `ingame-six-classes-on-picker.png` để vẽ chân dung).
- Mockup ChatGPT tự quyết: `mockup-chatgpt-1.png` (6 class trên bệ dưới cờ, panel thông tin + chân dung, Skill Preview, khung xem trước, NEXT / BACK).
- Asset (ChatGPT, nền phẳng): `Assets/ArtLibrary/ui/hero-select-ro/src/` -> `Tools/cut-ui-paper-sheet.py Tools/ui-ro-soft-sheets/hero-select-ro-{a-kit,b-icons,c-banners,d-skill-ring}.json write` + `Tools/build-hero-select-ro-art.py` (chân dung, nền làm mềm, thanh chỉ số / vòng sáng xám để tô màu) -> `Assets/Resources/Sprites/ui/hero-select-ro/`.
- Nhân vật trên bệ = sprite thật trong game (HeroPreviewStage), không vẽ (người dùng đã chốt: không class nào đội mũ).
- Cờ lần 1 chép bộ cờ màn cũ (có cờ lửa Boxing, thiếu Archer): xin vẽ lại theo mockup (`hero-select-ro-banners-chatgpt-2.png`).

## Chấm bản trong game lần 1 (`plans/reports/hero-select-redesign/r3/`): 8,6 / 10, sát mockup 90 %

Sửa V2 ChatGPT yêu cầu (đã làm):
1. Nền mềm lại: blur 1,5-2,5 px ở 720p, saturation -8-12 %, contrast -8 % (không vẽ lại).
2. Bỏ khung xanh đậm trong Skill Preview: 5 icon tròn viền vàng riêng (48-50 px, cách 5-7 px), bấm hiện tên; skill chưa mở mờ + ổ khoá. ChatGPT vẽ thêm khung tròn.
3. Chữ panel thông tin: tên 26-28, mô tả 14-15, nhãn chỉ số 13-14, số 14-15 Bold, thanh 10-12, hàng cách 23-24, chân dung 150-155 rộng; màu thanh ATK #B86B50, DEF #658FB7, HP #76A16B, MP #8D79BA, CRIT #D2A247.
4. Vòng sáng chọn: vàng ấm (lõi #FFF5CC, viền #FFC65D), nhịp sáng 2 s; class đang chọn scale 1,10 quanh chân, chuyển 180-220 ms; không làm tối class khác.
5. Chữ "Select Class" lên 7-9 px.
6. NEXT 184 x 58, BACK 174 x 46, cách 16-20, cùng trục X, cụm căn giữa theo chiều cao khung xem trước.
7. Khung xem trước sáng hơn 10-15 %.

## Chấm lần 2 (`ingame-v2-*.jpg`, `plans/reports/hero-select-redesign/r5/`): 9,4 / 10, "đủ để chốt UI màn Select Class", không cần vẽ lại asset

Polish V3 ChatGPT gợi ý (đã làm, ảnh `r6/`): halo vòng sáng dịu hơn 10-15 %; hàng icon skill căn giữa khi trang ít icon; ổ khoá nhỏ hơn ~10 % và mờ 85 %; tên skill 15 đậm, dòng điều kiện 12 và 85 % độ đậm; class đang chọn scale 1,12.
Tuỳ chọn ChatGPT nêu nhưng chưa làm: rim light rất nhẹ ở vai / tóc class đang chọn; quái / bù nhìn trong khung xem trước để thấy đòn trúng.
