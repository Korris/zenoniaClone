# Ý tưởng gameplay online và cày cuốc

Ngày ghi nhận: 06/11/2026.

Trạng thái: ghi chú ý tưởng để đọc lại và thảo luận sau; chưa phải quyết định thiết kế cuối cùng hoặc yêu cầu triển khai.

## Mục tiêu của người làm game

- Phát triển game hiện tại thành game online.
- Ưu tiên cảm giác cày cuốc thú vị, muốn tiếp tục đánh quái và kiếm trang bị.
- Tham khảo nhịp chơi của Trickster M và Mu Online, đồng thời giữ nét riêng của game hiện tại.

## Các băn khoăn hiện tại

1. Giữ hệ thống combo hiện tại hay làm đánh thường với động tác/hiệu ứng lặp lại đơn giản hơn?
2. Có nên cho dùng skill liên tục để tạo cảm giác farm như Mu Online?
3. Có nên cho trang bị thay đổi ngoại hình nhân vật, hay chỉ thay vũ khí như hiện tại?

Quan sát của người làm game: khi chạy thử khoảng 200 bot đứng cạnh nhau, rất khó phân biệt nếu không có tên trên đầu.

## Hướng đề xuất: giữ combo nhưng giảm thao tác khi farm

- Giữ chuỗi 2–3 động tác đánh thường để nhân vật có cá tính.
- Cho tự nối chuỗi khi giữ nút hoặc khi đã chọn mục tiêu; cần thử nghiệm cách điều khiển phù hợp.
- Animation có thể khác nhau giữa các đòn, nhưng hiệu ứng nên gọn và dễ đọc.
- Đòn cuối có thể mạnh hơn một chút hoặc đánh lan nhỏ; tránh dồn phần lớn sát thương vào cuối chuỗi.
- Đổi mục tiêu, né đòn hoặc quái chết giữa chuỗi vẫn cần cảm giác mượt.
- Combo đòi hỏi thao tác chủ động có thể dành cho boss hoặc PvP; không bắt người chơi lặp thao tác phức tạp hàng nghìn lần khi farm.

Ưu tiên cảm giác đánh trúng rõ ràng, nhịp đều và hạ quái đã tay.

## Hướng đề xuất: skill farm dùng thường xuyên, mỗi nhóm có vai trò

| Nhóm | Vai trò đề xuất |
| --- | --- |
| Đánh thường | Sát thương ổn định, dùng xen giữa các skill |
| Skill farm chủ lực | Hồi nhanh, dùng thường xuyên, thể hiện đặc trưng của class |
| Skill diện rộng hoặc khống chế | Dùng khi gom quái và chọn thời điểm |
| Skill mạnh hoặc phòng thủ | Đối phó quái nguy hiểm, boss và tình huống cần cứu nguy |

- Có thể cho dùng skill liên tục trong nhịp farm, nhưng không nhất thiết mọi skill đều không có hồi chiêu.
- Tránh một skill vừa mạnh nhất, rộng nhất, vừa dùng vô hạn khiến các nút khác mất ý nghĩa.
- Tránh hồi chiêu quá lâu khiến người chơi phần lớn chỉ chờ và đánh thường.
- Chưa chốt thời gian hồi, chi phí tài nguyên hay cơ chế tự dùng skill.

Điểm cần tạo ra: đầu tư vào nhân vật thì thấy farm tốt hơn. Ví dụ, từ đánh từng con sang xử lý một nhóm; từ cần ba lần dùng skill để hạ quái xuống còn hai. Dùng skill liên tục chỉ là một phần của cảm giác cày cuốc.

## Hướng đề xuất: ngoại hình thể hiện tiến trình trang bị

Chỉ đổi vũ khí có thể chưa đủ để tạo khác biệt khi nhiều người cùng class và cùng loại vũ khí.

Thứ tự ưu tiên:

1. Áo giáp hoặc bộ trang phục: thay đổi mảng màu và hình dáng lớn của nhân vật.
2. Vũ khí: thể hiện loại trang bị và cấp nâng cấp.
3. Một điểm nhấn như mũ, tóc hoặc phụ kiện lưng, tùy phong cách game.

- Ban đầu chưa cần hiển thị riêng từng găng tay, giày và mọi ô trang bị.
- Có thể dùng ngoại hình theo bộ hoặc bậc giáp để giới hạn khối lượng vẽ nhiều hướng và animation của game 2D.
- Chưa chốt cách ánh xạ từng món trang bị sang ngoại hình bộ.
- Kiếm được bộ mới cần mang lại thay đổi nhìn thấy ngay; ngoại hình cũng là phần thưởng cho việc cày.

## Đám đông khoảng 200 người

Không lấy việc nhận ra từng người khi tắt tên làm tiêu chuẩn duy nhất. Trong đám đông, cần ưu tiên nhìn rõ:

- Nhân vật của mình.
- Đồng đội và thành viên tổ đội.
- Mục tiêu đang chọn.
- Đối thủ hoặc mối nguy cần chú ý.

Các hướng hỗ trợ: viền chọn, dấu tổ đội, thanh máu hiển thị có chọn lọc và giảm hiệu ứng của người khác. Trang phục khác nhau chỉ giải quyết một phần vấn đề nhận diện.

### Thử 50 bot cùng farm một bãi (06/11/2026)

