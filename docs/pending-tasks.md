# Việc đang dở

Cập nhật 2026-11-01 khoảng 18:20. Đọc file này trước khi làm tiếp, rồi đối chiếu `git log` và ảnh thật. Xong việc nào thì xoá dòng đó.

Quy tắc khi làm tiếp: `CLAUDE.md`, `docs/sprite-weapon-rules.md`, `docs/new-style-class-brief.md`.

## Việc design: mở đúng tài khoản nào, chat nào

Khi người dùng nói "làm tiếp": tra bảng này, `select_browser` đúng deviceId, đọc tên qua `/api/auth/session` rồi mới gửi. Tay hiện tên phiên là "Tân Korris" (sidebar ghi Tay). deviceId có thể đổi sau khi tải lại extension: khi đó dùng `list_connected_browsers` và mở từng trình duyệt để đọc tên. Một bộ art vẽ trên một tài khoản, không đổi giữa chừng. Không có lượt thì dừng và báo.

| Tài khoản | deviceId | Có lại lượt |
|---|---|---|
| **Tay (Plus)** | `a75d77ec-120f-404a-a622-c9961a3cd94c` | 15:30 ngày 2026-11-02 |
| **Tan Pham (Plus)** | `7fd3e286-b745-4da0-b1ff-cecfb5c78fc7` | 07:41 ngày 2026-11-02 |
| Tieu Duy Phuong, xaochai | `661fd420-...` đang đăng nhập Tieu Duy Phuong | Không dùng |

| Việc design còn dở | Tài khoản | Chat |
|---|---|---|
| Mechanic idle và `attack1-3` hướng xuống (chi tiết bên dưới) | **Tay** | Chat mới |
| Mechanic `hit4-down`, `hit4-up` | **Tay** | Chat mới. Bản cũ: https://chatgpt.com/c/6ae70b65-5d84-83ec-a677-1cf3c6b7e905, https://chatgpt.com/c/6ae70bcc-d228-83ec-af67-d31216487669 |
| Mechanic `rocket-right`, `rocket-down`, `rocket-up` | **Tay** | Chat mới; tham khảo `hit4-right` https://chatgpt.com/c/6ae70af7-1008-83ec-b698-51a562f88558 |
| Mechanic `attack2` phải | **Tay** | Chat mới, prompt ở `dual-src/move/STATUS.txt` |
| Mechanic trang phục: chờ người dùng chốt | **Tay** | Chat mới |
| Mechanic art tia lửa đầu nòng pháo | **Tay** | "Generate Blast Sprite Sheet" https://chatgpt.com/c/6ae7106f-a52c-83ec-b2d7-1bbbaef3e34d: có prompt và ảnh tham chiếu, chưa ra ảnh |
| Mechanic pháo các hướng, súng lục mẫu | **Tay** | https://chatgpt.com/c/6ae704e9-21fc-83ec-a957-e02a9128798d |
| Mechanic hình màn chọn tướng: vẽ lại dáng giơ súng tự nhiên, đồng bộ trang phục | **Tay** | "Create hero image" https://chatgpt.com/c/6ae702cb-0f78-83ec-9428-5d9a1e1ba8a1 |
| Mechanic 10 tier súng lục, 10 tier pháo, sau khi duyệt bản thử | **Tay** | Chat mới, tải kèm mẫu `dual-src/` |
| Khiên Warrior 5 tier còn lại | **Tan Pham** | "Create shield image" https://chatgpt.com/c/6ae6ae1b-27b0-83ec-aa78-66d544aa811e |
| Wizard, Assassin các hàng thân còn lại | **Tan Pham** | Chat mới |
| Cleric `clericcast`, tư thế `attack3`, `attack1` | **Tan Pham** | Chat mới |
| Pet mới, khiên Cleric thêm | **Tay** | "Create Holy Shield Design" https://chatgpt.com/c/6ae701c0-e12c-83ec-93fe-e6b3a1c83e59; `imp` ở `Assets/ArtLibrary/pets/` |
| Tinh linh các tier, để dành làm pet | **Tay** | "Generate Creature Image" https://chatgpt.com/c/6ae6fa3f-426c-83ec-9ad4-7885e446362b |

