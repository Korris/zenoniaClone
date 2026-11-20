# Warrior RO Fantasy đòn chém 8 hướng x 8 frame: art (20/11 19:00)

## Kết quả: DỪNG, 0/8 hướng

- Chrome kết nối duy nhất: "Browser 1" (deviceId cb671945). `/api/auth/session` = "Tân Korris" (Tay, Plus) - Tay bị chặn ảnh tới 21:32.
- Tan Pham không kết nối sau 3 lần kiểm (18:58, 18:59, 19:01). Không gửi prompt, không tốn lượt, không mở chat mới.
- Không cài gì, không đổi code, không build, không chạy test (không có code đổi).

## Đã chuẩn bị cho lần sau

- Mẫu cỡ idle cùng hướng (quy tắc ChatGPT 8): `Assets/ArtLibrary/ro-fantasy-heroes/ref/warrior-attack8/idle-{down,down-right,right,up-right,up,up-left,left,down-left}.png`, cắt từ `src/body-warrior-idle-5dir-chatgpt-1.png`; 3 hướng trái là ảnh lật (chỉ dùng làm mẫu cỡ).
- Đo đầu / người trên idle: 0,41-0,43 (xuống 0,43, chéo xuống 0,42, ngang 0,41, chéo lên 0,42, lên 0,43).
- `prompts/warrior-attack-8dir.md`: thêm mục ảnh gửi kèm + câu mô tả từng hướng (3 hướng trái ghi rõ kiếm ở tay PHẢI = tay xa, không lật).

## Làm tiếp

1. Mở Chrome đăng nhập Tan Pham (hoặc dùng Tay sau 21:32), kiểm `/api/auth/session`.
2. Chat mới, tin đầu gửi kèm idle cùng hướng + `docs/reference/hero-idle-down-5-classes.png` + 2 ảnh tông; mỗi hướng 1 ảnh, lưu `src/body-warrior-attack-<hướng>-8f-chatgpt-N.png`.
3. Cài theo bước 1-3 của file prompt, ghép kiểm tra lưu `docs/reference/warrior-attack-8dir/`.

## Câu hỏi chưa giải quyết

- Người dùng mở Chrome Tan Pham cho agent, hay cho phép vẽ bằng Tay sau 21:32?
