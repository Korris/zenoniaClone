# Warrior T0 idle hướng xuống: thử A / B / C (18/11/2026)

Bước 4 ChatGPT đề xuất khi hỏi có nên vẽ lại nhân vật cho hợp RO Soft Fantasy (xem cuối `docs/reference/ui-style-ro-soft-fantasy/README.md`). Người dùng: "làm cái 4 trước cho warrior đi", "làm sprite down trước". CHƯA cài vào game.

- A `warrior-t0-idle-down-A.png`: frame hiện tại, ghép như game vẽ (`Tools/compose-hero-frame.py`: idle, down, frame 0).
- B `...-B.png`: A chỉ chỉnh màu bằng Python: độ bão hoà x0,87, tương phản x0,90, nâng tối 4 %.
- C `...-C.png`: ChatGPT (Tay, chat https://chatgpt.com/c/6af57fff-c4f8-83ec-8f05-0c4eb76c4cde) vẽ mới theo "Soft Action Chibi", gửi A phóng 4x trên `#00FF00` + `docs/reference/hero-idle-down-5-classes.png`, giữ tư thế / kiếm / khiên / tay / cỡ đầu; nền xanh tách bằng độ dư xanh lá, co về cùng chiều cao A. Bề ngang đầu C / A = 0,978 (trong mức 5 %). Ảnh gốc `...-C-chatgpt-raw.png`.
- `warrior-t0-A-B-C-on-maps-game-size.jpg`: cả 3 đặt lên ảnh chụp map tuyết / rừng / làng ở camera 192, cỡ game (0,703 px màn hình / px sprite ở 1080p; trong ảnh tuyết và làng có hero thật cùng cỡ để so).
- `warrior-t0-A-B-C-closeup-2x.png`: phóng 2x trên nền xám.

Nếu chọn C: cần vẽ đủ idle 4 hướng (thở), walk, 4 đòn, hurt, dash, skill pose; tách đầu (headless + anchor) để 6 kiểu tóc nhuộm vẫn chạy; kiếm / khiên vẫn là overlay theo bậc (C đang vẽ liền kiếm khiên T0 nên chỉ là mẫu nét, chưa dùng thẳng được).