Mechanic ưu tiên số 1 nhưng art `dual-src` ở Tay, nên chờ Tay lúc 15:30 để giữ đồng nhất. Muốn vẽ sớm trên Tan Pham thì hỏi người dùng và so kỹ nét, màu.

Các chat tham khảo khác trên Tan Pham: "Thiết kế sprite 2D" https://chatgpt.com/c/6ae6908e-40d8-83ec-96f7-024bc648cece (bộ 6 tướng, nguồn hình mẫu), "Vẽ kiếm game 2D" https://chatgpt.com/c/6ae68a74-d94c-83ec-b644-d617b67a150d (kiếm `holy`, `dragon`), "Vẽ lại nhân vật ngang trái" https://chatgpt.com/c/6ae69bbd-31d4-83ec-a67d-9d43bfe7d84b (Warrior idle trái). Trên Tay: https://chatgpt.com/c/6ae61667-9900-83ec-a607-145b6d0e7347 (màn chọn tướng).

## Mechanic: ưu tiên số 1

Người dùng dặn: thêm agent thoải mái miễn đồng nhất. Thiết kế: 2 súng lục (một món, chỉ số tính một lần) + pháo đeo chéo sau lưng. Mẫu `Assets/ArtLibrary/classes/mechanic/new-style/dual-src/hero-test-2-back.png`. Combo: tay trái, tay phải, hai tay (một lần sát thương, tia thứ 2 chỉ là hình), móc pháo ra bắn. Trụ là skill, một hình, không tier.

Trong game đang là bản thử: chỉ tier đầu cho mọi tier. Duyệt xong mới vẽ tier. Cài art khi có: `python Tools/build-mechanic-dual.py write` (xem docstring), đặt lại `FISTS`, cập nhật `HeroFx.MuzzleCannon`.

Thứ tự vẽ khi Tay có lượt (prompt ở `dual-src/attack/STATUS.txt`):
1. **Idle và attack hướng xuống, người dùng chê 2 tay bè ra 2 bên nhìn hài.** Idle xuống: cầm 2 súng lục giơ lên tự nhiên trước ngực và vai, khuỷu tay gập, sát người, nòng chĩa lên. `attack1`, `attack2`, `attack3` xuống: 2 tay đưa về phía trước, sát nhau hơn, súng chĩa theo hướng bắn. `attack3` là tư thế cast chung của skill: xem cả skill hướng xuống. Không cài `dual-src/move/idle-down.png`: dáng bị chê.
2. `hit4-down`: cầm pháo dọc, nòng chĩa xuống, 2 nắm tay trên một đường dọc giữa thân. `hit4-up`: tương tự, nòng chĩa lên, thấy gáy.
3. `rocket-*`: 2 tay ôm pháo chĩa xéo lên trời, ngả người ra sau, 4 frame. Hero khoá 850 ms, tên lửa phóng ở 400, 440, 480 ms. Thân đang là bản tạm.
4. `attack2` phải: thấy cả tay xa cầm súng thứ 2.
5. Art tia lửa đầu nòng nhát 4: phóng thẳng từ nòng theo hình nón, lõi trắng xanh, không cầu lửa, khói tròn, mảnh vỡ, rất mạnh. Đang dùng hình tạm; đổi ở khối `ART` đầu `MechanicCannonBlastFx.cs`.
6. Hình màn chọn tướng dáng giơ súng tự nhiên, rồi idle-down cùng dáng. Quay lại clip preview nếu đổi.
7. 10 tier súng lục (khoảng 10 ảnh), 10 tier pháo (20-30 ảnh), icon `weapon-mechanic-*`, `offhand-mechanic-*`.

Còn lại không cần vẽ:
- Đuôi đạn nhát 1 che mặt; viên đạn nhát 4 đè tóc khi bắn lên, qua hông chân khi bắn xuống.
- Đạn hướng lên, xuống lệch súng 10-18 đơn vị. Trên ảnh hướng ngang lệch 1-3,5 đơn vị.
- Tagline, tên món phụ, icon Mechanic còn ghi trụ. `death` còn dáng cũ.
- Súng lên, xuống vẫn nằm ngang khi đạn bay thẳng. `hurt` mất một súng ở nhiều frame. `attack1` lật ảnh nên hộp ở hông đổi bên 200 ms.
- Chưa xác nhận người khác thấy Mechanic mới khi online.
- Skill trụ `mechanic.deploy-turret` (`TurretRules.cs`): chưa thử offline, chưa nghe âm thanh, chưa thấy ảnh quay trái; hero che trụ khi đứng lên nó; trụ hơi nhỏ. Bỏ được `skills/robot-assault/`, `robot-assault.json`, nhánh drone trong `SkillMinion`, `SkillDescribe`.

