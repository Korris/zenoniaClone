# Quy tắc tạo map

Đọc hết file này trước khi làm hoặc sửa map, kể cả khi giao cho agent. Lập ngày 2026-11-05, sau khi làm Thatchwick Village, Willowbrook Meadows 72x48 và Frost Hollow 72x48.

## Bố trí: đường phải trống

1. **Không đặt vật cản trên đường.** Đường, lối đá, cầu, cầu thang, đầu và chân cầu thang, quảng trường, bãi quái (rect của zone) không có ô chặn của prop. Dọc mỗi con đường chừa lối trống rộng ít nhất 2 ô.
2. **Muốn map đẹp thì trang trí ở ven đường, chân vách, góc map, quanh điểm nhấn** (phế tích, trại, ao, hồ), không rải giữa bãi đánh quái.
3. **Đồ nhỏ đi xuyên qua được** (`FootH` = 0): bụi, đá nhỏ, gốc cây, xương, khúc gỗ, cây non, pha lê nhỏ, sậy, hoa. Đồ đi xuyên qua thấp (cao không quá 3 ô) **luôn vẽ dưới hero và quái** như mặt đất (`MapScenery.FlatDecorMaxTiles`); không được để vật nào hero đi qua mà vẽ đè lên hero.
4. **Cây chỉ chặn ô gốc** để hero đi ra sau tán. Đồ lớn (tảng đá to, bia đá, cột, tường, lều, rào, rào chắn) vẫn chặn nhưng phải nằm ngoài lối đi.
5. **Mỗi bãi quái có ít nhất gấp đôi số quái ô trống**, cách điểm xuất hiện, cổng và chỗ đến từ cổng ít nhất 5 ô. Cấp quái tăng dần theo khoảng cách từ cổng vào, không nhảy cóc.
6. **Mọi ô đi được phải nối tới điểm xuất hiện** (flood fill). Ô đi được mà không tới được thì chặn hoặc mở lối.
7. **Rà từng cầu thang và lối qua vách:** script phân loại theo màu hay đọc bậc đá xám thành vách. Mở bậc, giữ lan can chặn, nối liền đầu và chân. Viết vào file override để dựng lại không mất.
8. **Bottom 3 hàng nằm dưới HUD** vẫn phải chặn, kể cả map 48 hàng.

## Cách dựng map (đã chạy được)

- **Kích thước:** 36x24 ô (1 màn hình khoảng 18x10 ô) hoặc 72x48 cho bãi săn lớn. Map cho nhiều người săn nên làm 72x48 (xem phân tích sức chứa trong `docs/pending-tasks.md`).
- **Ảnh nền chỉ là đất, cây và đồ đứng là prop riêng** (y-sort, đi ra sau được). Không dùng một ảnh vẽ liền có cả nhà và cây (trừ làng nhỏ như Thatchwick, người dùng chấp nhận).
- **Art từ ChatGPT** (tài khoản theo `CLAUDE.md`):
  - Xin bản "ground only" của ảnh gốc: dặn rõ giữ vách, sông, cầu, bậc, hang; ChatGPT có thể vẽ lại vách, khi đó phải dựng lại rows và đặt prop bằng tay.
  - Mở rộng bằng vẽ nối tiếp (outpaint): khung 1536x1024 có 1/3 hoặc 1/2 là ảnh cũ, phần xám là chỗ vẽ mới. Dặn "GROUND ONLY", nối liền đường, suối. Dải mép có thể dùng khung dọc 1024x1536 ở tỉ lệ 1:1 (với pixel art không dùng tỉ lệ thu nhỏ).
  - Tấm prop: các món tách rời trên nền `#FF00FF`, cùng cỡ với cây trong ảnh gốc.
  - Lấy ảnh bằng `fetch(blob)` + tải xuống, không chụp màn hình.
