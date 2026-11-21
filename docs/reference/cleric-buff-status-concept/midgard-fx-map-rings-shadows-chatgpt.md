# Midgard Senki: FX đậm, màu map, 2 vòng buff quanh thân, bóng đổ (ChatGPT, 21/11/2026)

Chat (Tay): https://chatgpt.com/c/6b005a53-8a78-83ec-8011-ed9ffd5160b2 . Ảnh người dùng gửi: `midgard-senki-fx-map-reference.png`; ảnh map mình: `../cleric-fx-ro3/willowbrook-meadows-ingame-cleric.jpg`.

# So sánh Ragnarok Midgard Senki với game của bạn – Chốt hướng chỉnh FX và map

**Mình đồng ý với cả 3 nhận xét, nhưng cần phân biệt một điểm quan trọng:** FX Midgard Senki trong ảnh 1 là **hiệu ứng chiến đấu đang bùng nổ**, còn ba dải ảnh trước của bạn là buff và Heal. Không nên làm buff sáng và lớn bằng skill tấn công, nhưng hoàn toàn có thể học Midgard ở cách dùng **màu đặc, bão hòa cao, viền rõ và hạn chế ánh sáng trắng làm bạc màu**.

Nhìn trực tiếp hai ảnh:

- **Midgard:** FX xanh cyan đậm, có vùng xanh lam sâu và các nét đỏ san hô rõ ràng. Các mảng màu lớn, ít bị trong suốt, nhìn nổi ngay trên nền vàng đất.
- **Game bạn:** FX buff trước đó có thiết kế đẹp nhưng phần lớn là vàng kem, xanh băng nhạt và ánh sáng trắng. Trên nền tuyết, chúng bị hòa vào nền.
- **Map Midgard:** Cỏ xanh trung bình đến hơi đậm, đất vàng nâu khá phẳng, ít hoa và texture nhỏ.
- **Map Willowbrook:** Cỏ vàng chanh sáng, nhiều hoa và chi tiết li ti, đường đất sáng gần màu kem. Cảm giác tươi và hoàn thiện hơn về minh họa, nhưng nền cạnh tranh thị giác với nhân vật và FX.

**Hướng mình đề xuất:** Giữ phong cách RO Soft Fantasy của game bạn, nhưng chuyển FX sang kiểu *Midgard Color-Rich*: màu rõ hơn, ít trắng hơn, và làm map dịu xuống một mức vừa phải.

Các giá trị hex dưới đây là màu mục tiêu ước lượng từ ảnh chụp, không phải mã màu shader gốc của Midgard. Phần trăm chỉnh sửa là điểm khởi đầu để thử trong Unity.

## 1. FX Midgard – Vì sao màu nhìn đậm và nổi bật?

### 1.1. Phân tích màu

| Thành phần | Màu hex ước lượng | Vai trò |
|---|---|---|
| Lõi sáng xanh | `#72F4FF` | Điểm năng lượng sáng nhất |
| Thân cyan | `#12C9E8` | Mảng màu chính của luồng năng lượng |
| Xanh lam sâu | `#1479C7` | Tạo độ sâu và hình khối |
| Viền xanh tối | `#1768A8` | Giúp FX tách khỏi nền |
| Vòng đỏ san hô | `#E95060` | Vệt xoáy tương phản với xanh |
| Viền đỏ đậm | `#B93650` | Giữ đường cong rõ nét |

Trong ảnh Midgard, luồng năng lượng xanh có phần giữa cyan sáng, phần thân xanh lam đậm và các điểm trắng rất sáng. Vòng đỏ bên ngoài có mép đậm hơn phần phát sáng.

Đây là điều các buff của bạn còn thiếu: **sự khác biệt giữa thân hiệu ứng và phần glow**.

### 1.2. So sánh thông số màu

| Thuộc tính | FX Midgard (ước lượng) | Buff hiện tại của bạn | Nên chỉnh |
|---|---|---|---|
| Saturation vùng màu chính | 75–95% | Khoảng 35–60% | Tăng 25–40% tương đối |
| Value vùng màu chính (HSV) | 65–90% | Khoảng 85–100% | Giảm 10–18 điểm % |
| Alpha vùng thân | Cảm giác 0,65–0,90 | Cảm giác 0,20–0,45 | Tăng lên 0,55–0,75 lúc kích hoạt |
| Tỷ lệ màu đặc / glow | Khoảng 65/35 | Khoảng 35/65 | Đổi thành 65/35 |
| Độ dày đường chính | Khá dày, rõ | Mảnh, mềm | Tăng 30–50% |
| Glow | Tập trung quanh lõi | Lan rộng, bạc màu | Giảm bán kính 20–30% |

*Alpha và tỷ lệ màu đặc/glow là ước lượng thị giác từ ảnh tổng hợp; không thể suy ra chính xác blending mode hay alpha shader chỉ bằng screenshot.*

### 1.3. Có nên dùng nhiều Additive trắng?

