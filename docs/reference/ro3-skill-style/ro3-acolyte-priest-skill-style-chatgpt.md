<!-- ChatGPT (Tay) 21/11/2026 https://chatgpt.com/c/6b00ecfd-fef4-83ec-ab51-7f3975661b8c ; nguồn: khung trích từ https://www.youtube.com/watch?v=Uxz52wBS16Y (RO3 CBT Acolyte..Archbishop), ChatGPT không xem được video, chỉ 10 ảnh ghép 120 khung -->

# PHÂN TÍCH PHONG CÁCH SKILL RAGNAROK ONLINE 3 CBT — ACOLYTE → ARCHBISHOP

**Mục tiêu:** Chốt một bộ quy chuẩn VFX cho toàn bộ game RPG 2D của bạn, lấy phong cách hiệu ứng trong Ragnarok Online 3 (RO3) làm chuẩn, nhưng triển khai bằng Unity Particle System + Mesh Trail + Shader, không sử dụng sprite VFX.

## 0. Phạm vi quan sát và độ tin cậy

Mình đã xem toàn bộ **10 ảnh ghép, tổng cộng 120 khung hình** bạn gửi, trải từ Acolyte, Priest, High Priest đến Archbishop, với các mốc thời gian khoảng 0–620 giây.

Mình cũng đã thử mở [video YouTube gốc](https://www.youtube.com/watch?v=Uxz52wBS16Y), nhưng YouTube không cho công cụ truy xuất nội dung. Vì vậy, **mình chưa xem trực tiếp được video đang phát**, mà phân tích dựa trên các khung hình bạn cung cấp.

Đã đối chiếu thêm tên và hệ thống skill với tài liệu cộng đồng RO3.  

Quy ước trong toàn bộ phân tích:

- **[QS] Quan sát:** Nhìn thấy trực tiếp trong ảnh, bao gồm hình dạng, màu sắc và thứ tự xuất hiện ở các khung.
- **[ƯL] Ước lượng:** Kích thước, mã màu, thời lượng suy ra từ ảnh; không phải thông số nội bộ của RO3.
- **[ĐX] Đề xuất:** Thông số nên áp dụng cho game của bạn, không khẳng định đây là giá trị RO3 sử dụng.

**Kết luận lớn nhất:** RO3 sử dụng phong cách *Soft Holy Fantasy*: hiệu ứng có cấu trúc hình học rõ ràng, sáng nhưng không phủ trắng toàn màn hình, phần lớn kích thước tương đối gọn so với nhân vật. Các skill lớn được nhận diện bằng vòng phép, biểu tượng thần thánh và vùng tác dụng, chứ không đơn thuần tăng lượng hạt và Bloom.

---

# 1. Ngôn ngữ thiết kế skill RO3

## 1.1. Kích thước và độ hoành tráng

[QS] Trong đa số khung hình, hiệu ứng buff và hồi máu tập trung ngay xung quanh nhân vật. Các skill tấn công có vùng sáng lớn hơn, còn những skill như Magnus Exorcismus, Sanctuary, Holy Reckoning mới mở rộng thành một khu vực đáng kể trên mặt đất.

Một điểm rất đặc trưng là RO3 **không biến tất cả skill thành một vụ nổ khổng lồ**.

Bảng kích thước mình đề xuất chuẩn hóa theo chiều cao của nhân vật chibi, ký hiệu H:

| Loại hiệu ứng | Kích thước đề xuất [ĐX] |
|---|---|
| Buff cá nhân | Đường kính 0,8–1,5H |
| Heal đơn mục tiêu | 1,0–1,6H |
| Đạn Holy | Đường kính 0,3–0,7H |
| Đòn Holy chạm mục tiêu | 0,8–1,6H |
| Khiên bảo vệ | 1,2–1,8H |
| Skill AoE nhỏ | 2,5–4H |
| Sanctuary, AoE lớn | 4–6H |
| Ultimate | 5–7H |

Đây là kích thước FX, không phải bán kính gameplay.

Ví dụ: skill có phạm vi hồi máu 8 m không nhất thiết phải vẽ ánh sáng bao phủ toàn bộ 8 m. Chỉ cần vòng ranh giới đúng 8 m và vùng sáng tập trung quanh tâm hoặc các mục tiêu được hồi máu.

## 1.2. Hình học vòng phép

[QS] Những khung hình rõ nhất cho thấy RO3 thiên về hình học ma pháp mang tính nghi lễ:

- **Vòng tròn:** cấu trúc phổ biến nhất ở các skill bảo vệ và hồi máu.
- **Rune, đường khắc và ký hiệu chia hướng:** xuất hiện trong một số vòng phép phức tạp.
- **Ngôi sao và cấu trúc đa giác:** thể hiện rõ ở các ma pháp trận đỏ khi dùng kỹ năng lên quái, cũng như các đòn Holy cấp cao.
- **Vòng kép:** một đường ranh giới lớn và họa tiết nhỏ ở phần giữa.
- **Lưới ô vuông sáng:** nổi bật ở các vùng phép dạng Sanctuary hoặc hiệu ứng tác động trên mặt đất.

Không nên hiểu rằng RO3 dùng một loại hình cố định cho mọi skill. Đặc biệt, không có đủ bằng chứng để khẳng định toàn bộ vòng phép đều là ngũ giác hoặc lục giác.

**Quy tắc cho game bạn [ĐX]:**

| Chức năng | Hình học chính |
|---|---|
| Heal | Vòng tròn mềm + dấu thập ánh sáng |
| Buff ATK | Ngôi sao bốn cánh + vòng sáng |
| Buff DEF | Lục giác hoặc hình khiên |
| Tấn công Holy | Dấu thập, hình thoi, quầng sáng |
| Sanctuary | Vòng tròn kép + rune |
| Thanh tẩy | Vòng sáng + hạt bay ra ngoài |
| Ultimate Holy | Vòng đa tầng + thánh ấn |

## 1.3. Bảng màu RO3

[QS] Màu vàng kim và trắng ngà xuất hiện nhiều trong các phép thuộc hệ Holy; màu xanh lá thiên về hồi máu; xanh lam nhạt dùng ở nhiều hiệu ứng phòng hộ và hỗ trợ. Một số ma pháp trận tác động lên kẻ địch có màu đỏ rực.

Tuy nhiên, **màu đỏ không đồng nghĩa tất cả debuff đều màu đỏ**. Trong các khung Lex Aeterna, vòng đỏ cũng có thể chịu ảnh hưởng từ hệ thống đánh dấu mục tiêu hoặc hiệu ứng chiến đấu.

Bảng màu dưới đây là **ước lượng thị giác**, không phải lấy mẫu chính xác từ texture gốc.

<box gap={2}>
  {#each [
    {name:"Holy — trắng ngà",hex:"#FFF4C9",use:"Tâm sáng, đòn thánh"},
    {name:"Holy — vàng kim",hex:"#F3D16B",use:"Vòng phép, biểu tượng"},
    {name:"Heal — xanh lá",hex:"#91DB76",use:"Hồi HP, hạt hồi phục"},
    {name:"Heal — xanh bạc hà",hex:"#B6E9B7",use:"Ánh sáng phụ"},
    {name:"Protection — xanh lam",hex:"#9DDAEC",use:"Khiên, vùng bảo vệ"},
    {name:"Purification — trắng xanh",hex:"#D7F4F3",use:"Thanh tẩy, tia sáng"},
    {name:"Debuff — đỏ san hô",hex:"#E65A52",use:"Ấn chú lên quái"},
    {name:"Magic violet",hex:"#BCA3E4",use:"Hỗ trợ phép đặc biệt"}
  ] as c}
    <row align="center" gap={3}>
      <box background={c.hex} width="35px" height="26px" radius="sm" border={{size:1,color:"subtle"}}/>
      <box flex="1" gap="0">
        <text weight="medium" size="sm">{c.name}</text>
        <text color="secondary" size="xs">{c.use}</text>
      </box>
      <text color="secondary" size="xs" tabularNums>{c.hex}</text>
    </row>
  {/each}
</box>

[ĐX] Màu sáng nhất chỉ nên nằm ở lõi hạt, điểm giao nhau của các đường rune hoặc thời điểm impact. Không nên để cả vòng phép thành một mảng vàng hoặc trắng phát sáng đồng đều.

## 1.4. Độ đậm, độ trong và Bloom

[QS] Những vòng phép như Sanctuary và Holy Reckoning vẫn cho phép nhìn rõ địa hình, nhân vật và quái bên dưới. Các phần sáng có trọng tâm, không che toàn bộ vùng tác dụng.

