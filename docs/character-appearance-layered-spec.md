# Nhân vật nhiều lớp + đổi màu tóc / áo + mũ rời (spec 19/11/2026)

Mục tiêu: hết "đội quân nhân bản" trong trận boss. Theo Ragnarok Online: 5 class mặc chung một kiểu thường phục phiêu lưu, đầu trần; mỗi người khác nhau ở màu tóc, màu áo, mũ rời và vũ khí. Người dùng tự vẽ sprite bằng ChatGPT theo prompt ở cuối file; Claude làm code, shader, tool.

Thay cho bản RO Soft "vẽ liền tóc + vũ khí" (Warrior, 19/11 sáng). Bản đó vẫn chạy trong game cho tới khi bộ nhiều lớp xong.

## Lộ trình

| Bước | Ai | Việc |
|---|---|---|
| A | Người dùng | Prompt 1-2: thân gốc nam + nữ, đứng 5 hướng. Duyệt dáng và tỉ lệ trước khi vẽ tiếp |
| B | Claude | Shader `RoSoftRecolor`, `CharacterAppearanceData` (lưu + mạng), bảng màu, renderer nhiều lớp. Thử bằng ảnh test sinh bằng code |
| C | Người dùng | Prompt 3-4: đi, đánh 1-2-3. Prompt 5: tóc 5 class. Prompt 6: mũ. Prompt 7: vũ khí |
| D | Claude | Tool cắt + tự tính `HeadAnchor` và neo vũ khí, ghép vào game, chụp 8 hướng, chỉnh |
| E | Claude | Màn tạo nhân vật (chọn màu tóc / áo / mũ), đám đông ngẫu nhiên, lệch nhịp + cỡ, dấu hiệu nhận ra mình |

## Quy cách kỹ thuật (chốt)

**Khung hình.** Ô 320 x 256 px. Người cao 200 px tính cả tóc, chân chạm hàng 244, giữa ô theo chiều ngang. Trong game người cao 20,5 đơn vị như bây giờ (PPU = 200 / 20,5). Giống ô của đường vẽ RO Soft đang có (`HeroRenderer.Soft.cs`).

**Hướng.** Vẽ 5 hướng: xuống, chéo xuống-phải, phải, chéo lên-phải, lên. 3 hướng trái lật gương từ phải. Được lật vì thường phục gần đối xứng, còn vũ khí là lớp riêng, tự đặt vào đúng tay phải theo neo từng hướng. Quy tắc cũ "không lật vì đổi tay" chỉ đúng khi vũ khí vẽ liền vào thân.

**Lớp (vẽ từ sau ra trước).** Vũ khí sau thân, `BaseBody`, Tóc, Mũ, Vũ khí trước thân, Nắm tay.

**Thân gốc dùng chung theo giới.**
- Nam cho Warrior, Wizard, Mechanic. Nữ cho Assassin, Cleric.
- Thân vẽ ĐẦU TRỌC có mặt. Tóc là lớp riêng, nên một bộ thân dùng được cho mọi kiểu tóc.
- Class chỉ khác nhau ở kiểu tóc và vũ khí.

**Động tác.**
- Đứng: 1 frame mỗi hướng, code tự làm nhịp thở.
- Đi: 4 frame.
- Đánh 1: chém ngang, dùng cho kiếm và dao.
- Đánh 2: đâm hoặc chĩa thẳng tới trước, dùng cho gậy, súng, phép.
- Đánh 3: bổ từ trên xuống bằng hai tay.
- Mỗi đòn 4 frame. Combo 3 nhát của mọi class dùng 3 đòn này, vũ khí đổi góc theo neo.

**Quy tắc tô màu để shader nhận ra vùng.** Đây là phần bắt buộc ChatGPT phải theo.
- Nền: phẳng `#00FF00`, không bóng dưới chân.
- Vùng ÁO đổi màu (áo khoác / tunic, phần vải lớn): chỉ dùng 3 tông hồng tím thuần.
  - Sáng `#FF8CFF`, giữa `#FF00FF`, tối `#A000A0`.
  - Không dùng hồng tím ở bất cứ chỗ nào khác (má hồng vẽ cam nhạt).
