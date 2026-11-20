# Bible thiết kế lại quái (Monster Redesign Bible)

Lập 20/11/2026. Nguồn: chat ChatGPT art director (tài khoản Tay, CHỈ CHỮ, không vẽ ảnh nào): https://chatgpt.com/c/6aff88da-2f60-83ec-9211-1d9de14ecaa8

**Vì sao có file này.** Người dùng chê lượt vẽ lại 20/11 (27 quái): "tao kêu mày làm lại cả quái mà chứ ko phải đổi màu, làm lại cho phù hợp với game cute, quái nhìn có hồn hơn". Lượt đó gửi frame cũ làm mẫu nên ChatGPT giữ thiết kế và tư thế cũ, chỉ đổi tông. Từ nay **40 quái của `MonsterDef.All` (trừ Mire Hydra Queen v2, giữ nguyên làm thước đo boss) được THIẾT KẾ MỚI HOÀN TOÀN** theo file này: dễ thương kiểu Poring / RO, có hồn, mặt biểu cảm, silhouette riêng, có tính cách.

**Quy tắc cứng (đọc trước khi vẽ):**

1. **KHÔNG gửi frame cũ làm mẫu thiết kế; chỉ dùng để ghi cỡ/chân.** Cỡ thân, đường chân, độ lơ lửng ghi bằng SỐ trong prompt; tool cắt tự khớp chiều cao theo `Assets/ArtLibrary/monsters/ro-soft/heights.json`.
2. Mọi prompt vẽ ghi: "tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối".
3. Tin đầu mỗi chat vẽ gửi ảnh mẫu: `docs/reference/ro-soft-fantasy-5-class-redesign/40-players-new-classes-vs-mire-hydra-queen.png` (đích chất lượng, boss) + `docs/reference/ui-style-ro-soft-fantasy/05-pet.png` (độ dễ thương) + `docs/reference/hero-idle-down-5-classes.png` (cỡ hero). Khi ảnh ra trầm: gửi lại `docs/reference/ro-fantasy-heroes/sample-hero-user-pick.png` + `docs/reference/ui-style-ro-soft-fantasy/01-style-board.png`.
4. Đọc `docs/sprite-rules.md` (điều 0-8 mục art ChatGPT, mục "Quy tắc thiết kế quái", Sổ lỗi quái) trước khi vẽ.

Mục lục: 1. Quy tắc style chung + họ quái theo biome · 2-6. Từng quái theo biome (đủ 40, kèm master prompt và bảng tóm tắt của ChatGPT ở mục 6) · 7. Ràng buộc kỹ thuật · 8. Mẫu prompt (bản chốt dùng cho game) · 9. Thứ tự vẽ · 10. Kế hoạch quota.

**Bảng 40 quái (id game giữ nguyên, không đổi tên trong code):**

| # | id | Tên | Map | Lv | Vai trò | Cao thân (px ô 192) | Vẽ x |
|---|---|---|---|---|---|---|---|
| 1 | meadowSlime | Meadow Slime | Willowbrook | 1 | cận chiến yếu, chậm | 100 | 1 |
| 2 | hornRabbit | Horn Rabbit | Willowbrook | 3 | nhanh, giòn | 125 | 1 |
| 3 | thornBoar | Thorn Boar | Willowbrook | 5 | nặng, húc, thân rộng | 161 | 1 |
| 4 | meadowMantis | Meadow Mantis | Bramblewood | 6 | nhanh, sắc | 172 | 1 |
| 5 | forestWolf | Forest Wolf | Bramblewood / Rootburrow | 7 | nhanh, bầy | 140 | 1.1 |
| 6 | brambleTreant | Bramble Treant | Bramblewood / Rootburrow | 8 | tank chậm | 168 | 1.2 |
| 7 | thornSprite | Thorn Sprite | Bramblewood / Rootburrow | 9 | BAY, nhanh giòn | 130 | 1 |
| 8 | thornfangAlpha | Thornfang Alpha | Bramblewood (boss map) | 10 | boss | 166 | 1.65 |
| 9 | thornheartTreant | Thornheart Treant | Thornheart Chamber (boss hầm Rootburrow) | 10 | boss, tầm rộng | 180 | 2.0 |
| 10 | frostSlime | Frost Slime | Frost Hollow | 11 | cận chiến chậm | 104 | 1 |
| 11 | snowFox | Snow Fox | Frost Hollow | 12 | nhanh, giòn | 120 | 1 |
| 12 | iceImp | Ice Imp | Frost Hollow | 12 | trung bình, tầm ngắn | 135 | 1 |
| 13 | frostBear | Frost Bear | Frost Hollow | 13 | tank chậm | 158 | 1.15 |
| 14 | iceWraith | Ice Wraith | Frost Hollow | 13 | BAY, nhanh giòn | 165 | 1 |
| 15 | frostbiteYeti | Frostbite Yeti | Frost Hollow | 14 | nặng chậm | 162 | 1.2 |
| 16 | crystalGolem | Crystal Golem | Frost Hollow | 15 | tank chậm nhất | 169 | 1.25 |
| 17 | frostfangKing | Frostfang King | Frost Hollow (boss map) | 15 | boss | 155 | 1.65 |
| 18 | iceGoblin | Ice Goblin | Frostfang Den | 16 | cận chiến chuẩn | 151 | 1 |
| 19 | spectralWolf | Spectral Wolf | Frostfang Den | 18 | nhanh, đòn liên tục | 131 | 1 |
| 20 | glacierWarden | Glacier Warden | Frostfang Den (boss) | 20 | boss | 163 | 1.65 |
| 21 | rimeclawTyrant | Rimeclaw Tyrant (sói vua băng, đổi 20/11) | boss truy nã | 10 | raid boss, đứng yên | 330 (ô 384) | 2.5 |
| 22 | glacialColossus | Glacial Colossus | boss truy nã | 25 | raid boss, đứng yên | 330 (ô 384) | 2.5 |
| 23 | highlandIbex | Highland Ibex | Sunbreak | 16 | húc, tầm ngắn | 168 | 1 |
| 24 | dustscaleLizard | Dustscale Lizard | Sunbreak | 17 | nhanh, giòn | 120 | 1.15 |
| 25 | cliffHarpy | Cliff Harpy | Sunbreak | 18 | BAY, bắn lông vũ (tầm xa) | 150 | 1 |
| 26 | stonebackGuardian | Stoneback Guardian | Sunbreak | 20 | tank chậm nhất | 166 | 1.4 |
| 27 | ridgehornBehemoth | Ridgehorn Behemoth | Sunbreak (boss map) | 20 | boss | 178 | 1.8 |
| 28 | duneScarab | Dune Scarab | Amber Dunes | 21 | nhanh | 99 | 1 |
| 29 | sandRaider | Sand Raider | Amber Dunes | 26 | cận chiến nặng | 145 | 1 |
| 30 | amberWisp | Amber Wisp | Amber Dunes | 30 | BAY, nhanh | 110 | 1 |
| 31 | giantScorpion | Giant Scorpion | Scorpion Gorge | 26 | cận chiến chuẩn | 150 | 1 |
| 32 | sandWraith | Sand Wraith | Scorpion Gorge | 27 | BAY, ném cầu cát (tầm xa) | 160 | 1 |
| 33 | rockBasilisk | Rock Basilisk | Scorpion Gorge | 28 | thấp, nhanh | 130 | 1 |
| 34 | duneGolem | Dune Golem | Scorpion Gorge | 29 | tank chậm | 179 | 1.3 |
| 35 | duneDevourer | Dune Devourer | Scorpion Gorge (boss map) | 30 | boss | 151 | 1.9 |
| 36 | bogSlime | Bog Slime | Emberfen Marsh | 31 | cận chiến chậm | 96 | 1 |
| 37 | mireToad | Mire Toad | Emberfen Marsh | 33 | nặng | 106 | 1 |
| 38 | marshStalker | Marsh Stalker | Emberfen Marsh | 35 | nhanh, sắc | 143 | 1 |
| 39 | ruinWisp | Ruin Wisp | Sunken Reliquary | 38 | BAY, nhanh | 118 | 1 |
| 40 | mirefangHydra | Mirefang Hydra | Sunken Reliquary (boss map) | 40 | boss | 168 | 1.85 |

Cao thân = chiều cao thân đo ở sheet đi cũ (`heights.json`), tool giữ nguyên để thanh HP, hitbox, cỡ trong game không đổi. "Vẽ x" = `RenderScale` trong `MonsterDef.cs` (game phóng, không vẽ ô to hơn, trừ boss truy nã).

## 1. Quy tắc style chung + họ quái theo biome (ChatGPT art director, tin 1)

### ART DIRECTION V1 — MONSTER REDESIGN

RO Soft Fantasy · 2D Chibi RPG · Monster Style Bible

Mục tiêu: Thiết kế lại toàn bộ quái từ đầu, không còn cảm giác một model được đổi màu thành nhiều quái khác nhau. Mỗi quái phải có silhouette riêng, gương mặt có hồn, tính cách dễ nhận ra và vẫn thuộc cùng một thế giới với hero và Mire Hydra Queen.

Sau khi đối chiếu hai ảnh, tôi đề xuất chốt hai chuẩn:

- Ảnh 1 — chuẩn chất lượng: Mire Hydra Queen là chuẩn cuối cùng về độ hoàn thiện, thiết kế khối, chất lượng viền, màu sắc và khả năng đọc nhân vật giữa combat đông người. Không sửa boss này.
- Ảnh 2 — chuẩn độ dễ thương: Frost Slime, Horn Rabbit và Frostfang King thể hiện hướng quái tròn mềm, mắt giàu biểu cảm, phụ kiện đặc trưng và màu sắc tươi nhưng dịu.

Công thức tổng thể:

70% RO cute fantasy + 20% cá tính sinh vật + 10% nguy hiểm.

Tỉ lệ thay đổi dần theo cấp và loại quái. Boss được phép nguy hiểm hơn nhưng không trở thành quái kinh dị hoặc dark fantasy.

### A. QUY TẮC STYLE CHUNG

#### A1. Ngôn ngữ hình khối

Quái phải có thiết kế tròn, mềm, ít chi tiết vụn nhưng silhouette mạnh. Không bắt buộc tất cả quái đều đầu to thân nhỏ: slime, côn trùng, thú bốn chân và quái nguyên tố cần giữ cấu trúc cơ thể riêng.

Tỉ lệ đề xuất theo họ sinh vật

| Slime / Wisp | Thân chính chiếm 75–90%, mặt đặt trực tiếp trên thân |
|---|---|
| Thú bốn chân | Đầu 35–45% chiều dài tổng thể; chân ngắn, bàn chân lớn |
| Quái hai chân | Đầu 40–50% chiều cao; thân 30–35%; chân 20–25% |
| Côn trùng | Đầu 30–40%; thân bầu tròn; chân và càng tối giản |
| Golem / Treant | Đầu 30–40%; thân to, vai rộng, tay chân ngắn |
| Boss | Cho phép cơ thể dài hoặc rộng hơn; giữ đầu và gương mặt dễ đọc |

##### Quy tắc silhouette

- Một quái phải nhận ra được khi biến thành hình bóng đơn sắc.
- Có một khối chính + tối đa hai đặc trưng silhouette nổi bật.
- Ví dụ: thỏ là hai tai lớn; bọ cạp là càng và đuôi; treant là tán cây và cánh tay rễ.
- Không gắn sừng, gai, cánh, đuôi, tinh thể lên mọi quái.
- Không dùng cùng một thân tròn, chỉ đổi mắt, sừng và màu để tạo loài mới.
- Tay, chân và phụ kiện phải đọc rõ khi thu sprite về kích thước thực trong game.

Quy tắc quan trọng: Mỗi quái cần một hình dáng tổng thể riêng, không chỉ một bộ trang trí riêng.

#### A2. Khuôn mặt và tính cách

Đây là phần cần thay đổi mạnh nhất so với phong cách quái chỉ đổi màu.

##### Mắt

- Mắt lớn, sáng, có highlight trắng ngà.
- Ưu tiên mắt oval, tròn hoặc tam giác bo mềm.
- Khoảng cách hai mắt đủ thoáng để tạo cảm giác ngây thơ.
- Mắt không bắt buộc giống nhau giữa các loài.
- Quái hung dữ được dùng mắt hẹp, lông mày nghiêng nhưng không quá dữ tợn.
- Mỗi loài có một đặc điểm mắt riêng: lim dim, lém lỉnh, ngơ ngác, cau có, kiêu ngạo...

##### Miệng

Phải có nhiều kiểu miệng, không dùng một nụ cười hình chữ U cho cả bộ.

| Kiểu miệng | Cảm giác |
|---|---|
| ω | Đáng yêu, vô tư |
| o | Ngơ ngác, bất ngờ |
| w có răng nhỏ | Tinh nghịch |
| Cười lệch | Láu cá |
| Một răng nanh | Hung hăng nhưng dễ thương |
| Miệng rộng, 2–4 răng | Quái mạnh, boss |
| Mím môi hoặc cau có | Cứng đầu, nghiêm nghị |

Các dạng trên là gợi ý hình học, không phải khuôn mặt bắt buộc.

##### Biểu cảm theo animation

Mỗi quái tối thiểu có ba trạng thái:

- Idle / Walk: Tính cách mặc định, rõ mặt.
- Attack: Mắt và miệng thể hiện ý định tấn công, không chỉ đưa tay ra.
- Hit: Mắt nhắm hoặc co lại, má và cơ thể bị nén.

Một con slime ngơ ngác có thể phồng má khi tấn công. Một con sói kiêu ngạo có thể nhe chiếc răng nhỏ và nhướng một bên lông mày.

Không cần tất cả quái phải cười. Sự đa dạng tính cách tạo cảm giác chúng thực sự là các sinh vật khác nhau.

#### A3. Viền, đổ bóng và vật liệu

Bảng màu nền tảng RO Soft Fantasy

Viền nâu chính

#50382F

Viền phụ

#745347

Highlight kem

#FFF1D4

Bóng ấm

#C48F74

Ánh sáng vàng

#FFE5A5

Trắng ngà

#FFF8EB

##### Line art

- Viền ngoài nâu đậm, mềm, có thay đổi độ dày theo khối.
- Viền ngoài khoảng 3–5 px ở sprite gốc 192 px, tùy kích thước sinh vật.
- Đường nội bộ 1.5–3 px, nhạt hơn viền ngoài.
- Những phần tiếp giáp nhau có thể dùng viền màu cùng họ để tránh quá nặng.
- Tránh viền đen thuần #000000 và đường nét mảnh như tranh anime.

##### Cel shading

Sử dụng 2–3 cấp bóng mềm:

- Base color: màu pastel chính.
- Shadow: đậm hơn, hơi ấm hoặc hơi lạnh theo biome.
- Highlight: màu kem hoặc màu sáng cùng họ.

Mỗi vật liệu vẫn có phản ứng riêng:

| Chất liệu | Cách thể hiện |
|---|---|
| Lông | Mảng lớn, vài chóp lông, không vẽ từng sợi |
| Slime | Highlight lớn, bóng mềm, độ trong giả lập |
| Đá | Khối bo cạnh, 2–3 mặt màu rõ |
| Băng | Mặt sáng xanh lam nhạt, cạnh trắng |
| Gỗ | Vân cong ngắn, mắt gỗ thưa |
| Cát | Vàng kem, bóng nâu mật ong |
| Bùn | Olive và nâu ấm, không dùng đen bẩn |
| Ma thuật | Khối sáng rõ, ít hạt rời, không neon |

Không dùng gradient airbrush phủ toàn bộ cơ thể hoặc glow quá mạnh khiến sprite mờ.

#### A4. Kích thước theo cấp và vai trò

Hero cao khoảng 160 px. Không nên tăng kích thước quái tuyến tính theo level; phải dựa vào loài và vai trò.

| Loại | Chiều cao gợi ý | So với hero |
|---|---|---|
| Quái bé / bay | 90–120 px | 0.56–0.75× |
| Quái thường | 110–150 px | 0.69–0.94× |
| Quái lớn | 150–180 px | 0.94–1.13× |
| Boss map / hầm | 210–300 px | 1.3–1.9× |
| Wanted Boss | Khoảng 400 px | 2.5× |

Các số trên là kích thước nhân vật sau khi dựng, không nhất thiết là chiều cao được vẽ trong từng ô sprite. Boss có thể cần sheet lớn hơn 256 px; phải chọn kích thước cell phù hợp để không cắt sừng, cánh, đuôi và animation.

Đặc biệt, slime Lv31 vẫn có thể nhỏ hơn sói Lv8. Cấp độ thể hiện bằng sự trưởng thành trong thiết kế, không chỉ bằng chiều cao.

#### A5. Độ dễ thương và nguy hiểm theo cấp

Thang định hướng cảm giác

Chỉ số art direction, không phải thông số gameplay

Lv1–10

Cute 85% · Threat 15%

Lv11–20

Cute 75% · Threat 25%

Lv21–30

Cute 65% · Threat 35%

Lv31–40

Cute 55% · Threat 45%

Boss thường

Cute 45% · Threat 55%

Wanted Boss

Cute 35% · Threat 65%

Dễ thương

Đe doạ

Quái cấp cao hơn có thể dùng:

- Mắt sắc và biểu cảm tự tin hơn.
- Sừng, nanh hoặc giáp lớn hơn.
- Silhouette bất đối xứng có kiểm soát.
- Họa tiết cổ ngữ, vết nứt ma thuật hoặc cấu trúc cơ thể đặc biệt.

Nhưng không dùng tăng độ tối, tăng gai nhọn và tăng số lượng phụ kiện làm công thức nâng cấp level.

#### A6. Quy tắc thiết kế Boss

Mire Hydra Queen trong ảnh 1 là chuẩn chất lượng cho toàn bộ boss về sự trau chuốt, sức hút thị giác và độ rõ giữa đám đông.

##### Boss phải có 5 yếu tố

- Silhouette độc quyền: Nhìn từ xa phân biệt được với mọi quái thường.
- Gương mặt có cá tính: Có thể hung dữ, kiêu ngạo, cổ xưa, tinh nghịch, không vô cảm.
- Một điểm nhấn chủ đạo: Đầu, sừng, mặt nạ, vương miện, lõi năng lượng hoặc bộ phận cơ thể.
- Phân cấp chi tiết: Đầu và phần trên nổi bật nhất, thân ít chi tiết hơn, chân và đuôi đơn giản hơn.
- Dễ đọc trong combat: Không bị che mắt, vùng đầu và điểm yếu bởi hiệu ứng hoặc chi tiết trang trí.

##### Những điều cần tránh

- Không lấy quái thường rồi phóng to và đổi màu.
- Không nhân đôi số sừng, gai và mắt một cách tùy tiện.
- Không dùng đầu lâu, máu, thịt thối hay chất liệu kinh dị.
- Không sao chép vương miện, thân rắn và bố cục nhiều đầu của Mire Hydra Queen sang các boss khác.
- Không vẽ hiệu ứng skill cố định vào mọi frame cơ bản.

Boss đúng chuẩn phải đẹp như một nhân vật chính của biome, chứ không chỉ là quái thường kích thước lớn.

#### A7. Chuẩn sprite và animation

| Hạng mục | Quy chuẩn |
|---|---|
| Hướng | Xuống, lên, phải, trái = flip phải |
| Walk | 4 frame mỗi hướng |
| Attack | 4 frame mỗi hướng |
| Ô walk | 192×192 px |
| Ô attack | 256×256 px |
| Góc nhìn | Top-down 3/4, nhất quán với hero |
| Viền | Nâu đậm mềm |
| Ánh sáng | Trên-trái |
| Nền bàn giao | Phẳng #00FF00 hoặc #FF00FF |
| Shadow mặt đất | Tách riêng, không vẽ dính vào sprite |

Với sheet ba hàng, mỗi animation có 4 cột × 3 hàng, tức 12 frame. Walk và Attack là hai sheet riêng.

##### Quy tắc nhất quán bắt buộc

- Frame nhìn lên không được lộ mắt, miệng ở mặt trước.
- Frame nhìn phải phải giữ chính xác bộ phận bên trái/bên phải khi lật.
- Mắt, miệng, sừng, cánh, đuôi không tự thay đổi thiết kế giữa các hướng.
- Vị trí chân chạm đất cố định theo pivot.
- Animation nảy phải có squash/stretch, nhưng thể tích tổng thể không thay đổi quá mức.
- Slime không cần mô phỏng bước chân; thú bốn chân, côn trùng và humanoid phải có chuyển động đặc thù.
- Quái bay có nhịp lơ lửng, cánh hoặc cơ thể chuyển động rõ, không có bóng đổ dính dưới chân.
- Frame attack phải có chuẩn bị, ra đòn, tiếp diễn và hồi thế, không chỉ là bốn lần thay đổi vị trí hiệu ứng.

### B. QUY TẮC HỌ QUÁI THEO BIOME

Tôi chia thành 7 họ môi trường lớn, trong đó Snow có khu vực hang riêng, Desert có Gorge riêng và Marsh có Ruins riêng. Các khu vực phụ phải cùng hệ màu nhưng có motif phụ để người chơi cảm thấy đang tiến sâu vào một hệ sinh thái.

#### B1. MEADOW — Willowbrook (Lv1–5)

Từ khóa: Round · Bouncy · Innocent · Spring

Cảm giác đồng cỏ ấm áp, sinh vật nhỏ, tò mò, thân thiện. Quái phải giống những sinh vật đáng yêu người chơi muốn nuôi làm pet.

Bảng màu Meadow

#A9D77C

#6FAF68

#F4DA8C

#F6C4A5

#FFF1D7

#876B50

Hình khối chung: Hình tròn, quả lê, giọt nước, oval; góc bo mạnh, chân ngắn, tai hoặc mũi lớn.

Motif chung: Lá non, cỏ ba lá, cánh hoa, mầm cây và các đốm màu nhẹ.

Chi tiết lặp lại: Mảng sáng kem trên bụng hoặc má; điểm nhấn xanh lá tươi; chi tiết thiên nhiên nhỏ nằm ở vùng đầu hoặc lưng.

Biểu cảm: Vui vẻ, ngơ ngác, hơi nhát hoặc nghịch ngợm.

Không được: Gai sắc, mắt đỏ dữ tợn, quá nhiều giáp, bùn tối.

Ba quái Meadow phải là ba silhouette khác hẳn nhau, không phải ba phiên bản tròn có thêm tai hoặc sừng.

#### B2. FOREST — Bramblewood / Rootburrow (Lv6–10)

Từ khóa: Mossy · Woody · Mischievous · Ancient

Rừng có sinh vật láu cá, hơi hoang dã. Rootburrow là phiên bản cổ xưa hơn, nhiều rễ cây, đất sâu và gỗ già.

#91BD70

#527E4F

#B6A06F

#795B44

#E2C487

#D6DE9A

Hình khối chung: Oval hơi nhọn, tán lá tròn, thân hình quả thông, rễ cong, tai và lông tạo hình lá.

Motif chung: Lá răng cưa, dây leo, gai mềm của bụi mâm xôi, rêu, mắt gỗ và hạt giống.

Chi tiết lặp lại: Xanh rêu ở phần lưng hoặc trên đầu, gỗ nâu ấm, điểm nhấn vàng mật ong ở mắt, hoa hoặc lõi ma thuật.

Biểu cảm: Lém lỉnh, cảnh giác, cáu kỉnh nhẹ; boss có vẻ cổ xưa và uy nghi.

Rootburrow: Giảm xanh lá sáng, tăng nâu rễ và vàng hổ phách; dùng vòng rễ, mắt gỗ, nấm nhỏ như motif phụ. Không biến quái thành zombie cây tối màu.

Đặc biệt: Thorn Sprite phải có cảm giác tinh linh bay thật sự, khác Treant bằng độ nhẹ, hình giọt và lá. Hai boss Thornfang Alpha và Thornheart Treant phải có ngôn ngữ silhouette riêng.

#### B3. SNOW — Frost Hollow / Frostfang Den

Từ khóa: Fluffy · Crystal · Playful · Majestic

Tuyết mềm, băng trong, thú lông xù, ma thuật lạnh nhưng vẫn đáng yêu. Snow là biome có thể sáng và rực rỡ nhất về hiệu ứng, nhưng không được neon.

#D9F3FF

#82CBEA

#4D9FCB

#F7F7EA

#A8A1D8

#527294

Hình khối chung: Bầu tròn, lông phồng, tai lớn, chân ngắn; pha hình tinh thể có cạnh bo mềm.

Motif chung: Tinh thể băng ba chấu, chóp lông trắng, bông tuyết sáu cánh, băng phủ trên đầu hoặc vai.

Chi tiết lặp lại: Highlight trắng ngà, bóng lam dịu, một điểm xanh sapphire hoặc tím băng.

Biểu cảm: Hiếu động, tinh nghịch, hơi kiêu hãnh; boss mang vẻ hoàng gia hoặc linh thú.

Frostfang Den: Tăng lam sâu và tím băng, nhiều đường tinh thể và dấu cổ ngữ hơn, nhưng vẫn giữ vùng mặt sáng.

Phân biệt vật liệu: Frost Slime mềm và trong; Snow Fox mềm mượt; Ice Imp có dáng nhanh; Golem có mảng tinh thể cứng. Không cho mọi quái mọc cùng một kiểu tinh thể trên đầu.

Boss: Frostfang King là chuẩn linh thú tuyết; Glacier Warden phải có tính cách canh giữ khác biệt; Rimeclaw Tyrant và Glacial Colossus cần có silhouette Wanted riêng, không chỉ là phiên bản lớn hơn của boss thường.

#### B4. HIGHLAND — Sunbreak Foothills (Lv16–20)

Từ khóa: Windy · Rugged · Proud · Sunlit

Vùng núi cao nắng vàng, động vật cứng cáp, thích nghi với gió, vách đá và độ cao. Mạnh mẽ hơn Forest nhưng vẫn bo mềm.

#DCC18A

#B68C60

#879E70

#F2E1B7

#8EACB5

#755D51

Hình khối chung: Vai hoặc lưng cao, chân chắc, sừng cong, vảy lớn, hình thang bo góc và khối đá tròn.

Motif chung: Vòng xoắn sừng, vệt gió cong, vân đá cát, dấu nắng vàng và lông hoặc vảy có hình nêm.

Chi tiết lặp lại: Bụng hoặc mặt màu kem cát, màu nhấn vàng ấm, các mảng nâu đá ở chân hoặc lưng.

Biểu cảm: Cứng đầu, hiếu thắng, tự tin, có chút kiêu ngạo.

Quái bay: Cliff Harpy cần cánh lớn, thân gọn, đuôi có hình lông vũ rõ. Động tác bắn lông vũ phải nhận ra được ngay cả khi chưa có projectile VFX.

Boss: Ridgehorn Behemoth phải tạo cảm giác sức nặng và sức bật; ưu tiên sừng và vai làm điểm nhận diện, không biến thành một Golem có sừng.

#### B5. DESERT — Amber Dunes / Scorpion Gorge

Từ khóa: Amber · Sandy · Clever · Mysterious

Sa mạc sáng vàng mật ong, sinh vật lanh lợi, có cấu trúc giáp và vảy bo tròn. Scorpion Gorge chuyển sang đá đỏ và màu hổ phách cổ.

#EBC77D

#C9965D

#AC7253

#F6E4B7

#8D9B82

#79534C

Hình khối chung: Giáp vỏ tròn, thân thấp, đuôi cong, dáng thon của kẻ săn mồi; vài hình tam giác bo cạnh gợi cát và đá.

Motif chung: Vòng xoắn cát, hoa văn hổ phách, hình thoi trên vỏ, đường vân gió và mảnh đá cát.

Chi tiết lặp lại: Mép vỏ màu kem sáng, bóng nâu đỏ ấm, một điểm xanh xám hoặc vàng hổ phách để cân bằng.

Biểu cảm: Tinh ranh, lanh lợi, láu cá; các loài lớn có cảm giác lì lợm.

Scorpion Gorge: Tăng đá nâu đỏ, xám sa mạc và dấu phong hóa. Không chuyển thành tông đỏ đen, dung nham hoặc địa ngục.

Quái bay: Amber Wisp và Sand Wraith phải khác nhau cả khối chính lẫn chuyển động. Amber Wisp có cảm giác ma thuật sáng, nhẹ; Sand Wraith mang cảm giác bụi cát và gió xoáy.

Boss: Dune Devourer cần silhouette đặc biệt gắn với địa hình cát, không chỉ là bọ cạp hoặc thằn lằn khổng lồ.

#### B6. MARSH — Emberfen Marsh (Lv31–35)

Từ khóa: Squishy · Damp · Strange · Adorable

Đầm lầy kỳ lạ, ẩm ướt, có chút tinh nghịch và nguy hiểm nhưng không ghê rợn. Sinh vật trông như những cư dân thực sự của vùng đầm, không phải quái độc đổi màu xanh.

#A3B77A

#6C9066

#D4BD82

#A88365

#C8D6A8

#675F51

Hình khối chung: Khối tròn bẹp, cơ thể mềm, mắt nhô, chi ngắn hoặc dài tùy loài; đường cong bất đối xứng nhẹ.

Motif chung: Lá sen, cỏ lau, bọt nước, vết bùn nâu nhạt, rêu và gợn nước.

Chi tiết lặp lại: Điểm olive xanh sáng, một mảng nâu bùn, highlight vàng kem hoặc xanh lục nhạt ở vùng mặt.

Biểu cảm: Ngơ ngác, lười biếng, tinh quái, hơi ranh mãnh. Độ nguy hiểm đến từ hành vi và khả năng phục kích, không phải gương mặt kinh dị.

Phân biệt: Bog Slime cần mềm và nảy; Mire Toad có mắt và miệng đặc trưng của cóc; Marsh Stalker phải có dáng săn mồi rõ. Không dùng chung mặt tròn, mắt tím và lá sen cho cả ba.

Quan trọng: Không dùng xanh huỳnh quang, tím neon, chất nhầy đen, xương hoặc vết thương thối rữa.

#### B7. RUINS — Sunken Reliquary (Lv38–40)

Từ khóa: Ancient · Sacred · Aquatic · Regal

Tàn tích cổ chìm trong nước, pha trộn văn minh đá cũ, ánh sáng phép thuật và hệ sinh thái đầm lầy. Đây là biome có mức độ huyền bí cao nhất trong bộ hiện tại.

#64B9AF

#2E817B

#D8BC83

#8E819F

#EFDCAB

#5F536C

Hình khối chung: Vòng cung, thân uốn, đá chạm khắc bo cạnh, hình kim cương và đường cong giống dòng nước.

Motif chung: Phù văn cổ, vòng đá, hoa sen cách điệu, giọt nước ma thuật, họa tiết vàng cổ.

Chi tiết lặp lại: Xanh ngọc làm màu liên kết, vàng cổ trên họa tiết, tím xám làm bóng, ánh kem ở những chi tiết cần tập trung.

Biểu cảm: Bí ẩn, thông minh, nghiêm nghị, hơi kiêu hãnh; không nhất thiết dữ tợn.

Ruin Wisp: Là tinh linh tàn tích, cần khác Amber Wisp và Sand Wraith bằng hình khối, cấu trúc và nhịp chuyển động, không chỉ bằng màu và hoa văn.

Mirefang Hydra: Boss tàn tích cần thừa hưởng ngôn ngữ đầm nước, phù văn và sự uy nghi cổ xưa. Tuy nhiên không được sao chép bố cục hoặc đặc điểm nhận dạng của Mire Hydra Queen đã duyệt.

Với Mire Hydra Queen làm chuẩn, cần đặc biệt chú ý chất lượng nét mặt, tạo hình cổ và đầu rắn, mật độ chi tiết và các mảng sáng tối.

### C. QUY TẮC THỐNG NHẤT GIỮA CÁC BIOME

Có một nguy cơ lớn: nếu cố gắng đồng nhất mọi thứ quá mạnh, 40 quái mới vẫn sẽ giống nhau dù đã vẽ mới.

Tôi đề xuất dùng hệ thống nhận diện ba tầng:

| Tầng | Mục đích | Ví dụ |
|---|---|---|
| 1. Loài | Nhận diện quái ngay lập tức | Thỏ tai dài, bọ cạp càng lớn, slime mềm |
| 2. Biome | Nhận ra môi trường sống | Meadow có lá non, Snow có băng, Desert có cát |
| 3. Cá tính | Tạo sự khác biệt và sức sống | Nhút nhát, láu cá, kiêu ngạo, cáu kỉnh |

Thứ tự ưu tiên: Loài → Cá tính → Biome.

Motif biome là yếu tố liên kết màu sắc và môi trường, không phải chi tiết bắt buộc phải gắn lên mọi quái.

Chẳng hạn, một con sói rừng không nhất thiết phải đội lá cây. Chỉ cần lông, màu mắt và mảng nâu rêu phù hợp đã đủ liên kết với Bramblewood.

