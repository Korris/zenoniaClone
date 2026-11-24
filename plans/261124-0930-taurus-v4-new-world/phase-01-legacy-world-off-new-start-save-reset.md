# Phase 01 (P0a): Tắt thế giới cũ, điểm xuất phát mới, reset save

**Trạng thái: XONG code 24/11 (chưa commit, chưa xem trong game).**

## Context Links
- BIBLE V4 part1 mục II (cấu trúc), IX (quyết định khoá); `docs/pending-tasks.md` 24/11 09:20.
- Người dùng 24/11: không tái dùng map / quái / tên / art cũ; save cũ bỏ hết, không migrate.
- Code: `Assets/Scripts/Maps/MapRegistry.cs`, `World/ZoneHost.cs:32-44`, `RpgSession.cs:115-151`, `GameSession.Online.cs:74,89`, `Net/Server/ZoneServer.cs:246`, `Net/Server/ServerHeroFactory.cs:31`, `Rpg/SaveSerializer.cs:49`, `Rpg/WantedBoss.cs:33-36`, `server-api/src/rpg/item-validation.js:41,133`, `Net/Protocol/NetOpcodes.cs:13`.

## Overview
P1 ưu tiên. Không art. Làm nền để thế giới live chỉ còn Taurus, nhưng code cũ vẫn biên dịch + test được.

## Key Insights
- Không có cờ ẩn map: mọi map trong `MapRegistry.Maps` được zone dựng và chơi được. Map id là chuỗi (wire chuỗi ≤ 32), không enum.
- `MapSpawnTable.ZonesFor` / `BossFor` rơi về quái + boss Frost Hollow cho map không có `case` -> map mới PHẢI có case riêng.
- Map id cũ rải ở: `WantedBoss` rotation, `DungeonPoints`, `CardTable.Regions`, `View25D.Maps`, `Map3D.Maps`, `ParseLaunch`, `ZoneHost`, `MapGraphTests` (BFS từ `frostHollow`), 6 file test map cũ đọc rect bằng regex.
- Save: id map lạ -> zone đưa về `host.Start`; client offline ở lại map hiện tại. Save bỏ nên không cần dịch toạ độ.
- Quest `first-hunt` AutoStart đếm mọi kill -> phải tắt khỏi live, không thì chồng quest Slime mới.

## Quyết định (tự chọn, KISS)
1. **Tắt, không xoá:** `MapRegistry` có 2 danh sách: `Live` (Taurus: `tauWillowbrookVillage`, `tauCloverMeadows`, sau thêm `tauAncientRootShrine`) và `Legacy` (16 map cũ). `All` = Live, cộng Legacy khi `MapRegistry.IncludeLegacy` (cờ `--legacy-maps` ở test build / capture cũ / dotnet test cũ). Zone chỉ host `All`. Xoá code legacy để phase 08 (hoặc sau, khi các vùng sau có thay thế).
2. **Một chỗ cho map xuất phát:** `MapRegistry.StartMapId = tauWillowbrookVillage`; 6 chỗ đang ghi `OakhavenVillageMap.MapId` (RpgSession, GameSession.Online x2, ZoneHost, ParseLaunch, server-api `newCharacterState`) đọc hằng này. Capture run cũ (`captureRun`) giữ mặc định Frost Hollow khi `IncludeLegacy`, không thì Start.
3. **Save:** `SaveVersion` 6 -> 7; `SaveSerializer.Parse` gặp version < 7 -> trả save mới (nhân vật mới), offline xoá file. server-api zod `mapId` default `'tauWillowbrookVillage'`. Online: xoá collection nhân vật trên Mongo lúc deploy (phase 07), không viết migrate.
4. **Quest / Wanted cũ:** `QuestCatalog.All` = quest Taurus (phase 02+); quest cũ chuyển sang `QuestCatalog.Legacy` (không AutoStart, không hiện). `WantedBoss.Rotation` lọc theo map có trong `All` -> live không có Wanted Boss tới khi Verdant Horn (ngoài phạm vi V4).
5. **Wire 36:** bump vì map id / MonsterDef mới / quest; ghi chú comment.

