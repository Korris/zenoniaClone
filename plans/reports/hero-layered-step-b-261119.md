# Nhân vật nhiều lớp — bước B (19/11/2026)

Branch `worktree-agent-aa6958ffb93e4c411` (gốc `unity` 8c7bf5de + merge afa11f6c). Exe test: `D:\temp\player-layered\Zenonia.exe`. Spec: `docs/character-appearance-layered-spec.md`.

## Đã làm
1. **Shader** `Assets/Resources/Shaders/HeroRecolor.shader` (Built-in, sprite: vertex colour, premultiplied alpha, SpriteRenderer + MPB). `_MainTex`, `_MaskTex` (R tóc, G áo), `_HairColor`, `_OutfitColor`, `_HairAll` (=1: cả sprite là lớp tóc, pixel xám = tóc, phụ kiện có màu giữ nguyên, không cần mask). Dốc màu theo độ sáng xám: `#242424` → màu×0,12-0,24 (viền vẫn tối), `#555555` → bóng, `#999999` → đúng màu bảng, `#E0E0E0` → sáng (38 % về trắng). Pixel ngoài mask không đổi. Cùng công thức trong `Tools/hero_recolor_core.py` (sửa thì sửa cả hai).
2. **Tool** `Tools/build-hero-recolor-mask.py`:
   - `convert SRC OUT OUT-mask [--hair]`: nền `#00FF00` → trong suốt (khử viền xanh); hồng tím (hue 285-320°, S > 0,30, V > 0,18) → xám `#555555..#E0E0E0` theo độ sáng + mask G; `--hair`: pixel xám (chênh kênh ≤ 30) → mask R.
   - `selftest`: hình test tổng hợp → `docs/reference/hero-recolor-test/` (`source-grey-mask.png`, `hair8-x-outfit6.png`, `closeup-4-picks-3x.png`). Đã xem: 48 tổ hợp đúng màu, bóng giữ, viền/da/má/da thuộc không đổi, không rò mask (leak 0).
   - `test-sheets`: bộ test cho game (`Sprites/hero-layered/test/`, `Data/hero-layered/test/`). Module: `hero_recolor_core.py`, `hero_layered_test_figure.py`, `hero_layered_test_sheets.py`.
