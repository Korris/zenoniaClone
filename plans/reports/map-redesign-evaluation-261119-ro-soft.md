# Có nên vẽ lại toàn bộ map theo RO Soft Fantasy không (19/11/2026)

Chỉ đánh giá, chưa sửa gì trong `Assets/`. Đã đọc `docs/map-design-rules.md` và mục Map trong `CLAUDE.md`.

## Khuyến nghị

**Chưa vẽ lại toàn bộ. Chỉnh màu hàng loạt bằng một bộ chỉnh chung (LUT) cho ground + props trước. Sau này chỉ vẽ lại 1-2 map trưng bày, khi bộ sprite nhân vật mới đã vào game.**

Lý do:
1. Lệch chủ yếu nằm ở màu, không nằm ở bố cục. Khi ChatGPT vẽ lại cảnh 40 người theo RO (`6-40-players-...-ro-soft-fantasy.png`), nó giữ nguyên bố cục Sunken Reliquary: sân tròn, cột đổ, nước, lá sen. Nó chỉ làm sáng lên, bớt gắt và làm nước trong hơn. Mockup `40-players-new-classes-...png` còn giữ map khá tối, xanh rêu.
2. Chỉnh màu không đổi hình học. Ô chặn, cầu thang, cầu, đường đi và toạ độ save vẫn đúng 100 %, nên không vướng điều 1, 6, 7 của map-design-rules.
3. Vẽ lại toàn bộ tốn khoảng 130-160 giờ và 100-150 ảnh ChatGPT. Nó tranh hạn mức với việc vẽ 5 class x 8 hướng, là việc đang ưu tiên.

## 1. Map đang có

14 map trong `MapRegistry`. Ảnh nền vẽ painted, 64 px mỗi ô. Prop là sprite riêng, xếp theo y. Ô chặn là `rows` trong file `.cs` cộng file override.

| Map | Cỡ (ô) | Ground | Prop đặt (loại) | Ghi chú |
|---|---|---|---|---|
| Frost Hollow | 72x48 | 4 mảnh 2304x1536 + minimap | 231 (`fh*` 22) | tuyết, đã sáng |
| Willowbrook Meadows | 72x48 | 4 mảnh 2304x1536 + minimap | 208 (`wb*` 46 + `wbx*` 21) | cỏ xanh chói |
| Sunbreak Foothills | 48x32 | 4 mảnh 1536x1024 + minimap | 67 (`sb*` 20) | |
| Scorpion Gorge | 48x32 | 4 mảnh 1536x1024 + minimap | 26 (`sg*` 20) | đỏ cam |
| Bramblewood Forest | 36x24 | 1 ảnh 2304x1536 | 78 (`bw*` 23) | rìa rừng tối |
| Amber Dunes / Sunspire Oasis | 36x24 | 1 ảnh mỗi map | 22 / 24 | sa mạc sáng |
| Emberfen Marsh / Sunken Reliquary / Frostfang Den | 36x24 | 1 ảnh mỗi map | 26 / 19 / 17 | tối nhất, Sunken là sân boss Wanted |
| Oakhaven / Thatchwick (làng) | 36x24 | 1 ảnh vẽ liền cả nhà và cây | 0 | làng chính |
| Rootburrow / Thornheart Chamber | 36x24 / 24x16 | 1 ảnh, không prop | 0 | hang, cố ý tối |

Tổng cộng: 26 ảnh ground, 4 minimap, 201 ảnh prop trong `Sprites/village/props`, khoảng 80 sheet quái. Render pipeline là Built-in 2D (không có URP), nên không có sẵn color grading chạy trong game. Muốn dùng LUT thì phải chỉnh sẵn vào ảnh (bake offline) hoặc viết shader riêng.

## 2. Đo tông màu

Đo bằng Python. L là độ sáng trung bình, S là độ bão hoà HSV, contrast là độ lệch chuẩn của L, Dark là tỉ lệ điểm ảnh có L < 0,25.

