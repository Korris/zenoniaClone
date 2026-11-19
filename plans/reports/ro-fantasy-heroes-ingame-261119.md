# RO Fantasy heroes vào game (19/11/2026)

Branch worktree `worktree-agent-ac605c5222b3e999a`, gốc `ro-fantasy-heroes` 1ddbb3ce. Exe: `D:\temp\player-rofantasy\Zenonia.exe`. Bản sao build: `D:\temp\zu-copy-rofantasy`. Build Succeeded, 0 lỗi.

## Pipeline
```
python Tools/build-ro-fantasy-hero-game-assets.py write   # thân từng class + mask rỗng + 3 kiểu tóc + body.json / class.json
python Tools/build-ro-fantasy-hero-game-assets.py fists   # dò nắm tay, ảnh lưới 4x docs/reference/ro-fantasy-heroes-ingame/fists-<class>-<anim>.png
python Tools/build-ro-fantasy-hero-game-assets.py grips   # lớp nắm tay + neo vũ khí 8 hướng
python Tools/build-ro-fantasy-hero-game-assets.py check   # ảnh ghép như game docs/reference/ro-fantasy-heroes-ingame/<class>-<anim>.png
```
- Có thể thêm tên class ở cuối, ví dụ `write cleric`.
- Module (mỗi file dưới 200 dòng): `ro_fantasy_body_cut.py`, `ro_fantasy_hair_cells.py`, `ro_fantasy_grips.py`, `ro_fantasy_ingame_check.py`.
- Dùng lại: tách nền, khớp tóc theo da của tool check đợt A, `head_anchor` của thân chung, `hero_layered_fists` và `hero_layered_grip_poses` (tư thế cầm), `hero_layered_weapons_compose` (ghép ảnh).

### Thân
- Ô 320x256, chân ở hàng 244, đầu trọc tới đế cao 188 px (như thân chung). 5 hàng vẽ, game lật 3 hướng trái.
- Đồ giữ màu thật. Mask rỗng và `fixedOutfit: true` trong class.json.
- Xoá chấm xanh nền còn sót trong hình. Mắt xanh lá của Mechanic và Cleric được giữ.
- Neo đầu: đỉnh và bề ngang sọ (khoảng 76 px).

### Tóc
- 3 kiểu cho mỗi class, file `hair/0..2.png`.
- Khớp vào đầu từng hướng theo vùng da.
- Xám ép về trung tính, kéo lên dốc màu của shader. Viền tối nâu của tóc cũng đưa vào lớp tóc.
- Hướng xuống và chéo xuống: bỏ phần tóc sau lưng nằm dưới cằm.
- Phụ kiện:
  - Kính Mechanic tách sang `accSheet`, ẩn khi đội mũ.
  - Băng đô và nơ đỏ Assassin giữ màu, nằm ngay trong lớp tóc.
  - Khăn che mặt Assassin không bị tính là tóc.

### Vũ khí
- Nắm tay idle đọc trên lưới 4x, ghi vào `OVERRIDES` (25 frame).
- Găng Assassin tối và găng Cleric sáng màu không dò tự động được.

### Đi và đánh
Theo quyết định của người dùng lúc 12:25: mỗi class chỉ có MỘT đòn `attack`, cộng với `walk`.
- File nguồn: `src/body-<class>-<walk|attack>-<a>--<b>-chatgpt-N.png`, 2 hàng x 4 frame.
- Anim chỉ được cắt khi có đủ 5 hướng.
- Đã chạy thử với file giả, cắt đúng 5x4.

## Code
- `HeroRenderer.LayeredSetFor`:
  - Mặc định dùng `Data/hero-layered/ro-fantasy/<class>/class` nếu có.
  - `--hero-style layered-common`: thân chung theo giới.
  - `--hero-style classic` và `rosoft`: như cũ.
- `HeroLayeredSet.FixedOutfit`.
- `HeroRenderer.Layered`:
  - Class RO Fantasy dùng `attack` cho mọi nhát combo và tư thế skill.
  - Khi set chưa có walk/attack: vẽ frame đứng trong 4 frame giả.
    - Đi: nhún 3 px, nghiêng ±2,5°.
    - Đánh: ngả ra sau rồi lao tới theo hướng nhìn, nghiêng tới 8°.
  - `HeroLayeredView` có thêm gốc `L_Root` để xoay và dời cả hình.
