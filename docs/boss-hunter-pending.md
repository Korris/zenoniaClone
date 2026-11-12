# Boss Truy Nã (Boss Hunter): việc đang dở

Ghi lúc 12/11/2026 17:40, khi người dùng bảo dừng code, commit, push để làm tiếp sau. Đọc file này trước khi làm tiếp Boss Truy Nã.

## Người dùng đã chốt

- Sự kiện ngày "Boss Truy Nã" (chọn từ `docs/online-events-ideas.md`). Mỗi nhân vật nhận thưởng 1 lần / ngày, chỉ online.
- Boss truy nã là boss RIÊNG của sự kiện, khác boss map. Vẽ TO, kiểu cả đám người chơi xúm vào đánh. Máu CỐ ĐỊNH, không tăng theo số người.
- Boss có dùng skill và có cảnh báo. Làm HÌNH hoạt ảnh ra chiêu trước (bảng chiêu -> dải hoạt ảnh -> hiệu ứng), rồi mới code skill.
- Hiệu ứng skill của boss vẽ theo kiểu bảng `docs/reference/warrior-blade-cyclone-board.png` (4 giai đoạn trong cảnh game, sơ đồ phạm vi).
- Mọi thứ thuộc thiết kế / hình phải nhờ ChatGPT vẽ, không tự vẽ. "Bạn với ChatGPT cứ làm, không cần tui xác nhận."
- 17:17: boss 3 lần chiều cao nhân vật "còn bé quá" -> đã tăng lên 4,5 lần (chưa xem trong game).

## Tài liệu

- Kế hoạch: `plans/261112-1619-daily-wanted-boss/` (viết TRƯỚC khi đổi sang boss riêng + skill: phase 01-03 còn đúng phần thưởng / lưu ngày nhận; phần boss map đã lỗi thời).
- Thiết kế skill + cảnh báo (ChatGPT): `docs/wanted-boss-skills-design.md`.
- Chat ChatGPT (tài khoản Tay): boss + skill https://chatgpt.com/c/6af58a18-74d0-83ec-9440-0f2715067fc1 ; mockup bảng truy nã trong chat https://chatgpt.com/c/6af56a88-ea98-83ec-afeb-67dc9ce5613e ; ý tưởng sự kiện https://chatgpt.com/c/6af57fff-c4f8-83ec-8f05-0c4eb76c4cde

## Code đã có (đã commit, biên dịch 0 lỗi, dotnet 267 + 334, vitest 126)

- Luật dùng chung `Assets/Scripts/Rpg/WantedBoss.cs`: đổi boss 05:00 giờ VN, xoay `rimeclawTyrant` (Lv 10, `frostHollow`) / `glacialColossus` (Lv 25, `frostfangDen`) / `mireHydraQueen` (Lv 40, `sunkenReliquary`), bảng thưởng (150 G + 3 đá / 600 + 5 / 1000 + 8, kèm 3 HP + 3 MP), ô xuất hiện cố định từng boss.
- `server-api`: lưu `wantedDay` trong state nhân vật, không cho giảm (`rpg/wanted-day.js`, `item-validation.js`, `internal-routes.js`).
- Zone: `World/WantedBossCredit.cs` (ghi công + phát thưởng), `World/WantedBossSpawner.cs` (boss của ngày có mặt cả ngày, chết thì ra lại sau 30 phút), `MonsterDef.Wanted(...)` (máu = 30 lần boss map cùng tầm, `WantedRenderScale` 4,5, ô sheet 384 / 512).
- Client: `WantedBossTracker.cs` (dòng chat), `WantedBossBoard.cs` (bảng tạm, mở khi nói chuyện với Guard Rook ở Oakhaven).
- Tham số chạy thử: `--wanted-boss TYPE`, `--wanted-respawn-sec S`, `--wanted-hp-scale F`.
- Chạy thử tự động: `Tools/wanted-boss-test.ps1`, bot `DebugCapture.NetWanted.cs`, `server-api/scripts/dev-boost-character.mjs`.

## CHƯA XONG / ĐANG LỖI

