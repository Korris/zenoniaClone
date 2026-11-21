# Góc nhìn Willowbrook V2 so với RO3 (ChatGPT, 21/11/2026)

Chat (Tay): https://chatgpt.com/c/6b00f9a0-2b20-83ec-a0d4-361ae7da98ee . Ảnh gửi: willowbrook-25d-ro3-pass-nohud-ruins.jpg, ../ro3-pc-hud/warrior-hotbar-ingame-1280x720.jpg + 3 khung RO3 (168 s, 236 s, 404 s, chỉ local).

# So sánh camera Willowbrook V2 với Ragnarok Online 3 CBT

**Kết luận: Mình đồng ý với nhận xét rằng góc nhìn của Willowbrook hiện tại vẫn chưa giống RO3.** Sau khi xem kỹ 5 ảnh mới, mình cho rằng preset camera 43° từ mặt ngang mà mình đề xuất trước đó chưa phải lựa chọn tốt nhất nếu mục tiêu là bám sát ba khung hình RO3 này.

Vấn đề lớn nhất không phải FOV hay độ zoom riêng lẻ, mà là sự kết hợp giữa **góc nhìn còn quá ngang, mặt đất kéo dài lên phía xa, tỷ lệ sprite và cách phối cảnh tác động lên các vật thể**.

Theo hình ảnh hiện tại, mình sẽ thử tăng pitch thay vì giảm pitch như một số hướng điều chỉnh thường gặp khi muốn tạo cảm giác 3D.

## 1. Góc camera RO3 thực tế trông như thế nào?

Trước hết, cần phân biệt:

- **[Quan sát]** Những đặc điểm có thể trực tiếp nhìn thấy trong ba ảnh RO3.
- **[Ước lượng]** Thông số suy ra từ phối cảnh, không phải dữ liệu camera nội bộ của RO3.
- **[Đề xuất]** Cấu hình cần thử trong Unity.

### Bảng đối chiếu

| Thông số | RO3 CBT (ước lượng) | Willowbrook V2 |
|---|---|---|
| Pitch từ mặt ngang | **55–65°**, khả năng khoảng 58–60° | 43° |
| FOV dọc | Khoảng 25–35°, khó xác định | 25° |
| Kiểu phối cảnh | Perspective nhẹ, thiên hướng isometric | Perspective 25° |
| Nhân vật cao / màn hình | ~6–9% trong cảnh gameplay | ~9–12% |
| Vị trí nhân vật | Gần giữa, hơi thấp hoặc cao tùy cảnh | Gần giữa |
| Đường chân trời | Không thấy trong ba ảnh | Có đường ranh rõ ở phía xa ảnh 1 |
| Cảm giác mặt đất | Nhìn từ trên xuống khá nhiều | Nhìn xiên, cảm giác dải đất lùi xa |
| Cảnh xa | Địa hình và vật thể tiếp tục phủ khung | Mép cảnh dễ lộ |
| Tỷ lệ xa/gần | Tương đối ổn định, có thu nhỏ | Có perspective nhưng độ sâu chưa tự nhiên |

**Lưu ý:** Pitch 55–65° là giả thuyết để thử nghiệm, sai số có thể khá lớn. Không thể xác định chính xác pitch và FOV chỉ từ ảnh khi chưa biết kích thước vật thể, mặt phẳng địa hình và camera projection. Mình cũng không tìm được thông số camera chính thức của bản RO3 CBT trong các nguồn kiểm tra.

### RO3 nhìn từ trên xuống nhiều hơn Willowbrook

**[Quan sát — ảnh 3–5]**

Ở ảnh RO3 Priest dùng Kyrie Eleison:

- Nhìn thấy rất nhiều diện tích mặt trên của nền đất.
- Nhân vật vẫn thấy rõ mặt và thân trước.
- Có thể nhìn thấy đỉnh những bụi cây.
- Nền đất chiếm toàn bộ không gian chơi; không thấy rìa thế giới hay đường chân trời.
- Các vật thể không có sự phóng đại phối cảnh quá mạnh theo khoảng cách.

Ở ảnh RO3 Archbishop trong thành phố:

