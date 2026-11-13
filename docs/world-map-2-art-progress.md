# World Map 2.0: tiến độ hình (map + quái)

Theo `docs/world-map-2-design.md` và quy chuẩn `docs/world-art-bible.md`. Hình do ChatGPT vẽ, lưu vào repo, CHƯA cài vào game (chưa chạy tool cắt, chưa có MonsterDef / map class, chưa ghép map). Cập nhật mỗi khi vẽ thêm.

- Chat chính: https://chatgpt.com/c/6af5faea-f11c-83ec-9e09-3c13537b61a2 ("Đánh giá phong cách làng"), tài khoản **Tay** (Plus). ChatGPT đã có bối cảnh: làng, Oakhaven mới, World Map 2.0, Art Bible, các quái bên dưới, Willowbrook vẽ lại.
- **Quy chuẩn art** (13/11, người dùng yêu cầu đồng nhất style + tông màu): `docs/world-art-bible.md` (nguyên văn ChatGPT). Mọi prompt vẽ map / nhà / prop / quái phải dán đoạn "WORLD ART STYLE LOCK" (cuối file) + gửi kèm 1–2 ảnh chuẩn (`docs/reference/new-oakhaven-ingame-overview.png`, sheet quái cùng vùng).
- **Cỡ map**: người dùng đổi (13/11) map farm + dungeon thành 72x48 (như Willowbrook), làng mới 36x24. ChatGPT được hỏi lại về cỡ map cho game online: `docs/reference/world2-map-size-chatgpt-answer.txt` (khuyên farm 48x32 + channel; farm đông người 72x48). **Chờ người dùng chốt** → tạm chưa vẽ nền map farm mới.
- ChatGPT rà style 9 map cũ + quái mới: `docs/reference/world2-art-review-chatgpt.md`.
- Còn lại (lượt sau): attack Crystal Golem King; vẽ lại hàng LÊN attack Snow Harpy; prop cho Frost Hollow / Amber Dunes / Emberfen / Sunken Reliquary vẽ lại (nếu prop cũ lệch style); nhà Sunspire theo quy tắc nhà Art Bible; map mới khi chốt cỡ (48x32 hay 72x48); vùng 3–6, 3 làng mới.

## Map cũ lệch chuẩn (ChatGPT rà theo Art Bible, 13/11) và vẽ lại

Người dùng (13/11): map cũ lệch style/tông → vẽ lại theo Art Bible, GIỮ NGUYÊN bố cục (ô, đường, cổng, cầu, vách, sông, bãi quái) để đưa lại vào game dễ. Bản mới ở `Assets/ArtLibrary/maps/<map-id>/redraw/` (không ghi đè Resources); ảnh so sánh cũ (trái) / mới (phải): `docs/reference/world2-redraw-<map-id>-compare.png`. Đưa vào game: agent khác (ghép mảnh 72x48: khớp màu + feather mép như `Tools/build-*-large-painted.py`, phóng x1.5 lên 2304x1536/mảnh; map 36x24: phóng 1536x1024 → 2304x1536). Mọi bản dưới đều đã so bằng ảnh trộn 50 % cũ/mới.

