# Màn chọn slot nhân vật RO Soft Fantasy: mockup + mảnh UI (19/11/2026)

- Tài khoản **Tan Pham** (Plus), đọc `/api/auth/session` trước mỗi lần gửi: luôn "Tan Pham".
- Chat mới: https://chatgpt.com/c/6afe9ba5-6050-83ec-9020-3add7c282954 ("Vẽ mockup chọn nhân vật").
- Ảnh gửi kèm ở tin đầu: `sample-hero-user-pick.png`, `01-style-board.png`, `13-class-select-level-up.png` (ghi rõ chỉ tham khảo không khí).

## Ảnh đã nhận

| Ảnh | File | Ghi chú |
|---|---|---|
| Mockup | `docs/reference/character-select-ro-fantasy/mockup/01-slot-screen-chatgpt-1.png` (1672x941, 16:9) | Nhận ngay lần 1 |
| Mảnh UI | `Assets/ArtLibrary/ui/character-select/slot-ui-kit-chatgpt-2.png` (1536x1024, nền `#FF00FF`) | Vẽ lại 1 lần |

**Mockup** (đã mở xem):
- tiêu đề "Chọn nhân vật", nút "Đổi tài khoản" góc trên trái;
- hàng 6 slot: 3 có nhân vật trên bệ (Ánh Dương - Kiếm sĩ - Cấp 32, Tuyết Linh - Pháp sư - Cấp 27, Thiên Ân - Tu sĩ - Cấp 15), 3 trống "+ Tạo nhân vật";
- slot 1 viền vàng sáng; nhân vật đó hiện lớn trên bệ, khung thông tin Tên / Class / Cấp / Bản đồ cuối: Làng Khởi Đầu;
- nút "Tạo nhân vật", "Xoá", "Vào game" (nút chính, xanh lá);
- nền lâu đài, cầu đá, mây, tông sáng ấm. Chữ tiếng Việt đúng dấu. Không có chọn mũ.
- Điểm nhỏ: slot 6 sát mép phải; nhân vật Wizard cầm sách (art tham khảo, không phải sprite game).

**Mảnh UI bản 2** (đã mở xem + đo bằng scipy): đúng 7 món tách rời, không chữ, không nhân vật. Cách nhau ≥ 48 px, cách mép ≥ 30 px. Mép sắc, không quầng, không bóng ra nền.
1. khung slot thường; 2. khung slot đang chọn (viền vàng, đá đỏ); 3. khung slot trống có "+" và vòng nguyệt quế;
4. bệ đá tròn; 5. khung thông tin 9-slice (giữa trơn, góc trang trí, có hoa văn nhỏ giữa cạnh dưới); 6. dải tiêu đề xanh có cánh; 7. khung nhãn tên xanh.

## Ảnh vẽ lại

| Ảnh | Lỗi | Kết quả |
|---|---|---|
| Mảnh UI bản 1 (đã xoá) | đá đỉnh khung đang chọn cách mép trên 1 px; khung thông tin cách dải tiêu đề 21 px; 3 khung slot dính bệ đá bên trong | Bản 2 hết cả 3 lỗi |

Đã ghi 1 dòng Sổ lỗi trong `docs/sprite-rules.md`.

## Lượt vẽ
3 ảnh trên Tan Pham, không hết lượt, không A/B.

## Câu hỏi
1. Khung thông tin có hoa văn nhỏ giữa cạnh dưới: cắt 9-slice thì cạnh dưới sẽ lặp hoa văn; cần vẽ lại cạnh trơn hay cắt riêng hoa văn?
2. Nhãn tên trong mockup có 2 màu (đỏ cho slot chọn, xanh cho slot khác), tấm mảnh chỉ có bản xanh: có cần thêm bản đỏ không?
