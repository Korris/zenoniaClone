<!-- Nguồn: ChatGPT (tài khoản Tay), chat https://chatgpt.com/c/6af5faea-f11c-83ec-9e09-3c13537b61a2 , ngày 13/11/2026. Nguyên văn câu trả lời, chuyển sang Markdown bằng script (bảng giữ nguyên chữ). Từ nay mọi prompt vẽ map / nhà / prop / quái phải dán đoạn "WORLD ART STYLE LOCK" ở cuối file + gửi kèm 1–2 ảnh chuẩn. -->

## WORLD ART BIBLE v1.0

QUY CHUẨN MỸ THUẬT TOÀN THẾ GIỚI — 2D ACTION RPG

Phạm vi: Tất cả town, farm map, dungeon, boss arena, nhà cửa, cây cối, địa hình, props, quái thường và boss từ Lv1–60.

Bộ ảnh chuẩn đã duyệt:

- Oakhaven mới: chuẩn bố cục gameplay, kiến trúc, bảng màu tự nhiên, tỷ lệ NPC và môi trường.
- Thatchwick: chuẩn vật liệu gỗ, mái tranh, nền đất đá và cảm giác hand-painted.
- Frostfang Den: chuẩn độ tương phản, chiều sâu, vách đá và chất lượng môi trường chiến đấu.
- Forest Wolf, Thorn Sprite, Thornfang Alpha, Thornheart Treant, Snow Harpy, Crystal Golem King: chuẩn thiết kế quái, viền, cel shading và sprite 4 hướng.

Nguyên tắc bất biến: Mọi tài nguyên phải trông như được cùng một studio vẽ, sử dụng cùng một camera, cùng một mặt trời và cùng một hệ thống màu sắc.

### 1. Tên phong cách chính thức

Stylized Hand-Painted Fantasy JRPG — Chibi Anime / Zenonia-like

Đây là phong cách kết hợp hai ngôn ngữ mỹ thuật:

| Thành phần | Phong cách bắt buộc |
| --- | --- |
| Map background | Stylized hand-painted 2D fantasy environment |
| Nhà, cây, prop | Painterly fantasy illustration, crisp silhouettes |
| Hero, NPC, monster | Chibi anime fantasy, bold clean outlines |
| Boss | Exaggerated chibi fantasy, strong silhouettes |
| VFX | Anime cel-shaded effects, saturated accents, white cores |
| Tổng thể | Cozy fantasy ở town; dramatic fantasy ở dungeon |

Không gọi là pixel art, HD-2D, realistic 3D render hay photorealistic.

Đặc trưng nhận diện: Tỷ lệ chibi, vật liệu được vẽ tay, nét rõ ở rìa vật thể, đổ bóng theo mảng lớn, màu sống động nhưng không neon, chi tiết được phân cấp theo mức độ quan trọng.

### 2. Camera, góc nhìn và tỷ lệ

Canvas chuẩn

## 1536×1024

Background gốc

Lưới gameplay

## 36×24

Ô vuông

Kích thước ô

## 42.67 px

1536 ÷ 36

Camera: Orthographic 3/4 top-down fantasy RPG.

- Camera nhìn từ trên xuống, thấy mái nhà, đỉnh đầu, mặt trước và một phần hông vật thể.
- Chọn góc nghiêng quy ước khoảng 50° so với mặt phẳng đất (tương đương nhìn xuống 40° so với phương ngang camera). Đây là mục tiêu art, không phải phép đo chính xác từ ảnh.
- Không dùng góc isometric 45° xoay lưới thành hình kim cương.
- Mặt đất và đường đi giữ lưới vuông theo trục ngang/dọc game.
- Trục đứng của nhà và thân cây dựng thẳng trên hình; mái và mặt trên nhìn rõ, không méo phối cảnh.
- Các kiến trúc chính ưu tiên hướng cửa xuống phía người xem; hướng bên phải/trái phải nhất quán giữa các map.

Quy tắc Unity: Nếu bản đồ là một background phẳng trên mặt phẳng XY, camera Unity có thể đặt Orthographic nhìn trực diện vào mặt phẳng đó. Cảm giác 3/4 đã được vẽ sẵn trong artwork. Không nghiêng camera Unity thêm lần nữa.

