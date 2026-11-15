# Đánh thường: chỉ nhát 4 đẩy lùi (15/11/2026)

## Cơ chế cũ
- `ComboConfigs.cs`: cả 4 nhát mọi class đều có `Knockback` (Warrior 40/50/55/110, Fighter 20/25/35/95, Assassin 25/30/35/70, Wizard 30/35/40/90, Mechanic 25/30/35/100, Cleric 30/35/40/80). Chí mạng nhân đôi (`HeroController.Combat.MeleeStrike`, `ComboProjectile.Strike`).
- `Monster.TakeHit`: knockVel khác 0 (trừ boss) thì trượt 170 ms (x1,35, giảm dần). Trong lúc trượt `Monster.Step` bỏ qua `Think` nên AI đứng im, đồng hồ windup bị dừng. Khi windup hết, đòn chỉ trúng nếu hero còn trong `AttackRange + 10` (quái thường 12 + 10). Warrior đẩy 9-12 đơn vị mỗi nhát, mỗi nhát 250 ms thì 170 ms quái bị khoá: windup 350 ms kéo dài cả giây, và quái bị đẩy ra ngoài tầm nên đòn trượt. Không có hitstun riêng, không siêu giáp, hit-stop là dừng toàn cục (không thiên vị).
- Skill: knockback riêng (`SkillDamage`, `StatusKind.Knockback`), cùng đi qua `TakeHit`.
- Server zone là bản build Unity, chạy cùng `Monster.cs` / `ComboConfigs.cs` (authority), client proxy không tự áp.

## ChatGPT chốt (`docs/reference/combo-knockback-chatgpt.md`)
- Nhát 1-3: knockback 0, không khoá AI, không ngắt hay làm chậm windup (hit reaction 60 ms chỉ hình ảnh).
- Nhát 4: đẩy Warrior 24, Fighter 22, Assassin 14, Wizard 18, Mechanic 20, Cleric 16 đơn vị (trượt 170 ms); ngắt windup, quái hồi 300 ms sau khi trượt.
- Chí mạng bỏ x2 knockback. Skill giữ knockback riêng, skill có knockback được ngắt windup. Boss không bị đẩy, không bị ngắt.

## Đã sửa
- `Assets/Scripts/ComboConfigs.cs`: nhát 1-3 knockback 0 mọi class; nhát 4 = Warrior 110, Fighter 95 (giữ), Assassin 61, Wizard 78, Mechanic 87, Cleric 70.
- `Assets/Scripts/HeroController.Combat.cs`, `Assets/Scripts/ComboProjectile.cs`: bỏ x2 knockback khi chí mạng (chỉ đánh thường; skill không đổi).
- `Assets/Scripts/Monster.cs` (`TakeHit`): bị đẩy lúc đang Windup thì huỷ đòn, `Cooldown` 300 ms (đếm sau khi trượt xong). Áp cho mọi knockback (nhát 4, skill); boss không bị đẩy nên không bị ngắt.
- `Assets/Scripts/Rpg/Skills/StatusRules.cs`: `KnockInterruptRecoverMs = 300`.
- Test mới `Tests/dotnet/ZenoniaSkills.Tests/ComboKnockbackTests.cs`.
- Không thêm hit reaction 60 ms: chớp trắng 100 ms có sẵn, không khoá AI.

## Test
- ZenoniaSkills.Tests: 320/320 pass. ZenoniaNet.Tests: 404/404 pass.

## Exe + ảnh đã xem
- Exe: `C:/temp/player-combo-kb/Zenonia.exe` (build từ `C:/temp/zu-copy`, Succeeded 0 lỗi). KHÔNG ghi vào `C:/temp/player-wizard-d/` vì exe đó đang chạy lúc build (có thể người dùng đang test bộ Wizard D).
- Chụp Warrior `--map frost-hollow --bot attack --level 2 --dummies 1`, mỗi 0,1 s; đã tự xem:
  - `plans/reports/combo-knockback-261115-ingame-zoom.png`: t=2,5-2,6 slime bị nhát cuối (vòng đỏ) đẩy văng xuống; các slime bên trái vẫn dính sát hero suốt combo và hero ăn đòn (số đỏ 45 trên đầu hero t=2,7-2,9).
  - `...-ingame-sheet.png`, `...-ingame-sheet-tree-spot.png` (tp 12,30): slime đứng sát hero suốt combo, hero ăn đòn 44 giữa combo.