| Map | Đánh giá cũ | Lệch gì | Bản vẽ lại | Ghi chú khi đưa vào game |
|---|---|---|---|---|
| Willowbrook Meadows (72x48) | lệch nặng | cỏ vàng úa, như bản đồ zoom xa, thiếu khối | **xong** 4 mảnh: `willowbrook-large-r0c0-redraw-chatgpt-1`, `-r0c1-...-1`, `-r1c0-...-3`, `-r1c1-...-1` (+ tấm prop 21 món `willowbrook-large-prop-sheet-redraw-chatgpt-1.png`, cùng thứ tự/vị trí tấm prop cũ `hd/map/willowbrook-meadows/large/prop-sheet-chatgpt.png`) | mảnh r{r}c{c} = mảnh `willowbrook-large-ground-r{r}c{c}` trong Resources. Cầu gỗ giữa map (đáy r0c0) KHÔNG còn trong nền → cần prop cầu. Ao r1c0/r1c1 lệch nhẹ vài ô; mép nối giữa mảnh lệch màu nhẹ (r0c1 đậm hơn) → khớp màu khi ghép. Bản sai giữ `-old` (r1c0 v1 chép bố cục mảnh kề; v2 ao cao) |
| Amber Dunes | lệch nặng | cam vàng đồng màu, nhiễu | **xong** `amber-dunes/redraw/amber-dunes-redraw-chatgpt-2.png` | bố cục khớp; v1 thiếu lối ra mép phải dưới (`-missing-exit-old`) |
| Frost Hollow (72x48) | lệch nhẹ | trắng xanh gắt, vách kiểu tile | **xong** 4 mảnh `frost-hollow/redraw/frost-hollow-large-r{r}c{c}-redraw-chatgpt-1.png` | khớp từng mép vách, bậc, cầu, hang; vách vẫn hơi răng cưa (bố cục cũ vậy). Bậc đá phải rà lại ô chặn như luật map 7 |
| Sunspire Oasis | lệch nhẹ | vàng cam quá bão hoà, kiểu tile | **xong** `sunspire-oasis/redraw/sunspire-oasis-redraw-chatgpt-1.png` | khớp bố cục; nền nhà để trống; ít bụi cỏ hơn (prop bụi cũ vẫn dùng được) |
| Emberfen Marsh | lệch nhẹ | xanh đen quá tối | **xong** `emberfen-marsh/redraw/emberfen-marsh-redraw-chatgpt-1.png` | sáng hơn, 2 cầu + 2 dải nước đúng chỗ; đường dưới-phải ra mép phải (cũ có thể khác) → kiểm cổng |
| Sunken Reliquary | lệch nhẹ | đá phẳng, vùng tối | **xong** `sunken-reliquary/redraw/sunken-reliquary-redraw-chatgpt-1.png` | khớp rất sát; phòng boss có hoa văn tròn |
| Oakhaven cũ | lệch nặng | | không (đã có Oakhaven mới) | |
| Thatchwick | đạt | hơi nhạt | không | có thể tăng tương phản 10–15 % bằng code |
| Frostfang Den | đạt | | không | |

## Map mới

