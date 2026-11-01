# Việc đang dở

Cập nhật 2026-11-01 khoảng 10:45. Đọc file này trước khi làm tiếp, rồi đối chiếu `git status` và ảnh thật: file có thể đã cũ. Xong việc nào thì xoá dòng đó.

Quy tắc khi làm tiếp: `CLAUDE.md`, `docs/sprite-weapon-rules.md`, `docs/new-style-class-brief.md`.

**Chưa commit gì từ commit `810bef8` (branch `unity`).** Toàn bộ việc dưới đây, cả phần đã xong, đang nằm trong working tree.

## ChatGPT: tài khoản và cách liên lạc

| Tài khoản | Chrome | Hạn mức |
|---|---|---|
| Tan Pham (Plus) | deviceId `7fd3e286-b745-4da0-b1ff-cecfb5c78fc7` | Hết lượt, thử lại được **07:41 ngày 2026-11-02** |
| Tay (Plus) | deviceId `a75d77ec-120f-404a-a622-c9961a3cd94c` | Báo có lại từ **15:14 ngày 2026-11-01**, chưa thử lại |
| Tieu Duy Phuong, xaochai | | Không dùng. `661fd420-...` đang đăng nhập Tieu Duy Phuong |

- deviceId có thể đổi sau khi tải lại extension. Luôn đọc tên qua `/api/auth/session` trước khi gửi.
- Không có lượt thì dừng và báo, không đổi sang tài khoản cấm.
- Mỗi agent vẽ một hàng sprite trong một chat riêng nên có rất nhiều chat tên chung như "Create Image Redraw", "Redraw Six Frames". Không cần tìm lại: mở chat mới, tải ảnh tham chiếu trong repo là đủ. Bảng dưới chỉ ghi những chat đáng quay lại.

### Chat đáng quay lại (tài khoản Tan Pham)

| Tiêu đề trong Recents | Link | Dùng cho |
|---|---|---|
| Vẽ trụ súng chibi | https://chatgpt.com/c/6ae6ab25-a210-83ec-8aa8-b966ef90a57c | Mẫu trụ bắn Mechanic đã duyệt; ChatGPT có 2 ảnh để chọn, repo lưu 1 ảnh |
| Khuyến nghị vẽ lại | https://chatgpt.com/c/6ae6a84c-4fac-83ec-8693-f5f3bc1cf76b | Dáng Mechanic một súng; ChatGPT giải thích vì sao chọn đồ bổ trợ thay vì bom |
| Create Gun Image | https://chatgpt.com/c/6ae6afbb-1f28-83ec-bcdf-4d3ddbf2778b | Súng Mechanic. Agent đã dùng, chưa đối chiếu ảnh nào trong đây là `gun-scrap-base` |
| Tạo lại súng | https://chatgpt.com/c/6ae6b130-54c8-83ec-983f-10cb9916c4bf | Có vẻ là súng Mechanic, tạo lúc 10:25, chưa xác nhận của agent nào |
| Create shield image | https://chatgpt.com/c/6ae6ae1b-27b0-83ec-aa78-66d544aa811e | Khiên Warrior tier sau: viết tiếp ở đây để giữ hình bóng |
| Generate Shield Image | (mã bắt đầu `6ae6ad94`) | Khiên Warrior `bronze` |
| Tạo khiên RPG chibi | (mã bắt đầu `6ae6959f`) | Mẫu khiên `iron` |
| Vẽ kiếm game 2D | https://chatgpt.com/c/6ae68a74-d94c-83ec-b644-d617b67a150d | Kiếm `holy`, `dragon` |
| Thiết kế sprite 2D | https://chatgpt.com/c/6ae6908e-40d8-83ec-96f7-024bc648cece | Bộ 6 tướng hàng ngang, nguồn của hình mẫu, cả Warrior kiếm dài |
| Vẽ lại nhân vật** | https://chatgpt.com/c/6ae69365-92f8-83ec-bb4e-418cf5f6810e | Cleric không cánh |
| Vẽ lại nhân vật ngang trái | https://chatgpt.com/c/6ae69bbd-31d4-83ec-a67d-9d43bfe7d84b | Warrior idle trái: tay cầm kiếm xuôi xuống |
| Khuyến nghị tỉ lệ kiếm chém | https://chatgpt.com/c/6ae69433-af08-83ec-be55-f41ac8d6ddc7 | Hỏi về độ dài kiếm. Người dùng sau đó chọn giữ kiếm cỡ cũ |
| Tạo hình ảnh sprite wizard | (mã bắt đầu `6ae6ae0a`) | Một hàng Wizard, chưa rõ hàng nào |