Kích thước 42.67 px/ô không phải 42 px chính xác. Khi cắt, tạo collision và ánh xạ tọa độ, dùng kích thước logic `1536 / 36` và `1024 / 24` để tránh sai lệch tích lũy.

Với map 48×32 ở cùng mật độ pixel: 2048×1365.33 px theo lý thuyết. Nên chọn kích thước xuất hợp lệ, rồi chuẩn hóa hệ tọa độ theo chiều dài mỗi ô; không tự ý kéo dãn artwork gây biến dạng.

### 3. Một hướng ánh sáng duy nhất

Nguồn sáng: trên-trái màn hình (Top-Left / Northwest).

Ánh sáng đi từ góc trên-trái xuống dưới-phải. Bóng đổ về góc dưới-phải (Southeast), thống nhất trên tất cả map và sprites.

| Hướng sáng | Trên-trái → dưới-phải |
| --- | --- |
| Mặt nhận sáng | Mái trên/trái, tán cây trên/trái, mặt đá hướng sáng |
| Hướng bóng | Khoảng 45° xuống-phải theo màn hình |
| Bóng nhà, cây | Ngắn, khoảng 0.3–0.6 lần chiều cao vật thể biểu kiến |
| Bóng tiếp xúc | Đậm sát chân, mềm dần ra ngoài |
| Độ mờ | Khoảng 25–40% bóng lớn; 40–55% ngay điểm tiếp đất |

Các con số là chuẩn mục tiêu cho art mới, không phải thông số đã được đo từ ảnh mẫu.

Ánh sáng môi trường có thể đổi nhiệt độ theo vùng (lạnh ở tuyết, ấm ở sa mạc), nhưng không đổi hướng nguồn sáng chính.

Ngoại lệ: đèn lồng, lò rèn, dung nham, lõi pha lê, mắt và tim boss được phép phát sáng cục bộ, nhưng không được đảo hệ thống bóng chính.

### 4. Độ đậm viền, độ bão hòa và tô bóng

| Thuộc tính | Map / Nhà / Prop | Quái / Hero / Boss |
| --- | --- | --- |
| Viền ngoài | Mềm, có nét tối chọn lọc | Viền tối rõ toàn silhouette |
| Độ dày tại kích thước gốc | 1–2 px ở mép quan trọng, không viền toàn cảnh | 2–4 px với sprite chuẩn khoảng 180–230 px |
| Viền trong | Theo vật liệu, không tô từng viên đá bằng đen | 1–2 px giữa các mảng chính |
| Shading | Painterly, 3–4 nhóm sắc độ | Cel shading, 2–3 nhóm sắc độ |
| Highlights | Mềm vừa, tập trung vào vật liệu | Sắc nét ở mắt, móng, giáp, tinh thể |
| Texture | Tập trung ở mái, tường, đá, cây | Tập trung vào mặt, vũ khí, nhận dạng |
| Độ bão hòa | Thấp–trung bình | Trung bình–cao, có kiểm soát |

#### Quy tắc màu

- Nền dùng màu tự nhiên, không tạo cảm giác nền phát sáng.
- Quái nổi hơn nền nhờ viền, độ tương phản và màu nhấn.
- Màu bão hòa mạnh chỉ chiếm khoảng 5–15% diện tích một map, chủ yếu ở landmark, VFX và điểm tương tác.
- Chi tiết tối không được mất hẳn trong vùng đầm lầy, hang hoặc rừng.
- Không dùng bóng đổ màu đen thuần cho toàn bộ vật thể.
- Không phủ bloom mạnh, haze hoặc lớp glow lên toàn cảnh.

Mục tiêu: Nhìn ở kích thước game thực tế 1280×720 vẫn phân biệt ngay được hero, quái, đường đi, cửa nhà, vật cản và boss.

### 5. Bảng màu chuẩn toàn thế giới

Bảng màu dưới đây là master palette để tạo asset mới. Không bắt buộc từng pixel của ảnh cũ phải trùng mã hex, nhưng mọi asset mới phải nằm trong cùng một hệ màu và độ tương phản.