#### D. CHECKLIST NGHIỆM THU MỖI QUÁI

Khi thiết kế từng quái ở các lượt tiếp theo, tôi đề xuất kiểm tra 10 tiêu chí sau. Có thể dùng checklist này như quy tắc chung để duyệt bản vẽ.

10 tiêu chí thiết kế

0/10 đạt

Silhouette khác biệt rõ với những quái đã có
Mặt có biểu cảm và tính cách riêng
Hình khối phù hợp loài, không lạm dụng dáng slime
Màu sắc thuộc palette biome
Có 1–2 motif đặc trưng, không trang trí thừa
Viền nâu mềm và cel shading đúng chuẩn
Đọc được ở kích thước thực trong game
Tất cả hướng giữ đúng giải phẫu và phụ kiện
Walk/Attack có chuyển động và biểu cảm riêng
Không giống quái cũ chỉ đổi màu hoặc thêm phụ kiện

##### Thang điểm duyệt

| Điểm | Kết luận |
|---|---|
| 9.0–10 | Đạt chuẩn RO Soft Fantasy, được duyệt |
| 8.0–8.9 | Tốt, cần polish chi tiết |
| 7.0–7.9 | Cần sửa silhouette, biểu cảm hoặc màu |
| Dưới 7.0 | Thiết kế lại, không chỉ chỉnh màu |

Ngoài điểm số, có ba lỗi loại trực tiếp: trùng silhouette, thiếu cá tính hoặc chỉ đổi màu từ quái cũ.

### E. KẾT LUẬN ART DIRECTION

Để bộ quái đạt chất lượng đồng nhất với Mire Hydra Queen, tôi chốt 5 nguyên tắc không thay đổi qua các lượt thiết kế:

- Cute nhưng không trẻ con: Quái đáng yêu như sinh vật trong thế giới Ragnarok, không phải sticker emoji.
- Mỗi quái là một loài thật sự: Có cấu tạo, silhouette, biểu cảm và chuyển động riêng.
- Màu sáng ấm, pastel dịu: Bóng 2–3 cấp, viền nâu mềm; boss có thể đậm và tương phản hơn.
- Biome liên kết bằng ngôn ngữ thiết kế: Không đồng phục hóa toàn bộ sinh vật bằng cùng một motif.
- Boss đạt chất lượng nhân vật chính: Mỗi boss có cá tính và cấu trúc độc quyền, không làm phiên bản phóng to của quái thường.

Lưu ý về danh sách: Tôi đếm được 39 tên quái/boss riêng biệt trong mô tả hiện tại, tính cả Mire Hydra Queen và Mirefang Hydra là hai tên khác nhau, không phải 40. Trước khi hoàn thiện toàn bộ bộ quái, cần xác nhận một tên còn thiếu hoặc liệu hai tên Hydra có phải cùng một boss.

Thứ tự triển khai đề xuất: Meadow → Forest → Snow → Highland → Desert → Marsh → Ruins. Mỗi lượt 8–10 quái, nhưng mỗi quái phải được chốt thiết kế nhận diện trước khi triển khai toàn bộ sprite sheet hướng đi và đánh.

Chuẩn cuối cùng: Đặt bất kỳ quái mới nào cạnh hero trong ảnh 1 và các pet trong ảnh 2, người chơi phải cảm thấy chúng cùng một thế giới, nhưng không thể nhầm quái này với quái khác dù tắt màu sắc.

## 2. Meadow (Willowbrook) + Forest (Bramblewood, Rootburrow, Thornheart): 9 quái

### MONSTER REDESIGN — LƯỢT 1/5

9 quái Meadow + Forest | RO Soft Fantasy | Art Direction V2

Giữ nguyên toàn bộ quy tắc A và B đã chốt. Lượt này thiết kế 9 sinh vật mới hoàn toàn về cấu trúc, silhouette, khuôn mặt và chuyển động, không chỉnh sửa hoặc đổi màu từ quái cũ.

Mục tiêu: Quái thường đáng yêu như pet trong ảnh 2; hai boss đạt độ hoàn thiện của Mire Hydra Queen trong ảnh 1, nhưng có ngôn ngữ tạo hình riêng.

#### I. Quy chuẩn kích thước và nhận diện

| Quái | Lv | Cao thiết kế | Scale Unity | Vai trò |
|---|---|---|---|---|
| Meadow Slime | 1 | 100 px | ×1.0 | Yếu, chậm, cận chiến |
| Horn Rabbit | 3 | 125 px | ×1.0 | Nhanh, máu thấp |
| Thorn Boar | 5 | 160 px | ×1.0 | Nặng, húc |
| Meadow Mantis | 6 | 170 px | ×1.0 | Nhanh, đánh sắc |
| Forest Wolf | 7 | 140 px | ×1.0 | Nhanh, đi bầy |
| Bramble Treant | 8 | 170 px | ×1.2 | Tank, chậm |
| Thorn Sprite | 9 | 130 px | ×1.0 | Bay, nhanh, máu thấp |
| Thornfang Alpha | 10 | 166 px | ×1.65 | Boss bầy sói |
| Thornheart Treant | 10 | 180 px | ×2.0 | Boss cuối Rootburrow |

Chiều cao thiết kế là chiều cao hình vẽ trước khi áp dụng scale. Chiều cao hiển thị ước tính của Treant thường ≈204 px, Thornfang Alpha ≈274 px và Thornheart Treant ≈360 px, so với hero ≈160 px. Cần dùng ô boss lớn hơn nếu biên animation vượt ô 256 px.

Quy tắc góc nhìn: D = xuống (thấy mặt và ngực), U = lên (không thấy mặt), R = phải (nghiêng 3/4). Hướng trái lật từ R, không thiết kế riêng.

### II. MEADOW — WILLOWBROOK

Bộ ba Meadow sử dụng ba khối hoàn toàn khác nhau:

- Slime: giọt nước bẹp, đáy rộng.
- Rabbit: quả lê đứng, hai tai vươn cao.
- Boar: viên đá oval nằm ngang, vai và mõm lớn.

Đều sử dụng xanh cỏ, vàng kem, hồng đào và nâu ấm. Motif lặp lại là lá non, dấu cỏ ba lá hoặc đốm sáng kem, nhưng không bắt buộc mọi quái phải có lá trên đầu.

#### 01. Meadow Slime — Lv1

Tên gợi ý: Dewdrop Slime · Beginner monster

1. Tính cách: Ngây thơ, tò mò, hơi hậu đậu, thường vui vẻ nảy tới chỗ người chơi mà không biết mình đang gây nguy hiểm.

2. Silhouette / hình khối

- Khối giọt thạch lùn hình chuông, đáy rộng và hơi bẹt, đỉnh nghiêng nhẹ sang một bên.
- Cao 100 px, rộng khoảng 105–115 px.
- Không chân, không tay, không sừng.
- Thân mềm, chia thành khối thạch tròn chính và vành chân thạch mỏng bên dưới.
- Đỉnh có một chỏm thạch nhỏ nghiêng; không tạo hình giọt nước nhọn sắc.
- Điểm nhận diện là hình dáng giọt nước đang hơi đổ về một bên, không phải quả cầu slime thông thường.

3. Đặc trưng chữ ký

Một mầm cỏ hai lá rất nhỏ mọc từ chỏm thạch, bên trong thân có một bọt khí tròn màu kem lệch sang trái. Bọt khí nằm trong thạch, không bay rời khỏi nhân vật.

4. Mặt và biểu cảm

- Đi: Hai mắt oval đen nâu mở to, highlight kem; miệng ω nhỏ, hai má vàng nhạt.
- Đánh: Mắt nhíu thành nét quyết tâm đáng yêu, phồng má, miệng chữ o trước khi lao tới.
- Trúng đòn: Mắt nhắm ><, miệng méo, cả thân bị ép xuống và mầm cỏ ngả một bên.

5. Bảng màu

#B9DE8B

#88BE71

#5F9963

#E4F1B4

#FFF3D5

#624B3D

Xanh lá non là nền chính; vàng xanh nhạt dành cho phần thạch sáng, nâu đậm cho viền.

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thân phồng nhẹ, hai mắt chớp lệch nhịp | Thấy mầm cỏ và khối thạch sau, không có mặt | Đỉnh lắc nhẹ, mắt trước nổi bật |
| Walk | 4 nhịp: bẹp → bật lên → tròn → bẹp | Đáy giãn rồi thu, mầm cỏ rung | Thân nghiêng theo hướng di chuyển, đỉnh nảy trễ |
| Attack | Nén thân → phồng má → bật thẳng tới trước → rơi bẹp | Nén → bật ngược hướng mặt → hạ xuống | Nén về sau → lao ngang sang phải → thu hồi |

7. Vai trò chiến đấu: Cận chiến yếu, di chuyển chậm, đòn va chạm ngắn; không có gai, răng hoặc hiệu ứng độc.

8. Liên kết Meadow: Chồi cỏ hai lá, xanh lá non, highlight vàng kem liên kết với môi trường Willowbrook. Mặt tròn ngây thơ mở đầu cho ngôn ngữ quái Meadow.

Chốt nhận diện: Một giọt thạch lùn nghiêng đầu, có bọt khí bên trong và mầm cỏ hai lá.

#### 02. Horn Rabbit — Lv3

Tên gợi ý: Sprout Horn Rabbit · Fast / Fragile

1. Tính cách: Hiếu động, tinh nghịch, rất tự tin vào tốc độ nhưng dễ hoảng hốt khi bị đánh trúng.

2. Silhouette / hình khối

- Thân nhỏ hình quả lê đứng, bụng tròn, hai chân sau lớn và bàn chân mềm.
- Đầu chiếm khoảng 43% chiều cao phần thân không tính tai.
- Một tai dài dựng gần thẳng, một tai hơi cong ra ngoài, tạo silhouette bất đối xứng.
- Một chiếc sừng ngắn hình nụ mầm nằm chính giữa trán, hướng hơi về trước.
- Đuôi tròn như cục bông; hai chân trước rất nhỏ.
- Cao tổng 125 px, rộng 75–90 px.

Sừng là một chi tiết nhỏ nhưng rõ. Không biến Rabbit thành kỳ lân hoặc thú có ba sừng.

3. Đặc trưng chữ ký

Sừng cong nhẹ màu mật ong, có hai vạch màu sáng như vòng sinh trưởng; hai tai luôn lệch góc với nhau. Khi nhảy, tai cong nảy chậm hơn đầu.

4. Mặt và biểu cảm

- Đi: Mắt tròn long lanh, đồng tử nâu hạt dẻ, mũi hồng nhỏ, miệng cười với một răng cửa.
- Đánh: Một bên lông mày hạ thấp, nhe hai răng cửa, má phồng lên trước cú lao sừng.
- Trúng đòn: Hai tai cụp lệch, mắt mở tròn ngạc nhiên, miệng chữ o.

5. Bảng màu

#EACB98

#FFF0D5

#C78E69

#F1B2A0

#D4B25D

#684A3B

Lông kem mật ong, bụng trắng ngà, tai hồng đào, sừng vàng nâu.

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đứng trên chân sau, mũi khịt khịt, một tai giật nhẹ | Đuôi bông nổi bật, tai lệch hai bên | Nhấc một chân trước, tai quay như đang nghe ngóng |
| Walk | Nhảy hai chân: nén → bật → bay ngắn → tiếp đất | Chân sau đẩy lên, đuôi rung | Nhảy thành cung ngắn sang phải, tai quật ngược |
| Attack | Hạ đầu → nhảy lao sừng → chạm mục tiêu → bật lùi | Quay phần đầu theo hướng lên, sừng vẫn là điểm dẫn | Chồm thấp → lao sừng theo phương ngang → bật về vị trí |

7. Vai trò chiến đấu: Di chuyển nhanh, HP thấp, tấn công bằng cú húc sừng ngắn. Đòn đánh có anticipation ngắn hơn Boar, thể hiện sự nhanh nhẹn.

8. Liên kết Meadow: Màu kem mật ong chung với đồng cỏ; sừng có hình nụ mầm, không phải sừng kim loại. Vệt hồng ở tai thay cho đốm lá trên Slime.

Chốt nhận diện: Thỏ thân quả lê, một tai thẳng một tai cong, sừng nụ mầm và chân sau cực khỏe.

#### 03. Thorn Boar — Lv5

Tên gợi ý: Bristle Boar · Heavy Charger

1. Tính cách: Cứng đầu, tham ăn, dễ nổi cáu khi bị làm phiền, nhưng lúc bình thường trông như một cục bánh đang đi.

2. Silhouette / hình khối

- Thân hình viên sỏi oval nằm ngang, ngực và vai lớn, lưng cong cao.
- Đầu gần liền với thân, mõm hình đĩa bầu dục nhô về trước.
- Bốn chân ngắn, to như bốn cọc tròn, móng bo mềm.
- Mảng lông gáy dựng thành 3–4 chóp lớn hình lá răng cưa; không dùng nhiều gai nhọn rải khắp thân.
- Hai nanh trắng ngà rất ngắn, cong hướng lên.
- Cao khoảng 160 px, bề ngang 170–185 px.

3. Đặc trưng chữ ký

Mõm lớn có hai lỗ mũi oval nổi bật và một mảng lông gáy hình ba chiếc lá xếp chồng. Đây là hai điểm người chơi phải nhận ra ngay khi thu nhỏ.

4. Mặt và biểu cảm

- Đi: Mắt bé nhưng sáng, lông mày hơi cau, miệng mím như đang càu nhàu.
- Đánh: Nhíu mắt mạnh, phồng lỗ mũi, nhe hai nanh, mở miệng hừ.
- Trúng đòn: Mắt nhắm chặt, tai ép xuống, mõm bị nén ngược vào mặt; biểu cảm tức tối hơn là đau đớn.

5. Bảng màu

#B97853

#D99C70

#E8BE96

#8DA66A

#FFF0D3

#5A4236

Thân nâu caramel, mõm hồng đất, lông gáy xanh olive, nanh kem.

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Cào một chân trước, hít đất bằng mõm, tai vểnh | Lưng tròn và lông gáy nổi bật, đuôi cuộn nhỏ | Mõm nhấp nhô, chân trước gõ đất |
| Walk | Bước nặng luân phiên, thân lắc trái-phải nhẹ | Hông lắc, đuôi cuộn rung | Vai nhún theo bước, mõm dẫn hướng |
| Attack | Cào đất → hạ đầu → húc thẳng → phanh chân | Hạ vai → phóng về hướng lưng → dừng nặng | Vai lùi → lao ngang bằng đầu → trượt ngắn khi dừng |

7. Vai trò chiến đấu: Quái nặng, chạy chậm hơn Rabbit nhưng cú húc có lực, có thể biểu đạt bằng động tác phanh và nén vai. Không biến thành quái tấn công liên tục như sói.

8. Liên kết Meadow: Lông gáy olive có hình lá, mõm hồng đào và highlight kem. Cả bộ ba Meadow cùng pastel sáng nhưng Boar là màu ấm đậm nhất để tạo tiến trình Lv1→5.

Chốt nhận diện: Cục lợn rừng caramel thân ngang cực rộng, mõm lớn, bờm ba chóp lá olive.

### III. FOREST — BRAMBLEWOOD

Quái Forest có hình khối nhiều góc và nhiều chuyển động hơn Meadow nhưng giữ các cạnh bo mềm.

Mỗi sinh vật sử dụng một đặc trưng riêng: liềm lá, lông bờm, thân gốc cây, cánh hạt, bờm thủ lĩnh. Đây là những chi tiết không được hoán đổi giữa các loài.

#### 04. Meadow Mantis — Lv6

Tên gợi ý: Leafblade Mantis · Fast Striker

1. Tính cách: Tập trung, kiêu hãnh, thích làm dáng như kiếm sĩ; trước khi ra đòn thường nghiêng đầu quan sát đối thủ.

2. Silhouette / hình khối

- Cơ thể cao gầy hình chữ Y hẹp.
- Đầu hình tam giác bo tròn, hai mắt lớn nằm hai bên.
- Ngực thon, bụng hình hạt lúa kéo dài.
- Hai càng trước lớn cong như lưỡi liềm lá cây, là điểm nhận diện chính.
- Bốn chân sau mảnh nhưng khớp rõ, bàn chân nhỏ.
- Hai râu ngắn, cong ra ngoài.
- Cao 170 px, rộng khoảng 100–120 px khi khép càng.

3. Đặc trưng chữ ký

Hai càng hình lá liềm có mép sáng màu vàng lục; trên đầu là một đường màu kem chạy giữa trán giống gân lá.

Không gắn kiếm kim loại. Bản thân càng chính là vũ khí.

4. Mặt và biểu cảm

- Đi: Mắt oval dài hơi xếch, đồng tử đen nâu, miệng nhỏ tự tin.
- Đánh: Mắt hẹp lại, đầu cúi, hai càng mở ra tạo hình chữ V.
- Trúng đòn: Mắt nhắm lệch, râu rũ, càng co sát ngực, cơ thể gập nhẹ.

5. Bảng màu

#A5C875

#6F9F59

#D9D88B

#E8E3BD

#927956

#4E4335

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đưa một càng lên như chào đấu, nghiêng đầu | Thấy bụng dài và cánh lưng gấp gọn | Chuyển trọng tâm giữa chân, hai càng ở tư thế thủ |
| Walk | Bốn chân bước nhanh luân phiên, ngực giữ cao | Chân chuyển nhịp đối xứng, bụng lắc rất ít | Chân trước-sau đổi vị trí nhanh, đầu hướng mục tiêu |
| Attack | Mở càng → chém chéo hai lượt trong một động tác → thu về thủ | Càng mở sang hai phía, chém về hướng lên | Càng gần quét xuống trước, càng xa theo sau tạo nhịp kép |

7. Vai trò chiến đấu: Nhanh, sát thương cận chiến theo đòn cắt. Cảm giác sắc nhờ chuyển động càng, không nhờ gai và hiệu ứng phát sáng.

8. Liên kết Forest: Thân như lá non rừng, càng mang hình gân lá, xanh olive đậm hơn Meadow. Độ lanh lợi dự báo những sinh vật nguy hiểm hơn trong Bramblewood.

Chốt nhận diện: Bọ ngựa kiếm sĩ thân chữ Y, mắt sắc, hai càng lá liềm lớn.

#### 05. Forest Wolf — Lv7

Tên gợi ý: Mossmane Wolf · Fast Pack Hunter

1. Tính cách: Tò mò, tinh quái, thích trêu đối thủ; khi đi bầy rất tự tin nhưng một mình hơi nhút nhát.

2. Silhouette / hình khối

- Sói bốn chân nhỏ gọn, thân dài vừa phải, ngực lớn hơn bụng.
- Đầu tương đối to, mõm ngắn, tai tam giác rộng nhưng bo đầu.
- Bờm lông quanh cổ tạo ba khối cong như lá; không có lá cây thật mọc lên.
- Bốn chân thon, bàn chân lớn mềm.
- Đuôi dài có chóp cong như dấu hỏi.
- Cao khoảng 140 px, rộng 145–160 px tùy tư thế.

3. Đặc trưng chữ ký

Bờm hình ba búi lá olive quanh cổ và chiếc đuôi dấu hỏi có chóp màu kem. Không dùng sừng, vương miện hoặc giáp để giữ dáng sói rừng tự nhiên.

4. Mặt và biểu cảm

- Đi: Mắt vàng mật ong tròn hơi xếch, nụ cười lệch, lộ một nanh nhỏ.
- Đánh: Lông mày hạ thấp, nhe hai nanh, mõm nhăn nhẹ; vẫn giữ mắt rõ.
- Trúng đòn: Tai cụp, mắt nhắm một bên, miệng méo, thân lùi xuống.

5. Bảng màu

#A69A79

#D5C29B

#6F8C60

#F1DEB8

#D9B46F

#594638

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thở nhẹ, tai giật, nghiêng đầu một bên | Đuôi dấu hỏi lắc chậm, bờm nổi khối | Đánh hơi mặt đất rồi ngẩng lên |
| Walk | Chạy bước ngắn kiểu trot, hai cặp chân luân phiên | Hông nhún nhẹ, đuôi cân bằng | Thân duỗi và thu, đầu giữ tương đối ổn định |
| Attack | Hạ vai → chồm tới → cắn nhanh → lùi thấp | Chồm về phía lưng, đuôi vung cân bằng | Phóng ngang tới trước, mõm mở rồi khép, chân trước đỡ lực |

7. Vai trò chiến đấu: Nhanh và đi bầy. Animation phải ngắn, linh hoạt, dễ đồng bộ nhiều con cùng lúc. Có thể tạo cảm giác vây mục tiêu bằng di chuyển và hướng nhìn, không cần thiết kế từng con như mini-boss.

8. Liên kết Forest: Lông xám nâu rừng, bờm olive, mắt mật ong. Bờm tạo sự liên hệ nhẹ với lá rừng nhưng vẫn là lông tự nhiên.

Chốt nhận diện: Sói rừng nhỏ mắt tinh quái, bờm ba khối lá mềm, đuôi dấu hỏi.

#### 06. Bramble Treant — Lv8

Tên gợi ý: Bramble Stump · Slow Tank

1. Tính cách: Ông cây già khó tính, chậm chạp, hay buồn ngủ; nổi giận khi có ai giẫm lên rễ của mình.

2. Silhouette / hình khối

- Thân là một gốc cây bầu lớn hình thùng, vai rất rộng.
- Đầu không tách thành quả cầu riêng mà nằm trong phần thân trên, tạo cảm giác cây sống.
- Hai tay to là cành gỗ cong, bàn tay gồm ba ngón rễ.
- Hai chân là hai rễ lớn tách khỏi gốc, bước đi nặng.
- Đỉnh đầu có tán lá bất đối xứng gồm 3–4 cụm lớn.
- Thân rộng khoảng 155–170 px ở thiết kế gốc, cao 170 px trước scale ×1.2.
- Không dùng cơ thể người cơ bắp bằng gỗ.

3. Đặc trưng chữ ký

Một mắt gỗ hình xoắn ốc nằm giữa bụng; phía trên là hai lông mày bằng rêu rủ xuống, tạo gương mặt ông già khó tính.

Mắt gỗ bụng là hoa văn, không phải con mắt thứ ba.

4. Mặt và biểu cảm

- Đi: Mắt nửa nhắm, mí nặng, miệng gỗ cong xuống; giống đang buồn ngủ.
- Đánh: Hai mắt mở lớn, lông mày rêu dựng lên, miệng gỗ mở như đang quát.
- Trúng đòn: Mắt nhắm chặt, tán lá rung, miệng bị ép thành đường ngang, thân nghiêng nhưng không mất thăng bằng.

5. Bảng màu

#A07C56

#75583F

#BFA071

#79975A

#AFC37D

#E6D29B

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thân hô hấp rất nhẹ, mí mắt nặng, một chiếc lá rung | Tán lá che phần vai, thấy các rễ gỗ phía sau | Tay cành rũ xuống, ngón rễ co nhẹ |
| Walk | Nhấc từng chân rễ → đặt nặng → thân nghiêng → ổn định | Hai rễ thay nhau bước, tán lá rung trễ | Bước ngắn, vai và tay gỗ lắc ngược nhịp |
| Attack | Nâng tay cành → đập xuống trước mặt → giữ một nhịp → thu tay | Nâng hai vai, đập về phía trên, tán lá rung mạnh | Tay gần nâng cao → quét/đập về phải → kéo về chậm |

7. Vai trò chiến đấu: Tank chậm, tầm đánh rộng hơn quái nhỏ nhờ tay cành lớn. Đòn có chuẩn bị rõ, không đánh nhanh và không nhảy.

8. Liên kết Forest: Cấu trúc gỗ, tán olive, rêu xanh và vân xoắn thân cây liên kết trực tiếp với rễ Rootburrow. Không dùng tinh thể, giáp kim loại hay phù văn sáng.

Chốt nhận diện: Gốc cây ông già thân thùng cực rộng, tay cành ba ngón, mắt gỗ xoắn ở bụng.

#### 07. Thorn Sprite — Lv9

Tên gợi ý: Briar Pixie · Fast Flying Monster

1. Tính cách: Tinh linh nhỏ láu cá, thích trêu chọc và bay vòng quanh mục tiêu; khi bị đánh thì giận dỗi.

2. Silhouette / hình khối

- Thân chính như một hạt giống thuôn hình giọt nước, phần đầu lớn hơn thân.
- Hai cánh lớn hai bên dạng cánh lá mỏng, chia thùy, tạo silhouette gần hình cánh bướm nhưng không giống côn trùng thật.
- Hai tay nhỏ, chân cực ngắn, không dùng dáng humanoid cổ điển.
- Đỉnh đầu có hai râu gai cong mềm như chồi non.
- Thân nghiêng khoảng 10–15° khi lơ lửng.
- Cao tổng khoảng 130 px, rộng 125–145 px tính cả cánh.

3. Đặc trưng chữ ký

Đôi cánh lá hai thùy có đường gân vàng nhạt và hai râu gai cong mềm. Ở giữa ngực có một hạt sáng mật ong nhỏ, không phải lõi neon.

4. Mặt và biểu cảm

- Đi / bay: Hai mắt to hơi xếch, miệng cười lệch, hai má phồng như đang nghịch ngợm.
- Đánh: Mắt nheo tinh quái, miệng cười lộ một răng nhỏ, hai tay ép vào hạt sáng trước ngực.
- Trúng đòn: Mắt nhắm, má phồng giận, cánh cụp trong một nhịp, thân xoay lệch.

5. Bảng màu

#B5C878

#749F60

#DDE2A1

#E8C48D

#F8E9C7

#624D41

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Lơ lửng lên xuống 3–5 px, cánh vỗ nhẹ không đều | Thấy rõ hai cánh lá và lưng hạt giống, không thấy mắt | Nghiêng thân, cánh trước/sau lệch nhịp |
| Walk / Fly | Bay tới bằng các nhịp vỗ nhanh, thân nảy mềm | Cánh mở rộng khi lên, gập khi xuống | Thân lao nhẹ sang phải, cánh sau hiện rõ hơn |
| Attack | Khép cánh → gom năng lượng nhỏ ở ngực → bật người phóng đòn → mở cánh | Cánh khép và mở theo nhịp, tấn công về hướng lên | Nghiêng sang phải → đưa hai tay ra → phóng đòn ngắn → thu về |

7. Vai trò chiến đấu: Bay nhanh, HP thấp, né linh hoạt. Đòn đánh là xung năng lượng/gai ma thuật nhỏ, khác hẳn đòn vung tay của Treant. VFX tách khỏi sprite.

8. Liên kết Forest: Hình hạt giống, cánh lá, gân vàng và sắc xanh olive. Tuy nhiên không tạo thành một cây tí hon có cánh; đây là tinh linh thực vật nhẹ và linh hoạt.

Chốt nhận diện: Hạt giống bay có hai cánh lá lớn, râu gai cong và nụ cười tinh quái.

### IV. FOREST BOSSES — CHUẨN MIRE HYDRA QUEEN

Hai boss cùng map nhưng khác tính chất hoàn toàn:

- Thornfang Alpha: Boss thú săn mồi, tốc độ, thống trị bầy đàn.
- Thornheart Treant: Boss cổ thụ, sức nặng, tầm với rộng và quyền lực thiên nhiên.

Mỗi boss phải có độ hoàn thiện về mặt, silhouette, vật liệu và chi tiết tương đương Mire Hydra Queen, nhưng ít chi tiết vụn hơn để giữ nét RO Soft Fantasy.

#### 08. Thornfang Alpha — Lv10

Boss map Bramblewood · Pack Leader · Scale ×1.65

1. Tính cách: Thủ lĩnh kiêu hãnh, bình tĩnh và tinh ranh; chỉ gầm khi cần ra lệnh, luôn nhìn đối thủ như đang đánh giá sức mạnh của họ.

2. Silhouette / hình khối

Thiết kế mới hoàn toàn theo hình mũi tên tiến về phía trước, không lấy Forest Wolf phóng to.

- Thân sói bốn chân lớn, ngực sâu, vai cao và khỏe.
- Đầu rộng, mõm dài hơn Wolf thường nhưng vẫn dễ thương.
- Hai tai lớn dựng ngược ra ngoài, tạo góc V.
- Bờm cực dày xòe ra sau vai thành 5–6 khối cong lớn, tựa tán cây trong gió.
- Lưng dốc nhẹ về hông, tạo cảm giác sẵn sàng vồ.
- Chân trước to, bàn chân rộng có ba ngón rõ; chân sau săn chắc.
- Đuôi dài hình ngọn lửa cong, có 2–3 chóp lông lớn.
- Chiều cao thiết kế 166 px, hiển thị khoảng 274 px khi scale ×1.65.

Silhouette chủ đạo: Đầu kiêu hãnh + bờm hình vương miện lông + chân trước lớn. Không dùng vương miện thật hay áo giáp.

3. Đặc trưng chữ ký

Một chỏm bờm trán hình ba chiếc lá dựng thành mào thủ lĩnh, màu vàng rêu đậm hơn lông xung quanh. Hai vệt lông kem kéo từ khóe mắt xuống má như dấu hiệu của Alpha.

Không dùng sừng. Không tạo đầu sói pha rồng.

4. Mặt và biểu cảm

- Đi: Mắt hổ phách hơi hẹp, ánh nhìn tự tin, miệng cười rất nhẹ, một nanh trắng lộ ra.
- Đánh: Lông mày ép thấp, mắt sắc, miệng mở rộng lộ 4 răng nanh lớn nhưng bo mềm; tai dựng thẳng.
- Trúng đòn: Mắt nhíu, bờm ép về sau, miệng nghiến, chân trước chống mạnh xuống đất.
- Khi gầm: Cổ vươn, mõm hướng lên nhưng hai mắt và đặc trưng mặt không bị che hoàn toàn ở hướng D.

5. Bảng màu

#756D58

#A59B79

#D8C39A

#6D8657

#E1B96A

#493D35

Lông xám nâu olive; bờm vàng rêu; mắt hổ phách; ngực và mõm sáng kem.

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đứng cao, ngực nở, đuôi quét chậm, mắt theo dõi người chơi | Bờm phủ vai, sống lưng hiện rõ, đuôi cong sang một bên | Một chân trước bước lên nửa bước, đầu ngẩng và quan sát |
| Walk | Sải chân rộng, đầu ổn định, bờm và đuôi trễ nhịp | Vai luân phiên đẩy lên, chân sau thể hiện lực | Thân duỗi dài khi chạy, chân trước vươn, chân sau gom |
| Attack | Hạ thấp vai → gầm ngắn → bật vồ/cắn → đáp bằng chân trước | Thân lao về phía trên, bờm hất về sau | Chồm mạnh sang phải, hàm mở ở cực điểm, chân trước quét xuống |

7. Vai trò chiến đấu

Boss thủ lĩnh bầy sói, nhanh hơn Treant, có các đòn cắn, vồ hoặc gầm chỉ huy. Động tác cần rõ nhịp chuẩn bị dù boss di chuyển nhanh. Không thiết kế như một con sói khổng lồ chỉ cắn liên tục.

8. Liên kết Forest

- Cùng họ sinh vật với Forest Wolf qua cấu trúc sói bốn chân, màu lông ấm, mắt mật ong và bờm olive.
- Khác biệt rõ ở cơ thể khỏe, đầu rộng, bờm vương miện, hai vệt má và chuyển động lãnh đạo.
- Hình bờm liên kết với tán lá Bramblewood mà không cần gắn cây, lá hoặc rễ thật lên người.

9. Quy tắc boss — uy nghi nhưng dễ thương

Thornfang Alpha phải tạo cảm giác một vị vua sói trẻ của rừng, không phải sói ma quỷ.

- Điểm nhìn chính là mắt và mào bờm.
- Bờm lớn nhưng không che mặt.
- Mảng ngực kem giúp boss sáng và đọc rõ trên nền rừng.
- Hàm và móng có thể nguy hiểm nhưng bo mềm, không kéo dài như quái kinh dị.
- Tỉ lệ đầu đủ lớn để biểu cảm vẫn đọc được trong combat đông người.
- Không thêm khói đen, lửa tím, xương hoặc mắt đỏ.

Chốt nhận diện: Sói thủ lĩnh ngực rộng, bờm lá dạng vương miện, hai vệt má kem và ánh mắt hổ phách kiêu hãnh.

#### 09. Thornheart Treant — Lv10

Final Dungeon Boss · Rootburrow · Scale ×2.0

1. Tính cách: Cổ thụ canh giữ khu rừng, trầm tĩnh và uy nghi. Không ác độc; nổi giận vì cho rằng người chơi đang xâm phạm nơi rễ cây trú ngụ.

2. Silhouette / hình khối

Thiết kế theo hình cây cổ thụ hình chuông với hai tay rễ vươn rộng, khác Bramble Treant thân thùng.