1. Chạy thử e2e CHƯA QUA. Lần chạy cuối (log ở `C:\temp\wanted\`): PASS = đăng nhập, Guard Rook, bảng mở, dòng chat lúc vào, boss map chết không trả thưởng truy nã; FAIL = "the wanted boss died after 300 s" (bot không hạ được boss sự kiện trong 300 giây), kéo theo "WantedDay rise 0 -> 0" và "the claim chat line". Chưa biết do bot không tới / không đánh trúng boss to, hay do máu. Phần agent đang sửa script lúc bị dừng: xem lại `Tools/wanted-boss-test.ps1` (có thể đang dở).
2. Còn tiến trình thử chưa tắt được lúc dừng (lệnh tắt bị chặn): `Zenonia.exe` trong `C:\temp\player-wanted\` (zone cổng 7797 + 1 client), `mongod` cổng 27197, `node src/server.js` cổng 3197. Tắt tay trước khi chạy thử lại.
3. CHƯA xem boss trong game lần nào (cỡ 4,5 lần, hình thật `rimeclawTyrant`, thanh máu, chỗ đứng).
4. CHƯA code boss dùng skill: máy trạng thái NORMAL -> TELEGRAPH -> EXECUTE, 3 hình vùng (tròn, quạt, đường thẳng), cảnh báo đồng bộ theo giờ server, làm chậm / đẩy lùi / độc, cuồng nộ dưới 30 % máu. Theo `docs/wanted-boss-skills-design.md`.
5. Bảng truy nã trong game là bản tạm: phải dựng lại theo `docs/reference/wanted-boss-board-mockup.png` (chân dung trong mockup là "Frostfang King": xin vẽ lại theo boss mới + tấm mảnh rời).

## Hình (ChatGPT) đã có

- Concept 3 boss: `docs/reference/wanted-raid-bosses-concept.png`.
- Rimeclaw Tyrant, đủ bộ sprite: nguồn `Assets/ArtLibrary/monsters/wanted/rimeclaw-tyrant-*.png` (walk 4 hướng, attack 4 hướng, cast chain-slam + stomp, cast blizzard + defeat). Cắt bằng `py Tools/build-wanted-boss-sheets.py write` -> `Sprites/monsters/rimeclawTyrant{,-attack,-cast}.png`.
- Hình cảnh báo dùng chung: `Sprites/fx/wanted/warning-*.png` (vòng tròn, quạt 90°, đường thẳng, vùng an toàn, chấm than, cuồng nộ, chậm, độc, thanh niệm chú).
- Bảng chiêu tổng: `docs/reference/wanted-boss-rimeclaw-tyrant-skill-board.png`.
- Bảng từng chiêu theo kiểu Blade Cyclone (cách người dùng muốn): đã có 2 / 4: `docs/reference/wanted-boss-rimeclaw-skill-1-frostclaw-sweep-board.png`, `...-skill-2-broken-chain-slam-board.png` (bảng 2 mới tải về, CHƯA mở xem).
- `Sprites/fx/wanted/rimeclawTyrant-skills.png`: tấm hiệu ứng vẽ TRƯỚC khi có bảng kiểu mới, nhỏ và thưa -> sẽ thay.

## Hình còn phải xin ChatGPT

1. Bảng chiêu 3 (Frozen Ground Stomp) và 4 (Tyrant's Blizzard) của Rimeclaw theo cùng mẫu.
2. Tấm hiệu ứng lớn cho 4 chiêu, vẽ theo 4 bảng đó (nền `#FF00FF`, hình đặc, phủ kín vùng).
3. Glacial Colossus và Mire Hydra Queen: mỗi con lặp lại đúng trình tự của Rimeclaw (4 tấm walk / attack, bảng chiêu, 2 dải cast, 4 bảng từng chiêu, hiệu ứng); thêm stem vào `BOSSES` / `CASTS` trong `Tools/build-wanted-boss-sheets.py`.
4. Chân dung 3 boss + mảnh rời cho bảng truy nã.

## Triển khai

Khi lên server thật: `server-api` trước, rồi zone, rồi client (xem plan). Thay đổi "5 nhân vật, trùng class" của hôm nay cũng chưa deploy.