## Requirements
- Live chỉ thấy map Taurus; không portal nào trỏ map legacy khi legacy tắt (`ZoneHost.CheckPortalTargets` không log lỗi).
- `dotnet test` cả 2 project xanh: test map cũ bật `IncludeLegacy` trong fixture; `MapGraphTests` BFS từ `StartMapId` với Live, giữ bản legacy riêng.

## Related Code Files
- Sửa: `Maps/MapRegistry.cs`, `World/ZoneHost.cs`, `RpgSession.cs`, `GameSession.Online.cs`, `Rpg/SaveSerializer.cs`, `Rpg/QuestLog.cs` (tách Legacy), `Rpg/WantedBoss.cs`, `Net/Protocol/NetOpcodes.cs`, `server-api/src/rpg/item-validation.js`, `Tests/dotnet/ZenoniaNet.Tests/MapGraphTests.cs` + 6 test map cũ (fixture legacy), `Tests/dotnet/ZenoniaSkills.Tests/NewbieQuestTests.cs`, `QuestTests.cs`, `server-api/test/characters.test.js` (map mặc định).
- Tạo: không (map class thật ở phase 02; phase này có thể thêm map stub 1 ô để test, xoá khi 02 xong).

## Implementation Steps
1. `MapRegistry`: `Live`, `Legacy`, `IncludeLegacy`, `StartMapId`; `Get`/`FindLaunch` tìm trong `All`; `ResetState` -> Start.
2. Đổi 6 chỗ Oakhaven -> `StartMapId`; `--legacy-maps` trong `NetLaunchArgs` / `ParseLaunch`.
3. `SaveVersion` 7 + reset save cũ (offline + zone đọc save cũ trả nhân vật mới); zod default mapId.
4. `QuestCatalog.Legacy`, `WantedBoss` lọc map.
5. Version 36; sửa test; chạy 3 bộ test.

## Làm khác kế hoạch (24/11)
- Một cờ duy nhất `WorldFlags.LegacyEnabled` (`Rpg/WorldFlags.cs`, cùng `StartMapId`) thay cho `MapRegistry.IncludeLegacy` + cờ quest riêng; `MapRegistry.IncludeLegacy` chỉ đọc cờ này. Bật bằng `--legacy-maps` (NetLaunchArgs.Parse) hoặc tự bật khi chạy capture/bot cũ (Bootstrap); 2 project dotnet test bật qua `ModuleInitializer` (`LegacyWorldOnForTests.cs`).
- `MapRegistry.Get/FindLaunch/All` chặt: map legacy không tồn tại khi cờ tắt (mọi chỗ gọi `Get` đã kiểm null). `Live` hiện = 1 map stub `TaurusStartStubMap` (36x24 trống, an toàn, nền mượn ảnh làng cũ) mang id `tauWillowbrookVillage`; phase 02 thay bằng map thật. Stub chưa vào `View25D.Maps`.
- `QuestCatalog.All` thành property (Live + Legacy khi cờ bật); `Live` rỗng tới phase 02. `first-hunt` AutoStart không còn chạy ở live.
- Wanted Boss: không lọc Rotation (nhiều caller không chịu null). Thêm `WantedBoss.Enabled` (= cờ legacy): server không gửi status / không đặt boss, client `WantedBossTracker.Today` = null (HUD, board, guide, notice thoát sớm).
- `SaveSerializer.Parse` với version < 7 trả `Fresh`: giữ class, tên, ngoại hình; level 1, không vị trí, map = start. Bỏ luôn phép dịch toạ độ Frost Hollow (chỉ áp dụng save < v3) + xoá `FrostHollowSaveMigrationTests`; tham số `frostHollowRows` giữ nhưng không dùng. Offline: slot cũ đọc ra nhân vật mới (không còn `.bad`). `SaveSerializer.MapId` = start map.
- Pet Gacha: `PetGacha.Open` (= cờ legacy); đóng thì `Pull` / `ExchangeUr` báo "closed". TODO phase 08: pool chỉ Horn Rabbit N + Bramble Boar R (id `tau*`, chưa có def/art). Companion Spirit/Imp giữ nguyên.
- `RpgSession`: map đã lưu không tồn tại (đã tắt) = coi như không có vị trí lưu.
- Test: sửa 3 test cũ (PetTests, PetTeamGrowthCaptureTests, BagSizeTests) sang save v7 vì luật "save cũ còn tải được" đổi thật; thêm `SaveResetTests`; `MapGraphTests` BFS Live từ `StartMapId` + BFS Legacy từ frostHollow; `WantedRouteTests` lặp theo `Legacy`; wire test 36; server-api `card-state-schema` 7/8.
- Chưa làm (không thuộc phase 01): ẩn 40 card cũ, thắng Orin (phase 03), pet Taurus (phase 08), xoá Mongo (phase 07).

