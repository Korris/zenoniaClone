# Cleric: 8 skill + đánh thường theo "RO3 Soft Fantasy V1" (21/11)

Nhánh `ro-fantasy-heroes`. Spec: `docs/reference/ro3-skill-style/ro3-acolyte-priest-skill-style-chatgpt.md` mục 3-6, quy tắc đầu `docs/sprite-rules.md`. Người dùng chốt: theo đúng RO3, kể cả màu pastel (thay màu đậm Midgard 3248c37f và màu vàng / xanh concept).

## Chung
- `ClericLightLook` viết lại: 1 bảng màu cho 1 chức năng (Heal #B8EBAA/#F5FFDA/#FFF5C8, Holy #F2D078/#FFF1BC/#FFFBE5, Protection #9DDAEC + vàng #FFF0B8, Sanctuary #E5F2C3/#F4DE9A/#FFF8DF, Guardian #FFF2C7 + #D7EAD6, Purify #B6E8D8/#E3FAED + tím #A58CBD, Ascension #F3D47D/#FFF9E1 + #DCEECF). Material tạo theo (mask, bảng màu, vai trò: Line / Fill / Glyph / Flash / Arc), cache. Mỗi nét có viền tối cùng tông (quy tắc 6, chống mất trên tuyết), phần cộng thấp, chỉ lõi Flash qua ngưỡng bloom.
- `ClericLightFx` (bộ phát timeline): thêm `Env` (fade in/out rõ cho lớp duy trì), `Move`, `Dissolve`, `Trail` (mesh trail theo lớp), `Kind` (Main / Glow / Detail). LOD 40 người `ClericLightFxLod.cs` (quy tắc 12): mình 100 %; party: hình chính 90 %, glow 50 %, hạt 50 %; người khác: hình 55 %, glow 15 %, bỏ lớp Detail, hạt 20 %, không trail. Mọi vòng đất cùng một tỉ lệ elip 0,7, bán kính ngang = tầm gameplay.
- Shader `LightBlob` thêm `_Dissolve` (noise thủ tục, mặc định 0 không đổi FX khác).
- 8 mask mới (`Tools/light_skill_textures_cleric_ro3.py`, gọi qua `build-light-skill-textures.py gen`): holy-hexagon, holy-star4, holy-diamond, holy-grid, holy-rune-ring, holy-cross-latin, holy-wings, holy-streak.
- Số hồi máu: skill có FX ánh sáng chỉ hiện số #8CDD74 (`HeroFx.HealNumber`), bỏ vòng / tàn lửa / loé màn hình cũ của `HeroFx.Heal`.
- Không hiện tên skill trên đầu khi cast (người dùng 21/11 bỏ ý đó của quy tắc 11).

## Từng skill (thời gian khớp `SkillTable`)
| Skill | Hình mới |
|---|---|
| Heal (650 ms, hồi ở 0 ms) | lõi sáng ngực (sau hero), vòng đất 0,4H→1,2H có cung sáng chạy, 4 tia ngắn đi lên sau người, 8 hạt; người cast phần nhỏ hơn (2 tia) |
| Spirit Blessing | người cast: vòng rune vàng mảnh 1,3H + sáng tay + 6 hạt, dòng sáng tới đồng đội; lúc nhận: sao 4 cánh vàng trên đầu, vòng 1,3H, 2 vệt sáng chạy lên thân, 6 hạt. Bỏ cột sáng, mũi tên, la bàn |
| Divine Aegis | người cast: lục giác xanh-vàng; lúc nhận: lục giác 1,4H, khiên trong suốt trước ngực (viền + mặt mờ), 2 cung xanh quanh người, loé ngà |
| Duy trì ATK / DEF (60 s) | GIỮ 2 vòng quanh thân nhưng còn 30 % độ sáng (quy tắc 10), nét mảnh hơn (1 đv ~3 px); DEF bị đánh: vòng + khiên loé 150 ms (thay vòm) |
| Divine Light (đánh ở 0 ms) | KHÔNG bay đạn (sẽ tới sau số sát thương): tia sáng từ trên rơi xuống tâm vùng đánh ngay lúc cast, sao 4 cánh 1,1H, vòng rune vàng đúng bán kính + sóng ra mép, sáng ở gậy, 5 hạt |
| Sanctuary (8 nhịp / 1000 ms, 8 s) | toàn bộ trên đất: vòng rune ngoài đúng bán kính có cung sáng, vòng rune trong xoay, nền ô vuông, quầng xanh ngà; mở 450 ms full, sau đó ~25 %, mỗi nhịp hồi loé 40 % + vòng nhỏ dưới chân từng người được hồi; tan 300 ms cuối |
| Guardian Spirit (khiên 8 s, 0 ms) | mỗi người nhận khiên: ấn la bàn xanh xám dưới chân, 2 cánh sáng sau vai (chỉ lúc đầu), quầng ngà trên đầu, vòng sáng nâng từ chân lên, loé ngà; người cast thêm sóng mảnh ra bán kính 4 ô. Duy trì: quầng nhỏ 25 %, loé khi bị đánh, cánh loé 1 lần nếu khiên vỡ sớm |
| Purification (đánh quanh mình ở 0 ms) | vòng rune trắng-ngọc 1,4H + sóng ra bán kính 3 ô, thánh giá mảnh trên đầu, đốm tím (trạng thái xấu) rời người rồi phân rã bằng Dissolve, loé cuối + hạt; quái trong tầm loé nhỏ |
| Divine Ascension (hồi 0 ms, đánh 100 ms) | 0-100 ms ấn dưới chân ~40 %; 100 ms bùng: sóng tới mép bán kính 5 ô, cột sáng 2,6H sau lưng, loé lõi, loé trên quái, rung / hit-stop cho mình; 150-550 ms 3 vòng rune mở lần lượt; 300-1100 ms cánh sáng; tan tới 1450 ms |
| Đánh thường | đạn hình thoi ngà 0,33H xoay theo hướng bay + trail vàng ngắn ~0,6H; trúng: sao 4 cánh + vòng nhỏ + 4 hạt; nhát 4: vòng đúng bán kính nổ, sao lớn, 8 hạt |

Sanctuary, Guardian Spirit, Purification, Divine Ascension không còn dùng sprite khi bật FX ánh sáng; choreo sprite cũ + frame giữ lại cho `--cleric-fx legacy` (chưa xoá vì chưa ai xem bản mới trong game).

## Kiểm tra
- Build từ bản sao: xem mục "Exe" bên dưới. Chạy thử `--record-choreo` 8 skill: xem mục "Smoke".
- Không đổi Rpg / Skills số liệu / Net dùng chung: không chạy dotnet test, không cần build lại server cho việc này.

## Câu hỏi còn mở
1. Đánh thường + Divine Light giờ màu Holy vàng ngà (theo RO3) trong khi bảng màu class cũ ghi Cleric = xanh lá, vàng là của Mechanic: giữ vàng ngà hay pha xanh?
2. Divine Light không có đạn bay (vì sát thương ở 0 ms). Muốn đạn bay đúng spec thì phải đổi `atMs` của Strike trong `SkillTable` (+ build lại server).
3. 2 vòng quanh thân giữ lại ở 30 % độ sáng: người dùng có muốn đổi hẳn sang biểu tượng nhỏ kiểu RO3 không?
4. Xoá choreo sprite + frame cũ của 4 skill (và `--cleric-fx legacy` cho chúng) sau khi người dùng duyệt bản mới?