Mình đề xuất ba mức:

| Thành phần | Alpha đỉnh [ĐX] |
|---|---:|
| Đường rune chính | 65–85% |
| Lớp glow mềm | 12–30% |
| Mảng sáng trên đất | 10–22% |
| Flash chạm mục tiêu | 75–100% trong thời gian ngắn |
| Hạt nhỏ | 40–75% |

Nên kết hợp Alpha Blend cho nền và Additive cho lõi sáng, thay vì để toàn bộ hiệu ứng ở chế độ Additive.

## 1.5. Cột sáng và hạt phép

[QS] Có cột sáng, đặc biệt ở những skill thuộc cấp Archbishop, nhưng không phải skill nào cũng có.

- Buff thông thường: quầng sáng nhỏ, vài hạt, đôi khi biểu tượng trên đầu.
- Heal: một flash mềm và ánh sáng hướng lên.
- Sanctuary: lớp sáng theo vùng, không cần cột sáng cao.
- Resurrection và các skill cấp cao: nghi lễ ánh sáng rõ hơn.
- Skill Holy lớn: tia, cung tròn, ngôi sao hoặc thánh ấn lớn.

[ĐX] Số hạt đồng thời phù hợp phong cách này:

- Buff nhỏ: 4–8 hạt.
- Heal: 6–12 hạt.
- Khiên: 6–10 hạt.
- AoE: 15–30 hạt.
- Ultimate: 30–50 hạt trong pha bùng sáng, sau đó giảm.

Đây là ngân sách thiết kế để dễ đọc, không phải số lượng hạt được đo từ RO3.

## 1.6. Tên skill, biểu tượng và số chiến đấu

[QS] Trong nhiều khung, tên như `Holy Light`, `Turn Undead`, `Sanctuary`, `Holy Reckoning` xuất hiện phía trên nhân vật hoặc vị trí thi triển.

- Tên skill là chữ nhỏ nằm trong một nhãn tối, vàng hoặc sáng tùy cảnh.
- Biểu tượng như thiên thần, quầng sáng, dấu thập có thể xuất hiện độc lập phía trên nhân vật.
- Số hồi máu thường là **xanh lá**, ví dụ `30`, `177`.
- Số sát thương thường là **trắng, vàng hoặc vàng cam**, tùy đòn.
- Một số số sát thương lớn có viền tối để đọc được trên nền sáng.
- Những đòn đánh nhiều hit hiện nhiều số riêng thay vì gộp một số duy nhất.

**Phân biệt:** Các dòng tiêu đề lớn `ACOLYTE ACTIVE SKILLS`, `PRIEST ACTIVE SKILLS` ở trên cùng là lớp chữ giới thiệu của video, không phải UI mặc định của kỹ năng.

[ĐX] Với game của bạn, tên skill nên hiện tối đa 0,5–0,8 giây phía trên người thi triển. Không cần hiện tên skill mỗi lần buff tự động được làm mới, tránh rối khi 40 người đánh boss.

---

# 2. Phân tích từng skill từ 120 khung hình

**Lưu ý cách đọc:** Các số giây in góc ảnh là thời điểm trong video, không phải độ dài hoạt ảnh. Ví dụ, hai khung 160s và 164s không chứng minh hiệu ứng kéo dài 4 giây. Thời lượng bên dưới là mức mình đề xuất để tái tạo cảm giác chuyển động [ĐX], không phải thời lượng đo được từ clip.

## A. ACOLYTE — Hiệu ứng cơ bản

### 2.1. Ruwach

<text color="secondary" size="xs">Ảnh 1, khu vực 30–70s</text>

**[QS]** Khung có nhãn Ruwach cho thấy hiệu ứng tương đối nhỏ, tập trung quanh nhân vật. Một số khung đi kèm vòng hoặc quầng phép ở vùng mặt đất, nhưng ảnh chưa đủ rõ để xác nhận số lớp vòng.

**Thiết kế nên dựng [ĐX]:**

- Lớp 1: một vòng tròn vàng nhạt sát chân, đường kính 1,2H.
- Lớp 2: 4–6 tia phát sáng rất ngắn, hướng ra ngoài.
- Lớp 3: 6 hạt ánh sáng vàng nhỏ xoay quanh người.
- Lớp 4: flash trắng ngà tại thời điểm kích hoạt.
- Màu: `#FFF0B2`, `#F3CD68`, lõi `#FFFDF0`.
- Thời gian kích hoạt: 0,45–0,65s.
- Nếu skill tạo hiệu ứng duy trì: giảm độ sáng và dùng một quầng nhỏ thay vì chạy liên tục hiệu ứng kích hoạt.

**Đặc điểm cần giữ:** Đây là phép dò tìm/phát hiện, không phải phép gây sát thương diện rộng. Không nên làm nó giống vụ nổ Holy.

### 2.2. Pneuma

<text color="secondary" size="xs">Ảnh 1, khoảng 70–85s</text>

**[QS]** Hiệu ứng trên cầu đá có ánh sáng xanh cyan/trắng xanh ở mục tiêu, tương đối trong và thanh mảnh. Không thấy một mái vòm đặc phủ kín cả vùng.

**Thiết kế nên dựng [ĐX]:**

- Vòng ranh giới xanh lam nhạt đường kính 1,6–2H.
- Một cột khí trong suốt dạng hình trụ ngắn.
- 3–5 dải cong hướng lên, dùng Mesh Trail.
- Vài hạt cyan nổi chậm.
- Màu: `#A8E9F0`, `#D9FFFF`.
- Kích hoạt: 0,35s; sau đó duy trì vòng bảo vệ mờ theo thời gian buff.

Không sử dụng một bong bóng hình cầu xanh đậm kiểu sci-fi.

### 2.3. Heal

<text color="secondary" size="xs">Ảnh 1–2, khoảng 90–110s</text>

Trong bộ ảnh, phần thể hiện rõ hơn thuộc các skill hồi máu như Healing Skills và các hiệu ứng hồi HP về sau.

**[QS]**

- Số hồi máu màu xanh lá xuất hiện gần nhân vật.
- Hiệu ứng thiên về ánh sáng mềm, ít hạt.
- Không có bằng chứng rằng Heal thông thường luôn tạo ma pháp trận lớn trên đất.

**Thiết kế nên dựng [ĐX]:**

- Lớp 1: flash trắng xanh ở ngực, bán kính 0,35H.
- Lớp 2: một quầng sáng mở rộng từ chân, đường kính tối đa 1,3H.
- Lớp 3: 6–10 hạt vàng xanh bay lên.
- Lớp 4: một dấu thập nhỏ phát sáng trong 0,2s.
- Màu: `#B6EBB0`, `#E9FFCB`, `#FFF3BC`.
- Tổng thời gian: 0,55–0,75s.
- Số HP xanh lá xuất hiện trễ 0,15–0,2s so với lúc bắt đầu cast.

**Điểm quan trọng:** Hạt bay lên giống bụi phép, không phải một luồng nước xanh phụt lên.

### 2.4. Increase AGI

<text color="secondary" size="xs">Ảnh 2, khoảng 110–125s</text>

**[QS]** Hiệu ứng tăng tốc rất gọn, phần lớn ánh sáng nằm gần người. Không nhìn thấy những vệt tốc độ dài theo sau nhân vật trong các khung này.

**Thiết kế nên dựng [ĐX]:**

- Vòng vàng rất mảnh dưới chân: 1,1H.
- Hai luồng gió cong xoắn từ chân tới ngang vai.
- 5–7 hạt xanh vàng kéo lên nhanh.
- Một flash trắng vàng ngắn.
- Màu: `#F5E5A2`, `#C3E8AC`.
- Thời gian: 0,45–0,65s.

Không nên để trail tốc độ chạy theo nhân vật suốt thời gian buff.

### 2.5. Blessing

<text color="secondary" size="xs">Ảnh 2, khoảng 125–145s</text>

**[QS]** Có ánh sáng vàng xuất hiện gần nhân vật, phù hợp ngôn ngữ buff Holy. Phần biểu tượng không đủ rõ để kết luận chính xác hình thánh giá.

**Thiết kế nên dựng [ĐX]:**

- Dấu thập bốn cánh nhỏ trên đầu.
- Vòng tròn vàng ngà dưới chân, 1,3H.
- Một quầng sáng hướng xuống từ phía trên.
- 6 hạt vàng chuyển động nhẹ.
- Màu: `#FFF0BC`, `#EFC86E`.
- Thời gian: 0,6–0,85s.

