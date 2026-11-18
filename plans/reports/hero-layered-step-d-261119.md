# Nhân vật nhiều lớp, bước D: art thật vào game (19/11/2026)

Branch `worktree-agent-a130c6f285ca0c7c7` (gốc `unity` 05851b11, đã merge `unity` 60d3b56a = art đợt 3). Exe: `D:\temp\player-layered-d\Zenonia.exe` (bản sao `D:\temp\zu-copy-layered-d`, Succeeded, 0 lỗi).

## Pipeline (lệnh)
```
python Tools/build-hero-layered-game-assets.py write   # thân nam/nữ + tóc 5 class + json (~5 phút: dò khớp tóc)
python Tools/build-hero-layered-game-assets.py check   # ảnh ghép offline docs/reference/hero-layered-real/<class>-<anim|hats>.png
python Tools/build-hero-layered-headgear.py write|check # 19 mũ + icon + trường art của headgear.json; check = headgear-on-heads.png
python Tools/build-hero-layered-weapons.py write|check  # 8 vũ khí đúng cỡ spec + grip (chưa gắn lên người)
```
- Thân: lấy 5 hàng vẽ từ `ArtLibrary/hero-layered/sheets/<giới>/` (game tự lật 3 hướng trái). Frame đi canh lại theo tâm sọ (bản cắt cũ canh theo chân, người nhảy ngang 17-26 px). Áo hồng tím -> xám + mask (thêm viền khử răng cưa, hue tới 332). Đầu thắt lưng tô đỏ rượu -> màu da thuộc. Neo đầu từng frame (x, y đỉnh sọ; w = trung vị của hàng).
- Tóc: khớp ảnh tóc với đầu trọc idle bằng vùng da (một tỉ lệ cho cả ảnh, vị trí riêng từng đầu), tách tóc = khác đầu trọc + xám (kính Mechanic, nơ Assassin giữ màu), xám kéo về dốc màu shader (p3 -> 0,14, trung vị -> 0,60, p97 -> 0,88), dời/phóng nhẹ để phủ kín đỉnh sọ, cắt thành 5 ô neo đỉnh sọ (`refW` 75). Wizard dùng bản `-2` (mới nhất).
- Mũ: 2 hàng x 5 góc mỗi ảnh (một số ảnh 1536x1024: không giả định cỡ ảnh), tỉ lệ = khoảng cách góc 1->5 / sọ 1->5, đường ngồi = trung vị đáy, độ lún `DROP` từng mũ đọc bằng mắt. Mẩu rời < 3 % bị bỏ. Icon 96 px từ góc trước. `hidesHair` (khăn tu nữ, mũ phi công) ở `headgear.json` + `HeadgearDef.HidesHair`, test đối chiếu đã thêm.
- File: `Sprites/hero-layered/{male,female}/body/*`, `hair/<class>/0.png`, `headgear/<key>.png`, `weapons/*.png`, `icons/headgear-*.png`; `Data/hero-layered/{male,female}/body.json`, `<class>/class.json`, `weapons.json`. `.meta` chép từ sheet bước B (không nén, 4096; mask linear).

## Trong game
- Kiểu mặc định: nhiều lớp cho cả 5 class (hero mình, party, proxy, bot), cả ảnh túi đồ. Màu tóc/áo từ `CharacterAppearanceData` (mình: save; người khác: chọn theo hash tên), mũ khi đội. `--hero-style classic` = paper doll, `--hero-style rosoft` = Warrior RO Soft (đã chụp, đúng). Fighter không có art -> đường cũ, không cảnh báo.
- Anim: idle/walk; nhát combo 1/2/3 -> attack1/2/3, nhát 4 quay về attack1; skill/cast/xoay -> attack2 (frame dàn theo số frame sheet cũ); lướt = walk; bị đánh/chết như đường soft (idle + tô đỏ / mờ dần).
- Cờ chụp mới: `--class C --record-layered-real --shot-path DIR [--zoom Z] [--bot attack] [--wear-hat N]` (10 hero, 2 mỗi class, màu + mũ khác nhau).

## Ảnh (đã xem từng ảnh)
- Offline `docs/reference/hero-layered-real/`: `<class>-{idle,walk,attack1,attack2,attack3}.png` (8 hướng x mọi frame, 3 tổ hợp màu), `<class>-hats.png` (4 mũ x 8 hướng, idle + walk), `headgear-on-heads.png` (19 mũ x 5 góc), `weapons-sizes.png`. Tóc đúng chỗ mọi frame, kể cả đánh 3 frame 1 (tay giơ nằm sau đầu, tóc vẽ đè), tóc Cleric/đuôi ngựa Assassin đè cổ áo; không còn hồng tím (còn 58/126 px rải rác cả bộ, không thấy bằng mắt).
- Trong game `docs/reference/hero-layered-real/ingame/`: `warrior-crowd-idle-own-ring-green-name.png` (vòng + tên xanh của mình), `assassin-crowd-idle-hats.png`, `assassin-crowd-walk-right-hats.png`, `mechanic-crowd-attack-hats.png` (mình đội vương miện băng, đang đánh), `rosoft-style-same-scene-for-comparison.png`.
- Chưa xem: online thật (proxy/server), lướt, bị đánh, chết, skill pose, túi đồ với layered (code có, chưa chụp), hat id 9 (mũ phi công) trên người trong game (offline thì có trong `<class>-hats.png`).

## Còn thiếu / lỗi biết
- **Vũ khí chưa vẽ trên người** (TODO): sprite đúng cỡ đã có, thiếu grip từng frame (nắm tay + góc cho 5 anim x 8 hướng x 4 frame, theo quy tắc tư thế cầm). Hero đánh tay không; VFX đòn đánh vẫn ra như cũ.
- Đỉnh tóc dời lên tới 3 px để che viền sọ: gáy hướng lưng hơi lộ mép.
- Vương miện/kính đội trên khối tóc phồng thì hơi nổi (neo theo sọ, không theo tóc).
- Mũ Mechanic: kính trong lớp tóc vẫn hiện dưới mũ sắt / vương miện.
- Vương miện rắn/pha lê: nét mép xa còn trong hình góc chéo (nhỏ, không xoá).
- `FrostSmokeTest` (Unity batch) chưa chạy.

## Cần từ art mũ / vũ khí
- Vũ khí: không cần vẽ lại (cỡ chỉnh trong tool). Cần quyết tư thế cầm từng đòn trước khi đo grip.
- Băng đô vải xanh = băng đô ninja đổi màu (như báo cáo đợt 3).

## Kiểm tra
`ZenoniaSkills.Tests` 403/403, `ZenoniaNet.Tests` 422/422 (SDK 10).

## Câu hỏi
- Kính Mechanic có ẩn khi đội mũ trùm/mũ sắt không (cần tách kính khỏi lớp tóc)?
- Mũ nào nữa nên ẩn tóc (mũ len, mũ ếch, slime đang đè lên tóc, tóc lòi ra dưới)?
- Nhát combo 4 dùng lại attack1 có ổn, hay muốn vẽ đòn 4 riêng?