- Màn CHOOSE YOUR LOOK / CUSTOMIZE:
  - Class có `fixedOutfit` thì ẩn khối màu áo.
  - Dòng KIỂU TÓC (`<` `>`, "Hairstyle n / 3") chuyển vào bảng màu, dưới tiêu đề HAIRSTYLE.
  - Chỉ dùng sprite kit RO Soft có sẵn.
- `hairStyle` vốn đã được lưu trong `CharacterAppearanceData`, không đổi save hay protocol.
- Capture:
  - `--appearance-test`: kiểm thêm việc ẩn màu áo, có 3 kiểu tóc, nút `>`, kiểu tóc được lưu.
  - `--record-layered-real` và `--record-layered-farm`: kiểu tóc theo `i % 3`; hàng trước của layered-real đầu trần.

## Kiểm tra
- `ZenoniaSkills.Tests`: 426/426. Có thêm `RoFantasyHairStyleAppearanceTests`: kiểu tóc qua save, wire, op text, random đủ 3 kiểu, đủ 5 class.json có 3 kiểu và `fixedOutfit`.
- `ZenoniaNet.Tests`: 422/422 (SDK 10).
- Không đụng server-api nên không chạy vitest.
- `--appearance-test`: 30 PASS, 0 FAIL.

## Ảnh (đã xem)
- Offline `docs/reference/ro-fantasy-heroes-ingame/<class>-idle.png`: 5 class. Mỗi ảnh có 3 kiểu tóc x 3 màu x 8 hướng, kèm vũ khí, thêm hàng mũ class và mũ rơm.
  - Tóc đúng chỗ, nhuộm sạch.
  - Kính và băng đô giữ màu thật.
  - Kính Mechanic ẩn dưới mũ.
- Trong game `docs/reference/ro-fantasy-heroes-ingame/ingame/`:
  - `create-warrior-hairstyle-1..3`, `create-cleric-hairstyle-1..3`, `create-warrior-honey-blonde-attack-preview`: màn chọn, đổi kiểu tóc và màu, khối màu áo đã ẩn.
  - `world-warrior-walk-right-stand-in`, `world-cleric-idle`.
  - `crowd-idle-8-dirs-hairstyles`, `crowd-walk-right-stand-in`, `crowd-attack-left-stand-in`: 10 hero, 5 class, nhiều kiểu tóc, màu và mũ.
  - `farm-crowd-10-heroes`, `farm-crowd-hud`: đám đông vây quái.

## Còn thiếu / lỗi biết
- **Chưa có art đi và đánh.** Game đang vẽ frame đứng có nhún và nghiêng. Vũ khí giữ tư thế đứng, không vung theo đòn.
  - Khi có file: chạy lại `write`, rồi `fists` (thêm `OVERRIDES` cho frame mới, đọc trên ảnh lưới), `grips`, `check`.
  - Tool giả định 4 frame mỗi hàng. Nếu art có số frame khác thì phải sửa `MOTIONS`.
- **Kiếm Warrior** ở idle hướng xuống và chéo xuống nằm gần ngang, mũi ra khỏi ô. Lý do: nắm tay thấp, kiếm dài 150 px, luật không cho mũi cắm đất. Đã ghi Sổ lỗi.
- **Kiểu undercut Mechanic:** phần cạo cũng nhuộm màu tóc (art vẽ xám). Chấp nhận.
- **Chưa xem trong game:** online thật (proxy, zone), túi đồ, chết, bị đánh, lướt.
- **Chân dung HUD** vẫn là portrait cũ.
- **Sprite mới thêm sau lần chép đầu:** `build-from-copy.ps1` không chép `.meta`. Ảnh mới thêm phải chép tay cả `.meta` vào bản sao.

## Câu hỏi
1. Kiếm idle nằm ngang ở hướng xuống có chấp nhận không, hay đổi tư thế: dựng kiếm, vác vai, hoặc kiếm ngắn hơn?
2. Khi đội mũ có cần ẩn băng đô Assassin không? Hiện băng đô nằm trong lớp tóc, mũ không ẩn tóc thì băng đô vẫn hiện.
3. Đòn `attack` duy nhất có mấy frame? Tool đang chờ 4.
