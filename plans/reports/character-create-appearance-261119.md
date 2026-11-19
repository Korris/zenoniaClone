# Chọn ngoại hình khi tạo nhân vật (19/11/2026)

Branch `worktree-agent-a6b71b4cadfd8099a` (gốc `unity` e33a83b4, đã merge `unity` 47337e69 = bảng màu RO Soft mới). Exe: `D:\temp\player-create\Zenonia.exe` (bản sao `D:\temp\zu-copy-create`, Library lấy từ `zu-copy-layered-d`; Succeeded, 0 lỗi). Spec: `docs/character-appearance-layered-spec.md` bước E.

## Luồng
- **Online**: Chọn class (hero select online) -> màn "CUSTOMIZE AND NAME" (đã có từ trước, làm lại): chọn màu + đặt tên -> CREATE gửi `appearance` cùng request.
- **Offline / host**: hero select -> START trên class CHƯA có save -> màn "CHOOSE YOUR LOOK" -> CONFIRM sinh hero (`GameSession.Spawn(id, look)`), save mới ghi ngay. Class đã có save: vào thẳng như cũ. Cũng chạy từ màn đổi hero trong game (HeroChangeScreen); Back về hero select đúng class đang chọn.
- Màn chung (`CharacterCustomizeScreen`, 2 chế độ): 8 ô màu tóc + 6 ô màu áo (tên lấy từ `palettes.json` `hairNames/outfitNames`, không hardcode hex), RANDOM, hero layered sống trên bệ: nút `<` `>` xoay 8 hướng, tab IDLE / WALK / ATTACK (combo 1-2-3 lặp). Dòng chọn kiểu tóc chỉ hiện khi class có >1 kiểu (hiện mỗi class 1 kiểu: ẩn; plumbing `hairStyle` giữ nguyên). Không chọn mũ: bắt đầu đầu trần (`equippedHeadgearId = 0`).
- Mở màn: màu ngẫu nhiên sẵn, người chơi đổi.

## Code
- `Rpg/CharacterAppearanceData.cs`: `ForNewCharacter(classId, style, hair, outfit)` (null nếu ngoài phạm vi), `RandomNew(classId, styleCount, rng)`.
- `HeroPalettes.cs`: `HairName(i)`, `OutfitName(i)` đọc tên từ palettes.json.
- `HeroPreviewStage.cs`: vẽ hero layered + `SetAppearance` (trước đó là paper doll cũ), `SetPose(slot, dir8, motion)`, `HairStyleCount(class)`. Danh sách nhân vật online cũng dùng nên nay hiện đúng màu đã lưu (hero cũ chưa chọn: màu theo hash tên như trong game).
- `Net/Client/CharacterCustomizeScreen*.cs`: làm lại bằng kit RO Soft (`RoSoftSprites`: panel, slot, slot-selected, btn-mint/sky/gold/ivory, tab-on/off, divider-gem). Helper `CharacterFlowKit.RoPiece/RoText/RoButton`.
- `AccountFlow`: `PendingLook` thay `PendingStyle/PendingDye`; `Create(name, class, look)`; không ghi `CharacterLookPrefs` cho hero mới nữa. `ApiClient.CreateCharacterAsync(name, classId, look)` (look null = request cũ).
- `HeroController.Look.OnOwnAppearance`: chỉ chuyển prefs cũ khi server CHƯA có look (`!saved.stored`) -> look lưu server luôn thắng, sửa luôn câu hỏi bước B "prefs ghi đè phím H".
- `HeroSelectScreen`: START class chưa save -> `CharacterCustomizeScreen.OpenOffline`; ẩn khi màn ngoại hình mở; `ReturnTo(class)`. `GameSession.Spawn(id, newLook)`: chỉ dùng look khi chưa có save.
- `NameTags`: không vẽ tên khi HeroChangeScreen mở (trước đó tên "Warrior" đè lên hero select / màn ngoại hình).
- Nhỏ, ngoài renderer: `HeroLayeredSet.HairStyleCount`, `Dir8Util.ToCardinal`. Không sửa HeroRenderer*/HeroLayeredView.
- server-api: `createAppearanceSchema` (tuỳ chọn, strict: `hairStyle` 0-255 mặc định 0, `hairColorIndex` 0-7, `outfitColorIndex` 0-5, không nhận mũ/-1), `characters.create` ghi `state.appearance {stored:true,...,equippedHeadgearId:0}`, summary danh sách có `appearance`.
- Capture: `--appearance-test --shot-path DIR` (`DebugCapture.Appearance.cs`, bấm nút thật qua FakeInput); `--login-shots` thêm bước CREATE + kiểm look trên server; `--hero-select-test` bấm CONFIRM khi gặp màn ngoại hình.

