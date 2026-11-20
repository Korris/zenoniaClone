# Quan sát FX skill Ragnarok 3 + Ragnarok Midgard Senki (ChatGPT, 20/11/2026)

Chat (Tay, chỉ chữ): https://chatgpt.com/c/6b003947-a9a8-83ec-a9bf-b60212cc8de1 . ChatGPT KHÔNG xem được frame video YouTube, chỉ ảnh gameplay + bài viết.

## 1. Kết quả tìm web: RO3 và Ragnarok Midgard Senki

**Kết luận bước đầu: Bạn nhận xét đúng về mặt hình ảnh.** Các hiệu ứng chiến đấu hiện đại trong RO3 sử dụng nhiều vùng sáng có biên mềm, lớp màu bán trong suốt, vệt chuyển động và hạt sáng. Chúng không có cảm giác như một hình minh họa đặc được lật frame liên tục.

Tuy nhiên, **không thể kết luận rằng các game này không dùng sprite sheet**. Ngay cả hiệu ứng ánh sáng rất hiện đại vẫn có thể sử dụng sprite sheet làm texture cho particle, mesh và shader.

### Giới hạn kiểm chứng

- Mình đã tìm và kiểm tra các ảnh gameplay được công bố, trong đó có ảnh hiện tên skill trực tiếp.
- Đã tìm được video RO3 giới thiệu nhiều class và video gameplay Midgard Senki.
- **Mình chưa xem trực tiếp được nội dung từng khung hình của các video YouTube** vì truy cập video bị hạn chế. Vì thế không thể tuyên bố đã đo thời lượng, phân tích chuyển động từng frame hay xác minh shader của 4–6 skill mỗi game.
- Những phần mô tả hiệu ứng dưới đây sẽ phân biệt rõ **[Quan sát ảnh]**, **[Thông tin nguồn]** và **[Suy luận kỹ thuật]**.

Một điểm cần sửa: Ragnarok: Midgard Senki là MMORPG 2D phong cách paper-cut, dùng hệ thống skill theo vũ khí. Ragnarok: The Lost Memories là RPG thẻ bài phát hành trước đó, không phải tên khác của Midgard Senki. 

## 2. Ragnarok Online 3 — phân tích các hiệu ứng tìm được

### A. Archbishop — Epiclesis Prayer

