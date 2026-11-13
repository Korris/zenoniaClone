# Boss Truy Nã: skill và cảnh báo (thiết kế)

Ngày ghi nhận: 12/11/2026. Nguồn: ChatGPT (tài khoản Tay), chat https://chatgpt.com/c/6af58a18-74d0-83ec-9440-0f2715067fc1 (cùng chat vẽ concept + sprite boss). Người dùng: "bạn với nó cứ làm đi, không cần tui xác nhận". Các số dưới đây là số bắt đầu để chơi thử, chưa cân bằng.

Hiện trạng trước khi làm: mọi quái và boss chỉ có 1 đòn đánh thường (lấy đà 0,65 s, hồi 1,5 s), không skill, không cảnh báo mặt đất.

## Kết luận của ChatGPT

- NÊN cho boss truy nã dùng skill: chỉ đánh thường thì 5-20 người đứng yên bấm đánh, boss thành bao cát.
- Mỗi boss 3 skill thường + 1 skill cuồng nộ khi còn dưới 30 % máu. Mỗi boss cần: 1 đòn ép cận chiến rời boss, 1 đòn bắt người đánh xa di chuyển, 1 đòn diện rộng cả đội phải phản ứng. Không dùng skill liên tục: giữa hai skill phải có khoảng trống để gây sát thương và hồi máu.
- Bản đầu chỉ cần 2 skill mỗi boss, thêm dần.
- BẮT BUỘC có cảnh báo trước khi gây sát thương.

Quy ước: 1 ô = 1 tile; sát thương 1,0x = một đòn thường của boss đó.

## Rimeclaw Tyrant (Lv 10, rừng tuyết)

| Skill | Vùng | Cảnh báo | Sát thương, hiệu ứng | Hồi | Cách né |
|---|---|---|---|---|---|
| Frostclaw Sweep | Quạt 120° phía trước, bán kính 4 ô | 1,2 s | 1,5x, đẩy lùi 1 ô | 9 s | Ra sau boss hoặc ra ngoài quạt |
| Broken Chain Slam | Vòng tròn bán kính 2 ô tại chỗ một người chơi được chọn (khoá vị trí lúc bắt đầu cảnh báo) | 1,5 s | 1,2x, chậm 30 % trong 2 s | 13 s | Rời vòng |
| Frozen Ground Stomp | Vòng tròn quanh boss, bán kính 4 ô | 1,6 s | 1,8x, ĐÓNG BĂNG 3 s (đổi từ "chậm 40 % trong 2 s" ngày 12/11; 1,5 s lúc đầu, người dùng chê "hết nhanh" -> 3 s) | 17 s | Lùi khỏi boss |
| Tyrant's Blizzard (dưới 30 % máu) | Vòng tròn bán kính 6 ô, chừa 1 hành lang an toàn rộng 90° (đánh dấu xanh) | 2,2 s | 2,2x, chậm 40 % trong 3 s | 28 s | Vào vùng an toàn hoặc ra ngoài bán kính |

## Glacial Colossus (Lv 25, hang băng)

| Skill | Vùng | Cảnh báo | Sát thương, hiệu ứng | Hồi | Cách né |
|---|---|---|---|---|---|
| Crystal Fissure | Đường thẳng từ boss, dài 9 ô, rộng 2 ô | 1,4 s | 1,8x, chậm 30 % trong 2 s | 10 s | Đi vuông góc khỏi đường |
| Rune Impact | Quạt 100° phía trước, bán kính 4 ô | 1,3 s | 2,0x, đẩy lùi 2 ô | 12 s | Ra hai bên hoặc ra sau |
| Crystal Fall | 3 vòng tròn bán kính 1,5 ô tại 3 người chơi khác nhau (khoá lúc bắt đầu cảnh báo) | 1,8 s | 1,4x mỗi vùng, ĐÓNG BĂNG 2 s (đổi từ "chậm 30 % trong 2 s" ngày 12/11) | 18 s | Rời vòng, đừng chạy về phía đồng đội |
| Core Overload (dưới 30 % máu) | Vòng tròn bán kính 7 ô quanh boss, 2 hành lang an toàn đối nhau, mỗi cái 60° | 2,4 s | 2,5x, đẩy lùi 2 ô | 30 s | Vào hành lang an toàn hoặc ra ngoài |

Crystal Fissure và Rune Impact dùng chung động tác giơ tay đập xuống.

## Mire Hydra Queen (Lv 40, phế tích đầm lầy)

| Skill | Vùng | Cảnh báo | Sát thương, hiệu ứng | Hồi | Cách né |
|---|---|---|---|---|---|
| Triple Venom Breath | Quạt 120° phía trước, bán kính 6 ô | 1,6 s | 1,2x, độc 0,15x mỗi giây trong 4 s | 11 s | Sang hai bên hoặc ra sau |
| Poison Bloom | 3 vòng tròn tại chỗ người chơi (số liệu vòng: xem chat), để lại vũng độc 3 s | 1,8-2,0 s | 0,8x, độc 0,15x mỗi giây trong 4 s | 15 s | Rời vòng, đừng kéo vùng độc vào đội |
| Serpent Tail Crush | Vòng tròn quanh boss, bán kính 4 ô | 1,5 s | 1,7x, đẩy lùi 2 ô | 16 s | Rời phạm vi đuôi |
| Queen's Toxic Reign (dưới 30 % máu) | 5 vòng tròn bán kính 2 ô quanh khu giao tranh | 2,2 s | 1,5x, độc 0,2x mỗi giây trong 5 s | 30 s | Tìm khoảng trống giữa các vòng |