#### 5.1. Global Master Palette

Forest Sage

#788F55

Olive Grass

#A1A66A

Warm Stone

#C6B49A

Earth Path

#B9936C

Timber Brown

#805B42

Roof Slate

#506B83

Deep Shadow

#34434A

Warm Highlight

#E5CF9B

Chọn màu theo vai trò, không phải cố sử dụng cả tám màu ở mọi map. Ví dụ Frostfang Den thay Forest Sage bằng xanh băng nhưng vẫn dùng nguyên tắc phân chia sáng/tối.

#### 5.2. Palette riêng cho 6 vùng

Mỗi vùng có 5 màu chủ đạo: nền, màu phụ, vật liệu, bóng và điểm nhấn.

01 — Đồng cỏ / Rừng

Oakhaven, Willowbrook, Bramblewood

Cỏ

#788F55

Rừng sâu

#435C43

Đường đất

#B9936C

Bóng

#34483B

Nắng

#C99A57

02 — Tuyết / Băng

Frost Hollow, Snowcrest, Frostfang Den

Tuyết

#DCE8EF

Băng

#91BBD2

Đá

#71869E

Bóng

#405B77

Tinh thể

#4EC9F5

03 — Sa mạc

Sunspire, Amber Dunes, Scorpion Gorge

Cát

#D5B477

Cồn cát

#B88453

Đá

#977351

Bóng

#715542

Ốc đảo

#6FB8B7

04 — Đầm lầy

Mirewatch, Emberfen, Sunken Reliquary

Rêu

#79865C

Bùn cỏ

#536749

Đá cổ

#98947B

Bóng

#303F38

Nước độc

#81B993

05 — Núi lửa

Ashforge, Ashfall, Obsidian Crater

Tro

#817570

Đá tối

#615D62

Obsidian

#414249

Bóng

#2C3039

Dung nham

#EB8150

06 — Tinh thể / Thiên giới

Starfall Citadel, Crystal Highlands, Astral Spire

Đất sáng

#B3B8D5

Đá lam

#8193BA

Vách

#797BA5

Bóng

#454A70

Tinh thể

#8DDCEB

#### 5.3. Quy tắc cân bằng palette

| Yếu tố | Tỷ trọng mục tiêu |
| --- | --- |
| Màu nền / vật liệu chủ đạo | 55–65% |
| Màu phụ / vùng bóng | 25–30% |
| Điểm nhấn sáng, hoa, pha lê, lửa | 5–15% |

- Vùng tuyết có thể sáng tổng thể hơn đầm lầy, nhưng vẫn cần dải sáng–trung gian–tối rõ ràng.
- Sa mạc không được vàng cam cháy toàn màn hình.
- Rừng tối hơn đồng cỏ nhưng đường đi phải nổi bật.
- Đầm lầy không biến thành một khối đen xanh.
- Núi lửa dùng màu xám than làm nền, cam đỏ chỉ dành cho dung nham.
- Thiên giới sử dụng xanh bạc, tím xám và cyan, không biến thành nền neon phát sáng.

Quy tắc quan trọng: Đồng nhất art style không có nghĩa tất cả vùng phải có cùng độ sáng tuyệt đối. Chúng phải thống nhất cách phân cấp tương phản, chất liệu và độ bão hòa, nhưng vẫn giữ bản sắc khí hậu.

### 6. Quy chuẩn vẽ nhà và kiến trúc

#### 6.1. Tỷ lệ nhà so với hero

Lấy hero chibi hiển thị khoảng 85 px trong game ở mức zoom chuẩn làm đơn vị `H`.

| Thành phần | Tỷ lệ mục tiêu | Ví dụ khi H = 85 px |
| --- | --- | --- |
| Nhà dân nhỏ | 2–3H | 170–255 px |
| Guild Hall / nhà chính | 3–4H | 255–340 px |
| Cửa ra vào | 0.9–1.3H | 77–111 px |
| Cầu thang cửa | 0.1–0.25H/bậc theo chiều sâu hình chiếu | 9–21 px |
| Quầy hàng | 1.2–1.8H | 102–153 px |