Blessing nên là một trong các mẫu buff gọn và dễ nhận biết nhất của cả class.

### 2.6. Holy Light

<text color="secondary" size="xs">Ảnh 2, khoảng 130–150s</text>

**[QS]** Các khung có số sát thương và ánh sáng vàng tại mục tiêu. Đòn đánh có trọng tâm rõ, không phải một luồng laser dài.

**Thiết kế nên dựng [ĐX]:**

- Tích tụ một hạt sáng trắng vàng ở tay hoặc đầu gậy.
- Phóng đạn hình thoi/giọt sáng, cỡ 0,3H.
- Trail mảnh vàng dài khoảng 0,4–0,7H.
- Impact tạo sao bốn cánh và vòng vàng nhỏ, đường kính 0,9–1,2H.
- Màu: `#FFFBE4`, `#F8D16A`.
- Tích tụ 0,15s; bay tùy khoảng cách; impact 0,2–0,3s.

Đòn này nên **nhanh, sắc, sạch**, không dùng một quả cầu năng lượng to ngang đầu nhân vật.

### 2.7. Angelus

<text color="secondary" size="xs">Ảnh 2–3, khoảng 145–160s</text>

**[QS]** Hiệu ứng hỗ trợ có sắc vàng và xanh nhạt. Không có đủ khung rõ để tách chính xác vòng phép và các tia sáng.

**Thiết kế nên dựng [ĐX]:**

- Vòng bảo hộ mở ra từ chân, đường kính 1,6H.
- Một thánh ấn nhỏ nằm trên vòng.
- 4 cung sáng nhẹ nâng lên quanh người.
- Màu: `#FFF0C8`, `#D8EBD8`.
- Thời gian: 0,65–0,9s.

---

## B. PRIEST — Ma pháp trận, hồi máu và trừ tà

### 2.8. Turn Undead

<text color="secondary" size="xs">Ảnh 3, khoảng 180–195s</text>

**[QS]**

- Ma pháp trận màu đỏ có thể thấy rõ dưới quái trong một số khung.
- Xuất hiện hiệu ứng sáng tại mục tiêu và số sát thương.
- Các đường tạo thành hình ngôi sao, vòng tròn và nét ma pháp.

**Cần phân biệt:** Không thể chắc chắn toàn bộ vòng đỏ là riêng VFX của Turn Undead, vì dấu mục tiêu hoặc lớp cảnh báo chiến đấu có thể dùng chung hình học đỏ.

**Thiết kế nên dựng [ĐX]:**

- Một vòng ấn chú mảnh màu đỏ san hô dưới quái.
- Dấu thập vàng sáng xuất hiện tại tâm.
- Flash Holy trắng ngà đi từ trên xuống.
- Vòng đỏ biến mất ngay sau impact.
- Màu: `#E66A5C`, `#FFF0B3`.
- Đường kính: 1,4–1,8H đối với quái thường.
- Thời gian: 0,65–0,9s.

Không nên biến toàn bộ phép trừ tà thành lửa đỏ; phần kết liễu phải có màu Holy rõ.

### 2.9. Lex Aeterna

<text color="secondary" size="xs">Ảnh 4, khoảng 190–205s</text>

**[QS]**

- Có ấn chú/vòng đỏ ở phía dưới hoặc quanh mục tiêu.
- Các hình học màu đỏ tương đối sắc nét, đối lập với cảnh nền sáng.
- Vòng không phủ lên toàn màn hình.

**Thiết kế nên dựng [ĐX]:**

- Vòng đỏ san hô đường kính 1,2–1,6H.
- Ngôi sao bốn hoặc tám hướng ở bên trong.
- Một dấu ấn đỏ nhỏ hiện ngang ngực hoặc phía trên mục tiêu.
- Sau khi kích hoạt, giảm thành ấn chú nhỏ.
- Màu: `#E45C56`, `#FFC0A4`.
- Kích hoạt: 0,45s.
- Ấn chú có thể tồn tại đến khi debuff hết hiệu lực, nhưng chỉ ở độ sáng thấp.

Đây là mẫu tốt cho **debuff đánh dấu hoặc làm suy yếu đối thủ** trong game bạn.

### 2.10. Lex Divina

<text color="secondary" size="xs">Ảnh 4, khoảng 210–230s</text>

**[QS]** Khung hình cho thấy skill dạng đơn mục tiêu, có dấu hiệu status hoặc nhãn hiệu ứng. Phần VFX trực tiếp khá nhỏ, không có vòng AoE lớn.

**Thiết kế nên dựng [ĐX]:**

- Một thánh ấn trắng vàng lóe trên đầu quái.
- Hai cung sáng ép vào tâm.
- Một vòng rất mảnh dưới chân đối thủ.
- Khi trúng hiệu ứng Silence, hiện biểu tượng trạng thái nhỏ phía trên HP quái.
- Màu: `#F6E1A1` kết hợp xám tím `#B4A6C9`.
- Thời gian: 0,45–0,7s.

### 2.11. Magnus Exorcismus

<text color="secondary" size="xs">Ảnh 4, khoảng 200–225s</text>

Đây là một trong những skill đáng tham khảo nhất cho game của bạn.

**[QS]**

- Có vùng phép trên mặt đất.
- Xuất hiện các ô/lưới sáng vàng.
- Nhiều quái đứng trong phạm vi nhận sát thương, với nhiều số nổi lên cùng lúc.
- FX nằm chủ yếu trên mặt đất; không phải một cột sáng đặc chiếm toàn bộ không gian.

**Thiết kế nên dựng [ĐX]:**

1. Vòng tròn lớn xác lập phạm vi, đường kính 4–5H.
2. Lưới ma pháp vàng gồm các đường thẳng giao nhau.
3. Lớp ánh sáng mềm phủ mặt đất, alpha 10–18%.
4. Những điểm giao nhau của lưới sáng mạnh hơn.
5. Mỗi tick sát thương tạo một chớp Holy nhỏ ở quái bị trúng.
6. Một vài hạt vàng nhỏ bay lên khỏi vùng phép.

- Màu rune: `#F6D776`.
- Màu lõi: `#FFF8D5`.
- Kích hoạt: 0,5–0,8s.
- Mỗi nhịp gây sát thương: flash 0,12–0,2s.
- Vòng phép duy trì theo thời gian hiệu lực thực tế của skill.

**Nên học đúng điều này:** Magnus đẹp nhờ mặt đất được tổ chức thành hình học có chủ đích, không phải nhờ Particle bắn dày đặc.

### 2.12. Resurrection

<text color="secondary" size="xs">Ảnh 4, khoảng 225–235s</text>

**[QS]** Có tên skill, hiệu ứng màu tím hồng hoặc trắng sáng gần người thi triển trong các khung. Tuy nhiên, không có đủ chuỗi liên tiếp để phân biệt rõ hiệu ứng cast và hiệu ứng hồi sinh trên mục tiêu.

**Thiết kế nên dựng [ĐX]:**

- Vòng hồi sinh trắng ngà dưới người nằm xuống.
- 4 tia sáng mềm hội tụ từ các hướng.
- Quầng sáng hình cánh hoa hoặc thánh giá mở ra.
- Một cột sáng trắng xanh cao 1,5–2H.
- 8–12 hạt nhỏ bay lên.
- Màu: `#F3F6D8`, `#CFEDE6`, điểm nhấn `#E2C9F1`.
- Thời gian: 0,9–1,3s.

Chỉ nên có cột sáng lớn trong khoảnh khắc hồi sinh thành công.

### 2.13. Kyrie Eleison

<text color="secondary" size="xs">Ảnh 4–5, khoảng 235–255s</text>

**[QS]**

- Có quầng vàng hoặc vàng xanh xung quanh nhân vật.
- Biểu tượng bảo vệ nằm khá gần thân.
- Không thấy một quả cầu năng lượng đặc và lớn.

**Thiết kế nên dựng [ĐX]:**

- Vòng tròn ánh sáng tại chân, 1,25H.
- Một khiên mờ bằng các cung cong bao quanh người.
- Dấu thập nhỏ ở phía trước thân.
- Một quầng sáng vàng nổi lên.
- Màu: `#F6DE9E`, `#D8F4D5`.
- Thời gian cast: 0,5–0,7s.
- Khiên duy trì rất mờ; chỉ sáng mạnh lúc hấp thụ sát thương.

### 2.14. Sanctuary