| Map | Cỡ | Trạng thái | File | Ghi chú |
|---|---|---|---|---|
| Bramblewood Forest | 36x24 (chốt 2.1) | **nền + tấm prop xong (13/11 sáng)**, đã xem từng ảnh | `Assets/ArtLibrary/maps/bramblewood-forest/`: `bramblewood-forest-ground-only-chatgpt-1.png` (nền), `bramblewood-forest-prop-sheet-chatgpt-1.png` (20 món, lưới 5x4, nền `#FF00FF`), concept có cây `bramblewood-forest-chatgpt-1.png` (dùng làm sơ đồ đặt prop) | Lối ra: trên giữa (Willowbrook), phải giữa, trái: miệng hang rễ (Rootburrow). Nền giữ đúng bố cục concept: đường, suối, thác + vách thác, cầu gỗ, miệng hang VÀ vành rừng dày quanh viền (vẽ liền, vùng không đi tới); cây lẻ / bụi / đá bên trong đã bỏ để đặt prop. Suối mất bờ đá (dùng prop đá ven suối). Cây trong tấm prop cao ~280 px, to hơn cây vành rừng trong nền (~200 px): thu ~0,7 khi cắt |
| Rootburrow (dungeon) | 36x24 | **xong**, đã xem | `Assets/ArtLibrary/maps/rootburrow/rootburrow-chatgpt-1.png` | Vẽ liền nền + vách rễ cây. Lối vào khe sáng giữa mép phải; cổng boss vòm rễ xanh ngọc góc trên-trái; vũng nước mép trái; giữa map có 1 đảo vách (đường vòng quanh), các buồng thông nhau rộng hơn mô tả (không phải 3 buồng tách bạch). ~4 hàng dưới là vách |
| Thornheart Chamber (phòng boss) | 24x16 (thiết kế ghi 24x18) | **xong**, đã xem | `Assets/ArtLibrary/maps/rootburrow/thornheart-chamber-chatgpt-1.png` | Vẽ 1536x1024 = 24x16 ô, 64 px/ô (đúng mật độ game, không cần phóng). Sân bầu dục trống ~19x8 ô, hoa văn rễ giữa sân, trái tim gỗ ở vách trên. Vòm cửa vào nằm trong 3 hàng dưới (HUD che): đặt cổng ở miệng hành lang phía trên vòm |
| Sunbreak Foothills | 48x32 | **bản tổng thể + tấm prop + 4 góc chi tiết xong**, đã xem từng ảnh và ảnh ghép thử | `Assets/ArtLibrary/maps/sunbreak-foothills/`: `sunbreak-foothills-overview-ground-chatgpt-1.png`, `sunbreak-foothills-prop-sheet-chatgpt-1.png` (20 món, 5x4), 4 góc `sunbreak-foothills-r0c0-chatgpt-1.png`, `-r0c1-chatgpt-1.png`, `-r1c0-chatgpt-2.png`, `-r1c1-chatgpt-1.png` (mỗi góc 1536x1024 = 24x16 ô, 64 px/ô, không cần phóng); ảnh gửi đi ở `ref/`; ghép thô 4 góc (chưa khớp mép): `docs/reference/world2-map-sunbreak-foothills-4-quadrants-raw.png` | 4 góc giữ đúng bố cục bản tổng thể. Mép nối lệch nhẹ, cần min-cut / vá khi ghép: suối ngay dưới cầu trái lệch ~40 px giữa r0c0 và r1c0; miệng hang + bậc thang trên giữa lệch ~20 px giữa r0c0 và r0c1; mép dọc r1c0 / r1c1 ở đường xuống cổng nam. r1c0 bản 1 vẽ thêm 1 cây cầu ở mép trên (ghép ra 2 cầu) → bản 2 đã bỏ, bản 1 giữ tên `-double-bridge-old`. Bản tổng thể 32 px/ô chỉ dùng làm sơ đồ. Bố cục: lối ra dưới giữa (Frost Hollow, tuyết góc dưới-trái), phải giữa (Sunspire, sa thạch), hang băng + bậc thang trên giữa (Frostfang Den); suối chéo trên-trái → dưới-phải, 2 cầu đá vẽ CHÉO; sân boss nâng cao góc trên-phải có bậc thang. Lệch: có thêm 1 đường ra mép TRÁI giữa (không xin) → làm ngõ cụt hoặc chặn; nhiều gờ đá thấp viền quanh bãi cỏ → rà ô chặn kỹ |
| Scorpion Gorge (vùng 3, Lv26–30) | 48x32 | **bản tổng thể + tấm prop xong** (13/11 10:30), đã xem; CHƯA vẽ 4 góc chi tiết | `Assets/ArtLibrary/maps/scorpion-gorge/`: `scorpion-gorge-overview-ground-chatgpt-1.png`, `scorpion-gorge-prop-sheet-chatgpt-1.png` (20 món, 5x4), 4 ảnh gửi đi đã cắt sẵn ở `ref/overview-quadrant-r{r}c{c}-x2.png` | Hẻm núi đá đỏ. Lối ra: trái giữa (Amber Dunes, cát vàng), phải giữa (Mirewatch, cỏ úa + vũng bùn), cổng đền sa thạch + bậc thang trên giữa (Sunspire Vault). Lòng sông cạn trên-trái → dưới-giữa, 2 cầu đá vẽ THẲNG. Lòng chảo cát xoáy góc dưới-phải = sân boss Dune Devourer, vào bằng dốc. Màu đỏ cam khá đậm: xem lại khi vào game. Việc còn: 4 góc chi tiết (prompt như Sunbreak, mục "Việc 17–20" trong chat) |
| Sunspire Vault, Sandglass Djinn Chamber, vùng 4–6, 3 làng mới | | chưa | | |

**Tài khoản Tay hết lượt vẽ lúc 10:45 ngày 13/11, có lại 17:32.** Hàng đợi vùng 3 dừng ở Sand Wraith attack. Còn thiếu của Scorpion Gorge: 4 góc chi tiết, Sand Wraith attack, Rock Basilisk, Dune Golem, boss Dune Devourer (walk + attack mỗi con; mô tả từng con ở tin "Việc 28–35" đã soạn, xem bảng quái bên dưới).

