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

## Code đã có (đã commit, biên dịch 0 lỗi, dotnet 277 + 334, vitest 126)

- Luật dùng chung `Assets/Scripts/Rpg/WantedBoss.cs`: đổi boss 05:00 giờ VN, xoay `rimeclawTyrant` (Lv 10, `frostHollow`) / `glacialColossus` (Lv 25, `frostfangDen`) / `mireHydraQueen` (Lv 40, `sunkenReliquary`), bảng thưởng (150 G + 3 đá / 600 + 5 / 1000 + 8, kèm 3 HP + 3 MP), ô xuất hiện cố định từng boss.
- `server-api`: lưu `wantedDay` trong state nhân vật, không cho giảm (`rpg/wanted-day.js`, `item-validation.js`, `internal-routes.js`).
- Zone: `World/WantedBossCredit.cs` (ghi công + phát thưởng), `World/WantedBossSpawner.cs` (boss của ngày có mặt cả ngày, chết thì ra lại sau 30 phút), `MonsterDef.Wanted(...)` (máu = 30 lần boss map cùng tầm, `WantedRenderScale` 4,5, ô sheet 384 / 512).
- Client: `WantedBossTracker.cs` (dòng chat), `WantedBossBoard.cs` (bảng tạm, mở khi nói chuyện với Guard Rook ở Oakhaven).
- Tham số chạy thử: `--wanted-boss TYPE`, `--wanted-respawn-sec S`, `--wanted-hp-scale F`.
- Chạy thử tự động: `Tools/wanted-boss-test.ps1`, bot `DebugCapture.NetWanted.cs`, `server-api/scripts/dev-boost-character.mjs`.
- **Skill + cảnh báo (12/11 đêm, mục 4 cũ, nay XONG phần code):**
  - Dữ liệu thuần C# (test dotnet, `Tests/dotnet/ZenoniaSkills.Tests/WantedSkillTests.cs`): `Assets/Scripts/Rpg/WantedSkills/WantedSkillDef.cs` (1 skill), `WantedSkillTable.cs` (4 skill x 3 boss, đúng số liệu `wanted-boss-skills-design.md`), `WantedSkillArea.cs` (hình học điểm-trong-vùng tròn/quạt/đường thẳng/tròn-trừ-hành-lang-an-toàn, nới 0,25 ô ở rìa, dùng lại `SkillGeometry` có sẵn), `WantedSkillClock.cs` (hồi chiêu + khoảng nghỉ 3 s giữa 2 skill + chọn skill ngẫu nhiên trong các skill đã hồi, cuồng nộ dưới 30 % máu).
  - Máy trạng thái authority: `Monster.cs` thêm `State.Telegraph`/`State.Execute` (nối thêm cuối enum, không phá dây mạng cũ), `Monster.WantedSkills.cs` (chọn skill, khoá vùng ở `Monster.Ai.cs`'s Chase, gây sát thương mọi người chơi trong vùng lúc Execute), `Monster.Ai.cs` gọi vào từ Chase.
  - Mạng: `NetEventType.WantedSkillTelegraph` (`NetEventTypes.cs`, `NetEventBus.cs`), client nhận ở `NetEventPlayer.cs`. Mốc đánh (`AtMs`) lấy từ `ZoneHost.ClockMs` (cùng đồng hồ `ServerTimeMs`), client so với `NetClient.ServerNowMs` (không đếm cục bộ).
  - Trạng thái người chơi: dùng lại `MonsterStatus` (vốn của quái) cho hero (`HeroController.WantedStatus.cs`): chậm + độc đúng luật cũ, đẩy lùi là một cú đẩy tức thời theo `FrostHollowMap.Move` (không timer). Dash/bất tử né được hoàn toàn (giống đòn thường).
  - Client vẽ: `WantedSkillTelegraphFx.cs` + `WantedWarningSprites.cs` (vùng cảnh báo mặt đất dùng `warning-{circle,cone,line,safe}.png`, lấp dần theo `AtMs - ServerNowMs`), `WantedSkillVfx.cs` (hiệu ứng va chạm: bảng khai 1 chỗ `rimeclawTyrant-skill{1..4}.png`, rơi về `rimeclawTyrant-skills.png` cũ, rồi về không có gì), `Monster.WantedTelegraphVisuals.cs` (chọn khung `-cast.png`, thanh niệm chú, icon cuồng nộ), `HeroController.WantedOverlay.cs` (chấm than + icon chậm/độc trên đầu, CHỈ nhân vật của mình). `ProxySpawner` dọn hết khi đổi map.
  - Glacial Colossus / Mire Hydra Queen: đủ số liệu 4 skill, chưa có `-cast.png` hay `-skillN.png` riêng nên dùng cảnh báo mặt đất chung + anim attack thường (đúng yêu cầu "chưa có hình thì dùng cảnh báo chung").
  - **Đã chạy e2e thật và XEM ảnh (12/11 đêm, lần 2):** `Tools/wanted-boss-test.ps1 -HpScale 0.5` (tham số mới, mặc định 0,03): 45 PASS / 0 FAIL, boss chết sau 24-32 s. Bot chụp chuỗi 7 ảnh / lần ra chiêu, 3 lần đầu; log client ghi `[WantedTelegraph] spawn/impact` kèm `atMs` và `serverNow`: hiệu ứng va chạm bắn đúng mốc server (trễ 0-17 ms), cảnh báo hiện trên client khoảng 1,0-1,4 s. Ảnh: `docs/reference/wanted-boss-skill-telegraph-ingame.png` (Stomp: vòng đỏ lấp dần từ tâm, pha lê nổ dưới chân, boss + nhân vật vẫn thấy), `...-sweep-ingame.png`, `...-chain-slam-ingame.png`.
  - Sửa sau khi xem (lần 1 agent tưởng ảnh là "khung cast", thật ra là frame 1 hiệu ứng Stomp che kín boss): (a) hiệu ứng va chạm vẽ ở lớp mặt đất `UltimateFx.GroundFxOrder + 2`, dưới mọi nhân vật và boss; (b) thêm mảng lấp màu đỏ bake bằng code (`WantedWarningSprites.Fill/FillLine`), lớn dần từ tâm theo thời gian còn lại, vì `warning-*.png` chỉ là viền mảnh; (c) `TargetMarker` không vẽ vòng chọn mục tiêu cho boss truy nã (vòng 128 px phóng theo boss 4,5 lần thành cung đỏ thô che cảnh báo). Ảnh chụp đầu tiên của chuỗi vẫn muộn vì cờ telegraph của quái đi theo snapshot (trễ nội suy), không phải lỗi game.
  - CHƯA xem: cuồng nộ Tyrant's Blizzard + hành lang an toàn (bot chưa đánh boss xuống dưới 30 % trong lúc chụp), chấm than + icon chậm/độc trên đầu nhân vật (có thể bị thân boss che), skill của Glacial Colossus / Mire Hydra Queen, cảm giác chơi thật bằng tay.
  - Ghi chú cân bằng: phạm vi skill theo bảng thiết kế (vd Chain Slam 2 ô, quạt 4 ô) trông NHỎ so với boss 4,5 lần (thân boss đã rộng gần 3 ô): vòng Chain Slam nhắm người đứng sát boss nằm gọn dưới chân boss. Cân nhắc nhân bán kính theo cỡ boss.

## CHƯA XONG / ĐANG LỖI

1. (Trước đây FAIL "boss died after 300 s") -- 12/11 đêm chạy 2 lần đều PASS (hp-scale 0,03: 2 s; 0,5: 24-32 s); để ý nếu tái diễn.
2. Chưa xem cuồng nộ (Blizzard + hành lang an toàn), icon chậm/độc/chấm than trên đầu nhân vật, và chưa chơi thử bằng tay. Phạm vi skill có thể cần to theo cỡ boss (xem ghi chú cân bằng ở trên).
3. Glacial Colossus và Mire Hydra Queen còn thiếu sprite (walk/attack/cast) nên chưa đứng được trong game để thử số liệu skill của 2 con đó.
4. Bảng truy nã trong game là bản tạm: phải dựng lại theo `docs/reference/wanted-boss-board-mockup.png` (chân dung trong mockup là "Frostfang King": xin vẽ lại theo boss mới + tấm mảnh rời).
5. Client dự đoán cục bộ (online, nhân vật của mình) CHƯA biết mình đang bị CHẬM/ĐỘC từ boss skill cho tới khi server đồng bộ lại vị trí (không có trường riêng báo trạng thái này trong gói tự dự đoán) -- có thể giật hình nhẹ lúc bị chậm, không ảnh hưởng người chơi khác xem. Đóng băng (mục dưới) đã có đường riêng, không còn lỗi này.

## Đóng băng Frozen Ground Stomp (12/11/2026 đêm, XONG phần code + test)

- Đổi "chậm 40 % trong 2 s" -> "đóng băng 3 s" (1,5 s lúc đầu, người dùng chê hết nhanh; giữ nguyên bán kính/cảnh báo/sát thương/hồi chiêu). Chi tiết luật: `docs/wanted-boss-skills-design.md` mục "Luật đóng băng".
- File: `WantedSkillTable.cs` (StatusKind.Freeze), `HeroController.cs` (field `frozenMs`, khựng Simulate), `HeroController.WantedStatus.cs` (`TakeWantedSkillHit` nhánh Freeze + không đẩy lùi, `PredictFreeze` tự đoán phía client), `HeroController.WantedFreezeFx.cs` (hình cục băng + state machine 3 pha), `HeroRenderer.WantedFreeze.cs` (tint xanh thân hero), `HeroController.Net.cs` (`HeroFlags.Frozen`), `Net/Protocol/NetOwnerSim.cs` (`FreezeLeft`), `HeroController.SimState.cs` (capture/restore/Discrete bit 11), `WantedSkillTelegraphFx.cs` (gọi `PredictFreeze` đúng lúc hiệu ứng va chạm bắn ra).
- Test dotnet: `WantedSkillTests.cs` (StatusKind.Freeze, StatusMs 1500), `SnapshotTests.cs` (FreezeLeft round-trip). `dotnet test` 277 + 334 PASS.
- **Đã xem bằng log thật** (`Tools/wanted-boss-test.ps1 -HpScale 0.5`, 45 PASS / 0 FAIL, 1 lần chạy lại ra 43/2 nhưng 2 FAIL đó là bảng truy nã (Guard Rook) không liên quan, chạy lại liền sau ra 45/0): log client có `[WantedTelegraph] impact skill 2` ngay sau đó là tự đóng băng phía client (có lần đối chiếu từ server tới TRƯỚC cả lúc đồng hồ cục bộ chạm 0, chứng tỏ cả hai đường (tự đoán + OwnerSim.FreezeLeld) đều hoạt động), và log zone có `authority froze ... for 1500 ms` đúng lúc. Ảnh chụp chuỗi (`wanted-boss-telegraph-*`) có lần thấy rõ nhân vật đứng giữa hiệu ứng va chạm băng, nhiều lần khác thân boss 4,5 lần che kín nhân vật (lỗi camera/cỡ boss đã ghi ở trên, không phải lỗi mới) nên KHÔNG tự tin đã nhìn rõ 100% cục băng + tint trên nhân vật trong ảnh tĩnh -- mới xác nhận chắc bằng log, CHƯA chơi thử bằng tay/mắt thường.
- Hình cục băng (ChatGPT, chat boss): nguồn `ArtLibrary/custom/wanted-boss/skills/frozen-block-chatgpt-1.png` -> `Sprites/fx/wanted/frozen-block.png` (`build-wanted-boss-sheets.py fx`). Vẽ ở `orderBase + 7`, TRÊN mọi lớp của hero (agent đặt nhầm sau thân, đã sửa). ĐÃ XEM trong game: `docs/reference/wanted-boss-frozen-hero-ingame.png`, chuỗi `...-frozen-stomp-series-ingame.png` (hero trong khối băng, đứng im khoảng 1,4 s, băng vỡ rồi đánh tiếp; kiếm thò ra ngoài khối).
- CHƯA xem: nhiều người chơi cùng lúc bị đóng băng (online thật, không phải bot đơn), cảm giác tay cầm lúc bị đóng băng rồi tan băng.

## Hình (ChatGPT) đã có

- Concept 3 boss: `docs/reference/wanted-raid-bosses-concept.png`.
- Rimeclaw Tyrant, đủ bộ sprite: nguồn `Assets/ArtLibrary/monsters/wanted/rimeclaw-tyrant-*.png` (walk 4 hướng, attack 4 hướng, cast chain-slam + stomp, cast blizzard + defeat). Cắt bằng `py Tools/build-wanted-boss-sheets.py write` -> `Sprites/monsters/rimeclawTyrant{,-attack,-cast}.png`.
- Hình cảnh báo dùng chung: `Sprites/fx/wanted/warning-*.png` (vòng tròn, quạt 90°, đường thẳng, vùng an toàn, chấm than, cuồng nộ, chậm, độc, thanh niệm chú).
- Bảng chiêu tổng: `docs/reference/wanted-boss-rimeclaw-tyrant-skill-board.png`.
- Bảng từng chiêu theo kiểu Blade Cyclone (cách người dùng muốn): đủ 4 / 4 (12/11 tối), đã xem: `docs/reference/wanted-boss-rimeclaw-skill-{1-frostclaw-sweep,2-broken-chain-slam,3-frozen-ground-stomp,4-tyrants-blizzard}-board.png` (bảng 3: mũi tên ô "How to dodge" chỉ vào trong, không ảnh hưởng game).
- Tấm hiệu ứng lớn 4 chiêu (12/11 tối, chat boss ở trên): nguồn `ArtLibrary/custom/wanted-boss/skills/rimeclaw-tyrant-skill{1..4}-fx-chatgpt-1.png`, cắt bằng `py Tools/build-wanted-boss-sheets.py fx` -> `Sprites/fx/wanted/rimeclawTyrant-skill{1..4}.png` (2x2 frame, ô 768x512, không căn giữa lại). Ảnh xem: `docs/reference/wanted-boss-rimeclaw-skill-fx-sheets.png`. CHƯA xem trong game. Pivot (px trong ô): skill1 đỉnh quạt (384, 35), bán kính ~445, mở xuống; skill2 tâm (384, 284) rx 248 ry 197; skill3 tâm (380, 255) rx 325 ry 240; skill4 tâm (380, 237) rx 275 ry 215, nêm an toàn 90° hướng xuống, đỉnh nêm (384, ~300). Vùng đất hơi dẹt (1,25-1,3:1): scale X, Y riêng để khớp vòng cảnh báo tròn.
- `Sprites/fx/wanted/rimeclawTyrant-skills.png`: tấm cũ nhỏ, thưa; chỉ còn để dự phòng.

## Glacial Colossus (12/11/2026 đêm, XONG hình + nối code, đã xem bằng bot)

- Người dùng: "làm tiếp mấy con boss kia đi". Cùng chat ChatGPT boss (Tay).
- Sprite ĐÃ CÓ và đã cắt vào game (`py Tools/build-wanted-boss-sheets.py write glacialColossus`): nguồn `ArtLibrary/monsters/wanted/glacial-colossus-{walk,attack}-{down-up,left-right}-chatgpt-1.png`, `...-cast-slam-crystal-fall-...` (hàng 0: đập 2 tay, dùng cho Crystal Fissure + Rune Impact; hàng 1: Crystal Fall), `...-cast-overload-defeat-...` (hàng 2 cuồng nộ, hàng 3 gục). Khói ở frame cuồng nộ cuối hơi ánh tím (lẫn nền magenta).
- Bảng chiêu 4/4: `docs/reference/wanted-boss-glacial-skill-{1-crystal-fissure,2-rune-impact,3-crystal-fall,4-core-overload}-board.png`.
- Đổi thiết kế: Crystal Fall "chậm 30 % 2 s" -> ĐÓNG BĂNG 2 s (người dùng chê bộ skill chưa có gì khó). `CastRow` 0/0/1/2 trong `WantedSkillTable.cs`.
- `WantedSkillVfx.Play` nhận cả `WantedSkillDef`, thêm kiểu hình `Line` (neo ở đầu đường, RefPxX = nửa bề rộng px, RefPxY = chiều dài px) cho Crystal Fissure.
- `Tools/wanted-boss-playtest.ps1 -Boss glacialColossus|mireHydraQueen` có vị trí xuất phát.
- Hiệu ứng 4/4: `Sprites/fx/wanted/glacialColossus-skill{1..4}.png` (`build-wanted-boss-sheets.py fx glacialColossus`), pivot khai báo trong `WantedSkillVfx.Pieces`. Crystal Fall lần đầu lại vẽ đất dẹt, đã xin vẽ lại kèm tấm Chain Slam làm mẫu. Khi tải ảnh: một lần lấy nhầm ảnh cũ nạp lại trong DOM -> luôn mở ảnh ra kiểm trước khi chép.
- Chỗ đứng boss: ô (28,3) cũ nằm trong ngách đá sát mép bắc, đầu boss bị thanh HUD che, người chơi bị kẹt sau lưng boss -> dời ra giữa đấu trường (27,12). Mire Hydra Queen cũng dời (33,10) -> (30,14), CHƯA xem trong game. `wanted-boss-playtest.ps1` truyền `--wanted-boss` cho cả client (dòng chat đúng tên boss) và có `-Bot` (bot tự đánh + chụp chuỗi ảnh).
- ĐÃ XEM trong game (bot, `-HpScale 0.4`): boss đứng đủ hình giữa hang, Crystal Fissure (dải đỏ lấp dần rồi pha lê mọc dọc đường), Rune Impact, Crystal Fall: `docs/reference/wanted-boss-glacial-crystal-fissure-ingame.png`, `...-glacial-fight-ingame.png`. Bot Warrior Lv60 vũ khí khởi đầu chết 1 lần (boss Lv25 dành cho cả nhóm) nên bài bot báo FAIL; không phải lỗi code. CHƯA xem: Core Overload (cuồng nộ), đóng băng của Crystal Fall rõ từng frame, chơi tay.

## Mire Hydra Queen (12/11/2026 đêm, XONG hình + nối code, đã xem bằng bot)

- Sprite đã cắt (`build-wanted-boss-sheets.py write mireHydraQueen`): nguồn `ArtLibrary/monsters/wanted/mire-hydra-queen-*.png`; cast hàng 0 phun độc (Venom Breath + Poison Bloom), hàng 1 quật đuôi, hàng 2 cuồng nộ, hàng 3 gục. Tool báo "no empty gap" ở tấm đi ngang (frame vẽ sát nhau) nhưng ảnh cắt vẫn nguyên.
- Bảng chiêu 4/4: `docs/reference/wanted-boss-mire-skill-{1-triple-venom-breath,2-poison-bloom,3-serpent-tail-crush,4-toxic-reign}-board.png`.
- Hiệu ứng: `mireHydraQueen-skill{1,2,3}.png`; skill4 (Toxic Reign) dùng chung tấm skill2 (`FX_SHARED` trong tool). Pivot trong `WantedSkillVfx.Pieces`. Prompt ghi "không hồng / tím" để không lẫn nền key magenta.
- ĐÃ XEM (bot, `-HpScale 0.4`): boss ở giữa sân đền, Venom Breath + Poison Bloom: `docs/reference/wanted-boss-mire-fight-ingame.png`. Boss mờ đi khi hero đứng sau thân (cơ chế làm mờ có sẵn). Bot Warrior Lv60 vũ khí khởi đầu chết khi đánh solo nên bài bot FAIL (không phải lỗi code). CHƯA xem: Tail Crush, Toxic Reign (cuồng nộ), chơi tay. Vũng độc ở lại trên đất vẫn chưa làm (chỉ độc lúc trúng).

## Hình còn phải xin ChatGPT

1. (XONG 12/11 đêm) Glacial Colossus và Mire Hydra Queen: mỗi con lặp lại đúng trình tự của Rimeclaw (4 tấm walk / attack, bảng chiêu, 2 dải cast, 4 bảng từng chiêu, 4 tấm hiệu ứng 2x2 như Rimeclaw: prompt ghi vùng tròn gần tròn, tâm giữa ô); thêm stem vào `BOSSES` / `CASTS` trong `Tools/build-wanted-boss-sheets.py`.
2. Chân dung 3 boss + mảnh rời cho bảng truy nã.

## Triển khai

Khi lên server thật: `server-api` trước, rồi zone, rồi client (xem plan). Thay đổi "5 nhân vật, trùng class" của hôm nay cũng chưa deploy.