- Nhìn được phần lớn mái nhà.
- Đồng thời thấy được mặt trước và cửa sổ của nhà.
- Các mặt đường và mái cùng tuân theo phối cảnh 3D.
- Góc nhìn khá cao nhưng không thẳng đứng.

Đây là kiểu camera **high-angle isometric-like**, chứ không phải camera third-person nhìn xiên ngang.

### Willowbrook đang khác ở đâu?

**[Quan sát — ảnh 1]**

Có một dải ngang gần y = 75–90 px, phía sau hàng cây, nơi mặt cỏ kết thúc và chuyển thành nền xám nhạt.

Đây có thể là mép ground hoặc giới hạn vùng render, chưa chắc là đường chân trời vật lý của camera. Nhưng về cảm nhận thị giác, nó giống đường chân trời.

RO3 không để lộ loại ranh giới này trong ba khung tham chiếu.

Khi người chơi thấy hàng cây dựng trước một đường ngang phía xa, khung cảnh dễ trông như **một sân khấu 2.5D có phông nền**, hơn là thế giới 3D kéo dài liên tục.

Đây là khác biệt rất đáng sửa.

---

## 2. Sáu nguyên nhân chính khiến góc nhìn chưa giống

### 1. Pitch 43° vẫn hơi thấp

**Mức ảnh hưởng: RẤT LỚN**

Ở ảnh Willowbrook không HUD:

- Nhìn thấy nhiều phần thân trước của cây.
- Tán cây phía xa tạo thành một bức tường cây ngang màn hình.
- Mặt cỏ thu hẹp về phía rìa trên.
- Có cảm giác camera đang nhìn xuyên vào rừng, không hẳn đang nhìn xuống bản đồ.

Ở RO3:

- Camera có vẻ nhìn xuống từ trên cao nhiều hơn.
- Nhìn rõ nền đất phía sau nhân vật.
- Cảm giác không gian thiên về bản đồ MMORPG 3D hơn.

**[Đề xuất]** Tăng pitch từ 43° lên 55–60° để kiểm tra.

Nhưng không nên kết luận 60° là đúng nếu chưa so ảnh A/B cùng vị trí.

### 2. Mép xa của ground bị lộ

**Mức ảnh hưởng: RẤT LỚN**

Điều này rõ nhất ở ảnh 1, khi phía sau hàng cây là một đường phân cách ngang.

Không cần thay camera mới xử lý được.

Hãy mở rộng ground hoặc đặt background terrain tiếp nối, ít nhất đủ phủ toàn bộ camera frustum ở các mức zoom cho phép.

Ngay cả với camera 60°, nếu camera di chuyển sát rìa map, mép địa hình vẫn có thể lộ.

### 3. Cây nhìn giống billboard phẳng hơn cây 3D

**Mức ảnh hưởng: RẤT LỚN**

Cây hiện tại được vẽ rất đẹp. Nhưng ở một số cây, nhất là cây thông:

- Hình tán gần như giữ nguyên kiểu chiếu dù cây ở vị trí xa hay gần.
- Mặt trên tán không thay đổi đúng theo góc nhìn.
- Cây giữ một silhouette kiểu tranh minh họa.
- Các thân cây có hình dạng mặt trước rất rõ, trong khi phần thể tích 3D chưa thay đổi theo hướng camera.

RO3 có cây được dựng theo hình khối nên camera nhìn từ trên xuống sẽ thấy nhiều bề mặt hướng lên của tán.

Nếu tăng pitch mà không sửa billboard, sự khác biệt này thậm chí có thể lộ rõ hơn.

### 4. Nhân vật của bạn vẫn đọc như sprite mặt trước

**Mức ảnh hưởng: LỚN**

Trong ảnh 1, Cleric tương đối lớn và gần như nhìn chính diện.

Trong ảnh RO3, nhân vật nhỏ hơn, cảm giác được nhìn từ một camera trên cao; đầu và thân có sự phân bố hình khối tương ứng với góc nhìn.

Với sprite 2D chỉ xoay quanh trục Y, thay camera pitch không làm xuất hiện thêm mặt trên của đầu, vai và tóc.

