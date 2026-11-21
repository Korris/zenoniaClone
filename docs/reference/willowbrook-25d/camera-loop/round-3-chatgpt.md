# Vòng 3 lặp camera Willowbrook: nghiệm thu (ChatGPT, 21/11/2026)

Chat (Tay "Tân Korris"): https://chatgpt.com/c/6b00f9a0-2b20-83ec-a0d4-361ae7da98ee . Ảnh gửi: `round-3-low-props.jpg` (cầu trước / sau khi vẽ lại đá ven suối + khúc gỗ cho góc 58°, suối, mép tây), `round-3-hud-edges.jpg` (có HUD, mép bắc / nam / góc ĐB với rừng viền v2). Sheet vẽ: `Assets/ArtLibrary/maps/willowbrook-meadows/forest-25d/willowbrook-low-props-58-chatgpt-1.png`.

PHÁN QUYẾT VÒNG 3 — NGHIỆM THU CUỐI

Kết luận: ĐẠT. Chốt Willowbrook V3 làm chuẩn camera và art direction 2.5D cho toàn game. Không cần chỉnh thêm camera hay vẽ lại bộ prop này.

1. Đá + gỗ mới: ĐẠT — 9/10

Đá ven suối: Bản mới tốt hơn rõ rệt. Các khối đá thấp, mặt trên dễ nhìn hơn, không còn cảm giác một bức tường đá dựng đứng như bản cũ.

Khúc gỗ: ĐẠT. Hình dáng, rêu và màu gỗ hòa hợp với map. Mặt trên rõ hơn, phù hợp góc 58°.

Kích thước: GIỮ 100% như ảnh sau chỉnh.

Màu và độ đậm: GIỮ, không giảm saturation.

Bóng tiếp đất: Chốt contact shadow #586B61, opacity 22%, mép mềm, nằm dưới footprint. Không thêm bóng dài cho các prop thấp.

Collision: Giữ nguyên. Gỗ trang trí thấp cho phép đi xuyên như hiện tại.

2. Rừng viền V2: ĐẠT — 9/10

Quan sát cả mép bắc, nam và góc đông bắc: không còn thấy mép map hay đường lật gương lộ liễu. Rừng viền hòa khá tự nhiên với phần playable.

Chốt toàn bộ thông số hiện tại:

Viền rộng 22 m.

Fade vào map 2 ô.

Mật độ cây 8 m đầu: 1/35 m².

Mật độ bụi/đá: 1/12 m².

Mép nam: 6 m đầu chỉ dùng bụi thấp.

Chừa 6 ô quanh cổng.

Không lặp gương đường, nước hoặc công trình.

Không cần tăng mật độ cây nữa, vì một số góc rừng đã khá dày.

3. Camera cuối — KHÓA PRESET 2.5D V3

Thông số

	

Giá trị chốt

Projection

	

Perspective

Pitch

	

58° từ mặt ngang

Vertical FOV

	

27°

Hero height mặc định

	

69 px tại 720p

Hero screen X

	

50%

Hero feet Y

	

389 px / 720p (54%)

Look-at height

	

Chân + 0,35 × hero height

Smooth follow

	

0,12 s

Zoom gần nhất

	

80 px hero

Zoom xa nhất

	

60 px hero

Clamp ở mép map

	

OFF cả bốn phía

Đổi pitch/FOV khi zoom

	

Không

Camera distance

	

Tự tính để đạt hero px mục tiêu

Áp dụng với heroScale = 1 trong bản phát hành. Không phóng sprite hero để bù zoom.

Không chỉnh pitch, FOV hoặc hero size thêm trong các vòng polish map thông thường.

4. Quy tắc art prop cho mọi map 2.5D

Vẽ theo góc nhìn 58°: Áp dụng cho đá, gỗ, rương, bàn, thùng, bệ đá, vật trang trí có thể nhìn thấy mặt trên.

Đá thấp/dài: Mặt trên chiếm khoảng 35–45% chiều cao hiển thị; ưu tiên các khối ngắn thay vì một vách dài dựng đứng.

Khúc gỗ nằm ngang: Mặt trên thấy rõ, khoảng 25–30% đường kính hiển thị; đầu cắt dạng elip.

Vật cao đứng: Cây, cột, tượng, biển chỉ đường tiếp tục dùng sprite dựng đứng có chân neo trên ground.

Vật thấp nằm trên đất: Ưu tiên sprite góc cao phù hợp camera, không tự động xoay phẳng toàn bộ sprite.

Ánh sáng: Nắng trên-trái 45°, bóng tiếp đất 20–30% opacity tùy vật.

Phong cách: Giữ RO Soft Fantasy, không dùng low-poly 3D nếu chưa đạt độ hòa hợp với sprite.

Kích thước: Ưu tiên footprint trong world-space, không scale tùy tiện để vật vừa khung hình.

5. Điểm cuối và việc còn lại

Hạng mục

	

Vòng 2

	

Vòng 3

Camera so với RO3

	

8,8/10

	

9,0/10

Cảm giác RO3 tổng thể

	

8,2/10

	

8,6/10

Art prop đá/gỗ

	

7,5/10

	

9,0/10

Rừng viền

	

8,5/10

	

9,0/10

Điểm ước lượng theo cảm nhận từ các ảnh đã gửi, không phải độ tương đồng kỹ thuật đo được với RO3.

Hai việc còn lại có tác động lớn nhất:

Cây che nhân vật: Ở ảnh mép nam, Cleric bị tán cây làm mờ khá nhiều. Nên giới hạn vùng dither chỉ quanh phần sprite che hero, giữ thân cây và phần tán còn lại rõ. Mục tiêu hero vẫn nhận diện được khi đứng giữa rừng dày.

Ánh sáng và chiều sâu tán cây: Các cụm tán vẫn hơi đồng đều về sáng tối. Có thể tăng chênh lệch giữa mặt lá nhận nắng và phần khuất khoảng 8–10% value, nhưng không đổi màu chủ đạo hay texture.

CHỐT: Willowbrook Meadows 2.5D V3 được nghiệm thu. Camera, rừng viền và bộ đá/gỗ đã đủ tốt để dùng làm tiêu chuẩn sản xuất các map tiếp theo. Hai mục còn lại là polish về render/occlusion, không phải lý do để mở lại thiết kế camera.

