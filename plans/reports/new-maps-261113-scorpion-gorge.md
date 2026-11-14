# Scorpion Gorge vào game (World Map 2.1, 13/11/2026)

Chưa commit. Build thử: `C:/temp/player-scorpion-gorge/Zenonia.exe` (đã xoá sau khi chụp xong). Build khẩn riêng cho người dùng (yêu cầu giữa chừng, không liên quan map này): `C:/temp/player-world-map-22/Zenonia.exe`.

## Đã làm

- **Map** `scorpionGorge` / `ScorpionGorgeMap`, 48x32, Hostile, nền 2x2 mảnh 1536x1024 (`Sprites/village/tiles/scorpion-gorge-ground-r{r}c{c}.png`) + `-minimap.png`. Nhạc tạm `frost-hollow`.
- **Ghép 4 góc** (`Tools/build-scorpion-gorge-painted.py`): 4 góc gần như chép lại bản tổng thể nên mép khớp sẵn, không cần `WARPS`/`PATCHES` (chỉ còn san màu + trộn gương mặc định).
- **Ô chặn**: `build-scorpion-gorge-rows.py` + `plans/261113-1923-scorpion-gorge/layout-scorpion-gorge/scorpion-gorge-rows-override.txt`. Phân loại bằng `luma` + độ lệch chuẩn cục bộ (không dùng hiệu R-B như Sunbreak vì cát/đường ở đây còn đỏ hơn cả đá). Viền map phải ép `C` cả 4 cạnh bằng tay (cửa sổ lọc màu hụt ngưỡng ở sát mép ảnh). Lòng sông "cạn" (không phải suối nước) vẫn phải chặn kín cả dải uốn khúc bằng tay, chỉ hở đúng ô của 2 cầu đá thẳng — `sông chỉ qua được ở cầu` đã kiểm bằng flood fill (đóng từng cầu, đóng cả hai). Nền đền sa thạch (22-27,0-4) bị chặn cứng vì chưa có Sunspire Vault; bậc thang trước đền (23-26,5-6) để mở làm bệ cụt.
- **Prop**: 20 món `sg*` (`cut-scorpion-gorge-props.py`, phải tăng `iterations` nối blob cho bộ xương sườn vì các dẻ xương tách rời vài px trên tấm nguồn), 26 chỗ đặt (`ScorpionGorgeProps.cs`), def ở `ScorpionGorgePropDefs.cs`. Trại nhỏ (lều + lửa trại + xương rồng) cạnh cổng tây; cột đèn, bình gốm quanh ngã tư đền; tượng bọ cạp, trụ đá, đống xương gần sân boss; bụi gai + bình vỡ ở rìa đông (vùng đầm lầy Mirewatch).
- **Cổng**: tây (0,14)+(0,15) ↔ Amber Dunes (2 chiều, điểm đến `const int` cả hai phía — `ScorpionGorgeMap.WestArrivalTx/Ty` = (3,14), `AmberDunesMap.ScorpionGorgeArrivalTx/Ty` = (33,11)). Amber Dunes dùng đúng "track cụt" sẵn có ở mép đông (35,11), ghi sẵn trong comment cũ của file. Cổng đền (Sunspire Vault) và khe tường đông (Mirewatch) **chưa nối** vì chưa có map đích — để nguyên hình (đền chặn cứng, khe đông vẫn là nền đi được nhưng không có `MapPortal` nào ở đó, không phải ngõ cụt xây tường như Sunbreak's dead end).
- **Quái**: 50 con / 8 bãi — giantScorpion Lv26 x13 (2 bãi gần cổng), sandWraith Lv27 x12 (2 bãi qua sông), rockBasilisk Lv28 x13 (2 bãi đông bắc), duneGolem Lv29 x12 (2 bãi áp sát sân boss); boss `duneDevourer` Lv30 tại (38,25) trong lòng chảo cát xoáy, `HasOwnBoss` = true. Mỗi bãi ≥ 4x số quái ô trống (kiểm script in ra), cách cổng/điểm đến ≥ 5 ô.
- **Chỉ số quái** theo đường cong cũ (HP 14+22L, Atk 3+4L, Def 1+L, EXP 14L, gold L..2L+1, nhân hệ số theo "vai" của quái có sẵn — xác minh lại công thức bằng các quái Sunbreak đã cài, khớp chính xác): giantScorpion/rockBasilisk dùng hệ số "line" như Highland Ibex (Atk x1.1); sandWraith dùng hệ số Ice Wraith (HP x0.8, Atk x1.2, Def thấp) nhưng đánh cận chiến vì AI chưa bắn đạn (giữ tiền lệ Ruin Wisp, dù art vẽ ném cầu cát); duneGolem dùng hệ số Crystal Golem (HP x1.8, Atk x1.1, Def x1.6); duneDevourer dùng luật boss Glacier Warden/Ridgehorn (HP x7, Atk x1.5, Def x0.75, EXP/gold x6.67). HP hiển thị trong game đã nhân đôi đúng như code (xem Dune Devourer 9436/9436 = 4718*2).
- **Quái mới cài vào `Tools/build-meadow-monsters.py`** (`WORLD2_DIRS`, không cần `DROP_SHADOW`/`FLYERS` vì đợt vẽ này không có bóng sẵn — kể cả Sand Wraith lơ lửng vẫn ra đúng vì cuộn cát trong art đã chạm đáy ô), `Assets/Scripts/MonsterDef.cs` (5 def mới), `Assets/Scripts/Rpg/LootTable.cs` (5 dòng loot).

## File đã tạo

- `Tools/build-scorpion-gorge-painted.py`, `build-scorpion-gorge-rows.py`, `write-scorpion-gorge-map-class.py`, `cut-scorpion-gorge-props.py`, `preview-scorpion-gorge-composite.py`, `preview-scorpion-gorge-grid-crop.py`.
- `Assets/Scripts/Maps/ScorpionGorgeMap.cs`, `ScorpionGorgePropDefs.cs`, `ScorpionGorgeProps.cs`.
- `Tests/dotnet/ZenoniaNet.Tests/ScorpionGorgeMapTests.cs` (6 test).
- `Assets/Resources/Sprites/village/tiles/scorpion-gorge-ground-r{0,1}c{0,1}.png` + `-minimap.png`.
- `Assets/Resources/Sprites/village/props/sg*.png` (20 file).
- `Assets/Resources/Sprites/monsters/{giantScorpion,sandWraith,rockBasilisk,duneGolem,duneDevourer}.png` + `-attack.png` (10 file).
- `plans/261113-1923-scorpion-gorge/layout-scorpion-gorge/` (rows, override, visuals).

## File dùng chung đã chạm (chỉ dòng của mình)

`Assets/Scripts/Maps/MapRegistry.cs` (thêm map), `Assets/Scripts/MapSpawnTable.cs` (khối ScorpionGorge + 4 case), `Assets/Scripts/Maps/VillagePropDefs.cs` (nối `ScorpionGorgePropDefs.TryGet`), `Assets/Scripts/Maps/AmberDunesMap.cs` (thêm 1 portal + 1 const, ngoài danh sách được giao nhưng bắt buộc để nối 2 chiều), `Assets/Scripts/MonsterDef.cs`, `Assets/Scripts/Rpg/LootTable.cs`, `Tools/build-meadow-monsters.py`, `Tests/dotnet/ZenoniaNet.Tests/MapGraphTests.cs` (thêm 1 `InlineData` + 1 `Fact`), `docs/map-design-rules.md` (2 gạch đầu dòng), `docs/world-map-2-art-progress.md` (1 hàng).

## Toạ độ cổng

- Scorpion Gorge (0,14) / (0,15) → Amber Dunes (33,11).
- Amber Dunes (35,11) → Scorpion Gorge (3,14) = điểm xuất phát của map.
- Đền (22-27,0-4) chặn cứng, bậc (23-26,5-6) mở nhưng không dẫn đi đâu (ngõ cụt trước cửa). Khe tường đông (44-47,11-17, cạnh vũng bùn) vẫn đi được, không có cổng.

## Kết quả test

`dotnet test Tests/dotnet/ZenoniaSkills.Tests` 313/313 pass. `Tests/dotnet/ZenoniaNet.Tests` 401/401 pass (401 = 387 cũ + test mới: `ScorpionGorgeMapTests` 6 fact, `MapGraphTests` +1 `InlineData` theory +1 `Fact`). Dùng `~/.dotnet/dotnet.exe` (net10.0); `dotnet` trong PATH là 9.0.203 không chạy được project này.

## Đã xem (tự mở ảnh)

- Ghép: `plans/261113-1923-scorpion-gorge/layout-scorpion-gorge/visuals/stitched-overview.png`, 4 ảnh mép nối 1:1 (không thấy đường nối).
- Ô chặn: `rows-overlay-full.png` + 4 góc 1:1 — sông chặn đúng dải uốn khúc, 2 cầu vàng đúng chỗ, đền chặn đỏ, viền map kín.
- Ảnh ghép có prop + overlay cuối: `composite-full.png`, `composite-solid.png` — 0 problem (prop không đè đường/cầu/bãi quái, 50 quái đúng, bãi nào cũng đủ ô trống và cách cổng ≥ 5 ô).
- **Trong game** (`--map scorpion-gorge --tp x,y --solids --shots`, warrior Lv1): cổng tây + trại (ảnh thường), cả 2 cầu (ảnh thường, thấy Sand Wraith lượn ngay cạnh, sông chặn đúng, cầu đi được), đền + bậc (phải thêm `--god` vì Sand Wraith/Rock Basilisk giết hero Lv1 trong 1-2 giây — xem mục Sổ lỗi/ghi chú bên dưới), sân boss với Dune Devourer (HP 9436/9436 = đúng 2x số tính tay, art đẹp, không lỗi mép ô), bãi Rock Basilisk đông bắc, bãi Dune Golem (vây kín hero), bãi Giant Scorpion cạnh cổng (cũng cần `--god`). Điểm đến bên Amber Dunes (33,11): road hai nhánh nối đúng chỗ track cụt cũ, không lệch mép (cả thường lẫn `--god`).
- Minimap trong game: chấm đỏ quái phân bố đúng 8 cụm, vòng tròn đỏ = boss đúng vị trí lòng chảo.

## CHƯA kiểm

- Chưa đi bộ thật xuyên qua cổng tây (chỉ `--tp` riêng từng phía + test round-trip tự động `WorldMap21RegionThreeRunsSunspireAmberDunesScorpionGorge`), chưa đi bộ thật qua từng cầu (chỉ teleport + ảnh tô đỏ + flood fill).
- Chưa đánh boss thật, chưa xem đòn Sand Wraith ném cầu cát thật sự bay ra (AI hiện đánh cận chiến, chưa có VFX phóng chiếu — xem câu hỏi mở).
- Chưa thử online/server (bảng map + quái đổi → phải build lại server, không nằm trong việc này).
- Unity của người dùng chưa import PNG mới (chưa có `.meta`, Unity tự sinh khi mở — không tự ý mở Unity theo đúng quy tắc).
- Tiến trình `Zenonia.exe` PID còn treo máy sau lần chụp `--god --tp 4,5` cuối (bị chặn không cho kill cứng theo chính sách sandbox); thư mục `C:/temp/player-scorpion-gorge` xoá dở, còn vướng ~3 GB do tiến trình đó giữ file. Người dùng có thể tự đóng cửa sổ game đó rồi xoá tay.

## Sự cố giữa chừng (không phải lỗi map)

Lúc chụp ảnh xa cổng (đền, sân boss, bãi đông bắc — cách cổng > ~20 ô), ảnh luôn hiện lại y hệt cảnh cổng dù đã đổi `--tp`. Tưởng là lỗi dịch chuyển, hoá ra hero Lv1 (110 HP) bị quái Lv26-30 (sát thương ~100+/đòn) giết chết ngay khi vừa teleport vào giữa bãi quái rồi hồi sinh lại ở cổng — `--tp` vẫn chạy đúng. Xác nhận bằng ảnh `temple_1.png` (HP 0/110, đang bị 3 Sand Wraith + 1 Rock Basilisk vây). Sửa bằng cách thêm `--god`. Đã ghi vào `docs/map-design-rules.md` mục "Kiểm tra trước khi báo xong" để agent sau khỏi mất thời gian như tôi.

## Việc dở giữa chừng (yêu cầu khẩn của người dùng)

Nhận yêu cầu dừng tay build gấp bản chơi thử: `C:/temp/player-world-map-22/Zenonia.exe` (từ `C:/temp/zu-copy`, đã mirror đúng các file người dùng vừa sửa: `VillagePropDefs.cs`, `BramblewoodForest*.cs`, prop `wb*`, `cliff-harpy-feather.png` — xác nhận qua `git status`). Test 2 project pass trước khi build (313/313, 401/401). Không tìm thấy `C:/temp/player-world-map-21b/dev-start.json` trên máy để copy sang như yêu cầu — đã báo qua `SendMessage` tới `main`, chưa có phản hồi lại. dev-start.json trong exe mới là bản hiện có của repo (mirror tự động).

## Câu hỏi còn mở

- Sand Wraith đánh cận chiến dù art vẽ ném cầu cát (theo tiền lệ Ruin Wisp, AI chưa có đòn bắn chung cho quái thường ngoài Cliff Harpy). Có cần thêm đòn phóng chiếu cho Sand Wraith sau này không?
- Khe tường đông (Mirewatch) để hở nền đi được nhưng không có cổng — hay nên xây kín như "ngõ cụt" của Sunbreak để tránh hero đứng ở rìa map vô nghĩa? Hiện chưa chặn vì nghĩ việc dựng Mirewatch sẽ tới sớm và chỉ cần thêm 1 `MapPortal`, không cần sửa rows.
- `AmberDunesMap.cs` hiện có 2 lối ra phía đông (y11 mới nối Scorpion Gorge, y17 vẫn nối thẳng Emberfen Marsh) — theo sơ đồ thiết kế (`docs/world-map-2-design.md`) tuyến đúng phải là Amber Dunes → Scorpion Gorge → Mirewatch → Emberfen, nhưng việc sửa/bỏ cổng Amber Dunes→Emberfen cũ không nằm trong phạm vi việc này (đổi tuyến vùng 4 cần Mirewatch tồn tại trước), chưa đụng tới, chỉ ghi lại.