Đây là kích thước biểu kiến, không phải chiều cao kiến trúc ngoài đời. Nhà cần vừa đủ lớn để chibi đi vào hợp lý nhưng không nuốt phần lớn màn hình.

#### 6.2. Hình khối và góc mái

- Ưu tiên nhà có phần thân gọn, mái lớn nhưng không phình quá mức.
- Mái phải nhìn thấy mặt trên theo cùng một camera 3/4.
- Đường sống mái, đường tường và các cạnh kiến trúc phải thống nhất phối cảnh.
- Mái không được nhìn nghiêng như camera ngang trong khi nền lại nhìn từ trên.
- Nhà chính có cấu trúc phức tạp hơn nhà thường nhưng vẫn cùng ngôn ngữ thiết kế.
- Cửa hướng về vùng đi lại chính, không bị mái hay props che.

#### 6.3. Vật liệu theo vùng

| Region | Kiến trúc chủ đạo |
| --- | --- |
| Verdant | Gỗ sồi, tường trát kem, mái tranh, mái ngói xanh đậm |
| Frostbound | Gỗ sẫm, đá xám lạnh, mái dốc phủ tuyết |
| Sunscorch | Đá sa thạch, tường đất nung, mái bằng, vải bạt |
| Blighted | Gỗ ẩm, đá cổ rêu phủ, mái gỗ tối |
| Ashen | Đá bazan, kim loại rèn, gỗ cháy sẫm |
| Astral | Đá trắng xanh, đá phiến bạc, pha lê kiến trúc |

#### 6.4. Bóng tiếp đất

Mỗi nhà phải có:

- Bóng đậm ngay chân tường.
- Bóng dưới mái hiên và bậc cửa.
- Bóng lớn đổ xuống-phải theo hướng sáng chuẩn.
- Chuyển tiếp tự nhiên giữa nền và công trình.

Không được: Nhà trông như một icon đặt lên tấm thảm cỏ; không có tiếp xúc đất, mái quá lớn, cửa quá bé hoặc tỷ lệ mỗi ngôi nhà khác nhau vô lý.

### 7. Quy chuẩn địa hình và props

| Thành phần | Quy chuẩn |
| --- | --- |
| Cây thường | Tán khoảng 2–4 ô, thân rõ, bóng tiếp đất |
| Cây cổ thụ | Tán 4–7 ô, ưu tiên viền map và landmark |
| Bụi gai | Cụm 1–3 ô, không rải đều |
| Đá nhỏ | Khoảng 0.3–0.8 ô, chủ yếu ven đường |
| Đá lớn | 1–3 ô, silhouette rõ |
| Vách đá | Khối liên tục, có mặt trên sáng và mặt đứng tối |
| Suối | Thường rộng 1–3 ô, tùy địa hình |
| Cầu | Bề rộng di chuyển ít nhất 3 ô |
| Đường chính | Tối thiểu 3 ô thông thoáng |
| Quảng trường | Đủ không gian cho 10 hero, không có props chắn giữa |

#### Cây và rừng

- Tán cây lớn nằm thành từng cụm, chủ yếu quanh viền.
- Cây có thân rõ, rễ hoặc bóng tiếp đất.
- Rừng được xây dựng theo lớp: tán trước, thân giữa, bóng và cỏ dưới chân.
- Không lặp cùng một cây giống hệt nhau thành hàng như đóng dấu.
- Vùng tối được phép có cây dày nhưng đường đi vẫn phải đọc được.

#### Đá và vách

- Mặt trên sáng hơn mặt đứng.
- Mép trên của vách có highlight để nhận ra độ cao.
- Vách không được hòa tan vào đường đi.
- Đá có khối lớn, không vẽ quá nhiều đường nứt tí hon.
- Chân vách có bóng tiếp xúc và vài cụm đá vụn.

#### Nước

- Nước có một màu nền chủ đạo, thêm dòng chảy và highlight có chọn lọc.
- Bờ nước phải được phân biệt bằng đá, đất ẩm hoặc cỏ.
- Không phủ sọc trắng khắp mặt nước.
- Không để nước nhìn như một lớp nhựa cyan phát sáng.
- Cầu phải nằm đúng hướng lưu thông, có đủ diện tích di chuyển.

