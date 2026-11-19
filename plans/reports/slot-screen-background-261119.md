# Nền màn chọn slot nhân vật (19/11/2026)

- Tài khoản **Tan Pham** (Plus): đọc `/api/auth/session` trước lần gửi, ra "Tan Pham".
- Chat mockup: https://chatgpt.com/c/6afe9ba5-6050-83ec-9020-3add7c282954, tab riêng.
- Gửi kèm `docs/reference/character-select-ro-fantasy/mockup/01-slot-screen-chatgpt-1.png`.

## Prompt (1 dòng)
Vẽ mới hoàn toàn từ đầu, chỉ NỀN phong cảnh của màn trong ảnh: sân đá sáng, lan can đá, cột/cổng đá treo cờ xanh hoa huệ, lâu đài nhiều tháp, cầu đá, thác nước, trời xanh mây trắng, hoa cây ở góc; KHÔNG nhân vật, bệ, UI, chữ, nút, khung/ô slot; giữa và nửa dưới thoáng; câu tông RO Soft Fantasy đủ (sáng, ấm, pastel dịu, viền nâu đậm mềm, bóng 2-3 cấp, không neon, không trầm tối); 16:9, độ phân giải lớn nhất.

## Kết quả
| File | Ghi chú |
|---|---|
| `Assets/ArtLibrary/ui/character-select/slot-screen-background-chatgpt-1.png` | 1672x941 (16:9), gốc tải bằng blob |
| `Assets/Resources/Sprites/ui/character-select/slot-screen-background.png` (+ `.meta`) | Chép nguyên cỡ; `.meta` chép từ `info-panel.png.meta`, guid mới, không nén |

Đã mở xem: không nhân vật, không UI, không chữ. Tông sáng ấm như mockup. Hai cột cờ hoa huệ ở 2 mép, lâu đài + cầu đá bên trái + thác bên phải ở nửa trên, lan can ngang khoảng 45-55% chiều cao. Nửa dưới là sân đá trống, giữa có hoa văn la bàn mờ trên nền gạch (chỗ đặt bệ/nhân vật). Hoa ở 4 góc. Nhận ngay lần 1, không vẽ lại.

## Lượt vẽ
1 ảnh trên Tan Pham, không A/B, không hết lượt.

## Ghi chú
- **Game chưa dùng ảnh này:** `CharacterFlowKit.Backdrop` (`Assets/Scripts/CharacterFlowKit.cs:33`) vẫn nạp `UiArt.Get("hero-select/bg")`. Cần sửa code sang `character-select/slot-screen-background` (việc này chưa làm vì brief cấm sửa code). Lưu ý Backdrop dùng chung cho cả màn tuỳ chỉnh nhân vật.
- Tan Pham giao diện tiếng Việt: nút gửi là `aria-label="Gửi"` (hàm `__send` của brief tìm "Send" nên không bấm được).
- Không có lỗi sprite mới nên không thêm Sổ lỗi.

## Câu hỏi
1. Đổi nền cho cả màn tuỳ chỉnh nhân vật (dùng chung Backdrop) hay chỉ màn chọn slot?