<text color="secondary" size="xs">Ảnh 5, khoảng 245–265s</text>

**[QS]**

- Nhân vật đứng trên một vùng sáng hình học.
- Vùng phép có dạng ô vuông hoặc lưới sáng nhạt.
- Có các số hồi HP màu xanh lá.
- Phần sáng tập trung trên mặt đất, khá trong.

**Thiết kế nên dựng [ĐX]:**

- Một vòng tròn đường kính 4–5H.
- Bên trong có hình học nhẹ dạng lưới hoặc rune.
- Lớp glow trắng xanh hoặc vàng ngà rất mờ.
- Vòng nhỏ xuất hiện tại từng nhân vật được hồi máu.
- 12–20 hạt sáng bay nhẹ, tránh phát hạt liên tục quá dày.
- Màu: `#DFF5C4`, `#F9E7A9`.
- Vòng mở trong 0,4–0,6s.
- Trong thời gian tồn tại, chỉ có các nhịp sáng nhẹ theo tick hồi máu.

Sanctuary **không nên thành một vũng màu xanh lá đặc**.

### 2.15. Magnificat

<text color="secondary" size="xs">Ảnh 5, khoảng 265–280s</text>

**[QS]** Hiệu ứng rất kín đáo. Có một điểm sáng hoặc vòng nhỏ gần nhân vật khi kích hoạt, ít chuyển động lớn.

**Thiết kế nên dựng [ĐX]:**

- Vòng trắng vàng 1,2H.
- Dấu thập nhỏ phát sáng ở ngực.
- 4–6 hạt vàng nhạt đi lên.
- Một vòng ánh sáng lan ra rồi biến mất.
- Màu: `#FFF3CB`, `#D9E8BB`.
- Thời gian: 0,5–0,75s.

### 2.16. Gloria

<text color="secondary" size="xs">Ảnh 5, khoảng 280–295s</text>

**[QS]** Ánh sáng tương đối ít, không có vòng phép lớn nổi bật trong khung.

**Thiết kế nên dựng [ĐX]:**

- Một ngôi sao vàng ở trên đầu.
- 3 điểm sáng nhỏ xuất hiện theo nhịp.
- Quầng tròn rất mỏng mở rộng.
- Màu: `#F7E6A7`.
- Kích thước: 1–1,4H.
- Thời gian: 0,5s.

### 2.17. Impositio Manus

<text color="secondary" size="xs">Ảnh 6, khoảng 290–305s</text>

**[QS]** Có tên skill và hiệu ứng nhỏ quanh nhân vật. Không có dấu hiệu một AoE lớn.

**Thiết kế nên dựng [ĐX]:**

- Biểu tượng bàn tay/thánh ấn cách điệu bằng mesh.
- Quầng sáng vàng từ vai xuống thân.
- Một vòng vàng rất nhỏ ở chân.
- 4–6 hạt.
- Màu: `#F4D788`, `#FFF5CF`.
- Thời gian: 0,55–0,75s.

### 2.18. Suffragium

<text color="secondary" size="xs">Ảnh 6, khoảng 300–315s</text>

**[QS]** Hiệu ứng buff thiên về ánh sáng mềm, không có đòn đánh hoặc tác động môi trường mạnh.

**Thiết kế nên dựng [ĐX]:**

- Hai cung tròn màu xanh nhạt quay nhanh quanh mục tiêu.
- Dấu thập vàng ngà lóe trên đầu.
- Một quầng mỏng ở chân.
- Màu: `#BFE6E8`, `#FFF0BF`.
- Thời gian: 0,45–0,65s.

### 2.19. Aspersio

<text color="secondary" size="xs">Ảnh 6, khoảng 305–325s</text>

**[QS]** Nhân vật hoặc đối tượng nhận hiệu ứng có ánh sáng vàng và cam rất gọn quanh người. Không thấy một cơn mưa hạt dày.

**Thiết kế nên dựng [ĐX]:**

- Một tia sáng trắng vàng chạy dọc vũ khí hoặc tay.
- 5 hạt vàng xuất hiện gần vũ khí.
- Một nhịp flash nhỏ ở điểm tiếp xúc.
- Màu: `#FFF5CA`, `#F2CE77`.
- Thời gian: 0,4–0,6s.

Nếu skill tăng thuộc tính Holy cho vũ khí, nên để ánh sáng nằm gần vũ khí thay vì bao phủ cả người.

### 2.20. Safety Wall

<text color="secondary" size="xs">Ảnh 6, khoảng 320–335s</text>

**[QS]** Hiệu ứng tập trung ở mặt đất, có vùng bảo vệ khá nhỏ với ánh sáng xanh hoặc tím nhạt. Không thấy cấu trúc một bức tường đá đặc.

**Thiết kế nên dựng [ĐX]:**

- Vòng ranh giới xanh tím đường kính 1,5–2H.
- Bốn điểm neo sáng trên vòng.
- Một lớp màn bảo vệ hướng lên, alpha 10–18%.
- Những vệt sáng thẳng ngắn ở mép.
- Màu: `#C4D5ED`, `#C6ACE8`.
- Xuất hiện 0,4s; duy trì theo thời gian buff.

---

## C. HIGH PRIEST — Hỗ trợ cấp cao

### 2.21. Basilica

<text color="secondary" size="xs">Ảnh 7, khoảng 345–360s</text>

**[QS]**

- Có hồi phục HP màu xanh lá.
- Phần phép quanh nhân vật khá gọn.
- Không có một mái vòm đặc nổi bật trong những khung được cung cấp.

**Thiết kế nên dựng [ĐX]:**

- Vòng sáng trắng xanh 1,5H.
- Một thánh ấn ở tâm.
- Quầng sáng hướng lên cao 1H.
- 6–10 hạt vàng xanh.
- Màu: `#D7F0D3`, `#FFF1BC`.
- Kích hoạt 0,7s; duy trì ánh sáng mờ nếu có buff dài hạn.

### 2.22. Assumptio

<text color="secondary" size="xs">Ảnh 7, khoảng 360–375s</text>

**[QS]** Hiệu ứng nhận buff có màu vàng/trắng ngà, sáng nhưng khá gọn.

**Thiết kế nên dựng [ĐX]:**

- Hai vòng mảnh màu vàng đi từ chân lên.
- Một bóng khiên mờ bao quanh nửa thân.
- Thánh ấn nhỏ xuất hiện rồi tan.
- Màu: `#FFF2C6`, `#DDEADB`.
- Kích thước: 1,4–1,7H.
- Thời gian: 0,65–0,9s.

**Đối với Cleric của bạn:** Đây là một tham chiếu rất tốt cho Divine Aegis.

---

## D. ARCHBISHOP — Thiên thần, thánh ấn và ma pháp diện rộng

### 2.23. Hymn of Devotion

<text color="secondary" size="xs">Ảnh 8, khoảng 380–390s</text>

**[QS]** Có dấu hiệu buff xanh lục/trắng xanh quanh nhân vật. Hiệu ứng không bùng nổ lớn.

**Thiết kế nên dựng [ĐX]:**

- Quầng tròn xanh lá nhạt đường kính 1,3H.
- Một luồng ánh sáng đi lên.
- 8 hạt nhỏ xoay rồi bay ra.
- Màu: `#A5D7B5`, `#F0F8D6`.
- Thời gian: 0,6–0,8s.

### 2.24. Sacred Lily

<text color="secondary" size="xs">Ảnh 8, khoảng 390–405s</text>

**[QS]** Biểu tượng hoa hoặc cánh sáng vàng xuất hiện phía trên nhân vật. Đây là dạng biểu tượng rất dễ nhận ra.

**Thiết kế nên dựng [ĐX]:**

- Hoa bốn hoặc sáu cánh bằng mesh.
- Mở cánh từ tâm, sau đó mờ đi.
- Một vòng vàng ngà rất nhỏ dưới chân.
- Màu: `#FFF0BC`, `#F2C96D`.
- Kích thước hoa: 0,55–0,8H.
- Thời gian: 0,65–0,85s.

Điểm cần học là dùng **một biểu tượng duy nhất thật rõ**, không cần nhiều hạt.

### 2.25. Radiant Holy Light

<text color="secondary" size="xs">Ảnh 8, khoảng 405–465s</text>

**[QS]**

- Có ánh sáng trắng vàng quanh nhân vật.
- Một số khung có flash hoặc quầng sáng ở vùng phía trên.
- Phần lớn cảnh nền vẫn nhìn rõ.

**Thiết kế nên dựng [ĐX]:**

