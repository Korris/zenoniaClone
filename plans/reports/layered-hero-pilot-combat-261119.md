# Pilot thân gốc nhiều lớp, phong cách thủ thế RO + Trickster (19/11/2026)

Tài khoản Tay ("Tân Korris", Plus), chat `6afe1ebe-b668-83ec-8233-c010a9354ef0`. Không hết lượt, không dùng Tan Pham.

## Ảnh đã nhận (`Assets/ArtLibrary/hero-layered/pilot-combat/`)
| File | Lượt | Ghi chú |
|---|---|---|
| body-male-idle-5dir-chatgpt-3.png | 3 (vẽ lại 2 lần) | -1: đầu 1:3,3 (nhỏ quá). -2: đầu 1:2,52 (bằng bản cũ). -3: đạt. Bản -1 và -2 không giữ |
| body-female-idle-5dir-chatgpt-1.png | 1 | Lần đầu bị dừng giữa chừng ("You stopped", không phải do tôi bấm), bấm "Try again" thì ra ảnh này |
| 5-class-muted-lineup-concept-chatgpt-1.png | 1 (A/B) | Concept 5 class tông trầm, chọn ảnh 2 (xem mục Concept) |

Ảnh so sánh: `docs/reference/hero-layered-pilot-combat/compare-current-vs-combat.png`. Mỗi giới 2 hàng: (hiện tại | mới), rồi (mới đổi áo đỏ san hô | mới đổi áo xanh trời). Cùng chiều cao người 230 px, nền tuyết nhạt. Đổi màu bằng `Tools/hero_recolor_core.py` (`to_game` + `recolour`), cùng hàm shader dùng.

## Số đo tỉ lệ đầu
Đo chiều cao đầu (đỉnh sọ tới cằm) chia chiều cao người, trên ảnh phóng 2x có thước, hướng xuống:
- Bản hiện tại (nam): 220 / 557 = **1:2,53**
- Nam mới -1: 165 / 544 = 1:3,3
- Nam mới -2: 210 / 530 = 1:2,52
- **Nam mới -3: 160 / 457 = 1:2,86** (nhận)
- **Nữ mới -1: 181 / 510 = 1:2,82** (nhận)

## Kiểm tra
- Đầu trọc, có tai. Đúng 5 hướng; hướng chéo lên-phải không thấy mắt.
- Hai tay nắm ngang ngực, không cầm gì. Nắm tay đặc, chưa có lỗ nắm rõ.
- Áo khoác và khăn chỉ hồng tím. Không có hồng tím dưới vạt áo (quần, tất, ủng). Thắt lưng nâu.
- Đổi màu thử: áo và khăn đổi màu sạch, không còn viền hồng.
- Không hình nào chạm hình bên cạnh. Có chỗ gần: khăn hình 1 cách hình 2 khoảng 6-9 px.

## Nhận xét
- Thần thái tốt hơn hẳn bản đứng đơ: mày nhíu, chân dạng, khăn bay tạo hướng. Đầu nhỏ lại, vai rộng hơn.
- Bản nam -3 có thêm dây đeo chéo da nâu trên áo (không hồng tím nên không đổi màu, ổn). Nắm tay phía xa ở hướng ngang không có găng (da trần).
- Nữ dạng chân hẹp hơn nam, khăn bay ra phía trước ở hướng xuống.
- Có 1 chấm xanh rêu đậm cạnh khăn hình 1 (khoảng 272,545). Lỗi nhỏ, tool cắt cần xoá.
- Khăn bay dài làm hình rộng gần 300 px ở ảnh gốc. Ô game 320 px có thể chật khi đi hoặc đánh, cần xem khi cắt sheet.
- Người thấp hơn bản cũ do chùng gối: cao 457 px với nam, 510 px với nữ. Tool cắt co theo chiều cao 200 px như cũ, nên đầu sẽ nhỏ đi trên màn hình.
- Tóc và mũ cũ vẽ theo sọ to (bản 1:2,5). Sọ mới nhỏ hơn khoảng 12-25 %. Tool đặt tóc và mũ co theo `w` của sọ nên vẫn ghép được, nhưng nên xem lại.

## Concept 5 class tông trầm (yêu cầu thêm)
- Trong chat đã có sẵn prompt concept do một agent khác gửi (không phải tôi), kèm lineup + assassin.png + cleric.png. Tôi chỉ tải 2 ảnh A/B về, xem rồi chọn.
- **Ảnh 1 (loại):** Cleric đội mũ trùm, đá gậy Wizard xanh sáng.
- **Ảnh 2 (nhận):**
  - Cả 5 đầu trần. Mechanic có kính đeo trán, là phụ kiện chứ không phải mũ.
  - Tông trầm: độ bão hoà trung bình 0,35, không có pixel neon.
  - 4 người thủ thế rõ. Cleric đứng hơi thẳng, tay cầm sách.
  - Đúng giới: Assassin nữ (khăn đỏ sẫm, 2 dao), Cleric nữ (áo ngà viền xanh rêu, sách).
- Không vẽ lại vì ảnh 2 đạt.

## Số lượt vẽ
4 prompt của tôi (nam 3, nữ 1) + 1 lần "Try again" + 1 lần chọn A/B concept.

## Nếu người dùng duyệt
1. **Chốt tỉ lệ mới trong spec:**
   - Đầu 1:2,8, chiều cao người 200 px tính cả tóc.
   - Sửa Prompt 1-4 trong `docs/character-appearance-layered-spec.md`: tư thế thủ thế, có khăn, khăn cũng hồng tím.
2. **Vẽ lại đi + đánh 1-3 cho cả 2 giới** (Prompt 3-4), gửi kèm ảnh pilot cùng hướng:
   - Khoảng 24 ảnh, cộng các lần vẽ lại.
   - Khăn phải bay theo hướng đi.
   - Trong đòn đánh, khăn và tay không được đè lên sọ.
3. **Vẽ lại tóc 5 class kiểu lọn nhọn tung nhẹ** (Prompt 5), theo đầu trọc mới cắt từ pilot. Câu "bề ngang ≤ 1,15 lần sọ" vẫn giữ.
4. **Mũ:** thử ghép 19 mũ cũ lên sọ mới trước. Mũ nào lệch thì chỉnh `DROP` hoặc vẽ lại.
5. **Chạy lại tool:** `build-hero-layered-sheets.py` (`head_anchor`, ô 320x256) và tool dò nắm tay. Khăn bay có thể làm lệch tâm frame, nên canh theo tâm sọ.

Commit: 8553ef15 (pilot nam + nữ, ảnh so sánh, Sổ lỗi, báo cáo). Ảnh concept do agent kia commit ở 956aaaea, trùng từng byte với ảnh tôi chọn. Chưa push.

## Câu hỏi
- Một agent khác cũng đang dùng tab chat Tay (đã gửi prompt concept và tải ảnh `muted5-n*.png`). Có cần xác nhận lại để từ nay chỉ một agent dùng không?
- Có giữ khăn quàng cho mọi class không, hay chỉ một số class? Khăn làm hình rộng ra và phải vẽ trong mọi frame.
- Concept tông trầm có thay hướng màu RO Soft (sáng ấm) cho cả game không?
