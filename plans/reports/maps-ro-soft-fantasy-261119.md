# Map RO Soft Fantasy + khu boss Mire Hydra Queen to ra (19/11/2026)

Branch worktree `worktree-agent-a91063809692529b9` (từ `ro-fantasy-heroes` 77cc64ef). Đã đọc `docs/map-design-rules.md`, mục Map `CLAUDE.md`, báo cáo `map-redesign-evaluation-261119-ro-soft.md`.

## Kết quả

| Map | Cách làm |
|---|---|
| Sunken Reliquary | **ChatGPT vẽ lại** (Tan Pham, 2 ảnh: cột 0-35 và 22-57) + mở rộng khu boss |
| Oakhaven Village | **ChatGPT vẽ lại** (Tan Pham, 1 ảnh) |
| 12 map còn lại (Frost Hollow, Thatchwick, Willowbrook, Sunbreak, Bramblewood, Rootburrow, Thornheart, Sunspire, Amber Dunes, Scorpion Gorge, Emberfen, Frostfang Den) | chỉ chỉnh màu (bake LUT) |
| Prop (201 ảnh, mọi map) | chỉnh màu cùng profile với map đặt nó |
| Quái | không đụng |

## 1. Chỉnh màu (bake offline)

- Tool: `python Tools/bake-map-ro-soft-tone.py apply | revert | status | luts | preview`.
- Ảnh gốc sao lưu ở `Assets/ArtLibrary/maps/ro-soft-originals~/` (Unity bỏ qua thư mục có `~`), `manifest.json` ghi file nào đã chỉnh. `apply` luôn chỉnh từ bản gốc, chạy lại không chỉnh chồng. `revert` chép bản gốc về.
- Hàm chỉnh = bản v2 của `preview-map-ro-soft-tone-grade.py`, thêm 6 profile: ruins (Sunken prop, Emberfen, Bramblewood), meadow (Willowbrook, Sunbreak, làng), desert (bớt kem), snow (gần như không nâng sáng), icecave, cave (giữ tối). LUT `.cube` từng profile: `docs/reference/maps-ro-soft-fantasy/luts/`.
- Prop lấy profile của map đặt nó nhiều nhất, nên ground và prop cùng một hàm. Prop chưa map nào đặt: theo tiền tố tên.
- Lần 1, meadow và desert ra xám / hồng nhạt quá. Đã tăng bão hoà (meadow 1,08, desert 1,12) và xem lại.
- Chỉ đổi màu. Rows, prop, toạ độ giữ nguyên.

## 2. ChatGPT vẽ lại (Tan Pham)

- Mỗi lần gửi đều đọc `/api/auth/session` = "Tan Pham". Mỗi ảnh một chat mới. Prompt có câu tông RO (điều 0), gửi kèm `sample-hero-user-pick.png` và `01-style-board.png`. Chỉ một ảnh bố cục (là ground hiện tại) và toạ độ % của từng mốc.
- Không bị chặn quyền. Dùng 3 lượt vẽ, nhận cả 3, không vẽ lại lần nào.
- So bằng ảnh trộn 50 % trên ảnh bố cục: cả 3 khớp gần như từng ô, không bóng đôi (`*-ro-redraw-blend50-over-layout.jpg`).
- Cài đặt: `python Tools/install-map-ro-redraws.py [sunken|oakhaven] [--check]`.
  - Sunken: phóng x1,5. Ảnh đông khớp màu theo ảnh tây trên phần chung, rồi trộn mờ qua cột 27-29. Ở đó chỉ có nước, lối hẹp lên phòng nhỏ đông bắc và lối đá 2 ô.
  - Hai nền vẽ lại không qua bước chỉnh màu nữa (`REDRAWN` trong tool), chỉ prop của chúng được chỉnh.
- Nguồn: `Assets/ArtLibrary/maps/{sunken-reliquary,oakhaven-village}/ro-soft-redraw/`.
- Chat:
  - Sunken đông: https://chatgpt.com/c/6afece0f-681c-83ec-8253-b8cd54f5752b
  - Sunken tây: https://chatgpt.com/c/6afecf64-db1c-83ec-932e-c058eec65eea
  - Oakhaven: https://chatgpt.com/c/6afecfd9-acd4-83ec-b272-22954f605f8f

## 3. Khu boss Mire Hydra Queen