- Tích tụ một nguồn sáng ở giữa thân.
- Vòng Holy nhỏ mở ra từ chân.
- Một tia trắng vàng đi lên rồi tan.
- Vài hạt xanh vàng.
- Màu: `#FFFAE0`, `#E4F2C8`.
- Thời gian: 0,7–1,0s.

### 2.26. Prayer of the Holy Spirit

<text color="secondary" size="xs">Ảnh 8, khoảng 465–480s</text>

**[QS]**

- Nhân vật có quầng sáng vàng rất rõ.
- Có những đường sáng cong hoặc vòng phép mở rộng.
- Không phải một luồng sáng đặc hoàn toàn.

**Thiết kế nên dựng [ĐX]:**

- Một vầng hào quang nằm trên đầu hoặc ngang vai.
- 3 cung ánh sáng mềm nâng lên.
- Vòng sáng mở ra 1,5H.
- 8–12 hạt vàng ngà.
- Màu: `#F9E3A0`, `#FFF9E2`.
- Thời gian: 0,8–1,1s.

### 2.27. Judex

<text color="secondary" size="xs">Ảnh 9, khoảng 480–495s</text>

**[QS]**

- Đòn Holy gây sát thương trực tiếp tại quái.
- Có ánh sáng vàng quanh mục tiêu và các con số sát thương.
- Quy mô vừa, không có lưới AoE lớn như Magnus Exorcismus.

**Thiết kế nên dựng [ĐX]:**

- Một thánh ấn nhỏ ở vị trí quái.
- Tia Holy từ trên xuống, không cần kéo dài tới mép màn hình.
- Flash hình thoi/vòng tròn.
- 4 tia ngắn tỏa ra.
- Màu: `#FFFBE0`, `#F7D37C`.
- Kích thước: 1,2–1,8H.
- Thời gian: 0,45–0,7s.

### 2.28. Holy Sigma

<text color="secondary" size="xs">Ảnh 9, khoảng 495–505s</text>

**[QS]**

- Hình ngôi sao/đường hình học vàng nổi bật.
- Có vùng tác động lớn hơn Judex.
- Các đường vàng rất mảnh, tạo cảm giác thánh ấn được vẽ lên không gian.

**Thiết kế nên dựng [ĐX]:**

- Hai vòng tròn đồng tâm.
- Một hình sao bốn cánh kéo dài hoặc hình thoi.
- Các đường nối tạo thành thánh ấn vàng.
- Flash trắng ngà ở tâm.
- Màu: `#EBD18C`, `#FFF3C7`.
- Đường kính: 2,5–3,5H.
- Thời gian: 0,8–1,1s.

Đây là mẫu rất phù hợp cho một skill Holy cấp cao nhưng chưa phải ultimate.

### 2.29. Divine Retribution

<text color="secondary" size="xs">Ảnh 9, khoảng 505–515s</text>

**[QS]** Đòn đánh trực tiếp gây sát thương lên một quái, có ánh sáng vàng và vòng quanh mục tiêu.

**Thiết kế nên dựng [ĐX]:**

- Dấu ấn vàng dưới mục tiêu.
- Một tia sáng hướng xuống.
- Vòng sáng bung ra khi gây sát thương.
- 5–8 tia ngắn.
- Màu: `#FFF3C9`, `#F0C56D`.
- Thời gian: 0,55–0,8s.

### 2.30. Holy Reckoning

<text color="secondary" size="xs">Ảnh 9, khoảng 510–530s</text>

**[QS]**

- Vòng phép lớn màu trắng vàng trên mặt đất.
- Có các chi tiết hình học ở mép vòng.
- Nhiều quái bên trong nhận sát thương.
- Các tia/đường sáng xuất hiện theo từng nhịp.
- Vùng phép vẫn trong suốt, không xóa đi màu nền.

Đây là mẫu tiêu biểu của ma pháp diện rộng cấp cao RO3.

**Thiết kế nên dựng [ĐX]:**

1. Vòng ngoài đường kính 5–6H, đường dày và sáng hơn Sanctuary.
2. Vòng rune bên trong có các dấu thập nhỏ.
3. Một hình sao ở tâm, không cần quá phức tạp.
4. Những cung sáng xoay rất chậm.
5. Mỗi lần sát thương, tạo chớp nhỏ tại quái.
6. Đỉnh hiệu ứng có các tia trắng vàng hướng lên.

- Màu: `#F1DA9C`, `#FFF8DC`.
- Mở vòng: 0,35–0,5s.
- Pha bùng sáng: 0,3–0,5s.
- Các nhịp sát thương sau đó chỉ dùng 30–50% cường độ của nhịp đầu.

**Quan trọng:** Không làm cả vòng phát sáng liên tục ở cường độ cực đại.

### 2.31. Holy Spirit Surge

<text color="secondary" size="xs">Ảnh 10, khoảng 525–575s</text>

**[QS]**

- Có tia hoặc vệt sáng xanh cyan bay về phía mục tiêu trong một khung.
- Một số khung xuất hiện quầng sáng vàng quanh quái.
- Các hit riêng biệt tạo nhiều số sát thương.
- Hiệu ứng chuyển động thiên về những dải sáng ngắn, không phải laser đặc.

**Thiết kế nên dựng [ĐX]:**

- Một vệt Holy xanh trắng kéo từ nhân vật đến mục tiêu.
- Mesh Trail có lõi trắng, viền cyan rất nhạt.
- Mỗi lần chạm tạo sao vàng nhỏ.
- 3–5 hạt ánh sáng tách khỏi điểm impact.
- Màu: `#B4F3E6`, `#FFF1BF`.
- Chiều rộng trail: 0,1–0,18H.
- Impact: 0,2–0,3s.
- Nếu nhiều hit, dùng các nhịp sáng ngắn tách biệt nhau.

---

## 2.32. Highness Heal, Hymn of the Lamb và Purification

Có ba điểm cần tách rõ để tránh gán nhầm VFX:

- **Highness Heal:** Mình tìm được tên và mô tả của skill hồi máu trong tài liệu RO3, nhưng không xác định chắc một chuỗi khung riêng biệt được ghi Highness Heal trong 120 ảnh. Không nên khẳng định một quầng hồi máu bất kỳ là của skill này.
- **Hymn of the Lamb:** Không xác định được khung có nhãn này rõ ràng. Những hiệu ứng Hymn khác có thể tham khảo về ngôn ngữ ánh sáng, nhưng không đủ cơ sở mô tả chính xác Hymn of the Lamb.
- **Purification:** Đây là tên skill trong bộ thiết kế Cleric của bạn; không nên tự động coi nó là một skill có hiệu ứng tương ứng trực tiếp trong những khung RO3 đã gửi.

[ĐX] Nếu muốn bổ sung Highness Heal theo phong cách hình ảnh chung của RO3, mình chọn một quầng hồi máu trắng xanh lớn hơn Heal khoảng 30–50%, thêm 2 cung ánh sáng và số hồi HP nổi bật, thay vì một cột sáng khổng lồ.

---

# 3. Bộ quy tắc VFX chung cho toàn bộ game — RO3 Soft Fantasy V1

Đây là phần mình khuyên **chốt làm tiêu chuẩn sản xuất**, không chỉ dành riêng cho Cleric mà còn áp dụng cho Warrior, Wizard, Assassin, Mechanic, pet và boss.

Tất cả con số bên dưới là thông số thiết kế đề xuất, chưa được đo từ Unity hoặc file hiệu ứng gốc của RO3.

## 12 quy tắc bắt buộc

### Quy tắc 1 — Một skill chỉ có một hình ảnh chủ đạo

- Heal: quầng hồi máu.
- Buff: thánh ấn hoặc biểu tượng.
- Khiên: vòng bảo vệ.
- Holy attack: tia sáng và impact.
- AoE: ma pháp trận dưới đất.

Không đưa cả 5 hình ảnh đó vào một skill nhỏ.

Một skill thông thường nên có 3–4 lớp VFX; skill mạnh có thể dùng 5–6 lớp.

### Quy tắc 2 — Ưu tiên hình học hơn Particle

Với các skill Cleric, mình đề xuất tỷ trọng cảm nhận hình ảnh:

- 55–65%: hình học chính, vòng phép, thánh ấn, tia sáng.
- 20–30%: glow mềm và chuyển động.
- 10–20%: hạt nhỏ, điểm sáng, bụi phép.

Đây là tỷ trọng về thị giác, không phải số lượng renderer hay diện tích pixel.

### Quy tắc 3 — FX thông thường không vượt quá 1,8H

