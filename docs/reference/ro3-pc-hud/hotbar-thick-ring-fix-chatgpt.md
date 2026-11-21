# Thanh skill PC: viền dày, khó nhìn (ChatGPT, 21/11/2026)

Chat (Tay): https://chatgpt.com/c/6aff0d5c-f0a0-83ec-8726-10aa0543ada0 . Ảnh gửi: warrior-hotbar-ingame-1280x720.jpg, warrior-hotbar-zoom3x.jpg.

**Đồng ý, nên thay khung skill tròn hiện tại.** Vấn đề lớn nhất là viền kem quá dày, nhãn phím che phần trên icon và lớp phủ tím làm 4 skill chưa học gần như không nhận diện được. **Giữ khay navy và bố cục 8 skill**, chỉ sửa cách thể hiện từng ô.

### 1. Kích thước và viền skill (tại 1280×720)

- **Đường kính ô:** giữ 46 px, không cần tăng vì thanh đã vừa màn hình.
- **Viền hiện tại:** ước lượng 5–7 px, quá dày.
- **Viền mới:** chỉ **2 px**, tối đa 2.5 px.
- **Màu viền:** bỏ vành kem dày, dùng viền xanh thép nhạt `#A8C5D8`, thêm highlight trắng ngà 1 px ở cung trên.
- **Đường kính icon bên trong:** 42 px, chiếm **91% đường kính ô** thay vì khoảng 70–75% hiện tại.
- **Skill thường:** viền xanh thép 2 px.
- **Skill đang chọn:** viền vàng nhạt `#F4C66A` 2.5 px, không phát sáng mạnh.
- **Skill có thể nâng cấp:** thêm chấm vàng nhỏ 5 px ở góc trên-phải, không đổi toàn bộ viền thành vàng.
- **Khoảng cách giữa 8 ô:** 6 px mép–mép; giữ như mockup, không cần nén thêm.
- **Khay navy:** giữ kích thước hiện tại, giảm độ đục nền xuống khoảng **75–80%**, nhưng giữ viền ngoài trắng xanh 95–100% để thanh vẫn nổi trên map.

### 2. Nhãn phím 1–8

**Nên chuyển nhãn phím xuống góc dưới-phải ô.** Vị trí hiện tại che hình skill và tạo một hàng badge xanh quá dày.

- Badge mới: **15×13 px**, bo góc 4 px.
- Font: 9–10 px, semibold, màu trắng.
- Nền badge: navy `#32566B`, opacity 90%.
- Đặt chồng lên góc dưới-phải, phần badge nhô ra khỏi vòng khoảng 2–3 px.
- Không đặt nhãn tại đỉnh ô nữa.
- Badge Q/E của bình thuốc cũng chuyển xuống góc dưới-phải để đồng bộ.
- Badge **Space** giữ phía dưới Attack vì chữ dài; V và Shift có thể giữ phía trên hai nút riêng biệt hoặc chuyển xuống góc dưới-phải theo cùng quy tắc.

### 3. Hiển thị trạng thái skill

| Trạng thái | Cách hiển thị mới |
|---|---|
| **Đã học / sẵn sàng** | Icon màu nguyên bản 100%, không phủ màu, viền xanh thép mảnh |
| **Chưa học (ô 5–8)** | Icon vẫn giữ **70–75% độ sáng**, giảm saturation 20–30%; không phủ tím đậm. Thêm ổ khóa nhỏ 10 px ở góc dưới-trái |
| **Đang hồi chiêu** | Phủ radial cooldown màu navy đen alpha **40–45%**, quét theo chiều kim đồng hồ; số giây trắng 13–14 px có outline 1 px ở tâm |
| **Thiếu MP** | Giữ icon sáng 80%, phủ xanh navy alpha 15–20%, thêm biểu tượng giọt MP nhỏ 10 px có dấu gạch; không phủ tối toàn icon |
| **Không có mục tiêu / ngoài tầm** | Giữ icon rõ, giảm sáng 10–15% và hiển thị phản hồi ngắn khi bấm; không biến icon thành màu xám tối |
| **Ô trống** | Lõi xanh xám nhạt `#D8E2E8`, dấu sao mờ opacity 25%, viền 2 px |

**Quan trọng:** Skill chưa học không nên dùng chung lớp phủ tím tối với cooldown. Hai trạng thái phải nhận biết khác nhau ngay lập tức.

### 4. Bình thuốc, Attack, Auto, Run

- **Bình HP/MP:** giữ vòng 46 px, viền kem có thể giữ nhưng giảm còn 2–3 px. Chai bên trong chiếm khoảng 80–85% đường kính ô.
- **Attack:** giữ 58 px và viền trắng ngà hiện tại vì đây là nút chính, cần nổi hơn skill.
- **Auto/Run:** giữ 44 px, không cần thay asset.
- **Vạch phân nhóm:** giữ nét trắng ngà 2 px; tăng opacity vừa đủ, không làm sáng hơn icon skill.

### 5. Mảnh khung skill mới cần vẽ lại

Khi làm sheet tiếp theo, chỉ cần thay **2 mảnh**:

1. **Khung skill thường:** vòng tròn rỗng, đường kính khoảng 200 px trên sheet (xuống game 46 px), viền xanh thép 2 px ở tỷ lệ game, highlight trắng ngà mảnh ở phía trên; **lỗ giữa hoàn toàn #FF00FF** để icon bên dưới hiện đủ lớn.
2. **Khung skill trống:** cùng kích thước, viền xanh thép, lõi xanh xám nhạt, một dấu sao 4 cánh mờ ở tâm.

Có thể bổ sung **khung viền vàng đang chọn** cùng hình học để đổi state bằng sprite.

**Chốt:** 46 px ô, 42 px icon, viền 2 px xanh thép, nhãn phím 15×13 px ở góc dưới-phải, skill chưa học vẫn sáng 70–75%, cooldown tối tối đa 45%. Đây là thay đổi đáng làm nhất: thanh sẽ bớt cảm giác như một dãy đồng xu viền kem, icon skill tròn của Cleric/Warrior sẽ trở thành điểm nhấn chính và nhìn rõ hơn nhiều ở 1280×720.