- **Script:**
  - Ghép: `Tools/build-willowbrook-large-painted.py`, `Tools/build-frost-hollow-large-painted.py` (chỉnh màu, đường nối min-cut, cắt 2x2 mảnh 2304x1536, minimap 432x288). Vá một chỗ bằng `PATCHES`.
  - Ô chặn: `Tools/build-*-large-rows.py` + file override trong `plans/<map>/layout/`.
  - Prop: `Tools/cut-willowbrook-extra-props.py`, `Tools/cut-frost-hollow-props.py`; bảng `Assets/Scripts/Maps/<Map>*Props.cs`.
  - Xem trước: `Tools/preview-*-large-composite.py` (`--minimap` để bake prop vào minimap; in `PROBLEM` khi prop đè lối cấm).
  - Nền vẽ lại (redraw, 4 mảnh 1536x1024 kề nhau, không chồng mép): `Tools/build-large-ground-from-redraw-quarters.py <willowbrook|frost-hollow>` (khớp màu từng mảnh, san bậc sáng ở mép, cắt min-cut qua dải soi gương; `warps` để kéo vật bị lệch giữa hai mảnh). Sau đó `build-<map>-large-rows.py --redraw` (rows trong `.cs` là gốc + file `*-redraw-overrides.txt`) rồi `preview-*-composite.py --minimap`. Willowbrook: `cut-willowbrook-extra-props.py --redraw`, `cut-willowbrook-bridge-prop.py` (cầu là prop phẳng `wbBridge`), `grade-willowbrook-old-section-props-to-redraw.py`.
  - Quái: `Tools/build-meadow-monsters.py <id>` (sheet walk 768x768 ô 192, attack 1024x1024 ô 256, hàng xuống/lên/trái/phải).
- **Map ghép từ các mảnh vẽ rời, không có dải trùng (Sunbreak Foothills 48x32, 4 góc 1536x1024 đúng 64 px/ô):** `Tools/build-sunbreak-foothills-painted.py` (kéo lệch cục bộ `WARPS` cho suối / đường khớp qua mép, san màu hai bên mép, trộn gương ±14 px, `PATCHES`), `build-sunbreak-foothills-rows.py` (phân loại màu + file override + flood fill + kiểm suối / vách chỉ qua được ở cầu và bậc), `write-sunbreak-foothills-map-class.py` (dán rows vào class), `cut-sunbreak-foothills-props.py`, `preview-sunbreak-foothills-composite.py` (in `PROBLEM`), `preview-sunbreak-foothills-grid-crop.py` (phóng to một vùng có toạ độ từng ô để viết override).
- **Map ghép từ các mảnh vẽ rời, mép đã khớp sẵn gần như hoàn hảo (Scorpion Gorge 48x32, 4 góc 1536x1024 đúng 64 px/ô, chép lại bản tổng thể nên không cần `WARPS`):** `Tools/build-scorpion-gorge-painted.py`, `build-scorpion-gorge-rows.py` (phân loại cliff/nước bằng `luma` + `sd` cục bộ, KHÔNG dùng hiệu R-B vì cát/đường cũng rất đỏ như đá; viền map phải ép `C` cả 4 cạnh bằng tay vì cửa sổ làm mờ ở mép ảnh làm hụt ngưỡng màu đá; lòng sông "cạn" (không phải suối chảy) vẫn phải chặn kín cả dải, chỉ hở đúng ô cầu, viết tay toạ độ uốn khúc bằng `preview-scorpion-gorge-grid-crop.py`), `write-scorpion-gorge-map-class.py`, `cut-scorpion-gorge-props.py` (ribcage phải tăng `iterations` nối blob vì các dẻ xương tách rời nhau vài px), `preview-scorpion-gorge-composite.py`.
- **Hai map trỏ cổng vào nhau: toạ độ điểm đến phải là `const int`**, không đọc `static readonly Vector2Int` của map kia trong bảng `MapPortal`. Hai class đọc field static của nhau lúc khởi tạo thì trong bản build (Mono) một bên đọc ra (0,0): bản build đầu của Sunbreak đưa hero về ô (0,0) của Frost Hollow, còn `dotnet test` vẫn qua. Xem `SunbreakFoothillsMap.SouthArrivalTx`.
- **Tông RO Soft Fantasy (19/11/2026):** ground, minimap và prop đã bake màu bằng `Tools/bake-map-ro-soft-tone.py` (gốc ở `Assets/ArtLibrary/maps/ro-soft-originals~/`). Sửa hay dựng lại ground thì ghi bản CHƯA chỉnh vào thư mục gốc đó rồi chạy `apply`; map vẽ lại bằng ChatGPT (Sunken, Oakhaven) nằm trong `REDRAWN`, cài bằng `Tools/install-map-ro-redraws.py`.
- **Vẽ lại ground bằng ChatGPT, giữ bố cục (20/11/2026, 12 map):** `Tools/install-map-ro-redraw-windows.py layout|check|install|solids|sheet <map>`: cửa sổ 3:2 chồng nhau, căn ECC + optical flow vào ảnh bố cục, ghép mờ phần chồng; rows / prop / cổng không đổi. Xem `plans/reports/maps-ro-soft-redraw-261120.md`.
- **Engine:** mỗi map khai `Width`, `Height` theo rows; ảnh nền chia mảnh qua `GroundChunkCols/Rows`, tên `<GroundTexture>-r{r}c{c}` + `-minimap`.
- **Đổi kích thước map đang có người chơi:** toạ độ save là toạ độ thế giới, map cao thêm thì save cũ lệch. Phải tăng `SaveSerializer.SaveVersion` cùng commit với map mới và dịch toạ độ khi đọc (đã làm cho Frost Hollow). Assembly `Rpg` không thấy class map: truyền số hàng qua `SaveStore.FrostHollowRows` (gán ở `Bootstrap`).
- **Đổi bảng quái, map, protocol:** client, server, server-api phát hành cùng lúc; build lại server.
- **Bóng đổ (FakeSun, 21/11/2026):** bóng prop sinh tự động lúc vào map từ bảng prop theo TÊN loại (`Assets/Scripts/Shadows/StaticPropShadows.cs` `Classify`: oak / pine / tree / palm... = cây; house / cottage / tent / stall... = nhà; còn lại = đá; `FootH` 0 và thấp ≤ 3 ô = đồ trang trí; bridge / palisade / flower... = không bóng). Prop mới đặt tên theo các từ khoá đó hoặc thêm vào `Classify`. Hướng, màu, alpha chỉ sửa trong `FakeSun.cs`, không chỉnh tay từng map. Vách vẽ trong ảnh nền chưa có bóng.