- Phần cố định:
  - Da `#FFE2C8` / `#F2BC9A`
  - Viền nét nâu đậm `#3A2418`
  - Áo lót trắng ngà `#F4EEDC`
  - Thắt lưng, ủng, găng: da nâu `#8A5A34` / `#5E3A1E`
  - Mắt nâu đậm
- Không dùng xanh lá (trùng nền), không dùng hồng tím ngoài vùng áo.
- Tóc (lớp riêng): chỉ 4 tông xám `#242424` (viền), `#555555`, `#999999`, `#E0E0E0`. Không màu nào khác trên tóc.
- Tool sẽ biến vùng hồng tím thành xám và ghi thành mask. Shader nhân màu người chơi chọn theo độ sáng, nên viền và da không đổi.

**HeadAnchor.**
- Tool tự dò đỉnh và bề ngang sọ trọc ở mọi frame, ghi `x, y, w, angle` vào `Data/hero-layered/<gender>/head-anchors.json`.
- Tóc và mũ đặt theo điểm này, co theo `w`, nên tự nhún theo bước đi và đòn đánh.
- Mũ vẽ với điểm "ngồi" ở giữa mép dưới, chỗ chạm đầu.

**Vũ khí.**
- Vẽ dựng đứng, lưỡi hoặc đầu lên trên.
- Neo từng frame (`x, y, angle, behind`) lấy theo nắm tay, tool dò rồi chỉnh tay.
- Theo các quy tắc tư thế cầm trong `docs/sprite-rules.md`.

**Bảng màu.** `Data/hero-layered/palettes.json`.
- 8 màu tóc: nâu hạt dẻ, đen than, vàng mật ong, bạch kim, đỏ son, xanh biển nhạt, tím oải hương, hồng anh đào.
- 6 màu áo: đỏ son, xanh lam hoàng gia, xanh rêu, vàng mù tạt, ngọc lam (thay nâu đất 19/11: gần màu da thuộc thắt lưng), tím mận.

**Ngoại hình lưu cố định.**
- `CharacterAppearanceData { classId, hairColorIndex, outfitColorIndex, equippedHeadgearId }`, kèm kiểu tóc đang có.
- Lưu trên server cùng save nhân vật, gửi qua `NetAppearance`. Phải tăng protocol lên 23 và build lại server.
- Người khác hoặc bot chưa có dữ liệu thì chọn tổ hợp theo hash NetId, ổn định cho từng người, không đổi mỗi lần vào map.

## Prompt cho ChatGPT

Mỗi prompt một tin nhắn, cùng một chat. Ảnh tải về để vào `Assets/ArtLibrary/hero-layered/src/` (tên file ghi trong prompt). Luôn gửi kèm `docs/reference/hero-idle-down-5-classes.png` (mẫu tỉ lệ) và ảnh lineup `docs/reference/ro-soft-fantasy-5-class-redesign/5-class-lineup-v1-chatgpt.png` (mẫu nét RO Soft).

### Prompt 1: thân gốc NAM, đứng 5 hướng (file `body-male-idle-5dir.png`)
Vẽ mới hoàn toàn từ đầu một nhân vật chibi NAM làm "thân gốc" cho game RPG 2D top-down kiểu Ragnarok Online, nét RO Soft Fantasy như ảnh 2 (màu sáng ấm, viền nâu đậm mềm, đổ bóng mềm 2-3 cấp), tỉ lệ đầu-thân như ảnh 1 (đầu rộng khoảng 45% chiều cao người). Nhân vật ĐẦU TRỌC hoàn toàn (không tóc, không mũ, sọ tròn trơn màu da, vẫn có tai, mặt, mắt nâu đậm, lông mày, miệng cười nhẹ) vì tóc sẽ là lớp riêng. Mặc thường phục phiêu lưu tân thủ gọn gàng: áo khoác ngắn tay / tunic tới hông, quần, thắt lưng da, ủng da, găng tay ngắn; KHÔNG giáp, KHÔNG áo choàng, KHÔNG phụ kiện. Hai tay nắm hờ trống không, buông thả lỏng cạnh hông, hơi tách khỏi thân (vũ khí sẽ gắn sau). QUY TẮC MÀU BẮT BUỘC: phần áo khoác/tunic (vùng vải lớn ở ngực, vai, tay áo, vạt áo) chỉ tô 3 tông hồng tím thuần #FF8CFF (sáng), #FF00FF (giữa), #A000A0 (tối); quần màu nâu xám #6B5B4E/#4A3E35; áo lót trắng ngà #F4EEDC; thắt lưng, ủng, găng da nâu #8A5A34/#5E3A1E; da #FFE2C8 bóng #F2BC9A; viền nét nâu đậm #3A2418; má hồng tô cam nhạt; KHÔNG dùng hồng tím ở bất cứ chỗ nào ngoài áo khoác, KHÔNG dùng xanh lá. Ảnh ngang 1536x1024, 5 nhân vật xếp một hàng cách nhau rộng, cùng chiều cao, chân chạm cùng một đường đáy, cùng tư thế đứng: (1) nhìn thẳng xuống người xem, (2) chéo xuống-phải 3/4 mặt trước, (3) nhìn ngang sang phải (một mắt, mũi chân chỉ phải), (4) chéo lên-phải 3/4 lưng (thấy gáy, không thấy mắt), (5) quay lưng hoàn toàn. Nền phẳng một màu #00FF00 toàn ảnh, không bóng dưới chân, hình đặc không trong suốt, không chữ, không khung.

