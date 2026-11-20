# Vòng khoá kiểu RO3 + va chạm mềm (20/11/2026)

Yêu cầu: "hỏi ChatGPT Ragnarok 3 làm thế nào rồi làm vậy". ChatGPT (Tay, https://chatgpt.com/c/6afffe38-03f4-83ec-a5a6-653818256e43) đưa khuyến nghị và nói chưa xác minh hết nội bộ RO3. Nhánh `feat/target-ring-soft-collision` (worktree agent, chờ coordinator merge vào `ro-fantasy-heroes`).

## Đã làm
1. **Vòng khoá** (`Net/Client/TargetMarker.cs`, `TargetRingMesh.cs`): elip mesh thủ tục (không sprite, không texture; theo quyết định mới "hiệu ứng = particle / mesh / shader"), lõi vàng #E7C66B (Wanted #F3D17A), nét 2,5 px màn hình ở 1280x860 (Wanted 3 px), viền nâu mềm 1 px + dải mờ 1 px khử răng cưa, alpha 75-100 % chu kỳ 1,5 s, không xoay. Cỡ = lõi chân `Monster.FootRadius` x 1,15 + 2 (tối thiểu 8 đv), tỉ lệ 1 : 0,5. Chỉ vẽ mục tiêu của mình (như cũ), cả offline. Wanted boss giờ có vòng.
2. **Mũi tên Wanted** (`TargetArrowMesh.cs`): tam giác vàng thủ tục 22 px, viền nâu, trên đầu boss (0,9 x chiều cao vẽ), không lên quá 10 % trên cùng màn hình (thanh máu boss), nhún 2 px theo nhịp vòng, vẽ trên lớp FX. Lý do: chụp đám đông 40 người, vòng dưới chân Queen bị VFX che hết.
3. **Va chạm mềm** (dùng chung offline / zone server / client dự đoán):
   - `Rpg/BodyCollisionRules.cs` (thuần, test `BodyCollisionRulesTests`): 1 m = 20 đv; hero r 4,4; đo trên mặt đất (y x 2, như elip 1 : 0,5). Lõi chân: Wanted 0,5 x nửa thân (15-20 đv, Queen 17,9), boss map / quái 0,8 x nửa thân (boss 8-15, King 9,8; quái 4-14). Boss cứng, quái thường mềm (x 0,85). `ClipMove` bỏ phần đi vào lõi (trượt quanh), `Separate` tách chồng trong ~0,2 s (tối đa 50 đv/s, không dịch chuyển tức thời, tường chặn thì để chồng).
   - Hero (`HeroController.BodyCollision.cs`, gọi trong `Simulate`): người chơi đi xuyên nhau; không vào lõi quái; tự trôi ra khi đang chồng.
   - Quái (`Monster.BodyCollision.cs`, gọi trong `Monster.Step`): tự đi thì dừng ở chân hero, không xô ai (boss đứng yên giữa đám đông). Quái bị chặn vẫn đánh được hero đang chạm (`TouchStrikeSlack`); boss đánh được hero có ngực cách mép thân <= 16 đv (`BossContactReach`, đúng tầm kiếm Warrior) nên không bước tiếp vào hero.
   - Nhát khoá Warrior không lướt `StepPx` khi quái đã trong tầm (`LockedInReach`): trước đó mỗi nhát lướt 4 đv, hero chui dần ra sau thân King.
   - Knockback: không đổi (chỉ skill có knockback mới đẩy).
   - Hysteresis đuổi 0,12 m = 2,4 đv: `AttackChase` sẵn có biên 3 đv (dừng trong tầm - 3, đuổi lại khi ra ngoài tầm) nên không thêm hằng mới.
4. **Đám đông dễ nhìn** (`HeroController.CrowdFade.cs`): người chơi khác đứng trước và che hero mình mờ còn 50 % (chuyển 6 alpha/s); hero mình không mờ. Một phép so hình chữ nhật mỗi hero mỗi frame.
5. Công cụ: `--dummy-once` (một bộ quái, log khoảng cách mặt đất / tiếp xúc mỗi 15 frame) cho `--record-combo`; vai `--persist-role lock-ring` (giữ phím đánh, chụp 0,5 s x 80, log vị trí boss + khoảng cách) cho `Tools/wanted-boss-crowd-playtest.ps1 -Role lock-ring`.

## Đã xem
- Offline Oakhaven `--record-combo warrior --face right --tp 13,11 --dummies 1 --dummy-once --dummy-types X --dummy-dist 100`:
  - Frostfang King: hero tự đi tới, đứng cố định 27,5 đv mặt đất suốt 7 s, King đứng yên (trước khi sửa: King bước vào, hero lướt dần tới 14,2 đv, bị King che hết). Vòng vàng mượt quanh chân King (`docs/reference/target-ring/ring-closeup-king-3x.png`).
  - Frost Slime: 12,6 đv, chồng nhẹ (đúng "mềm"), vòng nhỏ quanh chân.
  - Mire Hydra Queen: 50 đv (đánh ở mép thân), vòng quanh gốc đuôi, không phình theo cổ (`offline-queen-ring.jpg`). Ảnh tổng `offline-king-slime-queen-contact.jpg`.
- Đám đông online (zone server = exe này, 39 bot + cửa sổ `-GodHero -Role lock-ring`, Mire Hydra Queen): boss đứng yên ở (728, 176) suốt 40 s, hero đứng 37,3 đv (lõi 22,3), `TargetMarker.Target()` = boss, 0 exception cả client lẫn zone. Mũi tên vàng thấy trên vương miện Queen (nhỏ, cùng tông vàng với vương miện; số sát thương lớn đôi khi đè lên nó). Vòng dưới chân bị VFX che (vì vậy có mũi tên). Ảnh `docs/reference/target-ring/crowd-*.png`.
- Test: `ZenoniaSkills.Tests` 474 pass, `ZenoniaNet.Tests` 455 pass.

## Chưa xem / còn lại
- PHẢI build lại zone server Linux (va chạm + tầm đánh boss đổi): máy thiếu module Unity Linux Dedicated Server. Server Windows local đã chạy thử.
- Hero mình trong đám đông vẫn bị VFX của người khác che (không thuộc phần mờ người chơi); xét làm mờ FX của người khác quanh hero mình.
- Mờ người che: thấy 1 Cleric mờ trong ảnh đám đông nhưng chưa đo kỹ (hero mình ở giữa VFX).
- Client dự đoán va chạm theo vị trí quái proxy (trễ nội suy) còn server theo vị trí thật: sát boss có thể giật sửa vị trí nhỏ; chưa thử ping cao.
- Wanted boss: tầm đánh thực tế tăng (mép thân 36 + 16 so với 37 chân-chân), chỉ đánh mục tiêu của nó.
- Điện thoại, WebGL, 2.5D (`--view25d`) chưa xem. Frame đánh chéo của art Warrior (kiếm chĩa sai) vẫn thấy trong ảnh, agent khác đang vẽ lại.
- Không merge được vào `ro-fantasy-heroes` từ worktree (harness chặn git ngoài worktree): coordinator merge.

## Câu hỏi
1. Hero mình bị VFX người khác che giữa 40 người: có làm mờ FX của người khác quanh hero mình không?
2. Wanted boss đánh hero đang chạm thân (tầm thực tế xa hơn trước): giữ hay chỉnh?