- Map 36x24 thành **58x24**: thêm 22 cột về phía đông. Số hàng giữ nguyên, nên toạ độ thế giới và save không lệch, không cần `SaveVersion`. Nền chia 2 mảnh 1856x1536 (dưới giới hạn 2048 của Standalone, không bị thu nhỏ) + minimap 348x144.
- Sân cũ: vòng tròn khoảng **70 ô** sàn, lối vào rộng 1 ô.
- Sân mới: bo tròn **19x15 ô** (cột 35-53, hàng 5-19), khoảng **280 ô** sàn, gấp 4 lần. Lối đá rộng **2 ô** (hàng 15-16, cột 22-34) từ sảnh.
- Sân trống. Tổ hydra để sát tường đông (51,12), 4 cột rêu ở 4 góc. Phòng nhỏ đông bắc và bãi ruinWisp giữ nguyên.
- Bản dựng đầu (`Tools/build-sunken-reliquary-wide-arena.py`, cắt ghép từ ảnh cũ) chỉ dùng làm ảnh bố cục cho ChatGPT. Lệnh `--rows` của tool in rows 58 cột.
- Boss tile **(45,12)**, đã đổi ở:
  - `MapSpawnTable.SunkenReliquaryBossTile` (Mirefang Hydra);
  - `WantedBoss` (mireHydraQueen);
  - `Tools/wanted-boss-crowd-playtest.ps1` (X 728, Y 152, Goal 728,184);
  - `Tools/wanted-boss-playtest.ps1`.
- Test mới `SunkenReliquaryMapTests`:
  - cỡ map và cỡ PNG;
  - flood fill: mọi ô đi được nối tới điểm xuất hiện;
  - 3 hàng HUD bị chặn;
  - lối đi 2 ô;
  - sân ≥ 140 ô, giữa sân trống;
  - 2 boss tile nằm giữa sân.
- Ảnh tô ô chặn `sunken-reliquary-wide-arena-solids.png` (trên nền vẽ lại):
  - đã xem: đỏ khớp tường và nước, sàn và lối đi không bị chặn;
  - Sunken không có cầu thang;
  - hai lối cụt ở mép tây (hàng 3-4 và 15-16) vẫn như cũ.
- **Map dùng chung với server: phải build lại zone server** (rows đổi, boss tile đổi). Client, server, server-api phát hành cùng lúc.

## 4. Kiểm tra

- `dotnet test` ZenoniaNet 455/455, ZenoniaSkills 437/437 (SDK 10).
- Build: `D:\temp\player-maps\Zenonia.exe`, từ bản sao `D:\temp\zu-copy-maps` (Library chép từ zu-copy-portal), ổ D còn 250 GB.
- Ảnh trong game, trước (`D:\temp\player-portal`) và sau, ghép cạnh nhau: `docs/reference/maps-ro-soft-fantasy/ingame/<map>-ingame-before-after.jpg`, đủ 14 map, đã xem từng ảnh. Lệnh `Tools/capture-maps-before-after.ps1`.
- Ảnh nền + prop trước và sau: `docs/reference/maps-ro-soft-fantasy/<map>-before-after-ro-soft.jpg`, đủ 14 map, đã xem.
- Đám đông 40 hero trên sân mới:
  - ảnh `sunken-arena-40-heroes-hud.jpg` và `-fight.jpg`;
  - chụp bằng `--map sunkenReliquary --tp 44,14 --record-layered-farm --farm-heroes 39`;
  - có thêm cờ `--farm-heroes` và lấy tâm theo `--tp` trong `DebugCapture.LayeredFarm.cs` (chỉ dùng khi chụp);
  - 40 người đứng còn rộng.

## Chưa xem / chưa làm

- Chưa chạy `wanted-boss-crowd-playtest.ps1` (cần mongod + server-api + zone thật). Ảnh đám đông là bot offline đánh slime, không có Mire Hydra Queen.
- Chưa xem trong game toàn bộ Oakhaven. Chỉ xem quanh quảng trường, cạnh ảnh tô ô chặn (rows không đổi).
- Một vài khối đá chìm ở chỗ trộn hai ảnh Sunken (cột 27-29) hơi mờ như bóng ma, phải nhìn kỹ mới thấy.
- Ghi chú có từ trước: rows của Oakhaven chặn một đoạn đường đất phía tây (hàng 8-9). Không do lần này.

## Câu hỏi chưa chốt

1. Có cho ChatGPT vẽ lại thêm map nào không (Emberfen, Bramblewood: hai map chỉnh màu còn lệch nét nhiều nhất), hay giữ chỉnh màu?
2. Quái có chỉnh màu theo map không?
3. Prop của Sunken và Oakhaven (tổ hydra, cột rêu, đèn) vẫn là art cũ đã chỉnh màu, nét dày hơn nền mới. Có vẽ lại prop không?