Đây là giới hạn hình học thật sự.

### 5. Vật thể gần/xa chưa thể hiện cùng một hệ phối cảnh

**Mức ảnh hưởng: LỚN**

Willowbrook có cây gần rất to, cây xa nhỏ hơn. Nhưng do sprite được vẽ với phối cảnh riêng, cộng thêm phối cảnh camera Unity, có thể xuất hiện sự không nhất quán.

Ví dụ:

- Thân cây thể hiện phối cảnh vẽ tay.
- Mặt đất sử dụng phép chiếu 3D thật.
- Sprite quái thể hiện góc nhìn 3/4 cố định.
- Camera lại nhìn từ 43°.

Bốn yếu tố này chưa chắc có cùng điểm nhìn giả định.

Đây là lý do một game có perspective camera vẫn có thể không trông như môi trường 3D.

### 6. Camera chưa tạo cùng tỷ lệ không gian và nhân vật

**Mức ảnh hưởng: VỪA–LỚN**

Trong ảnh Willowbrook, Cleric có vẻ lớn hơn nhân vật trong cảnh Priest RO3.

Nhưng cần tránh một sai lầm: **thu nhỏ nhân vật không tự động làm góc nhìn giống hơn**.

Nếu zoom ra mà không điều chỉnh pitch và art projection, cảnh có thể chỉ trông xa hơn nhưng vẫn giữ cảm giác billboard.

Thứ tự đúng nên là:

1. Chốt góc nhìn mặt đất.
2. Chỉnh projection của sprite.
3. Chỉnh độ zoom.
4. Cuối cùng chỉnh framing nhân vật.

---

## 3. FOV của RO3: rộng hay hẹp?

**[Quan sát]** RO3 không thể hiện hiệu ứng wide-angle quá mạnh. Trong ảnh thành phố, mái nhà và mặt đường gần mép màn hình không bị biến dạng nhiều. Trong ảnh Priest, vật thể xa không thu nhỏ cực nhanh.

Điều đó gợi ý một camera có phối cảnh tương đối nhẹ. Nhưng không đủ để suy ra chính xác FOV, vì FOV và khoảng cách camera có thể phối hợp tạo ra hình ảnh tương tự.

Mình ước lượng vùng đáng thử là **vertical FOV 25–35°**.

- FOV 20–22°: hình tương đối phẳng, dễ giữ tỷ lệ sprite nhưng chiều sâu phối cảnh yếu.
- FOV 25–28°: cân bằng, phù hợp thử đầu tiên.
- FOV 30–35°: tăng chênh lệch tỷ lệ xa/gần, có thể làm lộ billboard.
- FOV trên 40°: chưa có lý do để sử dụng nếu mục tiêu là ba ảnh RO3 này.

**Mình không khuyên tăng FOV mạnh.** Điểm cần chỉnh trước là pitch, hình học cảnh và tỷ lệ nhân vật.

### Độ cao camera so với nhân vật

Không thể suy ra số mét chính xác từ ba ảnh RO3 vì chưa biết chiều cao nhân vật và kích thước thế giới Unity tương ứng.

Tuy nhiên, có thể dùng tỷ lệ hiển thị để dựng preset.

**[Ước lượng]**

- RO3 trong ảnh Priest gameplay: nhân vật khoảng 6–9% chiều cao khung, tức khoảng 43–65 px khi chuẩn hóa về 720p.
- Willowbrook: Cleric khoảng 9–12%, tức khoảng 65–86 px, tùy đo cả tóc, vũ khí và chân hay không.
- Vị trí nhân vật trong RO3 thay đổi theo cảnh và camera, nhưng trong cảnh gameplay Priest, nhân vật gần vùng giữa, không có bằng chứng cho thấy luôn cố định ở 60–65% chiều cao màn hình.

**[Đề xuất]** Thử hero cao 65–72 px tại 1280×720, tính từ đỉnh tóc đến chân và không tính tên, thanh HP.

Đây là mức trung gian an toàn trước khi quyết định có cần zoom xa hơn không.

---

## 4. Preset camera mới cho Willowbrook