## Kiểm tra
- `ZenoniaSkills.Tests` 420/420 (thêm `CharacterCreateAppearanceTests`: phạm vi, random đủ màu, JSON server -> PlayerState, save offline giữ look, palettes có tên đủ). `ZenoniaNet.Tests` 422/422 (SDK 10).
- server-api vitest 182/182 (thêm `test/character-create-appearance.test.js` 14 test: lưu, mặc định style, list + GET, không gửi = không look, 9 ca 400 không tạo nhân vật, redeem tới zone + save vẫn giữ).
- `--appearance-test`: 20 PASS, 0 FAIL (bấm swatch, Random đổi look, Confirm sinh đúng class + đúng màu trên renderer, state lưu stored không mũ, save round trip, Back về hero select giữ class, Confirm đóng màn đổi hero).
- `Tools/character-flow-shots.ps1` (mongod + server-api thật, không zone): PASS "created hero keeps the picked look on the server: 0,4,2". 2 FAIL "in the world" / "account menu" là do script không chạy zone (đã ghi trong script là sẽ hỏng).

## Ảnh (đã xem) `docs/reference/character-create-appearance/`
- Warrior (nam): `01` random, `02` đỏ san hô + xanh trời, `03` xoay hướng lên-trái, `04` đi lên-phải, `05/06` đánh (vàng mật ong + ngọc bạc hà), `07` Random (hồng sakura), `08` oải hương + tím mận, `09-*` trong game Frost Hollow: tóc oải hương, áo tím mận, idle / đi / đánh.
- Cleric (nữ): `10` hồng sakura + đỏ san hô, `11` bạc + xanh xô thơm, `12` đi sang phải, `13` xanh trời + vàng ấm, `14-*` trong game đúng màu.
- Online: `online-7b-*` màn tạo có tên, `online-7c-*` danh sách: Frostwind tóc đỏ san hô áo xanh xô thơm, warrior không chọn hiện màu theo hash.
- Chưa xem: hero online trong zone thật (script này không chạy zone; dữ liệu đi tới zone đã kiểm bằng test redeem + test SaveSerializer), host mode, Fighter (không có art layered: preview paper doll, chỉ màu tóc có tác dụng), màn hẹp < 1138 px.

## Art UI còn thiếu (đang mượn mảnh gần nhất, không vẽ code)
1. Nút mũi tên xoay / chuyển trang: dùng `btn-ivory` (xoay) và `btn-sky` (kiểu tóc) + chữ `<` `>`.
2. Ô màu tròn trong kit RO Soft: dùng `character-flow/swatch` (đĩa trắng) tô màu, đặt trong `slot` / `slot-selected`.
3. Ô nhập tên kiểu RO Soft: vẫn dùng `chat-input` tối cũ.
4. Bệ đứng, cờ class, nền: vẫn art hero-select / character-flow cũ (khác tông với kit RO Soft).
5. Dải tiêu đề màn (góc trái trên) vẫn là nền tối code có sẵn từ trước (`CharacterFlowKit.Title`); có thể thay bằng `window-header`.

## Việc chưa làm / ghi chú
- Đổi ngoại hình 1 lần cho nhân vật cũ từ Settings: CHƯA làm (server đã có `InvOp.Appearance`; cần nút + cờ "đã đổi" trong save). Nhân vật cũ giữ màu theo hash tên.
- Ảnh chân dung HUD (góc trái) vẫn là portrait cũ, không theo màu tóc.
- Hero tay không trên người (vũ khí layered chưa có grip, agent khác đang làm).
- Deploy: server-api bản mới để lưu look khi tạo (bản cũ bỏ qua field `appearance`, nhân vật vẫn tạo được nhưng không có look). Không cần đổi protocol / zone.

## Câu hỏi
- Có làm nút "đổi ngoại hình 1 lần" trong Settings cho nhân vật cũ không?
- Màn mở với màu ngẫu nhiên hay cố định (nâu hạt dẻ + đỏ san hô)?