- Thân chính hình thân cây lớn thon ở cổ, bè rộng ở gốc.
- Đầu hình mặt nạ gỗ nằm trong thân trên, không tách thành đầu tròn.
- Hai vai là hai chạc cây cao, mở sang hai bên.
- Hai cánh tay dài gồm cành gỗ xoắn; bàn tay ba ngón rễ lớn, có khả năng vươn xa.
- Phần ngực có khe gỗ lớn bao quanh một lõi hình trái tim bằng nhựa cây hổ phách.
- Hai chân là hệ rễ lớn, chia thành các ngón bám đất; bước chậm, nặng.
- Tán lá mọc lệch phía trên, thành 4–5 cụm lớn hình mây, không phủ kín toàn bộ đầu.
- Cao thiết kế khoảng 180 px, hiển thị khoảng 360 px khi scale ×2.0.
- Sải tay khi mở có thể lớn hơn chiều cao boss khoảng 1.4–1.6 lần, vì vậy cần sheet boss kích thước lớn hơn.

Silhouette chủ đạo: Thân cây chuông + vai chạc cao + hai tay rễ dài + lõi tim giữa ngực.

3. Đặc trưng chữ ký

Trái tim hổ phách nằm trong khoang gỗ ở ngực, được bao quanh bằng ba vòng rễ cong tự nhiên.

Đây không phải tinh thể pha lê hoặc nguồn sáng neon. Lõi trông như nhựa cây hóa thạch đang phát sáng dịu.

Trên đầu có hai nhánh cong như sừng hươu làm bằng gỗ già, nhưng ngắn, dày và phân nhánh ít. Chúng tạo cảm giác cổ thụ thay vì quái vật có sừng.

4. Mặt và biểu cảm

- Đi: Hai mắt lớn nửa khép màu vàng ngà, đường lông mày gỗ cong trầm tĩnh, miệng gỗ ngang hơi buồn.
- Đánh: Mắt mở sáng hơn, lông mày gỗ hạ thấp, miệng mở thành khe lớn như đang gầm; lõi tim sáng hơn nhẹ.
- Trúng đòn: Mắt nhắm, thân nghiêng, lá rơi một vài chiếc, lõi tim chớp sáng ngắn.
- Khi nổi giận: Mắt sắc hơn và các rễ ngực siết quanh lõi tim, nhưng không đổi thành mắt đỏ hoặc mặt quỷ.

5. Bảng màu

#A7865C

#6D513D

#C9AD78

#77905A

#A8BB76

#F2D18C

Gỗ nâu vàng ấm; bóng nâu sâu; lá olive; lõi tim hổ phách. Viền ngoài dùng nâu đậm của Forest để giữ thống nhất.

6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thân đứng vững, lõi tim đập ánh sáng rất nhẹ, tay rễ chuyển động chậm như đang thở | Tán cây và rễ sau rõ, cành vai mở; lõi tim và mặt không thấy | Thân nghiêng nhẹ theo nhịp thở, một bàn tay rễ co duỗi |
| Walk | Nhấc một chân rễ lớn → đặt xuống → thân lắc trễ → tay rễ cân bằng | Bước nặng luân phiên, các rễ sau cuộn và mở | Dồn trọng lượng → nhấc rễ trước → tiến một bước lớn → ổn định |
| Attack | Hai tay mở rộng → nâng cao → quật/đập xuống trước mặt → giữ thế → thu rễ | Tay nâng và quật về phía trên, tán rung sau cú đánh | Tay gần vươn xa sang phải → quét rộng → co lại; tay xa giữ thăng bằng |

7. Vai trò chiến đấu

Boss cuối Rootburrow là tank khổng lồ, chậm nhưng tầm với rộng. Các đòn cần có thời gian chuẩn bị đủ rõ để người chơi nhận ra hướng đánh và né.

Cấu trúc tay cho phép:

- Quét ngang bằng cành rễ.
- Đập đất bằng bàn tay gỗ.
- Vươn rễ từ khoảng cách trung bình.
- Đòn nặng từ cả hai tay.

Các hiệu ứng rễ mọc, bụi đất và lá bay là VFX riêng, không gắn cố định vào sprite attack.

8. Liên kết Forest / Rootburrow

- Có gỗ, rêu, tán olive và vân xoắn giống Bramble Treant, nhưng là một thực thể cổ đại khác biệt hoàn toàn.
- Bramble Treant là gốc cây thấp, thân thùng, hai tay ngắn; Thornheart là cổ thụ cao, vai chạc, tay rễ dài và lõi tim.
- Màu hổ phách tạo liên hệ với ánh sáng cổ xưa trong Rootburrow.
- Không dùng trang bị người, áo giáp hoặc vương miện kim loại.

9. Quy tắc boss — uy nghi nhưng dễ thương

Thornheart Treant phải giống thần hộ mệnh của khu rừng trong truyện cổ tích.

- Gương mặt già, có cảm xúc, dễ đồng cảm.
- Thân to, tay dài, có cảm giác sức mạnh nhưng không đáng sợ kiểu kinh dị.
- Trái tim hổ phách là focal point phụ; gương mặt vẫn là điểm nhìn chính.
- Tán lá lớn nhưng không che mặt hoặc tay.
- Cành và rễ có đầu bo, đường cong hữu cơ, không chằng chịt.
- Bóng sâu làm nổi khối chứ không làm boss tối sầm.
- Chất lượng tương đương Mire Hydra Queen ở phân tầng hình khối, độ trau chuốt và khả năng đọc giữa đám đông, không bắt chước số lượng chi tiết hay cấu trúc boss rắn.

Chốt nhận diện: Cổ thụ hình chuông với hai tay rễ vươn rộng, mặt gỗ trầm tĩnh và trái tim nhựa cây hổ phách giữa ngực.

### V. KIỂM TRA TRÙNG LẶP CỦA 9 THIẾT KẾ

| Quái | Silhouette chủ đạo | Nhận diện từ xa | Cá tính |
|---|---|---|---|
| Meadow Slime | Giọt nước bẹp nghiêng | Mầm cỏ, bọt khí | Ngây thơ |
| Horn Rabbit | Quả lê và tai lệch | Sừng nụ mầm | Hiếu động |
| Thorn Boar | Oval ngang nặng | Mõm lớn, bờm ba chóp | Cứng đầu |
| Meadow Mantis | Chữ Y cao gầy | Hai càng liềm lá | Kiếm sĩ kiêu hãnh |
| Forest Wolf | Sói nhỏ, đuôi cong | Bờm olive, đuôi dấu hỏi | Tinh quái |
| Bramble Treant | Gốc cây hình thùng | Mắt gỗ xoắn, tay ngắn | Ông già cáu kỉnh |
| Thorn Sprite | Hạt giống có cánh | Cánh lá hai thùy | Láu cá |
| Thornfang Alpha | Sói lớn, bờm hình vương miện | Mào bờm, vệt má | Thủ lĩnh |
| Thornheart Treant | Cây chuông, tay rễ dài | Tim hổ phách, vai chạc | Hộ vệ cổ xưa |

#### VI. QUY TẮC BÀN GIAO CHO LƯỢT VẼ

Để giữ được các thiết kế này khi chuyển thành sprite thực tế:

- Chốt một hình đứng ba hướng trước: D, U, R, đủ màu và đúng tỷ lệ.
- Không tái sử dụng thân cũ: Mỗi quái cần silhouette, cấu trúc cơ thể, khuôn mặt và chi tiết chữ ký được dựng riêng.
- Walk 4 frame: Đúng nhịp di chuyển của loài; không ép sói, bọ ngựa và Treant dùng cùng animation.
- Attack 4 frame: Có anticipation, ra đòn, cực điểm, recovery. Với boss, bốn frame là mẫu tối thiểu; nếu động tác phức tạp có thể cần nhiều frame hơn.
- Hit reaction: Thiết kế biểu cảm và biến dạng riêng; nếu chưa làm sheet Hit thì vẫn phải giữ quy tắc mặt đã mô tả khi bổ sung sau.
- Boss dùng canvas riêng: Scale Unity ×1.65 và ×2.0 không có nghĩa là được phóng bitmap từ sheet nhỏ. Cần thiết kế và render đủ độ phân giải, tránh vỡ nét.

##### Kết luận lượt 1

9/9 quái có silhouette và tính cách riêng. Bộ ba Meadow tạo cảm giác mở đầu thân thiện, Forest tăng dần sự lanh lợi và sức mạnh, Thornfang Alpha đại diện cho săn mồi theo bầy, Thornheart Treant là thủ hộ cổ thụ.

Hai boss có đẳng cấp tạo hình cao hơn quái thường nhưng vẫn giữ đúng tinh thần RO Soft Fantasy: tròn mềm, có hồn, sáng ấm và dễ thương ngay cả khi nguy hiểm.

ART DIRECTION: CHỐT 9 THIẾT KẾ LƯỢT 1 — sẵn sàng triển khai sprite đứng, đi và đánh.

## 3. Snow (Frost Hollow): 8 quái

### MONSTER REDESIGN — LƯỢT 2/5

8 quái SNOW · Frost Hollow Lv11–15 · RO Soft Fantasy

Giữ nguyên toàn bộ quy tắc A và B, cùng tiêu chuẩn thiết kế của lượt 1. Lượt này xây dựng 8 sinh vật tuyết với 8 silhouette khác biệt, không tái sử dụng khối cơ thể, gương mặt hoặc animation từ Meadow và Forest.

Định hướng: Frost Hollow là vùng tuyết kỳ ảo, không phải vùng đất chết lạnh lẽo. Quái phải mềm mại, sáng, trong trẻo, đáng yêu nhưng có cảm giác mạnh hơn các sinh vật rừng.

Ba yêu cầu đặc biệt:

- Frost Slime: Không phải Meadow Slime đổi xanh. Chuyển sang cấu trúc bánh tuyết pha lê thấp, rộng, có các thùy băng mềm.
- Snow Fox: Không phải Forest Wolf màu trắng. Chuyển sang silhouette cáo nhỏ, tai lớn, thân thon, đuôi quạt tuyết khổng lồ.
- Frostfang King: Không phải Thornfang Alpha đổi màu hoặc phóng lớn Snow Fox. Đây là linh thú sói băng hoàng gia, với hình khối, tư thế, bờm và vương miện độc quyền.

Mire Hydra Queen tiếp tục là thước đo chất lượng boss.

### I. Quy chuẩn kích thước

| Quái | Lv | Cao gốc | Scale | Cao hiển thị | Vai trò |
|---|---|---|---|---|---|
| Frost Slime | 11 | 104 px | ×1.0 | 104 px | Chậm, cận chiến |
| Snow Fox | 12 | 120 px | ×1.0 | 120 px | Nhanh, máu thấp |
| Ice Imp | 12 | 135 px | ×1.0 | 135 px | Trung bình, tầm ngắn |
| Frost Bear | 13 | 158 px | ×1.15 | 182 px | Tank chậm |
| Ice Wraith | 13 | 165 px | ×1.0 | 165 px | Bay, nhanh, máu thấp |
| Frostbite Yeti | 14 | 162 px | ×1.2 | 194 px | Nặng, đòn vung chậm |
| Crystal Golem | 15 | 169 px | ×1.25 | 211 px | Tank chậm nhất, giáp dày |
| Frostfang King | 15 | 155 px | ×1.65 | 256 px | Boss map |

Chiều cao tính theo phần cơ thể nhìn thấy, bao gồm tai, sừng, tinh thể cố định nhưng không bao gồm bóng đất hoặc VFX tách rời. Với quái bay, chiều cao 165 px là chiều cao silhouette, không tính khoảng cách lơ lửng so với mặt đất.

Các số này là mục tiêu kích thước trong game; khi sản xuất cần bố trí art và padding phù hợp, không cắt sprite vào mép cell.

Ký hiệu hướng: D = xuống, U = lên, R = phải. Hướng trái lật R. Mỗi hướng có 4 frame Walk và 4 frame Attack; Idle có thể dùng sprite riêng hoặc animation ngắn.

### II. SNOW — FROST HOLLOW

#### 01. Frost Slime — Lv11

Tên gợi ý: Snowpuff Slime · Slow Melee

##### 1. Tính cách

Ngốc nghếch, hiền lành, thích lăn qua tuyết và vô tình làm người khác lạnh cóng khi chạm vào.

##### 2. Silhouette / hình khối

Thiết kế hình bánh tuyết ba thùy, khác hoàn toàn giọt nước nghiêng của Meadow Slime.

- Khối thân chính thấp, rộng, gần hình bánh mochi dẹt.
- Mép dưới chia thành ba thùy tuyết tròn: một thùy lớn phía trước, hai thùy nhỏ hai bên.
- Phần đỉnh lõm nhẹ như một lớp kem tuyết bị ấn xuống.
- Hai má thạch phồng ra thành khối tròn, không có tay chân.
- Thân có lớp ngoài trắng xanh đục và lớp lõi xanh băng hơi trong, nhưng không nhìn xuyên rõ như thủy tinh.
- Cao 104 px, rộng khoảng 120–132 px.

Silhouette phải trông giống một cục tuyết mềm đang bò, không giống Poring tròn hoặc Meadow Slime có mầm cỏ.

##### 3. Đặc trưng chữ ký

Ba thùy tuyết mềm quanh chân thân và một tinh thể băng nhỏ dạng giọt dẹt nằm chìm trong đỉnh đầu.

Tinh thể chỉ lộ 1/3 phía trên thân, không mọc thành ba gai cao. Bên trong thân có một mảng lõi màu lam nhạt hình bông tuyết bốn cánh cách điệu.

##### 4. Mặt và biểu cảm

- Đi: Hai mắt tròn nhỏ màu xanh đậm, đặt hơi xa nhau; miệng bé hình o, biểu cảm ngơ ngác. Hai má trắng phồng nhẹ.
- Đánh: Mắt nheo thành hai đường cong, miệng phồng như đang thổi hơi lạnh; thùy phía trước vươn ra.
- Trúng đòn: Mắt nhắm ><, miệng bẹt, thân nén thành bánh tuyết rất thấp; đỉnh tinh thể nghiêng lệch.

Không thêm răng hoặc biểu cảm hung dữ vì đây là quái yếu nhất Snow.

##### 5. Bảng màu

#E9F7F7

#BDE7F1

#85C8DE

#5E9DBB

#FFF9EB

#526B83

Màu chủ đạo trắng tuyết và xanh lam sữa. Viền nâu xám lạnh #526B83 kết hợp các đoạn viền nâu đậm chung để giữ phong cách RO.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Ba thùy lần lượt phồng rất nhẹ, mắt chớp chậm | Thấy lớp tuyết phủ trên lưng, không thấy mặt | Thùy trước nhô ra, lõi băng lắc nhẹ |
| Walk | Thùy trái co → thân trượt tới → thùy phải co → thân ổn định | Hai thùy sau đẩy thân tiến lên từng nhịp | Thân dồn sang phải, thùy sau kéo theo rồi thu về |
| Attack | Nén cả thân → phồng thùy trước → trượt húc ngắn → bật về | Thùy phía hướng lên vươn ra, thân nén rồi thu | Thùy phải kéo dài thành cú húc mềm → bật về trạng thái tròn |

Lưu ý: Không dùng chu kỳ nhảy cao như Meadow Slime. Frost Slime trượt, dồn thùy và lăn nhẹ.

##### 7. Vai trò chiến đấu

Chậm, cận chiến, đòn va chạm mềm tầm ngắn. Attack không có mưa băng, băng nhọn hoặc phép tầm xa.

##### 8. Liên kết Snow

Lớp tuyết mềm, lõi lam, highlight trắng kem cùng họ với lông Frost Bear và Snow Fox. Tinh thể chìm liên kết với Crystal Golem mà không trùng cấu trúc tinh thể.

Chốt nhận diện: Bánh mochi tuyết ba thùy, mắt ngơ ngác, lõi bông tuyết nằm trong thân.

#### 02. Snow Fox — Lv12

Tên gợi ý: Snowwhisk Fox · Fast / Fragile

##### 1. Tính cách

Tinh nghịch, thông minh, hơi nhút nhát; thích chạy vòng quanh đối thủ rồi bất ngờ lao vào cắn.

##### 2. Silhouette / hình khối

Thiết kế hình tam giác nhỏ phía trước + chiếc quạt tuyết cực lớn phía sau.

- Thân cáo thon nhỏ, lưng thấp, bụng gọn.
- Đầu hình tam giác bo mềm, mõm nhọn vừa phải, không có đầu vuông hoặc ngực lớn của sói.
- Hai tai cực lớn, rộng ở gốc, đầu mềm, hơi xòe ngang.
- Bốn chân thanh mảnh, bàn chân rất nhỏ.
- Đuôi chiếm khoảng 45–50% tổng diện tích silhouette, phồng thành hình quạt lông lớn, đầu đuôi tách thành ba chóp mềm.
- Đuôi luôn cong sang một bên, không dựng kiểu đuôi dấu hỏi của Forest Wolf.
- Cao khoảng 120 px; bề ngang tổng thể có thể 155–175 px gồm đuôi.

Điểm khác Forest Wolf: Cáo thon, chân nhỏ, tai lớn, mõm nhọn và đuôi chiếm ưu thế. Wolf có ngực lớn, chân khỏe và bờm cổ; Snow Fox tuyệt đối không có bờm vai.

##### 3. Đặc trưng chữ ký

Một chóp đuôi hình bông tuyết ba cánh, không phải tinh thể thật, tạo từ ba búi lông trắng xanh.

Hai mảng lông nhỏ dưới mắt như dấu phẩy, làm biểu cảm cáo trông ranh mãnh.

##### 4. Mặt và biểu cảm

- Đi: Mắt xanh sapphire hình hạnh nhân, đồng tử lớn, hai lông mày hơi nhướng; miệng cười lệch, mũi nhỏ đen nâu.
- Đánh: Mắt nheo sắc, lộ một răng nanh bé, tai ép về sau, môi cong thành nụ cười tinh quái.
- Trúng đòn: Mắt mở to bất ngờ, tai cụp, đuôi xù mạnh rồi cuộn vào thân như tự bảo vệ.

##### 5. Bảng màu

#F7F6EE

#DCEEF0

#A8D0DF

#78A9C8

#E9C9C7

#5B6680

Trắng kem, xanh băng nhạt, tai hồng phấn dịu, mắt xanh sapphire.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Ngồi hơi thấp, nghiêng đầu, đuôi quạt phủ một bên thân | Đuôi xòe cực rõ, hai tai nhô lên | Ngẩng mõm đánh hơi, đuôi cong chậm |
| Walk | Chạy nhón bằng các bước ngắn, hai chân trước gần như lướt | Hông thấp, đuôi nảy nhẹ theo từng bước | Thân kéo dài → chân gom → bật thấp → tiếp đất |
| Attack | Hạ đầu → lao chéo ngắn → cắn nhanh → bật lùi | Thân phóng về hướng lên, đuôi xòe cân bằng | Chồm sang phải bằng chân trước, mõm mở rất nhanh rồi bật lùi |

Không dùng trot nặng của Forest Wolf. Snow Fox phải có cảm giác lướt nhanh, nhẹ như gió.

##### 7. Vai trò chiến đấu

Nhanh, HP thấp, cắn tầm gần. Không có chiêu băng tầm xa ở thiết kế cơ bản. Đuôi lớn giúp chuyển động dễ nhận ra ngay cả khi có nhiều người chơi.

##### 8. Liên kết Snow

Lông trắng lam, đuôi như tuyết phồng, highlight kem và mắt xanh băng. Không gắn tinh thể lên trán hay thêm vương miện để giữ bản sắc cáo.

Chốt nhận diện: Cáo tuyết tí hon tai lớn, thân thon, đuôi quạt ba chóp chiếm gần nửa silhouette.

#### 03. Ice Imp — Lv12

Tên gợi ý: Icicle Imp · Short-range Skirmisher

##### 1. Tính cách

Lanh lợi, nghịch ác nhẹ, rất thích chọc phá; trông nhỏ nhưng luôn cố thể hiện mình là pháp sư băng đáng gờm.

##### 2. Silhouette / hình khối

Thiết kế thành tiểu quỷ hai chân dáng đồng hồ cát nghiêng, không phải goblin mặc áo hoặc slime có chân.

- Đầu rộng hình kim cương bo góc, hai má nhọn mềm.
- Thân rất nhỏ và thon, bụng tròn như quả trứng.
- Hai chân ngắn, bàn chân to như giày tuyết tự nhiên.
- Hai cánh tay dài hơn chân, bàn tay ba ngón.
- Hai tai nhọn hình vây băng dựng sang hai bên, một tai có đầu cong.
- Sau đầu có một mào băng ngắn hình ba ngọn tóc, không phải sừng dài.
- Cao 135 px, rộng khoảng 95–110 px.

Tổng thể phải giống sinh vật băng nhỏ tinh quái, không giống người mặc giáp.

##### 3. Đặc trưng chữ ký

Một chiếc mào tóc băng ba khối trên đầu và hai bàn tay có đầu ngón màu xanh sáng như đang phủ sương.

Không mang gậy, kiếm hay vũ khí. Ma thuật phát ra từ tay.

##### 4. Mặt và biểu cảm

- Đi: Mắt vàng ngà to nhưng hơi xếch, lông mày cong tinh nghịch, miệng cười lộ một răng nanh nhỏ.
- Đánh: Nhíu một mắt, miệng cười rộng, hai tay mở ra như sắp ném một viên tuyết nhỏ.
- Trúng đòn: Hai mắt xoắn hoặc nhắm lệch, miệng méo, hai tai rũ xuống và mào băng xẹp về sau.

##### 5. Bảng màu

#8ACBE4

#BEE7F0

#E8F7F7

#668EBB

#F5E6BA

#56617B

Da lam băng, bụng xanh sáng, mắt kem vàng. Không dùng da xanh lá của goblin.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Nhún trên hai chân, xoay cổ tay, cười tự mãn | Mào băng và tai vây rõ, tay lắc nhẹ | Khom người về trước, một tay chống hông |
| Walk | Chạy bước ngắn, tay vung ngược chân, thân lắc linh hoạt | Bàn chân luân phiên, mào băng nảy | Chạy nhón, chân sau đẩy mạnh, đầu hơi chúi |
| Attack | Lùi một chân → gom sương vào hai tay → phóng luồng lạnh ngắn → thu tay | Đưa tay qua vai, phóng về hướng lên | Xoay hông sang phải, vung một tay tạo cú đánh băng ngắn |

##### 7. Vai trò chiến đấu

Tốc độ trung bình khá, nhanh hơn goblin, tấn công tầm ngắn bằng luồng lạnh hoặc cú quạt tay phủ sương. Không bay, không có phép xa màn hình.

Attack có thể sử dụng VFX là một luồng lạnh ngắn, mỏng, dài khoảng 0.5–0.8 lần chiều cao Imp, tùy hitbox được chốt.

##### 8. Liên kết Snow

Mào tóc băng và đầu ngón phủ sương giúp gắn với Snow. Khác Snow Fox ở hình người hai chân, khác Ice Wraith ở bàn chân chạm đất và thân vật chất rõ.

Chốt nhận diện: Tiểu quỷ băng hai chân, đầu kim cương mềm, tai vây, tóc băng ba khối và nụ cười tinh quái.

#### 04. Frost Bear — Lv13

Tên gợi ý: Snowmound Bear · Slow Tank · Scale ×1.15

##### 1. Tính cách

Hiền khô, hơi buồn ngủ, thích nằm trong tuyết; khi bị đánh thức sẽ cáu kỉnh và dùng sức nặng để đẩy đối thủ.

##### 2. Silhouette / hình khối

Thiết kế núi tuyết hình vòm có bốn chân, không làm gấu đứng hai chân giống Yeti.

- Thân bốn chân cực rộng, lưng vòm cao như gò tuyết.
- Cổ ngắn gần như không thấy, đầu tròn lớn nằm thấp phía trước.
- Hai tai tròn nhỏ, nằm trên hai bên đầu.
- Mõm ngắn, mũi to hình tam giác bo mềm.
- Hai chân trước to như cột ngắn, bàn chân rất rộng; chân sau ngắn và thấp.
- Một mảng lông ngực hình yếm tuyết trắng.
- Cao gốc 158 px, rộng 180–195 px; scale ×1.15 cho cảm giác nặng hơn hero.

Không đặt tinh thể trên lưng. Độ mạnh đến từ khối lượng thân.

##### 3. Đặc trưng chữ ký

Một chỏm lông hình đỉnh núi ở giữa trán và mảng yếm lông trắng rộng hình chữ U dưới cằm. Bàn chân có ba móng kem ngắn trông như hạt đậu.

##### 4. Mặt và biểu cảm

- Đi: Mắt nhỏ nửa lim dim, mí trễ, mũi lớn, miệng cười lười biếng.
- Đánh: Mắt mở to hơn, nhíu lông mày, miệng hình oval rộng, hai răng nanh ngắn hiện ra.
- Trúng đòn: Mắt nhắm tịt, tai cụp nhẹ, mũi nhăn, thân lắc nhưng vẫn đứng vững.

##### 5. Bảng màu

#EAEFE7

#CCDDE0

#A7BAC7

#F9F5E5

#8C9AAB

#665B5A

Trắng ngà, bóng xanh xám dịu và mũi nâu xám. Giữ màu sáng để thân gấu không thành khối xám nặng nề.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thở bụng chậm, đầu gật ngủ, tai giật một lần | Lưng vòm chiếm phần lớn hình, hai chân sau chống vững | Mõm hướng thấp, bụng phập phồng nhẹ |
| Walk | Bước bốn chân nặng, vai nhô luân phiên | Hông lắc rất nhẹ, lưng giữ vòm | Chân trước nâng cao rồi đặt xuống, thân trễ một nhịp |
| Attack | Dồn trọng lượng → nhấc một chân trước → đập xuống → trở về | Nâng vai rồi quật chân về hướng lên | Nghiêng thân và vung bàn chân gần sang phải, sau đó hạ xuống nặng |

##### 7. Vai trò chiến đấu

Tank chậm, thân rộng, đánh gần bằng chân trước. Đòn có lực nhưng không nhanh, không vồ xa như boss sói.

##### 8. Liên kết Snow

Gấu trắng mềm với bóng xanh lạnh và yếm kem. Liên hệ với Yeti qua chất liệu lông nhưng phân biệt bằng bốn chân, lưng vòm và gương mặt buồn ngủ.

Chốt nhận diện: Gấu bốn chân như một gò tuyết sống, lưng vòm cực rộng, tai tròn nhỏ, mắt buồn ngủ.

#### 05. Ice Wraith — Lv13

Tên gợi ý: Veilfrost Wraith · Fast Flying / Fragile

##### 1. Tính cách

Bí ẩn, lặng lẽ, hơi nhút nhát; thích xuất hiện bất ngờ rồi lẩn vào sương băng, không phải ma quỷ độc ác.

##### 2. Silhouette / hình khối

Thiết kế chuông băng nổi với những dải khăn tuyết, hoàn toàn không giống Thorn Sprite có cánh lá.

- Phần đầu là khối chuông băng cong hình mái vòm cao, hơi nghiêng ra trước.
- Bên dưới là thân thuôn, không có chân.
- Hai bên có hai dải băng mỏng như tay áo dài, buông cong trong không khí.
- Đuôi phía dưới chia thành ba dải sương băng lớn, bo tròn ở đầu, không phải nhiều sợi nhỏ.
- Có một vòng băng mỏng phía sau đầu như vầng hào quang bị khuyết, không phải vương miện.
- Cao khoảng 165 px, rộng 115–135 px.
- Khoảng lơ lửng so với mặt đất thể hiện bằng bóng ellipse tách riêng, không vẽ dính vào sprite.

Không cho Wraith đội mũ trùm vải đen hoặc có bộ xương bên trong.

##### 3. Đặc trưng chữ ký

Vầng băng khuyết hình lưỡi trăng nằm sau đầu, lệch nhẹ sang một bên, kết hợp ba dải đuôi sương.

Vầng băng là vật thể có hình khối thật với viền mềm, không phải glow hoặc particle.

##### 4. Mặt và biểu cảm

- Đi / Bay: Hai mắt tròn dài màu tím lam đậm, mí cong buồn, miệng nhỏ như đang thở dài.
- Đánh: Mắt sáng mở rộng, miệng thành đường cong quyết tâm; hai dải tay nâng lên.
- Trúng đòn: Mắt nhắm chặt, mặt co lại, thân xẹp và ba dải đuôi xoắn vào nhau.

Không dùng hốc mắt đen hoặc nụ cười ma quỷ.

##### 5. Bảng màu

#D5F0F4

#A9D7E7

#7CB8D8

#A49BCB

#F6F7EC

#59617F

Xanh băng sữa, tím lavender nhạt, trắng ngà. Độ trong chỉ là hiệu ứng vật liệu giả lập, thân vẫn có opacity đủ lớn để đọc trên nền tuyết.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Lơ lửng lên xuống 4–6 px, hai dải tay trôi nhẹ, mắt chớp chậm | Vầng băng khuyết rõ, ba đuôi sương tách lớp | Thân nghiêng nhẹ, dải phía sau trễ chuyển động |
| Walk / Fly | Bay trượt êm, đầu ổn định, dải đuôi dao động đối pha | Dải sương kéo dài phía sau khi bay lên | Thân kéo ngang, vầng băng hơi xoay theo hướng di chuyển |
| Attack | Hai tay mở → thân co → phóng xung băng ngắn → dải đuôi xòe | Tay dồn về hướng lên, thân ngả về trước | Một dải tay vung về phải, xung băng thoát ra rồi thân xoay về |

##### 7. Vai trò chiến đấu

Bay nhanh, máu thấp. Đòn tầm ngắn là xung lạnh hoặc cú quét sương gần cơ thể; không biến thành pháp sư bắn xa.

##### 8. Liên kết Snow

Vầng băng, sắc lavender và chất liệu tuyết mờ liên kết với Frostfang Den về sau. Khác Thorn Sprite vì không có cánh, khác Ice Imp vì không có chân, khác Sand Wraith tương lai ở cấu trúc chuông băng và vầng khuyết.

Chốt nhận diện: Hồn băng hình chuông, vầng trăng băng khuyết sau đầu và ba dải sương mềm phía dưới.

#### 06. Frostbite Yeti — Lv14

Tên gợi ý: Snowtuft Yeti · Heavy Slow Swing · Scale ×1.2

##### 1. Tính cách

To xác, hơi vụng về, dễ nóng tính; thường đấm ngực dọa đối thủ nhưng có vẻ ngạc nhiên khi bị đánh trúng.

##### 2. Silhouette / hình khối

Thiết kế khối quả lê ngược đứng hai chân, vai rộng và tay dài, khác hoàn toàn Frost Bear.

- Đầu nằm sâu giữa hai vai phủ lông, cổ gần như không thấy.
- Vai cực rộng, thân thu hẹp dần về hông.
- Hai tay dài tới gần đầu gối, bàn tay to với bốn ngón tròn.
- Hai chân ngắn nhưng bàn chân cực lớn, giúp dáng đứng chắc.
- Mặt lộ qua một khoảng lông hình oval tối hơn; không phải mặt người.
- Đỉnh đầu có ba búi lông mềm dựng lệch.
- Lông ở khuỷu tay và hông xòe thành từng mảng lớn, không vẽ sợi vụn.
- Cao gốc 162 px, rộng 145–160 px; hiển thị khoảng 194 px.

Không dùng sừng, giáp hoặc vũ khí. Sức mạnh nằm ở tay và vai.

##### 3. Đặc trưng chữ ký

Một mặt nạ lông hình trái tim ngược bao quanh mắt và mũi, cùng hai cánh tay dài có lớp lông cổ tay như găng tuyết khổng lồ.

Không dùng tim thật hoặc biểu tượng trang trí; đó là ranh giới giữa hai màu lông.

##### 4. Mặt và biểu cảm

- Đi: Mắt tròn nhỏ sâu trong mặt, lông mày dày, miệng mím hơi cáu kỉnh.
- Đánh: Hai mắt mở rộng, miệng há lớn lộ hai nanh ngắn, lông mày ép xuống, hai tay giơ cao.
- Trúng đòn: Mắt nhắm ><, miệng méo, đầu rụt vào vai, hai tay đưa lên che mặt.

##### 5. Bảng màu

#F0F3EA

#C8DCE1

#9FBECA

#7898AA

#D6C0A7

#625A60

Lông trắng xám, vùng mặt xanh xám ấm, bàn tay và bàn chân kem xám.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thở mạnh, vai nhô lên xuống, một tay gãi đầu | Lưng phủ lông thành 3 khối lớn, tay thõng hai bên | Chuyển trọng tâm sang một chân, bàn tay đung đưa |
| Walk | Bước hai chân nặng, vai lắc trái-phải, tay vung chậm | Lông lưng nảy trễ, chân đặt xuống nặng | Chân bước rộng nhưng chậm, tay dài kéo theo thân |
| Attack | Gồng vai → giơ một tay cao → vung xuống theo cung rộng → hồi tay chậm | Xoay thân, nâng tay đập về phía lên | Vặn vai về sau → quét một tay lớn sang phải → dừng nặng |