Mình đề xuất ba cấu hình để bạn chụp A/B, trong đó **Preset B là lựa chọn nên thử đầu tiên**.


  Chọn preset so sánh
  
    {#each presets as item}
      {item.name}
    {/each}
  
  
  
    
      Pitch từ mặt ngang
      {p.pitch}°
    
    
      Vertical FOV
      {p.fov}°
    
    
      Chiều cao hero / 720p
      {p.hero} px
    
    
      Vị trí chân hero từ đỉnh
      {p.pos}
    
  
  {p.desc}
  GenUI.copy(`Willowbrook camera preset ${p.name}\nPitch from horizontal: ${p.pitch} degrees\nVertical FOV: ${p.fov} degrees\nHero height at 1280x720: ${p.hero}px\nHero feet screen Y: ${p.pos} from top\nPerspective projection; calibrate camera distance to target pixel height; no visible map boundary.`)}> Sao chép cấu hình


### Khoảng cách camera mới nên là bao nhiêu?

Không nên chọn một camera distance cố định nếu chưa biết một sprite hero cao bao nhiêu Unity unit.

Ví dụ, giả sử:

- Hero cao 1 Unity unit.
- Sprite dựng đứng, mặt phẳng hướng về camera.
- FOV dọc 27°.
- Mục tiêu hero cao 69 px tại 720p.

Công thức gần đúng:

\[
d \approx \frac{720 \times H}{2h\tan(FOV/2)}
\]

Trong đó \(H\) là chiều cao thực của sprite tính bằng world units, \(h\) là chiều cao hiển thị bằng pixel.

Kết quả khoảng **21.7 m** từ camera tới hero trong mô hình đơn giản. Với FOV 27° và h = 69 px, khi \(H=1\) m, giá trị chính xác gần 21.7 m. Khoảng cách thực tế cần hiệu chỉnh theo việc sprite có hướng thẳng tới camera hay không, cách định nghĩa chiều cao và điểm look-at.

Nếu sprite hero cao 2 Unity units thì khoảng cách tương ứng gần gấp đôi. Vì vậy không nên áp dụng trực tiếp khoảng cách trên nếu scale game khác.

### Look-at và framing

Mình đề xuất:

- Vị trí ngang nhân vật: 50% khung hình.
- Vị trí chân: 53–55% từ đỉnh màn hình, khoảng y = 382–396 px ở 720p.
- Camera nhìn vào vị trí gần thân nhân vật nhưng offset theo hướng phía trước để điều khiển framing.
- Look-at height khởi điểm: chân + 0.3–0.5 chiều cao hero, sau đó hiệu chỉnh theo screen-space.
- Smooth follow: 0.12–0.2 giây.
- Không đổi pitch động theo quãng đường chạy trong gameplay thông thường.

**Quan trọng:** Đừng cố đạt đúng vị trí nhân vật chỉ bằng tăng look-at height. Phải kiểm tra lại phép chiếu màn hình và hướng camera, vì thay đổi look-at cũng có thể làm góc nhìn thực tế thay đổi.

---

## 5. Làm sao để sprite trông bớt 2D khi tăng pitch?

### A. Phân biệt hai kiểu billboard

**Cylindrical billboard:** Chỉ xoay quanh trục Y thế giới, nhân vật luôn đứng thẳng.

Ưu điểm: nhân vật không bị nghiêng theo camera, phù hợp chibi 2D.

Nhược điểm: khi camera nhìn xuống 58–60°, vẫn thấy sprite với hình dáng mặt trước vốn được vẽ cố định.

**Spherical billboard:** Mặt phẳng xoay để đối diện camera cả pitch lẫn yaw.

Ưu điểm: chiều cao sprite ít bị co ngắn do mặt phẳng nhìn xiên.

Nhược điểm: chân sprite có thể không còn tạo cảm giác cắm xuống mặt đất, nhất là với cây cao. Nhìn từ bên cạnh dễ thấy billboard nghiêng ngả.

### Mình khuyên cấu hình hybrid

