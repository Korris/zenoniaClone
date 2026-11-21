<!-- ChatGPT (Tay "Tân Korris") 21/11/2026 https://chatgpt.com/c/6b017962-84d4-83ec-8b76-80286bcd2cc4 ; nguồn: khung trích từ https://www.youtube.com/watch?v=t-LNpJN5H54 . Khung video KHÔNG commit (giữ ngoài git ở D:/temp/ro-trailer-frames). -->

# Phân tích skill FX theo trailer "RO" người dùng thích (21/11)

Người dùng (21/11): "thực sự về phần skill tôi không muốn warrior xài skill như màu đỏ hiện tại, nó không phải style ragnarok; đây là clip trailer về skill ragnarok https://www.youtube.com/watch?v=t-LNpJN5H54 — hãy phân tích cùng chatgpt để sau này prompt có thể đáp ứng tốt mong muốn của tôi".

Template dán vào brief / prompt: `docs/reference/ro-trailer-skill-style/prompt-templates.md`. Quy tắc rút gọn: đầu `docs/sprite-rules.md` (mục "RO trailer style").

## 0. Video

- Tiêu đề: "Midgard Survivors – Now in 6 Languages, 10,000+ Downloads | Free Survivors-like Trailer" (96,7 s, 1080p). Midgard Survivors là game survivors-like "with the soul of a classic MMORPG" (RO cổ điển: Prontera, Poring, Swordman/Knight/Lord Knight, Wizard, Priest, Assassin, Monk; MVP Satan Morroc, Baphomet, Valkyrie Randgris, Golden Thief Bug, Osiris, Thanatos).
- Nhân vật sprite 2D trên map 3D nghiêng (giống RO1), camera xa: nhân vật cao ~55-60 px trên khung 960x540.
- Khung trích: 96 khung mỗi 1 s (0,5-95,5 s) + 3 sheet dày (0,25-0,45 s) quanh Bash / Magnum Break (30,6-33,4 s), Lord Knight vs Satan Morroc / Baphomet (57,6-62,6 s), Lord Knight vs Golden Thief Bug (71,2-74,0 s). Panel "Damage" góc phải ghi tên skill đang dùng nên biết đúng skill nào ra hình nào.

## 1. Quan sát của Claude (xem từng khung)

| Giây | Class / skill | Hình | Màu (lấy mẫu pixel) | Cỡ so với H |
|---|---|---|---|---|
| 31,4 | Knight Bash | QUẠT phẳng nằm đất phía trước, ~120°, mép ngoài mềm, lõi trắng loé quanh người + tia sáng mảnh toả ra | quạt #E4D970 / #F5EA7C / đỉnh #FFF988, lõi #FEFDF8; thấy rõ cỏ + cây xuyên qua quạt | bán kính ~280 px ≈ 4,5-5H |
| 31,6-31,9 | Magnum Break / Bash dư | elip vàng chanh nằm đất + vòng trắng mảnh giữa, tan trong ~0,3 s | #EDF280 + trắng | ĐK ~2-3H |
| 72,0-73,5 | Lord Knight Bash + Spiral Pierce vào Golden Thief Bug | quạt vàng champagne rất to phủ sàn (trong, thấy gạch) + 2-3 vòng elip vàng-trắng nghiêng quanh mục tiêu (Spiral Pierce), sao trắng ở điểm trúng | quạt #C9B389-#E4CD9D trên nền tối, vòng #D5C378 + #FFFFFF; mép san hô nhạt #D1806B rất ít | quạt ≈ 5-6H, vòng 2-2,5H |
| 58,5-62,6 | Lord Knight vs Satan Morroc, Baphomet | loé trắng-vàng ở người + mục tiêu, quạt vàng mờ, cột sáng vàng đổ từ trên xuống mục tiêu (Baphomet 61,7) | vàng ngà / trắng; ĐỎ chỉ ở vòng rune của boss | cột ~4-5H |
| 1,5 | (Wizard?) cột sáng / Meteor | 2 cột sáng vàng cam đứng | #C79E5F / #E4E2B1 | cao ~5H |
| 5,5-6,5, 34,5, 46,5, 86,5-88,5 | High Wizard Gravitation Field / Storm Gust | ma pháp trận XANH: vòng kép + ngôi sao 6 cánh + chấm sáng chạy vòng, nét mảnh, rất trong | #76ADCE #7391CB, tím #987DE0, lõi trắng | ĐK ~8-10H |
| 35,5-36,5 | Assassin Sonic Blow | chùm vệt chém nhọn san hô-hồng quanh mục tiêu, ngắn | #F37860 | ~2-3H |
| 39,5-41,5 | Monk Asura Strike / Holy Light | chữ Hán to mờ trắng, ấn trắng-cyan, tia trắng | trắng / cyan nhạt | 3-6H |
| 42,5, 43,5 | Archer / Hunter | vòng elip vàng nhạt tại mục tiêu, mưa tên trong vòng vàng trong | #F1E9A1 | 3-5H |
| 44,5, 80,5-81,5 | Priest / High Priest Magnus, Sanctuary | lưới / vòng trắng-xanh, ngôi sao, quầng trắng | trắng + #6AB4DE | 4-8H |
| 52,5-60,8, 65,5-68,5 | BOSS telegraph (Satan Morroc, Valkyrie, Randgris) | vòng rune ĐỎ đất rất to, sọc đỏ, vòng đỏ đánh số, chữ cảnh báo đỏ | #CB6F43, #D04C34, #8D372F | 6-12H |

Rút ra:
1. **Đỏ = boss nguy hiểm.** Trong cả trailer, đỏ đậm chỉ là cảnh báo boss (vòng rune, sọc, vòng đánh số). Kỹ năng người chơi gần như không dùng đỏ (ngoại lệ: Sonic Blow san hô, lửa cam-vàng của phép Fire). Warrior đỏ của mình đọc như vùng boss sắp đánh.
2. **Swordman / Knight / Lord Knight = ánh sáng vàng chanh / champagne / trắng ngà**, không phải lửa. Lực vật lý thể hiện bằng quạt sáng + loé trắng + vòng elip.
3. **To nhưng trong:** quạt Bash 4,5-6H, ma pháp trận 8-10H, nhưng thân alpha thấp (thấy cỏ, gạch, quái qua FX), không viền tối, không khối đặc. Hoành tráng nhờ DIỆN TÍCH + hình học rõ, không nhờ độ đục / độ đậm.
4. **Hình phẳng nằm đất là chính** (quạt, elip, ma pháp trận theo phối cảnh mặt đất); phần đứng chỉ là cột sáng, tia, sao loé.
5. **Nhịp ngắn:** quạt Bash bung và tan trong ~0,3-0,5 s (31,4 -> 31,9 còn dư elip); loé trắng 1-2 khung.
6. **Số dam to, màu theo loại** (trắng / vàng / cam viền đen) nổi trên FX; FX không che số.

So với Warrior hiện tại (`docs/reference/warrior-fx-grand/after-round3-snow-8-skills.jpg`): 8/8 skill đỏ-cam lửa, viền tối đỏ mận, thân bão hoà đục, nhiều vòng đỏ cam đặc quanh người, cột lửa, vết cháy: đúng kiểu "Fire warrior ARPG", sai cả màu (trùng màu boss) lẫn chất liệu (đục thay vì trong).

## 2. ChatGPT vòng 1 (nguyên văn, đã bỏ thẻ hiển thị của ChatGPT)

Gửi: 6 sheet 1 s (01, 03-07), 3 sheet dày, ảnh Warrior đỏ 8 skill, màu đã lấy mẫu; hỏi: định nghĩa RO style, vì sao Warrior đỏ không ra RO, màu theo class / nguyên tố, hình dạng, lớp, cỡ, nhịp, camera, palette từng class + boss, 8 skill Warrior, 3 template.

Lưu ý: bảng cỡ của vòng 1 QUÁ NHỎ (Power Slash R 1,2H...) và timeline cố định (Earth Break cắm 520 ms) đã bị vòng 2 sửa: dùng số ở vòng 2.

## ART DIRECTION SPEC V1.0 — RAGNAROK CLASSIC SKILL VFX

Unity 2D/2.5D RPG · Particle System + Mesh Trail + Shader · 21/11/2026

**Phán quyết:** Chủ game nhận xét đúng. Warrior hiện tại của bạn có chất lượng kỹ thuật khá tốt, nhưng ngôn ngữ hình ảnh đang nghiêng về *Fire Warrior / Action RPG hiện đại*, không phải *Swordman → Knight → Lord Knight của Ragnarok cổ điển*.

Tôi đề xuất **thay đổi art direction của toàn bộ 8 skill, nhưng giữ lại hệ thống Particle, Mesh Trail, Bloom, Distortion và Dissolve**. Không cần chuyển sang sprite animation.

Vấn đề lớn nhất không phải bạn dùng màu đỏ chưa đúng mã hex, mà là cả tám skill đều dùng gần như cùng một cách biểu đạt sức mạnh: đỏ cam, lửa, vệt chém nóng, vòng sáng lớn, nhiều lớp chồng lên nhau.

Trong trailer Midgard Survivors mà bạn cung cấp, sức mạnh của Knight/Lord Knight thường được thể hiện bằng **trắng ngà, vàng chanh nhạt, vàng kim, xung lực hình học và những chớp sáng ngắn**, chứ không phải cứ tấn công mạnh là sinh ra lửa đỏ.

### Quyết định nền tảng

| Thành phần | Chốt cho game |
|---|---|
| Style tổng thể | RO Classic FX Language, modernized |
| Warrior chủ đạo | Ivory White + Pale Gold + Soft Yellow |
| Warrior phụ | Steel Blue + Champagne Gold |
| Warrior lửa | Chỉ các skill thực sự mang nguyên tố Fire |
| Độ trong | Ưu tiên các lớp xuyên thấu, không dùng khối đặc lớn |
| Hình dạng | Quạt, cung kiếm, vòng xung lực, tia, ngôi sao, ấn phép |
| Tầng mặt đất | Quan trọng, nhất là AoE và skill có phạm vi |
| Bloom | Nhẹ, tập trung ở lõi và thời điểm va chạm |
| Boss warning | Đỏ gạch, đỏ cam, đỏ nguy hiểm, tách khỏi player FX |
| Công nghệ | Giữ procedural VFX, không cần sprite frame |
| Mục tiêu | Đạt khoảng 85–90% cảm giác VFX Ragnarok cổ điển, đồng thời sắc nét hơn ở độ phân giải hiện đại |

Các tỷ lệ, kích thước và thời gian bên dưới là **thông số thiết kế đề xuất**, không phải thông số được đo trực tiếp từ engine của Midgard Survivors.

---

## 1. Điều gì thực sự định nghĩa Ragnarok-style FX?

### 1.1. Đọc trực tiếp từ trailer

Các hình bạn gửi cho thấy ba nhóm hình ảnh rất khác nhau.

| Nhóm khung | Đặc điểm nổi bật | Kết luận |
|---|---|---|
| Bash / Magnum, 31.4–31.9 s | Quạt vàng chanh nhạt, hình nêm rõ, lõi sáng; quạt vẫn thấy mặt đất bên dưới | Sát thương vật lý có thể biểu hiện bằng ánh sáng chứ không cần lửa |
| LK Goldbug, 71.5–73.5 s | Vùng vàng champagne lớn, vòng sáng mềm, vệt đánh ngắn, một vài vùng trắng bùng | Chiêu mạnh có diện tích rộng nhưng không cần nhiều hình dạng sắc nhọn |
| LK Boss, 57.6–62.6 s | Chớp vàng/trắng quanh nhân vật và quái, tương phản với ma pháp trận đỏ của MVP | Player FX và boss telegraph có hệ màu riêng |
| Storm Gust / Gravity, 5.5–6.5 s, 34.5 s | Ma pháp trận xanh lam lớn, các đường hình học mảnh, nhiều vùng trong suốt | Magic có thể rất lớn nhưng vẫn đọc được nền |
| Assassin / Sonic Blow, 35.5–36.5 s | Cụm vệt đánh đỏ san hô tập trung, thời gian xuất hiện ngắn | Đỏ được dùng có chọn lọc, không phủ toàn màn hình |
| MVP Satan Morroc, 52.5–60.5 s | Vòng rune đỏ đất rất lớn, nhiều đường báo nguy hiểm | Đỏ đậm là tín hiệu nhận biết cơ chế boss |

Một lưu ý: trailer không chứng minh rằng tất cả skill gốc đều dùng Additive, cũng không cho biết alpha, shader hay timing nội bộ. Các con số trong spec sau là cách tái tạo **cảm giác thị giác** của ảnh tham chiếu, không phải reverse-engineering chính xác.

### 1.2. So sánh Warrior hiện tại với ngôn ngữ RO

Tôi chấm theo mức độ giống *classic RO* chứ không chấm độ đẹp độc lập.