##### 7. Vai trò chiến đấu

Nặng, chậm, đòn vung tay có tầm và sát thương rõ. Attack phải có anticipation dài hơn Frost Bear. Không đánh bằng nhảy nhanh hoặc quăng đá.

##### 8. Liên kết Snow

Lông trắng, bóng lam xám, hình khối mềm. Cùng họ chất liệu với Bear nhưng Yeti là sinh vật hai chân, có vai, tay dài và khuôn mặt riêng.

Chốt nhận diện: Người tuyết lông xù vai rộng, mặt trái tim ngược, hai tay dài khổng lồ và chân ngắn.

#### 07. Crystal Golem — Lv15

Tên gợi ý: Glintcore Golem · Heaviest Tank · Scale ×1.25

##### 1. Tính cách

Ít nói, chậm chạp, kiên nhẫn, gần như không hiểu cảm xúc của đối thủ; khi tấn công chỉ làm theo bản năng bảo vệ tinh thể.

##### 2. Silhouette / hình khối

Thiết kế pháo đài tinh thể đi bằng hai chân, không phải Treant bằng đá hoặc Yeti có giáp.

- Thân chính hình lục giác đứng, đáy rộng, vai không tách thành vai người.
- Đầu là khối tinh thể nhỏ hình thang bo cạnh nằm thấp trên thân.
- Hai tay là những khối đá băng lớn dạng hộp bo góc, không có cơ bắp.
- Cánh tay cấu tạo từ ba khối: vai, cẳng tay, nắm đấm, có khe khớp ngắn.
- Chân rất ngắn, bàn chân dạng bệ đá rộng, gần như một chân đế di động.
- Sau lưng có hai phiến tinh thể lớn nghiêng ra ngoài như cánh bảo vệ nhưng không phải cánh bay.
- Cao gốc 169 px, rộng 155–175 px; hiển thị khoảng 211 px.

Không làm đầu gai nhọn hoặc phủ tinh thể nhỏ toàn thân. Mọi khối đều dày, có trọng lượng.

##### 3. Đặc trưng chữ ký

Một lõi lục giác sáng màu xanh lam nằm giữa ngực, bao quanh bởi ba mảng đá băng lớn như cửa bảo vệ.

Trên hai nắm đấm có mặt phẳng tinh thể trắng xanh tạo cảm giác giáp cực dày.

##### 4. Mặt và biểu cảm

- Đi: Hai mắt hình hạt đậu phát sáng vàng nhạt nằm trên khối đầu, miệng là đường ngắn ngang; biểu cảm bình thản.
- Đánh: Mắt chuyển thành hai nét nghiêng quyết tâm, miệng mở thành khe ngắn, lõi ngực sáng tăng một cấp.
- Trúng đòn: Mắt co nhỏ, đầu giật lùi rất ít, khe giữa các khối rung; không biến dạng như sinh vật mềm.

##### 5. Bảng màu

#D5EEF3

#9ACFDF

#679DBB

#5E82A6

#F5F7E9

#5C6980

Tinh thể xanh nhạt, cạnh sáng trắng kem, khe khớp xanh lam xám. Không tạo bề mặt kim loại bóng.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Hầu như đứng yên, lõi ngực dao động nhẹ, đầu nghiêng một góc nhỏ | Hai phiến lưng tạo chữ V, thân đứng rất chắc | Nắm đấm hơi xoay, vai khối dịch 1–2 px |
| Walk | Nhấc bệ chân trái → đặt mạnh → nhấc bệ phải → đặt mạnh | Thân dao động cực ít, phiến lưng rung trễ | Cả khối nghiêng rất nhẹ khi nhấc chân rồi lập tức ổn định |
| Attack | Nâng nắm đấm → giữ thế → đập xuống → khóa khớp và hồi chậm | Xoay thân, đập về phía lên, phiến lưng nghiêng | Kéo tay gần về sau → đấm nặng sang phải → dừng một nhịp |

##### 7. Vai trò chiến đấu

Tank chậm nhất của Frost Hollow, giáp dày, tấn công bằng nắm đấm nặng. Không có animation chạy hoặc bật nhảy; mọi chuyển động phải thể hiện quán tính và trọng lượng.

##### 8. Liên kết Snow

Tinh thể xanh lam và highlight trắng cùng họ với Frost Slime, nhưng Golem có khối đa giác cứng và cơ thể phân mảnh. Khác Bramble Treant vì không dùng thân gỗ hình thùng hoặc tay rễ cong.

Chốt nhận diện: Pháo đài băng lục giác hai chân, tay hộp lớn, lõi ngực sáng và hai phiến tinh thể chữ V sau lưng.

### III. BOSS FROST HOLLOW

#### 08. Frostfang King — Lv15

Boss map Frost Hollow · Royal Ice Wolf · Scale ×1.65

##### 1. Tính cách

Vị vua sói băng điềm tĩnh và đầy kiêu hãnh, bảo vệ lãnh địa tuyết; không hung hãn vô cớ nhưng khi chiến đấu sẽ thể hiện sức mạnh áp đảo.

##### 2. Silhouette / hình khối

Thiết kế linh thú sói băng mang dáng sư tử hoàng gia, khác Thornfang Alpha hình mũi tên săn mồi.

- Thân bốn chân khỏe nhưng ngắn, ngực cao và rộng.
- Tư thế đứng thẳng, đầu ngẩng; trọng tâm dồn giữa bốn chân, không hạ vai như sói săn mồi.
- Đầu tương đối lớn, mõm ngắn rộng, không dài như Thornfang Alpha.
- Bờm xòe thành vòng lông tuyết tròn quanh cổ và hai vai, với 6–8 cụm lớn như mây tuyết, thay vì bờm đổ về sau.
- Hai chân trước lớn, phủ lông trắng như găng tuyết; chân sau gọn hơn.
- Đuôi ngắn hơn Thornfang Alpha, cực dày, cong lên thành một chùm lông tuyết có đầu hình giọt.
- Hai tai ngắn, nhọn bo mềm, nằm hai bên vương miện.
- Cao thiết kế 155 px, rộng 175–195 px; hiển thị cao khoảng 256 px sau scale ×1.65.

Silhouette chính: Vòng bờm tuyết tròn + đầu vua ngẩng cao + vương miện băng + bốn chân lớn.

Không dùng dáng sói dài, lưng thấp, bờm lùi ra sau hoặc đuôi dấu hỏi của Forest Wolf.

##### 3. Đặc trưng chữ ký

Vương miện băng ba chóp kiểu tuyết kết tinh, thiết kế mới dựa trên ý tưởng hoàng gia của bản pet cũ trong ảnh 2:

- Chóp giữa cao nhất, hình giọt băng dẹt.
- Hai chóp bên thấp, hướng ra ngoài.
- Đế vương miện là vòng vàng cổ mảnh, ôm sát đầu, không phải mũ kim loại lớn.
- Giữa vương miện có một viên ngọc lam sapphire nhỏ.
- Bờm phía má có hai cụm lông dài hướng xuống như dải áo choàng hoàng gia tự nhiên.

Không sao chép nguyên vương miện cũ; giữ ý tưởng ba chóp băng và vàng cổ nhưng đơn giản, cân đối, đọc rõ khi thu nhỏ.

Một đặc trưng phụ là hai vệt lông lam nhạt dưới mắt, khác vệt kem của Thornfang Alpha.

##### 4. Mặt và biểu cảm

- Đi: Mắt xanh sapphire lớn vừa phải, hơi hẹp và nghiêm nghị; lông mày cong tạo sự điềm tĩnh, miệng khép, hai nanh nhỏ thấp thoáng.
- Đánh: Mắt sắc, đồng tử thu nhỏ nhẹ, miệng mở rộng lộ bốn nanh băng trắng; bờm nở và tai dựng.
- Trúng đòn: Mắt nheo, hàm nghiến, một chân trước trượt lùi, bờm dồn về sau; thể hiện bị tác động nhưng vẫn giữ tư thế vua.
- Gầm: Đầu ngẩng cao, mắt không bị vương miện che, miệng mở lớn nhưng gương mặt vẫn có cảm xúc. Không dùng biểu cảm ác quỷ.

##### 5. Bảng màu

#F4F7F0

#C9E5EF

#80BCD9

#468DB8

#E7C477

#596684

Trắng tuyết chủ đạo, bờm trắng lam, bóng xanh sapphire, điểm vàng cổ trên vương miện. Viền nâu xám đậm mềm, không dùng đen.

##### 6. Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đứng uy nghi, ngực nở, bờm nâng nhẹ theo nhịp thở, đuôi quét chậm | Vòng bờm và lưng phủ lông hiện rõ, vương miện nhìn từ sau có ba chóp | Đầu ngẩng cao, một chân trước nhấc rất nhẹ rồi hạ xuống |
| Walk | Sải bước chắc và chậm vừa, đầu giữ cao, bờm chuyển động trễ | Vai đẩy luân phiên, đuôi lắc cân bằng | Chân trước đặt bước rộng, bờm xòe theo nhịp, thân tiến mượt |
| Attack | Hạ đầu một nhịp → gầm mạnh → lao vồ bằng hai chân trước → đáp xuống | Vai thu → lao về hướng lên → hai chân trước đập xuống | Xoay vai về sau → phóng sang phải bằng cú vồ/cắn → hạ chân nặng |

Attack của boss phải có cảm giác uy lực hơn Forest Wolf nhưng không nhanh như Thornfang Alpha. Bờm là bộ phận chuyển động thứ cấp chủ đạo.

##### 7. Vai trò chiến đấu

Boss map Frost Hollow, có thể kết hợp đòn vồ, cắn và gầm băng; sức mạnh đến từ cơ thể hoàng gia và năng lượng tuyết.

Đòn cơ bản cần giữ tính cận chiến rõ. Nếu về sau có skill diện rộng, VFX sẽ là sheet riêng:

- Gầm băng tạo sóng lạnh lan ra.
- Vồ mạnh tạo băng vỡ dưới chân.
- Cú cắn có vệt sáng lạnh ngắn.

Không gắn các VFX này vào từng frame Walk hoặc Attack cơ bản.

##### 8. Liên kết Snow

- Có cùng ngôn ngữ tuyết mềm với Snow Fox và Frost Bear nhưng tạo hình hoàn toàn khác.
- Vương miện kết nối với chất liệu băng của Crystal Golem mà không cần mọc tinh thể trên lưng.
- Bờm trắng phồng như mây tuyết, sử dụng highlight kem và bóng lam đúng bảng màu Snow.
- Ánh mắt và các mảng lông xanh sapphire giúp phân biệt boss với các quái thường.

##### 9. Quy tắc boss — uy nghi, đe doạ rõ nhưng vẫn dễ thương

Frostfang King phải trông như một vị vua linh thú bước ra từ truyện cổ tích tuyết, đủ uy nghi khi đứng giữa hàng chục hero.

- Focal point thứ nhất: Gương mặt và ánh mắt sapphire.
- Focal point thứ hai: Vương miện ba chóp băng.
- Focal point thứ ba: Vòng bờm lông tuyết trắng lam.
- Chân và thân không phủ chi tiết nhỏ quá mức để tránh làm loãng điểm nhìn.
- Không dùng giáp toàn thân hoặc áo choàng thật; bờm tự nhiên đã tạo cảm giác áo choàng.
- Không thêm sừng lớn, cánh, lửa tím, mắt đỏ hoặc aura tối.
- Độ tương phản tập trung ở mắt, vùng mặt và vương miện, giúp boss đọc rõ trên nền tuyết sáng.
- Có thể thêm các mảng xanh sapphire đậm ở chân trước và đuôi để tách silhouette khỏi nền, nhưng không vượt quá khoảng 20–25% diện tích thân.

Chốt nhận diện: Vua sói băng ngực cao, bờm tuyết tròn như sư tử, vương miện băng ba chóp vàng cổ và mắt sapphire điềm tĩnh.

### IV. KIỂM TRA TRÙNG SILHOUETTE

| Quái | Khối hình chủ đạo | Điểm chữ ký | Khác biệt so với lượt 1 |
|---|---|---|---|
| Frost Slime | Bánh tuyết ba thùy | Lõi bông tuyết | Không phải giọt thạch nghiêng có mầm |
| Snow Fox | Tam giác nhỏ + đuôi quạt | Đuôi lông ba chóp | Không có ngực lớn hoặc bờm sói |
| Ice Imp | Đồng hồ cát hai chân | Mào tóc băng, tai vây | Không giống thỏ, bọ ngựa hoặc Treant |
| Frost Bear | Núi tuyết bốn chân | Lưng vòm, tai tròn, yếm trắng | Không giống Boar oval ngang có mõm húc |
| Ice Wraith | Chuông băng có đuôi sương | Vầng băng khuyết | Không có cánh lá như Thorn Sprite |
| Frostbite Yeti | Quả lê ngược, tay dài | Mặt lông tim ngược | Không dùng gốc cây hoặc vai đá |
| Crystal Golem | Pháo đài lục giác | Lõi ngực, phiến lưng chữ V | Không có cấu trúc gỗ và rễ Treant |
| Frostfang King | Vòng bờm tròn, tư thế hoàng gia | Vương miện băng ba chóp | Không dùng dáng săn mồi, bờm đổ sau của Alpha |

#### V. TỈ LỆ CUTE / THREAT

| Quái | Cute | Threat | Cảm giác chính |
|---|---|---|---|
| Frost Slime | 90% | 10% | Ngây thơ |
| Snow Fox | 85% | 15% | Lanh lợi |
| Ice Imp | 75% | 25% | Tinh quái |
| Frost Bear | 80% | 20% | To lớn nhưng hiền |
| Ice Wraith | 70% | 30% | Bí ẩn |
| Frostbite Yeti | 60% | 40% | Vụng về nhưng mạnh |
| Crystal Golem | 55% | 45% | Kiên cố |
| Frostfang King | 45% | 55% | Uy nghi và quyền lực |

Đây là tỷ lệ định hướng thiết kế, không phải tỷ lệ màu sắc hoặc chỉ số gameplay.

### VI. QUY TẮC BÀN GIAO SPRITE SNOW

1. Giữ nhất quán góc nhìn

Hướng D phải thấy mặt, U phải thấy lưng và không thấy mặt, R là 3/4 nhìn sang phải. Snow Fox và Frostfang King cần kiểm tra kỹ độ che của đuôi và bờm ở hướng U.

2. Không lạm dụng tinh thể

Tinh thể chỉ là motif Snow, không phải phụ kiện bắt buộc. Crystal Golem là đại diện cấu trúc băng cứng; Slime có lõi nhỏ; King có vương miện. Bear, Fox và Yeti chủ yếu dùng lông.

3. Animation phải phản ánh vật liệu

- Slime: Trượt và nén mềm.
- Fox: Chạy nhón, nhẹ và bật nhanh.
- Imp: Bước nhanh, tay linh hoạt.
- Bear: Bước nặng bốn chân.
- Wraith: Lơ lửng và trôi.
- Yeti: Bước nặng hai chân, tay vung chậm.
- Golem: Chuyển động như khối đá có trọng lượng.
- King: Sải bước uy nghi, bờm chuyển động trễ.

4. Đọc rõ trên nền tuyết

Vì Frost Hollow sáng, không được chỉ dùng trắng thuần. Mỗi quái cần vùng bóng xanh/xám đủ rõ và viền nâu xám đậm mềm. Tránh glow trắng làm mất silhouette.

5. Hitbox và kích thước

Những bộ phận nhô lớn như đuôi Snow Fox, bờm Frostfang King, cánh tay Yeti hoặc phiến lưng Golem không mặc định mở rộng hitbox va chạm. Hitbox gameplay cần bám thân và chân theo vai trò, còn vùng trúng đòn có thể thiết kế riêng.

6. Boss phải được render ở độ phân giải đủ lớn

Frostfang King gốc 155 px, scale ×1.65 sẽ cao khoảng 256 px. Không nên chỉ phóng to bitmap 192 px. Cần vẽ ở độ phân giải nguồn đủ cao rồi đưa về kích thước hiển thị, giữ nét outline sạch và mặt rõ.

### VII. KẾT LUẬN LƯỢT 2

8/8 quái có silhouette riêng. Bộ Frost Hollow được phân tầng từ sinh vật mềm, nhanh, bay đến tank nặng và boss hoàng gia.

Ba cặp được kiểm soát trùng lặp đặc biệt:

- Meadow Slime ≠ Frost Slime: Giọt thạch nghiêng và nhảy cao so với bánh tuyết ba thùy trượt thấp.
- Forest Wolf ≠ Snow Fox: Sói ngực lớn, đuôi dấu hỏi và chạy bầy so với cáo thân thon, tai lớn, đuôi quạt tuyết.
- Thornfang Alpha ≠ Frostfang King: Sói thủ lĩnh dáng săn mồi, bờm đổ sau so với vua linh thú dáng cao, bờm tròn và vương miện băng.

ART DIRECTION: CHỐT 8 THIẾT KẾ LƯỢT 2 — tổng 17/39 quái đã được định nghĩa ở cấp thiết kế.

## 4. Frostfang Den + 2 boss truy nã + Highland (Sunbreak): 10 quái

### MONSTER REDESIGN — LƯỢT 3/5

10 quái · Frostfang Den + Wanted Boss + Highland Sunbreak

Giữ nguyên quy tắc A/B và 17 thiết kế đã chốt ở lượt 1–2. Lượt này bổ sung 3 quái hang băng, 2 Wanted Boss và 5 quái Highland.

Định hướng quan trọng:

- Frostfang Den: Cùng hệ sinh thái Snow nhưng cổ xưa, lạnh sâu, xanh sapphire và tím băng rõ hơn. Không chuyển sang dark fantasy.
- Wanted Boss: Đẳng cấp tạo hình Mire Hydra Queen, có sức mạnh và khả năng đọc tốt giữa 20–40 người chơi. Hai boss đứng cố định, chuyển động bằng đầu, thân trên, tay và các bộ phận đặc trưng.
- Highland: Ấm, nắng vàng, gió mạnh, đá cát. Tách biệt hoàn toàn khỏi Snow và Forest.
- Không trùng silhouette: Spectral Wolf không có cấu trúc sói bốn chân thông thường; Glacier Warden, Glacial Colossus, Crystal Golem và Stoneback Guardian phải có bốn cấu trúc riêng.

#### I. Bảng kích thước và vai trò

| Quái | Lv | Cao gốc | Scale | Hiển thị | Vai trò |
|---|---|---|---|---|---|
| Ice Goblin | 16 | 151 px | ×1 | 151 px | Cận chiến chuẩn |
| Spectral Wolf | 18 | 131 px | ×1 | 131 px | Nhanh, đánh liên tục |
| Glacier Warden | 20 | 163 px | ×1.65 | ≈269 px | Boss hang |
| Rimeclaw Tyrant | 10 | 400 px | ×1 | ≈400 px | Wanted Raid |
| Glacial Colossus | 25 | 400 px | ×1 | ≈400 px | Wanted Raid |
| Highland Ibex | 16 | 168 px | ×1 | 168 px | Húc tầm ngắn |
| Dustscale Lizard | 17 | 120 px | ×1.15 | 138 px | Nhanh, máu thấp |
| Cliff Harpy | 18 | 150 px | ×1 | 150 px | Bay, bắn xa |
| Stoneback Guardian | 20 | 166 px | ×1.4 | ≈232 px | Tank chậm nhất |
| Ridgehorn Behemoth | 20 | 178 px | ×1.8 | ≈320 px | Boss map |

Quy ước: D = xuống, U = lên, R = phải; trái lật R. Mỗi hướng có Walk 4 frame và Attack 4 frame.

Với Wanted, khoảng 400 px là chiều cao hiển thị mong muốn, không thể chứa nguyên nhân vật trong ô 384 px nếu không thu nhỏ, mở rộng canvas hoặc chia layer. Chốt phương án production: ô 384×384 chứa phần thân chính trong tư thế chuẩn ở chiều cao tối đa khoảng 360–370 px, sau đó scale Unity khoảng 1.08–1.10 để đạt 400 px. Attack và Cast dùng ô 512×512 để đủ biên chuyển động.

### II. FROSTFANG DEN — HANG BĂNG

Palette chung: Sapphire #4D86B5, lam băng #91CBE2, tím băng #AAA1CE, trắng kem #EDF4ED, bóng xanh xám #52647F.

Motif lặp lại là băng phân lớp, tinh thể ngậm trong đá, vệt sương bạc và phù văn lạnh cổ. Không gắn tinh thể lên tất cả sinh vật.

#### 01. Ice Goblin — Lv16

Tên gợi ý: Frostcap Goblin · Standard Melee

1) Tính cách: Ranh mãnh, tham lam, thích giành chiến lợi phẩm, thường cười khúc khích trước khi lao vào đánh.

2) Silhouette / hình khối

- Humanoid thấp, vai hẹp, bụng tròn nhô trước; lưng gù nhẹ.
- Đầu hình hạt đậu ngang, lớn khoảng 42% chiều cao phần đầu-thân.
- Hai tai to dài cong xuống như vây cá, khác tai dựng của Ice Imp.
- Tay ngắn khỏe, chân hơi vòng kiềng; bàn chân lớn.
- Cao 151 px, rộng khoảng 100–115 px.
- Không có mào băng ba chóp của Imp.

3) Đặc trưng chữ ký

Một chiếc mũ chỏm bằng vỏ nấm băng mọc tự nhiên như lớp da đầu, và cây dùi cui làm từ một khúc băng đục đầu tròn. Mũ lệch sang trái, có hai vết xước cong.

4) Mặt + biểu cảm

- Đi: Mắt vàng kem hơi xếch, lông mày tinh quái, cười lộ hai răng nhỏ.
- Đánh: Một mắt nheo, miệng há cười, má phồng.
- Trúng đòn: Mắt nhắm lệch, miệng méo, tai rủ xuống.

5) Màu

#8CB6C2 da xanh xám · #D9E8E5 bụng · #567F9D mũ · #B6A4CF bóng tím · #F3DC9C mắt · #514C62 viền.

6) Animation 3 hướng

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Cầm dùi cui gõ tay, đảo mắt | Lưng gù, mũ và tai rủ | Khom nhẹ, cười nhếch |
| Walk | Bước lạch bạch 4 nhịp, tay vung lệch | Bàn chân bước luân phiên, mũ rung | Chạy bước ngắn, bụng nảy nhẹ |
| Attack | Rút dùi về → vung chéo → chạm → thu | Xoay vai, đánh về hướng U | Vung dùi ngang sang phải rồi kéo về |

7) Vai trò: Cận chiến chuẩn, tốc độ và sức chịu đòn trung bình. Đòn ngắn, dễ đọc, không có phép diện rộng.

8) Liên kết: Mũ băng, da xám lạnh và dấu xước tinh thể liên kết Frostfang Den. Khác Ice Imp ở đầu ngang, tai rủ, dáng gù và vũ khí vật lý.

9) Boss: Không áp dụng.

Chốt: Goblin băng đầu hạt đậu, tai rủ, mũ nấm băng lệch và dùi cui tròn.

#### 02. Spectral Wolf — Lv18

Tên gợi ý: Moonveil Wolf · Rapid Combo Attacker

1) Tính cách: Im lặng, tinh nghịch theo kiểu ma thuật; xuất hiện và biến mất giữa những vệt sương, săn đuổi liên tục không nghỉ.

2) Silhouette / hình khối

Không sử dụng sói bốn chân thông thường.

- Cơ thể hình lưỡi liềm nằm ngang, phần đầu và ngực có hình sói rõ, phần thân sau tan thành hai dải hồn băng lớn.
- Chỉ có hai chân trước dạng chân sói; hai chân sau biến thành luồng sương cong, không có bàn chân sau rõ.
- Đầu thon dài, tai kéo ngược như hai ngọn lửa mềm.
- Lưng tạo đường cong chữ S, không có bờm cổ tròn hoặc bờm vai.
- Đuôi không phải một chiếc đuôi thật, mà là hai dải linh hồn tách nhau.
- Cao 131 px, rộng 160–180 px.

Silhouette gần như một con sói đang lao xuyên qua không khí, không thể đứng bằng bốn chân.

3) Đặc trưng chữ ký

Hai dải hồn trăng khuyết kéo dài phía sau và một dấu trăng nhỏ màu bạc nằm trên trán. Các dải có khối màu đục, viền mềm, không phải particle trong suốt.

4) Mặt + biểu cảm

- Đi/bay: Mắt hình hạnh nhân sáng lavender, miệng cười mỏng, tai kéo ngược.
- Đánh: Mắt thu thành hai nét sắc, nhe hai nanh nhỏ, mõm vươn dài.
- Trúng đòn: Mắt nhắm, đầu giật nhẹ, hai dải hồn xoắn và co ngắn.

5) Màu

#D7EAF0 thân sáng · #A5BBD9 bóng · #8D8AC2 lavender · #617FA9 lõi · #F1F4E9 bạc kem · #536078 viền.

6) Animation 3 hướng

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Lơ lửng thấp, hai chân trước co, dải hồn trôi | Lưng cong và hai dải hồn rõ, không thấy mặt | Thân chữ S dao động, mõm nghiêng |
| Walk | Trượt nhanh bằng hai nhịp kéo-thu, chân trước quạt nhẹ | Hai dải hồn kéo dài về sau | Lao lướt ngang, thân duỗi rồi co |
| Attack | Co đầu → cắn nhanh → rút → cắn lần hai trong cùng chu kỳ | Hai cú quật đầu về hướng U | Hai nhịp cắn liên tục sang phải, dải hồn giật theo |

7) Vai trò: Nhanh, đánh liên tục, mỗi đòn gọn và ít anticipation. Chuyển động giống lướt hơn chạy, không phải quái bay cao như Wraith.

8) Liên kết: Cùng tông sapphire/tím băng nhưng không có tinh thể hoặc bờm. Không trùng Forest Wolf, Snow Fox, Thornfang Alpha hay Frostfang King vì mất nửa thân sau và dùng cấu trúc linh hồn chữ S.

9) Boss: Không áp dụng.

Chốt: Đầu sói hồn ma với hai chân trước, thân chữ S và hai dải hồn trăng khuyết thay chân sau.

#### 03. Glacier Warden — Lv20

Boss hang · Ancient Ice Sentinel · Scale ×1.65

1) Tính cách: Người canh giữ cổ xưa, nghiêm nghị và kiên nhẫn; không hung ác, chỉ trừng phạt những kẻ bước vào nơi cấm.

2) Silhouette / hình khối

Thiết kế thành hiệp sĩ băng dạng chuông cao, không phải Golem khối hộp.

- Thân trên hình vòm hẹp, phủ các tấm băng tự nhiên xếp như áo choàng giáp.
- Không có chân riêng rõ rệt; đáy là ba mảng băng hình chân đế nổi thấp.
- Đầu nhỏ hình mũ trụ băng, đặt sâu dưới một mái che cong.
- Hai tay dài, một tay cầm khiên băng hình giọt nước đảo ngược, tay còn lại là cánh tay thương băng dài.
- Vai nhọn vừa phải nhưng bo cạnh, silhouette dọc cao.
- Một dải áo choàng băng cứng sau lưng, chia hai vạt cong.
- Cao gốc 163 px, hiển thị khoảng 269 px.

Khác Crystal Golem: Warden thon, cao, có khiên/thương và dáng hiệp sĩ; Golem thấp nặng, lục giác, hai nắm đấm lớn.

3) Đặc trưng chữ ký

Mặt nạ băng hình giọt nước ngược với một khe mắt ngang màu vàng kem, và chiếc khiên hình giọt nước có phù văn hình vòng cung.

Không dùng lõi lục giác giữa ngực như Crystal Golem.

4) Mặt + biểu cảm

- Đi: Khe mắt hiền nhưng nghiêm, đầu giữ thẳng, không có miệng lộ.
- Đánh: Khe mắt nghiêng sắc hơn, ánh sáng tăng nhẹ; phần mặt nạ hạ xuống.
- Trúng đòn: Khe mắt chớp ngắn, đầu nghiêng, vai giáp rung; biểu cảm thể hiện qua mắt và tư thế.

5) Màu

#D7E8EF băng sáng · #8FBFD7 lam · #548CB8 sapphire · #A9A2CC bóng tím · #E8D5A4 phù văn · #52617B viền.

6) Animation 3 hướng

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thương dựng, khiên trước ngực, các vạt băng rung nhẹ | Thấy hai vạt áo băng và mũ phía sau | Chuyển trọng tâm, thương nghiêng ra trước |
| Walk | Trượt bước ngắn trên chân đế, thân giữ thẳng | Thân tiến chậm, áo băng kéo theo | Tiến ngang bằng nhịp đẩy chân đế |
| Attack | Hạ thương → đâm thẳng → giữ → rút, khiên che bên cạnh | Xoay vai đâm về hướng U | Thu thương về sau → đâm sang phải → kéo lại |

7) Vai trò: Boss hang, tầm đánh dài hơn quái thường, thiên về kiểm soát lối đi bằng thương và khiên. Không cần chuyển động chạy nhanh.

8) Liên kết: Phù văn sapphire, băng phân lớp, chất liệu cổ liên kết Frostfang Den. Giữ màu sáng và đường cong mềm.

9) Boss

- Tỉ lệ đầu đủ lớn để khe mắt đọc được.
- Khiên và thương tạo điểm nhận diện, không cần giáp vụn khắp người.
- Tấm băng lớn cho cảm giác trang nghiêm.
- Mặt nạ không biến thành đầu lâu.
- Độ hoàn thiện như Mire Hydra Queen nhưng hình khối đơn giản, rõ hướng đánh.

Chốt: Hiệp sĩ băng thon cao, mặt nạ giọt nước, khiên giọt đảo ngược và thương băng dài.

### III. WANTED BOSSES — EVENT RAID

Hai boss có yêu cầu riêng:

- Đứng cố định tại vị trí spawn, không có Walk di chuyển trên map.
- Sprite cao khoảng 2.5× hero (~400 px).
- Idle dùng sheet 384×384 với scale phù hợp; Attack và Cast dùng 512×512.
- Cast gồm 4 hàng, mỗi hàng ứng với một skill riêng; mỗi hàng có 4 frame chuẩn bị/niệm/đỉnh/thu.
- Hướng D/U/R có thể dựng thành sheet riêng cho mỗi hướng, hoặc tổ chức atlas nhiều hàng, nhưng phải giữ consistency.
- Vùng đe doạ, cảnh báo và VFX tách khỏi sprite.
- Boss không được che hoàn toàn telegraph hoặc người chơi ở gần.

#### 04. Rimeclaw Tyrant — Lv10

Wanted Boss · Crowned Ice Wolf Tyrant · Stationary Raid

**Đổi thiết kế 20/11/2026 (người dùng):** "vẽ wanted boss con sói đi" rồi chọn "Rimeclaw Tyrant thành sói", ảnh mẫu chính: con ĐẦU TIÊN (bên trái) của `docs/reference/ui-style-ro-soft-fantasy/wanted-boss/5-wanted-boss-board-style.png` (cùng họ với Glacial Colossus ở giữa và Mire Hydra Queen v2 bên phải). Bản cua-sư tử cũ (ChatGPT art director lượt 3, "không dùng sói") BỎ. Id `rimeclawTyrant`, skill, luật server, số liệu giữ nguyên. Sprite ChatGPT (Tan Pham): https://chatgpt.com/c/6b0003a9-2194-83ec-97c9-65878e4f9677, ảnh `docs/reference/rimeclaw-tyrant-wolf/`.

1) Tính cách: Bạo chúa sói băng kiêu ngạo, gầm đe doạ, coi mình là vua tuyết. Dễ thương 35 % / uy hiếp 65 %.

2) Silhouette / hình khối (chép concept, chibi RO Soft)

- Sói BỐN CHÂN, ngực cao, đầu ngẩng to, thân sau ngắn gọn (vừa ô 384: rộng mõm-đuôi ≤ 320 px nguồn khi nhìn ngang).
- Lông trắng xù pha xanh băng, bờm dày quanh cổ, tai nhọn, đuôi trắng xù to cong lên.
- Cụm TINH THỂ BĂNG xanh to dọc lưng và vai (nhìn lên thấy một chùm tinh thể lớn giữa lưng).
- Áo choàng XANH NAVY viền vàng, cổ lông trắng; giáp ngực navy viền vàng có đá xanh hình thoi; giáp ống chân navy viền vàng có đá xanh ở cả bốn chân; móng vuốt xanh băng.
- Silhouette chính: chùm tinh thể lưng + vương miện + áo choàng navy xoè hai bên.