| Ảnh | L | S | contrast | R-B | Dark |
|---|---|---|---|---|---|
| In-game 40 người (Sunken) | 0,42 | 0,53 | 0,21 | +0,17 | 25 % |
| ChatGPT vẽ lại cảnh đó theo RO | 0,54 | 0,35 | 0,21 | +0,06 | 6 % |
| Mockup class mới vs boss | 0,49 | 0,39 | 0,21 | +0,03 | 13 % |
| Lineup 5 class | 0,77 | 0,21 | 0,23 | +0,07 | 4 % |
| Ground tối: Emberfen / Bramblewood / Sunken / Frostfang | 0,39-0,47 | 0,46-0,67 | | | 22-30 % |
| Ground sáng: Willowbrook / Sunbreak / Sunspire | 0,51-0,67 | 0,49-0,65 | | | 2-5 % |
| Frost Hollow | 0,70 | 0,16 | | | 4-7 % |

Kết luận khi đo:
- RO sáng hơn khoảng +0,12 L, **bớt bão hoà nhiều** (0,53 → 0,35) và trung tính hơn (R-B +0,17 → +0,06). Vùng tối giảm từ 25 % còn 6 %. Contrast gần như giữ nguyên.
- Vậy "ấm hơn" không đúng với map. Nắng thì kem, nhưng bóng ngả tím nhạt, còn nước là cyan trong. Map hiện tại nghiêng vàng-cam-xanh lá chói.
- Không phải map nào cũng "Zenonia 5 tối". Frost Hollow đã sáng. Willowbrook và Sunbreak sáng nhưng quá chói. Lệch tông nặng nhất là Sunken Reliquary, Emberfen, Bramblewood và Frostfang. Hai hang Rootburrow và Thornheart nên giữ tối vừa phải.

Nhận xét bằng mắt: nét map hiện tại dày chi tiết, viền tối đậm, lá cây có hoa văn dày. RO dùng mảng màu to hơn, viền mềm hơn, đá xám lavender, nhiều hoa pastel. Nhân vật pastel mới (lineup v1) đặt lên Sunken, Emberfen hay Bramblewood hiện tại sẽ lệch rõ: nhân vật sáng như sticker trên nền rêu tối. Đặt lên Frost Hollow hay Sunspire thì lệch ít.

## 3. Thử phương án rẻ: chỉnh màu

Script `Tools/preview-map-ro-soft-tone-grade.py` chỉ đọc ảnh, ghi kết quả vào `docs/reference/map-ro-soft-tone-test/`. Các bước:
- bớt bão hoà x0,9 (vàng-lá và cam đất x0,8, nước x1,08);
- dịch lá về màu bạc hà, nước teal về cyan;
- gamma 0,8 + sàn đen 0,035;
- bóng ngả lavender, vùng sáng ngả kem.

Đây là bản v2. Bản v1 bớt bão hoà quá tay (S 0,26) nên trông như phủ sương, đã bỏ.

Kết quả bản v2 (L / S, trước → sau):
- In-game Sunken: 0,42 / 0,53 → 0,52 / 0,36. Gần như trùng số đo bản RO (0,54 / 0,35).
- Bramblewood: 0,40 / 0,56 → 0,50 / 0,36.
- Willowbrook: 0,51 / 0,59 → 0,60 / 0,39.
- Emberfen: 0,39 / 0,67 → 0,49 / 0,42.

Nhìn bằng mắt: dịu và thoáng hơn rõ, nước trong hơn, rìa rừng bớt nặng. Nhưng **chỉnh màu chỉ được khoảng 60-70 % chặng đường**. Nét và độ dày chi tiết vẫn là kiểu painted cũ: viền đen, hoa văn lá vẫn còn, và hơi xám nếu đẩy quá tay. Chưa ghép thử nhân vật mới lên nền, vì chưa có sprite tách nền.

Có xuất thêm `ro-soft-grade-v2.cube` (LUT 33³) từ đúng hàm chỉnh đó, để chỉnh hàng loạt hoặc chỉnh tiếp trong Photoshop / Resolve.

