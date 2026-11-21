# Vòng 1 lặp camera Willowbrook (ChatGPT, 21/11/2026)

Chat (Tay "Tân Korris"): https://chatgpt.com/c/6b00f9a0-2b20-83ec-a0d4-361ae7da98ee . Ảnh gửi: `round-0-nohud.jpg`, `round-0-hud.jpg` (preset 58/27/69 của commit 482e98b8, 4 chỗ: phế tích 34,6, cầu 16,15, mép bắc 44,2, mép tây 3,24); so với 3 khung RO3 đã gửi trước trong chat (chỉ local).

PHÁN QUYẾT VÒNG 1: GIỮ camera 58°/27°. Bốn vị trí thử đều cho cảm giác nhìn xuống khá gần RO3 hơn bản 43° trước đây. Không cần đổi pitch nữa. Vấn đề còn lại chủ yếu là mép map, sprite vật thể thấp và chiều sâu cây.

Đánh giá: Camera đạt khoảng 8/10 so với hướng nhìn RO3 trong ba ảnh tham chiếu. Đây là điểm cảm nhận, không phải phép đo camera RO3.

1. Cỡ hero — CHỐT 69 px

Chọn 69 px tại 1280×720, cho cả bản test lẫn phát hành.

Không chấp nhận bản phát hành hero chỉ còn 60 px.

Giữ heroScale = 1 khi phát hành; chỉnh camera distance hoặc ViewScale để đạt 69 px, không phóng prefab lên 1.15.

Khóa 69 px tại mức zoom mặc định, không khóa kích thước khi người chơi chủ động zoom.

Đây là chiều cao từ đỉnh tóc đến chân, không tính tên, HP và vũ khí nhô cao.

Lý do: 60 px hơi nhỏ so với độ chi tiết sprite, đặc biệt khi chơi ở màn hình mobile. 69 px là điểm cân bằng giữa cảm giác RO3 và khả năng đọc animation.

2. Clamp camera — CHỐT HERO LUÔN Ở GIỮA

Chọn bỏ clamp camera theo mép map trong gameplay thông thường.

Tây/Đông: hero luôn ở screenX = 50% (640 px).

Bắc/Nam: chân hero luôn ở screenY = 54% (389 px).

Cho camera vượt giới hạn map để nhìn vào vùng rừng viền.

Không để nhân vật lệch sang 12% chiều ngang như ảnh mép tây.

Mép nam áp dụng đúng quy tắc này, không tạo trường hợp riêng.

Vùng ngoài map không cho đi vào và không sinh quái.

Lý do: Việc hero bị đẩy sang cạnh khi tới mép làm cảm giác camera thay đổi đột ngột, không giống trải nghiệm camera bám nhân vật đang cần xây dựng.

Điều kiện bắt buộc: vùng ngoài map phải đủ lớn để phủ toàn bộ camera frustum, kể cả ở mức zoom xa nhất.

3. Mép bắc — CHỌN DẢI RỪNG VIỀN RIÊNG

Không chấp nhận việc lật gương sân đá, đường đất hoặc nước.

Trong ảnh mép bắc, phần nền lát đá bị lặp khiến người chơi dễ nhận ra kỹ thuật mở rộng map.

Chốt:

Tạo một dải rừng viền chỉ gồm cỏ, cây, bụi và đá tự nhiên.

Độ rộng viền: 20 m từ mép playable, sau đó mở rộng thêm nếu camera ở mức zoom xa nhất vẫn nhìn thấy giới hạn.

Ground: 80–90% cỏ, 10–20% mảng đất/cỏ khác màu.

Cây lớn: 1 cây/20–30 m² ở phần rừng viền.

Bụi và cây nhỏ: 1 cụm/8–12 m².

Không có đường, cầu, tường, nhà, hồ hoặc sân đá lặp lại.

Không dùng mirror nguyên một cụm cây; thay đổi loại cây, scale ±10% và vị trí.

Áp dụng cho cả bốn mép và bốn góc map, không chỉ mép bắc. Dải viền chỉ render, không tham gia gameplay.

4. Sprite thấp/dài — CHỐT TIÊU CHÍ CHUYỂN SANG GROUND MESH

Đồng ý chuyển đá dài ven suối, khúc gỗ nằm ngang và vật thể thấp-dài sang biểu diễn nằm trên mặt đất.

Tiêu chí quyết định theo hình học thế giới:

Nếu chiều cao thực H ≤ 0.5 ô, đồng thời tỷ lệ chiều dài footprint so với chiều cao L/H ≥ 2.5 → chuyển sang ground-aligned mesh/decal.

Nếu H > 0.5 ô → giữ đứng hoặc dùng mesh 3D.

Nếu có mặt bên, độ dày, hốc hoặc cạnh đứng nhìn rõ → dùng mesh 3D thấp, không ép toàn bộ sprite nằm phẳng.

Áp dụng cụ thể:

Vật thể

	

Chọn

Đá dài ven suối

	

Mesh 3D thấp

Khúc gỗ lớn nằm ngang

	

Mesh 3D thấp

Khúc gỗ nhỏ rất dẹt

	

Ground-aligned mesh

Đá cuội nhỏ

	

Ground decal

Bụi cỏ, hoa

	

Ground patch hoặc crossed planes

Gốc cây

	

Giữ đứng hoặc mesh 3D

Cầu gỗ

	

Mesh 3D

Không xoay nguyên sprite khúc gỗ 90° nằm xuống đất, vì hình vẽ hiện tại đã chứa mặt trước và mặt bên. Làm vậy sẽ khiến hình bị bẹt.