**Không.** Trong ảnh Midgard, phần trắng sáng chủ yếu nằm ở đỉnh năng lượng, điểm va chạm và vài hạt nhỏ. Thân luồng vẫn giữ xanh cyan và xanh lam.

Với Unity:

- **Thân Mesh Trail:** Alpha Blend hoặc Premultiplied Alpha, không dùng Additive thuần cho toàn bộ thân.
- **Lõi năng lượng:** Additive, màu cyan sáng, chỉ chiếm khoảng 10–20% bề rộng.
- **Viền ngoài:** Alpha Blend, màu đậm hơn thân 15–25% về value.
- **Bloom:** Chỉ phát mạnh ở lõi và một số hạt; tránh để toàn bộ vệt phát trắng.
- **Distortion:** Giữ nhẹ, vì hiệu ứng buff cần rõ hình dạng hơn là làm méo cảnh.

### 1.4. Màu mới đề xuất cho buff của bạn

| Thành phần | Damage – vàng hổ phách | Defense – cyan ngọc |
|---|---|---|
| Viền đậm | `#A96519` | `#126E91` |
| Thân chính | `#E9A32D` | `#16B8D2` |
| Highlight | `#FFD56A` | `#75EAF0` |
| Lõi sáng | `#FFF1BA` | `#D6FCFF` |

- **Spirit Blessing:** Bỏ bớt vàng kem gần trắng, tăng tỷ trọng vàng hổ phách. Vòng và vệt xoáy nên có thân màu `#E9A32D`, không chỉ là ánh sáng vàng.
- **Divine Aegis:** Giảm xanh băng trắng, tăng cyan đậm. Đường viền vòm dùng `#16B8D2`, thêm lớp mép tối `#126E91`.
- **Huy hiệu kiếm/khiên:** Dùng màu thân đậm và outline rõ. Tăng kích thước 20–25%, giảm Bloom riêng huy hiệu 30–40%.
- **Lúc kích hoạt:** Thân alpha 0,60–0,75; viền alpha 0,75–0,90; highlight alpha 0,40–0,60. Với additive, cần kiểm tra bằng kết quả hiển thị chứ không chỉ nhìn giá trị alpha.

Mình khuyên chỉnh màu và blending trước, sau đó mới tăng số lượng particle. Thêm particle vào một hiệu ứng vốn đang quá trắng sẽ chỉ khiến nó càng bạc màu.

## 2. Map Midgard so với Willowbrook Meadows

Mình kiểm tra thêm màu pixel ở một số vùng cỏ và đất trong hai ảnh. Các vùng chọn không hoàn toàn tương đương vì Midgard có nền phẳng, còn Willowbrook có nhiều bóng cây, hoa và chi tiết vẽ tay.

### 2.1. Kết quả màu tham khảo

| Thuộc tính | Midgard | Willowbrook |
|---|---:|---:|
| Hue cỏ (HSV) | ~104° | ~72° |
| Saturation cỏ | ~55% | ~54% |
| Value cỏ | ~60% | ~66% |
| Hue đất | ~40° | ~32° |
| Saturation đất | ~59% | ~44% |
| Value đất | ~91% | ~95% |

Đây là mẫu pixel ở các vùng cụ thể, không phải thống kê toàn bộ map.

**Điểm đáng chú ý:** Cỏ Willowbrook không nhất thiết bão hòa hơn Midgard. Vấn đề lớn hơn là **hue nghiêng vàng chanh, nhiều vùng sáng và texture có tương phản nhỏ dày đặc**.

### 2.2. Nên chỉnh map thế nào?

Mình không khuyên bê nguyên màu Midgard sang Willowbrook vì Midgard có phong cách nền khá phẳng, trong khi game bạn đang dùng nền vẽ tay nhiều lớp.

Chốt một preset thử nghiệm:

| Shader parameter | Đề xuất |
|---|---|
| Brightness toàn map | −7% |
| Saturation toàn map | −6% |
| Contrast toàn map | −5% |
| Hue cỏ | Dịch +10° đến +15° về xanh lục |
| Saturation cỏ | −5% tương đối |
| Brightness cỏ | −8% |
| Brightness đất | −7% |
| Saturation đất | +5% |
| Contrast chi tiết nhỏ | −15% |
| Brightness hoa trắng/vàng | −10% |

Các giá trị brightness/contrast giả định shader có thang điều chỉnh thông thường; nếu shader của bạn dùng công thức khác, cần quy đổi tương ứng.