3. **Bảng màu** `Assets/Resources/Data/hero-layered/palettes.json`: 8 tóc (nâu hạt dẻ `#8A4E2E`, đen than `#34303A`, vàng mật ong `#E2A63E`, bạch kim `#E6DFD2`, đỏ son `#D2402F`, xanh biển nhạt `#5DA6D6`, tím oải hương `#A68AD6`, hồng anh đào `#EE8DB0`), 6 áo (đỏ son `#C9412F`, lam hoàng gia `#3B5FBF`, rêu `#6F8B3B`, mù tạt `#D3A22C`, nâu đất `#A9744A`, tím mận `#7F3B70`). Chỉ thêm cuối, không đổi thứ tự (index đã lưu).
4. **CharacterAppearanceData** (`Assets/Scripts/Rpg/CharacterAppearanceData.cs`, thuần C#): `classId` (không ghi, lấy từ save), `stored`, `hairStyle`, `hairColorIndex`, `outfitColorIndex` (-1 = chưa chọn), `equippedHeadgearId` (0 = đầu trần; client không bao giờ gửi, để code rơi đồ nối sau).
   - Lưu: `PlayerState.Appearance`, `SaveData.appearance` (không tăng SaveVersion: save cũ không có / `stored=false` = chưa chọn). server-api: `stateSchema.appearance` (zod, tuỳ chọn, kiểm phạm vi), hằng `APPEARANCE_HAIR_COLORS/OUTFIT_COLORS`.
   - Mạng: `NetAppearance` +3 byte `HairColor`, `OutfitColor` (255 = chưa chọn), `Headgear`; `InvOp.Appearance = 26` (ItemUid `"style,hair,outfit"`, server kiểm, lưu ngay, phát lại cho người xung quanh); `InvOp.HairStyle` nay cũng ghi vào save. **`NetProtocol.Version` 23.**
   - Chưa chọn (save cũ, swarm bot): chọn theo hash FNV-1a tên nhân vật (không phân biệt hoa thường; không có tên thì NetId) → giống nhau mọi client, không đổi khi qua map (đã thấy Swarm1 cùng màu ở 2 ảnh).
   - Chuyển từ `CharacterLookPrefs`: lần đầu nhận RpgState, máy còn prefs → gửi style như cũ + màu dye gần nhất trong bảng 8 màu (dye -1 = không chọn màu). Máy mới không có prefs → hiện style đã lưu; không gửi style trước khi biết look đã lưu (tránh ghi đè style 0).
   - Áp dụng: hero mình (offline từ save, online từ RpgState), proxy (`ProxyLook`), paper doll cũ của người khác nhận màu tóc đã CHỌN qua `HairColor` (hero mình vẫn dùng dye trên máy).
5. **Renderer nhiều lớp**: `HeroLayeredSet.cs` (đọc json, cache), `HeroLayeredView.cs` (vẽ), `HeroRenderer.Layered.cs` (nối vào renderer, tái dùng 8 hướng Dir8 của path Soft). Thứ tự: vũ khí sau, tay phụ sau, thân (mask + shader), tóc, mũ, vũ khí trước, tay phụ trước, nắm tay. Thân / nắm tay / tóc / mũ hướng trái = lật hàng phải; vũ khí theo neo riêng từng hướng nên kiếm luôn tay phải. Idle thở như Soft. Đám đông (mọi hero trừ mình, chỉ khi vẽ nhiều lớp): lệch nhịp bước đi + pha thở, cỡ 0,96-1,04 theo seed. Warrior vẫn chạy sheet RO Soft.
6. **Nhận diện mình** (người dùng chốt 05:00, sprite-rules đã đổi): `PlayerIndicatorRing.cs` (online, chỉ hero mình: hai đường elip mảnh màu bạc hà, không tô, thở chậm; khác vòng buff vàng/xanh tô đặc), xếp cùng bóng nên thân + chân vẽ đè lên. Vòng vàng cũ trong `LocalHeroMarker` bỏ (trùng màu buff Cleric), chữ V đổi bạc hà, dời lên +38. `NameTags`: tên mình xanh lá sáng, luôn hiện, các tên khác tránh nó.

## Quy cách art (bước C/D phải theo)
- `Sprites/hero-layered/<bộ>/body/<anim>.png`: hàng = 5 hướng vẽ (xuống, chéo xuống-phải, phải, chéo lên-phải, lên), cột = frame, ô 320×256, chân hàng 244, người 200 px. `<anim>-mask.png` cùng lưới (R tóc, G áo, nền đen). `body/hand/<anim>.png` nắm tay (tuỳ chọn).
- `hair/<kiểu>.png`, `headgear/<id>.png`: 5 ô một hàng; điểm neo (`ax, ay`) = đỉnh sọ (tóc) / điểm ngồi giữa mép dưới (mũ), `refW` = bề ngang sọ lúc vẽ, mũ có `drop` (tỉ lệ w kéo xuống từ đỉnh sọ).
- `Data/hero-layered/<bộ>/body.json`: `cellW, cellH, feetY, bodyPx, sheets, anims[{name, frames}], head[{anim, dir 0-4, f, x, y, w, angle}]`.
- `class.json`: `body`, `hair[]`, `headgear[]`, `weapons[{part, sheet, gx, gy}]` (vũ khí dựng đứng, điểm cầm), `grips[{part, anim, dir 0-7 thứ tự Dir8, f, x, y, angle (độ, theo chiều kim đồng hồ, 0 = dựng), behind}]`.
- Tên anim game không có trong bộ → idle / walk / `attack<số cuối>` / attack đầu tiên.

## Bật / tắt
- `--layered-test`: mọi hero KHÔNG phải của mình (proxy, bot, party) vẽ bằng bộ test. Không cờ = như cũ.
- `--class warrior --record-layered --shot-path DIR [--zoom Z]`: cảnh chụp 8 hero test quanh Warrior.
- `Tools/net-hair-crowd-shots.ps1 -ViewerArgs @("--layered-test")` (thêm tham số; máy chỉ có SDK 9 thì đặt PATH tới `C:\Users\korri\.dotnet` vì nhánh build net9 của script lỗi).

## Kiểm tra / build
- `ZenoniaSkills.Tests` 374/374 (thêm 14 test `CharacterAppearanceTests`: lưu/đọc, save cũ, `stored=false`, phạm vi, hash cố định, phân bố, wire, text InvOp, chuyển prefs, số màu trong palettes.json). `ZenoniaNet.Tests` 422/422 (NetAppearance 3 byte mới, InvOp.Appearance, Version 23). server-api vitest 158/158 (thêm `test/appearance-schema.test.js`).
- Build bản sao `D:\temp\zu-copy-layered` → `D:\temp\player-layered\Zenonia.exe`: Succeeded, 0 lỗi.

## Ảnh (đã xem từng ảnh)
`docs/reference/hero-layered-ingame/`:
- `layered-idle-dirs.png`: 8 hero test, 8 màu tóc / 6 màu áo khác nhau, 8 hướng, mũ trên 2 người, kiếm tay phải, khiên tay trái; Warrior soft ở giữa không đổi.
- `layered-idle-same.png`: cùng hướng chéo xuống-phải để so màu.
- `layered-walk-2.png`, `layered-attack-2.png`, `layered-attack-4.png`: đi chéo xuống-trái, đánh (kiếm đổi góc theo neo). Thấy lệch cỡ nhẹ giữa các hero.
- `warrior-regression-before-vs-new.png`: Warrior exe cũ (`player-ro-hero`) | exe mới, cùng lệnh: hình nhân vật như nhau (khác HUD vì exe cũ chưa có menu RO).
- `online-layered-test-crowd-ring-green-name-1/2.png`, `online-own-ring-and-green-name-3x.png`: online thật (zone server + 12 swarm bot + client `--layered-test`): bot vẽ nhiều lớp màu theo tên, tên người lạ trắng, tên mình "Viewer" xanh lá, vòng bạc hà dưới chân thấy rõ, chân không bị che.
- `docs/reference/hero-recolor-test/`: ảnh thử dốc màu.
- Chưa xem: chết / bị đánh / lướt trên path nhiều lớp (lướt chưa có vệt mờ cho lớp mới), vòng + chữ V khi bị người đứng trước che, chuyển prefs trên tài khoản thật (cần server-api + login), host mode.

## Ảnh hưởng server / phát hành
- Protocol 23: **phải build lại zone server** (dedicated) cùng client; client 22 bị từ chối.
- server-api: deploy bản mới để giữ `appearance` (bản cũ chỉ bỏ field đi, không lỗi, nhưng look không được lưu). Không deploy gì trong việc này.

## Câu hỏi còn mở
- Vòng định vị / tên xanh hiện cả lúc chơi offline không? (đang: chỉ online, như marker cũ.)
- Party có hiện tên không (sprite-rules ghi "party chưa chốt"; đang giữ: không hiện).
- Bảng màu: người dùng duyệt 14 màu trong `hair8-x-outfit6.png`? "nâu đất" áo hơi gần màu da thuộc của thắt lưng.
- Phím H (đổi kiểu tóc) online nay được lưu server, nhưng lần đăng nhập sau prefs trên máy (lúc tạo nhân vật) lại ghi đè như trước. Có muốn H ghi luôn vào prefs không?