3) Đặc trưng chữ ký: VƯƠNG MIỆN VÀNG nhiều chóp có viên đá xanh dương hình thoi to giữa trán.

4) Mặt + biểu cảm (BẮT BUỘC, lỗi 20/11: bản đầu mất lòng đen)

- Mắt: lòng trắng xanh nhạt, mống mắt VÀNG HỔ PHÁCH, CON NGƯƠI ĐEN TO, chấm sáng trắng nhỏ, viền mắt đen đậm, mí trên dày, ánh mắt kiêu ngạo. KHÔNG mắt phát sáng xanh / mắt trống / mắt trắng. Prompt gửi kèm ảnh phóng mắt concept (`ArtLibrary/monsters/wanted/rimeclaw-tyrant-wolf-ref/eyes-closeup-x4.jpg`).
- Idle / đi: há mõm gầm lộ nanh. Đánh: mắt trợn (vẫn con ngươi đen), há rộng. Gục: mắt nhắm hai vạch cong, vương miện lệch.

5) Màu: lông #F4F8FF / #E4E6F6, băng #8FD3F5 / #4A9FE0, navy #2E3A8C, vàng giáp + vương miện #E8BD79 / #C99545, mắt vàng hổ phách + con ngươi đen, viền nâu đậm mềm.

6) Animation (sheet hiện tại)

|  | Xuống | Lên | Ngang (trái = vẽ riêng, phải = lật gương) |
|---|---|---|---|
| Đi (đứng yên tại chỗ) | thở, ngực phồng xẹp, đầu khẽ gật | lưng, áo choàng, chùm tinh thể, đuôi lay | thở, áo choàng và đuôi lay |
| Đánh | hạ ngực, vồ quét vuốt chân trước, 3 vệt băng nhỏ ở frame 3 | quay lưng, nhấc chân trước vồ ra xa | nhấc chân trước gần camera, quét cung về trước |

7) Vai trò: Boss raid đứng yên, quét vuốt, đập, dậm đất, gầm bão tuyết (4 skill cũ giữ nguyên).

8) **Phân biệt với Frostfang King (boss map Frost Hollow, cũng là vua sói băng):** Rimeclaw to gấp ~1,6 lần trên màn (Wanted 2,5 lần hero, ô 384, so với Frostfang 1,65 x ô 192); Rimeclaw có áo choàng navy + giáp navy viền vàng + đá xanh + chùm tinh thể băng lưng + vương miện VÀNG nhiều chóp đá xanh to; Frostfang King KHÔNG giáp, không áo choàng, không tinh thể lưng, vương miện BĂNG ba chóp mảnh với ngọc nhỏ, bờm tròn kiểu sư tử. Khi vẽ Frostfang King sau này: không chép áo choàng / giáp / tinh thể của Rimeclaw.

9) Boss + 4 hàng Cast (sheet `-cast`, chỉ nhìn về người xem)

| Hàng | Skill / Hình thái | 4 frame |
|---|---|---|
| 0 | Broken Chain Slam (đập) | chồm hai chân sau, giơ vuốt → gồng → giữ đỉnh → đập hai chân trước xuống, vụn băng |
| 1 | Frozen Ground Stomp (dậm) | nén thân → nhấc chân, ngẩng đầu → gầm, tinh thể sáng → dậm, vụn băng quanh chân |
| 2 | Tyrant's Blizzard (cuồng nộ) | ngẩng đầu → dang áo choàng → hơi lạnh tụ → gầm, bông tuyết |
| 3 | Gục | loạng choạng → khuỵu → nằm bẹp → nằm sấp, mắt nhắm |

Skill 1 (Frostclaw Sweep, quạt) dùng sheet đánh. FX skill vẫn theo luật 20/11 (particle / mesh trail / shader), không vẽ trong sprite.

Chốt: sói vua băng bốn chân, vương miện vàng đá xanh, áo choàng + giáp navy viền vàng, chùm tinh thể băng lưng, mắt vàng con ngươi đen.

#### 05. Glacial Colossus — Lv25

Wanted Boss · Ancient Glacier Titan · Stationary Raid

1) Tính cách: Khổng lồ băng cổ xưa, trầm mặc, uy nghi, di chuyển ít nhưng mỗi động tác làm mặt đất rung chuyển; không có vẻ hung ác vô cớ.

2) Silhouette / hình khối

Thiết kế thành cột băng cổ đại có hai cánh tay treo như búa, khác toàn bộ Golem và Yeti.

- Thân chính là một khối băng cao hình tháp cụt, vai không rộng kiểu người.
- Đầu rất nhỏ nằm trong một hốc băng phía trên ngực, như gương mặt cổ xưa được khắc vào sông băng.
- Hai cánh tay cực dài là các khối băng treo dọc hai bên, đầu tay to như búa tảng.
- Không có chân humanoid rõ; chân đế là ba khối băng lớn kết nối thành bệ nứt, gắn cố định vào mặt đất.
- Phía sau có một phiến sông băng cao như sống núi, nghiêng về một bên, tạo silhouette bất đối xứng.
- Thân có các lớp băng ngang lớn như địa tầng, không dùng nhiều tinh thể nhỏ.
- Cao khoảng 400 px, bề ngang 330–390 px khi tay thả; mở tay có thể rộng hơn.

Khác Crystal Golem: Golem có đầu-thân-tay-chân lục giác riêng, đi được; Colossus là khối sông băng liền nền, đầu nhỏ, tay búa treo và thân phân tầng địa chất.

3) Đặc trưng chữ ký

Một khe sáng hình đường nứt dọc từ đầu xuống ngực, màu vàng băng cổ, không phải lõi lục giác hoặc trái tim tinh thể.

Hai tay búa có mặt phẳng cực lớn, chỉ 2–3 cạnh chính để giữ rõ khi thu nhỏ.

4) Mặt + biểu cảm

- Idle: Hai mắt nhỏ như hai khe sáng nằm trong hốc, miệng là vết nứt cong xuống, trầm mặc.
- Attack: Hai khe mắt mở rộng, phần băng quanh mặt dịch chuyển, vết nứt miệng rõ.
- Hit: Khe sáng nhấp nháy, thân rung, vài mảnh băng nhỏ tách khỏi vai rồi biến mất.
- Enrage: Đường nứt dọc sáng hơn, các lớp băng trượt lệch rất nhẹ; không biến thành quái lửa.

5) Màu

#E4F2F2 băng ngoài · #AACFE0 băng giữa · #669AB9 bóng sâu · #8A9DBB lớp cổ · #F2DA9E khe sáng · #52647D viền.

6) Animation 3 hướng

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Thân rung rất chậm, khe sáng đập nhịp, tay treo | Phiến sông băng sau lưng và lớp địa tầng rõ | Tháp băng nghiêng nhẹ, tay gần camera lớn |
| Walk | Không di chuyển; các lớp băng dịch chuyển tại chỗ | Bệ chân cố định, tay dao động nhỏ | Thân xoay vài độ quanh pivot cố định |
| Attack | Nâng một tay búa → đập xuống → thân rung → hồi | Tay giơ và đập về hướng U | Tay gần kéo cao rồi giáng xuống phía phải |

7) Vai trò: Raid boss đứng yên, tấn công cực nặng, phạm vi lớn, telegraph dài. Không chạy, không nhảy, không dùng chuỗi đấm nhanh.

8) Liên kết: Băng sapphire cổ, lớp địa tầng, khe sáng vàng nhạt cùng họ Snow/Frostfang Den nhưng tạo thành ngôn ngữ địa chất sông băng, không phải thú, hiệp sĩ hay Golem.

9) Boss + 4 hàng Cast

| Hàng | Skill / Hình thái | 4 frame niệm chiêu |
|---|---|---|
| 1 | Glacier Hammer — búa băng | Tay nâng → khối búa sáng → giữ thế → giáng |
| 2 | Ice Rift — khe nứt băng | Hai tay ép đất → khe ngực sáng → giữ → phát động |
| 3 | Frozen Pillar — cột băng | Thân nén → hai tay mở → phù văn sáng → triệu hồi |
| 4 | Glacial Quake — địa chấn | Bệ băng rung → thân ngả → hai tay nâng → chấn động |

VFX cột băng, khe nứt và địa chấn nằm sheet riêng. Cast chỉ thể hiện boss niệm chiêu. Không vẽ cột băng xuất hiện che kín người chơi vào sprite cơ bản.

Boss phải có sức nặng như công trình sông băng sống, đối lập với Rimeclaw Tyrant hoạt bát và hung hăng. Hai boss có cùng quy chuẩn raid nhưng khác nhịp animation, tỉ lệ khối và biểu cảm.

Chốt: Tháp sông băng cổ đại đứng cố định, đầu nhỏ trong hốc, hai tay búa treo và khe sáng vàng dọc thân.

### IV. HIGHLAND — SUNBREAK FOOTHILLS

Palette chung: Vàng cát #DCC18A, nâu đá #B68C60, olive #879E70, kem #F2E1B7, xanh gió #8EACB5, viền nâu #755D51.

Motif liên kết: vân đá cát, sừng xoắn, lông/vảy hình nêm, vệt gió và mảng sáng kem. Không biến mọi con thành quái có sừng hoặc giáp đá.

#### 06. Highland Ibex — Lv16

Tên gợi ý: Sunhorn Ibex · Short-range Charger

1) Tính cách: Bướng bỉnh, tự tin, thích đứng trên đá cao và thách thức người khác bằng những cú húc ngắn.

2) Silhouette / hình khối

- Dê núi bốn chân, ngực cao, bụng thon, chân chắc.
- Đầu nhỏ, mõm ngắn, cằm có chòm râu mềm.
- Hai sừng cực lớn cong thành hình chữ C hướng về sau, có 2–3 vòng gân rõ.
- Bốn móng chẻ màu nâu đậm, chân trước hơi cao.
- Cao 168 px, rộng 145–160 px.

Không dùng bờm sói hoặc thân ngang của Boar.

3) Đặc trưng chữ ký

Hai sừng vòng cung C lớn và chòm râu màu kem như giọt nước. Một vệt lông sáng chạy giữa hai mắt xuống mũi.

4) Mặt + biểu cảm

- Đi: Mắt nâu hổ phách hơi hẹp, miệng mím kiêu ngạo.
- Đánh: Mắt nhíu, lông mày hạ, sừng nghiêng về trước.
- Hit: Mắt mở to, tai giật, đầu lắc lệch, râu rung.

5) Màu

#C8A575 thân · #EAD4A5 bụng · #99734F sừng · #F5E9CF mặt · #82976B điểm olive · #604A3B viền.

6) Animation

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Gõ móng, hất cằm | Đuôi ngắn rung, sừng cong hai bên | Hơi ngẩng đầu như thách thức |
| Walk | Bước chắc, đầu nhún nhẹ | Hông dịch luân phiên | Bước nhanh vừa, sừng trễ chuyển động |
| Attack | Hạ đầu → lùi nửa bước → húc ngắn → ngẩng lên | Húc về hướng U, sừng dẫn trước | Dồn chân sau → húc sang phải → dừng |

7) Vai trò: Cận chiến tầm ngắn, cú húc mạnh hơn đòn thường nhưng không lao xuyên map.

8) Liên kết: Sừng cong, màu đất vàng và lông kem đặc trưng Sunbreak. Khác Thorn Boar ở dáng cao, chân thon và hai sừng C.

9) Boss: Không áp dụng.

Chốt: Dê núi dáng cao, hai sừng chữ C khổng lồ, chòm râu kem và mắt kiêu ngạo.

#### 07. Dustscale Lizard — Lv17

Tên gợi ý: Sunskitter Lizard · Fast / Fragile · Scale ×1.15

1) Tính cách: Nhanh nhẹn, tò mò, hay phơi nắng rồi bỏ chạy khi có tiếng động; thích quẫy đuôi trêu đối thủ.

2) Silhouette / hình khối

- Thân cực thấp hình thoi kéo dài, bụng sát đất.
- Đầu tam giác bo rộng, hai mắt tròn nhô nhẹ.
- Bốn chân xòe ngang như thằn lằn thật, ngón chân ba chấu mềm.
- Đuôi rất dài, mảnh ở gốc rồi mở thành chóp lá cát, cong sang một bên.
- Dọc lưng có 4–5 vảy hình mái ngói lớn, không phải gai.
- Cao gốc 120 px, rộng 165–185 px gồm đuôi; scale ×1.15.

3) Đặc trưng chữ ký

Đuôi hình chiếc lá cát và dãy vảy mái ngói dọc lưng, tạo đường silhouette thấp và dài khác mọi quái trước.

4) Mặt + biểu cảm

- Đi: Mắt to long lanh, đồng tử đen, miệng cong tinh nghịch.
- Đánh: Mắt hẹp, miệng há nhỏ, lộ lưỡi hồng trong khoảnh khắc.
- Hit: Mắt xoắn, đầu rụt lại, đuôi cuộn sát thân.

5) Màu

#D8B978 da · #F0DCA8 bụng · #A9855A vảy · #8E9D73 chóp đuôi · #E7A98D miệng · #6B5542 viền.

6) Animation

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Phập phồng bụng, chớp mắt, quẫy đuôi | Vảy lưng rõ, đuôi cong | Nhấc đầu ngó nghiêng |
| Walk | Bốn chân chạy xen kẽ rất nhanh, thân lắc nhẹ | Đuôi quét đối nhịp chân | Thân lướt sát đất, chân đạp ngang |
| Attack | Rụt đầu → phóng mõm cắn nhanh → thu | Cắn về hướng U, đuôi giữ cân bằng | Chồm thấp sang phải, ngoạm rồi lùi |

7) Vai trò: Nhanh, HP thấp, cắn cận chiến. Animation phải cảm giác bò chạy sát đất, không nảy như Rabbit.

8) Liên kết: Vảy vàng cát, điểm olive và dáng thích nghi nền đá. Khác Scorpion/Rock Basilisk ở lượt sau nhờ thân mảnh, vảy mềm và đuôi lá.

9) Boss: Không áp dụng.

Chốt: Thằn lằn thấp dài, chân xòe, vảy mái ngói và đuôi lá cát.

#### 08. Cliff Harpy — Lv18

Tên gợi ý: Sunfeather Harpy · Flying Ranged Feather Shooter

1) Tính cách: Tự tin, hơi kiêu kỳ, thích bay trên cao quan sát rồi bắn lông vũ vào kẻ xâm nhập.

2) Silhouette / hình khối

- Sinh vật chim chibi với đầu lớn tròn, thân nhỏ hình giọt nước.
- Hai cánh rất lớn xòe sang hai bên, tạo silhouette chữ M mềm.
- Không có cánh tay người; đầu cánh có 3–4 lông dài đủ cứng để phóng như phi tiêu.
- Hai chân chim nhỏ có móng cong bo mềm, luôn co khi bay.
- Mào đầu gồm ba lông dài uốn về sau.
- Đuôi chim dạng quạt ba lông, ngắn hơn cánh.
- Cao 150 px, rộng 165–185 px khi mở cánh.

Không thiết kế thành cô gái có cánh hoặc dùng mặt người; giữ cảm giác quái chim fantasy dễ thương.

3) Đặc trưng chữ ký

Một mào ba lông hình ngọn gió và dải lông vàng kem ở mép ngoài cánh. Đầu lông cánh có hình mũi tên mềm, đọc rõ là vũ khí tự nhiên.

4) Mặt + biểu cảm

- Đi/bay: Mắt lớn màu hổ phách, lông mày hơi xếch, mỏ nhỏ cười tự tin.
- Đánh: Mắt sắc, mỏ mở, một cánh đưa về trước như đang ngắm bắn.
- Hit: Mắt nhắm, mào rũ, cánh khép vào thân.

5) Màu

#C9AD82 lông chính · #F1DEB7 ngực · #A57F60 cánh · #E7C36F viền lông · #91A8AC điểm xanh gió · #645044 viền.

6) Animation + hướng đạn bắt buộc

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Bay lơ lửng, cánh vỗ êm, chân co | Thấy lưng và đuôi quạt, không thấy mặt | Cánh trước/sau tách lớp, thân nghiêng nhẹ |
| Walk/Fly | Hai cánh vỗ đối xứng, thân nhô lên xuống | Cánh mở rộng rồi khép, đuôi cân bằng | Cánh trước thấp hơn, thân tiến ngang |
| Attack | Thu cánh → mở cánh phải → phóng lông vũ về xuống màn hình → hồi | Nâng cánh → phóng lông về lên màn hình, xuất phát từ phía trên sprite → hồi | Xoay cánh gần về trước → phóng lông vũ sang phải màn hình → hồi |

Quy chuẩn projectile:

- Hướng D: vector màn hình (0, -1) nếu hệ tọa độ game Y hướng lên; trên ảnh sprite, đạn bay về cạnh dưới.
- Hướng U: vector (0, +1); trên ảnh sprite, đạn bay về cạnh trên.
- Hướng R: vector (+1, 0); đạn bay về cạnh phải.
- Hướng L: flip R và vector (-1, 0).

Đạn sinh tại đầu cánh đang bắn, nhưng hướng bay lấy từ facing của nhân vật, không lấy theo độ nghiêng của chiếc lông. Nếu game dùng hệ trục màn hình khác, ánh xạ lại vector theo hệ đó.

7) Vai trò: Bay, tấn công tầm xa bằng lông vũ. Không lao cắn hoặc dùng phép. Cần giữ cánh đủ thoáng để projectile không bị che.

8) Liên kết: Màu vàng cát, lông gió và xanh xám nhẹ của Highland. Khác Thorn Sprite và Ice Wraith vì có cấu tạo chim, cánh lông và chân chim rõ.

9) Boss: Không áp dụng.

Chốt: Chim Harpy đầu tròn, cánh chữ M lớn, mào ba lông, bắn phi tiêu lông vũ theo facing.

#### 09. Stoneback Guardian — Lv20

Tên gợi ý: Cairnback Guardian · Slowest Highland Tank · Scale ×1.4

1) Tính cách: Hiền lành và kiên nhẫn như một tảng đá; chỉ nổi giận khi bị chạm vào nơi trú ngụ hoặc bị tấn công liên tục.

2) Silhouette / hình khối

Thiết kế sinh vật mai đá sáu chân, không phải Golem hai chân.

- Thân thấp hình lục giác ngang, lưng mang một khối mai đá lớn dạng đồi.
- Sáu chân ngắn, to, xếp ba cặp, mỗi chân như một cột đá bo tròn.
- Đầu nhỏ hình nêm thò ra phía trước dưới mép mai.
- Không có tay hoặc nắm đấm.
- Mai gồm ba lớp đá cát lớn xếp chồng như bậc thềm, không phải tinh thể.
- Cao gốc 166 px, rộng 175–190 px; scale ×1.4 thành khoảng 232 px cao.

3) Đặc trưng chữ ký

Mai đá ba tầng như một ngọn đồi thu nhỏ và sáu chân trụ rõ ràng. Trên đỉnh mai có một mảng rêu olive nhỏ ở khe đá.

4) Mặt + biểu cảm

- Đi: Mắt tròn hiền, miệng ngắn, vẻ ngơ ngác.
- Đánh: Mắt nheo quyết tâm, đầu rụt vào một nhịp rồi bật ra.
- Hit: Mắt nhắm, đầu rụt sâu dưới mai, sáu chân ép xuống.

5) Màu

#C7AE83 đá sáng · #A48B6C đá giữa · #806D5A bóng · #879A70 rêu · #E9D7AE mặt · #655449 viền.

6) Animation

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đầu thò ra, chớp mắt chậm, mai gần như đứng yên | Mai đá ba tầng rõ nhất, sáu chân rất ít động | Đầu nghiêng, một chân trụ dịch nhỏ |
| Walk | Ba cặp chân nhấc lần lượt, mai nảy cực ít | Chân sau-trước luân phiên, thân tiến chậm | Chân trụ đặt xuống nặng, đầu kéo theo |
| Attack | Rụt đầu → dồn thân → đẩy mai/đầu húc ngắn → dừng | Dồn trọng lượng về phía U rồi húc | Nghiêng mai sang phải, đẩy đầu và vai đá vào mục tiêu |

7) Vai trò: Tank chậm nhất Highland, phòng thủ bằng mai và khối lượng. Không nhảy, không đấm, không dùng vũ khí.

8) Liên kết: Đá vàng cát, rêu olive và cấu trúc bậc thềm liên kết vách Sunbreak. Khác Crystal Golem vì sáu chân, mai nằm ngang, đầu nhỏ và không có lõi sáng.

9) Boss: Không áp dụng.

Chốt: Sinh vật mai đá ba tầng sáu chân, đầu nêm nhỏ thò dưới mai, dáng cực thấp và nặng.

#### 10. Ridgehorn Behemoth — Lv20

Boss map Sunbreak · Highland Colossal Beast · Scale ×1.8

1) Tính cách: Chúa tể vách núi, kiêu hãnh, lì lợm và mạnh mẽ; không săn mồi mà bảo vệ đỉnh cao và lãnh thổ.

2) Silhouette / hình khối

Thiết kế quái thú bốn chân khổng lồ dạng bò rừng–tê giác fantasy, nhưng không trùng Ibex hoặc Boar.

- Vai cực cao, lưng dốc xuống về hông, ngực sâu.
- Đầu lớn hình khối nêm, cổ ngắn.
- Một sừng chính dày mọc từ trán, nghiêng về phía trước như mũi núi, không cong C như Ibex.
- Hai gờ sừng phụ cực thấp dọc hai bên má, không tạo bộ ba sừng lớn.
- Bốn chân to như cột, móng hình bản đá bo.
- Một bờm lông ngắn chạy từ gáy xuống vai theo hình các bậc núi, không dùng bờm tròn sói.
- Đuôi ngắn có chùm lông hình nêm.
- Cao gốc 178 px, rộng 175–200 px; scale ×1.8 thành khoảng 320 px.

Silhouette chủ đạo là vai núi cao + sừng đơn hình mũi đá + chân trụ.

3) Đặc trưng chữ ký

Sừng mũi núi lớn có ba vòng vân đá tự nhiên; trên vai là mảng lông xếp tầng như ba rặng núi nhỏ. Một vệt màu kem chạy qua sống mũi.

4) Mặt + biểu cảm

- Đi: Mắt hổ phách hẹp, lông mày dày, mũi lớn, miệng mím đầy tự tin.
- Đánh: Mắt nhíu sắc, mũi phồng, miệng mở gầm lộ hai răng ngắn, sừng hạ xuống.
- Hit: Đầu giật lệch, mắt nhắm, một chân chống mạnh; vẫn giữ cảm giác cực nặng.
- Enrage nếu có: Mắt sáng hơn, bờm vai dựng, hơi thở mạnh; không chuyển thành lửa hoặc đá nóng.

5) Màu

#A78663 thân · #D2B58A lông · #E9D8B5 mặt · #857963 sừng · #89996E điểm olive · #5C483D viền.

6) Animation

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đứng như tượng, hít thở mạnh, gõ một móng | Vai núi cao rõ, đuôi rung nhẹ | Ngẩng đầu khoe sừng, vai nhô theo hơi thở |
| Walk | Bước chậm nặng, vai trồi lên, thân lắc trễ | Chân sau đẩy mạnh, lưng dốc rõ | Sải chân rộng, đầu giữ thấp vừa |
| Attack | Cào đất → cúi đầu → húc mạnh bằng sừng → phanh | Dồn chân sau → húc về hướng U → dừng | Hạ vai → lao húc sang phải → kéo đầu lên |

7) Vai trò: Boss map, húc nặng và kiểm soát khoảng cách gần-trung bình. Đòn có anticipation rõ, cảm giác sức nặng cao hơn Ibex và Thorn Boar.

8) Liên kết: Màu đất vàng, vân đá sừng, bờm vai như rặng núi và điểm olive tạo bản sắc Highland. Không dùng cánh, tinh thể hoặc bộ giáp ma thuật.

9) Boss

- Mặt và sừng là hai focal point.
- Sừng lớn nhưng phải không che mắt ở hướng D.
- Vai cao và ngực dày tạo cảm giác áp đảo ngay cả khi đứng yên.
- Các mảng lông và sừng chỉ dùng 2–3 cấp bóng, không phủ hoa văn vụn.
- Chân trụ và thân dưới ít chi tiết để tránh nhiễu khi raid.
- Độ hoàn thiện như Mire Hydra Queen, nhưng bản sắc là quái thú núi cao, không có đồ trang sức hoàng gia.

Chốt: Behemoth vai núi cao, một sừng đá lớn hướng trước, chân trụ khổng lồ và bờm ba tầng như rặng núi.

### V. KIỂM TRA KHÁC BIỆT SILHOUETTE

| Quái | Silhouette độc quyền | Không được trùng với |
|---|---|---|
| Ice Goblin | Humanoid gù, đầu ngang, mũ băng | Ice Imp |
| Spectral Wolf | Sói nửa thân, hai dải hồn chữ S | Forest Wolf, Snow Fox, hai vua sói |
| Glacier Warden | Hiệp sĩ băng thon, khiên và thương | Crystal Golem |
| Rimeclaw Tyrant | Sói vua bốn chân TO, áo choàng + giáp navy viền vàng, chùm tinh thể băng lưng, vương miện vàng (đổi 20/11) | Frostfang King (sói không giáp, nhỏ), Thornfang Alpha |
| Glacial Colossus | Tháp băng cố định, tay búa treo | Crystal Golem, Yeti, Warden |
| Highland Ibex | Dê núi, sừng đôi chữ C | Thorn Boar |
| Dustscale Lizard | Thằn lằn sát đất, đuôi lá dài | Forest Wolf, Snow Fox |
| Cliff Harpy | Chim cánh chữ M, bắn lông | Thorn Sprite, Ice Wraith |
| Stoneback Guardian | Mai đá ba tầng, sáu chân | Crystal Golem, Bramble Treant |
| Ridgehorn Behemoth | Vai núi cao, sừng đơn hướng trước | Ibex, Boar, Frost Bear |

#### VI. CHECKLIST SẢN XUẤT RIÊNG CHO WANTED

- Pivot cố định: Tâm chân hoặc chân đế không dịch khi Idle, Attack, Cast.
- Cast 4 hàng: Mỗi skill có pose riêng, không chỉ đổi màu cùng một pose.
- Telegraph tách biệt: Vòng đỏ, hình quạt, đường thẳng và vùng an toàn không vẽ vào sprite boss.
- Layer ưu tiên: Mặt và điểm nhận diện luôn nhìn thấy; tay/vuốt có thể tách layer để không che UI và người chơi quá mức.
- Khả năng đọc giữa đông người: Các pose quan trọng phải khác nhau ở silhouette, không chỉ ở glow.
- Enrage: Tăng cường biểu cảm và tương phản, không thay đổi toàn bộ thiết kế.
- Tỉ lệ 2.5× hero: Kiểm tra kích thước cuối trong Unity thay vì chỉ dựa vào pixel sheet.
- Hitbox: Dựa vào phần thân chính và cơ chế server, không mở rộng theo mọi đầu vuốt hoặc phiến băng trang trí.

### VII. KẾT LUẬN LƯỢT 3

10/10 quái có thiết kế khác biệt, và hai Wanted Boss được tách hoàn toàn khỏi ngôn ngữ quái thường.

- Frostfang Den: Goblin vật lý, sói linh hồn và hiệp sĩ băng.
- Wanted: Bạo chúa vuốt rộng và khổng lồ sông băng đứng cố định.
- Highland: Dê núi sừng đôi, thằn lằn thấp, Harpy bắn xa, sinh vật mai đá sáu chân và Behemoth sừng đơn.

ART DIRECTION: CHỐT 10 THIẾT KẾ LƯỢT 3 — tổng 27 thiết kế qua ba lượt, sẵn sàng triển khai sprite theo quy chuẩn đã duyệt.

## 5. Desert (Amber Dunes, Scorpion Gorge): 8 quái

### MONSTER REDESIGN — LƯỢT 4/5

8 quái DESERT · Amber Dunes + Scorpion Gorge · RO Soft Fantasy

Giữ nguyên toàn bộ quy tắc A/B và 27 thiết kế đã chốt trong ba lượt trước. Lượt này tập trung vào 8 silhouette mới hoàn toàn, mang cảm giác sa mạc nắng vàng, hổ phách, cát mềm và hẻm núi đá đỏ.

Định hướng: Desert phải có vẻ đẹp ấm áp, bí ẩn và tinh nghịch, không phải vùng đất chết khô cằn. Quái cấp cao nguy hiểm hơn nhưng vẫn có đôi mắt giàu biểu cảm, cơ thể bo mềm và cá tính riêng.

Ba yêu cầu bắt buộc:

- Không tạo Dune Golem bằng cách đổi màu Crystal Golem hoặc Stoneback Guardian.
- Sand Wraith phải khác Ice Wraith, Spectral Wolf và Amber Wisp cả silhouette lẫn animation.
- Dune Devourer phải là boss sống trong hố cát xoáy, không phải bọ cạp, thằn lằn hay sâu cát thông thường.

### I. QUY CHUẨN KÍCH THƯỚC VÀ KỸ THUẬT

| Quái | Lv | Cao gốc | Scale | Cao hiển thị | Vai trò |
|---|---|---|---|---|---|
| Dune Scarab | 21 | 99 px | ×1.0 | 99 px | Bọ nhanh, thấp |
| Sand Raider | 26 | 145 px | ×1.0 | 145 px | Giáp dày, chậm vừa |
| Amber Wisp | 30 | 110 px | ×1.0 | 110 px | Bay nhanh, cận chiến |
| Giant Scorpion | 26 | 150 px | ×1.0 | 150 px | Cận chiến chuẩn |
| Sand Wraith | 27 | 160 px | ×1.0 | 160 px | Bay, ném cầu cát xa |
| Rock Basilisk | 28 | 130 px | ×1.0 | 130 px | Thằn lằn đá nhanh |
| Dune Golem | 29 | 179 px | ×1.3 | ≈233 px | Tank chậm |
| Dune Devourer | 30 | 151 px | ×1.9 | ≈287 px | Boss hố cát |

#### Quy tắc lật sprite bắt buộc

- Hàng D = hướng xuống, U = hướng lên, R = hướng phải.
- Hướng L = lật ngang toàn bộ frame R, không vẽ riêng.
- Vũ khí cầm lệch bên sẽ đổi bên khi lật; do đó ưu tiên vũ khí hai bên đối xứng hoặc cầm chính giữa.
- Số lượng vũ khí là cố định xuyên suốt Idle / Walk / Attack.
- Không để vũ khí tự đổi từ dao sang giáo, tay không sang kiếm, hoặc một vũ khí thành hai.
- Các chi tiết bất đối xứng như vết nứt, sẹo, hoa văn có thể đảo bên khi lật. Không dùng chúng làm điều kiện logic gameplay.
- Với projectile, hướng bắn theo facing; không phụ thuộc bên cánh tay đang cầm hoặc vị trí vũ khí sau khi lật.

Quy ước vũ khí chốt: Scarab 0, Raider 1, Amber Wisp 0, Scorpion 3 vũ khí tự nhiên (2 càng + 1 ngòi đuôi), Sand Wraith 0, Basilisk 0, Golem 0, Devourer 0.

### II. AMBER DUNES — SA MẠC HỔ PHÁCH

Ngôn ngữ chung: Bọ cánh cứng tròn, kẻ lữ hành nhỏ, tinh linh ánh sáng hổ phách. Ba silhouette khác nhau rõ: vòm thấp / người mặc giáp / ngọn đèn bay.

Palette nền:

| Màu | Hex | Công dụng |
|---|---|---|
| Cát vàng | #EBC77D | Màu biome chính |
| Mật ong | #C9965D | Bóng và vật liệu cứng |
| Đất đỏ | #AC7253 | Giáp, hoa văn |
| Kem cát | #F6E4B7 | Highlight |
| Xanh xám | #8D9B82 | Màu phụ |
| Nâu viền | #79534C | Outline |

#### 01. Dune Scarab — Lv21

Tên gợi ý: Pebble Scarab · Fast Low-profile Bug

##### 1) Tính cách

Chăm chỉ, hiếu động, hơi tham lam; thích lăn những viên sỏi nhỏ và chạy trốn cực nhanh khi bị quấy rầy.

##### 2) Silhouette / hình khối

Thiết kế bọ mai vòm nửa quả trứng, sáu chân như mái chèo, không giống slime hoặc Stoneback Guardian.

- Thân rất thấp, mai hình bán cầu dẹt, rộng hơn cao.
- Đầu nhỏ hình viên sỏi nằm ngay dưới mép trước của mai.
- Sáu chân ngắn, ba cặp, xòe sang hai bên; đầu chân cong như mái chèo cát.
- Hai râu nhỏ hình thìa cong ra trước.
- Mai có đường chia giữa rõ thành hai cánh vỏ, mỗi bên một mảng lớn.
- Không có sừng dài, càng lớn hoặc gai.
- Cao 99 px, rộng 115–130 px.