## Đã vào game (13/11, 5 agent, chưa commit)

| Hạng mục | Trạng thái | Báo cáo |
|---|---|---|
| Amber Dunes, Emberfen, Sunspire, Sunken Reliquary vẽ lại | nền mới đã thay, 2 ô chặn Emberfen đổi, đã chụp trong game | `plans/reports/redraw-install-261113-four-small-maps.md` |
| Willowbrook, Frost Hollow vẽ lại | nền ghép 4 góc, cầu gỗ Willowbrook thành prop phẳng `wbBridge`, prop `wbx*` cắt lại, 57 ô nước đổi; Frost Hollow rà đủ 7 bậc thang, rows không đổi | `plans/reports/redraw-install-261113-willowbrook-frost-hollow.md` |
| 9 quái mới (forestWolf, thornSprite, thornfangAlpha, thornheartTreant, highlandIbex, dustscaleLizard, cliffHarpy, stonebackGuardian, ridgehornBehemoth) + đổi cấp vùng 1 | MonsterDef + LootTable + sprite đã cắt, bóng vẽ sẵn đã xoá (game tự vẽ bóng) | `plans/reports/monsters-install-261113-world2-nine-monsters.md` |
| Bramblewood Forest, Rootburrow, Thornheart Chamber | map + prop + ô chặn + cổng + quái + boss, nối Willowbrook (46,44) ↔ Bramblewood ↔ Rootburrow ↔ phòng boss | `plans/reports/new-maps-261113-bramblewood-rootburrow.md` |
| Sunbreak Foothills + đổi tuyến Frost Hollow → Sunbreak → Frostfang Den / Sunspire | map 48x32 ghép 4 góc (2x2 mảnh 1536x1024 + minimap), 20 prop `sb*`, rows + override, 7 cổng, 50 quái / 8 bãi + boss Ridgehorn trên sân cao; hang băng Frost Hollow → đường nam Sunbreak, hang trên ↔ Frostfang Den (cả 2 cửa Den về chân bậc Sunbreak), mép đông ↔ cổng tây Sunspire; đã chụp trong game cả 7 lượt qua cổng. Chưa thử online / server | `plans/reports/new-maps-261113-sunbreak-foothills.md` |

## Quái mới

Định dạng nguồn như `Tools/build-meadow-monsters.py`: 2 tấm `<id>-walk-chatgpt.png`, `<id>-attack-chatgpt.png`, lưới 4x4 (hàng xuống, lên, trái, phải), ChatGPT trả 1254x1254, nền `#FF00FF`. Bản xem: `docs/reference/world2-monsters-{bramblewood,rootburrow,snowcrest}.png`. Đã xem từng tấm; thứ tự hàng đúng ở mọi tấm (trái quay trái, không cần lật). ChatGPT rà theo Art Bible: tất cả đạt, riêng Snow Harpy lệch nhẹ (cánh trắng sáng, có thể chìm trên tuyết).