- Hero/quái: giữ cylindrical billboard, nhưng dùng sprite được vẽ cho góc nhìn 3/4 từ trên xuống.
- Cây tán tròn: dùng 2–3 mặt phẳng/lớp tán có depth khác nhau.
- Cây thông: dùng 2 mặt phẳng giao nhau hoặc một mesh low-poly cho tán.
- Tảng đá nhỏ: giữ sprite nếu góc nhìn không xoay.
- Công trình: ưu tiên mesh 3D.

### B. Dùng sprite riêng cho camera pitch cao

Hiện tại hero trông khá thẳng đứng.

Nếu camera mục tiêu là 58°, một cách làm tốt là có bộ sprite được vẽ để thể hiện thêm mặt trên của:

- Đầu/tóc.
- Vai.
- Vũ khí.
- Lưng và áo choàng.
- Bàn chân theo góc 3/4.

Không cần tạo 8 góc camera khác nhau ngay.

Chỉ cần một bộ sprite chuẩn dành riêng cho camera pitch 55–60°, với 4 hoặc 8 hướng di chuyển, đã có thể cải thiện sự nhất quán.

**Không nên dùng `transform.rotation` để nghiêng sprite 15° rồi coi như đã có góc nhìn 3D.** Phép biến đổi này không tạo ra hình học mặt trên, chỉ nghiêng hoặc co ảnh.

### C. Chỉnh mặt đất theo camera mới

Một lỗi phổ biến: sprite vẽ phối cảnh 3/4 nhưng ground texture lại được render bằng camera góc cao hơn nhiều.

Khi pitch tăng từ 43° lên 58°:

- Mặt đất nằm ngang sẽ ít bị co theo chiều sâu hơn về mặt hình chiếu trực giao cục bộ.
- Đường đất có thể trông rộng hơn theo chiều dọc màn hình.
- Texture phải được kiểm tra lại kích thước chi tiết.
- Những vật thể đã có bóng vẽ sẵn có thể không còn khớp với bóng trong thế giới 3D.

Đừng kéo giãn texture bằng tay để sửa toàn bộ. Hãy kiểm tra chiều rộng đường theo world units và tỷ lệ pixel thực tế trước.

### D. Cây xa phải có tỷ lệ phù hợp

Trong perspective camera, các vật thể cùng kích thước thế giới phải thu nhỏ theo khoảng cách.

Kiểm tra xem billboard có đang sử dụng cơ chế constant screen size hoặc script tự scale theo camera không. Nếu có, hãy tắt với cây và quái.

Cây xa chỉ nên nhỏ do perspective và khác biệt scale thiết kế, không phải do một hệ scale tùy tiện áp vào sprite.

---

## 6. Nếu thật sự muốn giống RO3, những gì nên làm 3D?

Không bắt buộc chuyển toàn bộ game sang 3D.

Mình đề xuất một hệ thống 2.5D hybrid theo thứ tự sau:

| Hạng mục | Có cần 3D thật? | Công sức ước lượng |
|---|---|---|
| Ground terrain | **Nên** dùng mesh/terrain | 2–5 ngày |
| Tán cây lớn | Rất nên có thể tích 3D | 5–12 ngày cho bộ mẫu |
| Cây thông | Nên dùng mesh low-poly | 2–4 ngày |
| Nhà/cầu/tường | Nên dùng mesh 3D | 4–10 ngày cho bộ mẫu |
| Đá lớn | Khuyến khích | 2–5 ngày |
| Hero | Chưa bắt buộc | Rất cao nếu đổi toàn bộ |
| Quái | Chưa bắt buộc | Cao nếu đổi toàn bộ |
| Pet | Không bắt buộc | Giữ 2D |

Các số ngày trên là ước lượng cho một người quen Unity và pipeline asset, đã có artwork đầu vào; không bao gồm việc làm lại mọi map, animation, QA thiết bị hoặc tối ưu game online.

### Cây là điểm nâng cấp mang lại khác biệt rõ nhất

Trong ảnh Willowbrook, cây chiếm diện tích rất lớn. Khi camera nhìn cao hơn, người chơi càng nhìn nhiều vào mặt trên tán.

Với billboard phẳng, bạn khó tái tạo chính xác:

- Mặt trên của tán.
- Chiều sâu giữa các cụm lá.
- Sự che khuất giữa các nhánh.
- Bóng tự đổ của một phần tán lên phần khác.
- Thay đổi hình dáng cây khi góc camera đổi.

Một cây 3D low-poly có 5–8 cụm tán và shader toon mềm có thể đáp ứng tốt hơn, mà vẫn giữ phong cách RO Soft Fantasy.

Tuy vậy, mesh 3D chỉ giúp tiến gần hơn về hình học. Nếu thiết kế khối và shader không khớp sprite hiện có, cây 3D mới có thể trông lạc phong cách. Vì vậy nên thử 1 cây tán tròn và 1 cây thông trước, không chuyển cả rừng ngay.

---

## 7. Thứ tự thử nghiệm để xác định chính xác lỗi camera

Mình khuyên bạn đừng tiếp tục chỉnh ánh sáng, màu hay post-processing vào lúc này. Những phần đó đã tốt hơn phiên bản đầu. Hãy khóa chúng lại và thử riêng camera.

1. **Chụp cùng một vị trí ở pitch 43°, 52°, 58°, 64°**; giữ FOV 25°, camera target và chiều cao hero trên màn gần tương đương bằng cách chỉnh distance. So sánh độ dốc mặt đất, phần thân/mặt cây nhìn thấy và tính tự nhiên của nhân vật.
2. Chọn pitch tốt nhất, sau đó thử FOV 23°, 27°, 31°. Điều chỉnh distance để hero luôn cao bằng nhau.
3. Chọn FOV, sau đó thử hero 65, 70, 75 và 80 px.
4. Loại bỏ hoàn toàn đường mép map nhìn thấy phía xa, vì nó làm sai cảm giác chiều sâu bất kể camera.
5. Chụp thêm hai vùng có nhà và cầu. Camera nào đẹp ở rừng nhưng làm nhà/cầu sai phối cảnh thì chưa phải camera phù hợp toàn game.
6. Thử thay duy nhất một cây billboard bằng cây 3D cùng kích thước, cùng vị trí, cùng màu. Nếu khác biệt rất lớn, vấn đề còn lại chủ yếu là hình học asset chứ không phải thông số camera.

Đánh giá camera phải dựa vào ảnh cùng một tọa độ, cùng độ phân giải và cùng chiều cao hero. Nếu không, zoom và bố cục sẽ khiến việc so sánh pitch bị nhiễu.

---

## Kết luận cuối cùng

Từ hai ảnh Willowbrook mới, mình cho rằng **bản V2 đã cải thiện rõ ánh sáng, bóng và bề mặt địa hình**, nhưng chưa đạt góc nhìn RO3 chủ yếu vì:

- Camera 43° có khả năng còn quá thấp.
- Mép xa ground tạo cảm giác cảnh có phông nền.
- Sprite cây và nhân vật chưa có hình chiếu nhất quán với góc camera.
- Nhân vật vẫn hơi lớn so với ba khung RO3.
- Các đối tượng lớn như cây, nhà và tường chưa có đủ thể tích 3D khi nhìn từ trên xuống.

**Cấu hình mình chọn thử đầu tiên: Pitch 58° từ mặt ngang, vertical FOV 27°, hero cao 69 px tại 720p, chân nhân vật ở y ≈ 389 px và không để lộ ranh giới ground.**

Đừng vội coi đây là cấu hình cuối. Nếu 58° làm cây và hero trông như các tấm ảnh dựng đứng, thì đó là dấu hiệu bạn đã tới giới hạn của sprite hiện tại. Khi ấy giải pháp không phải liên tục đổi pitch, mà là tăng chiều sâu hình học của cây và công trình, hoặc vẽ lại góc chiếu sprite cho phù hợp camera.

Mình dự đoán có thể cải thiện đáng kể cảm giác RO3 bằng preset mới, nhưng không thể kết luận chính xác tỷ lệ giống chỉ từ ba khung tham chiếu. Sự khác biệt giữa billboard 2D và khối 3D vẫn sẽ là giới hạn lớn nhất sau khi camera đã được cân chỉnh.

---