## Kiểm tra trước khi báo xong

- Xem ảnh ghép có prop và ảnh tô ô chặn (đỏ) của cả map.
- Build từ bản sao (`Tools/build-from-copy.ps1`), chụp trong game bằng `--map <id> --tp x,y --solids --shots`: cổng, điểm xuất hiện, từng cầu thang, cầu, bãi quái, đi qua cổng sang map kề.
- **Chụp gần bãi quái cấp cao (map Lv20+) phải thêm `--god`**: hero mặc định Lv1 110 HP, quái bãi mới (vd Lv26-30) đánh một đòn là chết, hero hồi sinh về cổng khiến ảnh chụp sau đó trông như `--tp` không chạy (đã gặp khi làm Scorpion Gorge: toạ độ xa cổng > ~20 ô luôn ra lại ảnh cổng, hoá ra do chết chứ không phải lỗi dịch chuyển). Xem khung HP ở góc trái ảnh trước khi kết luận `--tp` sai.
- `dotnet test` cả hai project.

## Sổ lỗi

| Ngày | Việc | Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|---|---|
| 2026-11-21 | Bóng đổ FakeSun, Willowbrook | Lần chụp đầu gần như không thấy bóng cây, đá, hero (mask đã đúng, dump mask có đủ blob) | Quad phủ trộn alpha thường màu bóng #536B69 (xám xanh, độ sáng gần bằng cỏ) ở alpha 0,18-0,34 lên cỏ sáng: cỏ chỉ tối đi ~5-8 % | Đổi sang nhân: nền x lerp(1, màu bóng, a) (`Blend DstColor Zero`), cỏ tối ~12-18 %, vẫn không đen, màu bóng vẫn ngả lạnh; giữ alpha theo spec |
