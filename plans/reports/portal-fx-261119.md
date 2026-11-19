# Cổng dịch chuyển ánh sáng: Particle System + shader + bloom (19/11/2026)

Branch: worktree `worktree-agent-a267db1e86357754c`, dựa trên `ro-fantasy-heroes` 149c1913. Chưa push, chưa merge.
Exe: `D:\temp\player-portal\Zenonia.exe` (build từ bản sao `D:\temp\zu-copy-portal`, 0 lỗi).

## Lượt 2 (người dùng: "làm cái cột sáng to ra màu xanh")

- Cột sáng to gấp đôi: rộng 40 + 8 × số ô (trước 20 + 4), cao 92 đv (trước 62). Vũng sáng 60 + 14 × số ô, hào quang 1,5 lần vũng.
- Một màu xanh băng cho mọi map, bỏ màu theo vùng (bỏ `PortalLightRegion` / `RegionOf`).
  - Phần cộng chủ yếu xanh (0,1 / 0,48 / 1,25), lõi trắng 0,4.
  - Lớp alpha xanh đậm (0,08 / 0,4 / 0,95, độ đục 0,55).
- Giới hạn hạt giữ nguyên 18 mỗi cổng. Hạt nằm trong khung rộng hơn.
- Đã chụp lại 3 chỗ, mỗi chỗ 10 khung, ảnh `*-new-*` trong `docs/reference/portal-fx/` đã thay. Đã xem từng khung:
  - **Frost Hollow:** cột xanh rộng, đọc rõ trên tuyết, không loá trắng.
  - **Oakhaven:** cột xanh rõ trên đường đất. Lõi xanh nhạt nhưng không cháy trắng, đã có màu đặc. Thùng gỗ phía sau cột bị nhuộm xanh nhẹ, vì cột y-sort đứng trước nó.
  - **Rootburrow:** cột xanh to lấp gần kín cửa hang rễ, bloom đẹp, không che hero.
  - Không khung nào chớp, không có đốm nhảy.
- Phần dưới là ghi chép lượt 1 (màu theo vùng), nay đã bỏ.

## Hiện trạng trước khi làm

- Code đang chạy là `MapPortalHaze` (sương sáng cuối đường cho cổng sát mép) + `MapPortalBeam` (cột sáng cho cổng giữa map).
- `MapPortalMarkers` (cột mốc prop) đã bị gỡ từ 13/11, không còn trong code. Sổ lỗi ghi "chốt cuối là cột mốc" là đã cũ. Vì vậy lượt này không có prop nào để giữ.

## Đã làm

| File | Vai trò |
|---|---|
| `Assets/Scripts/Maps/MapPortalExits.cs` | Logic thuần, có test. Gộp các ô cổng liền nhau. Chọn chỗ đặt ánh sáng: cổng mép lùi vào 1,6 ô, cổng mép dưới lùi 3,6 ô để tránh HUD; nếu trúng ô chặn thì lùi dần về phía cổng. Vùng màu theo map. Nhịp thở `Breath`. |
| `Assets/Scripts/Maps/MapPortalLightFx.cs` | Dựng cổng: hào quang và vũng sáng dưới đất, cột sáng, lõi, hạt. Thêm component thở. Cờ `--portal-fx off`. |
| `Assets/Scripts/Maps/MapPortalLightLook.cs` | Bộ material cho từng vùng màu. |
| `Assets/Resources/Shaders/LightParticle.shader` | Shader cho hạt, 2 pass: lớp alpha màu đặc (thấy trên tuyết) + lớp cộng HDR (bloom). Độ mờ lấy từ alpha của vertex. |
| `Assets/Scripts/Maps/MapScenery.cs` | Bật bản mới; tắt cờ hoặc thiếu shader thì về `MapPortalHaze` cũ. |
| `Tools/build-light-skill-textures.py` | Thêm `portal-veil`, `portal-mote`. Lệnh `gen <tên...>` chỉ sinh các texture được chỉ định. |
| `Tools/build-portal-fx-capture-sheet.py` | Ghép chuỗi ảnh `--shots` thành tấm so sánh. |
| `Tests/dotnet/ZenoniaNet.Tests/MapPortalExitsTests.cs` | Với mọi map: ánh sáng nằm trong map, đứng trên ô đi được, không nằm dưới HUD, mỗi ô cổng thuộc đúng một cổng. Nhịp thở không nhảy quá 0,01 mỗi frame. |

Hình của một cổng:
- **Mặt đất:** hào quang rộng mờ + vũng sáng elip mềm (`LightBlob`). Vẽ dưới hero và quái. Không có viền cứng, để tránh lỗi "đĩa bay".
- **Cột sáng:** `LightBeamScroll` với texture `portal-veil` gồm dải rộng mềm.
  - Cuộn lên chậm 0,22 uv/s, uốn nhẹ theo nhiễu.
  - Không dùng dissolve: lỗ dissolve nhìn như đốm nhảy.
  - Lõi hẹp ở giữa là phần duy nhất bloom.
  - Cột y-sort như prop.