5. Cây 3D thử nghiệm — CHỐT SPEC

Tạo đúng 2 cây mẫu, đặt cạnh cây sprite hiện tại, cùng chiều cao và bề rộng tán.

Thuộc tính

	

Cây tán tròn

	

Cây thông

Cụm tán

	

7 cụm chính + 12 cụm phụ

	

6 tầng tán

Hình học

	

Khối low-poly 8–12 mặt/cụm chính

	

6–8 mặt/tầng

Tổng triangles mục tiêu

	

500–900

	

300–600

Trunk

	

8 cạnh

	

6–8 cạnh

Shading

	

Toon 3 bậc

	

Toon 3 bậc

Outline

	

Có, rất nhẹ

	

Có, rất nhẹ

Outline ở 720p

	

0.7–1 px

	

0.7–1 px

Bảng màu lá — khởi điểm để khớp sprite hiện tại:

Vùng

	

Cây tán tròn

	

Cây thông

Sáng

	

#D9E47E

	

#B9D77A

Trung

	

#98BC64

	

#739F5C

Bóng

	

#557B4F

	

#486D50

Ánh nắng từ phía trên-trái màn hình, elevation 45°.

Mặt hướng nắng nhận màu sáng; mặt bên dùng màu trung; mặt khuất dùng màu bóng.

Soft toon, không dùng metallic.

Smoothness 0–0.05.

Bóng tự đổ trong tán có độ mềm vừa.

Outline nâu xanh tối, không dùng viền đen; có thể dùng inverted hull hoặc screen-space outline, ưu tiên độ dày theo pixel.

Giữ silhouette chibi tròn, các cụm lá lớn, không tạo hàng nghìn lá riêng lẻ.

Tiêu chí đạt: Ở kích thước hiển thị trong game, cây 3D phải hòa hợp với cây sprite, nhưng mặt trên và độ sâu tán rõ hơn. Nếu cây 3D giống đồ chơi nhựa thì chưa đạt, dù hình học đúng.

6. Camera cuối — GIỮ 58°/27°, chỉnh follow

Thông số

	

Chốt

Pitch từ mặt ngang

	

58°

Vertical FOV

	

27°

Hero mặc định

	

69 px / 720p

Hero screen X

	

50%

Hero feet screen Y

	

54%

Look-at height

	

Chân + 0.35 chiều cao hero

Camera follow

	

0.12 s

Zoom gần nhất

	

Hero 85 px

Zoom xa nhất

	

Hero 55 px

Zoom mặc định

	

Hero 69 px

Đổi FOV khi zoom

	

Không

Đổi pitch khi zoom

	

Không

Zoom bằng thay đổi camera distance.

Dùng khoảng cách được hiệu chỉnh theo pixel height thực tế, không đặt cứng một số mét nếu scale map khác.

Follow nên dùng SmoothDamp với thời gian làm mượt 0.12 s, không sử dụng một hệ số Lerp phụ thuộc frame rate.

Vị trí look-at 0.35 chiều cao hero là giá trị khởi tạo hình học; offset screen-space cần ưu tiên để giữ chân ở đúng 54%.

Lý do giữ pitch/FOV: Ở ảnh cầu và phế tích, tỷ lệ diện tích mặt đất, cây và nhân vật đã tương đối hợp lý. Tiếp tục đổi pitch lúc này dễ làm sai những vị trí đang tốt.

7. Những điểm khác cần sửa ngay

Theo thứ tự ưu tiên:

1. Cây tán lớn đang che màn hình nhiều hơn cần thiết. Ảnh phế tích và mép bắc có những cụm cây dày khiến không gian giống bản đồ vẽ hơn môi trường 3D. Giảm khoảng 10% diện tích che khuất tại các lối đi, không giảm toàn bộ cây.

2. Bóng dưới cây còn quá đều và mềm. Bổ sung bóng tán có hình dáng lá, có hướng nắng. Giữ contact shadow nhưng không để mọi cây có cùng một đốm tối tròn.

3. Tên và thanh HP quái chồng nhau. Ảnh phế tích có Rabbit chồng Cleric, làm giảm độ rõ nhân vật. Đây không phải lỗi camera nhưng ảnh hưởng cảm giác chơi. Gộp/ẩn nhãn khi quái gần nhau, ưu tiên nhãn mục tiêu đang chọn.

4. Các sprite thấp và dài vẫn tạo cảm giác vật thể đứng. Sửa khúc gỗ, đá dài và các vật nằm ngang trước khi tiếp tục thay camera.

5. Cây xa vẫn hơi sắc và bão hòa như cây gần. Giảm contrast khoảng 5–8% và saturation 3–5% theo khoảng cách, nhưng giữ cây quanh hero sắc nét.

6. HUD có banner tên map quá lớn khi xuất hiện. Giữ hiệu ứng vào vùng nhưng fade hoàn toàn sau 2–3 giây. HUD PC mới đã phù hợp hơn bản nút Attack tròn trước đây, không cần thay layout nữa ở vòng này.

CHỐT VÒNG 1: Giữ 58° / FOV 27° / hero 69 px / chân Y 54%. Bỏ clamp ở cả bốn mép, tạo dải rừng viền riêng, chuyển vật thể thấp-dài thành ground mesh hoặc mesh 3D thấp, thử hai cây 3D như spec trên.

Không chỉnh camera thêm trong vòng tiếp theo. Ưu tiên giải quyết độ phẳng của sprite và mép map; đó mới là các yếu tố đang giới hạn cảm giác RO3 của bản hiện tại.