Silhouette: Một chiếc mai tròn thấp đang chạy bằng sáu chân xòe.

##### 3) Đặc trưng chữ ký

Hai đốm hổ phách hình giọt nước nằm đối xứng trên mai và đôi râu hình thìa. Đường chia cánh vỏ tạo hình chữ Y ngược khi nhìn từ trên xuống.

Vũ khí cố định: 0. Tấn công bằng đầu và mép mai.

##### 4) Mặt + biểu cảm

- Đi: Hai mắt tròn như hạt đậu, long lanh màu nâu sẫm; miệng nhỏ cười ω.
- Đánh: Mắt nhíu quyết tâm, miệng mím, râu hướng ra trước.
- Trúng đòn: Mắt nhắm ><, đầu rụt dưới mai, sáu chân co vào.

##### 5) Bảng màu

#E9C375

#C99058

#A76F4D

#F8E5AD

#8C9A78

#6B4E3E

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Râu rung, đầu nghiêng, chân trước cào cát | Mai hai cánh rõ, sáu chân gần như đứng yên | Đầu thò ra, râu trước quét nhẹ |
| Walk | Ba cặp chân chạy xen kẽ cực nhanh, mai nảy rất ít | Chân sau đẩy, mai dịch đều | Chân mái chèo đạp liên tục, thân lướt sát đất |
| Attack | Rụt đầu → lao húc nhanh → chạm → bật về | Mai nghiêng về hướng U, đầu húc ngắn | Đầu bật sang phải, hai chân trước đạp mạnh rồi thu |

##### 7) Vai trò chiến đấu

Bọ nhanh, thấp, máu không cao; cận chiến bằng cú húc ngắn. Không lăn cả cơ thể như quả bóng vì cần giữ rõ sáu chân và mặt.

##### 8) Liên kết cùng map

Mai vàng mật ong, đốm hổ phách và chân mái chèo phù hợp với cát mềm Amber Dunes. Đường vân vỏ liên kết hoa văn sa mạc mà không biến thành giáp kim loại.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Bọ sáu chân cực thấp, mai vòm chia đôi, hai đốm hổ phách và râu hình thìa.

#### 02. Sand Raider — Lv26

Tên gợi ý: Dune Bandit · Armored Melee Raider

##### 1) Tính cách

Một tên cướp sa mạc lém lỉnh, thích khoe chiến lợi phẩm, hay làm bộ đáng sợ nhưng thực chất hơi vụng về.

##### 2) Silhouette / hình khối

Thiết kế kẻ cướp chibi dáng tam giác đứng, không giống Ice Goblin hay Ice Imp.

- Đầu lớn nằm trong khăn trùm cát, khăn tạo hình tam giác bo tròn phía trên.
- Mặt lộ ra trong một khoảng oval rộng, không bị che kín.
- Thân hình thang rộng phía dưới do áo giáp tấm cát và thắt lưng lớn.
- Hai tay ngắn chắc, có găng da; chân ngắn mang giày sa mạc.
- Giáp gồm 3–4 mảng lớn như mai giáp vải cứng, không dùng nhiều đinh tán.
- Một khăn choàng ngắn sau lưng chia hai vạt cân đối.
- Cao 145 px, rộng 100–115 px.

Silhouette: Đầu khăn tam giác + thân giáp hình thang + một vũ khí ở giữa.

##### 3) Đặc trưng chữ ký

Một cây chùy gỗ đầu tròn bọc đồng hổ phách, cầm bằng cả hai tay trước ngực.

- Vũ khí cố định: đúng 1 cây chùy.
- Không dùng dao, kiếm, rìu, cung hoặc hai vũ khí.
- Chùy có cán ngắn, đầu hình củ hành bo tròn, 3 gờ lớn.
- Cả Idle, Walk và Attack đều giữ cây chùy này.
- Khi lật R sang L, chùy vẫn nằm ở trục giữa cơ thể.

Phụ kiện phụ là chiếc khăn trùm có nếp gấp hình xoắn cát ở trán.

##### 4) Mặt + biểu cảm

- Đi: Mắt to màu nâu hổ phách, một lông mày nhướng, cười nhếch hơi kiêu ngạo.
- Đánh: Mắt nhíu, miệng hô lớn, hai tay siết cán chùy.
- Trúng đòn: Mắt xoắn, miệng méo, đầu lắc, khăn trùm lệch nhẹ.

##### 5) Bảng màu

#D8B57B

#A97550

#EAD6AC

#8C9A82

#C58F63

#654D41

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Hai tay giữ chùy giữa ngực, đầu chùy hơi nghiêng, mắt đảo | Thấy khăn sau và hai vạt áo; chùy giữ phía trước thân, có thể bị thân che một phần | Chùy nằm theo trục trước ngực, hai tay ôm cán |
| Walk | Bước lạch bạch, giáp nảy nhẹ, chùy giữ ổn định | Hai chân bước luân phiên, áo choàng rung | Thân nghiêng khi bước, chùy vẫn giữ giữa hai tay |
| Attack | Kéo chùy lên giữa trán → bổ xuống chính diện → chạm → thu | Nâng chùy qua vai và bổ về phía U | Xoay cả thân và bổ chùy sang phải bằng hai tay |

Không đổi kiểu vũ khí giữa các frame.

##### 7) Vai trò chiến đấu

Cận chiến giáp dày, chậm vừa, đánh bằng một cây chùy. Anticipation rõ, lực đánh nặng nhưng không nhanh như Goblin.

##### 8) Liên kết cùng map

Khăn cát, giáp hổ phách, đồng nâu và điểm xanh xám liên kết với Amber Dunes. Vẫn giữ mặt dễ thương, không thành chiến binh người trưởng thành quá thực tế.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Tên cướp chibi khăn tam giác, giáp hình thang, hai tay cầm đúng một chùy đầu tròn giữa ngực.

#### 03. Amber Wisp — Lv30

Tên gợi ý: Honeyglow Wisp · Fast Flying Melee

##### 1) Tính cách

Tinh linh hổ phách tinh nghịch, thích tò mò bay tới gần người chơi và chạm vào họ như đang chơi đùa.

##### 2) Silhouette / hình khối

Thiết kế ngọn đèn hổ phách bay hình con thoi tròn, không giống Ice Wraith dạng chuông hoặc Thorn Sprite có cánh.

- Thân chính là khối hổ phách hình thoi bo mạnh, tròn ở giữa và thu ở hai đầu.
- Không có đầu tách riêng; mặt nằm trên thân.
- Bốn mảnh hổ phách nhỏ nằm quanh thân theo hình chữ X, như bốn cánh hoa cứng, không phải cánh bay.
- Phía dưới là một chóp sáng ngắn hình giọt, không có chân hoặc đuôi dài.
- Khối thân lơ lửng và xoay nhẹ khi di chuyển.
- Cao 110 px, rộng 100–115 px.

Silhouette: Viên hổ phách nổi giữa bốn mảnh vệ tinh, hoàn toàn không có tay, chân hoặc cánh.

##### 3) Đặc trưng chữ ký

Bốn mảnh hổ phách vệ tinh đối xứng quanh thân và một lõi sáng hình ngôi sao bốn cánh mềm ở giữa bụng.

Vũ khí cố định: 0. Các mảnh vệ tinh là bộ phận cơ thể, không phải dao hay phi tiêu.

##### 4) Mặt + biểu cảm

- Đi/bay: Hai mắt tròn tím nâu, má vàng sáng, miệng cười nhỏ.
- Đánh: Mắt nheo, miệng o, bốn mảnh vệ tinh ép gần thân trước cú va chạm.
- Trúng đòn: Mắt nhắm, lõi sáng co lại, bốn mảnh rung lệch rồi trở về.

##### 5) Bảng màu

#F4D78A

#E9AF58

#C98743

#FFF0BC

#A88B88

#785345

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Lơ lửng 3–5 px, mảnh vệ tinh xoay góc nhỏ, mắt chớp | Thấy mặt sau hổ phách và lõi mờ, không có mắt | Khối thân nghiêng, vệ tinh trước/sau tách lớp |
| Walk/Fly | Bay nhanh theo nhịp tiến-thu, bốn mảnh xòe ra | Thân lướt về U, vệ tinh kéo trễ | Thân nghiêng sang phải, các mảnh co giãn đối xứng |
| Attack | Bốn mảnh khép → lõi sáng → thân lao chạm → bật về | Gom mảnh rồi lao về U | Khối thân kéo dài nhẹ sang phải → va chạm → trở về |

##### 7) Vai trò chiến đấu

Bay nhanh, cận chiến bằng va chạm năng lượng hổ phách. Không bắn projectile, không ném mảnh vệ tinh. VFX là tia sáng va chạm rất ngắn, tách riêng sprite.

##### 8) Liên kết cùng map

Màu hổ phách, ánh vàng mật ong, mảnh hình thoi liên kết cát và khoáng vật Amber Dunes. Khác Ruin Wisp tương lai vì cấu trúc vệ tinh hình X và tính cách vui vẻ.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Viên hổ phách biết cười với bốn mảnh vệ tinh hình X, bay bằng cách co-xòe cơ thể.

### III. SCORPION GORGE — HẺM NÚI BỌ CẠP

Scorpion Gorge đậm hơn Amber Dunes: tăng nâu đỏ, đá cát và hoa văn phong hóa.

Ba motif phụ: Vòng xoắn cát, phiến đá đỏ, đường vân hổ phách. Không dùng màu đỏ lửa hoặc tím độc neon.

#### 04. Giant Scorpion — Lv26

Tên gợi ý: Amberclaw Scorpion · Standard Melee

##### 1) Tính cách

Cứng đầu, hơi hung hăng, thích dọa đối thủ bằng cách giơ càng, nhưng lúc nghỉ lại giống một con cua nhỏ đang ngáp.

##### 2) Silhouette / hình khối

Thiết kế bọ cạp dạng chiếc neo ba chấu, không dùng hình mai vòm của Scarab.

- Thân trung tâm thấp hình hạt hạnh nhân, phần đầu nhỏ, bụng phân đốt.
- Hai càng lớn đối xứng mở sang hai bên, tạo hình chữ U rộng.
- Mỗi càng có hai ngón kẹp dày, đầu bo tròn.
- Tám chân mảnh chia bốn cặp, gập thành các góc lớn rõ.
- Đuôi gồm bốn đốt cong cao qua lưng, kết thúc bằng một ngòi cong hình giọt.
- Cao khoảng 150 px gồm đuôi, rộng 165–185 px tính cả càng.

Silhouette: Hai càng chữ U + đuôi cong cao ở giữa; khác Scarab hoàn toàn.

##### 3) Đặc trưng chữ ký

Hai càng có đốm hổ phách lớn đối xứng và đuôi cong hình dấu hỏi ngược, ngòi cuối màu kem cát.

Vũ khí tự nhiên cố định: 2 càng + 1 ngòi đuôi, tổng ba bộ phận tấn công. Không có vũ khí cầm ngoài.

##### 4) Mặt + biểu cảm

- Đi: Mắt nhỏ tròn trên hai cuống ngắn, nhìn hai bên, miệng cong như đang hừ.
- Đánh: Mắt nhíu, càng mở rộng, miệng nhe một răng nhỏ.
- Trúng đòn: Mắt nhắm, cuống mắt co, hai càng khép sát thân, đuôi cụp.

##### 5) Bảng màu

#C68D60

#E5B77A

#A7654C

#F4D8A7

#8B806D

#694C42

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Hai càng giơ nhẹ, cuống mắt xoay, đuôi nhấp nhô | Đuôi cong nổi bật, các đốt lưng rõ | Càng gần camera mở/khép nhỏ, đuôi giữ cao |
| Walk | Tám chân chạy lệch pha, thân tiến thấp, càng cân bằng | Chân sau đẩy theo cặp, đuôi rung nhẹ | Thân trượt ngang bằng bước chân nhanh, càng mở vừa |
| Attack | Hai càng mở → kẹp đồng thời → đuôi hạ đe dọa → thu | Càng đưa về U, đuôi cong cao | Càng gần kẹp sang phải, càng xa hỗ trợ; đuôi giữ thăng bằng |

Đòn cơ bản là kẹp càng. Ngòi đuôi hiện diện cố định và có thể dành cho skill riêng sau này; không cần đâm đuôi trong mỗi basic attack.

##### 7) Vai trò chiến đấu

Cận chiến chuẩn, tốc độ trung bình, tầm đánh rộng hơn Scarab do hai càng. Không quá chậm hoặc quá nặng.

##### 8) Liên kết cùng map

Giáp đỏ cát, đốm hổ phách và thân phân đốt phù hợp Scorpion Gorge. Đối xứng hai càng giúp lật sprite không phát sinh sai số.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Bọ cạp thân hạnh nhân, hai càng chữ U lớn và đuôi bốn đốt cong cao.

#### 05. Sand Wraith — Lv27

Tên gợi ý: Dustveil Wraith · Flying Ranged Caster

##### 1) Tính cách

Lặng lẽ, tinh quái, thích giả làm một cơn gió cát; thường lơ lửng quan sát rồi ném cầu cát bất ngờ.

##### 2) Silhouette / hình khối

Thiết kế cơn lốc cát hình con quay có mặt, không giống Ice Wraith hình chuông có dải sương.

- Đầu là một khối cát nhỏ hình tròn méo, nằm ở đỉnh thân.
- Thân là ba vòng cát dày xếp theo trục xoắn, thu nhỏ dần xuống dưới.
- Hai tay là hai luồng cát ngắn hình dấu ngoặc, không có ngón tay riêng.
- Đáy là một điểm xoáy nhỏ, không có chân hoặc đuôi.
- Có một vành khăn cát tự nhiên xoay ngang quanh cổ, không phải khăn vải thật.
- Cao 160 px, rộng 115–130 px.

Silhouette: Con quay xoắn ba tầng, đáy nhọn mềm, hai tay cát ngắn.

##### 3) Đặc trưng chữ ký

Ba vòng xoắn cát lớn và hai bàn tay gió hình dấu ngoặc. Khi niệm đòn, một viên cầu cát tròn xuất hiện giữa hai tay.

Vũ khí cố định: 0. Cầu cát là projectile được sinh ra khi Attack, không phải trang bị.

##### 4) Mặt + biểu cảm

- Đi/bay: Mắt vàng nâu như hai hạt cát sáng, miệng cười bí hiểm, lông mày cong.
- Đánh: Mắt nheo, miệng o, hai tay nâng sát nhau tạo cầu cát.
- Trúng đòn: Mắt nhắm, vòng xoắn méo và thân bị nén ngắn trong một nhịp.

##### 5) Bảng màu

#E8C992

#C69B65

#A97654

#F3E2B9

#9D9A85

#71584B

##### 6) Animation 3/4 và hướng projectile

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Ba vòng xoắn chuyển động chậm, mắt chớp, thân bay thấp | Vòng cát phía sau rõ, không có mắt | Vòng xoắn lệch góc, tay trước nổi bật |
| Walk/Fly | Thân xoắn nhanh hơn, đáy kéo dài khi tiến | Vòng xoắn nén-thả, bay về U | Thân nghiêng phải, vòng dưới kéo trễ |
| Attack | Hai tay gom cầu → nâng giữa ngực → ném xuống màn hình → thu | Tạo cầu trước thân theo hướng U → ném lên màn hình → thu | Gom cầu ở trước ngực → xoay thân → ném sang phải màn hình → thu |

Hướng đạn bắt buộc:

| Facing | Vector màn hình (X phải, Y lên) | Vị trí xuất phát |
|---|---|---|
| D | (0, -1) | Chính giữa, phía trước ngực theo hướng xuống |
| U | (0, +1) | Chính giữa, phía trước thân theo hướng lên |
| R | (+1, 0) | Chính giữa trước thân, phía phải |
| L (flip R) | (-1, 0) | Mirror vị trí xuất phát hướng R |

Cầu cát bay thẳng theo facing, không tự đổi hướng khi animation kết thúc. Nếu gameplay dùng homing, cơ chế đó phải điều khiển ở projectile script, không sửa sprite.

##### 7) Vai trò chiến đấu

Bay, tấn công xa bằng cầu cát. Không quét cận chiến. Projectile riêng có hình cầu cát compact, viền nâu mềm, 2–3 mảng sáng tối; không dùng quả cầu lửa.

##### 8) Liên kết cùng map

Ba vòng xoắn cát, màu nâu hổ phách và vành cát phù hợp hẻm núi. Khác Amber Wisp khối hổ phách cứng có vệ tinh, Ice Wraith có vầng băng khuyết và dải đuôi.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Hồn cát hình con quay ba tầng, hai tay gió, ném cầu cát từ giữa ngực theo facing.

#### 06. Rock Basilisk — Lv28

Tên gợi ý: Pebblecrest Basilisk · Fast Stone Lizard

##### 1) Tính cách

Lì lợm, hiếu thắng, có vẻ luôn cau có; thường chạy bất ngờ rồi đứng im như một tảng đá để đánh lừa đối thủ.

##### 2) Silhouette / hình khối

Thiết kế thằn lằn đá hình cá sấu lùn, khác Dustscale Lizard thân mảnh và đuôi lá.

- Thân ngắn, bè ngang, bụng thấp nhưng ngực rộng.
- Đầu cực lớn hình nêm đá, chiếm khoảng 40% chiều dài thân không tính đuôi.
- Mõm rộng, hàm dưới lớn, hai mắt nằm cao ở góc đầu.
- Bốn chân ngắn khỏe đặt dưới thân, không xòe ngang như Dustscale Lizard.
- Trên lưng là ba phiến đá lớn xếp kiểu sống lưng khủng long, đầu bo.
- Đuôi ngắn, dày, hình chùy đá tròn ở cuối.
- Cao 130 px, rộng 155–170 px.

Silhouette: Đầu nêm lớn + ba phiến sống lưng + đuôi chùy ngắn.

##### 3) Đặc trưng chữ ký

Ba phiến đá sống lưng hình mái núi và chiếc đuôi chùy đá bo tròn.

Không có vảy mái ngói nhỏ, đuôi lá dài hoặc thân mảnh của Dustscale Lizard.

Vũ khí tự nhiên: hàm cắn và một đuôi chùy, không mang vũ khí cầm.

##### 4) Mặt + biểu cảm

- Đi: Mắt nhỏ màu vàng mật ong, mí nặng, miệng rộng mím như đang cáu.
- Đánh: Mắt nheo, hàm mở rộng lộ 4 răng ngắn, đầu hạ thấp.
- Trúng đòn: Mắt nhắm, hàm dưới lệch nhẹ, đầu rụt vào vai, đuôi co lại.

##### 5) Bảng màu

#B9A181

#8E7C66

#D8C4A0

#A16F59

#8B9A78

#645348

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đứng im như đá, mắt đảo, đuôi gõ đất nhẹ | Ba phiến lưng rõ, thân gần như bất động | Hàm nhấp nhô nhẹ, đuôi nghiêng |
| Walk | Chạy bước ngắn nhưng mạnh, đầu giữ thấp | Chân luân phiên nhanh, lưng ít lắc | Chạy thấp với hai chân trước đẩy, đuôi chùy cân bằng |
| Attack | Hạ đầu → ngoạm nhanh → giữ một nhịp → nhả | Xoay đầu cắn về U | Hàm bật sang phải, đầu lùi lại ngay sau cú cắn |

##### 7) Vai trò chiến đấu

Nhanh, cận chiến, sức chịu đòn vừa phải. Đòn cơ bản bằng hàm; đuôi chùy là đặc trưng silhouette, có thể dùng trong skill riêng nhưng không cần đánh liên tục.

##### 8) Liên kết cùng map

Đá cát, nâu đỏ, olive rêu và phiến sống lưng như vách Gorge. Khác Dustscale Lizard ở đầu lớn, thân ngắn, chân khỏe và đuôi chùy.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Thằn lằn đá đầu nêm rộng, ba phiến sống lưng, đuôi chùy tròn và vẻ mặt cau có.

#### 07. Dune Golem — Lv29

Tên gợi ý: Sandstone Sentinel · Slow Tank · Scale ×1.3

##### 1) Tính cách

Trầm tính, hơi ngốc nghếch, đi đâu cũng như đang mang cả một tảng núi cát; chỉ tấn công khi có người bước quá gần.

##### 2) Silhouette / hình khối

Thiết kế thân cát hình cối xay cổ, hai cánh tay dạng cột đá xoay, khác Crystal Golem dạng pháo đài và Stoneback Guardian dạng mai sáu chân.

- Thân chính hình trụ cát rộng, thu nhỏ nhẹ ở eo, không có cổ.
- Đầu là một khối đá tròn thấp đặt chìm vào vai, mắt nằm sâu dưới một đường gờ đá.
- Hai cánh tay dài dạng trụ tròn, có khớp xoay hình vòng đá, bàn tay ba ngón dày.
- Chân là hai cột đá ngắn, bàn chân hình đế vuông bo.
- Quanh bụng có một vòng đá sa thạch nổi như đai cối xay, tạo silhouette đặc trưng.
- Trên vai có 2–3 mảng đá cát xếp chồng nhưng không tạo giáp hộp.
- Cao gốc 179 px, rộng 155–170 px; scale ×1.3 thành khoảng 233 px.

Khác Crystal Golem: Thân trụ, tay trụ và đai đá tròn, không có lõi lục giác hoặc phiến tinh thể.
Khác Stoneback Guardian: Hai chân và hai tay rõ, không có mai sáu chân.

##### 3) Đặc trưng chữ ký

Vòng đai đá cối xay quanh bụng, có ba hoa văn hình nêm lớn, và hai khớp vai hình vòng tròn.

Vũ khí cố định: 0. Đánh bằng cánh tay đá tự nhiên.

##### 4) Mặt + biểu cảm

- Đi: Hai mắt tròn nhỏ màu kem, miệng ngắn hơi ngơ ngác, lông mày là gờ đá thẳng.
- Đánh: Mắt hẹp, gờ đá nghiêng, miệng mở thành khe ngắn, hai tay nâng.
- Trúng đòn: Mắt nhắm, đầu lùi nhẹ, đai đá rung, một ít cát rơi khỏi khớp.

##### 5) Bảng màu

#D3B88A

#AE8E69

#8E715A

#EAD8B0

#9B9F83

#665244

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đứng nặng, đai đá xoay rất nhẹ, cát rơi vài hạt | Vai và vòng đá lưng rõ, chân cố định | Một tay trụ hạ thấp, đầu xoay nhỏ |
| Walk | Nhấc chân đá trái → đặt xuống → nhấc chân phải → đặt xuống | Hai chân bước nặng, đai đá lắc trễ | Thân nghiêng nhẹ, cánh tay trụ đung đưa rất chậm |
| Attack | Hai tay kéo về → đấm/đập đồng thời xuống trước → giữ → thu | Xoay vai đưa hai tay đập về U | Một tay trụ quét sang phải, tay kia giữ thăng bằng |

##### 7) Vai trò chiến đấu

Tank chậm, giáp dày, tấn công cận chiến bằng tay đá. Không dùng kỹ năng xoay toàn thân như lốc cát trong basic attack.

##### 8) Liên kết cùng map

Sa thạch, đai đá cối xay, hoa văn nêm cổ và cát rơi tại khớp giúp liên kết Scorpion Gorge. Không thêm pha lê, rêu dày hoặc vương miện.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Golem sa thạch thân trụ, hai tay cột tròn, đai đá cối xay quanh bụng và mặt ngơ ngác.

### IV. BOSS SCORPION GORGE

#### 08. Dune Devourer — Lv30

Boss map · Sandpit Sovereign · Scale ×1.9

##### 1) Tính cách

Một sinh vật cổ đại sống dưới hố cát, tham ăn và kiêu ngạo; thường giả vờ ngủ, rồi bất ngờ mở miệng khổng lồ để nuốt mọi thứ đến gần.

##### 2) Silhouette / hình khối

Thiết kế đóa hoa cát ăn thịt khổng lồ mọc giữa xoáy cát, không dùng cơ thể sâu, bọ cạp hay thằn lằn.

- Thân chính là miệng tròn cực lớn hướng lên, viền miệng chia thành năm thùy cát đá dày như cánh hoa.
- Bên trong miệng có ba vòng hàm mềm bằng đá cát, mỗi vòng có các răng tù ngắn; không dùng răng kim nhọn dày đặc.
- Ở phía trước có hai mắt lớn nằm trên hai gờ cát riêng, hơi nổi lên khỏi mép miệng, giúp boss có biểu cảm.
- Phía ngoài miệng là một lớp vành thân hình bát, hòa vào hố cát.
- Có bốn xúc tu cát ngắn, dày, mọc đối xứng quanh viền; đầu xúc tu hình thìa, không phải dây dài.
- Không có chân, không có đuôi, không có thân humanoid.
- Cao thiết kế 151 px tính từ đáy vành đến đỉnh mắt/thùy miệng; scale ×1.9 thành khoảng 287 px.
- Bề ngang boss hiển thị có thể 380–440 px tùy mức mở miệng; cần cell boss đủ rộng và tách hiệu ứng hố cát.

Silhouette: Một đóa hoa cát năm thùy có miệng lớn và hai mắt nổi, nằm cố định giữa hố xoáy.

##### 3) Đặc trưng chữ ký

Miệng năm thùy hình hoa cát và hai mắt nổi trên gờ cát, kết hợp bốn xúc tu thìa đối xứng.

Vũ khí tự nhiên cố định: 4 xúc tu + hàm miệng, không có vũ khí cầm.

Hố cát xoáy dưới boss là môi trường/VFX riêng, không vẽ cố định vào toàn bộ sprite. Boss có vành thân rõ để không biến mất khi tắt hiệu ứng.

##### 4) Mặt + biểu cảm

- Đi/Idle: Hai mắt hổ phách nửa nhắm, lông mày cong đầy tự mãn, miệng khép một nửa như đang ngủ.
- Đánh: Mắt mở lớn, lông mày nhíu, năm thùy miệng xòe rộng, răng tù lộ rõ.
- Trúng đòn: Mắt nhắm lệch, thùy miệng co lại, xúc tu rụt vào vành thân.
- Enrage: Mắt sáng vàng hơn, miệng mở rộng thêm, các thùy nâng cao; không đổi thành quái lửa hoặc kinh dị.

##### 5) Bảng màu

#E5C28A

#C58F61

#A76E53

#F3DBA8

#8D9A7E

#6C4F44

Thân cát vàng mật ong, thùy ngoài nâu đỏ, mép trong kem sáng, mắt hổ phách và vài mảng olive xám giúp cân bằng.

##### 6) Animation 3 hướng

Boss đứng cố định trong hố, không có Walk dịch chuyển. Hướng D/U/R vẫn phải có tạo hình tương ứng nếu gameplay cho phép boss đổi facing.

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Miệng mở hé, mắt nhìn trước, năm thùy nhấp nhô chậm | Nhìn từ sau, thấy vành thân và mặt sau thùy; không thấy mắt | Một bên vành miệng lớn hơn do phối cảnh, mắt gần camera nổi bật |
| Walk | Không di chuyển; thân nhún và xoay nhỏ tại chỗ | Vành thân giữ pivot, thùy rung nhẹ | Thân nghiêng về phải nhưng tâm hố không đổi |
| Attack | Miệng khép → mở xòe năm thùy → hút/cắn → đóng bớt | Thùy sau nâng, miệng mở hướng U | Miệng xòe lệch về phải, hai xúc tu bên phải quật ngắn rồi thu |

Quan trọng: Attack không được làm hố cát dịch chuyển. Pivot luôn nằm tại tâm hố.

##### 7) Vai trò chiến đấu

Boss map cố định hoặc gần như cố định ở giữa hố cát xoáy, thiên về hút, cắn diện rộng và đánh bằng xúc tu.

Đề xuất các hình thái attack có thể tái sử dụng:

- Miệng mở rộng hút mục tiêu ở gần.
- Hai xúc tu phía trước đập vào vùng cận chiến.
- Thùy miệng khép mạnh tạo cú cắn vòng tròn.
- Hố cát xoáy tăng tốc và tạo hiệu ứng kéo.

Các hiệu ứng hút và vòng xoáy phải do gameplay/VFX điều khiển, không vẽ toàn bộ vào sprite.

##### 8) Liên kết Scorpion Gorge

- Thân cát vàng, vành đá nâu đỏ, hoa văn hổ phách.
- Hình vòng xoắn liên kết Sand Wraith nhưng cấu trúc sinh học hoàn toàn khác.
- Thùy miệng có đường vân giống đá phong hóa; không dùng lá xanh vì đây không phải cây.
- Màu kem phía trong miệng giúp boss dễ đọc trên nền hố cát tối hơn.

##### 9) Quy tắc Boss — uy nghi nhưng vẫn dễ thương

Dune Devourer phải có cảm giác một con quái cổ đại khổng lồ tham ăn, chứ không phải miệng quỷ kinh dị.

- Focal point 1: Hai mắt biểu cảm nổi trên gờ cát.
- Focal point 2: Miệng năm thùy mở rộng.
- Focal point 3: Bốn xúc tu thìa và vành thân.
- Răng tù, ít, to; không làm hàng trăm răng nhọn.
- Miệng bên trong tối hơn nhưng không đen tuyệt đối, không dùng đỏ máu.
- Khi idle, boss nhìn có vẻ lười biếng và đáng yêu; khi attack, hình miệng lớn tạo sự nguy hiểm.
- Các thùy và xúc tu phải được vẽ ở độ phân giải đủ cao để vẫn rõ khi scale ×1.9.
- Chất lượng vật liệu, biểu cảm và phân tầng khối phải tương đương Mire Hydra Queen nhưng không mô phỏng cấu trúc Hydra nhiều đầu.

Chốt nhận diện: Boss miệng hoa cát năm thùy, hai mắt nổi tinh quái, bốn xúc tu thìa, ngồi giữa hố cát xoáy.

### V. KIỂM TRA SILHOUETTE CỦA 8 QUÁI

| Quái | Silhouette riêng | Khác với quái cũ |
|---|---|---|
| Dune Scarab | Mai vòm dẹt, sáu chân mái chèo | Không giống Slime/Stoneback |
| Sand Raider | Humanoid khăn tam giác, chùy giữa ngực | Không giống Goblin/Imp |
| Amber Wisp | Hổ phách hình thoi, bốn vệ tinh X | Không giống Wraith/Sprite |
| Giant Scorpion | Hai càng U, đuôi cong cao | Không giống Scarab |
| Sand Wraith | Con quay ba vòng cát | Không giống Ice Wraith |
| Rock Basilisk | Đầu nêm rộng, đuôi chùy, ba phiến đá | Không giống Dustscale Lizard |
| Dune Golem | Thân trụ, tay cột, đai cối xay | Không giống Crystal Golem/Stoneback |
| Dune Devourer | Miệng hoa cát năm thùy trong hố xoáy | Không giống bất kỳ boss trước |

### VI. KIỂM TRA VŨ KHÍ VÀ FLIP

| Quái | Số vũ khí | Vị trí / tính đối xứng | Khi flip R → L |
|---|---|---|---|
| Dune Scarab | 0 | Đầu và mai | Không đổi cấu trúc |
| Sand Raider | 1 | Chùy giữa hai tay | Giữ một chùy ở trục giữa |
| Amber Wisp | 0 | Bốn mảnh cơ thể đối xứng | Giữ đủ bốn mảnh |
| Giant Scorpion | 3 tự nhiên | Hai càng đối xứng + ngòi giữa | Giữ hai càng, một ngòi |
| Sand Wraith | 0 | Cầu cát sinh ở giữa | Đảo vector đạn |
| Rock Basilisk | 0 cầm tay | Hàm giữa + một đuôi | Giữ giải phẫu |
| Dune Golem | 0 | Hai tay đá đối xứng | Không thêm vũ khí |
| Dune Devourer | 0 cầm tay | Bốn xúc tu và hàm giữa | Giữ đủ bốn xúc tu |

Quy tắc kiểm tra: Trong mọi frame, không được tự xuất hiện hoặc biến mất vũ khí. Với các quái có bộ phận bị che bởi phối cảnh, chúng có thể bị khuất một phần nhưng vẫn phải giữ liên tục về giải phẫu và số lượng.

### VII. KẾT LUẬN LƯỢT 4

8/8 quái có silhouette, biểu cảm và cơ chế chuyển động riêng.

Amber Dunes có bọ thấp, kẻ cướp giáp dày và tinh linh hổ phách bay. Scorpion Gorge có bọ cạp, hồn cát bắn xa, thằn lằn đá, Golem sa thạch và boss hoa cát ăn thịt.

Các lỗi đặc biệt đã được xử lý ở cấp thiết kế:

