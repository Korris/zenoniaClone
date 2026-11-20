# Midgard Senki vs FX Cleric: vì sao họ dùng FX trong suốt trên nền pastel sáng được (ChatGPT, 20/11/2026)

Chat (Tay, chỉ chữ): https://chatgpt.com/c/6b003947-a9a8-83ec-a9bf-b60212cc8de1 . ChatGPT chỉ xem ảnh PvP + bài viết, không xem frame video; số liệu là ước lượng nhìn ảnh.

## Kết luận: KHÔNG cần tối map ngay. Nên sửa FX trước.

Sau khi tìm thêm ảnh gameplay và tài liệu về **Ragnarok: Midgard Senki**, mình điều chỉnh khuyến nghị trước: **giữ nguyên RO Soft Fantasy sáng là lựa chọn ưu tiên**, ít nhất cho đến khi thử lại Heal và Spirit Blessing với shader mới.

Midgard Senki là ví dụ rất đáng tham khảo: game chủ đích sử dụng phong cách 2D paper-cut, màu sáng và nhân vật dễ thương, nhưng vẫn có những vòng năng lượng xanh-tím, tia sáng và phép lửa nổi rõ trên nền cát vàng. 

Nguồn chính đã kiểm tra:

- [Online Station — ảnh PvP Midgard Senki trên nền cát sáng](https://www.online-station.net/mobile-game/996505)
- [Thisisgame — mô tả phong cách 2D sáng của Midgard Senki](https://thisisgamesea.com/news/pr-news/ragnarok-midgard-senki-h1-2027-launch-mvp-pets-classes-pvp/)
- [Google Play — thông tin và hình ảnh Midgard Senki](https://play.google.com/store/apps/details?id=com.rojourney.and)
- [Midgard Senki — Test Gameplay](https://www.youtube.com/watch?v=_ovB_Pj8l14)

**Giới hạn:** Mình quan sát được ảnh gameplay PvP, nhưng chưa xem được video theo từng frame. Các con số bên dưới là ước lượng thị giác và preset đề xuất, không phải thông số shader đã đo từ Midgard Senki.

## 1. Map Midgard Senki sáng đến đâu so với game bạn?

| Đặc điểm | Midgard Senki | Soft Fantasy của bạn |
|---|---|---|
| Value nền chính | Khoảng 75–90% ở ảnh cát | Khoảng 80–95% ở tuyết sáng |
| Saturation | Vừa, cát vàng ấm | Thấp-vừa, tuyết trắng xanh |
| Độ tương phản | Có bụi cây, vật thể tối ngắt nền | Tuyết phẳng khá đồng đều |
| Vignette | Không thấy rõ trong ảnh PvP | Không thấy rõ |
| Bóng đổ / vùng tối | Có các mảng cây, vật thể tối | Có nhưng ít quanh Cleric |

*Value là ước lượng từ ảnh hiển thị, không phải kết quả lấy mẫu pixel đã chuẩn hóa.*

**[Quan sát]** Điểm khác đáng chú ý không phải Midgard Senki tối hơn nhiều, mà là nền cát của họ **vàng ấm**, trong khi hiệu ứng chính **xanh-tím lạnh**.

Nền tuyết game bạn lại trắng xanh, cùng họ màu với Spirit Blessing và Heal. Vì vậy dù Value gần nhau, độ tách biệt màu kém hơn.

**[Suy luận]** Đây có thể là một trong những nguyên nhân quan trọng nhất khiến FX Cleric của bạn bị chìm.

## 2. FX Midgard Senki khác Cleric của bạn thế nào?

Đối chiếu ảnh PvP Midgard Senki với các frame Spirit Blessing và Heal bạn gửi:

| Tiêu chí | Midgard Senki quan sát được | Cleric hiện tại |
|---|---|---|
| Màu chủ đạo | Xanh tím, cam, màu khá mạnh | Mint nhạt, trắng, vàng kem |
| Hình dạng | Vòng năng lượng có mép rõ | Vòng phép khá mảnh và nhạt |
| Lõi màu | Có mảng tím/xanh đậm | Lõi bị sáng trắng che |
| Quầng ngoài | Mềm, tập trung quanh mép | Một số vùng sáng lan rộng |
| Kích thước | Có vòng lớn, nhưng phần sáng mạnh không phủ kín vòng | Heal có lúc phủ sáng gần toàn thân |
| Bloom | Hình ảnh không quá mờ trắng | Có frame bị cháy trắng |
| Độ trong | Nền vẫn nhìn thấy qua nhiều vùng hiệu ứng | Có vùng additive chồng sáng mạnh |

**[Quan sát]** Vòng xanh-tím của Midgard Senki có phần mép sáng rõ nhưng phần trong tối hơn. Vùng sáng không đồng đều trên toàn bộ diện tích.

**[Suy luận]** Cách làm này giúp người chơi nhìn thấy hình dạng skill mà không cần đẩy Bloom lên cao.

Về **thời lượng và số hạt**, ảnh tĩnh không đủ để so sánh chính xác. Mình không thể khẳng định Midgard Senki dùng ít particle hơn bạn hay thời gian flash ngắn hơn; đây chỉ là hai hướng tối ưu nên thử.

## 3. Vấn đề của bạn chủ yếu nằm ở FX hay map?

**Mình nghiêng rõ về FX là nguyên nhân chính**, dựa trên ảnh đã gửi.

- **Spirit Blessing:** màu mint quá nhạt, vòng ngoài thiếu vùng màu đủ mạnh để nổi trên tuyết.
- **Heal:** flash trắng và additive chồng lên nhau làm tóc, mặt và trang phục Cleric mất chi tiết ở những frame sáng nhất.
- **Bloom:** ánh sáng lan rộng nhưng không tăng độ rõ của hình dạng skill.
- **Map:** tuyết trắng xanh làm vấn đề trầm trọng hơn, nhưng không phải nguyên nhân duy nhất.

Trong ảnh Midgard Senki, FX xanh-tím vẫn nổi trên cát vàng sáng mà không cần toàn bộ map tối.

Vì vậy, **đừng giảm Value map 15–20% ngay**. Trước hết hãy sửa FX, giữ nguyên map và kiểm tra kết quả.

## 4. Chốt art direction

**Chọn RO Soft Fantasy nguyên bản + FX trong suốt có lõi màu rõ.**

Không đổi sang Adventure. Cũng chưa cần Darkened toàn bộ.

Chỉ khi đã tối ưu FX mà những map như Frostfang Den vẫn khó nhìn, mới giảm Value riêng vùng tuyết quá trắng khoảng 5–8%.

Đây là thay đổi quan trọng so với khuyến nghị trước: **ưu tiên sửa màu, blending và hình dạng hiệu ứng thay vì hạ sáng môi trường**.

## 5. Tám quy tắc làm FX như Midgard Senki trên nền pastel sáng

Các quy tắc dưới đây là đề xuất để tái hiện những đặc điểm thị giác quan sát được, không phải công thức kỹ thuật chính thức của Midgard Senki.

1. **Lõi có màu rõ, quầng mới nhạt.** Spirit Blessing dùng lõi teal `#35B69F`, quầng mint `#B3FFE5`. Đừng để cả hai cùng trắng-xanh nhạt.

2. **Dùng chênh lệch màu, không chỉ chênh lệch sáng.** Trên tuyết xanh lạnh, thêm vàng kem hoặc teal bão hòa vào FX thánh. Tránh vòng trắng-cyan gần trùng màu nền.

3. **Tách 3 mức sáng:** shape chính alpha 0.55–0.8; quầng mềm alpha 0.12–0.25; spark additive sáng mạnh nhưng nhỏ. Đây là preset thử, không phải tỷ lệ của Midgard Senki.

4. **Không làm sáng toàn bộ diện tích FX.** Với vòng buff, chỉ 20–35% chiều dài cung có điểm sáng mạnh tại một thời điểm. Phần còn lại vẫn nhìn thấy nhưng không phát sáng dữ dội.

5. **Giữ nhân vật đọc được trong lúc cast.** Heal không được biến tóc và mặt Cleric thành mảng trắng. Giảm flash, hoặc dùng mask để hạn chế ánh sáng phủ phần mặt.

6. **Dùng mép màu rõ thay outline đen.** Vòng phép có dải teal đậm hơn ở sát mặt đất, chuyển mềm ra mint. Không cần viền nâu đen kiểu sprite.

7. **Bloom chỉ là lớp hoàn thiện.** Thử tắt Bloom: shape skill vẫn phải đẹp và nhận ra được. Sau đó bật Bloom nhẹ, threshold HDR tuyến tính thử 1.1–1.3, chỉnh intensity theo pipeline Unity.

8. **40 người đánh boss phải có ngân sách FX.** Giữ đầy đủ hiệu ứng của bản thân và cảnh báo boss; giảm spark, flash và particle trang trí của người ngoài party, không xóa thông tin gameplay quan trọng.

## 6. Áp dụng ngay cho Cleric

| Skill | Thay đổi ưu tiên |
|---|---|
| **Heal** | Giảm flash trắng khoảng 40–60%; thêm lõi vàng kem/teal; giữ ánh sáng dạng hạt thay vì phủ cả người |
| **Spirit Blessing** | Tăng độ bão hòa vòng trong, tạo cung teal rõ; giữ vòng ngoài mint trong suốt |
| **Divine Light** | Lõi đạn vàng-trắng nhỏ nhưng đặc; trail alpha màu teal, spark additive ở đuôi và impact |
| **Divine Aegis** | Mép khiên teal rõ, mặt khiên alpha 0.12–0.25; chỉ sáng mạnh khi kích hoạt hoặc đỡ đòn |

**Bài test quyết định:** Chụp cùng một khung Cleric trên tuyết với hai phiên bản FX — bản hiện tại và bản đã sửa màu/blending — giữ nguyên map, camera và exposure. Nếu bản mới nổi rõ mà không cháy trắng, bạn đã chứng minh được rằng không cần tối toàn bộ game.

**Chốt cuối:** Midgard Senki cho thấy nền pastel sáng và FX ánh sáng trong suốt hoàn toàn có thể phối hợp đẹp. Điểm nên học không phải làm nền tối, mà là **màu lõi đủ mạnh, quầng trong suốt, điểm sáng có chọn lọc và hình dạng skill rõ ngay cả khi không có Bloom**. Với ảnh Cleric của bạn, mình sẽ ưu tiên sửa FX trước và giữ nguyên Soft Fantasy.