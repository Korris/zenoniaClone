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