Buff, heal và debuff đơn mục tiêu nên giữ trong khoảng 0,8–1,8H.

Chỉ AoE hoặc skill đặc biệt mới mở rộng nhiều lần chiều cao nhân vật.

### Quy tắc 4 — Đỉnh sáng ngắn, phần còn lại dịu

Một skill điển hình nên chia:

| Giai đoạn | Thời lượng [ĐX] | Cường độ tương đối |
|---|---:|---:|
| Anticipation | 0,10–0,25s | 25–45% |
| Activation | 0,15–0,30s | 85–100% |
| Sustain/Impact | 0,10–0,30s | 45–70% |
| Fade | 0,15–0,35s | Giảm về 0 |

Không giữ Bloom đỉnh suốt thời gian phép tồn tại.

### Quy tắc 5 — Không dùng Bloom để thay thế hình dạng

Đề xuất trong Unity:

- Có hai vùng màu: lõi emissive và lớp màu nền.
- Chỉ lõi phát sáng mạnh.
- Với dự án Linear HDR, có thể thử emission lõi ở mức 1,5–3 trước khi tone mapping.
- Glow phụ nên nhẹ hơn lõi khoảng 50–75%.
- Không tăng Bloom toàn camera chỉ để làm một skill nổi bật.

Những giá trị HDR này là mức thử nghiệm, không phải preset RO3 và phải kiểm tra trực tiếp với cấu hình render của bạn.

### Quy tắc 6 — Vòng đất phải được nhìn thấy trên mọi loại map

Vì game của bạn có đồng cỏ, tuyết, hang băng, đầm lầy và đấu trường boss:

- Đường viền rune alpha đỉnh 65–85%.
- Nền phép alpha 10–20%.
- Vòng có lõi sáng và viền tối nhẹ để không mất trên nền tuyết.
- Độ dày nét chính nên vào khoảng 2–4 px ở camera gameplay chuẩn 1280×720.
- Những đường rune phụ chỉ 1–2 px.

Không dùng một màu vàng nhạt quá yếu trên nền cát vàng hoặc một màu cyan quá yếu trên tuyết xanh.

### Quy tắc 7 — Tách rõ nền phép, thân phép và UI

Trong Unity 2D:

1. Ground VFX: nằm trên địa hình nhưng dưới chân/nhân vật.
2. World VFX: các tia và hạt xuất hiện quanh nhân vật.
3. Foreground VFX: những chi tiết thật sự cần đi phía trước nhân vật.
4. UI: tên skill, buff icon, số HP và sát thương.

Với vòng phép, nên dùng mesh nằm trên mặt phẳng thế giới rồi chiếu theo camera game của bạn, không đơn giản lấy một hình tròn UI dựng thẳng đứng.

### Quy tắc 8 — Mỗi chức năng có bảng màu cố định

- Holy: vàng ngà + trắng.
- Heal: xanh lá dịu + trắng ngà.
- Protection: xanh lam nhạt + vàng.
- Buff ATK: vàng kim.
- Purify: trắng xanh.
- Debuff: đỏ san hô hoặc tím nhạt.
- Ultimate: kết hợp màu Holy chính với một màu nhấn, không dùng rainbow.

Một skill nên có tối đa 2 màu chính và 1 màu lõi sáng.

### Quy tắc 9 — Mesh Trail phải mềm và ngắn

Cho các skill projectile và chém phép:

- Độ rộng trail: 0,08–0,22H.
- Đuôi trail tắt dần trong 0,12–0,25s.
- Phần đầu sáng nhất, phần sau giảm alpha.
- Không tạo vệt dài tồn tại xuyên qua cả màn hình.

Riêng AoE ma pháp trận, ưu tiên Line Mesh hoặc Ring Mesh hơn Trail.

### Quy tắc 10 — Các skill duy trì không được phát sáng liên tục như lúc cast

Sau khi skill kích hoạt:

- Giảm độ sáng xuống còn khoảng 15–30% đỉnh.
- Giữ một biểu tượng hoặc quầng thật nhỏ nếu cần.
- Chỉ flash trở lại khi có một sự kiện: hồi HP, nhận sát thương, giải hiệu ứng.

Đây là cách giữ màn hình boss 40 người dễ nhìn.

### Quy tắc 11 — Dùng số HP và sát thương làm một phần của cảm giác skill

Đề xuất:

- Hồi máu: `#8CDD74`.
- Sát thương thường: `#FFF5DC`.
- Holy damage: `#F5D16C`.
- Sát thương chí mạng: vàng đậm hơn và phóng to khoảng 15–20%.
- Mỗi mục tiêu có một cụm số riêng.
- Không để Particle đi xuyên qua số đến mức mất khả năng đọc.

Chữ và số nên nằm ở UI/canvas tách biệt khỏi Bloom của world VFX.

### Quy tắc 12 — LOD VFX cho 40 người đánh boss

| Mức | Hero chính | Đồng đội gần | Người chơi xa |
|---|---:|---:|---:|
| Hạt VFX | 100% | 40–60% | 10–25% |
| Rune chính | Đầy đủ | Đầy đủ | Đơn giản |
| Glow phụ | Đầy đủ | 50% | 0–20% |
| Distortion | Có chọn lọc | Tắt | Tắt |
| Màn sáng toàn cảnh | Không | Không | Không |

Đối với boss 40 người, phải ưu tiên telegraph nguy hiểm của boss cao hơn VFX tấn công hoặc buff của người chơi.

---

# 4. Ánh xạ 8 skill Cleric của bạn theo RO3

Đây là cấu hình cụ thể mình đề xuất để giao cho người dựng VFX trong Unity.

<text color="secondary" size="xs">Kích thước dùng H = chiều cao hiển thị của Cleric; thời gian là thời gian của VFX, không phải cooldown hay cast time gameplay.</text>

## Bảng ánh xạ tổng thể

| Skill Cleric | Skill RO3 tham chiếu | Độ ưu tiên |
|---|---|---|
| Heal | Heal + Highness Heal | Rất cao |
| Spirit Blessing (+ATK) | Blessing + Impositio Manus | Rất cao |
| Divine Aegis (+DEF) | Kyrie Eleison + Assumptio | Rất cao |
| Divine Light | Holy Light + Judex | Rất cao |
| Sanctuary | Sanctuary + Magnus Exorcismus (hình học) | Rất cao |
| Guardian Spirit | Basilica + Kyrie Eleison | Cao |
| Purification | Ruwach + Lex Divina (ngôn ngữ thanh tẩy) | Cao |
| Divine Ascension | Prayer of the Holy Spirit + Sacred Lily + Holy Sigma | Rất cao |

## 4.1. Heal

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_HEAL_V1</text>

**[ĐX]** Tái tạo cảm giác Heal của RO3, không làm giống phép hồi máu dạng nước.

- Lớp 1 — `Heal_Core`: hình cầu ánh sáng trắng xanh tại ngực, nở 0,15s.
- Lớp 2 — `Heal_Ring`: vòng sáng 1,2H mở từ 0,4H lên 1,2H.
- Lớp 3 — `Heal_Rays`: 4 tia sáng ngắn hướng lên.
- Lớp 4 — `Heal_Particles`: 8 hạt xanh vàng.
- Lớp 5 — `Heal_Number`: số HP xanh lá, do UI quản lý.

Màu: `#B8EBAA`, `#F5FFDA`, `#FFF5C8`.

Thời gian 0,65s. Bloom thấp-vừa. Không Distortion.

**Dấu hiệu nhận diện:** Một quầng xanh ấm bung nhẹ, kết thúc sạch.

## 4.2. Spirit Blessing (+ATK)

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_SPIRIT_BLESSING_V1</text>

**[ĐX]** Dựa trên Blessing và Impositio Manus.

- Lớp 1 — Ngôi sao bốn cánh vàng trên đầu.
- Lớp 2 — Vòng vàng mảnh 1,3H dưới chân.
- Lớp 3 — Hai đường ánh sáng vàng chạy lên thân.
- Lớp 4 — 6 hạt vàng kim nhỏ.
- Lớp 5 — Biểu tượng buff ATK xuất hiện ở HUD.

Màu: `#F2D078`, `#FFF1BC`, `#FFFBE5`.

Thời gian 0,65s.

Không để cột sáng kéo dài suốt thời gian +ATK.

## 4.3. Divine Aegis (+DEF, khiên)

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_DIVINE_AEGIS_V1</text>

**[ĐX]** Tham chiếu Kyrie Eleison và Assumptio.