### Chat trên tài khoản Tay

| Tiêu đề | Link | Dùng cho |
|---|---|---|
| Chưa ghi lại tiêu đề | https://chatgpt.com/c/6ae61667-9900-83ec-a607-145b6d0e7347 | Thảo luận màn chọn tướng, art màn chọn tướng, kiếm dáng song song |

## Chia tài khoản cho phần còn lại

- **Tay, từ 15:14 ngày 2026-11-01:** súng Mechanic, gậy Wizard, dao Assassin, gậy Cleric, trụ Mechanic, linh thú Cleric. Hẹn trong phiên lúc 15:21.
- **Tan Pham, từ 07:41 ngày 2026-11-02:** 5 khiên Warrior, `attack3` Wizard, `melee3`, `melee4`, `nightfall` Assassin, `clericcast` Cleric và các hàng vẽ lại.

## Art đang dở: cần lượt vẽ

Người dùng duyệt style mới cho cả 5 class. Hiệu ứng skill không đổi. Dash không vẽ riêng: dùng sprite đi kèm 4 dư ảnh (`HeroDashTrail.cs`), cho mọi class.

### Mechanic (`Tools/build-mechanic-new-style.py`, `Assets/ArtLibrary/classes/mechanic/new-style/`)
- [ ] Súng: mẫu `src/gun-scrap-base.png` đã có, grip đo (39, 110), chưa cài. Còn `scrap` cường hoá và 9 tier, khoảng 19 ảnh.
- [ ] Trụ bắn 10 tier, khoảng 20 ảnh, theo `Assets/ArtLibrary/classes/mechanic/turret-src/turret-sample.png`, nòng chĩa trái, grip giữa đế.
- [ ] Khi có art trụ: bật `CompanionGrounded = true` ở dòng Mechanic trong `HeroClassDef.cs`. Đang tắt vì robot cũ sẽ bị cắm xuống đất.
- [ ] Icon súng và trụ.
- [ ] `death` vẫn dáng cũ có găng.
- [ ] Người dùng chưa quyết: súng các động tác (`s` 0,23) nhỏ hơn hẳn súng trong hình mẫu.

### Wizard (`Tools/build-wizard-new-style.py`, `Assets/ArtLibrary/classes/wizard/new-style/`)
- [ ] `attack3` xuống, lên, trái (phải đã xong).
- [ ] `attack1`, `attack2` hàng trái vẽ riêng rồi bỏ khỏi `MirrorLeft`: đang đổi tay khi quay trái.
- [ ] Vẽ lại `idle` phải: pha lê nằm trên ngực.
- [ ] Gậy 10 tier, mẫu chuẩn, grip, icon `weapon-wizard-*`, khoảng 20 ảnh. Cỡ cố định `s = 0.447`.

### Assassin (`Tools/build-assassin-new-style.py`, `Assets/ArtLibrary/classes/assassin/new-style/`)
- [ ] `melee3`, `melee4`, `nightfall`, khoảng 10 ảnh. Combo đang lẫn nét mới và cũ.
- [ ] Dao 10 tier, khoảng 20 ảnh, kèm icon.
- [ ] Vẽ lại `hurt` xuống: mắt đang mở, bản cũ nhắm.
- [ ] Bóng Shadow Clone: mũi dao bị cắt ở ô 192 px, chưa xem trong game.

### Cleric (`Tools/build-cleric-new-style.py`, `Assets/ArtLibrary/classes/cleric/new-style/`)
- [ ] `clericcast` 4 hướng, giữ gậy đúng tay sheet cũ, rồi thêm vào `HandAnims`. Hiện lúc cast còn hiện cánh và áo xanh.
- [ ] Vẽ lại `attack3` trái frame 3-5: áo phải che chân. Nhân vật có dáng trẻ con, mọi frame phải kín đáo, áo che tới gối.
- [ ] Gậy 10 tier, linh thú 10 tier (`companion/art`, `companion/float`), icon.