## Luật đóng băng (Frozen Ground Stomp, 12/11/2026)

Dùng `StatusKind.Freeze` riêng cho hero (KHÔNG qua `MonsterStatus`, vì cần đồng bộ mạng giống hệt dash/invuln): `HeroController.frozenMs`.

- Khi đang đóng băng: không đi, không đánh thường, không skill, không dash; anim giữ nguyên frame hiện tại (animTimer/idleTimer không tăng). Vẫn nhận sát thương, vẫn hồi máu tự nhiên, vẫn độc/chậm nếu có (tách khỏi `MonsterStatus`).
- Không cộng dồn: trúng lại lấy `max(còn lại, mới)`.
- Không bị đẩy lùi trong lúc đóng băng (hiệu ứng khác có knockback bị bỏ qua khi đang đóng băng).
- Dash/bất tử lúc trúng đòn vẫn né được (giống mọi hiệu ứng khác) -- không né invuln riêng cho phần client tự đoán (xem dưới).
- KHÔNG chặn uống bình máu/mana (phím đó nằm ngoài vòng lặp sim bị khựng).
- Mạng: cờ `HeroFlags.Frozen` trong `FacingFlags` (mọi client thấy mọi hero đóng băng), `OwnerSim.FreezeLeft` (hero của mình, phục vụ đối chiếu/dự đoán lại). Client tự dự đoán (`HeroController.PredictFreeze`) tự đóng băng ngay lúc hiệu ứng va chạm bắn ra (cùng mốc `WantedSkillTelegraphFx` vẽ hiệu ứng), không chờ gói tin server -- nên không trượt rồi bị kéo lại. Không chặn theo `invuln` phía dự đoán (giá trị đó có thể cũ do một đòn thường gần đó), chỉ chặn dead/God/đang dash.
- Hình: `Sprites/fx/wanted/frozen-block.png` (cục băng phủ quanh hero, lòng rỗng) + tint xanh nhẹ lên thân hero (luôn có, kể cả khi chưa có hình). Vẽ ngay dưới thân hero (orderBase + 2, thân ở orderBase + 3).

## Cảnh báo

| Hình thức | Mức | Cách làm |
|---|---|---|
| Vùng nguy hiểm trên mặt đất | Bắt buộc | Đỏ / cam bán trong suốt, viền sáng, hiện trước khi đánh |
| Boss đổi tư thế | Bắt buộc | Giơ tay, cúi người, mở miệng, lõi rune sáng |
| Thanh niệm chú | Nên có | Thanh nhỏ ngay dưới thanh máu boss, chạy từ đầy về 0 |
| Âm thanh | Nên có | Một tiếng trầm lúc bắt đầu cảnh báo |
| Dấu chấm than trên đầu NHÂN VẬT CỦA MÌNH khi đang đứng trong vùng | Nên có | Lớp UI, không bị hiệu ứng che; chỉ hiện cho mình, không hiện cho 20 người |

Thời gian cảnh báo: dưới 1,2 s là quá gấp cho skill phải né chủ động (joystick, trễ mạng 50-150 ms). Quạt nhỏ 1,2-1,4 s; đường thẳng / vòng quanh boss 1,4-1,7 s; vòng nhắm người chơi 1,5-1,9 s; độc nhiều mục tiêu 1,8-2,0 s; cuồng nộ 2,2-2,5 s.

Đồng bộ: server chọn skill, hướng, vị trí và MỐC THỜI GIAN đánh theo đồng hồ server, khoá vùng ngay; client vẽ cảnh báo theo thời gian còn lại tới mốc đó (không tự đếm từ lúc nhận tin). Sát thương đúng hình đã cảnh báo; nới vùng an toàn 0,2-0,3 ô ở rìa; cảnh báo không được biến mất trước khi đòn xảy ra. Không trì hoãn sát thương theo ping từng người.

## Phần lập trình tối thiểu

- 3 hàm hình học: vòng tròn, hình quạt, đường thẳng ("nhiều vòng tròn" = nhiều vòng tròn cùng lúc). Cuồng nộ = vòng tròn trừ các quạt an toàn, hoặc nhiều vòng tròn.
- Máy trạng thái boss: NORMAL -> TELEGRAPH -> EXECUTE -> NORMAL; dưới 30 % máu bật cờ ENRAGED, không viết AI thứ hai.
- Dữ liệu một skill: id, kiểu hình, tầm, bán kính, góc, bề rộng, số mục tiêu, thời gian cảnh báo, hồi chiêu, hệ số sát thương, hiệu ứng + thời lượng, id động tác, id hiệu ứng hình.
- Vùng đánh tính từ một tâm gameplay cố định (tâm vòng chân boss), trùng tâm vùng cảnh báo, không nhất thiết trùng tâm sprite.
- Hoãn: boss đuổi người chơi, skill bắn đạn bay, bù trễ nâng cao.
- Thứ tự: (1) vòng tròn + quạt, cảnh báo, đồng bộ thời gian, trúng đòn, một boss chạy được với hình đơn giản; ... (5) cân bằng, âm thanh, thanh niệm chú, cuồng nộ.

## Hình cần ChatGPT vẽ thêm

Dùng chung (một tấm trên nền `#FF00FF`): vòng cảnh báo tròn, vùng quạt, dải đường thẳng, vùng an toàn viền xanh ngọc, dấu chấm than đỏ viền trắng, thanh niệm chú (navy viền vàng), biểu tượng cuồng nộ, biểu tượng Slow và Poison. Riêng từng boss: động tác ra chiêu và hiệu ứng va chạm (xem chat).