## Todo List
- [x] MapRegistry Live / Legacy / StartMapId
- [x] 6 chỗ start map + server-api default
- [x] SaveVersion 7, save cũ = nhân vật mới
- [x] QuestCatalog.Legacy, Wanted lọc
- [x] Wire 36
- [x] Test xanh (ZenoniaSkills, ZenoniaNet, vitest)

## Success Criteria
Game không cờ: tạo nhân vật -> vào map Start (stub / phase 02). `--legacy-maps --map frost-hollow` vẫn chạy như cũ. 3 bộ test xanh (trừ 2 FrostHollow fail sẵn có).

## Risk Assessment
- Capture / bot cũ (`DebugCapture.*`, `ZenoniaSwarm`) mặc định Frost Hollow: tự bật legacy khi có cờ capture cũ -> không vỡ.
- Card tutorial đang chưa commit dùng card cũ: phase 03 thay; commit card tutorial TRƯỚC khi bắt đầu phase này (tránh trộn diff).
- Quên một chỗ id cũ -> grep `MapId` các class legacy ngoài thư mục Maps.

## Security Considerations
Server vẫn quyết map (Welcome); client không tự chọn map online. Save cũ bị bỏ chỉ khi version < 7 (không xoá nhầm save mới).

## Next Steps
Phase 02 thêm 2 map thật vào `Live`.

## Câu hỏi cho người dùng
1. Đồng ý giữ code map / quái cũ (tắt khỏi game) tới khi xong Taurus rồi mới xoá? (Xoá ngay = sửa ~30 file test / capture.)
2. Production: xoá hẳn nhân vật Mongo lúc deploy (đề xuất) hay giữ tài khoản + pet tài khoản (pet là của account) chỉ reset nhân vật?
3. Pet Gacha: pool cũ (spirit, imp, frostSlime, hornRabbit cũ...) tắt hết, chỉ còn Horn Rabbit N + Bramble Boar R mới (BIBLE XI)? Pet companion spirit / robot của class giữ?
4. 40 card cũ: ẩn khỏi Album (đề xuất) hay xoá?
5. Dungeon 1 map 3 phòng (BIBLE) dù luật map 21 "dungeon nhiều tầng đi sâu"? Đề xuất theo BIBLE.
6. Map nền tạm (greybox) cho P0/P1 dùng màu phẳng sinh bằng script, CHỈ để test gameplay tới khi có art P2: đồng ý?
7. Quái dungeon: BIBLE muốn spawn theo phiên (instance). Game là map công cộng: đề xuất respawn chậm (120 s) + boss per hero (có sẵn), không làm instance. Đồng ý?
8. Card tutorial: V4 bắt THẮNG Orin mới xong quest; trước đây người dùng chốt "bỏ cuộc ngay = xong quest". Theo V4 (thua / hoà / bỏ cuộc đều đấu lại) hay giữ cũ?
9. Thưởng 300 G + 3 lá của quest Orin cũ bỏ, thay bằng "chọn 1 lá trong 8" theo V4 (Gold đã cho ở quest Slime)?