- Chưa xem: ranged class (Wizard/Mechanic/Cleric) trong game, online. Bot attack tự xoay hướng theo quái nên khó tách riêng từng nhát trong ảnh.

## Build lại server
CÓ: `ComboConfigs.cs`, `Monster.cs`, `StatusRules.cs` là luật dùng chung, phải build lại zone server trước khi test online.

## Câu hỏi còn mở
- Assassin nhát 4 có 2 nhát đâm, mỗi nhát đều đẩy 61, nên tổng quãng đẩy hơn 14 một chút (nhát 2 cách 90 ms, đặt lại trượt). Để nguyên hay chỉ cho nhát đâm cuối đẩy?
- Có muốn thêm hit reaction 60 ms (giật hình) riêng không?

## Vòng 2 (người dùng: "hỏi ChatGPT đi rồi làm" cho 2 câu mở)
ChatGPT chốt (nối vào `docs/reference/combo-knockback-chatgpt.md`):
- Assassin nhát 4: chỉ cú đâm 2 đẩy, tổng 14 đơn vị, ngắt windup. Cú 1 có sát thương nhưng không đẩy, không ngắt.
- KHÔNG thêm giật hình 60 ms (trùng jitter có sẵn).
- Thêm: quái đang windup / tung đòn / follow-through khi trúng đòn phải GIỮ pose báo đòn (trước đây chớp trắng 110 ms đổi sang khung đứng, che mất cảnh báo windup 350 ms). Quái đứng / đi thì vẫn đổi khung như cũ.

Đã làm:
- `Assets/Scripts/ComboSystem.cs`: `ComboHit.KnockbackFor(laterStab)`, cú đâm đầu của bước nhiều cú = 0. `HeroController.Combat.MeleeStrike` dùng nó (Assassin 61 ở cú 2 = khoảng 14 đơn vị).
- `Assets/Scripts/Monster.Visuals.cs` (`Render`): bỏ điều kiện `flash <= 0`, pose windup / đòn / follow-through không còn bị khung đứng che. Chỉ hình ảnh, không đụng luật server.
- Test `ComboKnockbackTests.AssassinFinisherPushesOnlyWithItsLastStab`. ZenoniaSkills 321/321, ZenoniaNet 404/404.
- Exe build lại: `C:/temp/player-combo-kb/Zenonia.exe` (Succeeded, 0 lỗi; exe đang mở là `player-wizard-d`, không đụng).

Kiểm:
- Ảnh trong game Assassin quá rối để thấy quãng đẩy (quái đi lại, camera theo hero). Đã build một bản chẩn đoán tạm có log (log đã gỡ khỏi code ngay sau build, bản chẩn đoán đã xoá), log `plans/reports/combo-knockback-261115-assassin-diag-log.txt`: nhát 1-3 và cú đâm 1 knock=0, cú đâm 2 knock=61, quái nhận v=(0, -82,35) = 61 x 1,35 (trượt 170 ms khoảng 14 đơn vị). Ảnh `plans/reports/combo-knockback-261115-assassin-finisher-ingame.png`: dấu X của nhát 4, hero ăn đòn 40 ngay sau combo.
- Chưa tự xem được pose windup không bị che (khó bắt khung).

Lưu ý: hero trúng đòn thì huỷ cú đâm 2 đang chờ (`TakeDamage`: `stabLeft = 0`, code có sẵn). Giờ quái đánh lại được giữa combo nên đôi khi nhát 4 Assassin sẽ không đẩy. Chưa đổi, cần người dùng quyết.

## Vòng 3: cú đâm 2 Assassin khi hero trúng đòn
ChatGPT chốt: cú 2 luôn ra (đòn cam kết), chỉ huỷ khi chết hoặc bị khống chế thật. Sửa `HeroController.Combat.cs` (TakeDamage không huỷ cú 2, trừ khi chết), `HeroController.WantedStatus.cs` + `StatusRules.CancelsPendingStab` (đóng băng / choáng / bị đẩy lùi thì huỷ). Test 322/322 + 404/404. Exe build lại `C:/temp/player-combo-kb/Zenonia.exe`. Chưa chụp trong game riêng vòng này (khó bắt đúng lúc quái đánh trúng giữa 90 ms). Build lại server: có.