| Tiêu chí | Warrior hiện tại | Hướng RO cần đạt |
|---|---|---|
| Màu | 8/8 skill đều thuộc họ đỏ/cam | Khoảng 6/8 skill thiên về neutral/gold/steel |
| Viền | Vệt nóng đỏ, cam và các lõi sáng mạnh | Mép mềm, sáng nhạt, ít đường viền tối |
| Vật liệu | Lửa, plasma, năng lượng cháy | Ánh kiếm, luồng lực, phép hỗ trợ |
| Hình dạng | Nhiều cung tròn cháy, slash lớn | Mỗi skill có silhouette riêng |
| Độ trong | Có nhiều vùng sáng dày đặc, lấn nhân vật | Cho phép nhìn thấy người và nền xuyên qua FX |
| Bloom | Ánh nóng khá thường xuyên | Chỉ tăng mạnh vào impact |
| Mặt đất | Có vòng và ấn nhưng đi kèm nhiều lớp lửa | Vòng mặt đất sạch, tách riêng khỏi vệt đánh |
| Nhịp | Một số skill duy trì lượng năng lượng cao khá lâu | Flash ngắn, rõ, sau đó tan nhanh |
| Nhận diện class | Fire swordsman | Knight vật lý / Holy-neutral knight |

**Điểm giống phong cách Ragnarok cho toàn bộ bộ skill hiện tại: khoảng 5/10.** Đây là đánh giá art direction từ ảnh mẫu, không phải đánh giá kỹ thuật shader hay animation.

### 1.3. Tám quy tắc tạo cảm giác Ragnarok

#### A. Light first, fire second

Với skill vật lý neutral:

- 60–75% diện tích hình sáng nên là trắng ngà hoặc vàng nhạt.
- 15–30% là màu vàng/steel thể hiện chuyển động.
- 5–10% là các điểm nhấn rất sáng.
- Không dùng đỏ lửa làm lớp nền chủ đạo.

Đây là tỷ lệ thị giác mục tiêu theo diện tích hiệu ứng trong các khung ở thời điểm bùng, không phải tỷ lệ chính xác của trailer.

#### B. Transparency without losing shape

Dùng hai loại lớp cùng tồn tại:

- **Shape layer:** quạt, vòng, slash, alpha 0.35–0.65, thường Alpha Blend.
- **Highlight layer:** những đường mảnh và lõi trắng/vàng, alpha 0.60–0.90, Additive.

Không nên biến mọi lớp thành Additive, vì khi nhiều skill đánh cùng lúc, chúng hòa thành một mảng trắng.

#### C. Không có viền đen dày quanh VFX

RO có viền khá rõ ở nhân vật sprite và nhiều asset môi trường, nhưng các FX sáng như Bash/Spiral Pierce trong trailer thường không có outline đen như một món đồ được vẽ.

Đề xuất:

- Vệt sáng: không có outline tối.
- Vòng phép: viền cùng họ màu, đậm hơn phần lõi khoảng 15–25% độ sáng cảm nhận.
- Đá vỡ/vật thể vật chất: được có nét tối nhẹ theo style bản đồ.
- Bóng đổ: không dùng để làm viền cho vệt năng lượng.

#### D. Silhouette quan trọng hơn số lượng particle

Bash cần được nhìn thành **một quạt vàng** trước khi người xem thấy những tia nhỏ.

Spiral Pierce cần đọc thành **một xung lực xoắn/hướng đâm**.

Magnum Break cần đọc thành **một vòng bùng từ tâm**.

Tôi đề xuất nguyên tắc: trong 100 ms đầu tiên của impact, người chơi phải nhận diện được hình học chủ đạo của skill, kể cả khi ẩn toàn bộ hạt phụ.

#### E. Mặt đất và không trung có vai trò khác nhau

- Ground layer: bán kính tác động, ma pháp trận, lan sóng, phạm vi AoE.
- Air layer: vệt kiếm, quạt chém, tia, cột sáng, cú đánh.
- Contact layer: điểm trúng mục tiêu, ngôi sao, flash, bụi.

VFX có thể có cả ba, nhưng không phải skill nào cũng cần cả ba.

#### F. Peak mạnh nhưng ngắn

Đối với một skill đánh thường:

- Prepare: 80–160 ms.
- Bùng sáng: 40–100 ms.
- Dư âm: 120–240 ms.

Một Ultimate có thể duy trì lâu hơn, nhưng phải có nhiều đỉnh nhịp thay vì một quầng sáng sáng đều từ đầu đến cuối.

#### G. Có khoảng trống bên trong FX

Trong các khung Lord Knight, hiệu ứng nhìn rộng nhưng nhiều vùng là nền xuyên qua được. Đây là một lý do nó không giống một quả cầu lửa đặc.

Thiết kế nên dành ít nhất 45–65% diện tích bounding box lớn nhất của FX cho vùng rỗng hoặc rất trong, tùy skill.

#### H. Player FX không cạnh tranh màu với boss warning

Ở các khung Satan Morroc, vùng vòng đỏ lớn có chức năng cảnh báo. Nếu cả Warrior cũng liên tục tạo vòng đỏ cùng màu và kích thước, người chơi sẽ khó phân biệt đâu là skill của mình, đâu là vùng boss sắp gây sát thương.

Đây là vấn đề gameplay, không chỉ thẩm mỹ.

---

## 2. Ngôn ngữ màu theo class và nguyên tố trong Ragnarok

Cần phân biệt hai khái niệm:

1. **Ngôn ngữ thị giác của job/class:** người xem nhận ra nghề nghiệp nhờ cách chuyển động, vũ khí, hình dạng và màu.
2. **Nguyên tố gameplay:** Neutral, Fire, Water, Wind, Earth, Holy, Shadow, Ghost, Poison...

Ragnarok không quy định mỗi class chỉ được một màu. Swordman có thể sử dụng Magnum Break thuộc Fire; Rune Knight có Dragon Breath; Assassin có Enchant Poison. RO gốc có hệ nguyên tố tách khỏi job, và các skill như Bash, Bowling Bash, Spiral Pierce là ví dụ hữu ích cho tuyến Knight.

### 2.1. Bản đồ màu theo job

| Job RO | Màu thường nhận diện | Hình dạng FX đặc trưng |
|---|---|---|
| Swordman | Trắng, vàng nhạt, steel | Quạt ngắn, star impact, cung kiếm |
| Knight | Vàng champagne, trắng ngà | Xung lực vũ khí, quạt, vòng đẩy |
| Lord Knight | Gold, ivory, đôi khi tím/steel ở buff | Xoắn lực, ánh kiếm, vòng buff |
| Rune Knight | Gold + Fire/Water/Wind tùy rune | Rune, cột lửa, sóng nguyên tố |
| Crusader | Ivory, vàng kim, xanh lam nhạt | Khiên sáng, dấu thập, vòng bảo vệ |
| Paladin | Trắng ngọc, gold, xanh lam | Grand Cross, thánh ấn, kết giới |
| Mage | Xanh, cyan, tím, cam tùy phép | Bolt, vòng phép, tia năng lượng |
| Wizard / High Wizard | Xanh băng, lam, tím; lửa cho Fire | Lục giác, sao phép, vùng AoE lớn |
| Acolyte / Priest | Trắng, vàng kem, xanh mint | Halo, vòng hồi phục, thánh ấn |
| Thief / Assassin | Tím, teal, san hô ở sát thương nhanh | Slash mảnh, afterimage, chuỗi hit |
| Merchant / Blacksmith | Gold, cam đồng, steel | Tia kim loại, impact nặng, bánh răng |
| Mechanic | Steel, cyan, đồng vàng, muzzle flash cam | Cannon, bánh răng, khói, projectile |
| Monk / Champion | Trắng, vàng ấm, teal/blue | Chấn lực từ nắm đấm, aura, vòng khí |

**Lưu ý quan trọng:** Đây là ngôn ngữ màu được chuẩn hóa cho dự án của bạn dựa trên tinh thần RO, không phải palette chính thức áp dụng cho mọi bản Ragnarok.

### 2.2. Bảng màu theo nguyên tố

| Nguyên tố | Màu nền | Màu lõi | Cảm giác |
|---|---|---|---|
| Neutral | `#D5C378` | `#FEFDF8` | Lực vật lý, xung lực |
| Fire | `#D9874D` | `#FFF2B5` | Nóng, bùng nổ |
| Water | `#5EACCF` | `#E7FBFF` | Dòng chảy, sóng, giọt |
| Wind | `#80CBB3` | `#F0FFF6` | Xoắn, nhẹ, tốc độ |
| Earth | `#A89463` | `#E4D5A1` | Đá, bụi, nặng |
| Holy | `#E8CD86` | `#FFFFFF` | Phép thánh, bảo vệ |
| Shadow | `#554C83` | `#BCABEA` | Bóng tối, hấp thụ |
| Ghost | `#907FCE` | `#F2E7FF` | Linh thể, phi vật chất |
| Poison | `#76B65D` | `#DCFFC1` | Độc, sương, giọt |

Không nên nhầm màu FX với hệ nguyên tố trong công thức damage. Ví dụ, một cú đánh Knight Neutral hoàn toàn có thể sử dụng ánh vàng như Bash mà không biến thành Holy.

Trong Ragnarok, Poison và Ghost là các thuộc tính riêng, không đơn thuần là các sắc thái của Shadow.

---

## 3. Từ vựng hình dạng, tỷ lệ, thời gian và cách xếp lớp

Đây là phần nên đưa thẳng vào tài liệu kỹ thuật của VFX artist và Unity agent.

### 3.1. Chuẩn đo kích thước H

Để các class không có FX lớn nhỏ thất thường, tôi đề xuất lấy chiều cao hiển thị của một nhân vật chibi chuẩn làm đơn vị.

**H = chiều cao nhân vật từ đỉnh đầu đến bàn chân trong ảnh gameplay, không tính bóng, tên, thanh HP, vũ khí nhô lên hoặc FX.**

Ví dụ, nếu Hero render cao 96 px trên màn hình ở mức zoom chuẩn:

| Kích thước | Ví dụ với H = 96 px |
|---|---:|
| 0.25H | 24 px |
| 0.5H | 48 px |
| 1H | 96 px |
| 1.5H | 144 px |
| 2H | 192 px |
| 3H | 288 px |
| 4H | 384 px |

Những con số này chỉ là ví dụ. Không lấy 192 px của sprite source làm H, vì kích thước texture gốc không nhất thiết bằng kích thước render.

Với vòng tròn nằm trên mặt đất, đo **đường kính theo không gian mặt đất**, không đo đường kính elip đang thấy trên màn hình. Camera nghiêng 50–60° sẽ làm vòng tròn bị nén theo phối cảnh. Vì vậy cần kiểm tra cả đường kính world-space lẫn silhouette screen-space.

### 3.2. Bộ từ vựng hình dạng

Các thông số sau là preset đề xuất cho hero size H.

| Hình dạng | Dùng cho | Kích thước | Thời gian |
|---|---|---|---|
| Quạt sáng (Sector) | Bash, Power Slash | Bán kính 0.9–1.5H, góc 90–125° | 120–260 ms |
| Cung kiếm (Slash Arc) | Chém ngang, combo | Bán kính 0.8–1.7H, góc 110–220° | 100–240 ms |
| Vòng elip mặt đất | Shockwave, buff, AoE | Đường kính 1.5–4H | 180–600 ms |
| Vòng tròn lan | Magnum-like burst | Đường kính 1–3H | 140–350 ms |
| Ngôi sao impact | Trúng đòn | 0.25–0.75H, 6–10 tia | 50–160 ms |
| Tia thẳng (Streak) | Charge, Pierce | Dài 1–4H, rộng 0.05–0.3H | 90–250 ms |
| Cột sáng | Holy, ultimate | Cao 1.5–5H | 250–850 ms |
| Ma pháp trận lục giác | Wizard | Đường kính 2–5H | 400–1500 ms |
| Ngôi sao 6 cánh | Spell cấp cao | Đường kính 2–6H | 350–1200 ms |
| Vòng rune mảnh | Buff, seal | 1.5–4H | 250–1000 ms |
| Hạt bụi/đốm sáng | Impact phụ | 0.02–0.12H mỗi hạt | 150–450 ms |
| Vệt bóng sau người | Dash, Assassin | 0.7–1.2H mỗi bóng | 80–200 ms |
| Chữ skill lớn | MMO classic notification | UI riêng, không phải FX chính | 250–700 ms |

Trong trailer, các chữ *Phase 2*, *Fire Rain*, *Death's Scythe* và tên boss tạo cảm giác MMO. Tuy nhiên tôi không khuyến nghị đưa chữ lớn vào mọi skill của người chơi.

**Chốt:** Chỉ hiện thông báo chữ lớn cho boss phase, skill boss quan trọng, combo đặc biệt hoặc sự kiện. Skill Warrior thông thường không cần chữ bay giữa màn hình.

### 3.3. Cấu trúc sáu lớp chuẩn cho một skill

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<box border radius="xl" padding={3} gap={2}>
  Ví dụ: Power Slash — thứ tự render và độ nổi bật
  <box gap="1px">
    {#each [
      {name:"L0 · Ground mark",desc:"Vòng chân hoặc vệt tiếp xúc · Alpha Blend",w:38,c:"#AAA06E"},
      {name:"L1 · Soft body",desc:"Quạt vàng nhạt, mềm, trong · Alpha Blend",w:77,c:"#D5C378"},
      {name:"L2 · Main slash",desc:"Cung kiếm quyết định silhouette · Alpha/Add",w:94,c:"#E4D970"},
      {name:"L3 · Bright edge",desc:"Đường mép mảnh vàng trắng · Additive",w:68,c:"#FFF0AC"},
      {name:"L4 · Contact flash",desc:"Sao impact tại mục tiêu · Additive",w:45,c:"#FFF9DC"},
      {name:"L5 · Residue",desc:"Hạt nhỏ, tia sót lại · Alpha Blend",w:25,c:"#B5AA7E"}
    ] as l}
      <row align="center" gap={2}>
        <box width="94px" flex="0 0 94px" gap={1}>
          <box background={l.c} width={`${l.w}%`} height="10px" radius="xs"/>
        </box>
        <box flex="1" gap="2px">
          {l.name}
          <caption>{l.desc}</caption>
        </box>
      </row>
    {/each}
  </box>
  <caption>Độ dài thanh minh họa mức ưu tiên thị giác, không phải alpha hay thời lượng.</caption>