#### Đường và gameplay readability

Đường đi quan trọng phải rộng ít nhất 3 ô ≈128 px trên ảnh 1536×1024.

- Đường chính có màu tương phản vừa đủ với cỏ.
- Chỗ giao nhau và lối ra map phải đọc được ngay.
- Không đặt gốc cây, đá, nấm lớn hoặc hàng rào giữa đường.
- Không có đường giả dẫn đến chỗ bị khóa nếu không có dấu hiệu thị giác rõ.
- Bãi quái cần không gian mở, ít vật cản; có thể bố trí bãi mục tiêu khoảng 8×6 ô.
- Ba hàng ô cuối dành cho vùng HUD che khuất không nên chứa lối đi, portal hoặc NPC quan trọng.
- Rừng và vách đóng kín viền map, trừ các lối ra được thiết kế.

### 8. Quy chuẩn nhân vật và quái

#### 8.1. Tỷ lệ và hình dáng

| Loại | Tỷ lệ so với hero |
| --- | --- |
| Monster nhỏ | 0.6–0.9× |
| Monster thường | 0.9–1.4× |
| Monster lớn / Elite | 1.4–1.8× |
| Map Boss | 1.5–2.5× |
| Dungeon Boss | 2–3× hoặc hơn nếu arena cho phép |

Tỷ lệ là chiều cao hiển thị mục tiêu; cần test cùng camera vì sói nằm ngang và treant đứng thẳng có silhouette khác nhau.

- Đầu lớn, hình khối đơn giản và nhận diện được từ xa.
- Chi tiết đặc trưng phải đọc được ngay ở kích thước game.
- Mắt sáng, móng, vũ khí, lõi tinh thể chỉ là điểm nhấn.
- Boss phải lớn hơn quái cùng loài, có ít nhất 2–3 đặc điểm nhận diện riêng.
- Không tăng kích thước boss bằng cách kéo dãn sprite walk của quái thường.

#### 8.2. Sprite sheet chuẩn

| Thông số | Quy chuẩn |
| --- | --- |
| Canvas | 1024×1024 |
| Grid | 4 hàng × 4 cột |
| Cell | 256×256 |
| Hàng 1 | DOWN — nhìn về người xem |
| Hàng 2 | UP — nhìn lưng |
| Hàng 3 | LEFT — mặt hướng trái |
| Hàng 4 | RIGHT — mặt hướng phải |
| Background key | #FF00FF ưu tiên; #00FF00 nếu được yêu cầu |
| Alpha / nền | Nền phẳng opaque, không caro |
| Alignment | Chung anchor chân hoặc bóng dưới chân |
| Frame separation | Không chạm nhau, không chạm biên cell |

Với quái bay, bóng nằm cố định tại mặt đất; thân có thể nhấp nhô theo frame nhưng không làm thay đổi điểm đứng gameplay.

Khi quay UP: Vật phóng phải đi về phía trên màn hình, ra xa người xem. Không vẽ projectile bay xuống dưới như khi quay DOWN.

Khi quay LEFT / RIGHT: Vũ khí, đầu, tay và projectile phải tuân thủ hướng đó. Không tự đảo tay hoặc đổi thiết kế giữa các frame.

#### 8.3. Định dạng màu và chất lượng

- Viền tối rõ, không sử dụng viền xám nhạt biến mất trên nền tuyết.
- 2–3 mảng cel shading chính, có highlight nhỏ tại vật liệu đặc biệt.
- Màu ở walk và attack phải trùng nhau.
- Boss không được đổi số gai, vị trí sẹo, mắt, mũ hoặc áo giáp giữa animation.
- Hạt sáng và hiệu ứng không được có viền hồng từ nền key.
- Sprite giữ khoảng trống an toàn trong cell, đặc biệt với cánh, đuôi, vũ khí và gai dài.

Nền key là màu dành cho xử lý tách nền. Khi import Unity, phải loại màu key đúng cách, tránh viền magenta hoặc xanh lá ở mép sprite sau khi lọc texture.

### 9. Danh sách lỗi cấm — Art Rejection Rules

Bất kỳ ảnh mới nào có một trong các lỗi nghiêm trọng dưới đây phải sửa trước khi import vào game.

