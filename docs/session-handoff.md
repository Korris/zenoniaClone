# Bàn giao phiên làm việc — 2026-10-27

## Bắt đầu phiên sau

- Đọc `AGENTS.md` và file này; trả lời bằng tiếng Việt.
- Kiểm tra `git status --short` trước khi sửa. Workspace có rất nhiều thay đổi chưa commit từ trước và trong phiên này, cùng vài ảnh bị xóa từ trước. Không reset, ghi đè hay phục hồi các thay đổi đó tùy tiện.
- Chưa commit, push hay deploy. Không thực hiện các việc đó khi chưa có yêu cầu/cho phép.
- Không có task đang làm dở. Sau lúc ghi bàn giao ban đầu, người dùng tiếp tục yêu cầu sửa đi bộ và hiệu ứng vật phẩm; xem mục bổ sung cuối file.

## Kết quả và yêu cầu cần giữ

### Vũ khí và vật phẩm rơi

- Atlas `assets/custom/weapon-icons-chibi.png`: 30 mẫu, 10 mẫu/class Warrior (kiếm), Wizard (trượng), Mechanic (súng). Mỗi hình dùng chung cặp cấp 1/5, 10/15, 20/25…90/95.
- Bảng mô tả: `assets/custom/weapon-icons-chibi.json`; prompt: `assets/custom/weapon-icons-chibi-prompt.txt`.
- `src/graphics/weapon-icons.js`: tách 30 silhouette theo alpha vì khoảng cách hàng trong ảnh không đều; không cắt atlas thành ô bằng nhau. Chọn icon theo `itemLevel` và `classId`.
- Vũ khí mới có `classId` theo class người chơi lúc quái chết, được giữ qua save/load. Đồ cũ chưa có class dùng class người chơi khi hiển thị. Sanitizer item hỗ trợ đến cấp 95; MAX_LEVEL người chơi vẫn là 30, chưa mở nội dung/cân bằng game đến 95.
- Người dùng KHÔNG muốn thay vật phẩm dưới đất thành hình khác/icon vector đơn giản. Đã thử và bỏ hướng đó. Phải giữ đúng hình dáng/màu/đặc trưng ảnh chibi trong túi đồ.
- Bản hiện tại: `src/graphics/ground-sprite-builder.js` tạo bản nhỏ ~27 px từ ảnh gốc, thu nhỏ nhiều bước và lọc màu cục bộ để bớt lấm tấm. Registry tạo `groundItems` và `groundWeaponIcons` riêng, inventory vẫn dùng ảnh chi tiết. Renderer căn vị trí theo pixel canvas để bớt rung viền.
- Kích thước rơi hiện tại `DROP_SIZE = 6.6` world units (~26.4 px màn hình), tăng 10% từ 6. Áp dụng trang bị, HP/MP, vàng.
- Bình HP/MP dùng Canvas trong `src/graphics/potion-renderer.js`, dáng thân gọn/cổ ngắn.
- Đã KHÔI PHỤC tỉ lệ rơi bình thường: `GUARANTEED_WEAPON_DROPS = false` trong `src/systems/equipment-system.js`. Quái thường 12% rơi trang bị ngẫu nhiên, boss luôn rare/epic. Không bật lại 100% nếu người dùng chưa yêu cầu.

### Di chuyển và quái

- `src/systems/monster-ai-system.js`: `CHASE_SPEED_FACTOR = 0.5`, tốc độ đuổi bằng 50% tốc độ gốc. Lang thang vẫn 0.5; nhịp đánh và tốc độ về nhà giữ nguyên.
- `src/game/player-controller.js`: player gọi `facingFromVector(..., true)` để ưu tiên trái/phải khi đi chéo. Chỉ đi thẳng lên/xuống dùng hướng lên/xuống. Áp dụng cả đổi hướng khi chuẩn bị đánh. Quái vẫn dùng quy tắc hướng cũ.

### Combo và âm thanh — thay đổi mới nhất

- `src/data/hero-stats-config.js`: cả 3 class có 4 đòn. Đòn 1/2/3 thường; đòn 4 là finisher (giữ thông số finisher cũ, chèn một đòn thường ở nhịp 3).
- Melee xác định finisher theo `combo.config.hits.length`, không hardcode nhịp 3. Wizard/Mechanic chỉ nổ lớn ở nhịp 4.
- `src/graphics/player-renderer.js`: nhịp 1/2/3 dùng animation 1/2/1; nhịp 4 dùng animation 3 cũ (chưa cần vẽ sheet attack4).
- NPC hướng dẫn đã đổi thành nhấn J 4 lần.
- `src/audio/combat-audio.js`: Web Audio tổng hợp tiếng kiếm/phép/súng, tiếng trúng quái và tiếng hô võ thuật "Hah!" tổng hợp chỉ ở đòn 4. Không phải giọng người thu âm. Không có dependency mới, không gọi API.
- `src/main.js` mở AudioContext từ thao tác keyboard/pointer. `updatePlayer` phát sound ở hit event; `damageMonster` phát impact có giới hạn tránh chồng tiếng khi đánh diện rộng. Các module âm thanh không làm lỗi Node tests khi không có AudioContext.
- Chưa có phản hồi của người dùng về chất lượng tiếng hô hoặc icon nhỏ mới nhất; có thể cần tinh chỉnh sau khi họ chơi thử.

## Kiểm tra gần nhất