### Prompt 2: thân gốc NỮ (file `body-female-idle-5dir.png`)
Giữ y nguyên nét vẽ, tỉ lệ, cỡ người, quy tắc màu và bố cục 5 hướng của thân gốc nam vừa vẽ; vẽ phiên bản NỮ: dáng nữ chibi, mặt nữ (mắt to hơn, mi rõ, mắt nâu đậm), vẫn ĐẦU TRỌC hoàn toàn (tóc là lớp riêng), cùng bộ thường phục nhưng áo khoác/tunic dáng nữ tới hông, quần ngắn ngang gối + tất cao màu nâu xám, ủng da. Vùng áo khoác vẫn CHỈ 3 tông hồng tím #FF8CFF / #FF00FF / #A000A0, mọi màu khác như bản nam. Hai tay nắm hờ trống không cạnh hông. Ảnh ngang 1536x1024, 5 hướng một hàng như bản nam, nền phẳng #00FF00, không bóng, hình đặc, không chữ.

### Prompt 3: ĐI 4 frame (mỗi giới 3 ảnh)
Gửi kèm ảnh đứng của đúng 2 hướng đó (Claude cắt sẵn ở `Assets/ArtLibrary/hero-layered/ref/`). Cặp hướng: (xuống, chéo xuống-phải), (phải, chéo lên-phải), (lên, để hàng dưới trống). File `body-<male|female>-walk-<a>--<b>.png`.