- **Hạt:** `ParticleSystem` dựng bằng code.
  - Tối đa 18 hạt mỗi cổng, 5-7 hạt/s, sống 3-3,8 s.
  - Bay lên 8-11 đv/s, đung đưa ngang theo sin.
  - Mờ vào rồi mờ ra mượt theo thời gian sống.
  - Bật prewarm nên vào map không thấy hạt bật ra đột ngột.
- **Thở:** độ sáng của nền và cột dao động 80-100 % theo chu kỳ 3,6 s, đường cos liên tục. Các cổng lệch pha nhau.
- **Màu theo vùng:**

| Vùng | Map | Màu |
|---|---|---|
| Tuyết | Frost Hollow, Frostfang Den | xanh băng |
| Làng, đồng cỏ | Oakhaven, Thatchwick, Willowbrook | vàng hổ phách |
| Rừng, hang rễ | Bramblewood, Rootburrow, Thornheart | xanh lá |
| Sa mạc | Sunbreak, Sunspire, Amber Dunes, Scorpion Gorge | ngọc lam |
| Đầm, phế tích | Emberfen, Sunken Reliquary | tím |

- **Không đổi ô cổng, trigger, ô tới.** Không thêm ô chặn, ánh sáng không có va chạm.
- **Số hạt:** map nhiều cổng nhất có 3 cổng, tức tối đa 54 hạt. Mỗi cổng có 4 quad + 1 particle system.

## Ảnh trong game (đã tự xem từng khung)

Ảnh ở `docs/reference/portal-fx/`:
- `*-sheet.jpg`: 10 khung cách 0,15 s, cắt quanh cổng.
- `*-full.jpg`: nguyên màn hình.
- `new` là bản mới, `old` là bản `--portal-fx off`.
- Hero Warrior đứng cạnh cổng, độ phân giải 1280x720.

| Chỗ | Lệnh |
|---|---|
| Tuyết | `--class warrior --map frost-hollow --tp 4,21 --face left` (cổng sang Thatchwick) |
| Làng | `--map oakhaven-village --tp 16,20` (cổng nam sang Willowbrook) |
| Map tối | `--map rootburrow --tp 32,13 --face right` (cổng đông sang Bramblewood) |

Đã thấy:
- **Tuyết:**
  - Cột sáng xanh rõ trên tuyết, có quầng bloom nhẹ ở chân.
  - Không loá trắng.
  - Bản cũ là sương trắng phủ làm bạc màu cả vách đá bên trái, khó nhận ra là cổng.
  - Hạt bay trên tuyết rất khó thấy, kể cả sau khi tăng cỡ hạt.
- **Làng:**
  - Lần chụp 1: cột vàng loá trắng cả mặt đường, nhìn như lửa. Đã ghi vào Sổ lỗi `docs/sprite-rules.md`.
  - Sau khi hạ phần cộng: cột hổ phách mềm, thấy được hạt vàng nhỏ bay lên, không loá.
  - Bản cũ gần như không thấy gì.
- **Rootburrow (tối):**
  - Cột xanh lá sáng, bloom đẹp.
  - Thấy hạt xanh bay lên.
  - Hero không bị che.
  - Bản cũ là sương trắng mờ.
- **Cả 3 chỗ:** 10 khung liên tiếp chỉ khác nhau rất nhẹ do vân cuộn và nhịp thở. Không có khung nào chớp, không có đốm nhảy chỗ.

Chưa xem:
- 11 map còn lại, nhất là các màu sa mạc và tím.
- Willowbrook, nơi cổng trái có ánh sáng sát mép (0,6 ô) vì ô bên trong bị chặn.
- Đi bộ qua cổng thật.
- Online, Android.
- Đo FPS trên điện thoại.

Ở Rootburrow, cột sáng nằm gần cụm nút góc trên-phải (camera chạm mép map). Không bị che hẳn.

## Kiểm tra

- `dotnet test Tests/dotnet/ZenoniaSkills.Tests`: 437 đạt.
- `dotnet test Tests/dotnet/ZenoniaNet.Tests`: 452 đạt, có 4 test mới cho cổng (2 test chạy trên mọi map).
- Không đổi số liệu skill hay protocol, không cần build lại server.

## Texture tạm (Python) cần ChatGPT vẽ lại sau

Nền đen phẳng, chỉ xám, không trong suốt, không ô caro. Có ảnh rồi thì chạy `python Tools/build-light-skill-textures.py import <tên> <ảnh>`.

1. `portal-veil` 128x256: dải sáng dọc rộng, mềm, liền mạch theo chiều dọc. Không có tia mảnh.
2. `portal-mote` 64x64: đốm sáng tròn mềm, lõi sáng nhỏ, không có tia loé.

Hai texture dùng chung với Ice Storm (`noise-clouds`, `beam-streaks`) đã nằm trong danh sách của báo cáo `light-skill-fx-261119.md`.

## Câu hỏi chưa chốt

1. Người dùng duyệt hình cột sáng chưa? Lần trước cột sáng B đã được chọn, rồi sau đó thử sương C.
2. Có cần thêm sương mờ cuối đường (bản C) cho cổng sát mép không? Hiện bản mới thay hẳn sương.
3. Hạt trên tuyết gần như không thấy. Có cần làm đậm hơn, hay chỉ cần cột sáng là đủ?