## 4. Các phương án

| Phương án | Ảnh ChatGPT | Giờ | Rủi ro |
|---|---|---|---|
| A. Giữ nguyên | 0 | 0 | lệch tông với nhân vật mới, rõ nhất ở 4 map tối |
| **B. Chỉnh màu hàng loạt** (ground + 201 prop + minimap, cùng LUT, tinh chỉnh riêng từng biome) | 0 | 6-10 | gần như không. Rows / cầu thang không đổi. Phải giữ ảnh gốc trong `ArtLibrary` và chỉnh từ bản gốc (không chỉnh chồng). Prop và ground phải cùng LUT để không lệch màu nhau. Chỉ đổi ảnh nên không phải build lại server |
| C. B + vẽ lại 1-2 map trưng bày (Oakhaven = làng chính, Sunken Reliquary = sân boss Wanted) | 15-30 | 25-40 | ChatGPT hay vẽ lại vách và bậc. Phải dựng lại rows, override, rà từng cầu thang (điều 7), flood fill (điều 6), chụp trong game. Oakhaven là ảnh liền nên ít prop phải cắt |
| D. Vẽ lại toàn bộ 14 map | 100-150 (kể cả vẽ lại vì hỏng): khoảng 3 ảnh mỗi map nhỏ, 10-12 ảnh mỗi map lớn, khoảng 25 tấm prop | 130-160 | lớn: Frost Hollow / Willowbrook trước đây mất nhiều ngày mỗi map. Đổi cỡ hay vách thì lệch save (`SaveVersion`) và phải build lại server khi đổi rows. Tranh hạn mức ChatGPT với 5 class x 8 hướng |

## 5. Thứ tự nếu làm

1. Chốt nhân vật trước: Warrior T0 bản lineup v1, có 8 hướng. Màu map nên theo nhân vật, không đi trước nhân vật.
2. Ghép thử Warrior mới lên 3 nền: Sunken gốc, Sunken chỉnh v2, Frost Hollow. Nhìn ở cỡ chơi thật (camera 192).
3. Nếu bản chỉnh đẹp hơn: viết bước bake. Chỉnh từ ảnh gốc trong `ArtLibrary`, ghi đè ground, prop và minimap của từng map, có tham số riêng cho mỗi biome (tuyết ít nâng sáng, hang giữ tối hơn). Build từ bản sao và chụp mọi map.
4. Quái: thử nhẹ cùng LUT (quái tông Zenonia 5 cũng lệch), hoặc để cho đợt vẽ lại boss Wanted.
5. Chỉ khi xong nhân vật mới xét phương án C (vẽ lại Oakhaven, Sunken). Phương án D để sau phát hành, nếu còn muốn.

## File

- Báo cáo: `plans/reports/map-redesign-evaluation-261119-ro-soft.md`
- Ảnh thử: `docs/reference/map-ro-soft-tone-test/*-before-after-ro-soft-grade-v2.jpg` (sunken-reliquary, bramblewood-forest, willowbrook-large-r0c0, emberfen-marsh, ingame-wanted-mire-hydra) và `ro-soft-grade-v2.cube`
- Script: `Tools/preview-map-ro-soft-tone-grade.py` (mới, chưa commit, không ghi vào `Assets/`)

## Câu hỏi chưa giải quyết

1. Người dùng muốn map sáng "hẳn RO" như lineup (L khoảng 0,77, nền gần trắng) hay chỉ mức như bản ChatGPT vẽ lại cảnh boss (L khoảng 0,54)? Hai mức này khác nhau nhiều.
2. Hang Rootburrow và Thornheart có giữ tối để tạo không khí không?
3. Quái có chỉnh màu cùng map không, hay chờ vẽ lại?
4. Có muốn xin ChatGPT vẽ thử 1 map (Sunken ground-only theo RO) để so với bản chỉnh màu không? Việc này chỉ nên làm khi tài khoản rảnh, sau khi vẽ xong nhân vật.
