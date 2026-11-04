# Fighter: 10 tier găng (phase 3), 2026-11-04 00:30-01:15

Plan: `plans/261104-0025-fighter-class/phase-03-glove-tiers.md` (đã cập nhật, trạng thái xong). Chưa commit, chưa build.

## Đã làm
- Vẽ 10 tier ở một chat mới trên tài khoản Tay ("Vẽ găng boxing", https://chatgpt.com/c/6aea18f3-4748-83ec-b4f7-379f0507523f): ảnh đầu tải kèm `custom/hero-select/boxing.png` làm mẫu kiểu vẽ, mỗi tier một prompt một dòng, Normal trái, Enhanced phải, nắm đấm hướng PHẢI, nền #00FF00. 10 lượt, không tier nào phải vẽ lại; không tier nào xanh lá nên không cần nền magenta. Đã mở xem từng ảnh trước khi cắt.
- Nguồn: `Assets/ArtLibrary/classes/fighter/new-style/glove-src/<tier>-pair-chatgpt.png` (10 file, bản gốc 1536x1024 trong `Downloads/fighter-glove-<tier>.png`).
- Tool mới `Tools/build-fighter-gloves.py` (`check OUT`, `install [tier...]`, `icons`, `sheet OUT`), chép cách `key_green`/`cut_pair`/`install_icons`/`new_guid_copy` của tool Assassin:
  - Tách nền xanh (ngưỡng dư xanh, giữ mảng từ 400 px, nở 3 px để giữ viền sáng), ghép hai mảng lớn nhất sau khi nở 12 px để tia lửa rời vẫn theo găng.
  - Bản thường co về 170 px bề ngang đặc trên canvas 200x200, bản cường hoá cùng hệ số, đặt chồng lên bản thường bằng dò hình bóng (`best_overlap`) nên đổi +N găng không nhảy.
  - Grip = trọng tâm phần nắm đấm (60% bề ngang bên phải), cùng điểm cho hai bản; cổ găng dài thì dời vào trong, chừa 3 px mép.
  - Icon 96x96, khung 86 px, không xoay; `offhand-*` lật ngang (găng trái).
- Cài: 20 png + `.meta` ở `Assets/Resources/Sprites/classes/fighter/weapons/glove/`, 20 icon + `.meta` `Sprites/icons/weapon-fighter-0..9.png`, `offhand-fighter-0..9.png`, folder meta `classes/fighter.meta`, `fighter/weapons.meta`, `weapons/glove.meta` (GUID mới, chép từ dao Assassin). `held-fighter.json`: chỉ `baseGrip`/`enhancedGrip` đổi, `glow` giữ của phase 1, `anchors` không đụng (lúc ghi còn rỗng), giữ định dạng một dòng.
- Grip: leather (122,100), iron (121,100), bronze (123,100), tiger (114,100), flame (121,100), frost (121,100), thunder (125,100), shadow (119,100), holy (126,100), dragon (117,100).

## Đã xem bằng mắt
- 10 ảnh ChatGPT ở cỡ 1/2: cả hai bản nhìn ngang, khớp ngón hướng phải, ngón cái trên, thiết kế 10 tier khác hẳn nhau (găng da, găng sắt, cestus đồng lộ da tay, vuốt hổ lông, găng lửa đúng hình màn chọn tướng, pha lê băng, cuộn sét cơ khí, gai đen khói tím, cánh trắng vàng có hào quang và ngọc, đầu rồng há miệng).
- `check` 2x từng tier và `sheet` nền tối, nền sáng: cắt sạch, không viền xanh, grip nằm trên nắm đấm, bản cường hoá trùng vị trí bản thường; dải icon 2 hàng đọc được ở 96 px.
- Chưa xem trong game: chưa có sheet thân và anchor (phase 2).

## Hạn chế còn lại
- `dragon` cường hoá rộng 220 px, lửa phun bị cắt 20 px ở mép canvas.
- `flame` (lửa tính vào bề ngang), `thunder`, `holy` (cẳng tay dài): nắm đấm nhỏ hơn `leather` khoảng 20%; nếu trong game thấy lệch cỡ thì đổi `GLOVE_W` theo tier hoặc đo theo bề ngang nắm đấm.
- `bronze` là cestus lộ da tay, màu da của găng không theo nhân vật.
- Unity sẽ tự sinh `.meta` cho `glove-src/*.png` khi mở project (chưa có).

## Sổ lỗi
Đã thêm 4 dòng ngày 2026-11-04 (Fighter) vào `docs/sprite-weapon-rules.md`: grip trọng tâm toàn găng lệch về cổ tay; bản cường hoá lệch vì lửa; nhóm tab mất giữa lúc vẽ; nút Send chưa dựng ngay sau khi điền chữ. `docs/pending-tasks.md` mục Fighter đã ghi chat, tài khoản, tình trạng.

## Câu hỏi chưa giải quyết
- `s` anchor khoảng 0,18 trong plan tính cho găng 170 px: nắm đấm của các tier cẳng tay dài sẽ nhỏ hơn trên tay, cần xem trong game rồi quyết định cỡ.
- Có muốn `bronze` đổi màu da theo hero không (hiện là màu vẽ sẵn).