- Sand Raider dùng đúng một cây chùy, không đổi vũ khí giữa Walk/Attack.
- Sand Wraith có vector đạn rõ cho D/U/R/L.
- Dune Golem khác Crystal Golem và Stoneback Guardian ở cấu trúc thân trụ, đai đá và tay cột.
- Dune Devourer gắn với hố xoáy nhưng sprite và VFX tách riêng.
- Mọi quái đều có phương án lật trái từ phải không phá vỡ số lượng vũ khí.

ART DIRECTION: CHỐT 8 THIẾT KẾ LƯỢT 4 — tổng 35 thiết kế qua bốn lượt.

## 6. Marsh (Emberfen) + Ruins (Sunken Reliquary): 5 quái, master prompt của ChatGPT, bảng tóm tắt 40 quái

### MONSTER REDESIGN — LƯỢT 5/5

5 quái cuối · Marsh + Ruins · Master Prompt Template · Tổng hợp 40 quái

Giữ nguyên toàn bộ quy tắc A/B và 35 thiết kế đã chốt trong bốn lượt trước.

Lượt cuối hoàn thiện hai biome có độ huyền bí cao nhất:

- MARSH — Emberfen: Sinh vật đầm lầy kỳ lạ, ẩm ướt, tinh nghịch, hơi nguy hiểm nhưng vẫn đáng yêu.
- RUINS — Sunken Reliquary: Sinh vật tàn tích cổ đại, xanh ngọc, vàng cổ, mang cảm giác thiêng liêng và bí ẩn.

Đặc biệt, Mirefang Hydra và Mire Hydra Queen là hai sinh vật khác nhau. Mirefang là cận vệ trung thành của nữ hoàng, phải có silhouette, khuôn mặt và cấu trúc đầu riêng, nhưng cùng họ thủy quái đầm lầy.

### PHẦN 1 — THIẾT KẾ 5 QUÁI CUỐI

#### I. Quy chuẩn kích thước

| Quái | Lv | Cao gốc | Scale | Cao hiển thị | Vai trò |
|---|---|---|---|---|---|
| Bog Slime | 31 | 96 px | ×1 | 96 px | Chậm, cận chiến |
| Mire Toad | 33 | 106 px | ×1 | 106 px | Nặng, giáp dày |
| Marsh Stalker | 35 | 143 px | ×1 | 143 px | Nhanh, đánh sắc |
| Ruin Wisp | 38 | 118 px | ×1 | 118 px | Bay nhanh, cận chiến |
| Mirefang Hydra | 40 | 168 px | ×1.85 | ≈311 px | Boss map, cận vệ Hydra Queen |

Quy ước hướng: D = xuống, U = lên, R = phải, L = lật ngang R.

Tất cả sử dụng Walk 4 frame × 3 hướng và Attack 4 frame × 3 hướng. Quái bay sử dụng chu kỳ Fly thay Walk. Boss sử dụng canvas đủ rộng để không cắt đầu, cổ hoặc đuôi.

### II. MARSH — EMBERFEN

Palette chung: Xanh olive #A3B77A, xanh rêu #6C9066, vàng bùn #D4BD82, nâu đất #A88365, xanh kem #C8D6A8, nâu viền #675F51.

Motif chung: lá sen, cỏ lau, bọt nước, vệt bùn, rêu và gợn nước. Không làm mọi quái thành slime độc xanh neon.

#### 01. Bog Slime — Lv31

Tên gợi ý: Lilybog Slime · Slow Melee

##### 1) Tính cách

Lười biếng, ngái ngủ và hơi ngốc nghếch; thích nằm trong bùn ấm, thường phản ứng chậm khi có người tới gần.

##### 2) Silhouette / hình khối

Thiết kế vũng bùn sống hình bánh dẹt bất đối xứng, khác hẳn hai slime trước.

- Thân cực thấp, trải rộng theo hình oval méo, không có đỉnh nhọn.
- Phần trước tạo thành một mép bùn tràn ra như chiếc môi dưới lớn.
- Phần lưng gồ thành hai mô bùn mềm lệch nhau.
- Hai bên có hai thùy bùn ngắn như bàn chân chèo, không phải tay thật.
- Mặt nằm trên mô bùn phía trước, không ở chính giữa thân.
- Một phần thân có cảm giác sệt, nặng, không trong suốt như thạch.
- Cao 96 px, rộng 125–140 px.

Phân biệt tuyệt đối:

- Meadow Slime: Giọt thạch nghiêng, nảy cao.
- Frost Slime: Bánh tuyết ba thùy, trượt mềm.
- Bog Slime: Vũng bùn dẹt, hai mô lưng, chuyển động chảy và kéo lê.

##### 3) Đặc trưng chữ ký

Một lá sen mini lệch bên phải đỉnh lưng, nằm gần như ngang với bề mặt bùn; bên cạnh có một cọng cỏ lau ngắn cong.

Trên thân có 2–3 bong bóng bùn màu kem olive, một đốm bùn nâu ở má.

Không dùng mầm cỏ hai lá của Meadow Slime hoặc tinh thể chìm của Frost Slime.

##### 4) Mặt và biểu cảm

- Đi: Mắt oval nửa nhắm, mí nặng, đồng tử nâu đậm; miệng cười lười biếng như đang ngáp.
- Đánh: Hai mắt mở lớn bất ngờ, miệng phồng thành chữ o, mép bùn trước nâng lên.
- Trúng đòn: Mắt nhắm ><, miệng bẹt, hai mô lưng xẹp xuống, lá sen nghiêng mạnh.

Không dùng mắt độc tím hoặc miệng hung dữ.

##### 5) Bảng màu

#9FB985

#71956B

#C9D7A7

#A58A67

#D8BE86

#5F5848

Thân xanh olive ấm, bùn nâu nhạt, lá sen xanh rêu, highlight kem vàng.

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Mắt lim dim, một bong bóng nổi rồi xẹp, lá sen lắc nhẹ | Thấy hai mô bùn và lá sen từ sau, không có mặt | Mép bùn trước chảy thấp, cỏ lau rung |
| Walk | Mép trái kéo tới → thân dồn lên → mép phải kéo → thân trải xuống | Hai mô bùn kéo thân tiến lên bằng nhịp chậm | Mép trước trườn sang phải, thân sau kéo theo rồi xẹp |
| Attack | Mép bùn cuộn vào → thân phồng → tràn/húc ngắn tới trước → thu lại | Mô sau nâng cao rồi đẩy thân về hướng U | Thùy bùn phía phải phồng thành cú va chạm ngắn rồi chảy về |

Không nhảy cao, không lăn như Frost Slime.

##### 7) Vai trò chiến đấu

Chậm, cận chiến, đòn đẩy bùn tầm ngắn. Sát thương thể hiện bằng cảm giác nặng và dính, không dùng gai độc hoặc projectile.

##### 8) Liên kết cùng map

Lá sen, bong bóng, cỏ lau và bùn olive tạo nhận diện Emberfen. Màu nâu bùn liên kết với da Mire Toad, trong khi xanh rêu liên kết với Marsh Stalker.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Vũng bùn sống dẹt hai mô, mắt ngái ngủ, lá sen mini lệch bên và mép thân chảy như bùn đặc.

#### 02. Mire Toad — Lv33

Tên gợi ý: Mudshield Toad · Heavy Armored Tank

##### 1) Tính cách

Ông cóc béo khó tính, thích ngồi bất động trong bùn, rất lì đòn; khi bị chọc giận sẽ phồng má và húc đối thủ.

##### 2) Silhouette / hình khối

Thiết kế con cóc hình yên ngựa, hai mắt lớn nổi cao và lưng giáp mai bùn, không giống gấu hoặc Stoneback Guardian.

- Thân cực rộng, thấp, hình quả bí dẹt.
- Đầu gần liền thân, miệng rộng chiếm gần toàn bộ mặt trước.
- Hai mắt nổi trên hai ụ tròn lớn, đặt cách xa nhau.
- Hai chân trước ngắn, bàn chân bốn ngón bẹt; hai chân sau to, gập sát thân.
- Lưng phủ 3–4 phiến da sừng dày dạng mai bùn tự nhiên, không phải đá thật.
- Hai túi má phồng lớn dưới mắt.
- Cao 106 px, rộng khoảng 140–155 px.

Silhouette: Thân bí dẹt + hai ụ mắt cao + chân cóc xòe ngang. Không có lớp mai đá ba tầng hoặc sáu chân.

##### 3) Đặc trưng chữ ký

Hai túi má tròn phồng như bong bóng bùn và đường miệng cực rộng hình nụ cười ngược.

Lưng có ba phiến giáp da sừng màu nâu olive, viền bo mềm, tạo cảm giác tank nhưng vẫn là da cóc.

Vũ khí cố định: 0. Không mang gậy, khiên hoặc vũ khí rời.

##### 4) Mặt và biểu cảm

- Đi: Mắt tròn nặng mí, đồng tử vàng mật ong, miệng mím cau có, hai má hơi phồng.
- Đánh: Má phồng lớn, mắt nhíu, miệng mở rộng lộ chiếc lưỡi ngắn màu đào.
- Trúng đòn: Mắt nhắm mạnh, má xẹp, miệng méo, bốn chân ép sát đất.

Không dùng lưỡi dài đánh xa vì vai trò là tank cận chiến.

##### 5) Bảng màu

#899D70

#647B5A

#B7A17B

#DDC79B

#D69E85

#625346

Da xanh olive, giáp da nâu bùn, bụng kem, miệng hồng đất.

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Ngồi bẹp, mắt chớp chậm, má phồng rồi xẹp | Lưng giáp hiện rõ, chân sau gập hai bên | Miệng mím, một ụ mắt đảo nhẹ |
| Walk | Hai chân trước kéo → chân sau đẩy → thân nhích → hạ thấp | Chân sau đẩy thân từng nhịp ngắn | Bước cóc nặng, không bật cao, bụng gần chạm đất |
| Attack | Phồng hai má → hạ đầu → húc/cắn ngắn → ngồi bẹp trở lại | Dồn thân và bật húc về phía U | Hạ đầu sang phải → lao ngắn bằng vai/mõm → dừng |

##### 7) Vai trò chiến đấu

Tank nặng, giáp dày, chậm. Đòn cận chiến bằng mõm hoặc húc thân; không dùng lưỡi kéo mục tiêu hoặc phun độc xa trong basic attack.

##### 8) Liên kết cùng map

Mảng olive, túi má bùn, giáp da sừng và ánh kem kết nối trực tiếp với Emberfen. Khác Bog Slime ở cấu trúc cóc bốn chân, mắt nổi và giáp dày.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Cóc đầm lầy thân bí dẹt, hai mắt ụ cao, hai má bong bóng và ba phiến giáp da trên lưng.

#### 03. Marsh Stalker — Lv35

Tên gợi ý: Reedblade Stalker · Fast Precision Striker

##### 1) Tính cách

Kẻ săn mồi đầm lầy ranh mãnh, kiên nhẫn, thích ẩn sau cỏ lau và tung đòn nhanh trước khi đối thủ kịp phản ứng.

##### 2) Silhouette / hình khối

Thiết kế sinh vật săn mồi hai chân kiểu chim lội nước pha bò sát fantasy, không phải sói, cáo hoặc thằn lằn bốn chân.

- Thân thon hình giọt nước ngược, vai hẹp, hông nhỏ.
- Đầu kéo dài thành mõm nhọn ngắn như mỏ chim, có hàm dưới mềm.
- Hai chân dài gập ngược, bàn chân ba ngón xòe để bước trên bùn.
- Hai tay ngắn nhưng cẳng tay kéo dài thành hai phiến sừng lá sắc, đối xứng.
- Cổ dài vừa phải, có ba búi lông/vảy nhỏ dựng sau gáy.
- Đuôi thon ngắn, cuối đuôi có một chùm vảy như lá lau.
- Cao 143 px, rộng 90–105 px khi khép tay.

Silhouette: Chân lội nước dài + thân thon + hai cẳng tay lưỡi lá, khác hẳn Mantis sáu chân.

##### 3) Đặc trưng chữ ký

Hai lưỡi sừng hình lá lau gắn trực tiếp trên cẳng tay, một bên mỗi tay, và hai chân cao kiểu chim lội nước.

Vũ khí cố định: 2 lưỡi sừng tự nhiên, đối xứng. Không dùng dao rời.

##### 4) Mặt và biểu cảm

- Đi: Mắt vàng hổ phách nhỏ nhưng rõ, mí hơi xếch, miệng cười lệch đầy tự tin.
- Đánh: Mắt nheo sắc, đầu hạ thấp, miệng mở lộ hai răng nhỏ, hai lưỡi tay mở ra.
- Trúng đòn: Mắt nhắm, cổ rụt, lưỡi tay khép vào thân, một chân nâng lên để giữ thăng bằng.

##### 5) Bảng màu

#78966E

#526F59

#B0BE88

#D3BB8E

#A88166

#594D43

Thân xanh rêu, bụng olive sáng, lưỡi tay vàng xanh, chân nâu bùn.

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Đứng trên một chân ngắn hạn, mắt quan sát, lưỡi tay khép | Cổ dài, lưng thon, chùm vảy gáy rõ | Đầu cúi quan sát, chân trước nhấc nhẹ |
| Walk | Bước dài, nhấc cao bàn chân để tránh bùn, tay giữ sát thân | Chân luân phiên nhanh, đuôi cân bằng | Chạy bằng bước dài thấp, cổ nghiêng về trước |
| Attack | Hạ thân → hai lưỡi tay mở → chém giao nhau → thu nhanh | Xoay hông, hai lưỡi chém về phía U | Một lưỡi quét sang phải, lưỡi còn lại theo sau thành đòn kép |

##### 7) Vai trò chiến đấu

Nhanh, đòn sắc, HP không cao. Có thể dùng basic attack gồm hai nhát liên tiếp trong cùng animation, nhưng tốc độ tổng thể phải khác Mantis: Stalker dùng bước lao và chém cẳng tay, Mantis dùng càng liềm từ tư thế côn trùng.

##### 8) Liên kết cùng map

Hình lá lau, chân lội bùn, vảy rêu và màu olive kết nối Emberfen. Khác Mire Toad và Bog Slime bằng silhouette cao thon, khô và nhanh.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Kẻ săn đầm hai chân cao, mõm chim-bò sát, hai lưỡi lá lau đối xứng trên cẳng tay.

### III. RUINS — SUNKEN RELIQUARY

Palette chung: Xanh ngọc #64B9AF, teal sâu #2E817B, vàng cổ #D8BC83, tím đá #8E819F, kem sáng #EFDCAB, bóng tím nâu #5F536C.

Motif chung: phù văn cổ, vòng cung đá, hoa sen cách điệu, giọt nước ma thuật và đồ trang trí vàng cổ.

Thiết kế Ruins phải khác Marsh: ít bùn hơn, nhiều đường nét cổ đại, cảm giác thiêng liêng hơn nhưng vẫn mềm và dễ thương.

#### 04. Ruin Wisp — Lv38

Tên gợi ý: Relic Wisp · Fast Flying Melee

##### 1) Tính cách

Tinh linh nhỏ cổ xưa, tò mò và tinh nghịch; thích bay quanh di vật, dùng những cú chạm ma thuật để đuổi người lạ.

##### 2) Silhouette / hình khối

Thiết kế mặt nạ linh hồn hình đồng xu cổ có hai vòng cung nổi, không trùng Amber Wisp hoặc Ice Wraith.

- Thân chính là một chiếc mặt nạ đá ngọc hình oval đứng, viền dày, mặt hơi lồi.
- Không có tay chân rõ; hai bên là hai vòng cung năng lượng ngắn, giống hai mảnh nhẫn bị tách.
- Phía dưới thân có một chóp hạt ngọc nhỏ giúp giữ dáng dọc.
- Phía sau là một nửa vòng phù văn hình bán nguyệt, đặt lệch về phía trên đầu.
- Không có bốn vệ tinh hình X, không có ba dải sương, không có thân xoắn.
- Cao 118 px, rộng 95–110 px.

Silhouette: Mặt nạ oval đứng + hai vòng cung hai bên + bán nguyệt phù văn sau đầu.

##### 3) Đặc trưng chữ ký

Một mặt nạ ngọc xanh có hai mắt lớn và nửa vòng phù văn vàng cổ sau đầu.

Hai vòng cung bên thân là bộ phận cơ thể ma thuật, không phải cánh hoặc vũ khí rời.

Vũ khí cố định: 0. Tấn công bằng cú chạm thân và xung năng lượng ngắn.

##### 4) Mặt và biểu cảm

- Đi/bay: Mắt lớn màu vàng kem, miệng cười bí ẩn, một bên lông mày hơi nhướng.
- Đánh: Mắt nheo quyết tâm, miệng thành chữ o, hai vòng cung khép về trước.
- Trúng đòn: Mắt nhắm, miệng méo, vòng phù văn nghiêng lệch, thân rung như chuông nhỏ.

##### 5) Bảng màu

#77C4B8

#3F958E

#DCC68E

#F2E3B9

#9C90B0

#5B5765

Xanh ngọc chủ đạo, viền vàng cổ, bóng tím đá, highlight kem.

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Bay lên xuống 3–4 px, vòng phù văn xoay vài độ, mắt chớp | Thấy mặt sau đá ngọc và hoa văn, không có mắt | Vòng cung trước nổi bật, thân hơi nghiêng |
| Walk/Fly | Hai vòng cung mở-khép theo nhịp bay, thân trượt nhanh | Bán nguyệt phù văn rung trễ, thân bay lên | Thân nghiêng phải, vòng trước đẩy nhẹ như mái chèo |
| Attack | Hai vòng khép → thân phát sáng → lao chạm nhanh → lùi | Khép vòng và lao về hướng U | Vòng trước đẩy ra → thân va sang phải → thu |

##### 7) Vai trò chiến đấu

Bay nhanh, cận chiến bằng cú chạm năng lượng. Không phóng đạn, không có luồng phép xa. VFX va chạm là một vòng teal nhỏ, xuất hiện rất ngắn.

##### 8) Liên kết cùng map

Ngọc xanh, vòng phù văn, vàng cổ và hình oval như di vật kết nối Sunken Reliquary. Khác Amber Wisp vì cấu trúc mặt nạ/vòng cung, khác Ice Wraith vì không có thân chuông hoặc dải sương.

##### 9) Boss

Không áp dụng.

Chốt nhận diện: Tinh linh mặt nạ ngọc oval, hai vòng cung hai bên và bán nguyệt phù văn vàng cổ phía sau.

#### 05. Mirefang Hydra — Lv40

Tên gợi ý: Royal Mirefang · Hydra Queen's Guardian · Scale ×1.85

##### 1) Tính cách

Cận vệ trung thành của Mire Hydra Queen, hung hăng và cảnh giác với người lạ nhưng ngoan ngoãn trước nữ hoàng; giống một sủng thú bảo vệ chủ nhân.

##### 2) Silhouette / hình khối

Thiết kế thủy quái Hydra ba đầu dạng thú cưng chiến đấu, không sao chép thân hình và bố cục của Mire Hydra Queen.

- Thân chính thấp, bè ngang, giống một con kỳ giông đầm lầy khổng lồ hơn là rắn nhiều đầu đứng cao.
- Bốn chân ngắn khỏe, bàn chân ba ngón có màng, thân nằm gần mặt đất.
- Ba cổ mọc từ phần vai trước, tương đối ngắn và dày, chia hình quạt thấp.
- Đầu giữa lớn nhất, hai đầu bên nhỏ hơn khoảng 20–25%.
- Ba đầu có mõm ngắn tròn, mắt lớn, sừng sụn mềm hoặc mào mang nước, không có vương miện.
- Sau lưng có một vây lớn hình cánh sen gấp, chia ba thùy bo tròn.
- Đuôi dài dày, thấp, cong sang một bên, chóp đuôi dạng vây cá.
- Cao gốc 168 px, rộng khoảng 175–200 px; scale ×1.85 thành khoảng 311 px cao. Bề ngang hiển thị có thể lớn hơn 350 px.

Silhouette: Thủy quái thân thấp bốn chân, ba cổ ngắn xòe hình quạt, vây lưng hoa sen, đuôi cá.

Khác nữ hoàng ở chiều cao cổ, đầu, ngôn ngữ trang sức và tư thế.

##### 3) Đặc trưng chữ ký

Ba đầu ngắn có tính cách khác nhau và một vây lưng hình hoa sen ba thùy.

- Đầu giữa: Nghiêm nghị, mắt vàng ngọc, biểu cảm thủ lĩnh.
- Đầu trái: Tinh nghịch, hay cười, mắt mở lớn.
- Đầu phải: Cau có, nhe một răng nhỏ, dễ giận.

Cả ba thuộc cùng một cơ thể và cùng loài, không dùng ba màu đầu khác nhau quá mạnh.

Trên cổ có vài hoa văn giọt nước teal và một vòng phù văn vàng cổ nhỏ ở ngực, tạo mối liên hệ với nữ hoàng nhưng không trang trí vương miện.

Vũ khí tự nhiên: 3 bộ hàm, một đuôi và bốn chân. Không mang vũ khí rời.

##### 4) Mặt và biểu cảm

- Đi: Đầu giữa nhìn thẳng cảnh giác; đầu trái nghiêng tò mò; đầu phải cau mày. Ba miệng có biểu cảm riêng.
- Đánh: Đầu giữa há miệng gầm, hai đầu bên nhe răng hoặc vươn tới; cả ba cùng hướng sự chú ý về mục tiêu.
- Trúng đòn: Đầu giữa nhắm mắt, hai đầu bên co về vai, vây lưng rung, thân hạ thấp.
- Idle đặc biệt: Đầu trái có thể nhìn sang đầu giữa như muốn chơi; đầu phải quay đi khó chịu; đầu giữa giữ vẻ nghiêm nghị.

##### 5) Bảng màu

#69B6A0

#3D8D83

#A5C59D

#D8C28F

#9A88A8

#564D60

Thân teal rêu, bụng kem olive, vây xanh ngọc, hoa văn vàng cổ, bóng tím xám.

##### 6) Animation 3/4

|  | D — Xuống | U — Lên | R — Phải |
|---|---|---|---|
| Idle | Ba đầu chuyển động lệch nhịp, đầu giữa ổn định, hai đầu bên nhìn quanh | Thấy ba cổ từ sau, vây sen và đuôi rõ, tuyệt đối không thấy mặt | Đầu gần camera nổi bật, ba cổ tách lớp không chồng hoàn toàn |
| Walk | Bốn chân bước nặng vừa, thân lắc nhẹ, ba cổ cân bằng độc lập | Hông và đuôi chuyển động trễ, cổ nhún nhẹ | Chân trước bước → thân tiến → chân sau đẩy, đuôi cong cân bằng |
| Attack | Đầu giữa vươn cắn → hai đầu bên lao theo → ba đầu thu về trong một chu kỳ | Ba cổ vươn về hướng U, đầu không quay ngược ra màn hình | Đầu gần cắn sang phải, hai đầu còn lại mở góc hỗ trợ, sau đó thu về |

Quy tắc animation đặc biệt: Ba đầu không được tự đổi vị trí hoặc số lượng. Khi quay hướng phải, đầu gần camera che một phần đầu giữa nhưng phải vẫn nhìn ra đủ ba cổ/đầu qua bố cục lệch tầng. Khi lật trái, thứ tự trước-sau của hai đầu bên đảo theo ảnh, nhưng danh tính và màu sắc các đầu vẫn nhất quán.

##### 7) Vai trò chiến đấu

Boss map Lv40, cận chiến mạnh bằng ba đầu cắn, đuôi quét và khả năng kiểm soát khu vực gần thân.

Đề xuất ba hình thái cơ bản:

- Triple Bite: Ba đầu cắn lệch nhịp.
- Mire Tail Sweep: Đuôi quét thấp, VFX nước bùn riêng.
- Royal Guard Roar: Cả ba đầu gầm, tạo cảm giác bảo vệ nữ hoàng.

Attack sheet 4 frame cơ bản dùng Triple Bite. Những chiêu khác tạo sheet riêng nếu gameplay cần.

##### 8) Liên kết Ruins và Mire Hydra Queen

Mirefang cùng họ Hydra thông qua:

- Ba đầu và cổ linh hoạt.
- Vảy teal/olive, bụng sáng kem.
- Mào nước, phù văn ngọc và vàng cổ.
- Biểu cảm nhiều đầu có tính cách riêng.
- Cơ thể sinh vật đầm lầy thích nghi với môi trường nước.

Nhưng khác nữ hoàng bắt buộc:

| Yếu tố | Mire Hydra Queen | Mirefang Hydra |
|---|---|---|
| Địa vị | Nữ hoàng / chủ nhân | Sủng thú / cận vệ |
| Tư thế | Uy nghi, thống trị | Thấp, bốn chân, sẵn sàng lao tới |
| Cổ | Nhấn mạnh hình tượng Hydra hoàng gia | Ngắn, dày, xòe quạt |
| Đầu | Ngôn ngữ tạo hình của boss đã duyệt | Mõm ngắn, gương mặt thú cưng |
| Trang sức | Theo thiết kế nữ hoàng đã duyệt | Không vương miện, chỉ phù văn nhỏ |
| Silhouette | Giữ nguyên ảnh 1 | Kỳ giông ba đầu có vây sen và đuôi cá |
| Cảm giác | Quyền lực và nguy hiểm | Trung thành, tinh nghịch nhưng bảo vệ dữ dội |

Không chỉnh sửa bất kỳ yếu tố đã duyệt nào của Mire Hydra Queen để làm hai boss khác nhau.

##### 9) Quy tắc boss — uy nghi và đáng yêu

Mirefang phải trông như thú cưng chiến đấu của một nữ hoàng cổ đại, không phải phiên bản thu nhỏ của nữ hoàng.

- Focal point 1: Ba gương mặt khác tính cách.
- Focal point 2: Vây lưng sen ba thùy.
- Focal point 3: Thân thấp khỏe và đuôi vây cá.
- Hai đầu bên nhỏ hơn tạo phân cấp, không tranh điểm nhìn với đầu giữa.
- Giữ mắt to, má và mõm mềm, răng ít nhưng rõ.
- Có thể hung dữ khi Attack nhưng không dùng nanh dài kinh dị, nhiều mắt hoặc hoa văn hỗn loạn.
- Cổ không quá dài để tránh đè lên HUD hoặc chồng vào người chơi.
- VFX đầm lầy và phù văn tách khỏi sprite.
- Độ hoàn thiện về vật liệu, biểu cảm, viền và phân cấp chi tiết tương đương Mire Hydra Queen.

Chốt nhận diện: Hydra cận vệ thân kỳ giông thấp, ba đầu ngắn khác tính cách, bốn chân màng, vây lưng hoa sen và đuôi cá.

### PHẦN 2 — MASTER PROMPT TEMPLATE

Ba template dưới đây dùng cho toàn bộ bộ quái. Mỗi prompt là một dòng liền, có các trường [ ... ] để thay thông tin từng quái.

Quy ước chung:

- Concept lineup: 4–6 quái cùng biome, dùng để duyệt thiết kế và silhouette, chưa phải sprite animation.
- Walk sheet: 4 cột × 3 hàng = 12 frame.
- Attack sheet: 4 cột × 3 hàng = 12 frame.
- Nền xanh/magenta phải phẳng tuyệt đối, không tạo viền màu lạ quanh nhân vật.
- Không đánh bóng mặt đất vào sprite.
- Hướng trái luôn flip từ hướng phải; phụ kiện bất đối xứng đảo bên khi flip.
- Với boss nhiều đầu hoặc sprite cực rộng, phải sử dụng canvas/cell lớn hơn; không ép toàn bộ boss vào ô 192 px.

#### A. Master Prompt — Concept Lineup một biome

Writing

Vẽ mới hoàn toàn từ đầu MỘT ảnh CONCEPT LINEUP gồm đúng [SỐ QUÁI: 4–6] quái của biome [TÊN BIOME] trong game RPG 2D chibi góc nhìn 3/4 từ trên xuống kiểu Ragnarok, phong cách RO Soft Fantasy, danh sách theo thứ tự trái sang phải: [TÊN 1 + LV + VAI TRÒ + CỠ PX + SILHOUETTE + ĐẶC TRƯNG CHỮ KÝ + TÍNH CÁCH + MÀU HEX], [TÊN 2 + ...], [TÊN 3 + ...], [TÊN 4 + ...], [TÊN 5 NẾU CÓ], [TÊN 6 NẾU CÓ]; tất cả quái phải là THIẾT KẾ MỚI HOÀN TOÀN, mỗi con có silhouette, cấu trúc cơ thể, tỉ lệ, gương mặt và tính cách khác hẳn nhau và khác các quái cũ, không đổi màu từ một thân chung, không sao chép quái của Ragnarok hoặc game khác; mỗi quái đứng riêng, nhìn hướng XUỐNG theo góc 3/4 top-down, toàn thân nhìn rõ từ đỉnh tới chân/đuôi, cùng góc camera và cùng chất lượng hoàn thiện, giữ tỉ lệ kích thước giữa các quái theo [BẢNG KÍCH THƯỚC], hero chuẩn cao khoảng 160 px; mắt to biểu cảm, miệng có cá tính, khối tròn mềm nhưng đúng giải phẫu từng loài, motif biome [MOTIF CHUNG], bảng màu biome [4–6 MÃ HEX], phụ kiện và vũ khí đúng số lượng cố định [SỐ VŨ KHÍ TỪNG QUÁI], ưu tiên đối xứng trái-phải để sprite có thể flip; bố cục một hàng ngang, khoảng cách giữa mọi nhân vật đủ rộng, không chồng nhau, không chạm mép, không thêm quái hoặc vật thể khác, không chữ, không tên, không số, không bảng UI, không khung; tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối; nền phẳng #FF00FF hoặc #00FF00, không caro, không gradient, không trong suốt, không bóng dưới chân, không ánh sáng hoặc màu nền phản chiếu lên nhân vật, chỉ nhân vật và chi tiết cơ thể cố định, ảnh ngang độ phân giải cao [KÍCH THƯỚC ẢNH].

Cách dùng: Thay đầy đủ các trường, đặc biệt silhouette, mặt và chữ ký từng quái. Không chỉ điền tên game vì như vậy model dễ tự dùng hình dáng quái phổ biến.

#### B. Master Prompt — Sprite Sheet ĐI 4×3

Writing

Vẽ mới hoàn toàn từ đầu sprite sheet WALK cho quái [TÊN GAME] Lv[CẤP], biome [BIOME], vai trò [VAI TRÒ], phong cách RPG 2D chibi góc nhìn 3/4 từ trên xuống kiểu Ragnarok; GIỮ CHÍNH XÁC thiết kế đã duyệt: loài [LOÀI], silhouette [MÔ TẢ KHỐI CHÍNH], kích thước [CHIỀU CAO GỐC] px so với hero khoảng 160 px, tỉ lệ đầu/thân [TỈ LỆ], mặt [MẮT + MIỆNG + BIỂU CẢM], đặc trưng chữ ký [CHI TIẾT], vật liệu [CHẤT LIỆU], bảng màu [4–6 HEX], số vũ khí cố định [SỐ] loại [LOẠI VŨ KHÍ], đặt tại [VỊ TRÍ], tuyệt đối không thêm/bớt/đổi vũ khí; ảnh sprite sheet đúng 4 CỘT × 3 HÀNG gồm 12 frame bằng nhau, mỗi ô 192×192 px, toàn ảnh 768×576 px, hàng 1 = hướng XUỐNG nhìn thấy đầy đủ mặt và ngực, hàng 2 = hướng LÊN nhìn thấy lưng, hàng lên không thấy mắt, không thấy miệng hoặc khuôn mặt phía trước, hàng 3 = hướng PHẢI nhìn ngang 3/4, hướng TRÁI trong game sẽ lật ngang hàng PHẢI nên mọi chi tiết bất đối xứng được phép đổi bên khi lật, ưu tiên vũ khí ở giữa hoặc hai bên đối xứng; mỗi hàng có 4 frame của đúng một chu kỳ đi [MÔ TẢ CHUYỂN ĐỘNG ĐẶC THÙ LOÀI], frame 1 = [TƯ THẾ 1], frame 2 = [TƯ THẾ 2], frame 3 = [TƯ THẾ 3], frame 4 = [TƯ THẾ 4], với sinh vật bay thay bước chân bằng nhịp lơ lửng/cánh/đuôi; mọi frame giữ nguyên thiết kế, số chi, số đầu, số vũ khí, màu và kích thước cơ thể, chỉ thay đổi tư thế, squash/stretch vừa phải, chân hoặc điểm tiếp xúc đất đặt theo pivot thống nhất, các bộ phận chuyển động phụ có quán tính hợp lý; nhân vật nằm trọn từng ô, cách mép ô ít nhất [PADDING] px, không chạm hoặc tràn ô, không bóng đất, không FX rời, không chữ, không số, không đường kẻ lưới; tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối; nền phẳng #FF00FF hoặc #00FF00, không caro, không gradient, không trong suốt, không bóng dưới chân, màu nền tuyệt đối đồng nhất; tất cả frame cùng scale và góc camera, không được tự tạo thêm hướng hoặc frame.