**Cần người dùng chốt:**
- Cỡ súng `s` 0,15 (đề xuất giữ).
- Trang phục đang lệch: `idle`, `walk` không tay áo; `attack1`, `attack2` tay áo nâu; `attack3`, nhát 4 tay áo trắng và áo gi-lê; mẫu có đai lớn. Đề xuất theo `idle`, `walk`, vẽ lại khoảng 12 hàng attack.
- Nhát 4 tăng windup 140 lên khoảng 260 ms để thấy móc pháo (đề xuất có, phải build lại server).
- Tên lửa Rocket Barrage bay từ nòng pháo: đang lệch 19-22 đơn vị và phủ hero sau nửa giây. Phải sửa hiệu ứng skill, chỉ làm nếu người dùng đồng ý.
- Pháo các tier: đề xuất hướng lưng tô màu bằng script.
- Đạn nhát 4 nhìn 32x20, vùng trúng 12x12: đề xuất giữ, chơi thử rồi tính.
- Skill trụ: số liệu tạm 12 giây, 15 phát 65%, tầm 6 ô, đánh một mục tiêu (Robot Assault cũ đánh diện rộng); trụ tiếp tục bắn khi chủ chết, hết khi chủ rời vùng.

## Việc trên Tan Pham từ 07:41 ngày 2026-11-02

- **Warrior:** 5 khiên `thunder`, `verdant`, `shadow`, `holy`, `dragon`: tải từ `shield-src/ref/`, lưu `<tier>-pair-chatgpt.png`, chạy `Tools/build-warrior-shield.py <tier>`, rồi `icons`.
- **Wizard:** `attack3` xuống, lên, trái; `attack1`, `attack2` trái rồi bỏ khỏi `MirrorLeft`; vẽ lại `idle` phải (`Tools/build-wizard-new-style.py`).
- **Assassin:** `melee3`, `melee4`, `nightfall`, `hurt` xuống (`Tools/build-assassin-new-style.py`). Combo đang lẫn nét.
- **Cleric** (`Tools/build-cleric-new-style.py`): `clericcast` 4 hướng, bỏ cánh và áo cũ, thêm vào `HandAnims`, thêm tay vào `FREE_FIST` của `Tools/build-cleric-shield.py`. Vẽ lại tư thế `attack3` trái frame 3-5, `attack1` phải frame 2-3, soi `attack3` xuống và phải: nhân vật dáng trẻ con nên tư thế không được lộ đùi hay dưới váy. Trang phục giữ váy ngắn, người dùng không yêu cầu áo dài: không tự đổi. `hurt` đang áo dài do Claude tự vẽ, hỏi người dùng có đổi không.

## Xem trong game, chưa ai xem

Mới xem ảnh ghép hoặc ảnh tĩnh:
- Gậy Wizard, gậy và khiên Cleric, dao Assassin (tier đầu trông tối và mảnh trên người; frame đánh cũng tối, chưa rõ nguyên nhân), cường hoá từ +7.
- Pet khi di chuyển, bấm thật nút Mua và Trang bị, Trade có pet, pet biến mất trên máy người khác, hết độ bền khi offline, hero chết khi đeo pet.
- Đăng nhập và quên mật khẩu trên điện thoại, gửi mail Gmail thật. APK `C:\temp\apk\Zenonia.apk` chưa cài thử trên máy thật: nút, chữ, video màn chọn tướng.
- Chuyển động từng class: giật frame, vũ khí trong tay, đường chém. Đo cỡ đầu mọi hàng so với idle hướng xuống (`grow_head`).
- Smoke test `FrostSmokeTest` trong Unity chưa chạy lại.

## Lỗi đã biết, chưa sửa

