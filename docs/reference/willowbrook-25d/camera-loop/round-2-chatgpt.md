# Vòng 2 lặp camera Willowbrook (ChatGPT, 21/11/2026)

Chat (Tay "Tân Korris"): https://chatgpt.com/c/6b00f9a0-2b20-83ec-a0d4-361ae7da98ee . Ảnh gửi: `round-1-edges.jpg` (không clamp + rừng viền, 4 mép / góc), `round-1-mesh-ab.jpg` (sprite trái / thử 3D phải: cây tán tròn + thông, cụm đá thấp ven suối, khúc gỗ trụ; bảng màu v2 lấy từ sprite), `round-1-hud.jpg` (có HUD, bảng màu 3D v1 theo spec vòng 1, mép tây).


Mép map + rừng viền: ĐẠT, GIỮ.

Chốt dùng cho mọi map 2.5D sau này.
Giữ dải viền 22 m như hiện tại.
Sửa duy nhất 2 số:
tăng vùng fade vào map từ 1,5 ô → 2,0 ô,
giảm mật độ cây trong 8 m đầu sát mép playable từ 1/25 m² → 1/35 m².
Bụi + đá viền giữ nhưng giảm nhẹ từ 1/10 m² → 1/12 m² ở 8 m đầu.
Giữ rule mép nam 6 m đầu chỉ bụi thấp, đúng.

Cây 3D: BỎ.

Phán quyết rõ: cây 3D test hiện tại không tốt hơn cây sprite.
Lý do: đọc thành low-poly ngoại lai, khối quá “game prototype”, phá phong cách RO Soft hiện có.
Baseline chính thức của Willowbrook và các map 2.5D sau này: tiếp tục dùng cây sprite.
Nếu sau này quay lại 3D cây thì phải là nhánh R&D riêng, không đưa vào production loop hiện tại.

Đá dài + khúc gỗ 3D: BỎ.

3D test hiện tại vẫn quá thô, chưa hòa với map.
Cách thay thế chốt: giữ 2D nhưng vẽ lại asset cho đúng pitch 58°, không dùng mesh 3D thấp, không cho “nằm phẳng bù 1/sin58”.
Cụ thể:
đá dài ven suối → tách thành 3–4 sprite khối ngắn, mỗi khối có mặt trên thấy rõ 35–45% chiều cao khối;
khúc gỗ → sprite mới với miệng cắt elip thấy rõ, phần mặt trên thân gỗ thấy khoảng 25–30% đường kính;
tất cả asset thấp-dài này thêm contact shadow 22–28% opacity.
Tức là: không đổi loại đối tượng sang 3D, mà đổi bộ vẽ 2D cho phù hợp camera 58°.

Camera: GIỮ preset hiện tại, chỉ siết zoom.

Giữ nguyên: pitch 58°, FOV dọc 27°, hero 69 px, chân hero y = 389, no clamp, SmoothDamp 0,12 s.
Chỉ đổi 1 mục: thu hẹp zoom bánh xe từ 85..55 px → 80..60 px.
Không đổi FOV/pitch theo zoom.
Không đổi look-at kiểu lớn; chốt look-at height = chân + 0,35H.
Phần còn chưa giống RO3 giờ không còn nằm ở camera chính, mà nằm ở asset thấp-dài và một số cụm cây foreground quá dày.

Chấm điểm:

Camera: 8,8/10
Cảm giác RO3 tổng thể: 8,2/10
So với vòng 1: tăng khoảng +0,8 điểm cảm giác RO3.
Kết luận ngắn: camera đã chốt được rồi; từ vòng sau nên ngừng xoay camera, tập trung sửa asset 2D theo pitch 58° và giữ hệ rừng viền này làm chuẩn cho các map tiếp theo.