Giữ y nguyên thân gốc <NAM/NỮ> đầu trọc ở ảnh 1 (mặt, cỡ người, cỡ đầu, trang phục, quy tắc màu: áo khoác chỉ hồng tím #FF8CFF/#FF00FF/#A000A0). Vẽ CHU KỲ ĐI 4 FRAME, ảnh ngang 1536x1024, 2 hàng x 4 cột: hàng trên = hướng <A>, hàng dưới = hướng <B>. Frame 1 chân trái bước tới; frame 2 hai chân gần chụm, người nhô cao nhất; frame 3 chân phải bước tới; frame 4 hai chân gần chụm. Tay nắm hờ trống không, vung nhẹ ngược chân. Mọi frame cùng cỡ người và cỡ đầu như ảnh 1, đầu không to hơn, đầu vẫn trọc. Chân của cả hàng chạm cùng một đường đáy, không hình nào chạm mép ô hay hình bên cạnh. Nền phẳng #00FF00, không bóng, hình đặc, không chữ, không vạch kẻ ô.

### Prompt 4: ĐÁNH 1 / 2 / 3, mỗi đòn 4 frame (mỗi giới 9 ảnh: 3 đòn x 3 cặp hướng)
File `body-<male|female>-attack<1|2|3>-<a>--<b>.png`.

Giữ y nguyên thân gốc <NAM/NỮ> đầu trọc ở ảnh 1 (mặt, cỡ người, cỡ đầu, trang phục, quy tắc màu: áo khoác chỉ hồng tím #FF8CFF/#FF00FF/#A000A0). Vẽ ĐÒN ĐÁNH <số> 4 FRAME TAY KHÔNG (vũ khí sẽ gắn sau, hai bàn tay vẫn NẮM như đang cầm cán), ảnh ngang 1536x1024, 2 hàng x 4 cột: hàng trên = hướng <A>, hàng dưới = hướng <B>.
- Đòn 1, chém ngang: frame 1 lấy đà, nắm tay phải kéo ra sau ngang vai; frame 2 quét ngang trước người; frame 3 hết đà, tay phải duỗi về hướng đang nhìn; frame 4 thu về gần tư thế đứng.
- Đòn 2, đâm/chĩa thẳng: frame 1 rút tay phải về hông; frame 2 bước tới; frame 3 tay phải duỗi thẳng chĩa về hướng đang nhìn, tay trái giữ trước ngực; frame 4 thu về.
- Đòn 3, bổ hai tay: frame 1 hai nắm tay chụm giơ cao trên đầu (không che mặt); frame 2 bổ xuống; frame 3 hai tay chạm thấp trước người theo hướng đang nhìn; frame 4 đứng lên.

Hướng lên thì đánh ra xa người xem. Mọi frame cùng cỡ người và cỡ đầu như ảnh 1, đầu trọc, đầu không bị tay che. Chân của cả hàng chạm cùng một đường đáy, không hình nào chạm mép ô hay hình bên cạnh. Không hiệu ứng. Nền phẳng #00FF00, không bóng, hình đặc, không chữ.

### Prompt 5: TÓC 5 class (5 ảnh, mỗi class 1 ảnh, file `hair-<class>-5dir.png`)
Gửi kèm ảnh đầu trọc 5 hướng do Claude cắt ra (`ref/bald-head-<male|female>-5dir.png`).

Ảnh 1 là đầu trọc 5 hướng của thân gốc (xuống, chéo xuống-phải, phải, chéo lên-phải, lên). Vẽ một KIỂU TÓC lên đúng 5 đầu đó, giữ nguyên vị trí và cỡ từng đầu, tóc mọc từ chân tóc ôm sọ (không như mũ dán lên), mái và lọn rủ tự nhiên. Kiểu tóc: <xem dưới>. TÓC CHỈ DÙNG 4 TÔNG XÁM: #242424 (viền), #555555, #999999, #E0E0E0, không màu nào khác (game tự nhuộm màu). Giữ mặt và da y như ảnh 1. Ảnh ngang 1536x1024, 5 đầu một hàng đúng thứ tự và khoảng cách như ảnh 1, nền phẳng #00FF00, không chữ.
- Warrior (nam): tóc ngắn xù rối tự nhiên, vài lọn dựng nhẹ.
- Wizard (nam): tóc mái dài lệch che một bên trán, đuôi tóc ngắn sau gáy.
- Mechanic (nam): tóc xù nhọn hất ngược, có kính bảo hộ trên trán. Kính được tô màu thật: đồng #B07A3A, tròng xanh nhạt #9ED8FF.
- Assassin (nữ): tóc dài buộc đuôi ngựa cao, mái lệch. Nơ buộc được tô màu thật: đỏ #D8343A.
- Cleric (nữ): tóc ngang vai, mái thưa, hai lọn rủ trước tai.

### Prompt 6: MŨ / PHỤ KIỆN ĐẦU RỜI (10 ảnh, mỗi ảnh 2 món, file `headgear-<a>-<b>.png`)
Gửi kèm `ref/bald-head-male-5dir.png` làm mẫu cỡ đầu.

Vẽ 2 món phụ kiện đầu RỜI (không có người, không có đầu) cho game, nét RO Soft Fantasy như các ảnh trước, đúng cỡ để đội lên đầu trọc ở ảnh 1 (bề ngang vành mũ khoảng bằng bề ngang sọ ở ảnh 1). Ảnh ngang 1536x1024, 2 hàng: hàng trên = món <A>, hàng dưới = món <B>. Mỗi hàng 5 góc nhìn cùng thứ tự và khoảng cách như 5 đầu ở ảnh 1 (xuống, chéo xuống-phải, phải, chéo lên-phải, lên). Mặt sau của mũ là mặt sau đặc, không vẽ lòng mũ rỗng. Mép dưới mỗi món (chỗ chạm đầu) nằm trên cùng một đường ngang trong hàng. Màu thật, không dùng xanh lá #00FF00 (nếu món có màu xanh lá thì nền đổi thành #FF00FF). Nền phẳng #00FF00, không bóng, hình đặc, không chữ.

19 món chia làm 10 ảnh (mỗi ảnh 2 món, ảnh cuối 1 món), theo nguồn mũ đã chốt:
- Quà nhiệm vụ tân thủ theo class: (1) mũ sắt nhỏ (Warrior) + mũ phù thuỷ nhọn xanh trắng có khoá vàng và tua (Wizard); (2) kính bảo hộ đồng đeo trán (Mechanic) + băng đô ninja đỏ có đuôi bay (Assassin); (3) khăn trùm tu nữ trắng viền vàng (Cleric) + mũ rơm.
- Shop NPC: (4) băng đô vải xanh + mũ len có quả bông; (5) mũ phi công da có kính.
- Quái rơi: (5) tai thỏ trắng hồng (Horn Rabbit); (6) mũ slime xanh ngọc tròn mềm (Frost Slime) + tai cáo cam (Snow Fox); (7) sừng quỷ nhỏ đỏ (Ice Imp) + mũ ếch xanh rêu (Mire Toad, nếu màu xanh lá thì nền #FF00FF); (8) vòng hoa dại (Thorn Sprite).
- Boss truy nã (MVP): (8) vương miện sói bạc đính đá xanh (Rimeclaw Tyrant); (9) vương miện pha lê băng (Glacial Colossus) + vương miện rắn tím vàng (Mire Hydra Queen); (10) mũ phù thuỷ tối thượng đen tím viền vàng sao bạc (phần thưởng hiếm).

### Prompt 7: VŨ KHÍ (1 ảnh, file `weapons-5-classes.png`)
Vẽ 8 món rời (không người), nét RO Soft Fantasy, mỗi món DỰNG ĐỨNG lưỡi hoặc đầu chĩa lên trên, chuôi ở dưới, nằm giữa ô của nó. Ảnh ngang 1536x1024, 2 hàng x 4 cột:
- Hàng 1: kiếm thẳng chuôi vàng ngọc đỏ (Warrior), khiên đỏ hoa huệ trắng viền vàng (Warrior), gậy phép cán gỗ đầu vòng vàng pha lê xanh (Wizard), sách phép bìa xanh viền vàng đang đóng (Wizard).
- Hàng 2: súng ngắn đồng điểm xanh nhìn ngang nòng chĩa lên (Mechanic), dao găm tím pha lê (Assassin), gậy thánh vàng đầu pha lê xanh (Cleric), khiên trắng viền vàng đá xanh (Cleric).

Cỡ so với thân gốc cao 200 px: kiếm dài 150 px, gậy 190 px, khiên cao 80 px, sách 45 px, súng 70 px, dao 75 px. Nền phẳng #00FF00, không bóng, hình đặc, không chữ.

## Đã chốt (người dùng 19/11/2026)
1. Bỏ quy tắc "vòng dưới chân chỉ cho buff": nhân vật của mình có `PlayerIndicatorRing`.
2. Tên của mình hiện, màu xanh lá sáng.
3. Nguồn mũ: KHÔNG chọn lúc tạo nhân vật (bắt đầu đầu trần).
   - **Quái rơi (nguồn chính, khoảng 60 %):** mỗi bãi quái rơi mũ đặc trưng của loài với tỉ lệ 1-5 %.
   - **Boss truy nã / MVP:** rơi mũ hiếm, nhìn là biết người kỳ cựu.
   - **Shop NPC trong làng (khoảng 30 %):** bán 3-4 mũ phổ thông giá vừa, chơi 15-20 phút là mua được.
   - **Quà nhiệm vụ tân thủ (khoảng 10 %):** NPC tặng mũ khởi đầu theo class sau khi đánh 5 con quái đầu tiên.
   - Đã làm (19/11, báo cáo `plans/reports/headgear-system-261119.md`): id 1-19 theo thứ tự Prompt 6, luật trong `Rpg/HeadgearCatalog.cs`, dữ liệu + neo art trong `Data/hero-layered/headgear.json`; art `Sprites/hero-layered/headgear/<key>.png`, icon `Sprites/icons/headgear-<key>.png` (chưa có thì đầu trần + icon tạm).