- Gậy `archangel` có chấm đỏ nhỏ. Gậy Cleric đè tay khiên ở `attack2` xuống frame 1, `attack3` xuống frame 1 và 3. Hướng phải `attack1` frame 2, 3 khiên nằm sau lưng. Cast và dash ẩn khiên. Idle xuống của 3 class Wizard, Assassin, Cleric vẽ liền vũ khí nên không theo đồ.
- Wizard: 6 tier đá xanh dễ nhầm, pha lê chạm cằm ở `walk` phải frame 1. Assassin: nhiều tier cùng tím; bóng Shadow Clone cắt mũi dao ở 59 frame, nhát `melee3` còn dao cũ.
- Warrior: `melee2` frame giữa tay lệch tới 57 px; `iron` thường và cường hoá chung một ảnh.
- Nhân vật cũ có thể còn tên món phụ "Spirit". Xem lại "Việc còn tồn" trong `docs/sprite-weapon-rules.md`.
- **Chỉ số món phụ không có tác dụng:** `DmgReduction`, `RobotDmg`, `RobotRate`, `CastSpeed`, `MpCostCut`, `CritDmg`, `HealPower`, `ShieldPower` chỉ được đọc trong các file món phụ, mô tả đồ vẫn quảng cáo. Làm thật sau khi xong art.

## Số liệu chỉnh sau khi chơi thử

- Pet (`PetConfig.cs`): `spirit` +15% máu, `imp` +10% sát thương, 3000 vàng, độ bền 1000. `imp` ước tính chỉ 8-17 phút nếu đánh liên tục: có lẽ phải tăng. Chưa có sửa độ bền. `imp` không tăng hồi máu.
- "Holy Shield" Cleric: +2 DEF, +10 HP, +6 MP.

## Deploy: người dùng chưa cần ngay

Tên game Pocket Valor, tên miền `pocketvalor.luckyday.io.vn`, VPS Ubuntu có sẵn nginx, MongoDB riêng ngoài VPS.
1. Cài Linux Build Support và Linux Dedicated Server cho Unity 6000.3.9f1.
2. DNS bản ghi `A` `pocketvalor` về IP VPS, chế độ DNS only nếu dùng Cloudflare.
3. Điền `MONGO_URI`, `SMTP_USER`, `SMTP_PASS`, `SMTP_FROM` trong `deploy/.env.prod` (git đã bỏ qua, không commit).
4. `.\Tools\package-server.ps1`, chép lên VPS, `bash start.sh`, theo `docs/deploy.md`. Deploy `server-api` trước zone vì save lên bản 2.
5. Bản phát hành: `Tools/build-from-copy.ps1 -Release -ApiUrl https://pocketvalor.luckyday.io.vn`.

Chưa chạy thật trên Ubuntu, chưa có bản server Linux, nginx và certbot chưa chạy lần nào, mongodump thật chưa chạy. Protocol đang 12: build lại client và server cùng lúc. Chờ người dùng chốt: bản phát hành mở thẳng màn đăng nhập (đề xuất có; hiện mở màn chọn tướng offline).

## Tính năng chờ làm

- Bom Mechanic làm skill (người dùng chưa quyết).
- Cánh thành item cho mọi class. Sprite class không vẽ cánh.
- Boxing: chỉ có art màn chọn tướng và bị khoá. Art ở `Assets/ArtLibrary/custom/idle-down-set/boxing.png`.
- Màn đăng nhập, đăng ký có hình riêng: đang dùng tạm UI kit (`AccountScreenGui.cs`).
- Party chat chưa qua mạng; hero của mình có thể bị che trong đám đông.

## Trước khi phát hành

`Assets/Resources/Data/dev-start.json` đang bật cheat: level 60, thêm 100 điểm skill, đủ vũ khí, đá 9999, 2 pet. Bản test luôn đọc file này (người dùng dặn), bản phát hành không đọc. Phím F10 là phím test tạm.

## Bản build để xem

- APK test: `C:\temp\apk\Zenonia.apk`. Build từ `C:\temp\zu-copy-sel` bằng Unity `6000.3.9f1-x86_64`, bản này mới có module Android.
- Bản Windows test: `C:\temp\player-ui\Zenonia.exe`. Mechanic bản thử: `C:\temp\mech-build\Zenonia.exe`.
- Video so sánh Warrior cũ và mới: `C:\temp\compare\warrior-cu-vs-moi-1-danh-thuong.mp4`, `warrior-cu-vs-moi-2-skill.mp4`. Bản Warrior cũ: `C:\temp\cmp-old\Zenonia.exe`.