| Quái | Thư mục | Walk | Attack | Ghi chú |
|---|---|---|---|---|
| Forest Wolf (Lv7) | `monsters/bramblewood/` | có | có | sói xám nâu, rêu + gai mâm xôi |
| Thorn Sprite (Lv9) | `monsters/bramblewood/` | có | có | bóng dưới chân màu tím sẫm + quầng hồng quanh hạt sáng: ~22 % pixel bán trong suốt khi key magenta → xem kỹ sau khi chạy tool |
| Thornfang Alpha (boss map Lv10) | `monsters/bramblewood/` | có (bản 1 được chọn; bản 2 ít gai: `thornfang-alpha-walk-chatgpt-alt.png`) | có | frame sát nhau / sát mép ô, kiểm khi cắt |
| Thornheart Treant (boss Rootburrow Lv10) | `monsters/rootburrow/` | có | có | frame sát mép ô |
| Snow Harpy (Lv18) | `monsters/snowcrest/` | có | có (bản sửa 13/11 10:25) | attack đã vẽ lại: hàng LÊN lông băng bay lên trên, đúng hướng cả 4 hàng; bản sai giữ tên `snow-harpy-attack-chatgpt-up-row-wrong-old.png`. Bóng tím dưới chân; cánh trắng có thể chìm trên tuyết. Chưa có map nào dùng (2.1: giữ cho Frost Hollow / Frostfang Den) |
| Crystal Golem King (elite / boss sự kiện Lv20) | `monsters/snowcrest/` | có | có (nện tay pha lê, 13/11 10:20) | đúng hướng 4 hàng; pha lê xanh tím trên nền magenta: kiểm độ đục sau key; frame sát mép ô. Chưa có map nào dùng |
| Highland Ibex (Lv16) | `monsters/sunbreak/` | có | có (húc sừng) | 13/11 sáng. Dê núi nâu xám, tuyết trên lưng. 4 hàng đúng hướng, hiệu ứng hàng LÊN nằm phía trên |
| Dustscale Lizard (Lv17) | `monsters/sunbreak/` | có | có (lao cắn) | thân thấp gai sa thạch cam, trông lông lá hơn là vảy; đúng hướng |
| Cliff Harpy (Lv18) | `monsters/sunbreak/` | có | có (bắn 3 lông vũ) | quái bay, bóng tròn TÍM SẪM tách rời dưới chân (gần màu key → xem kỹ khi key magenta, có thể phải vẽ bóng bằng code); lông bay đúng hướng cả 4 hàng |
| Stoneback Guardian (Lv19–20) | `monsters/sunbreak/` | có | có (đập đất) | golem đá lưng rêu; bóng tím dưới chân; frame sát mép ô |
| Giant Scorpion (Lv26) | `monsters/scorpion-gorge/` | có | có (chích đuôi) | giáp tím than viền cam, ngòi xanh ngọc; không bóng vẽ sẵn (từ đợt này prompt dặn không vẽ bóng); đúng hướng 4 hàng |
| Sand Wraith (Lv27) | `monsters/scorpion-gorge/` | có | **chưa** (hết lượt) | bóng ma vải liệm kem + tím, thân dưới là cuộn cát; vài vệt hồng mờ trong vạt áo tím (gần màu key) → xem khi cắt. Attack dự kiến: ném cầu cát lõi xanh ngọc |
| Rock Basilisk (Lv28), Dune Golem (Lv29–30), Dune Devourer (boss map Lv30) | `monsters/scorpion-gorge/` | **chưa** | **chưa** | Basilisk: thằn lằn da vảy nhẵn xanh rêu xám, phiến tinh thể đỏ sẫm trên lưng, cắn. Dune Golem: sa thạch vàng + rune xanh ngọc, đấm cát. Dune Devourer: sâu cát khổng lồ giáp tím than, miệng tròn nhiều răng, 4 mắt xanh ngọc, đớp |
| Ridgehorn Behemoth (boss map Lv20) | `monsters/sunbreak/` | có | có (húc sừng đá) | dê khổng lồ bọc đá, sừng đá vân cam, sẹo mõm; kín ô, frame gần chạm nhau và chạm mép ô (nhất là attack) → kiểm khi cắt; bóng tím dưới chân |

## Prompt chuẩn

Gõ 1 dòng (xuống dòng = gửi). Dán "WORLD ART STYLE LOCK" (cuối `docs/world-art-bible.md`) ở đầu, rồi `|||`, rồi yêu cầu riêng. Gửi kèm ảnh mẫu.