- `npm.cmd test`: **162/162 pass** sau thay đổi combo/audio.
- `git diff --check`: pass.
- `node tools/check-weapon-icons.mjs`: boot game, inventory và drops cả 3 class; đủ 30 ground icons kích thước 27 px; không có page error.
- `node tools/check-combat-audio.mjs`: 12 lần đánh (4 nhịp x 3 class), AudioContext chạy, tín hiệu âm thanh khác 0, chỉ đòn 4 có thêm oscillator giọng hô, không page error.
- Mẫu âm thanh: `tools/screenshots/combat-audio/four-hit-combos.webm`.
- Ảnh thử: `tools/screenshots/weapon-icons/{warrior,wizard,mechanic}-{inventory,drops}.png`.
- Browser scripts cần server ở `http://127.0.0.1:5173`, Chrome tại `C:/Program Files/Google/Chrome/Application/chrome.exe`. Chrome trong sandbox từng timeout; dùng quyền phù hợp theo môi trường, không lặp chạy mù.
- PowerShell đôi khi chặn `npm.ps1`; dùng `npm.cmd test`.

## Lưu ý phạm vi

- Workspace có sẵn thay đổi UI/inventory/potion/combat trước phần công việc này; xem diff trước khi sửa, không coi toàn bộ diff là của phiên này.
- Tài liệu roadmap/PDR cũ có thể vẫn mô tả combo 3 đòn; code và ghi chú này phản ánh trạng thái mới.
- Không cài thêm dependency, không sửa `.env`.

## Bổ sung sau lần bàn giao ban đầu: đi bộ và opacity vật phẩm

- Người dùng nhận xét đi bộ chỉ nhún vũ khí, tay không vung. Đã dùng image_gen vẽ lại walk cho cả 3 class, giữ nhân vật và trang phục, đổi sang tay/chân luân phiên rõ hơn và hạ vũ khí xuống khi đi.
- Nguồn mới: `assets/hd/{hero,wizard,mechanic}/walk-v2.png`; bản normalized 384x384 (4 hướng x 4 khung, mỗi khung 96x96): `assets/hd/{hero,wizard,mechanic}/normalized/walk-v2.png`.
- Atlas của cả 3 class đã trỏ đến `walk-v2.png`. Không ghi đè ảnh walk.png cũ. Các file `walk-v2-draft.png` là bản nháp trước khi sửa tay/vũ khí, không được load vào game.
- Builder riêng: `python tools/build-hd-walk.py` dùng lại pipeline cũ và idle để khớp cỡ đầu, chỉ ghi walk-v2 và metadata của nó. Không tái tạo các animation khác.
- Prompt/reference: `assets/hd/walk-v2-prompt.txt`.
- Người dùng nói nhấp nháy vật phẩm nghĩa là kiểu CSS opacity/color pulse, KHÔNG phải bật/tắt ẩn hiện. Đã bỏ toàn bộ logic blink theo continue, kể cả 3 giây cuối. `drawDrops` dùng opacity cosine 0.64–1, chu kỳ 900ms (350ms gần hết hạn); save/restore alpha để không ảnh hưởng nhân vật. Kích thước 6.6 giữ nguyên.
- Kiểm tra mới nhất: **163/163 tests pass**, git diff --check pass.
- `node tools/check-walk-animation.mjs`: 3 class, 4 khung/hướng, di chuyển trái/phải và render không page error. Ảnh so sánh: `tools/screenshots/walk-v2/old-vs-new.png` và ảnh gameplay mỗi class/hướng.
- `node tools/check-weapon-icons.mjs` đã bổ sung đo alpha thực tế trên canvas: sáng -> mờ (vẫn khác 0) -> sáng; không rò globalAlpha.
- Hai thay đổi này chưa có phản hồi chơi thử của người dùng. Không tự tuyên bố chu kỳ đi bộ đã hoàn hảo; nếu cần cải thiện tiếp hãy xem các khung/clip trước khi vẽ lại.

## Điều chỉnh mới nhất: vật phẩm tối–sáng thay cho opacity

- Người dùng không thích opacity và yêu cầu vật phẩm tối nhẹ rồi sáng về màu gốc. Quyết định này thay thế yêu cầu opacity ở trên.
- `drawDrops` giữ nguyên globalAlpha, dùng `ctx.filter = brightness(...)` trong khoảng 78–100%; chu kỳ 900ms, gần hết hạn 350ms. Vật phẩm không trong suốt hay biến mất. Save/restore filter để không làm tối nhân vật/cảnh.
- Đã commit và push các thay đổi trước đó lên origin/main bằng commit `d766525`. Điều chỉnh brightness này là công việc mới sau lần push đó; kiểm tra trạng thái Git trước khi commit tiếp.

## Công việc mới: nhặt từng món và thiết kế lại thông báo

- Đã sửa `updatePickups` để chọn món gần nhất đang chạm nhân vật, chỉ nhặt một món mỗi lần nhấn attack. Giữ nút không tự nhặt cả chồng. Thêm `nearbyDrop` / `lootFeedback` vào world, preview khi đứng vào đồ, xác nhận khi nhặt, lỗi đầy túi, reset khi chuyển map.
- Đã thêm test `tests/loot-pickup.test.js`: tổng **166/166 pass** ở thời điểm kiểm tra sau phần này. Browser script mới `tools/check-loot-pickup.mjs`, ảnh `tools/screenshots/loot-pickup/`.
- UI popup nhỏ ở giữa màn hình trong code hiện tại bị người dùng chê vì che hero. Người dùng yêu cầu GEN ẢNH XEM THỬ UI trước khi triển khai thay thế; chưa triển khai notification bên phải vào code.
- Khu thông báo dự kiến ở mép phải ngay dưới minimap, phía trên cụm nút chiến đấu. Màu navy, viền slate, font và phong cách phải đồng bộ UI hiện tại.
- Quyết định mới nhất: **CHỈ MỘT thông báo tại một thời điểm**, không xếp chồng 3 card. Tên đồ khi đi vào, đồ vừa nhặt và tiến độ quest (kill/material) dùng CHUNG một vị trí và lần lượt thay nhau. **Không hiển thị hướng dẫn J/pickup**.
- `docs/loot-notification-ui-draft.png` là mockup sai hướng (3 card cùng lúc), KHÔNG dùng làm đặc tả triển khai. Mockup sửa một card sẽ được lưu tại `docs/loot-notification-ui-mockup.png`; prompt ở `docs/loot-notification-ui-prompt.txt`.
- Chưa tự bổ sung hệ thống quest nhặt material chỉ vì mockup có ví dụ. Sau khi người dùng chấp thuận bố cục, cần nối event quest hiện có và xác định phạm vi material theo yêu cầu thực tế.

