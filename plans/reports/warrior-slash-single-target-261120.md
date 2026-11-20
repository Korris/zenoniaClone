# Warrior: đòn khoá 1 mục tiêu + vệt chém (20/11/2026)

Yêu cầu: "thêm 1 con agent làm hiệu ứng vệt chém slash vfx cho warrior ko làm combo attack nữa giờ là attack cứng lock target nhân vật chém vào 1 con quái duy nhất".

## Đã làm
- Combo 4 nhát -> 1 nhát nặng: `ComboConfigs.WarriorHits` = windup 120 / active 90 / recover 190 ms (400 ms), x1,75 (gần DPS đơn mục tiêu của chuỗi cũ 5,2x / ~1,18 s), knockback 0, Reach 34. `ComboSystem`: chuỗi 1 nhát thì bấm trong lúc chém = chém tiếp (buffer).
- Khoá mục tiêu (`HeroClassDef.LockTargetMelee`, chỉ Warrior): `Rpg/LockTargetRules.cs` (thuần, có test) + `HeroController.LockTarget.cs`. Mỗi windup chọn quái: giữ quái đã khoá nếu còn sống và trong tầm (ngực -> mép hộp đánh <= Reach + 8), không thì quái gần nhất trong tầm; quay 4 hướng về nó theo mặt đất (chân -> chân); hướng nhìn 8 hướng cũng nhìn đúng quái. Đòn rơi: chọn lại (giữ khoá nếu còn hợp lệ), sát thương CHỈ quái đó; không có quái thì chém hụt. Không còn hộp chém lan.
- Online: server (authority) và client dự đoán chạy cùng code; client chỉ quay mặt + vẽ hình, server quyết sát thương. Proxy (người khác) vẽ vệt hướng về quái gần nhất trong tầm theo hướng mặt được gửi về.
- Vệt chém: ChatGPT (Tan Pham) vẽ 4 frame lưỡi liềm đỏ trên #00FF00 -> `Tools/build-warrior-lock-slash-vfx.py` (`cut`, `preview`, `check`) -> `Sprites/classes/warrior/fx/attack/lock-slash.png`. `WarriorLockSlashFx.cs`: xoay về quái, lật dọc ở nửa trái (vẫn quét trên xuống), mép trước tới quá chân quái 6 đv, chém lên thì vẽ sau hero; 200 ms phát đúng lúc đòn rơi (sự kiện Hit của combo = frame f1). Âm: whoosh nặng slash4, bỏ tiếng hét finisher. Hit-stop to chỉ khi crit.
- Test: `LockTargetRulesTests` (chọn / giữ khoá / quay mặt / chuỗi 1 nhát lặp), sửa `ComboKnockbackTests`. `ZenoniaSkills.Tests` 463 pass, `ZenoniaNet.Tests` 455 pass.
- Docs: README (bảng class, phím J), sprite-rules (dòng bảng + 3 mục Sổ lỗi), pending-tasks.

## Đã xem
- Ảnh xem trước nền tối / sáng cạnh vệt Warrior cũ: `docs/reference/warrior-lock-slash/preview.png`.
- Ghép offline 8 hướng như game vẽ: `offline-8dir-check.png`.
- Trong game (offline, Oakhaven, `--record-combo warrior --face left|up|right|down --dummies 3`, 60 fps): `ingame-{left,up,right,down}-frames.jpg`, `ingame-right-f42-full.jpg`. Mỗi nhát chỉ hiện 1 số sát thương (1 quái), vệt đi từ kiếm tới quái, quay đúng hướng quái.
- 2 lần sửa sau khi xem (ghi Sổ lỗi): vệt to và đè người hero; hero quay chéo lên khi quái ngang hàng.

## Chưa xem / còn lại
- Online thật (zone server + client) chưa chạy. PHẢI build lại zone server (bảng combo dùng chung), nếu không client cũ / server cũ lệch nhịp và vẫn chém lan.
- Điện thoại, WebGL, đường vẽ classic (`--hero-style classic`, chỉ còn dùng `melee1lr`).
- Art Warrior RO Fantasy hàng đánh chéo lên (lên-trái / lên / lên-phải) kiếm chĩa sai phía: khoá mục tiêu làm lộ rõ hơn. Cần vẽ lại bằng ChatGPT.
- `--zoom` của `--record-combo` không có tác dụng (camera giữ cỡ dev-start), ảnh chụp ở tầm nhìn thường.
- Build dùng chung bản sao `C:\temp\zu-copy` (C: chỉ còn ~6 GB, không đủ tạo bản sao riêng; Library 21 GB). Không chạy lúc agent khác build.

## Lượt 2 (20/11 chiều): tự đi tới quái + vòng khoá
- Người dùng: (1) có tự đi tới quái, (2) có vòng khoá offline, (3) giữ 400 ms / x1,75.
- `AttackChase.cs`: bấm đánh khi không quái nào trong tầm -> chọn quái (đang khoá nếu trong 8 ô, không thì gần nhất trong 8 ô, đi được trong 12 bước ô) -> giữ phím di chuyển theo `TileNav` (gần + thấy thẳng thì đi thẳng 8 hướng) -> vào tầm (chừa 4 đv) thì khoá quái đó và bấm chém. Nuốt phím đánh của người chơi lúc đang đi (không chém hụt từng frame). Dừng: bấm di chuyển, quái chết, xa > 12 ô, > 6 s, kẹt 1 s, Auto bật.
- Online: chỉ là phím (như AutoBattle) nên server vẫn quyết di chuyển và sát thương; không đổi giao thức, không đổi server.
- Vòng đỏ `TargetMarker` dưới quái đang đi tới / mới khoá (4 s), offline và online. Offline không vẽ thanh máu IMGUI trên đỉnh (đè hàng icon HUD gọn; online vẫn vậy như trước).
- Đã xem: offline `chase-offline-frames.jpg` (hero tự đi trái tới slime, vòng đỏ, chém 23), `chase-offline-marker-f78.jpg` (chụp trước khi tắt thanh máu offline); online server + client local trên Frost Hollow, client chỉ giữ phím đánh (`--net-bot chase`): hero tự xuống cầu thang tới slime, có vòng, 2 quái chết, lên cấp, không có exception (`chase-online-frames.jpg`).
- Build Linux server: không được, máy thiếu module Unity Linux Dedicated Server.

## Câu hỏi
1. (Đã trả lời lượt 2: có tự đi, có vòng khoá, giữ 400 ms / x1,75.)
2. Tự đi có nên đuổi cả boss / Wanted boss không? Hiện đuổi mọi quái gần nhất trong 8 ô.
3. Tầm chém 34 + 8 đv nhìn khá xa (vệt bay tới quái, kiếm không chạm): có muốn hero đứng sát hơn khi đi tới không?