Lưu ý production: Với quái có scale Unity lớn hơn ×1, sprite gốc vẫn phải vẽ đúng tỉ lệ đã chốt và đủ độ phân giải. Với boss cần sheet cell lớn hơn, thay toàn bộ thông số cell và canvas tương ứng, không chỉ đổi con số chiều cao.

#### C. Master Prompt — Sprite Sheet ĐÁNH 4×3

Writing

Vẽ mới hoàn toàn từ đầu sprite sheet ATTACK cho quái [TÊN GAME] Lv[CẤP], biome [BIOME], vai trò [VAI TRÒ], dựa trên thiết kế và sprite WALK đã duyệt, PHẢI GIỮ Y NGUYÊN khuôn mặt, silhouette, giải phẫu, tỷ lệ đầu/thân, màu sắc, hoa văn, phụ kiện, số lượng đầu/chân/tay/cánh/đuôi và đúng [SỐ] vũ khí [LOẠI] tại [VỊ TRÍ], không tự đổi vũ khí giữa frame; góc camera RPG 2D chibi 3/4 từ trên xuống kiểu Ragnarok, ảnh đúng 4 CỘT × 3 HÀNG = 12 frame, mỗi ô 256×256 px, toàn ảnh 1024×768 px, hàng 1 = hướng XUỐNG thấy mặt và ngực, hàng 2 = hướng LÊN nhìn từ sau, hàng lên không thấy mắt, không thấy miệng hoặc mặt trước, hàng 3 = hướng PHẢI 3/4, hướng TRÁI trong game bằng cách lật ngang hàng PHẢI, vũ khí/chi tiết bất đối xứng có thể đảo bên khi lật; mỗi hàng thể hiện đúng MỘT chu kỳ tấn công [TÊN / KIỂU ĐÒN], frame 1 = ANTICIPATION [TƯ THẾ LẤY ĐÀ], frame 2 = WIND-UP [TƯ THẾ GỒNG / NÂNG VŨ KHÍ], frame 3 = IMPACT [TƯ THẾ RA ĐÒN MẠNH NHẤT], frame 4 = RECOVERY [TƯ THẾ THU HỒI], hướng đòn và đầu/vũ khí luôn theo facing tương ứng, quái tầm xa dùng điểm bắn [VỊ TRÍ SPAWN] và hướng projectile D xuống màn hình, U lên màn hình, R sang phải, L lật R sang trái, projectile VFX tách riêng không vẽ đạn bay rời trong sheet này; thân sheet đánh to bằng sheet đi, nhân vật ở tư thế trung tính phải có đúng chiều cao và chiều rộng như sheet WALK khi đo trên ô tương ứng, ô đánh lớn hơn chỉ để chứa tay, chân, vũ khí và biên chuyển động, không phóng to nhân vật, không thu nhỏ nhân vật; giữ pivot chân/thân nhất quán giữa mọi frame và hai sheet, không cho nhân vật nhảy lệch vị trí vô lý, không thêm quái, bóng đất, vòng target, telegraph, chữ, số, lưới hay UI; tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối; nền phẳng #FF00FF hoặc #00FF00, không caro, không gradient, không trong suốt, không bóng dưới chân, không viền màu nền lọt vào nhân vật, các frame rõ, đồng bộ, không chạm mép ô.

##### Quy tắc sử dụng ba Master Prompt

- Lineup dùng để duyệt nhận diện. Chỉ khi lineup đạt mới triển khai Walk.
- Walk là chuẩn cuối về hình dáng, màu, mặt, kích thước và góc nhìn.
- Attack phải bám Walk, không vẽ lại thành một thiết kế khác.
- Khi cung cấp ảnh tham chiếu, ghi rõ ảnh nào là thiết kế đã duyệt, ảnh nào chỉ để tham khảo chất lượng.
- Nếu ảnh AI sai số frame, hướng hoặc số chi, phải sửa sheet đó; không điều chỉnh gameplay để hợp với sprite sai.

Lưu ý về nền: Mỗi lần tạo ảnh, chỉ chọn một trong hai màu #FF00FF hoặc #00FF00, không trộn cả hai. Với quái xanh lá Marsh hoặc Forest, nền magenta thường dễ tách hơn.

### PHẦN 3 — MASTER MONSTER LIST: 40 QUÁI

Danh sách dưới đây tổng hợp 40 thiết kế từ lượt 1–5, bao gồm quái thường, boss map, boss hầm và Wanted Boss.

Tên gợi ý chỉ là phương án đặt tên dễ thương; tên game gốc vẫn được giữ nguyên.

#### A. MEADOW — Willowbrook

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Meadow Slime | Dewdrop Slime | Giọt thạch xanh non nghiêng đầu, có mầm cỏ hai lá và bọt khí trong thân, ngây thơ và nảy chậm. |
| Horn Rabbit | Sprout Horn Rabbit | Thỏ thân quả lê, hai tai lệch, sừng nụ mầm màu mật ong, nhảy nhanh và tinh nghịch. |
| Thorn Boar | Bristle Boar | Lợn rừng caramel thân oval cực rộng, mõm lớn, bờm ba chóp lá olive, cứng đầu và húc nặng. |

#### B. FOREST — Bramblewood / Rootburrow

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Meadow Mantis | Leafblade Mantis | Bọ ngựa kiếm sĩ cao gầy hình chữ Y, hai càng liềm lá lớn, mắt sắc và chém nhanh. |
| Forest Wolf | Mossmane Wolf | Sói rừng nhỏ lông xám nâu, bờm olive ba khối, đuôi dấu hỏi, tinh quái và đi bầy. |
| Bramble Treant | Bramble Stump | Gốc cây ông già thân thùng rộng, tay cành ba ngón, mắt gỗ xoắn ở bụng, chậm và lì đòn. |
| Thorn Sprite | Briar Pixie | Tinh linh hạt giống nhỏ có hai cánh lá lớn, râu gai cong, bay nhanh và hay trêu chọc. |
| Thornfang Alpha | Thornfang Alpha | Sói thủ lĩnh ngực rộng, bờm lá dạng vương miện, hai vệt má kem và ánh mắt hổ phách kiêu hãnh. |
| Thornheart Treant | Thornheart Treant | Cổ thụ hộ vệ hình chuông, vai chạc cao, hai tay rễ vươn rộng và trái tim hổ phách giữa ngực. |

#### C. SNOW — Frost Hollow / Frostfang Den / Wanted

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Frost Slime | Snowpuff Slime | Bánh mochi tuyết ba thùy, lõi bông tuyết, mắt ngơ ngác, di chuyển bằng cách trượt và dồn thân. |
| Snow Fox | Snowwhisk Fox | Cáo tuyết thân thon, tai lớn, đuôi quạt ba chóp chiếm gần nửa silhouette, chạy lướt cực nhanh. |
| Ice Imp | Icicle Imp | Tiểu quỷ băng hai chân, đầu kim cương mềm, tai vây và mào tóc băng ba khối, tấn công sương lạnh tầm ngắn. |
| Frost Bear | Snowmound Bear | Gấu trắng bốn chân như gò tuyết, lưng vòm rộng, tai nhỏ, mắt buồn ngủ, tank chậm. |
| Ice Wraith | Veilfrost Wraith | Hồn băng hình chuông có vầng trăng băng khuyết sau đầu và ba dải sương mềm, bay nhanh. |
| Frostbite Yeti | Snowtuft Yeti | Yeti lông xù vai rộng, mặt trái tim ngược, hai tay dài khổng lồ và đòn vung chậm. |
| Crystal Golem | Glintcore Golem | Pháo đài băng lục giác hai chân, nắm đấm hộp lớn, lõi ngực sáng và hai phiến tinh thể chữ V. |
| Frostfang King | Frostfang King | Vua sói băng ngực cao, bờm tuyết tròn như sư tử, vương miện ba chóp và mắt sapphire uy nghi. |
| Ice Goblin | Frostcap Goblin | Goblin băng dáng gù, đầu ngang, tai rủ, mũ nấm băng lệch và một dùi cui tròn. |
| Spectral Wolf | Moonveil Wolf | Sói linh hồn chỉ có hai chân trước, nửa thân sau tan thành hai dải hồn chữ S, lao lướt cắn liên tục. |
| Glacier Warden | Glacier Warden | Hiệp sĩ băng thon cao, mặt nạ giọt nước, khiên giọt đảo ngược và một thương băng dài. |
| Rimeclaw Tyrant | Rimeclaw Tyrant | (đổi 20/11) Sói vua băng bốn chân khổng lồ, vương miện vàng đá xanh, áo choàng + giáp navy viền vàng, chùm tinh thể băng lưng, mắt vàng con ngươi đen, boss raid đứng yên. |
| Glacial Colossus | Glacial Colossus | Tháp sông băng cổ đại cố định, đầu nhỏ trong hốc, hai tay búa treo và khe sáng vàng chạy dọc thân. |

#### D. HIGHLAND — Sunbreak Foothills

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Highland Ibex | Sunhorn Ibex | Dê núi dáng cao, hai sừng chữ C lớn, chòm râu kem, mắt kiêu ngạo và cú húc ngắn. |
| Dustscale Lizard | Sunskitter Lizard | Thằn lằn cát cực thấp, chân xòe, vảy mái ngói và đuôi lá dài, chạy nhanh sát đất. |
| Cliff Harpy | Sunfeather Harpy | Chim Harpy đầu tròn, cánh chữ M lớn, mào ba lông, bay và bắn phi tiêu lông vũ theo hướng nhìn. |
| Stoneback Guardian | Cairnback Guardian | Sinh vật mai đá ba tầng sáu chân, đầu nêm nhỏ thò dưới mai, cực chậm và giáp dày. |
| Ridgehorn Behemoth | Ridgehorn Behemoth | Quái thú núi cao vai như rặng núi, một sừng đá lớn hướng trước, bốn chân trụ và cú húc cực nặng. |

#### E. DESERT — Amber Dunes / Scorpion Gorge

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Dune Scarab | Pebble Scarab | Bọ sáu chân cực thấp, mai vòm chia đôi, hai đốm hổ phách và râu hình thìa, chạy nhanh. |
| Sand Raider | Dune Bandit | Kẻ cướp chibi khăn tam giác, giáp hình thang, hai tay cầm đúng một chùy đầu tròn ở giữa ngực. |
| Amber Wisp | Honeyglow Wisp | Viên hổ phách biết cười, bốn mảnh vệ tinh hình X, bay nhanh và va chạm cận chiến. |
| Giant Scorpion | Amberclaw Scorpion | Bọ cạp thân hạnh nhân, hai càng chữ U lớn, đuôi bốn đốt cong cao, cận chiến bằng càng. |
| Sand Wraith | Dustveil Wraith | Hồn cát dạng con quay xoắn ba tầng, hai tay gió, bay và ném cầu cát từ giữa ngực. |
| Rock Basilisk | Pebblecrest Basilisk | Thằn lằn đá đầu nêm rộng, thân ngắn, ba phiến sống lưng và đuôi chùy tròn, cắn nhanh. |
| Dune Golem | Sandstone Sentinel | Golem sa thạch thân trụ, hai tay cột đá, đai cối xay quanh bụng và mặt ngơ ngác, tank chậm. |
| Dune Devourer | Sandpit Sovereign | Boss miệng hoa cát năm thùy, hai mắt nổi tinh quái, bốn xúc tu thìa, ngồi giữa hố cát xoáy. |

#### F. MARSH — Emberfen Marsh

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Bog Slime | Lilybog Slime | Vũng bùn sống dẹt hai mô, mắt ngái ngủ, lá sen mini lệch bên và thân chảy như bùn đặc. |
| Mire Toad | Mudshield Toad | Cóc đầm lầy thân bí dẹt, hai ụ mắt cao, hai má bong bóng và ba phiến giáp da dày trên lưng. |
| Marsh Stalker | Reedblade Stalker | Kẻ săn đầm hai chân cao kiểu chim-bò sát, hai lưỡi lá lau đối xứng trên cẳng tay, đánh nhanh sắc. |

#### G. RUINS — Sunken Reliquary

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Ruin Wisp | Relic Wisp | Tinh linh mặt nạ ngọc oval, hai vòng cung hai bên và bán nguyệt phù văn vàng cổ, bay nhanh cận chiến. |
| Mirefang Hydra | Royal Mirefang | Hydra cận vệ thân kỳ giông thấp, ba đầu ngắn khác tính cách, bốn chân màng, vây lưng hoa sen và đuôi cá. |

#### H. BOSS ĐÃ DUYỆT — GIỮ NGUYÊN

| Tên game | Tên gợi ý | Concept một câu |
|---|---|---|
| Mire Hydra Queen | Giữ nguyên | Nữ hoàng Hydra đầm lầy đã duyệt trong ảnh 1, là chuẩn chất lượng boss và được giữ nguyên thiết kế. |

### PHẦN 4 — KIỂM TRA TOÀN BỘ BỘ QUÁI

#### 1. Thống kê

| Khu vực | Số quái |
|---|---|
| Meadow | 3 |
| Forest + Rootburrow | 6 |
| Snow + Frostfang Den + Wanted | 13 |
| Highland | 5 |
| Desert + Scorpion Gorge | 8 |
| Marsh | 3 |
| Ruins | 2 |
| Tổng cộng | 40 |

Trong 40 tên này có Mire Hydra Queen đã duyệt, không phải thiết kế mới trong năm lượt. Vì vậy kết quả là 39 thiết kế được đặc tả mới + 1 boss giữ nguyên.

#### 2. Các nhóm dễ trùng — quy tắc khóa

| Nhóm | Quy tắc phân biệt bắt buộc |
|---|---|
| 3 Slime | Giọt nghiêng nảy / bánh tuyết ba thùy trượt / vũng bùn dẹt chảy |
| Sói + cáo | Wolf bốn chân ngực lớn / Fox đuôi quạt / Alpha bờm đổ sau / King bờm tròn / Spectral nửa thân linh hồn |
| Golem + Guardian | Crystal lục giác / Stoneback mai sáu chân / Dune thân trụ đai đá / Colossus tháp băng cố định / Warden hiệp sĩ băng |
| 4 sinh vật bay nguyên tố | Thorn Sprite cánh lá / Ice Wraith chuông băng / Amber Wisp vệ tinh X / Sand Wraith con quay / Ruin Wisp mặt nạ ngọc |
| Treant | Bramble Treant gốc cây thân thùng tay ngắn / Thornheart cổ thụ tay rễ dài và tim hổ phách |
| Thằn lằn | Dustscale thân mảnh, đuôi lá / Rock Basilisk đầu lớn, sống lưng đá, đuôi chùy |
| Hydra | Mirefang thân thấp, ba đầu ngắn kiểu thú cưng / Mire Hydra Queen giữ nguyên hình tượng nữ hoàng |

#### 3. Tiêu chuẩn duyệt cuối

Một quái chỉ đạt khi đồng thời thỏa mãn:

- Nhận diện được bằng silhouette đơn sắc.
- Không nhầm với bất kỳ quái nào trong 40 tên.
- Có biểu cảm và tính cách riêng.
- Thuộc đúng bảng màu biome.
- Vật liệu và giải phẫu hợp lý.
- Góc nhìn D/U/R nhất quán, U không thấy mặt.
- Hướng L có thể flip R mà không sai số vũ khí.
- Walk và Attack giữ đúng nhân vật, kích thước và pivot.
- Đủ rõ khi đưa vào Unity ở kích thước gameplay.
- Boss đạt độ trau chuốt ngang Mire Hydra Queen, quái thường đạt cảm giác đáng yêu của bộ pet tham chiếu.

### CHỐT ART DIRECTION — 5/5

40/40 tên quái đã được bao phủ trong bộ Monster Style Bible V1.

Bộ quái có bảy hệ ngôn ngữ môi trường: Meadow mềm và ngây thơ; Forest tinh nghịch, nhiều gỗ và lá; Snow trong trẻo, lông tuyết và băng; Highland khỏe, nhiều sừng và đá cát; Desert ấm vàng, hổ phách và xoắn cát; Marsh mềm ẩm và kỳ lạ; Ruins huyền bí, ngọc xanh và vàng cổ.

Ba Master Prompt ở Phần 2 là chuẩn tái sử dụng cho các đợt vẽ concept, Walk và Attack. Các mô tả 1–9 của từng quái là nguồn thiết kế cụ thể cần điền vào prompt, không được giản lược thành tên quái và bảng màu.

Trạng thái: CHỐT MONSTER ART DIRECTION V1 — hoàn tất 5/5 lượt thiết kế, sẵn sàng chuyển sang sản xuất và nghiệm thu sprite.

## 7. Ràng buộc kỹ thuật (giữ từ sheet cũ, không đổi code)

Thiết kế mới, nhưng sheet phải lắp vào đúng chỗ cũ. Tool cắt: `Tools/build-ro-soft-monsters.py build|check <id>` (keying / tách / đặt frame dùng chung `Tools/build-meadow-monsters.py`; boss truy nã thêm `Tools/build-wanted-boss-sheets.py`).

| Mục | Quái thường + boss map | Boss truy nã (rimeclawTyrant, glacialColossus) |
|---|---|---|
| Sheet đi `Sprites/monsters/<id>.png` | 4x4 ô 192 (768x768), hàng: xuống, lên, trái, phải; chân ở hàng px cuối ô (pivot đáy giữa) | 4x4 ô 384 (1536x1536) |
| Sheet đánh `<id>-attack.png` | 4x4 ô 256 (1024x1024), chân ở y = 224 (pivot cách đáy 32 px) | 4x4 ô 512, chân y = 448 |
| Sheet niệm chiêu `<id>-cast.png` | không có | 4x4 ô 512, chân 448, chỉ nhìn về người xem: hàng 0 skill 2 (đập mục tiêu), hàng 1 skill 3 (dậm quanh boss), hàng 2 skill 4 (cuồng nộ), hàng 3 gục |
| Nguồn ChatGPT | 1 ảnh đi + 1 ảnh đánh, mỗi ảnh 4 cột x 3 hàng (xuống, lên, phải), 1536x1024, file `Assets/ArtLibrary/monsters/ro-soft/<id>-{walk,attack}-chatgpt-N.png` (N lớn nhất thắng) | như trên + 2 ảnh cast (2 hàng x 4) theo `build-wanted-boss-sheets.py` |

- **Hàng trái = lật hàng phải** (tool tự lật). Vì vậy thiết kế KHÔNG được có chi tiết bất đối xứng phải giữ một bên (vũ khí cầm tay phải, vết sẹo một bên mắt, túi đeo một bên...) trừ khi chấp nhận nó đổi bên khi quay trái. Vũ khí đặt giữa hoặc hai tay giống nhau.
- **Cỡ thân:** tool đặt chiều cao thân (trung vị các frame đi) = số "Cao thân" ở bảng đầu file, lấy từ `ro-soft/heights.json`. Thanh HP, tên, hitbox (`HitW`/`HitH` trong `MonsterDef`) không đổi. Thân trong sheet đánh phải TO BẰNG thân ở sheet đi (prompt ghi rõ; nếu lệch thêm id vào `ATK_MATCH`).
- **Tỉ lệ khung:** quái thân rộng (boar, bear, golem, basilisk, scorpion, hydra) vẫn phải nằm gọn trong ô; frame không chạm mép ảnh nguồn, các frame cách nhau đủ rộng để tool tách theo khe trống.
- **Quái bay** (thornSprite, cliffHarpy, iceWraith, amberWisp, sandWraith, ruinWisp): vẽ thân lơ lửng, KHÔNG vẽ bóng; tool nâng thân lên `HOVER` px trên đường chân (hiện 10-16 px), game tự vẽ bóng ở chân.
- **Không vẽ bóng dưới chân** cho mọi quái (game vẽ bóng bằng code; bóng vẽ sẵn trên nền magenta ra mảng tím, xem Sổ lỗi 13/11).
- **Màu nền (key):** nền phẳng một màu, không xin trong suốt (ChatGPT vẽ ô caro). Mặc định `#FF00FF`; quái có tím / hồng / magenta (Spectral Wolf, Ruin Wisp, Mirefang Hydra, wisp tím...) hoặc vệt trắng mờ thì dùng `#00FF00` và ghi "không dùng xanh lá thuần trên nhân vật"; quái xanh lá (Meadow Slime, Bog Slime, Mantis, Treant, Thorn Sprite, Mire Toad, Marsh Stalker) dùng `#FF00FF`. Tool đọc màu nền từ góc ảnh.
- **Đạn / hiệu ứng trong sheet đánh:** ghi hướng bay TỪNG HÀNG (xuống: bay về người xem; lên: bay ra xa người xem; phải: bay sang phải). Cliff Harpy (lông vũ) và Sand Wraith (cầu cát) là quái tầm xa thật (`Ranged` profile), đạn game tự vẽ; trong sheet chỉ cần tư thế ném / vỗ cánh + một chút hiệu ứng ở tay / cánh.
- **Vũ khí:** đúng số lượng ở cả hai sheet (ví dụ "đúng 1 dao" hay "2 dao giống nhau" ghi ở cả sheet đi và sheet đánh; lỗi Sand Raider 20/11).
- **Nhịp:** đi 4 frame (8 FPS), đánh 4 frame / 400 ms, trúng đòn ở frame 3 (`Monster`, `HeroPet.Sheets.cs` dùng chung sheet quái cho pet: pet tự đổi theo).
- **Ghi đè gợi ý của ChatGPT (đối chiếu với code):** ChatGPT viết boss / quái rộng "phải dùng canvas / ô lớn hơn, không ép vào ô 192". Trong game, boss map (Thornfang Alpha, Thornheart Treant, Frostfang King, Glacier Warden, Ridgehorn Behemoth, Dune Devourer, Mirefang Hydra) VẪN dùng ô 192 / 256 và được phóng bằng `RenderScale`; chỉ 2 boss truy nã dùng ô 384 / 512. Ảnh nguồn ChatGPT vẫn là 1536x1024 (mỗi frame khoảng 380 px, đủ nét); tool co về ô. Không đổi `WalkCellPx` / `AttackCellPx` nếu chưa hỏi.
- **Chi tiết bất đối xứng trong thiết kế lượt 1-3 (trước khi ChatGPT được nhắc luật lật):** Glacier Warden (khiên + thương), Ice Goblin (mũ nấm lệch + 1 dùi cui), Horn Rabbit (2 tai lệch), Bog Slime (lá sen lệch bên). Khi quay trái các chi tiết này đổi bên. Tai / lá / mũ đổi bên thì chấp nhận; vũ khí (Glacier Warden, Ice Goblin) khi vẽ sheet ghi rõ "vũ khí cầm ở tay phía người xem trong hàng phải" và kiểm hàng trái sau khi lật; nếu nhìn sai thì đổi sang vũ khí đặt giữa thân.
- **Boss map** dùng ô 192 / 256 như quái thường, game phóng bằng `RenderScale` (1.65-2.0): vẽ chi tiết vừa đủ đọc khi phóng, nét viền dày hơn quái thường một chút.

## 8. Mẫu prompt vẽ

Dán nguyên văn, thay phần `<...>` bằng mục của quái trong file này. Mỗi prompt MỘT DÒNG (Enter là gửi). Prompt đầu mỗi chat gửi kèm 3 ảnh mẫu ở "Quy tắc cứng" điều 3, KHÔNG gửi frame quái cũ.

**8.1 Concept lineup theo biome (1 ảnh, 4-6 quái):**

```
Vẽ mới hoàn toàn từ đầu, không dựa trên các ảnh trước và không dựa trên quái cũ của game. Concept lineup quái biome <BIOME> cho game RPG 2D chibi góc nhìn 3/4 từ trên xuống kiểu Ragnarok: <N> quái đứng thành một hàng, mỗi con nhìn chéo xuống-phải về người xem, cách nhau rộng, đúng tỉ lệ cỡ với nhau và với hero ảnh 3 (<tên 1> cao ~<px> so với hero 160, <tên 2> ~<px>, ...). Theo đúng thiết kế sau: <tên 1>: <silhouette>, <đặc trưng chữ ký>, <mặt + biểu cảm khi đi>, màu <hex chính>; <tên 2>: ...; Quy tắc họ <BIOME>: <motif chung, bảng màu chung>. Dễ thương kiểu Poring / RO, có hồn, mắt to biểu cảm, mỗi con một tính cách và silhouette khác hẳn nhau. Tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối. Chất lượng hoàn thiện như boss Mire Hydra Queen ở ảnh 1. Ghi tên nhỏ dưới mỗi con. Nền phẳng một màu <#FF00FF hoặc #00FF00>, không caro, không trong suốt, không vẽ bóng dưới chân. Ảnh ngang 1536x1024.
```

**8.2 Sheet ĐI của một quái (sau khi concept đã chốt; gửi kèm ảnh concept, cắt riêng con đó và phóng to 2-3 lần nearest):**

```
Vẽ mới hoàn toàn từ đầu, không dựa trên các ảnh trước. Sprite sheet ĐI của quái <tên> cho game RPG 2D chibi góc nhìn 3/4 từ trên xuống: chép y nguyên thiết kế ở ảnh đính kèm (hình khối, mặt, màu, <đặc trưng chữ ký>), không thêm chi tiết mới. Lưới 4 cột x 3 hàng, mỗi ô một frame, các frame cách nhau rộng và không chạm mép ảnh: hàng 1 đi XUỐNG về phía người xem (thấy mặt), hàng 2 đi LÊN ra xa người xem (thấy lưng, KHÔNG thấy mắt), hàng 3 đi sang PHẢI (nhìn nghiêng phải). 4 frame một chu kỳ đi lặp: <chuyển động walk của quái>. Cùng cỡ thân ở mọi frame, chân chạm cùng một đường đáy trong mỗi hàng <quái bay: thân lơ lửng cách đáy một khoảng bằng nhau, không có chân chạm đất>. <Vũ khí / số đầu / số chân bắt buộc ở cả 12 frame>. Tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối. Nền phẳng một màu <#FF00FF hoặc #00FF00>, không caro, không trong suốt, không vẽ bóng dưới chân, không chữ. Ảnh ngang 1536x1024.
```

**8.3 Sheet ĐÁNH của một quái (gửi kèm ảnh concept phóng to + sheet đi vừa nhận):**

```
Vẽ mới hoàn toàn từ đầu. Sprite sheet ĐÁNH của quái <tên>, đúng thiết kế và đúng CỠ THÂN như sheet đi ở ảnh đính kèm (thân trong sheet đánh TO BẰNG ĐÚNG thân ở sheet đi, không thu nhỏ để chừa chỗ hiệu ứng). Lưới 4 cột x 3 hàng: hàng 1 đánh về phía người xem (XUỐNG), hàng 2 đánh ra xa người xem (LÊN, thấy lưng), hàng 3 đánh sang PHẢI. 4 frame: 1 lấy đà, 2 ra đòn, 3 trúng (cực điểm), 4 về thế thủ. Động tác: <attack của quái>. Mặt khi đánh: <biểu cảm đánh>. <Đạn / hiệu ứng: hàng xuống bay về người xem, hàng lên bay ra xa người xem, hàng phải bay sang phải>. <Vũ khí / số đầu bắt buộc ở cả 12 frame>. Tông màu RO Soft Fantasy: sáng, ấm, pastel dịu, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp, không neon, không tông trầm tối. Nền phẳng một màu <#FF00FF hoặc #00FF00>, không caro, không trong suốt, không vẽ bóng dưới chân, không chữ. Ảnh ngang 1536x1024.
```

**8.5 Mắt (mọi quái, lỗi Rimeclaw 20/11):** prompt ghi rõ mắt từng quái theo concept (màu mống mắt, CON NGƯƠI ĐEN, chấm sáng, viền mắt đậm) và câu "KHÔNG mắt trống, không mắt phát sáng toàn bộ". Không viết "mắt phát sáng" trong prompt: ChatGPT sẽ xoá con ngươi. Gửi kèm ảnh phóng to mắt của concept (nearest x3-4). Nhận ảnh về: phóng từng frame thấy mặt để kiểm con ngươi trước khi cài.

**8.4 Boss truy nã:** như 8.2 / 8.3 nhưng theo layout của `Tools/build-wanted-boss-sheets.py` (mỗi ảnh 2 hàng x 4: `walk-down-up`, `walk-left-right`, `attack-down-up`, `attack-left-right`, `cast-<a>-<b>`), ghi "ĐÚNG <N> đầu / tay ở cả 8 frame" nếu nhiều đầu (lỗi Mire Hydra Queen 19/11), idle KHÔNG gửi sheet đi làm mẫu.

## 9. Thứ tự vẽ (pha ảnh)

1. **Concept lineup theo biome trước** (mẫu 8.1), mỗi ảnh 4-6 quái để thấy cả họ cùng nhau và so cỡ:
   - L1 Meadow + Forest thường: Meadow Slime, Horn Rabbit, Thorn Boar, Meadow Mantis, Forest Wolf, Bramble Treant (6)
   - L2 Forest bay + boss: Thorn Sprite, Thornfang Alpha, Thornheart Treant (3, boss cần chỗ)
   - L3 Snow nhỏ: Frost Slime, Snow Fox, Ice Imp, Ice Wraith, Ice Goblin, Spectral Wolf (6)
   - L4 Snow to: Frost Bear, Frostbite Yeti, Crystal Golem, Frostfang King, Glacier Warden (5)
   - L5 Boss truy nã: Rimeclaw Tyrant, Glacial Colossus (2, ảnh riêng, cạnh hero để so cỡ 2.5x)
   - L6 Highland: Highland Ibex, Dustscale Lizard, Cliff Harpy, Stoneback Guardian, Ridgehorn Behemoth (5)
   - L7 Desert: Dune Scarab, Sand Raider, Amber Wisp, Giant Scorpion, Sand Wraith, Rock Basilisk (6)
   - L8 Desert to + Marsh + Ruins: Dune Golem, Dune Devourer, Bog Slime, Mire Toad, Marsh Stalker, Ruin Wisp, Mirefang Hydra (7; nếu chật tách 2 ảnh)
   Xem từng concept so với mục của quái trong file này: sai silhouette / đặc trưng / màu thì vẽ lại con đó (tối đa 2 lần), rồi chốt bản tốt nhất và ghi đường dẫn ảnh chốt vào `docs/pending-tasks.md`. **Không cần người dùng duyệt concept** (người dùng 20/11: "ko cần duyệt, cứ làm luôn đi"): agent tự chốt rồi vẽ sheet luôn. Lưu concept ở `docs/reference/monster-redesign-concepts/<biome>-concept-N.png`.
2. **Sau đó từng quái: sheet đi rồi sheet đánh** (8.2, 8.3), gửi kèm con đó cắt từ concept đã chốt (phóng to nearest). Thứ tự: quái thấp cấp trước (người chơi gặp trước): Meadow → Forest → Snow → Den → Highland → Desert → Marsh → Ruins → boss map → boss truy nã.
3. Mỗi quái: `build`, `check`, xem ảnh trước/sau TỪNG FRAME (đúng hướng, đúng số chi, cỡ đánh = cỡ đi, hàng lên không thấy mắt, đạn đúng hướng), chụp trong game (`--dummy-types`), rồi commit từng quái. Lỗi mới ghi Sổ lỗi.

## 10. Kế hoạch quota

Ước tính số ảnh:

| Hạng mục | Ảnh |
|---|---|
| Concept lineup 8 ảnh + vẽ lại ~4 | 12 |
| 38 quái thường + boss map x 2 sheet (đi, đánh) | 76 |
| 2 boss truy nã: 4 ảnh đi/đánh + 2 ảnh cast mỗi con | 12 |
| Vẽ lại do lỗi (~15 %) | ~15 |
| **Tổng** | **~115** |

Lượt 20/11 tài khoản Tay ra được khoảng 54 ảnh trước khi hết lượt, nên mỗi tài khoản một cửa sổ khoảng 50 ảnh:

| Cửa sổ | Tài khoản | Việc |
|---|---|---|
| 20/11 từ 09:17 | Tan Pham (Plus), chat mới, kiểm đúng tên tài khoản | 8 concept (+ vẽ lại) ≈ 12; rồi sheet Meadow + Forest thường + Snow nhỏ (≈ 13 quái x 2 = 26); dư thì sang Snow to. ≈ 45-50 |
| 20/11 từ 21:32 | Tay (Plus) | phần Snow còn lại, Den, Highland, Desert, Marsh, Ruins, boss map (≈ 25 quái x 2 = 50) |
| 21/11 (khi có lượt lại) | Tan Pham rồi Tay | 2 boss truy nã (12) + vẽ lại lỗi (~15) |

Không dùng tài khoản "Tuan Tai" (743f7981), "xaochai", "Tieu Duy Phuong". Hết lượt giữa chừng: ghi quái đang dở + chat vào `docs/pending-tasks.md` rồi dừng.