Server + 50 bot `ZenoniaSwarm` (`--mix 100:0:0`, Lv1, đồ khởi đầu, 5 class luân phiên) + 1 client Warrior chụp hình, Frost Hollow. Ảnh: `C:\temp\cap-crowd50\` (chỉ ở máy đó). Bot swarm cùng đuổi một con quái gần nhất nên dồn chặt hơn người thật.

Thấy trong ảnh: cả đám thành một cục, 3–4 Assassin tóc tím giống hệt nhau đứng sát nhau, tên "Swarm…" chồng thành mảng chữ không đọc được, quái bị che gần hết. Hero của mình chỉ dễ thấy khi đứng ra ngoài cục.

ChatGPT (Tay, "Đánh giá khả năng đọc hình ảnh" https://chatgpt.com/c/6aedf84c-b1d0-83ec-9c96-1e0992c046d5) chấm: art 8.5/10, phân biệt class 7, tìm hero 3, thấy quái đang đánh 2, đọc nguy hiểm 3, tổng 4/10. Nguyên nhân chính là sprite chồng nhau, tên chồng nhau, hero không có dấu riêng, quái không được ưu tiên hiển thị; trang phục giống nhau chỉ ở mức trung bình, màu so với nền tuyết không phải vấn đề. Đề xuất theo thứ tự rẻ trước:

1. Vòng dưới chân hero của mình, hiện viền mỏng hoặc mũi tên khi bị che (không đẩy hero lên trên mọi người bằng sorting cố định).
2. Luật tên: đông người thì chỉ hiện tên party, bạn, mục tiêu, người đang tương tác; giữ Alt để hiện hết.
3. Vòng đỏ dưới quái đang chọn, thanh máu mục tiêu cố định trên màn hình, vùng báo đòn quái luôn rõ.
4. Giảm hiệu ứng người lạ (khoảng 20–35%), party 60–75%, của mình và đòn quái 100%.
5. Giãn đội hình mềm quanh mục tiêu (không dùng va chạm cứng), làm ở logic di chuyển phía server.
6. Palette swap 4–6 biến thể mỗi class, rồi mới tới ngoại hình theo bậc giáp.

Đã làm thử (06/11/2026, chưa commit), cùng chat ChatGPT trên chấm lại:

- Vòng vàng dưới chân hero mình + chữ V vàng trên đầu khi bị người khác che (`Net/Client/LocalHeroMarker.cs`, chỉ online): vòng 9/10, V 8.5/10 (tam giác đặc lúc đầu dễ nhầm dấu nhiệm vụ).
- Luật tên (`Net/Client/NameTags.cs`): gần hero xếp trước, bỏ tên đè lên tên khác hoặc lên vòng vàng, tối đa 8, tên đang hiện được ưu tiên để khỏi nhấp nháy, giữ Alt hiện hết: 8/10, hết mảng chữ. Tổng độ dễ đọc từ 4 lên 7.5/10.
- Mục tiêu (`Net/Client/TargetMarker.cs`): quái mình đánh trúng gần nhất trong 4 s, vòng đỏ 4 đoạn dưới chân + thanh máu cố định giữa trên màn hình. Đã thấy thanh máu trong ảnh, chưa thấy vòng đỏ (lần sửa cỡ vòng chưa chụp được frame có đánh trúng), chưa gửi ChatGPT.
- Công cụ test: bot Swarm đánh xa (Wizard, Mechanic, Cleric) dừng ở 45 đơn vị thay vì sát quái; client chụp có `--net-bot crowd` (đánh con quái gần đám đông nhất). 50 bot một bãi giết sạch quái rất nhanh: mật độ quái chưa đủ cho bãi đông.

### Nhận diện nhân vật: vấn đề chính (06/11/2026)

Người làm game: marker chỉ giúp tìm mình; vấn đề thật là nhân vật giống nhau quá, chơi như với một đám bản sao. ChatGPT đồng ý và đề xuất:

- Ưu tiên theo hiệu quả/chi phí: màu tóc (P0) > màu trang phục theo vùng (P0) > kiểu tóc, mũ, phụ kiện lưng (P1) > giáp theo bậc, pet, huy hiệu guild (P2) > aura, danh hiệu, idle riêng (P3). Không nên đổi cỡ người, aura cho mọi người, pet cho mọi người khi chưa có luật ẩn.
- Đổi màu giải quyết phần lớn cảm giác bản sao giai đoạn đầu nhưng chưa tạo cá tính lâu dài; về sau cần cả đổi dáng (tóc, mũ, phụ kiện). Wizard (mũ trùm), Cleric (mũ), Mechanic (kính), Assassin (tóc buộc): đổi màu tóc tác động không đều, cần thêm một vùng màu trang phục dễ thấy.
- Cách làm: không bắt ChatGPT vẽ lại sheet theo từng màu (lệch dáng, mắt, frame). Giữ sheet gốc, tạo mask cùng kích thước bằng script (R tóc, G vải chính, B vải phụ), shader đổi màu giữ sáng tối; giữ màu nhận diện class (giáp, viền vàng, kính, mặt nạ, biểu tượng thánh). Không đổi hue thô với tóc đỏ, dùng bảng màu có kiểm soát độ sáng.
- Mốc 1 "Character Identity v1": 5 class × 6 màu tóc × 4 phối màu trang phục, 0 animation mới, chọn màu khi tạo nhân vật, lưu và đồng bộ online. Mốc 2: mỗi class 1 kiểu tóc hoặc mũ thay thế, thử trên idle, walk, đánh thường trước.
- Test A/B bằng 50 bot: hiện tại / màu ngẫu nhiên / màu + vũ khí các bậc / + phụ kiện mẫu; đo số cặp trông như bản sao, bám theo 1 bot 5 giây, còn nhận ra class không.
- Bẫy: giáp theo bậc làm sớm (5 class × 5 bộ × ~160 frame ≈ 4.000 frame).

Bản xem trước đầu tiên (06/11/2026) cho tự chọn cả màu tóc và màu áo (8 × 6), ảnh đã xoá vì cách này bị bác ở dưới.

ChatGPT chấm độ đa dạng: A 1/10, B 7/10, C 8/10. A→B cải thiện lớn nhất; vẫn nhận ra cùng một model (tóc dựng, giáp). Góp ý: tóc đen nâng lên than xanh (~#343D50) để không lẫn viền; tóc xanh lá giảm bão hoà 15–20%; bỏ áo teal (gần xanh lá, xanh dương) thay cam đồng hoặc nâu đỏ; áo xám nâng sáng; giữ nguyên giáp, viền vàng, cổ lông trắng; giữ mắt đỏ mặc định (đừng đổi theo tóc); shader nên ánh xạ theo dải tối/giữa/sáng của bảng màu đích thay vì HSV thô, thử thêm nền hang tối. Mảnh dáng đầu tiên nên làm cho Warrior: mũ hoặc băng trán (ví dụ băng trán có 2 sừng kim loại nhỏ), 1 mẫu, thử đủ 4 hướng trước.

**Người làm game bác việc cho tự chọn màu áo (06/11/2026):** đồ trên người là bộ giáp (trang bị có chỉ số), không phải quần áo mặc tạm như game khác. Mỗi người tự chọn màu giáp là vô lý; màu/ngoại hình giáp chỉ được đổi theo bộ giáp đang mặc. ChatGPT đồng ý, sửa đề xuất:

- Tự chọn khi tạo nhân vật: màu tóc (8), sau này kiểu tóc; màu mắt, màu da ưu tiên thấp. Giáp, áo choàng, khăn, kim loại, viền: chỉ theo giáp đang mặc. Vũ khí theo tier như hiện tại. Aura chỉ theo cường hoá/trạng thái.
- Nguyên tắc: cùng class, cùng giáp, cùng cấp cường hoá thì giáp trông giống nhau; chỉ tóc khác.
- Bản đầu "giáp đổi ngoại hình": dùng lại cách đổi màu nhưng `armorAppearanceId` lấy từ giáp đang mặc, đổi màu vải và có thể cả kim loại, viền. Bắt đầu 5 kiểu giáp mỗi class (25 bảng màu), vẫn một bộ sheet mỗi class. Đây chỉ là bản tạm trước khi vẽ dáng giáp thật.
- Đừng để một bộ giáp luôn mạnh nhất: chia theo nhánh (ví dụ cùng khoảng Lv30: thiên ATK, thiên DEF, thiên HP, thiên tốc độ) có đánh đổi thật, nếu không cuối cùng mọi người vẫn mặc giống nhau.
- Rút lại điểm B: 35 Warrior Lv1 cùng giáp khởi đầu, chỉ khác tóc khoảng 4–5/10 (chấp nhận được với người mới); nhiều cấp, 5 kiểu giáp + tóc 7–8/10; thêm vũ khí 8/10; thêm kiểu tóc/mũ 9/10.
- Mốc mới: 1A màu tóc (lưu `hairColorId`); 1B 5 bảng màu giáp mỗi class theo giáp đang mặc. Test 50 bot theo trang bị hợp lệ từng khoảng cấp (Lv1 / Lv10–40 / Lv40–60), không phát đồ ngẫu nhiên cho ảnh đẹp. Sau đó nếu chỉ đầu tư một thứ art mới: dáng riêng cho giáp cấp cao.

Game đã có ô giáp 10 bậc (`Rpg/ItemDefs.cs`: Leather Vest … Dragon Armor) nhưng hiện chưa đổi ngoại hình.

Bản xem trước theo giáp (07/11/2026, chưa đụng game): `Tools/preview-warrior-hair-armor-recolor.py` (`masks`, `lineup`, `check`, `crowd`). Tách sheet `body/bare` Warrior thành tóc, vải (áo choàng, khăn, áo lót), kim loại (giáp, găng, giày), viền vàng; tóc theo lựa chọn, vải + kim loại theo 5 kiểu giáp (mỗi kiểu 2 món): rookie nâu (Leather Vest, Steel Plate), frost xanh băng + bạc (Bronze, Frost Plate), crimson đỏ + thép đen (Flame, Royal Plate), arcane tím + thép ánh tím (Verdant, Arcane Armor), dragon đỏ sẫm + vàng tối (Celestial, Dragon Armor). Ảnh: `docs/reference/warrior-armor-looks-5-palettes.jpg`, `docs/reference/warrior-armor-crowd-by-level-band.jpg` (35 Warrior: Lv1 toàn rookie, Lv10–40 rookie/frost/crimson + vũ khí bậc 1–5, Lv40–60 crimson/arcane/dragon + bậc 5–9). Đã soát `idle`, `walk`, `melee1` 4 hướng; mắt đi theo màu tóc (giữ mắt đỏ cần tách mask mắt riêng); còn: găng có frame giữ nâu gốc, mắt ở `melee1` hướng xuống hơi nâu đỏ. Chưa soát các động tác khác.

Người làm game: lần đầu thấy game giáp giữ nguyên dáng, mặc giáp khác chỉ đổi màu. ChatGPT (cùng chat, có tra web) trả lời: tái dùng sprite và đổi palette là kỹ thuật RPG lâu đời nhưng không phải mọi món giáp chỉ đổi màu; ví dụ gần: Diablo (1996) gom giáp thành 3 nhóm dáng nhẹ/vừa/nặng; Diablo II nhiều món dùng chung vài dáng giáp; Ragnarok Online thân nhân vật theo nghề, nhiều giáp không đổi sprite, có hệ đổi palette trang phục riêng (không theo giáp); Tibia, Ultima Online có nhuộm/đổi màu nhưng nhiều giáp vẫn có dáng riêng. Không có bằng chứng Zenonia, MapleStory, Lineage làm đúng kiểu này. Rủi ro: giáp cấp cao (Dragon, Celestial) chỉ đổi màu sẽ thấy nâng cấp "giả"; nhóm Lv40–60 trông đa dạng phần lớn nhờ vũ khí, bỏ vũ khí vẫn là cùng một bộ giáp đổi màu.

Đề xuất: giữ 5 bảng màu, thêm 3 cấp dáng bằng giáp vai (không dùng mũ vì che tóc chibi): Basic (rookie, frost) vai nhỏ hiện tại; Advanced (crimson, arcane) vai to hơn có crest hoặc pha lê; Legendary (dragon) vai lớn góc cạnh hoạ tiết rồng. Vai xoay theo tay khi chém nên cần vị trí và thứ tự che từng frame: thử 1 bộ vai Dragon qua 4 hướng × idle/walk/attack trước. Điểm 5 kiểu: rookie 8, frost 8.5 (thích nhất), crimson 8, arcane 7 (thiếu dấu hiệu phép), dragon 6.5 (chưa đủ legendary). Tên món nên khớp nhóm màu (Bronze Plate đang dùng màu Frost, Verdant Armor dùng tím Arcane). Đám đông: Lv1 5/10, Lv10–40 7.5/10, Lv40–60 8/10. Ảnh concept giáp vai: `docs/reference/warrior-pauldron-silhouette-concept-chatgpt.png` (ChatGPT vẽ đè lên ảnh đám đông, không phải sprite). Chat: "Đánh giá khả năng đọc game" https://chatgpt.com/c/6aedf84c-b1d0-83ec-9c96-1e0992c046d5.

Thử giáp vai rồng (07/11/2026): ChatGPT (Tay, "Create Dragon Pauldrons Sprite Sheet" https://chatgpt.com/c/6aee1183-cce8-83ec-a602-66f5885582b2) vẽ 5 mảnh (trước trái/phải, ngang, sau trái/phải), ảnh gốc và mảnh cắt ở `Assets/ArtLibrary/classes/warrior/armor-src/`. Ghép trong `preview-warrior-hair-armor-recolor.py pauldrons`: vị trí tự dò theo hai đầu cổ lông (hướng ngang: mép dưới cổ lông), rộng 38 px trong ô 192, vẽ trên thân dưới vũ khí. Đã xem `idle`, `walk` 4 hướng: nằm đúng vai, không che mặt. Chưa làm đòn chém (vai xoay theo tay, cần đặt tay từng frame). Ở cỡ đám đông giáp vai khó thấy hơn ở ảnh phóng to. Ảnh: `docs/reference/warrior-dragon-pauldron-test-zoom.jpg`, cột cuối của `warrior-armor-looks-5-palettes.jpg`. Chưa chốt làm gì.

## Phạm vi thử nghiệm nhỏ trước khi mở rộng

- Một class và một bãi quái.
- Combo tự nối, nhẹ thao tác.
- Một skill farm có hồi chiêu ngắn.
- Ba bậc trang bị có ngoại hình khác nhau.
- Chơi thử khoảng 20–30 phút để đánh giá: nhặt được món mới và mạnh lên có khiến người chơi muốn tiếp tục farm không?

## Hướng tạm nghiêng về

Giữ combo hiện tại nhưng giảm thao tác lặp; có skill farm dùng thường xuyên; bổ sung ngoại hình bộ giáp theo từng bước. Cần chơi thử trước khi chốt hoặc mở rộng triển khai.

### Trickster M và đề xuất áo + găng + giày, không mũ (07/11/2026)

Người làm game đề xuất: giáp thân, găng, giày đều đổi ngoại hình theo món đang mặc (trước mắt đổi màu), không hiện mũ. Hỏi ChatGPT (cùng chat, có tra web) Trickster M làm sao:

- Trickster M (NCSOFT, 2D pixel chibi, 8 nhân vật gốc) đổi ngoại hình chủ yếu bằng hệ **Fashion** (giống biến hình của Lineage M): Fashion có độ hiếm, cộng chỉ số, tốc độ, hiệu ứng skill, có thể biến thành nhân vật thú khác; lấy qua triệu hồi (gacha). Thêm Pet, Title. Có va chạm giữa nhân vật (tranh chỗ đứng là chiến thuật).
- Không xác minh được: nhân vật dựng bằng lớp (paper-doll) hay sprite liền, số hướng, giáp/găng/giày/mũ có đổi hình không, đổi màu tóc, nhuộm, mount, cách xử lý tên và hiệu ứng khi đông.
- Hướng của mình (trang bị nào thì ngoại hình đó) khác Trickster M (Fashion có chỉ số). Học: dáng chibi rõ, nhiều ngoại hình khác nhau thật chứ không chỉ đổi màu, tách hệ ngoại hình khỏi logic animation. Không nên chép Fashion gacha có chỉ số.
- Về đề xuất: giáp thân đổi ngoại hình đồng ý hoàn toàn; không hiện mũ đồng ý (giữ tóc làm nhận diện); găng, giày đổi màu theo món làm được; găng, giày vẽ dáng riêng từng bậc chưa nên làm vì ở 80–90 px găng chỉ vài pixel, giày khó thấy khi chạy mà vẫn phải làm đủ 4 hướng × mọi động tác. Có ngân sách vẽ thì ưu tiên dáng giáp thân (vai) trước.

Game đã có ô Gloves, Boots 10 bậc trong `Rpg/ItemDefs.cs` (Cloth Gloves … Dragon Gauntlets, Leather Boots … Dragon Boots). Chưa chốt làm gì.

### Người làm game chốt hướng (07/11/2026): vai thuộc giáp, áo + găng + giày đổi theo món, không mũ

- Giáp vai không phải món riêng mà là một phần của bộ giáp (bậc giáp đổi màu vải, màu kim loại và ở bậc cao đổi dáng vai).
- Giáp thân, găng, giày mỗi món tự đổi ngoại hình theo món đang mặc (trước mắt đổi màu). Không hiện mũ.

Bản xem trước: `Tools/preview-warrior-hair-armor-recolor.py` (`mix-lineup`, `mixed`): kim loại tách thành găng (bán kính 15 px quanh anchor kiếm và khiên, cả lớp `body/hand`), giày (24% dưới của dáng người), còn lại là giáp thân; giáp rồng tự có vai (đứng, đi). Ảnh: `docs/reference/warrior-armor-looks-5-palettes.jpg`, `warrior-armor-gloves-boots-mix.jpg` (6 bộ phối), `warrior-armor-gloves-boots-mixed-crowd.jpg` (35 Warrior tự phối theo khoảng cấp).

ChatGPT (cùng chat): duyệt hướng này. Ở cỡ trong game giáp thân 9/10, vai rồng 8/10, găng 5/10, giày 3/10: giáp + vũ khí tạo 80–90% khác biệt; găng, giày giữ đổi màu (đã làm được) nhưng chưa đáng vẽ dáng riêng. Phối lẫn không lố; lộn xộn nhất là Frost + Dragon + Crimson. Nên có "trông đồng bộ" khi mặc đủ bộ (đã có sẵn vì cùng bảng màu), chưa cần set bonus chỉ số (dễ ép mọi người mặc cùng bộ). Vai thuộc giáp rồng: đúng, giữ. Bậc tiếp theo nên có dáng vai: Arcane (đầu pha lê tím hoặc crest), rồi Crimson (thép đen cạnh nhọn, hai lớp); Frost, Rookie giữ. Đừng nhiều gai, sừng (dính tóc, vũ khí ở 80–90 px). Cần sửa: găng Crimson/Arcane lẫn vào tay áo (thêm viền cổ tay 1–2 px), giày Rookie quá tối (nâng 10–15%), kiểm tra vai rồng khi chém. Chưa chốt làm gì.

### Người làm game làm rõ (07/11/2026): mỗi món giáp Lv1–60 phải có ngoại hình khác hẳn, không chỉ đổi màu

Cấp 1–60 = 7 bậc giáp (Leather Vest … Verdant Armor). Hai cách: (1) vẽ lại toàn bộ thân Warrior cho từng bộ (đúng nhất, rất nặng); (2) giữ thân, vẽ mảnh giáp đắp lên (ngực, vai, sau có thể găng, giày) đặt theo từng frame như vũ khí. Người làm game chọn thử cách 2 trước, không ổn thì cách 1.

Thử cách 2 với Steel Plate (Lv10): ChatGPT (Tay, chat "Create Dragon Pauldrons Sprite Sheet") vẽ 7 mảnh thép xanh xám (giáp ngực trước, ngang; vai trước trái/phải, ngang, sau trái/phải), ảnh gốc và mảnh cắt `Assets/ArtLibrary/classes/warrior/armor-src/steel-*`. Ghép bằng `Tools/preview-warrior-hair-armor-recolor.py kit steel`: ngực rộng 0,66 cổ lông, đặt dưới cổ lông (hướng sau bị áo choàng che nên không có); vai thép thu 0,74 so với vai rồng, hướng ngang dời ra sau 5 px. Đã xem đủ đứng, đi 4 hướng: bám đúng, không che mặt. Ảnh so Lv1/Lv10: `docs/reference/warrior-armor-steel-plate-kit-test.jpg`. Chưa làm: đòn chém, dash, hurt, skill; tay, chân vẫn là giáp da gốc; hướng ngang giáp ngực bị khiên che gần hết. Phần đổi màu theo bậc (shader + mask, `ArmorLooks.cs`, protocol 15) đã vào game nhưng chưa commit, chờ quyết định gộp với cách 2.

### Chốt cách 1 (07/11/2026): cứ 10 cấp một bộ trang phục vẽ lại cả thân

Người làm game thấy bản dán mảnh giáp (cách 2) "nhìn hề", chọn cách 1. ChatGPT (cùng chat đánh giá) cũng chỉ ra lý do bản dán trông như sticker: ngực phẳng như mai rùa, vai tròn như bóng, lệch nét với thân, tay chân vẫn giáp da.

Bản thiết kế 7 bộ (Lv1 Leather Vest giữ như hiện tại, Lv10 Steel, Lv20 Bronze vảy, Lv30 Frost bạc xanh băng, Lv40 Flame sắt đen viền lửa, Lv50 Royal trắng vàng áo choàng xanh, Lv60 Verdant xanh lá kiểu elf): `docs/reference/warrior-armor-lv1-60-design-v1.png`, chat "Warrior Armor Progression" (Tay) https://chatgpt.com/c/6aee1c6e-7fa0-83ec-9316-e085a41216e5. Chờ người dùng duyệt thiết kế. Bậc art của giáp = itemLevel/10, nên Lv1…Lv60 ứng với tier 0…6.

Đã vào game (07/11/2026, chưa commit): `ArmorLooks.cs` chọn bộ thân theo bậc giáp đang mặc (itemLevel/10: 0 Leather … 6 Verdant), `HeroRenderer.SetOutfit` nạp `Sprites/classes/warrior/body/<t1-steel…t6-verdant>/{bare,hand}`; hàng chưa vẽ dùng hàng Lv1. Phím debug E/Q đổi cả vũ khí lẫn bộ trang phục cùng bậc. Online gửi bậc giáp qua `NetAppearance.ArmorTier` (protocol 15). Sheet dựng bằng `Tools/build-warrior-outfit-sheets.py` (khớp chiều cao, chân, tâm tóc vào frame cũ nên dùng chung anchor vũ khí). Đã vẽ: Steel đủ đứng 4 hướng, đi 4 hướng, chém 1 hướng xuống; Bronze/Frost/Flame/Royal/Verdant chỉ đứng hướng xuống (lần đầu ChatGPT vẽ tay buông sát người nên kiếm lơ lửng, vẽ lại theo tư thế tay dang). Ảnh trong game: `docs/reference/warrior-outfits-lv1-60-idle-down-ingame.png`. Phần đổi màu theo vùng (shader, mask) đã bỏ.

### Chi phí vẽ lại cả thân theo bộ, cách các game khác làm (07/11/2026)

Người làm game lo: cách 1 phải vẽ lại toàn bộ sprite cho mỗi bộ. ChatGPT (cùng chat, tra web) trả lời:
- Trickster M: asset nhân vật nằm trong atlas animation đã "bake" sẵn (bài trích asset trên RaGEZONE, resource `bakedvfanimations`); Fashion giống biến hình Lineage M. Không xác minh được là sprite liền mỗi bộ hay ghép lớp. Trickster Online 2003: file .NRI/.BAC/.PAL, có palette và Item Fusion (lấy ngoại hình món này, chỉ số món kia). Không suy bản M từ bản 2003.
- MapleStory: ghép lớp thật (body, head, face, hair, cap, coat, pants, shoes, gloves, shield, cape, weapon) có anchor và thứ tự lớp; Nexon cho người làm đồ vẽ trên body mẫu. Ragnarok: body theo nghề + đầu, mũ, vũ khí, khiên, áo choàng riêng, có palette.
- Ước tính (5 class, 7 bộ, 40 dải, ~5 frame): (a) thân đầy đủ mỗi bộ 1.400 dải / 7.000 frame; (b) ghép lớp 200 dải thân + ~1.400 dải lớp giáp, rủi ro cao vì AI phải vẽ khớp thân (thử Steel đã hỏng); (c) 3 dáng thân + đổi màu ~600 dải; (d) vẽ lại có kiểm soát từ frame Lv1 (đang làm), vẫn 1.400 dải nhưng tái dùng tư thế, anchor.
- Khuyên (d): giữ nguyên đầu, tóc, mặt, chân, toạ độ tay; script kiểm tra căn chỉnh; sửa tay frame lỗi. Giảm chi phí bằng cách chia nhóm: Lv1/Lv10 đơn giản, Lv20/30 và Lv40/50 dùng chung một phần cấu trúc, Lv60 riêng hoàn toàn. Ghép lớp chỉ đáng khi có hàng trăm tổ hợp đồ.

### Zenonia S làm thế nào (07/11/2026)

Ảnh người dùng gửi: `docs/reference/zenonia-s-equipment-armor-changes-body.png` (cùng nhân vật, hai món giáp khác nhau). Giáp quyết định cả bộ trang phục, kể cả tay chân (bộ xanh ↔ bộ đen trắng); mũ (băng đô hoa hồng xanh) hiện thành món đeo trên đầu; găng và giày giống nhau ở hai ảnh và không hiện riêng. Người làm game chốt hướng: chỉ giáp và mũ đổi ngoại hình, găng giày không.

ChatGPT (cùng chat) duyệt quy tắc "Character Appearance v2": giáp = cả bộ trang phục; mũ = phụ kiện nhỏ trên tóc; găng, giày chỉ đổi chỉ số; vũ khí, khiên lớp riêng như cũ; màu tóc cá nhân. Đọc ảnh Zenonia S đúng, nhưng hai ảnh chưa chứng minh mọi găng/giày đều không hiện; không tìm được tài liệu Zenonia S dùng sprite liền hay ghép lớp. Mũ không cần cùng bộ với giáp.
Mũ cho đầu chibi: cao 8–15% chiều cao đầu; băng đô rộng 50–75% bề ngang đầu; hoa, đá, crest 15–25%. Trước: ôm theo đường tóc, một phần chân bị tóc che; ngang: thấy độ dày; trái lật từ phải nếu đối xứng; sau: nằm sau hoặc trên đỉnh tóc. Không đặt cả mũ một lớp trước tóc (một phần sau tóc, phần trang trí trước tóc). Đừng chỉ đặt theo điểm tóc cao nhất: neo theo đầu, có offset và thứ tự vẽ theo hướng/animation/frame. Gợi ý 10 bậc: băng da, băng trán thép, crest đồng, băng đô pha lê xanh, crest lửa, vương miện thép đen ngắn, nguyệt quế vàng, trang sức trán tím, tiara bạc cánh nhỏ, dragon crest 2 sừng ngắn. Tránh mũ trùm kín, mặt nạ, sừng dài. Assassin (tóc buộc cao), Wizard (mũ trùm) cần quy tắc riêng.
Trước khi vẽ hàng loạt: khoá toạ độ nắm tay theo frame gốc, chiều cao, cỡ đầu, vị trí chân thống nhất giữa các bộ, giáp không che sai khuỷu tay khi chém. Bài test: làm đủ một bộ (đứng, đi, chém 1 cả 4 hướng + 1 skill tay mạnh), gắn 3 mũ mẫu (băng đô, crest, dragon crest), xem mũ có trượt, bị tóc che sai, che mặt khi đổi hướng không; ổn mới nhân lên 7 bộ.

### Warrior trông đầu to hơn Wizard, Mechanic (07/11/2026)

Đo idle hướng phải (ô 192): cao người wizard 160, mechanic 163, warrior 164; đỉnh tóc tới cằm 51% / 54% / 51% (giống nhau); bề ngang mặt (da) 38 / 49 / 66 px; tóc ~91–96 px cả ba. Ảnh: `docs/reference/warrior-head-proportion-vs-wizard-mechanic.png`.
ChatGPT: đầu không cao hơn, nhưng mặt quá rộng, mắt hơi to, tóc phồng, cổ lông dày sát cằm làm đầu + vai thành một khối; Wizard/Mechanic có chuyển tiếp đầu → cổ → vai rõ. Không nên thu cả đầu bằng script (đầu thấp hơn class khác, hở cổ, mất tóc). Mục tiêu: cao người giữ 164, đầu tới cằm 80–83 px, mặt 54–58 px, tóc 88–92 px (giảm phồng hai bên/sau 5–7%, giữ lọn nhọn), mắt 90–95%, cổ lông mỏng hơn 15–20% và không chạm cằm. Thử trên một frame idle phải, so ở cỡ trong game, chốt rồi áp cho mọi bộ và động tác. Bộ Steel (không có cổ lông) vẫn trông đầu to vì thân hẹp hơn. Có đoạn "Warrior proportion lock" cho prompt trong chat.

### Bộ đồ theo bậc vũ khí (07/11/2026)

Người làm game: mỗi bậc là một bộ đồng bộ kiếm + khiên + giáp + mũ; mũ đổi ngoại hình như Zenonia S (phụ kiện nhỏ trên tóc). Bộ mặc định cho Lv1, các bộ khác Lv20 → Lv100 (T0 Lv1, T1 Lv20 … T9 Lv100). Thiết kế 10 bộ theo chủ đề kiếm/khiên (T0 hiện tại, T1 vàng-xanh, T2 đỏ crimson, T3 lửa phượng, T4 băng, T5 sấm vàng-xanh, T6 đen vàng cánh dơi, T7 tím đầu lâu, T8 trắng vàng cánh, T9 rồng đỏ vàng): `docs/reference/warrior-sets-10-tiers-design.png` (chat "Warrior Armor Progression"). Bản 7 bộ Lv1–60 trước (Steel, Bronze…) bỏ. Chưa đổi: tên giáp trong `ItemDefs.ArmorTierNames` và cấp mặc (`EquipLevel`) để khớp Lv20…100.
Người làm game: T3 và T9 giống nhau, mũ nhìn "lỏ" (bỏ mũ hoặc thử kiểu khác). Vẽ lại T3 (phượng hoàng: sắt cháy đen, lửa cam vàng, vai lông vũ, áo choàng cam cháy mép) và T9 (rồng: vảy đỏ thẫm, viền vàng đậm, vai đầu rồng, áo choàng đỏ sẫm thêu rồng vàng), không mũ: `docs/reference/warrior-sets-t3-flame-t9-dragon-v2.png`.
Thử 8 kiểu mũ cùng màu (`docs/reference/warrior-helmet-styles-8.png`). Người làm game chốt: mũ theo chủ đề từng bộ, mỗi bộ một kiểu. Bảng 10 bộ có mũ theo chủ đề (T0 Lv1 … T9 Lv100): `docs/reference/warrior-sets-10-tiers-themed-helmets.png`.
Chốt (07/11/2026): T0 Lv1 không mũ (bộ mặc định, tóc trần). T5 Lv60 mũ mới: vương miện gai sét vàng trên mũ thép xanh đậm (`docs/reference/warrior-sets-t0-t5-helmet-fix.png`, phương án C). Còn hỏi: vũ khí bậc nào đổi chủ đề vì trùng sét (bậc 0 kiếm/khiên iron có sét xanh và bậc 5 búa thunder).
Kiếm, khiên bậc 0 (Lv1) đổi theo hình màn chọn nhân vật (07/11/2026): kiếm lưỡi thép vết nứt đỏ chuôi vàng, khiên sắt đen hoa văn bạc; bỏ chủ đề sét (chỉ còn ở bậc 5). ChatGPT chat "Generate Weapon Sheet"; nguồn `sword-src/mu-ref-v2/select-sword-pair-chatgpt.png`, `shield-src/mu-ref-v2/select-shield-pair-chatgpt.png`; glow kiếm bậc 0 đổi #ff6a4a, khiên #d9dde6. Ảnh trong game: `docs/reference/warrior-t0-weapons-from-hero-select-ingame.png`.

Phím H đổi màu tóc (thử, 07/11/2026): `HairColors.cs` + `Resources/Shaders/SpriteHairRecolor.shader`, ảnh vùng tóc `body/hair-mask`, `body/set-t*/hair-mask` (`Tools/build-warrior-hair-masks.py`). Chỉ đổi trên máy mình, không lưu, không gửi online; 8 màu như bản xem trước. Lỗi biết: Lv1 mắt đổi theo tóc; T2 chùm lông đỏ trên mũ bị đổi theo tóc và còn vệt đỏ ở mép tóc mái. Ảnh: `docs/reference/warrior-hair-colour-test-ingame.png`.
Dáng đứng nhìn xuống của T1, T2 lệch bản Lv1: Lv1 hơi xoay người (nắm tay trái thấp ngang hông, phải cao ngang ngực, áo choàng xoè rộng); ChatGPT vẽ đứng thẳng đối xứng. Cần vẽ lại.

### Đổi màu tóc: các game làm thế nào (07/11/2026)

ChatGPT (chat đánh giá, có nguồn): MapleStory tóc là lớp sprite riêng có quy tắc che với mũ; Ragnarok, Trickster Online dùng palette .PAL (màu tóc theo bảng màu định sẵn); Terraria texture tóc riêng + shader khi vẽ; Stardew Valley tóc lớp riêng, màu lưu trong dữ liệu nhân vật. Chung: game biết pixel nào là tóc từ dữ liệu art, không đoán theo màu. Zenonia, Elsword, Moonlighter, Octopath: chưa xác minh.
Khuyên: bỏ tìm tóc theo màu đỏ. Giai đoạn đầu giữ thân liền mỗi bộ + một HairMask đã soát cho mỗi frame (thêm HelmetOcclusionMask nếu cần), đổi màu bằng palette 3 dải tối/giữa/sáng thay vì HSV; "chưa có HairMask đã soát thì frame chưa xong". Không khuyên cách nhờ AI vẽ bản tóc màu phẳng rồi so (AI lệch vài px là mask nhiễu) trừ khi bản tô được làm bằng thao tác thay pixel trên cùng ảnh. Dài hạn: nếu đầu các bộ trùng khớp, dùng chung một bộ hoạt hình tóc cho cả 10 bộ (lớp tóc riêng), nhưng mũ vẽ liền thân thì phải xử lý tóc trước/sau mũ. Bẫy khác: T9 mạnh nhất khiến người cấp cao mặc giống nhau (về sau cho nhiều món cùng cấp khác nhánh); bộ chưa vẽ đủ động tác thì lúc phát hành không cho mặc, script build kiểm tra đủ (class, giáp, hướng, động tác).

**Người làm game chốt (07/11/2026): bỏ đổi màu tóc.** Mỗi class có 5 kiểu tóc khác nhau, mỗi kiểu một màu cố định. Đã gỡ phím H, shader và ảnh đánh dấu tóc. Chưa chốt: tóc là lớp riêng (vẽ 5 kiểu cho mọi frame, thân các bộ không có tóc, mũ phải nằm xen với tóc) hay mỗi bộ vẽ 5 lần.

**Cách làm 5 kiểu tóc dễ nhất (ChatGPT, 07/11/2026):** chọn tách **nguyên đầu** (mặt + tai + tóc) thành lớp riêng dùng chung cho mọi bộ giáp; mũ thành lớp riêng đè lên đầu (cần 2 lớp nếu mũ có phần trước và sau tóc). Mỗi frame thân có điểm neo cổ (neckX, neckY, xoay, tư thế đầu); đầu đặt pivot vào đó. Đầu ít đổi giữa các động tác: bắt đầu 3 tư thế đầu mỗi hướng (bình thường, chém, bị đánh) → 5 kiểu × 4 hướng × 3 = 60 hình đầu/class (60–100). So: lớp tóc riêng ~1.000 frame tóc; vẽ lại ×5 = 2.000 dải; mũ che hết tóc thì 5 kiểu tóc vô nghĩa; chỉ hiện tóc trong thị trấn cũng không nên. Chi phí chuyển: thân các bộ đã vẽ phải bỏ đầu (cắt theo đường cổ bằng mask, đầu mới che mép nối); mũ có miếng che má hay tóc đè vai có thể phải sửa tay. Bản thử nhỏ nhất: 1 bộ (Lv1), 2 kiểu tóc khác dáng, 4 hướng, 3 tư thế đầu, 1 mũ có phần trước/sau tóc, thử đứng, đi, dash, chém 1; xem đầu có trượt, cổ có hở, mũ có ôm tóc tự nhiên không.
T2–T8 đứng nhìn xuống đã vẽ (đúng dáng Lv1, còn tóc đỏ và mũ vẽ liền), chưa vào game; T9 chưa vẽ. T6 có biểu tượng dơi giống logo Batman, T7 tóc đâm xuyên mũ trùm, T8 mép áo choàng trơn.

**Bản thử lớp đầu riêng (07/11/2026):** chỉ Warrior thân Lv1, `idle` hướng xuống (4 frame thở), 2 kiểu tóc: 0 = tóc đỏ nhọn gốc, 1 = tóc ngắn vuốt ngược xanh đen (ChatGPT sửa từ ảnh đầu cắt, chat https://chatgpt.com/c/6aee9a6a-ac28-83ec-8617-be45be6c8f60; mắt, má, miệng, cằm ghép lại từ mặt gốc). Thân không đầu `body/headless/idle.png`, đầu `heads/<kiểu>/down.png` (ô 192 cùng khung thân, đặt sẵn cho frame 0), độ lệch thở từng frame `Data/classes/warrior/head-anchors.json` (0; -0,7; -1,4; -0,7 px, đo lại trên frame khớp). Code `HeroHeadLayer.cs` + `HeroRenderer.HairStyle`; đầu cùng thứ tự vẽ với thân, gần camera hơn, khiên, kiếm, nắm tay vẫn đè lên. Phím H đổi kiểu (chỉ máy mình, không lưu, không gửi online), chụp `--hairstyle N`. Tool lúc đó `Tools/build-warrior-head-layer.py` (nay gộp vào `Tools/build-head-layer.py`). Ảnh: `docs/reference/warrior-head-layer-prototype-ingame.png` (hàng trên kiểu 0, dưới kiểu 1, 4 frame thở). Kiểu 0 trùng từng pixel với frame 0 cũ; frame 1-3 mềm hơn chút (GPU nội suy song tuyến, sheet cũ uốn bicubic). Giới hạn: hướng khác, động tác khác và thân các bộ T1+ (mũ vẽ liền) vẫn vẽ thân liền; kiểu 1 có thể đè lên lông cổ áo nếu tóc dài hơn; kiểu tóc chưa lưu trong dữ liệu nhân vật, chưa đồng bộ online.

**5 kiểu tóc cho cả 5 class (07/11/2026, chưa commit):** idle hướng xuống, thân Lv1, phím H đổi vòng 5 kiểu cho class đang chơi (chỉ máy mình, không lưu, không online), chụp `--hairstyle N`. Ảnh trong game: `docs/reference/hairstyles-5-per-class-idle-down-ingame.png` (hàng = class, cột = kiểu 0-4), từng class `docs/reference/hairstyles-<class>-idle-down-ingame.png`.
- Warrior (nam): 0 tóc đỏ nhọn gốc; 1 ngắn vuốt ngược xanh đen; 2 vàng, mái dựng cao (pompadour), hai bên ngắn; 3 nâu hạt dẻ xù dài ngang vai, rẽ giữa; 4 trắng bạc rối, búi củ tỏi trên đỉnh.
- Wizard (nam, **giữ mũ trùm**: mũ là một phần trang phục, tóc đổi bên dưới và lộ ra ngoài mũ): 0 trắng tím gốc; 1 bạc xanh thẳng dài tới vai; 2 cam xù bung khỏi mũ; 3 nâu, một bím dài vắt vai; 4 xanh navy ngắn, mái vuốt lệch sắc cạnh hở một bên trán, vài lọn nhọn chĩa ra mép mũ, hai bên ngắn trên tai (vẽ lại 07/11 vì bản bob mái bằng trông như con gái; bản cũ ở `heads/rejected-v1/style4-navy-bob-down.png`; nguồn `heads/wizard-hair4-v2-chatgpt-1.png` ứng viên phải, ứng viên trái tóc nhọn rối bị loại vì gần dáng kiểu 2 cam; IoU vùng tóc với kiểu 0-3: 0,60 / 0,62 / 0,82 / 0,76; chat https://chatgpt.com/c/6aeec025-b9c8-83ec-a3f7-abf760638ac0).
- Mechanic (nam, giữ kính bảo hộ nghiêng trên tóc như kiểu 0, tóc mái che trán, khối tóc cỡ kiểu 0): 0 vàng nhọn gốc; 1 hồng tro, tóc dài buộc đuôi thấp: đỉnh đầu tròn trơn ôm sát, mái lệch một bên, hai lọn mảnh rủ tới cằm, đuôi tóc ló bên phải sau gáy (vẽ lại 07/11 vì bản bạch kim rối bay trùng hình bóng kiểu 0, chỉ khác màu: IoU viền tóc 0,97; bản mới 0,67; bản cũ ở `heads/rejected-v3/`; nguồn `heads/mechanic-hair1-v4-chatgpt-1.png` ứng viên trái, ứng viên phải tóc undercut rẽ ngôi bị loại vì đầu nhỏ; chat https://chatgpt.com/c/6aeebdc9-e544-83ec-b933-f521bf8cf7a7); 2 đen nhọn, tóc mái dài lởm chởm; 3 cam đồng xoăn bồng; 4 xanh teal sẫm, phần trên dài hất lệch một bên, hai bên ngắn. Vẽ lại 07/11 (bản 1: đỏ đinh, nâu xù dài, bạc vuốt ngược, xanh mohawk bị chê không hợp: kính thẳng ngang thấp, lộ trán, lông mày nhíu, tóc nhỏ hơn kiểu 0; để ở `heads/rejected-v1/`). Chat vẽ lại https://chatgpt.com/c/6aeeb826-07b4-83ec-9bee-c53d90f0294a: tải 4 bản sao đầu kiểu 0, xin sửa chỉ tóc.
- Assassin (nữ, giữ băng đô đỏ và khăn che mặt): 0 đuôi ngựa tím gốc; 1 trắng bạc hai bím đuôi (twin tails); 2 đỏ thẫm bob ngắn; 3 xanh navy thẳng dài mái bằng (hime); 4 hồng hai búi tròn.
- Cleric (nữ, **giữ mũ đội trắng vàng** và hai trang sức bên, tóc đổi bên dưới): 0 vàng ngang vai gốc; 1 bạc tím thẳng dài; 2 hồng hai bím tết rủ trước vai; 3 nâu hạt dẻ dài gợn sóng; 4 xanh da trời hai đuôi.
Cách làm: mỗi class một chat ChatGPT, tải ảnh đầu cắt 4x trên nền #00FF00 (nữ kèm ảnh màn chọn nhân vật, prompt ghi FEMALE), xin MỘT ảnh 4 đầu cạnh nhau (Warrior 3 đầu vì đã có kiểu 1); tool tách từng đầu theo cột trống, khớp cỡ/vị trí vào mặt gốc, ghép lại mắt, má, miệng, cằm (Assassin cả băng đô) của kiểu 0. Tổng 5 lượt vẽ, không phải vẽ lại. Chat (Tay): Warrior https://chatgpt.com/c/6aeea125-d67c-83ec-8a74-51eeb532df28, Wizard https://chatgpt.com/c/6aeea287-8274-83ec-af02-dc9a051884d7, Mechanic https://chatgpt.com/c/6aeea2a6-9c80-83ec-a573-24cdf104d55c, Assassin https://chatgpt.com/c/6aeea2d5-3be0-83ec-905a-15688b878442, Cleric https://chatgpt.com/c/6aeea403-fecc-83ec-8416-bee6e5660ef2. Ảnh gốc ChatGPT: `ArtLibrary/classes/<class>/heads/<class>-hair-*up-chatgpt-1.png`.
File: `Tools/build-head-layer.py <class> split|write|style|sheet|check` (+ `head_layer_core.py`, `head_layer_masks.py`, `head_layer_classes.py` đường cắt cổ và bộ lọc màu từng class, `head_layer_styles.py` khớp + ghép mặt); `HeroHeadLayer.cs` đọc thêm `sy` (thân 4 class kia thở bằng co giãn dọc, không phải nâng như Warrior); art `Sprites/classes/<class>/body/headless/idle.png`, `heads/0..4/down.png`, `Data/classes/<class>/head-anchors.json`. Kiểu 0 frame 0 trùng từng pixel với thân cũ ở cả 5 class; frame 1-3 lệch dưới 1 px (đầu co bằng GPU). Class không có file (Fighter) tự bỏ qua.
Giới hạn: chỉ idle hướng xuống thân Lv1, hướng/động tác khác và bộ giáp T1+ vẫn thân liền (kiểu tóc biến mất khi đi, đánh); lông mày, trán do ChatGPT vẽ lại nên hơi khác kiểu 0 (mắt trở xuống giống hệt); tóc dài rủ trước vai đè lên cổ áo (Wizard 1, Cleric 1-3, Assassin 1, 3), phần tóc sau gáy của Cleric đã xoá để khỏi thành râu; bím Cleric 2 chạm nắm tay trái; kiểu Warrior 1 làm bằng tool cũ (không ghép lại). Chưa lưu vào save, chưa gửi online.

**Người làm game chốt (07/11/2026, 13:45): mũ không đổi ngoại hình.** Chỉ giáp (cả bộ trang phục) và 2 vũ khí đổi ngoại hình; mũ, găng, giày chỉ có chỉ số. Thay cho chốt "chỉ giáp và mũ" ở mục Zenonia S và mũ theo chủ đề ở mục bộ đồ theo bậc: các bộ T1–T9 không có mũ, tóc luôn lộ.

**Người làm game chốt (07/11/2026, 14:29): bỏ mũ ở mọi class**, kể cả mũ trùm Wizard và mũ đội Cleric vốn là trang phục gốc (xem ảnh so sánh `docs/reference/wizard-hood-up-vs-down-design-chatgpt.png`). Chỉ trang phục và vũ khí đổi ngoại hình.

**Các game khác hiện tên thế nào (07/11/2026, 15:55, ChatGPT tra web, cùng chat):** phần lớn không xác minh được luật mặc định. Xác minh được: Genshin co-op hiện tên đồng đội trên nhân vật khi ở gần (tối đa 4 người nên ít chồng tên); MapleStory M có tuỳ chọn ẩn thông tin người khác và của bản thân riêng (ở chế độ chụp ảnh); Ragnarok M có tuỳ chọn hiện tên/nhân vật khác và giảm số người hiển thị (Displayed Players: Low); Toram Online có tên trên đầu người khác, màu tên theo party/guild/bạn bè, có tuỳ chọn hiện người khác, cộng đồng vẫn kêu tên chồng khi đông; Black Desert Mobile có nhóm cài đặt hiển thị nhân vật. Trickster M, Lineage M/W, MapleStory PC, Zenonia: chưa xác minh. Khuyên cho game mình: bản thân không tên (giữ vòng chân + mũi tên); party có tên ngắn màu xanh lá nhạt + dấu party, ưu tiên cao, chồng nhãn thì giữ dấu ẩn tên (không đồng ý ẩn hẳn tên party); người lạ gần hiện tên trắng/xám, tối đa 8 tên trên điện thoại / 12 trên PC, trong bán kính khoảng 200 px quanh mình, ưu tiên gần nhất / đang chọn / vừa chat; người lạ xa không tên; mục tiêu đang chọn luôn hiện tên; đối thủ PvP tên đỏ. Cập nhật danh sách 3-5 lần/giây, trễ 0,5-1 s trước khi ẩn để không nhấp nháy. Cài đặt 3 chế độ: Smart (mặc định), All, Minimal (chỉ party, mục tiêu, người vừa chat). **Người làm game chốt (07/11/2026, 16:00): giữ luật cũ: ẩn tên mình và tên người trong party, hiện tên người lạ** (không hiện tên party như ChatGPT khuyên). `NameTags.cs` đã làm đúng, kèm luật đám đông: tối đa 8 tên, gần nhất trước, bỏ tên chồng nhau, giữ Alt hiện hết.

**Đánh giá đám đông 50 bot (07/11/2026, 15:50, chat "Đánh giá khả năng đọc game"):** ảnh `docs/reference/hairstyles-crowd-50-bots-{1,3,5}.png` (1280x720, 10 bot mỗi class, đủ 5 kiểu tóc, cùng Lv1 và đồ khởi đầu, đi lang thang không đánh; Wizard, Cleric đã bỏ mũ). Điểm: phân biệt class 9/10; phân biệt 5 kiểu tóc khi đứng riêng 8/10 (trước khi có kiểu tóc khoảng 2/10); khi 50 người tụ lại 5,5/10; đọc thông tin trên màn hình 5/10; tổng ngoại hình 8/10, đủ để làm tiếp animation. Bỏ mũ là đúng, class vẫn nhận ra nhờ trang phục + vũ khí. Tóc sáng (trắng, bạc, tím nhạt, vàng nhạt, hồng nhạt) chìm trên tuyết: Warrior bạc, Wizard trắng/bạc, Cleric vàng nhạt/bạc, Mechanic hồng nhạt; khuyên tăng độ tối viền và vùng bóng tóc trước, chưa đổi màu hàng loạt. Assassin tóc tím/đen lẫn giáp tối. Warrior + Assassin đứng sát nhau thì vũ khí lớn chồng hình. Lỗi lớn nhất: tên nhân vật chồng nhau, đè tóc/vũ khí, không rõ tên của ai; người đứng sau vùng HUD (cần điều khiển, nút) mất nhận diện. Chưa thấy đầu lệch; cảm giác đầu to nhỏ khác nhau do tóc dựng, kính, tóc sáng; muốn kết luận phải chụp từng class 4 hướng trên nền trơn. Ưu tiên: P0 sửa tên khi đông (chỉ hiện tên bản thân/party/mục tiêu, người khác khi gần hoặc chạm, hoặc chống chồng nhãn); P0 làm nổi nhân vật của mình; P1 tăng tương phản viền/bóng tóc sáng; P1 kiểm thứ tự vẽ nhân vật/đầu/vũ khí 4 hướng; P2 kiểm điểm neo đầu ở dash, hurt, attack, skill; P3 chỉnh vài kiểu tóc quá giống. Lưu ý: quy tắc người dùng đã chốt là chỉ người lạ mới có tên, nên đề xuất về tên phải hỏi người dùng.

**Bàn với ChatGPT: giữ mũ hay không (07/11/2026, 14:41, chat "Đánh giá khả năng đọc game")**, kèm ảnh so sánh `cleric-hat-vs-no-hat-design-chatgpt.png`, `wizard-hood-up-vs-down-design-chatgpt.png`. Phương án: A không mũ; B mũ cố định theo class (không đổi theo giáp); C mũ đổi theo bậc giáp; D như C + nút ẩn mũ. ChatGPT: ở cỡ 80-90 px, kiểu tóc + màu tóc nhận diện người chơi tốt hơn mũ; mũ lớn/mũ trùm nhận diện class nhưng xoá khác biệt 5 kiểu tóc (Wizard bỏ mũ thắng rõ). Chi phí: A 0 hình; B khoảng 20 hình cơ sở (4 hướng x 5 class); C khoảng 200 hình cơ sở (5 class x 10 bậc x 4 hướng), mỗi mũ phải hợp 5 kiểu tóc nên khoảng 1.000 tổ hợp phải kiểm (3.000 nếu thêm tư thế đầu), lỗi tóc xuyên mũ (búi, pompadour, hai đuôi), mũ trượt khi đầu nhún, thứ tự trước/sau tóc; D không giảm chi phí của C. MapleStory làm được vì có cả pipeline: tóc nhiều lớp (trước, sau, tóc bị ép khi đội mũ), mũ chia lớp trước/sau, quy tắc zmap/vslot. Khuyên: A cho trang bị (không có mũ theo bậc) + "B-lite" cho nhận diện class: Cleric giữ dấu hiệu thánh giá vàng, đá xanh, trang sức bên tóc (có thể thêm vòng cung trắng-vàng mảnh sau đỉnh đầu), bỏ phần mũ trắng lớn che đỉnh tóc (4-8 hình, không theo bậc); Wizard luôn hạ mũ trùm; Mechanic giữ kính, Assassin giữ băng đô. Mũ Cleric lớn có thể để làm ngoại hình màn chọn nhân vật hoặc trang phục cosmetic riêng. Chờ người làm game chốt.

**Kiểu tóc idle + walk 4 hướng (07/11/2026 chiều):** Warrior, Mechanic, Assassin, Cleric đủ 5 kiểu ở idle và walk 4 hướng; ChatGPT vẽ mỗi class 1 ảnh nhìn ngang + 1 ảnh nhìn lưng (5 đầu, đầu đầu là bản sao kiểu 0), tool khớp tỉ lệ theo bản sao, hướng ngang khớp theo mặt rồi ghép lại mặt kiểu 0, hướng lưng giữ hàng trong ảnh. Tổng 8 lượt vẽ, không phải vẽ lại. Wizard chưa làm (mũ trùm). Chi tiết: `docs/pending-tasks.md`.

Kế hoạch (người dùng, 07/11/2026): làm xong hệ đầu/kiểu tóc và trang phục cho Warrior rồi làm tiếp các class khác. Cleric và Assassin là nhân vật nữ.