- **Vẽ lại mảnh map cũ** (kèm mảnh cũ 1536x1024 + mảnh mới kề bên hoặc `new-oakhaven-ingame-overview.png` cho mảnh đầu): "VẼ LẠI <MAP> theo Art Bible. Ảnh 1 = mảnh <vị trí> (r?c?) của ảnh nền cũ 72x48 ô, mảnh này = 36x24 ô, ảnh NGANG 1536x1024. Ảnh 2 = mảnh mới nằm ngay <TRÁI/TRÊN> mảnh này. Vẽ mới hoàn toàn từ đầu ..., cùng phong cách, màu cỏ, màu đường, độ sáng như ảnh 2; mép <TRÁI/TRÊN> nối liền ảnh 2. GIỮ Y NGUYÊN BỐ CỤC ảnh 1 từng pixel: <liệt kê đường, suối, thác, cầu, vách, phế tích> đúng vị trí, độ rộng, hình dạng. GROUND ONLY: không cây, bụi lớn, đá lớn, nhà, rào, cầu (prop riêng). Không viền, không tối góc, không chữ."
- **Map mới 36x24 (concept)**, kèm `new-oakhaven-ingame-overview.png` + `world-maps-current-contact-sheet.png`: "Vẽ mới hoàn toàn từ đầu ảnh nền map <TÊN> (vùng N, bãi farm LvA–B), ảnh NGANG 1536x1024 = lưới 36x24 ô (~42 px/ô) ... Bố cục: đường đất rộng ~3 ô từ lối ra giữa mép TRÊN (...) ...; bãi quái là khoảng cỏ trống rộng (~8x6 ô) không vật cản; cầu và đường hoàn toàn trống. Viền map kín bằng rừng dày và vách, trừ N lối ra; trang trí chỉ ở ven đường, chân vách, góc map. 3 hàng ô dưới cùng bị HUD che nên chỉ là viền. Không nhân vật, không quái, không UI, không chữ, không lưới." Map lớn: thêm "GROUND ONLY ...".
- **Quái walk**, kèm sheet walk một quái cùng vùng đã có trong game: "Vẽ mới hoàn toàn từ đầu sprite sheet WALK của quái <TÊN> (LvN): <mô tả>, chibi anime fantasy đầu to kiểu Zenonia 5, viền tối rõ, cùng cỡ thân, độ dày nét và kiểu tô như sheet mẫu đính kèm. Ảnh VUÔNG 1024x1024, lưới 4x4 đều: hàng 1 quay mặt XUỐNG (về phía người xem), hàng 2 quay LÊN (thấy lưng), hàng 3 quay sang TRÁI (đầu chỉ về mép trái ảnh), hàng 4 quay sang PHẢI (đầu chỉ về mép phải ảnh); mỗi hàng 4 frame chạy bước liên tục, chân chạm cùng đường đáy của ô. Nền phẳng một màu #FF00FF tuyệt đối (không caro, không trong suốt, không bóng đổ màu khác, không đường kẻ ô), frame không chạm nhau và không chạm mép ảnh, hình đặc và đục. Không chữ." Boss: thêm "to và oai hơn hẳn ... chiếm gần kín ô (to hơn <quái thường> khoảng 1,5 lần)".
- **Quái attack**, kèm walk vừa vẽ + sheet attack mẫu: "Vẽ mới hoàn toàn từ đầu sprite sheet ATTACK của đúng con <TÊN> ở ảnh 1 (giữ y nguyên thiết kế, màu, cỡ thân như sheet walk ảnh 1; bố cục theo sheet attack mẫu ảnh 2). [lưới + hàng như trên]; mỗi hàng 4 frame: lấy đà, lao tới, trúng (hiệu ứng ngắn nằm gọn trong ô), lùi về. Hàng quay LÊN thì đạn / hiệu ứng bay LÊN (ra xa người xem). [nền như trên]".

## Lấy ảnh (lưu ý)

- Tải bằng JS: cuộn khung chat xuống đáy, lấy ảnh `Generated image` CUỐI trong `main` VÀ nằm SAU ảnh "User attachment" cuối (tức ảnh trả lời tin vừa gửi), `fetch(src)` → `<a download>`. Hai lần lấy theo "ảnh có src chưa thấy" / "ảnh Generated cuối" ra NHẦM ảnh cũ (Oakhaven, Thorn Sprite) vì DOM ảo hoá nạp lại ảnh cũ với src mới khi trang bị cuộn → luôn mở file ra xem.
- Chờ xong: đuôi `main` không còn "%", "Setting the scene", "Adding final touches", "One last tweak", "Preview", "Thinking" (bỏ qua chữ "Thinking effort" cố định ở ô nhập) và có "Edit". Trả lời chữ: chờ tới khi hết nút Stop (`[data-testid=stop-button]`).
- Lấy câu trả lời chữ: phần tử có class chứa `DilResponseRoot` cuối cùng, chuyển sang Markdown bằng JS rồi tải về như ảnh (in thẳng ra có thể bị chặn "[BLOCKED: Cookie/query string data]").