## Đã triển khai mockup một thẻ bên phải

- Người dùng đã yêu cầu "ok làm thử đi". UI chạy thật ở `src/ui/activity-notice.js`: x720/y158, 224x58 trên canvas960x640, dưới minimap, navy/slate đồng bộ inventory; có icon món đồ, nhãn NEARBY/PICKED UP/QUEST PROGRESS và tên. Không có J/pickup hint, không che hero.
- `src/systems/activity-notice-system.js` quản lý một thẻ hiển thị và hàng đợi event, gộp tiến độ mới nhất của cùng quest. Preview nhường chỗ cho sự kiện rồi hiện lại khi vẫn đứng gần đồ. World clear map/death xóa notification/queue.
- `quest-flow.js` nối tiến độ kill quest và trạng thái ready vào thẻ chung. `drawHud` bỏ tracker quest cũ tại cùng vị trí; xem danh sách quest qua Q vẫn hoạt động. Chưa bổ sung objective/material mới ngoài hệ thống kill/reach sẵn có.
- Thông báo nhặt đồ chính giữa đã bỏ khỏi world renderer. Banner lớn của level/map vẫn giữ nguyên.
- Kiểm tra mới nhất **168/168 tests pass**, `node tools/check-loot-pickup.mjs` pass gồm preview/pickup/quest, nhặt đúng một món mỗi press, giữ nút không nhặt cả stack, không page error.
- Ảnh thật mới: `tools/screenshots/loot-pickup/01-nearby.png`, `02-picked-up.png`, `03-quest-progress.png`.
- Những thay đổi sau `d766525` chưa được commit/push trong tác vụ triển khai UI này.

## Hiệu ứng đồ rơi / thu thập theo ảnh cc.png

- Người dùng gửi `cc.png` làm mẫu: quầng sáng sát đất, cột sáng mảnh, sao lấp lánh; khi nhặt có vòng lóe rồi hạt sáng bay lên/hút về hero. Chỉ đổi FX, giữ icon và UI một thẻ dưới minimap như đã chốt, không chuyển sang stack popup trong ảnh tham khảo.
- Đã dùng image_gen tạo `assets/custom/loot-effects.png`, nền alpha thật, 2 hàng x6 khung (idle và pickup). Prompt lưu `assets/custom/loot-effects-prompt.txt`.
- `src/graphics/loot-effects.js`: tách/tint 60 frame theo trắng, xanh lá, xanh dương, tím, vàng (tiền); idle glow sau shadow/trước item, cột sáng vẽ thêm sau props để không bị cỏ che; lõi tia mảnh được vẽ ở độ phân giải game để giữ nét. Không thêm rarity mới vào gameplay.
- `LootPickupEffect` lưu bản sao hình đồ đã nhận, hút/thu nhỏ về hero và tan trong 480ms; item thực được nhận đúng một lần ngay khi pickup thành công. Túi đầy không phát hiệu ứng/không mất đồ. Vẫn cần nhấn attack mỗi món, không đổi thành auto-loot khi chạm.
- `WorldState.lootPickupEffects` cập nhật/xóa cả khi hit-stop và clear map. Vật phẩm dưới đất vẫn brightness pulse 78–100%, không opacity.
- `node tools/check-loot-effects.mjs`: đủ60 frame tint, 5 màu, flash + absorption, không nhận trùng, hiệu ứng kết thúc và không page error. Ảnh và video tại `tools/screenshots/loot-effects/`, video `loot-effects.webm`.
- Bộ test tại thời điểm nối spritesheet: **170/170 pass**. Kiểm tra brightness trong `check-weapon-icons.mjs` đã tách FX nền khỏi phép đo vì beam có animation độc lập.
- Chưa commit/push các thay đổi mới này. Người dùng chưa chơi thử để góp ý độ sáng/độ dài tia.

## HiDPI và sprite 192px — công việc mới nhất