### Warrior (`Tools/build-warrior-shield.py`, `Assets/ArtLibrary/classes/warrior/shield-src/`)
- [ ] 5 khiên `thunder`, `verdant`, `shadow`, `holy`, `dragon`: tải ảnh từ `shield-src/ref/`, lưu `<tier>-pair-chatgpt.png`, chạy `Tools/build-warrior-shield.py <tier>`, rồi `icons`.
- [ ] `melee2` các frame giữa tay còn lệch tới 57 px so với sheet cũ.
- [ ] `iron` bản thường và cường hoá đang chung một ảnh, nên kiếm khởi đầu cũng có vết nứt đỏ.
- [ ] 8 mẫu kiếm từ `bronze` có hai dáng: `iron`, `bronze` theo cây cắt từ hình gốc, 8 mẫu còn lại dáng song song. Người dùng đã chấp nhận.

## Code và kiểm tra, không cần lượt vẽ
- [ ] Đo cỡ đầu mọi hàng của cả 5 class so với idle hướng xuống rồi chỉnh (`grow_head`).
- [ ] Xem chuyển động thật trong game từng class: giật frame, vũ khí trong tay, đường chém. Mới xem ảnh tĩnh, phần lớn trên ảnh ghép.
- [ ] Assassin: frame đánh chụp được trong game trông tối và mờ, chưa rõ là hiệu ứng sẵn có hay lỗi sprite mới.
- [ ] Màn chọn tướng: robot đã đổi thành trụ, chưa xem trong game có đè cột bên cạnh không. Ảnh chân dung Mechanic cắt nòng súng ở mép.
- [ ] Kiểm tra khung "In game" ở màn chọn tướng còn cắt mũi kiếm không, sau khi idle hướng xuống dùng kiếm cỡ cũ.
- [ ] Bản build và smoke test `FrostSmokeTest` trong Unity chưa chạy lại từ đầu đợt này.
- [ ] Đánh giá trên điện thoại thật: sprite chi tiết hơn có rối không.

## Tính năng chờ làm
- [ ] **Trụ bắn đặt tại chỗ (cách B, người dùng dặn nhớ):** Mechanic đặt trụ xuống, trụ đứng yên tự bắn, hết giờ biến mất. Cần logic phía server và đồng bộ online. Xem lại tên, hình skill "Robot Assault".
- [ ] Bom của Mechanic làm thành skill (ChatGPT gợi ý, người dùng chưa quyết).
- [ ] Cánh thành item cho mọi class. Sprite class không vẽ cánh.
- [ ] Boxing: chỉ có art trên màn chọn tướng và bị khoá. Art ở `Assets/ArtLibrary/custom/idle-down-set/boxing.png`, chưa có class.
- [ ] Màn đăng nhập, đăng ký có hình riêng. Lưu nhân vật online và đăng nhập thật: hiện server tạo nhân vật mới mỗi lần vào, người online chọn class ở màn Connect cũ.
- [ ] Từ trước: party chat chưa qua mạng; deploy server chưa làm vì thiếu Linux server module; hero của mình có thể bị che trong đám đông; `combo` Warrior hướng trái lật ảnh đổi tay nay đã vẽ lại, cần xem lại mục "Việc còn tồn" trong `docs/sprite-weapon-rules.md`.

## Trước khi phát hành
`Assets/Resources/Data/dev-start.json` đang bật cheat. Phím F10 là phím test tạm.

## Bản build và video để xem
- Bản chơi: `C:\temp\player-kiem-goc\Zenonia.exe`, đủ 5 class đến thời điểm cập nhật file này. Không còn được build từ file mới sẽ cũ dần.
- Video so sánh Warrior cũ và mới: `C:\temp\compare\warrior-cu-vs-moi-1-danh-thuong.mp4`, `warrior-cu-vs-moi-2-skill.mp4`. Quay lại bằng cờ `--record-compare attack|skills`.
- Bản Warrior cũ để quay lại: `C:\temp\cmp-old\Zenonia.exe`.