### 2.3. Màu map mục tiêu


  {#each [{c:"#77964C",n:"Cỏ chính"},{c:"#91A962",n:"Cỏ sáng"},{c:"#C9A16F",n:"Đường đất"},{c:"#9B855D",n:"Đất tối"},{c:"#E6DBAE",n:"Hoa sáng"}] as p}
    
      
      {p.n}
      {p.c}
    
  {/each}


### 2.4. Có nên làm phẳng và bớt chi tiết?

**Có, nhưng chỉ giảm khoảng 15–25% mật độ chi tiết nhỏ ở vùng chiến đấu.**

- Giữ nguyên cây lớn, đường đi, bờ đá, nhà cửa và những mảng hoa đẹp.
- Giảm 20–30% số chấm sáng li ti trên cỏ, đặc biệt những vùng nhân vật thường đứng đánh quái.
- Giảm 15–20% độ tương phản của các vết cỏ, sỏi và hoa nhỏ.
- Giữ các cụm hoa lớn làm điểm nhấn, không rải đều toàn map.
- Giảm chi tiết trực tiếp dưới chân quái và nhân vật để vòng phép dễ đọc hơn.

Mình khuyên **không giảm saturation toàn map quá 10%**, vì Willowbrook sẽ mất chất Soft Fantasy và trông xám. Midgard nhìn dịu một phần vì dùng mảng màu lớn, không chỉ vì màu tối.

---

## 3. Thiết kế hai vòng buff duy trì quanh thân nhân vật

Đây là thay đổi mình ủng hộ nhất.

**Bỏ cách dùng vòng dưới chân làm tín hiệu chính khi buff đang duy trì.** Thay vào đó, cho hai vòng năng lượng nhỏ quay quanh thân Cleric. Vòng vàng biểu thị Damage, vòng cyan biểu thị Defense.

Không cần giữ huy hiệu kiếm/khiên bay trên đầu sau khi kích hoạt; chỉ giữ icon buff trong HUD.

### 3.1. Vòng Damage – Spirit Blessing


  
    
    **Vòng vàng hổ phách – ngang eo**
  
  - Độ cao: **40–45% chiều cao nhân vật**, tính từ bàn chân.
  - Đường kính ngang: **1,20 lần bề rộng sprite**.
  - Tỷ lệ chiều sâu ellipse: **0,38–0,45 lần đường kính ngang** khi chiếu lên màn hình.
  - Độ dày nét chính: **3–4 px** ở mức nhân vật cao khoảng 80–100 px trên màn hình.
  - Alpha thân: **0,65–0,75**; viền tối: **0,75–0,85**.
  - Tốc độ quay: **1 vòng/6 giây**, theo chiều kim đồng hồ khi nhìn từ trên xuống.
  - Hai đoạn sáng ngắn chiếm tổng cộng 25–35% chu vi, tạo cảm giác năng lượng chuyển động.
  - Particle: 1–2 hạt vàng nhỏ/giây, bay lên tối đa ngang vai rồi tan.


### 3.2. Vòng Defense – Divine Aegis


  
    
    **Vòng cyan ngọc – ngang ngực thấp**
  
  - Độ cao: **55–60% chiều cao nhân vật**, cao hơn vòng vàng khoảng 15% chiều cao.
  - Đường kính ngang: **1,30 lần bề rộng sprite**.
  - Tỷ lệ chiều sâu ellipse: **0,38–0,45**.
  - Độ dày nét chính: **3–4 px**.
  - Alpha thân: **0,60–0,70**; viền tối: **0,75–0,85**.
  - Tốc độ quay: **1 vòng/8 giây**, ngược chiều kim đồng hồ.
  - Có thể thêm 2 đoạn ký hiệu hình khiên nhỏ chạy theo vòng, nhưng không dùng huy hiệu lớn.
  - Particle: 1–2 hạt cyan/giây, không dùng vòm sáng lớn khi duy trì.


### 3.3. Khi có cả hai buff


  
    
      
        
        
        
      
      
        
        
        
      
    
    
    
    
      
      
      
      
      
      
      
      
    
    
    
    
    
    
    
      Defense
      
      Damage
      
    
    Sơ đồ vị trí vòng quanh thân, không theo tỷ lệ pixel
  


- **Vàng ở eo, cyan ở ngực thấp:** Hai vòng khác độ cao, không chồng lên nhau.
- Hai vòng quay ngược chiều để người chơi nhận biết đang có hai buff.
- Không cần vòm khiên thường trực; vòm chỉ hiện lại ngắn 0,15–0,25 giây khi nhân vật bị đánh nếu muốn có phản hồi phòng thủ.
- Tổng particle khi có hai buff: tối đa **3–4 hạt/giây**.
- Giữ phần mặt, tóc và vùng mắt hoàn toàn không bị vòng che.

### 3.4. Cách render trước/sau nhân vật

Vì nhân vật của bạn là sprite 2D nhưng FX dùng mesh, cần chia vòng theo độ sâu để tạo cảm giác bao quanh cơ thể.

- **Nửa sau:** Render phía sau sprite nhân vật.
- **Nửa trước:** Render phía trước sprite, nhưng thấp hơn vùng mặt.
- Dùng hai MeshRenderer hoặc chia mesh thành hai đoạn, đồng bộ rotation và shader.
- Tính phần trước/sau theo góc camera và vị trí trên ellipse, không cố định theo world Z nếu camera có thể xoay.
- Nếu dùng Unity Built-in RP, kiểm tra sorting layer, render queue và depth test để mesh không xuyên qua sprite sai thứ tự.
- Tránh để vòng bị đứt hoặc nhảy vị trí khi nhân vật đổi hướng đi.

### 3.5. Cách giữ vòng gọn khi đông người

Mặc dù muốn buff rõ hơn, không nên để hai vòng trở thành hai đĩa sáng lớn.

- Giới hạn đường kính lớn nhất khoảng 1,3 lần bề rộng sprite.
- Giữ chiều dày 3–4 px ở camera gameplay chuẩn, không tự động phóng to quá mức khi zoom.
- Giảm particle của nhân vật khác xuống 0–1 hạt/giây.
- Với nhân vật ở xa, chỉ giữ hai nét vòng, tắt hạt và Bloom phụ.
- Không giữ vòng phép lớn dưới chân khi đang duy trì buff.
- Khi buff hết hạn, vòng tương ứng Dissolve trong **0,25–0,35 giây**.

## 4. Thứ tự triển khai trong Unity

| Ưu tiên | Việc cần làm | Mức quan trọng |
|---|---|---|
| **1** | Đổi shader FX từ màu nhạt/Additive nhiều sang thân màu đậm Alpha Blend, lõi Additive nhỏ | Rất cao |
| **2** | Làm hai vòng quanh thân cho trạng thái duy trì: vàng ở eo, cyan ở ngực thấp | Rất cao |
| **3** | Giảm brightness map Willowbrook 7%, dịch hue cỏ +10–15° về xanh lục | Cao |
| **4** | Giảm chi tiết nhỏ map 15–25%, nhất là quanh khu vực đánh quái | Cao |
| **5** | Chỉnh huy hiệu kiếm/khiên và vòm khiên lúc kích hoạt: viền rõ, màu đậm, ít trắng | Trung bình |
| **6** | Tối ưu render trước/sau sprite, LOD particle và kiểm tra khi di chuyển | Trung bình |

### 5. Tiêu chí nghiệm thu

- **FX kích hoạt:** Nhìn rõ trên cả nền tuyết trắng và cỏ xanh; vẫn thấy màu vàng/cyan đặc trưng khi giảm Bloom về 0 để kiểm tra.
- **Buff duy trì:** Đứng yên hay chạy đều thấy hai vòng khác màu, không che mắt, tóc, tên và thanh HP.
- **Map:** Bớt chói nhưng không tối hoặc xám; hoa và cây vẫn giữ phong cách Soft Fantasy.
- **Hiệu năng:** Hai vòng dùng mesh đơn giản, shader dùng chung với MaterialPropertyBlock để thay màu/alpha; hạn chế particle không cần thiết.

**Chốt thiết kế:** Mình chọn **FX đậm theo Midgard + map Willowbrook dịu hơn khoảng 7% + hai vòng năng lượng quanh thân khi duy trì buff**.

Đừng cố biến Willowbrook thành map phẳng hoàn toàn như Midgard. Nền vẽ tay nhiều lớp là thế mạnh của game bạn. Điều cần thay đổi là để **nhân vật và FX nổi hơn nền**, chứ không phải hy sinh toàn bộ chi tiết map.

Sau khi chỉnh, nên chụp lại cùng một vị trí, cùng camera, cùng thời điểm buff trên cả map tuyết và map cỏ để so sánh trực tiếp.


---

# Phần bóng đổ

# Chốt hệ thống bóng đổ cho game RPG 2D – So với Ragnarok Midgard Senki

**Mình đề xuất bổ sung bóng đổ trước khi chỉnh màu map lần cuối.** Willowbrook hiện có nền vẽ tay đẹp, nhưng nhân vật, quái, cây, nhà và đá chưa được liên kết với mặt đất bằng hệ thống ánh sáng thống nhất. Đây là một trong những nguyên nhân khiến cảnh trông phẳng và có cảm giác chói.

Tuy nhiên, không nên biến game thành hệ thống bóng 3D thực. Với RPG 2D góc nhìn 3/4, cách phù hợp nhất là kết hợp:

- **Blob shadow:** Bóng elip mềm dưới chân nhân vật, quái và boss.
- **Directional shadow:** Bóng chiếu lệch theo hướng nắng cho cây, nhà, đá lớn.
- **Contact shadow:** Bóng tiếp xúc nhỏ, đậm hơn ngay dưới vật thể.
- **Baked shadow:** Bóng tĩnh vẽ hoặc tạo sẵn cho địa hình, công trình.

Các số liệu dưới đây là thông số thiết kế đề xuất cho Unity, không phải số liệu nội bộ đã xác minh của Ragnarok.

## 1. Midgard Senki và Ragnarok đổ bóng như thế nào?

### 1.1. Ragnarok Midgard Senki – Ảnh 1

Quan sát ảnh bạn gửi:

- **Nhân vật:** Có vùng tối nhỏ ngay dưới chân, nhưng không thấy bóng hình người kéo dài rõ rệt theo hướng nắng.
- **Quái:** Poring có vùng tiếp xúc tối bên dưới. Bóng rất ngắn, mềm, không nổi bật.
- **Màu bóng:** Có xu hướng tối hơn màu nền, hơi lạnh trên vùng cỏ. Không phải bóng đen đặc.
- **Hướng nắng:** Không có bóng chiếu dài đủ rõ để xác định chính xác góc chiếu sáng.
- **Cây và nhà:** Ảnh Midgard này không có cây lớn hay nhà đủ rõ để đánh giá bóng của chúng.
- **FX:** Vệt đỏ/xanh nổi bật chủ yếu nhờ màu bão hòa, mảng màu đậm và tương phản với nền vàng, không phải nhờ hệ thống bóng đổ phức tạp.

**Kết luận:** Từ ảnh này có thể xác nhận cảm giác bóng tiếp xúc mềm, nhưng không đủ cơ sở nói Midgard dùng directional shadow cho mọi đối tượng.

### 1.2. Ragnarok Online PC gốc

- Nhân vật 2D đứng trên nền 3D thường có bóng tối đơn giản dưới chân.
- Bóng nhân vật thiên về dạng elip hoặc blob, không phải silhouette mô phỏng hình người.
- Một số vật thể môi trường có vùng tối được thể hiện bằng texture, mô hình và ánh sáng của map.
- Cách thể hiện khác nhau theo client, map và phiên bản.

### 1.3. Ragnarok Origin và các bản Ragnarok 3D hiện đại

- Nhân vật và quái 3D thường có contact shadow và bóng môi trường mềm hơn.
- Cây, nhà, đá có thể tạo bóng chiếu có hướng tùy cấu hình đồ họa.
- Ambient Occlusion giúp vùng chân tường, gốc cây và khe đá tối hơn.
- Không nên sao chép trực tiếp bóng 3D dài vào game 2D của bạn, vì sprite 2D không có hình học chiều sâu thực.

### 1.4. Phong cách nên chọn

| Thành phần | Hướng thể hiện |
|---|---|
| Nhân vật | Blob mềm + bóng lệch rất ngắn |
| Quái thường | Blob theo kích thước thân |
| Boss lớn | Blob rộng + bóng tiếp xúc |
| Cây | Bóng gốc + bóng tán lệch |
| Nhà | Bóng chân tường + bóng mái |
| Đá | Bóng tiếp xúc + bóng chiếu ngắn |
| Vách đá | Bóng mép vách và vùng chân vách |

**Mình chọn phong cách Ragnarok Origin mềm và sáng, kết hợp sự đơn giản của Midgard Senki.** Không dùng bóng đen dài, sắc như game 3D realistic.

---

## 2. Thông số bóng đề xuất cho Willowbrook

### 2.1. Chốt hướng nắng

Dùng một hướng nắng cố định cho tất cả map ngoài trời, sau đó điều chỉnh nhẹ theo biome.

- Hướng ánh sáng trên màn hình: **từ góc trên-trái xuống góc dưới-phải**.
- Vector hướng bóng trên màn hình: `(0.75, 0.66)` sau khi chuẩn hóa.
- Góc hướng bóng: khoảng **41° từ trục ngang sang phải, hướng xuống**.
- Độ cao mặt trời giả lập: **55–60°**.
- Độ dài bóng vật thể đứng: khoảng **0,25–0,45 lần chiều cao hiển thị**, tùy loại vật thể.

Đây là hướng nghệ thuật trên màn hình, không phải góc Euler của Unity. Với camera 3/4, phải chuyển hướng này sang tọa độ mặt đất của scene.

Mình không khuyên dùng bóng dài bằng chiều cao vật thể. Nó sẽ làm cảnh bị bận và khiến cây trông như đang nghiêng hoặc đổ.

### 2.2. Màu bóng


  {#each [{c:"#536B69",n:"Bóng chính"},{c:"#59667D",n:"Bóng lạnh"},{c:"#5D6250",n:"Bóng cỏ"},{c:"#766B69",n:"Bóng đất"},{c:"#667B8E",n:"Bóng tuyết"}] as s}
    
      
      {s.n}
      {s.c}
    
  {/each}


- **Màu mặc định:** `#536B69` – xám xanh hơi lạnh.
- Trên cỏ: `#5D6250` – xanh olive xám, tránh bóng tím quá rõ.
- Trên đường đất: `#766B69` – nâu xám lạnh.
- Trên tuyết: `#667B8E` – xanh xám lạnh.
- Không dùng `#000000` làm màu bóng chính.
- Không dùng tím bão hòa cao vì sẽ khiến cảnh chuyển sang cảm giác phép thuật thay vì ánh sáng tự nhiên.

Lưu ý: Màu cuối cùng phụ thuộc blending. Một lớp bóng màu `#536B69` alpha 0,3 phủ lên cỏ sẽ khác rất nhiều so với nhân trực tiếp màu nền với hệ số 0,7.

### 2.3. Bóng nhân vật và quái

| Đối tượng | Bề rộng bóng so với thân | Alpha đỉnh | Độ lệch |
|---|---:|---:|---|
| Hero | 75–90% | 0,24–0,32 | 3–6 px |
| Pet nhỏ | 65–85% | 0,18–0,25 | 2–4 px |
| Slime | 80–100% | 0,22–0,30 | 2–5 px |
| Quái thường | 80–105% | 0,24–0,34 | 4–7 px |
| Boss lớn | 90–110% | 0,30–0,40 | 8–15 px |

*Pixel tính theo kích thước gameplay gần với ảnh Willowbrook 1280×720, không phải pixel của sprite gốc 192×192.*

- Bóng có dạng elip nằm trên mặt đất, không phải hình tròn đứng.
- Tỷ lệ chiều sâu/bề rộng ellipse: **0,35–0,50**.
- Mép bóng feather mềm, chiếm **25–35% bán kính**.
- Tâm bóng hơi lệch về dưới-phải theo hướng nắng.
- Hero nên có một vùng contact tối hơn ngay dưới giày, nhưng không cần thêm một đĩa bóng thứ hai quá rõ.

**Không nên squash sprite nhân vật rồi tô đen** cho hero và quái thường. Cách đó dễ tạo hình người nằm trên đất, trông giả và không hợp chibi.

### 2.4. Bóng boss lớn

Với boss Wanted kích thước lớn:

- Dùng một blob elip lớn theo footprint thực tế của boss, không theo toàn bộ chiều cao sprite.
- Bề rộng khoảng **0,9–1,1 lần vùng thân tiếp xúc mặt đất**.
- Alpha trung tâm **0,30–0,40**, mép tan xuống 0.
- Thêm một vùng contact tối nhỏ dưới chân hoặc bụng boss.
- Nếu boss có nhiều chân, chỉ dùng một bóng tổng lớn và 2–4 contact nhỏ rất nhẹ.
- Khi boss nhảy, giảm alpha và tăng kích thước bóng nhẹ theo độ cao, để thể hiện đang rời mặt đất.

Với Mire Hydra Queen, bóng lớn phải giúp người chơi cảm nhận trọng lượng boss nhưng không làm mất vòng target indicator hay telegraph.

## 3. Bóng cây, nhà, đá và vách – Phần Willowbrook đang thiếu nhiều nhất

### 3.1. Cây lớn

Đây là nhóm nên được ưu tiên sau bóng nhân vật.

- **Bóng gốc cây:** Elip nhỏ dưới thân, alpha 0,30–0,40, feather 25%.
- **Bóng tán cây:** Một mảng bóng mềm, hơi méo, chiếu về dưới-phải.
- Kích thước bóng tán: khoảng **60–80% đường kính tán cây**.
- Độ lệch tâm bóng: khoảng **20–35% đường kính tán**, theo hướng nắng.
- Alpha: **0,14–0,22**, không nên đậm như bóng gốc.
- Mép bóng: feather 20–30% bán kính.
- Hình bóng: 3–5 khối tròn mềm nối nhau, không dùng một ellipse hoàn hảo.

**Không dùng sprite cây gốc squash rồi tô đen** cho tất cả cây. Lá và cành sẽ tạo silhouette rối, đôi khi giống một cây bị đổ xuống đất.

Thay vào đó, tạo 3–4 mẫu bóng tán bằng mesh phẳng, shader radial/soft mask và biến đổi scale, rotation.

### 3.2. Nhà và công trình

| Thành phần | Alpha | Cách làm |
|---|---:|---|
| Chân tường | 0,28–0,38 | Dải bóng tiếp xúc |
| Bóng mái hiên | 0,22–0,32 | Mesh theo mép mái |
| Bóng nhà chiếu xuống đất | 0,14–0,22 | Polygon mềm lệch hướng nắng |
| Bóng hàng rào | 0,12–0,20 | Dải ngắn, đứt đoạn |

- Độ dài bóng mái/nhà: khoảng **15–30% chiều cao hiển thị của nhà**.
- Không cần tạo silhouette đầy đủ của cả căn nhà.
- Với nhà mái tranh ở Willowbrook, ưu tiên bóng mái chiếu xuống tường và đất gần chân nhà.
- Bóng tĩnh có thể bake trực tiếp vào texture nền hoặc một lớp shadow overlay riêng.

**Lưu ý quan trọng:** Nhà đã được vẽ với ánh sáng và bóng nội tại. Không nên phủ thêm directional shadow quá đậm khiến mặt tiền tối hai lần.

### 3.3. Đá và vách

- Đá nhỏ: contact shadow alpha **0,22–0,30**, lệch 3–8 px.
- Đá lớn: contact shadow 0,28–0,35; bóng chiếu dài 15–25% chiều cao đá.
- Vách đá: tạo dải tối dưới mép vách, alpha **0,20–0,32**.
- Chân vách: tăng tối 10–18% trong vùng sát nền.
- Bóng mép mềm 8–20 px tùy kích thước vật thể.

Không nên tạo một bóng đen chạy liên tục quanh mọi mép vách, vì sẽ khiến địa hình trông như được cắt từ giấy.

---

## 4. Cách tạo bóng trong Unity 2D

### 4.1. Nên dùng blob, mesh hay sprite bóng?

| Phương pháp | Đề xuất |
|---|---|
| Blob elip bằng shader | **Dùng cho hero, pet, quái, boss** |
| Mesh polygon + shader mềm | **Dùng cho cây, nhà, đá lớn** |
| Bóng bake vào map | **Dùng cho công trình và địa hình tĩnh** |
| Squash sprite gốc rồi tô đen | Không dùng làm mặc định |
| Realtime 3D shadow map | Không cần trong chế độ 2D |

**Cấu hình blob shadow đề xuất:**

- Một quad mesh nằm sát mặt đất.
- Shader Unlit Transparent với radial falloff.
- Một material dùng chung.
- Màu, alpha, scale truyền qua dữ liệu instance hoặc vertex color.
- Không dùng Bloom, Distortion, Dissolve.
- Không cần Particle System cho bóng.

Nếu dùng MaterialPropertyBlock, lưu ý nó không tự bảo đảm batching hoặc GPU instancing. Cần kiểm tra draw calls thực tế bằng Unity Frame Debugger và Profiler.

### 4.2. Bóng chồng lên nhau có bị đậm hơn không?

**Không nên để các bóng chồng lên nhau cộng dồn vô hạn.**

Ví dụ, 5 người đứng sát nhau, mỗi bóng alpha 0,30. Nếu Alpha Blend thông thường, vùng chồng có thể tối tới:

\[
1-(1-0.3)^5 \approx 0.832
\]

Tức độ phủ tương đương 83%, quá tối.

Mình đề xuất:

- Với blob của nhân vật và quái: render vào một shadow mask riêng, lấy **max opacity** thay vì cộng dồn.
- Giới hạn độ tối tương đương alpha **0,35–0,40** cho vùng bóng động.
- Bóng cây/nhà tĩnh có thể bake riêng, nhưng cần tránh cộng chồng quá mức với mask động.
- Có thể dùng một render target độ phân giải bằng 1/2 hoặc 1/4 màn hình, sau đó composite một lần lên mặt đất.
- Khi không có render target, dùng alpha thấp hơn và kiểm soát mật độ bóng; cách này rẻ hơn nhưng không loại bỏ hoàn toàn cộng dồn.

---

## 5. Bóng có giúp FX nổi hơn và map bớt chói không?

**Có, nhưng bóng không thay thế việc chỉnh màu map.**

Bóng giúp theo ba cách:

- **Tạo chiều sâu:** Nhân vật và quái không còn cảm giác dán lên nền.
- **Tăng tương phản cục bộ:** Vùng tối dưới chân khiến vòng vàng/cyan của buff dễ nhận biết hơn.
- **Phân cấp ánh sáng:** Cảnh có vùng sáng, vùng trung gian và vùng tối, thay vì mọi thứ cùng sáng.

Tuy nhiên, blob dưới chân chỉ ảnh hưởng một vùng nhỏ. Nó không giải quyết được toàn bộ cảm giác chói của cỏ vàng và hoa trắng.

### Nên chỉnh bóng và màu map cùng lúc không?

**Nên làm cùng một đợt nghiệm thu, nhưng triển khai theo thứ tự.**

1. Thêm bóng nhân vật, quái, cây và nhà với thông số cơ bản.
2. Chụp lại Willowbrook ở đúng camera hiện tại.
3. Sau đó giảm brightness map, bắt đầu từ **−4%**, thay vì áp dụng ngay −7%.
4. Nếu vẫn chói, giảm tiếp từng bước 2%.
5. Cuối cùng tinh chỉnh saturation và hue cỏ.

Lý do: Sau khi thêm bóng, cảm giác sáng tổng thể của map đã thay đổi. Nếu vừa giảm brightness 7%, vừa thêm bóng đậm, cảnh có thể trở nên tối và thiếu sức sống.

Mình đề xuất preset cuối cùng để thử:

| Thuộc tính | Mức thử |
|---|---:|
| Brightness map | −4% đến −7% |
| Saturation map | −3% đến −6% |
| Contrast toàn map | −3% |
| Hue cỏ | +10° đến +15° |
| Bóng nhân vật | Alpha 0,28 |
| Bóng tán cây | Alpha 0,18 |
| Bóng nhà | Alpha 0,20 |
| Bóng đá | Alpha 0,26 |

**Quan trọng:** Không giảm toàn bộ contrast quá nhiều. FX cần nền dịu, nhưng nhân vật vẫn cần tương phản rõ với mặt đất.

---

## 6. Chi phí hiệu năng – 40 người + quái

Với chế độ 2D Unity, hệ thống bóng này có thể rất nhẹ nếu thiết kế đúng.

### 6.1. Ước lượng ngân sách

Giả sử có 40 người chơi, 20 quái thường, 1 boss và khoảng 30 vật thể môi trường nhìn thấy.

| Nhóm | Số bóng | Cách xử lý |
|---|---:|---|
| 40 hero | 40 | Blob instanced |
| 20 quái | 20 | Blob instanced |
| 1 boss | 1–5 | Blob + contact |
| Cây, nhà, đá | ~30 | Baked hoặc mesh tĩnh |
| Tổng | ~95 | Chủ yếu quad đơn giản |

Đây là ngân sách ví dụ, không phải số lượng cố định của map.

- **Draw calls mục tiêu:** 1–3 cho bóng động nếu dùng instancing hoặc batching phù hợp; bóng tĩnh đã bake không cần draw call riêng.
- **Triangle:** Khoảng 2 tam giác cho mỗi blob; dưới 300 tam giác cho phần lớn bóng động.
- **Shader:** Unlit, một lần sample mask hoặc radial falloff toán học.
- **Render texture:** Nếu dùng shadow mask, thử 1/2 resolution trước; 1/4 nếu cần tối ưu mobile.
- **Chi phí mục tiêu:** Dưới **0,5 ms GPU trên PC**, dưới **1 ms GPU trên mobile tầm trung** cho riêng bóng. Đây là mục tiêu cần đo, không phải hiệu năng được bảo đảm.

### 6.2. Các quy tắc tối ưu

- Dùng **một material chung** cho tất cả blob.
- Không tạo material riêng cho từng nhân vật.
- Dùng GPU instancing hoặc gộp mesh theo nhóm nếu pipeline hỗ trợ.
- Không tạo shadow camera riêng cho từng nhân vật.
- Không dùng realtime light/shadow cho toàn bộ sprite.
- Chỉ cập nhật vị trí và scale bóng khi nhân vật di chuyển hoặc thay đổi trạng thái.
- Với vật thể tĩnh, bake bóng để giảm chi phí.
- Tắt particle của bóng hoàn toàn; bóng không cần particle.
- Với nhân vật xa, giảm độ phân giải mask thay vì xóa bóng ngay, để tránh nhân vật trông lơ lửng.

---

## 7. Xếp lại thứ tự ưu tiên so với 6 việc trước

Mình sẽ **đưa hệ thống bóng lên ưu tiên số 2**, ngay sau việc chỉnh FX quá nhạt.

| Ưu tiên mới | Việc cần làm | Lý do |
|---|---|---|
| **1** | Đổi màu và blending FX sang kiểu Midgard đậm, rõ | Tác động trực tiếp đến độ rõ skill |
| **2** | **Thêm hệ thống bóng hero, quái, boss, cây, nhà, đá** | Sửa vấn đề cảnh phẳng, thiếu chiều sâu |
| **3** | Làm hai vòng buff duy trì quanh thân | Giúp nhận diện Damage/Defense rõ ràng |
| **4** | Chỉnh brightness/hue/saturation map sau khi có bóng | Tránh chỉnh map quá tối |
| **5** | Giảm 15–25% chi tiết nhỏ ở vùng chiến đấu | Giúp FX nổi hơn |
| **6** | Chỉnh huy hiệu kiếm/khiên, vòm khiên khi kích hoạt | Hoàn thiện độ rõ |
| **7** | Tối ưu batching, LOD, kiểm tra hiệu năng và sorting | Bảo đảm chạy ổn định |

## 8. Thông số chốt để đưa cho người lập trình

- **Shadow style:** Soft Fantasy, blob + directional mềm.
- **Sun direction:** Trên-trái → dưới-phải, khoảng 41° trên màn hình.
- **Shadow color:** `#536B69`, điều chỉnh theo biome.
- **Hero blob:** Width 0,85× thân, ellipse depth 0,42, alpha 0,28.
- **Monster blob:** Width 0,95× thân, alpha 0,28.
- **Boss blob:** Theo footprint, alpha 0,35.
- **Tree crown:** Width 0,7× tán, alpha 0,18, lệch 0,3× đường kính tán.
- **House shadow:** Alpha 0,20, chiều dài 0,2× chiều cao hiển thị.
- **Rock shadow:** Alpha 0,26.
- **Edge feather:** 25–35% bán kính.
- **Overlap:** Max-opacity shadow mask, cap alpha 0,40.
- **Rendering:** Shared Unlit shader, GPU instancing hoặc mesh batching; bake bóng tĩnh.

**Kết luận:** Game của bạn không cần hệ thống bóng 3D phức tạp. Chỉ cần bổ sung blob shadow nhất quán cho nhân vật/quái và directional shadow mềm cho cây/nhà/đá là Willowbrook sẽ có chiều sâu hơn đáng kể.

Mình đặc biệt khuyên **không dùng bóng đen dài kiểu silhouette**. Với phong cách RO Soft Fantasy, bóng nên mềm, ngắn, hơi lạnh và đủ đậm để tạo cảm giác vật thể thực sự đứng trên mặt đất, trong khi các hiệu ứng Damage vàng và Defense cyan vẫn là điểm sáng nổi bật nhất.

