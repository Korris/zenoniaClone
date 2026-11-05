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
| Mechanic `attack1-3` hướng xuống (chi tiết bên dưới). Idle xuống, lên đã vẽ 2026-11-02 ở https://chatgpt.com/c/6ae84d8a-4a1c-83ec-8063-dcadf178c2f8 | **Tay** | Chat mới |
| Mechanic `hit4-down`, `hit4-up` | **Tay** | Chat mới. Bản cũ: https://chatgpt.com/c/6ae70b65-5d84-83ec-a677-1cf3c6b7e905, https://chatgpt.com/c/6ae70bcc-d228-83ec-af67-d31216487669 |
| Mechanic `rocket-right`, `rocket-down`, `rocket-up` | **Tay** | Chat mới; tham khảo `hit4-right` https://chatgpt.com/c/6ae70af7-1008-83ec-b698-51a562f88558 |
| Mechanic `attack2` phải | **Tay** | Chat mới, prompt ở `dual-src/move/STATUS.txt` |
| Mechanic trang phục: chờ người dùng chốt | **Tay** | Chat mới |
| Mechanic art tia lửa đầu nòng pháo | **Tay** | "Generate Blast Sprite Sheet" https://chatgpt.com/c/6ae7106f-a52c-83ec-b2d7-1bbbaef3e34d: có prompt và ảnh tham chiếu, chưa ra ảnh |
| Mechanic pháo các hướng, súng lục mẫu | **Tay** | https://chatgpt.com/c/6ae704e9-21fc-83ec-a957-e02a9128798d |
| Mechanic hình màn chọn tướng: vẽ lại dáng giơ súng tự nhiên, đồng bộ trang phục | **Tay** | "Create hero image" https://chatgpt.com/c/6ae702cb-0f78-83ec-9428-5d9a1e1ba8a1 |
| Mechanic 9 tier súng nhìn dọc nòng cho idle xuống, lên, sau khi người dùng duyệt dáng này. 30 hình pháo cường hoá nếu người dùng muốn | **Tay** | Chat mới |
| Khiên Warrior 5 tier còn lại | **Tan Pham** | "Create shield image" https://chatgpt.com/c/6ae6ae1b-27b0-83ec-aa78-66d544aa811e |
| Wizard, Assassin các hàng thân còn lại | **Tan Pham** | Chat mới |
| Assassin combo vẽ mới (2026-11-03), **đã ghi vào game**, chưa xem trong game: đòn 1 chém ngang, đòn 2 tay trái hất lên, đòn 3 bật lên chém chữ X rồi ngồi dang dao, đòn 4 lao đâm hai nhát. Nhân vật nữ theo hình màn chọn tướng. Còn dở: hướng xuống của đòn 4 đâm vào camera nên khó thấy; hiệu ứng chém đã đổi sang nét chém Warrior màu tím (`DaggerFx.Swing`, `SlashEffect` classTint), mới xem hướng phải; thời gian đòn giãn 1,3 lần, hệ số sát thương tăng 1,3 lần (`ComboConfigs.AssassinHits`), phải build lại server khi chơi online; `nightfall`, `hurt` xuống vẫn kiểu cũ; walk, idle, hurt dáng nhân vật cũ (nơ to) nên lệch nét với combo mới | **Tay** | https://chatgpt.com/c/6ae99899-6464-83ec-8a1a-2b061b5c8726 (bản dùng); chat cũ https://chatgpt.com/c/6ae991ac-a098-83ec-9910-5a4320ecabba vẽ sai thành nam, bỏ |
| Dao Assassin 10 tier **v2** (2026-11-03 tối): mỗi tier một kiểu vũ khí theo sheet ChatGPT `dagger-src/ref-chatgpt-10-weapons-v2.png` (tier 1 là cặp dao tím của màn chọn tướng; vuốt hổ, kiếm đâm, dao rắn, dao băng hai đầu, đao lửa, vòng sét, liềm đầu lâu, nguyệt đao, đao hư không). **Đã cài đủ 10 tier** (art, grip, icon, sheet `armed`) bằng `Tools/build-assassin-new-style.py dagger-install` rồi `armed`, đã xem ghép idle, melee1, melee2 của 3 tier, chưa xem trong game. Ảnh gốc `<tier>-pair-chatgpt.png` (1024 x 1536, trái Normal, phải Enhanced), bộ v1 sáng cùng ngày ở `dagger-src/old-20261103-v1/`, bộ trước nữa ở `old-20261103/`. Điểm cầm của 6 tier không phải dao thường đặt cố định ở bảng `GRIP_Y`. Người dùng chê nhỏ trong game nên art dao nâng 258 lên 400 px (`DAGGER_LEN`, 81 lên 126 px khung, khoảng 3/4 nhân vật); idle hướng xuống tách dao thành doll (`Tools/build-assassin-idle-down-doll.py`). Đã chụp trong game (`C:\temp\cap-dagger\`): idle phải, idle xuống, đòn đánh với vuốt. Hiệu ứng cường hoá +9 trên dao mờ hơn kiếm Warrior (quầng tím mỏng, chưa thấy vân sáng trong lưỡi), chưa chỉnh. Chưa commit | **Tay** (hiện tên "Tân Korris", browser cb671945…) | https://chatgpt.com/c/6ae8db29-6900-83ec-a73a-0d1f6d148fa4 (chat "Tạo warrior 2D", có cả thiết kế vũ khí Warrior, Mechanic); bộ v1 https://chatgpt.com/c/6ae9abed-f84c-83ec-9ddc-2570deae62c1 |
| Cleric `clericcast`, tư thế `attack3`, `attack1` | **Tan Pham** | Chat mới |
| Pet mới, khiên Cleric thêm | **Tay** | "Create Holy Shield Design" https://chatgpt.com/c/6ae701c0-e12c-83ec-93fe-e6b3a1c83e59; `imp` ở `Assets/ArtLibrary/pets/` |
| Map mới cạnh Green Village (Oakhaven), lối ra phía đông, hơi hướng game "Brown Skies Adventure RPG" (đã nhắc tên game trong prompt, yêu cầu thiết kế riêng, không chép). Đã gửi 2026-11-05 kèm ảnh chụp làng và `oakhaven-ground.png`. ChatGPT ra bản 1 (1536 x 1024), đặt tên "Amberfall Trail": cỏ xanh phía tây chuyển dần sang đất vàng, vách đá phía bắc, trại bỏ hoang phía tây nam, phế tích phía nam, lối ra phía đông. Người dùng sau đó bảo bỏ hết yêu cầu cũ, vẽ theo hẳn phong cách Brown Skies: bản 2 "The Forgotten Frontier" (tông nâu đất trầm, vách sa thạch, khu định cư bỏ hoang có tháp canh, giếng, xe ngựa, cầu đá, bia cổ). ChatGPT không tìm được hình thật của game này nên vẽ theo mô tả. Nét bản 2 lệch hẳn với làng. Sau đó người dùng gửi ảnh mẫu: game thật là **Brown Dust 2** (tên "Brown Skies" là gọi nhầm). Theo hướng môi trường Brown Dust 2 (vẽ tay kiểu anime, nắng ấm, nhà mái tranh), nhân vật vẫn theo style của mình: bản 3 "Sunlit Thatch-Roof Village Square" (làng 5 nhà mái tranh, quảng trường, giếng, sạp chợ, suối, cầu gỗ) và bản 4 "Willowbrook Meadows" (bãi săn cạnh Green Village: cổng làng phía tây, đồng cỏ lớn giữa map, rừng nhỏ phía bắc, suối và cầu gỗ phía đông, lán bỏ hoang và đống rơm phía nam). Người dùng chốt (2026-11-05): làng mái tranh đặt cạnh Green Village (cổng phía đông), bãi săn Willowbrook cạnh làng mới (cuối đường), quái dùng lại loại có sẵn Lv 1-5, làng mới chưa có NPC. Ảnh gốc đã ở `Assets/ArtLibrary/hd/map/thatch-village/thatch-village-chatgpt.png`, `willowbrook-meadows/willowbrook-meadows-chatgpt.png`. **Đã code (chưa commit)**, plan `plans/261105-1006-thatch-village-willowbrook-maps/`: `ThatchVillageMap.cs` (tên hiển thị Thatchwick Village), `WillowbrookMeadowsMap.cs`, cổng đông Oakhaven (35,10-11), Willowbrook 16 quái Lv1-2 không boss, protocol 14 (phải build lại server, client và server phát hành cùng lúc), `Tools/build-map-backdrop.py`. Test: ZenoniaNet 298, ZenoniaSkills 188 đều qua. Build từ bản sao thành công, đã chụp trong game (`C:\temp\cap-newmaps-1`): quảng trường Thatchwick, 2 cổng Thatchwick, cổng tây và bãi cỏ Willowbrook, cổng đông Oakhaven trên cầu. Một lần chụp thấy hero đi qua cổng Oakhaven sang Thatchwick. Chưa đi tay khứ hồi, chưa chạy `net-persist-test`, chưa thử online. Quái băng (slime, sói xanh) lạc tông với đồng cỏ xanh; có slime sát cổng tây Willowbrook. **Willowbrook đã tách prop (2026-11-05)**: ảnh nền chỉ còn đất (`willowbrook-meadows-ground-chatgpt.png`), 45 prop `wb*` cắt từ ảnh gốc (`Tools/build-willowbrook-props.py`), cây chỉ chặn ô gốc. Build lại, đã chụp (`C:\temp\cap-newmaps-2`): hero đi ra sau tán cây bị che hết, đi trước cây thì hiện bình thường. Thưa hơn ảnh gốc (thiếu bụi nhỏ, đá ven suối), cây không có bóng. Người dùng chọn làm bãi săn lớn 72x48, đang lập kế hoạch `plans/261105-1052-willowbrook-large-hunting-map/`. **Sức chứa (ChatGPT phân tích 2026-11-05, cùng chat):** map 36x21 ô chơi được, camera thấy khoảng 18x10 ô, nên mỗi map chỉ khoảng 2x2 màn hình; quá nhỏ cho 30-60 người cùng lúc. Đề xuất: làng 20-25 người/kênh, bãi săn 10-15 người/kênh, Willowbrook nâng lên khoảng 24 quái chia 4-5 cụm, hồi sinh 7 giây; map săn sau này 72x48 ô với 50-80 quái. Server hiện chưa có kênh (mỗi map một zone, tối đa 200 người/server). Người dùng chưa chốt | **Tay** | "Map Design Request" https://chatgpt.com/c/6aebe98d-e3f4-83ec-8ffa-608486cdbd64 |
| Quái đồng cỏ mới cho Willowbrook 72x48 (người dùng chốt 2026-11-05): thỏ sừng Lv2, lợn rừng gai Lv3-4, bọ ngựa Lv5, người cây bụi gai Lv6; Lv1 dùng lại `bogSlime`. Định dạng: walk 768x768 (4x4 ô 192, hàng xuống/lên/trái/phải), attack 1024x1024 (ô 256, chân ở 224). Đang vẽ thử con thỏ, nền #FF00FF | **Tay** | "Create Rabbit Sprite Sheet" https://chatgpt.com/c/6aec0250-ccb4-83ec-8811-83442a1274bf |
| Tinh linh các tier, để dành làm pet | **Tay** | "Generate Creature Image" https://chatgpt.com/c/6ae6fa3f-426c-83ec-9ad4-7885e446362b |

Mechanic ưu tiên số 1 nhưng art `dual-src` ở Tay, nên chờ Tay lúc 15:30 để giữ đồng nhất. Muốn vẽ sớm trên Tan Pham thì hỏi người dùng và so kỹ nét, màu.

Các chat tham khảo khác trên Tan Pham: "Thiết kế sprite 2D" https://chatgpt.com/c/6ae6908e-40d8-83ec-96f7-024bc648cece (bộ 6 tướng, nguồn hình mẫu), "Vẽ kiếm game 2D" https://chatgpt.com/c/6ae68a74-d94c-83ec-b644-d617b67a150d (kiếm `holy`, `dragon`), "Vẽ lại nhân vật ngang trái" https://chatgpt.com/c/6ae69bbd-31d4-83ec-a67d-9d43bfe7d84b (Warrior idle trái). Trên Tay: https://chatgpt.com/c/6ae61667-9900-83ec-a607-145b6d0e7347 (màn chọn tướng).

## Mechanic: ưu tiên số 1

Người dùng dặn: thêm agent thoải mái miễn đồng nhất. Thiết kế: 2 súng lục (một món, chỉ số tính một lần) + pháo đeo chéo sau lưng. Mẫu `Assets/ArtLibrary/classes/mechanic/new-style/dual-src/hero-test-2-back.png`. Combo: tay trái, tay phải, hai tay (một lần sát thương, tia thứ 2 chỉ là hình), móc pháo ra bắn. Trụ là skill, một hình, không tier.

Trong game đang là bản thử: súng lục đã có đủ 10 tier, tên trong `held-mechanic-dual.json` là tên tier. Duyệt xong mới vẽ tier. Cài art khi có: `python Tools/build-mechanic-dual.py write` (xem docstring), đặt lại `FISTS`, cập nhật `HeroFx.MuzzleCannon`.

Thứ tự vẽ khi Tay có lượt (prompt ở `dual-src/attack/STATUS.txt`):
1. **Attack hướng xuống, người dùng chê 2 tay bè ra 2 bên nhìn hài.** `attack1`, `attack2`, `attack3` xuống: 2 tay đưa về phía trước, sát nhau hơn, súng chĩa theo hướng bắn. `attack3` là tư thế cast chung của skill: xem cả skill hướng xuống. Mẫu nhân vật: `dual-src/ref/hero-select-five-heroes.png` (người dùng dặn). Idle xuống, lên đã vẽ lại theo mẫu này và đã cài, chờ người dùng xem trong game; trang phục theo màn chọn tướng nên các hàng attack xuống đang lệch với idle.
   **Idle xuống, lên: súng đã chĩa theo hướng mặt (2026-11-02), chờ người dùng xem trong game.** Dùng art nhìn dọc nòng `weapons/pistol/pistol-front.png`, `pistol-back.png` (nguồn `dual-src/pistol-front-back.png`, chat https://chatgpt.com/c/6ae85922-b950-83ec-be6a-8e6c4942f5ca) qua `art` của anchor. Mọi tier đang dùng chung hình tier đầu, không có hiệu ứng cường hoá ở 2 hàng này; muốn theo tier thì vẽ thêm 9 ảnh, đặt ở `weapons/pistol/<tier>/pistol-front.png`, `pistol-back.png`, game tự lấy.
2. `hit4-down`: cầm pháo dọc, nòng chĩa xuống, 2 nắm tay trên một đường dọc giữa thân. `hit4-up`: tương tự, nòng chĩa lên, thấy gáy.
3. `rocket-*`: 2 tay ôm pháo chĩa xéo lên trời, ngả người ra sau, 4 frame. Hero khoá 850 ms, tên lửa phóng ở 400, 440, 480 ms. Thân đang là bản tạm.
4. `attack2` phải: thấy cả tay xa cầm súng thứ 2.
5. Art tia lửa đầu nòng nhát 4: phóng thẳng từ nòng theo hình nón, lõi trắng xanh, không cầu lửa, khói tròn, mảnh vỡ, rất mạnh. Đang dùng hình tạm; đổi ở khối `ART` đầu `MechanicCannonBlastFx.cs`.
6. Hình màn chọn tướng dáng giơ súng tự nhiên, rồi idle-down cùng dáng. Quay lại clip preview nếu đổi.
7. 10 tier súng lục: đã cài ngày 2026-11-02, gồm bản thường, bản cường hoá và icon `weapon-mechanic-*`, chưa xem trong game (`Tools/build-mechanic-pistol-tiers.py`, ảnh gốc ở `dual-src/tiers/`).
   **10 tier pháo: đã cài ngày 2026-11-02**, 9 tier `cannon/<tier>/{front,back,side}.png` (scrap giữ art hiện có) và 10 icon `offhand-mechanic-*`, chưa xem trong game (`Tools/build-mechanic-cannon-tiers.py`, ảnh gốc ở `dual-src/tiers/cannon/`, icon cũ ở `tiers/cannon/old-icons/`, chat ChatGPT `https://chatgpt.com/c/6ae854ff-b5f4-83ec-aa3c-c865549cecf3`, tài khoản Tay). Pháo chưa có bản cường hoá: code chưa vẽ hiệu ứng cường hoá trên art của anchor. Người dùng sẽ cho hiện hiệu ứng cường hoá trong ô đồ, nên icon để lề khoảng 10%, không có quầng.
   **2026-11-03 tối, bộ v2 theo sheet của người dùng** `refs/ref-user-10-guns-and-cannons.png` (mỗi hàng một tier: MASTER, BRASS, COPPER, PLASMA, EMBER, FROST, VOLT, GATLING, STAR, DRAGON; cột súng Normal, súng Enhanced, pháo ngang, pháo đầu nòng về người xem, pháo đuôi về người xem). ChatGPT (chat "Tạo warrior 2D" https://chatgpt.com/c/6ae8db29-6900-83ec-a73a-0d1f6d148fa4, tài khoản Tay, sheet đã đính trong chat) vẽ riêng từng tier: 10 ảnh súng `tiers/<tier>-pair-chatgpt.png` (trên Normal, dưới Enhanced, nòng sang phải) và 9 ảnh pháo `tiers/cannon/<tier>-chatgpt.png` (3 góc như `cannon-masters.png`, nhưng tả bằng chữ, không tải ảnh mẫu vì tải kèm thì ChatGPT chỉ tô màu khẩu mẫu). **Đã cài đủ 10 tier súng (cả scrap, tier 0 nay cũng lấy từ ảnh ChatGPT chứ không giữ khẩu mẫu) và 9 tier pháo (scrap giữ art mẫu), icon `weapon-mechanic-*`, `offhand-mechanic-*`.** Bộ v1 (cùng hình bóng khẩu mẫu) ở `tiers/old-20261103-v1/`, `tiers/cannon/old-20261103-v1/`. Tool đổi: súng co đều theo chiều dài mẫu (không bóp vào khung 258x238 nữa), đặt góc dưới trái, grip dò từ hàng đáy lên; pháo co đều theo chiều dài, canvas nở đối xứng khi dày hơn mẫu (`GearOverlays.Centered` lấy tâm làm pivot nên không lệch); icon từng tier tự đóng khung theo art của nó. Đã xem ảnh ghép (`sheet` của hai tool): súng trên tay ở idle phải, attack2 trái, attack3 phải, idle xuống; pháo 4 hướng và attack4; icon trên nền tối, sáng. **Chưa xem trong game, chưa commit.** Điểm bắn đạn (`tip` trong `build-mechanic-dual.py`) vẫn tính theo khẩu mẫu nên tier dài dẹt có thể lệch vài đơn vị; hiệu ứng cường hoá chưa vẽ trên pháo.

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
- **Wizard:** `attack3` xuống, lên, trái đã vẽ và cài ngày 2026-11-04 (xem mục dưới). Còn `attack1`, `attack2` trái rồi bỏ khỏi `MirrorLeft`; vẽ lại `idle` phải (`Tools/build-wizard-new-style.py`).
- **Assassin:** `melee3`, `melee4`, `nightfall`, `hurt` xuống (`Tools/build-assassin-new-style.py`). Combo đang lẫn nét.
- **Cleric** (`Tools/build-cleric-new-style.py`): `clericcast` 4 hướng, bỏ cánh và áo cũ, thêm vào `HandAnims`, thêm tay vào `FREE_FIST` của `Tools/build-cleric-shield.py`. Vẽ lại tư thế `attack3` trái frame 3-5, `attack1` phải frame 2-3, soi `attack3` xuống và phải: nhân vật dáng trẻ con nên tư thế không được lộ đùi hay dưới váy. Trang phục giữ váy ngắn, người dùng không yêu cầu áo dài: không tự đổi. `hurt` đang áo dài do Claude tự vẽ, hỏi người dùng có đổi không.

## Xem trong game, chưa ai xem

Mới xem ảnh ghép hoặc ảnh tĩnh:
- Gậy Wizard, gậy và khiên Cleric, dao Assassin (tier đầu trông tối và mảnh trên người; frame đánh cũng tối, chưa rõ nguyên nhân), cường hoá từ +7.
- Pet khi di chuyển, bấm thật nút Mua và Trang bị, Trade có pet, pet biến mất trên máy người khác, hết độ bền khi offline, hero chết khi đeo pet.
- Đăng nhập và quên mật khẩu trên điện thoại, gửi mail Gmail thật. APK `C:\temp\apk\Zenonia.apk` chưa cài thử trên máy thật: nút, chữ, video màn chọn tướng.
- Chuyển động từng class: giật frame, vũ khí trong tay, đường chém. Đo cỡ đầu mọi hàng so với idle hướng xuống (`grow_head`).
- Smoke test `FrostSmokeTest` trong Unity chưa chạy lại.
- Quest online, dấu `?`, `!` trên đầu NPC và minimap, cửa sổ danh sách quest (`QuestLogWindow`, nút Quests): có unit test, build được, chưa chạy thử trong game, chưa xem dấu minimap cỡ 15 có rõ không (`QuestSize` trong `Minimap.cs`). Người dùng tự test.

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

Chưa chạy thật trên Ubuntu, chưa có bản server Linux, nginx và certbot chưa chạy lần nào, mongodump thật chưa chạy. Protocol đang 13: build lại client và server cùng lúc. Chờ người dùng chốt: bản phát hành mở thẳng màn đăng nhập (đề xuất có; hiện mở màn chọn tướng offline).

## Đang bàn dở (2026-11-02): map tự do kiểu MU, chưa sửa code

Người dùng muốn: bỏ Boss Gate, đi map tự do, độ khó nằm ở quái chứ không cấm khám phá. Giữ quest Elder (tuỳ chọn) và phần quest. Không đổi dungeon, không sửa hệ thống không liên quan. Đã báo cáo audit, **chưa code gì, chờ người dùng chốt 3 điểm rồi mới làm**:

1. Cấp đeo đồ = tier × 10, tối đa 60 (`MaxLevel` = 60). Chỉ kiểm tra lúc Equip, đồ đang đeo giữ nguyên. Vẫn trade và giữ đồ cao cấp được, cường hoá không tăng yêu cầu.
2. Frostfang King giữ Lv 3 (đứng ở (7,5) giữa bãi slime đầu game).
3. Nút "Return to Town" hiện khoảng 10 giây sau khi hồi sinh.

Thứ tự làm khi được chốt: gate và cảnh báo, EXP, đồ, quái, về thành.

- **Gate:** chỉ có một chỗ, portal Den sang Sunspire. Bỏ gate, boss vẫn ở đó để lấy đồ, có thể thêm cấp đề xuất hoặc cảnh báo.
- **EXP:** nhân theo cấp quái trừ cấp người nhận, 100/60/25/10%. Không cấm kéo cấp. Khoảng 240 con để lên Lv 20, so với 200 khi đánh một mình và 90 khi chưa nhân.
- **Quái:** Frost Hollow Lv 1-10, Den Lv 13-18, theo công thức HP 14+22L, Atk 3+4L, Def 1+L, EXP 14L, vàng L..2L+1.
- **Warden:** giữ lại. Tính ra đường vòng cách 107 đơn vị, aggro 85, đi qua (19,14), (24,19), (33,17) tới (34,10). Chưa thử trong game.

**Cấp tối đa** (người dùng hỏi sao không nâng): nâng được, đổi cùng lúc `Leveling.MaxLevel` và `MAX_LEVEL` trong `server-api/src/rpg/rpg-constants.js`; EXP không tràn số (tới Lv 100 là 1.975.018). Vướng ở skill tree: học full cần 80 SP, cấp 60 cho 60 SP nên phải chọn hướng; lên 80 thì hết phải chọn, trên 80 thì dư SP. Quái hiện cao nhất Lv 35. Đề xuất giữ 60, nâng khi có map cao và thêm skill hoặc rank. Người dùng chưa trả lời.

## Warrior dáng kiếm dựng thẳng (2026-11-03): người dùng dừng, idle trong project là bản cũ

Người dùng chốt bản 1 (`design-centered-side-1.png`), cỡ kiếm 0,34. Đã vẽ và cài thử idle 4 hướng rồi bị bác vì ốm, dài hơn nhân vật cũ (xem Sổ lỗi 2026-11-03); đã vẽ lại 3 ảnh đúng tỉ lệ cũ (`new-style/centered/v2-idle-down-bare.png`, `v2-idle-up-bare.png`, `v2-idle-sides-bare.png`, chat https://chatgpt.com/c/6ae8644a-23f8-83ec-a15f-4e32aa0e5b50, tài khoản Tay, hiện tên "Tân Korris", deviceId `cb671945-164b-41cc-b92d-0c0a7e8c311f` đêm 2026-11-03; Tan Pham là `581b7ddf-...`). Người dùng bảo dừng vẽ, giữ sprite cũ; thay vào đó mở rộng ô nhân vật trong màn Bag và cho đứng giữa (đã làm, xem mục Màn Bag). Tool `Tools/build-warrior-centered-idle.py` (lệnh `grid`, `preview`, `write`) đã trỏ sang v2 nhưng bảng `GEAR` còn toạ độ của bộ ốm: nếu làm tiếp phải đọc lại nắm tay từ lưới `grid` rồi `preview`, `write`. Bộ ốm đầu tiên giữ ở `centered/idle-*-bare.png` để đối chiếu. Hàng idle cũ sao lưu ở `centered/old/`.

Nếu làm tiếp: v2 đầu to hơn bản cũ 14% (tóc 95 so với 83 px), có thể co bằng `grow_head`; walk, hurt vẫn dáng cũ nên kiếm nhảy từ thẳng sang chéo khi bước; hướng trái kiếm ở tay xa sau thân lòi 80 px dưới chân; `armed/idle-down-big.png` còn art cũ.

Máy này đã cài Unity 6000.3.9f1 (01:16 ngày 2026-11-03), bản copy để build ở `C:	emp\zu-copy`, exe test ở `C:	emp\player-centered\Zenonia.exe`.

Câu hỏi còn chờ về viền ô đồ: giữ màu độ hiếm nhưng làm mờ hơn (đề xuất) hay bỏ hẳn cho giống hình thử. Ở ô trống không có đồ hợp loại, dòng "Pick an item from the list" cần đổi chữ.

## Kiếm Warrior theo bộ vũ khí MU (2026-11-03): đã cài bộ v2, chờ người dùng xem trong game

Người dùng chốt hình mẫu cuối là `sword-src/ref-mu-weapons-wanted.png` (hero có cánh, 10 set: Balrog, Chaos Axe, Divine Sword, Star Lance, Crimson Blade, Thunder Hammer, Lightning Blade, Dark Reaper, Dragon Slayer, Frost Blade). Cách người dùng dặn: chụp Warrior idle xuống đang cầm kiếm trong game (`sword-src/ref-ingame-idle-down-sword.png`) gửi kèm hình mẫu cho ChatGPT, bảo vẽ y chang. Chat "Generate Weapon Image" https://chatgpt.com/c/6ae8e429-c390-83ec-8f7e-25ee59bbe541 (tài khoản Tay), 5 ảnh 4 vũ khí dựng thẳng: `sword-src/mu-ref-v2/pair1..5-*.png`. Gán tier: iron=Lightning Blade, bronze=Star Lance, crimson=Crimson Blade, flame=Balrog, frost=Frost Blade, thunder=Thunder Hammer, verdant=Chaos Axe xanh lá, shadow=Dark Reaper, holy=Divine Sword, dragon=Dragon Slayer. Cài bằng `Tools/build-warrior-weapons-from-sheet.py write` (mặc định `SHEET_SET=mu-ref-v2`, `ART_H=250`: vũ khí trên màn hình 170 px, xấp xỉ nhân vật như hình mẫu), icon làm lại. Bộ v1 ngắn hơn (`mu-ref/`, `ART_H=200`, theo `ref-mu-weapons-4dir.png`) bị bác vì "không giống". Art gốc trước đêm nay ở `sword-src/old-20261103/`.

Idle hướng xuống: người dùng bác thân mới theo hình mẫu (`centered/mu-pose-idle-down-bare.png`) vì tỉ lệ mặt khác hero hiện tại, đã trả về idle cũ (`git checkout` 3 sheet idle và `held-gear-melee.json`). Kiếm cũ chéo -135 ở hông vẫn dùng với art mới. Chưa xem: nhát chém với hiệu ứng, cỡ vũ khí ở các hướng khác. Chưa làm: khiên 10 set theo hình mẫu (người dùng chưa yêu cầu).

Thêm 2026-11-03 (sau 02:40): idle hướng xuống đổi góc kiếm -135 thành 145 (lưỡi chéo vào trong, qua trước chân về góc dưới phải, theo `cccc.png` người dùng gửi; chỉ đổi `angle` trong `held-gear-melee.json`). Khiên 10 set cùng hình mẫu: chat "Shield lineup image" https://chatgpt.com/c/6ae8e8da-104c-83ec-ae50-2b4d46885af3, 5 ảnh `shield-src/mu-ref-v2/shield-pair1..5-*.png`, cài bằng `PART=shield python Tools/build-warrior-weapons-from-sheet.py write` (art cao 380, grip giữa, icon `offhand-warrior-*` qua `build-warrior-shield.py icons`), art cũ ở `shield-src/old-mu-shield-20261103/`. Tên món theo set: `ItemDefs.WeaponTierNames`, `OffhandTierNames` (Warrior) dùng trong `ItemFactory.Generate`; đồ đã có trong save giữ tên cũ ("Legendary Sword") vì tên lưu trong item; tên hiện ở ô chi tiết bên phải màn Bag khi chọn món. Chưa làm: tên trên ô lưới, đổi tên đồ cũ trong save, các class khác.

## Hiệu ứng chém thường (2026-11-03): chỉ còn vệt kiếm trắng đỏ như cũ

Thử theo thứ tự trong một buổi: (1) phương án C của ChatGPT, đường chém vẽ sẵn (`SlashEffect`) lõi trắng viền màu `glow` vũ khí; (2) người dùng bảo bỏ lõi trắng, nhuộm cả đường chém và vệt kiếm theo màu vũ khí; (3) chỉ một hiệu ứng, bỏ vệt kiếm; (4) nhát 4 thành một đường chém dọc; (5) cuối cùng người dùng bảo bỏ hẳn đường chém vẽ sẵn, giữ vệt kiếm (`SwordTrail`) màu class như cũ. Hiện `HeroFx.Swing` chỉ gọi `SwordTrail.Spawn(... cls.Palette)`. Code còn giữ để dùng lại: `SlashEffect` (frame theo màu vũ khí, nhát 4 là crescent cao 1,35), `SpriteTint.ColorizeBody`, `ClassPalette.ForWeapon`, `HeroRenderer.WeaponGlow`, tham số `weaponGlow` của `HeroFx.Swing`. Chat ChatGPT: https://chatgpt.com/c/6ae91e29-4910-83ec-879e-dcc7ad4ecc43. Ảnh thử ở scratchpad `slash/game2..5`. Sau đó người dùng đổi ý: vệt kiếm lấy màu vũ khí (`ClassPalette.ForWeapon(weaponGlow, cls.Palette)`: lõi sáng cùng tông, không trắng; tay không thì màu class). Đã build, chụp, vệt mờ nhưng đúng màu xanh, tím, vàng theo tier. Cuối cùng (08:20) người dùng bảo trả về như cũ có 2 hiệu ứng: `HeroFx.Swing` gọi lại cả `SlashEffect.Spawn` (màu class, nhát 4 là vòng) và `SwordTrail.Spawn(cls.Palette)`; muốn màu vũ khí thì truyền `weaponGlow` vào hai chỗ đó (code vẫn còn).

Độ nét vũ khí (08:15): người dùng thấy vũ khí trong game và icon trong túi mờ hơn chân dung. Nguyên nhân: game vẽ 4 px mỗi đơn vị, chân dung 8 px, icon 96 px vẽ ở 50 px, đều thu qua mipmap. Đã thử cắt art bằng 2 lần cỡ màn hình và SAI: cỡ trên màn hình = art px x s x DrawScale nên vũ khí và khiên nhỏ theo; đã trả về 250/330 và thêm unsharp mask khi cắt, icon dựng 104 px từ ảnh gốc có làm nét (`ICON_PX`, `icon_of` trong tool). Muốn nét hơn nữa phải đổi `s` cùng lúc với cỡ art ở mọi anchor, chưa làm.

## Hiệu ứng cường hoá +9, +11, +15 quá dày (2026-11-03): đã có đề xuất, chưa sửa code hiệu ứng

Người dùng chê dày, không sạch. Đã chụp trong game Warrior kiếm `dragon` (đủ idle 4 hướng, walk phải, attack 3 hướng) và `frost` (idle xuống, phải, attack phải), gửi ChatGPT: https://chatgpt.com/c/6ae94546-d91c-83ec-85ee-d71f5b99d5d6 (tài khoản Tay). Chụp bằng `--class warrior --tp 4,21 --face <hướng> --god --plus N --worn-tier T --zoom 30 --shots ...` (`--worn-tier` mới thêm vào `DebugCapture.cs`).

ChatGPT xếp nguyên nhân: (1) art `-enhanced` có viền dày, silhouette rộng hơn; (2) quầng + bloom; (3) `ShineLift`; (4) dải sáng quá rộng, mạnh; (5) sao, đốm. Hướng đề xuất: art cường hoá trùng silhouette bản thường, không viền neon; thêm viền sáng mỏng làm lớp chính; quầng sát vũ khí; +15 thêm một xung năng lượng 0,35-0,5 giây mỗi 5-7 giây.

Số ChatGPT đề xuất, chưa thử trong game:

| | +9 | +11 | +13 | +15 |
|---|---|---|---|---|
| Chu kỳ / thời gian quét (s) | 4 / 1,1 | 3,5 / 1 | 3 / 0,85 | 2,5 / 0,75 |
| Nửa bề rộng dải | 0,075 | 0,08 | 0,085 | 0,095 |
| `ShineAlpha` / `ShineLift` | 0,24 / 0 | 0,30 / 0 | 0,36 / 0,01 | 0,43 / 0,02 |
| `HaloAlpha` / `HaloBreath` | 0,12 / 0,02 | 0,17 / 0,025 | 0,23 / 0,035 | 0,30 / 0,045 |
| Sao / cỡ | 0 | 1 / 1,2 | 1 / 1,4 | 2 / 1,6 |
| Đốm | 0 | 0 | 2 | 4 |
| Viền mỏng (lớp mới) | 0,22 | 0,32 | 0,42 | 0,55 |

Khi đánh: viền x0,85, quầng x0,45, dải sáng x0,35, tắt sao và đốm, chuyển trong 0,08-0,12 giây. Khi đi: quầng x0,7, dải sáng x0,7.

Lưu ý khi làm: ChatGPT ghi viền dày 0,75-1,25 px art và quầng lan 1-2 px art, nhưng 250 px art chỉ còn khoảng 95 px trên màn hình 720p (1 px art khoảng 0,4 px màn hình), nên phải tính theo px màn hình, khoảng 2,5-3 px art. Game dùng `BloomEffect` riêng, không phải URP. Đề xuất đổi preset theo nền tuyết, nền tối chưa cần làm.

**Đã làm thử (2026-11-03, chưa commit, chờ người dùng xem trong game):** đồ cầm tay dùng art thường cả khi cường hoá (`FeelConfig.EnhanceUsesBaseArt`, pet và robot vẫn dùng art cường hoá); viền mỏng mới (`HeldRimBaker.cs`, shader `SpriteRim` vẽ alpha thường, vì additive không hiện trên tuyết); quầng nửa độ phân giải, lan 12 px art; số liệu theo bảng trên (`FeelConfig.EnhanceLooks`); giảm khi đi, đánh (`EnhanceMotionK`); +15 có xung chạy dọc viền mỗi 6 giây. Ô đồ trong túi dùng chung bảng số nên hiệu ứng ở ô cũng nhẹ đi. Viền xanh của kiếm frost vẫn khó thấy trên tuyết. Ảnh so cũ/mới ở scratchpad của phiên, không lưu.

Người dùng xem bản thử: sạch, không rối, nhưng các cấp không khác nhau rõ. Hỏi lại ChatGPT (cùng chat), đề xuất mỗi cấp thêm một nét riêng, cấp sau giữ nét cấp trước:
- +7: viền màu mỏng. +9: rãnh năng lượng trong lưỡi sáng lên, rộng 2-3 px màn hình, sáng liên tục (quan trọng nhất). +11: năng lượng chảy dọc rãnh, chu kỳ 1,5 s. +13: 1-2 hạt hình thoi 0,55-0,75 đơn vị bay dọc lưỡi, bỏ sao và đốm cũ. +15: màu phụ (dragon `#FFF0B0`, frost `#8068FF`) và một viên thoi nổi cố định cạnh 1/3 trên của lưỡi, 1,1 đơn vị, nhấp nhô 1,8 s; xung 6 s giữ làm điểm nhấn phụ.
- Viền alpha 0,25/0,35/0,43/0,50/0,60, rộng 0,8-1,4 px màn hình; quầng 0/0,08/0,12/0,16/0,22, lan 0-4 px màn hình; rãnh alpha 0/0,45/0,55/0,65/0,75.
- Khi đánh vẫn giữ viền, rãnh, viên thoi 100%, dòng chảy 75%, quầng 35%, dải sáng 25%, hạt 0. Khiên chỉ có viền và rãnh yếu hơn, không có viên thoi.
- Frost: viền xanh đậm `#167BFF` thay vì cyan nhạt, có thể thêm nét viền tối mỏng bên ngoài trên tuyết.
- Phương án khác: vòng phép dưới chân cho +15. Không dùng vì trái quy tắc "vòng dưới chân dành cho buff".
**Đã làm thử (2026-11-03, chưa commit, chờ người dùng xem trong game):** `HeldEnergyBaker.cs` tự lấy màu năng lượng từ art (hue nổi nhất, bỏ dải vàng kim loại 18-62°) và vùng rãnh có màu đó; màu này thay `glow` của manifest cho mọi lớp hiệu ứng (manifest còn màu cũ, ví dụ búa thunder xanh mà `glow` vàng). Rìu `verdant` đen bạc không có màu năng lượng nên chỉ có viền. `HeldEnhanceFx` vẽ: +9 rãnh sáng, +11 dòng chảy, +13 hạt thoi, +15 màu phụ + viên thoi (`FxSprites.Gem`). Số đã tăng so với bảng ChatGPT vì ở cỡ thật còn quá nhạt: viền 0,3-0,8, rãnh 0,7-1, viên thoi 2 đơn vị, hạt 1-1,15. Ô đồ trong túi giữ sao và đốm như cũ (`ItemEnhanceSlotFx.CellLooks`). Chưa xem: hạt +13 và dòng chảy (ảnh chụp tĩnh khó bắt), các class khác, pet và robot, viên thoi đè mặt khi đánh.

Thêm sau đó (cùng ngày): +15 có hào quang năng lượng động quanh lưỡi (shader `SpriteAura`, mask `HeldRimBaker.BakeAura` vươn 22 px art) và bóng mờ vũ khí khi đi, đánh (`HeldEnhanceFx.DrawGhosts`). Viền giờ dày dần theo cấp (`RimWidth` 0,16-0,32 của vùng hào quang, khoảng 1,3-2,7 px màn hình). Người dùng chê +1 và +7 gần như giống nhau: +7 đã tăng viền 0,6 và rãnh sáng 0,35, ảnh chụp vẫn chỉ hơi khác, chờ người dùng xem trong game.

Người dùng thấy lúc chơi vẫn khó nhận ra vì nhân vật nhỏ (camera 160 đơn vị, nhân vật 20, kiếm khoảng 95 px ở 720p). Đã thử cả hai hướng: (2) hào quang động có ở mọi cấp từ +7, vươn dần `AuraReach` 0,2/0,36/0,5/0,72/1; (1) tuỳ chọn test `cameraViewHeight` trong `dev-start.json` (0 = 160 mặc định; `DevStartConfig.CameraViewHeight`, `CameraFollow.Configure`), chỉ bản test đọc. Chờ người dùng chơi thử và chọn độ zoom; nếu đổi thật thì sửa `GameConfig.ViewHeightUnits` và bỏ tuỳ chọn này.

Người dùng bảo giữ cỡ hào quang nhưng bớt đậm: `AuraAlpha` còn 0,22/0,3/0,33/0,42/0,5, shader mềm cạnh và mỏng dần ra ngoài, bóng mờ 0,35. Chờ người dùng test.

ChatGPT trả lời câu "nhân vật nhỏ quá" (cùng chat, để làm sau theo lời người dùng): ở 90 px, hiệu ứng cần hình rõ bên ngoài kiếm chứ không phải chi tiết bên trong; vật nhỏ hơn khoảng 6 px không ai để ý. Đề xuất: vỏ năng lượng vươn 0,35-1,35 đơn vị, 1-3 ngọn lửa lớn dài 1,3-3 đơn vị, mảnh năng lượng rời ở +13, +15 có vệt năng lượng kéo dài khỏi mũi kiếm 4,5 đơn vị (khoảng 20 px); bỏ viên thoi và hạt nhỏ làm dấu hiệu chính. Camera: thử 136, chưa dùng 120, nhưng zoom chỉ to thêm 18% nên không thay được việc sửa hiệu ứng.

Thử phóng to nhân vật (camera giữ nguyên): tuỳ chọn test `heroScale` trong `dev-start.json` (0 = bình thường), `HeroRenderer.FigureScale` phóng thân, đồ, hiệu ứng quanh chân; bóng và vùng va chạm giữ nguyên, áp cho hero trong thế giới (`GameSession`, 3 chỗ tạo hero), không áp chân dung màn Bag. Ảnh ở cỡ thật: ×1,25 hào quang +15 rõ hơn chút; ×1,4 +15 rõ, +11 thấy viền; +1 và +7 vẫn gần giống. Quái, cây, ô map không to theo nên hero to hơn so với cảnh. Chưa chơi thử.

ChatGPT lần 4 (cùng chat, sau bản hào quang nhẹ), chưa làm: giữ hào quang nhẹ làm nền (0,20-0,42), thêm "đường năng lượng" mảnh sáng chạy dọc kiếm từ +9 (rộng 1,5-3 px màn hình, alpha 0,5-0,95, chảy 2,4-0,9 s), +11 có đợt sáng mạnh 0,3 s mỗi 3 s, +13 thêm 1 ngọn lửa rời 6-8 px chạy dọc lưỡi, +15 thêm 2 ngọn và một lưỡi năng lượng kéo dài khỏi mũi kiếm 18-20 px, rộng 6-8 px, ruột trong suốt, hai mép sáng. Phóng nhân vật: nên thử 1,15 trước, tối đa 1,2; tránh 1,3 khi chưa thử cảnh đông 5 class.

Đã làm thử đề xuất lần 4: đường năng lượng là viền có dải sáng chạy liên tục (`PulsePeriod`), đợt sáng bùng `SurgeEvery`/`SurgeSec`, ngọn lửa rời (`FxSprites.Flame`, thay hạt thoi), lưỡi năng lượng ở mũi kiếm +15 (`HeldEnhanceFx.Tip.cs`, `FxSprites.TipBlade`); bỏ viên thoi. `dev-start.json` đặt `heroScale` 1,15. Người dùng chê +15 vẫn đậm: đã hạ viền 0,65, rãnh 0,7, hào quang 0,28, lưỡi mũi kiếm 0,5, lửa 0,5. Bản test mới ở `C:\temp\player-enh2` (bản cũ đang mở). Lưỡi ở mũi kiếm nhạt, ảnh chụp khó thấy, chưa xem trong game.

Nút test tạm "-1" cạnh nút Enhance (`InventoryPanel.DevDowngrade.cs`): hạ một bậc, chỉ bản test và chơi offline. **Bỏ trước khi phát hành:** xoá file đó và 2 dòng gọi trong `InventoryPanel.Detail.cs`.

## Combo chém Warrior đổi hướng (2026-11-03): đã làm, chưa commit, chờ người dùng xem

Người dùng yêu cầu: đòn 1 chém trái qua phải, đòn 2 phải qua trái, đòn 3 chém từ dưới lên, đòn 4 giữ nguyên. Không vẽ mới: `Tools/build-warrior-combo-directions.py` tạo `melee1lr` (thân `melee1`), `melee2rl` (thân `melee1` chạy ngược), `melee3up` (đã vẽ mới, xem dưới) trong `body/{bare,armed,hand}`, cùng anchor kiếm/khiên trong `held-gear-melee.json` (vị trí theo nắm tay của frame nguồn, góc kiếm theo bảng `SWEEP_*`, giữ kiếm cùng phía nắm tay để không chém xuyên đầu). `ComboAnims` đổi sang 3 đòn mới + `melee4`; `melee1`, `melee3` cũ giữ trong `ExtraAnims` cho Earth Break và Blade Cyclone. Chạy lại tool sau `build-melee-sheets.py` (tool đó ghi đè file anchor). Chưa làm: tư thế thân vẫn là thân cũ chạy xuôi hoặc ngược nên có frame tay và kiếm không khớp hẳn hướng chém; vệt kiếm (`SwordTrail`) đòn 3 vẫn là cung ngang, chưa quét dọc từ dưới lên; mới xem ảnh ghép và ảnh chụp hướng xuống trong game.

Đòn 3 vẽ mới (2026-11-03, người dùng chê bản chạy ngược không giống chém lên): ChatGPT chat "Draw Uppercut Sprite Row" https://chatgpt.com/c/6ae97092-8cbc-83ec-8e9c-25967fe224cd (tài khoản Tay), 4 ảnh `new-style/attack/src/melee3up-{down,up,left,right}.png`, thân tay không, hướng trái kiếm ở tay xa. Cài bằng `python Tools/build-warrior-attack.py write melee3up <dir>` (nắm tay trong `FISTS`, góc kiếm `NEW_ANGLES`, khiên hạ khỏi mặt ở frame ngồi thấp, `SWORD_FRONT`). Đã xem ảnh ghép 4 hướng và ảnh chụp combo hướng phải trong game. Hiệu ứng chém đòn 3 đi từ dưới lên: `HeroClassDef.RisingStep` = 3, `HeroFx.RisingArc` đặt cung bên phía tay kiếm, chạy từ thấp lên cao, tâm ở giữa người (`RisingLift`); mới xem hướng xuống và phải trong game. Chưa xem: cỡ đầu hàng lên (scale 0,47) chưa đo so với hàng khác.

Hiệu ứng đòn 4 (2026-11-03): người dùng chê vòng tròn xoay khi chém bổ xuống. ChatGPT vẽ nhát bổ dọc 4 frame trên nền đen (cùng chat đòn 3), ảnh gốc `ArtLibrary/classes/warrior/fx-src/cleave-chatgpt.png`, cắt bằng `Tools/build-warrior-cleave-vfx.py` ra `Sprites/classes/warrior/fx/warrior-cleave-vfx.png`. `SlashEffect` dùng ảnh này cho đòn cuối thay vòng tròn (không xoay theo hướng, lật khi quay trái); vệt kiếm đòn 4 chạy từ cao xuống thấp bên phía tay kiếm (`HeroFx.Swing`, `chop`). Đã xem trong game hướng xuống, phải. Ảnh hiệu ứng nằm ở vùng trúng đòn trước mặt nên hướng phải nhát bổ ở xa phía trước người.

## Cửa sổ Settings (2026-11-03): đã làm, chưa commit, chờ người dùng xem

Nút bánh răng dưới minimap giờ mở cửa sổ Settings (khung `win-panel`, nút vàng): bật/tắt nhạc nền, bật/tắt âm thanh hiệu ứng (lưu `PlayerPrefs` theo máy, không lưu vào save hay server), nút "Change hero" đi đúng đường cũ `HeroChangeScreen.Open` (offline màn chọn tướng; online có tài khoản thì menu tài khoản "Change character"; kết nối dev chỉ báo không đổi được). Code: `SettingsWindow.cs`, `SettingsToggle.cs`, `AudioPrefs.cs`; tắt tiếng ở `GameMusic` (giảm dần về 0), `GameSfx` (mute cả âm đang phát), `SkillSfx.Loop`, `BladeCycloneUltimate`. Art icon nhạc, loa, công tắc ON/OFF: `Sprites/ui/settings/`, cắt bằng `Tools/build-settings-ui-art.py` từ `ArtLibrary/custom/settings-ui/settings-art-sheet.png`; hình thử `settings-mockup-1.png`. ChatGPT (tài khoản Tay): https://chatgpt.com/c/6ae9758d-9b2c-83ec-8410-c3d399d4b91f. Chụp thử: `--open-settings`, `--settings-show on,off`, `--settings-press ChangeHero`, `--open-quests`.

Đã xem ảnh chụp ở 1280x720 và 960x640, và bấm Change hero offline (màn chọn tướng mở, game dừng). Chưa thử: nghe thật khi tắt/bật (bản chụp không bấm công tắc để không ghi PlayerPrefs của máy), online, điện thoại. Âm thanh khác ngoài 4 chỗ trên: không có (video màn chọn tướng tắt tiếng sẵn).

## Skill không hồi chiêu (2026-11-03): đã sửa, chưa commit

Người dùng muốn spam skill như game cày cuốc: `SkillTable.CooldownScale` = 0, mọi skill `CooldownMs` = 0, giới hạn còn MP và thời gian khoá khi niệm (`CastLockMs`). Số hồi chiêu thiết kế giữ trong `BaseCooldownMs`; đặt scale 1 là trả lại. Mô tả skill hiện "No cooldown". Thời gian khoá khi niệm cũng giới hạn 0,45 s (`SkillTable.MaxCastLockMs`, giá trị gốc ở `BaseCastLockMs`); skill có lướt/nhảy giữ khoá tới hết di chuyển (Nightfall Execution 0,68 s). Sát thương và hình vẫn theo `AtMs` cũ, nên skill lớn (Earth Break 1,45 s gốc, Ice Storm, Rocket Barrage, Orbital Cannon, Astral Cataclysm) bị cắt động tác niệm của nhân vật, hiệu ứng vẫn chạy tiếp; chưa xem các skill này trong game, mới xem skill chính của Warrior bấm mỗi 0,5 s. Bảng skill dùng chung với zone server: phải build lại server khi deploy. Bản test vẫn bật `noCooldowns`, `noMpCost` trong `dev-start.json`, nên chơi thử chưa thấy khác; nên xem lại giá MP khi tắt `noMpCost`.

## Màn Bag mới (2026-11-02): đã làm, chờ người dùng xem

2026-11-03: ô nhân vật rộng 336 (trước 288), lưới giữa 350 với ô 62 (trước 398, ô 72), ô phải 210; Warrior đứng giữa ô (`WarriorShiftX` 0, texture 672 x 528). Chưa xem trên điện thoại.

Trang bị và túi đồ chung một màn kiểu Mu, khung `win-panel`, hình thử `Assets/ArtLibrary/custom/inventory-ui-ref/bag-layout-mock-1.png`, ChatGPT https://chatgpt.com/c/6ae85e92-4e1c-83ec-a0c9-58c911146787. Code: `InventoryPanel.cs`, `.Loadout.cs`, `.Bag.cs`, `.Detail.cs`, `HeroPortraitView.cs`. Chạm ô đang mặc thì túi lọc đúng loại, chỉ món class đó đeo được. Chưa xem: popup Stats, chạy online, trên điện thoại. `BagSprites.FrameTitle`, `FrameBody`, `TabArrow`, `PortraitBackdrop` và `bag-frame` không còn dùng ở màn này.

## Fighter, class thứ 6 (2026-11-04 đêm): code xong, art đang làm, chưa build, chưa commit

Plan `plans/261104-0025-fighter-class/`, báo cáo code `plans/reports/agent-261104-0030-fighter-class-code.md`. Ô "boxing" ở màn chọn tướng nay là Fighter, chọn được; phím 6 ở màn chọn, F11 đổi class trong game (F6 đã là bloom).

Đã có trong code (`HeroClass.Fighter` thêm cuối enum, id `fighter` thêm cuối `RpgClasses.Ids`, không đổi số các class cũ):
- Chỉ số brawler (HP 95, Atk 13, Def 4, Speed 88, crit +4%), combo 4 đấm jab / cross / hook / uppercut trên `melee1..melee4` (`ComboConfigs.FighterHits`, tầm ngắn hơn dao, nhát 4 x2,2 đẩy lùi mạnh), FX chém dùng tạm nét chém Warrior tô cam (`ClassPalette.Orange`), âm thanh mượn Warrior + tiếng hét Assassin (`ClassSfx`).
- Vũ khí: một món "Gloves" vẽ hai phần `off` + `main` từ `Sprites/classes/fighter/weapons/glove/<tier>-{base,enhanced}.png`, manifest `Data/classes/fighter/held-fighter.json` (10 tier leather, iron, bronze, tiger, flame, frost, thunder, shadow, holy, dragon). Món phụ = găng thứ hai (template `glove`, +crit rate, +max HP). Tên tier trong `ItemDefs.WeaponTierNames / OffhandTierNames`.
- 8 skill chỉ dùng effect chung (`SkillTable.Fighter.cs`): Dash Punch (L), Iron Fist, Ground Slam, Rapid Jabs, Shockwave, Tiger Rush, Iron Body, Dragon Fist. Chưa có art skill: `SkillVfx` vẽ vòng tròn cam tạm. Server dùng chung bảng này: phải build lại server.
- Test dotnet: `ZenoniaSkills.Tests` 188, `ZenoniaNet.Tests` 279 đều qua (máy này chỉ có SDK .NET 9, chạy qua bản sao csproj net9.0 trong scratch; csproj gốc vẫn net10.0).

Còn thiếu, chờ hai agent art (phase 2, 3): sheet thân `Sprites/classes/fighter/body/{armed,bare}/*.png`, `anchors` trong `held-fighter.json`, art găng và icon `Sprites/icons/weapon-fighter-*.png`, `offhand-fighter-*.png`. Thiếu thì game không crash: `SpriteSheet.Load` chỉ log "Missing texture", hero vô hình, găng không vẽ, icon trống.
Găng 10 tier (phase 3, 2026-11-04 01:10, xong, chưa commit): vẽ ở chat "Vẽ găng boxing" https://chatgpt.com/c/6aea18f3-4748-83ec-b4f7-379f0507523f (tài khoản Tay, 10 lượt, không phải vẽ lại), nguồn `ArtLibrary/classes/fighter/new-style/glove-src/<tier>-pair-chatgpt.png`, cài bằng `Tools/build-fighter-gloves.py install` (art 200x200 `weapons/glove/`, grip trong `held-fighter.json`, icon `weapon-fighter-0..9`, `offhand-fighter-0..9` lật ngược). Đã xem sheet nền tối, nền sáng và icon; chưa xem trong game (chưa có thân, anchor). Lưu ý: lửa bản cường hoá `dragon` bị cắt 20 px ở mép phải canvas; `flame`, `thunder`, `holy` nắm đấm nhỏ hơn `leather` vì lửa hoặc cẳng tay dài tính vào bề ngang 170 px. Báo cáo `plans/reports/agent-261104-0030-fighter-glove-tiers.md`.
Thân Fighter (phase 2, 2026-11-04 00:30-01:30, xong, chưa commit, chưa xem trong game): 24 dải ChatGPT trong một chat "Create Brawler Sprite Strip" https://chatgpt.com/c/6aea1911-3668-83ec-ab96-0fafd90ba8a6 (tài khoản Tay "Tân Korris", 24 lượt, không phải vẽ lại lần nào, không cần sang Tan Pham), nguồn `ArtLibrary/classes/fighter/new-style/src/<anim>-<dir>.png` (idle, walk, hurt, melee1-4, dash x down/up/right; trái = phải lật). Cài bằng `Tools/build-fighter-new-style.py write` (+ `Tools/fighter-strip-helpers.py`): `body/{bare,armed}/{idle,walk,hurt,death,melee1-4,dash}.png` (`death` = bản sao `hurt`), meta chép từ Assassin, 64 anchor `main`/`off` trong `held-fighter.json` (`s` 0,18, `sx` -1 ở nắm tay phía xa hàng xuống/lên và hàng lật). Nắm tay đọc bằng mắt vào `FISTS` (khoảng 3 px), hướng knuckle tính theo cánh tay rồi kẹp vào nón của hướng nhìn (`CONE`, `GUARD`, `ANGLE_FIX`), găng sau thân theo `BEHIND`. Đã xem ảnh ghép 32 hàng với găng `leather`. Còn tồn: hàng lên tóc còn đỏ (ChatGPT vẽ vệt sáng tóc cùng màu khăn, `match_hair` chỉ kéo được phần tối), nắm tay đấm thẳng vào camera (`melee1`, `melee2` xuống frame 2) to hơn găng nên lòi ra dưới găng. Báo cáo `plans/reports/agent-261104-0030-fighter-body-sprites.md`.
Chưa làm: `server-api/src/rpg/rpg-constants.js` `CLASS_IDS` chưa có `fighter` nên tạo nhân vật online sẽ bị API từ chối (ngoài phạm vi agent code, cần thêm `'fighter'` và sửa test node); icon skill `Sprites/classes/fighter/skill-icons/`, âm thanh riêng `Audio/sfx/fighter`; `--record-party` vẫn 5 class; README chưa ghi Fighter.

## Tính năng chờ làm

- Bom Mechanic làm skill (người dùng chưa quyết).
- Cánh thành item cho mọi class. Sprite class không vẽ cánh.
- Màn đăng nhập, đăng ký có hình riêng: đang dùng tạm UI kit (`AccountScreenGui.cs`).
- Party chat chưa qua mạng; hero của mình có thể bị che trong đám đông.

## Trước khi phát hành

`Assets/Resources/Data/dev-start.json` đang bật cheat: level 60, thêm 100 điểm skill, đủ vũ khí, đá 9999, 2 pet. Bản test luôn đọc file này (người dùng dặn), bản phát hành không đọc. Phím F10 là phím test tạm.

## Bản build để xem

- APK test: `C:\temp\apk\Zenonia.apk`. Build từ `C:\temp\zu-copy-sel` bằng Unity `6000.3.9f1-x86_64`, bản này mới có module Android.
- Bản Windows test: `C:\temp\player-ui\Zenonia.exe`. Mechanic bản thử: `C:\temp\mech-build\Zenonia.exe`.
- Video so sánh Warrior cũ và mới: `C:\temp\compare\warrior-cu-vs-moi-1-danh-thuong.mp4`, `warrior-cu-vs-moi-2-skill.mp4`. Bản Warrior cũ: `C:\temp\cmp-old\Zenonia.exe`.

- Màn Equipment (2026-11-03): người dùng muốn nhân vật hiện đúng vũ khí đang mặc ở cả 5 class, vẫn nhìn xuống (đã thử quay hướng phải, bị bác). Làm doll idle xuống cho Assassin (`Tools/build-assassin-idle-down-doll.py`), Wizard và Cleric (`Tools/build-idle-down-doll-from-redraw.py`, ChatGPT vẽ lại thân tay không, chat https://chatgpt.com/c/6aea0959-50c4-83ec-be08-fc7b177e7272, tài khoản Tay). Warrior đã có doll. Mechanic: idle xuống vẽ súng qua anchor `pistol-front` dùng chung cho mọi tier, pháo `front` theo tier; muốn súng theo tier phải vẽ 9 hình nhìn dọc nòng. Chưa commit. Hình thử: `C:\temp\cap-dagger\inv-*.png`.

- Wizard gậy 9 tier (2026-11-03 tối đến 2026-11-04 rạng sáng, chưa commit): người dùng chốt sheet 9 cây mới (chat "Thiết kế gậy phép", tài khoản Tay, https://chatgpt.com/c/6aea0e41-46f4-83ec-8942-3b7200747a01), lưu `ArtLibrary/classes/wizard/new-style/staff-src/ref-user-9-staffs.png`. Thứ tự trong sheet = tier 2..10: wood (Gậy Tinh Tú trăng khuyết), crystal, moon, arcane (mắt tím), flame, ice, nature (dây leo, nền magenta), holy (cánh), void (Hắc Long Đế Vương). Đã vẽ riêng từng cây 2 bản trong cùng chat, lưu `<tier>-src.png` (bản cũ ở `staff-src/old-20261103/`), cài bằng `build-wizard-new-style.py staff-write` cả 9 tier: sprite, grip trong `held-wizard.json`, icon `weapon-wizard-1..9`. `apprentice` giữ art cũ. Đã xem ảnh ghép idle/walk/attack: tay nắm đúng thân, gậy dựng thẳng; còn lỗi cũ `walk`/`idle` phải frame 1 đầu gậy đè má (chưa sửa anchor). Chưa xem trong game. Mảnh pha lê bay rời của `crystal` bị cắt bỏ vì tool chỉ giữ 2 khối liền lớn nhất.
- Gậy Wizard (2026-11-04 0:20): người dùng chê gậy nhìn không lực, nâng art 263 lên 390 px (`STAFF_H`, 117 lên 174 px khung, cao hơn nhân vật một chút), cài lại cả 10 tier kể cả `apprentice` từ `staff-src/apprentice-src.png`; các hằng hàng art nhân `STAFF_K`. Đã xem ghép 10 tier idle xuống, walk phải, attack phải. Lỗi cũ đầu gậy đè mặt ở `walk` phải frame 1 và `idle` phải frame 1 giờ rõ hơn, chưa sửa (đổi `ANGLE` hoặc `behind` cho 2 frame đó trong `build-wizard-new-style.py`). Chưa commit.
- Wizard `attack3` xuống, lên, trái (2026-11-04 0:20-0:40, chưa commit): ChatGPT vẽ lại 3 dải 6 frame theo thiết kế mũ trùm mới trong chat https://chatgpt.com/c/6aea1832-fccc-83ec-b1f4-f1276f848a42 (tài khoản Tay), mỗi dải một lượt, không phải vẽ lại lần nào; lưu `new-style/src/wizard-attack3-{down,up,left}.png`. Ảnh mẫu: dải `attack3` phải (tư thế) và dải `attack2` xuống (thiết kế nhìn thẳng); không tải `hero-select/wizard.png` vì chân dung đó đội mũ nhọn, không phải mũ trùm. Nắm tay đọc vào `FISTS`, góc cũ sai ở xuống frame 4, 5 và lên frame 0, 2, 4, 5 đổi trong `ANGLE_FRAME`, rồi `write`: chỉ hàng 0-2 của `bare`, `armed`, `hand/attack3.png` và anchor `attack3` đổi, idle xuống (doll) giữ nguyên (đã đối chiếu từng hàng với bản sao). Đã xem ảnh ghép tier 9 cả 18 frame; chưa xem trong game. Còn tồn: hàng lên lộ tóc trắng dưới mũ trùm trong khi `attack1`, `attack2` lên chỉ có mũ; `attack1`, `attack2` trái vẫn lật từ phải (`MirrorLeft`); `idle` phải chưa vẽ lại.
- Súng lục Mechanic (2026-11-04 0:15): người dùng chê nhỏ, `s` anchor `gun`, `gunL` 0,15 lên 0,26 (39 lên 67 px khung, `PISTOL_S` trong `build-mechanic-dual.py`), hình nhìn dọc nòng `pistol-front`, `pistol-back` chỉ lên 0,195 vì art đó vẽ to sẵn (0,26 thì che cả người). Bảng điểm bắn `MuzzleLeftHand`, `MuzzleRightHand`, `MuzzleBothRight`, `MuzzleBothLeft` trong `HeroFx.cs` ghi lại từ `build-mechanic-dual.py muzzles`. Đã xem ảnh ghép 10 tier; chưa xem đạn trong game. Chưa commit.
- Fighter tổng hợp (2026-11-04 01:35, phiên chính): 3 agent xong code, 24 dải thân, 10 tier găng; `compose-hero-frame.py` thêm `fighter`; găng `GLOVE_S` 0,18 lên 0,24 (48 px) rồi `write` + `armed` lại; `server-api` `CLASS_IDS` thêm `fighter`, test `characters.test.js` đổi 5 thành 6 nhưng chưa chạy được (vitest chưa cài trong `server-api/`). Build Unity OK. Chưa commit. Plan: `plans/261104-0025-fighter-class/`.
- Fighter idle/walk xuống (2026-11-04 02:05): vẽ lại tay buông dọc thân vì găng che mặt; `FISTS` và `ANGLE_FIX` idle/walk down đổi, `write` + `armed` lại. Bản thủ cũ ở `new-style/src/old-guard-20261104/`.