- Lớp 1 — Vòng lục giác xanh vàng dưới chân, đường kính 1,4H.
- Lớp 2 — Một khiên bằng mesh trong suốt xuất hiện trước nhân vật.
- Lớp 3 — Hai cung sáng vòng quanh người, chiều cao 1,4H.
- Lớp 4 — Flash trắng ngà ở tâm khi kích hoạt.
- Lớp 5 — Khi nhận hit, khiên nhấp sáng 0,12–0,18s.

Màu: `#B9E5EA`, `#FFF0B8`.

Kích hoạt 0,75s; giữ viền khiên rất mờ khi buff còn hiệu lực, hoặc chỉ hiển thị khi nhận sát thương.

**Không làm:** bong bóng xanh to đục bao kín nhân vật. RO3 trong các khung tham chiếu có vẻ ưu tiên quầng bảo vệ nhẹ hơn.

## 4.4. Divine Light — Đạn sáng

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_DIVINE_LIGHT_V1</text>

**[ĐX]** Tham chiếu Holy Light + Judex.

- Lớp 1 — Tích tụ ánh sáng ở đầu gậy hoặc bàn tay.
- Lớp 2 — Đạn hình thoi vàng ngà, đường kính 0,3H.
- Lớp 3 — Mesh Trail trắng vàng dài 0,5H.
- Lớp 4 — Impact hình sao bốn cánh đường kính 1,1H.
- Lớp 5 — 4 hạt vàng bắn ra khỏi điểm chạm.

Màu: `#FFF9DC`, `#F6D076`.

Tích tụ 0,15–0,2s; impact 0,25s; thời gian bay phụ thuộc khoảng cách.

Với hệ thống khóa mục tiêu hiện tại của bạn, có thể cho đạn bay từ điểm cast tới tâm thân quái, còn impact gắn với điểm trúng ở phía người chơi nhìn thấy.

## 4.5. Sanctuary — Vùng hồi máu

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_SANCTUARY_V1</text>

**[ĐX]** Đây nên là skill gần RO3 nhất trong bộ Cleric.

- Lớp 1 — Vòng ngoài trắng vàng, kích thước theo bán kính gameplay.
- Lớp 2 — Vòng rune nhỏ phía trong.
- Lớp 3 — Lưới ô vuông hoặc thánh ấn trên mặt đất.
- Lớp 4 — Mảng ánh sáng xanh ngà alpha 12–18%.
- Lớp 5 — Những hạt hồi phục rất nhỏ bay lên.
- Lớp 6 — Flash hồi HP riêng ở từng mục tiêu khi tick.

Màu: `#E5F2C3`, `#F4DE9A`, `#FFF8DF`.

- Mở vòng: 0,45s.
- Sau đó duy trì nhẹ.
- Mỗi tick hồi máu: phát sáng trong 0,15s.

**Cực kỳ quan trọng:** Vòng Sanctuary phải đi theo mặt đất 2D nghiêng 3/4, không dựng một vòng tròn thẳng đứng trước camera.

## 4.6. Guardian Spirit

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_GUARDIAN_SPIRIT_V1</text>

**[ĐX]** Kết hợp Basilica + Kyrie Eleison, thêm nhận diện của riêng game bạn.

- Lớp 1 — Một thánh ấn nhỏ ở chân.
- Lớp 2 — Hai cánh ánh sáng mềm mở ra sau vai.
- Lớp 3 — Quầng vàng trắng nhỏ trên đầu.
- Lớp 4 — Một vòng ánh sáng nâng từ chân lên.
- Lớp 5 — Khi hiệu ứng bảo vệ kích hoạt, cánh hoặc khiên lóe sáng.

Màu: `#FFF2C7`, `#D7EAD6`.

Kích hoạt 0,8–1,1s.

Hai cánh chỉ xuất hiện trong khoảnh khắc ban đầu, không nên luôn dang rộng vì sẽ che các hero khác khi đánh boss.

## 4.7. Purification

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_PURIFICATION_V1</text>

**[ĐX]** Dùng ngôn ngữ ánh sáng của các skill Ruwach và phép hỗ trợ, nhưng thiết kế hành vi thanh tẩy riêng.

- Lớp 1 — Vòng trắng xanh dưới chân, đường kính 1,4H.
- Lớp 2 — Một thánh giá mảnh trên đầu.
- Lớp 3 — Các đốm tím/xám tượng trưng trạng thái xấu tách khỏi nhân vật.
- Lớp 4 — Những đốm bị phân rã bằng shader Dissolve.
- Lớp 5 — Flash trắng xanh kết thúc.

Màu: `#E3FAED`, `#B6E8D8`, màu debuff `#A58CBD`.

Thời gian: 0,65–0,85s.

Điểm nhận diện nên là **trạng thái xấu bị tách ra rồi tan biến**, không chỉ tạo thêm một vòng hồi máu.

## 4.8. Divine Ascension — Ultimate Cleric

<text color="success" weight="medium" size="sm">VFX thiết kế: CLERIC_DIVINE_ASCENSION_V1</text>

**[ĐX]** Học cấu trúc nghi lễ và ánh sáng từ Prayer of the Holy Spirit, Sacred Lily và Holy Sigma. Đây là tổng hợp phong cách, không phải sao chép một skill riêng trong clip.

- Lớp 1 — Vòng phép vàng lớn đường kính 4–5H.
- Lớp 2 — Thánh ấn dạng ngôi sao ở tâm.
- Lớp 3 — Ba vòng rune mở lần lượt.
- Lớp 4 — Một biểu tượng thiên thần hoặc cánh sáng vàng sau nhân vật.
- Lớp 5 — Cột ánh sáng trắng ngà cao 2,5–3H.
- Lớp 6 — Vòng sóng Holy tỏa ra trên mặt đất.
- Lớp 7 — Những hạt vàng bay lên và tan dần.

Màu: `#FFF9E1`, `#F3D47D`, `#DCEECF`.

<box gap={2}>
  <text weight="medium" size="sm">Nhịp animation đề xuất</text>
  <table>
    <table-row><table-cell width="32%">0–0,25s</table-cell><table-cell>Thánh ấn xuất hiện dưới chân</table-cell></table-row>
    <table-row><table-cell>0,25–0,55s</table-cell><table-cell>Các vòng rune mở rộng</table-cell></table-row>
    <table-row><table-cell>0,55–0,85s</table-cell><table-cell>Cánh sáng và cột Holy xuất hiện</table-cell></table-row>
    <table-row><table-cell>0,85–1,1s</table-cell><table-cell>Vòng sóng lan ra, đạt đỉnh</table-cell></table-row>
    <table-row><table-cell>1,1–1,45s</table-cell><table-cell>Hạt tan và ánh sáng giảm dần</table-cell></table-row>
  </table>
</box>

Giảm cường độ Bloom khoảng 40–50% ở chế độ xem hiệu ứng của đồng đội. Nếu nhiều Cleric cast cùng lúc, không được để các cột sáng biến màn hình thành màu trắng.

---

# 5. Phần nào của RO3 3D không nên bắt chước trong game 2D?

## 5.1. Không sao chép cột sáng 3D quá cao

RO3 có camera 3D và hình khối môi trường nên cột sáng có chiều sâu. Ở game 2D sprite, cột sáng cao dễ tạo cảm giác như một tấm ảnh billboard đứng thẳng.

**Cách sửa:** Dùng 2–3 mesh dạng dải cong giao nhau, feather mềm ở mép; cột chính ngắn hơn và chỉ tồn tại trong pha impact.

## 5.2. Không sử dụng Distortion mạnh

Trong game 3D, Distortion có thể làm méo cảnh 3D và tạo cảm giác năng lượng mạnh.

Với map painted 2D của bạn, Distortion dễ làm méo cỏ, đường đất và sprite, khiến hình bị nhòe hoặc rung không tự nhiên.

**Đề xuất:** Các phép Heal/Buff/Sanctuary dùng Distortion bằng 0. Chỉ một số ultimate, vụ nổ mạnh hoặc skill boss mới dùng, với độ dịch chuyển ảnh khoảng 1–3 px ở độ phân giải 720p.

## 5.3. Không bắt chước sự biến dạng phối cảnh của phép dưới đất

RO3 có mặt phẳng thế giới 3D, nên vòng phép và đường lưới biến dạng theo phối cảnh camera một cách tự nhiên.

Game 2D orthographic của bạn cần đặt mesh vào hệ tọa độ mặt đất thống nhất.