</box>
```

Trong Unity, **thứ tự L0–L5 là thứ tự logic**, không có nghĩa mọi particle phải dùng sáu Sorting Order liên tiếp. Hiệu ứng sẽ có phần ở dưới chân và phần ở phía trước/sau nhân vật.

Một kỹ năng thường chỉ cần 3–5 lớp hoạt động. Skill ultimate có thể dùng 6–8 lớp nếu các lớp có nhiệm vụ khác nhau.

#### Quy tắc thiết kế từng lớp

| Layer | Chức năng | Alpha mục tiêu | Blend |
|---|---|---|---|
| Ground | Định vị, phạm vi, cảm giác tiếp đất | 0.12–0.35 | Alpha |
| Main body | Hình chính của skill | 0.35–0.65 | Alpha |
| Energy detail | Đường kiếm, luồng khí, rune | 0.40–0.75 | Alpha/Add |
| Bright core | Lõi trắng, cạnh sáng | 0.65–0.95 | Add |
| Contact impact | Xác nhận hit | 0.70–1.00 | Add |
| Afterglow | Tàn dư, vòng lan | 0.08–0.30 | Alpha/Add |

Alpha trong bảng là giá trị mục tiêu của từng lớp trước khi cộng sáng và tone mapping. Không thể suy ra độ sáng cuối cùng chỉ từ alpha nếu shader dùng HDR emission.

### 3.4. Nhịp chuẩn theo cấp độ skill

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<box border radius="xl" padding={3} gap={3}>
  Timeline mẫu — Skill chém thường 400 ms
  <svg viewBox="0 0 340 116" width="100%" xmlns="http://www.w3.org/2000/svg">
    {#each [0,100,200,300,400] as n}
      <line x1={20+n*0.75} y1="12" x2={20+n*0.75} y2="103" stroke="currentColor" opacity="0.12"/>
      {n} ms
    {/each}
    {#each [
      {name:"Prepare",start:0,end:100,y:22,c:"#8BA5BA"},
      {name:"Main slash",start:95,end:260,y:44,c:"#D5C378"},
      {name:"Peak hit",start:125,end:180,y:66,c:"#F3D874"},
      {name:"Residue",start:180,end:400,y:88,c:"#A69C80"}
    ] as t}
      <rect x={20+t.start*0.75} y={t.y} width={(t.end-t.start)*0.75} height="14" rx="3" fill={t.c}/>
      {t.name}
    {/each}
  </svg>
  <caption>Peak bắt đầu cùng animation impact, không cần chờ particle đạt kích thước lớn nhất.</caption>
</box>
```

| Loại skill | Prepare | Peak | Dissolve/Tail | Tổng |
|---|---:|---:|---:|---:|
| Basic Slash | 60–100 ms | 40–75 ms | 120–200 ms | 220–375 ms |
| Heavy Slash | 100–180 ms | 70–120 ms | 180–300 ms | 350–600 ms |
| Dash Attack | 100–200 ms | 50–100 ms | 130–250 ms | 300–550 ms |
| AoE Physical | 150–250 ms | 80–150 ms | 250–400 ms | 500–850 ms |
| Large Ultimate | 250–500 ms | 120–250 ms | 450–850 ms | 900–1600 ms |
| Channeled Spell | 250–450 ms | Nhiều đỉnh | 300–600 ms | Theo gameplay |

**Quan trọng:** Gameplay hit time và VFX peak phải được đồng bộ bởi cùng event animation/network. Không được để visual timing tùy thuộc vào FPS hoặc thời gian particle tự chạy.

### 3.5. Bloom, Distortion, camera

Các giá trị dưới đây là mức thử nghiệm ban đầu, không phải preset tuyệt đối cho mọi dự án Unity.

| Thông số | Skill thường | Ultimate | Boss |
|---|---|---|---|
| Bloom threshold | 1.1–1.4 HDR | 1.0–1.2 HDR | 1.0–1.3 HDR |
| Bloom intensity URP tham khảo | 0.15–0.35 | 0.30–0.55 | 0.25–0.50 |
| Screen distortion | 0–2 px | 2–5 px | 2–6 px |
| Camera shake | 0–0.025H screen | 0.03–0.07H | 0.03–0.08H |
| Shake duration | 0–80 ms | 80–160 ms | 80–180 ms |
| Hit-stop offline | 20–35 ms | 30–55 ms | 0–50 ms |
| Full-screen flash | Không | Hạn chế, tối đa 1–2 frame | Chỉ khi có lý do gameplay |

`Bloom intensity` phụ thuộc pipeline, volume và color grading; giá trị này cần hiệu chỉnh bằng screenshot, không chuyển nguyên xi giữa Built-in RP và URP.

Với game online:

- Hit-stop chỉ nên tác động animation và hiệu ứng cục bộ của client.
- Không dừng simulation hoặc network tick.
- Camera shake không được làm dịch chuyển UI.
- Trong trận 40 người đánh boss, giảm hoặc tắt shake từ skill đồng đội.
- FX flash lớn không được che vùng telegraph.

---

## 4. Master Palette — bảng màu giao thẳng cho Unity

### 4.1. Nguyên tắc dùng palette

Tôi phân ra bốn màu:

- **Outer:** vùng sáng thấp hoặc biên bên ngoài.
- **Body:** chiếm diện tích nhiều nhất của FX.
- **Bright:** highlight thể hiện lực và chuyển động.
- **Core:** phần sáng nhất, dùng ít.

Màu Core không có nghĩa phải tạo một quả cầu ở tâm. Đối với kiếm, core thường là một đường sắc mảnh, còn đối với cannon, core nằm ở điểm nổ.

### 4.2. Palette theo class