Nguồn: [Ảnh gameplay RO3 — Archbishop](https://cdn.wanuxi.com/2025/08/Ragnarok-Online-3_first-gameplay_trailer-6.jpg)

- **[Quan sát ảnh]** Có cột sáng trắng-vàng gần vị trí đồng đội, vùng chân cột rất sáng và các nét sáng xanh nhạt xung quanh. Nhãn Epiclesis Prayer hiện trong cảnh.
- **[Suy luận]** Có thể tách thành 3–4 lớp: cột sáng mềm, vùng sáng sát đất, vệt/hạt sáng nhỏ, vòng phép.
- **Lõi:** trắng ngà/vàng nhạt; **quầng:** xanh trắng.
- **Nền:** cỏ xanh khá sáng, nhưng cột sáng vẫn nổi nhờ màu gần trắng, độ sáng tập trung và sự chuyển tiếp với nền xanh.
- **Viền tối:** Không thấy viền outline tối rõ quanh cột sáng.
- **Thời lượng:** Chưa xác định.

### B. Archbishop — First Aid

Nguồn: cùng ảnh trên.

- **[Quan sát ảnh]** Nhãn First Aid hiện phía trên một nhân vật.
- **Chưa xác định được FX riêng** của First Aid do trong ảnh có nhiều skill đồng thời.
- Không đủ căn cứ để nói có cột sáng, hạt xanh, vòng đất hay Bloom riêng của First Aid.
- **Thời lượng và blending:** Chưa xác định.

### C. Archbishop / Party — Ice Mist

Nguồn: cùng ảnh trên.

- **[Quan sát ảnh]** Nhãn Ice Mist hiện giữa nhóm nhân vật; có vùng hiệu ứng xanh trắng trong cảnh.
- **Chưa thể tách chắc chắn** vùng xanh trắng nào thuộc Ice Mist, vùng nào thuộc kỹ năng khác.
- **[Suy luận]** Một hiệu ứng sương băng phù hợp với phong cách này có thể dùng lớp sương alpha mềm, hạt băng và ánh xanh rất nhẹ.
- **Thời lượng:** Chưa xác định.

### D. High Wizard — Cold Bolt

Nguồn: [4Gamer — ảnh gameplay High Wizard](https://www.4gamer.net/games/854/G085427/20250731033/screenshot.html)

- **[Quan sát ảnh]** Có nhãn Cold Bolt trong cảnh đánh Hydra; tại vùng giao chiến xuất hiện các tia và hạt trắng-xanh cùng những điểm sáng nổi bật.
- **Lõi:** trắng-xanh; **quầng:** tím/xanh lạnh.
- **Nền:** hang động xanh tím tối, nên các điểm sáng tương phản khá mạnh.
- Không thấy outline đậm quanh từng tia.
- **[Suy luận]** Có thể dùng các đoạn tia ngắn hoặc mesh/particle; không xác nhận được có distortion.
- **Thời lượng:** Chưa xác định.

### E. Warlock — phép xoáy băng và lửa

Nguồn: [GamingPH — ảnh Warlock chiến đấu](https://gamingph.com/2025/07/ragnarok-online-3-reveals-first-gameplay-trailer-pre-registration-now-open/)

- **[Quan sát ảnh]** Một vùng xoáy xanh đen có các dải cong cyan và bông tuyết sáng, cùng vật thể lửa cam-vàng đang rơi xuống. Nhãn Crimson Rock xuất hiện trong cảnh.
- **Xoáy băng:** phần nền xoáy xanh đen tương đối đặc, mép cong cyan sáng và các hạt tuyết trắng-xanh.
- **Lửa:** lõi vàng sáng, thân cam, viền ngoài đỏ/cam mờ.
- **Nền:** đất cát vàng rất sáng. Vòng băng vẫn nổi vì có **mảng xanh đen làm nền tương phản**, không chỉ nhờ Bloom.
- Đây là ví dụ quan trọng: FX trông phát sáng nhưng vẫn cần vùng màu đậm.
- **[Suy luận]** Vòng xoáy có thể là mesh với texture cuộn và alpha, phối hợp particle tuyết.
- **Thời lượng:** Chưa xác định.

### F. Đòn đánh vòng vàng — nhân vật cận chiến

Nguồn: [Ảnh RO3 — đòn đánh vòng vàng trong dungeon](https://www.ligagame.tv/images/2025/r3_174fc.png)

- **[Quan sát ảnh]** Vòng cung vàng bao quanh các mục tiêu, có nhiều tia trắng-vàng ngắn và điểm sáng khi trúng đòn.
- **Lõi:** vàng/trắng; **quầng:** vàng cam trong suốt.
- **Nền:** sàn đá nâu tối, giúp vòng vàng nổi bật.
- **[Suy luận]** Rất phù hợp để dựng bằng mesh arc/trail kết hợp spark particle.
- Không thể xác nhận đây là skill cụ thể nào của Knight chỉ từ ảnh.
- **Thời lượng:** Chưa xác định.

### G. Raid 10 người và giao tranh đông

Nguồn: [4Gamer — trải nghiệm boss 10 người tại G-STAR 2025](https://www.4gamer.net/games/854/G085427/20251116005/)

- **[Thông tin nguồn]** Người trải nghiệm xác nhận trận boss 10 người có rất nhiều thông tin trên màn hình; việc né đòn và quan sát đồng đội khá khó.
- **[Quan sát ảnh giao tranh đông]** Các vòng vàng, tia và luồng sáng vẫn nhìn được trên nền đất đá, nhưng nhân vật và nhãn tên bắt đầu chồng lấn.
- Chưa có căn cứ xác nhận RO3 tự động giảm FX đồng đội theo số người.

**Nhận định riêng về RO3:** Hiệu ứng không đơn thuần là ánh trắng cộng sáng lên nền. Các ảnh cho thấy sự phối hợp giữa **vùng màu đậm có hình dạng, ánh sáng ở mép, các hạt sáng riêng và hiệu ứng va chạm**.

## 3. Ragnarok: Midgard Senki — nguồn và phân tích

Nguồn tìm được:

- [Midgard Senki — Gameplay Android/iOS, tháng 11/2025](https://www.youtube.com/watch?v=9W7QTuTF-eI)
- [Midgard Senki — Gameplay CBT, tháng 3/2026](https://www.youtube.com/watch?v=5S5ADu4SxVk)
- [Online Station — ảnh giao tranh PvP](https://www.online-station.net/mobile-game/996505)
- [24BUYM — giới thiệu skill theo vũ khí](https://www.24buym.com/blog/ragnarok-midgard-senki-final-test-2026)

Mình xác minh được ba tên skill cụ thể từ bài viết và một cảnh chiến đấu có nhiều loại FX. **Chưa đủ dữ liệu để phân tích trực quan 4–6 skill riêng biệt của Midgard Senki như yêu cầu**, nên không gán các hiệu ứng không rõ nguồn cho skill cụ thể.

### A. Bowling Bash — kiếm

- **[Thông tin nguồn]** Jellopy Sword cung cấp Bowling Bash, đòn tấn công diện rộng. 
- **Chưa quan sát được đầy đủ animation.** Không thể kết luận đòn chém gồm mấy trail hoặc có shockwave.
- **[Đề xuất dựng tương tự]** Một mesh trail cong, một vệt va chạm ngắn, 4–8 spark nhỏ.
- **Màu, thời lượng thực tế:** Chưa xác minh.

### B. Lightning Bolt — trượng phép

- **[Thông tin nguồn]** Smooth Staff cung cấp Lightning Bolt, tấn công một mục tiêu. 
- **Chưa quan sát được trực tiếp toàn bộ FX.**
- **[Đề xuất dựng tương tự]** Tia sét phân nhánh xanh-tím có lõi trắng, một flash va chạm và một ít spark.
- **Màu, thời lượng thực tế:** Chưa xác minh.

### C. Arrow Shower — cung

- **[Thông tin nguồn]** Skill Arrow Shower được nhắc đến khi chuyển từ trượng sang cung. 
- **Chưa xác minh hình ảnh từng mũi tên**, số đợt rơi hay vòng đánh dấu.
- **[Đề xuất dựng tương tự]** Nhiều projectile nhỏ và spark chạm đất, ưu tiên hình dạng mũi tên rõ hơn Bloom.
- **Thời lượng:** Chưa xác minh.

### D. Giao tranh PvP nhiều hiệu ứng

Nguồn: [Ảnh PvP Midgard Senki](https://www.online-station.net/mobile-game/996505)

- **[Quan sát ảnh]** Có một vòng năng lượng xanh-tím lớn, mép có các tia sáng nhọn. Bên trong là vùng màu tím/xanh bán trong suốt; phía bên phải có hiệu ứng lửa cam-vàng và các vụ nổ tím.
- **Lõi/quầng:** Vòng xanh có phần mép sáng hơn vùng bên trong; hiệu ứng lửa có vùng vàng sáng và mép cam.
- **Nền:** Cát vàng nhạt, rất gần độ sáng của Soft Fantasy. Dù vậy, vòng xanh-tím vẫn đọc được nhờ khác biệt màu và hình dạng mép.
- **Viền tối:** Không có outline nâu/đen kiểu sprite cartoon quanh vòng. Vùng tím đậm ở bên trong góp phần tạo tương phản.
- **[Suy luận]** Có thể tái hiện bằng vòng mesh alpha, tia sáng additive và particle nhỏ.
- **Thời lượng, distortion, dissolve:** Không xác minh được.

**Nhận định về Midgard Senki:** Ảnh PvP củng cố rằng FX bán trong suốt có thể nổi trên nền sáng khi sử dụng màu đủ tách biệt và hình dạng mép rõ. Nhưng chưa đủ bằng chứng để kết luận toàn bộ thư viện FX của game dùng cùng một shader hoặc phương pháp.

---

## 4. Họ có thực sự dùng ánh sáng trong suốt thay sprite nguyên khối?

**Có thể khẳng định về hình thức thị giác, chưa thể khẳng định về cấu trúc tài nguyên.**

- **Chắc chắn từ ảnh:** Có các vùng trong suốt, ánh sáng mép mềm, mảng màu đặc hơn, hạt sáng và hình dạng được phối hợp trong cùng hiệu ứng.
- **Suy luận rất hợp lý:** Particle System, mesh theo đường cong, texture alpha và nhiều chế độ blending là những cách phù hợp để dựng các hiệu ứng đó.
- **Chưa chứng minh:** Họ dùng chính xác shader nào, bao nhiêu particle, có dùng Distortion hay Dissolve hay không.
- **Không đúng nếu nói tuyệt đối:** Sprite sheet là công nghệ cũ và không thể làm FX như vậy. Một flipbook RGBA/HDR đặt trên mesh hoặc particle vẫn có thể tạo FX mềm, trong suốt và phát sáng.

## 5. Quy tắc chung mình rút ra cho game của bạn

Các giá trị sau là **preset Unity mình đề xuất**, không phải thông số được trích xuất từ RO3 hoặc Midgard Senki.

### A. Thiết kế FX 3–5 lớp

1. **Shape layer:** hình dạng chính đủ rõ, alpha 0.45–0.8, ưu tiên Premultiplied Alpha hoặc alpha blend.
2. **Inner light:** lõi sáng nhỏ, alpha/additive có kiểm soát.
3. **Outer glow:** quầng mềm, alpha 0.1–0.3, không che toàn bộ nhân vật.
4. **Particle accents:** spark, bụi sáng, tia nhỏ; additive mạnh nhưng tổng diện tích thấp.
5. **Impact:** flash ngắn và sóng lan; chỉ dùng distortion nhẹ nếu thực sự giúp tạo cảm giác lực.

Không cần skill nào cũng đủ năm lớp.

### B. Alpha/Additive

- Không cố định tỷ lệ 70–85% alpha cho toàn game.
- Ưu tiên alpha/premultiplied cho **hình dạng chính**; additive cho điểm sáng nhỏ và quầng.
- Mục tiêu là FX vẫn nhận ra được khi tắt Bloom. Nếu tắt Bloom mà skill gần như biến mất, cần sửa shape hoặc màu.

### C. Đọc rõ trên map sáng

- Giữ vùng lõi màu bão hòa trung bình và có độ sáng khác nền.
- Dùng đường viền **mềm và có màu**, không viền đậm khép kín.
- Vòng phép xanh mint trên tuyết nên có một dải teal hoặc xanh lam đậm hơn ở phần sát đất.
- Ánh sáng trắng chỉ tập trung vào spark, lõi va chạm và những khoảnh khắc rất ngắn.
- Không phụ thuộc hoàn toàn vào Bloom để tạo nhận diện skill.

### D. Combat boss 40 người

Đề xuất ba mức chất lượng hiển thị:

| Thành phần | FX giữ lại |
|---|---|
| Bản thân, mục tiêu, boss | 100% hiệu ứng gameplay |
| Party | 50–70% particle trang trí |
| Người chơi khác | 15–30% particle trang trí |

- Hiệu ứng boss cảnh báo nguy hiểm **không được giảm**.
- Có thể gộp vòng buff và giảm các particle trùng loại.
- Giới hạn flash trắng trên cùng vùng màn hình.
- Dùng giới hạn ngân sách particle và overdraw, không chỉ giới hạn số lượng GameObject.

## 6. Áp dụng cho bốn skill Cleric

### Heal — hồi máu

Mục tiêu: mềm, thánh thiện, dễ nhìn trên nền tuyết.

- **Lớp 1:** Vòng phép mỏng xanh teal `#48BFA8`, alpha 0.55–0.7, nằm dưới chân.
- **Lớp 2:** Dải ánh sáng vàng kem `#FFE6A0` bay lên, alpha 0.35–0.55.
- **Lớp 3:** 6–10 hạt sáng nhỏ trắng-vàng, additive.
- **Lớp 4:** Flash vàng ngắn ở ngực, không phủ trắng toàn thân.
- **Thời lượng đề xuất:** 0.65–0.85 giây; vòng phép biến mất trước quầng sáng.
- **Lưu ý:** Giữ con số hồi máu màu xanh đọc rõ và hiển thị phía trên FX.

### Spirit Blessing — buff sát thương

- **Lớp 1:** Hai cung rune xanh ngọc xoay chậm dưới chân, alpha 0.5–0.7.
- **Lớp 2:** Luồng năng lượng xanh-vàng xoắn nhẹ quanh nhân vật, dùng mesh mềm.
- **Lớp 3:** Hạt sáng vàng nhỏ bay lên.
- **Lớp 4:** Một flash trắng-vàng ngắn khi buff được áp dụng.
- **Thời lượng kích hoạt:** 0.8–1.1 giây; sau đó chỉ giữ icon trạng thái, không để quầng sáng lớn liên tục.
- **Lưu ý:** Trong ảnh bạn gửi trước đó, vòng xanh ngoài khá nhạt. Ưu tiên tăng độ rõ của phần cung trong thay vì tăng Bloom cả vòng.

### Divine Light — đạn sáng

- **Lớp 1:** Projectile lõi vàng kem hoặc trắng ngà, hình thoi/ngọn lửa nhỏ.
- **Lớp 2:** Mesh Trail cong nhẹ xanh ngọc-vàng, alpha 0.5–0.75.
- **Lớp 3:** 3–6 spark additive bay khỏi đuôi.
- **Lớp 4:** Flash va chạm vàng ngắn, kèm vòng lan bán trong suốt.
- **Thời lượng:** Phụ thuộc khoảng cách và tốc độ đạn; impact khoảng 0.2–0.35 giây.
- **Lưu ý:** Giữ lõi projectile đủ đặc để nhìn thấy trên tuyết; đừng biến cả viên đạn thành một chấm Bloom trắng.

### Divine Aegis — khiên thánh

- **Lớp 1:** Viền khiên hình cung hoặc mặt vòm màu xanh teal, alpha 0.55–0.75.
- **Lớp 2:** Bề mặt khiên xanh ngọc nhạt rất trong, alpha 0.12–0.25.
- **Lớp 3:** Đường rune vàng kem nhỏ ở cạnh khiên.
- **Lớp 4:** Flash sáng chạy quanh mép khi kích hoạt hoặc đỡ đòn.
- **Thời lượng:** Dựng khiên 0.3–0.5 giây; duy trì hình dạng trong thời gian buff; khi bị đánh lóe sáng 0.1–0.2 giây.
- **Lưu ý:** Phần trung tâm phải nhìn xuyên được, tránh che mặt và animation của nhân vật.

## 7. Chốt hướng nghệ thuật

**Mình vẫn chọn RO Soft Fantasy Darkened, không đổi toàn bộ sang Soft Adventure.**

Lý do mạnh nhất từ những ảnh nghiên cứu được là **vòng băng xanh đen của RO3 và vòng năng lượng tím-xanh của Midgard Senki vẫn đọc được trên nền đất/cát sáng**, nhờ hình dạng, màu đậm và mép sáng, chứ không cần toàn bộ môi trường tối.

Với game của bạn, mình sẽ thực hiện theo thứ tự:

1. Giữ Soft Fantasy, chỉ hạ sáng map tuyết/đá sáng khoảng 10–18%.
2. Làm lại shape layer cho Spirit Blessing và Heal: thêm vùng màu teal rõ, không thêm outline cartoon.
3. Giảm diện tích additive trắng và Bloom; giữ spark sáng.
4. Test đồng thời 4 skill Cleric trên tuyết, cỏ và đá sáng.
5. Test 40 người với chế độ giảm FX người ngoài party.

**Kết luận kỹ thuật:** Hướng bạn đang làm bằng Particle System + shader phát sáng là đúng. Việc còn thiếu không phải thêm nhiều Bloom, mà là phân cấp **hình dạng có màu → ánh sáng mềm → điểm sáng mạnh**. Đây là điểm đáng học nhất từ các ảnh gameplay đã kiểm chứng.

Các video để bạn đối chiếu thêm, nhưng mình chưa kiểm tra được trực tiếp từng frame:

- [RO3 — Gameplay tất cả class (2025)](https://www.youtube.com/watch?v=cy2EZSvLiWQ)
- [RO3 — Rune Knight và Royal Guard](https://www.youtube.com/watch?v=qzsP6kSx6Eo)
- [Midgard Senki — Gameplay Android/iOS](https://www.youtube.com/watch?v=9W7QTuTF-eI)
- [Midgard Senki — Gameplay CBT](https://www.youtube.com/watch?v=5S5ADu4SxVk)