- Nếu camera orthographic nhìn gần như từ trên xuống: dùng vòng tròn mặt đất theo world space.
- Nếu camera giả 2.5D có pitch: dùng mesh thực sự nằm trên ground plane để camera tự chiếu.
- Nếu chỉ có background 2D vẽ sẵn: cần một quy tắc chiếu/tỷ lệ elip nhất quán theo phối cảnh tranh.

Không được cho vòng phép xoay mặt theo camera như một billboard nhân vật.

## 5.4. Không sử dụng bóng đổ 3D giả cho mọi hạt

Các hạt phép nhỏ trong game của bạn không cần tạo bóng xuống mặt đất.

Chỉ những vật thể phép đặc biệt như thánh trụ, thiên thạch hoặc kiếm ánh sáng lớn mới cần hiệu ứng tiếp xúc đất riêng.

## 5.5. Không bê nguyên mật độ FX khi có 40 người

Một màn hình minh họa một nhân vật trong RO3 có thể đẹp khi hiển thị đầy đủ hạt, tia và glow.

Nhưng cùng mật độ đó nhân lên 40 người sẽ gây ra:

- Che boss và telegraph.
- Mất khả năng phân biệt người chơi.
- Bloom chồng lấn.
- Overdraw cao.
- Tốn tài nguyên mobile.

Nên cho các hiệu ứng đồng đội dùng phiên bản Lite và chỉ hiện đầy đủ hiệu ứng của nhân vật chính.

## 5.6. Không dùng độ mờ theo khoảng cách camera giống 3D một cách máy móc

Trong RO3, chiều sâu và ánh sáng môi trường ảnh hưởng tới độ sáng cuối cùng của VFX.

Trong game 2D của bạn, nên giữ màu hiệu ứng nhất quán giữa map sáng, map tối và map tuyết.

Thay vì tăng giảm toàn bộ cường độ theo vùng, hãy điều chỉnh lớp glow mềm và giữ nguyên màu lõi để nhân vật luôn đọc được.

---

# 6. Cấu trúc triển khai Unity thực tế

Bạn đã chọn Particle System + Mesh Trail + Shader, không dùng sprite. Mình đồng ý với hướng đó cho phép thuật kiểu RO3, vì các hiệu ứng trọng tâm đều có thể dựng bằng hình học tương đối đơn giản.

## 6.1. Prefab mẫu cho mọi skill

```text
ClericSkill_VFX
│
├── Ground
│   ├── RingMesh
│   ├── RuneMesh
│   └── GroundGlow
│
├── Character
│   ├── CoreGlow
│   ├── SymbolMesh
│   └── RisingParticles
│
├── Impact
│   ├── FlashMesh
│   ├── MeshTrail
│   └── BurstParticles
│
└── Controller
    ├── VFXTimeline
    ├── ColorPalette
    └── VFXQualityLOD
```

Không phải skill nào cũng phải bật tất cả các thành phần. Prefab này là cấu trúc tổng quát để tái sử dụng.

## 6.2. Shader nên có những gì?

| Shader/module | Chức năng |
|---|---|
| Soft Additive | Lõi phép và hạt phát sáng |
| Alpha Blend | Lớp glow mềm, vùng phép |
| Rune Shader | Vẽ rune và vòng hình học |
| Dissolve | Thanh tẩy, tan biến |
| Trail Shader | Đạn Holy, tia chém |
| Radial Gradient | Vòng sáng, sóng năng lượng |
| Depth/Sorting Control | Giữ thứ tự giữa VFX và sprite |

Với Unity Built-in RP, URP 2D hoặc camera giả 2.5D, cách triển khai render và sorting sẽ khác nhau, nhưng bộ nguyên tắc hình ảnh phía trên vẫn dùng chung được.

Riêng các thánh ấn phức tạp, nên tạo mesh vector/procedural hoặc SDF từ hình học, không bắt buộc dùng ảnh sprite. Những họa tiết quá phức tạp không cần thiết ở kích thước nhân vật nhỏ.

## 6.3. Ngân sách hiệu năng đề xuất

Đối với một nhân vật Cleric trên PC/mobile:

| Skill | Particle đồng thời [ĐX] | Cường độ |
|---|---:|---|
| Heal | 8–12 | Nhẹ |
| Spirit Blessing | 6–10 | Nhẹ |
| Divine Aegis | 6–10 | Nhẹ |
| Divine Light | 5–9 | Nhẹ |
| Sanctuary | 15–25 | Vừa |
| Guardian Spirit | 10–16 | Vừa |
| Purification | 8–14 | Nhẹ |
| Divine Ascension | 30–45 | Cao |

Không có nghĩa toàn bộ 40 người cùng cast 45 particle sẽ tự động an toàn. Phải giới hạn tổng số VFX visible, số emitter, số material pass và diện tích overdraw của những lớp glow lớn.

---

# 7. Chốt phong cách cuối cùng cho game bạn

Nếu phải chọn một bộ tham chiếu hình ảnh từ RO3 để tất cả skill của game bạn đồng bộ, mình sẽ chốt như sau:

| Hạng mục | Chuẩn nên học |
|---|---|
| Buff cá nhân | Blessing / Assumptio |
| Hồi máu | Heal / Highness Heal |
| Bảo vệ | Kyrie Eleison / Safety Wall |
| Đạn phép | Holy Light / Judex |
| Vùng phép | Sanctuary / Magnus Exorcismus |
| Ultimate | Holy Reckoning / Holy Sigma |
| Thánh ấn, thiên thần | Sacred Lily / Prayer of the Holy Spirit |
| Trạng thái xấu | Lex Aeterna / Lex Divina |

## Đánh giá mức độ nên bám theo RO3

<box gap={3}>
  {#each [
    {name:"Bảng màu Holy pastel",v:95},
    {name:"Vòng phép và hình học",v:95},
    {name:"FX gọn, dễ đọc",v:100},
    {name:"Nhịp sáng, impact, fade",v:90},
    {name:"Particle và ánh sáng",v:85},
    {name:"Cột sáng và phối cảnh 3D",v:45}
  ] as item}
    <box gap={1}>
      <row align="center" justify="between">
        <text size="sm">{item.name}</text>
        <text color="secondary" size="xs" tabularNums>{item.v}%</text>
      </row>
      <box background="surface-tertiary" height="7px" radius="full" clip>
        <box background="rgba(162,153,103,0.9)" width={`${item.v}%`} height="100%" radius="full"/>
      </box>
    </box>
  {/each}
  <caption>Điểm mức độ ưu tiên học theo RO3 do mình đề xuất, không phải điểm giống hiện tại của game bạn.</caption>
</box>

**Kết luận:** Mình khuyên bạn chọn **RO3 Soft Holy Fantasy V1** làm chuẩn VFX thống nhất.

Đặc trưng cần giữ nhất là:

1. Hình học ma pháp rõ ràng.
2. Ánh sáng vàng ngà, xanh lá, xanh lam dịu.
3. FX gọn, nhân vật vẫn là trọng tâm.
4. Ít hạt nhưng chuyển động có chủ đích.
5. Vòng phép đẹp ở mặt đất, không cần quá nhiều cột sáng.
6. Mỗi skill có một hình tượng dễ nhận biết.
7. Bloom có điểm nhấn, không phát sáng tràn lan.
8. AoE hoành tráng nhờ rune, vòng phép và nhịp sáng, không phải vì Particle dày.

Với Cleric, mình sẽ ưu tiên làm **Heal → Spirit Blessing → Divine Aegis → Sanctuary → Divine Light → Guardian Spirit → Purification → Divine Ascension**. Sau khi năm skill đầu đạt cùng một chất lượng màu sắc và nét vẽ, chúng sẽ tạo được thư viện module để dựng ba skill còn lại nhanh hơn.

Đặc biệt, **Sanctuary và Divine Ascension nên là hai skill mẫu để nghiệm thu chất lượng VFX**: Sanctuary kiểm tra khả năng tạo vòng phép trên mọi map, còn Divine Ascension kiểm tra liệu hiệu ứng lớn có thật sự giống phong cách RO3 mà vẫn không che nhân vật và boss.

Điều mình sẽ không khẳng định từ bộ ảnh này là thời lượng chính xác từng phép, kích thước world-space thật, thông số Bloom gốc hoặc thứ tự các lớp Particle nội bộ của RO3. Các phần đó cần video liên tục hoặc dữ liệu game để đo, còn bộ thông số phía trên là một style guide có thể bắt đầu triển khai và tinh chỉnh trực tiếp trong Unity.

---