| Lỗi cấm | Lý do |
| --- | --- |
| Cỏ xanh neon, quá bão hòa | Lặp lại vấn đề của Oakhaven cũ |
| Đường đất cam gắt | Cạnh tranh với hero và VFX |
| Chi tiết nhỏ rải đều toàn map | Nhiễu, khó đọc bãi quái |
| Mỗi nhà/cây một hướng sáng | Mất cảm giác cùng thế giới |
| Nhà như đồ chơi, không có bóng tiếp đất | Mất trọng lượng, sai tỷ lệ |
| Trộn pixel art, 3D render và hand-painted | Không đồng nhất style |
| Viền đen dày quanh toàn bộ background | Làm map trông như sprite phóng to |
| Mái nhà/cây/đá sai góc camera | Phá phối cảnh |
| Đường quá hẹp, props chắn cổng | Cản gameplay |
| Bloom, haze, glow phủ toàn ảnh | Mất độ sắc nét |
| Quái đổi mặt, màu, chi tiết giữa frame | Animation thiếu nhất quán |
| Projectile quay UP nhưng bay xuống | Sai hướng chiến đấu |
| Nền caro, gradient, trong suốt ở sprite sheet yêu cầu chroma key | Không đúng pipeline |
| Chữ, nhãn map, UI, minimap, nhân vật trong background | Không phải background sạch |
| Sprite chạm biên cell hoặc bị cắt | Lỗi cắt animation |

Ngoài ra, không được tái tạo nguyên ảnh tham khảo nếu nhiệm vụ yêu cầu vẽ map mới. Phải giữ ngôn ngữ mỹ thuật nhưng thay bố cục, cảnh quan và landmark theo nội dung vùng.

### 10. Checklist duyệt ảnh trước khi đưa vào Unity

Đối với map, cần kiểm tra cả bản ảnh sạch và khi đặt vào game với HUD, nhân vật, monster và skill VFX. Một background đẹp khi xem riêng chưa chắc đọc tốt trong chiến đấu.

### 11. MASTER ART PROMPT — English

Đây là đoạn chuẩn dùng chung cho mọi yêu cầu vẽ map, nhà, prop hoặc quái. Dán vào đầu prompt rồi viết thêm yêu cầu cụ thể về loại asset, layout, animation và kích thước.

WORLD ART STYLE LOCK v1.0

WORLD ART STYLE LOCK — Create original artwork in a unified stylized hand-painted fantasy JRPG style, matching the approved New Oakhaven Village, Thatchwick Village, Frostfang Den, and existing Zenonia-like chibi monster sprites. Use a consistent orthographic 3/4 top-down viewpoint, never isometric or photorealistic. The main light always comes from the upper-left, casting short, soft shadows toward the lower-right, with convincing contact shadows. Environments use natural, moderately saturated colors, cohesive warm/cool lighting, painterly textures, crisp silhouettes, and selective fine details. Avoid neon grass, harsh orange paths, excessive clutter, mismatched perspectives, and inconsistent lighting. Buildings have believable proportions, visible rooftops, readable entrances, and natural material shading. Terrain preserves open gameplay routes and clearly grouped vegetation and obstacles. Characters and monsters use exaggerated chibi proportions, expressive faces, bold dark outlines, solid opaque colors, and clean two-to-three-tone cel shading. Maintain exact design, scale, lighting, and directional consistency across animation frames. Follow the specified canvas, sprite grid, and background color precisely. No unintended text, UI, borders, checkerboards, or extra objects.

Quy tắc sử dụng lâu dài: World Art Bible này là chuẩn mặc định. Mỗi vùng được thay màu, vật liệu, khí hậu và loại quái, nhưng không thay camera, nguồn sáng, ngôn ngữ hình khối, quy chuẩn viền và tỷ lệ gameplay.

Khi vẽ sprite quái mới, vẫn nên đính kèm sheet WALK đã duyệt của quái gần nhất; khi vẽ map mới, đính kèm Oakhaven mới và một map farm/dungeon tương ứng. Prompt chung giữ được định hướng, còn ảnh tham khảo giúp khóa thiết kế chính xác hơn.
