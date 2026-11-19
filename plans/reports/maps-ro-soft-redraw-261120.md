# Vẽ lại toàn bộ map RO Soft Fantasy bằng ChatGPT (20/11/2026)

Theo yêu cầu "làm lại toàn bộ map + quái theo style ro fantasy" (phần map). Đã đọc `docs/map-design-rules.md`, `docs/sprite-rules.md` điều 0, báo cáo `maps-ro-soft-fantasy-261119.md`, brief ChatGPT.

## Kết quả

12 map vẽ lại ground, đã commit từng map, đã xem ảnh ô chặn và ảnh trong game:

| Map | Ảnh ChatGPT | Ghi chú |
|---|---|---|
| Willowbrook Meadows 72x48 | 4 | mép ghép không thấy |
| Thatchwick Village | 1 | nhà, giếng, sạp khớp từng ô |
| Frost Hollow 72x48 | 4 | mọi cầu thang, 2 cầu còn mở |
| Frostfang Den | 1 | từ hang tối thành hang băng sáng; vách vẽ thành cột nhỏ hơn, vài ô viền đỏ trên nền tuyết (rows cũ) |
| Sunspire Oasis | 1 | đá lát hơi hồng |
| Amber Dunes | 1 | |
| Scorpion Gorge 48x32 | 4 | thay đổi nhẹ (ChatGPT bám sát bản cũ) |
| Emberfen Marsh | 1 | nước đầm xanh trong |
| Bramblewood Forest | 1 | |
| Rootburrow | 1 | hang sáng, ấm |
| Thornheart Chamber 24x16 | 1 | |
| Sunbreak Foothills 48x32 | 4 (1 lần A/B) | thay đổi nhẹ |

Oakhaven, Sunken Reliquary: giữ bản 19/11. Khu Mire Hydra (58x24, sân 19x15) không đụng; `SunkenReliquaryMapTests` vẫn qua.

## Cách làm

- **Giữ nguyên bố cục:** rows, prop, cổng, spawn, khu Wanted, toạ độ không đổi. Không cần `SaveVersion`, **không cần build lại zone server** (rows không đổi).
- Tool mới `Tools/install-map-ro-redraw-windows.py layout|check|install|solids|sheet <map>`:
  - cắt ground hiện tại thành cửa sổ 3:2 (36x24: 1 cửa sổ; 48x32: 4 cửa sổ 27x18 chồng 6/4 ô; 72x48: 4 cửa sổ 39x26 chồng 6/4 ô) → ảnh bố cục 1536x1024 gửi ChatGPT;
  - khi cài: căn ảnh vẽ lại vào ảnh bố cục bằng ECC affine + optical flow DIS làm mượt (tối đa 24 px), khớp màu theo phần chồng, ghép mờ phần chồng; flow trung bình 1-6,5 px ở ảnh bố cục (< 0,2 ô);
  - `solids`: tô ô chặn đỏ lên ground mới để rà cầu thang / cầu / cổng.
- Minimap giữ ảnh cũ (có prop bake sẵn). Prop (cây, nhà, đá) vẫn art cũ đã chỉnh màu 19/11.
- 12 map đã thêm vào `REDRAWN` của `Tools/bake-map-ro-soft-tone.py` (không chỉnh màu chồng).
- Nguồn: `Assets/ArtLibrary/maps/<map>/ro-soft-redraw/` (`*-layout.png`, `*-chatgpt-N.png`, `*-before-ground.jpg`).

## ChatGPT

- Tài khoản Tay ("Tân Korris", đã đọc `/api/auth/session` trước mỗi chat mới). **26 lượt vẽ, chưa hết lượt**, không bị chặn quyền. Không dùng Tan Pham.
- Prompt một dòng, có câu tông RO (điều 0), tin đầu mỗi chat gửi kèm `sample-hero-user-pick.png` + `02-hud.png`.
- Chat: Willowbrook https://chatgpt.com/c/6aff4131-e5b8-83ec-9455-833aceb43da9 · Thatchwick https://chatgpt.com/c/6aff4328-d0b8-83ec-b9da-f1a63e01c559 · Frost Hollow https://chatgpt.com/c/6aff4387-123c-83ec-b488-223c5caa36da · Frostfang https://chatgpt.com/c/6aff4524-8104-83ec-99cc-7d7fc634de1e · Sunspire/Amber/Scorpion https://chatgpt.com/c/6aff4558-f4a4-83ec-aee9-48d9451293b4 · Emberfen/Bramblewood/Rootburrow/Thornheart/Sunbreak https://chatgpt.com/c/6aff4715-5740-83ec-b3c1-98168f0cf042

## Kiểm tra

- Ảnh trước/sau ground: `docs/reference/maps-ro-soft-redraw/<map>-before-after.jpg`; ô chặn: `<map>-solids.jpg`; trộn 50 %: `<map>-w<i>-blend50-over-layout.jpg`; trong game: `ingame/<map>-ingame-before-after.jpg` (trước = `D:\temp\player-maps` 19/11). Đã xem hết.
- `dotnet test`: ZenoniaSkills 452/452, ZenoniaNet 455/455.
- Build `D:\temp\player-maps2\Zenonia.exe` (bản sao `D:\temp\zu-copy-maps`, đã xoá `Library/PlayerDataCache` sau build).

## Chưa xem / chưa làm

- Trong game chỉ chụp 1 ảnh ở điểm vào mỗi map; chưa đi qua từng cầu thang / cổng trong game (đã rà trên ảnh ô chặn, rows không đổi).
- Prop cũ nét dày hơn nền mới (nhất là cây Willowbrook / Emberfen, nhà Sunspire); minimap vẫn màu cũ.
- Cột sáng cổng ở vài ảnh "sau" mờ hơn ảnh "trước": do bản build trước là bản 19/11 cũ hơn (FX cổng khác), không do ground.

## Câu hỏi

1. Có vẽ lại prop (cây, nhà, đá) cho khớp nét nền mới không?
2. Đá lát Sunspire hơi hồng: giữ hay vẽ lại vàng kem?
3. Scorpion Gorge / Sunbreak đổi ít: có muốn vẽ lại mạnh tay hơn (sẽ phải dựng lại rows)?