- Người dùng chê FX vẫn như khói, gửi `ab.jpg` đối chiếu trái (sai) và phải (muốn): tia dài mảnh, lõi trắng, quầng nhỏ/tia sao sắc, không mây sáng. ĐÃ TẠM DỪNG sửa FX theo câu "khoan" để xử lý vấn đề độ nét chung. Chưa tạo spritesheet thay thế, không coi FX cũ đã đạt.
- Người dùng đồng ý thử nâng render web trước khi cân nhắc Unity. Đã thêm `src/core/canvas-display.js`: giữ logic960x640, canvas vật lý theo devicePixelRatio (giới hạn2x), tự resize theo viewport/zoom/màn hình. Đã sửa transform world/screen trong main/controller/world-renderer, giữ UI logical.
- Input chuột dùng tọa độ logic960x640 thay cho kích thước buffer vật lý, nên không lệch click khi HiDPI hoặc resize.
- CSS bỏ `image-rendering: pixelated/crisp-edges`, dùng auto cho phong cách chibi vẽ mượt. Hero/camera căn tới pixel vật lý để tránh bước nhảy theo lưới world4px.
- `python tools/build-hd-render.py` dựng lại đủ8 animation x3 class từ raw gốc (walk dùng walk-v2) ở192x192, giữ frame footprint24 world units (pixelsPerUnit8). Asset mới ở `assets/hd/{hero,wizard,mechanic}/normalized-hd/`; atlas đã trỏ vào đó. Ảnh96px cũ giữ nguyên để so sánh; portrait vẫn dùng bản cũ152px.
- Builder mới giữ alpha cạnh ảnh qua alpha_composite một lần, không dùng mask-paste làm alpha bị nhân thêm.
- Ground icon chuẩn bị54px thay vì27px để không phải phóng gấp đôi ở màn hình2x. Kích thước hiển thị vật phẩm vẫn6.6 world units.
- Kiểm tra mới nhất: **172/172 tests pass**. `node tools/check-display-quality.mjs` kiểm tra DPR1/1.5/2, buffer960x640/1440x960/1920x1280, frame192, click tab Equipment/Inventory và đóng menu sau resize768x512 đều đúng; không page error.
- Ảnh so sánh: `tools/screenshots/display-quality/before-dpr2.png` và `after-dpr2.png` (có thêm1x và1.5x). Ảnh nhân vật/UI đã rõ hơn qua đối chiếu; backdrop và NPC vẫn giữ asset cũ nên không hứa toàn cảnh có thêm chi tiết gốc.
- Browser test tạo context riêng mỗi DPR để save lần trước không làm Enter thành Continue.
- Chưa commit/push thay đổi mới. Khi được yêu cầu tiếp tục FX: gen lại ảnh đúng mẫu trước, bỏ render ép vuông/vẽ nhiều lượt/tô màu phẳng; kiểm tra ảnh thật trong game với ab.jpg trước khi kết luận.

## Mới nhất: NPC rõ hơn và BỎ hiệu ứng đồ rơi theo yêu cầu

- Đã nâng cả10 NPC từ96 lên192px/khung, pixelsPerUnit8, giữ footprint24 world units. Rebuild từ raw `assets/hd/map/Oakhaven/` vào `assets/hd/npcs/normalized-hd/`, không upscale từ ảnh96px. Body164px (child140px), alpha_composite một lần để giữ cạnh ảnh. Atlas và renderer NPC đã chuyển sang bản mới; portrait152px cũng dựng từ raw.
- `npm run hd:npcs` (tools/build-hd-npcs.py) nay mặc định xuất normalized-hd. Browser kiểm tra đủ10 frame192 ởDPR1/1.5/2; ảnh `tools/screenshots/display-quality/after-dpr2.png`, trướcNPC ở `before-npc-dpr2.png`.
- QUYẾT ĐỊNH MỚI NHẤT CỦA NGƯỜI DÙNG: dẹp hiệu ứng vật phẩm sáng khi rơi và tan/hút lúc nhặt vì lag. Quyết định này THAY THẾ yêu cầu làm lại theo ab.jpg. Không tiếp tục gen hoặc tự bật lại FX.
- Đã bỏ toàn bộ đường runtime của loot glow/beam/absorption: loader không tải loot-effects.png, registry không tạo60 frame tint, renderer không vẽ glow/ray/ghost, world không tạo/update/giữ LootPickupEffect. Đã bỏ cả filter brightness lặp theo món đồ để giảm chi phí. Icon rõ, độ sáng ổn định, nhặt xong biến mất ngay; UI một thẻ và nhặt từng món vẫn giữ.
- Source/ảnh FX cũ giữ trên đĩa làm tài liệu nhưng không được runtime import/load. `tools/check-loot-effects.mjs` đã đổi sang xác nhận FX tắt và pickup ngay, không còn test atlas60 frame như trước.
- Kiểm tra gần nhất **172/172 tests pass**, `check-loot-pickup.mjs` pass, không page error. Chưa commit/push các thay đổi mới.

## Mũ / giáp / găng / giày: 10 bộ hình như vũ khí

- Người dùng yêu cầu gen cả4 loại trang bị theo cặp1/5,10/15…90/95. Đã dùng image_gen tạo40 icon mới,4 atlas riêng5 cột x2 hàng: `assets/custom/helmet-icons-chibi.png`, `armor-icons-chibi.png`, `gloves-icons-chibi.png`, `boots-icons-chibi.png`.
- Phong cách khớp inventory-icons-chibi và vũ khí: da, thép, đồng, băng, lửa, hoàng gia, thiên nhiên, arcane, thiên giới, rồng. Mỗi loại10 mẫu dùng chung cho3 class (không tạo120 mẫu theo class).
- Mapping/prompt: `assets/custom/armor-icons-chibi.json` và `armor-icons-chibi-prompt.txt`.
- `equipmentTier()` trong item-definitions là hàm chung cho vũ khí và4 slot armor. `itemSprite` giải quyết icon theo slot/itemLevel; inventory/equipment/activity notice/ground đều đồng bộ. Renderer ground dùng groundArmorIcons54px choHiDPI.
- `equipment-icons.js` tìm rãnh trong suốt giữa hàng/cột rồi crop/tight trim; tránh cắt sai do khoảng cách vẽ không đều. Ảnh runtime được xác nhận đủ10 icon/slot và kiểm tra trực quan bảng40 hình.
- Tên trang bị mới theo tier (Frost Helm, Royal Plate, Dragon Boots...). Giữ templateId và công thức/stat template cũ để tương thích save, không mở MAX_LEVEL hay tăng tỉ lệ rơi. Tên đã lưu cũ được giữ nguyên.
- **173/173 tests pass**, `node tools/check-armor-icons.mjs` pass (40 icon, save/load, inventory, ground, DPR2, không page error).
- Bảng xem40 tier: `tools/screenshots/armor-icons/all-tiers.png`; ảnh inventory/drop của từngslot ở cùng thư mục.
- FX đồ rơi vẫn tắt hoàn toàn theo yêu cầu giảm lag. Chưa commit/push các thay đổi mới này.

