
# Coding Agent Instructions

## General
- Act as a senior software engineer.
- Read relevant project files before modifying code.
- Follow the existing architecture and conventions.
- Prefer modifying existing files over creating unnecessary files.
- Do not introduce new dependencies unless necessary.

## Workflow
Read `docs/session-handoff.md` at the start of a new session for the latest project state and user decisions; verify against the current code before editing.

1. Understand the request.
2. Inspect related files and dependencies.
3. Implement the solution.
4. Run relevant tests or build commands.
5. Fix errors introduced by the changes.
6. Summarize modified files and test results.

## Rules
- Do not modify unrelated code.
- Never expose or overwrite secrets in .env files.
- Do not delete data or run database migrations without approval.
- Do not push commits or deploy without approval.
- Ask before making major architectural changes.
- Respond in Vietnamese.

## Art direction — bắt buộc cho mọi thiết kế
- **Art style (Phong cách đồ họa): Đồ họa 2D sắc nét, thiết kế nhân vật theo phong cách anime/manga dễ thương nhưng kịch tính, kết hợp với góc nhìn từ trên xuống (top-down view) cổ điển.**
- Game có phong cách anime/manga chibi kiểu Zenonia. Mọi nhân vật, quái, NPC, item, icon skill, hiệu ứng, môi trường và UI mới phải đồng bộ với nhân vật và tài nguyên chibi hiện có.
- Không chuyển sang mỹ thuật fantasy phương Tây/châu Âu: tránh phong cách Diablo/WoW, hiện thực u tối, giáp nặng cầu kỳ, huy hiệu kim loại hoành tráng, hoa văn gothic/baroque và icon fantasy quá nghiêm nghị. Không tự thêm các đặc điểm này chỉ vì yêu cầu "vẽ tay", "HD" hoặc "sắc nét".
- Icon skill phải có hình khối gọn, đường nét sạch, màu tươi, sắc thái dễ thương nhưng có lực, biểu tượng đơn giản dễ đọc ở kích thước nhỏ. Không làm như tranh minh họa sử thi thu nhỏ, tránh nhồi chi tiết, ánh kim và viền trang trí nặng nề.
- Hiệu ứng phải mang ngôn ngữ anime: nét chém rõ, chuyển động có lực, phép màu sáng gọn, không che nhân vật quá mức; không dùng mây khói/glow mờ để thay thế hình hiệu ứng.
- Khi gen ảnh, ghi rõ phong cách trên vào prompt, dùng nhân vật/item chibi hiện có làm tham chiếu chính. Ảnh người dùng gửi dùng để giữ đúng bố cục/hành vi và phong cách họ yêu cầu, không tự diễn giải thành một thể loại game khác.
- Đánh giá ảnh ở kích thước thật trong game, đặt cạnh hero và UI trước khi nhận là hoàn tất. Ảnh đẹp riêng lẻ chưa đủ nếu lệch phong cách tổng thể.
- Bộ `skill-icons-*-painted.png` vừa tạo ngày 2026-10-27 đã bị người dùng phản hồi là lệch sang game châu Âu. **Không xem bộ này là mẫu phong cách đã được chấp nhận** hoặc dùng nó làm chuẩn để gen tiếp. Xem `README.md` phần Art direction và ghi chú mới nhất trong `docs/session-handoff.md`.