Màu hiển thị dưới đây là mã sRGB để thiết kế, không phải HDR emission đã tone-map.

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<box gap={3}>
  {#each [
    {name:"Warrior — Neutral / Knight Gold",colors:["#B2A873","#D5C378","#F5EA7C","#FEFDF8"],alpha:"Outer .18–.32 · Body .40–.60 · Bright .55–.80 · Core .75–.95",blend:"Body Alpha · Bright/Core Add"},
    {name:"Wizard — Arcane / Ice",colors:["#527FA7","#76ADCE","#A9D9EF","#F2FBFF"],alpha:"Outer .15–.30 · Body .28–.50 · Bright .45–.70 · Core .70–.95",blend:"Magic circle Alpha · Bolts Add"},
    {name:"Mechanic — Steel / Arcane Tech",colors:["#587A88","#84B7C5","#D5E8E9","#FFF5D8"],alpha:"Outer .20–.35 · Body .40–.65 · Bright .55–.80 · Core .80–1",blend:"Steel/Smoke Alpha · Muzzle Add"},
    {name:"Assassin — Poison / Shadow",colors:["#526B69","#75B987","#B4E1A0","#F1FFE1"],alpha:"Outer .18–.35 · Body .30–.55 · Bright .55–.78 · Core .70–.90",blend:"Mist/Slash Alpha · Hit Add"},
    {name:"Cleric — Holy / Protection",colors:["#BBAE88","#E8D398","#FFF0C1","#FFFFFF"],alpha:"Outer .12–.28 · Body .28–.48 · Bright .45–.70 · Core .70–.95",blend:"Halo Alpha · Seal/Core Add"},
    {name:"Fighter — Spirit / Impact",colors:["#709D9C","#A4D6C9","#F4E7AE","#FFFFFF"],alpha:"Outer .18–.30 · Body .38–.58 · Bright .55–.85 · Core .80–1",blend:"Air/Impact Alpha · Spirit Add"},
    {name:"Boss — Danger Telegraph",colors:["#8D372F","#CB6F43","#E6A36A","#FFE0A7"],alpha:"Outer .25–.45 · Body .40–.65 · Bright .65–.90 · Core .75–1",blend:"Danger pattern Alpha · Explosive impact Add"}
  ] as p}
    <box border radius="lg" padding={3} gap={2}>
      {p.name}
      <grid columns={4} gap={1}>
        {#each p.colors as c,i}
          <grid-item>
            <box background={c} height="36px" radius="sm"/>
            {["Outer","Body","Bright","Core"][i]}
            {c}
          </grid-item>
        {/each}
      </grid>
      <caption>{p.alpha}</caption>
      <caption>{p.blend}</caption>
    </box>
  {/each}
</box>
```

#### Palette phụ bắt buộc

| Trường hợp | Outer | Body | Bright | Core |
|---|---|---|---|---|
| Warrior Steel | `#718B9D` | `#A5BCCD` | `#DEE9EF` | `#FFFFFF` |
| Warrior Fire | `#B67C50` | `#E0A35A` | `#F6D586` | `#FFF9DE` |
| Wizard Arcane Purple | `#665B9E` | `#987DE0` | `#C9BAF2` | `#FCF7FF` |
| Wizard Wind | `#5C9F93` | `#91D4BF` | `#D1F4E6` | `#F8FFFA` |
| Mechanic Explosion | `#95654A` | `#D8A35E` | `#F7DB98` | `#FFFFFF` |
| Assassin Shadow | `#554B79` | `#8774B5` | `#BBADE0` | `#F4EEFF` |
| Assassin Sonic Hit | `#B96064` | `#F37860` | `#FFC1A0` | `#FFF8ED` |
| Fighter Aura | `#A08B60` | `#D6BD80` | `#F5E6AF` | `#FFFFFF` |
| Boss Fire | `#963D32` | `#DA834F` | `#EAAB40` | `#FFF1BF` |

Đối với game dùng RO Soft Fantasy pastel, tôi khuyến nghị dùng các màu này làm **điểm xuất phát cho texture tint và vertex color**. Shader có thể đẩy emission lên nhưng phải giữ đúng quan hệ sáng-tối giữa Outer, Body, Bright và Core.

### 4.3. Những màu phải cấm trong Player Telegraph

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<box border radius="lg" padding={3} gap={2}>
  Reserved: Boss Danger System
  <grid columns={4} gap={2}>
    {#each ["#D04C34","#CB6F43","#C83832","#8D372F"] as c}
      <grid-item>
        <box background={c} height="42px" radius="sm"/>
        {c}
      </grid-item>
    {/each}
  </grid>
  Đây là các màu dành riêng cho **hình báo nguy hiểm**: sector trên mặt đất, vòng rune boss, đường thẳng cảnh báo, mũi tên báo tấn công và countdown.

  Không có nghĩa cấm hoàn toàn sắc đỏ khỏi mọi phép của người chơi.
</box>
```

**Quy tắc sử dụng:**

1. Player không được tạo ma pháp trận mặt đất đỏ trầm có cùng hình dạng và độ đậm như boss warning.
2. Player Fire được dùng cam vàng, nhưng không dùng cùng một màu đỏ gạch làm đường giới hạn AoE.
3. Player Assassin được dùng đỏ san hô `#F37860` ở vệt đánh ngắn, nhưng không dùng nó làm telegraph sàn.
4. Player buff không được có vòng đỏ đất trùng boss enrage.
5. Khi skill người chơi nằm trong vòng cảnh báo boss, boss warning phải được ưu tiên về độ rõ hình học.

Nếu mục tiêu là đọc màu thật tốt trong trận 40 người, nên giữ thêm khác biệt về hình: **Player = đường mềm, xung lực, rune mảnh; Boss = vùng hình học phẳng, đường biên liên tục, hatch/pulse rõ**. Như vậy người bị rối loạn nhận biết màu vẫn phân biệt được.

---

## 5. Thiết kế lại toàn bộ 8 skill Warrior

Tôi chốt Warrior mới theo hướng:

**Knight of Light and Steel** — không phải Holy Paladin, không phải Fire Berserker.

Warrior dùng năng lượng chiến đấu Neutral có ánh vàng, thép và trắng ngà. Vẫn giữ vài kỹ năng nguyên tố để tạo cao trào nhưng các skill lửa không được thống trị cả bộ.

### 5.1. Tổng quan tám skill

| Skill | Hình chủ đạo | Màu chốt | Kích thước tối đa | Tổng FX |
|---|---|---|---|---:|
| Power Slash | Quạt vàng + sao impact | Ivory Gold | R 1.2H | 340 ms |
| Whirlwind | Hai vòng khí vàng/steel | Pale Gold + Steel | R 1.7H | 600 ms |
| Flame Cleave | Một cung kiếm cam-vàng | Warm Gold Fire | R 1.45H | 450 ms |
| Charging Strike | Mũi lao sáng + vệt dash | White + Steel Gold | Dài 3H | 490 ms |
| Earth Break | Ấn vàng + kiếm rơi + nứt đất | Gold + Earth | R 2.2H | 1100 ms |
| Blade Cyclone | Vòng cung kiếm xoắn, có khoảng rỗng | Champagne Gold | R 2H | 900 ms |
| Inferno Crash | Bùng vàng lửa + sóng nhiệt | Fire Gold | R 2.3H | 1250 ms |
| Berserker | Aura vàng ngà + vệt chém tốc độ | Gold + Soft Crimson Accent | R 1.2H | 850 ms |

`R` là bán kính tính từ vị trí nhân vật hoặc điểm impact. Với Charging Strike, kích thước ghi theo chiều dài di chuyển hiển thị của vệt FX, không phải khoảng cách dash thực tế.

### 5.2. Power Slash

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<row align="center" gap={2}>
  *Neutral*
  Bash-inspired · Basic/Heavy Attack
</row>
```

**Hình ảnh:** một quạt sáng vàng chanh nhạt phía trước nhân vật, góc quạt khoảng 115°, cạnh trước sắc và lõi vàng trắng. Cuối quạt chạm mục tiêu xuất hiện sao va chạm 8 tia.

Đừng làm một cung lửa bao quanh Warrior. Người xem phải cảm nhận đây là cú đánh bằng kiếm thép có lực rất mạnh.

- Main Sector: `#D5C378`, alpha 0.45.
- Inner Highlight: `#F5EA7C`, alpha 0.68.
- Sword Edge: `#FEFDF8`, Additive 0.85.
- Sao impact: đường kính 0.4H, xuất hiện 60 ms.
- Bán kính quạt 1.2H, góc 115°.
- Không có scorch, flame tongue, ember.

**Timeline**

| Thời gian | Hành động |
|---|---|
| 0–90 ms | Nhấc kiếm, một vệt sáng hẹp hình thành |
| 90–145 ms | Quạt mở nhanh 0→115° |
| 145 ms | Damage event + star impact |
| 145–220 ms | Quạt giữ silhouette nhưng giảm alpha |
| 220–340 ms | Tan hoàn toàn |

Kết quả cần đạt: tương tự cảm giác quạt Bash trong sheet 31.4–31.9 s, không phải sao chép chính xác.

### 5.3. Whirlwind

*Neutral / Wind Motion*

**Hình ảnh:** Warrior xoay kiếm tạo hai vòng cung ánh vàng nằm hơi nghiêng, với một luồng khí thép xanh rất nhạt ở phía ngoài.

Không tạo vòng lửa đặc. Vùng giữa nên đủ trong để vẫn thấy nhân vật và quái.

- Arc A: quét 220°, rộng 0.12H.
- Arc B: quét 260°, lệch 160 ms.
- Outer Air: `#A5BCCD`, alpha 0.26.
- Gold Body: `#D5C378`, alpha 0.50.
- White edge: `#FEFDF8`, Additive 0.75.
- Bán kính lớn nhất 1.7H.
- Mỗi arc có chỗ đứt hợp lý, không phải vòng tròn hoàn toàn khép kín.

**Timeline:** 0–120 ms lấy đà; 120–280 ms vòng thứ nhất; 280–440 ms vòng thứ hai; 440–600 ms tan.

Nếu gameplay có nhiều tick damage, đặt tick tại khoảng 220 và 380 ms. Không đặt tick damage chỉ vì particle quay qua quái nếu thiết kế gameplay không hỗ trợ.

### 5.4. Flame Cleave

*Fire — Controlled*

Đây là một trong hai skill có thể giữ lửa rõ rệt.

**Hình ảnh:** một nhát kiếm dài với lớp màu vàng mật ong, bên ngoài có một lượng nhỏ ngọn lửa cam. Tập trung vào cạnh kiếm sáng chứ không phải ngọn lửa bao trùm.

- Arc chủ đạo: `#F6D586`.
- Thân: `#E0A35A`.
- Viền ngoài: `#B67C50` rất mờ.
- Core: `#FFF9DE`.
- Góc chém 135°.
- Bán kính 1.45H.
- Flame tongues: tối đa 5–7 cụm nhỏ, lifespan dưới 200 ms.
- Không tạo mặt đất đỏ, không có vòng boss-style.

**Timeline:** 0–115 ms chuẩn bị; 115–210 ms chém; 170–230 ms impact; 230–450 ms lửa tắt.

Lửa chỉ nên chiếm khoảng 20–30% diện tích hiệu ứng chính tại peak, phần còn lại là ánh kiếm vàng trắng.

### 5.5. Charging Strike

*Neutral / Mobility*

**Hình ảnh:** kiếm và khiên hướng về phía trước; một mũi nhọn năng lượng trắng-steel dẫn đường, sau nhân vật là hai vệt gió mảnh.

Dùng spear-like force về mặt VFX, nhưng **không biến Warrior cầm kiếm và khiên thành nhân vật dùng thương**.

- Mũi lực phía trước dài 0.8H, rộng 0.25H.
- Vệt dash kéo dài tối đa 3H.
- Vàng champagne `#D5C378`.
- Steel blue `#A5BCCD`.
- Core `#FFFFFF`.
- Có afterimage của chuyển động, nhưng không tạo bóng nhân vật nguyên vẹn bằng particle nếu không cần.
- Impact star 0.6H, sau đó một vòng xung lực mặt đất 1.0–1.3H.

**Timeline**

| Thời gian | Hành động |
|---|---|
| 0–130 ms | Charge-up nhỏ |
| 130–310 ms | Dash trail bám theo người |
| 310–370 ms | Impact vào mục tiêu |
| 370–490 ms | Vòng lực tan |

Nếu thời gian dash gameplay thay đổi theo khoảng cách, nên đồng bộ bằng một normalized dash timeline thay vì cố định ở 180 ms.

### 5.6. Earth Break

*Neutral / Earth Impact · Ultimate*

Đây là skill hiện tại có thiết kế khá phức tạp và đã được đầu tư vào kiếm vẽ riêng, rune, nứt đất, cột và finisher. Tôi **không đề xuất bỏ toàn bộ cấu trúc**.

Thay vào đó, chuyển từ **Flaming Greatsword Apocalypse** sang **Golden Knight Judgment**.

**Giữ lại:** kiếm triệu hồi, ấn tròn dưới đất, contact compression flash, shockwave, crack mesh, các cột năng lượng và nhịp finisher.

**Sửa:**

- Kiếm: từ lửa đỏ cam thành ánh thép trắng ngà, rune vàng kim.
- Halo sau kiếm: từ cam nóng thành champagne gold nhẹ.
- Crack: đá nứt nâu đất, lõi vàng ngắn, không kéo dài như dung nham.
- Pillars: 3–5 cột sáng vàng/trắng thấp hoặc mảnh vỡ đất, không phải cột lửa liên tục.
- Finisher fireball: thay bằng **impact star vàng trắng + một vòng shockwave mở rộng**, giữ nguyên damage event.
- Scorch: dùng vệt đất tối nhẹ thay vì mảng đất cháy đen rõ.

Palette Earth Break

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<row gap={1} align="center">
  {#each ["#887C5A","#C6B37D","#F3E1A8","#FFFFFF"] as c}
    <box flex="1" background={c} height="35px" radius="sm"/>
  {/each}
</row>
```

**Kích thước:** Rune R 2.0H, shockwave lớn nhất R 2.2H. Kiếm triệu hồi có thể cao 2.3–2.8H nhưng phải giữ một silhouette thanh mảnh.

**Timeline đề xuất 1100 ms**

| Thời gian | Visual event |
|---|---|
| 0–160 ms | Rune mặt đất xuất hiện |
| 160–350 ms | Kiếm xuất hiện và tích năng lượng |
| 350–520 ms | Kiếm rơi |
| 520 ms | Impact + hit-stop |
| 520–640 ms | Compression flash, shockwave thứ nhất |
| 640–850 ms | Nứt đất, các cột năng lượng |
| 850–970 ms | Finisher star + shockwave thứ hai |
| 970–1100 ms | Rune và bụi tan |

Đây là timing thiết kế mới. Nếu animation hiện tại có những mốc damage khác thì giữ damage event và remap các giai đoạn visual tương ứng, không tự ý thay logic gameplay.

**Chốt:** Earth Break vẫn phải là một trong những skill ấn tượng nhất của Warrior, nhưng ấn tượng bằng **khối lực và trọng lượng**, không phải bằng số lượng lửa.

### 5.7. Blade Cyclone

*Neutral / Spinning Blade*

**Hình ảnh:** một hệ 2–3 vòng cung ánh kiếm quay quanh Warrior, mỗi vòng hơi nghiêng khác nhau. Vùng trung tâm rỗng, phần rìa có các mũi nhọn ánh sáng ngắn.

- Vòng trong R 1.0H.
- Vòng ngoài R 2.0H.
- Body `#D5C378`, alpha 0.30–0.50.
- Bright `#F5EA7C`, alpha 0.65.
- Core trắng chỉ trên cạnh arc.
- Không dùng vòng plasma đỏ kín.
- Không tạo lốc lửa cao che nhân vật.

**Timeline:** 0–150 ms tạo vòng; 150–650 ms xoay và gây damage; 650–900 ms giảm tốc, tan. Nên có ba đỉnh sáng tại khoảng 210, 390 và 570 ms để người chơi thấy nhịp combo.

Skill này có thể lấy cảm hứng từ cảm giác quét kiếm liên tục của Knight nhưng không cần sao chép một animation RO cụ thể.

### 5.8. Inferno Crash

*Fire — Ultimate*

Đây là skill lửa chính, được phép mạnh hơn Flame Cleave.

**Hình ảnh:** Warrior nhảy hoặc dồn lực đập xuống đất, sinh ra vòng bùng vàng kim, cam ấm và một lõi trắng. Lửa lan theo vòng thấp, không dựng thành bức tường cháy kín.

- Ground seal vàng nhạt R 1.7H.
- Fire impact R 1.6H.
- Shockwave tối đa R 2.3H.
- Body `#E0A35A`.
- Bright `#F6D586`.
- Core `#FFF9DE`.
- Vùng đỏ trầm chỉ được xuất hiện trong bụi đất nóng rất nhỏ, không dùng đường rune đỏ.
- Ember tối đa 12–20 hạt nhỏ trong trường hợp solo.

**Timeline:** 0–260 ms charge; 260–470 ms leap/strike; 470–610 ms impact; 610–900 ms vòng lửa lan; 900–1250 ms tàn dư biến mất.

Ở peak, vàng trắng phải nổi hơn cam đỏ. Hình ảnh nên gợi *Magnum Break cấp cao kết hợp một cú ground slam*, thay vì vụ nổ nham thạch kiểu ARPG tối màu.

### 5.9. Berserker

*Buff / Frenzy · Special Accent*

Đây là trường hợp được phép sử dụng một ít đỏ san hô hoặc crimson để thể hiện trạng thái cuồng chiến.

Nhưng **không dùng một vòng lửa đỏ cháy quanh Warrior**.

Thiết kế:

- Một vòng rune vàng champagne R 0.9H dưới chân.
- Ba đường sáng trắng-vàng bốc từ thân lên.
- Hai nhịp pulse mạnh.
- Aura chủ đạo vàng ngà.
- Đỏ san hô `#F37860` chỉ ở 2–3 vệt chéo cực ngắn hoặc icon buff.
- Không dùng `#D04C34` làm vòng cảnh báo.

**Timeline kích hoạt:** 0–180 ms tích lực; 180–310 ms pulse thứ nhất; 310–530 ms pulse thứ hai; 530–850 ms aura thu nhỏ.

Trong thời gian buff, để một halo vàng rất mỏng dưới chân hoặc vài vệt speed streak khi chém. Không giữ cả hiệu ứng kích hoạt suốt thời gian buff.

#### Tổng phân bổ màu sau khi làm lại

| Họ màu | Số skill | Ghi chú |
|---|---:|---|
| Gold / Ivory Neutral | 4 | Power Slash, Whirlwind, Earth Break, Blade Cyclone |
| Steel / Gold Neutral | 1 | Charging Strike |
| Gold / Fire | 2 | Flame Cleave, Inferno Crash |
| Gold / Crimson Accent | 1 | Berserker |

Như vậy **5/8 skill không còn là lửa, 2/8 là Fire thật, 1/8 có điểm nhấn đỏ nhỏ**. Điều này tạo ra bộ Warrior có nhận diện tương tự Knight trong trailer hơn, đồng thời vẫn giữ tên và vai trò của cả tám skill.

---

## 6. Unity VFX Implementation Standard

Phần này bổ sung để agent Unity có thể thực hiện mà không phải đoán các thuật ngữ mỹ thuật.

### 6.1. Kiến trúc khuyến nghị

Mỗi skill có một prefab root, ví dụ `VFX_Warrior_PowerSlash_RO`.

```text
VFX_Warrior_PowerSlash_RO
├── Ground
│   └── GroundRing_Mesh
├── Main
│   ├── SectorMesh
│   └── SlashTrail
├── Highlights
│   ├── EdgeTrail
│   └── EnergyParticles
├── Impact
│   ├── StarburstMesh
│   └── ContactFlash
├── Residue
│   └── SmallParticles
└── VFXController
```

Không bắt buộc mọi skill có đủ các nhánh. Tắt nhánh không sử dụng.

#### Thành phần được phép

| Thành phần | Mục đích |
|---|---|
| Particle System | Spark, glow particle, bụi, hạt khí |
| Mesh Trail | Vệt kiếm, lưỡi chém, xoáy |
| Procedural Mesh | Sector, fan, annulus, polygon rune, sao |
| Shader Graph / custom shader | Alpha soft edge, emission, dissolve |
| Line Renderer / Mesh | Đường rune, tia, giới hạn vòng |
| Post Processing | Bloom nhẹ, distortion có kiểm soát |
| Timeline / custom controller | Đồng bộ visual event |

Không cần sprite frame. Đối với các hình trang trí trên ấn phép, có thể dựng bằng mesh/line hoặc texture mask phi-animation nếu quy định dự án cho phép. Nếu muốn **100% không sử dụng texture sprite**, dùng procedural SDF, mesh và shader tính hình trực tiếp.

### 6.2. Blend Mode chuẩn

**Alpha Blend** cho vùng có diện tích lớn:

```text
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
```

**Additive** cho cạnh năng lượng và flash:

```text
Blend SrcAlpha One
ZWrite Off
```

Hai trạng thái trên là cách blend thông dụng, chưa phải shader hoàn chỉnh. Nếu dùng premultiplied alpha, cấu hình blend cần thay đổi tương ứng.

Không sử dụng Additive cho toàn bộ sector lớn khi đã có nhiều player đứng cạnh nhau.

#### HDR emission

Đề xuất bắt đầu bằng:

- Main body: emission khoảng 0–0.8, tùy shader.
- Bright edge: 1–2.
- Peak core: 2–4 trong thời gian ngắn.
- Boss warning ground: ưu tiên màu/độ tương phản ổn định, emission thấp.

Đây là hệ số thử nghiệm tương đối nếu shader dùng emission HDR nhân với màu tuyến tính. Chúng không tương đương trực tiếp với độ sáng cuối cùng dưới ACES, Gamma hay Linear.

### 6.3. Sorting, perspective và ground projection

Với game của bạn đang dùng sprite chibi trên mặt đất 2.5D:

- Ground seal phải nằm đúng world ground plane.
- Camera nghiêng thì ground seal tự bị foreshorten đúng như nền.
- Trail của kiếm nên follow hướng tấn công trong world-space hoặc camera-projected plane phù hợp.
- Particle bốc lên phải có chiều cao thật, không được trở thành hình dán phẳng vào nền.
- Hit marker cần anchor vào vị trí trúng quái, không luôn đặt ở tâm sprite.

Nếu dùng SpriteRenderer Sorting Layer cho nhân vật và MeshRenderer cho FX, cần thiết kế quy tắc chung về render queue/sorting để tránh FX luôn nằm trước mặt Hero.

**Với một cú slash:** phần vệt kiếm có thể đi trước hoặc sau thân tùy góc quay, trong khi star impact phải hiển thị tại mục tiêu. Không nên ép mọi lớp cùng một Sorting Order.

### 6.4. Performance Budget cho mobile và boss 40 người

Dưới đây là **mức ngân sách khởi điểm để benchmark**, không phải đảm bảo FPS cho mọi thiết bị.

| Hạng mục | Hero solo | Skill của đồng đội trong trận đông |
|---|---:|---:|
| Particle sống đồng thời/skill | 20–60 | 4–15 |
| Trail chính | 1–3 | 0–1 |
| Mesh chính | 2–6 | 1–3 |
| Peak Additive | Đầy đủ | Giảm 40–70% |
| Ground FX | Đầy đủ nếu liên quan gameplay | Ẩn nếu thuần trang trí |
| Distortion | Có giới hạn | Tắt |
| Camera shake | Có giới hạn | Tắt |
| Bloom | Theo scene | Không dùng volume riêng cho từng nhân vật |

Không chỉ đếm particle: cần đo overdraw, fill-rate, shader variant, texture bandwidth, SetPass/draw calls và CPU simulation.

Tôi khuyến nghị bốn quality tier:

- **Ultra:** Self 100%, Party 70%, Other 35%.
- **High:** Self 100%, Party 55%, Other 20%.
- **Medium:** Self 80%, Party 35%, Other 10%.
- **Low:** Self 65%, Party 20%, Other chỉ giữ contact hit tối thiểu.

Các tỷ lệ áp dụng cho chi tiết FX trang trí, không phải độ rõ gameplay. Cảnh báo boss, vùng gây sát thương và trạng thái thiết yếu phải được giữ dù ở Low.

### 6.5. Test cảnh đông người

Bắt buộc test tối thiểu bốn cảnh:

1. Hero đứng một mình trên nền tuyết sáng.
2. Hero đánh 5 quái trên cỏ và đường đất.
3. 10 người cùng đánh boss.
4. 40 người đánh boss, đồng thời boss phát telegraph đỏ.

Nếu skill đẹp ở cảnh 1 nhưng che mất mục tiêu ở cảnh 4, skill đó **chưa đạt art direction cho game online**, dù screenshot solo rất đẹp.

---

## 7. TEMPLATE A — Skill VFX Brief

Template này dùng để giao việc cho lập trình viên hoặc Unity agent. Điền các trường trong `{…}`. Tôi đưa cả tiếng Việt và tiếng Anh.

### A1. Bản tiếng Việt

## SKILL VFX BRIEF — RO CLASSIC SOFT FANTASY

**Dự án:** {Tên game}  
**Class:** {Warrior / Wizard / Mechanic / Assassin / Cleric / Fighter / Boss}  
**Tên skill:** {Tên skill}  
**Vai trò gameplay:** {Single-target / AoE / Dash / Buff / Ultimate / Telegraph}  
**Nguyên tố:** {Neutral / Fire / Water / Wind / Earth / Holy / Shadow / Ghost / Poison}  
**Camera:** {Góc nghiêng, FOV, orthographic/perspective}  
**Chiều cao hero H:** {Số px khi render ở zoom chuẩn hoặc số world unit}  
**Pipeline:** {Unity Built-in / URP}  
**Nền thử nghiệm:** {Tên map sáng / tối}

### 1. Mục tiêu phong cách

Tạo skill theo ngôn ngữ VFX của Ragnarok Online cổ điển, tham chiếu cảm giác hình ảnh của trailer Midgard Survivors: ánh sáng mềm, sắc vàng ngà/pastel, silhouette hình học rõ, nhiều vùng trong suốt, impact nhanh và có trọng lượng.

Không làm theo kiểu dark fantasy, Diablo, lửa plasma đặc, neon sci-fi hoặc anime beam quá chói, trừ khi được nêu rõ cho skill này.

Không dùng sprite frame animation. Chỉ dùng Unity Particle System, Mesh Trail, procedural mesh, shader, Bloom, Distortion và Dissolve.

### 2. Silhouette chính

- Hình dạng chủ đạo: {Quạt / Cung kiếm / Vòng / Cột / Tia / Ma pháp trận / Sao impact}
- Hướng phát triển: {Theo hướng tấn công / Từ tâm lan ra / Từ trên xuống / Xoắn quanh hero}
- Kích thước tối đa: {…H}
- Góc mở hoặc góc quét: {…°}
- Tỷ lệ vùng rỗng/trong: {…%}
- Vị trí anchor: {Weapon socket / Hero feet / Target hit point / World position}

Hình chính phải nhận diện được khi tắt toàn bộ particle trang trí.

### 3. Palette

| Vai trò | Hex | Alpha | Blend |
|---|---|---|---|
| Outer | {#…} | {…} | {Alpha} |
| Body | {#…} | {…} | {Alpha} |
| Bright | {#…} | {…} | {Additive} |
| Core | {#…} | {…} | {Additive} |

Không được tự thay palette sang đỏ/cam vì muốn skill trông mạnh hơn.

### 4. Danh sách layer

- Ground: {Hình dạng, kích thước, màu, alpha, thời gian}
- Main Body: {Hình dạng, kích thước, màu, alpha, thời gian}
- Energy Trail: {Mesh/Trail, độ rộng, độ cong, màu}
- Bright Core: {Vị trí, độ sáng, độ rộng}
- Impact: {Starburst, shockwave, flash}
- Residue: {Số hạt, lifespan, dissolve}

Không bắt buộc tạo đủ sáu layer nếu skill đơn giản.

### 5. Timeline và đồng bộ

- T = 0 ms: {Bắt đầu skill}
- T = {…} ms: {Prepare}
- T = {…} ms: {Main FX xuất hiện}
- T = {…} ms: {Damage event / hit event}
- T = {…} ms: {Peak brightness}
- T = {…} ms: {Dissolve}
- T = {…} ms: {FX kết thúc}

**Tổng thời lượng:** {…ms}  
**Hit-stop client-side:** {…ms}  
**Damage tick:** {Các mốc ms được gameplay cung cấp}

Không thay gameplay timing nếu không được yêu cầu.

### 6. Shader và render

- Blend chính: {Alpha Blend}
- Blend highlight: {Additive}
- Emission: {…}
- Bloom: {…}
- Distortion: {…px hoặc tắt}
- Ground alignment: {World ground plane}
- Sorting: {Quy tắc tương tác với sprite hero, quái, props}
- Dissolve: {Soft radial / Noise / Directional}
- Camera shake: {Biên độ, thời lượng hoặc tắt}

### 7. Điều tuyệt đối không được làm

- Không dùng boss-warning red cho FX mặt đất của người chơi.
- Không tạo flame tongue và ember nếu skill không mang Fire.
- Không dùng outline đen quanh lớp năng lượng.
- Không che hero hoặc target quá lâu.
- Không dùng Additive cho mọi layer.
- Không tăng kích thước/bloom chỉ để tạo cảm giác mạnh.
- Không làm lệch hình học mặt đất khi đổi góc camera.

### 8. Performance

- Platform mục tiêu: {PC / Mobile / Cả hai}
- Concurrent players: {…}
- Max particle sống đồng thời: {…}
- Max trail: {…}
- Max mesh renderer: {…}
- Other-player LOD: {…}
- Camera distortion của đồng đội: {Off}

### 9. Deliverables

1. Prefab skill hoàn chỉnh.
2. Material và shader reusable.
3. Tham số palette có thể chỉnh qua Inspector.
4. Timeline/VFX controller đồng bộ animation và gameplay.
5. Screenshot 3 thời điểm: prepare, peak, dissolve.
6. Video hoặc chuỗi ảnh thử ở nền sáng, nền tối và combat đông người.
7. Thống kê performance thực tế.

### 10. Acceptance Criteria

- RO Style Score ≥ {8.5}/10.
- Silhouette chính rõ trong ≤ {100} ms sau impact.
- Không trùng boss telegraph.
- Không mất nhân vật quá {100} ms vì độ chói.
- Màu phù hợp class và nguyên tố.
- Gameplay hit timing không thay đổi.
- Đạt mục tiêu performance trên thiết bị thử nghiệm.


### A2. English Version

## SKILL VFX BRIEF — RO CLASSIC SOFT FANTASY

**Project:** {Game Name}  
**Class:** {Warrior / Wizard / Mechanic / Assassin / Cleric / Fighter / Boss}  
**Skill:** {Skill Name}  
**Gameplay Role:** {Single-target / AoE / Dash / Buff / Ultimate / Telegraph}  
**Element:** {Neutral / Fire / Water / Wind / Earth / Holy / Shadow / Ghost / Poison}  
**Camera:** {Pitch, FOV, projection type}  
**Hero Height H:** {Rendered pixel height or world units}  
**Unity Pipeline:** {Built-in / URP}  
**Test Environment:** {Map and lighting}

### 1. Art Direction

Create a skill VFX based on the visual language of classic Ragnarok Online, using Midgard Survivors as a visual reference.

Prioritize soft ivory-gold highlights, pastel colors, readable geometric silhouettes, transparent energy, short bright impacts, and distinct physical weight.

Avoid dark-fantasy flames, heavy neon plasma, excessive bloom, or giant anime-style energy beams unless explicitly required by this skill.

Do not use frame-by-frame animated sprites. Implement the effect with Particle Systems, Mesh Trails, procedural geometry, shaders, Bloom, Distortion, and Dissolve.

### 2. Main Silhouette

- Primary shape: {Sector / Slash arc / Ring / Pillar / Ray / Magic circle / Impact star}
- Motion direction: {…}
- Maximum size: {…H}
- Arc or sector angle: {…degrees}
- Transparent/empty area: {…%}
- Anchor: {Weapon socket / Hero feet / Target impact point / World position}

The primary silhouette must remain recognizable with all decorative particles disabled.

### 3. Color Palette

| Role | Hex | Alpha | Blend |
|---|---|---|---|
| Outer | {#…} | {…} | Alpha |
| Body | {#…} | {…} | Alpha |
| Bright | {#…} | {…} | Additive |
| Core | {#…} | {…} | Additive |

Do not arbitrarily shift the palette toward red/orange to make an attack feel powerful.

### 4. VFX Layers

- Ground: {Shape, radius, color, opacity, lifetime}
- Main Body: {Shape, dimensions, color, opacity}
- Energy Trail: {Width, curvature, material, timing}
- Bright Core: {Position, width, intensity}
- Impact: {Starburst, flash, shockwave}
- Residue: {Particle count, fade, dissolve}

Use only the layers necessary for readability. Avoid unnecessary overdraw.

### 5. Timing

- T = 0 ms: {Start}
- T = {…} ms: {Anticipation}
- T = {…} ms: {Primary shape onset}
- T = {…} ms: {Gameplay hit event}
- T = {…} ms: {Brightness peak}
- T = {…} ms: {Dissolve begins}
- T = {…} ms: {VFX fully disappears}

Total duration: {…ms}  
Client-side hit-stop: {…ms}  
Damage ticks: {Authoritative gameplay timings}

Do not alter gameplay hit timing without approval.

### 6. Rendering

- Main Blend: {Alpha Blend}
- Highlight Blend: {Additive}
- HDR Emission: {…}
- Bloom: {…}
- Distortion: {…}
- Ground Projection: {World-ground aligned}
- Sorting Policy: {Hero / Enemy / Props}
- Dissolve: {Soft radial / Noise / Directional}
- Camera Shake: {Intensity / Duration / Disabled}

### 7. Forbidden Design Choices

- No player ground warnings using the reserved boss-danger palette.
- No flames or embers for non-Fire skills.
- No dark outlines around luminous energy.
- No prolonged full-body character occlusion.
- No excessive all-layer Additive blending.
- No unnecessary large glow spheres.
- No camera-dependent ground-circle deformation.

### 8. Performance

- Target Platform: {…}
- Concurrent Players: {…}
- Peak Particle Count: {…}
- Trail Count: {…}
- Mesh Renderer Count: {…}
- Other-Player LOD Policy: {…}
- Other-Player Camera Distortion: Disabled

### 9. Deliverables

1. Complete Unity VFX prefab.
2. Reusable materials and shaders.
3. Inspector-exposed palette parameters.
4. Gameplay-synchronized VFX controller.
5. Prepare, peak, and dissolve screenshots.
6. Bright/dark environment and crowded-combat tests.
7. Measured performance results.

### 10. Acceptance Criteria

- Ragnarok Style Score ≥ {8.5}/10.
- Recognizable primary silhouette within {100} ms of impact.
- No confusion with enemy telegraphs.
- No prolonged character or target occlusion.
- Correct class and elemental identity.
- Unchanged authoritative combat timing.
- Meets measured performance budget.


---

## 8. TEMPLATE B — ChatGPT Concept Image Prompt

Template này dành cho việc yêu cầu ChatGPT vẽ **ảnh concept VFX để duyệt art direction**, không phải tạo asset cuối cùng.

Điểm quan trọng là không để image model tự biến toàn bộ skill thành một vụ nổ đỏ cam chỉ vì nghe từ như *powerful*, *epic*, *ultimate*.

### B1. Bản tiếng Việt

Vẽ mới hoàn toàn từ đầu một CONCEPT VFX cho skill {TÊN SKILL} của class {CLASS} trong game RPG 2D chibi, phong cách RAGNAROK ONLINE CLASSIC / MIDGARD SURVIVORS, được hiện đại hóa nhẹ bằng ánh sáng và shader nhưng giữ nguyên ngôn ngữ hình ảnh RO cổ điển.

Góc nhìn 3/4 top-down, camera nghiêng {…} độ từ mặt đất, nhân vật chibi cao H = {…} px khi render.

**CHỦ THỂ:** {Mô tả nhân vật, vũ khí và tư thế}. Không đổi thiết kế nhân vật nếu có ảnh reference.

**HÌNH DẠNG VFX CHÍNH:** {Quạt vàng / Cung kiếm / Vòng xung lực / Cột sáng / Ma pháp trận / Tia / Ngôi sao impact}. Hình dạng này phải là silhouette mạnh nhất của toàn bộ hiệu ứng.

**KÍCH THƯỚC:** {…H}. Hướng tác động {…}. Góc quét {…°}. Vị trí hiệu ứng {Trước mặt / Quanh chân / Tại mục tiêu / Từ trên xuống}.

**BẢNG MÀU BẮT BUỘC:**
- Outer: {#HEX}
- Body: {#HEX}
- Bright: {#HEX}
- Core: {#HEX}

Phần lớn diện tích ánh sáng dùng màu {MÀU CHỦ ĐẠO}. Lõi sáng {MÀU LÕI} chỉ tập trung ở cạnh sắc và điểm impact. Có vùng trong suốt lớn để vẫn nhìn thấy nhân vật và mặt đất.

**NGÔN NGỮ RO:** Ánh sáng vàng ngà hoặc pastel mềm, lớp năng lượng xuyên thấu, vòng mặt đất mảnh, quạt sáng có hình học rõ, impact nhanh, starburst nhỏ, vài hạt lấp lánh. Không dùng viền đen dày quanh năng lượng. Đường nét có chủ đích, không rối.

**CẤU TRÚC:** {Số layer} lớp gồm {Ground / Main Slash / Energy Edge / Contact Flash / Residue}. Không tự thêm các lớp ngoài mô tả.

**THỜI ĐIỂM MUỐN VẼ:** {Prepare / Impact Peak / Dissolve}, tại T = {…} ms của một skill dài {…} ms.

**CẢM GIÁC:** {Nặng / Nhanh / Thanh thoát / Trang nghiêm / Ma thuật / Bùng nổ}, nhưng không thay đổi palette để làm FX dữ dội hơn.

**BỐ CỤC:** {Chỉ một frame lớn / 3 panel cùng một skill: prepare, impact, dissolve / 6-frame progression}. Nếu nhiều panel, giữ cùng nhân vật, camera, map, kích thước và hướng tấn công trong toàn bộ panel.

**NỀN:** {Nền tuyết sáng / Cỏ pastel / Đá hang tối / Nền phẳng theo yêu cầu}. Nếu vẽ concept có nhân vật, bảo đảm FX thực sự gắn vào vị trí chân, kiếm hoặc mục tiêu.

**TUYỆT ĐỐI KHÔNG:** lửa đỏ nếu skill Neutral, plasma neon, mảng năng lượng đặc, quả cầu cháy che hết nhân vật, vòng rune đỏ kiểu boss, ánh sáng vàng cam cháy rực toàn màn hình, flare vô cớ, độ tương phản kiểu dark fantasy, chữ trên ảnh.

Đây là concept để triển khai bằng Unity Particle System + Mesh Trail + Shader, KHÔNG phải sprite sheet có viền cắt hoặc khung asset.


### B2. English Version

Create a completely new VFX CONCEPT IMAGE for the skill {SKILL NAME}, used by the {CLASS} class in a 2D chibi action RPG.

Art direction: CLASSIC RAGNAROK ONLINE / MIDGARD SURVIVORS, lightly modernized with polished lighting and shaders while preserving the original Ragnarok visual language.

Camera: 3/4 top-down, {…} degrees above the ground plane. Rendered hero height H = {…} pixels.

**SUBJECT:** {Character, weapon, pose}. Preserve the exact character design if reference images are supplied.

**PRIMARY VFX SILHOUETTE:** {Pale-gold sector / Sword slash arc / Shockwave ring / Light pillar / Magic circle / Energy ray / Impact star}. This must be the single most recognizable shape.

**SCALE:** {…H}. Attack direction: {…}. Sweep angle: {…degrees}. Anchor: {Weapon / Hero feet / Target / World position}.

**MANDATORY COLOR PALETTE:**
- Outer: {#HEX}
- Body: {#HEX}
- Bright: {#HEX}
- Core: {#HEX}

The majority of the energy should use {PRIMARY COLOR}. The brightest {CORE COLOR} is restricted to thin edges and impact points.

**RAGNAROK VISUAL LANGUAGE:** Soft ivory-gold or pastel light, translucent energy, thin ground rings, clear sector geometry, short impact flashes, compact starbursts, sparse glowing particles, and generous negative space.

Avoid thick dark outlines around energy. Keep the character and ground visible through most of the effect.

**LAYERS:** {Number} layers: {Ground / Main Shape / Energy Edge / Impact / Residue}. Do not add unrelated effects.

**FRAME TO SHOW:** {Prepare / Impact Peak / Dissolve}, T = {…} ms of a {…} ms animation.

**EMOTIONAL FEEL:** {Heavy / Swift / Elegant / Sacred / Magical / Explosive}. Express power through timing, shape, and contrast rather than adding red flames.

**COMPOSITION:** {One large concept frame / Three-panel progression / Six-frame progression}. If multiple panels are requested, maintain identical camera, character proportions, environment, and attack direction.

**ENVIRONMENT:** {Bright snowy map / Pastel meadow / Dark cave / Flat background}. The VFX must appear physically attached to the hero, weapon, or impact location.

**STRICT NEGATIVES:** No red flames for Neutral attacks, no neon plasma, no opaque energy blobs, no giant fire spheres hiding the hero, no boss-danger red rune circles, no excessive fiery orange bloom, no dark-fantasy color grading, no random lens flares, and no text.

This image is an art-direction concept for implementation using Unity Particle Systems, Mesh Trails, and Shaders. It is not a frame-by-frame sprite animation or a cutout asset sheet.


#### Khuyến nghị sử dụng concept prompt

Với một skill quan trọng như Earth Break, nên duyệt ba hình ở ba thời điểm trước khi lập trình:

- Một hình ở prepare để duyệt silhouette.
- Một hình ở peak để duyệt màu, tỷ lệ và độ sáng.
- Một hình ở dissolve để duyệt lượng dư âm.

Không nên đánh giá chỉ từ frame rực rỡ nhất. Đây là lỗi dễ khiến skill thực tế bị sáng quá lâu hoặc có quá nhiều lớp.

---

## 9. TEMPLATE C — Ragnarok VFX Review Checklist

Checklist này dùng khi bạn gửi ảnh chụp in-game để tôi hoặc agent khác chấm điểm và đề xuất sửa. Nên yêu cầu reviewer **chỉ nêu các lỗi còn lại**, tránh liên tục lặp lại những việc đã sửa.

### 9.1. Rubric chấm điểm chuẩn 100

| Tiêu chí | Điểm tối đa | Nội dung |
|---|---:|---|
| A. Màu và nhận diện class | 20 | Đúng palette, đúng job/element, không bị toàn đỏ |
| B. Silhouette và hình học | 20 | Hình chính rõ, không chồng hình vô nghĩa |
| C. Transparency và blend | 15 | Nhìn xuyên FX, không đục, không quá Additive |
| D. Tỷ lệ theo H | 10 | FX cân với hero, mục tiêu và sân chơi |
| E. Timing / Impact | 15 | Lấy đà, bùng, tan, damage event đúng |
| F. Ground / 2.5D integration | 10 | Vòng nằm đúng đất, không như sticker |
| G. Combat readability | 10 | Thấy Hero, quái, cảnh báo boss |
| **Tổng** | **100** | **Quy đổi /10 = Tổng /10** |

#### Ngưỡng duyệt

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<box gap={2}>
  {#each [
    {range:"9.0–10",label:"Approved / Polish",pct:95,c:"#6FAD89",desc:"Đã đạt RO style, chỉ sửa nhỏ"},
    {range:"8.5–8.9",label:"Approved with fixes",pct:85,c:"#91BDB3",desc:"Đúng hướng, hoàn thiện vài chi tiết"},
    {range:"7.5–8.4",label:"Revise",pct:74,c:"#D0B57F",desc:"Có nền tảng đúng nhưng còn lệch rõ"},
    {range:"6.0–7.4",label:"Major revision",pct:58,c:"#CA9573",desc:"Sai nhiều ở màu, hình hoặc độ trong"},
    {range:"< 6.0",label:"Art-direction reset",pct:40,c:"#B07874",desc:"Cần thiết kế lại silhouette hoặc palette"}
  ] as r}
    <row align="center" gap={2}>
      <box width="66px">
        {r.range}
      </box>
      <box flex="1" gap={1}>
        <row align="baseline" justify="between">
          {r.label}
        </row>
        <box background="surface-tertiary" radius="full" height="6px" clip>
          <box background={r.c} width={`${r.pct}%`} height="100%" radius="full"/>
        </box>
        <caption>{r.desc}</caption>
      </box>
    </row>
  {/each}
</box>
```

Các thanh thể hiện mức đánh giá tương đối; ngưỡng điểm ghi bên trái mới là tiêu chí chính thức.

### 9.2. Bản tiếng Việt

## REVIEW CHECKLIST — RAGNAROK CLASSIC VFX

Chỉ trả lời bằng chữ, không tạo ảnh.

**Skill:** {Tên skill}  
**Class:** {Tên class}  
**Nguyên tố:** {Nguyên tố}  
**Ảnh tham chiếu:** {Tên file / Timestamp}  
**Ảnh in-game:** {Tên file / Timestamp}  
**Phiên bản:** {V1 / V2 / V3}  
**Các thay đổi đã thực hiện:** {Danh sách thay đổi}

### Nhiệm vụ

So sánh trực tiếp ảnh trong game với ngôn ngữ VFX của Ragnarok Online cổ điển, đặc biệt là các frame tham chiếu từ Midgard Survivors.

Không chấm theo độ đẹp độc lập, không ưu tiên hiệu ứng nhiều lửa hay Bloom mạnh. Chỉ chấm độ đúng art direction và khả năng đọc hình trong combat.

Nếu chỉ có screenshot, không được tự nhận đã xác minh timing, FPS, shader hoặc animation thực tế. Ghi rõ các tiêu chí chưa thể kiểm chứng.

### Chấm điểm

A. Màu và nhận diện class: {0–20}  
B. Silhouette và hình học: {0–20}  
C. Transparency và blend: {0–15}  
D. Tỷ lệ theo H: {0–10}  
E. Timing và impact: {0–15, chỉ khi có chuỗi frame/video}  
F. Ground / 2.5D integration: {0–10}  
G. Combat readability: {0–10}

Nếu không có đủ bằng chứng, ghi N/A và tính điểm trên phần có thể đánh giá, kèm tỷ lệ tiêu chí đã kiểm chứng.

### Những câu hỏi bắt buộc

1. Màu của skill có đúng class và element chưa?
2. Có đang dùng quá nhiều đỏ/cam hoặc quá giống boss warning không?
3. Silhouette chính có nhận ra trong 100 ms gần impact không?
4. Có quá nhiều layer khiến skill bị rối không?
5. Main body có trong suốt đủ để nhìn thấy hero, quái và nền không?
6. Bright edge/Core có bị quá dày và cháy trắng không?
7. Kích thước so với H có đúng bản thiết kế không?
8. Ground ring có nằm đúng mặt đất theo camera không?
9. Impact có đúng thời điểm gây damage không?
10. Có gây khó đọc gameplay khi nhiều người cùng dùng không?

### Format phản hồi

**PHÁN QUYẾT:** {Đạt / Đạt có điều kiện / Chưa đạt}

**ĐIỂM RO STYLE:** {x/10}

**3–5 lỗi còn lại, theo mức độ ưu tiên:**

1. {LỚN / VỪA / NHỎ} — {Lỗi} — {Cách sửa cụ thể bằng hex, alpha, %, H, ms}
2. {…}
3. {…}

**NHỮNG THỨ PHẢI GIỮ NGUYÊN:** {Các yếu tố đã đạt, không sửa lại}

**THÔNG SỐ CẦN CHỈNH CHO VÒNG SAU:** {Các giá trị cụ thể}

**MỤC TIÊU VÒNG SAU:** {Điểm mục tiêu và điều kiện duyệt}

Không đề xuất thay đổi lớn ở các phần đã đạt. Không tạo danh sách cải tiến vô tận nếu ảnh đã đạt ngưỡng.


### 9.3. English Version

## REVIEW CHECKLIST — CLASSIC RAGNAROK VFX

Text-only review. Do not generate images.

**Skill:** {Skill Name}  
**Class:** {Class}  
**Element:** {Element}  
**Reference Frames:** {Files / Timestamps}  
**In-Game Frames:** {Files / Timestamps}  
**Revision:** {V1 / V2 / V3}  
**Changes Already Implemented:** {List}

### Objective

Compare the current in-game VFX directly against the visual language of classic Ragnarok Online, using Midgard Survivors footage as the primary art-direction reference.

Evaluate stylistic accuracy and gameplay readability, not standalone visual spectacle. Do not reward excessive flames, stronger bloom, larger energy bursts, or additional decorative layers.

If only still images are provided, do not claim to have verified actual timing, FPS, shader implementation, or animation behavior. Mark such criteria as unverified.

### Scoring Rubric

A. Class identity and palette: {0–20}  
B. Primary silhouette and geometry: {0–20}  
C. Transparency and blend behavior: {0–15}  
D. Scale relative to hero height H: {0–10}  
E. Timing and impact: {0–15; verify only with a time sequence/video}  
F. Ground-plane and 2.5D integration: {0–10}  
G. Combat readability: {0–10}

For unverified criteria, mark N/A and normalize the score across verified criteria. Report coverage.

### Required Checks

1. Does the palette match the class and element?
2. Is the effect overly red/orange or visually confused with boss danger zones?
3. Is the main silhouette immediately recognizable at impact?
4. Are there too many overlapping layers?
5. Is the main body transparent enough to reveal the hero, enemies, and ground?
6. Are bright edges or core highlights excessively thick or overexposed?
7. Is the effect correctly scaled relative to H?
8. Does ground-aligned geometry match the scene perspective?
9. Does the VFX impact synchronize with the gameplay hit event?
10. Does the effect remain readable during crowded multiplayer combat?

### Required Response Format

**VERDICT:** {Approved / Approved with fixes / Not approved}

**RAGNAROK STYLE SCORE:** {x/10}

**TOP 3–5 REMAINING ISSUES:**

1. {MAJOR / MEDIUM / MINOR} — {Issue} — {Exact fix with hex, alpha, %, H, or ms}
2. {…}
3. {…}

**KEEP UNCHANGED:** {Elements that already match the approved design}

**NEXT REVISION VALUES:** {Exact parameter changes}

**NEXT ROUND TARGET:** {Target score and acceptance criteria}

Do not reopen approved decisions without new evidence. Do not endlessly suggest polish when the asset has already reached the acceptance threshold.


---

## 10. Lộ trình chuyển Warrior sang RO style

Tôi đề xuất **không viết lại tất cả tám skill cùng lúc**. Trước tiên phải tạo được một chuẩn ánh sáng và vật liệu chung, sau đó mới chuyển từng skill.

| Ưu tiên | Việc thực hiện | Kết quả mong muốn |
|---|---|---|
| P0 | Tạo shader/material chung `RO_SoftEnergy_Alpha` và `RO_SoftEnergy_Add` | Khóa cách hiển thị màu và độ trong |
| P1 | Làm mới Power Slash | Chuẩn quạt Bash, vàng ngà, không lửa |
| P1 | Làm mới Whirlwind | Chuẩn cung kiếm và chuyển động |
| P1 | Làm mới Charging Strike | Chuẩn tốc độ, steel và impact |
| P2 | Chuyển Blade Cyclone sang vàng ngà | Chuẩn multi-hit AoE |
| P2 | Chuyển Earth Break sang Golden Knight Judgment | Chuẩn ultimate vật lý |
| P3 | Sửa Flame Cleave và Inferno Crash | Chuẩn Fire không lẫn boss warning |
| P3 | Sửa Berserker | Chuẩn buff không che nhân vật |
| P4 | Áp dụng bộ palette và material cho 5 class còn lại | Đồng nhất art direction toàn game |

### 10.1. Những thứ nên giữ lại từ Warrior hiện tại

Không nên vứt bỏ mọi thành quả cũ. Tôi khuyến nghị giữ:

- **Mesh Trail và hệ thống xoay theo hướng đánh:** rất phù hợp để dựng cung kiếm RO.
- **Hệ thống Impact Star, shockwave và hit-stop:** chỉ cần chỉnh palette, độ sáng và timing.
- **Procedural Rune và Ground Ring:** dùng lại cho Earth Break, buff, Wizard, Cleric.
- **Shader Dissolve:** vẫn hữu ích để FX tan sạch, không phải tắt đột ngột.
- **Cấu trúc animation và combat event:** không cần làm lại damage logic để thay art style.
- **Kiếm vẽ riêng của Earth Break:** giữ mesh, thiết kế và thời điểm xuất hiện, nhưng làm lại phần ánh sáng quanh nó.

Vấn đề nằm chủ yếu ở art direction của material, silhouette, thứ tự lớp và mức độ phủ sáng.

### 10.2. Ba kỹ năng nên dùng làm chuẩn vàng

```text (khối hiển thị của ChatGPT, giữ nguyên số liệu)
<box gap={2}>
  <row align="start" gap={3}>
    <box background="surface-secondary" radius="lg" padding={2}>
      <icon name="swords" size="lg"/>
    </box>
    <box flex="1" gap={1}>
      **Power Slash — chuẩn đánh vật lý**

      Phải đạt màu vàng quạt Bash, rõ silhouette và sáng ngắn. Khi duyệt thành công, sử dụng chung palette này cho nhiều kỹ năng Knight khác.
    </box>
  </row>
  <divider color="subtle"/>
  <row align="start" gap={3}>
    <box background="surface-secondary" radius="lg" padding={2}>
      <icon name="orbit" size="lg"/>
    </box>
    <box flex="1" gap={1}>
      **Blade Cyclone — chuẩn AoE**

      Phải tạo cảm giác lực lan rộng mà vẫn thấy Hero và quái ở giữa. Đây là bài kiểm tra transparency và additive tốt nhất.
    </box>
  </row>
  <divider color="subtle"/>
  <row align="start" gap={3}>
    <box background="surface-secondary" radius="lg" padding={2}>
      <icon name="shield" size="lg"/>
    </box>
    <box flex="1" gap={1}>
      **Earth Break — chuẩn Ultimate**

      Phải đủ mạnh về mặt hình ảnh mà không trở về phong cách plasma/lửa đỏ. Đây là bài kiểm tra cuối cùng cho toàn bộ art direction.
    </box>
  </row>
</box>
```

### 10.3. Phán quyết cuối

**Tôi sẽ không chọn hướng làm Warrior VFX giống Ragnarok bằng cách chỉ giảm saturation của màu đỏ.** Làm vậy vẫn giữ nguyên những vòng lửa, lưỡi kiếm cháy và kết cấu năng lượng đang khiến cả bộ skill lệch phong cách.

Thay vào đó, cần chuyển cách biểu đạt sức mạnh:

| Warrior hiện tại | Warrior theo RO Classic |
|---|---|
| Lửa đỏ tạo cảm giác mạnh | Hình kiếm và xung lực tạo cảm giác mạnh |
| Bloom lớn tạo cao trào | Impact ngắn tạo cao trào |
| Nhiều particle tạo độ hoành tráng | Silhouette rõ tạo độ hoành tráng |
| Mọi skill cùng một họ màu | Mỗi skill có hình riêng, class có palette thống nhất |
| Năng lượng bao phủ nhân vật | Năng lượng bao quanh nhưng chừa không gian nhìn |
| Tập trung vào frame đẹp nhất | Tập trung vào cả nhịp xuất hiện–va chạm–biến mất |

**Mục tiêu chốt cho dự án:** Warrior phải được nhận ra là một Knight dùng kiếm và khiên, với ánh kiếm vàng ngà đặc trưng, kể cả khi tắt toàn bộ UI và chỉ nhìn một frame đang chiến đấu.

Nếu hoàn thiện được Power Slash, Blade Cyclone và Earth Break theo chuẩn trên, bạn sẽ có ba mẫu FX đủ mạnh để chuẩn hóa toàn bộ Warrior và các class còn lại, mà không phải hy sinh chất lượng shader hiện có.

Tài liệu này dựa chủ yếu trên các frame Midgard Survivors và Warrior bạn đã gửi. Các quy ước class/skill được đối chiếu thêm với [Ragnarok Online — Lord Knight Skills](https://www.playragnarok.com/gameguide/classes_skill.aspx?c=21) và [Midgard Survivors](https://midgardsurvivors.com/). Các palette, timing, alpha, H và ngân sách hiệu năng là chuẩn thiết kế đề xuất dành riêng cho game của bạn, cần kiểm chứng trong Unity.

## 3. ChatGPT vòng 2 (nguyên văn): sửa cỡ theo trailer, SkillTable, bỏ quy tắc 4 bậc sáng, DNA, prompt 1 dòng, palette cuối

Gửi: số đo cỡ (H ≈ 55-60 px, quạt Bash ≈ 4,5-6H, ma pháp trận 8-10H), yêu cầu người dùng "hoành tráng", SkillTable là nguồn thời gian, hỏi xác nhận quy tắc 4 bậc sáng cũ, DNA ≤120 từ, prompt 1 dòng, palette cuối + Cleric giữ hay đổi.

## RO STYLE DNA V2 — Chốt kích thước, màu và timing

**Chốt sửa vòng 2:** Bạn đo đúng một điểm rất quan trọng mà bản V1 của tôi đánh giá thấp: **FX trong Midgard Survivors rất lớn so với nhân vật, nhưng mỏng, nhạt và xuyên thấu.** Đề xuất R 1.2–2.2H trước đó quá nhỏ nếu lấy chính các cảnh Bash, Golden Thief Bug và Storm Gust trong trailer làm mục tiêu.

Giữ định hướng vàng ngà/ánh sáng mềm, nhưng đổi nguyên tắc từ **“FX nhỏ để không che nhân vật”** thành **“FX lớn, silhouette rõ, diện tích trong suốt cao, lõi sáng chiếm ít”**.

Các tỷ lệ đo từ trailer dưới đây dùng H ≈ 55–60 px theo số liệu bạn cung cấp. Cần phân biệt kích thước hiệu ứng tại peak với hitbox gameplay: FX có thể rộng hơn phạm vi gây sát thương, nhưng không được đánh lừa người chơi về vị trí trúng đòn.

### 1. Bảng kích thước H đã sửa

| Loại FX | V1 | V2 chốt | Tham chiếu |
|---|---:|---:|---|
| Quạt Bash/Power Slash | R 0.9–1.5H | **R 4.5–5.5H** | Bash 31.4 s, 72–73.5 s |
| Cung kiếm lớn | R 0.8–1.7H | **R 2.5–4.5H** | Các cú Knight quét kiếm |
| Vòng Spiral Pierce tại mục tiêu | Chưa tách rõ | **Đường kính 2–2.5H** | Golden Thief Bug |
| Vòng xung lực/AoE | ĐK 1.5–4H | **ĐK 4–8H** | Magnum-like burst |
| Cột sáng | Cao 1.5–5H | **Cao 4.5–6H** | 1.5 s |
| Ma pháp trận lớn | ĐK 2–6H | **ĐK 8–10H** | 6.5 s, 46.5 s |
| Sao impact chính | ĐK 0.25–0.75H | **ĐK 0.8–1.8H** | Đề xuất tăng theo cảm giác lực |
| Sao impact Ultimate | Chưa tách | **ĐK 1.5–2.5H** | Đề xuất, không phải số đo riêng |
| Rune buff quanh chân | ĐK 1.5–4H | **ĐK 2–4H** | Phân biệt với đại ma pháp trận |

ĐK = đường kính; R = bán kính. Không được nhầm R 5H với đường kính 5H. Các số từ ảnh trailer là xấp xỉ theo phối cảnh và bounding box nhìn thấy.

### 2. Tám skill Warrior — To + trong + nhạt

| Skill | Kích thước V2 | Thiết kế visual | Alpha main |
|---|---|---|---:|
| **Power Slash** | Quạt R **4.5–5H** | Quạt vàng chanh nhạt, viền vàng trắng mảnh, impact lớn tại mục tiêu | 0.18–0.35 |
| **Whirlwind** | R **3.5–4.5H** | Hai cung khí vàng/steel lớn, đứt đoạn, tâm rỗng | 0.18–0.32 |
| **Flame Cleave** | Cung R **3.5–4.5H** | Cung vàng mật ong lớn, viền lửa cam rất nhẹ | 0.22–0.40 |
| **Charging Strike** | Trail dài theo dash; ngang **1.5–2H** | Mũi xung lực steel/ivory, vệt sáng rộng, impact 1.5H | 0.20–0.40 |
| **Earth Break** | Rune ĐK **7–9H**, shockwave R **4–5H** | Kiếm ánh vàng, nứt đất, vòng lực lớn trong suốt | 0.16–0.32 |
| **Blade Cyclone** | R **4–5H** | Ba cung kiếm vàng champagne xoay, tâm thoáng | 0.16–0.30 |
| **Inferno Crash** | Shockwave R **4.5–5.5H** | Vòng vàng lửa lớn, core nhỏ, không phủ đỏ kín mặt đất | 0.20–0.38 |
| **Berserker** | Aura cao **2.5–3.5H**, pulse R **2–3H** | Vầng sáng vàng ngà, các đường năng lượng vươn lên | 0.18–0.32 |

**Quan trọng:** Alpha ở bảng là của **lớp thân FX rộng**, không phải của Bright/Core. Các đường sáng và contact flash vẫn có thể đạt alpha 0.65–0.95 trong thời gian ngắn.

Quạt Power Slash R 5H rất lớn so với kiếm thật. Tôi chấp nhận điều đó vì đây là lựa chọn art direction đã được chủ game định hướng. Tuy nhiên quạt nên có mép mềm, khối sáng không đều, và nhiều vùng alpha thấp thay vì một hình nêm đặc đồng nhất.

### 3. Timing — SkillTable là nguồn chân lý duy nhất

**Chốt:** Không dùng các mốc timing cố định từ bảng V1 để ghi đè lên game.

| Thành phần | Quy tắc bắt buộc |
|---|---|
| Skill start | Lấy từ SkillTable và sự kiện gameplay |
| Cast / windup | Giữ đúng dữ liệu SkillTable |
| Damage event | Giữ nguyên toàn bộ mốc hit |
| Multi-hit | Giữ nguyên `SkillHitSplit`, số hit, thứ tự và thời điểm |
| Earth Break | **Kiếm cắm đất tại 1450 ms** theo ví dụ đã xác nhận |
| Visual prepare | Được thiết kế trước mốc hit |
| Visual peak | Căn với hit event tương ứng |
| Visual dissolve | Được kéo dài sau hit, không ảnh hưởng damage |
| Server synchronization | Không sửa contract hoặc combat calculation |
| VFX duration | Được dài hơn thời gian gây damage nếu chỉ còn dư âm |

**Ví dụ Earth Break:** tại 1450 ms, kiếm phải chạm đất và phát impact tương ứng. Có thể bố trí ánh sáng tích tụ trước 1450 ms và shockwave tan sau 1450 ms, nhưng không tự chuyển cú cắm kiếm về 520 ms như template V1. Các hit khác phải đọc chính xác từ `SkillHitSplit`, không tự suy ra.

Đoạn bắt buộc bổ sung vào mọi Unity VFX brief:

**TIMING AUTHORITY — MANDATORY:** SkillTable shared with the server is the single source of truth for all gameplay timing. Read cast time, animation events, hit timestamps, and SkillHitSplit directly from the existing data. Never invent, replace, normalize, or modify these values. Remap only VFX anticipation, expansion, brightness peak, and dissolve around the authoritative timestamps. Example: Earth Break sword impact at 1450 ms must remain exactly at 1450 ms. All hit counts, hit ordering, damage logic, and server contracts must remain unchanged. Cosmetic VFX may continue after the final hit.

### 4. Thay quy tắc bốn bậc sáng cũ

**Xác nhận: quy tắc cũ không phù hợp với phong cách FX mục tiêu**, đặc biệt ở viền đỏ mận tối và thân đậm bão hòa đục. Không phải Ragnarok tuyệt đối không có màu tối hoặc khối đặc, mà chúng không nên là quy tắc mặc định cho hiệu ứng năng lượng.

**Quy tắc mới — RO Translucent Light, 7 dòng:**

1. **Outer:** màu cùng họ, nhạt hoặc ít bão hòa; alpha 0.08–0.20, Alpha Blend, không viền tối đỏ mận.
2. **Body:** pastel sáng, alpha 0.18–0.40, Alpha Blend; chiếm khoảng 65–80% diện tích hình chính.
3. **Bright:** vàng nhạt/trắng ngà, alpha 0.45–0.75, Additive hoặc Alpha tùy nền; chiếm khoảng 15–25%.
4. **Core:** trắng/ivory, alpha 0.75–0.95, Additive; chỉ chiếm khoảng 3–8%, tối đa 10–12% cho impact.
5. **Negative space:** khoảng 45–65% vùng bao FX phải là khoảng trống hoặc vùng có opacity rất thấp; tính riêng với tỷ lệ các lớp bên trên.
6. **Bloom:** tập trung Bright/Core lúc impact, không làm toàn bộ Body phát sáng HDR mạnh liên tục.
7. **Scale:** skill lớn 4–6H, đại ma pháp trận 8–10H; tăng độ hoành tráng bằng hình học và nhịp, không tăng độ đục.

Các tỷ lệ diện tích Outer/Body/Bright/Core không nhất thiết cộng đúng 100% vì các lớp chồng nhau.

---

### 5. RO STYLE DNA — đoạn chuẩn dùng chung

Bản tiếng Việt

RO STYLE DNA: Hiệu ứng skill kiểu Ragnarok cổ điển, hiện đại hóa theo RO3 Soft Fantasy: HOÀNH TRÁNG về kích thước nhưng NHẠT, TRONG SUỐT và DỄ ĐỌC. Quạt chém có thể đạt bán kính 4.5–6H, đại ma pháp trận đường kính 8–10H. Ưu tiên ánh sáng pastel, trắng ngà, vàng dịu, hình học rõ, vòng phép mảnh và impact mạnh nhưng ngắn. Body alpha 0.18–0.40, Alpha Blend; Bright/Core dùng Additive có kiểm soát. Không dùng viền tối đỏ mận, plasma đục, lửa đỏ mặc định hoặc Bloom che nhân vật. Màu theo class và nguyên tố; đỏ cảnh báo dành riêng cho boss. Chỉ dùng procedural VFX, Particle System, Mesh Trail và shader.

English Version

RO STYLE DNA: Classic Ragnarok-inspired skill VFX, modernized for RO3 Soft Fantasy. Effects must look GRAND in scale yet PALE, TRANSLUCENT, and READABLE. Sword sectors may reach a 4.5–6H radius; major magic circles may span 8–10H in diameter. Favor soft pastel light, ivory, gentle gold, clear geometry, thin magic rings, and powerful but brief impacts. Use Alpha Blend for the main body (alpha 0.18–0.40) and restrained Additive highlights. Avoid dark burgundy outlines, opaque plasma, default red flames, and bloom that hides characters. Respect class and elemental palettes; reserve danger-red telegraphs for bosses. Implement using procedural geometry, Particle Systems, Mesh Trails, and shaders, without animated sprite frames.

### 6. Template B rút gọn — một dòng, tiếng Anh

Template dùng lại, dưới 900 ký tự, không xuống dòng trong nội dung prompt.

Create a {SKILL} VFX concept for {CLASS}, classic Ragnarok / RO3 Soft Fantasy, chibi 3/4 top-down camera {ANGLE}°. GRAND scale yet PALE, TRANSLUCENT, READABLE. Main shape: {SHAPE}, radius/size {SIZE}H, direction {DIRECTION}. Colors Outer {OUTER}, Body {BODY}, Bright {BRIGHT}, Core {CORE}. Body alpha 0.18-0.40 Alpha Blend; thin Bright/Core Additive; core 3-8% area; 45-65% visual negative space. Soft pastel light, ivory highlights, clear geometry, brief impact, delicate ground FX. Show {PHASE} at {TIME}ms, matching SkillTable/SkillHitSplit without changing gameplay timing. Use procedural Particle System + Mesh Trail + shader, NO sprite frames. No opaque plasma, dark burgundy outlines, excessive bloom, default red flames or boss-warning red. Background {BACKGROUND}, hero height H={H}px, keep hero and target visible.

Ví dụ điền sẵn — Warrior Power Slash

Create a Power Slash VFX concept for Warrior (sword + shield), classic Ragnarok / RO3 Soft Fantasy, chibi 3/4 top-down camera 58°. GRAND scale yet PALE, TRANSLUCENT, READABLE. Main shape: huge 115° pale-gold Bash sector, radius 5H, sweeping forward, with a thin ivory edge and compact impact star. Colors Outer #B2A873, Body #D5C378, Bright #F5EA7C, Core #FEFDF8. Body alpha 0.18-0.35 Alpha Blend; Bright/Core Additive; core 3-8% area; large transparent areas. Show the impact peak aligned to the existing SkillTable hit event, without changing timing. Procedural Particle System + Mesh Trail + shader, NO sprite frames. No red fire, opaque plasma, burgundy outline or excessive bloom. Pastel snowy ground, hero H=58px, keep hero and enemy visible.

`H=58px` trong ví dụ là chiều cao tham chiếu theo trailer. Khi giao cho agent dựng FX trong Unity, thay bằng chiều cao Hero render thực tế trong game.

---

### 7. Bảng palette cuối cùng — RO3 Soft Fantasy V2

Hex RGB sRGB, dùng làm base color trước HDR emission.

| Class | Outer | Body | Bright | Core |
|---|---|---|---|---|
| **Warrior** | #B2A873 | #D5C378 | #F5EA7C | `#FEFDF8` |
| **Wizard** | #527FA7 | #76ADCE | #A9D9EF | `#F2FBFF` |
| **Mechanic** | #587A88 | #84B7C5 | #D5E8E9 | `#FFF5D8` |
| **Assassin** | #526B69 | #75B987 | #B4E1A0 | `#F1FFE1` |
| **Cleric** | #BBAE88 | #F3D16B | #FFF4C9 | `#FFFFFF` |
| **Fighter** | #709D9C | #A4D6C9 | #F4E7AE | `#FFFFFF` |
| **Boss danger** | #8D372F | #CB6F43 | #E6A36A | `#FFE0A7` |

**Dùng cho từng class:**

| Class | Dùng cho |
|---|---|
| Warrior | Neutral slash, Bash, Spiral-like force, Knight impact; Fire riêng dùng bảng phụ |
| Wizard | Arcane/Ice, vòng phép lam; Fire/Wind/Earth đổi sang palette nguyên tố |
| Mechanic | Súng năng lượng, pháo, máy móc; muzzle flash và nổ sử dụng gold phụ |
| Assassin | Poison, dao găm, vệt độc; Shadow dùng tím, Sonic Blow có thể dùng coral |
| Cleric | Holy attack, blessing, thánh ấn; Heal/Protect giữ palette riêng |
| Fighter | Nắm đấm, chấn khí, spirit aura; đòn mạnh thêm champagne gold |
| Boss danger | Vùng sàn nguy hiểm, rune đỏ, telegraph; không áp màu này cho mọi quái |

#### Cleric — quyết định giữ nguyên màu RO3 Soft Fantasy cũ

**GIỮ màu cũ. Không thay hệ Cleric/heal/buff đã duyệt.**

| Nhóm | Màu đã chốt | Quyết định |
|---|---|---|
| Holy / Blessing | `#FFF4C9`, `#F3D16B` | **GIỮ** |
| Heal | `#91DB76` | **GIỮ** |
| Protection / Barrier | `#9DDAEC` | **GIỮ** |
| Holy Core | `#FFFFFF` | GIỮ |
| Neutral Knight Gold | `#D5C378`, `#F5EA7C` | Dùng riêng Warrior |

Palette bốn bậc của Cleric ở bảng trên chỉ là bộ màu **Holy mặc định**, không thay màu hiệu ứng Heal xanh lá hay Barrier xanh lam.

Để đồng bộ visual, cả ba nhóm Holy/Heal/Protection dùng **cùng nguyên tắc alpha, blend, soft edge và bloom của RO Style DNA V2**, nhưng không đổi hue chính của từng nhóm.

---

### 8. Chốt thông số áp dụng toàn game

| Thông số | Giá trị cuối |
|---|---|
| Art direction | RO3 Soft Fantasy + Classic RO VFX |
| FX scale | Lớn, theo chức năng: R 4.5–6H cho quạt lớn |
| Major magic circle | ĐK 8–10H |
| Main body alpha | 0.18–0.40 |
| Core coverage | 3–8% diện tích lớp sáng, peak tối đa 12% |
| Negative space | 45–65% vùng bao hiệu ứng |
| Main blend | Alpha Blend |
| Highlight blend | Additive có giới hạn |
| Timing authority | SkillTable + SkillHitSplit |
| Earth Break impact | 1450 ms — không thay |
| Player danger-red ground FX | Cấm |
| Cleric Holy/Heal/Protect palette | Giữ nguyên |
| Asset technique | Particle + Mesh Trail + Shader, không sprite frame |

**Phán quyết cuối V2:** Chốt **TO + TRONG + NHẠT + HÌNH RÕ + HIT ĐÚNG SKILLTABLE**.

Đây là thay đổi quan trọng so với V1: **không giảm độ hoành tráng để đạt style Ragnarok**. Giữ hiệu ứng lớn theo trailer, nhưng giảm độ đục, độ bão hòa không cần thiết và sự chồng chéo giữa các lớp.