## Bag reference UI, monster rear views and fuller wolf (2026-10-27)
- Bag rebuilt around `bag.png`: generated snowy navy frame in assets/custom/bag-frame.png; Equipment opens loadout/status, Detail shows paged gear and equip/unequip, Inventory shows actual gold/potion stacks with Sort/Use. Shared tabs and red X. Existing 20 gear capacity preserved. Enhance is a dim, inactive placeholder because no enhancement system exists.
- Skills retains the existing three-branch prerequisite tree for every class, upgrade and hotbar binding, inside the shared Bag shell. Layout/interaction helpers updated; no fake materials or new stats added to saves.
- HD Frost Hollow monsters previously mapped up to down. Added true rear artwork for frostSlime/iceGoblin; spectralWolf has a completely redrawn sturdy body, broad chest, thicker legs/paws and fuller tail, target front height about 11% larger. Four walk frames per each of down/up/left/right.
- Runtime now loads assets/hd/monsters/normalized-hd/*.png (192px cells, 8 pixels per world unit). `npm run hd:monsters` rebuilds via tools/build-hd-monsters-v2.py. Original raw sheets preserved; new reference-based sources and prompts beside them. Removed detached generated motes during normalization; no runtime glow was added.
- Validation: 174/174 unit tests pass; check-bag-monsters.mjs passes Equipment/detail/equip/page2/Inventory/all three skill trees/close and distinct rear art for all three HD monsters, no page errors. check-display-quality.mjs passes DPR1/1.5/2 plus resized mouse input. git diff --check clean.
- Visual checks: tools/screenshots/bag-monsters/{equipment,equipment-detail,inventory,warrior-tree,wizard-tree,mechanic-tree,monster-directions}.png. Monster sheet screenshot enlarged only for inspecting artwork; normal world size remains 24 units per cell.
- Weapon/armor screenshot scripts now open Equipment Detail after B. No commit/push performed.

## Bag visual correction after user comparison (2026-10-27)
- User rejected the first Bag as too different from bag.png. Replaced runtime skin with generated assets/custom/bag-frame-v2.png: painted leather backpack and red close backing embedded, aged metal corner brackets/rivets, snowy edges, textured navy interior. Original skin retained. Built-in image_gen used with bag.png as reference; exact prompt saved in bag-frame-v2-prompt.txt.
- character-menu draws the new title rail/body separately, removes the procedural backpack when skin is loaded, and draws the X/labels/tab symbols over the art. Tabs retain previous hit bounds.
- bag-widgets uses layered bevels, translucent blue panel gradients so skin grain remains visible, finer corner details on slots, condensed Bag-only typography, and a subtle static lit stone dais behind the loadout hero. pixel-text now accepts an optional font; other UI defaults unchanged.
- Empty equipment slots show desaturated silhouettes from the existing item art. Status rows are more compact. Supply grid is centered with equal side margins; hit tests use the same updated positions. Item art, gear capacity, skill tree and combat logic unchanged.
- Validation: 174 unit tests pass; browser Bag interactions/all three class trees pass without page errors; display-quality check passes DPR1/1.5/2 and resized pointer input. Updated screenshots remain in tools/screenshots/bag-monsters. No commit or push.

## Unified Inventory, no Equipment Detail (2026-10-27)
- User requested removal of Equipment Detail and all weapons/items inside Inventory. Removed equipmentView state and Detail route/button completely. Equipment stays loadout/status, with Unequip for an occupied selected slot.
- Inventory now uses one paged 3x4 grid: gold/HP/MP stacks first, followed by 20 gear slots. Shared selection/hit-testing/navigation maps the 23 cells across two pages; supplies retain independent stack counts and gear capacity remains 20. Right pane shows supply Use or gear stats/Equip. Sort orders gear by slot/descending level and stacks by quantity. X discards only Inventory gear.
- Updated controller tab reset, layout, unit tests and weapon/armor/browser screenshot scripts. 174 tests pass; check-bag-monsters verifies potion use, gear equip in Inventory, unequip from Equipment, page 2, all class skill trees and close without page errors. Latest Inventory screenshot: tools/screenshots/bag-monsters/inventory.png. No commit/push.

## Bag footer containment fix (2026-10-27)
- User screenshot gg.png showed buttons overlapping the painted bottom frame. Moved Inventory/Equipment footer buttons upward from y538 to y528, reduced height from38 to32, inset Sort/page from left panel edge, and ended inner panels at y568 so they no longer cover the painted snow/border.
- BAG_ACTIONS now provides shared bounds for every footer button, used by drawing and hit tests. Moved temporary action feedback above the buttons inside the right pane instead of below the frame. No gameplay changes. 174 tests pass; browser check covers all affected buttons.

## Wizard Ice Storm and skill preview (2026-10-27)
- User supplied icestorm.png and requested a matching spell. Replaced the existing Blizzard presentation with Ice Storm while retaining save id `blizzard`, frost-tree prerequisites, five ranks, original MP/cooldown/damage/radius arrays and four damage waves. The new iceStorm cast kind places a fixed area 26 units ahead; later movement does not drag the storm.
- Built-in image_gen created a 6x4 VFX atlas and a second transparency cleanup edit. Sources assets/hd/wizard/fx/ice-storm-{raw,clean}.png, prompt ice-storm-prompt.txt. tools/build-ice-storm.py packs the clean atlas to ice-storm.png (1152x768, 24 cells of192px). No image generation API key/dependencies added.
- IceStorm entity has 12 deterministic meteors, four timed damage waves, bounded lifetime. Renderer draws the cast circle behind actors, falling diagonal ice spears/impacts/fading chips above. Registry slices four six-frame strips. Uses ordinary world.effects lifecycle; clearing map removes pending waves too. performRadialHit accepts optional fixed center; existing callers preserve their previous center/knockback.
- User then requested maximum starting skill points and slower falling to inspect effects. GameSession SKILL_PREVIEW_MODE=true: New Game grants75 points, enough for all15 skills x5 ranks, and sets transient player.skillTestMode. Level requirements are bypassed only for preview sessions; prerequisites still apply. Continue sets the preview flag but does not overwrite saved points. Turn off the constant to restore normal progression. Player level/stats remain unchanged.
- Final timing: cast650ms, fall560ms (previous320), wave spacing400ms (previous250), impact440ms and residual fade490ms, overall2700ms. No slow motion/global speed changes.
- Validation:177/177 tests pass. tools/check-ice-storm.mjs verifies actual New Game wizard starts75 points, learns/binds/casts at level1, atlas4x6 frames loads, damages enemies and cleans up. No browser errors. Screenshots tools/screenshots/ice-storm/{cast,falling,impact,fading}.png. No commit/push.

## Published checkpoint and painted icons/FX (2026-10-27)
- At user's request committed/pushed prior game work as ea53c5a on origin/main. New assets were normalized to lowercase assets/ Git paths for Linux compatibility. Unrelated staged root reference images/deletions and Python caches were excluded and preserved locally.
- User then requested generating replacements for remaining pixel icons/effects. Added three painted skill atlases (15 icons/class,45 total): assets/custom/skill-icons-{warrior,wizard,mechanic}-painted.png. Wizard initially generated labels/sidebar; regenerated to a clean5x3 tile atlas. Exact initial/final prompts stored beside assets. Built-in image_gen used throughout.
- Added combat-fx-painted.png: four animation frames for each legacy key slash/arc/dashTrail/hitSpark/circular/flame/spark. Existing wizard/mechanic HD spell/gun FX and Ice Storm remain. utility-icons-painted.png replaces NPC available/turnIn/talk markers and square hit particles with painted gold stars.
- painted-combat-assets.js slices skill tiles via opaque alpha bands (generated gutters are uneven), slices combat FX at verified transparent row boundaries, and builds8px-per-world-unit frames matching previous effect dimensions. Registry prefers painted replacements, preserving pixel assets only as missing-image fallbacks. NPC marker draw honors pixelsPerUnit. Particles draw the star sprite rather than fillRect squares.
- Validation:177 unit tests pass; check-painted-combat.mjs confirms all45 icons exceed100px, seven4-frame effects preserve old world extents, all3 markers use HD art, and particle sprite exists; screenshots all3 skill trees plus all-icons-effects.png, no page errors. Updated Bag and Ice Storm browser checks also run. Latest visual files in tools/screenshots/painted-combat. These icon/FX changes are not yet committed/pushed.

## User art-direction correction — takes precedence (2026-10-27)
- User rejected the latest painted skill icons: too Western/European fantasy and inconsistent with the chibi game. Technical checks passing did not mean the visual style was accepted. Do not call these icons approved or use them as future generation references. They remain connected in local code; this documentation update does not replace or revert artwork.
- Required direction, verbatim: "Art style (Phong cách đồ họa): Đồ họa 2D sắc nét, thiết kế nhân vật theo phong cách anime/manga dễ thương nhưng kịch tính, kết hợp với góc nhìn từ trên xuống (top-down view) cổ điển."
- Added mandatory guidance to AGENTS.md and Art direction in README.md: Zenonia-like anime/manga chibi throughout; compact readable icons, clean outlines, lively colors, cute but forceful action. Hand-painted/HD does not authorize a switch to Western fantasy, epic miniature paintings, heavy ornate metal crests, gothic/baroque or dark realism. Compare new art with existing chibi hero/items at actual runtime size before declaring success.
- Latest user request handled here is to persist the style rule. No new images generated, no art replacement/revert, no commit/push in this documentation-only step.

## Chibi skill-icon replacement (2026-10-27)
- User explicitly requested replacing the rejected icons immediately. Generated45 new icons with built-in image_gen, using assets/custom/inventory-icons-chibi.png and assets/hd/wizard/idle.png as the actual style references. Rejected Western-fantasy icons were NOT used as references.
- New runtime sources: assets/custom/skill-icons-warrior-chibi.png, skill-icons-wizard-chibi.png, skill-icons-mechanic-chibi.png; exact prompts in the matching -chibi-prompt.txt files. Compact cel-shaded subjects, clean warm outlines, simple anime movement strokes, bright colors, no ornate metal frames. Wizard atlas has transparent subject backgrounds, warrior/mechanic have simple navy square backgrounds.
- Changed only the three skill-atlas file references in asset-atlas.js; tree/hotbar continue using the same45 skill IDs. Improved icon slicing to expand opaque anchor bands into transparent gutters before tight trimming, preserving thin flame tips, stars and motion lines. Combat effects and gameplay unchanged in this step.
- check-painted-combat.mjs now saves to tools/screenshots/skill-icons-chibi, including all-skill-icons.png and all three class trees. Browser check verifies all45 icons are nonempty and high resolution with no page errors.177 tests passed. Technical/visual inspection complete; do not claim the user has approved the style until they respond. No commit/push.

## Warrior melee reach and anime slash (2026-10-27)
- User additionally requested longer warrior reach and wider attack visuals, allowing new generated slash art if needed. Warrior combo reach now30/32/34/42 (was18/18/19/24), widths28/30/32/40 (was20/20/22/28). Damage multipliers, hit timings, lunge distances and fourth-hit finisher unchanged; wizard/mechanic combo configs untouched.
- Melee VFX center now follows reach/2+4 rather than fixed14. Per-hit fxScale1.45/1.55/1.65/1.15. Kept the finisher's extra spinning arc at scale1.0. Hit2 uses local flipY instead of rotating180 degrees, so its sweep stays pointed at the target. VisualEffect/drawRotated support that optional flag.
- Enlarged old FX looked too diffuse in actual gameplay, so generated new assets/custom/warrior-slashes-chibi.png (4frames x2rows) with inventory chibi and actual black-haired warrior idle as references. Clean broad white/cyan anime crescents, gold secondary edge for finisher; no fog. Exact built-in image_gen prompt in warrior-slashes-chibi-prompt.txt. Registry overrides slash/arc with this atlas at the same world-space base sizes; no character animation sheet regenerated.
- Added focused melee regression: target beyond old reach is hit; distant and rear targets excluded; normal/finisher tested in four directions. Browser check-warrior-range.mjs validates hits1/2/4 in all4 directions and saves actual gameplay screenshots in tools/screenshots/warrior-range.178 unit tests pass. Icon/FX browser check still passes45icons,7effects,3markers. No commit/push.

## Attack button visual correction (2026-10-27)
- User disliked the Attack button and requested removing J/Space. Removed the visible J / SPACE label. Replaced its flat procedural sword with the existing chibi item artwork for the equipped weapon (starter weapon for class when empty), centered in a softly lit navy circular button matching the HUD's silver-blue rim. Wizard now shows staff, mechanic gun, warrior sword. Existing keyboard bindings remain; no generated image needed because the matching chibi weapon art already exists.
- hud-renderer receives full registry for skill/button drawing; sizes and positions of skill slots unchanged. Node syntax check and painted-combat browser check pass, no page errors. No commit/push.

## Earth Break / Rocket Barrage ultimates and crop repair (2026-10-27)
- User supplied ultimate.png for warrior and ultimatemechanic.png for mechanic, requesting close visual matches. Earth Break replaces sword-tier3 Blade Storm presentation under unchanged save id bladeStorm; Rocket Barrage replaces explosives-tier3 Carpet Bomb under unchanged save id carpetBomb. Existing tree prerequisites/five ranks/hotbar remain. Icon atlas ultimate-icons-chibi.png provides new compact anime icons for both.
- Earth Break: charge ring, forward golden sweep, giant vertical sword/stone eruption, fixed glowing fissure patch. MP25..35, cooldown8s, main damage320..440% ATK, radius42..66 world units. Sweep deals20% of main damage, then main slam launches non-boss targets for1500ms, fissures tick four times at12% of main damage. Only targets actually hit are launched; bosses immune to launch. Airborne bodies lift while shadows stay grounded, AI resumes after landing.
- Rocket Barrage: blue target reticle, fan launch, diagonal descent, orange/blue chain blasts and discrete anime smoke/debris.8..12 rockets by rank, MP25/32/40/50/60, cooldown18s, per-rocket320..480% ATK, local blast radius14..22, forward fan60degrees/range60..80. Launches in three close groups; impacts staggered85ms. A fixed snapshot defines targets, not a caster-following damage area.
- UltimateEffect owns bounded event timelines in world.effects; handles long frames, death and cleanup. Player roots briefly and uses existing attack frames during cast. A landed incoming hit releases the cast lock to show hurt/knockback; map clear releases lock too. Save ids retained.
- Generated source atlases: assets/custom/earth-break-fx.png (4stages x6frames), rocket-barrage-fx.png (5stages x6frames), and ultimate-icons-chibi.png. Exact built-in image_gen prompts stored beside each. Uses user refs plus actual chibi hero/item art.
- User reported broken warrior slicing. Source sweep frames overlap; cannot repair by equal grid cropping. Regenerated ONLY that strip as earth-break-sweep-source.png (3x2, isolated frames). tools/split-earth-break.py exports24 independent384px PNGs in assets/custom/earth-break-frames with fixed per-stage anchors and frames.json. Runtime loads these24 PNGs, not raw Earth Break atlas rectangles. Contact-sheet.png shows each complete frame. Alpha<=48 discarded to avoid background haze. Slam render scale reduced to fit the complete sword in typical viewport.
- Rocket atlas uses verified uneven row gutters and per-row alpha column gutters to avoid clipped rocket noses/neighbour fragments. UI skill/hotbar icons resolve to the two new ultimate icons.
- tools/check-ultimates.mjs checks both spells' four stages, actual damage,24 independent warrior frames, lifetime cleanup, and hurt/MISS; screenshots tools/screenshots/ultimates. These changes are local/uncommitted.

## Hurt and MISS — final user rule (2026-10-27)
- User requested knockback + hurt pose without blinking. Removed immunity blinking AND red hit tint in player-renderer. Hurt frames get priority and last220ms. Successful incoming damage interrupts combo, applies existing100ms knockback and retains500ms invulnerability; hero remains continuously visible.
- User clarified: ONLY BASIC ATTACKS can MISS, never skills. Final implementation follows this, superseding earlier broad rollout/commentary. Default ordinary hit chance95% for both sides, injectable world.hitRoll for deterministic tests. Melee combo explicitly opts in; wizard/mechanic combo projectiles carry canMiss=true, including their basic finisher explosion. Skill projectiles defaultfalse; radial/strike/dash/ultimate/DoT damage never rolls MISS. Enemy single ordinary shots opt in; boss radial projectile special does not.
- damageMonster defaults canMiss=false; ordinary paths opt in. Incoming hitPlayer defaults ordinary=true, projectiles pass their flag. Miss shows MISS only, with no damage, hurt, immunity, hit flash, knockback or death rewards. Optional accuracy/evasion fractions supported centrally; no extra progression stats invented.
-186 tests pass including hit-chance samples, no-blink original hurt frames, ranged ordinary MISS versus guaranteed skill hits, both ultimate timelines and map-lock cleanup. Browser checks verify rendered stages and outgoing/incoming MISS/hurt. No commit/push.

## Smaller meteor explosion visuals (2026-10-27)
- User said oversized meteor explosion artwork looked blurry. Reduced Ice Storm impact/residual shard drawing to75% and Meteor Rain fireBurst visuals to75% via optional radial visualScale. The falling spears, timings and actual damage/radius are unchanged. No new image generated for this size-only correction.

## Smooth rendering / anti-lag (2026-10-27)
- User reported lag when screen recording. Profiling: game itself holds 60fps (CPU ~7%, still 60fps at 4x CPU throttle); lag was judder, amplified when the recorder delays frames.
- Camera renderX/renderY now snap to screen pixels (1/PIXEL_SCALE world px), not whole world px (old: uneven 4/8px background jumps).
- game/render-interpolation.js: snapshot positions before each 60Hz step, render blends prev->current by loop alpha (player, npcs, monsters, projectiles, drops, effects, particles, texts, camera); >32px moves treated as teleports; positions restored after draw.
- GPU: canvas context alpha:false; attack-button weapon shadow baked once per sprite (no per-frame shadowBlur); damage numbers use offset stroke instead of shadowBlur.
- Always-on FPS readout: src/ui/fps-counter.js (top-left under portrait, FPS + worst frame ms per 500ms window, green/amber/red), drawn from main.js render callback on every screen.
- GPU smoothing: Chrome imageSmoothingQuality 'high'/'medium' re-filter every draw (measured: walk 19% -> 7.8% GPU, combat 24% -> 10.5% after fix). graphics/smooth-sprite-draw.js builds lazy half-size mip levels per HD source (once, 'high') and draws with 'low' from the smallest level >= on-screen size. Used by hero, NPC, HD monster, effects, loot, ice storm, ultimates, HUD weapon, backdrop. renderWorld resets quality to 'low' each frame (menus still use 'high', only while open).
- Pre-warming all levels was measured (815 HD sprites: +106ms load, +42MB RAM) and NOT adopted; lazy build ~0.13ms/sprite, no first-cast hitch (tools/check-first-cast-hitch.mjs). Only first-ever hitch is AudioContext creation (~80ms) on the first key press (title screen).
- Co-op load sim (game is still single-player, no networking): tools/check-combat-soak.mjs [sec] [hero1] [hero2] runs SOLO then DUO; DUO injects a 2nd Player into the same world (stepped via updatePlayer with world.player swapped, drawn via drawPlayer), in-page bots chase monsters + cast hotbar skills. 60s result: DUO warrior+wizard 59.9fps, p99 16.8ms, 0 frames >25ms, GPU 13.6%, JS 6.3%. Sim limits: monsters only target hero 1, camera/HUD follow hero 1.
- Tests: tests/render-interpolation.test.js, tests/fps-counter.test.js, tests/smooth-sprite-draw.test.js. No commit/push.

## Frost Hollow local boss (2026-10-27)
- Generated Frostfang King, a sharp anime/chibi ice yeti, using built-in imagegen with iceGoblin HD artwork as style reference. Source and full prompt: assets/hd/monsters/raw/frostfangKing.png and frostfangKing-prompt.txt. Build: tools/build-frostfang-boss.py -> normalized-hd/frostfangKing.png (16 frames, down/up/left/right, real rear view). Render scale 1.65.
- Spawn at tile (7,5), north-west clearing. HP580 = Ice Goblin HP58 x10; ATK33 =11 x3. Waits at post until aggro; leash120. Boss immunities/HUD/minimap and guaranteed boss-quality loot apply. Local boss never sets the final Demon Cyclops completion flag or opens victory; returns on map re-entry.
- Browser check tools/check-frostfang-boss.mjs passed, four-direction screenshots tools/screenshots/frostfang-boss. Unit tests cover ratios, walkable spawn, idle/aggro and no accidental victory. No commit/push.

## Frost Hollow directional attacks (2026-10-27)
- Built-in imagegen generated four identity-matched anime/chibi attack sheets: frostSlime, iceGoblin, spectralWolf, frostfangKing. Source PNGs and exact prompts: assets/hd/monsters/raw/<type>-attack.png and <type>-attack-prompt.txt. Goblin selected initial attack source; padded alternate was rejected because it resembled walking. Wolf uses padded second generation with full nose/tail.
- tools/build-monster-attacks.py splits along alpha gutters, removes disconnected scraps, preserves one scale across each sheet, and packs 16 frames into normalized-hd/<type>-attack.png. Cells256px, feetY224, ppu8; extended limbs have room without moving the feet origin. Existing walk sheets unchanged.
- Manifest registers attack assets per HD monster. Builder exposes hd.attack[down/up/left/right]. Renderer selects frames0/1 during AI windup and frames2/3 over first240ms of cooldown, so strike matches actual damage and never loops throughout cooldown. Missing attack art falls back to walking/idle; existing hit flash still takes priority. No damage/AI balance changes.
- Latest full suite197/197 passed. tools/check-monster-attacks.mjs validates64 nonempty unclipped attack frames and captures12 in-game direction/phase screenshots in tools/screenshots/monster-attacks. No page errors. No commit/push.
